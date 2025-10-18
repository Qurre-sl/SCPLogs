using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LabApi.Features.Console;
using Newtonsoft.Json;
using SCPLogs.Extensions;

namespace SCPLogs.Sockets.Udp;

public class Client : ISender
{
    private UdpClient? _client;
    private IPEndPoint? _endpoint;
    private bool _alive;
    private readonly List<Message> _messages;
    private int _failedAttempts;
    private const int MaxFailedAttempts = 5;

    internal Client()
    {
        _messages = [];
        _alive = true;
        _failedAttempts = 0;

        try
        {
            _endpoint = new IPEndPoint(IPAddress.Parse(Main.Instance.Config?.Ip ?? "127.0.0.1"), (int)(Main.Instance.Config?.Port ?? 8080));
            _client = new UdpClient();

            _ = Task.Run(CollectMessages);
            _ = Task.Run(GetCommands);
            _ = Task.Run(UpdateOnline);
            _ = Task.Run(HandShake);
        }
        catch (Exception ex)
        {
            Logger.Error($"UDP initialization error: {ex}");
            _alive = false;
        }
    }

    ~Client()
    {
        _alive = false;
        _client?.Close();
    }

    public void Send(string data, string[] channels)
    {
        _messages.Add(new Message(data, channels));
    }

    public void Reply(string data, string argument)
    {
        _ = Task.Run(() =>
        {
            SendRequest(new
            {
                token = Main.Instance.Config?.ClientToken ?? "",
                action = "Reply",
                data,
                source = argument
            });
        });
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
                var payload = new
                {
                    token = Main.Instance.Config?.ClientToken ?? "",
                    action = "SendLog",
                    messages = _messages.ToArray()
                };

                _messages.Clear();

                SendRequest(payload);
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

            try
            {
                var response = SendRequest(new
                {
                    token = Main.Instance.Config?.ClientToken ?? "",
                    action = "GetCommands"
                });

                if (string.IsNullOrEmpty(response))
                    continue;

                var result = JsonConvert.DeserializeObject<GetCommandsResponse>(response);

                if (result?.Commands == null)
                    continue;

                foreach (Command command in result.Commands)
                    try
                    {
                        GameCore.Console.singleton.TypeCommand("/" + command.Raw,
                            new BotSender(command.Author, command.Reply));
                    }
                    catch (Exception e)
                    {
                        Logger.Debug(e);
                    }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Error getting commands: {ex}");
            }
        }
    }

    private async Task UpdateOnline()
    {
        while (_alive)
        {
            try
            {
                SendRequest(new
                {
                    token = Main.Instance.Config?.ClientToken ?? "",
                    action = "UpdateOnline",
                    data = SocketExtensions.GetOnline()
                });
            }
            catch (Exception ex)
            {
                Logger.Debug($"Error updating online: {ex}");
            }

            await Task.Delay(30000);
        }
    }

    private async Task HandShake()
    {
        while (_alive)
        {
            try
            {
                SendRequest(new
                {
                    token = Main.Instance.Config?.ClientToken ?? "",
                    action = "UpdateHandShake"
                });
            }
            catch (Exception ex)
            {
                Logger.Debug($"Error updating handshake: {ex}");
            }

            await Task.Delay(1000);
        }
    }

    private string SendRequest(object payload)
    {
        try
        {
            if (!_alive || _client == null)
                return string.Empty;

            var json = JsonConvert.SerializeObject(payload);
            var data = Encoding.UTF8.GetBytes(json);

            _client.Send(data, data.Length, _endpoint);

            var asyncResult = _client.BeginReceive(null, null);
            asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));

            if (!asyncResult.IsCompleted)
            {
                _failedAttempts++;
                if (_failedAttempts >= MaxFailedAttempts)
                {
                    Logger.Warn("UDP communication timeout, recreating client...");
                    RecreateClient();
                }
                return string.Empty;
            }

            var response = _client.EndReceive(asyncResult, ref _endpoint);
            _failedAttempts = 0;
            return Encoding.UTF8.GetString(response);
        }
        catch (Exception ex)
        {
            _failedAttempts++;
            Logger.Debug($"UDP request error ({_failedAttempts}/{MaxFailedAttempts}): {ex.Message}");

            if (_failedAttempts >= MaxFailedAttempts)
            {
                Logger.Warn("UDP connection lost, recreating client...");
                RecreateClient();
            }

            return string.Empty;
        }
    }

    private void RecreateClient()
    {
        try
        {
            _client?.Close();
            _client = new UdpClient();
            _failedAttempts = 0;
            Logger.Info("UDP client recreated successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to recreate UDP client: {ex}");
        }
    }

    private class GetCommandsResponse
    {
        [JsonProperty("commands")]
        public Command[]? Commands { get; set; }
    }
}
