using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class Zoom
    {
        public static int size = (int)HudManager.Instance.UICamera.orthographicSize;
        private static int last = 0;
        public static void Postfix()
        {
            if (Main.UseZoom.Value && (GameStates.IsFreePlay || (GameStates.IsInGame && !PlayerControl.LocalPlayer.IsAlive() && GameStates.IsInTask)))
            {
                //チャットなど開いていて、動けない状態 or ｴﾃﾞｨｯﾄﾓｰﾄﾞ なら操作を無効にする
                if (!PlayerControl.LocalPlayer.CanMove || (GameStates.IsFreePlay && Main.EditMode)) return;

                if (Input.mouseScrollDelta.y < 0) size += (int)1.5;
                if (Input.mouseScrollDelta.y > 0 && size > 1.5) size -= (int)1.5;
            }
            else
                size = 3;

            //位置を調整
            if (last != size)
            {
                HudManager.Instance.UICamera.orthographicSize = size;
                Camera.main.orthographicSize = size;
                ResolutionManager.ResolutionChanged.Invoke((float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);
                last = size;
            }
        }
    }
}