using Newtonsoft.Json;

namespace SCPLogs.Sockets;

internal class Command
{
    [JsonProperty("command")] internal string Raw { get; set; } = string.Empty;

    [JsonProperty("reply")] internal string Reply { get; set; } = string.Empty;

    [JsonProperty("author")] internal string Author { get; set; } = string.Empty;
}