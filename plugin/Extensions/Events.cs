using System;
using System.Text.RegularExpressions;
using PlayerRoles;
using Qurre.API;

namespace SCPLogs.Extensions;

internal static class EventsExtensions
{
    private static readonly Regex BlockRegex = new("^[\u0600-\u065F\u066A-\u06EF\u06FA-\u06FF]+$");

    internal static string GetTime()
    {
        return $"[<t:{DateTimeOffset.Now.ToUnixTimeSeconds()}:T>]";
    }

    internal static void SendLog(string message, string[] channels)
    {
        if (Main.Sender is null)
        {
            Log.Warn("Sender is null");
            Log.Debug("Trying to send log: " + message);
            return;
        }

        string time = string.Empty;
        
        if (!message.Contains("<t:"))
            time = GetTime() + " ";

        Main.Sender.Send(time + message, channels);
    }

    internal static bool IsOneFraction(Player first, Player second)
    {
        RoleTypeId roleType1 = first.RoleInformation.Role;
        RoleTypeId roleType2 = second.RoleInformation.Role;

        if (roleType1 is RoleTypeId.Spectator or RoleTypeId.None)
            roleType1 = first.RoleInformation.CachedRole;

        if (roleType2 is RoleTypeId.Spectator or RoleTypeId.None)
            roleType2 = second.RoleInformation.CachedRole;

        return roleType1.GetFaction() == roleType2.GetFaction();
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
        RoleTypeId roleType = player.RoleInformation.Role;

        if (roleType is RoleTypeId.Spectator or RoleTypeId.None)
            roleType = player.RoleInformation.CachedRole;

        return $"{roleType}";
    }
}