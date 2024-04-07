using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;
public sealed class Doctor : RoleBase, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Doctor),
            player => new Doctor(player),
            CustomRoles.Doctor,
            () => RoleTypes.Scientist,
            CustomRoleTypes.Crewmate,
            20700,
            SetupOptionItem,
            "doc",
            "#80ffdd",
            from: From.NebulaontheShip
        );
    public Doctor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TaskCompletedBatteryCharge = OptionTaskCompletedBatteryCharge.GetFloat();
        CanseeComms = OptionComm.GetBool();
    }
    private static OptionItem OptionTaskCompletedBatteryCharge;
    private static OptionItem OptionComm;
    enum OptionName
    {
        DoctorTaskCompletedBatteryCharge,
        CanseeComms
    }
    private static float TaskCompletedBatteryCharge;
    private static bool CanseeComms;
    private static void SetupOptionItem()
    {
        OptionTaskCompletedBatteryCharge = FloatOptionItem.Create(RoleInfo, 10, OptionName.DoctorTaskCompletedBatteryCharge, new(0f, 10f, 1f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionComm = BooleanOptionItem.Create(RoleInfo, 11, OptionName.CanseeComms, false, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ScientistCooldown = 0f;
        AURoleOptions.ScientistBatteryCharge = TaskCompletedBatteryCharge;
    }
    public bool CheckSeeDeathReason(PlayerControl seen)//IDeathReasonSeeable
    {
        return Utils.IsActive(SystemTypes.Comms) || CanseeComms;
    }
}