using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using System;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Madmate;
using static TownOfHost.Modules.SelfVoteManager;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;
public sealed class FortuneTeller : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(FortuneTeller),
            player => new FortuneTeller(player),
            CustomRoles.FortuneTeller,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            28000,
            SetupOptionItem,
            "fo",
            "#6b3ec3",
            introSound: () => GetIntroSound(RoleTypes.Scientist)
        );
    public FortuneTeller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Max = OptionMaximum.GetFloat();
        Divination.Clear();
        count = 0;
        mcount = 0;
        Votemode = (VoteMode)OptionVoteMode.GetValue();
        rolename = Optionrolename.GetBool();
        srole = OptionRole.GetBool();
        cantaskcount = OptionCanTaskcount.GetFloat();
        onemeetingmaximum = Option1MeetingMaximum.GetFloat();
    }

    private static OptionItem OptionMaximum;
    private static OptionItem OptionVoteMode;
    private static OptionItem Optionrolename;
    private static OptionItem OptionRole;
    private static OptionItem OptionCanTaskcount;
    private static OptionItem Option1MeetingMaximum;

    public float Max;
    public VoteMode Votemode;
    public bool rolename;
    public bool srole;
    int count;
    float cantaskcount;
    float onemeetingmaximum;
    float mcount;
    Dictionary<byte, CustomRoles> Divination = new();

    enum Option
    {
        Ucount,
        Votemode,
        rolename, //占った相手の名前の上に占い結果を表示するかの設定
        tRole, //占い時役職を表示するか、陣営を表示するかの設定
        cantaskcount,//効果を発揮タスク数
        meetingmc,//1会議に占える数(最大で)
    }
    public enum VoteMode
    {
        uvote,
        SelfVote,
    }

    public override void Add()
        => AddS(Player);

    private static void SetupOptionItem()
    {
        OptionMaximum = FloatOptionItem.Create(RoleInfo, 10, Option.Ucount, new(1f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 11, Option.Votemode, EnumHelper.GetAllNames<VoteMode>(), 0, false);
        Optionrolename = BooleanOptionItem.Create(RoleInfo, 12, Option.rolename, true, false);
        OptionRole = BooleanOptionItem.Create(RoleInfo, 13, Option.tRole, true, false);
        OptionCanTaskcount = FloatOptionItem.Create(RoleInfo, 14, Option.cantaskcount, new(0, 99, 1), 5, false);
        Option1MeetingMaximum = FloatOptionItem.Create(RoleInfo, 15, Option.meetingmc, new(0f, 99f, 1f), 0f, false)
            .SetValueFormat(OptionFormat.Times);
    }

    private void SendRPC(byte targetid, CustomRoles role)
    {
        using var sender = CreateSender();
        sender.Writer.Write(count);
        sender.Writer.Write(targetid);
        sender.Writer.WritePacked((int)role);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        count = reader.ReadInt32();
        Divination[reader.ReadByte()] = (CustomRoles)reader.ReadPackedInt32();
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (Divination.ContainsKey(seen.PlayerId) && rolename)
        {
            if (srole)
                return $"<color={Utils.GetRoleColorCode(Divination[seen.PlayerId])}>" + GetString(Divination[seen.PlayerId].ToString());
            else return GetString(Divination[seen.PlayerId].GetCustomRoleTypes().ToString());
        }
        return "";
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(MyTaskState.CompletedTasksCount < cantaskcount ? Color.gray : Max <= count ? Color.gray : Color.cyan, $"({Max - count})");
    public override void OnStartMeeting() => mcount = 0;
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (MadAvenger.Skill) return true;
        if (Max > count && Is(voter) && MyTaskState.CompletedTasksCount >= cantaskcount && (mcount < onemeetingmaximum || onemeetingmaximum == 0))
        {
            var target = Utils.GetPlayerById(votedForId);
            if (Votemode == VoteMode.uvote)
            {
                if (Player.PlayerId == votedForId || votedForId == SkipId) return true;
                Uranai(votedForId);
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

                        Uranai(votedForId);
                    SetMode(Player, status is VoteStatus.Self);
                    return false;
                }
            }
        }
        return true;
    }
    public void Uranai(byte votedForId)
    {
        var target = Utils.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;//死んでるならここで処理を止める。
        count++;//全体のカウント
        mcount++;//1会議のカウント
        var FtR = target.GetRoleClass()?.GetFtResults(Player); //結果を変更するかチェック
        var role = FtR is not CustomRoles.NotAssigned ? FtR.Value : target.GetCustomRole(); ;
        var s = GetString("Skill.Tellerfin") + (role.IsCrewmate() ? "!" : "...");
        Divination[votedForId] = role;
        SendRPC(votedForId, role);
        Utils.SendMessage(string.Format(GetString("Skill.Teller"), Utils.GetPlayerColor(target, true), srole ? "<b>" + GetString($"{role}").Color(Utils.GetRoleColor(role)) + "</b>" : GetString($"{role.GetCustomRoleTypes()}")) + s + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - mcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == VoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
        Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}", "FortuneTeller");
    }
}