using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using TownOfHost.Modules;
using static TownOfHost.Translator;
using Object = UnityEngine.Object;

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.InitializeOptions))]
    public static class GameSettingMenuPatch
    {
        public static void Prefix(GameSettingMenu __instance)
        {
            // Unlocks map/impostor amount changing in online (for testing on your custom servers)
            // オンラインモードで部屋を立て直さなくてもマップを変更できるように変更
            __instance.HideForOnline = new Il2CppReferenceArray<Transform>(0);
        }
    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
    [HarmonyPriority(Priority.First)]
    public static class GameOptionsMenuPatch
    {
        public static TMPro.TextMeshPro Timer;
        public static void Postfix(GameOptionsMenu __instance)
        {
            foreach (var ob in __instance.Children)
            {
                switch (ob.Title)
                {
                    case StringNames.GameShortTasks:
                    case StringNames.GameLongTasks:
                    case StringNames.GameCommonTasks:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 99);
                        break;
                    case StringNames.GameKillCooldown:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                        break;
                    case StringNames.GameNumImpostors:
                        if (DebugModeManager.IsDebugMode)
                        {
                            ob.Cast<NumberOption>().ValidRange.min = 0;
                        }
                        break;
                    default:
                        break;
                }
            }
            var ResetToDefault = GameObject.Find("ResetToDefault");
            if (ResetToDefault != null && Main.HideResetToDefault.Value)
            {
                ResetToDefault.SetActive(false);
            }

            var template = Object.FindObjectsOfType<StringOption>().FirstOrDefault();
            if (template == null) return;

            var gameSettings = GameObject.Find("Game Settings");
            if (gameSettings == null) return;
            gameSettings.transform.FindChild("GameGroup").GetComponent<Scroller>().ScrollWheelSpeed = 1f;

            var gameSettingMenu = Object.FindObjectsOfType<GameSettingMenu>().FirstOrDefault();
            if (gameSettingMenu == null) return;
            List<GameObject> menus = new() { gameSettingMenu.RegularGameSettings, gameSettingMenu.RolesSettings.gameObject };
            List<SpriteRenderer> highlights = new() { gameSettingMenu.GameSettingsHightlight, gameSettingMenu.RolesSettingsHightlight };

            var roleTab = GameObject.Find("RoleTab");
            var gameTab = GameObject.Find("GameTab");
            List<GameObject> tabs = new() { gameTab, roleTab };

            foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
            {
                var obj = gameSettings.transform.parent.Find(tab + "Tab");
                if (obj != null)
                {
                    obj.transform.FindChild("../../GameGroup/Text").GetComponent<TMPro.TextMeshPro>().SetText(GetString("TabGroup." + tab));
                    continue;
                }

                var tohkSettings = Object.Instantiate(gameSettings, gameSettings.transform.parent);
                tohkSettings.name = tab + "Tab";
                tohkSettings.transform.FindChild("BackPanel").transform.localScale =
                tohkSettings.transform.FindChild("Bottom Gradient").transform.localScale = new Vector3(1.2f, 1f, 1f);
                tohkSettings.transform.FindChild("Background").transform.localScale = new Vector3(1.3f, 1f, 1f);
                tohkSettings.transform.FindChild("UI_Scrollbar").transform.localPosition += new Vector3(0.35f, 0f, 0f);
                tohkSettings.transform.FindChild("UI_ScrollbarTrack").transform.localPosition += new Vector3(0.35f, 0f, 0f);
                tohkSettings.transform.FindChild("GameGroup/SliderInner").transform.localPosition += new Vector3(-0.15f, 0f, 0f);
                var tohkMenu = tohkSettings.transform.FindChild("GameGroup/SliderInner").GetComponent<GameOptionsMenu>();

                //OptionBehaviourを破棄
                tohkMenu.GetComponentsInChildren<OptionBehaviour>().Do(x => Object.Destroy(x.gameObject));

                var scOptions = new List<OptionBehaviour>();
                foreach (var option in OptionItem.AllOptions)
                {
                    if (option.Tab != (TabGroup)tab) continue;
                    if (option.OptionBehaviour == null)
                    {
                        var stringOption = Object.Instantiate(template, tohkMenu.transform);
                        scOptions.Add(stringOption);
                        stringOption.OnValueChanged = new System.Action<OptionBehaviour>((o) => { });
                        stringOption.TitleText.text = option.Name;
                        stringOption.Value = stringOption.oldValue = option.CurrentValue;
                        stringOption.ValueText.text = option.GetString();
                        stringOption.name = option.Name;
                        stringOption.transform.FindChild("Background").localScale = new Vector3(1.2f, 1f, 1f);
                        stringOption.transform.FindChild("Plus_TMP").localPosition += new Vector3(option.HideValue ? 100f : 0.3f, option.HideValue ? 100f : 0f, option.HideValue ? 100f : 0f);
                        stringOption.transform.FindChild("Minus_TMP").localPosition += new Vector3(option.HideValue ? 100f : 0.3f, option.HideValue ? 100f : 0f, option.HideValue ? 100f : 0f);
                        stringOption.transform.FindChild("Value_TMP").localPosition += new Vector3(0.3f, 0f, 0f);
                        stringOption.transform.FindChild("Title_TMP").localPosition += new Vector3(0.15f, 0f, 0f);
                        stringOption.transform.FindChild("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(3.5f, 0.37f);

                        option.OptionBehaviour = stringOption;
                    }
                    option.OptionBehaviour.gameObject.SetActive(true);
                }
                tohkMenu.Children = scOptions.ToArray();
                tohkSettings.gameObject.SetActive(false);
                menus.Add(tohkSettings.gameObject);

                var tohkTab = Object.Instantiate(roleTab, roleTab.transform.parent);
                tohkTab.transform.FindChild("Hat Button").FindChild("Icon").GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite($"TownOfHost.Resources.TabIcon_{tab}.png", 100f);
                tabs.Add(tohkTab);
                var tohkTabHighlight = tohkTab.transform.FindChild("Hat Button").FindChild("Tab Background").GetComponent<SpriteRenderer>();
                highlights.Add(tohkTabHighlight);
            }

            for (var i = 0; i < tabs.Count; i++)
            {
                tabs[i].transform.localPosition = new(0.8f * (i - 1) - tabs.Count / 3f, tabs[i].transform.localPosition.y, tabs[i].transform.localPosition.z);
                var button = tabs[i].GetComponentInChildren<PassiveButton>();
                if (button == null) continue;
                var copiedIndex = i;
                button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                Action value = () =>
                {
                    for (var j = 0; j < menus.Count; j++)
                    {
                        menus[j].SetActive(j == copiedIndex);
                        highlights[j].enabled = j == copiedIndex;
                    }
                };
                button.OnClick.AddListener(value);
            }

            // ボタン生成
            CreateButton("OptionReset", Color.red, new Vector2(-4f, -4.8f), new Action(() =>
            {
                OptionItem.AllOptions.ToArray().Where(x => x.Id > 0).Do(x => x.SetValue(x.DefaultValue));
            }));
            CreateButton("OptionCopy", Color.green, new Vector2(-4f, -4.2f), new Action(() =>
            {
                OptionSerializer.SaveToClipboard();
            }));
            CreateButton("OptionLoad", Color.green, new Vector2(-4f, -4.5f), new Action(() =>
            {
                OptionSerializer.LoadFromClipboard();
            }));

            var timergobj = GameObject.Find("Timer");
            if (timergobj != null)
            {
                Timer = GameObject.Instantiate(GameObject.Find("Timer"), GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainSettingsTab").transform.parent)
                    .GetComponent<TMPro.TextMeshPro>();
                Timer.name = "MenuTimer";
                Timer.transform.localPosition = new Vector3(-4.85f, 2.1f, -30f);
                Timer.transform.localScale = new Vector2(0.8f, 0.8f);
            }

            static void CreateButton(string text, Color color, Vector2 position, Action action)
            {
                var ToggleButton = Object.Instantiate(GameObject.Find("Main Camera/Hud/Menu/GeneralTab/ControlGroup/JoystickModeButton"), GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainSettingsTab").transform);
                ToggleButton.transform.localPosition = new Vector3(position.x, position.y, -15f);
                ToggleButton.transform.localScale = new Vector2(0.6f, 0.6f);
                ToggleButton.name = text;
                ToggleButton.transform.FindChild("Text_TMP").GetComponent<TMPro.TextMeshPro>().text = Translator.GetString(text);
                ToggleButton.transform.FindChild("Background").GetComponent<SpriteRenderer>().color = color;
                var passiveButton = ToggleButton.GetComponent<PassiveButton>();
                passiveButton.OnClick = new();
                passiveButton.OnClick.AddListener(action);
            }
        }
    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    public class GameOptionsMenuUpdatePatch
    {
        private static float _timer = 1f;

        public static void Postfix(GameOptionsMenu __instance)
        {
            //タイマー表示
            if (GameOptionsMenuPatch.Timer != null)
            {
                var rtimer = GameStartManagerPatch.GetTimer();
                rtimer = Mathf.Max(0f, rtimer -= Time.deltaTime);
                int minutes = (int)rtimer / 60;
                int seconds = (int)rtimer % 60;
                GameOptionsMenuPatch.Timer.text = Utils.ColorString(rtimer <= 60 ? Color.red : Color.white, $"{minutes:00}:{seconds:00}");
            }

            if (__instance.transform.parent.parent.name == "Game Settings") return;
            foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
            {
                if (__instance.transform.parent.parent.name != tab + "Tab") continue;
                __instance.transform.FindChild("../../GameGroup/Text").GetComponent<TMPro.TextMeshPro>().SetText(GetString("TabGroup." + tab));

                _timer += Time.deltaTime;
                if (_timer < 0.1f) return;
                _timer = 0f;

                float numItems = __instance.Children.Length;
                var offset = 2.7f;

                foreach (var option in OptionItem.AllOptions)
                {
                    if ((TabGroup)tab != option.Tab) continue;
                    if (option?.OptionBehaviour == null || option.OptionBehaviour.gameObject == null) continue;

                    var enabled = true;
                    var parent = option.Parent;

                    enabled = AmongUsClient.Instance.AmHost &&
                        !option.IsHiddenOn(Options.CurrentGameMode);
                    //起動時以外で表示/非表示を切り替える際に使う
                    if (enabled)
                    {
                        switch (option.Name)
                        {
                            case "KickModClient":
                                enabled = ModUpdater.nothostbug;
                                break;
                        }
                    }
                    var opt = option.OptionBehaviour.transform.Find("Background").GetComponent<SpriteRenderer>();
                    opt.size = new(5.0f, 0.45f);
                    while (parent != null && enabled)
                    {
                        enabled = parent.GetBool();
                        parent = parent.Parent;
                        opt.color = new(0f, 1f, 0f);
                        opt.size = new(4.8f, 0.45f);
                        opt.transform.localPosition = new Vector3(0.11f, 0f);
                        option.OptionBehaviour.transform.Find("Title_TMP").transform.localPosition = new Vector3(-0.95f, 0f);
                        option.OptionBehaviour.transform.FindChild("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(3.4f, 0.37f);
                        if (option.Parent?.Parent != null)
                        {
                            opt.color = new(0f, 0f, 1f);
                            opt.size = new(4.6f, 0.45f);
                            opt.transform.localPosition = new Vector3(0.24f, 0f);
                            option.OptionBehaviour.transform.Find("Title_TMP").transform.localPosition = new Vector3(-0.7f, 0f);
                            option.OptionBehaviour.transform.FindChild("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(3.3f, 0.37f);
                            if (option.Parent?.Parent?.Parent != null)
                            {
                                opt.color = new(1f, 0f, 0f);
                                opt.size = new(4.4f, 0.45f);
                                opt.transform.localPosition = new Vector3(0.37f, 0f);
                                option.OptionBehaviour.transform.Find("Title_TMP").transform.localPosition = new Vector3(-0.55f, 0f);
                                option.OptionBehaviour.transform.FindChild("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(3.2f, 0.37f);
                            }
                        }
                    }

                    option.OptionBehaviour.gameObject.SetActive(enabled);
                    if (enabled)
                    {
                        offset -= option.IsHeader ? 0.7f : 0.5f;
                        option.OptionBehaviour.transform.localPosition = new Vector3(
                            option.OptionBehaviour.transform.localPosition.x,
                            offset,
                            option.OptionBehaviour.transform.localPosition.z);

                        if (option.IsHeader)
                        {
                            numItems += 0.5f;
                        }
                    }
                    else
                    {
                        numItems--;
                    }
                }
                __instance.GetComponentInParent<Scroller>().ContentYBounds.max = (-offset) - 1.5f;
            }
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.OnEnable))]
    public class StringOptionEnablePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;

            __instance.OnValueChanged = new Action<OptionBehaviour>((o) => { });
            __instance.TitleText.text = option.GetName() + option.Fromtext;
            __instance.Value = __instance.oldValue = option.CurrentValue;
            __instance.ValueText.text = option.GetString();

            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
    public class StringOptionIncreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;
            //if (option.Id == 1 && option.CurrentValue == 1 && !Main.TaskBattleOptionv) option.CurrentValue++;
            option.SetValue(option.CurrentValue + (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
    public class StringOptionDecreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;
            //if (option.Id == 1 && option.CurrentValue == 0 && !Main.TaskBattleOptionv) option.CurrentValue--;
            option.SetValue(option.CurrentValue - (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
    public class RpcSyncSettingsPatch
    {
        public static void Postfix()
        {
            OptionItem.SyncAllOptions();
        }
    }
    [HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.Start))]
    public static class RolesSettingsMenuPatch
    {
        public static void Postfix(RolesSettingsMenu __instance)
        {
            foreach (var ob in __instance.Children)
            {
                switch (ob.Title)
                {
                    case StringNames.EngineerCooldown:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                        break;
                    case StringNames.ShapeshifterCooldown:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                        break;
                    default:
                        break;
                }
            }
        }
    }
    [HarmonyPatch(typeof(NormalGameOptionsV07), nameof(NormalGameOptionsV07.SetRecommendations))]
    public static class SetRecommendationsPatch
    {
        public static bool Prefix(NormalGameOptionsV07 __instance, int numPlayers, bool isOnline)
        {
            numPlayers = Mathf.Clamp(numPlayers, 4, 15);
            __instance.PlayerSpeedMod = __instance.MapId == 4 ? 1.25f : 1f; //AirShipなら1.25、それ以外は1
            __instance.CrewLightMod = 0.5f;
            __instance.ImpostorLightMod = 1.75f;
            __instance.KillCooldown = 25f;
            __instance.NumCommonTasks = 2;
            __instance.NumLongTasks = 4;
            __instance.NumShortTasks = 6;
            __instance.NumEmergencyMeetings = 1;
            if (!isOnline)
                __instance.NumImpostors = NormalGameOptionsV07.RecommendedImpostors[numPlayers];
            __instance.KillDistance = 0;
            __instance.DiscussionTime = 0;
            __instance.VotingTime = 150;
            __instance.IsDefaults = true;
            __instance.ConfirmImpostor = false;
            __instance.VisualTasks = false;

            __instance.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Shapeshifter);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Scientist);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.GuardianAngel);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Engineer);

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) //HideAndSeek
            {
                __instance.PlayerSpeedMod = 1.75f;
                __instance.CrewLightMod = 5f;
                __instance.ImpostorLightMod = 0.25f;
                __instance.NumImpostors = 1;
                __instance.NumCommonTasks = 0;
                __instance.NumLongTasks = 0;
                __instance.NumShortTasks = 10;
                __instance.KillCooldown = 10f;
            }
            if (Options.IsStandardHAS) //StandardHAS
            {
                __instance.PlayerSpeedMod = 1.75f;
                __instance.CrewLightMod = 5f;
                __instance.ImpostorLightMod = 0.25f;
                __instance.NumImpostors = 1;
                __instance.NumCommonTasks = 0;
                __instance.NumLongTasks = 0;
                __instance.NumShortTasks = 10;
                __instance.KillCooldown = 10f;
            }
            return false;
        }
    }
}
