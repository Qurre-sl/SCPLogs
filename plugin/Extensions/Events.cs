using System;
using System.Text.RegularExpressions;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using PlayerRoles;

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
            Logger.Warn("Sender is null");
            Logger.Debug("Trying to send log: " + message);
            return;
        }

        string time = string.Empty;

        if (!message.Contains("<t:"))
            time = GetTime() + " ";

        Main.Sender.Send(time + message, channels);
    }

    internal static bool IsOneFraction(Player first, Player second)
    {
        RoleTypeId roleType1 = first.Role;
        RoleTypeId roleType2 = second.Role;

        /* TODO: make it when will add in labapi
        if (roleType1 is RoleTypeId.Spectator or RoleTypeId.None)
            roleType1 = first.PreviousRole;

        if (roleType2 is RoleTypeId.Spectator or RoleTypeId.None)
            roleType2 = second.PreviousRole;
        */

        return roleType1.GetFaction() == roleType2.GetFaction();
    }

    internal static string PrintPlayer(Player player, bool printRole = true)
    {
        string nickname = BlockRegex.Replace(player.DisplayName, "?");
        string reply = $"`{nickname}` - {player.UserId}";

        if (printRole)
            reply += $" ({GetRolePrint(player)})";

        return reply;
    }

    internal static string GetRolePrint(Player player)
    {
        RoleTypeId roleType = player.CurrentRole;

        if (roleType is RoleTypeId.Spectator or RoleTypeId.None)
            roleType = player.PreviousRole;

        return $"{roleType}";
    }
}