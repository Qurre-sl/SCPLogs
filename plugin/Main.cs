using System;
using JetBrains.Annotations;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Loader.Features.Plugins;
using SCPLogs.Configs;
using SCPLogs.Sockets;
using SCPLogs.Sockets.Http;

namespace SCPLogs;

[PublicAPI]
public class Main : Plugin<Global>
{
    public override string Name { get; } = "SCP Logs";
    public override string Description { get; } = "Send game events to external services via sockets";
    public override string Author { get; } = "ZXC Team";
    public override Version Version { get; } = new(3, 0, 0);
    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);
    public override string ConfigFileName { get; set; } = "scplogs.yml";

    internal static Main Instance { get; private set; } = null!;

    public static ISender? Sender { get; private set; }

    public override void LoadConfigs()
    {
        Instance = this;
        base.LoadConfigs();

        if (Config == null)
        {
            Logger.Error("Failed to load config, using defaults");
            Config = new Global();
        }

        LuaConfig.Load(Config.LuaConfig);
    }

    public override void Enable()
    {
        if (Config == null)
        {
            Logger.Error("Config is null, cannot enable plugin");
            return;
        }

        Sender = Config.Protocol switch
        {
            Protocol.Http => new Client(),
            Protocol.Tcp => new Sockets.Tcp.Client(),
            Protocol.Udp => new Sockets.Udp.Client(),
            Protocol.WebSocket => new Sockets.WebSocket.Client(),
            _ => null
        };

        if (Sender == null)
        {
            Logger.Warn($"Unknown protocol: {Config.Protocol}");
            return;
        }

        Events.Load();
    }

    public override void Disable()
    {
        Events.Unload();
        Sender = null;
    }
}