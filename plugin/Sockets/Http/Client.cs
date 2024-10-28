using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MEC;
using Newtonsoft.Json;
using Qurre.API;
using SCPLogs.Extensions;

namespace SCPLogs.Sockets.Http;

public class Client : ISender
{
    private readonly HttpClient _httpClient;
    private readonly Uri _host;
    private bool _alive;
    private readonly List<Message> _messages;
    
    internal Client()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", Main.GlobalConfig.ClientToken);

        _host = new Uri($"http://{Main.GlobalConfig.Ip}:{Main.GlobalConfig.Port}/");
        _alive = true;
        _messages = [];

        new Task(CollectMessages).Start();
        new Task(GetCommands).Start();
        new Task(UpdateOnline).Start();
        new Task(HandShake).Start();
    }
    
    ~Client()
        => _alive = false;

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

        new Task(() => {
            HandleRequest(_httpClient.PostAsync(_host.AbsoluteUri + "Reply", content),
                "sending the command response").Wait();
        }).Start();
    }

    private async void CollectMessages()
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
                Log.Debug(ex);
            }

        }
    }

    private async void GetCommands()
    {
        while (_alive)
        {
            await Task.Delay(1000);

            string reply = await HandleRequestAndGet(_httpClient.GetAsync(_host.AbsoluteUri + "Commands"),
                "getting commands from client");

            if (string.IsNullOrEmpty(reply))
                continue;

            var json = JsonConvert.DeserializeObject<Command[]>(reply);

            if (json is null)
                continue;

            foreach (Command command in json)
                try
                {
                    GameCore.Console.singleton.TypeCommand("/" + command.Raw,
                        new BotSender(command.Author, command.Reply));
                }
                catch (Exception e)
                {
                    Log.Debug(e);
                }
        }
    }

    private async void UpdateOnline()
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

    private async void HandShake()
    {
        while (_alive)
        {
            await HandleRequest(_httpClient.GetAsync(_host.AbsoluteUri + "UpdateHandShake"),
                "updating HandShake");

            await Task.Delay(1000);
        }
    }

    private static async Task HandleRequest(Task<HttpResponseMessage> task, string job = "unknown")
    {
        try
        {
            HttpResponseMessage response = await task;
        
            if (response.StatusCode == HttpStatusCode.OK)
                return;
        
            string responseString = await response.Content.ReadAsStringAsync();
            Log.Error($"Caused error when {job}:\n{responseString}");
        }
        catch (Exception ex)
        {
            Log.Debug($"Caused error in {job}:\n{ex}");
            await Task.Delay(30000);
        }
    }
    
    private static async Task<string> HandleRequestAndGet(Task<HttpResponseMessage> task, string job = "unknown")
    {
        try
        {
            HttpResponseMessage response = await task;
            string responseString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
                return responseString;

            Log.Error($"Caused error when {job}:\n{responseString}");
        }
        catch (Exception ex)
        {
            Log.Debug($"Caused error in {job}:\n{ex}");
            await Task.Delay(30000);
        }
        
        return string.Empty;
    }
}