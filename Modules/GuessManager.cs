using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using TownOfHost.Roles.Core;
using TownOfHost.Attributes;
using TownOfHost.Roles.AddOns.Common;
using UnityEngine;
using static TownOfHost.Translator;
using TownOfHost.Modules.ChatManager;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.Madmate;

namespace TownOfHost;

//参考→https://github.com/0xDrMoe/TownofHost-Enhanced/releases/tag/v1.0.1
//上手くいかない部分はこっちも参考にしました→https://github.com/AsumuAkaguma/TownOfHost_ForE/releases/tag/vBOMB!
public static class GuessManager
{

    public static Dictionary<byte, int> GuesserGuessed = new();
    public static Dictionary<byte, int> TGuess = new();

    [GameModuleInitializer]
    public static void Guessreset()
    {
        Logger.Info("全員のゲスをリセットするぜ!!", "Guesser");
        GuesserGuessed.Clear();
        TGuess.Clear();//ゲスでターン終了したらリセットされないので
    }
    public static void Reset()
    {
        Logger.Info("会議制限を解除するぜ", "Guesser");
        TGuess.Clear();
    }

    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Length; i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }

    private static bool ComfirmIncludeMsg(string msg, string key)
    {
        var keys = key.Split('|');
        for (int i = 0; i < keys.Length; i++)
        {
            if (msg.Contains(keys[i])) return true;
        }
        return false;
    }

    public static bool GuesserMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;

        int operate = 0;
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "bt", false)) operate = 2;
        else return false;

        if (!pc.IsAlive()) return true;//本当は悪あがきはやめなって処理入れたかった(´・ω・｀)
        if (operate == 2)
        {
            //ゲッサー能力なくても生きててかつどれかの設定でHidemeg有効なってたらメッセージを隠す。
            if ((pc.IsAlive() && (Guesser.TryHideMsg.GetBool() || LastImpostor.TryHideMsg.GetBool())) || LastNeutral.TryHideMsg.GetBool())
                ChatManager.SendPreviousMessagesToAll();

            //Notゲッサーはここで処理を止める。
            if (!pc.Is(CustomRoles.Guesser)
                && !(pc.Is(CustomRoles.LastImpostor) && LastImpostor.GiveGuesser.GetBool())
                && !(pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool()))
            {
                Utils.SendMessage(GetString("NotGuesserError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("NotGuesserErrortitle")));
                return true;
            }
            if (!MsgToPlayerAndRole(msg, out byte targetId, out CustomRoles role, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId, "<color=#e6b422>" + GetString("GuessErrortitle") + "</color>");
                return true;
            }
            if (MadAvenger.Skill)//マッドアベンジャー中は処理しないぜ★
            {
                return true;
            }
            var target = Utils.GetPlayerById(targetId);
            if (target != null)
            {
                bool guesserSuicide = false;
                if (!GuesserGuessed.ContainsKey(pc.PlayerId)) GuesserGuessed.Add(pc.PlayerId, 0);
                if (!TGuess.ContainsKey(pc.PlayerId)) TGuess.Add(pc.PlayerId, 0);

                //弾補充処理
                if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.AddTama.GetBool() && pc.Is(CustomRoles.Guesser) && GuesserGuessed[pc.PlayerId] >= LastImpostor.CanGuessTime.GetInt() + Guesser.CanGuessTime.GetInt())
                {
                    Utils.SendMessage(GetString("GuessercountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuessercountErrorT")));
                    return true;
                }
                else
                //弾補充なし。
                if (pc.Is(CustomRoles.LastImpostor) && (!LastImpostor.AddTama.GetBool() || !pc.Is(CustomRoles.Guesser)) && GuesserGuessed[pc.PlayerId] >= LastImpostor.CanGuessTime.GetInt())
                {
                    Utils.SendMessage(GetString("GuessercountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuessercountErrorT")));
                    return true;
                }
                else
                if (pc.Is(CustomRoles.LastImpostor) && TGuess[pc.PlayerId] >= LastImpostor.OwnCanGuessTime.GetInt())
                {
                    Utils.SendMessage(GetString("GuesserMTGcountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuesserMTGcountErrorT")));
                    return true;
                }
                //弾補充処理
                if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.AddTama.GetBool() && pc.Is(CustomRoles.Guesser) && GuesserGuessed[pc.PlayerId] >= LastImpostor.CanGuessTime.GetInt() + Guesser.CanGuessTime.GetInt())
                {
                    Utils.SendMessage(GetString("GuessercountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuessercountErrorT")));
                    return true;
                }
                else
                //弾補充なし。
                if (pc.Is(CustomRoles.LastImpostor) && (!LastImpostor.AddTama.GetBool() || !pc.Is(CustomRoles.Guesser)) && GuesserGuessed[pc.PlayerId] >= LastImpostor.CanGuessTime.GetInt())
                {
                    Utils.SendMessage(GetString("GuessercountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuessercountErrorT")));
                    return true;
                }
                else
                if (pc.Is(CustomRoles.LastImpostor) && TGuess[pc.PlayerId] >= LastImpostor.OwnCanGuessTime.GetInt())
                {
                    Utils.SendMessage(GetString("GuesserMTGcountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuesserMTGcountErrorT"))); return true;
                }
                else//ラスポスで弾補充/ラスニュで弾補充されたなら上で処理。
                if (!(pc.Is(CustomRoles.LastImpostor) && LastImpostor.AddTama.GetBool()) && !(pc.Is(CustomRoles.LastNeutral) && LastNeutral.AddTama.GetBool()) && pc.Is(CustomRoles.Guesser) && GuesserGuessed[pc.PlayerId] >= Guesser.CanGuessTime.GetInt())
                {
                    Utils.SendMessage(GetString("GuessercountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuessercountErrorT")));
                    return true;
                }
                else//ラスポス,ラスニュならうえで処理した奴適応させるからここでは処理しない。
                if (!(pc.Is(CustomRoles.LastImpostor) && LastImpostor.GiveGuesser.GetBool()) && !(pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool()) && pc.Is(CustomRoles.Guesser) && TGuess[pc.PlayerId] >= Guesser.OwnCanGuessTime.GetInt())
                {
                    Utils.SendMessage(GetString("GuesserMTGcountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuesserMTGcountErrorT"))); return true;
                }

                if (role == CustomRoles.GM || target.Is(CustomRoles.GM)) return true;
                //スニッチ
                if (pc.Is(CustomRoles.Guesser) && target.Is(CustomRoles.Snitch) && target.AllTasksCompleted() && !Guesser.ICanGuessTaskDoneSnitch.GetBool() && pc.Is(CustomRoleTypes.Impostor))
                {
                    Utils.SendMessage(string.Format(GetString("GuessSnitch"), GetString("Impostor")), pc.PlayerId, Utils.ColorString(Palette.ImpostorRed, "【===ﾁｮｯﾄﾏﾃﾁｮﾄﾏﾃｸﾙｰｻﾝ!!===】"));
                    return true;
                }
                if (pc.Is(CustomRoles.LastImpostor) && target.Is(CustomRoles.Snitch) && target.AllTasksCompleted() && !LastImpostor.ICanGuessTaskDoneSnitch.GetBool() && pc.Is(CustomRoleTypes.Impostor))
                {
                    Utils.SendMessage(string.Format(GetString("GuessSnitch"), GetString("Impostor")), pc.PlayerId, Utils.ColorString(Palette.ImpostorRed, "【===ﾁｮｯﾄﾏﾃﾁｮﾄﾏﾃｸﾙｰｻﾝ!!===】"));
                    return true;
                }
                if (target.Is(CustomRoles.Snitch) && target.AllTasksCompleted() && !Guesser.MCanGuessTaskDoneSnitch.GetBool() && pc.Is(CustomRoleTypes.Madmate))
                {
                    Utils.SendMessage(string.Format(GetString("GuessSnitch"), GetString("Madmate")), pc.PlayerId, Utils.ColorString(Palette.ImpostorRed, "【===ﾁｮｯﾄﾏﾃﾁｮﾄﾏﾃｸﾙｰｻﾝ!!===】"));
                    return true;
                }
                if (pc.Is(CustomRoles.Guesser) && target.Is(CustomRoles.Snitch) && target.AllTasksCompleted() && !Guesser.NCanGuessTaskDoneSnitch.GetBool() && pc.Is(CustomRoleTypes.Neutral))
                {
                    Utils.SendMessage(string.Format(GetString("GuessSnitch"), GetString("Neutral")), pc.PlayerId, Utils.ColorString(Palette.DisabledGrey, "【===ﾁｮｯﾄﾏﾃﾁｮﾄﾏﾃｸﾙｰｻﾝ!!===】"));
                    return true;
                }
                if (pc.Is(CustomRoles.LastNeutral) && target.Is(CustomRoles.Snitch) && target.AllTasksCompleted() && !LastNeutral.ICanGuessTaskDoneSnitch.GetBool() && pc.Is(CustomRoleTypes.Neutral))
                {
                    Utils.SendMessage(string.Format(GetString("GuessSnitch"), GetString("Neutral")), pc.PlayerId, Utils.ColorString(Palette.DisabledGrey, "【===ﾁｮｯﾄﾏﾃﾁｮﾄﾏﾃｸﾙｰｻﾝ!!===】"));
                    return true;
                }

                if (role.IsAddOn())
                {
                    return true;
                }
                if (pc.Is(CustomRoleTypes.Impostor) && role == CustomRoles.Egoist)
                {
                    return true;
                }
                if (pc.Is(CustomRoles.Egoist) && target.Is(CustomRoleTypes.Impostor) && Guesser.ICanGuessNakama.GetBool())
                {
                    return true;
                }

                //↓ここから成功/失敗の処理。
                if (pc.PlayerId == target.PlayerId)
                {
                    Utils.SendMessage(GetString("Guesssuicide"), pc.PlayerId, Utils.ColorString(Color.cyan, GetString("Guesssuicidetitle")));
                    guesserSuicide = true;
                }
                else if (role.IsRiaju() && target.GetCustomRole().IsRiaju())
                {//ラバーズって打っててどれかのラバーズなら破滅★
                    guesserSuicide = false;
                }
                else if (pc.Is(CustomRoleTypes.Crewmate) && !pc.Is(CustomRoleTypes.Madmate) &&
                        (
                        (role.IsCrewmate() && !Guesser.CCanGuessNakama.GetBool()) ||
                        (role.IsWhiteCrew() && !Guesser.CCanWhiteCrew.GetBool())))
                {
                    guesserSuicide = true;
                }
                else if (pc.Is(CustomRoles.Guesser) && pc.Is(CustomRoleTypes.Impostor) &&
                (
                    (role.IsImpostor() && !Guesser.ICanGuessNakama.GetBool()) ||
                    (role.IsWhiteCrew() && !Guesser.ICanWhiteCrew.GetBool())))
                {
                    guesserSuicide = true;
                }
                else if (pc.Is(CustomRoles.LastImpostor) && pc.Is(CustomRoleTypes.Impostor) &&
                        ((role.IsImpostor() && !LastImpostor.ICanGuessNakama.GetBool()) ||
                        (role.IsWhiteCrew() && !LastImpostor.ICanWhiteCrew.GetBool())))
                {
                    guesserSuicide = true;
                }
                else if (pc.Is(CustomRoleTypes.Madmate) && (
                        (role.IsImpostor() && !Guesser.MCanGuessNakama.GetBool()) ||
                        (role.IsMadmate() && !Guesser.MCanGuessNakama.GetBool()) ||
                        (role.IsWhiteCrew() && !Guesser.MCanWhiteCrew.GetBool())))
                {
                    guesserSuicide = true;
                }
                else if (pc.Is(CustomRoles.Guesser) && pc.Is(CustomRoleTypes.Neutral) &&
                        role.IsWhiteCrew() && !Guesser.NCanWhiteCrew.GetBool())
                {
                    guesserSuicide = true;
                }
                else if (pc.Is(CustomRoles.LastNeutral) && pc.Is(CustomRoleTypes.Neutral) &&
                        role.IsWhiteCrew() && !Guesser.NCanWhiteCrew.GetBool())
                {
                    guesserSuicide = true;
                }
                else if (CheckTargetRoles(target, role))
                {
                    guesserSuicide = true;
                }
                Logger.Info($"{pc.GetNameWithRole()} が {target.GetNameWithRole()} をゲス", "Guesser");

                var dp = guesserSuicide ? pc : target;
                var tempDeathReason = CustomDeathReason.Guess;
                tempDeathReason = guesserSuicide ? CustomDeathReason.Misfire : CustomDeathReason.Guess;
                var dareda = target;
                target = dp;

                Logger.Info($"ゲッサー：{target.GetNameWithRole()} ", "Guesser");

                string Name = dp.GetRealName();

                GuesserGuessed[pc.PlayerId]++;
                TGuess[pc.PlayerId]++;

                _ = new LateTask(() =>
                {
                    var playerState = PlayerState.GetByPlayerId(dp.PlayerId);
                    playerState.DeathReason = tempDeathReason;
                    dp.SetRealKiller(pc);
                    RpcGuesserMurderPlayer(dp);

                    //死者检查
                    //Utils.AfterPlayerDeathTasks(dp, true);
                    CustomRoleManager.OnMurderPlayer(pc, target);

                    Utils.NotifyRoles(isForMeeting: true, NoCache: true);

                    _ = new LateTask(() =>
                    {
                        foreach (var pl in Main.AllPlayerControls)
                        {
                            Utils.SendMessage(Utils.GetPlayerColor(dp, true) + GetString("Meetingkill"), pl.PlayerId, GetString("MSKillTitle"));
                        }
                    }, 0.1f, "Guess Msg");
                }, 0.2f, "Guesser Kill");
                _ = new LateTask(() =>
                {
                    foreach (var spl in Main.AllPlayerControls.Where(pc => !pc.IsAlive()))
                    {
                        string send = "";
                        if (pc == dp) send = string.Format(GetString("Rgobaku"), Utils.GetPlayerColor(pc, true), Utils.GetPlayerColor(dareda, true), GetString($"{role}").Color(Utils.GetRoleColor(role)));
                        else send = string.Format(GetString("RMeetingKill"), Utils.GetPlayerColor(pc, true), Utils.GetPlayerColor(dp, true));
                        Utils.SendMessage(send, spl.PlayerId, GetString("RMSKillTitle"));
                    }
                }, 2.0f, "Reikai Guess Msg");
            }
        }
        return true;
    }

    private static bool CheckTargetRoles(PlayerControl target, CustomRoles role)
    {
        bool result = !target.Is(role);

        switch (target.GetCustomRole())
        {
            case CustomRoles.Engineer:
                if (role == CustomRoles.Engineer) result = false;
                break;
            case CustomRoles.Scientist:
                if (role == CustomRoles.Scientist) result = false;
                break;
            case CustomRoles.Impostor:
                if (role == CustomRoles.Impostor) result = false;
                break;
            case CustomRoles.Shapeshifter:
                if (role == CustomRoles.Shapeshifter) result = false;
                break;
        }

        return result;
    }

    public static TextMeshPro nameText(this PlayerControl p) => p.cosmetics.nameText;
    public static TextMeshPro NameText(this PoolablePlayer p) => p.cosmetics.nameText;
    public static void RpcGuesserMurderPlayer(this PlayerControl pc, float delay = 0f)
    {
        // DEATH STUFF //
        var amOwner = pc.AmOwner;
        pc.Data.IsDead = true;
        pc.RpcExileV2();
        var playerState = PlayerState.GetByPlayerId(pc.PlayerId);
        playerState.SetDead();
        PlayerState.AllPlayerStates[pc.PlayerId].SetDead();
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        foreach (var ap in Main.AllPlayerControls) ap.KillFlash();
        if (amOwner)
        {
            hudManager.ShadowQuad.gameObject.SetActive(false);
            pc.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
            pc.RpcSetScanner(false);
            ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
            meetingHud.SetForegroundForDead();
        }
        PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
            x => x.TargetPlayerId == pc.PlayerId
        );
        if (voteArea == null) return;
        if (voteArea.DidVote) voteArea.UnsetVote();
        voteArea.AmDead = true;
        voteArea.Overlay.gameObject.SetActive(true);
        voteArea.Overlay.color = Color.white;
        voteArea.XMark.gameObject.SetActive(true);
        voteArea.XMark.transform.localScale = Vector3.one;
        foreach (var playerVoteArea in meetingHud.playerStates)
        {
            if (playerVoteArea.VotedFor != pc.PlayerId) continue;
            playerVoteArea.UnsetVote();
            meetingHud.RpcClearVote(playerVoteArea.TargetPlayerId);
            meetingHud.ClearVote();
            MeetingHudPatch.CastVotePatch.Prefix(meetingHud, playerVoteArea.TargetPlayerId, pc.PlayerId);
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            meetingHud.RpcClearVote(voteAreaPlayer.GetClientId());
            meetingHud.ClearVote();
            MeetingHudPatch.CastVotePatch.Prefix(meetingHud, playerVoteArea.TargetPlayerId, pc.PlayerId);
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GuessKill, SendOption.Reliable, -1);
        writer.Write(pc.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        _ = new LateTask(() => meetingHud.CheckForEndVoting(), 5f, "GuessMeetingCheck");
    }
    public static void RpcClientGuess(PlayerControl pc)
    {
        var amOwner = pc.AmOwner;
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        foreach (var ap in Main.AllPlayerControls) ap.KillFlash();
        if (amOwner)
        {
            hudManager.ShadowQuad.gameObject.SetActive(false);
            pc.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
            pc.RpcSetScanner(false);
            ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
            meetingHud.SetForegroundForDead();
        }
        PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
            x => x.TargetPlayerId == pc.PlayerId
        );
        //pc.Die(DeathReason.Kill);
        if (voteArea == null) return;
        if (voteArea.DidVote) voteArea.UnsetVote();
        voteArea.AmDead = true;
        voteArea.Overlay.gameObject.SetActive(true);
        voteArea.Overlay.color = Color.white;
        voteArea.XMark.gameObject.SetActive(true);
        voteArea.XMark.transform.localScale = Vector3.one;
        foreach (var playerVoteArea in meetingHud.playerStates)
        {
            if (playerVoteArea.VotedFor != pc.PlayerId) continue;
            playerVoteArea.UnsetVote();
            meetingHud.RpcClearVote(playerVoteArea.TargetPlayerId);
            meetingHud.ClearVote();
            MeetingHudPatch.CastVotePatch.Prefix(meetingHud, playerVoteArea.TargetPlayerId, pc.PlayerId);
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            MeetingHudPatch.CastVotePatch.Prefix(meetingHud, playerVoteArea.TargetPlayerId, pc.PlayerId);
            meetingHud.RpcClearVote(voteAreaPlayer.GetClientId());
            meetingHud.ClearVote();
            playerVoteArea.UnsetVote();
        }
        _ = new LateTask(() => meetingHud.CheckForEndVoting(), 5f, "GuessMeetingCheck");
    }
    private static bool MsgToPlayerAndRole(string msg, out byte id, out CustomRoles role, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
        }

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            //并不是玩家编号，判断是否颜色
            //byte color = GetColorFromMsg(msg);
            //好吧我不知道怎么取某位玩家的颜色，等会了的时候再来把这里补上
            id = byte.MaxValue;
            error = GetString("GuessError1");
            role = new();
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("GuessError2");
            role = new();
            return false;
        }

        if (!ChatCommands.GetRoleByInputName(msg, out role, true))
        {
            error = GetString("GuessError1");
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static string ChangeNormal2Vanilla(CustomRoles role)
    {
        switch (role)
        {
            case CustomRoles.Engineer:
                role = CustomRoles.Engineer;
                break;
            case CustomRoles.Scientist:
                role = CustomRoles.Scientist;
                break;
            case CustomRoles.Impostor:
                role = CustomRoles.Impostor;
                break;
            case CustomRoles.Shapeshifter:
                role = CustomRoles.Shapeshifter;
                break;
        }

        return GetString(Enum.GetName(typeof(CustomRoles), role)).TrimStart('*').ToLower().Trim().Replace(" ", string.Empty).RemoveHtmlTags();
    }
    private static void SendRPC(int playerId, CustomRoles role)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Guess, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write((byte)role);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadInt32();
        CustomRoles role = (CustomRoles)reader.ReadByte();
        GuesserMsg(pc, $"/bt {PlayerId} {GetString(role.ToString())}", true);
    }
}