using System.Globalization;
using HarmonyLib;
using InnerNet;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Patches;

using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.MakePublic))]
    class MakePublicPatch
    {
        public static bool Prefix(GameStartManager __instance)
        {
            // 定数設定による公開ルームブロック
            if (!AmongUsClient.Instance.AmHost) return false;
            if (!Main.AllowPublicRoom)
            {
                var message = "";
                message = GetString("DisabledByProgram");
                Logger.Info(message, "MakePublicPatch");
                Logger.SendInGame(message);
                return false;
            }
            if (ModUpdater.isBroken || ModUpdater.hasUpdate || !VersionChecker.IsSupported || (!Main.IsPublicAvailableOnThisVersion && !Patches.CustomServerHelper.IsCs()))
            {
                var message = "";
                if (!Main.IsPublicAvailableOnThisVersion) message = GetString("PublicNotAvailableOnThisVersion");
                if (!VersionChecker.IsSupported) message = GetString("UnsupportedVersion");
                if (ModUpdater.isBroken) message = GetString("ModBrokenMessage");
                if (ModUpdater.hasUpdate) message = GetString("CanNotJoinPublicRoomNoLatest");
                Logger.Info(message, "MakePublicPatch");
                Logger.SendInGame(message);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(MMOnlineManager), nameof(MMOnlineManager.Start))]
    class MMOnlineManagerStartPatch
    {
        public static void Postfix(MMOnlineManager __instance)
        {
            MMOnlineManagerUpdatePatch.check = true;
            MMOnlineManagerUpdatePatch.LastServer = ServerManager.Instance.CurrentRegion.TranslateName;
            if (!(ModUpdater.hasUpdate || ModUpdater.isBroken || !VersionChecker.IsSupported || !ModUpdater.publicok || (!Main.IsPublicAvailableOnThisVersion && !CustomServerHelper.IsCs()))) return;
            var obj = GameObject.Find("NormalMenu/Buttons/FindGameButton/FindGameButton");
            if (obj)
            {
                obj?.SetActive(false);
                var parentObj = obj.transform.parent.gameObject;
                var textObj = UnityEngine.Object.Instantiate<TMPro.TextMeshPro>(obj.transform.FindChild("Text_TMP").GetComponent<TMPro.TextMeshPro>());
                textObj.transform.position = new Vector3(1f, -0.3f, 0);
                textObj.name = "CanNotJoinPublic";
                textObj.DestroyTranslator();
                string message = "";
                if (ModUpdater.hasUpdate)
                {
                    message = GetString("CanNotJoinPublicRoomNoLatest");
                }
                else if (ModUpdater.isBroken)
                {
                    message = GetString("ModBrokenMessage");
                }
                else if (!VersionChecker.IsSupported)
                {
                    message = GetString("UnsupportedVersion");
                }
                else if (!Main.IsPublicAvailableOnThisVersion)
                {
                    message = GetString("PublicNotAvailableOnThisVersion");
                }
                textObj.text = $"<size=2>{Utils.ColorString(Color.red, message)}</size>";
            }
        }
    }
    [HarmonyPatch(typeof(MMOnlineManager), nameof(MMOnlineManager.Update))]
    class MMOnlineManagerUpdatePatch
    {
        public static bool check = false;
        public static StringNames LastServer;
        public static void Postfix(MMOnlineManager __instance)
        {
            if (!check) return;
            if (LastServer != ServerManager.Instance.CurrentRegion.TranslateName)
            {
                LastServer = ServerManager.Instance.CurrentRegion.TranslateName;
                if (!CustomServerHelper.IsCs())
                {
                    if (!GameObject.Find("CanNotJoinPublic"))
                        MMOnlineManagerStartPatch.Postfix(__instance);
                }
                else
                {
                    GameObject.Find("NormalMenu/Buttons/FindGameButton/FindGameButton").SetActive(true);
                    Object.Destroy(GameObject.Find("CanNotJoinPublic"));
                }
            }
        }
    }
    [HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Update))]
    class SplashLogoAnimatorPatch
    {
        public static void Prefix(SplashManager __instance)
        {
            if (DebugModeManager.AmDebugger)
            {
                __instance.sceneChanger.AllowFinishLoadingScene();
                __instance.startedSceneLoad = true;
            }
        }
    }
    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsAllowedOnline))]
    class RunLoginPatch
    {
        public static void Prefix(ref bool canOnline)
        {
#if DEBUG
            if (CultureInfo.CurrentCulture.Name != "ja-JP") canOnline = false;
#endif
        }
    }
    [HarmonyPatch(typeof(BanMenu), nameof(BanMenu.SetVisible))]
    class BanMenuSetVisiblePatch
    {
        public static bool Prefix(BanMenu __instance, bool show)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            show &= PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data != null;
            __instance.BanButton.gameObject.SetActive(AmongUsClient.Instance.CanBan());
            __instance.KickButton.gameObject.SetActive(AmongUsClient.Instance.CanKick());
            __instance.MenuButton.gameObject.SetActive(show);
            return false;
        }
    }
    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.CanBan))]
    class InnerNetClientCanBanPatch
    {
        public static bool Prefix(InnerNet.InnerNetClient __instance, ref bool __result)
        {
            __result = __instance.AmHost;
            return false;
        }
    }
    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.KickPlayer))]
    class KickPlayerPatch
    {
        public static void Prefix(InnerNet.InnerNetClient __instance, int clientId, bool ban)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (ban) BanManager.AddBanPlayer(AmongUsClient.Instance.GetRecentClient(clientId));
        }
    }
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendAllStreamedObjects))]
    class InnerNetObjectSerializePatch
    {
        public static void Prefix()
        {
            if (AmongUsClient.Instance.AmHost)
                GameOptionsSender.SendAllGameOptions();
        }
    }
}