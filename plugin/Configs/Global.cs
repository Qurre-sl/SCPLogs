using SCPLogs.Sockets;

namespace SCPLogs.Configs;

public class Global
{
    public string Ip { get; set; } = "127.0.0.1";
    public uint Port { get; set; } = 8080;
    public Protocol Protocol { get; set; } = Protocol.Udp;
    public string ClientToken { get; set; } = "GENERATE RANDOM";
    public string BadgeOnline { get; set; } = "reply = string.format(\"%s/%s players\", Count, Slots)";
    public string[] SendUnAllowedEvents { get; set; } = ["RemoteAdminCommandEvent", "GameConsoleCommandEvent"];
}