using System.Linq;
using AmongUs.GameOptions;
using UnityEngine;
using System.Collections.Generic;
using TownOfHost.Roles.Core;
using HarmonyLib;

namespace TownOfHost.Roles.Crewmate;
public sealed class Comebacker : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Comebacker),
            player => new Comebacker(player),
            CustomRoles.Comebacker,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            25800,
            SetupOptionItem,
            "cb",
            "#ff9966",
            introSound: () => ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.StartFans).FirstOrDefault().MinigamePrefab.OpenSound
        );
    public Comebacker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Cooldown = OptionCooldown.GetFloat();
        Tp = new(999f, 999f);
    }
    private static OptionItem OptionCooldown;
    enum OptionName
    {
        Cooldown
    }
    private static float Cooldown;
    private Vector2 Tp;
    private static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.Cooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = Cooldown;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public override bool CantVentIdo(PlayerPhysics physics, int ventId) => false;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (Tp != new Vector2(999f, 999f))
        {
            var tp = Tp;
            _ = new LateTask(() =>
            {
                Player.RpcSnapToForced(tp);
                Logger.Info("ベントに飛ぶよ!", "Comebacker");
            }, 0.7f, "TP");
        }
        ShipStatus.Instance.AllVents.DoIf(vent => vent.Id == ventId, vent => Tp = (Vector2)vent.transform.position);
        Logger.Info("ベントを設定するよ!", "Comebacker");
        return true;
    }
}
