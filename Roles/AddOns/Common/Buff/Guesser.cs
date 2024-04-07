using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using TownOfHost.Attributes;

namespace TownOfHost.Roles.AddOns.Common;

public static class Guesser
{
    private static readonly int Id = 75000;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Guesser);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "∮");
    private static List<byte> playerIdList = new();

    public static OptionItem CanGuessTime;
    public static OptionItem OwnCanGuessTime;
    public static OptionItem TryHideMsg;
    //crew
    public static OptionItem Crewmateset;
    public static OptionItem CCanGuessVanilla;
    public static OptionItem CCanGuessNakama;
    public static OptionItem CCanWhiteCrew;
    //imp
    public static OptionItem impset;
    public static OptionItem ICanGuessVanilla;
    public static OptionItem ICanGuessNakama;
    public static OptionItem ICanGuessTaskDoneSnitch;
    public static OptionItem ICanWhiteCrew;
    //mad
    public static OptionItem Madset;
    public static OptionItem MCanGuessVanilla;
    public static OptionItem MCanGuessNakama;
    public static OptionItem MCanGuessTaskDoneSnitch;
    public static OptionItem MCanWhiteCrew;
    //Neutral
    public static OptionItem Neuset;
    public static OptionItem NCanGuessVanilla;
    public static OptionItem NCanGuessTaskDoneSnitch;
    public static OptionItem NCanWhiteCrew;
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Guesser, fromtext: "<color=#ffffff>From:<color=#ff0000>The Other Roles</color></size>");
        AddOnsAssignData.Create(Id + 10, CustomRoles.Guesser, true, true, true, true);
        //共通設定
        CanGuessTime = FloatOptionItem.Create(Id + 49, "CanGuessTime", new(1, 15, 1), 3, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser])
                .SetValueFormat(OptionFormat.Players);
        OwnCanGuessTime = FloatOptionItem.Create(Id + 50, "OwnCanGuessTime", new(1, 15, 1), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser])
                .SetValueFormat(OptionFormat.Players);
        TryHideMsg = BooleanOptionItem.Create(Id + 51, "TryHideMsg", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        //クルーメイト
        Crewmateset = BooleanOptionItem.Create(Id + 52, "Cremateset", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        CCanGuessVanilla = BooleanOptionItem.Create(Id + 53, "CanGuessVanilla", true, TabGroup.Addons, false).SetParent(Crewmateset);
        CCanGuessNakama = BooleanOptionItem.Create(Id + 54, "CanGuessNakama", true, TabGroup.Addons, false).SetParent(Crewmateset);
        CCanWhiteCrew = BooleanOptionItem.Create(Id + 55, "CanWhiteCrew", false, TabGroup.Addons, false).SetParent(Crewmateset);
        //imp
        impset = BooleanOptionItem.Create(Id + 56, "Impset", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        ICanGuessVanilla = BooleanOptionItem.Create(Id + 57, "CanGuessVanilla", true, TabGroup.Addons, false).SetParent(impset);
        ICanGuessNakama = BooleanOptionItem.Create(Id + 58, "CanGuessNakama", true, TabGroup.Addons, false).SetParent(impset);
        ICanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 59, "CanGuessTaskDoneSnitch", false, TabGroup.Addons, false).SetParent(impset);
        ICanWhiteCrew = BooleanOptionItem.Create(Id + 60, "CanWhiteCrew", false, TabGroup.Addons, false).SetParent(impset);
        //mad
        Madset = BooleanOptionItem.Create(Id + 61, "Madset", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        MCanGuessVanilla = BooleanOptionItem.Create(Id + 62, "CanGuessVanilla", true, TabGroup.Addons, false).SetParent(Madset);
        MCanGuessNakama = BooleanOptionItem.Create(Id + 63, "CanGuessNakama", true, TabGroup.Addons, false).SetParent(Madset);
        MCanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 64, "CanGuessTaskDoneSnitch", true, TabGroup.Addons, false).SetParent(Madset);
        MCanWhiteCrew = BooleanOptionItem.Create(Id + 65, "CanWhiteCrew", false, TabGroup.Addons, false).SetParent(Madset);
        //Neu
        Neuset = BooleanOptionItem.Create(Id + 66, "Neuset", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guesser]);
        NCanGuessVanilla = BooleanOptionItem.Create(Id + 67, "CanGuessVanilla", true, TabGroup.Addons, false).SetParent(Neuset);
        NCanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 68, "CanGuessTaskDoneSnitch", false, TabGroup.Addons, false).SetParent(Neuset);
        NCanWhiteCrew = BooleanOptionItem.Create(Id + 69, "CanWhiteCrew", false, TabGroup.Addons, false).SetParent(Neuset);

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

}