using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Watcher
    {
        private static readonly int Id = 74900;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Watcher);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "∑");
        private static List<byte> playerIdList = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Watcher, fromtext: "<color=#ffffff>From:<color=#ff0000>TOR_GM_Edition</color></size>");
            AddOnsAssignData.Create(Id + 10, CustomRoles.Watcher, true, true, true, true);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

    }
}