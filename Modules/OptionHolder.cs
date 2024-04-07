using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Crewmate;
using TownOfHost.Roles.AddOns.Neutral;

namespace TownOfHost
{
    [Flags]
    public enum CustomGameMode
    {
        Standard, //= 0x01,
        HideAndSeek, //= 0x02,
        TaskBattle, //= 0x03,
        All = int.MaxValue
    }

    [HarmonyPatch]
    public static class Options
    {
        static Task taskOptionsLoad;
        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.Initialize)), HarmonyPostfix]
        public static void OptionsLoadStart()
        {
            Logger.Info("Options.Load Start", "Options");
            taskOptionsLoad = Task.Run(Load);
        }
        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
        public static void WaitOptionsLoad()
        {
            taskOptionsLoad.Wait();
            Logger.Info("Options.Load End", "Options");
        }

        // プリセット
        private static readonly string[] presets =
        {
            Main.Preset1.Value, Main.Preset2.Value, Main.Preset3.Value,
            Main.Preset4.Value, Main.Preset5.Value
        };

        // ゲームモード
        public static OptionItem GameMode;
        public static CustomGameMode CurrentGameMode => (CustomGameMode)GameMode.GetValue();

        public static readonly string[] gameModes =
        {
            "Standard", "HideAndSeek","TaskBattle",
        };

        // MapActive
        public static bool IsActiveSkeld => AddedTheSkeld.GetBool() || Main.NormalOptions.MapId == 0;
        public static bool IsActiveMiraHQ => AddedMiraHQ.GetBool() || Main.NormalOptions.MapId == 1;
        public static bool IsActivePolus => AddedPolus.GetBool() || Main.NormalOptions.MapId == 2;
        public static bool IsActiveAirship => AddedTheAirShip.GetBool() || Main.NormalOptions.MapId == 4;
        public static bool IsActiveFungle => AddedTheFungle.GetBool() || Main.NormalOptions.MapId == 5;

        // 役職数・確率
        public static Dictionary<CustomRoles, OptionItem> CustomRoleCounts;
        public static Dictionary<CustomRoles, IntegerOptionItem> CustomRoleSpawnChances;
        public static readonly string[] rates =
        {
            "Rate0",  "Rate5",  "Rate10", "Rate20", "Rate30", "Rate40",
            "Rate50", "Rate60", "Rate70", "Rate80", "Rate90", "Rate100",
        };

        // 各役職の詳細設定
        public static OptionItem EnableGM;
        public static float DefaultKillCooldown = Main.NormalOptions?.KillCooldown ?? 20;
        public static OptionItem DefaultShapeshiftCooldown;
        public static OptionItem CanMakeMadmateCount;
        public static OptionItem MadMateOption;
        public static OptionItem MadmateCanSeeKillFlash;
        public static OptionItem MadmateCanSeeDeathReason;
        public static OptionItem MadmateRevengeCrewmate;
        public static OptionItem MadNekomataCanImp;
        public static OptionItem MadNekomataCanNeu;
        public static OptionItem MadNekomataCanMad;
        public static OptionItem MadNekomataCanCrew;
        public static OptionItem MadmateCanFixLightsOut;
        public static OptionItem MadmateCanFixComms;
        //public static OptionItem MadmateHasImpostorVision;
        public static OptionItem MadmateHasSun;
        public static OptionItem MadmateHasMoon;
        public static OptionItem MadmateCanSeeOtherVotes;
        public static OptionItem MadmateVentCooldown;
        public static OptionItem MadmateVentMaxTime;
        public static OptionItem MadmateCanMovedByVent;

        public static OptionItem KillFlashDuration;

        // HideAndSeek
        public static OptionItem AllowCloseDoors;
        public static OptionItem KillDelay;
        //public static OptionItem IgnoreCosmetics;
        public static OptionItem IgnoreVent;
        public static float HideAndSeekKillDelayTimer = 0f;
        //特殊モード
        public static OptionItem ONspecialMode;
        public static OptionItem InsiderMode;
        public static OptionItem Taskcheck;
        //public static OptionItem CommRepo;

        // タスク無効化
        public static OptionItem DisableTasks;
        public static OptionItem DisableSwipeCard;
        public static OptionItem DisableSubmitScan;
        public static OptionItem DisableUnlockSafe;
        public static OptionItem DisableUploadData;
        public static OptionItem DisableStartReactor;
        public static OptionItem DisableResetBreaker;

        //TaskBattle
        public static OptionItem TaskBattleSet;
        public static OptionItem TaskBattleCanVent;
        public static OptionItem TaskBattleVentCooldown;
        public static OptionItem TaskBattletaskc;
        public static OptionItem TaskBattletaska;
        public static OptionItem TaskBattletasko;
        public static OptionItem TaskBattleTeamMode;
        public static OptionItem TaskBattleTeamC;
        public static OptionItem TaskBattleTeamWinType;
        public static OptionItem TaskBattleTeamWinTaskc;

        //デバイスブロック
        public static OptionItem DisableDevices;
        public static OptionItem DisableSkeldDevices;
        public static OptionItem DisableSkeldAdmin;
        public static OptionItem DisableSkeldCamera;
        public static OptionItem DisableMiraHQDevices;
        public static OptionItem DisableMiraHQAdmin;
        public static OptionItem DisableMiraHQDoorLog;
        public static OptionItem DisablePolusDevices;
        public static OptionItem DisablePolusAdmin;
        public static OptionItem DisablePolusCamera;
        public static OptionItem DisablePolusVital;
        public static OptionItem DisableAirshipDevices;
        public static OptionItem DisableAirshipCockpitAdmin;
        public static OptionItem DisableAirshipRecordsAdmin;
        public static OptionItem DisableAirshipCamera;
        public static OptionItem DisableAirshipVital;
        public static OptionItem DisableFungleDevices;
        public static OptionItem DisableFungleVital;
        public static OptionItem DisableDevicesIgnoreConditions;
        public static OptionItem DisableDevicesIgnoreImpostors;
        public static OptionItem DisableDevicesIgnoreMadmates;
        public static OptionItem DisableDevicesIgnoreNeutrals;
        public static OptionItem DisableDevicesIgnoreCrewmates;
        public static OptionItem DisableDevicesIgnoreAfterAnyoneDied;

        // ランダムマップ
        public static OptionItem RandomMapsMode;
        public static OptionItem AddedTheSkeld;
        public static OptionItem AddedMiraHQ;
        public static OptionItem AddedPolus;
        public static OptionItem AddedTheAirShip;
        public static OptionItem AddedTheFungle;
        // public static OptionItem AddedDleks;

        // ランダムスポーン
        public static OptionItem EnableRandomSpawn;
        //Skeld
        public static OptionItem RandomSpawnSkeld;
        public static OptionItem RandomSpawnSkeldCafeteria;
        public static OptionItem RandomSpawnSkeldWeapons;
        public static OptionItem RandomSpawnSkeldLifeSupp;
        public static OptionItem RandomSpawnSkeldNav;
        public static OptionItem RandomSpawnSkeldShields;
        public static OptionItem RandomSpawnSkeldComms;
        public static OptionItem RandomSpawnSkeldStorage;
        public static OptionItem RandomSpawnSkeldAdmin;
        public static OptionItem RandomSpawnSkeldElectrical;
        public static OptionItem RandomSpawnSkeldLowerEngine;
        public static OptionItem RandomSpawnSkeldUpperEngine;
        public static OptionItem RandomSpawnSkeldSecurity;
        public static OptionItem RandomSpawnSkeldReactor;
        public static OptionItem RandomSpawnSkeldMedBay;
        //Mira
        public static OptionItem RandomSpawnMira;
        public static OptionItem RandomSpawnMiraCafeteria;
        public static OptionItem RandomSpawnMiraBalcony;
        public static OptionItem RandomSpawnMiraStorage;
        public static OptionItem RandomSpawnMiraJunction;
        public static OptionItem RandomSpawnMiraComms;
        public static OptionItem RandomSpawnMiraMedBay;
        public static OptionItem RandomSpawnMiraLockerRoom;
        public static OptionItem RandomSpawnMiraDecontamination;
        public static OptionItem RandomSpawnMiraLaboratory;
        public static OptionItem RandomSpawnMiraReactor;
        public static OptionItem RandomSpawnMiraLaunchpad;
        public static OptionItem RandomSpawnMiraAdmin;
        public static OptionItem RandomSpawnMiraOffice;
        public static OptionItem RandomSpawnMiraGreenhouse;
        //Polus
        public static OptionItem RandomSpawnPolus;
        public static OptionItem RandomSpawnPolusOfficeLeft;
        public static OptionItem RandomSpawnPolusOfficeRight;
        public static OptionItem RandomSpawnPolusAdmin;
        public static OptionItem RandomSpawnPolusComms;
        public static OptionItem RandomSpawnPolusWeapons;
        public static OptionItem RandomSpawnPolusBoilerRoom;
        public static OptionItem RandomSpawnPolusLifeSupp;
        public static OptionItem RandomSpawnPolusElectrical;
        public static OptionItem RandomSpawnPolusSecurity;
        public static OptionItem RandomSpawnPolusDropship;
        public static OptionItem RandomSpawnPolusStorage;
        public static OptionItem RandomSpawnPolusRocket;
        public static OptionItem RandomSpawnPolusLaboratory;
        public static OptionItem RandomSpawnPolusToilet;
        public static OptionItem RandomSpawnPolusSpecimens;
        //AIrShip
        public static OptionItem RandomSpawnAirship;
        public static OptionItem RandomSpawnAirshipBrig;
        public static OptionItem RandomSpawnAirshipEngine;
        public static OptionItem RandomSpawnAirshipKitchen;
        public static OptionItem RandomSpawnAirshipCargoBay;
        public static OptionItem RandomSpawnAirshipRecords;
        public static OptionItem RandomSpawnAirshipMainHall;
        public static OptionItem RandomSpawnAirshipNapRoom;
        public static OptionItem RandomSpawnAirshipMeetingRoom;
        public static OptionItem RandomSpawnAirshipGapRoom;
        public static OptionItem RandomSpawnAirshipVaultRoom;
        public static OptionItem RandomSpawnAirshipComms;
        public static OptionItem RandomSpawnAirshipCockpit;
        public static OptionItem RandomSpawnAirshipArmory;
        public static OptionItem RandomSpawnAirshipViewingDeck;
        public static OptionItem RandomSpawnAirshipSecurity;
        public static OptionItem RandomSpawnAirshipElectrical;
        public static OptionItem RandomSpawnAirshipMedical;
        public static OptionItem RandomSpawnAirshipToilet;
        public static OptionItem RandomSpawnAirshipShowers;
        //Fungle
        public static OptionItem RandomSpawnFungle;
        public static OptionItem RandomSpawnFungleKitchen;
        public static OptionItem RandomSpawnFungleBeach;
        public static OptionItem RandomSpawnFungleCafeteria;
        public static OptionItem RandomSpawnFungleRecRoom;
        public static OptionItem RandomSpawnFungleBonfire;
        public static OptionItem RandomSpawnFungleDropship;
        public static OptionItem RandomSpawnFungleStorage;
        public static OptionItem RandomSpawnFungleMeetingRoom;
        public static OptionItem RandomSpawnFungleSleepingQuarters;
        public static OptionItem RandomSpawnFungleLaboratory;
        public static OptionItem RandomSpawnFungleGreenhouse;
        public static OptionItem RandomSpawnFungleReactor;
        public static OptionItem RandomSpawnFungleJungleTop;
        public static OptionItem RandomSpawnFungleJungleBottom;
        public static OptionItem RandomSpawnFungleLookout;
        public static OptionItem RandomSpawnFungleMiningPit;
        public static OptionItem RandomSpawnFungleHighlands;
        public static OptionItem RandomSpawnFungleUpperEngine;
        public static OptionItem RandomSpawnFunglePrecipice;
        public static OptionItem RandomSpawnFungleComms;

        // CustomSpawn
        public static OptionItem CustomSpawn;
        public static OptionItem RandomSpawnCustom1;
        public static OptionItem RandomSpawnCustom2;
        public static OptionItem RandomSpawnCustom3;
        public static OptionItem RandomSpawnCustom4;
        public static OptionItem RandomSpawnCustom5;
        public static OptionItem RandomSpawnCustom6;
        public static OptionItem RandomSpawnCustom7;
        public static OptionItem RandomSpawnCustom8;

        // 投票モード
        public static OptionItem VoteMode;
        public static OptionItem WhenSkipVote;
        public static OptionItem WhenSkipVoteIgnoreFirstMeeting;
        public static OptionItem WhenSkipVoteIgnoreNoDeadBody;
        public static OptionItem WhenSkipVoteIgnoreEmergency;
        public static OptionItem WhenNonVote;
        public static OptionItem WhenTie;
        public static readonly string[] voteModes =
        {
            "Default", "Suicide", "SelfVote", "Skip"
        };
        public static readonly string[] tieModes =
        {
            "TieMode.Default", "TieMode.All", "TieMode.Random"
        };
        public static VoteMode GetWhenSkipVote() => (VoteMode)WhenSkipVote.GetValue();
        public static VoteMode GetWhenNonVote() => (VoteMode)WhenNonVote.GetValue();

        // ボタン回数
        public static OptionItem SyncButtonMode;
        public static OptionItem SyncedButtonCount;
        public static int UsedButtonCount = 0;

        // 全員生存時の会議時間
        public static OptionItem AllAliveMeeting;
        public static OptionItem AllAliveMeetingTime;

        // 追加の緊急ボタンクールダウン
        public static OptionItem AdditionalEmergencyCooldown;
        public static OptionItem AdditionalEmergencyCooldownThreshold;
        public static OptionItem AdditionalEmergencyCooldownTime;

        public static OptionItem ShowRoleAtFirstMeeting;

        //転落死
        public static OptionItem LadderDeath;
        public static OptionItem LadderDeathChance;

        // 通常モードでかくれんぼ
        public static bool IsStandardHAS => StandardHAS.GetBool() && CurrentGameMode == CustomGameMode.Standard;
        public static OptionItem StandardHAS;
        public static OptionItem StandardHASWaitingTime;

        // リアクターの時間制御
        public static OptionItem SabotageTimeControl;
        public static OptionItem PolusReactorTimeLimit;
        public static OptionItem AirshipReactorTimeLimit;
        public static OptionItem FungleReactorTimeLimit;
        public static OptionItem FungleMushroomMixupDuration;

        // サボタージュのクールダウン変更
        public static OptionItem ModifySabotageCooldown;
        public static OptionItem SabotageCooldown;

        // 停電の特殊設定
        public static OptionItem LightsOutSpecialSettings;
        public static OptionItem DisableAirshipViewingDeckLightsPanel;
        public static OptionItem DisableAirshipGapRoomLightsPanel;
        public static OptionItem DisableAirshipCargoLightsPanel;
        public static OptionItem BlockDisturbancesToSwitches;

        // マップ改造
        public static OptionItem Sabotage;
        public static OptionItem MapModification;
        public static OptionItem AirShipVariableElectrical;
        public static OptionItem DisableAirshipMovingPlatform;
        public static OptionItem CuseVent;
        public static OptionItem CuseVentCount;
        public static OptionItem ResetDoorsEveryTurns;
        public static OptionItem DoorsResetMode;
        public static OptionItem DisableFungleSporeTrigger;
        // その他
        public static OptionItem FixFirstKillCooldown;
        public static OptionItem FixZeroKillCooldown;
        public static OptionItem CanseeVoteresult;
        public static OptionItem VRcanseemitidure;
        public static OptionItem DisableTaskWin;
        public static OptionItem GhostCanSeeOtherRoles;
        public static OptionItem GhostCanSeeOtherTasks;
        public static OptionItem GhostCanSeeOtherVotes;
        public static OptionItem GhostCanSeeDeathReason;
        public static OptionItem GhostIgnoreTasks;
        public static OptionItem GhostIgnoreAllTasks;
        public static OptionItem GhostCanSeeNumberOfButtonsOnOthers;
        public static OptionItem CommsCamouflage;
        public static OptionItem RoleImpostor;
        public static OptionItem ALoversRole;
        public static OptionItem BLoversRole;
        public static OptionItem CLoversRole;
        public static OptionItem DLoversRole;
        public static OptionItem ELoversRole;
        public static OptionItem FLoversRole;
        public static OptionItem GLoversRole;

        // プリセット対象外
        public static OptionItem NoGameEnd;
        public static OptionItem AutoDisplayLastResult;
        public static OptionItem AutoDisplayKillLog;
        public static OptionItem SuffixMode;
        public static OptionItem HideGameSettings;
        public static OptionItem HideSettingsDuringGame;
        public static OptionItem ColorNameMode;
        public static OptionItem ChangeNameToRoleInfo;
        public static OptionItem RoleAssigningAlgorithm;
        public static OptionItem sotodererukomando;

        public static OptionItem ApplyDenyNameList;
        public static OptionItem KickPlayerFriendCodeNotExist;
        public static OptionItem KickModClient;
        public static OptionItem ApplyBanList;

        public static readonly string[] suffixModes =
        {
            "SuffixMode.None",
            "SuffixMode.Version",
            "SuffixMode.Streaming",
            "SuffixMode.Recording",
            "SuffixMode.RoomHost",
            "SuffixMode.OriginalName",
            "SuffixMode.Timer"
        };
        public static readonly string[] RoleAssigningAlgorithms =
        {
            "RoleAssigningAlgorithm.Default",
            "RoleAssigningAlgorithm.NetRandom",
            "RoleAssigningAlgorithm.HashRandom",
            "RoleAssigningAlgorithm.Xorshift",
            "RoleAssigningAlgorithm.MersenneTwister",
        };
        public static SuffixModes GetSuffixMode()
        {
            return (SuffixModes)SuffixMode.GetValue();
        }

        public static int SnitchExposeTaskLeft = 1;

        public static bool IsLoaded = false;
        public static int GetRoleCount(CustomRoles role)
        {
            return GetRoleChance(role) == 0 ? 0 : CustomRoleCounts.TryGetValue(role, out var option) ? option.GetInt() : 0;
        }

        public static int GetRoleChance(CustomRoles role)
        {
            return CustomRoleSpawnChances.TryGetValue(role, out var option) ? option.GetInt() : 0;
        }
        public static void Load()
        {
            if (IsLoaded) return;
            OptionSaver.Initialize();
            // プリセット
            _ = PresetOptionItem.Create(0, TabGroup.MainSettings)
                .SetColor(new Color32(204, 204, 0, 255))
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);

            // ゲームモード
            GameMode = StringOptionItem.Create(1, "GameMode", gameModes, 0, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);

            #region 役職・詳細設定
            CustomRoleCounts = new();
            CustomRoleSpawnChances = new();

            var sortedRoleInfo = CustomRoleManager.AllRolesInfo.Values.OrderBy(role => role.ConfigId);
            // GM
            EnableGM = BooleanOptionItem.Create(100, "GM", false, TabGroup.MainSettings, false)
                .SetColor(Utils.GetRoleColor(CustomRoles.GM))
                .SetHeader(true);

            RoleAssignManager.SetupOptionItem();

            //タスクバトル
            TaskBattleSet = BooleanOptionItem.Create(100317, "TaskBattleSet", false, TabGroup.MainSettings, false).SetGameMode(CustomGameMode.TaskBattle)
                .SetHeader(true)
                .SetColorcode("#87crfa");
            TaskBattleCanVent = BooleanOptionItem.Create(100307, "TaskBattleCanVent", false, TabGroup.MainSettings, false).SetParent(TaskBattleSet)
                .SetGameMode(CustomGameMode.TaskBattle);
            TaskBattleVentCooldown = FloatOptionItem.Create(100308, "TaskBattleVentCooldown", new(0f, 99f, 1f), 5f, TabGroup.MainSettings, false).SetParent(TaskBattleCanVent)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.TaskBattle);
            TaskBattletaska = BooleanOptionItem.Create(100309, "TaskBattleTaska", false, TabGroup.MainSettings, false).SetGameMode(CustomGameMode.TaskBattle).SetParent(TaskBattleSet);
            TaskBattletaskc = BooleanOptionItem.Create(100311, "TaskBattleTaskc", false, TabGroup.MainSettings, false).SetGameMode(CustomGameMode.TaskBattle).SetParent(TaskBattleSet);
            TaskBattletasko = BooleanOptionItem.Create(100312, "TaskBattleTasko", false, TabGroup.MainSettings, false).SetGameMode(CustomGameMode.TaskBattle).SetParent(TaskBattleSet);
            TaskBattleTeamMode = BooleanOptionItem.Create(100313, "TaskBattleTeamMode", false, TabGroup.MainSettings, false).SetParent(TaskBattleSet)
                .SetGameMode(CustomGameMode.TaskBattle);
            TaskBattleTeamC = FloatOptionItem.Create(100314, "TaskBattleTeamC", new(1f, 15f, 1f), 2f, TabGroup.MainSettings, false).SetParent(TaskBattleTeamMode)
                .SetGameMode(CustomGameMode.TaskBattle);
            TaskBattleTeamWinType = BooleanOptionItem.Create(100315, "TaskBattleTeamWT", false, TabGroup.MainSettings, false).SetParent(TaskBattleTeamMode)
                .SetGameMode(CustomGameMode.TaskBattle);
            TaskBattleTeamWinTaskc = FloatOptionItem.Create(100316, "TaskBattleTeamWinTaskc", new(1f, 999f, 1f), 20f, TabGroup.MainSettings, false).SetParent(TaskBattleTeamWinType)
                .SetGameMode(CustomGameMode.TaskBattle);

            //特殊モード
            ONspecialMode = BooleanOptionItem.Create(200000, "ONspecialMode", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#00c1ff");
            InsiderMode = BooleanOptionItem.Create(200001, "InsiderMode", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
                .SetGameMode(CustomGameMode.Standard);
            Taskcheck = BooleanOptionItem.Create(200002, "Taskcheck", false, TabGroup.MainSettings, false).SetParent(InsiderMode);
            ColorNameMode = BooleanOptionItem.Create(200003, "ColorNameMode", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
                .SetGameMode(CustomGameMode.All);
            StandardHAS = BooleanOptionItem.Create(200004, "StandardHAS", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
                .SetGameMode(CustomGameMode.Standard);
            StandardHASWaitingTime = FloatOptionItem.Create(200005, "StandardHASWaitingTime", new(0f, 180f, 2.5f), 10f, TabGroup.MainSettings, false).SetParent(StandardHAS)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            RoleImpostor = BooleanOptionItem.Create(200006, "VRoleImpostor", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
                .SetGameMode(CustomGameMode.Standard);
            // Impostor
            sortedRoleInfo.Where(role => role.CustomRoleType == CustomRoleTypes.Impostor).Do(info =>
            {
                SetupRoleOptions(info);
                info.OptionCreator?.Invoke();
            });

            DefaultShapeshiftCooldown = FloatOptionItem.Create(5011, "DefaultShapeshiftCooldown", new(1f, 999f, 1f), 15f, TabGroup.ImpostorRoles, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds);

            // Madmate, Crewmate, Neutral
            sortedRoleInfo.Where(role => role.CustomRoleType != CustomRoleTypes.Impostor).Do(info =>
            {
                SetupRoleOptions(info);
                info.OptionCreator?.Invoke();

            });
            // Madmate Common Options
            CanMakeMadmateCount = IntegerOptionItem.Create(101012, "CanMakeMadmateCount", new(0, 15, 1), 0, TabGroup.MadmateRoles, false)
                .SetValueFormat(OptionFormat.Players)
                .SetHeader(true)
                .SetColor(Palette.ImpostorRed);
            MadMateOption = BooleanOptionItem.Create(1010013, "MadmateOption", false, TabGroup.MadmateRoles, false)
                .SetHeader(true)
                .SetColorcode("#ffa3a3");
            MadmateCanFixLightsOut = BooleanOptionItem.Create(101014, "MadmateCanFixLightsOut", false, TabGroup.MadmateRoles, false).SetColorcode("#ffcc66").SetParent(MadMateOption);
            MadmateCanFixComms = BooleanOptionItem.Create(101015, "MadmateCanFixComms", false, TabGroup.MadmateRoles, false).SetColorcode("#999999").SetParent(MadMateOption);
            //MadmateHasImpostorVision = BooleanOptionItem.Create(101004, "MadmateHasImpostorVision", false, TabGroup.MadmateRoles, false).SetParent(MadMateOption);
            MadmateHasSun = BooleanOptionItem.Create(101004, "MadmateHasSun", false, TabGroup.MadmateRoles, false).SetColorcode("#ec6800").SetParent(MadMateOption);
            MadmateHasMoon = BooleanOptionItem.Create(1010016, "MadmateHasMoon", false, TabGroup.MadmateRoles, false).SetColorcode("#ffff33").SetParent(MadMateOption);

            MadmateCanSeeKillFlash = BooleanOptionItem.Create(101005, "MadmateCanSeeKillFlash", false, TabGroup.MadmateRoles, false).SetColorcode("#61b26c").SetParent(MadMateOption);
            MadmateCanSeeOtherVotes = BooleanOptionItem.Create(101006, "MadmateCanSeeOtherVotes", false, TabGroup.MadmateRoles, false).SetColorcode("#800080").SetParent(MadMateOption);
            MadmateCanSeeDeathReason = BooleanOptionItem.Create(101007, "MadmateCanSeeDeathReason", false, TabGroup.MadmateRoles, false).SetColorcode("#80ffdd").SetParent(MadMateOption);
            MadmateRevengeCrewmate = BooleanOptionItem.Create(101008, "MadmateExileCrewmate", false, TabGroup.MadmateRoles, false).SetColorcode("#00fa9a").SetParent(MadMateOption);
            MadNekomataCanImp = BooleanOptionItem.Create(101017, "NekoKabochaImpostorsGetRevenged", false, TabGroup.MadmateRoles, false).SetParent(MadmateRevengeCrewmate).SetColorcode("#ff1919");
            MadNekomataCanCrew = BooleanOptionItem.Create(101009, "NekomataCanCrew", true, TabGroup.MadmateRoles, false).SetParent(MadmateRevengeCrewmate).SetColorcode("#8cffff");
            MadNekomataCanMad = BooleanOptionItem.Create(1010018, "NekoKabochaMadmatesGetRevenged", true, TabGroup.MadmateRoles, false).SetParent(MadmateRevengeCrewmate).SetColorcode("#ff1919");
            MadNekomataCanNeu = BooleanOptionItem.Create(101010, "NekomataCanNeu", true, TabGroup.MadmateRoles, false).SetParent(MadmateRevengeCrewmate).SetColorcode("#808080");

            MadmateVentCooldown = FloatOptionItem.Create(101011, "MadmateVentCooldown", new(0f, 180f, 5f), 0f, TabGroup.MadmateRoles, false).SetColorcode("#8cffff").SetParent(MadMateOption)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds);
            MadmateVentMaxTime = FloatOptionItem.Create(101018, "MadmateVentMaxTime", new(0f, 180f, 5f), 0f, TabGroup.MadmateRoles, false).SetColorcode("#8cffff").SetParent(MadMateOption)
                .SetValueFormat(OptionFormat.Seconds);
            MadmateCanMovedByVent = BooleanOptionItem.Create(101013, "MadmateCanMovedByVent", true, TabGroup.MadmateRoles, false).SetColorcode("#8cffff").SetParent(MadMateOption);

            // Add-Ons
            SetupRoleOptions(50300, TabGroup.Addons, CustomRoles.ALovers, assignCountRule: new(2, 2, 2), fromtext: "<color=#ffffff>From:<color=#ff6be4>Love Couple Mod</color></size>");
            ALoversRole = BooleanOptionItem.Create(73010, "LoversRole", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.ALovers]);
            SetupRoleOptions(50310, TabGroup.Addons, CustomRoles.BLovers, assignCountRule: new(2, 2, 2));
            BLoversRole = BooleanOptionItem.Create(73020, "LoversRole", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.BLovers]);
            SetupRoleOptions(50320, TabGroup.Addons, CustomRoles.CLovers, assignCountRule: new(2, 2, 2));
            CLoversRole = BooleanOptionItem.Create(73030, "LoversRole", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.CLovers]);
            SetupRoleOptions(50330, TabGroup.Addons, CustomRoles.DLovers, assignCountRule: new(2, 2, 2));
            DLoversRole = BooleanOptionItem.Create(73040, "LoversRole", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.DLovers]);
            SetupRoleOptions(50340, TabGroup.Addons, CustomRoles.ELovers, assignCountRule: new(2, 2, 2));
            ELoversRole = BooleanOptionItem.Create(73050, "LoversRole", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.ELovers]);
            SetupRoleOptions(50350, TabGroup.Addons, CustomRoles.FLovers, assignCountRule: new(2, 2, 2));
            FLoversRole = BooleanOptionItem.Create(73060, "LoversRole", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.FLovers]);
            SetupRoleOptions(50360, TabGroup.Addons, CustomRoles.GLovers, assignCountRule: new(2, 2, 2));
            GLoversRole = BooleanOptionItem.Create(73070, "LoversRole", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.GLovers]);
            //特定の陣営
            LastImpostor.SetupCustomOption();
            LastNeutral.SetupCustomOption();
            Workhorse.SetupCustomOption();

            //バフ(ゲッサー→特定陣営→会議効果→タスクターン)
            Guesser.SetupCustomOption();
            Serial.SetupCustomOption();
            Connecting.SetupCustomOption();
            Watcher.SetupCustomOption();
            AdditionalVoter.SetupCustomOption();
            Nurse.SetupCustomOption();
            Bakeneko.SetupCustomOption();
            Speeding.SetupCustomOption();
            Director.SetupCustomOption();
            Psychic.SetupCustomOption();
            Opener.SetupCustomOption();
            Sun.SetupCustomOption();
            Moon.SetupCustomOption();
            //デバフ達
            Notvoter.SetupCustomOption();
            Elector.SetupCustomOption();
            NotConvener.SetupCustomOption();
            Transparent.SetupCustomOption();
            Water.SetupCustomOption();
            LowBattery.SetupCustomOption();
            Slacker.SetupCustomOption();
            #endregion

            KillFlashDuration = FloatOptionItem.Create(90000, "KillFlashDuration", new(0.1f, 0.45f, 0.05f), 0.3f, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // HideAndSeek
            SetupRoleOptions(100000, TabGroup.MainSettings, CustomRoles.HASFox, customGameMode: CustomGameMode.HideAndSeek);
            SetupRoleOptions(100100, TabGroup.MainSettings, CustomRoles.HASTroll, customGameMode: CustomGameMode.HideAndSeek);
            AllowCloseDoors = BooleanOptionItem.Create(101000, "AllowCloseDoors", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.HideAndSeek);
            KillDelay = FloatOptionItem.Create(101001, "HideAndSeekWaitingTime", new(0f, 180f, 5f), 10f, TabGroup.MainSettings, false)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.HideAndSeek);
            //IgnoreCosmetics = CustomOption.Create(101002, Color.white, "IgnoreCosmetics", false)
            //    .SetGameMode(CustomGameMode.HideAndSeek);
            IgnoreVent = BooleanOptionItem.Create(101003, "IgnoreVent", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.HideAndSeek);

            // マップ改造
            MapModification = BooleanOptionItem.Create(102000, "MapModification", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ccff66");
            AirShipVariableElectrical = BooleanOptionItem.Create(101600, "AirShipVariableElectrical", false, TabGroup.MainSettings, false).SetParent(MapModification);
            DisableAirshipMovingPlatform = BooleanOptionItem.Create(101700, "DisableAirshipMovingPlatform", false, TabGroup.MainSettings, false).SetParent(MapModification);
            CuseVent = BooleanOptionItem.Create(101701, "Can'tUseVent", false, TabGroup.MainSettings, false).SetParent(MapModification);
            CuseVentCount = FloatOptionItem.Create(101702, "CuseVentCount", new(1f, 15f, 1f), 5f, TabGroup.MainSettings, false).SetValueFormat(OptionFormat.Players).SetParent(CuseVent);
            ResetDoorsEveryTurns = BooleanOptionItem.Create(101800, "ResetDoorsEveryTurns", false, TabGroup.MainSettings, false).SetParent(MapModification);
            DoorsResetMode = StringOptionItem.Create(101810, "DoorsResetMode", EnumHelper.GetAllNames<DoorsReset.ResetMode>(), 0, TabGroup.MainSettings, false).SetParent(ResetDoorsEveryTurns);
            DisableFungleSporeTrigger = BooleanOptionItem.Create(101900, "DisableFungleSporeTrigger", false, TabGroup.MainSettings, false).SetParent(MapModification);
            DisableDevices = BooleanOptionItem.Create(101200, "DisableDevices", false, TabGroup.MainSettings, false).SetParent(MapModification)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableSkeldDevices = BooleanOptionItem.Create(101210, "DisableSkeldDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldAdmin = BooleanOptionItem.Create(101211, "DisableSkeldAdmin", false, TabGroup.MainSettings, false).SetParent(DisableSkeldDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableSkeldCamera = BooleanOptionItem.Create(101212, "DisableSkeldCamera", false, TabGroup.MainSettings, false).SetParent(DisableSkeldDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#cccccc");
            DisableMiraHQDevices = BooleanOptionItem.Create(101220, "DisableMiraHQDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableMiraHQAdmin = BooleanOptionItem.Create(101221, "DisableMiraHQAdmin", false, TabGroup.MainSettings, false).SetParent(DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableMiraHQDoorLog = BooleanOptionItem.Create(101222, "DisableMiraHQDoorLog", false, TabGroup.MainSettings, false).SetParent(DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#cccccc");
            DisablePolusDevices = BooleanOptionItem.Create(101230, "DisablePolusDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusAdmin = BooleanOptionItem.Create(101231, "DisablePolusAdmin", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisablePolusCamera = BooleanOptionItem.Create(101232, "DisablePolusCamera", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#cccccc");
            DisablePolusVital = BooleanOptionItem.Create(101233, "DisablePolusVital", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#33ccff");
            DisableAirshipDevices = BooleanOptionItem.Create(101240, "DisableAirshipDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCockpitAdmin = BooleanOptionItem.Create(101241, "DisableAirshipCockpitAdmin", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableAirshipRecordsAdmin = BooleanOptionItem.Create(101242, "DisableAirshipRecordsAdmin", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableAirshipCamera = BooleanOptionItem.Create(101243, "DisableAirshipCamera", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#cccccc");
            DisableAirshipVital = BooleanOptionItem.Create(101244, "DisableAirshipVital", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#33ccff");
            DisableFungleDevices = BooleanOptionItem.Create(101250, "DisableFungleDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableFungleVital = BooleanOptionItem.Create(101251, "DisableFungleVital", false, TabGroup.MainSettings, false).SetParent(DisableFungleDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#33ccff");

            DisableDevicesIgnoreConditions = BooleanOptionItem.Create(101290, "IgnoreConditions", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreImpostors = BooleanOptionItem.Create(101291, "IgnoreImpostors", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#ff1919");
            DisableDevicesIgnoreMadmates = BooleanOptionItem.Create(101292, "IgnoreMadmates", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#ff1919");
            DisableDevicesIgnoreNeutrals = BooleanOptionItem.Create(101293, "IgnoreNeutrals", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#808080");
            DisableDevicesIgnoreCrewmates = BooleanOptionItem.Create(101294, "IgnoreCrewmates", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#8cffff");
            DisableDevicesIgnoreAfterAnyoneDied = BooleanOptionItem.Create(101295, "IgnoreAfterAnyoneDied", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#666699");
            // タスク無効化
            DisableTasks = BooleanOptionItem.Create(100300, "DisableTasks", false, TabGroup.MainSettings, false).SetParent(MapModification)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#666666");
            DisableSwipeCard = BooleanOptionItem.Create(100301, "DisableSwipeCardTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableSubmitScan = BooleanOptionItem.Create(100302, "DisableSubmitScanTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUnlockSafe = BooleanOptionItem.Create(100303, "DisableUnlockSafeTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUploadData = BooleanOptionItem.Create(100304, "DisableUploadDataTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableStartReactor = BooleanOptionItem.Create(100305, "DisableStartReactorTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableResetBreaker = BooleanOptionItem.Create(100306, "DisableResetBreakerTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);

            //サボ
            Sabotage = BooleanOptionItem.Create(100805, "Sabotage", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#990000")
                .SetGameMode(CustomGameMode.Standard);
            // リアクターの時間制御
            SabotageTimeControl = BooleanOptionItem.Create(100800, "SabotageTimeControl", false, TabGroup.MainSettings, false).SetParent(Sabotage)
                .SetGameMode(CustomGameMode.Standard);
            PolusReactorTimeLimit = FloatOptionItem.Create(100801, "PolusReactorTimeLimit", new(1f, 60f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            AirshipReactorTimeLimit = FloatOptionItem.Create(100802, "AirshipReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            FungleReactorTimeLimit = FloatOptionItem.Create(100803, "FungleReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            FungleMushroomMixupDuration = FloatOptionItem.Create(100804, "FungleMushroomMixupDuration", new(1f, 20f, 1f), 10f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            // サボタージュのクールダウン変更
            ModifySabotageCooldown = BooleanOptionItem.Create(100810, "ModifySabotageCooldown", false, TabGroup.MainSettings, false).SetParent(Sabotage)
                .SetGameMode(CustomGameMode.Standard);
            SabotageCooldown = FloatOptionItem.Create(100811, "SabotageCooldown", new(1f, 60f, 1f), 30f, TabGroup.MainSettings, false).SetParent(ModifySabotageCooldown)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            CommsCamouflage = BooleanOptionItem.Create(100812, "CommsCamouflage", false, TabGroup.MainSettings, false).SetParent(Sabotage)
                .SetGameMode(CustomGameMode.Standard);
            //CommRepo = BooleanOptionItem.Create(227, "CommRepo", false, TabGroup.MainSettings, false).SetParent(Sabotage)
            //    .SetGameMode(CustomGameMode.Standard);

            // 停電の特殊設定
            LightsOutSpecialSettings = BooleanOptionItem.Create(101500, "LightsOutSpecialSettings", false, TabGroup.MainSettings, false).SetParent(Sabotage)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipViewingDeckLightsPanel = BooleanOptionItem.Create(101511, "DisableAirshipViewingDeckLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipGapRoomLightsPanel = BooleanOptionItem.Create(101512, "DisableAirshipGapRoomLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCargoLightsPanel = BooleanOptionItem.Create(101513, "DisableAirshipCargoLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            BlockDisturbancesToSwitches = BooleanOptionItem.Create(101514, "BlockDisturbancesToSwitches", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);

            // ランダムマップ
            RandomMapsMode = BooleanOptionItem.Create(100400, "RandomMapsMode", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ffcc66")
                .SetGameMode(CustomGameMode.All);
            AddedTheSkeld = BooleanOptionItem.Create(100401, "AddedTheSkeld", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#888888");
            AddedMiraHQ = BooleanOptionItem.Create(100402, "AddedMIRAHQ", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#ff6633");
            AddedPolus = BooleanOptionItem.Create(100403, "AddedPolus", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#660066");
            AddedTheAirShip = BooleanOptionItem.Create(100404, "AddedTheAirShip", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#ff3300");
            AddedTheFungle = BooleanOptionItem.Create(100406, "AddedTheFungle", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#ff9900");
            // MapDleks = CustomOption.Create(100405, Color.white, "AddedDleks", false, RandomMapMode)
            //     .SetGameMode(CustomGameMode.All);

            // ランダムスポーン
            EnableRandomSpawn = BooleanOptionItem.Create(101300, "RandomSpawn", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ff99cc")
                .SetGameMode(CustomGameMode.All);
            RandomSpawn.SetupCustomOption();

            // 投票モード
            VoteMode = BooleanOptionItem.Create(100500, "VoteMode", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#33ff99")
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVote = StringOptionItem.Create(100510, "WhenSkipVote", voteModes[0..3], 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreFirstMeeting = BooleanOptionItem.Create(100511, "WhenSkipVoteIgnoreFirstMeeting", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreNoDeadBody = BooleanOptionItem.Create(100512, "WhenSkipVoteIgnoreNoDeadBody", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreEmergency = BooleanOptionItem.Create(100513, "WhenSkipVoteIgnoreEmergency", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenNonVote = StringOptionItem.Create(100520, "WhenNonVote", voteModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenTie = StringOptionItem.Create(100530, "WhenTie", tieModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            ShowRoleAtFirstMeeting = BooleanOptionItem.Create(100540, "ShowRoleAtFirstMeeting", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#33ff99");
            SyncButtonMode = BooleanOptionItem.Create(100550, "SyncButtonMode", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColorcode("#33ff99");
            SyncedButtonCount = IntegerOptionItem.Create(100560, "SyncedButtonCount", new(0, 100, 1), 10, TabGroup.MainSettings, false).SetParent(SyncButtonMode)
                .SetValueFormat(OptionFormat.Times)
                .SetGameMode(CustomGameMode.Standard);
            // 全員生存時の会議時間
            AllAliveMeeting = BooleanOptionItem.Create(100900, "AllAliveMeeting", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#33ff99");
            AllAliveMeetingTime = FloatOptionItem.Create(100901, "AllAliveMeetingTime", new(1f, 300f, 1f), 10f, TabGroup.MainSettings, false).SetParent(AllAliveMeeting)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // 生存人数ごとの緊急会議
            AdditionalEmergencyCooldown = BooleanOptionItem.Create(101400, "AdditionalEmergencyCooldown", false, TabGroup.MainSettings, false).SetColorcode("#33ff99");
            AdditionalEmergencyCooldownThreshold = IntegerOptionItem.Create(101401, "AdditionalEmergencyCooldownThreshold", new(1, 15, 1), 1, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Players)
                .SetGameMode(CustomGameMode.Standard);
            AdditionalEmergencyCooldownTime = FloatOptionItem.Create(101402, "AdditionalEmergencyCooldownTime", new(1f, 60f, 1f), 1f, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // 転落死
            LadderDeath = BooleanOptionItem.Create(101100, "LadderDeath", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ffcc00");
            LadderDeathChance = StringOptionItem.Create(101110, "LadderDeathChance", rates[1..], 0, TabGroup.MainSettings, false).SetParent(LadderDeath);

            //幽霊設定
            GhostCanSeeOtherRoles = BooleanOptionItem.Create(901_001, "GhostCanSeeOtherRoles", true, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#666699")
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherTasks = BooleanOptionItem.Create(901_002, "GhostCanSeeOtherTasks", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#666699");
            GhostCanSeeOtherVotes = BooleanOptionItem.Create(901_003, "GhostCanSeeOtherVotes", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#666699");
            GhostCanSeeDeathReason = BooleanOptionItem.Create(901_004, "GhostCanSeeDeathReason", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#666699");
            GhostIgnoreAllTasks = BooleanOptionItem.Create(901_005, "GhostIgnoreAllTasks", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#666699");
            GhostCanSeeNumberOfButtonsOnOthers = BooleanOptionItem.Create(901_006, "GhostCanSeeNumberOfButtonsOnOthers", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#666699");
            GhostIgnoreTasks = BooleanOptionItem.Create(901_000, "GhostIgnoreTasks", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#666699");

            // その他
            FixFirstKillCooldown = BooleanOptionItem.Create(900_000, "FixFirstKillCooldown", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#cc3366");
            FixZeroKillCooldown = BooleanOptionItem.Create(900_001, "FixZeroKillCooldown", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#cc3366");
            CanseeVoteresult = BooleanOptionItem.Create(900_002, "CanseeVoteresult", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#cc3366");
            VRcanseemitidure = BooleanOptionItem.Create(900_003, "CanseeMeetingAfterMitidure", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetParent(CanseeVoteresult);
            DisableTaskWin = BooleanOptionItem.Create(905_000, "DisableTaskWin", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ccff00")
                .SetGameMode(CustomGameMode.All);
            NoGameEnd = BooleanOptionItem.Create(905_001, "NoGameEnd", false, TabGroup.MainSettings, false)
                .SetColorcode("#ff1919")
                .SetGameMode(CustomGameMode.All);
            // プリセット対象外
            AutoDisplayLastResult = BooleanOptionItem.Create(1_000_000, "AutoDisplayLastResult", true, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#66ffff")
                .SetGameMode(CustomGameMode.All);
            AutoDisplayKillLog = BooleanOptionItem.Create(1_000_006, "AutoDisplayKillLog", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#66ffff");
            HideGameSettings = BooleanOptionItem.Create(1_000_002, "HideGameSettings", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff");
            HideSettingsDuringGame = BooleanOptionItem.Create(1_000_003, "HideGameSettingsDuringGame", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff"); ;
            SuffixMode = StringOptionItem.Create(1_000_001, "SuffixMode", suffixModes, 0, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff");
            ChangeNameToRoleInfo = BooleanOptionItem.Create(1_000_004, "ChangeNameToRoleInfo", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff");
            RoleAssigningAlgorithm = StringOptionItem.Create(1_000_005, "RoleAssigningAlgorithm", RoleAssigningAlgorithms, 0, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff")
                .RegisterUpdateValueEvent(
                    (object obj, OptionItem.UpdateValueEventArgs args) => IRandom.SetInstanceById(args.CurrentValue)
                );
            sotodererukomando = BooleanOptionItem.Create(1_000_007, "sotodererukomando", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff");

            ApplyDenyNameList = BooleanOptionItem.Create(1_000_100, "ApplyDenyNameList", true, TabGroup.MainSettings, true)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            KickPlayerFriendCodeNotExist = BooleanOptionItem.Create(1_000_101, "KickPlayerFriendCodeNotExist", false, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All);
            KickModClient = BooleanOptionItem.Create(1_000_102, "KickModClient", false, TabGroup.MainSettings, true)
            .SetGameMode(CustomGameMode.All);
            ApplyBanList = BooleanOptionItem.Create(1_000_110, "ApplyBanList", true, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All);

            DebugModeManager.SetupCustomOption();

            OptionSaver.Load();

            Combinations = null; //使わないから消す

            IsLoaded = true;
        }
        private static List<CombinationRoles> Combinations = new();
        public static void SetupRoleOptions(SimpleRoleInfo info) => SetupRoleOptions(info.ConfigId, info.Tab, info.RoleName, info.AssignInfo.AssignCountRule, fromtext: Utils.GetFrom(info), combination: info.Combination);
        public static void SetupRoleOptions(int id, TabGroup tab, CustomRoles role, IntegerValueRule assignCountRule = null, CustomGameMode customGameMode = CustomGameMode.Standard, string fromtext = "", CombinationRoles combination = CombinationRoles.None)
        {
            if (role.IsVanilla() || (combination != CombinationRoles.None && Combinations.Contains(combination))) return;
            assignCountRule ??= new(1, 15, 1);
            var from = "<line-height=25%><size=25%>\n</size><size=50%><pos=60%></color> " + fromtext + "</size>";

            var spawnOption = IntegerOptionItem.Create(id, combination == CombinationRoles.None ? role.ToString() : combination.ToString(), new(0, 100, 10), 0, tab, false, from)
                .SetColor(Utils.GetRoleColor(role))
                .SetValueFormat(OptionFormat.Percent)
                .SetHeader(true)
                .SetGameMode(customGameMode) as IntegerOptionItem;
            var hidevalue = role is CustomRoles.Jackal or CustomRoles.JackalMafia or CustomRoles.Madonna or CustomRoles.CountKiller or CustomRoles.Egoist or CustomRoles.GrimReaper or CustomRoles.Remotekiller or CustomRoles.LastImpostor or
                CustomRoles.LastNeutral or CustomRoles.ALovers or CustomRoles.BLovers or CustomRoles.CLovers or CustomRoles.DLovers or CustomRoles.ELovers or CustomRoles.FLovers or CustomRoles.GLovers;

            var countOption = IntegerOptionItem.Create(id + 1, "Maximum", assignCountRule, assignCountRule.Step, tab, false, HideValue: hidevalue)
                .SetParent(spawnOption)
                .SetValueFormat(OptionFormat.Players)
                .SetGameMode(customGameMode)
                .SetHidden(hidevalue);

            if (combination != CombinationRoles.None) Combinations.Add(combination);
            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        public class OverrideTasksData
        {
            public static Dictionary<CustomRoles, OverrideTasksData> AllData = new();
            public static Dictionary<CustomRoles, CustomRoles> chRoles = new(); //1人までしか対応していない、
            public CustomRoles Role { get; private set; }
            public int IdStart { get; private set; }
            public OptionItem doOverride;
            public OptionItem assignCommonTasks;
            public OptionItem numLongTasks;
            public OptionItem numShortTasks;

            /// <summary>
            /// タスクの上書き設定。
            /// </summary>
            /// <param name="idStart">ID(+3まで使用するので注意)</param>
            /// <param name="tab">タブ</param>
            /// <param name="role">設定に出すロール</param>
            /// <param name="chrole">設定名(ユニット用)</param>
            public OverrideTasksData(int idStart, TabGroup tab, CustomRoles role, CustomRoles chrole = CustomRoles.NotAssigned)
            {
                this.IdStart = idStart;
                this.Role = role;
                Dictionary<string, string> replacementDic = new() { { "%role%", Utils.GetRoleName(chrole == CustomRoles.NotAssigned ? role : chrole) } };
                doOverride = BooleanOptionItem.Create(idStart++, "doOverride", false, tab, false).SetParent(CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.None);
                doOverride.ReplacementDictionary = replacementDic;
                assignCommonTasks = BooleanOptionItem.Create(idStart++, "assignCommonTasks", true, tab, false).SetParent(doOverride)
                    .SetValueFormat(OptionFormat.None);
                assignCommonTasks.ReplacementDictionary = replacementDic;
                numLongTasks = IntegerOptionItem.Create(idStart++, "roleLongTasksNum", new(0, 99, 1), 3, tab, false).SetParent(doOverride)
                    .SetValueFormat(OptionFormat.Pieces);
                numLongTasks.ReplacementDictionary = replacementDic;
                numShortTasks = IntegerOptionItem.Create(idStart++, "roleShortTasksNum", new(0, 99, 1), 3, tab, false).SetParent(doOverride)
                    .SetValueFormat(OptionFormat.Pieces);
                numShortTasks.ReplacementDictionary = replacementDic;

                role = chrole == CustomRoles.NotAssigned ? role : chrole;

                if (!AllData.ContainsKey(role)) AllData.Add(role, this);
                else Logger.Warn("重複したCustomRolesを対象とするOverrideTasksDataが作成されました", "OverrideTasksData");
            }
            public static OverrideTasksData Create(int idStart, TabGroup tab, CustomRoles role)
            {
                return new OverrideTasksData(idStart, tab, role);
            }
            public static OverrideTasksData Create(SimpleRoleInfo roleInfo, int idOffset, CustomRoles rolename = CustomRoles.NotAssigned)
            {
                return new OverrideTasksData(roleInfo.ConfigId + idOffset, roleInfo.Tab, roleInfo.RoleName, rolename);
            }
        }
    }
}
