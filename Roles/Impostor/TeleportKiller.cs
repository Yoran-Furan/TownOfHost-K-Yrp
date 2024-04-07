using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using System.Collections.Generic;
using HarmonyLib;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class TeleportKiller : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(TeleportKiller),
            player => new TeleportKiller(player),
            CustomRoles.TeleportKiller,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            60015,
            SetupOptionItem,
            "tk"
        );
    public TeleportKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.OnEnterVentOthers.Add(OnEnterVentOthers);
        KillCooldown = OptionKillCoolDown.GetFloat();
        Cooldown = OptionCoolDown.GetFloat();
        Maximum = OptionmMaximum.GetFloat();
        Duration = OptionDuration.GetFloat();
        Ventgaaa = Optionventgaaa.GetBool();
        PlatformFall = OptionPlatformFall.GetBool();
        LadderFall = OptionLadderFall.GetBool();
        //ZiplineFall = OptionZiplineFall.GetBool(); ziplineってどうやってチェックするの..
        Dokkaaaan = OptionDokkaaaan.GetBool();
        //LeaveSkin = OptionLeaveSkin.GetBool();
        TpKillCooldownReset = OptionTpKillCooldownReset.GetBool();
        DeathReason = OptionDeathReason.GetBool();
        usecount = 0;
        TeleportandKill = new();
        LadderPatch.Ladder.Clear();
        isAnimation = false;
        CheckVentD.Clear();
    }
    enum OptionName
    {
        KillCooldown,
        Cooldown,
        MaximumT,
        Duration,
        Jibakukei,
        Ventgaaa,
        PlatformFall,
        LadderFall,
        //ZiplineFall,
        Dokkaaaan,
        //LeaveSkin,
        TpKillCooldownReset,
        TKChangeDeathReason
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionCoolDown;
    static OptionItem OptionmMaximum;
    static OptionItem OptionDuration;
    static OptionItem OptionJibakukei;
    static OptionItem Optionventgaaa;
    static OptionItem OptionPlatformFall;
    static OptionItem OptionLadderFall;
    //static OptionItem OptionZiplineFall;
    static OptionItem OptionDokkaaaan;
    //static OptionItem OptionLeaveSkin;
    static OptionItem OptionTpKillCooldownReset;
    static OptionItem OptionDeathReason;
    static float KillCooldown;
    static float Cooldown;
    static float Maximum;
    int usecount;
    static float Duration;
    static bool Ventgaaa; //↓ターゲットが使ってると自爆する系
    static bool PlatformFall;
    static bool LadderFall;
    //static bool ZiplineFall;
    static bool Dokkaaaan; //ターゲットが死んでいると自爆する
    //static bool LeaveSkin;
    static bool TpKillCooldownReset;
    static bool DeathReason;
    List<byte> TeleportandKill;
    bool isAnimation;
    (Vector2, Vector2, float) AnimationData;
    static Dictionary<byte, int> CheckVentD = new();
    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, OptionName.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCoolDown = FloatOptionItem.Create(RoleInfo, 11, OptionName.Cooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionmMaximum = FloatOptionItem.Create(RoleInfo, 12, OptionName.MaximumT, new(0f, 999, 1f), 0f, false)
            .SetValueFormat(OptionFormat.Times);
        OptionDuration = FloatOptionItem.Create(RoleInfo, 13, OptionName.Duration, new(0f, 15, 1f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionJibakukei = BooleanOptionItem.Create(RoleInfo, 14, OptionName.Jibakukei, false, false);
        Optionventgaaa = BooleanOptionItem.Create(RoleInfo, 15, OptionName.Ventgaaa, false, false).SetParent(OptionJibakukei);
        OptionPlatformFall = BooleanOptionItem.Create(RoleInfo, 16, OptionName.PlatformFall, false, false).SetParent(OptionJibakukei);
        OptionLadderFall = BooleanOptionItem.Create(RoleInfo, 17, OptionName.LadderFall, false, false).SetParent(OptionJibakukei);
        //OptionZiplineFall = BooleanOptionItem.Create(RoleInfo, 18, OptionName.ZiplineFall, false, false).SetParent(OptionJibakukei);
        OptionDokkaaaan = BooleanOptionItem.Create(RoleInfo, 19, OptionName.Dokkaaaan, false, false).SetParent(OptionJibakukei);
        //OptionLeaveSkin = BooleanOptionItem.Create(RoleInfo, 15, OptionName.LeaveSkin, false, false);
        OptionTpKillCooldownReset = BooleanOptionItem.Create(RoleInfo, 20, OptionName.TpKillCooldownReset, false, false);
        OptionDeathReason = BooleanOptionItem.Create(RoleInfo, 21, OptionName.TKChangeDeathReason, false, false);
    }

    public bool CanBeLastImpostor { get; } = false;
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(usecount);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        usecount = reader.ReadInt32();
    }
    public override void OnShapeshift(PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost || Is(target) || (!target.IsAlive() && !Dokkaaaan) || (usecount >= Maximum && Maximum != 0)) return;
        usecount++;
        SendRPC();
        Logger.Info($"Player: {Player.name},Target: {target.name}, count: {usecount}", "TeleportKiller");
        _ = new LateTask(() =>
        {
            if (!target.IsAlive() && Dokkaaaan)
            {
                Logger.Info($"ターゲットが生きてないから自爆☆ Killer:{Player.name} Target:{target.name}", "TeleportKiller");
                PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Bombed;
                Player.RpcMurderPlayer(Player);
                return;
            }
            if (!TPCheck(target, true))
            {
                Logger.Info($"ターゲットはキル可能な状態ではないためキルがブロックされました Killer:{Player.name} Target:{target.name}", "TeleportKiller");
                Player.RpcProtectedMurderPlayer();
                if ((target.inVent || target.MyPhysics.Animations.IsPlayingEnterVentAnimation())
                        && Ventgaaa)
                {
                    Player.RpcSnapToForced(target.transform.position);
                    PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Bombed;
                    Player.RpcMurderPlayer(Player, true);
                    Logger.Info($"ターゲットがベントに入ってたせいでTPした時ベントに体があああ(自爆) Killer:{Player.name} Target:{target.name}", "TeleportKiller");
                    return;
                }
                if (!target.IsAlive()) return;
                Logger.Info($"キル待機中", "TeleportKiller");
                TeleportandKill.Add(target.PlayerId);
            }
            else
            {
                TeleportKill(Player, target);
            }
        }, 1.5f, "TeleportKiller-1");
    }
    public static bool TPCheck(PlayerControl target, bool KillerTP = false)
    {
        if (target.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
        {
            if (!KillerTP) return false;
            return LadderFall;
        }

        if (target.inMovingPlat)
        {
            if (!KillerTP) return false;
            return PlatformFall;
        }

        if (target.MyPhysics.Animations.IsPlayingEnterVentAnimation()
                || target.inVent)
        {
            if (!KillerTP) return false;
            return !Ventgaaa;
        }

        if (!target.IsAlive()) return false;

        return true;
    }

    public void TeleportKill(PlayerControl Player, PlayerControl target)
    {
        //キラーのTP
        var p = Player.transform.position;
        var check = TPCheck(target);
        if ((target.inVent || target.MyPhysics.Animations.IsPlayingEnterVentAnimation()) && !Ventgaaa)
        {
            target.MyPhysics.RpcBootFromVent(CheckVentD[target.PlayerId]);
            Logger.Info($"ベントでもキルするのだ", "TeleportKiller");
            _ = new LateTask(() => TeleportandKill.Add(target.PlayerId), 1.5f);
            check = false;
        }
        else
        {
            Player.RpcSnapToForced(target.transform.position);
        }
        if (check)
        {
            //ターゲットのTP
            target.RpcSnapToForced(p);
            _ = new LateTask(() =>
            {
                if (!target.inVent && !target.MyPhysics.Animations.IsPlayingEnterVentAnimation())
                {
                    if (target.GetCustomRole().IsImpostor()) return;
                    PlayerState.GetByPlayerId(target.PlayerId).DeathReason = DeathReason ? CustomDeathReason.TeleportKill : CustomDeathReason.Kill;
                    target.SetRealKiller(Player);
                    target.RpcMurderPlayer(target, true);
                    if (TpKillCooldownReset) Player.SetKillCooldown(KillCooldown);
                }
            }, 0.5f, "TeleportKiller-2");
        }

        if (target.MyPhysics.Animations.IsPlayingAnyLadderAnimation() || target.inMovingPlat)
        {
            var start = Player.transform.position;
            var goal = target.inMovingPlat ? (Vector2)Player.transform.position - new Vector2(0, 4) : new Vector2(Player.transform.position.x, LadderPatch.Ladder[target.PlayerId].y);
            var t = 0.0f;
            AnimationData = (start, goal, t);
            isAnimation = true;
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (TeleportandKill.Count != 0)
        {
            foreach (var targetid in TeleportandKill)
            {
                var target = Utils.GetPlayerById(targetid);
                if (TPCheck(target))
                {
                    TeleportKill(Player, target);
                    TeleportandKill.Remove(targetid);
                }
            }
        }
        if (isAnimation)
        {
            var (start, goal, t) = AnimationData;
            t += Time.deltaTime / 2.0f;
            AnimationData.Item3 = (t > 1.0f) ? 1.0f : t; // 上限は1.0
            Player.RpcSnapToForced(Vector2.Lerp(start, goal, t));
            if (t >= 1)
            {
                isAnimation = false;
                PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Fall;
                Player.RpcMurderPlayer(Player, true);
                Logger.Info($"TPした先には足場がぁ!? うあああ落ちるうう(落下死) Killer:{Player.name}", "TeleportKiller");
            }
        }
    }


    public static bool OnEnterVentOthers(PlayerPhysics physics, int id)
    {
        CheckVentD[physics.myPlayer.PlayerId] = id;
        return true;
    }
    public override string GetProgressText(bool comms = false) => Maximum == 0 ? "" : Utils.ColorString(Maximum >= usecount ? Color.red : Color.gray, $"({Maximum - usecount})");

    public float CalculateKillCooldown() => KillCooldown;

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = Cooldown;
        //AURoleOptions.ShapeshifterLeaveSkin = LeaveSkin;
        AURoleOptions.ShapeshifterDuration = Duration;
        AURoleOptions.KillCooldown = KillCooldown;
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.ClimbLadder))]
    class LadderPatch
    {
        public static Dictionary<byte, Vector2> Ladder = new();
        public static void Postfix(PlayerPhysics __instance, Ladder source, byte climbLadderSid)
        {
            var sourcePos = source.transform.position;
            var targetPos = source.Destination.transform.position;
            if (sourcePos.y > targetPos.y)
                Ladder[__instance.myPlayer.PlayerId] = targetPos;
            else
                Ladder[__instance.myPlayer.PlayerId] = sourcePos;
        }
    }
}