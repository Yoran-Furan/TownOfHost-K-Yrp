using System.Collections.Generic;
using System.Linq;
using System.Text;

using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.AddOns.Common;

namespace TownOfHost;

[HarmonyPatch]
public static class MeetingHudPatch
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class CheckForEndVotingPatch
    {
        public static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            MeetingVoteManager.Instance?.CheckAndEndMeeting();
            return false;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
    public static class CastVotePatch
    {
        public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] byte srcPlayerId /* 投票した人 */ , [HarmonyArgument(1)] byte suspectPlayerId /* 投票された人 */ )
        {
            var voter = Utils.GetPlayerById(srcPlayerId);
            var votefor = Utils.GetPlayerById(suspectPlayerId);
            foreach (var pc in Main.AllPlayerControls)
                if (pc.GetRoleClass()?.CheckVoteAsVoter(suspectPlayerId, voter) == false || (!votefor.IsAlive() && suspectPlayerId != 253 && suspectPlayerId != 254))
                {
                    __instance.RpcClearVote(voter.GetClientId());
                    Logger.Info($"{voter.GetNameWithRole()} は投票しない！ => {srcPlayerId}", nameof(CastVotePatch));
                    return false;
                }
                else
                if (voter.Is(CustomRoles.Elector) && suspectPlayerId == 253)
                {
                    Utils.SendMessage("君はイレクターなんだよ。\nスキップできない属性でね。\n誰かに投票してね。", voter.PlayerId);
                    __instance.RpcClearVote(voter.GetClientId());
                    Logger.Info($"{voter.GetNameWithRole()} イレクター発動 => {srcPlayerId}", nameof(CastVotePatch));
                    return false;
                }

            MeetingVoteManager.Instance?.SetVote(srcPlayerId, suspectPlayerId);
            return true;
        }
    }
    public static string Oniku = "";
    public static string Send = "";
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartPatch
    {
        public static void Prefix()
        {
            Logger.Info("------------会議開始------------", "Phase");
            ChatUpdatePatch.DoBlockChat = true;
            GameStates.AlreadyDied |= !Utils.IsAllAlive;
            Main.AllPlayerControls.Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
            MeetingStates.MeetingCalled = true;
        }
        public static void Postfix(MeetingHud __instance)
        {
            MeetingVoteManager.Start();

            SoundManager.Instance.ChangeAmbienceVolume(0f);
            if (!GameStates.IsModHost) return;
            var myRole = PlayerControl.LocalPlayer.GetRoleClass();
            foreach (var pva in __instance.playerStates)
            {
                var pc = Utils.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;
                var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.NameText.transform);
                roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                roleTextMeeting.fontSize = 1.5f;
                (roleTextMeeting.enabled, roleTextMeeting.text)
                    = Utils.GetRoleNameAndProgressTextData(PlayerControl.LocalPlayer, pc);
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enableWordWrapping = false;

                // 役職とサフィックスを同時に表示する必要が出たら要改修
                var suffixBuilder = new StringBuilder(32);
                if (myRole != null)
                {
                    suffixBuilder.Append(myRole.GetSuffix(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                }
                suffixBuilder.Append(CustomRoleManager.GetSuffixOthers(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                if (suffixBuilder.Length > 0)
                {
                    roleTextMeeting.text = suffixBuilder.ToString();
                    roleTextMeeting.enabled = true;
                }
                if (Options.ShowRoleAtFirstMeeting.GetBool() && MeetingStates.FirstMeeting) Utils.SendRoleInfo(pc);
            }
            CustomRoleManager.AllActiveRoles.Values.Do(role => role.OnStartMeeting());
            Send = "";

            Send += "<size=110%><b>" + string.Format(GetString("Message.Day"), Main.day).Color(Palette.Orange) + "</b></size>\n";
            foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
            {
                var RoleText = roleClass.MeetingMeg();
                if (RoleText != "") Send += RoleText + "\n\n";
            }
            if (Oniku != "")
            {
                Send += "<color=#001e43><size=80%>※" + Oniku + "</size></color>\n";

            }
            if (Options.SyncButtonMode.GetBool())
            {
                Send += "<size=80%><color=#006e54>★" + string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "</size></color>\n";
                Logger.Info("緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "回使用可能です。", "SyncButtonMode");
            }
            if (AntiBlackout.OverrideExiledPlayer)
            {
                Send += "<color=#640125><size=80%>！" + GetString("Warning.OverrideExiledPlayer") + "</size></color>\n";
            }
            if (MeetingVoteManager.Voteresult != "")
            {
                if (Send != "") Send += "\n";
                Send += "<size=120%>【" + GetString("LastMeetingre") + "】\n</size>" + MeetingVoteManager.Voteresult;
            }
            TemplateManager.SendTemplate("OnMeeting", noErr: true);
            if (MeetingStates.FirstMeeting) TemplateManager.SendTemplate("OnFirstMeeting", noErr: true);
            if (Send != "") Utils.SendMessage(Send);
            MeetingVoteManager.Voteresult = "";
            Oniku = "";
            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    foreach (var seen in Main.AllPlayerControls)
                    {
                        var seenName = seen.GetRealName(isMeeting: true);
                        var coloredName = Utils.ColorString(seen.GetRoleColor(), seenName);
                        foreach (var seer in Main.AllPlayerControls)
                        {
                            seen.RpcSetNamePrivate(
                                seer == seen ? coloredName : seenName,
                                true,
                                seer);
                        }
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                }, 3f, "SetName To Chat");
            }

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                var seer = PlayerControl.LocalPlayer;
                var seerRole = seer.GetRoleClass();

                var target = Utils.GetPlayerById(pva.TargetPlayerId);
                if (target == null) continue;

                var sb = new StringBuilder();

                //会議画面での名前変更
                //自分自身の名前の色を変更
                //NameColorManager準拠の処理
                pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);

                //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。

                if (seer.KnowDeathReason(target))
                    sb.Append($"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})");

                sb.Append(seerRole?.GetMark(seer, target, true));
                sb.Append(CustomRoleManager.GetMarkOthers(seer, target, true));

                //相手のサブロール処理
                foreach (var subRole in target.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.ALovers:
                            if (seer.Is(CustomRoles.ALovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ALovers), "♥"));
                            break;
                        case CustomRoles.BLovers:
                            if (seer.Is(CustomRoles.BLovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.BLovers), "♥"));
                            break;
                        case CustomRoles.CLovers:
                            if (seer.Is(CustomRoles.CLovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.CLovers), "♥"));
                            break;
                        case CustomRoles.DLovers:
                            if (seer.Is(CustomRoles.DLovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DLovers), "♥"));
                            break;
                        case CustomRoles.ELovers:
                            if (seer.Is(CustomRoles.ELovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ELovers), "♥"));
                            break;
                        case CustomRoles.FLovers:
                            if (seer.Is(CustomRoles.FLovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.FLovers), "♥"));
                            break;
                        case CustomRoles.GLovers:
                            if (seer.Is(CustomRoles.GLovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.GLovers), "♥"));
                            break;
                        case CustomRoles.MaLovers:
                            if (seer.Is(CustomRoles.MaLovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.MaLovers), "♥"));
                            break;
                        case CustomRoles.Connecting:
                            if (seer.Is(CustomRoles.Connecting) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Connecting), "Ψ"));
                            break;
                    }
                }
                //本人のsubrole処理
                foreach (var subRole in seer.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.Guesser:
                            if (!seer.Is(CustomRoles.Guesser)) break;
                            if (!seer.Data.IsDead && target == seer)
                                pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Guesser), "<line-height=100%><size=50%>∮ゲッサー能力発動:/bt <color=#ffff00>ID</color> 役職名</color></size>\n") + pva.NameText.text + "<size=30%>\n </size>";
                            if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                                pva.NameText.text = Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " " + pva.NameText.text;
                            break;
                        case CustomRoles.LastImpostor:
                            if (!LastImpostor.GiveGuesser.GetBool()) break;
                            if (!seer.Data.IsDead && target == seer)
                                pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Guesser), "<line-height=100%><size=50%>∮ゲッサー能力発動:/bt <color=#ffff00>ID</color> 役職名</color></size>\n") + pva.NameText.text + "<size=30%>\n </size>";
                            if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                                pva.NameText.text = Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " " + pva.NameText.text;
                            break;
                        case CustomRoles.LastNeutral:
                            if (!LastNeutral.GiveGuesser.GetBool()) break;
                            if (!seer.Data.IsDead && target == seer)
                                pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Guesser), "<line-height=100%><size=50%>∮ゲッサー能力発動:/bt <color=#ffff00>ID</color> 役職名</color></size>\n") + pva.NameText.text + "<size=30%>\n </size>";
                            if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                                pva.NameText.text = Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " " + pva.NameText.text;
                            break;
                    }
                }
                //会議画面ではインポスター自身の名前にSnitchマークはつけません。

                pva.NameText.text += sb.ToString();
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class UpdatePatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
            {
                __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
                {
                    var player = Utils.GetPlayerById(x.TargetPlayerId);
                    player.RpcExileV2();
                    var state = PlayerState.GetByPlayerId(player.PlayerId);
                    state.DeathReason = CustomDeathReason.Execution;
                    state.SetDead();
                    Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                    Logger.Info($"{player.GetNameWithRole()}を処刑しました", "Execution");
                    __instance.CheckForEndVoting();
                });
            }
            if (Balancer.Id != 255)
            {
                if (!Utils.GetPlayerById(Balancer.target1).IsAlive()
                    || !Utils.GetPlayerById(Balancer.target2).IsAlive())
                    MeetingVoteManager.Instance.EndMeeting(false);
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class OnDestroyPatch
    {
        public static void Postfix()
        {
            MeetingStates.FirstMeeting = false;
            Logger.Info("------------会議終了------------", "Phase");
            if (AmongUsClient.Instance.AmHost)
            {
                AntiBlackout.SetIsDead();
                foreach (var p in SelfVoteManager.CheckVote)
                    SelfVoteManager.CheckVote[p.Key] = false;
                foreach (var pc in Main.AllPlayerControls)
                    (pc.GetRoleClass() as IUseTheShButton)?.ResetS(pc);
            }
            // MeetingVoteManagerを通さずに会議が終了した場合の後処理
            MeetingVoteManager.Instance?.Destroy();
        }
    }

    public static void TryAddAfterMeetingDeathPlayers(CustomDeathReason deathReason, params byte[] playerIds)
    {
        var AddedIdList = new List<byte>();
        foreach (var playerId in playerIds)
            if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
            {
                ReportDeadBodyPatch.Musisuruoniku[playerId] = false;
                AddedIdList.Add(playerId);
                if (deathReason == CustomDeathReason.Revenge && Options.VRcanseemitidure.GetBool())
                    MeetingVoteManager.Voteresult += "\n<size=60%>" + Utils.GetPlayerColor(Utils.GetPlayerById(playerId)) + GetString("votemi");
            }

        CheckForDeathOnExile(deathReason, AddedIdList.ToArray());
    }
    public static void CheckForDeathOnExile(CustomDeathReason deathReason, params byte[] playerIds)
    {
        foreach (var playerId in playerIds)
        {
            //Loversの後追い
            if (CustomRoles.ALovers.IsPresent() && !Main.isALoversDead && Main.ALoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.ALoversSuicide(playerId, true);
            if (CustomRoles.BLovers.IsPresent() && !Main.isBLoversDead && Main.BLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.BLoversSuicide(playerId, true);
            if (CustomRoles.CLovers.IsPresent() && !Main.isCLoversDead && Main.CLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.CLoversSuicide(playerId, true);
            if (CustomRoles.DLovers.IsPresent() && !Main.isDLoversDead && Main.DLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.DLoversSuicide(playerId, true);
            if (CustomRoles.ELovers.IsPresent() && !Main.isELoversDead && Main.ELoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.ELoversSuicide(playerId, true);
            if (CustomRoles.FLovers.IsPresent() && !Main.isFLoversDead && Main.FLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.FLoversSuicide(playerId, true);
            if (CustomRoles.GLovers.IsPresent() && !Main.isGLoversDead && Main.GLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.GLoversSuicide(playerId, true);
            //MaL
            if (CustomRoles.MaLovers.IsPresent() && !Main.isMaLoversDead && Main.MaMaLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.MadonnaLoversSuicide(playerId, true);
            //道連れチェック
            RevengeOnExile(playerId, deathReason);
        }
    }
    private static void RevengeOnExile(byte playerId, CustomDeathReason deathReason)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return;
        var target = PickRevengeTarget(player, deathReason);
        if (target == null) return;
        TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, playerId);
        TryAddAfterMeetingDeathPlayers(CustomDeathReason.Revenge, target.PlayerId);
        target.SetRealKiller(player);
        Logger.Info($"{player.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "RevengeOnExile");
    }
    private static PlayerControl PickRevengeTarget(PlayerControl exiledplayer, CustomDeathReason deathReason)//道連れ先選定
    {
        List<PlayerControl> TargetList = new();
        if (exiledplayer.GetRoleClass() is INekomata nekomata)
        {
            // 道連れしない状態ならnull
            if (!nekomata.DoRevenge(deathReason))
            {
                return null;
            }
            TargetList = Main.AllAlivePlayerControls.Where(candidate => candidate != exiledplayer && !Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId) && nekomata.IsCandidate(candidate)).ToList();
        }
        else
        {
            var isMadmate =
                exiledplayer.Is(CustomRoleTypes.Madmate) ||
                // マッド属性化時に削除
                (exiledplayer.GetRoleClass() is SchrodingerCat schrodingerCat && schrodingerCat.AmMadmate);
            foreach (var candidate in Main.AllAlivePlayerControls)
            {
                if (candidate == exiledplayer || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;
                switch (exiledplayer.GetCustomRole())
                {
                    // ここにINekomata未適用の道連れ役職を追加
                    default:
                        if (isMadmate && deathReason == CustomDeathReason.Vote && Options.MadmateRevengeCrewmate.GetBool())
                        {
                            if ((candidate.Is(CustomRoleTypes.Impostor) && Options.MadNekomataCanImp.GetBool()) ||
                            (candidate.Is(CustomRoleTypes.Neutral) && Options.MadNekomataCanNeu.GetBool()) ||
                            (candidate.Is(CustomRoleTypes.Crewmate) && Options.MadNekomataCanCrew.GetBool()) ||
                            (candidate.Is(CustomRoleTypes.Madmate) && Options.MadNekomataCanMad.GetBool()))
                                TargetList.Add(candidate);
                        }
                        else
                            foreach (var subRole in exiledplayer.GetCustomSubRoles())
                            {
                                switch (subRole)
                                {
                                    case CustomRoles.Bakeneko:
                                        if (exiledplayer.Is(CustomRoles.Bakeneko) && deathReason == CustomDeathReason.Vote)
                                        {
                                            if (
                                            (candidate.Is(CustomRoleTypes.Impostor) && Bakeneko.Imp.GetBool()) ||
                                            (candidate.Is(CustomRoleTypes.Neutral) && Bakeneko.Neu.GetBool()) ||
                                            (candidate.Is(CustomRoleTypes.Crewmate) && Bakeneko.Crew.GetBool()) ||
                                            (candidate.Is(CustomRoleTypes.Madmate) && Bakeneko.Mad.GetBool()))
                                                TargetList.Add(candidate);
                                        }
                                        break;
                                }
                            }
                        break;
                }
            }
        }
        if (TargetList == null || TargetList.Count == 0) return null;
        var rand = IRandom.Instance;
        var target = TargetList[rand.Next(TargetList.Count)];
        return target;
    }
}

[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetHighlighted))]
class SetHighlightedPatch
{
    public static bool Prefix(PlayerVoteArea __instance, bool value)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!__instance.HighlightedFX) return false;
        __instance.HighlightedFX.enabled = value;
        return false;
    }
}
