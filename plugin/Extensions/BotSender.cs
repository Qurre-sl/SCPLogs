namespace SCPLogs.Extensions;

internal class BotSender : CommandSender
{
    private readonly string _argument;

    internal BotSender(string name, string argument)
    {
        Nickname = name;
        _argument = argument;
    }

    public override string Nickname { get; }

    public override string SenderId => "SERVER CONSOLE";
    public override ulong Permissions => ServerStatic.PermissionsHandler.FullPerm;
    public override byte KickPower => byte.MaxValue;
    public override bool FullPermissions => true;

    public override bool Available()
    {
        return true;
    }

    public override void RaReply(string text, bool success, bool logToConsole, string overrideDisplay)
    {
        Main.Sender?.Reply(text, _argument);
    }

    public override void Print(string text)
    {
        Main.Sender?.Reply(text, _argument);
    }
}