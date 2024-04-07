using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

using TownOfHost.Attributes;
using TownOfHost.Roles.Core;
using TownOfHost.Modules;

[assembly: AssemblyFileVersionAttribute(TownOfHost.Main.PluginVersion)]
[assembly: AssemblyInformationalVersionAttribute(TownOfHost.Main.PluginVersion)]
namespace TownOfHost
{
    [BepInPlugin(PluginGuid, "Town Of Host-K", PluginVersion)]
    [BepInIncompatibility("jp.ykundesu.supernewroles")]
    [BepInProcess("Among Us.exe")]
    public class Main : BasePlugin
    {
        // == プログラム設定 / Program Config ==
        // modの名前 / Mod Name (Default: Town Of Host)
        public static readonly string ModName = "Town Of Host-K";
        // modの色 / Mod Color (Default: #00bfff)
        public static readonly string ModColor = "#00c1ff";
        // 公開ルームを許可する / Allow Public Room (Default: true)
        public static readonly bool AllowPublicRoom = true;
        // フォークID / ForkId (Default: OriginalTOH)
        public static readonly string ForkId = "TOH-K";
        // Discordボタンを表示するか / Show Discord Button (Default: true)
        public static readonly bool ShowDiscordButton = true;
        // Discordサーバーの招待リンク / Discord Server Invite URL (Default: https://discord.gg/W5ug6hXB9V)
        public static readonly string DiscordInviteUrl = "https://discord.gg/5DPqH8seFq";
        // ==========
        public const string OriginalForkId = "OriginalTOH"; // Don't Change The Value. / この値を変更しないでください。
        // == 認証設定 / Authentication Config ==
        // デバッグキーの認証インスタンス
        public static HashAuth DebugKeyAuth { get; private set; }
        // デバッグキーのハッシュ値
        public const string DebugKeyHash = "8e5f06e453e7d11f78ad96b2ca28ff472e085bdb053189612a0a2e0be7973841";
        // デバッグキーのソルト
        public const string DebugKeySalt = "59687b";
        // デバッグキーのコンフィグ入力
        public static ConfigEntry<string> DebugKeyInput { get; private set; }

        // ==========
        //Sorry for many Japanese comments.
        public const string PluginGuid = "com.kymario.townofhost-k";
        public const string PluginVersion = "5.1.61.1";
        // サポートされている最低のAmongUsバージョン
        public static readonly string LowestSupportedVersion = "2024.3.5";
        // このバージョンのみで公開ルームを無効にする場合
        public static readonly bool IsPublicAvailableOnThisVersion = false;
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
        public static Version version = Version.Parse(PluginVersion);
        public static BepInEx.Logging.ManualLogSource Logger;
        public static bool hasArgumentException = false;
        public static string ExceptionMessage;
        public static bool ExceptionMessageIsShown = false;
        public static string credentialsText;
        public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
        public static HideNSeekGameOptionsV07 HideNSeekSOptions => GameOptionsManager.Instance.currentHideNSeekGameOptions;
        //Client Options
        public static ConfigEntry<string> HideName { get; private set; }
        public static ConfigEntry<string> HideColor { get; private set; }
        public static ConfigEntry<bool> ForceJapanese { get; private set; }
        public static ConfigEntry<bool> JapaneseRoleName { get; private set; }
        public static ConfigEntry<int> MessageWait { get; private set; }
        public static ConfigEntry<bool> ShowResults { get; private set; }
        public static ConfigEntry<bool> ChangeSomeLanguage { get; private set; }
        public static ConfigEntry<bool> Hiderecommendedsettings { get; private set; }
        public static ConfigEntry<bool> UseWebHook { get; private set; }
        public static ConfigEntry<bool> UseYomiage { get; private set; }
        public static ConfigEntry<bool> UseZoom { get; private set; }
        public static ConfigEntry<bool> SyncYomiage { get; private set; }
        public static ConfigEntry<bool> CustomName { get; private set; }
        public static ConfigEntry<bool> HideResetToDefault { get; private set; }
        public static ConfigEntry<bool> CustomSprite { get; private set; }
        public static Dictionary<byte, PlayerVersion> playerVersion = new();
        //Preset Name Options
        public static ConfigEntry<string> Preset1 { get; private set; }
        public static ConfigEntry<string> Preset2 { get; private set; }
        public static ConfigEntry<string> Preset3 { get; private set; }
        public static ConfigEntry<string> Preset4 { get; private set; }
        public static ConfigEntry<string> Preset5 { get; private set; }
        //Other Configs
        public static ConfigEntry<string> BetaBuildURL { get; private set; }
        public static ConfigEntry<float> LastKillCooldown { get; private set; }
        public static ConfigEntry<float> LastShapeshifterCooldown { get; private set; }
        public static OptionBackupData RealOptionsData;
        public static Dictionary<byte, string> AllPlayerNames;
        public static Dictionary<(byte, byte), string> LastNotifyNames;
        public static Dictionary<byte, Color32> PlayerColors = new();
        public static Dictionary<byte, CustomDeathReason> AfterMeetingDeathPlayers = new();
        public static Dictionary<CustomRoles, string> roleColors;
        public static List<byte> winnerList;
        public static List<int> clientIdList;
        public static List<(string, byte, string)> MessagesToSend;
        public static bool isChatCommand = false;
        public static List<PlayerControl> ALoversPlayers = new();
        public static bool isALoversDead = true;
        public static List<PlayerControl> BLoversPlayers = new();
        public static bool isBLoversDead = true;
        public static List<PlayerControl> CLoversPlayers = new();
        public static bool isCLoversDead = true;
        public static List<PlayerControl> DLoversPlayers = new();
        public static bool isDLoversDead = true;
        public static List<PlayerControl> ELoversPlayers = new();
        public static bool isELoversDead = true;
        public static List<PlayerControl> FLoversPlayers = new();
        public static bool isFLoversDead = true;
        public static List<PlayerControl> GLoversPlayers = new();
        public static bool isGLoversDead = true;
        public static List<PlayerControl> MaMaLoversPlayers = new();
        public static bool isMaLoversDead = true;
        public static Dictionary<byte, float> AllPlayerKillCooldown = new();
        public static bool HnSFlag = false;
        public static List<List<byte>> TaskBattleTeams = new();
        public static bool RTAMode = false;
        public static bool EditMode = false;
        public static int page = 0;
        public static int day;
        public static string gamelog;
        public static Dictionary<int, List<Vector2>> CustomSpawnPosition = new();
        //public static bool TaskBattleOptionv = false;

        /// <summary>
        /// 基本的に速度の代入は禁止.スピードは増減で対応してください.
        /// </summary>
        public static Dictionary<byte, float> AllPlayerSpeed = new();
        public const float MinSpeed = 0.0001f;
        public static int AliveImpostorCount;
        public static int AliveNeutalCount;
        public static int SKMadmateNowCount;
        public static Dictionary<byte, bool> CheckShapeshift = new();
        public static Dictionary<byte, byte> ShapeshiftTarget = new();
        public static bool VisibleTasksCount;
        public static string nickName = "";
        public static bool introDestroyed = false;
        public static float DefaultCrewmateVision;
        public static float DefaultImpostorVision;
        public static bool IsChristmas = DateTime.Now.Month == 12 && DateTime.Now.Day is 24 or 25;
        public static bool White = DateTime.Now.Month == 3 && DateTime.Now.Day is 14;
        public static bool IsInitialRelease = DateTime.Now.Month == 10 && DateTime.Now.Day is 31;
        public static bool IsHalloween = DateTime.Now.Month == 10 && DateTime.Now.Day is 31;
        public static bool GoldenWeek = DateTime.Now.Month == 5 && DateTime.Now.Day is 3 or 4 or 5;
        public static bool April = DateTime.Now.Month == 4 && DateTime.Now.Day is 1;
        public static bool DebugAntiblackout = true;

        public const float RoleTextSize = 2f;

        public static IEnumerable<PlayerControl> AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.PlayerId <= 15);
        public static IEnumerable<PlayerControl> AllAlivePlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive() && p.PlayerId <= 15);

        public static Main Instance;
        public override void Load()
        {
            Instance = this;

            //Client Options
            HideName = Config.Bind("Client Options", "Hide Game Code Name", "Town Of Host-K");
            HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{ModColor}");
            ForceJapanese = Config.Bind("Client Options", "Force Japanese", false);
            JapaneseRoleName = Config.Bind("Client Options", "Japanese Role Name", true);
            ShowResults = Config.Bind("Result", "Show Results", true);
            ChangeSomeLanguage = Config.Bind("Client Options", "Change Some Language", true);
            Hiderecommendedsettings = Config.Bind("Client Options", "Hide recommended settings", false);
            UseWebHook = Config.Bind("Client Options", "UseWebHook", false);
            UseYomiage = Config.Bind("Client Options", "UseYomiage", false);
            UseZoom = Config.Bind("Client Options", "UseZoom", false);
            SyncYomiage = Config.Bind("Client Options", "SyncYomiage", true);
            CustomName = Config.Bind("Client Options", "CustomName", true);
            HideResetToDefault = Config.Bind("Client Options", "Hide ResetToDefault", false);
            CustomSprite = Config.Bind("Client Options", "CustomSprite", true);
            DebugKeyInput = Config.Bind("Authentication", "Debug Key", "");

            Logger = BepInEx.Logging.Logger.CreateLogSource("TownOfHost-K");
            TownOfHost.Logger.Enable();
            TownOfHost.Logger.Disable("NotifyRoles");
            TownOfHost.Logger.Disable("SendRPC");
            TownOfHost.Logger.Disable("ReceiveRPC");
            TownOfHost.Logger.Disable("SwitchSystem");
            TownOfHost.Logger.Disable("CustomRpcSender");
            //TownOfHost.Logger.isDetail = true;

            // 認証関連-初期化
            DebugKeyAuth = new HashAuth(DebugKeyHash, DebugKeySalt);

            // 認証関連-認証
            DebugModeManager.Auth(DebugKeyAuth, DebugKeyInput.Value);

            winnerList = new();
            VisibleTasksCount = false;
            MessagesToSend = new List<(string, byte, string)>();

            Preset1 = Config.Bind("Preset Name Options", "Preset1", "Preset_1");
            Preset2 = Config.Bind("Preset Name Options", "Preset2", "Preset_2");
            Preset3 = Config.Bind("Preset Name Options", "Preset3", "Preset_3");
            Preset4 = Config.Bind("Preset Name Options", "Preset4", "Preset_4");
            Preset5 = Config.Bind("Preset Name Options", "Preset5", "Preset_5");
            BetaBuildURL = Config.Bind("Other", "BetaBuildURL", "");
            MessageWait = Config.Bind("Other", "MessageWait", 1);
            LastKillCooldown = Config.Bind("Other", "LastKillCooldown", (float)30);
            LastShapeshifterCooldown = Config.Bind("Other", "LastShapeshifterCooldown", (float)30);

            PluginModuleInitializerAttribute.InitializeAll();
            Blacklist.FetchBlacklist();

            IRandom.SetInstance(new NetRandomWrapper());

            hasArgumentException = false;
            ExceptionMessage = "";
            try
            {

                roleColors = new Dictionary<CustomRoles, string>()
                {
                    // マッドメイト役職
                    {CustomRoles.SKMadmate, "#ff1919"},
                    //特殊クルー役職
                    //ニュートラル役職
                    //HideAndSeek
                    {CustomRoles.HASFox, "#e478ff"},
                    {CustomRoles.HASTroll, "#00ff00"},
                    //TaskBattle
                    {CustomRoles.TaskPlayerB, "#9adfff"},
                    // GM
                    {CustomRoles.GM, "#ff5b70"},
                    //サブ役職
                    {CustomRoles.LastImpostor, "#ff1919"},
                    {CustomRoles.LastNeutral,"#cccccc"},
                    {CustomRoles.Workhorse, "#00ffff"},

                    {CustomRoles.Watcher, "#800080"},
                    {CustomRoles.Speeding, "#33ccff"},
                    {CustomRoles.Moon,"#ffff33"},
                    {CustomRoles.Guesser,"#999900"},
                    {CustomRoles.Sun,"#ec6800"},
                    {CustomRoles.Director,"#cee4ae"},
                    {CustomRoles.Connecting,"#96514d"},
                    {CustomRoles.Serial,"#ff1919"},
                    {CustomRoles.AdditionalVoter,"#93ca76"},
                    {CustomRoles.Opener,"#007bbb"},
                    {CustomRoles.Bakeneko,"#ffcc99"},
                    {CustomRoles.Psychic,"#9933ff"},
                    {CustomRoles.Nurse,"#ffadd6"},
                    //デバフ
                    {CustomRoles.NotConvener,"#006666"},
                    {CustomRoles.Notvoter,"#6c848d"},
                    {CustomRoles.Water,"#17184b"},
                    {CustomRoles.LowBattery,"#660000"},
                    {CustomRoles.Slacker,"#460e44"},
                    {CustomRoles.Elector,"#544a47"},
                    {CustomRoles.Transparent,"#7b7c7d"},

                    //第三属性
                    {CustomRoles.ALovers, "#ff6be4"},
                    {CustomRoles.BLovers, "#d70035"},
                    {CustomRoles.CLovers, "#fac559"},
                    {CustomRoles.DLovers, "#6c9bd2"},
                    {CustomRoles.ELovers, "#00885a"},
                    {CustomRoles.FLovers, "#fdede4"},
                    {CustomRoles.GLovers, "#af0082"},
                    {CustomRoles.MaLovers, "#f09199"},

                    {CustomRoles.NotAssigned, "#ffffff"}
                };

                var type = typeof(RoleBase);
                var roleClassArray =
                CustomRoleManager.AllRolesClassType = Assembly.GetAssembly(type)
                    .GetTypes()
                    .Where(x => x.IsSubclassOf(type)).ToArray();

                foreach (var roleClassType in roleClassArray)
                    roleClassType.GetField("RoleInfo")?.GetValue(type);
            }
            catch (ArgumentException ex)
            {
                TownOfHost.Logger.Error("エラー:Dictionaryの値の重複を検出しました", "LoadDictionary");
                TownOfHost.Logger.Exception(ex, "LoadDictionary");
                hasArgumentException = true;
                ExceptionMessage = ex.Message;
                ExceptionMessageIsShown = false;
            }
            TownOfHost.Logger.Info($"{Application.version}", "AmongUs Version");

            var handler = TownOfHost.Logger.Handler("GitVersion");
            handler.Info($"{nameof(ThisAssembly.Git.Branch)}: {ThisAssembly.Git.Branch}");
            handler.Info($"{nameof(ThisAssembly.Git.BaseTag)}: {ThisAssembly.Git.BaseTag}");
            handler.Info($"{nameof(ThisAssembly.Git.Commit)}: {ThisAssembly.Git.Commit}");
            handler.Info($"{nameof(ThisAssembly.Git.Commits)}: {ThisAssembly.Git.Commits}");
            handler.Info($"{nameof(ThisAssembly.Git.IsDirty)}: {ThisAssembly.Git.IsDirty}");
            handler.Info($"{nameof(ThisAssembly.Git.Sha)}: {ThisAssembly.Git.Sha}");
            handler.Info($"{nameof(ThisAssembly.Git.Tag)}: {ThisAssembly.Git.Tag}");

            ClassInjector.RegisterTypeInIl2Cpp<ErrorText>();

            Harmony.PatchAll();
        }
    }
    public enum CustomDeathReason
    {
        Kill,
        Vote,
        Suicide,
        Spell,
        FollowingSuicide,
        Bite,
        Bombed,
        Misfire,
        Torched,
        Sniped,
        Revenge,
        Revenge1,
        Execution,
        Infected,
        Grim,
        Disconnected,
        Fall,
        Magic,
        Guess,
        TeleportKill,
        etc = -1
    }
    //WinData
    public enum CustomWinner
    {
        Draw = -1,
        Default = -2,
        None = -3,
        Impostor = CustomRoles.Impostor,
        Crewmate = CustomRoles.Crewmate,
        Jester = CustomRoles.Jester,
        PlagueDoctor = CustomRoles.PlagueDoctor,
        Terrorist = CustomRoles.Terrorist,
        ALovers = CustomRoles.ALovers,
        BLovers = CustomRoles.BLovers,
        CLovers = CustomRoles.CLovers,
        DLovers = CustomRoles.DLovers,
        ELovers = CustomRoles.ELovers,
        FLovers = CustomRoles.FLovers,
        GLovers = CustomRoles.GLovers,
        MaLovers = CustomRoles.MaLovers,
        Executioner = CustomRoles.Executioner,
        Arsonist = CustomRoles.Arsonist,
        Egoist = CustomRoles.Egoist,
        Jackal = CustomRoles.Jackal,
        Remotekiller = CustomRoles.Remotekiller,
        Chef = CustomRoles.Chef,
        GrimReaper = CustomRoles.GrimReaper,
        CountKiller = CustomRoles.CountKiller,
        God = CustomRoles.God,
        HASTroll = CustomRoles.HASTroll,
        TaskPlayerB = CustomRoles.TaskPlayerB,
    }
    /*public enum CustomRoles : byte
    {
        Default = 0,
        HASTroll = 1,
        HASHox = 2
    }*/
    public enum SuffixModes
    {
        None = 0,
        TOH,
        Streaming,
        Recording,
        RoomHost,
        OriginalName,
        Timer
    }
    public enum VoteMode
    {
        Default,
        Suicide,
        SelfVote,
        Skip
    }

    public enum TieMode
    {
        Default,
        All,
        Random
    }

    public enum CombinationRoles
    {
        None,
        AssassinandMerlin,
        DriverandBraid
    }
}
