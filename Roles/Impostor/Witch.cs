using System.Collections.Generic;
using System.Text;
using Hazel;

using AmongUs.GameOptions;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Witch : RoleBase, IImpostor, IUseTheShButton
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Witch),
                player => new Witch(player),
                CustomRoles.Witch,
                () => ((SwitchTrigger)OptionModeSwitchAction.GetValue() is SwitchTrigger.OnShapeshift or SwitchTrigger.OcShButton) ? RoleTypes.Shapeshifter : RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                1500,
                SetupOptionItem,
                "wi",
                from: From.TheOtherRoles
            );
        public Witch(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);
            cool = OptionShcool.GetFloat();
            occool = cool;
        }
        public override void OnDestroy()
        {
            Witches.Clear();
            SpelledPlayer.Clear();
            CustomRoleManager.MarkOthers.Remove(GetMarkOthers);
        }
        public static OptionItem OptionModeSwitchAction;
        public static OptionItem OptionShcool;
        enum OptionName
        {
            WitchModeSwitchAction,
        }
        public enum SwitchTrigger
        {
            TriggerKill,
            TriggerVent,
            TriggerDouble,
            OnShapeshift,
            OcShButton,
        };

        public bool IsSpellMode;
        public float cool;
        private float occool;
        public List<byte> SpelledPlayer = new();
        public static SwitchTrigger NowSwitchTrigger;

        public static List<Witch> Witches = new();
        public static void SetupOptionItem()
        {
            OptionModeSwitchAction = StringOptionItem.Create(RoleInfo, 10, OptionName.WitchModeSwitchAction, EnumHelper.GetAllNames<SwitchTrigger>(), 0, false);
            OptionShcool = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        }
        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.ShapeshifterDuration = 1f;
            AURoleOptions.ShapeshifterCooldown = NowSwitchTrigger is SwitchTrigger.OcShButton ? occool : 0;
        }
        public override void Add()
        {
            IsSpellMode = false;
            SpelledPlayer.Clear();
            NowSwitchTrigger = (SwitchTrigger)OptionModeSwitchAction.GetValue();
            Witches.Add(this);
            Player.AddDoubleTrigger();

        }
        private void SendRPC(bool doSpell, byte target = 255)
        {
            using var sender = CreateSender();
            sender.Writer.Write(doSpell);
            if (doSpell)
            {
                sender.Writer.Write(target);
            }
            else
            {
                sender.Writer.Write(IsSpellMode);
            }
        }

        public override void ReceiveRPC(MessageReader reader)
        {
            var doSpel = reader.ReadBoolean();
            if (doSpel)
            {
                var spelledId = reader.ReadByte();
                if (spelledId == 255)
                {
                    SpelledPlayer.Clear();
                }
                else
                {
                    SpelledPlayer.Add(spelledId);
                }
            }
            else
            {
                IsSpellMode = reader.ReadBoolean();
            }
        }
        public void SwitchSpellMode(bool kill)
        {
            bool needSwitch = false;
            switch (NowSwitchTrigger)
            {
                case SwitchTrigger.TriggerKill:
                    needSwitch = kill;
                    break;
                case SwitchTrigger.TriggerVent:
                    needSwitch = !kill;
                    break;
                case SwitchTrigger.OnShapeshift:
                    needSwitch = !kill;
                    break;
            }
            if (needSwitch)
            {
                IsSpellMode = !IsSpellMode;
                SendRPC(false);
                Utils.NotifyRoles(SpecifySeer: Player);
            }
        }
        public static bool IsSpelled(byte target = 255)
        {
            foreach (var witch in Witches)
            {
                if (target == 255 && witch.SpelledPlayer.Count != 0) return true;

                if (witch.SpelledPlayer.Contains(target))
                {
                    return true;
                }
            }
            return false;
        }
        public void SetSpelled(PlayerControl target)
        {
            if (!IsSpelled(target.PlayerId))
            {
                SpelledPlayer.Add(target.PlayerId);
                SendRPC(true, target.PlayerId);
                //キルクールの適正化
                Player.SetKillCooldown();
            }
        }
        public bool UseOCButton => NowSwitchTrigger is SwitchTrigger.OnShapeshift or SwitchTrigger.OcShButton;
        public void OnClick()
        {
            if (NowSwitchTrigger is SwitchTrigger.OcShButton)
            {
                var target = Player.GetKillTarget();
                if (target != null)
                {
                    SendRPC(target);
                    Player.RpcResetAbilityCooldown();
                    Player.RpcProtectedMurderPlayer(target);
                    SetSpelled(target);
                    Utils.NotifyRoles(SpecifySeer: Player);
                }
                occool = target is null ? 0 : cool;
                Player.MarkDirtySettings();
                Player.RpcResetAbilityCooldown();
            }
            else
            if (NowSwitchTrigger is SwitchTrigger.OnShapeshift)
            {
                SwitchSpellMode(false);
            }
        }
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            var (killer, target) = info.AttemptTuple;
            if (NowSwitchTrigger == SwitchTrigger.TriggerDouble)
            {
                info.DoKill = killer.CheckDoubleTrigger(target, () => { SetSpelled(target); });
            }
            else
            {
                if (IsSpellMode)
                {//呪いならキルしない
                    info.DoKill = false;
                    SetSpelled(target);
                }
                SwitchSpellMode(true);
            }
            //切れない相手ならキルキャンセル
            info.DoKill &= info.CanKill;
        }
        public override void AfterMeetingTasks()
        {
            if (Player.IsAlive() || MyState.DeathReason != CustomDeathReason.Vote)
            {//吊られなかった時呪いキル発動
                var spelledIdList = new List<byte>();
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (SpelledPlayer.Contains(pc.PlayerId) && !Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                    {
                        pc.SetRealKiller(Player);
                        spelledIdList.Add(pc.PlayerId);
                    }
                }
                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Spell, spelledIdList.ToArray());
            }
            //実行してもしなくても呪いはすべて解除
            SpelledPlayer.Clear();
            if (!AmongUsClient.Instance.AmHost) return;
            SendRPC(true);
            if (occool is 0)
            {
                occool = cool;
                Player.MarkDirtySettings();
            }
        }
        public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            seen ??= seer;
            if (isForMeeting && IsSpelled(seen.PlayerId))
            {
                return Utils.ColorString(Palette.ImpostorRed, "†");
            }
            return "";
        }
        public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        {
            seen ??= seer;
            if (!Is(seen) || isForMeeting) return "";
            if (NowSwitchTrigger is SwitchTrigger.OcShButton) return "";

            var sb = new StringBuilder();
            sb.Append(isForHud ? GetString("WitchCurrentMode") : "Mode:");
            if (NowSwitchTrigger == SwitchTrigger.TriggerDouble)
            {
                sb.Append(GetString("WitchModeDouble"));
            }
            else
            {
                sb.Append(IsSpellMode ? GetString("WitchModeSpell") : GetString("WitchModeKill"));
            }
            return sb.ToString();
        }
        public bool OverrideKillButtonText(out string text)
        {
            if (NowSwitchTrigger != SwitchTrigger.TriggerDouble && IsSpellMode)
            {
                text = GetString("WitchSpellButtonText");
                return true;
            }
            text = default;
            return false;
        }
        public override bool OnEnterVent(PlayerPhysics physics, int ventId)
        {
            if (NowSwitchTrigger is SwitchTrigger.TriggerVent)
            {
                SwitchSpellMode(false);
            }
            return true;
        }
    }
}