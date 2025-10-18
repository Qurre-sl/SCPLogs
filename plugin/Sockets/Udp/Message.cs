using Newtonsoft.Json;

namespace SCPLogs.Sockets.Udp;

public readonly struct Message(string text, string[] channels)
{
    [JsonProperty("text")]
    public string Text { get; } = text;

    [JsonProperty("channels")]
    public string[] Channels { get; } = channels;
}
