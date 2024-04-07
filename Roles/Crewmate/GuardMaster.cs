using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class GuardMaster : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(GuardMaster),
            player => new GuardMaster(player),
            CustomRoles.GuardMaster,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21100,
            SetupOptionItem,
            "gms",
            "#8FBC8B"
        );
    public GuardMaster(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanSeeCheck = OptoonCheck.GetBool();
        AddGuardCount = OptionAddGuardCount.GetInt();
        Guard = 0;
    }
    private static OptionItem OptionAddGuardCount;
    private static OptionItem OptoonCheck;
    private static Options.OverrideTasksData Tasks;
    private static int AddGuardCount;
    private static bool CanSeeCheck;
    int Guard = 0;
    enum OptionName
    {
        AddGuardCount,
        MadGuardianCanSeeWhoTriedToKill
    }
    private static void SetupOptionItem()
    {
        OptoonCheck = BooleanOptionItem.Create(RoleInfo, 10, OptionName.MadGuardianCanSeeWhoTriedToKill, true, false);
        OptionAddGuardCount = FloatOptionItem.Create(RoleInfo, 11, OptionName.AddGuardCount, new(1f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // 直接キル出来る役職チェック
        if (Guard <= 0) return true; // ガードなしで普通にキル

        if (!NameColorManager.TryGetData(killer, target, out var value) || value != RoleInfo.RoleColorCode)
        {
            NameColorManager.Add(killer.PlayerId, target.PlayerId);
            if (CanSeeCheck)
                NameColorManager.Add(target.PlayerId, killer.PlayerId, RoleInfo.RoleColorCode);
            Utils.NotifyRoles();
        }
        killer.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(target);
        info.CanKill = false;
        Guard--;
        Main.gamelog += $"\n{System.DateTime.Now.ToString("HH.mm.ss")} [GuardMaster]　" + Utils.GetPlayerColor(Player) + ":  " + string.Format(Translator.GetString("GuardMaster.Guard"), Utils.GetPlayerColor(killer, true) + $"(<b>{Utils.GetTrueRoleName(killer.PlayerId, false)}</b>)");
        Logger.Info($"{target.GetNameWithRole()} : ガード残り{Guard}回", "GuardMaster");
        Utils.NotifyRoles();
        return true;
    }
    public override bool OnCompleteTask()
    {
        if (IsTaskFinished && Player.IsAlive())
            Guard += AddGuardCount;
        return true;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(Guard == 0 ? UnityEngine.Color.gray : RoleInfo.RoleColor, $"({Guard})");
}