using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AmongUs.Data;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Crewmate;
using TownOfHost.Roles.AddOns.Neutral;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Utils
    {
        public static bool IsActive(SystemTypes type)
        {
            // ないものはfalse
            if (!ShipStatus.Instance.Systems.ContainsKey(type))
            {
                return false;
            }
            int mapId = Main.NormalOptions.MapId;
            switch (type)
            {
                case SystemTypes.Electrical:
                    {
                        var SwitchSystem = ShipStatus.Instance.Systems[type].Cast<SwitchSystem>();
                        return SwitchSystem != null && SwitchSystem.IsActive;
                    }
                case SystemTypes.Reactor:
                    {
                        if (mapId == 2) return false;
                        else
                        {
                            var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                            return ReactorSystemType != null && ReactorSystemType.IsActive;
                        }
                    }
                case SystemTypes.Laboratory:
                    {
                        if (mapId != 2) return false;
                        var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                        return ReactorSystemType != null && ReactorSystemType.IsActive;
                    }
                case SystemTypes.LifeSupp:
                    {
                        if (mapId is 2 or 4) return false;
                        var LifeSuppSystemType = ShipStatus.Instance.Systems[type].Cast<LifeSuppSystemType>();
                        return LifeSuppSystemType != null && LifeSuppSystemType.IsActive;
                    }
                case SystemTypes.Comms:
                    {
                        if (mapId is 1 or 5)
                        {
                            var HqHudSystemType = ShipStatus.Instance.Systems[type].Cast<HqHudSystemType>();
                            return HqHudSystemType != null && HqHudSystemType.IsActive;
                        }
                        else
                        {
                            var HudOverrideSystemType = ShipStatus.Instance.Systems[type].Cast<HudOverrideSystemType>();
                            return HudOverrideSystemType != null && HudOverrideSystemType.IsActive;
                        }
                    }
                case SystemTypes.HeliSabotage:
                    {
                        var HeliSabotageSystem = ShipStatus.Instance.Systems[type].Cast<HeliSabotageSystem>();
                        return HeliSabotageSystem != null && HeliSabotageSystem.IsActive;
                    }
                case SystemTypes.MushroomMixupSabotage:
                    {
                        var mushroomMixupSabotageSystem = ShipStatus.Instance.Systems[type].TryCast<MushroomMixupSabotageSystem>();
                        return mushroomMixupSabotageSystem != null && mushroomMixupSabotageSystem.IsActive;
                    }
                default:
                    return false;
            }
        }
        public static SystemTypes GetCriticalSabotageSystemType() => (MapNames)Main.NormalOptions.MapId switch
        {
            MapNames.Polus => SystemTypes.Laboratory,
            MapNames.Airship => SystemTypes.HeliSabotage,
            _ => SystemTypes.Reactor,
        };
        public static void SetVision(this IGameOptions opt, bool HasImpVision)
        {
            if (HasImpVision)
            {
                opt.SetFloat(
                    FloatOptionNames.CrewLightMod,
                    opt.GetFloat(FloatOptionNames.ImpostorLightMod));
                if (IsActive(SystemTypes.Electrical))
                {
                    opt.SetFloat(
                    FloatOptionNames.CrewLightMod,
                    opt.GetFloat(FloatOptionNames.CrewLightMod) * 5);
                }
                return;
            }
            else
            {
                opt.SetFloat(
                    FloatOptionNames.ImpostorLightMod,
                    opt.GetFloat(FloatOptionNames.CrewLightMod));
                if (IsActive(SystemTypes.Electrical))
                {
                    opt.SetFloat(
                    FloatOptionNames.ImpostorLightMod,
                    opt.GetFloat(FloatOptionNames.ImpostorLightMod) / 5);
                }
                return;
            }
        }
        //誰かが死亡したときのメソッド
        public static void TargetDies(MurderInfo info)
        {
            PlayerControl killer = info.AppearanceKiller, target = info.AttemptTarget;

            if (!target.Data.IsDead || GameStates.IsMeeting) return;
            foreach (var seer in Main.AllPlayerControls)
            {
                if (KillFlashCheck(info, seer))
                {
                    seer.KillFlash();
                }
            }
        }
        public static bool KillFlashCheck(MurderInfo info, PlayerControl seer)
        {
            PlayerControl killer = info.AppearanceKiller, target = info.AttemptTarget;

            if (seer.Is(CustomRoles.GM)) return true;
            if (seer.Data.IsDead || killer == seer || target == seer) return false;

            if (seer.GetRoleClass() is IKillFlashSeeable killFlashSeeable)
            {
                return killFlashSeeable.CheckKillFlash(info);
            }

            return seer.GetCustomRole() switch
            {
                // IKillFlashSeeable未適用役職はここに書く
                _ => (seer.Is(CustomRoleTypes.Madmate) && Options.MadmateCanSeeKillFlash.GetBool())
                || (seer.Is(CustomRoles.Psychic) && (!IsActive(SystemTypes.Comms) || Psychic.CanSeeComms.GetBool())),
            };
        }
        public static void KillFlash(this PlayerControl player, bool sound = true)
        {
            //キルフラッシュ(ブラックアウト+リアクターフラッシュ)の処理
            bool ReactorCheck = IsActive(GetCriticalSabotageSystemType());

            var Duration = Options.KillFlashDuration.GetFloat();
            if (ReactorCheck) Duration += 0.2f; //リアクター中はブラックアウトを長くする

            //実行
            var state = PlayerState.GetByPlayerId(player.PlayerId);
            state.IsBlackOut = true; //ブラックアウト
            if (player.PlayerId == 0)
            {
                FlashColor(new(1f, 0f, 0f, 0.5f));
                if (Constants.ShouldPlaySfx() && sound) RPC.PlaySound(player.PlayerId, Sounds.KillSound);
            }
            else if (!ReactorCheck) player.ReactorFlash(0f); //リアクターフラッシュ
            player.MarkDirtySettings();
            _ = new LateTask(() =>
            {
                state.IsBlackOut = false; //ブラックアウト解除
                player.MarkDirtySettings();
            }, Options.KillFlashDuration.GetFloat(), "RemoveKillFlash");
        }
        public static void BlackOut(this IGameOptions opt, bool IsBlackOut)
        {
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
            if (IsBlackOut)
            {
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0);
                opt.SetFloat(FloatOptionNames.CrewLightMod, 0);
            }
            return;
        }
        /// <summary>
        /// seerが自分であるときのseenのRoleName + ProgressText
        /// </summary>
        /// <param name="seer">見る側</param>
        /// <param name="seen">見られる側</param>
        /// <returns>RoleName + ProgressTextを表示するか、構築する色とテキスト(bool, Color, string)</returns>
        public static (bool enabled, string text) GetRoleNameAndProgressTextData(PlayerControl seer, PlayerControl seen = null)
        {
            var roleName = GetDisplayRoleName(seer, seen);
            var progressText = GetProgressText(seer, seen);
            var text = roleName + (roleName != "" ? " " : "") + progressText;
            return (text != "", text);
        }
        /// <summary>
        /// GetDisplayRoleNameDataからRoleNameを構築
        /// </summary>
        /// <param name="seer">見る側</param>
        /// <param name="seen">見られる側</param>
        /// <returns>構築されたRoleName</returns>
        public static string GetDisplayRoleName(PlayerControl seer, PlayerControl seen = null)
        {
            seen ??= seer;
            //デフォルト値
            bool enabled = seer == seen
                        || seen.Is(CustomRoles.GM)
                        || (Main.VisibleTasksCount && !seer.IsAlive() && Options.GhostCanSeeOtherRoles.GetBool())
                        || (Options.ALoversRole.GetBool() && seer.Is(CustomRoles.ALovers) && seen.Is(CustomRoles.ALovers))
                        || (Options.BLoversRole.GetBool() && seer.Is(CustomRoles.BLovers) && seen.Is(CustomRoles.BLovers))
                        || (Options.CLoversRole.GetBool() && seer.Is(CustomRoles.CLovers) && seen.Is(CustomRoles.CLovers))
                        || (Options.DLoversRole.GetBool() && seer.Is(CustomRoles.DLovers) && seen.Is(CustomRoles.DLovers))
                        || (Options.ELoversRole.GetBool() && seer.Is(CustomRoles.ELovers) && seen.Is(CustomRoles.ELovers))
                        || (Options.FLoversRole.GetBool() && seer.Is(CustomRoles.FLovers) && seen.Is(CustomRoles.FLovers))
                        || (Options.GLoversRole.GetBool() && seer.Is(CustomRoles.GLovers) && seen.Is(CustomRoles.GLovers))
                        || (Options.InsiderMode.GetBool() && seer.GetCustomRole().IsImpostor())
                        || (Options.RoleImpostor.GetBool() && seer.GetCustomRole().IsImpostor() && seen.GetCustomRole().IsImpostor());
            var (roleColor, roleText) = GetTrueRoleNameData(seen.PlayerId);

            //seen側による変更
            seen.GetRoleClass()?.OverrideDisplayRoleNameAsSeen(seer, ref enabled, ref roleColor, ref roleText);

            //seer側による変更
            seer.GetRoleClass()?.OverrideDisplayRoleNameAsSeer(seen, ref enabled, ref roleColor, ref roleText);

            return enabled ? ColorString(roleColor, roleText) : "";
        }
        /// <summary>
        /// 引数の指定通りのRoleNameを表示
        /// </summary>
        /// <param name="mainRole">表示する役職</param>
        /// <param name="subRolesList">表示する属性のList</param>
        /// <returns>RoleNameを構築する色とテキスト(Color, string)</returns>
        public static (Color color, string text) GetRoleNameData(CustomRoles mainRole, List<CustomRoles> subRolesList, bool showSubRoleMarks = true)
        {
            string roleText = "";
            Color roleColor = Color.white;

            if (mainRole < CustomRoles.NotAssigned)
            {
                roleText = GetRoleName(mainRole);
                roleColor = GetRoleColor(mainRole);
            }

            if (subRolesList != null)
            {
                foreach (var subRole in subRolesList)
                {
                    if (subRole <= CustomRoles.NotAssigned) continue;
                    switch (subRole)
                    {
                        case CustomRoles.LastImpostor:
                            roleText = GetRoleString("Last-") + roleText;
                            break;
                        case CustomRoles.LastNeutral:
                            roleText = GetRoleString("Last-") + roleText;
                            break;
                    }
                }
            }

            string subRoleMarks = showSubRoleMarks ? GetSubRoleMarks(subRolesList) : "";
            if (roleText != "" && subRoleMarks != "")
                subRoleMarks = " " + subRoleMarks; //空じゃなければ空白を追加

            return (roleColor, roleText + subRoleMarks);
        }
        public static string GetSubRoleMarks(List<CustomRoles> subRolesList)
        {
            var sb = new StringBuilder(100);
            if (subRolesList != null)
            {
                foreach (var subRole in subRolesList)
                {
                    if (subRole <= CustomRoles.NotAssigned) continue;
                    switch (subRole)
                    {
                        case CustomRoles.Watcher:
                            sb.Append(Watcher.SubRoleMark);
                            break;
                        case CustomRoles.Speeding:
                            sb.Append(Speeding.SubRoleMark);
                            break;
                        case CustomRoles.Moon:
                            sb.Append(Moon.SubRoleMark);
                            break;
                        case CustomRoles.Guesser:
                            sb.Append(Guesser.SubRoleMark);
                            break;
                        case CustomRoles.Sun:
                            sb.Append(Sun.SubRoleMark);
                            break;
                        case CustomRoles.Director:
                            sb.Append(Director.SubRoleMark);
                            break;
                        case CustomRoles.Nurse:
                            sb.Append(Nurse.SubRoleMark);
                            break;
                        case CustomRoles.Connecting:
                            sb.Append(Connecting.SubRoleMark);
                            break;
                        case CustomRoles.Serial:
                            sb.Append(Serial.SubRoleMark);
                            break;
                        case CustomRoles.AdditionalVoter:
                            sb.Append(AdditionalVoter.SubRoleMark);
                            break;
                        case CustomRoles.Opener:
                            sb.Append(Opener.SubRoleMark);
                            break;
                        case CustomRoles.Psychic:
                            sb.Append(Psychic.SubRoleMark);
                            break;
                        case CustomRoles.Bakeneko:
                            sb.Append(Bakeneko.SubRoleMark);
                            break;
                        case CustomRoles.NotConvener:
                            sb.Append(NotConvener.SubRoleMark);
                            break;
                        case CustomRoles.Transparent:
                            sb.Append(Transparent.SubRoleMark);
                            break;
                        case CustomRoles.Notvoter:
                            sb.Append(Notvoter.SubRoleMark);
                            break;
                        case CustomRoles.Water:
                            sb.Append(Water.SubRoleMark);
                            break;
                        case CustomRoles.Slacker:
                            sb.Append(Slacker.SubRoleMark);
                            break;
                        case CustomRoles.LowBattery:
                            sb.Append(LowBattery.SubRoleMark);
                            break;
                        case CustomRoles.Elector:
                            sb.Append(Elector.SubRoleMark);
                            break;
                    }
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// 対象のRoleNameを全て正確に表示
        /// </summary>
        /// <param name="playerId">見られる側のPlayerId</param>
        /// <returns>RoleNameを構築する色とテキスト(Color, string)</returns>
        private static (Color color, string text) GetTrueRoleNameData(byte playerId, bool showSubRoleMarks = true)
        {
            var state = PlayerState.GetByPlayerId(playerId);
            var (color, text) = GetRoleNameData(state.MainRole, state.SubRoles, showSubRoleMarks);
            CustomRoleManager.GetByPlayerId(playerId)?.OverrideTrueRoleName(ref color, ref text);
            return (color, text);
        }
        /// <summary>
        /// 対象のRoleNameを全て正確に表示
        /// </summary>
        /// <param name="playerId">見られる側のPlayerId</param>
        /// <returns>構築したRoleName</returns>
        public static string GetTrueRoleName(byte playerId, bool showSubRoleMarks = true)
        {
            var (color, text) = GetTrueRoleNameData(playerId, showSubRoleMarks);
            return ColorString(color, text);
        }
        public static string GetRoleName(CustomRoles role)
        {
            return GetRoleString(Enum.GetName(typeof(CustomRoles), role));
        }
        public static string GetDeathReason(CustomDeathReason status)
        {
            return GetString("DeathReason." + Enum.GetName(typeof(CustomDeathReason), status));
        }
        public static Color GetRoleColor(CustomRoles role)
        {
            if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = role.GetRoleInfo()?.RoleColorCode;
            _ = ColorUtility.TryParseHtmlString(hexColor, out Color c);
            return c;
        }
        public static string GetRoleColorCode(CustomRoles role)
        {
            if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = role.GetRoleInfo()?.RoleColorCode;
            return hexColor;
        }
        /// <summary></summary>
        /// <param name="player">色表示にするプレイヤー</param>
        /// <param name="borudo">trueの場合ボールドで返します。</param>
        public static string GetPlayerColor(PlayerControl player, bool borudo = false)
        {
            if (player == null) return "";
            if (borudo) return "<b>" + ColorString(Main.PlayerColors[player.PlayerId], $"{player.name}</b>");
            else return ColorString(Main.PlayerColors[player.PlayerId], $"{player.name}");
        }
        /// <summary></summary>
        /// <param name="player">色表示にするプレイヤー</param>
        /// <param name="borudo">trueの場合ボールドで返します。</param>
        public static string GetPlayerColor(byte player, bool borudo = false)
        {
            if (GetPlayerById(player) == null) return "";
            if (borudo) return "<b>" + ColorString(Main.PlayerColors[player], $"{GetPlayerById(player).name}</b>");
            else return ColorString(Main.PlayerColors[player], $"{GetPlayerById(player).name}");
        }
        public static string GetFrom(SimpleRoleInfo info)
        {
            string Fromtext = "From:";
            switch (info.From)
            {
                case From.None: Fromtext = ""; break;
                case From.TheOtherRoles: Fromtext += $"<color=#ff0000>{info.From}</color>"; break;
                case From.TOR_GM_Edition: Fromtext += $"<color=#ff0000>{info.From}</color>"; break;
                case From.TOR_GM_Haoming_Edition: Fromtext += $"<color=#ff0000>TOR_GM_Haoming</color>"; break;
                case From.SuperNewRoles: Fromtext += "<color=#ffa500>Super</color><color=#ff0000>New</color><color=#00ff00>Roles</color>"; break;
                case From.ExtremeRoles: Fromtext += $"<color=#d3d3d3>{info.From}</color>"; break;
                case From.NebulaontheShip: Fromtext += $"<color=#191970>{info.From}</color>"; break;
                case From.au_libhalt_net: Fromtext += $"<color=#ffc0cb>{info.From}</color>"; break;
                case From.FoolersMod: Fromtext += $"<color=#00ff00>{info.From}</color>"; break;
                case From.SheriffMod: Fromtext += $"<color=#f8cd46>{info.From}</color>"; break;
                case From.Jester: Fromtext += $"<color=#ec62a5>{info.From}</color>"; break;
                case From.TownOfUs: Fromtext += $"<color=#daa520>{info.From}</color>"; break;
                case From.TownOfHost: Fromtext += $"<color=#00bfff>{info.From}</color>"; break;
                case From.TownOfHost_Y: Fromtext += $"<color=#ffff00>{info.From}</color>"; break;
                case From.TownOfHost_for_E: Fromtext += $"<color=#18e744>{info.From}</color>"; break;
                case From.TownOfHost_E: Fromtext += $"<color=#ffc0cb>{info.From}</color>"; break;
                case From.RevolutionaryHostRoles: Fromtext += $"<color=#00ff7f>{info.From}</color>"; break;
            }
            return Fromtext;
        }
        ///<summary>
        /// コンビネーション役職だと専用の名前を返す<br>
        /// それ以外は通常と同じ名前になる
        ///</summary>
        public static string GetCombinationCName(this CustomRoles role, bool color = true)
        {
            if (role.GetRoleInfo() is null || role.GetRoleInfo()?.Combination is null)
            {
                if (color)
                    return GetRoleName(role).Color(GetRoleColor(role));
                else
                    return GetRoleName(role);
            }

            if (color)
                return role.GetRoleInfo()?.Combination == CombinationRoles.None ? GetRoleName(role).Color(GetRoleColor(role)) : GetString(role.GetRoleInfo().Combination.ToString()).Color(GetRoleColor(role));
            return role.GetRoleInfo()?.Combination == CombinationRoles.None ? GetRoleName(role) : GetString(role.GetRoleInfo().Combination.ToString());
        }

        public static string GetVitalText(byte playerId, bool RealKillerColor = false)
        {
            var state = PlayerState.GetByPlayerId(playerId);
            string deathReason = state.IsDead ? GetString("DeathReason." + state.DeathReason) : GetString("Alive");
            if (RealKillerColor)
            {
                var KillerId = state.GetRealKiller();
                Color color = KillerId != byte.MaxValue ? Main.PlayerColors[KillerId] : GetRoleColor(CustomRoles.Doctor);
                deathReason = ColorString(color, deathReason);
            }
            return deathReason;
        }
        public static (string, Color) GetRoleTextHideAndSeek(RoleTypes oRole, CustomRoles hRole)
        {
            string text = "Invalid";
            Color color = Color.red;
            switch (oRole)
            {
                case RoleTypes.Impostor:
                case RoleTypes.Shapeshifter:
                    text = "Impostor";
                    color = Palette.ImpostorRed;
                    break;
                default:
                    switch (hRole)
                    {
                        case CustomRoles.Crewmate:
                            text = "Crewmate";
                            color = Color.white;
                            break;
                        case CustomRoles.HASFox:
                            text = "Fox";
                            color = Color.magenta;
                            break;
                        case CustomRoles.HASTroll:
                            text = "Troll";
                            color = Color.green;
                            break;
                    }
                    break;
            }
            return (text, color);
        }

        public static bool HasTasks(GameData.PlayerInfo p, bool ForRecompute = true)
        {
            if (GameStates.IsLobby) return false;
            //Tasksがnullの場合があるのでその場合タスク無しとする
            if (p.Tasks == null) return false;
            if (p.Role == null) return false;
            if (p.Disconnected) return false;

            var hasTasks = true;
            var States = PlayerState.GetByPlayerId(p.PlayerId);
            if (p.Role.IsImpostor)
                hasTasks = false; //タスクはCustomRoleを元に判定する
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                if (p.IsDead) hasTasks = false;
                if (States.MainRole is CustomRoles.HASFox or CustomRoles.HASTroll) hasTasks = false;
            }
            else
            {
                // 死んでいて，死人のタスク免除が有効なら確定でfalse
                if (p.IsDead && Options.GhostIgnoreTasks.GetBool())
                {
                    return false;
                }
                var role = States.MainRole;
                var roleClass = CustomRoleManager.GetByPlayerId(p.PlayerId);
                if (roleClass != null)
                {
                    switch (roleClass.HasTasks)
                    {
                        case HasTask.True:
                            hasTasks = true;
                            break;
                        case HasTask.False:
                            hasTasks = false;
                            break;
                        case HasTask.ForRecompute:
                            hasTasks = !ForRecompute;
                            break;
                    }
                }
                switch (role)
                {
                    case CustomRoles.GM:
                    case CustomRoles.SKMadmate:
                        hasTasks = false;
                        break;
                    default:
                        if (role.IsImpostor()) hasTasks = false;
                        break;
                }

                foreach (var subRole in States.SubRoles)
                    switch (subRole)
                    {
                        case CustomRoles.ALovers:
                            //ラバーズはタスクを勝利用にカウントしない
                            hasTasks &= !ForRecompute;
                            break;
                        case CustomRoles.BLovers:
                            hasTasks &= !ForRecompute;
                            break;
                        case CustomRoles.CLovers:
                            hasTasks &= !ForRecompute;
                            break;
                        case CustomRoles.DLovers:
                            hasTasks &= !ForRecompute;
                            break;
                        case CustomRoles.ELovers:
                            hasTasks &= !ForRecompute;
                            break;
                        case CustomRoles.FLovers:
                            hasTasks &= !ForRecompute;
                            break;
                        case CustomRoles.GLovers:
                            hasTasks &= !ForRecompute;
                            break;
                        case CustomRoles.MaLovers:
                            hasTasks &= !ForRecompute;
                            break;
                    }
            }
            return hasTasks;
        }
        private static string GetProgressText(PlayerControl seer, PlayerControl seen = null)
        {
            seen ??= seer;
            var comms = IsActive(SystemTypes.Comms);
            bool enabled = seer == seen
                        || (Main.VisibleTasksCount && !seer.IsAlive() && Options.GhostCanSeeOtherTasks.GetBool());
            string text = GetProgressText(seen.PlayerId, comms);

            if (Options.GhostCanSeeNumberOfButtonsOnOthers.GetBool() && !seer.IsAlive()) text += $"[{PlayerState.GetByPlayerId(seen.PlayerId).NumberOfRemainingButtons}]";

            //seer側による変更
            seer.GetRoleClass()?.OverrideProgressTextAsSeer(seen, ref enabled, ref text);

            return enabled ? text : "";
        }
        private static string GetProgressText(byte playerId, bool comms = false)
        {
            var ProgressText = new StringBuilder();
            var State = PlayerState.GetByPlayerId(playerId);
            if (State == null || GetPlayerById(playerId) == null) return "";
            var role = State.MainRole;
            var roleClass = CustomRoleManager.GetByPlayerId(playerId);
            ProgressText.Append(GetTaskProgressText(playerId, comms));
            if (roleClass != null)
            {
                ProgressText.Append(roleClass.GetProgressText(comms));
            }
            if (GetPlayerById(playerId).CanMakeMadmate()) ProgressText.Append(ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $"[{Options.CanMakeMadmateCount.GetInt() - Main.SKMadmateNowCount}]"));
            return ProgressText.ToString();
        }
        public static string GetTaskProgressText(byte playerId, bool comms = false)
        {
            var pc = GetPlayerById(playerId);
            if (pc == null) return "";
            var state = PlayerState.GetByPlayerId(playerId);
            if (state == null) return "";
            float t1 = 0;
            float t2 = 0;
            float pa = 0;
            foreach (var p in Main.AllPlayerControls)
            {
                var task = PlayerState.GetByPlayerId(p.PlayerId).taskState;
                if (task.hasTasks && HasTasks(p.Data))
                {
                    t1 += p.GetPlayerTaskState().AllTasksCount;
                    t2 += p.GetPlayerTaskState().CompletedTasksCount;
                    pa = t2 / t1;

                }
            }

            Color TextColor = Color.yellow;
            var info = GetPlayerInfoById(playerId);
            var TaskCompleteColor = HasTasks(info) ? Color.green : GetRoleColor(state.MainRole).ShadeColor(0.5f); //タスク完了後の色
            var NonCompleteColor = HasTasks(info) ? Color.yellow : Color.white; //カウントされない人外は白色

            if (Workhorse.IsThisRole(playerId))
                NonCompleteColor = Workhorse.RoleColor;

            var NormalColor = state.taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;

            TextColor = comms ? Color.gray : NormalColor;
            string Completed = comms ? "?" : $"{state.taskState.CompletedTasksCount}";
            float pas = pa * 100;
            double ret1 = Math.Round(pas);
            //ラスポスの処理
            if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.GiveDire.GetBool())
            {
                if ((!GameStates.Meeting && LastImpostor.Meeting.GetBool()) || (comms && !LastImpostor.comms.GetBool())) return ColorString(TextColor, $"<color=#cee4ae>[??]</color>");
                else if (!LastImpostor.PercentGage.GetBool()) return ColorString(TextColor, $"<color=#cee4ae>[{t2}/{t1}]</color>");
                else if (LastImpostor.PonkotuPercernt.GetBool())
                {//55%→5.5%→6%→60%
                    double ret = ret1 * 0.1f;
                    double ret2 = Math.Round(ret);
                    double ret3 = ret2 * 10;
                    return ColorString(TextColor, $"({Completed}/{state.taskState.AllTasksCount})  <color=#cee4ae>[{ret3}%]</color>");
                }
                else return ColorString(TextColor, $"<color=#cee4ae>[{ret1}%]</color>");
            }//ラスニュの処理
            else if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveDire.GetBool())
            {
                if ((!GameStates.Meeting && LastNeutral.Meeting.GetBool()) || (comms && !LastNeutral.comms.GetBool())) return ColorString(TextColor, $"<color=#cee4ae>[??]</color>");
                else if (!LastNeutral.PercentGage.GetBool()) return ColorString(TextColor, $"<color=#cee4ae>[{t2}/{t1}]</color>");
                else if (LastNeutral.PonkotuPercernt.GetBool())
                {//55%→5.5%→6%→60%
                    double ret = ret1 * 0.1f;
                    double ret2 = Math.Round(ret);
                    double ret3 = ret2 * 10;
                    return ColorString(TextColor, $"({Completed}/{state.taskState.AllTasksCount})  <color=#cee4ae>[{ret3}%]</color>");
                }
                else return ColorString(TextColor, $"<color=#cee4ae>[{ret1}%]</color>");

            }
            else//ディレクターの処理
            if (pc.Is(CustomRoles.Director) && GameStates.IsInGame)
            {
                if (((comms && !Director.comms) || (!GameStates.Meeting && Director.Meeting.GetBool())) && (state == null || state.taskState == null || !state.taskState.hasTasks))
                    return $"<color=#cee4ae>[??]</color>";
                else if (state == null || state.taskState == null || (!state.taskState.hasTasks && !Director.PercentGage))
                    return $"<color=#cee4ae>[{t2}/{t1}]</color>";
                else if (state == null || state.taskState == null || (!state.taskState.hasTasks && Director.PercentGage))
                    return $"<color=#cee4ae>[{ret1}%]</color>";
                else if ((!GameStates.Meeting && Director.Meeting.GetBool()) || (comms && !Director.comms))
                    return ColorString(TextColor, $"({Completed}/{state.taskState.AllTasksCount})  <color=#cee4ae>[??]</color>");
                else if (!Director.PercentGage)
                    return ColorString(TextColor, $"({Completed}/{state.taskState.AllTasksCount})  <color=#cee4ae>[{t2}/{t1}]</color>");
                else if (Director.PonkotuPercernt.GetBool())
                {//55%→5.5%→6%→60%
                    double ret = ret1 * 0.1f;
                    double ret2 = Math.Round(ret);
                    double ret3 = ret2 * 10;
                    return ColorString(TextColor, $"({Completed}/{state.taskState.AllTasksCount})  <color=#cee4ae>[{ret3}%]</color>");
                }
                else return ColorString(TextColor, $"({Completed}/{state.taskState.AllTasksCount})  <color=#cee4ae>[{ret1}%]</color>");
            }
            if (!pc.IsAlive() && Options.GhostIgnoreAllTasks.GetBool() && GameStates.IsInGame)//死んでて霊界がタスク進捗を見れるがON
            {
                if (state == null || state.taskState == null || !state.taskState.hasTasks)
                    return $"<color=#cee4ae>[{t2}/{t1}]</color>";
                else return ColorString(TextColor, $"({Completed}/{state.taskState.AllTasksCount})  <color=#cee4ae>[{t2}/{t1}]</color>");
            }
            if (state == null || state.taskState == null || !state.taskState.hasTasks) return "";
            else return ColorString(TextColor, $"({Completed}/{state.taskState.AllTasksCount})");
        }
        public static void ShowActiveSettingsHelp(byte PlayerId = byte.MaxValue)
        {
            if ((Options.HideGameSettings.GetBool() || (Options.HideSettingsDuringGame.GetBool() && GameStates.IsInGame)) && PlayerId != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), PlayerId);
                return;
            }
            SendMessage(GetString("CurrentActiveSettingsHelp") + ":", PlayerId);
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SendMessage(GetString("HideAndSeekInfo"), PlayerId);
                if (CustomRoles.HASFox.IsEnable()) { SendMessage(GetRoleName(CustomRoles.HASFox) + GetString("HASFoxInfoLong"), PlayerId); }
                if (CustomRoles.HASTroll.IsEnable()) { SendMessage(GetRoleName(CustomRoles.HASTroll) + GetString("HASTrollInfoLong"), PlayerId); }
            }
            else
            {
                if (Options.DisableDevices.GetBool()) { SendMessage(GetString("DisableDevicesInfo"), PlayerId); }
                if (Options.SyncButtonMode.GetBool()) { SendMessage(GetString("SyncButtonModeInfo"), PlayerId); }
                if (Options.SabotageTimeControl.GetBool()) { SendMessage(GetString("SabotageTimeControlInfo"), PlayerId); }
                if (Options.RandomMapsMode.GetBool()) { SendMessage(GetString("RandomMapsModeInfo"), PlayerId); }
                if (Options.InsiderMode.GetBool()) { SendMessage(GetString("InsiderModeInfo"), PlayerId); }
                if (Options.IsStandardHAS) { SendMessage(GetString("StandardHASInfo"), PlayerId); }
                if (Options.EnableGM.GetBool()) { SendMessage(GetRoleName(CustomRoles.GM) + GetString("GMInfoLong"), PlayerId); }
                foreach (var role in CustomRolesHelper.AllStandardRoles)
                {
                    if (role.IsEnable())
                    {
                        if (role.GetRoleInfo()?.Description is { } description)
                        {
                            SendMessage(description.FullFormatHelp, PlayerId, removeTags: false);
                        }
                        // RoleInfoがない役職は従来処理
                        else
                        {
                            SendMessage(GetRoleName(role) + "\n\n" + GetString(Enum.GetName(typeof(CustomRoles), role) + "InfoLong"), PlayerId);
                        }
                    }
                }
                foreach (var role in CustomRolesHelper.AllAddOns)
                    if (role.IsEnable())
                    {
                        SendMessage(GetAddonsHelp(role), PlayerId);
                    }
            }
            if (Options.NoGameEnd.GetBool()) { SendMessage(GetString("NoGameEndInfo"), PlayerId); }
        }
        public static void ShowActiveSettings(byte PlayerId = byte.MaxValue)
        {
            var mapId = Main.NormalOptions.MapId;
            if ((Options.HideGameSettings.GetBool() || (Options.HideSettingsDuringGame.GetBool() && GameStates.IsInGame)) && PlayerId != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), PlayerId);
                return;
            }
            var sb = new StringBuilder().AppendFormat("<line-height={0}>", ActiveSettingsLineHeight);
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                sb.Append(GetString("Roles")).Append(':');
                if (CustomRoles.HASFox.IsEnable()) sb.AppendFormat("\n{0}:{1}", GetRoleName(CustomRoles.HASFox), CustomRoles.HASFox.GetCount());
                if (CustomRoles.HASTroll.IsEnable()) sb.AppendFormat("\n{0}:{1}", GetRoleName(CustomRoles.HASTroll), CustomRoles.HASTroll.GetCount());
                SendMessage(sb.ToString(), PlayerId);
                sb.Clear().Append(GetString("Settings")).Append(':');
                sb.Append(GetString("HideAndSeek"));
            }
            else
            {
                sb.AppendFormat("<size={0}>", ActiveSettingsSize);
                sb.Append("<size=100%>").Append(GetString("Settings")).Append('\n').Append("</size>");
                sb.AppendFormat("\n【{0}: {1}】\n", RoleAssignManager.OptionAssignMode.GetName(true), RoleAssignManager.OptionAssignMode.GetString());
                if (RoleAssignManager.OptionAssignMode.GetBool())
                {
                    ShowChildrenSettings(RoleAssignManager.OptionAssignMode, ref sb);
                    CheckPageChange(PlayerId, sb);
                }
                foreach (var role in Options.CustomRoleCounts)
                {
                    if (!role.Key.IsEnable()) continue;
                    if (role.Key is CustomRoles.HASFox or CustomRoles.HASTroll) continue;

                    sb.Append($"\n【{GetCombinationCName(role.Key)}×{role.Key.GetCount()}】\n");
                    ShowChildrenSettings(Options.CustomRoleSpawnChances[role.Key], ref sb);
                    CheckPageChange(PlayerId, sb);
                }
                foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Id >= 80000 && !x.IsHiddenOn(Options.CurrentGameMode)))
                {
                    if (opt.Name is "RandomSpawn")
                    {
                        foreach (var randomOpt in opt.Children)
                        {
                            var Id = randomOpt.Id / 100;
                            //マップIDor6(カススポ)
                            if (Id % 10 != mapId && Id % 100 != 5 && Id % 10 != 9) continue;
                            //現在のマップのみ表示する
                            if (randomOpt.GetBool())
                            {
                                //Onの時は頭に改ページを入れる
                                CheckPageChange(PlayerId, sb, true);
                                sb.Append($"\n【{opt.GetName(false)}】");
                                sb.Append($"\n {randomOpt.GetName(true)}: {randomOpt.GetString()}\n");

                                ShowChildrenSettings(randomOpt, ref sb, 1);
                            }
                            else
                            {
                                //オフならそのままで大丈夫
                                sb.Append($"\n【{opt.GetName(false)}】");
                                sb.Append($"\n {randomOpt.GetName(false)}: {randomOpt.GetString()}\n");
                            }
                        }
                        CheckPageChange(PlayerId, sb);
                    }
                    else
                    {
                        if (opt.Name is "KillFlashDuration" or "RoleAssigningAlgorithm")
                            sb.Append($"\n【{opt.GetName(true)}: {opt.GetString()}】\n");
                        else
                        if (opt.Name is "KickModClient" or "KickPlayerFriendCodeNotExist" or "ApplyDenyNameList" or "ApplyBanList")
                            sb.Append($"\n【{opt.GetName(true)}】\n");
                        else sb.Append($"\n【{opt.GetName(false)}】\n");
                        ShowChildrenSettings(opt, ref sb);
                        CheckPageChange(PlayerId, sb);
                    }
                }
            }
            SendMessage(sb.ToString(), PlayerId, removeTags: false);
        }
        private static void CheckPageChange(byte PlayerId, StringBuilder sb, bool force = false, string title = "")
        {
            //2Byte文字想定で1000byt越えるならページを変える
            if (force || sb.Length > 500)
            {
                SendMessage(sb.ToString(), PlayerId, title, removeTags: false);
                sb.Clear();
                sb.AppendFormat("<line-height=55%><size={0}>", ActiveSettingsSize);
            }
        }
        public static void CopyCurrentSettings()
        {
            var sb = new StringBuilder();
            if ((Options.HideGameSettings.GetBool() || (Options.HideSettingsDuringGame.GetBool() && GameStates.IsInGame)) && !AmongUsClient.Instance.AmHost)
            {
                ClipboardHelper.PutClipboardString(GetString("Message.HideGameSettings"));
                return;
            }
            sb.Append($"━━━━━━━━━━━━【{GetString("Roles")}】━━━━━━━━━━━━");
            foreach (var role in Options.CustomRoleCounts)
            {
                if (!role.Key.IsEnable()) continue;
                sb.Append($"\n【{GetCombinationCName(role.Key)}×{role.Key.GetCount()}】\n");
                ShowChildrenSettings(Options.CustomRoleSpawnChances[role.Key], ref sb);
                var text = sb.ToString();
                sb.Clear().Append(text.RemoveHtmlTags());
            }
            sb.Append($"━━━━━━━━━━━━【{GetString("Settings")}】━━━━━━━━━━━━");
            foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Id >= 80000 && !x.IsHiddenOn(Options.CurrentGameMode)))
            {
                if (opt.Name == "KillFlashDuration")
                    sb.Append($"\n【{opt.GetName(true)}: {opt.GetString()}】\n");
                else
                    sb.Append($"\n【{opt.GetName(true)}】\n");
                ShowChildrenSettings(opt, ref sb);
                var text = sb.ToString();
                sb.Clear().Append(text.RemoveHtmlTags());
            }
            sb.Append($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            ClipboardHelper.PutClipboardString(sb.ToString());
        }
        public static void ShowActiveRoles(byte PlayerId = byte.MaxValue)
        {
            if ((Options.HideGameSettings.GetBool() || (Options.HideSettingsDuringGame.GetBool() && GameStates.IsInGame)) && PlayerId != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), PlayerId);
                return;
            }
            SendMessage(GetActiveRoleText(), PlayerId, removeTags: false);
        }

        public static string GetActiveRoleText()
        {
            var sb = new StringBuilder().AppendFormat("<line-height={0}>", ActiveSettingsLineHeight);
            sb.AppendFormat("<size={0}>", ActiveSettingsSize);
            sb.AppendFormat("\n◆{0}:{1}", GetRoleName(CustomRoles.GM), Options.EnableGM.GetString());
            sb.Append("\n<size=100%>\n").Append(GetString("Roles")).Append('\n').Append("</size>");
            CustomRoles[] roles = null;
            CustomRoles[] addons = null;
            if (Options.CurrentGameMode == CustomGameMode.Standard) roles = CustomRolesHelper.AllStandardRoles;
            if (Options.CurrentGameMode == CustomGameMode.Standard) addons = CustomRolesHelper.AllAddOns;
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) roles = CustomRolesHelper.AllHASRoles;
            if (roles != null)
            {
                foreach (CustomRoles role in roles)
                {//Roles
                    if (role.IsEnable()) sb.AppendFormat("\n●{0}:{1}x{2}", role.GetCombinationCName(), $"{role.GetChance()}%", role.GetCount());
                }
            }
            if (addons != null)
            {
                sb.Append("\n<size=100%>\n").Append(GetString("Addons")).Append('\n').Append("</size>");
                foreach (CustomRoles Addon in addons)
                {
                    if (Addon.IsEnable()) sb.AppendFormat("\n★{0}:{1}x{2}", GetRoleName(Addon).Color(GetRoleColor(Addon)), $"{Addon.GetChance()}%", Addon.GetCount());
                }
            }
            return sb.ToString();
        }
        public static void WH_ShowActiveRoles(byte PlayerId = byte.MaxValue)
        {
            StringBuilder sb;
            if (Options.CurrentGameMode == CustomGameMode.Standard)
            {
                sb = new StringBuilder(GetString("Roles")).Append(':');
                sb.AppendFormat("\n {0}:{1}", GetRoleName(CustomRoles.GM), Options.EnableGM.GetString().RemoveHtmlTags());
                var rr = CustomRoleTypes.Neutral;//☆インポスターも表示させるため
                foreach (CustomRoles role in CustomRolesHelper.AllRoles)
                {
                    if (rr != role.GetCustomRoleTypes() && role.IsEnable())
                    {
                        rr = role.GetCustomRoleTypes();
                        if (role is
                            //ラスト
                            CustomRoles.Workhorse or
                            CustomRoles.LastImpostor or
                            CustomRoles.LastNeutral or
                            //バフ
                            CustomRoles.Watcher or
                            CustomRoles.Speeding or
                            CustomRoles.Moon or
                            CustomRoles.Guesser or
                            CustomRoles.Sun or
                            CustomRoles.Director or
                            CustomRoles.Connecting or
                            CustomRoles.Serial or
                            CustomRoles.Opener or
                            CustomRoles.Bakeneko or
                            CustomRoles.Psychic or
                            CustomRoles.Nurse or
                            //デバフ
                            CustomRoles.NotConvener or
                            CustomRoles.Notvoter or
                            CustomRoles.Water or
                            CustomRoles.LowBattery or
                            CustomRoles.Slacker or
                            CustomRoles.Elector or
                            //第三
                            CustomRoles.ALovers or
                            CustomRoles.BLovers or
                            CustomRoles.CLovers or
                            CustomRoles.DLovers or
                            CustomRoles.FLovers or
                            CustomRoles.ELovers or
                            CustomRoles.GLovers or
                            CustomRoles.MaLovers
                            )
                            sb.AppendFormat($"\n ☆{GetString("Addons")}");
                        else
                        {
                            sb.AppendFormat($"\n ☆{GetString($"{rr}")}");
                        }
                    }
                    if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                    if (role.IsEnable()) sb.AppendFormat("\n　{0}:{1}x{2}", role.GetCombinationCName(false), $"{role.GetChance()}%", role.GetCount());
                }
            }
            else
            {
                sb = new StringBuilder(GetString(Options.CurrentGameMode.ToString()))
                .Append("\n\n").Append(GetString("TaskPlayerB") + ":");
                foreach (var pc in Main.AllPlayerControls)
                    sb.Append("\n  " + pc.name);
            }
            Webhook.Send(sb.ToString());
        }
        public static void ShowChildrenSettings(OptionItem option, ref StringBuilder sb, int deep = 0, bool Askesu = false)
        {
            foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
            {
                if (opt.Value.Name == "Maximum") continue; //Maximumの項目は飛ばす
                if (opt.Value.Name == "FixedRole") continue;
                if (opt.Value.Name == "DisableSkeldDevices" && !Options.IsActiveSkeld) continue;
                if (opt.Value.Name == "DisableMiraHQDevices" && !Options.IsActiveMiraHQ) continue;
                if (opt.Value.Name == "DisablePolusDevices" && !Options.IsActivePolus) continue;
                if (opt.Value.Name == "PolusReactorTimeLimit" && !Options.IsActivePolus) continue;
                if (opt.Value.Name == "DisableAirshipDevices" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "AirshipReactorTimeLimit" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "DisableFungleDevices" && !Options.IsActiveFungle) continue;
                if (opt.Value.Name == "FungleReactorTimeLimit" && !Options.IsActiveFungle) continue;
                if (opt.Value.Name == "FungleMushroomMixupDuration" && !Options.IsActiveFungle) continue;
                if (opt.Value.Name == "DisableFungleSporeTrigger" && !Options.IsActiveFungle) continue;
                if (opt.Value.Name == "ResetDoorsEveryTurns" && !(Options.IsActiveFungle || Options.IsActiveAirship || Options.IsActivePolus)) continue;
                if (opt.Value.Name == "AirShipVariableElectrical" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "DisableAirshipMovingPlatform" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "DisableAirshipViewingDeckLightsPanel" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "DisableAirshipCargoLightsPanel" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "DisableAirshipGapRoomLightsPanel" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "ResetDoorsEveryTurns" && !(Options.IsActiveSkeld || Options.IsActiveMiraHQ || Options.IsActiveAirship || Options.IsActivePolus)) continue;
                if (Askesu) if (opt.Value.Name == "%roleTypes%Maximum") continue;

                if (deep > 0)
                {
                    sb.Append(string.Concat(Enumerable.Repeat("┃", Mathf.Max(deep - 1, 0))));
                    sb.Append(opt.Index == option.Children.Count ? "┗ " : "┣ ");
                }
                sb.Append($"{opt.Value.GetName(true).RemoveHtmlTags()}: {opt.Value.GetString()}\n");
                if (opt.Value.GetBool()) ShowChildrenSettings(opt.Value, ref sb, deep + 1);
            }
        }
        public static string ShowAddonSet(OptionItem option, int deep = 0)
        {
            var sb = "";
            foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
            {
                if (opt.Value.Name == "Maximum") continue; //Maximumの項目は飛ばす
                if (opt.Value.Name == "FixedRole") continue;
                if (opt.Value.Name == "%roleTypes%Maximum") continue;

                if (deep > 0)
                {
                    sb += string.Concat(Enumerable.Repeat("┃", Mathf.Max(deep - 1, 0)));
                    sb += opt.Index == option.Children.Count ? "┗ " : "┣ ";
                }
                sb += $"{opt.Value.GetName(true).RemoveHtmlTags()}: {opt.Value.GetString()}\n";
                if (opt.Value.GetBool()) ShowAddonSet(opt.Value, deep + 1);
            }
            return sb;
        }
        public static void ShowLastResult(byte PlayerId = byte.MaxValue)
        {
            if (AmongUsClient.Instance.IsGameStarted)
            {
                SendMessage(GetString("CantUse.lastresult"), PlayerId);
                return;
            }
            var sb = new StringBuilder();

            var winnerColor = ((CustomRoles)CustomWinnerHolder.WinnerTeam).GetRoleInfo()?.RoleColor ?? Palette.DisabledGrey;

            sb.Append("""<align="center">""");
            sb.Append("<size=150%>").Append(GetString("LastResult")).Append("</size>");
            sb.Append('\n').Append(SetEverythingUpPatch.LastWinsText.Mark(winnerColor, false));
            sb.Append("</align>");

            sb.Append("<size=70%>\n");
            List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);

            foreach (var pc in cloneRoles) if (GetPlayerById(pc) == null) continue;

            foreach (var id in Main.winnerList)
            {
                sb.Append($"\n★ ".Color(winnerColor)).Append(SummaryTexts(id, true));
                cloneRoles.Remove(id);
            }
            foreach (var id in cloneRoles)
            {
                sb.Append($"\n　 ").Append(SummaryTexts(id, true));
            }
            SendMessage(sb.ToString(), PlayerId, removeTags: false);
        }
        public static void WH_ShowLastResult(byte PlayerId = byte.MaxValue)
        {
            var sb = new StringBuilder();

            var winnerColor = ((CustomRoles)CustomWinnerHolder.WinnerTeam).GetRoleInfo()?.RoleColor ?? Palette.DisabledGrey;
            var tb = Options.CurrentGameMode == CustomGameMode.TaskBattle;
            sb.Append(GetString("LastResult"));
            if (!tb) sb.Append('\n').Append(SetEverythingUpPatch.LastWinsText).Append('\n');
            List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);
            foreach (var id in Main.winnerList)
            {
                var sr = GetSubRolesText(id).RemoveColorTags() != "";
                sb.Append("\n★ ").Append(Main.AllPlayerNames[id]);
                if (!tb)
                {
                    sb.Append("\n┣  ").Append(GetVitalText(id)).Append('　');
                    sb.Append(GetProgressText(id).RemoveColorTags());
                    sb.Append(sr ? "\n┣  " : "\n┗   ").Append(GetTrueRoleName(id, false).RemoveColorTags());
                    if (sr) sb.Append("\n┗  ").Append(GetSubRolesText(id).RemoveColorTags());
                }
                else
                {
                    sb.Append('\n').Append($"{Main.AllPlayerNames[id]}{GetString("Win")}").Append('\n');
                    sb.Append('　').Append(GetProgressText(id).RemoveColorTags());
                }
                cloneRoles.Remove(id);
            }
            foreach (var id in cloneRoles)
            {
                var sr = GetSubRolesText(id).RemoveColorTags() != "";
                sb.Append("\n〇 ").Append(Main.AllPlayerNames[id]);
                if (!tb)
                {
                    sb.Append("\n┣  ").Append(GetVitalText(id)).Append('　');
                    sb.Append(GetProgressText(id).RemoveColorTags());
                    sb.Append(sr ? "\n┣  " : "\n┗   ").Append(GetTrueRoleName(id, false).RemoveColorTags());
                    if (sr) sb.Append("\n┗  ").Append(GetSubRolesText(id).RemoveColorTags());
                }
                else
                    sb.Append('　').Append(GetProgressText(id).RemoveColorTags());
            }
            Webhook.Send(sb.ToString());
        }
        public static void ShowLastWins(byte PlayerId = byte.MaxValue)
        {
            SendMessage("<size=0>", PlayerId, $"{SetEverythingUpPatch.LastWinsText}");
        }
        public static void ShowTimer(byte PlayerId = byte.MaxValue) => SendMessage(GetTimer(), PlayerId);
        public static string GetTimer()
        {
            var sb = new StringBuilder();
            float timerValue = GameStartManagerPatch.GetTimer();
            int minutes = (int)timerValue / 60;
            int seconds = (int)timerValue % 60;
            return $"{minutes:00}:{seconds:00}";
        }
        public static void ShowKillLog(byte PlayerId = byte.MaxValue)
        {
            if (GameStates.IsInGame)
            {
                SendMessage(GetString("CantUse.killlog"), PlayerId);
                return;
            }
            SendMessage(EndGamePatch.KillLog, PlayerId, removeTags: false);
        }
        public static string GetSubRolesText(byte id, bool disableColor = false)
        {
            var SubRoles = PlayerState.GetByPlayerId(id).SubRoles;
            if (SubRoles.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var role in SubRoles)
            {
                if (role is CustomRoles.NotAssigned or
                            CustomRoles.LastImpostor or
                            CustomRoles.LastNeutral
                ) continue;

                var RoleText = disableColor ? GetRoleName(role) : ColorString(GetRoleColor(role), GetRoleName(role));
                sb.Append($"{ColorString(Color.gray, " + ")}{RoleText}");
            }

            return sb.ToString();
        }
        public static void SendRoleInfo(PlayerControl player)
        {
            var role = player.GetCustomRole();
            if (role == CustomRoles.Braid) role = CustomRoles.Driver;
            if (!role.IsVanilla() && role.GetRoleInfo()?.Description is { } description)
            {
                var RoleTextData = GetRoleColorCode(role);
                String RoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                String RoleInfoTitle = $"<color={RoleTextData}>{RoleInfoTitleString}";
                Utils.SendMessage(description.FullFormatHelp, player.PlayerId, title: RoleInfoTitle, removeTags: false);
            }
            // roleInfoがない役職
            else
            {
                var RoleTextData = GetRoleColorCode(role);
                //var SendRoleInfo = "";
                String RoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                String RoleInfoTitle = $"<color={RoleTextData}>{RoleInfoTitleString}";
                {
                    Utils.SendMessage("<b><line-height=2.0pic><size=150%>" + GetString(role.ToString()).Color(player.GetRoleColor()) + "</b>\n<size=90%><line-height=1.8pic>" + player.GetRoleInfo(true), player.PlayerId, RoleInfoTitle);
                }
            }
            //addon(一回これで応急手当。)
            GetAddonsHelp(player);
        }
        public static void GetAddonsHelp(PlayerControl player)
        {
            var AddRoleTextData = GetRoleColorCode(player.GetCustomRole());
            var AddRoleInfoTitleString = $"{GetString("AddonInfoTitle")}";
            var AddRoleInfoTitle = $"<color={AddRoleTextData}>{AddRoleInfoTitleString}";
            var s = new StringBuilder();
            var sb = new StringBuilder();
            var lp = new StringBuilder();
            var ln = new StringBuilder();
            var wh = new StringBuilder();
            ShowChildrenSettings(Options.CustomRoleSpawnChances[CustomRoles.Guesser], ref sb);
            ShowChildrenSettings(Options.CustomRoleSpawnChances[CustomRoles.LastImpostor], ref lp);
            ShowChildrenSettings(Options.CustomRoleSpawnChances[CustomRoles.LastNeutral], ref ln);
            ShowChildrenSettings(Options.CustomRoleSpawnChances[CustomRoles.Workhorse], ref wh);

            var k = "<line-height=2.0pic><size=100%>~~~~~~~~~~~~~~~~~~~~~~~~\n\n<size=150%><b>";
            //バフ
            if (player.Is(CustomRoles.Guesser)) s.Append(k + "<color=#999900>" + GetString("Guesser") + " ∮\n<size=90%><line-height=1.8pic>" + GetString("GuesserInfo") + "\n</b></color><size=60%>From:<color=#ff0000>The Other Roles</color></size>\n<size=60%></color><line-height=1.3pic><line-height=1.3pic>" + GetString("GuesserInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + sb.ToString() + "\n");
            CheckPageChange(player.PlayerId, s, title: AddRoleInfoTitle);
            if (player.Is(CustomRoles.Serial)) s.Append(k + "<color=#ff1919>" + GetString("Serial") + " ∂\n<size=90%><line-height=1.8pic>" + GetString("SerialInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("SerialInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.Serial]) + "\n");
            if (player.Is(CustomRoles.Connecting)) s.Append(k + "<color=#96514d>" + GetString("Connecting") + " Ψ\n<size=90%><line-height=1.8pic>" + GetString("ConnectingInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("ConnectingInfoLong") + "\n");
            if (player.Is(CustomRoles.Watcher)) s.Append(k + "<color=#800080>" + GetString("Watcher") + " ∑\n<size=90%><line-height=1.8pic>" + GetString("WatcherInfo") + "\n</b></color><size=60%>From:<color=#ff0000>TOR GM Edition</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("WatcherInfoLong") + "\n");
            if (player.Is(CustomRoles.AdditionalVoter)) s.Append(k + "<color=#93ca76>" + GetString("AdditionalVoter") + " Å\n<size=90%><line-height=1.8pic>" + GetString("AdditionalVoterInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("AdditionalVoterInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.AdditionalVoter]) + "\n");
            if (player.Is(CustomRoles.Nurse)) s.Append(k + "<color=#ffadd6>" + GetString("Nurse") + " Й\n<size=90%><line-height=1.8pic>" + GetString("NurseInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("NurseInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.Nurse]) + "\n");
            CheckPageChange(player.PlayerId, s, title: AddRoleInfoTitle);
            if (player.Is(CustomRoles.Bakeneko)) s.Append(k + "<color=#ffcc99>" + GetString("Bakeneko") + " =^^=\n<size=90%><line-height=1.8pic>" + GetString("BakenekoInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("BakenekoInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.Bakeneko]) + "\n");
            if (player.Is(CustomRoles.Speeding)) s.Append(k + "<color=#33ccff>" + GetString("Speeding") + " ∈\n<size=90%><line-height=1.8pic>" + GetString("SpeedingInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("SpeedingInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.Speeding]) + "\n");
            if (player.Is(CustomRoles.Director)) s.Append(k + "<color=#cee4ae>" + GetString("Director") + " θ\n<size=90%><line-height=1.8pic>" + GetString("DirectorInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("DirectorInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.Director]) + "\n");
            if (player.Is(CustomRoles.Opener)) s.Append(k + "<color=#007bbb>" + GetString("Opener") + " п\n<size=90%><line-height=1.8pic>" + GetString("OpenerInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("OpenerInfoLong") + "\n");
            if (player.Is(CustomRoles.Psychic)) s.Append(k + "<color=#9933ff>" + GetString("Psychic") + " ☯\n<size=90%><line-height=1.8pic>" + GetString("PsychicInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("PsychicInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.Psychic]) + "\n");
            CheckPageChange(player.PlayerId, s, title: AddRoleInfoTitle);
            if (player.Is(CustomRoles.Sun)) s.Append(k + "<color=#ec6800>" + GetString("Sun") + " ＊\n<size=90%><line-height=1.8pic>" + GetString("SunInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("SunInfoLong") + "\n");
            if (player.Is(CustomRoles.Moon)) s.Append(k + "<color=#ffff33>" + GetString("Moon") + " э\n<size=90%><line-height=1.8pic>" + GetString("MoonInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("MoonInfoLong") + "\n");

            CheckPageChange(player.PlayerId, s, title: AddRoleInfoTitle);
            //デバフ
            if (player.Is(CustomRoles.Notvoter)) s.Append(k + "<color=#6c848d>" + GetString("Notvoter") + " Ｖ\n<size=90%><line-height=1.8pic>" + GetString("NotvoterInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("NotvoterInfoLong") + "\n");
            if (player.Is(CustomRoles.Elector)) s.Append(k + "<color=#544a47>" + GetString("Elector") + " Ｅ\n<size=90%><line-height=1.8pic>" + GetString("ElectorInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("ElectorInfoLong") + "\n");
            if (player.Is(CustomRoles.NotConvener)) s.Append(k + "<color=#006666>" + GetString("NotConvener") + " Ｃ\n<size=90%><line-height=1.8pic>" + GetString("NotConvenerInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("NotConvenerInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.NotConvener]) + "\n");
            if (player.Is(CustomRoles.Transparent)) s.Append(k + "<color=#7b7c7d>" + GetString("Transparent") + " Ｔ\n<size=90%><line-height=1.8pic>" + GetString("TransparentInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("TransparentInfoLong") + "\n");
            if (player.Is(CustomRoles.Water)) s.Append(k + "<color=#17184b>" + GetString("Water") + " Ｗ\n<size=90%><line-height=1.8pic>" + GetString("WaterInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("WaterInfoLong") + "\n");
            if (player.Is(CustomRoles.LowBattery)) s.Append(k + "<color=#250d00>" + GetString("LowBattery") + " Ｌ\n<size=90%><line-height=1.8pic>" + GetString("LowBatteryInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("LowBatteryInfoLong") + "\n");
            if (player.Is(CustomRoles.Slacker)) s.Append(k + "<color=#460e44>" + GetString("Slacker") + " ＳＬ\n<size=90%><line-height=1.8pic>" + GetString("SlackerInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("SlackerInfoLong") + "\n");

            CheckPageChange(player.PlayerId, s, title: AddRoleInfoTitle);
            //第三
            if (player.Is(CustomRoles.ALovers)) s.Append(k + "<color=#ff6be4>" + GetString("ALovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("ALoversInfo") + "\n</b></color><size=60%>From:<color=#ff6be4>Love Couple Mod</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LoversInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.ALovers]) + "\n");
            if (player.Is(CustomRoles.BLovers)) s.Append(k + "<color=#d70035>" + GetString("BLovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("BLoversInfo") + "\n</b></color><size=60%>From:<color=#ff6be4>Love Couple Mod</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LoversInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.BLovers]) + "\n");
            if (player.Is(CustomRoles.CLovers)) s.Append(k + "<color=#fac559>" + GetString("CLovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("CLoversInfo") + "\n</b></color><size=60%>From:<color=#ff6be4>Love Couple Mod</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LoversInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.CLovers]) + "\n");
            if (player.Is(CustomRoles.DLovers)) s.Append(k + "<color=#6c9bd2>" + GetString("DLovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("DLoversInfo") + "\n</b></color><size=60%>From:<color=#ff6be4>Love Couple Mod</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LoversInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.DLovers]) + "\n");
            if (player.Is(CustomRoles.ELovers)) s.Append(k + "<color=#00885a>" + GetString("ELovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("ELoversInfo") + "\n</b></color><size=60%>From:<color=#ff6be4>Love Couple Mod</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LoversInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.ELovers]) + "\n");
            if (player.Is(CustomRoles.FLovers)) s.Append(k + "<color=#fdede4>" + GetString("FLovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("FLoversInfo") + "\n</b></color><size=60%>From:<color=#ff6be4>Love Couple Mod</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LoversInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.FLovers]) + "\n");
            if (player.Is(CustomRoles.GLovers)) s.Append(k + "<color=#af0082>" + GetString("GLovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("GLoversInfo") + "\n</b></color><size=60%>From:<color=#ff6be4>Love Couple Mod</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LoversInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ShowAddonSet(Options.CustomRoleSpawnChances[CustomRoles.GLovers]) + "\n");
            if (player.Is(CustomRoles.MaLovers)) s.Append(k + "<color=#f09199>" + GetString("MaLovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("MaLoversInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("MaLoversInfoLong") + "\n");
            CheckPageChange(player.PlayerId, s, title: AddRoleInfoTitle);
            //ラスト系
            if (player.Is(CustomRoles.LastImpostor)) s.Append(k + "<color=#ff1919>" + GetString("LastImpostor") + "\n<size=90%><line-height=1.8pic>" + GetString("LastImpostorInfo") + "\n</b></color><size=60%>From:<color=#00bfff>Town Of Host</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LastImpostorInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + lp.ToString() + "\n");
            if (player.Is(CustomRoles.LastNeutral)) s.Append(k + "<color=#cccccc>" + GetString("LastNeutral") + "\n<size=90%><line-height=1.8pic>" + GetString("LastNeutralInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("LastNeutralInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + ln.ToString() + "\n");
            if (player.Is(CustomRoles.Workhorse)) s.Append(k + "<color=#00ffff>" + GetString("Workhorse") + "\n<size=90%><line-height=1.8pic>" + GetString("WorkhorseInfo") + "\n</b></color><size=60%>From:<color=#00bfff>Town Of Host</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("WorkhorseInfoLong") + "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + wh.ToString() + "\n");
            CheckPageChange(player.PlayerId, s, title: AddRoleInfoTitle);

            if (s.ToString() != "") SendMessage(s.ToString(), player.PlayerId, AddRoleInfoTitle, removeTags: false);
        }
        public static string GetAddonsHelp(CustomRoles role)
        {
            var s = "";
            var k = "<line-height=2.0pic><size=150%>";
            //バフ
            if (role == CustomRoles.Guesser) s += k + "<color=#999900>" + GetString("Guesser") + " ∮\n<size=90%><line-height=1.8pic>" + GetString("GuesserInfo") + "\n</color><size=60%>From:<color=#ff0000>The Other Roles</color></size>\n<size=60%></color><line-height=1.3pic><line-height=1.3pic>" + GetString("GuesserInfoLong");
            if (role == CustomRoles.Serial) s += k + "<color=#ff1919>" + GetString("Serial") + " ∂\n<size=90%><line-height=1.8pic>" + GetString("SerialInfo") + "\n<size=60%></color><line-height=1.3pic>" + GetString("SerialInfoLong");
            if (role == CustomRoles.Connecting) s += k + "<color=#96514d>" + GetString("Connecting") + " Ψ\n<size=90%><line-height=1.8pic>" + GetString("ConnectingInfo") + "\n<size=60%></color><line-height=1.3pic>" + GetString("ConnectingInfoLong");
            if (role == CustomRoles.Watcher) s += k + "<color=#800080>" + GetString("Watcher") + " ∑\n<size=90%><line-height=1.8pic>" + GetString("WatcherInfo") + "\n</color><size=60%>From:<color=#ff0000>TOR GM Edition</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("WatcherInfoLong");
            if (role == CustomRoles.AdditionalVoter) s += k + "<color=#93ca76>" + GetString("AdditionalVoter") + " Å\n<size=90%><line-height=1.8pic>" + GetString("AdditionalVoterInfo") + "\n<size=60%></color><line-height=1.3pic>" + GetString("AdditionalVoterInfoLong");
            if (role == CustomRoles.Nurse) s += k + "<color=#ffadd6>" + GetString("Nurse") + " Й\n<size=90%><line-height=1.8pic>" + GetString("NurseInfo") + "\n<size=60%></color><line-height=1.3pic>" + GetString("NurseInfoLong");
            if (role == CustomRoles.Bakeneko) s += k + "<color=#ffcc99>" + GetString("Bakeneko") + " =^^=\n<size=90%><line-height=1.8pic>" + GetString("BakenekoInfo") + "\n<size=60%></color><line-height=1.3pic>" + GetString("BakenekoInfoLong");
            if (role == CustomRoles.Speeding) s += k + "<color=#33ccff>" + GetString("Speeding") + " ∈\n<size=90%><line-height=1.8pic>" + GetString("SpeedingInfo") + "\n<size=60%></color><line-height=1.3pic>" + GetString("SpeedingInfoLong");
            if (role == CustomRoles.Director) s += k + "<color=#cee4ae>" + GetString("Director") + " θ\n<size=90%><line-height=1.8pic>" + GetString("DirectorInfo") + "\n<size=60%></color><line-height=1.3pic>" + GetString("DirectorInfoLong");
            if (role == CustomRoles.Opener) s += k + "<color=#007bbb>" + GetString("Opener") + " п\n<size=90%><line-height=1.8pic>" + GetString("OpenerInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("OpenerInfoLong");
            if (role == CustomRoles.Psychic) s += k + "<color=#9933ff>" + GetString("Psychic") + " ☯\n<size=90%><line-height=1.8pic>" + GetString("PsychicInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("PsychicInfoLong");
            if (role == CustomRoles.Sun) s += k + "<color=#ec6800>" + GetString("Sun") + " ＊\n<size=90%><line-height=1.8pic>" + GetString("SunInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("SunInfoLong");
            if (role == CustomRoles.Moon) s += k + "<color=#ffff33>" + GetString("Moon") + " э\n<size=90%><line-height=1.8pic>" + GetString("MoonInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("MoonInfoLong");

            //デバフ
            if (role == CustomRoles.Notvoter) s += k + "<color=#6c848d>" + GetString("Notvoter") + " Ｖ\n<size=90%><line-height=1.8pic>" + GetString("NotvoterInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("NotvoterInfoLong");
            if (role == CustomRoles.Elector) s += k + "<color=#544a47>" + GetString("Elector") + " Ｅ\n<size=90%><line-height=1.8pic>" + GetString("ElectorInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("ElectorInfoLong");
            if (role == CustomRoles.NotConvener) s += k + "<color=#006666>" + GetString("NotConvener") + " Ｃ\n<size=90%><line-height=1.8pic>" + GetString("NotConvenerInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("NotConvenerInfoLong");
            if (role == CustomRoles.Transparent) s += k + "<color=#7b7c7d>" + GetString("Transparent") + " Ｔ\n<size=90%><line-height=1.8pic>" + GetString("TransparentInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("TransparentInfoLong");
            if (role == CustomRoles.Water) s += k + "<color=#17184b>" + GetString("Water") + " Ｗ\n<size=90%><line-height=1.8pic>" + GetString("WaterInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("WaterInfoLong");
            if (role == CustomRoles.LowBattery) s += k + "<color=#250d00>" + GetString("LowBattery") + " Ｌ\n<size=90%><line-height=1.8pic>" + GetString("LowBatteryInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("LowBatteryInfoLong");
            if (role == CustomRoles.Slacker) s += k + "<color=#460e44>" + GetString("Slacker") + " ＳＬ\n<size=90%><line-height=1.8pic>" + GetString("SlackerInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("SlackerInfoLong");

            //第三
            if (role == CustomRoles.ALovers) s += k + "<color=#ff6be4>" + GetString("ALovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("ALoversInfo") + "\n</b></color><size=60%>From:<color=#ff6be4>Love Couple Mod</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LoversInfoLong");
            if (role == CustomRoles.BLovers) s += k + "<color=#d70035>" + GetString("BLovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("BLoversInfo") + "\n</b></color><size=60%>From:<color=#ff6be4>Love Couple Mod</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LoversInfoLong");
            if (role == CustomRoles.CLovers) s += k + "<color=#fac559>" + GetString("CLovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("CLoversInfo") + "\n</b></color><size=60%>From:<color=#ff6be4>Love Couple Mod</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LoversInfoLong");
            if (role == CustomRoles.DLovers) s += k + "<color=#6c9bd2>" + GetString("DLovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("DLoversInfo") + "\n</b></color><size=60%>From:<color=#ff6be4>Love Couple Mod</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LoversInfoLong");
            if (role == CustomRoles.ELovers) s += k + "<color=#00885a>" + GetString("ELovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("ELoversInfo") + "\n</b></color><size=60%>From:<color=#ff6be4>Love Couple Mod</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LoversInfoLong");
            if (role == CustomRoles.FLovers) s += k + "<color=#fdede4>" + GetString("FLovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("FLoversInfo") + "\n</b></color><size=60%>From:<color=#ff6be4>Love Couple Mod</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LoversInfoLong");
            if (role == CustomRoles.GLovers) s += k + "<color=#af0082>" + GetString("GLovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("GLoversInfo") + "\n</b></color><size=60%>From:<color=#ff6be4>Love Couple Mod</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LoversInfoLong");
            if (role == CustomRoles.MaLovers) s += k + "<color=#f09199>" + GetString("MaLovers") + "　♥\n<size=90%><line-height=1.8pic>" + GetString("MaLoversInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("MaLoversInfoLong");

            //ラスト
            if (role == CustomRoles.LastImpostor) s += k + "<color=#ff1919>" + GetString("LastImpostor") + "\n<size=90%><line-height=1.8pic>" + GetString("LastImpostorInfo") + "\n</b></color><size=60%>From:<color=#00bfff>Town Of Host</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("LastImpostorInfoLong");
            if (role == CustomRoles.LastNeutral) s += k + "<color=#cccccc>" + GetString("LastNeutral") + "\n<size=90%><line-height=1.8pic>" + GetString("LastNeutralInfo") + "\n</b><size=60%></color><line-height=1.3pic>" + GetString("LastNeutralInfoLong");
            if (role == CustomRoles.Workhorse) s += k + "<color=#00ffff>" + GetString("Workhorse") + "\n<size=90%><line-height=1.8pic>" + GetString("WorkhorseInfo") + "\n</b></color><size=60%>From:<color=#00bfff>Town Of Host</color></size>\n<size=60%></color><line-height=1.3pic>" + GetString("WorkhorseInfoLong");
            var sb = new StringBuilder();
            var a = "";
            ShowChildrenSettings(Options.CustomRoleSpawnChances[role], ref sb, Askesu: true);
            if (sb.ToString() != "") a = "\n\n<size=90%>" + GetString("Settings") + "\n<size=60%>" + sb.ToString();
            return s + a;
        }
        public static void ShowHelp()
        {
            SendMessage(
                GetString("CommandList")
                + $"\n/winner - {GetString("Command.winner")}"
                + $"\n/lastresult - {GetString("Command.lastresult")}"
                + $"\n/rename - {GetString("Command.rename")}"
                + $"\n/now - {GetString("Command.now")}"
                + $"\n/h now - {GetString("Command.h_now")}"
                + $"\n/h roles {GetString("Command.h_roles")}"
                + $"\n/h addons {GetString("Command.h_addons")}"
                + $"\n/h modes {GetString("Command.h_modes")}"
                + $"\n/dump - {GetString("Command.dump")}"
                );
        }
        public static void ShowHelpK()
        {
            SendMessage(
                GetString("CommandList")
                + $"\n/w - {GetString("Command.winnerteam")}"
                + $"\n/forceend - {GetString("Command.forceend")}"
                + $"\n/allplayertp - {GetString("Command.apt")}"
                + $"\n/timer - {GetString("Command.timer")}"
                + $"\n/voice - {GetString("Command.voice")}"
                );
        }
        public static bool Kiroku = true;
        public static void SendMessage(string text, byte sendTo = byte.MaxValue, string title = "", bool removeTags = true)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (GameStates.Meeting) Kiroku = false;
            if (title == "") title = $"<color={Main.ModColor}>" + GetString($"DefaultSystemMessageTitle") + "</color>";
            var Text = new StringBuilder();
            if (removeTags)//ﾖｸﾜｶﾗﾝ!
            {
                text.RemoveHtmlTags();
            }
            Text.Append($"{text}");
            var Send = Text.ToString();
            Main.MessagesToSend.Add((Send, sendTo, title));
            if (GameStates.Meeting) _ = new LateTask(() => Kiroku = true, 1.50f);
        }
        public static void ApplySuffix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            string name = DataManager.player.Customization.Name;
            if (Main.nickName != "") name = Main.nickName;
            if (AmongUsClient.Instance.IsGameStarted)
            {
                if (Options.ColorNameMode.GetBool() && Main.nickName == "") name = Palette.GetColorName(Camouflage.PlayerSkins[PlayerControl.LocalPlayer.PlayerId].ColorId);
            }
            else
            {
                if (AmongUsClient.Instance.IsGamePublic)
                    name = $"<color={Main.ModColor}>TownOfHost-K v{Main.PluginVersion}</color>\r\n" + name;
                switch (Options.GetSuffixMode())
                {
                    case SuffixModes.None:
                        break;
                    case SuffixModes.TOH:
                        name += $"\r\n<color={Main.ModColor}>TOH-K v{Main.PluginVersion}</color>";
                        break;
                    case SuffixModes.Streaming:
                        name += $"\r\n<color={Main.ModColor}>{GetString("SuffixMode.Streaming")}</color>";
                        break;
                    case SuffixModes.Recording:
                        name += $"\r\n<color={Main.ModColor}>{GetString("SuffixMode.Recording")}</color>";
                        break;
                    case SuffixModes.RoomHost:
                        name += $"\r\n<color={Main.ModColor}>{GetString("SuffixMode.RoomHost")}</color>";
                        break;
                    case SuffixModes.OriginalName:
                        name += $"\r\n<color={Main.ModColor}>{DataManager.player.Customization.Name}</color>";
                        break;
                    case SuffixModes.Timer:
                        float timerValue = GameStartManagerPatch.GetTimer();
                        if (timerValue < GameStartManagerPatch.Timer2 - 2 || GameStartManagerPatch.Timer2 < 25)
                            GameStartManagerPatch.Timer2 = timerValue;
                        timerValue = GameStartManagerPatch.Timer2;
                        int minutes = (int)timerValue / 60;
                        int seconds = (int)timerValue % 60;
                        name += $"\r\n<color=red>{minutes:00}:{seconds:00}</color>";
                        break;
                }
            }
            if (name != PlayerControl.LocalPlayer.name && PlayerControl.LocalPlayer.CurrentOutfitType == PlayerOutfitType.Default) PlayerControl.LocalPlayer.RpcSetName(name);
        }
        private static Dictionary<byte, PlayerControl> cachedPlayers = new(15);
        public static PlayerControl GetPlayerById(int playerId) => GetPlayerById((byte)playerId);
        public static PlayerControl GetPlayerById(byte playerId)
        {
            if (cachedPlayers.TryGetValue(playerId, out var cachedPlayer) && cachedPlayer != null)
            {
                return cachedPlayer;
            }
            var player = Main.AllPlayerControls.Where(pc => pc.PlayerId == playerId).FirstOrDefault();
            cachedPlayers[playerId] = player;
            return player;
        }
        public static GameData.PlayerInfo GetPlayerInfoById(int PlayerId) =>
            GameData.Instance.AllPlayers.ToArray().Where(info => info.PlayerId == PlayerId).FirstOrDefault();
        private static StringBuilder SelfMark = new(20);
        private static StringBuilder SelfSuffix = new(20);
        private static StringBuilder TargetMark = new(20);
        private static StringBuilder TargetSuffix = new(20);
        public static void NotifyRoles(bool isForMeeting = false, PlayerControl SpecifySeer = null, bool NoCache = false, bool ForceLoop = false)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (Main.AllPlayerControls == null) return;

            //ミーティング中の呼び出しは不正
            if (GameStates.IsMeeting) return;

            var caller = new StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            var logger = Logger.Handler("NotifyRoles");
            logger.Info("NotifyRolesが" + callerClassName + "." + callerMethodName + "から呼び出されました");
            HudManagerPatch.NowCallNotifyRolesCount++;
            HudManagerPatch.LastSetNameDesyncCount = 0;

            var seerList = PlayerControl.AllPlayerControls;
            if (SpecifySeer != null)
            {
                seerList = new();
                seerList.Add(SpecifySeer);
            }
            var isMushroomMixupActive = IsActive(SystemTypes.MushroomMixupSabotage);
            //seer:ここで行われた変更を見ることができるプレイヤー
            //target:seerが見ることができる変更の対象となるプレイヤー
            foreach (var seer in seerList)
            {
                //seerが落ちているときに何もしない
                if (seer == null || seer.Data.Disconnected) continue;

                if (seer.IsModClient()) continue;
                string fontSize = isForMeeting ? "1.5" : Main.RoleTextSize.ToString();
                if (isForMeeting && (seer.GetClient().PlatformData.Platform is Platforms.Playstation or Platforms.Switch)) fontSize = "70%";
                logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":START");

                var seerRole = seer.GetRoleClass();
                // 会議じゃなくて，キノコカオス中で，seerが生きていてdesyncインポスターの場合に自身の名前を消す
                if (!isForMeeting && isMushroomMixupActive && seer.IsAlive() && !seer.Is(CustomRoleTypes.Impostor) && seer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true)
                {
                    seer.RpcSetNamePrivate("<size=0>", true, force: NoCache);
                }
                else
                {
                    //名前の後ろに付けるマーカー
                    SelfMark.Clear();

                    //seer役職が対象のMark
                    SelfMark.Append(seerRole?.GetMark(seer, isForMeeting: isForMeeting));
                    //seerに関わらず発動するMark
                    SelfMark.Append(CustomRoleManager.GetMarkOthers(seer, isForMeeting: isForMeeting));

                    //ハートマークを付ける(自分に)
                    if (seer.Is(CustomRoles.ALovers)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.ALovers), "♥"));
                    if (seer.Is(CustomRoles.BLovers)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.BLovers), "♥"));
                    if (seer.Is(CustomRoles.CLovers)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.CLovers), "♥"));
                    if (seer.Is(CustomRoles.DLovers)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.DLovers), "♥"));
                    if (seer.Is(CustomRoles.ELovers)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.ELovers), "♥"));
                    if (seer.Is(CustomRoles.FLovers)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.FLovers), "♥"));
                    if (seer.Is(CustomRoles.GLovers)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.GLovers), "♥"));

                    if (seer.Is(CustomRoles.MaLovers)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.MaLovers), "♥"));
                    if (seer.Is(CustomRoles.Connecting)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.Connecting), "Ψ"));

                    if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                    {
                        if (Options.TaskBattletaska.GetBool())
                        {
                            var t1 = 0f;
                            var t2 = 0;
                            if (!Options.TaskBattleTeamMode.GetBool() && !Options.TaskBattleTeamWinType.GetBool())
                            {
                                foreach (var pc in Main.AllPlayerControls)
                                {
                                    t1 += pc.GetPlayerTaskState().AllTasksCount;
                                    t2 += pc.GetPlayerTaskState().CompletedTasksCount;
                                }
                            }
                            else
                            {
                                foreach (var t in Main.TaskBattleTeams)
                                {
                                    if (!t.Contains(seer.PlayerId)) continue;
                                    t1 = Options.TaskBattleTeamWinTaskc.GetFloat();
                                    foreach (var id in t.Where(id => Utils.GetPlayerById(id).IsAlive()))
                                        t2 += GetPlayerById(id).GetPlayerTaskState().CompletedTasksCount;
                                }
                            }
                            SelfMark.Append($"<color=yellow>({t2}/{t1})</color>");
                        }
                        if (Options.TaskBattletasko.GetBool())
                        {
                            var to = 0;
                            if (!Options.TaskBattleTeamMode.GetBool() && !Options.TaskBattleTeamWinType.GetBool())
                            {
                                foreach (var pc in Main.AllPlayerControls)
                                    if (pc.GetPlayerTaskState().CompletedTasksCount > to) to = pc.GetPlayerTaskState().CompletedTasksCount;
                            }
                            else
                                foreach (var t in Main.TaskBattleTeams)
                                {
                                    var to2 = 0;
                                    foreach (var id in t.Where(id => Utils.GetPlayerById(id).IsAlive()))
                                        to2 += GetPlayerById(id).GetPlayerTaskState().CompletedTasksCount;
                                    if (to2 > to) to = to2;
                                }
                            SelfMark.Append($"<color=#00f7ff>({to})</color>");
                        }
                    }
                    //Markとは違い、改行してから追記されます。
                    SelfSuffix.Clear();

                    //seer役職が対象のLowerText
                    SelfSuffix.Append(seerRole?.GetLowerText(seer, isForMeeting: isForMeeting));
                    //seerに関わらず発動するLowerText
                    SelfSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, isForMeeting: isForMeeting));
                    //追放者
                    if (Options.CanseeVoteresult.GetBool() && MeetingVoteManager.Voteresult != "" && !GameStates.Meeting)
                    {
                        SelfSuffix.Append("<color=#ffffff><size=75%>" + MeetingVoteManager.Voteresult + "</color></size>");
                    }
                    if ((seer.Is(CustomRoles.Guesser) ||
                    (LastNeutral.GiveGuesser.GetBool() && seer.Is(CustomRoles.LastNeutral)) ||
                    (LastImpostor.GiveGuesser.GetBool() && seer.Is(CustomRoles.LastImpostor))) && GameStates.Meeting
                    )
                    {
                        SelfSuffix.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Guesser)}><size=50%>∮ゲッサー能力発動:/bt <color=#ffff00>ID</color> 役職名</color>");
                    }
                    //seer役職が対象のSuffix
                    SelfSuffix.Append(seerRole?.GetSuffix(seer, isForMeeting: isForMeeting));
                    //seerに関わらず発動するSuffix
                    SelfSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, isForMeeting: isForMeeting));

                    //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                    string SeerRealName = (seer.GetRoleClass() is IUseTheShButton) ? Main.AllPlayerNames[seer.PlayerId] : seer.GetRealName(isForMeeting);

                    if (!isForMeeting && MeetingStates.FirstMeeting && Options.ChangeNameToRoleInfo.GetBool())
                        SeerRealName = seer.GetRoleInfo();

                    //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                    var (enabled, text) = GetRoleNameAndProgressTextData(seer);
                    string SelfRoleName = enabled ? $"<size={fontSize}>{text}</size>" : "";
                    string SelfDeathReason = seer.KnowDeathReason(seer) ? $"({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(seer.PlayerId))})" : "";
                    string SelfName = $"{ColorString(seer.GetRoleColor(), SeerRealName)}{SelfDeathReason}{SelfMark}";
                    SelfName = SelfRoleName + "\r\n" + SelfName;
                    SelfName += SelfSuffix.ToString() == "" ? "" : "\r\n " + SelfSuffix.ToString();
                    if (!isForMeeting) SelfName += "\r\n";

                    //適用
                    seer.RpcSetNamePrivate(SelfName, true, force: NoCache);
                }
                //seerが死んでいる場合など、必要なときのみ第二ループを実行する
                if (seer.Data.IsDead //seerが死んでいる
                    || seer.GetCustomRole().IsImpostor() //seerがインポスター
                    || PlayerState.GetByPlayerId(seer.PlayerId).TargetColorData.Count > 0 //seer視点用の名前色データが一つ以上ある
                    || seer.Is(CustomRoles.Arsonist)
                    || seer.Is(CustomRoles.Chef)
                    || seer.Is(CustomRoles.ALovers)
                    || seer.Is(CustomRoles.BLovers)
                    || seer.Is(CustomRoles.CLovers)
                    || seer.Is(CustomRoles.DLovers)
                    || seer.Is(CustomRoles.ELovers)
                    || seer.Is(CustomRoles.FLovers)
                    || seer.Is(CustomRoles.GLovers)
                    || seer.Is(CustomRoles.MaLovers)
                    || seer.Is(CustomRoles.Connecting)
                    || seer.Is(CustomRoles.God)
                    || Witch.IsSpelled()
                    || seer.Is(CustomRoles.Executioner)
                    || seer.Is(CustomRoles.Doctor) //seerがドクター
                    || seer.Is(CustomRoles.Puppeteer)
                    || seer.Is(CustomRoles.ProgressKiller)
                    || CustomRoles.TaskStar.IsEnable()
                    || seer.IsNeutralKiller() //seerがキル出来るニュートラル
                    || seer.Is(CustomRoles.Director)
                    || IsActive(SystemTypes.Electrical)
                    || IsActive(SystemTypes.Comms)
                    || isMushroomMixupActive
                    || Options.CurrentGameMode == CustomGameMode.TaskBattle
                    || (seer.GetRoleClass() is IUseTheShButton) //ﾜﾝｸﾘｯｸシェイプボタン持ち
                    || NoCache
                    || ForceLoop
                )
                {
                    foreach (var target in Main.AllPlayerControls)
                    {
                        //targetがseer自身の場合は何もしない
                        if (target == seer) continue;
                        logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":START");

                        // 会議じゃなくて，キノコカオス中で，targetが生きていてseerがdesyncインポスターの場合にtargetの名前を消す
                        if (!isForMeeting && isMushroomMixupActive && target.IsAlive() && !seer.Is(CustomRoleTypes.Impostor) && seer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true)
                        {
                            target.RpcSetNamePrivate("<size=0>", true, seer, force: NoCache);
                        }
                        else
                        {
                            //名前の後ろに付けるマーカー
                            TargetMark.Clear();

                            //seer役職が対象のMark
                            TargetMark.Append(seerRole?.GetMark(seer, target, isForMeeting));
                            //seerに関わらず発動するMark
                            TargetMark.Append(CustomRoleManager.GetMarkOthers(seer, target, isForMeeting));

                            //ハートマークを付ける(相手に)
                            if (seer.Is(CustomRoles.ALovers) && target.Is(CustomRoles.ALovers)) TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.ALovers)}>♥</color>");
                            //霊界からラバーズ視認
                            else if (seer.Data.IsDead && !seer.Is(CustomRoles.ALovers) && target.Is(CustomRoles.ALovers)) TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.ALovers)}>♥</color>");

                            if (seer.Is(CustomRoles.BLovers) && target.Is(CustomRoles.BLovers)) TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.BLovers)}>♥</color>");
                            else if (seer.Data.IsDead && !seer.Is(CustomRoles.BLovers) && target.Is(CustomRoles.BLovers)) TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.BLovers)}>♥</color>");
                            if (seer.Is(CustomRoles.CLovers) && target.Is(CustomRoles.CLovers)) TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.CLovers)}>♥</color>");
                            else if (seer.Data.IsDead && !seer.Is(CustomRoles.CLovers) && target.Is(CustomRoles.CLovers)) TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.CLovers)}>♥</color>");
                            if (seer.Is(CustomRoles.DLovers) && target.Is(CustomRoles.DLovers)) TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.DLovers)}>♥</color>");
                            else if (seer.Data.IsDead && !seer.Is(CustomRoles.DLovers) && target.Is(CustomRoles.DLovers)) TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.DLovers)}>♥</color>");
                            if (seer.Is(CustomRoles.ELovers) && target.Is(CustomRoles.ELovers)) TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.ELovers)}>♥</color>");
                            else if (seer.Data.IsDead && !seer.Is(CustomRoles.ELovers) && target.Is(CustomRoles.ELovers)) TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.ELovers)}>♥</color>");
                            if (seer.Is(CustomRoles.FLovers) && target.Is(CustomRoles.FLovers)) TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.FLovers)}>♥</color>");
                            else if (seer.Data.IsDead && !seer.Is(CustomRoles.FLovers) && target.Is(CustomRoles.FLovers)) TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.FLovers)}>♥</color>");
                            if (seer.Is(CustomRoles.GLovers) && target.Is(CustomRoles.GLovers)) TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.GLovers)}>♥</color>");
                            else if (seer.Data.IsDead && !seer.Is(CustomRoles.GLovers) && target.Is(CustomRoles.GLovers)) TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.GLovers)}>♥</color>");

                            if (seer.Is(CustomRoles.MaLovers) && target.Is(CustomRoles.MaLovers))
                            {
                                TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.MaLovers)}>♥</color>");
                            }
                            else if (seer.Data.IsDead && !seer.Is(CustomRoles.MaLovers) && target.Is(CustomRoles.MaLovers))
                            {
                                TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.MaLovers)}>♥</color>");
                            }
                            if (seer.Is(CustomRoles.Connecting) && target.Is(CustomRoles.Connecting))
                            {
                                TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.Connecting)}>Ψ</color>");
                            }
                            else if (seer.Data.IsDead && !seer.Is(CustomRoles.Connecting) && target.Is(CustomRoles.Connecting))
                            {
                                TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.Connecting)}>Ψ</color>");
                            }
                            //プログレスキラー
                            if (seer.Is(CustomRoles.ProgressKiller) && target.Is(CustomRoles.Workhorse) && ProgressKiller.Workhorseseer)
                            {
                                TargetMark.Append($"<color=blue>♦</color>");
                            }
                            //エーリアン
                            if (seer.Is(CustomRoles.Alien) && target.Is(CustomRoles.Workhorse) && Alien.modePK && Alien.Workhorseseer)
                            {
                                TargetMark.Append($"<color=blue>♦</color>");
                            }

                            if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                                if (Options.TaskBattletaskc.GetBool())
                                    TargetMark.Append($"<color=yellow>({target.GetPlayerTaskState().CompletedTasksCount}/{target.GetPlayerTaskState().AllTasksCount})</color>");

                            //インサイダーモードタスク表示
                            if (Options.Taskcheck.GetBool())
                            {
                                if (target.GetPlayerTaskState() != null && target.GetPlayerTaskState().AllTasksCount > 0)
                                {
                                    if (seer.Is(CustomRoleTypes.Impostor))
                                    {
                                        TargetMark.Append($"<color=yellow>({target.GetPlayerTaskState().CompletedTasksCount}/{target.GetPlayerTaskState().AllTasksCount})</color>");
                                    }
                                }
                            }

                            //他人の役職とタスクは幽霊が他人の役職を見れるようになっていてかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                            var targetRoleData = GetRoleNameAndProgressTextData(seer, target);
                            var TargetRoleText = targetRoleData.enabled ? $"<size={fontSize}>{targetRoleData.text}</size>\r\n" : "";

                            TargetSuffix.Clear();
                            //seerに関わらず発動するLowerText
                            TargetSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target, isForMeeting: isForMeeting));

                            //seer役職が対象のSuffix
                            TargetSuffix.Append(seerRole?.GetSuffix(seer, target, isForMeeting: isForMeeting));
                            //seerに関わらず発動するSuffix
                            TargetSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, target, isForMeeting: isForMeeting));
                            // 空でなければ先頭に改行を挿入
                            if (TargetSuffix.Length > 0)
                            {
                                TargetSuffix.Insert(0, "\r\n");
                            }

                            //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                            string TargetPlayerName = (seer.GetRoleClass() is IUseTheShButton) ? Main.AllPlayerNames[target.PlayerId] : target.GetRealName(isForMeeting);

                            //ターゲットのプレイヤー名の色を書き換えます。
                            TargetPlayerName = TargetPlayerName.ApplyNameColorData(seer, target, isForMeeting);

                            if (seer.Is(CustomRoles.Guesser))
                            {
                                if (seer.IsAlive() && target.IsAlive() && isForMeeting)
                                {
                                    TargetPlayerName = ColorString(Color.yellow, target.PlayerId.ToString()) + " " + TargetPlayerName;
                                }
                            }
                            else
                            if (seer.Is(CustomRoles.LastImpostor) && LastImpostor.GiveGuesser.GetBool())
                            {
                                if (seer.IsAlive() && target.IsAlive() && isForMeeting)
                                {
                                    TargetPlayerName = ColorString(Color.yellow, target.PlayerId.ToString()) + " " + TargetPlayerName;
                                }
                            }
                            else
                            if (seer.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool())
                            {
                                if (seer.IsAlive() && target.IsAlive() && isForMeeting)
                                {
                                    TargetPlayerName = ColorString(Color.yellow, target.PlayerId.ToString()) + " " + TargetPlayerName;
                                }
                            }
                            string TargetDeathReason = "";
                            if (seer.KnowDeathReason(target))
                                TargetDeathReason = $"({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(target.PlayerId))})";

                            if (IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool() && !isForMeeting)
                                TargetPlayerName = $"<size=0%>{TargetPlayerName}</size>";

                            //全てのテキストを合成します。
                            string TargetName = $"{TargetRoleText}{TargetPlayerName}{TargetDeathReason}{TargetMark}{TargetSuffix}";

                            //適用
                            target.RpcSetNamePrivate(TargetName, true, seer, force: NoCache);
                        }
                        logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":END");
                    }
                }
                logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":END");
            }
        }
        public static void MarkEveryoneDirtySettings()
        {
            PlayerGameOptionsSender.SetDirtyToAll();
        }
        public static void SyncAllSettings()
        {
            PlayerGameOptionsSender.SetDirtyToAll();
            GameOptionsSender.SendAllGameOptions();
        }
        public static bool CanVent;
        public static void AfterMeetingTasks()
        {
            GameStates.Meeting = false;
            //天秤会議だと送らない
            if (Balancer.Id == 255 && Balancer.target1 != 255 && Balancer.target2 != 255)
            {
                foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
                    roleClass.BalancerAfterMeetingTasks();
            }
            else
            {
                Main.day++;
                Main.gamelog += "\n<size=80%>" + string.Format(GetString("Message.Day"), Main.day).Color(Palette.Orange) + "</size><size=60%>";
                foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
                    roleClass.AfterMeetingTasks();
            }

            if (Options.AirShipVariableElectrical.GetBool())
                AirShipElectricalDoors.Initialize();
            DoorsReset.ResetDoors();
            // 空デデンバグ対応 会議後にベントを空にする
            var ventilationSystem = ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var systemType) ? systemType.TryCast<VentilationSystem>() : null;
            if (ventilationSystem != null)
            {
                ventilationSystem.PlayersInsideVents.Clear();
                ventilationSystem.IsDirty = true;
            }
            GuessManager.Reset();//会議後にリセット入れる
            //いつかエイプリルフールネタで会議後全員の役職change入れる。予定()
        }

        public static void ChangeInt(ref int ChangeTo, int input, int max)
        {
            var tmp = ChangeTo * 10;
            tmp += input;
            ChangeTo = Math.Clamp(tmp, 0, max);
        }
        public static void CountAlivePlayers(bool sendLog = false)
        {
            if (Options.CuseVent.GetBool() && (Options.CuseVentCount.GetFloat() >= Main.AllAlivePlayerControls.Count())) Utils.CanVent = true;
            else CanVent = false;
            HudManagerPatch.BottonHud();
            int AliveImpostorCount = Main.AllAlivePlayerControls.Count(pc => pc.Is(CustomRoleTypes.Impostor));
            int AliveNeutalCount = Main.AllAlivePlayerControls.Count(pc => pc.Is(CustomRoleTypes.Neutral));
            if (Main.AliveImpostorCount != AliveImpostorCount)
            {
                Logger.Info("生存しているインポスター:" + AliveImpostorCount + "人", "CountAliveImpostors");
                Main.AliveImpostorCount = AliveImpostorCount;
                LastImpostor.SetSubRole();
            }
            if (Main.AliveNeutalCount != AliveNeutalCount)
            {
                Logger.Info("生存しているニュートラル:" + AliveNeutalCount + "人", "CountAliveNeutral");
                Main.AliveNeutalCount = AliveNeutalCount;
                LastNeutral.SetSubRole();
            }

            if (sendLog)
            {
                var sb = new StringBuilder(100);
                foreach (var countTypes in EnumHelper.GetAllValues<CountTypes>())
                {
                    var playersCount = PlayersCount(countTypes);
                    if (playersCount == 0) continue;
                    sb.Append($"{countTypes}:{AlivePlayersCount(countTypes)}/{playersCount}, ");
                }
                sb.Append($"All:{AllAlivePlayersCount}/{AllPlayersCount}");
                Logger.Info(sb.ToString(), "CountAlivePlayers");
            }
        }
        public static string PadRightV2(this object text, int num)
        {
            int bc = 0;
            var t = text.ToString();
            foreach (char c in t) bc += Encoding.GetEncoding("UTF-8").GetByteCount(c.ToString()) == 1 ? 1 : 2;
            return t?.PadRight(Mathf.Max(num - (bc - t.Length), 0));
        }
        public static void DumpLog()
        {
            string t = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            string fileName = $"{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}/TownOfHost-K-v{Main.PluginVersion}-{t}.log";
            FileInfo file = new(@$"{Environment.CurrentDirectory}/BepInEx/LogOutput.log");
            file.CopyTo(fileName);
            OpenDirectory(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            if (PlayerControl.LocalPlayer != null)
                HudManager.Instance?.Chat?.AddChat(PlayerControl.LocalPlayer, "デスクトップにログを保存しました。バグ報告チケットを作成してこのファイルを添付してください。");
        }
        public static void OpenDirectory(string path)
        {
            var startInfo = new ProcessStartInfo(path)
            {
                UseShellExecute = true,
            };
            Process.Start(startInfo);
        }
        public static string SummaryTexts(byte id, bool isForChat)
        {
            var builder = new StringBuilder();
            // チャットならposタグを使わない(文字数削減)
            if (isForChat)
            {
                builder.Append("<b>" + ColorString(Main.PlayerColors[id], Main.AllPlayerNames[id]) + "</b>");
                builder.Append(": ").Append(GetProgressText(id).RemoveColorTags());
                builder.Append(' ').Append(GetVitalText(id));
                builder.Append("<b> ").Append(GetTrueRoleName(id, false) + "</b>");
                if (GetPlayerById(id) != null) builder.Append(' ').Append(GetSubRolesText(id));
            }
            else
            {
                // 全プレイヤー中最長の名前の長さからプレイヤー名の後の水平位置を計算する
                // 1em ≒ 半角2文字
                // 空白は0.5emとする
                // SJISではアルファベットは1バイト，日本語は基本的に2バイト
                var longestNameByteCount = Main.AllPlayerNames.Values.Select(name => name.GetByteCount()).OrderByDescending(byteCount => byteCount).FirstOrDefault();
                //最大11.5emとする(★+日本語10文字分+半角空白)
                var pos = Math.Min(((float)longestNameByteCount / 2) + 1.5f /* ★+末尾の半角空白 */ , 11.5f);
                builder.Append(ColorString(Main.PlayerColors[id], Main.AllPlayerNames[id]));
                builder.AppendFormat("<pos={0}em>", pos).Append(GetProgressText(id)).Append("</pos>");
                // "(00/00) " = 4em
                pos += 4f;
                builder.AppendFormat("<pos={0}em>", pos).Append(GetVitalText(id)).Append("</pos>");
                // "Lover's Suicide " = 8em
                // "回線切断 " = 4.5em
                pos += DestroyableSingleton<TranslationController>.Instance.currentLanguage.languageID == SupportedLangs.English ? 8f : 4.5f;
                builder.AppendFormat("<pos={0}em>", pos);
                builder.Append(GetTrueRoleName(id, false));
                builder.Append(GetSubRolesText(id));
                builder.Append("</pos>");
            }
            return builder.ToString();
        }
        public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", "");
        public static string RemoveColorTags(this string str) => Regex.Replace(str, "</?color(=#[0-9a-fA-F]*)?>", "");
        public static void FlashColor(Color color, float duration = 1f)
        {
            var hud = DestroyableSingleton<HudManager>.Instance;
            if (hud.FullScreen == null) return;
            var obj = hud.transform.FindChild("FlashColor_FullScreen")?.gameObject;
            if (obj == null)
            {
                obj = GameObject.Instantiate(hud.FullScreen.gameObject, hud.transform);
                obj.name = "FlashColor_FullScreen";
            }
            hud.StartCoroutine(Effects.Lerp(duration, new Action<float>((t) =>
            {
                obj.SetActive(t != 1f);
                obj.GetComponent<SpriteRenderer>().color = new(color.r, color.g, color.b, Mathf.Clamp01((-2f * Mathf.Abs(t - 0.5f) + 1) * color.a)); //アルファ値を0→目標→0に変化させる
            })));
        }

        public static Sprite LoadSprite(string path, float pixelsPerUnit = 1f)
        {
            Sprite sprite = null;
            try
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
                var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                using MemoryStream ms = new();
                stream.CopyTo(ms);
                ImageConversion.LoadImage(texture, ms.ToArray());
                sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f), pixelsPerUnit);
            }
            catch
            {
                Logger.Error($"\"{path}\"の読み込みに失敗しました。", "LoadImage");
            }
            return sprite;
        }
        public static string ColorString(Color32 color, string str) => $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";
        /// <summary>
        /// Darkness:１の比率で黒色と元の色を混ぜる。マイナスだと白色と混ぜる。
        /// </summary>
        public static Color ShadeColor(this Color color, float Darkness = 0)
        {
            bool IsDarker = Darkness >= 0; //黒と混ぜる
            if (!IsDarker) Darkness = -Darkness;
            float Weight = IsDarker ? 0 : Darkness; //黒/白の比率
            float R = (color.r + Weight) / (Darkness + 1);
            float G = (color.g + Weight) / (Darkness + 1);
            float B = (color.b + Weight) / (Darkness + 1);
            return new Color(R, G, B, color.a);
        }

        /// <summary>
        /// 乱数の簡易的なヒストグラムを取得する関数
        /// <params name="nums">生成した乱数を格納したint配列</params>
        /// <params name="scale">ヒストグラムの倍率 大量の乱数を扱う場合、この値を下げることをお勧めします。</params>
        /// </summary>
        public static string WriteRandomHistgram(int[] nums, float scale = 1.0f)
        {
            int[] countData = new int[nums.Max() + 1];
            foreach (var num in nums)
            {
                if (0 <= num) countData[num]++;
            }
            StringBuilder sb = new();
            for (int i = 0; i < countData.Length; i++)
            {
                // 倍率適用
                countData[i] = (int)(countData[i] * scale);

                // 行タイトル
                sb.AppendFormat("{0:D2}", i).Append(" : ");

                // ヒストグラム部分
                for (int j = 0; j < countData[i]; j++)
                    sb.Append('|');

                // 改行
                sb.Append('\n');
            }

            // その他の情報
            sb.Append("最大数 - 最小数: ").Append(countData.Max() - countData.Min());

            return sb.ToString();
        }

        public static bool TryCast<T>(this Il2CppObjectBase obj, out T casted)
        where T : Il2CppObjectBase
        {
            casted = obj.TryCast<T>();
            return casted != null;
        }
        public static int AllPlayersCount => PlayerState.AllPlayerStates.Values.Count(state => state.CountType != CountTypes.OutOfGame);
        public static int AllAlivePlayersCount => Main.AllAlivePlayerControls.Count(pc => !pc.Is(CountTypes.OutOfGame));
        public static bool IsAllAlive => PlayerState.AllPlayerStates.Values.All(state => state.CountType == CountTypes.OutOfGame || !state.IsDead);
        public static int PlayersCount(CountTypes countTypes) => PlayerState.AllPlayerStates.Values.Count(state => state.CountType == countTypes);
        public static int AlivePlayersCount(CountTypes countTypes) => Main.AllAlivePlayerControls.Count(pc => pc.Is(countTypes));
        private const string ActiveSettingsSize = "70%";
        private const string ActiveSettingsLineHeight = "55%";
    }
}