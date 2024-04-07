using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
    class ShipFixedUpdatePatch
    {
        public static void Postfix(ShipStatus __instance)
        {
            //ここより上、全員が実行する
            if (!AmongUsClient.Instance.AmHost) return;
            //ここより下、ホストのみが実行する
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Main.introDestroyed)
            {
                if (Options.HideAndSeekKillDelayTimer > 0)
                {
                    Options.HideAndSeekKillDelayTimer -= Time.fixedDeltaTime;
                }
                else if (!float.IsNaN(Options.HideAndSeekKillDelayTimer))
                {
                    Utils.MarkEveryoneDirtySettings();
                    Options.HideAndSeekKillDelayTimer = float.NaN;
                    Logger.Info("キル能力解禁", "HideAndSeek");
                }
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(byte))]
    class ShipStatusUpdateSystemPatch
    {
        public static void Prefix(ShipStatus __instance,
            [HarmonyArgument(0)] SystemTypes systemType,
            [HarmonyArgument(1)] PlayerControl player,
            [HarmonyArgument(2)] byte amount)
        {
            if (systemType != SystemTypes.Sabotage)
            {
                Logger.Info("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole() + ", amount: " + amount, "UpdateSystem");
            }
            if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            {
                Logger.SendInGame("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole() + ", amount: " + amount);
            }
        }
        public static void CheckAndOpenDoorsRange(ShipStatus __instance, int amount, int min, int max)
        {
            var Ids = new List<int>();
            for (var i = min; i <= max; i++)
            {
                Ids.Add(i);
            }
            CheckAndOpenDoors(__instance, amount, Ids.ToArray());
        }
        private static void CheckAndOpenDoors(ShipStatus __instance, int amount, params int[] DoorIds)
        {
            if (DoorIds.Contains(amount)) foreach (var id in DoorIds)
                {
                    __instance.RpcUpdateSystem(SystemTypes.Doors, (byte)id);
                }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
    class CloseDoorsPatch
    {
        public static bool Prefix(ShipStatus __instance)
        {
            return !(Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) || Options.AllowCloseDoors.GetBool();
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    class StartPatch
    {
        public static void Postfix()
        {
            Logger.CurrentMethod();
            Logger.Info("-----------ゲーム開始-----------", "Phase");
            if (GameStates.IsFreePlay && Main.EditMode)
            {
                Main.CustomSpawnPosition.TryAdd(AmongUsClient.Instance.TutorialMapId, new List<Vector2>());
                _ = new LateTask(() =>
                {
                    PlayerControl.LocalPlayer.SetRole(AmongUs.GameOptions.RoleTypes.Shapeshifter);
                    if (PlayerControl.AllPlayerControls.Count < 10)
                    {
                        //SNR参考 https://github.com/SuperNewRoles/SuperNewRoles/blob/master/SuperNewRoles/Modules/BotManager.cs
                        byte id = 0;
                        foreach (var p in PlayerControl.AllPlayerControls)
                            id++;
                        for (var i = 0; PlayerControl.AllPlayerControls.Count < 10; i++)
                        {
                            var dummy = GameObject.Instantiate(AmongUsClient.Instance.PlayerPrefab);
                            dummy.isDummy = true;
                            dummy.PlayerId = id;
                            GameData.Instance.AddPlayer(dummy);
                            AmongUsClient.Instance.Spawn(dummy);
                            dummy.NetTransform.enabled = true;
                            dummy.SetColor(8);
                            id++;
                        }
                    }
                    //Mark
                    var mark = Utils.LoadSprite("TownOfHost.Resources.SpawnMark.png", 300f);
                    foreach (var p in PlayerControl.AllPlayerControls.ToArray().Where(p => p.PlayerId > 1))
                    {
                        _ = new LateTask(() =>
                        {
                            var nametext = p.transform.Find("Names/NameText_TMP");
                            nametext.transform.position -= new Vector3(0, nametext.gameObject.activeSelf ? 0.3f : -0.3f);
                            nametext.gameObject.SetActive(true);
                        }, 0.5f);
                        GameObject.Destroy(p.transform.Find("Names/ColorblindName_TMP").gameObject);
                        p.transform.Find("BodyForms").gameObject.active = false;
                        var hand = p.transform.Find("BodyForms/Seeker/SeekerHand");
                        var Mark = GameObject.Instantiate(hand, hand.transform.parent.parent.parent);
                        Mark.transform.localPosition = new Vector2(0, 0);
                        Mark.GetComponent<SpriteRenderer>().sprite = mark;
                        Component.Destroy(Mark.GetComponent<PowerTools.SpriteAnimNodeSync>());
                        Mark.name = "Mark";
                        Mark.gameObject.SetActive(true);
                    }
                }, 0.2f);
                return;
            }
            if (GameStates.IsModHost && Main.UseWebHook.Value) Utils.WH_ShowActiveRoles();
            Utils.CountAlivePlayers(true);
            Main.RTAMode = Options.CurrentGameMode == CustomGameMode.TaskBattle && Main.AllPlayerControls.Count() is 1;
            _ = new LateTask(() =>
            {
                HudManagerPatch.TaskBattlep = PlayerControl.LocalPlayer.transform.position;
                HudManagerPatch.TaskBattleTimer = 0f;
            }, 1f, "TaskBattle TimerReset");
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.StartMeeting))]
    class StartMeetingPatch
    {
        public static void Prefix(ShipStatus __instance, PlayerControl reporter, GameData.PlayerInfo target)
        {
            MeetingStates.ReportTarget = target;
            MeetingStates.DeadBodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
        }
        public static void Postfix()
        {
            // 全プレイヤーを湧いてない状態にする
            foreach (var state in PlayerState.AllPlayerStates.Values)
            {
                state.HasSpawned = false;
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    class BeginPatch
    {
        public static void Postfix()
        {
            Logger.CurrentMethod();

            //ホストの役職初期設定はここで行うべき？
        }
    }
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
    class CheckTaskCompletionPatch
    {
        public static bool Prefix(ref bool __result)
        {
            if (Options.DisableTaskWin.GetBool() || Options.NoGameEnd.GetBool() || TaskState.InitialTotalTasks == 0)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}