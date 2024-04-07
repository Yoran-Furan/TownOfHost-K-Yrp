using UnityEngine;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Mole : RoleBase, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Mole),
                player => new Mole(player),
                CustomRoles.Mole,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                13104,
                null,
                "ml"
            );
        public Mole(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
        }
        public override bool CantVentIdo(PlayerPhysics physics, int ventId)
        {
            _ = new LateTask(() =>
            {
                if (Main.NormalOptions.MapId == 0)
                {
                    int chance = IRandom.Instance.Next(1, 15);
                    if (chance == 1) Player.RpcSnapToForced(new Vector2(4.3f, -0.3f));
                    if (chance == 2) Player.RpcSnapToForced(new Vector2(9.4f, -6.4f));
                    if (chance == 3) Player.RpcSnapToForced(new Vector2(2.5f, -10.0f));
                    if (chance == 4) Player.RpcSnapToForced(new Vector2(8.8f, 3.3f));
                    if (chance == 5) Player.RpcSnapToForced(new Vector2(16.0f, -3.2f));
                    if (chance == 6) Player.RpcSnapToForced(new Vector2(16.2f, -6.0f));
                    if (chance == 7) Player.RpcSnapToForced(new Vector2(9.5f, -14.3f));
                    if (chance == 8) Player.RpcSnapToForced(new Vector2(-9.7f, -8.1f));
                    if (chance == 9) Player.RpcSnapToForced(new Vector2(-10.6f, -4.2f));
                    if (chance == 10) Player.RpcSnapToForced(new Vector2(-12.5f, -6.9f));
                    if (chance == 11) Player.RpcSnapToForced(new Vector2(-15.2f, 2.5f));
                    if (chance == 12) Player.RpcSnapToForced(new Vector2(-21.9f, -3.1f));
                    if (chance == 13) Player.RpcSnapToForced(new Vector2(-20.7f, -7.0f));
                    if (chance == 14) Player.RpcSnapToForced(new Vector2(-15.3f, -13.7f));

                };
                if (Main.NormalOptions.MapId == 1)
                {
                    int chance = IRandom.Instance.Next(1, 12);
                    if (chance == 1) Player.RpcSnapToForced(new Vector2(23.8f, -1.9f));
                    if (chance == 2) Player.RpcSnapToForced(new Vector2(15.4f, -1.8f));
                    if (chance == 3) Player.RpcSnapToForced(new Vector2(4.3f, 0.5f));
                    if (chance == 4) Player.RpcSnapToForced(new Vector2(-6.2f, 3.6f));
                    if (chance == 5) Player.RpcSnapToForced(new Vector2(0.5f, 10.7f));
                    if (chance == 6) Player.RpcSnapToForced(new Vector2(11.6f, 13.8f));
                    if (chance == 7) Player.RpcSnapToForced(new Vector2(6.8f, 3.1f));
                    if (chance == 8) Player.RpcSnapToForced(new Vector2(13.3f, 20.1f));
                    if (chance == 9) Player.RpcSnapToForced(new Vector2(17.8f, 25.2f));
                    if (chance == 10) Player.RpcSnapToForced(new Vector2(22.4f, 17.2f));
                    if (chance == 11) Player.RpcSnapToForced(new Vector2(23.9f, 7.2f));
                };
                if (Main.NormalOptions.MapId == 2)
                {
                    int chance = IRandom.Instance.Next(1, 13);
                    if (chance == 1) Player.RpcSnapToForced(new Vector2(19.0f, -24.8f));
                    if (chance == 2) Player.RpcSnapToForced(new Vector2(20.1f, -25.0f));
                    if (chance == 3) Player.RpcSnapToForced(new Vector2(30.9f, -11.9f));
                    if (chance == 4) Player.RpcSnapToForced(new Vector2(33.0f, -9.6f));
                    if (chance == 5) Player.RpcSnapToForced(new Vector2(23.8f, -7.7f));
                    if (chance == 6) Player.RpcSnapToForced(new Vector2(9.6f, -7.7f));
                    if (chance == 7) Player.RpcSnapToForced(new Vector2(2.0f, -9.5f));
                    if (chance == 8) Player.RpcSnapToForced(new Vector2(6.9f, -14.4f));
                    if (chance == 9) Player.RpcSnapToForced(new Vector2(3.5f, -16.6f));
                    if (chance == 10) Player.RpcSnapToForced(new Vector2(12.2f, -18.8f));
                    if (chance == 11) Player.RpcSnapToForced(new Vector2(16.4f, -19.6f));
                    if (chance == 12) Player.RpcSnapToForced(new Vector2(22.0f, -12.2f));

                };
                if (Main.NormalOptions.MapId == 4)
                {
                    int chance = IRandom.Instance.Next(1, 13);
                    if (chance == 1) Player.RpcSnapToForced(new Vector2(-22.0f, -1.6f));
                    if (chance == 2) Player.RpcSnapToForced(new Vector2(-12.6f, 8.5f));
                    if (chance == 3) Player.RpcSnapToForced(new Vector2(-15.7f, -11.7f));
                    if (chance == 4) Player.RpcSnapToForced(new Vector2(-2.7f, -9.3f));
                    if (chance == 5) Player.RpcSnapToForced(new Vector2(0.2f, -2.5f));
                    if (chance == 6) Player.RpcSnapToForced(new Vector2(7.0f, -3.7f));
                    if (chance == 7) Player.RpcSnapToForced(new Vector2(9.8f, 3.1f));
                    if (chance == 8) Player.RpcSnapToForced(new Vector2(3.6f, 6.9f));
                    if (chance == 9) Player.RpcSnapToForced(new Vector2(12.7f, 5.9f));
                    if (chance == 10) Player.RpcSnapToForced(new Vector2(23.2f, 8.3f));
                    if (chance == 11) Player.RpcSnapToForced(new Vector2(24.0f, -1.4f));
                    if (chance == 12) Player.RpcSnapToForced(new Vector2(30.4f, -3.6f));
                }
                if (Main.NormalOptions.MapId == 5)
                {
                    int chance = IRandom.Instance.Next(1, 11);
                    if (chance == 1) Player.RpcSnapToForced(new Vector2(-15.4f, -9.9f));
                    if (chance == 2) Player.RpcSnapToForced(new Vector2(1.3f, -10.6f));
                    if (chance == 3) Player.RpcSnapToForced(new Vector2(15.2f, -16.4f));
                    if (chance == 4) Player.RpcSnapToForced(new Vector2(22.8f, -8.5f));
                    if (chance == 5) Player.RpcSnapToForced(new Vector2(25.2f, 11.0f));
                    if (chance == 6) Player.RpcSnapToForced(new Vector2(9.4f, 0.6f));
                    if (chance == 7) Player.RpcSnapToForced(new Vector2(-12.2f, 8.0f));
                    if (chance == 8) Player.RpcSnapToForced(new Vector2(-16.9f, -2.6f));
                    if (chance == 9) Player.RpcSnapToForced(new Vector2(-2.5f, -9.0f));
                    if (chance == 10) Player.RpcSnapToForced(new Vector2(2.9f, 0.9f));
                };

            }, 0.7f, "TP");
            return false;
        }
        public bool OverrideImpVentButton(out string text)
        {
            text = "Mole.Vent";
            return true;
        }
    }
}
