using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Roles.Crewmate;
public sealed class UltraStar : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(UltraStar),
            player => new UltraStar(player),
            CustomRoles.UltraStar,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            64030,
            SetupOptionItem,
            "us",
            "#ffff8e"
        );
    public UltraStar(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Speed = OptionSpeed.GetFloat();
        cankill = Optioncankill.GetBool();
        KillCool = Optionkillcool.GetFloat();
    }
    private static OptionItem OptionSpeed;
    private static OptionItem Optioncankill;
    private static OptionItem Optionkillcool;
    enum OptionName
    {
        Speed,
        UScankill,
        USkillcool
    }
    float colorchange;
    float outkill;
    private static float Speed;
    private static bool cankill;
    private static float KillCool;

    private static void SetupOptionItem()
    {
        OptionSpeed = FloatOptionItem.Create(RoleInfo, 9, OptionName.Speed, new(1.5f, 5f, 0.25f), 2.0f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        Optioncankill = BooleanOptionItem.Create(RoleInfo, 10, OptionName.UScankill, false, false);
        Optionkillcool = FloatOptionItem.Create(RoleInfo, 13, OptionName.USkillcool, new(0f, 180f, 2.5f), 30f, false, Optioncankill)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        //ホストじゃない or タスクターンじゃない or 生存していない ならブロック
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask || !player.IsAlive()) return;
        {//参考→https://github.com/Yumenopai/TownOfHost_Y/releases/tag/v514.20.3
            colorchange %= 18;
            if (colorchange is >= 0 and < 1) player.RpcSetColor(8);
            else if (colorchange is >= 1 and < 2) player.RpcSetColor(1);
            else if (colorchange is >= 2 and < 3) player.RpcSetColor(10);
            else if (colorchange is >= 3 and < 4) player.RpcSetColor(2);
            else if (colorchange is >= 4 and < 5) player.RpcSetColor(11);
            else if (colorchange is >= 5 and < 6) player.RpcSetColor(14);
            else if (colorchange is >= 6 and < 7) player.RpcSetColor(5);
            else if (colorchange is >= 7 and < 8) player.RpcSetColor(4);
            else if (colorchange is >= 8 and < 9) player.RpcSetColor(17);
            else if (colorchange is >= 9 and < 10) player.RpcSetColor(0);
            else if (colorchange is >= 10 and < 11) player.RpcSetColor(3);
            else if (colorchange is >= 11 and < 12) player.RpcSetColor(13);
            else if (colorchange is >= 12 and < 13) player.RpcSetColor(7);
            else if (colorchange is >= 13 and < 14) player.RpcSetColor(15);
            else if (colorchange is >= 14 and < 15) player.RpcSetColor(6);
            else if (colorchange is >= 15 and < 16) player.RpcSetColor(12);
            else if (colorchange is >= 16 and < 17) player.RpcSetColor(9);
            else if (colorchange is >= 17 and < 18) player.RpcSetColor(16);
            colorchange += Time.fixedDeltaTime * 2;
        }
        if (cankill)
        {
            outkill += Time.fixedDeltaTime;
            Vector2 GSpos = player.transform.position;

            PlayerControl target = null;
            var KillRange = 0.5;//GameOptionsData.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 1 / 1000)];

            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.PlayerId != player.PlayerId)
                {
                    float targetDistance = Vector2.Distance(GSpos, pc.transform.position);
                    if (targetDistance <= KillRange && player.CanMove && pc.CanMove)
                    {
                        target = pc;
                        break;
                    }
                }
            }
            if (target != null && cankill && (outkill >= KillCool + 5))
            {
                outkill = 5;//ラグの調整
                player.RpcResetAbilityCooldown();
                target.SetRealKiller(player);
                player.RpcMurderPlayer(target);
                Utils.MarkEveryoneDirtySettings();
                Utils.NotifyRoles();
                KillCoolCheck(player.PlayerId);
            }
        }
    }
    public override void AfterMeetingTasks()//あのままじゃホストだけキルクール回復するバグあったから
    {
        if (cankill)
        {
            Logger.Info("ウルトラスターのクールを戻す", "UltraStar");
            outkill = 0;
        }
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        Main.AllPlayerSpeed[Player.PlayerId] += Speed;//代入してたから修正()
    }

    public static void KillCoolCheck(byte playerId)
    {
        cankill = true;
        float EndTime = KillCool;
        var pc = Utils.GetPlayerById(playerId);

        _ = new LateTask(() =>
        {
            //ミーティング中なら無視。
            if (GameStates.IsMeeting) return;
            pc.RpcProtectedMurderPlayer();
            cankill = true;
        }, EndTime, "★");
    }
}