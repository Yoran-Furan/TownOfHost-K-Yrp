using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class AdditionalVoter
    {
        private static readonly int Id = 70700;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.AdditionalVoter);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Å");
        public static List<byte> playerIdList = new();
        public static OptionItem AdditionalVote;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.AdditionalVoter);
            AddOnsAssignData.Create(Id + 10, CustomRoles.AdditionalVoter, true, true, true, true);
            AdditionalVote = IntegerOptionItem.Create(Id + 50, "MayorAdditionalVote", new(1, 99, 1), 1, TabGroup.Addons, false).SetValueFormat(OptionFormat.Votes).SetParent(CustomRoleSpawnChances[CustomRoles.AdditionalVoter]);
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