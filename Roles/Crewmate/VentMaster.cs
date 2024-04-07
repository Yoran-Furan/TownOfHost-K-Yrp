using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class VentMaster : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(VentMaster),
            player => new VentMaster(player),
            CustomRoles.VentMaster,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            22000,
            null,
            "vm",
            "#ff6666",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public VentMaster(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.OnEnterVentOthers.Add(OnEnterVentOthers);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = 0;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }
    public static bool OnEnterVentOthers(PlayerPhysics physics, int ventId)
    {
        var user = physics.myPlayer;
        if (!user.Is(CustomRoles.VentMaster))
        {
            foreach (var seer in Main.AllPlayerControls)
            {
                if (seer.Is(CustomRoles.VentMaster) && seer.PlayerId != user.PlayerId) if (!seer.Data.IsDead || !GameStates.IsMeeting) seer.KillFlash(false);
            }
        }
        return true;
    }
}