/*using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class NotConvener
    {
        private static readonly int Id = 70000;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.NotConvener);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｃ");
        public static List<byte> playerIdList = new();
        public static OptionItem OptionConvener;
        public static Convener Mode;
        public enum Convener
        {
            NotButton,
            NotReport,
            ConvenerAll,
        }
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.NotConvener);
            AddOnsAssignData.Create(Id + 10, CustomRoles.NotConvener, true, true, true, true);
            OptionConvener = StringOptionItem.Create(50, "ConverMode", EnumHelper.GetAllNames<Convener>(), 0, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.NotConvener]);
        }
        public static bool CancelReportDeadBody(PlayerControl repo, GameData.PlayerInfo oniku)
        {
            if (repo.Is(CustomRoles.GrimReaper) && oniku != null && (Mode == Convener.NotReport || Mode == Convener.ConvenerAll))
            {
                Logger.Info("ボタンをキャンセル。", "NotConvener");
                return true;
            }
            if (repo.Is(CustomRoles.GrimReaper) && oniku == null && (Mode == Convener.NotButton || Mode == Convener.ConvenerAll))
            {
                Logger.Info("ボタンも使えない。", "NotConvener");
                return true;
            }
            return false;
        }
        public static void Init()
        {
            playerIdList = new();
            Mode = (Convener)OptionConvener.GetValue();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

    }
}*/