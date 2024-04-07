using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using TownOfHost.Roles.AddOns.Common;

namespace TownOfHost.Roles.AddOns.DeBuff
{
    public static class Elector
    {
        private static readonly int Id = 70500;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Elector);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｅ");
        public static List<byte> playerIdList = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Elector);
            AddOnsAssignData.Create(Id + 10, CustomRoles.Elector, true, true, true, true);
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