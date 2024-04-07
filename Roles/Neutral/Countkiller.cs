using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;

public sealed class CountKiller : RoleBase, ILNKiller, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(CountKiller),
            player => new CountKiller(player),
            CustomRoles.CountKiller,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            60100,
            SetupOptionItem,
            "ck",
            "#FF1493",
            true,
            assignInfo: new RoleAssignInfo(CustomRoles.CountKiller, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            }
        );
    public CountKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        VictoryCount = OptionVictoryCount.GetInt();
        KillCooldown = OptionKillCooldown.GetFloat();
        CanVent = OptionCanVent.GetBool();
        HasImpostorVision = OptionHasImpostorVision.GetBool();
        KillCount = 0;
    }
    static OptionItem OptionKillCooldown;
    private static OptionItem OptionVictoryCount;
    private static OptionItem OptionHasImpostorVision;
    public static OptionItem OptionCanVent;

    enum OptionName
    {
        VictoryCount
    }
    private int VictoryCount;
    public static bool CanVent;
    private static bool HasImpostorVision;
    private static float KillCooldown;
    int KillCount = 0;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionVictoryCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.VictoryCount, new(1, 10, 1), 5, false)
        .SetValueFormat(OptionFormat.Times);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanVent, true, false);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
    }
    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.CountKiller;
    public float CalculateKillCooldown() => KillCooldown;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
    public override void Add()
    {
        var playerId = Player.PlayerId;
        KillCooldown = OptionKillCooldown.GetFloat();

        VictoryCount = OptionVictoryCount.GetInt();
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 後{VictoryCount - KillCount}発", "CountKiller");
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(VictoryCount);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        VictoryCount = reader.ReadInt32();
    }
    public bool CanUseKillButton() => Player.IsAlive() && VictoryCount > 0;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => CanVent;
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;
            if (target.Is(CustomRoles.SchrodingerCat))
            {
                SchrodingerPatch.SAddPlayer(killer, target);
                return;
            }
            KillCount++;
            Logger.Info($"{killer.GetNameWithRole()} : 残り{VictoryCount - KillCount}発", "CountKiller");
            SendRPC();
            killer.ResetKillCooldown();

            if (KillCount >= VictoryCount) Win();
        }
        return;
    }
    public void Win()
    {
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CountKiller);
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
    }
    public override string GetProgressText(bool comms = false)
    => Utils.ColorString(RoleInfo.RoleColor, $"({KillCount}/{VictoryCount})");
}