/*using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using TownOfHost.Attributes;

namespace TownOfHost.Roles.AddOns.Common;

public static class Guesser
{
    private static readonly int Id = 79000;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Guesser);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｇ");
    private static List<byte> playerIdList = new();

    public static OptionItem CanGuessTime;
    public static OptionItem CanGuessVanilla;
    public static OptionItem CanGuessNakama;
    public static OptionItem CanGuessTaskDoneSnitch;
    public static OptionItem TryHideMsg;
    public static OptionItem ChangeGuessDeathReason;
    public static OptionItem CanWhiteCrew;
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Guesser);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Guesser, true, true, true, true);
        CanGuessTime = FloatOptionItem.Create(Id + 50, "CanGuessTime", new(1, 15, 1), 3, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser])
                .SetValueFormat(OptionFormat.Players);
        CanGuessVanilla = BooleanOptionItem.Create(Id + 51, "CanGuessVanilla", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        CanGuessNakama = BooleanOptionItem.Create(Id + 52, "CanGuessNakama", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        CanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 53, "CanGuessTaskDoneSnitch", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        TryHideMsg = BooleanOptionItem.Create(Id + 54, "TryHideMsg", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        ChangeGuessDeathReason = BooleanOptionItem.Create(Id + 55, "ChangeGuessDeathReason", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        CanWhiteCrew = BooleanOptionItem.Create(Id + 56, "CanWhiteCrew", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
    }
    [GameModuleInitializer]

    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

}*/