using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using TownOfHost.Roles.AddOns.Common;

namespace TownOfHost.Roles.AddOns.Buff
{
    public static class Speeding
    {
        private static readonly int Id = 75200;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Speeding);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "∈");
        public static List<byte> playerIdList = new();
        private static OptionItem OptionSpeed;
        public static float Speed;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Speeding);
            AddOnsAssignData.Create(Id + 10, CustomRoles.Speeding, true, true, true, true);
            OptionSpeed = FloatOptionItem.Create(Id + 50, "Speed", new(0.5f, 10f, 0.25f), 2f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Speeding]);
        }
        public static void Init()
        {
            playerIdList = new();
            Speed = OptionSpeed.GetFloat();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

    }
}