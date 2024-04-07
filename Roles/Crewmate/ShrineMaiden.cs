using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Madmate;
using System;
using static TownOfHost.Modules.SelfVoteManager;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;
public sealed class ShrineMaiden : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ShrineMaiden),
            player => new ShrineMaiden(player),
            CustomRoles.ShrineMaiden,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            28410,
            SetupOptionItem,
            "SM",
            "#b7282e",
            introSound: () => GetIntroSound(RoleTypes.Scientist)
        );
    public ShrineMaiden(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Max = OptionMaximum.GetFloat();
        Repo = false;
        count = 0;
        mcount = 0;
        cantaskcount = Optioncantaskcount.GetFloat();
        Votemode = (VoteMode)OptionVoteMode.GetValue();
        onemeetingmaximum = Option1MeetingMaximum.GetFloat();
        Oniku = 111;
    }

    private static OptionItem OptionMaximum;
    private static OptionItem OptionVoteMode;
    private static OptionItem Optioncantaskcount;
    private static OptionItem Option1MeetingMaximum;
    public float Max;
    public VoteMode Votemode;
    int count;
    float cantaskcount;
    float onemeetingmaximum;
    float mcount;
    static bool Repo;
    static byte Oniku;

    enum Option
    {
        Ucount,
        Votemode,
        cantaskcount,
        meetingmc
    }
    public enum VoteMode
    {
        uvote,
        SelfVote,
    }

    private static void SetupOptionItem()
    {
        OptionMaximum = FloatOptionItem.Create(RoleInfo, 11, Option.Ucount, new(1f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 12, Option.Votemode, EnumHelper.GetAllNames<VoteMode>(), 0, false);
        Optioncantaskcount = FloatOptionItem.Create(RoleInfo, 14, Option.cantaskcount, new(0, 99, 1), 5, false);
        Option1MeetingMaximum = FloatOptionItem.Create(RoleInfo, 15, Option.meetingmc, new(0f, 99f, 1f), 0f, false)
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Add()
        => AddS(Player);

    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(count);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        count = reader.ReadInt32();
    }
    public override void OnReportDeadBody(PlayerControl _, GameData.PlayerInfo O)
    {
        if (O != null)
        {
            Repo = true;
            Oniku = O.PlayerId;
        }
        else
        {
            Repo = false;
        }
    }
    public override void AfterMeetingTasks()
    {
        Repo = false;//いらない気がするけど一応保険
        Oniku = 111;
    }
    public override void OnStartMeeting() => mcount = 0;
    public override string GetProgressText(bool comms = false) => Utils.ColorString(MyTaskState.CompletedTasksCount < cantaskcount ? Color.gray : Max <= count ? Color.gray : Color.cyan, $"({Max - count})");
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (MadAvenger.Skill) return true;
        if (Repo && Max > count && Is(voter) && MyTaskState.CompletedTasksCount >= cantaskcount && (mcount < onemeetingmaximum || onemeetingmaximum == 0))
        {
            if (Votemode == VoteMode.uvote)
            {
                if (Player.PlayerId == votedForId || votedForId == SkipId) return true;
                Miko(votedForId);
                return false;
            }
            else
            {
                if (CheckSelfVoteMode(Player, votedForId, out var status))
                {
                    if (status is VoteStatus.Self)
                        Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Divied"), GetString("Vote.Divied")) + GetString("VoteSkillMode"), Player.PlayerId);
                    if (status is VoteStatus.Skip)
                        Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                    if (status is VoteStatus.Vote)
                        Miko(votedForId);
                    SetMode(Player, status is VoteStatus.Self);
                    return false;
                }
            }
        }
        return true;
    }
    public void Miko(byte votedForId)
    {
        var target1 = Utils.GetPlayerById(Oniku);
        var target2 = Utils.GetPlayerById(votedForId);
        if (!target2.IsAlive()) return;
        count++;
        mcount++;

        Logger.Info($"Player: {Player.name},Target1: {target1.name}Target2: {target2.name}", "ShrineMaiden");
        var FtR1 = target1.GetRoleClass()?.GetFtResults(Player);
        var role1 = FtR1 is not CustomRoles.NotAssigned ? FtR1.Value : target1.GetCustomRole();
        SendRPC();
        var ta1 = target1.GetCustomRole();
        var ta2 = target2.GetCustomRole();
        var t1 = ta1.GetCustomRoleTypes();
        var t2 = ta2.GetCustomRoleTypes();
        //マッドならimpにする
        if (t1 == CustomRoleTypes.Madmate) t1 = CustomRoleTypes.Impostor;
        if (t2 == CustomRoleTypes.Madmate) t2 = CustomRoleTypes.Impostor;

        if (t1 == t2)
        {
            Utils.SendMessage(string.Format(GetString("ShrineMaidencollect"), Utils.GetPlayerColor(target1, true), Utils.GetPlayerColor(target2, true)) + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - mcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count)) + (Votemode == VoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : ""), Player.PlayerId);
        }
        else
        {
            Utils.SendMessage(string.Format(GetString("ShrineMaidencollect"), Utils.GetPlayerColor(target1, true), Utils.GetPlayerColor(target2, true)) + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - mcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count)) + (Votemode == VoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : ""), Player.PlayerId);
        }
    }
}