using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;
public sealed class Magician : RoleBase, IImpostor, IUseTheShButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Magician),
            player => new Magician(player),
            CustomRoles.Magician,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            60177,
            SetupOptionItem,
            "mc"
        );
    public Magician(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        DefaultCooldown = OptionMagicCooldown.GetFloat();
        Maximum = OptionMaximum.GetFloat();
        Radius = OptionRadius.GetFloat();
        show = OptionShow.GetBool();
        rt = OptionRt.GetFloat();
        ResetM = OptionResetM.GetBool();
        ResetL = OptionResetL.GetBool();
        MagicCooldown = DefaultCooldown;
        count = 0;
        killc = 0;
        KillList.Clear();
    }

    static OptionItem OptionMagicCooldown;
    static OptionItem OptionMaximum;
    static OptionItem OptionRadius;
    static OptionItem OptionShow;
    static OptionItem OptionRt;
    static OptionItem OptionResetM;
    static OptionItem OptionResetL;

    float MagicCooldown;
    float DefaultCooldown;
    float Maximum;
    float Radius;
    bool show; //会議で死亡してるかを表示するか
    float rt; //必要なキル数
    bool ResetM; //←↓会議後リセットするかの設定 | キル数
    bool ResetL;// マジックで消す予定の人

    float count;
    float killc;
    List<byte> KillList = new();

    enum Option
    {
        Cooldown,
        MaximumM,
        MagicianRadius,
        ShowD,
        rtM,
        MagicianResetM,
        MagicianResetL,
    }

    public static void SetupOptionItem()
    {
        OptionMagicCooldown = FloatOptionItem.Create(RoleInfo, 10, Option.Cooldown, new FloatValueRule(0, 120, 1), 15, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionMaximum = FloatOptionItem.Create(RoleInfo, 11, Option.MaximumM, new FloatValueRule(0, 99, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OptionRadius = FloatOptionItem.Create(RoleInfo, 12, Option.MagicianRadius, new(0.5f, 3f, 0.5f), 1.5f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionRt = FloatOptionItem.Create(RoleInfo, 13, Option.rtM, new FloatValueRule(0, 99, 1), 1, false)
        .SetValueFormat(OptionFormat.Players);
        OptionShow = BooleanOptionItem.Create(RoleInfo, 14, Option.ShowD, false, false);
        OptionResetM = BooleanOptionItem.Create(RoleInfo, 15, Option.MagicianResetM, true, false);
        OptionResetL = BooleanOptionItem.Create(RoleInfo, 16, Option.MagicianResetL, false, false);
    }

    public override void AfterMeetingTasks()
    {
        if (ResetM) killc = 0;
        if (ResetL) KillList.Clear();
        //クールダウンリセット
        if (count >= Maximum && Maximum != 0)
            MagicCooldown = 999;
        else
            MagicCooldown = DefaultCooldown;
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
        => killc++;

    public void OnClick()
    {
        if (count >= Maximum && Maximum != 0) return;
        Dictionary<PlayerControl, float> distance = new();
        float dis;
        bool check = false;
        foreach (var p in Main.AllAlivePlayerControls)
        {
            dis = Vector2.Distance(Player.transform.position, p.transform.position);
            //↑プレイヤーとの距離 ↓自分以外、で近くにいて 既にターゲットにされていないか
            if (p.PlayerId != Player.PlayerId && dis < Radius && !KillList.Contains(p.PlayerId))
            {
                distance.Add(p, dis);
            }
        }
        if (distance.Count != 0)
        {
            var min = distance.OrderBy(c => c.Value).FirstOrDefault();
            var target = min.Key;

            count++;
            check = (count >= Maximum && Maximum != 0) || MagicCooldown != DefaultCooldown;
            KillList.Add(target.PlayerId);
            Player.RpcProtectedMurderPlayer(target);
            MagicCooldown = (count >= Maximum && Maximum != 0) ? 999 : DefaultCooldown;
            _ = new LateTask(() => Utils.NotifyRoles(Player), 0.1f);
        }
        else
        {
            check = MagicCooldown != 0;
            MagicCooldown = 0;
        }
        if (check)
        {
            Player.MarkDirtySettings();
            Player.RpcResetAbilityCooldown();
        }
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (KillList.Count == 0 || killc < rt) return;
        foreach (var id in KillList)
        {
            var target = Utils.GetPlayerById(id);
            if (!target.IsAlive()) continue;
            var state = PlayerState.GetByPlayerId(target.PlayerId);
            Player.RpcProtectedMurderPlayer(target);
            if (!show)
                target.RpcExileV2();
            else
            {
                var position = target.transform.position;
                target.RpcSnapToForced(new Vector2(9999f, 9999f));
                target.RpcMurderPlayer(target, true);
                _ = new LateTask(() => target.RpcSnapToForced(position), 0.5f);
            }
            state.DeathReason = CustomDeathReason.Magic;
            state.SetDead();
        }
        KillList.Clear();
        killc -= rt;
        Player.SetKillCooldown();
        _ = new LateTask(() => (Player.GetRoleClass() as IUseTheShButton)?.ResetS(Player), 0.3f);
        _ = new LateTask(() => Utils.NotifyRoles(Player), 0.1f);
    }

    public override string GetAbilityButtonText() => GetString("MagicButtonText");

    public override string GetProgressText(bool comms = false)
    {
        if (Maximum == 0 && rt == 0) return "";
        var text = "(";
        if (Maximum != 0) text += Maximum - count;
        if (rt != 0) text += (Maximum != 0 ? " | " : "") + $"{killc}/{rt}";
        var color = killc < rt ? Color.yellow : count < Maximum && Maximum != 0 ? Color.red : Color.gray;
        return Utils.ColorString(color, text + ")");
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = MagicCooldown;
        AURoleOptions.ShapeshifterDuration = MagicCooldown;
    }
}