using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch]
    public class ModUpdater
    {
        private static readonly string URL = "https://api.github.com/repos/KYMario/TownOfHost-K";
        public static bool hasUpdate = false;
        public static bool isBroken = false;
        public static bool isChecked = false;
        public static Version latestVersion = null;
        public static string latestTitle = null;
        public static string downloadUrl = null;
        public static GenericPopup InfoPopup;
        public static bool publicok = Main.AllowPublicRoom;
        public static bool matchmaking = false;
        public static bool nothostbug = false;
        public static string body = "詳細のチェックに失敗しました";

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.LowerThanNormal)]
        public static void StartPostfix()
        {
            DeleteOldDLL();
            InfoPopup = UnityEngine.Object.Instantiate(Twitch.TwitchManager.Instance.TwitchPopup);
            InfoPopup.name = "InfoPopup";
            InfoPopup.TextAreaTMP.GetComponent<RectTransform>().sizeDelta = new(2.5f, 2f);
            if (!isChecked)
            {
                CheckRelease(Main.BetaBuildURL.Value != "").GetAwaiter().GetResult();
            }
            MainMenuManagerPatch.UpdateButton.Button.gameObject.SetActive(hasUpdate);
            MainMenuManagerPatch.UpdateButton.Button.transform.Find("FontPlacer/Text_TMP").GetComponent<TMPro.TMP_Text>().SetText($"{GetString("updateButton")}\n{latestTitle}");
            MainMenuManagerPatch.UpdateButton2.Button.gameObject.SetActive(hasUpdate);
        }
        public static async Task<bool> CheckRelease(bool beta = false)
        {
            string url = beta ? Main.BetaBuildURL.Value : URL + "/releases/latest";
            try
            {
                string result;
                using (HttpClient client = new())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "TownOfHost-K Updater");
                    using var response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseContentRead);
                    if (!response.IsSuccessStatusCode || response.Content == null)
                    {
                        Logger.Error($"ステータスコード: {response.StatusCode}", "CheckRelease");
                        return false;
                    }
                    result = await response.Content.ReadAsStringAsync();
                }
                JObject data = JObject.Parse(result);
                if (beta)
                {
                    latestTitle = data["name"].ToString();
                    downloadUrl = data["url"].ToString();
                    hasUpdate = latestTitle != ThisAssembly.Git.Commit;
                }
                else
                {
                    latestVersion = new(data["tag_name"]?.ToString().TrimStart('v'));
                    latestTitle = $"Ver. {latestVersion}";
                    JArray assets = data["assets"].Cast<JArray>();
                    for (int i = 0; i < assets.Count; i++)
                    {
                        if (assets[i]["name"].ToString() == "TownOfHost-K_Steam.dll" && Constants.GetPlatformType() == Platforms.StandaloneSteamPC)
                        {
                            downloadUrl = assets[i]["browser_download_url"].ToString();
                            break;
                        }
                        if (assets[i]["name"].ToString() == "TownOfHost-K_Epic.dll" && Constants.GetPlatformType() == Platforms.StandaloneEpicPC)
                        {
                            downloadUrl = assets[i]["browser_download_url"].ToString();
                            break;
                        }
                        if (assets[i]["name"].ToString() == "TownOfHost-K.dll")
                            downloadUrl = assets[i]["browser_download_url"].ToString();
                    }
                    hasUpdate = latestVersion.CompareTo(Main.version) > 0;
                }
                if (downloadUrl == null)
                {
                    Logger.Error("ダウンロードURLを取得できませんでした。", "CheckRelease");
                    return false;
                }
                isChecked = true;
                isBroken = false;
                body = data["body"].ToString();
                if (body.Contains("📢公開ルーム○")) publicok = true;
                else if (body.Contains("📢公開ルーム×")) publicok = false;
                nothostbug = body.Contains("非ホストmodクライアントにバグあり");
            }
            catch (Exception ex)
            {
                isBroken = true;
                Logger.Error($"リリースのチェックに失敗しました。\n{ex}", "CheckRelease", false);
                return false;
            }
            return true;
        }
        public static void StartUpdate(string url)
        {
            ShowPopup(GetString("updatePleaseWait"));
            if (!BackupDLL())
            {
                ShowPopup(GetString("updateManually"), true);
                return;
            }
            _ = DownloadDLL(url);
            return;
        }
        public static bool BackupDLL()
        {
            try
            {
                File.Move(Assembly.GetExecutingAssembly().Location, Assembly.GetExecutingAssembly().Location + ".bak");
            }
            catch
            {
                Logger.Error("バックアップに失敗しました", "BackupDLL");
                return false;
            }
            return true;
        }
        public static void DeleteOldDLL()
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.bak"))
                {
                    Logger.Info($"{Path.GetFileName(path)}を削除", "DeleteOldDLL");
                    File.Delete(path);
                }
            }
            catch
            {
                Logger.Error("削除に失敗しました", "DeleteOldDLL");
            }
            return;
        }
        public static async Task<bool> DownloadDLL(string url)
        {
            try
            {
                using HttpClient client = new();
                using var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    using var content = response.Content;
                    using var stream = content.ReadAsStream();
                    using var file = new FileStream("BepInEx/plugins/TownOfHost-K.dll", FileMode.Create, FileAccess.Write);
                    stream.CopyTo(file);
                    ShowPopup(GetString("updateRestart"), true);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"ダウンロードに失敗しました。\n{ex}", "DownloadDLL", false);
            }
            ShowPopup(GetString("updateManually"), true);
            return false;
        }
        private static void DownloadCallBack(object sender, DownloadProgressChangedEventArgs e)
        {
            ShowPopup($"{GetString("updateInProgress")}\n{e.BytesReceived}/{e.TotalBytesToReceive}({e.ProgressPercentage}%)");
        }
        private static void ShowPopup(string message, bool showButton = false)
        {
            if (InfoPopup != null)
            {
                InfoPopup.Show(message);
                var button = InfoPopup.transform.FindChild("ExitGame");
                if (button != null)
                {
                    button.gameObject.SetActive(showButton);
                    button.GetComponentInChildren<TextTranslatorTMP>().TargetText = StringNames.QuitLabel;
                    button.GetComponent<PassiveButton>().OnClick = new();
                    button.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
                    {
                        Application.OpenURL("https://github.com/KYMario/TownOfHost-K/releases/latest");
                        Application.Quit();
                    }));
                }
            }
        }
        public static async Task<bool> CheckR(int check)
        {
            string result;
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "TownOfHost-K");
                using var response = await client.GetAsync(new Uri(ModUpdater.URL + "/releases/latest"), HttpCompletionOption.ResponseContentRead);
                if (!response.IsSuccessStatusCode || response.Content == null)
                {
                    Logger.Error($"ステータスコード: {response.StatusCode}", "CheckRelease-R");
                    if (check == 0) return Main.AllowPublicRoom;
                    else if (check == 1) return false;
                }
                result = await response.Content.ReadAsStringAsync();
            }
            JObject data = JObject.Parse(result);
            if (check == 0)
            {
                if (data["body"].ToString().Contains("[公開ルームok]")) publicok = true;
                else if (data["body"].ToString().Contains("[公開ルーム禁止！]")) publicok = false;
                else publicok = Main.AllowPublicRoom;
                if (data["body"].ToString().Contains("[マッチメイキングok]")) matchmaking = true;
                else matchmaking = false;
                return publicok;
            }
            else
            if (check == 1)
            {
                return data["body"].ToString().Contains("[使用禁止バージョン]");
            }
            else
            if (check == 2)
            {
                if (data["body"].ToString().Contains("[マッチメイキングok]")) matchmaking = true;
                else matchmaking = false;
                return matchmaking;
            }
            return false;
        }
    }
}
