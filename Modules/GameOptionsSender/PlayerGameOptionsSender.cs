using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using InnerNet;
using Mathf = UnityEngine.Mathf;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;

namespace TownOfHost.Modules
{
    public class PlayerGameOptionsSender : GameOptionsSender
    {
        public static void SetDirty(PlayerControl player) => SetDirty(player.PlayerId);
        public static void SetDirty(byte playerId) =>
            AllSenders.OfType<PlayerGameOptionsSender>()
            .Where(sender => sender.player.PlayerId == playerId)
            .ToList().ForEach(sender => sender.SetDirty());
        public static void SetDirtyToAll() =>
            AllSenders.OfType<PlayerGameOptionsSender>()
            .ToList().ForEach(sender => sender.SetDirty());

        public override IGameOptions BasedGameOptions =>
            Main.RealOptionsData.Restore(new NormalGameOptionsV07(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>());
        public override bool IsDirty { get; protected set; }

        public PlayerControl player;

        public PlayerGameOptionsSender(PlayerControl player)
        {
            this.player = player;
        }
        public void SetDirty() => IsDirty = true;

        public override void SendGameOptions()
        {
            if (player.AmOwner)
            {
                var opt = BuildGameOptions();
                foreach (var com in GameManager.Instance.LogicComponents)
                {
                    if (com.TryCast<LogicOptions>(out var lo))
                        lo.SetGameOptions(opt);
                }
                GameOptionsManager.Instance.CurrentGameOptions = opt;
            }
            else base.SendGameOptions();
        }

        public override void SendOptionsArray(Il2CppStructArray<byte> optionArray)
        {
            for (byte i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
            {
                if (GameManager.Instance.LogicComponents[i].TryCast<LogicOptions>(out _))
                {
                    SendOptionsArray(optionArray, i, player.GetClientId());
                }
            }
        }
        public static void RemoveSender(PlayerControl player)
        {
            var sender = AllSenders.OfType<PlayerGameOptionsSender>()
            .FirstOrDefault(sender => sender.player.PlayerId == player.PlayerId);
            if (sender == null) return;
            sender.player = null;
            AllSenders.Remove(sender);
        }
        public override IGameOptions BuildGameOptions()
        {
            if (Main.RealOptionsData == null)
            {
                Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);
            }

            var opt = BasedGameOptions;
            AURoleOptions.SetOpt(opt);
            var state = PlayerState.GetByPlayerId(player.PlayerId);
            opt.BlackOut(state.IsBlackOut);

            CustomRoles role = player.GetCustomRole();
            switch (role.GetCustomRoleTypes())
            {
                case CustomRoleTypes.Impostor:
                    AURoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
                    break;
                case CustomRoleTypes.Madmate:
                    AURoleOptions.EngineerCooldown = Options.MadmateVentCooldown.GetFloat();
                    AURoleOptions.EngineerInVentMaxTime = Options.MadmateVentMaxTime.GetFloat();
                    if (Options.MadmateHasSun.GetBool() || player.Is(CustomRoles.Sun))//サンがついてて
                    {
                        //停電でムーンor停電無効設定ON
                        if (Utils.IsActive(SystemTypes.Electrical) && (Options.MadmateHasMoon.GetBool() || player.Is(CustomRoles.Moon)))
                        {
                            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultImpostorVision * 4.5f);
                        }
                        //停電でムーンor停電無効OFF
                        else if (Utils.IsActive(SystemTypes.Electrical))
                        {
                            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
                        }
                        //ただの日常(?)
                        else opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultImpostorVision);
                    }
                    else
                    if (Options.MadmateHasMoon.GetBool() || player.Is(CustomRoles.Moon))//サン無しムーンのみ
                    {
                        if (Utils.IsActive(SystemTypes.Electrical))
                        {
                            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision * 4.5f);
                        }
                    }
                    if (Options.MadmateCanSeeOtherVotes.GetBool())
                        opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    break;
            }

            var roleClass = player.GetRoleClass();
            roleClass?.ApplyGameOptions(opt);
            foreach (var subRole in player.GetCustomSubRoles())
            {
                switch (subRole)
                {
                    case CustomRoles.LastImpostor:
                        if (LastImpostor.GiveWatcher.GetBool()) opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                        break;
                    case CustomRoles.LastNeutral:
                        if (LastNeutral.GiveWatcher.GetBool()) opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                        break;
                    case CustomRoles.Watcher:
                        opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                        break;
                    case CustomRoles.Sun:
                        if (player.GetCustomRole().IsMadmate()) break;//マッドならうえで処理してるからここではしない。
                        if (Utils.IsActive(SystemTypes.Electrical) && player.Is(CustomRoles.Moon)) { opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultImpostorVision * 4.5f); }
                        else//停電時はクルー視界
                        if (Utils.IsActive(SystemTypes.Electrical)) { opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision); }
                        else opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultImpostorVision);
                        break;
                    case CustomRoles.Moon:
                        if (player.GetCustomRole().IsMadmate()) break;//マッドならうえで処理してるからここではしない。
                        if (Utils.IsActive(SystemTypes.Electrical)) { opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision * 4.5f); }
                        break;
                }
            }

            //キルクール0に設定+修正する設定をONにしたと気だけ呼び出す。
            if (Options.FixZeroKillCooldown.GetBool() && AURoleOptions.KillCooldown == 0 && Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var ZerokillCooldown))
            {//0に限りなく近い小数にしてキルできない状態回避する
                AURoleOptions.KillCooldown = Mathf.Max(0.00000000000000000000000000000000000000000001f, ZerokillCooldown);
            }
            else
            if (Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var killCooldown))
            {
                AURoleOptions.KillCooldown = Mathf.Max(0f, killCooldown);
            }

            if (Main.AllPlayerSpeed.TryGetValue(player.PlayerId, out var speed) && !player.Is(CustomRoles.Speeding))
            {
                AURoleOptions.PlayerSpeedMod = Mathf.Clamp(speed, Main.MinSpeed, 5f);
            }
            else
            if (player.Is(CustomRoles.Speeding) && Trapper.Tora && !ReportDeadBodyPatch.CanReport[player.PlayerId])
            {
                AURoleOptions.PlayerSpeedMod = Main.MinSpeed;
            }
            else
            if (player.Is(CustomRoles.Speeding))
            {
                AURoleOptions.PlayerSpeedMod = Speeding.Speed;
            }

            state.taskState.hasTasks = Utils.HasTasks(player.Data, false);
            if (Options.GhostCanSeeOtherVotes.GetBool() && player.Data.IsDead)
                opt.SetBool(BoolOptionNames.AnonymousVotes, false);
            if (Options.AdditionalEmergencyCooldown.GetBool() &&
                Options.AdditionalEmergencyCooldownThreshold.GetInt() <= Utils.AllAlivePlayersCount)
            {
                opt.SetInt(
                    Int32OptionNames.EmergencyCooldown,
                    Options.AdditionalEmergencyCooldownTime.GetInt());
            }
            if (Options.SyncButtonMode.GetBool() && Options.SyncedButtonCount.GetValue() <= Options.UsedButtonCount)
            {
                opt.SetInt(Int32OptionNames.EmergencyCooldown, 3600);
            }
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Options.HideAndSeekKillDelayTimer > 0)
            {
                if (!Main.HnSFlag)
                {
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0f);
                    if (player.Is(CountTypes.Impostor))
                    {
                        AURoleOptions.PlayerSpeedMod = Main.MinSpeed;
                    }
                }
            }
            if (Options.CurrentGameMode == CustomGameMode.TaskBattle && Options.TaskBattleCanVent.GetBool())
            {
                opt.SetFloat(FloatOptionNames.EngineerCooldown, Options.TaskBattleVentCooldown.GetFloat());
                AURoleOptions.EngineerInVentMaxTime = 0;
            }
            MeetingTimeManager.ApplyGameOptions(opt);

            AURoleOptions.ShapeshifterCooldown = Mathf.Max(1f, AURoleOptions.ShapeshifterCooldown);
            AURoleOptions.ProtectionDurationSeconds = 0f;

            return opt;
        }

        public override bool AmValid()
        {
            return base.AmValid() && player != null && !player.Data.Disconnected && Main.RealOptionsData != null;
        }
    }
}