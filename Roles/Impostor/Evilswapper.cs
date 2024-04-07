/*using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Madmate;
using static TownOfHost.Modules.SelfVoteManager;
using TownOfHost.Roles.Core.Interfaces;


namespace TownOfHost.Roles.Impostor;
public sealed class Evilswapper : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Evilswapper),
                player => new Evilswapper(player),
                CustomRoles.Evilswapper,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                13000,
                null,
                "es"
            //from: From.SuperNewRoles
            );
    public Evilswapper(PlayerControl player)
    : base(
    RoleInfo,
    player
    )
    { }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (MadAvenger.Skill) return true;
        if (CheckSelfVoteMode(Player, votedForId, out var status))
        {
            if (status is VoteStatus.Self)
                Utils.SendMessage("");
            if (status is VoteStatus.Skip)
                Utils.SendMessage("VoteSkillFin", Player.PlayerId);
            if (status is VoteStatus.Vote)
                (votedForId);
            SetMode(Player, status is VoteStatus.Self);
            return false;
        }
        return true;
    }
}*/