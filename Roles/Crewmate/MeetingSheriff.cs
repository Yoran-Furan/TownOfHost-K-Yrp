using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Madmate;
using System.Linq;
using static TownOfHost.Translator;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Crewmate;
public sealed class MeetingSheriff : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MeetingSheriff),
            player => new MeetingSheriff(player),
            CustomRoles.MeetingSheriff,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20450,
            SetupOptionItem,
            "Ms",
            "#f8cd46",
            from: From.SuperNewRoles
            );
    public MeetingSheriff(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Max = OptionSheriffShotLimit.GetFloat();
        count = 0;
        mcount = 0;
        cankillMad = OptioncankillMad.GetBool();
        cankillN = OptioncankillN.GetBool();
        cantaskcount = Optioncantaskcount.GetFloat();
        onemeetingmaximum = Option1MeetingMaximum.GetFloat();
    }

    private static OptionItem OptionSheriffShotLimit;
    private static OptionItem OptioncankillMad;
    private static OptionItem OptioncankillN;
    private static OptionItem Optioncantaskcount;
    private static OptionItem Option1MeetingMaximum;
    public float Max;
    float cantaskcount;
    bool cankillMad;
    bool cankillN;
    int count;
    float onemeetingmaximum;
    float mcount;

    enum Option
    {
        SheriffShotLimit,
        cantaskcount,//効果を発揮タスク数
        cankillMad,
        cankillN,
        meetingmc
    }
    private static void SetupOptionItem()
    {
        OptionSheriffShotLimit = FloatOptionItem.Create(RoleInfo, 10, Option.SheriffShotLimit, new(1f, 15f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        Optioncantaskcount = FloatOptionItem.Create(RoleInfo, 11, Option.cantaskcount, new(0, 99, 1), 5, false);
        OptioncankillMad = BooleanOptionItem.Create(RoleInfo, 12, Option.cankillMad, true, false);
        OptioncankillN = BooleanOptionItem.Create(RoleInfo, 13, Option.cankillN, true, false);
        Option1MeetingMaximum = FloatOptionItem.Create(RoleInfo, 14, Option.meetingmc, new(0f, 99f, 1f), 0f, false)
            .SetValueFormat(OptionFormat.Times);
    }

    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(count);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        count = reader.ReadInt32();
    }
    public override void OnStartMeeting() => mcount = 0;
    public override string GetProgressText(bool comms = false) => Utils.ColorString(MyTaskState.CompletedTasksCount < cantaskcount ? Color.gray : Max <= count ? Color.gray : Color.cyan, $"({Max - count})");
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (MadAvenger.Skill) return true;
        if (Max > count && Is(voter) && MyTaskState.CompletedTasksCount >= cantaskcount && (mcount < onemeetingmaximum || onemeetingmaximum == 0))
        {
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is VoteStatus.Self)
                    Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.MeetingSheriff"), GetString("Vote.MeetingSheriff")) + GetString("VoteSkillMode"), Player.PlayerId);
                if (status is VoteStatus.Skip)
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                if (status is VoteStatus.Vote)
                    Sheriff(votedForId);
                SetMode(Player, status is VoteStatus.Self);
                return false;
            }
        }
        return true;
    }
    public void Sheriff(byte votedForId)
    {
        PlayerState state;
        var target = Utils.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;
        if (!AmongUsClient.Instance.AmHost) return;
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance.KillOverlay;
        count++;
        mcount++;//1会議のカウント
        SendRPC();

        if (CanBeKilledBy(target.GetCustomRole()) && !(target.Is(CustomRoles.Alien) && Alien.modeTR))
        {
            state = PlayerState.GetByPlayerId(target.PlayerId);
            target.RpcExileV2();
            state.DeathReason = CustomDeathReason.Kill;
            state.SetDead();
            Logger.Info($"{Player.GetNameWithRole()}がシェリフ成功({target.GetNameWithRole()}) 残り{Max - count}", "MeetingSheriff");
            Utils.SendMessage(Utils.GetPlayerColor(target, true) + GetString("Meetingkill"), title: GetString("MSKillTitle"));
            hudManager.ShowKillAnimation(target.Data, target.Data);
            foreach (var ap in Main.AllPlayerControls) ap.KillFlash();
            SoundManager.Instance.PlaySound(Player.KillSfx, false, 0.8f);
            PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(x => x.TargetPlayerId == target.PlayerId);
            if (voteArea == null) return;
            if (voteArea.DidVote) voteArea.UnsetVote();
            foreach (var playerVoteArea in meetingHud.playerStates)
            {
                if (playerVoteArea.VotedFor != target.PlayerId) continue;
                playerVoteArea.UnsetVote();
                meetingHud.RpcClearVote(playerVoteArea.TargetPlayerId);
                meetingHud.ClearVote();
                MeetingHudPatch.CastVotePatch.Prefix(meetingHud, playerVoteArea.TargetPlayerId, target.PlayerId);
                var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
                if (!voteAreaPlayer.AmOwner) continue;
                MeetingHudPatch.CastVotePatch.Prefix(meetingHud, playerVoteArea.TargetPlayerId, target.PlayerId);
                meetingHud.RpcClearVote(voteAreaPlayer.GetClientId());
                meetingHud.ClearVote();
                playerVoteArea.UnsetVote();
            }
            _ = new LateTask(() => meetingHud.CheckForEndVoting(), 5f, "MeetingSheriffCheck");
            return;
        }
        state = PlayerState.GetByPlayerId(Player.PlayerId);
        Player.RpcExileV2();
        state.DeathReason = target.Is(CustomRoles.Tairou) && Tairou.DeathReasonTairo ? CustomDeathReason.Revenge1 : target.Is(CustomRoles.Alien) && Alien.DeathReasonTairo ? CustomDeathReason.Revenge1 : CustomDeathReason.Misfire;
        state.SetDead();
        Logger.Info($"{Player.GetNameWithRole()}がシェリフ失敗({target.GetNameWithRole()}) 残り{Max - count}", "MeetingSheriff");
        Utils.SendMessage(Utils.GetPlayerColor(Player, true) + GetString("Meetingkill"), title: GetString("MSKillTitle"));
        foreach (var ap in Main.AllPlayerControls) ap.KillFlash();
        hudManager.ShowKillAnimation(Player.Data, Player.Data);
        SoundManager.Instance.PlaySound(Player.KillSfx, false, 0.8f);
        PlayerVoteArea voteArea2 = MeetingHud.Instance.playerStates.First(x => x.TargetPlayerId == Player.PlayerId);
        if (voteArea2 == null) return;
        if (voteArea2.DidVote) voteArea2.UnsetVote();

        foreach (var playerVoteArea in meetingHud.playerStates)
        {
            if (playerVoteArea.VotedFor != Player.PlayerId) continue;
            playerVoteArea.UnsetVote();
            meetingHud.RpcClearVote(playerVoteArea.TargetPlayerId);
            meetingHud.ClearVote();
            MeetingHudPatch.CastVotePatch.Prefix(meetingHud, playerVoteArea.TargetPlayerId, Player.PlayerId);
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            MeetingHudPatch.CastVotePatch.Prefix(meetingHud, playerVoteArea.TargetPlayerId, Player.PlayerId);
            meetingHud.RpcClearVote(voteAreaPlayer.GetClientId());
            meetingHud.ClearVote();
            playerVoteArea.UnsetVote();
        }
        //5s後にチェックを入れる(把握のため)
        _ = new LateTask(() => meetingHud.CheckForEndVoting(), 5f, "MeetingSheriffCheck");
    }
    bool CanBeKilledBy(CustomRoles role)
    {
        return role.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => role is not CustomRoles.Tairou,
            CustomRoleTypes.Madmate => cankillMad,
            CustomRoleTypes.Neutral => cankillN,
            _ => false
        };
    }// ↓改良したの作っちゃった☆ 動くかはわかんない byけーわい
    //ｶｲﾘｮｳｼﾃﾓﾗｯﾀﾅﾗﾂｶﾜﾅｲﾜｹｶﾞﾅｲ!!(大狼の処理とマッドの処理が出来てたからニュートラルもできるはず!!)
}
//コード多分改良できるけど動いてるからヨシ。(´・ω・｀)