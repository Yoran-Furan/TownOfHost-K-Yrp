using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.AddOns.Crewmate;
using TownOfHost.Roles.AddOns.Common;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckProtect))]
    class CheckProtectPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return false;
            Logger.Info("CheckProtect発生: " + __instance.GetNameWithRole() + "=>" + target.GetNameWithRole(), "CheckProtect");
            if (__instance.Is(CustomRoles.Sheriff))
            {
                if (__instance.Data.IsDead)
                {
                    Logger.Info("守護をブロックしました。", "CheckProtect");
                    return false;
                }
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    class CheckMurderPatch
    {
        public static Dictionary<byte, float> TimeSinceLastKill = new();
        public static void Update()
        {
            for (byte i = 0; i < 15; i++)
            {
                if (TimeSinceLastKill.ContainsKey(i))
                {
                    TimeSinceLastKill[i] += Time.deltaTime;
                    if (15f < TimeSinceLastKill[i]) TimeSinceLastKill.Remove(i);
                }
            }
        }
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return false;
            // 処理は全てCustomRoleManager側で行う
            if (!CustomRoleManager.OnCheckMurder(__instance, target))
            {
                // キル失敗
                __instance.RpcMurderPlayer(target, false);
            }

            return false;
        }

        // 不正キル防止チェック
        public static bool CheckForInvalidMurdering(MurderInfo info)
        {
            (var killer, var target) = info.AttemptTuple;

            // Killerが既に死んでいないかどうか
            if (!killer.IsAlive())
            {
                Logger.Info($"{killer.GetNameWithRole()}は死亡しているためキャンセルされました。", "CheckMurder");
                return false;
            }
            // targetがキル可能な状態か
            if (
                // PlayerDataがnullじゃないか確認
                target.Data == null ||
                // targetの状態をチェック
                target.inVent ||
                target.MyPhysics.Animations.IsPlayingEnterVentAnimation() ||
                target.MyPhysics.Animations.IsPlayingAnyLadderAnimation() ||
                target.inMovingPlat)
            {
                Logger.Info("targetは現在キルできない状態です。", "CheckMurder");
                return false;
            }
            // targetが既に死んでいないか
            if (!target.IsAlive())
            {
                Logger.Info("targetは既に死んでいたため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }
            // 会議中のキルでないか
            if (MeetingHud.Instance != null)
            {
                Logger.Info("会議が始まっていたため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }

            // 連打キルでないか
            float minTime = Mathf.Max(0.02f, AmongUsClient.Instance.Ping / 1000f * 6f); //※AmongUsClient.Instance.Pingの値はミリ秒(ms)なので÷1000
            //TimeSinceLastKillに値が保存されていない || 保存されている時間がminTime以上 => キルを許可
            //↓許可されない場合
            if (TimeSinceLastKill.TryGetValue(killer.PlayerId, out var time) && time < minTime)
            {
                Logger.Info("前回のキルからの時間が早すぎるため、キルをブロックしました。", "CheckMurder");
                return false;
            }
            TimeSinceLastKill[killer.PlayerId] = 0f;

            // HideAndSeek_キルボタンが使用可能か
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Options.HideAndSeekKillDelayTimer > 0)
            {
                Logger.Info("HideAndSeekの待機時間中だったため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }
            // キルが可能なプレイヤーか(遠隔は除く)
            if (!info.IsFakeSuicide && !killer.CanUseKillButton())
            {
                Logger.Info(killer.GetNameWithRole() + "はKillできないので、キルはキャンセルされました。", "CheckMurder");
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    class MurderPlayerPatch
    {
        private static readonly LogHandler logger = Logger.Handler(nameof(PlayerControl.MurderPlayer));
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] MurderResultFlags resultFlags, ref bool __state /* 成功したキルかどうか */ )
        {
            Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}{(target.protectedByGuardianThisRound ? "(Protected)" : "")}", "MurderPlayer");

            logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}({resultFlags})");
            var isProtectedByClient = resultFlags.HasFlag(MurderResultFlags.DecisionByHost) && target.IsProtected();
            var isProtectedByHost = resultFlags.HasFlag(MurderResultFlags.FailedProtected);
            var isFailed = resultFlags.HasFlag(MurderResultFlags.FailedError);
            var isSucceeded = __state = !isProtectedByClient && !isProtectedByHost && !isFailed;
            if (isProtectedByClient)
            {
                logger.Info("守護されているため，キルは失敗します");
            }
            if (isProtectedByHost)
            {
                logger.Info("守護されているため，キルはホストによってキャンセルされました");
            }
            if (isFailed)
            {
                logger.Info("キルはホストによってキャンセルされました");
            }

            if (isSucceeded)
            {
                if (target.shapeshifting)
                {
                    //シェイプシフトアニメーション中
                    //アニメーション時間を考慮して1s、加えてクライアントとのラグを考慮して+0.5s遅延する
                    _ = new LateTask(
                        () =>
                        {
                            if (GameStates.IsInTask)
                            {
                                target.RpcShapeshift(target, false);
                            }
                        },
                        1.5f, "RevertShapeshift");
                }
                else
                {
                    if (Main.CheckShapeshift.TryGetValue(target.PlayerId, out var shapeshifting) && shapeshifting)
                    {
                        //シェイプシフト強制解除
                        target.RpcShapeshift(target, false);
                    }
                }
                Camouflage.RpcSetSkin(target, ForceRevert: true, RevertToDefault: true);

            }
        }
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, bool __state)
        {
            // キルが成功していない場合，何もしない
            if (!__state)
            {
                return;
            }
            if (target.AmOwner) RemoveDisableDevicesPatch.UpdateDisableDevices();
            if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost) return;
            //以降ホストしか処理しない
            // 処理は全てCustomRoleManager側で行う
            CustomRoleManager.OnMurderPlayer(__instance, target);
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
    class ShapeshiftPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            Logger.Info($"{__instance?.GetNameWithRole()} => {target?.GetNameWithRole()}", "Shapeshift");

            var shapeshifter = __instance;
            var shapeshifting = shapeshifter.PlayerId != target.PlayerId;

            if (Main.CheckShapeshift.TryGetValue(shapeshifter.PlayerId, out var last) && last == shapeshifting)
            {
                Logger.Info($"{__instance?.GetNameWithRole()}:Cancel Shapeshift.Prefix", "Shapeshift");
                return;
            }

            Main.CheckShapeshift[shapeshifter.PlayerId] = shapeshifting;
            Main.ShapeshiftTarget[shapeshifter.PlayerId] = target.PlayerId;

            shapeshifter.GetRoleClass()?.OnShapeshift(target);

            if (!AmongUsClient.Instance.AmHost) return;

            if (!shapeshifting) Camouflage.RpcSetSkin(__instance);

            //変身解除のタイミングがずれて名前が直せなかった時のために強制書き換え
            if (!shapeshifting)
            {
                _ = new LateTask(() =>
                {
                    Utils.NotifyRoles(NoCache: true);
                },
                1.2f, "ShapeShiftNotify");
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckShapeshift))]
    public static class PlayerControlCheckShapeshiftPatch
    {
        private static readonly LogHandler logger = Logger.Handler(nameof(PlayerControl.CheckShapeshift));

        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool shouldAnimate)
        {
            if (Main.EditMode && GameStates.IsFreePlay)
            {
                var mapid = AmongUsClient.Instance.TutorialMapId;
                if (target.PlayerId is 1)
                {
                    if (Main.page is 0)
                    {
                        if (Main.CustomSpawnPosition[mapid].Count < 8)
                            Main.CustomSpawnPosition[mapid].Add(__instance.transform.position);
                    }
                    else
                        __instance.NetTransform.SnapTo(Main.CustomSpawnPosition[mapid][Main.page - 2]);
                }
                else if (Main.page is not 0)
                {
                    if (target.PlayerId is 2)
                    {
                        Main.CustomSpawnPosition[mapid][Main.page - 2] = __instance.transform.position;
                    }
                    if (target.PlayerId is 3)
                    {
                        Main.CustomSpawnPosition[mapid].Remove(Main.CustomSpawnPosition[mapid][Main.page - 2]);
                    }
                    if (target.PlayerId is 4)
                    {
                        Minigame.Instance.ForceClose();
                        Main.page = 0;
                        _ = new LateTask(() => DestroyableSingleton<HudManager>.Instance.AbilityButton.DoClick(), 0.03f, "Open Menu");
                    }
                }
                else if (target.PlayerId <= Main.CustomSpawnPosition[mapid].Count + 1)
                {
                    Minigame.Instance.ForceClose();
                    _ = new LateTask(() =>
                    {
                        PlayerControl.AllPlayerControls[1].SetName("スポーンに移動する");
                        PlayerControl.AllPlayerControls[2].SetName("ここに移動させる");
                        PlayerControl.AllPlayerControls[3].SetName("削除する");
                        PlayerControl.AllPlayerControls[4].SetName("戻る");
                        foreach (var pc in PlayerControl.AllPlayerControls)
                            if (pc.PlayerId > 4) pc.SetName("<size=0>");
                        DestroyableSingleton<HudManager>.Instance.AbilityButton.DoClick();
                        Main.page = target.PlayerId;
                    }, 0.03f, "Open Menu");
                }
                __instance.RpcRejectShapeshift();
                return false;
            }

            if (AmongUsClient.Instance.IsGameOver || !AmongUsClient.Instance.AmHost)
            {
                return false;
            }

            // 無効な変身を弾く．これより前に役職等の処理をしてはいけない
            if (!CheckInvalidShapeshifting(__instance, target, shouldAnimate))
            {
                __instance.RpcRejectShapeshift();
                return false;
            }

            var button = (__instance.GetRoleClass() as IUseTheShButton)?.CheckShapeshift(__instance, target);
            if (button.HasValue)
            {
                shouldAnimate = false;
                return button.Value;
            }

            var shapeshifter = __instance;
            var shapeshifting = shapeshifter.PlayerId != target.PlayerId;
            // 変身したとき一番近い人をマッドメイトにする処理
            if (shapeshifter.CanMakeMadmate() && shapeshifting)
            {
                var sidekickable = shapeshifter.GetRoleClass() as ISidekickable;
                var targetRole = sidekickable?.SidekickTargetRole ?? CustomRoles.SKMadmate;

                Vector2 shapeshifterPosition = shapeshifter.transform.position;//変身者の位置
                Dictionary<PlayerControl, float> mpdistance = new();
                float dis;
                foreach (var p in Main.AllAlivePlayerControls)
                {
                    if (p.Data.Role.Role != RoleTypes.Shapeshifter && !p.Is(CustomRoleTypes.Impostor) && !p.Is(targetRole))
                    {
                        dis = Vector2.Distance(shapeshifterPosition, p.transform.position);
                        mpdistance.Add(p, dis);
                    }
                }
                if (mpdistance.Count != 0)
                {
                    var min = mpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                    PlayerControl targetm = min.Key;
                    targetm.RpcSetCustomRole(targetRole);
                    Logger.Info($"Make SKMadmate:{targetm.name}", "Shapeshift");
                    Main.SKMadmateNowCount++;
                    Utils.MarkEveryoneDirtySettings();
                    Utils.NotifyRoles();
                }
            }
            // 役職の処理
            var role = shapeshifter.GetRoleClass();
            if (role?.CheckShapeshift(target, ref shouldAnimate) == false)
            {
                if (role.CanDesyncShapeshift)
                {
                    shapeshifter.RpcSpecificRejectShapeshift(target, shouldAnimate);
                }
                else
                {
                    shapeshifter.RpcRejectShapeshift();
                }
                return false;
            }

            shapeshifter.RpcShapeshift(target, shouldAnimate);
            return false;
        }
        private static bool CheckInvalidShapeshifting(PlayerControl instance, PlayerControl target, bool animate)
        {
            logger.Info($"Checking shapeshift {instance.GetNameWithRole()} -> {(target == null || target.Data == null ? "(null)" : target.GetNameWithRole())}");

            if (!target || target.Data == null)
            {
                logger.Info("targetがnullのため変身をキャンセルします");
                return false;
            }
            if (!instance.IsAlive())
            {
                logger.Info("変身者が死亡しているため変身をキャンセルします");
                return false;
            }
            // RoleInfoによるdesyncシェイプシフター用の判定を追加
            if (instance.Data.Role.Role != RoleTypes.Shapeshifter && instance.GetCustomRole().GetRoleInfo()?.BaseRoleType?.Invoke() != RoleTypes.Shapeshifter)
            {
                logger.Info("変身者がシェイプシフターではないため変身をキャンセルします");
                return false;
            }
            if (instance.Data.Disconnected)
            {
                logger.Info("変身者が切断済のため変身をキャンセルします");
                return false;
            }
            if (target.IsMushroomMixupActive() && animate)
            {
                logger.Info("キノコカオス中のため変身をキャンセルします");
                return false;
            }
            if (MeetingHud.Instance && animate)
            {
                logger.Info("会議中のため変身をキャンセルします");
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    class ReportDeadBodyPatch
    {
        public static Dictionary<byte, bool> CanReport;
        public static Dictionary<byte, bool> Musisuruoniku;
        public static Dictionary<byte, List<GameData.PlayerInfo>> WaitReport = new();
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
        {
            if (GameStates.IsMeeting) return false;
            GameStates.Meeting = true;
            Logger.Info($"{__instance.GetNameWithRole()} => {target?.Object?.GetNameWithRole() ?? "null"}", "ReportDeadBody");
            if (Options.IsStandardHAS && target != null && __instance == target.Object) return true; //[StandardHAS] ボタンでなく、通報者と死体が同じなら許可
            if (Options.CurrentGameMode is CustomGameMode.HideAndSeek or CustomGameMode.TaskBattle || Options.IsStandardHAS) return false;
            if (!CanReport[__instance.PlayerId])
            {
                GameStates.Meeting = false;
                WaitReport[__instance.PlayerId].Add(target);
                Logger.Warn($"{__instance.GetNameWithRole()}:通報禁止中のため可能になるまで待機します", "ReportDeadBody");
                return false;
            }
            /*if (Utils.IsActive(SystemTypes.Comms) && Options.CommRepo.GetBool())
            {
                GameStates.Meeting = false;
                WaitReport[__instance.PlayerId].Add(target);
                Logger.Info("コミュサボ中はレポート出来なくするため、レポートをキャンセルします。", "ReportDeadBody");
                return false;
            }*/
            if (__instance.Is(CustomRoles.NotConvener))
            {
                if (NotConvener.Mode == NotConvener.Convener.ConvenerAll)
                {
                    GameStates.Meeting = false;
                    Logger.Info($"NotCoonvenerの設定がALLだから通報を全てキャンセルする。", "ReportDeadBody");
                    return false;
                }
                if (target == null && NotConvener.Mode == NotConvener.Convener.NotButton)
                {
                    GameStates.Meeting = false;
                    Logger.Info($"NotCoonvenerの設定がボタンのみだからこれはキャンセルする。", "ReportDeadBody");
                    return false;
                }
                if (target != null && NotConvener.Mode == NotConvener.Convener.NotReport)
                {
                    GameStates.Meeting = false;
                    Logger.Info($"NotCoonvenerの設定がレポートのみだから通報をキャンセルする。", "ReportDeadBody");
                    return false;
                }
            }

            if (target != null)
            {
                var tage = Utils.GetPlayerById(target.PlayerId);
                if (tage.Is(CustomRoles.Transparent))
                {
                    GameStates.Meeting = false;
                    Logger.Info($"ターゲットがトランスパレントだから通報をキャンセルする。", "ReportDeadBody");
                    return false;
                }
                else
                if (!Musisuruoniku[tage.PlayerId])
                {
                    GameStates.Meeting = false;
                    Logger.Info($"ターゲットがなんらかの理由で無視されるようになってるので通報をキャンセルする。", "ReportDeadBody");
                    return false;
                }
            }
            if (!AmongUsClient.Instance.AmHost) return true;

            //通報者が死んでいる場合、本処理で会議がキャンセルされるのでここで止める
            if (__instance.Data.IsDead) { GameStates.Meeting = false; return false; }

            foreach (var role in CustomRoleManager.AllActiveRoles.Values)
            {
                if (role.CancelReportDeadBody(__instance, target))
                {
                    GameStates.Meeting = false; return false;
                }
            }

            if (Options.SyncButtonMode.GetBool() && target == null)
            {
                Logger.Info("最大:" + Options.SyncedButtonCount.GetInt() + ", 現在:" + Options.UsedButtonCount, "ReportDeadBody");
                if (Options.SyncedButtonCount.GetFloat() <= Options.UsedButtonCount)
                {
                    GameStates.Meeting = false;
                    Logger.Info("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", "ReportDeadBody");
                    return false;
                }
                else Options.UsedButtonCount++;
                if (Options.SyncedButtonCount.GetFloat() == Options.UsedButtonCount)
                {
                    Logger.Info("使用可能ボタン回数が最大数に達しました。", "ReportDeadBody");
                }
            }

            //=============================================
            //以下、ボタンが押されることが確定したものとする。
            //=============================================
            if (target != null)
            {
                Main.gamelog += $"\n{System.DateTime.Now.ToString("HH.mm.ss")} [Meeting]　" + Utils.GetPlayerColor(target.PlayerId, true) + Translator.GetString("Meeting.Report") + "\n┗  " + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(__instance.PlayerId, true));
                MeetingHudPatch.Oniku = Utils.GetPlayerColor(target.PlayerId, true) + Translator.GetString("Meeting.Report") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(__instance.PlayerId, true));
            }
            else
            {
                Main.gamelog += $"\n{System.DateTime.Now.ToString("HH.mm.ss")} [Meeting]　" + Translator.GetString("Meeting.Button") + "\n┗  " + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(__instance.PlayerId, true));
                MeetingHudPatch.Oniku = Translator.GetString("Meeting.Button") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(__instance.PlayerId, true));
            }
            foreach (var kvp in PlayerState.AllPlayerStates)
            {
                var pc = Utils.GetPlayerById(kvp.Key);
                kvp.Value.LastRoom = pc.GetPlainShipRoom();
            }
            foreach (var role in CustomRoleManager.AllActiveRoles.Values)
            {
                role.OnReportDeadBody(__instance, target);
            }

            var State = PlayerState.GetByPlayerId(__instance.PlayerId);
            if (State.NumberOfRemainingButtons > 0 && target is null)
                State.NumberOfRemainingButtons--;

            Main.AllPlayerControls
                .Where(pc => Main.CheckShapeshift.ContainsKey(pc.PlayerId))
                .Do(pc => Camouflage.RpcSetSkin(pc, RevertToDefault: true));
            MeetingTimeManager.OnReportDeadBody();

            Utils.NotifyRoles(isForMeeting: true, NoCache: true);

            Utils.SyncAllSettings();
            return true;
        }
        public static async void ChangeLocalNameAndRevert(string name, int time)
        {
            //async Taskじゃ警告出るから仕方ないよね。
            var revertName = PlayerControl.LocalPlayer.name;
            PlayerControl.LocalPlayer.RpcSetNameEx(name);
            await Task.Delay(time);
            PlayerControl.LocalPlayer.RpcSetNameEx(revertName);
        }
    }
    /*[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
    public static class PlayerControlStartMeetingPatch
    {
        public static void Prefix()
        {
            foreach (var kvp in PlayerState.AllPlayerStates)
            {
                var pc = Utils.GetPlayerById(kvp.Key);
                kvp.Value.LastRoom = pc.GetPlainShipRoom();
            }
        }
    }*/
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdatePatch
    {
        private static StringBuilder Mark = new(20);
        private static StringBuilder Suffix = new(120);
        public static void Postfix(PlayerControl __instance)
        {
            var player = __instance;

            if (Main.EditMode && GameStates.IsFreePlay)
            {
                var mapid = AmongUsClient.Instance.TutorialMapId;
                if (player.PlayerId is 1)
                {
                    player.SetName(Main.CustomSpawnPosition[mapid].Count > 7 ? "これ以上追加できません" : "追加");
                    player.NetTransform.SnapTo(new Vector2(9999f, 9999f));
                }
                else if (player.PlayerId is not 0)
                {
                    var check = Main.CustomSpawnPosition[mapid].Count >= player.PlayerId - 1;
                    player.SetName(check ? $"カスタムスポーン{player.PlayerId - 1}" : "<size=0>");
                    player.NetTransform.SnapTo(check ? Main.CustomSpawnPosition[mapid][player.PlayerId - 2] : new Vector2(9999f, 9999f));
                }

                if (Minigame.Instance.IsDestroyedOrNull() && Main.page is not 0)
                    Main.page = 0;
                return;
            }

            if (!GameStates.IsModHost) return;

            TargetArrow.OnFixedUpdate(player);

            CustomRoleManager.OnFixedUpdate(player);

            if (AmongUsClient.Instance.AmHost)
            {//実行クライアントがホストの場合のみ実行
                if (GameStates.IsLobby && (ModUpdater.hasUpdate || ModUpdater.isBroken || !Main.AllowPublicRoom || !VersionChecker.IsSupported || !ModUpdater.publicok || (!Main.IsPublicAvailableOnThisVersion && !Patches.CustomServerHelper.IsCs())) && AmongUsClient.Instance.IsGamePublic)
                    AmongUsClient.Instance.ChangeGamePublic(false);

                if (GameStates.IsInTask && ReportDeadBodyPatch.CanReport[__instance.PlayerId] && ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Count > 0)
                {
                    var info = ReportDeadBodyPatch.WaitReport[__instance.PlayerId][0];
                    ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Clear();
                    Logger.Info($"{__instance.GetNameWithRole()}:通報可能になったため通報処理を行います", "ReportDeadbody");
                    __instance.ReportDeadBody(info);
                }

                DoubleTrigger.OnFixedUpdate(player);

                //ターゲットのリセット
                if (GameStates.IsInTask && player.IsAlive() && Options.LadderDeath.GetBool())
                {
                    FallFromLadder.FixedUpdate(player);
                }

                if (GameStates.IsInGame)
                {
                    ALoversSuicide();
                    BLoversSuicide();
                    CLoversSuicide();
                    DLoversSuicide();
                    ELoversSuicide();
                    FLoversSuicide();
                    GLoversSuicide();
                }
                if (GameStates.IsInGame) MadonnaLoversSuicide();

                if (GameStates.IsInGame && player.AmOwner)
                    DisableDevice.FixedUpdate();

                if (__instance.AmOwner)
                {
                    Utils.ApplySuffix();
                }
            }
            //LocalPlayer専用
            if (__instance.AmOwner)
            {
                //キルターゲットの上書き処理
                if (GameStates.IsInTask && !(__instance.Is(CustomRoleTypes.Impostor) || __instance.Is(CustomRoles.Egoist)) && __instance.CanUseKillButton() && !__instance.Data.IsDead)
                {
                    var players = __instance.GetPlayersInAbilityRangeSorted(false);
                    PlayerControl closest = players.Count <= 0 ? null : players[0];
                    HudManager.Instance.KillButton.SetTarget(closest);
                }
            }

            //役職テキストの表示
            var RoleTextTransform = __instance.cosmetics.nameText.transform.Find("RoleText");
            var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();
            if (RoleText != null && __instance != null)
            {
                if (GameStates.IsLobby)
                {
                    if (Main.playerVersion.TryGetValue(__instance.PlayerId, out var ver))
                    {
                        if (Main.ForkId != ver.forkId) // フォークIDが違う場合
                            __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>{ver.forkId}</size>\n{__instance?.name}</color>";
                        else if (Main.version.CompareTo(ver.version) == 0)
                            __instance.cosmetics.nameText.text = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})" ? $"<color=#87cefa>{__instance.name}</color>" : $"<color=#ffff00><size=1.2>{ver.tag}</size>\n{__instance?.name}</color>";
                        else __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>v{ver.version}</size>\n{__instance?.name}</color>";
                    }
                    else __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                }
                if (GameStates.IsInGame)
                {
                    //if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                    //{
                    //    var hasRole = main.AllPlayerCustomRoles.TryGetValue(__instance.PlayerId, out var role);
                    //    if (hasRole) RoleTextData = Utils.GetRoleTextHideAndSeek(__instance.Data.Role.Role, role);
                    //}
                    (RoleText.enabled, RoleText.text) = Utils.GetRoleNameAndProgressTextData(PlayerControl.LocalPlayer, __instance);
                    if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                    {
                        RoleText.enabled = false; //ゲームが始まっておらずフリープレイでなければロールを非表示
                        if (!__instance.AmOwner) __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                    }

                    //変数定義
                    var seer = PlayerControl.LocalPlayer;
                    var seerRole = seer.GetRoleClass();
                    var target = __instance;
                    string RealName;
                    Mark.Clear();
                    Suffix.Clear();

                    //名前変更
                    RealName = target.GetRealName();

                    //NameColorManager準拠の処理
                    RealName = RealName.ApplyNameColorData(seer, target, false);

                    //seer役職が対象のMark
                    Mark.Append(seerRole?.GetMark(seer, target, false));
                    //seerに関わらず発動するMark
                    Mark.Append(CustomRoleManager.GetMarkOthers(seer, target, false));

                    //ハートマークを付ける(会議中MOD視点)
                    if (__instance.Is(CustomRoles.ALovers) && PlayerControl.LocalPlayer.Is(CustomRoles.ALovers))
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.ALovers)}>♥</color>");
                    }
                    else if (__instance.Is(CustomRoles.ALovers) && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.ALovers)}>♥</color>");
                    }
                    if (__instance.Is(CustomRoles.BLovers) && PlayerControl.LocalPlayer.Is(CustomRoles.BLovers)) Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.BLovers)}>♥</color>");
                    else if (__instance.Is(CustomRoles.BLovers) && PlayerControl.LocalPlayer.Data.IsDead) Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.BLovers)}>♥</color>");
                    if (__instance.Is(CustomRoles.CLovers) && PlayerControl.LocalPlayer.Is(CustomRoles.CLovers)) Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.CLovers)}>♥</color>");
                    else if (__instance.Is(CustomRoles.CLovers) && PlayerControl.LocalPlayer.Data.IsDead) Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.CLovers)}>♥</color>");
                    if (__instance.Is(CustomRoles.DLovers) && PlayerControl.LocalPlayer.Is(CustomRoles.DLovers)) Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.DLovers)}>♥</color>");
                    else if (__instance.Is(CustomRoles.DLovers) && PlayerControl.LocalPlayer.Data.IsDead) Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.DLovers)}>♥</color>");
                    if (__instance.Is(CustomRoles.ELovers) && PlayerControl.LocalPlayer.Is(CustomRoles.ELovers)) Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.ELovers)}>♥</color>");
                    else if (__instance.Is(CustomRoles.ELovers) && PlayerControl.LocalPlayer.Data.IsDead) Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.ELovers)}>♥</color>");
                    if (__instance.Is(CustomRoles.FLovers) && PlayerControl.LocalPlayer.Is(CustomRoles.FLovers)) Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.FLovers)}>♥</color>");
                    else if (__instance.Is(CustomRoles.FLovers) && PlayerControl.LocalPlayer.Data.IsDead) Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.FLovers)}>♥</color>");
                    if (__instance.Is(CustomRoles.GLovers) && PlayerControl.LocalPlayer.Is(CustomRoles.GLovers)) Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.GLovers)}>♥</color>");
                    else if (__instance.Is(CustomRoles.GLovers) && PlayerControl.LocalPlayer.Data.IsDead) Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.GLovers)}>♥</color>");

                    if (__instance.Is(CustomRoles.MaLovers) && PlayerControl.LocalPlayer.Is(CustomRoles.MaLovers))
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.MaLovers)}>♥</color>");
                    }
                    else if (__instance.Is(CustomRoles.MaLovers) && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.MaLovers)}>♥</color>");
                    }
                    if (__instance.Is(CustomRoles.Connecting) && PlayerControl.LocalPlayer.Is(CustomRoles.Connecting))
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Connecting)}>Ψ</color>");
                    }
                    else if (__instance.Is(CustomRoles.Connecting) && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Connecting)}>Ψ</color>");
                    }

                    if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                    {
                        if (PlayerControl.LocalPlayer.PlayerId == __instance.PlayerId)
                        {
                            if (!Options.EnableGM.GetBool())
                            {
                                if (Options.TaskBattletaska.GetBool())
                                {
                                    var t1 = 0f;
                                    var t2 = 0;
                                    if (!Options.TaskBattleTeamMode.GetBool() && !Options.TaskBattleTeamWinType.GetBool())
                                    {
                                        foreach (var pc in Main.AllPlayerControls)
                                        {
                                            t1 += pc.GetPlayerTaskState().AllTasksCount;
                                            t2 += pc.GetPlayerTaskState().CompletedTasksCount;
                                        }
                                    }
                                    else
                                    {
                                        foreach (var t in Main.TaskBattleTeams)
                                        {
                                            if (!t.Contains(seer.PlayerId)) continue;
                                            t1 = Options.TaskBattleTeamWinTaskc.GetFloat();
                                            foreach (var id in t.Where(id => Utils.GetPlayerById(id).IsAlive()))
                                                t2 += Utils.GetPlayerById(id).GetPlayerTaskState().CompletedTasksCount;
                                        }
                                    }
                                    Mark.Append($"<color=yellow>({t2}/{t1})</color>");
                                }
                                if (Options.TaskBattletasko.GetBool())
                                {
                                    var to = 0;
                                    if (!Options.TaskBattleTeamMode.GetBool() && !Options.TaskBattleTeamWinType.GetBool())
                                    {
                                        foreach (var pc in Main.AllPlayerControls)
                                            if (pc.GetPlayerTaskState().CompletedTasksCount > to) to = pc.GetPlayerTaskState().CompletedTasksCount;
                                    }
                                    else
                                        foreach (var t in Main.TaskBattleTeams)
                                        {
                                            var to2 = 0;
                                            foreach (var id in t.Where(id => Utils.GetPlayerById(id).IsAlive()))
                                                to2 += Utils.GetPlayerById(id).GetPlayerTaskState().CompletedTasksCount;
                                            if (to2 > to) to = to2;
                                        }
                                    Mark.Append($"<color=#00f7ff>({to})</color>");
                                }
                            }
                        }
                        else
                        {
                            if (Options.TaskBattletaskc.GetBool())
                                Mark.Append($"<color=yellow>({target.GetPlayerTaskState().CompletedTasksCount}/{target.GetPlayerTaskState().AllTasksCount})</color>");
                        }
                    }

                    //seerに関わらず発動するLowerText
                    Suffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target));
                    //追放者
                    if (Options.CanseeVoteresult.GetBool() && MeetingVoteManager.Voteresult != "" && seer == target)
                    {
                        Suffix.Append("<color=#ffffff><size=75%>" + MeetingVoteManager.Voteresult + "</color></size>");
                    }
                    //seer役職が対象のSuffix
                    Suffix.Append(seerRole?.GetSuffix(seer, target));

                    //seerに関わらず発動するSuffix
                    Suffix.Append(CustomRoleManager.GetSuffixOthers(seer, target));

                    /*if(main.AmDebugger.Value && main.BlockKilling.TryGetValue(target.PlayerId, out var isBlocked)) {
                        Mark = isBlocked ? "(true)" : "(false)";
                    }*/
                    if (Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool())
                        RealName = $"<size=0>{RealName}</size> ";

                    string DeathReason = seer.Data.IsDead && seer.KnowDeathReason(target) ? $"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})" : "";
                    //Mark・Suffixの適用
                    target.cosmetics.nameText.text = $"{RealName}{DeathReason}{Mark}";

                    if (Suffix.ToString() != "")
                    {
                        //名前が2行になると役職テキストを上にずらす必要がある
                        RoleText.transform.SetLocalY(0.35f);
                        target.cosmetics.nameText.text += "\r\n" + Suffix.ToString();

                    }
                    else
                    {
                        //役職テキストの座標を初期値に戻す
                        RoleText.transform.SetLocalY(0.2f);
                    }
                }
                else
                {
                    //役職テキストの座標を初期値に戻す
                    RoleText.transform.SetLocalY(0.2f);
                }
            }
        }
        //FIXME: 役職クラス化のタイミングで、このメソッドは移動予定
        public static void ALoversSuicide(byte deathId = 0x7f, bool isExiled = false)
        {
            if (CustomRoles.ALovers.IsPresent() && Main.isALoversDead == false)
            {
                foreach (var loversPlayer in Main.ALoversPlayers)
                {
                    //生きていて死ぬ予定でなければスキップ
                    if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                    Main.isALoversDead = true;
                    foreach (var partnerPlayer in Main.ALoversPlayers)
                    {
                        //本人ならスキップ
                        if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;

                        //残った恋人を全て殺す(2人以上可)
                        //生きていて死ぬ予定もない場合は心中
                        if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                        {
                            PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                            if (isExiled || GameStates.IsMeeting)
                            {
                                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                                ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                            }
                            else
                                partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                        }
                    }
                }
            }
        }
        public static void BLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
        {
            if (CustomRoles.BLovers.IsPresent() && Main.isBLoversDead == false)
            {
                foreach (var loversPlayer in Main.BLoversPlayers)
                {
                    if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                    Main.isBLoversDead = true;
                    foreach (var partnerPlayer in Main.BLoversPlayers)
                    {
                        if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                        if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                        {
                            PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                            if (isExiled || GameStates.IsMeeting)
                            {
                                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                                ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                            }
                            else
                                partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                        }
                    }
                }
            }
        }
        public static void CLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
        {
            if (CustomRoles.CLovers.IsPresent() && Main.isCLoversDead == false)
            {
                foreach (var loversPlayer in Main.CLoversPlayers)
                {
                    if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                    Main.isCLoversDead = true;
                    foreach (var partnerPlayer in Main.CLoversPlayers)
                    {
                        if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                        if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                        {
                            PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                            if (isExiled || GameStates.IsMeeting)
                            {
                                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                                ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                            }
                            else
                                partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                        }
                    }
                }
            }
        }
        public static void DLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
        {
            if (CustomRoles.DLovers.IsPresent() && Main.isDLoversDead == false)
            {
                foreach (var loversPlayer in Main.DLoversPlayers)
                {
                    if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                    Main.isDLoversDead = true;
                    foreach (var partnerPlayer in Main.DLoversPlayers)
                    {
                        if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                        if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                        {
                            PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                            if (isExiled || GameStates.IsMeeting)
                            {
                                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                                ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                            }
                            else
                                partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                        }
                    }
                }
            }
        }
        public static void ELoversSuicide(byte deathId = 0x7f, bool isExiled = false)
        {
            if (CustomRoles.ELovers.IsPresent() && Main.isELoversDead == false)
            {
                foreach (var loversPlayer in Main.ELoversPlayers)
                {
                    if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                    Main.isELoversDead = true;
                    foreach (var partnerPlayer in Main.ELoversPlayers)
                    {
                        if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                        if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                        {
                            PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                            if (isExiled || GameStates.IsMeeting)
                            {
                                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                                ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                            }
                            else
                                partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                        }
                    }
                }
            }
        }
        public static void FLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
        {
            if (CustomRoles.FLovers.IsPresent() && Main.isFLoversDead == false)
            {
                foreach (var loversPlayer in Main.FLoversPlayers)
                {
                    if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                    Main.isFLoversDead = true;
                    foreach (var partnerPlayer in Main.FLoversPlayers)
                    {
                        if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                        if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                        {
                            PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                            if (isExiled || GameStates.IsMeeting)
                            {
                                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                                ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                            }
                            else
                                partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                        }
                    }
                }
            }
        }
        public static void GLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
        {
            if (CustomRoles.GLovers.IsPresent() && Main.isGLoversDead == false)
            {
                foreach (var loversPlayer in Main.GLoversPlayers)
                {
                    if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                    Main.isGLoversDead = true;
                    foreach (var partnerPlayer in Main.GLoversPlayers)
                    {
                        if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                        if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                        {
                            PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                            if (isExiled || GameStates.IsMeeting)
                            {
                                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                                ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                            }
                            else
                                partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                        }
                    }
                }
            }
        }
        public static void MadonnaLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
        {
            if (CustomRoles.Madonna.IsPresent() && Main.isMaLoversDead == false)
            {
                foreach (var MaloversPlayer in Main.MaMaLoversPlayers)
                {
                    //生きていて死ぬ予定でなければスキップ
                    if (!MaloversPlayer.Data.IsDead && MaloversPlayer.PlayerId != deathId) continue;

                    Main.isMaLoversDead = true;
                    foreach (var partnerPlayer in Main.MaMaLoversPlayers)
                    {
                        //本人ならスキップ
                        if (MaloversPlayer.PlayerId == partnerPlayer.PlayerId) continue;

                        //残った恋人を全て殺す(2人以上可)
                        //生きていて死ぬ予定もない場合は心中
                        if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                        {
                            PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                            if (isExiled || GameStates.IsMeeting)
                            {
                                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                                ReportDeadBodyPatch.Musisuruoniku[MaloversPlayer.PlayerId] = false;
                            }
                            else
                                partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                        }
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    class PlayerStartPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            var roleText = UnityEngine.Object.Instantiate(__instance.cosmetics.nameText);
            roleText.transform.SetParent(__instance.cosmetics.nameText.transform);
            roleText.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            roleText.transform.localScale = new(1f, 1f, 1f);
            roleText.fontSize = Main.RoleTextSize;
            roleText.text = "RoleText";
            roleText.gameObject.name = "RoleText";
            roleText.enabled = false;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
    class SetColorPatch
    {
        public static bool IsAntiGlitchDisabled = false;
        public static bool Prefix(PlayerControl __instance, int bodyColor)
        {
            //色変更バグ対策
            if (!AmongUsClient.Instance.AmHost || __instance.CurrentOutfit.ColorId == bodyColor || IsAntiGlitchDisabled) return true;
            if (AmongUsClient.Instance.IsGameStarted && Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                //ゲーム中に色を変えた場合
                __instance.RpcMurderPlayer(__instance, true);
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
    class CoEnterVentPatch
    {
        static bool MadBool = false;
        public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                var user = __instance.myPlayer;

                if (MadBool)
                {
                    MadBool = false;
                    return true;
                }

                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek && Options.IgnoreVent.GetBool())
                    __instance.RpcBootFromVent(id);

                if ((!user.GetRoleClass()?.OnEnterVent(__instance, id) ?? false) ||
                    (user.Data.Role.Role != RoleTypes.Engineer && //エンジニアでなく
                !user.CanUseImpostorVentButton()) || //インポスターベントも使えない
                Utils.CanVent//ベントが使えない状態
                )
                {
                    //一番遠いベントに追い出す
                    var sender = CustomRpcSender.Create("Farthest Vent")
                        .StartMessage();
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc == __instance || pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue; //本人とホストは別の処理
                        Dictionary<int, float> Distance = new();
                        Vector2 position = pc.transform.position;
                        //一番遠いベントを調べて送る
                        foreach (var vent in ShipStatus.Instance.AllVents)
                            Distance.Add(vent.Id, Vector2.Distance(position, vent.transform.position));
                        var ventid = Distance.OrderByDescending(x => x.Value).First().Key;
                        sender.AutoStartRpc(__instance.NetId, (byte)RpcCalls.BootFromVent, pc.GetClientId())
                            .Write(pc)
                            .EndRpc();
                    }
                    sender.EndMessage();
                    sender.SendMessage(); //多分負荷あれだし、テープで無理やり戻した感じだから参考にしない方がいい、

                    /*MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, -1);
                    writer.WritePacked(127);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);*/
                    _ = new LateTask(() =>
                    {
                        int clientId = user.GetClientId();
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    }, 0.5f, "Fix DesyncImpostor Stuck");
                    return false;
                }
                //マッドでベント移動できない設定なら矢印を消す
                if ((!user.GetRoleClass()?.CantVentIdo(__instance, id) ?? false) ||
                    (user.GetCustomRole().IsMadmate() && !Options.MadmateCanMovedByVent.GetBool()))
                {
                    if (!MadBool && user.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        MadBool = true;
                    int clientId = user.GetClientId();
                    _ = new LateTask(() =>
                    {
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    }, 0.1f, "Vent- BootFromVent");
                    _ = new LateTask(() =>
                    {
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.EnterVent, SendOption.Reliable, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    }, 0.25f, "Vent- EnterVent");
                    //BootFromVentで255とかしてもできるが、タイミングがよくわかんないので上ので今のとこはおｋ
                }
                CustomRoleManager.OnEnterVent(__instance, id);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetName))]
    class SetNamePatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] string name)
        {
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
    class PlayerControlCompleteTaskPatch
    {
        public static bool Prefix(PlayerControl __instance)
        {
            var pc = __instance;

            Logger.Info($"TaskComplete:{pc.GetNameWithRole()}", "CompleteTask");
            var taskState = pc.GetPlayerTaskState();
            taskState.Update(pc);

            var roleClass = pc.GetRoleClass();
            var ret = true;
            if (roleClass != null)
            {
                ret = roleClass.OnCompleteTask();
            }
            if (pc.Is(CustomRoles.TaskPlayerB) && Options.CurrentGameMode == CustomGameMode.TaskBattle && taskState.IsTaskFinished)
            {
                if (!Options.TaskBattleTeamMode.GetBool())
                {
                    foreach (var otherPlayer in Main.AllAlivePlayerControls)
                    {
                        if (otherPlayer == pc || otherPlayer.AllTasksCompleted()) continue;
                        otherPlayer.RpcExileV2();
                        var playerState = PlayerState.GetByPlayerId(otherPlayer.PlayerId);
                        playerState.SetDead();
                    }
                }
                else
                {
                    foreach (var team in Main.TaskBattleTeams)
                    {
                        if (team.Contains(pc.PlayerId)) continue;
                        team.Do(playerId =>
                        {
                            Utils.GetPlayerById(playerId).RpcExileV2();
                            var playerState = PlayerState.GetByPlayerId(playerId);
                            playerState.SetDead();
                        });
                    }
                }
            }
            //属性クラスの扱いを決定するまで仮置き
            ret &= Workhorse.OnCompleteTask(pc);
            Utils.NotifyRoles();
            return ret;
        }
        public static void Postfix()
        {
            //人外のタスクを排除して再計算
            GameData.Instance.RecomputeTaskCounts();
            Logger.Info($"TotalTaskCounts = {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}", "TaskState.Update");
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
    class PlayerControlProtectPlayerPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}", "ProtectPlayer");
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RemoveProtection))]
    class PlayerControlRemoveProtectionPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            Logger.Info($"{__instance.GetNameWithRole()}", "RemoveProtection");
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    class PlayerControlSetRolePatch
    {
        public static bool Prefix(PlayerControl __instance, ref RoleTypes roleType)
        {
            var target = __instance;
            var targetName = __instance.GetNameWithRole();
            Logger.Info($"{targetName} =>{roleType}", "PlayerControl.RpcSetRole");
            if (!ShipStatus.Instance.enabled) return true;
            if (roleType is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost)
            {
                var targetIsKiller = target.GetRoleClass() is IKiller;
                var ghostRoles = new Dictionary<PlayerControl, RoleTypes>();
                foreach (var seer in Main.AllPlayerControls)
                {
                    var self = seer.PlayerId == target.PlayerId;
                    var seerIsKiller = seer.GetRoleClass() is IKiller;

                    if ((self && targetIsKiller) || (!seerIsKiller && target.Is(CustomRoleTypes.Impostor)))
                    {
                        ghostRoles[seer] = RoleTypes.ImpostorGhost;
                    }
                    else
                    {
                        ghostRoles[seer] = RoleTypes.CrewmateGhost;
                    }
                }
                if (ghostRoles.All(kvp => kvp.Value == RoleTypes.CrewmateGhost))
                {
                    roleType = RoleTypes.CrewmateGhost;
                }
                else if (ghostRoles.All(kvp => kvp.Value == RoleTypes.ImpostorGhost))
                {
                    roleType = RoleTypes.ImpostorGhost;
                }
                else
                {
                    foreach ((var seer, var role) in ghostRoles)
                    {
                        Logger.Info($"Desync {targetName} =>{role} for{seer.GetNameWithRole()}", "PlayerControl.RpcSetRole");
                        target.RpcSetRoleDesync(role, seer.GetClientId());
                    }
                    return false;
                }
            }
            return true;
        }
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
        public static class PlayerControlDiePatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (AmongUsClient.Instance.AmHost)
                {
                    // 死者の最終位置にペットが残るバグ対応
                    __instance.RpcSetPet("");
                }
            }
        }
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MixUpOutfit))]
        public static class PlayerControlMixupOutfitPatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (!__instance.IsAlive())
                {
                    return;
                }
                // 自分がDesyncインポスターで，バニラ判定ではインポスターの場合，バニラ処理で名前が非表示にならないため，相手の名前を非表示にする
                if (
                    PlayerControl.LocalPlayer.Data.Role.IsImpostor &&  // バニラ判定でインポスター
                    !PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) &&  // Mod判定でインポスターではない
                    PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true)  // Desyncインポスター
                {
                    // 名前を隠す
                    __instance.cosmetics.ToggleNameVisible(false);
                }
            }
        }
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckSporeTrigger))]
        public static class PlayerControlCheckSporeTriggerPatch
        {
            public static bool Prefix()
            {
                if (Options.DisableFungleSporeTrigger.GetBool())
                {
                    return false;
                }
                return true;
            }
        }
    }
}