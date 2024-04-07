using AmongUs.GameOptions;
using System.Collections.Generic;
using UnityEngine;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class VentOpener : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(VentOpener),
            player => new VentOpener(player),
            CustomRoles.VentOpener,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            22370,
            SetupOptionItem,
            "vo",
            "#fbe000",
            introSound: () => GetIntroSound(RoleTypes.Engineer)
        );
    public VentOpener(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.OnEnterVentOthers.Add(OnEnterVentOthers);
        ventid.Clear();
        count = OptionCount.GetFloat();
        cooldown = OptionCooldown.GetInt();
        Defo = count is 0;
        fuhatu = !OptinoFuhatu.GetBool();
        Imp = OptionImp.GetBool();
        Mad = OptionMad.GetBool();
        Crew = OptionCrew.GetBool();
        Neutral = OptionNeutral.GetBool();
        taskc = OptionCanTaskcount.GetFloat();
    }

    private static OptionItem OptionCount;
    private static OptionItem OptionCooldown;
    private static OptionItem OptinoFuhatu;
    private static OptionItem OptionImp;
    private static OptionItem OptionCrew;
    private static OptionItem OptionMad;
    private static OptionItem OptionNeutral;
    private static OptionItem OptionCanTaskcount;
    static int cooldown;
    static bool fuhatu;
    static bool Imp;
    static bool Crew;
    static bool Mad;
    static bool Neutral;
    static bool Defo;
    static float taskc;
    float count;

    static Dictionary<byte, int> ventid = new();

    enum OptionName
    {
        vocount,
        Cooldown,
        Fuhatu,
        VoImp,
        VoMad,
        VoCrew,
        VoNeutral,
        cantaskcount
    }

    private static void SetupOptionItem()
    {
        OptionCount = FloatOptionItem.Create(RoleInfo, 10, OptionName.vocount, new(0, 30, 1), 3, false);
        OptionCooldown = IntegerOptionItem.Create(RoleInfo, 11, OptionName.Cooldown, new(0, 999, 1), 30, false);
        OptinoFuhatu = BooleanOptionItem.Create(RoleInfo, 12, OptionName.Fuhatu, false, false);
        OptionImp = BooleanOptionItem.Create(RoleInfo, 13, OptionName.VoImp, true, false);
        OptionMad = BooleanOptionItem.Create(RoleInfo, 14, OptionName.VoMad, true, false);
        OptionCrew = BooleanOptionItem.Create(RoleInfo, 15, OptionName.VoCrew, true, false);
        OptionNeutral = BooleanOptionItem.Create(RoleInfo, 16, OptionName.VoNeutral, true, false);
        OptionCanTaskcount = FloatOptionItem.Create(RoleInfo, 17, OptionName.cantaskcount, new(0, 99, 1), 5, false);
    }

    public static bool OnEnterVentOthers(PlayerPhysics physics, int ventId)
    {
        ventid[physics.myPlayer.PlayerId] = ventId;
        return true;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (count <= 0 && !Defo && MyTaskState.CompletedTasksCount < taskc) return false;
        bool check = false;
        foreach (var (playerid, id) in ventid)
        {
            var pc = Utils.GetPlayerById(playerid);
            var role = pc.GetCustomRole();
            if (pc.inVent //ベントに入ってるか ↓設定とかのチェック
                 && ((role.IsImpostor() && Imp)
                 || (role.IsMadmate() && Mad)
                 || (role.IsCrewmate() && Crew)
                 || (role.IsNeutral() && Neutral)))
            {
                pc.MyPhysics?.RpcBootFromVent(id);
                check = true;
            }
        }
        if (check)
            Player.KillFlash(false);
        if ((check || fuhatu) && count > 0)
            count--;
        _ = new LateTask(() => Player.MyPhysics?.RpcBootFromVent(ventId), 0.5f);
        Utils.NotifyRoles(false, Player);
        return false;
    }
    public override bool CantVentIdo(PlayerPhysics physics, int ventId) => false;
    public override string GetProgressText(bool comms = false) => Defo ? "" : Utils.ColorString(count > 0 && MyTaskState.CompletedTasksCount >= taskc ? RoleInfo.RoleColor : Color.red, $"({count})");

    public override void OnVentilationSystemUpdate(PlayerControl user, VentilationSystem.Operation Operation, int ventId)
    {
        if (Operation != VentilationSystem.Operation.Move) return;
        ventid[user.PlayerId] = ventId;
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = cooldown;
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
}