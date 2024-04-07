using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;
public sealed class MadBait : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadBait),
            player => new MadBait(player),
            CustomRoles.MadBait,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Madmate,
            10300,
            null,
            "mb",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadBait(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
    }

    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;

    public bool CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;

    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (target.Is(CustomRoles.MadBait) && !info.IsSuicide && !killer.GetCustomRole().IsImpostor())
            _ = new LateTask(() => killer.CmdReportDeadBody(target.Data), 0.15f, "MadBait Self Report");
    }
}