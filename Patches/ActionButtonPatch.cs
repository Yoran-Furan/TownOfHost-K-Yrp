using HarmonyLib;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.DoClick))]
public static class SabotageButtonDoClickPatch
{
    public static bool Prefix()
    {
        if (!PlayerControl.LocalPlayer.inVent && GameManager.Instance.SabotagesEnabled())
        {
            DestroyableSingleton<HudManager>.Instance.ToggleMapVisible(new MapOptions
            {
                Mode = MapOptions.Modes.Sabotage
            });
        }

        return false;
    }
}

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class AbilityButtonDoClickPatch
{
    public static bool Prefix()
    {
        if (!AmongUsClient.Instance.AmHost || HudManager._instance.AbilityButton.isCoolingDown || !PlayerControl.LocalPlayer.CanMove || Utils.IsActive(SystemTypes.MushroomMixupSabotage)) return true;
        if (PlayerControl.LocalPlayer.GetRoleClass() is IUseTheShButton sb && sb.UseOCButton)
        {
            PlayerControl.LocalPlayer.Data.Role.SetCooldown();
            sb.OnClick();
            return false;
        }
        return true;
    }
}

/*[HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
public static class KillButtonDoClickPatch
{
    public static void Prefix()
    {
        var players = PlayerControl.LocalPlayer.GetPlayersInAbilityRangeSorted(false);
        PlayerControl closest = players.Count <= 0 ? null : players[0];
        if (!GameStates.IsInTask || !PlayerControl.LocalPlayer.CanUseKillButton() || closest == null
            || PlayerControl.LocalPlayer.Data.IsDead || HudManager._instance.KillButton.isCoolingDown) return;
        PlayerControl.LocalPlayer.CheckMurder(closest); //一時的な修正
    }
}*/
