using AmongUs.GameOptions;
using System;
using System.Linq;
using System.Collections.Generic;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Madmate;

using static TownOfHost.Modules.SelfVoteManager;
using static TownOfHost.Modules.MeetingVoteManager;
using static TownOfHost.Modules.MeetingTimeManager;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;
public sealed class Balancer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Balancer),
            player => new Balancer(player),
            CustomRoles.Balancer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            22215,
            SetupOptionItem,
            "bal",
            "#cff100",
            introSound: () => GetIntroSound(RoleTypes.Crewmate),
            from: From.SuperNewRoles
        );
    public Balancer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        meetingtime = OptionMeetingTime.GetInt();
        s = OptionS.GetBool();
        target1 = 255;
        target2 = 255;
        Target1 = 255;
        Target2 = 255;
        used = false;
        Id = 255;
        nickname = null;
    }

    static OptionItem OptionMeetingTime;
    static OptionItem OptionS;

    //共有用
    public static byte target1 = 255, target2 = 255;
    public static byte Id = 255;
    public static int meetingtime;
    static string nickname;
    static bool s; //誰かが死亡するまで、能力を使えない
    //プレイヤーによって操作できる
    byte Target1, Target2;
    bool used;

    enum Option
    {
        meetingtime,
        sbalancer
    }

    private static void SetupOptionItem()
    {
        OptionMeetingTime = IntegerOptionItem.Create(RoleInfo, 10, Option.meetingtime, new(15, 120, 1), 30, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionS = BooleanOptionItem.Create(RoleInfo, 11, Option.sbalancer, false, false);
    }

    public override void Add()
        => AddS(Player);
    public override void OnDestroy()
    {
        Id = 255;
        if (Target1 != 255 && Target2 != 255)
        {
            Utils.GetPlayerById(Target1).RpcSetName(Main.AllPlayerNames[Target1]);
            Utils.GetPlayerById(Target2).RpcSetName(Main.AllPlayerNames[Target2]);
        }
        target1 = 255;
        target2 = 255;
        if (nickname != null)
            Main.nickName = nickname;
        nickname = null;
    }

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (MadAvenger.Skill) return true;
        //誰かが天秤を発動していて、自分ではないなら実行しない
        if (Id is not 255 && Id != Player.PlayerId) return true;
        //発動してるなら～
        if (Id is not 255)
        {
            //投票先が天秤のターゲットではないなら投票しない
            if (votedForId == Target1 || votedForId == Target2)
                return true;
            return false;
        }

        //通常会議の処理 投票した人が自分ではない or 能力使用済みならここから先は実行しない
        if (voter.PlayerId != Player.PlayerId || used || (s && !GameStates.AlreadyDied))
            return true;

        //天秤モードかチェック
        if (CheckSelfVoteMode(Player, votedForId, out var status))
        {
            if (status is VoteStatus.Self)
            {
                //ターゲットの情報をリセット
                Target1 = 255;
                Target2 = 255;
                Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Balancer"), GetString("Vote.Balancer")) + GetString("VoteSkillMode"), Player.PlayerId);
                //正直前のメッセージの方が好きby ky
                //↑ﾏｴﾉｯﾃﾅﾆ..._(:3 」∠)_ byʕ⓿ᴥ⓿ʔ
            }
            if (status is VoteStatus.Skip)
            {
                SetMode(Player, false);
                Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
            }
            //選ぶ処理
            if (status is VoteStatus.Vote)
            {
                Vote();
            }
            return false;
        }
        else
        {
            if (votedForId == Player.PlayerId && ((Target1 != 255 && Target2 == 255) || (Target1 == 255 && Target2 != 255)))
            {
                Vote();
                return false;
            }
        }
        return true;

        void Vote()
        {
            //1一目が決まってないなら一人目を決める
            if (Target1 == 255)
                Target1 = votedForId;
            //二人目が決まってないなら二人目を決める
            else if (Target2 == 255)
                Target2 = votedForId;

            //同じ人なら二人目をリセット
            if (Target1 == Target2)
                Target2 = 255;

            //プレイヤーの状態を取得
            var p1 = Utils.GetPlayerById(Target1);
            var p2 = Utils.GetPlayerById(Target2);

            //切断or死んでいるならリセット
            if (!p1.IsAlive())
                Target1 = 255;
            if (!p2.IsAlive())
                Target2 = 255;

            //どちらかの情報があるならチャットで伝える
            if (Target1 != 255 || Target2 != 255)
            {
                //どちらかが決まっていなかったら一人目
                var n = (Target1 != 255 && Target2 != 255) ? GetString("TowPlayer") : GetString("OnePlayer");
                var s = string.Format(GetString("Skill.Balancer"), n, Utils.GetPlayerColor(Utils.GetPlayerById(votedForId), true));
                Utils.SendMessage(s.ToString(), Player.PlayerId);
            }

            //二人決まったなら会議を終了
            if (Target1 != 255 && Target2 != 255)
            {
                Voteresult = "<color=#cff100>☆" + GetString("BalancerMeeting") + "☆</color>\n" + string.Format(GetString("BalancerMeetingInfo"), Utils.GetPlayerColor(Utils.GetPlayerById(Target1), true), Utils.GetPlayerColor(Utils.GetPlayerById(Target2), true));
                used = true;
                target1 = Target1;
                target2 = Target2;
                ExileControllerWrapUpPatch.AntiBlackout_LastExiled = null;
                MeetingHud.Instance.RpcClose();
            }
        }
    }

    public override bool VotingResults(ref GameData.PlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile)
    {
        //天秤モードじゃないor自分の天秤じゃないなら実行しない
        if (Id != Player.PlayerId) return false;

        //ディクテーターなどの強制的に会議を終わらせるものなら生存確認の処理スキップ
        if (!ClearAndExile)
        {
            var d1 = Utils.GetPlayerById(Target1);
            var d2 = Utils.GetPlayerById(Target2);

            //二人とも切断or死んでいるなら同数
            if (!d1.IsAlive() && !d2.IsAlive())
            {
                IsTie = true;
                Exiled = null;
                return true;
            }
            IsTie = false;
            //チェック
            if (!d1.IsAlive())
            {
                Exiled = d2.Data;
                return true;
            }
            if (!d2.IsAlive())
            {
                Exiled = d1.Data;
                return true;
            }
        }

        var rand = new Random();
        Dictionary<byte, int> data = new(2)
        {
            //セット
            { Target1, 0 },
            { Target2, 0 }
        };

        //投票をカウント、投票してない場合はどちらかに投票させる
        foreach (var voteData in Instance.AllVotes)
        {
            var voted = voteData.Value;

            //死んでたらスキップ
            if (!Utils.GetPlayerById(voted.Voter).IsAlive()) continue;

            //ディクテーターなどの強制的に会議を終わらせるものではないならランダム投票
            if (!voted.HasVoted && !ClearAndExile)
            {
                var id = rand.Next(0, 2) is 0 ? Target1 : Target2;
                Instance.SetVote(voted.Voter, id, isIntentional: false);
                data[id] += 1;
            }
            else if (voted.VotedFor is not NoVote) //投票なし(死亡時、会議強制終了時など)の人はスキップ
            {
                data[voted.VotedFor] += voted.NumVotes;
            }
        }
        //暗転対策の追放リセット
        ExileControllerWrapUpPatch.AntiBlackout_LastExiled = null;

        //ランダムで追放者を決める 同数ならどちらも追放
        var exileId = data.Where(kv => kv.Value == data.Values.Max())
                        .Select(kv => kv.Key)
                        .OrderBy(x => Guid.NewGuid())
                        .FirstOrDefault();
        Exiled = GameData.Instance.GetPlayerById(exileId);
        if (data.Values.Distinct().Count() == 1)
        {
            //追放画面が出てくるちょい前に名前を変える
            _ = new LateTask(() =>
            {
                //ホストなら別の処理
                if (exileId is 0)
                {
                    nickname = Main.nickName;
                    Main.nickName = GetString("Balancer.Executad") + "<size=0>";
                }
                else
                    Utils.GetPlayerById(exileId).RpcSetName(GetString("Balancer.Executad") + "<size=0>");
            }, 4f, "dotiramotuihou☆");
            var toExile = data.Keys.ToArray();
            foreach (var playerId in toExile)
            {
                Utils.GetPlayerById(playerId)?.SetRealKiller(null);
            }
            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, toExile);
            Voteresult = GetString("Balancer.Executad");
            Main.gamelog += $"\n{DateTime.Now.ToString("HH.mm.ss")} [Vote]　" + GetString("Balancer.Executad");
        }
        return true;
    }

    public override void BalancerAfterMeetingTasks()
    {
        //天秤会議になってない状態なら
        if (Id == 255 && Target1 is not 255 && Target2 is not 255)
        {
            //天秤会議にする
            Id = Player.PlayerId;
            //対象の名前を天秤の色に
            foreach (var pc in Main.AllPlayerControls.Where(pc => pc.PlayerId == Target1 || pc.PlayerId == Target2))
                pc.RpcSetName("<color=red>Ω" + Utils.ColorString(RoleInfo.RoleColor, Main.AllPlayerNames[pc.PlayerId]) + "<color=red>Ω");
            Balancer(meetingtime);
            PlayerControl.LocalPlayer.NoCheckStartMeeting(PlayerControl.LocalPlayer.Data);
            //アナウンス(合体させるからコメントアウト)
            //Utils.SendMessage($"{Main.AllPlayerNames[Target1]}と{Main.AllPlayerNames[Target2]}が天秤に掛けられました！\n\nどちらかに投票せよ！");

            _ = new LateTask(() =>
            {
                //名前を戻す
                Utils.GetPlayerById(Target1)?.RpcSetName(Main.AllPlayerNames[Target1]);
                Utils.GetPlayerById(Target2)?.RpcSetName(Main.AllPlayerNames[Target2]);
            }, 0.5f);

            return;
        }
    }
    public override void AfterMeetingTasks()
    {
        //自分の天秤会議じゃないなら実行しない
        if (Id != Player.PlayerId)
            return;

        //名前を戻す
        Utils.GetPlayerById(Target1)?.RpcSetName(Main.AllPlayerNames[Target1]);
        Utils.GetPlayerById(Target2)?.RpcSetName(Main.AllPlayerNames[Target2]);

        if (nickname != null)
            Main.nickName = nickname;
        nickname = null;

        //名前にロールとかのを適用
        _ = new LateTask(() => Utils.NotifyRoles(isForMeeting: false, ForceLoop: true, NoCache: true), 0.2f);

        //リセット
        Id = 255;
        Target1 = 255;
        Target2 = 255;
        target1 = 255;
        target2 = 255;
        return;
    }
}