using System;
using System.Text.RegularExpressions;
using Qurre.API;

namespace SCPLogs.Extensions;

internal static class EventsExtensions
{
    private static readonly Regex BlockRegex = new("^[\u0600-\u065F\u066A-\u06EF\u06FA-\u06FF]+$");

    internal static string GetTime()
    {
        return $"[<t:{DateTimeOffset.Now.ToUnixTimeSeconds()}:T>] ";
    }

    internal static void SendLog(string message, string[] channels)
    {
        if (Main.Sender is null)
        {
            Log.Warn("Sender is null");
            Log.Debug("Trying to send log: " + message);
            return;
        }

        Main.Sender.Send(GetTime() + message, channels);
    }

    internal static string PrintPlayer(Player player, bool printRole = true)
    {
        string nickname = BlockRegex.Replace(player.UserInformation.Nickname, "?");
        string reply = $"`{nickname}` - {player.UserInformation.UserId}";

        if (printRole)
            reply += $" ({GetRolePrint(player)})";

        return reply;
    }

    internal static string GetRolePrint(Player player)
    {
        return $"{player.RoleInformation.Role}";
    }
}