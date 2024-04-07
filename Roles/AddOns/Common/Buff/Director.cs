using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Director
    {
        private static readonly int Id = 75400;
        public static Color RoleColor = Utils.GetRoleColor(CustomRoles.Director);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "θ");
        public static List<byte> playerIdList = new();
        private static OptionItem OptionPercentGage;
        private static OptionItem Optioncomms;
        public static bool comms;
        public static bool PercentGage;
        public static OptionItem Meeting;
        public static OptionItem PonkotuPercernt;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Director);
            AddOnsAssignData.Create(Id + 10, CustomRoles.Director, true, true, true, true);
            OptionPercentGage = BooleanOptionItem.Create(Id + 50, "PercentGage", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Director]);
            PonkotuPercernt = BooleanOptionItem.Create(Id + 51, "PonkotuPercernt", true, TabGroup.Addons, false).SetParent(OptionPercentGage);
            Optioncomms = BooleanOptionItem.Create(Id + 55, "CanseeComms", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Director]);
            Meeting = BooleanOptionItem.Create(Id + 56, "CanseeMeeting", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Director]);
        }
        public static void Init()
        {
            playerIdList = new();
            PercentGage = OptionPercentGage.GetBool();
            comms = Optioncomms.GetBool();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

    }
}