/*using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;
public sealed class Assassin : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Assassin),
            player => new Assassin(player),
            CustomRoles.Assassin,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            1902,
            null,
            "as",
            tab: TabGroup.Combinations,
            assignInfo: new RoleAssignInfo(CustomRoles.Assassin, CustomRoleTypes.Impostor)
            {
                AssignUnitRoles = new CustomRoles[2] { CustomRoles.Assassin, CustomRoles.Merlin }
            },
            combination: CombinationRoles.AssassinandMerlin
        );
    public Assassin(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        MeetingStates = 0;
        hostname = null;
    }

    static string hostname;
    static int MeetingStates;

    //ここに書いておこう！
    //この役職は実装しない予定だ！
    //えなんでかって？SHRとかにあるかｒ(((
    //配信とかで使ってあげてください(
    //ちなみにマーリン死んでたら終わり☆

    public override bool VotingResults(ref GameData.PlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile)
    {
        if (Exiled != null)
        {
            if (Exiled.PlayerId == Player.PlayerId && MeetingStates is 0)
            {
                Exiled = null;
                IsTie = true;
                MeetingStates = 1;
            }
        }
        if (MeetingStates is 2)
        {
            PlayerControl target;
            if (Exiled is null)
                target = Player;
            else
                target = Utils.GetPlayerById(Exiled.PlayerId);
            if (target.Is(CustomRoles.Merlin))
                MeetingStates = 3;
            Exiled = Player.Data;
            IsTie = false;
            _ = new LateTask(() =>
            {
                var text = Main.AllPlayerNames[target.PlayerId];
                if (target.PlayerId == PlayerControl.LocalPlayer.PlayerId && Main.nickName != "")
                    text = Main.nickName;
                text += $"は{GetString($"{CustomRoles.Merlin}")}{(MeetingStates is 3 ? "だ" : "ではなか")}った。<size=0>";
                if (Is(PlayerControl.LocalPlayer))
                {
                    hostname = Main.nickName;
                    Main.nickName = text;
                }
                else
                    Player.RpcSetName(text);
            }, 4f);
        }

        return false;
    }

    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        if (MeetingStates is not 2 and not 3) return;

        if (Is(PlayerControl.LocalPlayer))
            Main.nickName = hostname;
        else
            Player.RpcSetName(Main.AllPlayerNames[Player.PlayerId]);

        if (MeetingStates is 3)
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
            DecidedWinner = true;
        }
        MeetingStates = 0;
    }

    public override void AfterMeetingTasks()
    {
        if (MeetingStates is 1)
        {
            _ = new LateTask(() =>
            {
                Player.NoCheckStartMeeting(null);
                MeetingStates = 2;
                Utils.SendMessage("マーリンはだれ？", title: $"<color={RoleInfo.RoleColorCode}><size=3>アサシン会議");
            }, 0.4f);
        }
    }

    public override void OnStartMeeting()
    {
        var sender = new CustomRpcSender("Assassin SoseiRPC", SendOption.Reliable, true);
        sender.StartMessage(Player.GetClientId());
        var writer = sender.stream;
        List<byte> sosei = new();
        writer.StartMessage(1); // Data
        {
            writer.WritePacked(GameData.Instance.NetId);
            foreach (var info in GameData.Instance.AllPlayers.ToArray().Where(i => i.IsDead))
            {
                info.IsDead = false;
                writer.StartMessage(info.PlayerId);
                info.Serialize(writer);
                writer.EndMessage();
                sosei.Add(info.PlayerId);
            }
            writer.EndMessage();
        }
        sender.EndMessage();

        _ = new LateTask(() =>
        {
            foreach (var id in sosei)
                Utils.GetPlayerById(id).RpcExileV2();
        }, 3f);
    }

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (MeetingStates is 2 && !Is(voter))
            return false;
        return true;
    }
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        if (MeetingStates is 2)
            MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, sourceVotedForId);
        return (null, null, true);
    }
}*/