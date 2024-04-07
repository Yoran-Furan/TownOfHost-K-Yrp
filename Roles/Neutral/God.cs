/*using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
namespace TownOfHost.Roles.Neutral;

//開発版専用役職
//Y参考

public sealed class God : RoleBase, ISystemTypeUpdateHook
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(God),
            player => new God(player),
            CustomRoles.God,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50800,
            SetupOptionItem,
            "g",
            "#ffff00",
            from: From.SuperNewRoles
        );
    public God(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        taskCompleteToWin = OptionTaskCompleteToWin.GetBool();
        viewVoteFor = OptionViewVoteFor.GetBool();

        if (Player != null)
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                NameColorManager.Add(Player.PlayerId, pc.PlayerId);
            }
        }
    }
    private static OptionItem OptionTaskCompleteToWin;
    private static OptionItem OptionViewVoteFor;
    private static Options.OverrideTasksData Tasks;
    enum OptionName
    {
        GodTaskCompleteToWin,
        GodViewVoteFor,
    }
    private static bool taskCompleteToWin;
    private static bool viewVoteFor;
    public static void SetupOptionItem()
    {
        OptionTaskCompleteToWin = BooleanOptionItem.Create(RoleInfo, 10, OptionName.GodTaskCompleteToWin, true, false);
        OptionViewVoteFor = BooleanOptionItem.Create(RoleInfo, 11, OptionName.GodViewVoteFor, false, false);
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        if (viewVoteFor)
        {
            opt.SetBool(BoolOptionNames.AnonymousVotes, false);
        }
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        enabled = true;
    }
    public static bool CheckWin()
    {
        return Main.AllAlivePlayerControls.ToArray()
                .Any(p => p.Is(CustomRoles.God) &&
                          (!taskCompleteToWin || p.GetPlayerTaskState().IsTaskFinished));
    }

    // コミュ
    bool ISystemTypeUpdateHook.UpdateHudOverrideSystem(HudOverrideSystemType switchSystem, byte amount)
    {
        if ((amount & HudOverrideSystemType.DamageBit) <= 0) return false;
        return true;
    }
    bool ISystemTypeUpdateHook.UpdateHqHudSystem(HqHudSystemType hqHudSystemType, byte amount)
    {
        var tags = (HqHudSystemType.Tags)(amount & HqHudSystemType.TagMask);
        if (tags == HqHudSystemType.Tags.FixBit) return false;
        return true;
    }
    // 停電
    bool ISystemTypeUpdateHook.UpdateSwitchSystem(SwitchSystem switchSystem, byte amount)
    {
        return false;
    }
    // O2
    bool ISystemTypeUpdateHook.UpdateLifeSuppSystem(LifeSuppSystemType switchSystem, byte amount)
    {
        return false;
    }
    // リアクター
    bool ISystemTypeUpdateHook.UpdateReactorSystem(ReactorSystemType switchSystem, byte amount)
    {
        return false;
    }
    bool ISystemTypeUpdateHook.UpdateHeliSabotageSystem(HeliSabotageSystem switchSystem, byte amount)
    {
        return false;
    }
}
*/