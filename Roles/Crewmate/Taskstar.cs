using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class TaskStar : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(TaskStar),
            player => new TaskStar(player),
            CustomRoles.TaskStar,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            28720,
            SetupOptionItem,
            "ts",
            "#FFD700"
        );
    public TaskStar(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    private static void SetupOptionItem()
    {
        Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (IsTaskFinished)
            enabled = true;
    }
    public override bool OnCompleteTask()
    {
        if (IsTaskFinished)
        {
            Player.MarkDirtySettings();
        }

        return true;
    }
}