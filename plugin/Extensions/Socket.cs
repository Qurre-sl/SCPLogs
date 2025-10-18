using LabApi.Features.Wrappers;
using MoonSharp.Interpreter;
using SCPLogs.Lua;

namespace SCPLogs.Extensions;

internal static class SocketExtensions
{
    internal static string GetOnline()
    {
        Script luaScript = new();
        Internal.PrepareTable(luaScript.Globals);

        luaScript.Globals["Count"] = Player.List.Count;
        luaScript.Globals["Slots"] = CustomNetworkManager.slots;

        luaScript.DoString(Main.Instance.Config?.BadgeOnline ?? "reply = \"\"");
        var reply = luaScript.Globals.Get("reply");

        if (reply.IsNil())
            return string.Empty;

        return reply.Type == DataType.String ? reply.String : reply.ToString();
    }
}