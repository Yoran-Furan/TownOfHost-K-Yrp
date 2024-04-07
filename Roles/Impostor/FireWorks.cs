using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;

public sealed class FireWorks : RoleBase, IImpostor, IUseTheShButton
{
    public enum FireWorksState
    {
        Initial = 1,
        SettingFireWorks = 2,
        WaitTime = 4,
        ReadyFire = 8,
        FireEnd = 16,
        CanUseKill = Initial | FireEnd
    }
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(FireWorks),
            player => new FireWorks(player),
            CustomRoles.FireWorks,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            1700,
            SetupCustomOption,
            "fw",
            from: From.TownOfHost
        );
    public FireWorks(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        FireWorksCount = OptionFireWorksCount.GetInt();
        FireWorksRadius = OptionFireWorksRadius.GetFloat();
        Cankill = OptionCankillAlltime.GetBool();
        Cool = OptionCooldown.GetFloat();
        FClick = true;
    }

    static OptionItem OptionFireWorksCount;
    static OptionItem OptionFireWorksRadius;
    static OptionItem OptionCankillAlltime;
    static OptionItem OptionCooldown;
    enum OptionName
    {
        FireWorksMaxCount,
        FireWorksRadius,
        FireWankillAlltime
    }

    int FireWorksCount;
    float FireWorksRadius;
    float Cool;
    int NowFireWorksCount;
    bool Cankill;
    bool FClick;
    List<Vector3> FireWorksPosition = new();
    FireWorksState State = FireWorksState.Initial;

    public static void SetupCustomOption()
    {
        OptionFireWorksCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.FireWorksMaxCount, new(1, 5, 1), 1, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionFireWorksRadius = FloatOptionItem.Create(RoleInfo, 11, OptionName.FireWorksRadius, new(0.5f, 3f, 0.5f), 1f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionCankillAlltime = BooleanOptionItem.Create(RoleInfo, 13, OptionName.FireWankillAlltime, false, false);
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 14, GeneralOption.Cooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Add()
    {
        NowFireWorksCount = FireWorksCount;
        FireWorksPosition.Clear();
        State = FireWorksState.Initial;
    }

    public bool CanUseKillButton()
    {
        if (Cankill) return true;
        if (!Player.IsAlive()) return false;
        return (State & FireWorksState.CanUseKill) != 0;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterDuration = 1f;
        AURoleOptions.ShapeshifterCooldown = Cool;
    }
    public bool UseOCButton => true;
    public void OnClick()
    {
        if (FClick)
        {
            FClick = false;
            return;
        }
        Logger.Info($"FireWorks ShapeShift", "FireWorks");
        switch (State)
        {
            case FireWorksState.Initial:
            case FireWorksState.SettingFireWorks:
                Logger.Info("花火を一個設置", "FireWorks");
                FireWorksPosition.Add(Player.transform.position);
                NowFireWorksCount--;
                if (NowFireWorksCount == 0)
                    State = Main.AliveImpostorCount <= 1 ? FireWorksState.ReadyFire : FireWorksState.WaitTime;
                else
                    State = FireWorksState.SettingFireWorks;
                break;
            case FireWorksState.ReadyFire:
                Logger.Info("花火を爆破", "FireWorks");
                if (AmongUsClient.Instance.AmHost)
                {
                    //爆破処理はホストのみ
                    bool suicide = false;
                    foreach (var fireTarget in Main.AllAlivePlayerControls)
                    {
                        foreach (var pos in FireWorksPosition)
                        {
                            var dis = Vector2.Distance(pos, fireTarget.transform.position);
                            if (dis > FireWorksRadius) continue;

                            if (fireTarget == Player)
                            {
                                //自分は後回し
                                suicide = true;
                            }
                            else
                            {
                                PlayerState.GetByPlayerId(fireTarget.PlayerId).DeathReason = CustomDeathReason.Bombed;
                                fireTarget.SetRealKiller(Player);
                                fireTarget.RpcMurderPlayer(fireTarget);
                            }
                        }
                    }
                    if (suicide)
                    {
                        var totalAlive = Main.AllAlivePlayerControls.Count();
                        //自分が最後の生き残りの場合は勝利のために死なない
                        if (totalAlive != 1)
                        {
                            MyState.DeathReason = CustomDeathReason.Misfire;
                            Player.RpcMurderPlayer(Player);
                        }
                    }
                    Player.MarkDirtySettings();
                }
                State = FireWorksState.FireEnd;
                break;
            default:
                break;
        }
        Utils.NotifyRoles();
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        string retText = "";

        if (State == FireWorksState.WaitTime && Main.AliveImpostorCount <= 1)
        {
            Logger.Info("爆破準備OK", "FireWorks");
            State = FireWorksState.ReadyFire;
            Utils.NotifyRoles();
        }
        switch (State)
        {
            case FireWorksState.Initial:
            case FireWorksState.SettingFireWorks:
                retText = string.Format(GetString("FireworksPutPhase"), NowFireWorksCount);
                break;
            case FireWorksState.WaitTime:
                retText = GetString("FireworksWaitPhase");
                break;
            case FireWorksState.ReadyFire:
                retText = GetString("FireworksReadyFirePhase");
                break;
            case FireWorksState.FireEnd:
                break;
        }
        return retText;
    }
    public override string GetAbilityButtonText()
    {
        if (State == FireWorksState.ReadyFire)
            return GetString("FireWorksExplosionButtonText");
        else
            return GetString("FireWorksInstallAtionButtonText");
    }
}