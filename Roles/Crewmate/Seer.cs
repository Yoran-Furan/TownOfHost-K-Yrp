using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;
public sealed class Seer : RoleBase, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Seer),
            player => new Seer(player),
            CustomRoles.Seer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21000,
            SetupOptionItem,
            "se",
            "#61b26c",
            from: From.TheOtherRoles
        );
    public Seer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanseeComms = OptionCanSeeComms.GetBool();
    }
    private static bool CanseeComms;
    private static OptionItem OptionCanSeeComms;
    enum OptionName
    {
        CanseeComms,
    }
    private static void SetupOptionItem()
    {
        OptionCanSeeComms = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanseeComms, true, false);
    }
    public bool CheckKillFlash(MurderInfo info) // IKillFlashSeeable
    {
        return !Utils.IsActive(SystemTypes.Comms) || CanseeComms;
    }
}