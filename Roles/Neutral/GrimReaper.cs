using AmongUs.GameOptions;
using System.Collections.Generic;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral
{
    public sealed class GrimReaper : RoleBase, ILNKiller//死神はにゃんこを仲間にできない
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(GrimReaper),
                player => new GrimReaper(player),
                CustomRoles.GrimReaper,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                52100,
                SetupOptionItem,
                "GR",
                "#4b0082",
                true,
                countType: CountTypes.GrimReaper,//こいつ生存カウント分ける。(生存カウント入れないため)
                assignInfo: new RoleAssignInfo(CustomRoles.GrimReaper, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
                }
            );
        public GrimReaper(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            CanVent = OptionCanVent.GetBool();
            CanUseSabotage = OptionCanUseSabotage.GetBool();
            HasImpostorVision = OptionHasImpostorVision.GetBool();
            Canbotton = OptionCanbotton.GetBool();
        }

        private static OptionItem OptionKillCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        private static OptionItem OptionHasImpostorVision;
        private static OptionItem OptionCanbotton;
        private static float KillCooldown;
        public static bool CanVent;
        public static bool CanUseSabotage;
        private static bool HasImpostorVision;
        private static bool Canbotton;
        enum OptionName
        {
            Canbotton,
        }

        Dictionary<byte, float> GrimPlayers = new(14);
        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
            OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);//正味サボ使用不可でもいい気がする
            OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
            OptionCanbotton = BooleanOptionItem.Create(RoleInfo, 14, OptionName.Canbotton, false, false);
        }
        public float CalculateKillCooldown() => KillCooldown;
        public bool CanUseSabotageButton() => CanUseSabotage;
        public bool CanUseImpostorVentButton() => CanVent;
        public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            {
                if (!info.IsSuicide)
                {
                    (var kille, var taret) = info.AttemptTuple;
                    {
                        Logger.Info($"{kille?.Data?.PlayerName}:キル", "GrimReaper");
                        Main.AllPlayerKillCooldown[kille.PlayerId] = 999;
                        kille.SyncSettings();//もう君はキルできないよ...!
                    }
                }

                var (killer, target) = info.AttemptTuple;

                if (info.IsFakeSuicide) return;

                if (!GrimPlayers.ContainsKey(target.PlayerId))
                {
                    killer.SetKillCooldown();
                    GrimPlayers.Add(target.PlayerId, 0f);
                }
                info.DoKill = false;

            }
        }
        public override void OnReportDeadBody(PlayerControl _, GameData.PlayerInfo __)
        {
            foreach (var targetId in GrimPlayers.Keys)
            {
                var target = Utils.GetPlayerById(targetId);
                KillBitten(target, true);
            }
            GrimPlayers.Clear();
        }
        public override void AfterMeetingTasks()//あのままじゃホストだけキルクール回復するバグあったから
        {
            Logger.Info("死神のキルクールを戻す", "GrimReaper");
            Main.AllPlayerKillCooldown[Player.PlayerId] = KillCooldown;
            Player.SyncSettings();
        }
        private void KillBitten(PlayerControl target, bool isButton = false)
        {
            var Grim = Player;
            if (target.IsAlive())
            {
                PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Grim;
                target.SetRealKiller(Grim);
                CustomRoleManager.OnCheckMurder(
                    Grim, target,
                    target, target
                );
                Logger.Info($"死神キル:{target.name}を天界に連れ去ったぜ", "GrimReaper");
                if (!isButton && Grim.IsAlive())
                {
                    RPC.PlaySoundRPC(Grim.PlayerId, Sounds.KillSound);
                }
            }
            else
            {
                Logger.Info($"死神キル:{target.name}は会議始まる前に死んだぜ", "GrimReaper");
            }
        }
        public override bool CancelReportDeadBody(PlayerControl repo, GameData.PlayerInfo oniku)
        {
            if (repo.Is(CustomRoles.GrimReaper) && oniku != null)//死体通報はデフォでさせない。
            {
                Logger.Info("死神だから会議をキャンセル。", "GrimReaper");
                return true;
            }
            if (repo.Is(CustomRoles.GrimReaper) && oniku == null && !Canbotton)//ボタン使用不可でボタンであろう場面のみ
            {
                Logger.Info("死神はボタンも使えない。", "GrimReaper");
                return true;
            }
            return false;
        }
        public bool OverrideKillButtonText(out string text)
        {
            text = GetString("WarlockCurseButtonText");
            return true;
        }
        public bool OverrideKillButton(out string text)
        {
            text = "Grim_Kill";
            return true;
        }
    }
}