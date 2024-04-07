using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class Noisemaker : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Noisemaker),
            player => new Noisemaker(player),
            CustomRoles.Noisemaker,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            4000,
            SetupOptionItems,
            "nm"
        );
    public Noisemaker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        Probability = OptionProbability.GetInt();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionProbability;
    enum OptionName
    {
        Probability,
    }
    private static int Probability;
    private static float KillCooldown;
    private static void SetupOptionItems()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionProbability = FloatOptionItem.Create(RoleInfo, 11, OptionName.Probability, new(0, 100, 5), 50, false)
            .SetValueFormat(OptionFormat.Percent);
    }
    public float CalculateKillCooldown() => KillCooldown;
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;
            int chance = IRandom.Instance.Next(1, 101);
            if (chance <= Probability)
            {
                Logger.Info($"{killer?.Data?.PlayerName}: フラ全体通知成功", "Noisemaker");
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    player.KillFlash();
                }
            }
            else
            {
                Logger.Info($"{killer?.Data?.PlayerName}: フラ全体通知失敗", "Noisemaker");
            }
        }
    }
}