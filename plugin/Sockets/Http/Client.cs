using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using LabApi.Features.Console;
using Newtonsoft.Json;
using SCPLogs.Extensions;
using Console = GameCore.Console;

namespace SCPLogs.Sockets.Http;

public class Client : ISender
{
    private const int MaxFailedAttempts = 5;
    private const int ReconnectDelayMs = 5000;
    private readonly Uri _host;
    private readonly HttpClient _httpClient;
    private readonly List<Message> _messages;
    private bool _alive;
    private int _failedAttempts;

    internal Client()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Main.Instance.Config?.ClientToken ?? "");

        _host = new Uri($"http://{Main.Instance.Config?.Ip ?? "127.0.0.1"}:{Main.Instance.Config?.Port ?? 8080}/");
        _alive = true;
        _messages = [];
        _failedAttempts = 0;

        _ = Task.Run(CollectMessages);
        _ = Task.Run(GetCommands);
        _ = Task.Run(UpdateOnline);
        _ = Task.Run(HandShake);
    }

    public void Send(string data, string[] channels)
    {
        _messages.Add(new Message(data, channels));
    }

    public void Reply(string data, string argument)
    {
        FormUrlEncodedContent content = new(new Dictionary<string, string>
        {
            { "data", data },
            { "source", argument }
        });

        _ = Task.Run(async () =>
        {
            await HandleRequest(_httpClient.PostAsync(_host.AbsoluteUri + "Reply", content),
                "sending the command response");
        });
    }

    ~Client()
    {
        _alive = false;
    }

    private async Task CollectMessages()
    {
        while (_alive)
        {
            await Task.Delay(2000);

            if (_messages.Count == 0)
                continue;

            try
            {
                FormUrlEncodedContent content = new(new Dictionary<string, string>
                {
                    { "messages", JsonConvert.SerializeObject(_messages) }
                });

                _messages.Clear();

                await HandleRequest(_httpClient.PostAsync(_host.AbsoluteUri + "SendLog", content),
                    "sending logs");
            }
            catch (Exception ex)
            {
                Logger.Debug(ex);
            }
        }
    }

    private async Task GetCommands()
    {
        while (_alive)
        {
            await Task.Delay(1000);

            var reply = await HandleRequestAndGet(_httpClient.GetAsync(_host.AbsoluteUri + "Commands"),
                "getting commands from client");

            if (string.IsNullOrEmpty(reply))
                continue;

            var json = JsonConvert.DeserializeObject<Command[]>(reply);

            if (json is null)
                continue;

            foreach (var command in json)
                try
                {
                    Console.singleton.TypeCommand("/" + command.Raw,
                        new BotSender(command.Author, command.Reply));
                }
                catch (Exception e)
                {
                    Logger.Debug(e);
                }
        }
    }

    private async Task UpdateOnline()
    {
        while (_alive)
        {
            FormUrlEncodedContent content = new(new Dictionary<string, string>
            {
                { "data", SocketExtensions.GetOnline() }
            });

            await HandleRequest(_httpClient.PostAsync(_host.AbsoluteUri + "UpdateOnline", content),
                "updating the status with online");

            await Task.Delay(30000);
        }
    }

    private async Task HandShake()
    {
        while (_alive)
        {
            await HandleRequest(_httpClient.GetAsync(_host.AbsoluteUri + "UpdateHandShake"),
                "updating HandShake");

            await Task.Delay(1000);
        }
    }

    private async Task HandleRequest(Task<HttpResponseMessage> task, string job = "unknown")
    {
        try
        {
            var response = await task;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _failedAttempts = 0;
                return;
            }

            var responseString = await response.Content.ReadAsStringAsync();
            Logger.Error($"Caused error when {job}:\n{responseString}");
            await HandleFailure();
        }
        catch (Exception ex)
        {
            Logger.Debug($"Caused error in {job}:\n{ex}");
            await HandleFailure();
        }
    }

    private async Task<string> HandleRequestAndGet(Task<HttpResponseMessage> task, string job = "unknown")
    {
        try
        {
            var response = await task;
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _failedAttempts = 0;
                return responseString;
            }

            Logger.Error($"Caused error when {job}:\n{responseString}");
            await HandleFailure();
        }
        catch (Exception ex)
        {
            Logger.Debug($"Caused error in {job}:\n{ex}");
            await HandleFailure();
        }

        return string.Empty;
    }

    private async Task HandleFailure()
    {
        _failedAttempts++;

        if (_failedAttempts >= MaxFailedAttempts)
        {
            Logger.Warn($"HTTP connection lost after {_failedAttempts} failed attempts. Reconnecting...");
            _failedAttempts = 0;
        }

        await Task.Delay(ReconnectDelayMs);
    }
}