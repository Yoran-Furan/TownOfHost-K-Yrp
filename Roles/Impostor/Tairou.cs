using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Tairou : RoleBase, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Tairou),
                player => new Tairou(player),
                CustomRoles.Tairou,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                5000,
                SetupOptionItem,
                "t"
            );
        public Tairou(PlayerControl player)
            : base(
                RoleInfo,
                player
            )
        {
            DeathReasonTairo = OptionDeathReasonTairo.GetBool();
        }
        public static OptionItem OptionDeathReasonTairo;
        enum OptionName
        {
            DeathReasonTairo
        }
        public static bool DeathReasonTairo;
        private static void SetupOptionItem()
        {
            OptionDeathReasonTairo = BooleanOptionItem.Create(RoleInfo, 10, OptionName.DeathReasonTairo, true, false);
        }
        public override CustomRoles GetFtResults(PlayerControl player) => CustomRoles.Crewmate;
    }
}