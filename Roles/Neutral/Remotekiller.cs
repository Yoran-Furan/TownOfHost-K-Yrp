using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral
{
    public sealed class Remotekiller : RoleBase, ILNKiller, ISchrodingerCatOwner
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Remotekiller),
                player => new Remotekiller(player),
                CustomRoles.Remotekiller,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                50925,
                SetupOptionItem,
                "rk",
                "#8f00ce",
                true,
                introSound: () => GetIntroSound(RoleTypes.Impostor),
                countType: CountTypes.Remotekiller,
                assignInfo: new RoleAssignInfo(CustomRoles.Remotekiller, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
                }
            );
        public Remotekiller(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = RKillCooldown.GetFloat();
            Rtarget = 111;
        }

        private static OptionItem RKillCooldown;
        private static OptionItem RKillAnimation;
        private byte Rtarget;
        private static float KillCooldown;

        public bool CanBeLastImpostor { get; } = false;
        enum OptionName
        {
            KillAnimation
        }

        private static void SetupOptionItem()
        {
            RKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            RKillAnimation = BooleanOptionItem.Create(RoleInfo, 11, OptionName.KillAnimation, true, false);
        }
        public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Remotekiller;
        public float CalculateKillCooldown() => KillCooldown;
        public override bool OnInvokeSabotage(SystemTypes systemType) => false;

        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (!info.CanKill) return; //キル出来ない相手には無効
            var (killer, target) = info.AttemptTuple;

            if (target.Is(CustomRoles.Bait)) return;
            if (info.IsFakeSuicide) return;
            //登録
            killer.SetKillCooldown(KillCooldown);
            Rtarget = target.PlayerId;
            killer.RpcProtectedMurderPlayer(target);
            info.DoKill = false;
        }
        public override void OnReportDeadBody(PlayerControl _, GameData.PlayerInfo __)
        {
            Rtarget = 111;
        }
        public bool OverrideKillButtonText(out string text)
        {
            text = GetString("rkTargetButtonText");
            return true;
        }
        public override bool OnEnterVent(PlayerPhysics physics, int ventId)
        {
            var user = physics.myPlayer;
            if (Rtarget != 111 && Player.PlayerId == user.PlayerId)
            {
                var target = Utils.GetPlayerById(Rtarget);
                if (!target.IsAlive()) return true;
                if (RKillAnimation.GetBool())
                {
                    _ = new LateTask(() =>
                    {
                        target.SetRealKiller(user);
                        user.RpcMurderPlayer(target, true);
                    }, 1f);
                }
                else
                {
                    target.SetRealKiller(user);
                    target.RpcMurderPlayer(target, true);
                }

                RPC.PlaySoundRPC(user.PlayerId, Sounds.KillSound);
                RPC.PlaySoundRPC(user.PlayerId, Sounds.TaskComplete);
                Logger.Info($"Remotekillerのターゲット{target.name}のキルに成功", "Remotekiller.kill");
                Rtarget = 111;
                return !RKillAnimation.GetBool();
            }
            return true;
        }
        public bool CanUseSabotageButton() => false;
    }
}
