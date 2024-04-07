using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Serial
    {
        private static readonly int Id = 70600;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Serial);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "∂");
        public static List<byte> playerIdList = new();
        public static OptionItem KillCooldown;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Serial);
            AddOnsAssignDataOnlyKiller.Create(Id + 10, CustomRoles.Serial, true, true, true, true);
            KillCooldown = FloatOptionItem.Create(Id + 50, "KillCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Serial])
                .SetValueFormat(OptionFormat.Seconds);
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