using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class ProgressKiller : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ProgressKiller),
            player => new ProgressKiller(player),
            CustomRoles.ProgressKiller,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            9000,
            SetupOptionItem,
            "pk"
        );
    public ProgressKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Madseer = OptionMadseer.GetBool();
        Workhorseseer = OptionWorkhorseseer.GetBool();
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    public static OptionItem OptionMadseer;
    public static OptionItem OptionWorkhorseseer;
    enum OptionName
    {
        Madseer,
        Workhorseseer,
    }
    public static bool Madseer;
    public static bool Workhorseseer;
    private static void SetupOptionItem()
    {
        OptionMadseer = BooleanOptionItem.Create(RoleInfo, 10, OptionName.Madseer, true, false);
        OptionWorkhorseseer = BooleanOptionItem.Create(RoleInfo, 11, OptionName.Workhorseseer, true, false);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;
        if (Madseer && seen.Is(CustomRoleTypes.Madmate) && seer.Is(CustomRoles.ProgressKiller) && seer != seen)
        {
            if (seen.GetPlayerTaskState().IsTaskFinished)
                return Utils.ColorString(RoleInfo.RoleColor, "☆");
        }
        return "";
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seer.Is(CustomRoles.ProgressKiller) && !seen.Is(CustomRoleTypes.Madmate) && seer != seen)
        {
            if (seen.GetPlayerTaskState().IsTaskFinished)
                return Utils.ColorString(RoleInfo.RoleColor, "〇");
        }
        return "";
    }
}