/*using System.Linq;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;
public sealed class Merlin : RoleBase, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Merlin),
            player => new Merlin(player),
            CustomRoles.Merlin,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            29020,
            null,
            "mer",
            "#8cffff",
            combination: CombinationRoles.AssassinandMerlin
        );
    public Merlin(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        foreach (var impostor in Main.AllPlayerControls.Where(player => player.Is(CustomRoleTypes.Impostor)))
        {
            NameColorManager.Add(Player.PlayerId, impostor.PlayerId, impostor.GetRoleColorCode());
        }
    }
    //もし設定など入れたい場合は
    //あああ = Assassin.設定名
    //って感じにこっちに持ってくればよい(←わかりずらい)
}
*/