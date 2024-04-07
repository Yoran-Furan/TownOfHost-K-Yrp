using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class ToiletFan : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ToiletFan),
            player => new ToiletFan(player),
            CustomRoles.ToiletFan,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            22004,
            SetupOptionItem,
            "to",
            "#5f5573",
            introSound: () => GetIntroSound(RoleTypes.Crewmate),
            from: From.SuperNewRoles
        );
    public ToiletFan(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Cooldown = OptionCooldown.GetFloat();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = Cooldown;
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    private static OptionItem OptionCooldown;
    enum OptionName
    {
        Cooldown
    }
    private static float Cooldown;
    private static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.Cooldown, new(0f, 10f, 1f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 79);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 80);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 81);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 82);
        return false;
    }
}