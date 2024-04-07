using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Moon
    {
        private static readonly int Id = 75100;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Moon);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "э");
        public static List<byte> playerIdList = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Moon);
            AddOnsAssignDataNotImp.Create(Id + 10, CustomRoles.Moon, true, true, true);
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