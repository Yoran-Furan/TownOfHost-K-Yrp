using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral
{
    public sealed class JackalMafia : RoleBase, ILNKiller, ISchrodingerCatOwner
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(JackalMafia),
                player => new JackalMafia(player),
                CustomRoles.JackalMafia,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                50904,
                SetupOptionItem,
                "jm",
                "#00b4eb",
                true,
                countType: CountTypes.Jackal,
                assignInfo: new RoleAssignInfo(CustomRoles.JackalMafia, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
                }
            );
        public JackalMafia(PlayerControl player)
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
            JackalCanKillMafia = OptionJJackalCanKillMafia.GetBool();
            JackalCanAlsoBeExposedToJMafia = OptionJackalCanAlsoBeExposedToJMafia.GetBool();
            MafiaCanAlsoBeExposedToJackal = OptionJMafiaCanAlsoBeExposedToJackal.GetBool();
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        }

        private static OptionItem OptionKillCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        private static OptionItem OptionHasImpostorVision;
        private static OptionItem OptionJackalCanAlsoBeExposedToJMafia;
        private static OptionItem OptionJMafiaCanAlsoBeExposedToJackal;
        private static OptionItem OptionJJackalCanKillMafia;
        private static float KillCooldown;
        public static bool CanVent;
        public static bool CanUseSabotage;
        private static bool HasImpostorVision;
        private static bool JackalCanAlsoBeExposedToJMafia;
        private static bool MafiaCanAlsoBeExposedToJackal;
        private static bool JackalCanKillMafia;
        enum JackalOption
        {
            JackalCanAlsoBeExposedToJMafia,
            MafiaCanAlsoBeExposedToJackal,
            JackalCanKillMafia
        }
        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
            OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
            OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
            OptionJJackalCanKillMafia = BooleanOptionItem.Create(RoleInfo, 16, JackalOption.JackalCanKillMafia, false, false);
            OptionJMafiaCanAlsoBeExposedToJackal = BooleanOptionItem.Create(RoleInfo, 14, JackalOption.MafiaCanAlsoBeExposedToJackal, false, false);
            OptionJackalCanAlsoBeExposedToJMafia = BooleanOptionItem.Create(RoleInfo, 15, JackalOption.JackalCanAlsoBeExposedToJMafia, true, false);
        }   //↑あってるかは知らない、
        public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Jackal;
        public float CalculateKillCooldown() => KillCooldown;
        public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
        public bool CanUseSabotageButton() => CanUseSabotage;
        public bool CanUseImpostorVentButton() => CanVent;
        public override bool OnInvokeSabotage(SystemTypes systemType) => CanUseSabotage;
        public bool CanUseKillButton()
        {
            if (PlayerState.AllPlayerStates == null) return false;
            int livingImpostorsNum = 0;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                var role = pc.GetCustomRole();
                if (role == CustomRoles.Jackal) livingImpostorsNum++;
            }
            return livingImpostorsNum <= 0;
        }
        public override bool OnCheckMurderAsTarget(MurderInfo info)
        {
            (var killer, var target) = info.AttemptTuple;
            if (killer.Is(CustomRoles.Jackal) && !JackalCanKillMafia)
            {
                info.CanKill = false;
                return false;
            }
            return true;
        }
        public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            //seenが省略の場合seer
            seen ??= seer;
            if (seer.PlayerId == Player.PlayerId && seen.Is(CustomRoles.Jackal) && MafiaCanAlsoBeExposedToJackal) return Utils.ColorString(RoleInfo.RoleColor, "★");
            else return "";
        }
        public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            seen ??= seer;
            if (seer.Is(CustomRoles.Jackal) && seen.Is(CustomRoles.JackalMafia) && JackalCanAlsoBeExposedToJMafia) return Utils.ColorString(RoleInfo.RoleColor, "★");
            else return "";
        }
    }
}