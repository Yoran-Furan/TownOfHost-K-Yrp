using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Connecting
    {
        private static readonly int Id = 75500;
        public static Color RoleColor = Utils.GetRoleColor(CustomRoles.Connecting);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Ψ");
        public static List<byte> playerIdList = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Connecting, new(2, 15, 1));
            AddOnsAssignDataTeamImp.Create(Id + 10, CustomRoles.Connecting, true, true);
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