using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using Hazel;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Bomber : RoleBase, IImpostor, IUseTheShButton
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Bomber),
                player => new Bomber(player),
                CustomRoles.Bomber,
                () => RoleTypes.Shapeshifter,
                CustomRoleTypes.Impostor,
                1302,
                SetupOptionItem,
                "bb"
            );
        public Bomber(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            KillDelay = OptionKillDelay.GetFloat();
            Blastrange = OptionBlastrange.GetFloat();
            ExplosionPlayers.Clear();
            Explosion = OptionExplosion.GetInt();
            Cooldown = OptionCooldown.GetFloat();
            ExplosionMode = false;
        }

        static OptionItem OptionKillDelay;
        static OptionItem OptionBlastrange;
        static OptionItem OptionExplosion;
        static OptionItem OptionCooldown;
        static OptionItem OptionOC;
        enum OptionName
        {
            BomberKillDelay,
            blastrange,
            Explosion,
            Cooldown,
            UOcShButton
        }

        static float KillDelay;
        static float Blastrange = 1;
        int Explosion;
        bool ExplosionMode;
        static float Cooldown;

        public bool CanBeLastImpostor { get; } = false;
        Dictionary<byte, float> ExplosionPlayers = new(14);

        private static void SetupOptionItem()
        {
            OptionKillDelay = FloatOptionItem.Create(RoleInfo, 10, OptionName.BomberKillDelay, new(1f, 1000f, 1f), 10f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionBlastrange = FloatOptionItem.Create(RoleInfo, 11, OptionName.blastrange, new(1f, 30f, 0.5f), 1f, false);
            OptionExplosion = IntegerOptionItem.Create(RoleInfo, 12, OptionName.Explosion, new(1, 99, 1), 2, false);
            OptionCooldown = FloatOptionItem.Create(RoleInfo, 13, OptionName.Cooldown, new(0f, 999f, 1f), 0, false);
            OptionOC = BooleanOptionItem.Create(RoleInfo, 14, OptionName.UOcShButton, false, false);
        }

        private void SendRPC()
        {
            using var sender = CreateSender();
            sender.Writer.Write(Explosion);
            sender.Writer.Write(ExplosionMode);
        }
        public override void ReceiveRPC(MessageReader reader)
        {
            Explosion = reader.ReadInt32();
            ExplosionMode = reader.ReadBoolean();
        }

        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (!ExplosionMode) return;
            if (!info.CanKill) return; //キル出来ない相手には無効
            var (killer, target) = info.AttemptTuple;

            if (target.Is(CustomRoles.Bait)) return;
            if (info.IsFakeSuicide) return;

            //誰かに噛まれていなければ登録
            if (!ExplosionPlayers.ContainsKey(target.PlayerId))
            {
                Explosion--;
                SendRPC();
                ExplosionMode = false;
                killer.RpcResetAbilityCooldown();
                killer.SetKillCooldown();
                ExplosionPlayers.Add(target.PlayerId, 0f);
                Utils.NotifyRoles(SpecifySeer: Player);
            }
            info.DoKill = false;
        }
        public override void OnShapeshift(PlayerControl target)
        {
            if (target.PlayerId != Player.PlayerId && 0 < Explosion && !UseOCButton)
            {
                ExplosionMode = !ExplosionMode;
            }
        }
        public void OnClick()
        {
            var target = Player.GetKillTarget();
            if (0 >= Explosion || target == null) return;
            Explosion--;
            SendRPC();
            ExplosionMode = false;
            Player.RpcResetAbilityCooldown();
            Player.RpcProtectedMurderPlayer(target);
            ExplosionPlayers.Add(target.PlayerId, 0f);
            Utils.NotifyRoles(SpecifySeer: Player);
        }
        public override string GetProgressText(bool comms = false) => ExplosionMode ? "<color=red>◆" : "" + Utils.ColorString(0 < Explosion ? Color.red : Color.gray, $"({Explosion})");
        public override void OnFixedUpdate(PlayerControl _)
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask) return;

            foreach (var (targetId, timer) in ExplosionPlayers.ToArray())
            {
                if (timer >= KillDelay)
                {
                    var target = Utils.GetPlayerById(targetId);
                    if (target.IsAlive())
                    {
                        var pos = target.transform.position;
                        foreach (var target2 in Main.AllAlivePlayerControls)
                        {
                            var dis = Vector2.Distance(pos, target2.transform.position);
                            if (dis > Blastrange) continue;
                            if (target2.IsAlive())
                            {
                                PlayerState.GetByPlayerId(target2.PlayerId).DeathReason = CustomDeathReason.Bombed;
                                target2.SetRealKiller(Player);
                                target2.RpcMurderPlayer(target2, true);
                                RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
                                Logger.Info($"{target.name}を爆発させました。", "bomber");
                            }
                        }
                    }

                    ExplosionPlayers.Remove(targetId);
                }
                else
                {
                    ExplosionPlayers[targetId] += Time.fixedDeltaTime;
                }
            }
        }

        public bool UseOCButton => OptionOC.GetBool();
        public override void OnReportDeadBody(PlayerControl _, GameData.PlayerInfo __)
        {
            ExplosionPlayers.Clear();
        }
        public bool OverrideKillButtonText(out string text)
        {
            text = GetString("BomberButtonText");
            if (!ExplosionMode) return false;
            return true;
        }
        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.ShapeshifterCooldown = Cooldown;
            AURoleOptions.ShapeshifterLeaveSkin = false;
            AURoleOptions.ShapeshifterDuration = 1;
        }
    }
}
