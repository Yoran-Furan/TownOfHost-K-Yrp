using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class Trapper : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Trapper),
            player => new Trapper(player),
            CustomRoles.Trapper,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20800,
            SetupOptionItem,
            "tra",
            "#5a8fd0",
            from: From.TownOfHost
        );
    public Trapper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        BlockMoveTime = OptionBlockMoveTime.GetFloat();
    }

    private static OptionItem OptionBlockMoveTime;
    enum OptionName
    {
        TrapperBlockMoveTime
    }

    private static float BlockMoveTime;
    public static bool Tora;

    private static void SetupOptionItem()
    {
        OptionBlockMoveTime = FloatOptionItem.Create(RoleInfo, 10, OptionName.TrapperBlockMoveTime, new(1f, 180f, 1f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        if (info.IsSuicide) return;

        var killer = info.AttemptKiller;
        var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
        Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;
        ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
        Tora = true;
        killer.MarkDirtySettings();
        _ = new LateTask(() =>
        {
            Tora = false;
            Main.AllPlayerSpeed[killer.PlayerId] = tmpSpeed;
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
            killer.MarkDirtySettings();
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
        }, BlockMoveTime, "Trapper BlockMove");
    }
}