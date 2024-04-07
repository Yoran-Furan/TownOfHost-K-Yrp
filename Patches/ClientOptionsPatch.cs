using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules.ClientOptions;

namespace TownOfHost
{
    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
    public static class OptionsMenuBehaviourStartPatch
    {
        private static ClientActionItem ForceJapanese;
        private static ClientActionItem JapaneseRoleName;
        private static ClientActionItem UnloadMod;
        private static ClientActionItem DumpLog;
        private static ClientActionItem ChangeSomeLanguage;
        private static ClientActionItem ForceEnd;
        private static ClientActionItem WebHookD;
        private static ClientActionItem Yomiage;
        private static ClientActionItem UseZoom;
        private static ClientActionItem SyncYomiage;
        private static ClientActionItem CustomName;
        private static ClientActionItem HideResetToDefault;
        private static ClientActionItem CustomSprite;

        public static void Postfix(OptionsMenuBehaviour __instance)
        {
            if (__instance.DisableMouseMovement == null)
            {
                return;
            }

            if (ForceJapanese == null || ForceJapanese.ToggleButton == null)
            {
                ForceJapanese = ClientOptionItem.Create("ForceJapanese", Main.ForceJapanese, __instance);
            }
            if (JapaneseRoleName == null || JapaneseRoleName.ToggleButton == null)
            {
                JapaneseRoleName = ClientOptionItem.Create("JapaneseRoleName", Main.JapaneseRoleName, __instance);
            }
            if (UnloadMod == null || UnloadMod.ToggleButton == null)
            {
                UnloadMod = ClientActionItem.Create("UnloadMod", ModUnloaderScreen.Show, __instance);
            }
            if (DumpLog == null || DumpLog.ToggleButton == null)
            {
                DumpLog = ClientActionItem.Create("DumpLog", Utils.DumpLog, __instance);
            }
            if (ChangeSomeLanguage == null || ChangeSomeLanguage.ToggleButton == null)
            {
                ChangeSomeLanguage = ClientOptionItem.Create("ChangeSomeLanguage", Main.ChangeSomeLanguage, __instance);
            }
            if ((ForceEnd == null || ForceEnd.ToggleButton == null) && AmongUsClient.Instance.AmHost)
            {
                ForceEnd = ClientActionItem.Create("ForceEnd", ForceEndProcess, __instance);
            }
            if (WebHookD == null || WebHookD.ToggleButton == null)
            {
                WebHookD = ClientOptionItem.Create("UseWebHook", Main.UseWebHook, __instance);
            }
            if (Yomiage == null || Yomiage.ToggleButton == null)
            {
                Yomiage = ClientOptionItem.Create("UseYomiage", Main.UseYomiage, __instance);
            }
            if (UseZoom == null || UseZoom.ToggleButton == null)
            {
                UseZoom = ClientOptionItem.Create("UseZoom", Main.UseZoom, __instance);
            }
            if (SyncYomiage == null || SyncYomiage.ToggleButton == null)
            {
                SyncYomiage = ClientOptionItem.Create("SyncYomiage", Main.SyncYomiage, __instance);
            }
            if ((CustomName == null || CustomName.ToggleButton == null) && (Main.IsHalloween || Main.IsChristmas || Main.White || Main.GoldenWeek || Main.April))
            {
                CustomName = ClientOptionItem.Create("CustomName", Main.CustomName, __instance);
            }
            if (HideResetToDefault == null || HideResetToDefault.ToggleButton == null)
            {
                HideResetToDefault = ClientOptionItem.Create("HideResetToDefault", Main.HideResetToDefault, __instance);
            }
            if (CustomSprite == null || CustomSprite.ToggleButton == null)
            {
                CustomSprite = ClientOptionItem.Create("CustomSprite", Main.CustomSprite, __instance);
            }
            if (ModUnloaderScreen.Popup == null)
            {
                ModUnloaderScreen.Init(__instance);
            }

            if (!AmongUsClient.Instance.AmHost && ForceEnd != null)
                ForceEnd = null;

        }
        private static void ForceEndProcess()
        {
            if (!GameStates.IsInGame) return;
            //左シフトが押されているなら強制廃村
            if (Input.GetKey(KeyCode.LeftShift))
            {
                GameManager.Instance.enabled = false;
                CustomWinnerHolder.WinnerTeam = CustomWinner.None;
                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorByKill, false);
                return;
            }
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
            GameManager.Instance.LogicFlow.CheckEndCriteria();
        }
    }

    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Close))]
    public static class OptionsMenuBehaviourClosePatch
    {
        public static void Postfix()
        {
            if (ClientActionItem.CustomBackground != null)
            {
                ClientActionItem.CustomBackground.gameObject.SetActive(false);
            }
            ModUnloaderScreen.Hide();
        }
    }
}
