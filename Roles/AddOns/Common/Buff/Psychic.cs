using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Psychic
    {
        private static readonly int Id = 71000;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Psychic);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "☯");
        public static List<byte> playerIdList = new();
        public static OptionItem CanSeeComms;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Psychic);
            AddOnsAssignData.Create(Id + 10, CustomRoles.Psychic, true, true, true, true);
            CanSeeComms = BooleanOptionItem.Create(Id + 50, "CanseeComms", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
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