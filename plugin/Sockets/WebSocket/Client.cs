using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LabApi.Features.Console;
using Newtonsoft.Json;
using SCPLogs.Extensions;
using Console = GameCore.Console;

namespace SCPLogs.Sockets.WebSocket;

public class Client : ISender
{
    private const int MaxFailedAttempts = 5;
    private const int ReconnectDelayMs = 5000;
    private readonly List<Message> _messages;
    private bool _alive;
    private CancellationTokenSource _cts;
    private int _failedAttempts;
    private ClientWebSocket? _webSocket;

    internal Client()
    {
        _messages = [];
        _alive = true;
        _cts = new CancellationTokenSource();
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
        _ = Task.Run(async () =>
        {
            await SendRequest(new
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
        _cts.Cancel();
        _webSocket?.Dispose();
    }

    private async Task MaintainConnection()
    {
        while (_alive)
            try
            {
                if (_webSocket?.State != WebSocketState.Open)
                {
                    Logger.Info("Attempting WebSocket connection...");

                    _webSocket?.Dispose();
                    _cts.Cancel();
                    _cts = new CancellationTokenSource();
                    _webSocket = new ClientWebSocket();

                    var uri = new Uri(
                        $"ws://{Main.Instance.Config?.Ip ?? "127.0.0.1"}:{Main.Instance.Config?.Port ?? 8080}");
                    await _webSocket.ConnectAsync(uri, _cts.Token);
                    _failedAttempts = 0;
                    Logger.Info($"WebSocket connected to {uri}");
                }

                await Task.Delay(5000);
            }
            catch (Exception ex)
            {
                _failedAttempts++;
                Logger.Debug(
                    $"WebSocket connection attempt failed ({_failedAttempts}/{MaxFailedAttempts}): {ex.Message}");

                _webSocket?.Dispose();
                _webSocket = null;

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

                await SendRequest(payload);
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
                var response = await SendRequest(new
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
                await SendRequest(new
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
                await SendRequest(new
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

    private async Task<string> SendRequest(object payload)
    {
        try
        {
            if (!_alive || _webSocket?.State != WebSocketState.Open)
                return string.Empty;

            var json = JsonConvert.SerializeObject(payload);
            var data = Encoding.UTF8.GetBytes(json);

            await _webSocket.SendAsync(
                new ArraySegment<byte>(data),
                WebSocketMessageType.Text,
                true,
                _cts.Token
            );

            var buffer = new byte[65536];
            var result = await _webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                _cts.Token
            );

            _failedAttempts = 0;
            return Encoding.UTF8.GetString(buffer, 0, result.Count);
        }
        catch (Exception ex)
        {
            _failedAttempts++;
            Logger.Debug($"WebSocket request error ({_failedAttempts}/{MaxFailedAttempts}): {ex.Message}");

            if (_failedAttempts >= MaxFailedAttempts)
            {
                Logger.Warn("WebSocket connection lost, will attempt reconnection...");
                _webSocket?.Dispose();
                _webSocket = null;
            }

            return string.Empty;
        }
    }

    private class GetCommandsResponse
    {
        [JsonProperty("commands")] public Command[]? Commands { get; set; }
    }
}