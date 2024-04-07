using AmongUs.GameOptions;
using Hazel;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class Decrescendo : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Decrescendo),
            player => new Decrescendo(player),
            CustomRoles.Decrescendo,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            70050,
            SetupOptionItem,
            "De"
        );
    public Decrescendo(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        KillerCount = 0;
        Killcount = KillerCountopt.GetInt();
        Yowakunaru = false;
        CanVent = OptionCanVent.GetBool();
        Jav = OptionJaHasImpostorVision.GetBool();
        SaidaiCooldown = OptionSaidaiCooldown.GetFloat();
        Minsikai = OptionMinsikai.GetFloat();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem KillerCountopt;
    public static OptionItem OptionCanVent;
    private static OptionItem OptionSaidaiCooldown;
    private static OptionItem OptionJaHasImpostorVision;
    private static OptionItem OptionMinsikai;
    public static bool CanVent;
    enum OptionName
    {
        KillerCount,
        DeCanvent,
        DeCanImpVi,
        SaidaiCooldown,
        Minsikai
    }
    /// <summary>キル数</summary>
    public int KillerCount;
    /// <summary>弱化開始キル数</summary>
    public int Killcount;
    /// <summary>デフォのクール</summary>
    private static float KillCooldown;
    /// <summary>最大クール</summary>
    private static float SaidaiCooldown;
    static bool Jav;
    private static float Minsikai;
    public bool CanBeLastImpostor { get; } = false;
    private static bool Yowakunaru;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 9, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionSaidaiCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.SaidaiCooldown, new(0f, 180f, 2.5f), 60f, false)
        .SetValueFormat(OptionFormat.Seconds);
        KillerCountopt = IntegerOptionItem.Create(RoleInfo, 11, OptionName.KillerCount, new(1, 10, 1), 4, false)
            .SetValueFormat(OptionFormat.Times);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 12, OptionName.DeCanvent, false, false);
        OptionJaHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, OptionName.DeCanImpVi, false, false);
        OptionMinsikai = FloatOptionItem.Create(RoleInfo, 14, OptionName.Minsikai, new(0.0f, 1.0f, 0.25f), 0.05f, false, OptionJaHasImpostorVision);
    }
    public float CalculateKillCooldown()
    {
        if (!Yowakunaru) return KillCooldown;
        else
        {
            var cool = KillCooldown * Killcount * (KillerCount - Killcount + 3) / (Killcount + 1);
            if (cool <= SaidaiCooldown) cool = SaidaiCooldown;
            return cool;
        }
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        KillCooldown = OptionKillCooldown.GetFloat();

        Killcount = KillerCountopt.GetInt();
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 後{KillerCount}発", "Decrescendo");
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(KillerCount);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        KillerCount = reader.ReadInt32();
    }
    //public bool CanUseKillButton() => Player.IsAlive() && KillerCount > 0;
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.CanKill) return;
        if (!AmongUsClient.Instance.AmHost) return;
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;
            KillerCount++;
            Logger.Info($"{killer.GetNameWithRole()} : 残り{Killcount - KillerCount}発", "Decrescendo");
            SendRPC();
            if (KillerCount >= Killcount)
            {
                //初期キルクール →ａ, キル数 →ｂ,
                //弱化開始キル数 →ｃ
                //ａ×ｃ×（ｂ-ｃ+３）／（ｃ+１）
                Yowakunaru = true;
                {
                    var cool = KillCooldown * Killcount * (KillerCount - Killcount + 3) / (Killcount + 1);
                    if (cool >= SaidaiCooldown) cool = SaidaiCooldown;
                    Main.AllPlayerKillCooldown[killer.PlayerId] = cool;
                    killer.SyncSettings();//キルクール処理を同期
                }
            }
        }
        return;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive() || !Yowakunaru || !Jav) return;//死んでる or 弱化してない or インポしかいノママ
        var Light = FloatOptionNames.ImpostorLightMod;
        //ｄ×{1-(ｂ-ｃ+１)/(１１-ｃ)}
        float b = KillerCount;
        float c = Killcount;
        float sikai = Main.DefaultImpostorVision * (10 - b) / (11 - c);
        Logger.Info($"{sikai}= {Main.DefaultImpostorVision} * (10 - {b}) / (11 - {c})", "de");
        if (sikai <= Minsikai) sikai = Minsikai;
        opt.SetFloat(Light, (float)sikai);
    }

    public bool CanUseImpostorVentButton()
    {
        if (CanVent && Yowakunaru) return false;
        else return true;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(!Yowakunaru ? Color.red : Color.gray, !Yowakunaru ? $"({Killcount - KillerCount})" : $"(´・ω・｀)");
}