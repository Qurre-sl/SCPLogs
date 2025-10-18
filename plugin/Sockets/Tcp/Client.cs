using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LabApi.Features.Console;
using Newtonsoft.Json;
using SCPLogs.Extensions;
using Console = GameCore.Console;

namespace SCPLogs.Sockets.Tcp;

public class Client : ISender
{
    private const int MaxFailedAttempts = 5;
    private const int ReconnectDelayMs = 5000;
    private readonly List<Message> _messages;
    private bool _alive;
    private TcpClient? _client;
    private int _failedAttempts;
    private NetworkStream? _stream;

    internal Client()
    {
        _messages = [];
        _alive = true;
        _failedAttempts = 0;

        _ = Task.Run(MaintainConnection);
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

    ~Client()
    {
        _alive = false;
        _stream?.Close();
        _client?.Close();
    }

    private async Task MaintainConnection()
    {
        while (_alive)
            try
            {
                if (_client?.Connected != true)
                {
                    Logger.Info("Attempting TCP connection...");
                    _client?.Close();
                    _client = new TcpClient();
                    await _client.ConnectAsync(Main.Instance.Config?.Ip ?? "127.0.0.1",
                        (int)(Main.Instance.Config?.Port ?? 8080));
                    _stream = _client.GetStream();
                    _failedAttempts = 0;
                    Logger.Info("TCP connected successfully");
                }

                await Task.Delay(5000);
            }
            catch (Exception ex)
            {
                _failedAttempts++;
                Logger.Debug($"TCP connection attempt failed ({_failedAttempts}/{MaxFailedAttempts}): {ex.Message}");
                _stream?.Close();
                _client?.Close();
                _stream = null;
                _client = null;

                await Task.Delay(ReconnectDelayMs);
            }
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

                foreach (var command in result.Commands)
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
            if (!_alive || _stream == null || !_stream.CanWrite)
                return string.Empty;

            var json = JsonConvert.SerializeObject(payload);
            var data = Encoding.UTF8.GetBytes(json);

            _stream.Write(data, 0, data.Length);

            if (!_stream.CanRead)
                return string.Empty;

            var buffer = new byte[65536];
            var bytesRead = _stream.Read(buffer, 0, buffer.Length);

            _failedAttempts = 0;
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }
        catch (Exception ex)
        {
            _failedAttempts++;
            Logger.Debug($"TCP request error ({_failedAttempts}/{MaxFailedAttempts}): {ex.Message}");

            if (_failedAttempts >= MaxFailedAttempts)
            {
                Logger.Warn("TCP connection lost, will attempt reconnection...");
                _stream?.Close();
                _client?.Close();
                _stream = null;
                _client = null;
            }

            return string.Empty;
        }
    }

    private class GetCommandsResponse
    {
        [JsonProperty("commands")] public Command[]? Commands { get; set; }
    }
}