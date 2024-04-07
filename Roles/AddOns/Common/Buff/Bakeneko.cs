using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Bakeneko
    {
        private static readonly int Id = 71100;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Bakeneko);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "=^^= ");
        public static List<byte> playerIdList = new();
        public static OptionItem Imp;
        public static OptionItem Crew;
        public static OptionItem Mad;
        public static OptionItem Neu;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Bakeneko);
            AddOnsAssignData.Create(Id + 10, CustomRoles.Bakeneko, true, true, true, true);
            Imp = BooleanOptionItem.Create(Id + 50, "NekoKabochaImpostorsGetRevenged", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bakeneko]);
            Crew = BooleanOptionItem.Create(Id + 51, "NekomataCanCrew", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bakeneko]);
            Mad = BooleanOptionItem.Create(Id + 52, "NekoKabochaMadmatesGetRevenged", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bakeneko]);
            Neu = BooleanOptionItem.Create(Id + 53, "NekomataCanNeu", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bakeneko]);
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