using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Nurse
    {
        private static readonly int Id = 71200;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Nurse);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Й");
        public static List<byte> playerIdList = new();
        public static OptionItem CanSeeComms;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Nurse);
            AddOnsAssignData.Create(Id + 10, CustomRoles.Nurse, true, true, true, true);
            CanSeeComms = BooleanOptionItem.Create(Id + 50, "CanseeComms", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Nurse]);
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