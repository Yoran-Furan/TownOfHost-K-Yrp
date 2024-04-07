using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using TownOfHost.Modules;
using static TownOfHost.Translator;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    public class GameStartManagerPatch
    {
        public static float GetTimer() => timer;
        public static float SetTimer(float time) => timer = time;
        public static float Timer2 = 0; //毎秒タイマー送るのはあれだから
        private static float timer = 600f;
        private static TextMeshPro warningText;
        public static TextMeshPro HideName;
        private static TextMeshPro timerText;
        private static TextMeshPro GameMaster;
        private static SpriteRenderer cancelButton;

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
        public class GameStartManagerStartPatch
        {
            public static void Postfix(GameStartManager __instance)
            {
                __instance.MinPlayers = 1;

                __instance.GameRoomNameCode.text = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
                // Reset lobby countdown timer
                timer = 600f;
                Timer2 = 0f;
                //ゲームマスターONのテキスト HideNameの後に作るとおかしくなるので先にInstantiateしておく
                GameMaster = Object.Instantiate(__instance.GameRoomNameCode, __instance.StartButton.transform.parent);
                GameMaster.gameObject.SetActive(false);
                GameMaster.name = "GMText";
                GameMaster.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.GM), GetString("GameMasterON"));

                HideName = Object.Instantiate(__instance.GameRoomNameCode, __instance.GameRoomNameCode.transform);
                HideName.gameObject.SetActive(true);
                HideName.name = "HideName";
                HideName.color =
                    ColorUtility.TryParseHtmlString(Main.HideColor.Value, out var color) ? color :
                    ColorUtility.TryParseHtmlString(Main.ModColor, out var modColor) ? modColor : HideName.color;
                HideName.text = Main.HideName.Value;

                warningText = Object.Instantiate(__instance.GameStartText, __instance.transform);
                warningText.name = "WarningText";
                warningText.transform.localPosition = new(0f, 0f - __instance.transform.localPosition.y, -1f);
                warningText.gameObject.SetActive(false);

                timerText = Object.Instantiate(__instance.PlayerCounter, __instance.PlayerCounter.transform.parent);
                timerText.autoSizeTextContainer = false;
                timerText.fontSize = 3.2f;
                timerText.name = "Timer";
                timerText.DestroyChildren();
                timerText.transform.localPosition += Vector3.down * 0.2f;
                timerText.gameObject.SetActive(AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame);

                cancelButton = Object.Instantiate(__instance.StartButton, __instance.transform);
                cancelButton.name = "CancelButton";
                var cancelLabel = cancelButton.GetComponentInChildren<TextMeshPro>();
                cancelLabel.DestroyTranslator();
                cancelLabel.text = GetString("Cancel");
                cancelButton.transform.localScale = new(0.4f, 0.4f, 1f);
                cancelButton.color = Color.red;
                cancelButton.transform.localPosition = new(0f, -0.37f, 0f);
                var buttonComponent = cancelButton.GetComponent<PassiveButton>();
                buttonComponent.OnClick = new();
                buttonComponent.OnClick.AddListener((Action)(() => __instance.ResetStartState()));
                cancelButton.gameObject.SetActive(false);
                GameObject.Find("StartText_TMP").transform.position += new Vector3(0f, 0.3f, 0f);

                if (!AmongUsClient.Instance.AmHost) return;

                // Make Public Button
                if ((ModUpdater.isBroken || ModUpdater.hasUpdate || !Main.AllowPublicRoom || !VersionChecker.IsSupported || !ModUpdater.publicok || (!Main.IsPublicAvailableOnThisVersion && !Patches.CustomServerHelper.IsCs())) && !ModUpdater.matchmaking)
                {
                    __instance.MakePublicButton.color = Palette.DisabledClear;
                    __instance.privatePublicText.color = Palette.DisabledClear;
                }

                if (Main.NormalOptions.KillCooldown == 0f)
                    Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>());
                if (AURoleOptions.ShapeshifterCooldown == 0f)
                    AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
        public class GameStartManagerUpdatePatch
        {
            private static float exitTimer = 0f;
            private static float ext = 0f;
            public static void Prefix(GameStartManager __instance)
            {
                // Lobby code
                if (DataManager.Settings.Gameplay.StreamerMode)
                {
                    __instance.GameRoomNameCode.color = new(255, 255, 255, 0);
                    HideName.enabled = true;
                }
                else
                {
                    __instance.GameRoomNameCode.color = new(255, 255, 255, 255);
                    HideName.enabled = false;
                }

                // GameMaster Text
                GameMaster.gameObject.SetActive(Options.EnableGM.GetBool());
                GameMaster.transform.localPosition = new Vector3(0f, GameStates.IsCountDown ? 0f : AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame ? -0.4f : -0.6f);
            }
            public static void Postfix(GameStartManager __instance)
            {
                if (!AmongUsClient.Instance) return;
                string warningMessage = "";
                if (AmongUsClient.Instance.AmHost)
                {
                    bool canStartGame = true;
                    List<string> mismatchedPlayerNameList = new();
                    foreach (var client in AmongUsClient.Instance.allClients.ToArray())
                    {
                        if (client.Character == null) continue;
                        var dummyComponent = client.Character.GetComponent<DummyBehaviour>();
                        if (dummyComponent != null && dummyComponent.enabled)
                            continue;
                        if (Options.KickModClient.GetBool() && Client(client.Character.PlayerId) && client.Character.PlayerId != 0)
                        {
                            canStartGame = false;
                            mismatchedPlayerNameList.Add(Utils.ColorString(Palette.PlayerColors[client.ColorId], client.Character.Data.PlayerName));
                        }
                        if (!MatchVersions(client.Character.PlayerId, true))
                        {
                            canStartGame = false;
                            mismatchedPlayerNameList.Add(Utils.ColorString(Palette.PlayerColors[client.ColorId], client.Character.Data.PlayerName));
                        }
                    }
                    if (!canStartGame)
                    {
                        __instance.StartButton.gameObject.SetActive(false);
                        warningMessage = Utils.ColorString(Color.red, string.Format(GetString("Warning.MismatchedVersion"), String.Join(" ", mismatchedPlayerNameList), $"<color={Main.ModColor}>{Main.ModName}</color>"));
                    }
                    cancelButton.gameObject.SetActive(__instance.startState == GameStartManager.StartingStates.Countdown);
                }
                else
                {
                    if (Options.KickModClient.GetBool())
                    {
                        if (GameStates.IsModHost)
                        {
                            ext += Time.deltaTime;
                            if (ext > 10)
                            {
                                ext = 0;
                                AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                                SceneChanger.ChangeScene("MainMenu");
                            }
                            warningMessage = Utils.ColorString(Color.red, string.Format(GetString("Warning.CantModClient"), $"<color={Main.ModColor}>{Main.ModName}</color>", Math.Round(10 - ext).ToString()));
                        }
                    }
                    if (MatchVersions(0))
                        exitTimer = 0;
                    else
                    {
                        exitTimer += Time.deltaTime;
                        if (exitTimer > 10)
                        {
                            exitTimer = 0;
                            AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                            SceneChanger.ChangeScene("MainMenu");
                        }

                        warningMessage = Utils.ColorString(Color.red, string.Format(GetString("Warning.AutoExitAtMismatchedVersion"), $"<color={Main.ModColor}>{Main.ModName}</color>", Math.Round(10 - exitTimer).ToString()));
                    }
                }
                if (warningMessage == "")
                {
                    warningText.gameObject.SetActive(false);
                }
                else
                {
                    warningText.text = warningMessage;
                    warningText.gameObject.SetActive(true);
                }

                // Lobby timer
                if (!GameData.Instance || AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
                    return;
                timer = Mathf.Max(0f, timer -= Time.deltaTime);
                int minutes = (int)timer / 60;
                int seconds = (int)timer % 60;
                string countDown = $"({minutes:00}:{seconds:00})";
                if (timer <= 60) countDown = Utils.ColorString(Color.red, countDown);
                timerText.text = countDown;
            }
            private static bool MatchVersions(byte playerId, bool acceptVanilla = false)
            {
                if (!Main.playerVersion.TryGetValue(playerId, out var version)) return acceptVanilla;
                return Main.ForkId == version.forkId
                    && Main.version.CompareTo(version.version) == 0
                    && version.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})";
            }
            private static bool Client(byte playerId)
                => Main.playerVersion.TryGetValue(playerId, out var version);
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
        public static class GameStartManagerBeginGamePatch
        {
            public static bool Prefix(GameStartManager __instance)
            {
                SelectRandomMap();

                var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
                if (invalidColor.Any())
                {
                    var msg = GetString("Error.InvalidColor");
                    Logger.SendInGame(msg);
                    msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}({p.Data.DefaultOutfit.ColorId})"));
                    Utils.SendMessage(msg);
                    return false;
                }

                if (Options.CurrentGameMode == CustomGameMode.TaskBattle && Options.TaskBattleTeamMode.GetBool())
                {
                    //チェック
                    var teamc = Math.Min(Options.TaskBattleTeamC.GetFloat(), Main.AllPlayerControls.Count());
                    var playerc = Main.AllPlayerControls.Count() / teamc;

                    //チーム数でプレイヤーが足りない場合
                    if (Options.TaskBattleTeamC.GetFloat() > Main.AllPlayerControls.Count())
                    {
                        var msg = GetString("Warning.MoreTeamsThanPlayers");
                        Logger.SendInGame(msg);
                        Logger.Warn(msg, "BeginGame");
                    }
                    //合計タスク数が足りない場合
                    if (Options.TaskBattleTeamWinType.GetBool() && Main.NormalOptions.TotalTaskCount * playerc < Options.TaskBattleTeamWinTaskc.GetFloat())
                    {
                        var msg = GetString("Warning.TBTask");
                        Logger.SendInGame(msg);
                        Logger.Warn(msg, "BeginGame");
                    }
                }

                RoleAssignManager.CheckRoleCount();

                Options.DefaultKillCooldown = Main.NormalOptions.KillCooldown;
                Main.LastKillCooldown.Value = Main.NormalOptions.KillCooldown;
                Main.NormalOptions.KillCooldown = 0f;

                var opt = Main.NormalOptions.Cast<IGameOptions>();
                AURoleOptions.SetOpt(opt);
                Main.LastShapeshifterCooldown.Value = AURoleOptions.ShapeshifterCooldown;
                AURoleOptions.ShapeshifterCooldown = 0f;

                PlayerControl.LocalPlayer.RpcSyncSettings(GameOptionsManager.Instance.gameOptionsFactory.ToBytes(opt, AprilFoolsMode.IsAprilFoolsModeToggledOn));

                __instance.ReallyBegin(false);
                TemplateManager.SendTemplate("Start", noErr: true);
                return false;
            }
            private static void SelectRandomMap()
            {
                if (Options.RandomMapsMode.GetBool())
                {
                    var rand = IRandom.Instance;
                    List<byte> randomMaps = new();
                    /*TheSkeld   = 0
                    MIRAHQ     = 1
                    Polus      = 2
                    Dleks      = 3
                    TheAirShip = 4
                    TheFungle  = 5*/
                    if (Options.AddedTheSkeld.GetBool()) randomMaps.Add(0);
                    if (Options.AddedMiraHQ.GetBool()) randomMaps.Add(1);
                    if (Options.AddedPolus.GetBool()) randomMaps.Add(2);
                    // if (Options.AddedDleks.GetBool()) RandomMaps.Add(3);
                    if (Options.AddedTheAirShip.GetBool()) randomMaps.Add(4);
                    if (Options.AddedTheFungle.GetBool()) randomMaps.Add(5);

                    if (randomMaps.Count <= 0) return;
                    var mapsId = randomMaps[rand.Next(randomMaps.Count)];
                    Main.NormalOptions.MapId = mapsId;
                }
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.ResetStartState))]
        class ResetStartStatePatch
        {
            public static void Prefix()
            {
                if (GameStates.IsCountDown)
                {
                    Main.NormalOptions.KillCooldown = Options.DefaultKillCooldown;
                    PlayerControl.LocalPlayer.RpcSyncSettings(GameOptionsManager.Instance.gameOptionsFactory.ToBytes(GameOptionsManager.Instance.CurrentGameOptions, AprilFoolsMode.IsAprilFoolsModeToggledOn));
                }
            }
        }
    }

    [HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.SetText))]
    public static class HiddenTextPatch
    {
        private static void Postfix(TextBoxTMP __instance)
        {
            if (__instance.name == "GameIdText") __instance.outputText.text = new string('*', __instance.text.Length);
        }
    }

    [HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
    class UnrestrictedNumImpostorsPatch
    {
        public static bool Prefix(ref int __result)
        {
            __result = Main.NormalOptions.NumImpostors;
            return false;
        }
    }

    [HarmonyPatch(typeof(LobbyTimerExtensionUI), nameof(LobbyTimerExtensionUI.ShowLobbyTimer))]
    class ShowLobbyTimerPatch
    {
        public static void Postfix(LobbyTimerExtensionUI __instance, [HarmonyArgument(0)] int timeRemainingSeconds)
        {
            //タイマー関連だからここに置かせて！
            GameStartManagerPatch.SetTimer(timeRemainingSeconds + 1);
        }
    }
}
