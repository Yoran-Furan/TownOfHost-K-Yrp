using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Hazel;
using UnityEngine;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.AddOns.Neutral;

namespace TownOfHost
{
    [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
    class GameEndChecker
    {
        private static GameEndPredicate predicate;
        public static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;

            //ゲーム終了判定済みなら中断
            if (predicate == null) return false;

            //ゲーム終了しないモードで廃村以外の場合は中断
            if (Options.NoGameEnd.GetBool() && CustomWinnerHolder.WinnerTeam != CustomWinner.Draw) return false;

            //廃村用に初期値を設定
            var reason = GameOverReason.ImpostorByKill;

            //ゲーム終了判定
            predicate.CheckForEndGame(out reason);

            //ゲーム終了時
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
            {
                //カモフラージュ強制解除
                Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, ForceRevert: true, RevertToDefault: true));

                switch (CustomWinnerHolder.WinnerTeam)
                {
                    case CustomWinner.Crewmate:
                        Main.AllPlayerControls
                            .Where(pc => pc.Is(CustomRoleTypes.Crewmate) && (!pc.Is(CustomRoles.ALovers) || !pc.Is(CustomRoles.BLovers) || !pc.Is(CustomRoles.CLovers) || !pc.Is(CustomRoles.DLovers) || !pc.Is(CustomRoles.ELovers) || !pc.Is(CustomRoles.FLovers) || !pc.Is(CustomRoles.GLovers) || !pc.Is(CustomRoles.MaLovers)))
                            .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                        break;
                    case CustomWinner.Impostor:
                        if (Egoist.CheckWin()) break;

                        Main.AllPlayerControls
                            .Where(pc => (pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoleTypes.Madmate)) && (!pc.Is(CustomRoles.ALovers) || !pc.Is(CustomRoles.BLovers) || !pc.Is(CustomRoles.CLovers) || !pc.Is(CustomRoles.DLovers) || !pc.Is(CustomRoles.ELovers) || !pc.Is(CustomRoles.FLovers) || !pc.Is(CustomRoles.GLovers) || !pc.Is(CustomRoles.MaLovers)))
                            .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                        break;
                }
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None)
                {
                    if (Main.ALoversPlayers.Count > 0 && Main.ALoversPlayers.ToArray().All(p => p.IsAlive()) && !reason.Equals(GameOverReason.HumansByTask))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.ALovers);
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.ALovers) && p.IsAlive())
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                    else
                    {
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.ALovers))
                            .Do(p => CustomWinnerHolder.WinnerIds.Remove(p.PlayerId));
                    }
                    if (Main.BLoversPlayers.Count > 0 && Main.BLoversPlayers.ToArray().All(p => p.IsAlive()) && !reason.Equals(GameOverReason.HumansByTask))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.BLovers);
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.BLovers) && p.IsAlive())
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                    else
                    {
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.BLovers))
                            .Do(p => CustomWinnerHolder.WinnerIds.Remove(p.PlayerId));
                    }
                    if (Main.CLoversPlayers.Count > 0 && Main.CLoversPlayers.ToArray().All(p => p.IsAlive()) && !reason.Equals(GameOverReason.HumansByTask))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CLovers);
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.CLovers) && p.IsAlive())
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                    else
                    {
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.CLovers))
                            .Do(p => CustomWinnerHolder.WinnerIds.Remove(p.PlayerId));
                    }
                    if (Main.DLoversPlayers.Count > 0 && Main.DLoversPlayers.ToArray().All(p => p.IsAlive()) && !reason.Equals(GameOverReason.HumansByTask))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.DLovers);
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.DLovers) && p.IsAlive())
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                    else
                    {
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.DLovers))
                            .Do(p => CustomWinnerHolder.WinnerIds.Remove(p.PlayerId));
                    }
                    if (Main.FLoversPlayers.Count > 0 && Main.FLoversPlayers.ToArray().All(p => p.IsAlive()) && !reason.Equals(GameOverReason.HumansByTask))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.FLovers);
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.FLovers) && p.IsAlive())
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                    else
                    {
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.FLovers))
                            .Do(p => CustomWinnerHolder.WinnerIds.Remove(p.PlayerId));
                    }
                    if (Main.ELoversPlayers.Count > 0 && Main.ELoversPlayers.ToArray().All(p => p.IsAlive()) && !reason.Equals(GameOverReason.HumansByTask))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.ELovers);
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.ELovers) && p.IsAlive())
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                    else
                    {
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.ELovers))
                            .Do(p => CustomWinnerHolder.WinnerIds.Remove(p.PlayerId));
                    }
                    if (Main.GLoversPlayers.Count > 0 && Main.GLoversPlayers.ToArray().All(p => p.IsAlive()) && !reason.Equals(GameOverReason.HumansByTask))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.GLovers);
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.GLovers) && p.IsAlive())
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                    else
                    {
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.GLovers))
                            .Do(p => CustomWinnerHolder.WinnerIds.Remove(p.PlayerId));
                    }
                    if (Main.MaMaLoversPlayers.Count > 0 && Main.MaMaLoversPlayers.ToArray().All(p => p.IsAlive()) && !reason.Equals(GameOverReason.HumansByTask))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.MaLovers);
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.MaLovers) && p.IsAlive())
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                    else
                    {
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.MaLovers))
                            .Do(p => CustomWinnerHolder.WinnerIds.Remove(p.PlayerId));
                    }
                    //追加勝利陣営
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (!pc.Is(CustomRoles.Terrorist) && pc.Is(CustomRoles.LastNeutral) && pc.IsAlive() && LastNeutral.GiveOpportunist.GetBool()
                        )
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.LastNeutral);
                        }
                        else
                        if (pc.GetRoleClass() is IAdditionalWinner additionalWinner)
                        {
                            var winnerRole = pc.GetCustomRole();
                            if (additionalWinner.CheckWin(ref winnerRole))
                            {
                                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                                CustomWinnerHolder.AdditionalWinnerRoles.Add(winnerRole);
                            }
                        }
                        /*if (God.CheckWin())
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.God);
                            Main.AllPlayerControls
                                .Where(p => p.Is(CustomRoles.God) && p.IsAlive())
                                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                        }*/
                    }
                }
                ShipStatus.Instance.enabled = false;
                StartEndGame(reason);
                predicate = null;
            }
            return false;
        }
        public static void StartEndGame(GameOverReason reason)
        {
            AmongUsClient.Instance.StartCoroutine(CoEndGame(AmongUsClient.Instance, reason).WrapToIl2Cpp());
        }
        private static IEnumerator CoEndGame(AmongUsClient self, GameOverReason reason)
        {
            // サーバー側のパケットサイズ制限によりCustomRpcSenderが利用できないため，遅延を挟むことで順番の整合性を保つ．

            // バニラ画面でのアウトロを正しくするためのゴーストロール化
            List<byte> ReviveRequiredPlayerIds = new();
            var winner = CustomWinnerHolder.WinnerTeam;
            foreach (var pc in Main.AllPlayerControls)
            {
                if (winner == CustomWinner.Draw)
                {
                    SetGhostRole(ToGhostImpostor: true);
                    continue;
                }
                bool canWin = CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) ||
                        CustomWinnerHolder.WinnerRoles.Contains(pc.GetCustomRole());
                bool isCrewmateWin = reason.Equals(GameOverReason.HumansByVote) || reason.Equals(GameOverReason.HumansByTask);
                SetGhostRole(ToGhostImpostor: canWin ^ isCrewmateWin);

                void SetGhostRole(bool ToGhostImpostor)
                {
                    if (!pc.Data.IsDead) ReviveRequiredPlayerIds.Add(pc.PlayerId);
                    if (ToGhostImpostor)
                    {
                        Logger.Info($"{pc.GetNameWithRole()}: ImpostorGhostに変更", "ResetRoleAndEndGame");
                        pc.RpcSetRole(RoleTypes.ImpostorGhost);
                    }
                    else
                    {
                        Logger.Info($"{pc.GetNameWithRole()}: CrewmateGhostに変更", "ResetRoleAndEndGame");
                        pc.RpcSetRole(RoleTypes.CrewmateGhost);
                    }
                }
            }

            // CustomWinnerHolderの情報の同期
            var winnerWriter = self.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, SendOption.Reliable);
            CustomWinnerHolder.WriteTo(winnerWriter);
            self.FinishRpcImmediately(winnerWriter);

            // 蘇生を確実にゴーストロール設定の後に届けるための遅延
            yield return new WaitForSeconds(EndGameDelay);

            if (ReviveRequiredPlayerIds.Count > 0)
            {
                // 蘇生 パケットが膨れ上がって死ぬのを防ぐため，1送信につき1人ずつ蘇生する
                for (int i = 0; i < ReviveRequiredPlayerIds.Count; i++)
                {
                    var playerId = ReviveRequiredPlayerIds[i];
                    var playerInfo = GameData.Instance.GetPlayerById(playerId);
                    // 蘇生
                    playerInfo.IsDead = false;
                    // 送信
                    GameData.Instance.SetDirtyBit(0b_1u << playerId);
                    AmongUsClient.Instance.SendAllStreamedObjects();
                }
                // ゲーム終了を確実に最後に届けるための遅延
                yield return new WaitForSeconds(EndGameDelay);
            }

            // ゲーム終了
            GameManager.Instance.RpcEndGame(reason, false);
        }
        private const float EndGameDelay = 0.2f;

        public static void SetPredicateToNormal() => predicate = new NormalGameEndPredicate();
        public static void SetPredicateToHideAndSeek() => predicate = new HideAndSeekGameEndPredicate();
        public static void SetPredicateToTaskBattle() => predicate = new TaskBattleGameEndPredicate();

        // ===== ゲーム終了条件 =====
        // 通常ゲーム用
        class NormalGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;
                if (CheckGameEndByLivingPlayers(out reason)) return true;
                if (CheckGameEndByTask(out reason)) return true;
                if (CheckGameEndBySabotage(out reason)) return true;

                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;

                int Imp = Utils.AlivePlayersCount(CountTypes.Impostor);
                int Jackal = Utils.AlivePlayersCount(CountTypes.Jackal);
                int Remotekiller = Utils.AlivePlayersCount(CountTypes.Remotekiller);
                int GrimReaper = Utils.AlivePlayersCount(CountTypes.GrimReaper);
                int Crew = Utils.AlivePlayersCount(CountTypes.Crew);

                if (Imp == 0 && Crew == 0 && Jackal == 0 && Remotekiller == 0) //全滅
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.ALovers))) //ラバーズ勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.ALovers);
                }
                else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.BLovers)))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.BLovers);
                }
                else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.CLovers)))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CLovers);
                }
                else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.DLovers)))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.DLovers);
                }
                else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.ELovers)))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.ELovers);
                }
                else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.FLovers)))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.FLovers);
                }
                else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.GLovers)))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.GLovers);
                }
                else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.MaLovers))) //マドンナ勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.MaLovers);
                }
                else if (Imp == 1 && Crew == 0 && GrimReaper == 1)//死神勝利(1)
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.GrimReaper);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.GrimReaper);
                }
                else if (Jackal == 0 && Remotekiller == 0 && Crew <= Imp) //インポスター勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }
                else if (Imp == 0 && Remotekiller == 0 && Crew <= Jackal) //ジャッカル勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalMafia);
                }
                else if (Imp == 0 && Jackal == 0 && Crew <= Remotekiller)
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Remotekiller);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Remotekiller);
                }
                else if (Jackal == 0 && Imp == 0 && GrimReaper == 1 && Remotekiller == 0)//死神勝利(2)
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.GrimReaper);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.GrimReaper);
                }
                else if (Jackal == 0 && Remotekiller == 0 && Imp == 0) //クルー勝利
                {
                    reason = GameOverReason.HumansByVote;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                }
                else return false; //勝利条件未達成

                return true;
            }
        }

        // HideAndSeek用
        class HideAndSeekGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;

                if (CheckGameEndByLivingPlayers(out reason)) return true;
                if (CheckGameEndByTask(out reason)) return true;

                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;

                int Imp = Utils.AlivePlayersCount(CountTypes.Impostor);
                int Crew = Utils.AlivePlayersCount(CountTypes.Crew);

                if (Imp == 0 && Crew == 0) //全滅
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (Crew <= 0) //インポスター勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }
                else if (Imp == 0) //クルー勝利(インポスター切断など)
                {
                    reason = GameOverReason.HumansByVote;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                }
                else return false; //勝利条件未達成

                return true;
            }
        }
    }
    // ﾀｽﾊﾞﾄ用
    class TaskBattleGameEndPredicate : GameEndPredicate
    {
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;

            if (CheckGameEndByLivingPlayers(out reason)) return true;

            return false;
        }

        public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;

            if (Main.RTAMode)
            {
                if (PlayerControl.LocalPlayer.GetPlayerTaskState().IsTaskFinished)
                {
                    reason = GameOverReason.HumansByTask;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.TaskPlayerB);
                    CustomWinnerHolder.WinnerIds.Add(PlayerControl.LocalPlayer.PlayerId);
                }
            }
            else
            if (!Options.TaskBattleTeamWinType.GetBool())
            {
                int TaskPlayerB = Utils.AlivePlayersCount(CountTypes.TaskPlayer);
                bool win = TaskPlayerB <= 1;
                if (Options.TaskBattleTeamMode.GetBool())
                {
                    foreach (var pc in Main.AllPlayerControls)
                        if (pc.AllTasksCompleted())
                            win = true;
                }
                if (win)
                {
                    reason = GameOverReason.HumansByTask;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.TaskPlayerB);
                    foreach (var pc in Main.AllAlivePlayerControls)
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                }
                else return false; //勝利条件未達成
            }
            else
            {
                foreach (var t in Main.TaskBattleTeams)
                {
                    var task = 0;
                    foreach (var id in t)
                        task += Utils.GetPlayerById(id).GetPlayerTaskState().CompletedTasksCount;
                    if (Options.TaskBattleTeamWinTaskc.GetFloat() <= task)
                    {
                        reason = GameOverReason.HumansByTask;
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.TaskPlayerB);
                        foreach (var id in t)
                            CustomWinnerHolder.WinnerIds.Add(id);
                    }
                }
            }

            return true;
        }
    }
    public abstract class GameEndPredicate
    {
        /// <summary>ゲームの終了条件をチェックし、CustomWinnerHolderに値を格納します。</summary>
        /// <params name="reason">バニラのゲーム終了処理に使用するGameOverReason</params>
        /// <returns>ゲーム終了の条件を満たしているかどうか</returns>
        public abstract bool CheckForEndGame(out GameOverReason reason);

        /// <summary>GameData.TotalTasksとCompletedTasksをもとにタスク勝利が可能かを判定します。</summary>
        public virtual bool CheckGameEndByTask(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (Options.DisableTaskWin.GetBool() || TaskState.InitialTotalTasks == 0) return false;

            if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
            {
                reason = GameOverReason.HumansByTask;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                return true;
            }
            return false;
        }
        /// <summary>ShipStatus.Systems内の要素をもとにサボタージュ勝利が可能かを判定します。</summary>
        public virtual bool CheckGameEndBySabotage(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (ShipStatus.Instance.Systems == null) return false;

            // TryGetValueは使用不可
            var systems = ShipStatus.Instance.Systems;
            LifeSuppSystemType LifeSupp;
            if (systems.ContainsKey(SystemTypes.LifeSupp) && // サボタージュ存在確認
                (LifeSupp = systems[SystemTypes.LifeSupp].TryCast<LifeSuppSystemType>()) != null && // キャスト可能確認
                LifeSupp.Countdown < 0f) // タイムアップ確認
            {
                // 酸素サボタージュ
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                reason = GameOverReason.ImpostorBySabotage;
                LifeSupp.Countdown = 10000f;
                return true;
            }

            ISystemType sys = null;
            if (systems.ContainsKey(SystemTypes.Reactor)) sys = systems[SystemTypes.Reactor];
            else if (systems.ContainsKey(SystemTypes.Laboratory)) sys = systems[SystemTypes.Laboratory];
            else if (systems.ContainsKey(SystemTypes.HeliSabotage)) sys = systems[SystemTypes.HeliSabotage];
            ICriticalSabotage critical;
            if (sys != null && // サボタージュ存在確認
                (critical = sys.TryCast<ICriticalSabotage>()) != null && // キャスト可能確認
                critical.Countdown < 0f) // タイムアップ確認
            {
                // リアクターサボタージュ
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                reason = GameOverReason.ImpostorBySabotage;
                critical.ClearSabotage();
                return true;
            }

            return false;
        }
    }
}