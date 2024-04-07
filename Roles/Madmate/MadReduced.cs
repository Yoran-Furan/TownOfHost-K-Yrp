using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Modules.MeetingVoteManager;
namespace TownOfHost.Roles.Madmate;
public sealed class MadReduced : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadReduced),
            player => new MadReduced(player),
            CustomRoles.MadReduced,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            60150,
            SetupOptionItem,
            "mre",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadReduced(PlayerControl player)
    : base(
        RoleInfo,
        player
        )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
        canVent = OptionCanVent.GetBool();
        Vote = OptionVote.GetInt();
    }
    private static OptionItem OptionCanVent;
    private static OptionItem OptionVote;
    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    private static int Vote;
    private static bool canVent;
    bool Skill;
    byte forvote;
    enum OptionName
    {
        CanVent,
        ReVote
    }

    public bool CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;

    public static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanVent, false, false);
        OptionVote = FloatOptionItem.Create(RoleInfo, 11, OptionName.ReVote, new(-99f, -1f, 1f), -2f, false)
            .SetValueFormat(OptionFormat.Votes);
    }
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        if (voterId == Player.PlayerId)
        {
            if (sourceVotedForId == NoVote)
            {
                Skill = false;
                numVotes = 1;
            }
            else
            if (sourceVotedForId == Skip)
            {
                Skill = false;
                numVotes = 1;
            }
            else
            if (sourceVotedForId == Player.PlayerId)
            {
                Skill = false;
                numVotes = 1;
            }
            else
            {
                Skill = true;
                numVotes = Vote;
                forvote = sourceVotedForId;
            }
        }
        return (votedForId, numVotes, doVote);
    }
    public override void OnStartMeeting()
    {
        if (Skill)
        {
            var T = Utils.GetPlayerById(forvote);
            Voteresult += string.Format(Translator.GetString("Skill.MadReduced"), Utils.GetPlayerColor(T, true), $"<b> {Vote * -1}</b>");
        }
    }
}