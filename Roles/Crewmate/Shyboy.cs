using AmongUs.GameOptions;
using UnityEngine;
using System;
using TownOfHost.Roles.Core;
namespace TownOfHost.Roles.Crewmate;
public sealed class Shyboy : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Shyboy),
            player => new Shyboy(player),
            CustomRoles.Shyboy,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            22015,
            SetupOptionItem,
            "Sy",
            "#00fa9a",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Shyboy(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        tuuti = true;
        Shytime = OptionShytime.GetFloat();
        Notshy = OptionNotShy.GetFloat();
        Shydeath = 0;
        AfterMeeting = 0;
    }
    private static OptionItem OptionShytime;
    private static OptionItem OptionNotShy;
    float Shydeath;
    float Cool;
    float AfterMeeting;
    bool tuuti;
    float Last;
    private static float Notshy;
    enum OptionName
    {
        Shytime,
        NotShy
    }
    private static float Shytime;
    private static void SetupOptionItem()
    {
        OptionShytime = FloatOptionItem.Create(RoleInfo, 10, OptionName.Shytime, new(0f, 15f, 0.5f), 5f, false);
        OptionNotShy = FloatOptionItem.Create(RoleInfo, 11, OptionName.NotShy, new(0f, 30f, 1f), 10f, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        //ししゃごにゅー
        double Coold = Math.Round(Shytime + 1 / 4 - Shydeath);
        AURoleOptions.EngineerCooldown = (float)Coold;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId) => false;
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        Cool += Time.fixedDeltaTime;
        if (Player.IsAlive() && Cool >= 0.25)
        {
            Cool = 0;
            //シャイのクールｳﾙｾｪからログださない()()バグ起こったらここtrueか削除して探そう!!((((
            //30回に1回だけとかlog残すか考えたけど余計重くなりそう。
            var cooldown = (float)Math.Round(Shytime + 1 / 4 - Shydeath);
            if (Last != cooldown) //必要な時だけ送る
            {
                Last = cooldown;
                Player.MarkDirtySettings();
            }
            Player.RpcResetAbilityCooldown(log: false);
        }
        AfterMeeting += Time.fixedDeltaTime;

        var Shydeathdi = 5 * Main.DefaultCrewmateVision;
        if (GameStates.IsInTask && Player.IsAlive() && Notshy <= AfterMeeting - 5)
        {
            if (tuuti)
            {
                tuuti = false;
                Player.RpcProtectedMurderPlayer();
            }

            Vector2 GSpos = player.transform.position;
            bool Hito = false;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc != player)
                {
                    float HitoDistance = Vector2.Distance(GSpos, pc.transform.position);
                    if (HitoDistance <= Shydeathdi && player.CanMove && pc.CanMove)
                    {
                        Hito = true;
                        break;
                    }
                }
            }
            if (Hito)//周囲に人がいる状況
            {
                Shydeath += Time.fixedDeltaTime;
            }
            else
            {
                Shydeath -= Time.fixedDeltaTime * 1 / 4;//周囲に人がいないとカウントをちょっとずつ減らす
            }

            if (Shydeath <= -0.25f)//値がマイナスにならないようにする
            {
                Shydeath = 0;
            }

            if (Shytime <= Shydeath)
            {
                Logger.Info("もぉみんなかまうからシャイ君しんぢゃったぁ～!", "Shyboy");
                PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Suicide;
                Player.RpcMurderPlayer(Player);//一定時間周囲に人がいたら恥ずかしくて死ぬ。
                Shydeath = 0;//0sの無限キル防止(おきないだろうけど)
            }
        }
    }
    public override void AfterMeetingTasks()
    {
        tuuti = true;
        Logger.Info("シャイクールを直す", "Shyboy");
        Shydeath = 0;//会議明け修正
        AfterMeeting = 0;
    }
    //いつか可視化を顔文字でしたい!!!
}