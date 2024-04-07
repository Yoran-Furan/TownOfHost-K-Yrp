using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class Evilgambler : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Evilgambler),
                player => new Evilgambler(player),
                CustomRoles.Evilgambler,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                3000,
                SetupOptionItem,
                "eg",
                from: From.SuperNewRoles
            );
    public Evilgambler(PlayerControl player)
    : base(
    RoleInfo,
    player
    )
    {
        gamblecollect = Optiongamblecollect.GetInt();
        collectkillCooldown = OptioncollectkillCooldown.GetFloat();
        notcollectkillCooldown = OptionnotcollectkillCooldown.GetFloat();
    }

    private static OptionItem Optiongamblecollect;
    private static OptionItem OptioncollectkillCooldown;
    private static OptionItem OptionnotcollectkillCooldown;
    enum OptionName
    {
        Evillgamblergamblecollect,
        EvillgamblercollectkillCooldown,
        EvillgamblernotcollectkillCooldown,
    }

    private static float gamblecollect;
    private static float collectkillCooldown;
    private static float notcollectkillCooldown;

    public bool CanBeLastImpostor { get; } = false;
    private static void SetupOptionItem()
    {
        Optiongamblecollect = FloatOptionItem.Create(RoleInfo, 10, OptionName.Evillgamblergamblecollect, new(0, 100, 5), 50, false)
            .SetValueFormat(OptionFormat.Percent);
        OptioncollectkillCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.EvillgamblercollectkillCooldown, new(0f, 180f, 2.5f), 2.5f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionnotcollectkillCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.EvillgamblernotcollectkillCooldown, new(0f, 180f, 2.5f), 50.0f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    //public static void SetKillCooldown(byte id, float amount) => Main.AllPlayerKillCooldown[id] = amount;
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;
            int chance = IRandom.Instance.Next(1, 101);
            if (chance < gamblecollect)
            {//gamble成功
                Logger.Info($"{killer?.Data?.PlayerName}:${chance}成功", "Evilgamble");
                Main.AllPlayerKillCooldown[killer.PlayerId] = collectkillCooldown;
                killer.SyncSettings();//キルクール処理を同期
            }
            else
            {
                Logger.Info($"{killer?.Data?.PlayerName}:${chance}失敗", "Evilgamble");
                Main.AllPlayerKillCooldown[killer.PlayerId] = notcollectkillCooldown;
                killer.SyncSettings();//キルクール処理を同期
            }
        }
        return;
    }
}