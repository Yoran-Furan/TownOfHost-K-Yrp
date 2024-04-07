using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Crewmate;
using TownOfHost.Roles.AddOns.Neutral;
using static TownOfHost.Translator;
using TownOfHost.Roles.Neutral;
using TownOfHost.Modules.ChatManager;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    class ChangeRoleSettings
    {
        public static void Postfix(AmongUsClient __instance)
        {
            //注:この時点では役職は設定されていません。
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);

            PlayerState.Clear();

            Main.AllPlayerKillCooldown = new Dictionary<byte, float>();
            Main.AllPlayerSpeed = new Dictionary<byte, float>();

            Main.SKMadmateNowCount = 0;

            Main.AfterMeetingDeathPlayers = new();
            Main.clientIdList = new();

            Main.CheckShapeshift = new();
            Main.ShapeshiftTarget = new();

            ReportDeadBodyPatch.CanReport = new();
            ReportDeadBodyPatch.Musisuruoniku = new();

            Options.UsedButtonCount = 0;
            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            Main.introDestroyed = false;

            MeetingTimeManager.Init();
            Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
            Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

            Main.LastNotifyNames = new();

            Main.PlayerColors = new();
            //名前の記録
            Main.AllPlayerNames = new();

            Camouflage.Init();
            var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
            if (invalidColor.Any())
            {
                var msg = Translator.GetString("Error.InvalidColor");
                Logger.SendInGame(msg);
                msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}({p.Data.DefaultOutfit.ColorId})"));
                Utils.SendMessage(msg);
                Logger.Error(msg, "CoStartGame");
            }

            foreach (var target in Main.AllPlayerControls)
            {
                foreach (var seer in Main.AllPlayerControls)
                {
                    var pair = (target.PlayerId, seer.PlayerId);
                    Main.LastNotifyNames[pair] = target.name;
                }
            }
            foreach (var pc in Main.AllPlayerControls)
            {
                var colorId = pc.Data.DefaultOutfit.ColorId;
                if (AmongUsClient.Instance.AmHost && Options.ColorNameMode.GetBool()) pc.RpcSetName(Palette.GetColorName(colorId));
                PlayerState.Create(pc.PlayerId);
                Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;
                Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[colorId];
                Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod); //移動速度をデフォルトの移動速度に変更
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = new();
                ReportDeadBodyPatch.Musisuruoniku[pc.PlayerId] = true;
                pc.cosmetics.nameText.text = pc.name;

                var outfit = pc.Data.DefaultOutfit;
                Camouflage.PlayerSkins[pc.PlayerId] = new GameData.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
                Main.clientIdList.Add(pc.GetClientId());
            }
            Main.VisibleTasksCount = true;
            if (__instance.AmHost)
            {
                RPC.SyncCustomSettingsRPC();
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    if (!Main.HnSFlag)
                        Options.HideAndSeekKillDelayTimer = Options.KillDelay.GetFloat();
                }
                if (Options.IsStandardHAS)
                {
                    Options.HideAndSeekKillDelayTimer = Options.StandardHASWaitingTime.GetFloat();
                }
            }
            CustomRoleManager.Initialize();
            FallFromLadder.Reset();
            LastImpostor.Init();
            LastNeutral.Init();
            TargetArrow.Init();
            DoubleTrigger.Init();
            Watcher.Init();
            Serial.Init();
            Director.Init();
            Speeding.Init();
            Connecting.Init();
            Opener.Init();
            Moon.Init();
            Sun.Init();
            Psychic.Init();
            Bakeneko.Init();
            Guesser.Init();
            Nurse.Init();
            Workhorse.Init();
            PlayerSkinPatch.RemoveAll();
            NotConvener.Init();
            Notvoter.Init();
            AdditionalVoter.Init();
            Elector.Init();
            Water.Init();
            Slacker.Init();
            Transparent.Init();
            LowBattery.Init();
            CustomWinnerHolder.Reset();
            AntiBlackout.Reset();
            GuessManager.Guessreset();
            Madonna.Mareset();
            SelfVoteManager.Init();
            IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());
            ChatManager.ResetChat();
            Roles.Madmate.MadAvenger.Skill = false;
            MeetingStates.MeetingCalled = false;
            MeetingStates.FirstMeeting = true;
            GameStates.AlreadyDied = false;
            MeetingVoteManager.Voteresult = "";
            MeetingHudPatch.Oniku = "";
            MeetingHudPatch.Send = "";
            Main.day = 1;
            Main.gamelog = $"{GetString("GameLog")}\n<size=60%>{DateTime.Now.ToString("HH.mm.ss")} [Start]\n</size><size=80%>" + string.Format(GetString("Message.Day"), Main.day).Color(Palette.Orange) + "</size><size=60%>";
            if (Options.CuseVent.GetBool() && (Options.CuseVentCount.GetFloat() >= Main.AllAlivePlayerControls.Count())) Utils.CanVent = true;
            else Utils.CanVent = false;
        }
    }
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class SelectRolesPatch
    {
        public static void Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            //CustomRpcSenderとRpcSetRoleReplacerの初期化
            Dictionary<byte, CustomRpcSender> senders = new();
            foreach (var pc in Main.AllPlayerControls)
            {
                senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                        .StartMessage(pc.GetClientId());
            }
            RpcSetRoleReplacer.StartReplace(senders);

            RoleAssignManager.SelectAssignRoles();

            if (Options.CurrentGameMode != CustomGameMode.HideAndSeek)
            {
                if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                {
                    Main.TaskBattleTeams.Clear();
                    if (Options.TaskBattleTeamMode.GetBool())
                    {
                        var rand = new Random();
                        var AllPlayerCount = Options.EnableGM.GetBool() ? Main.AllPlayerControls.Count() - 1 : Main.AllPlayerControls.Count();
                        var teamc = Math.Min(Options.TaskBattleTeamC.GetFloat(), AllPlayerCount);
                        var c = AllPlayerCount / teamc;//1チームのプレイヤー数 ↑チーム数
                        List<PlayerControl> ap = new();
                        List<byte> playerlist = new();
                        foreach (var pc in Main.AllPlayerControls)
                            ap.Add(pc);
                        if (Options.EnableGM.GetBool())
                            ap.RemoveAll(x => x == PlayerControl.LocalPlayer);
                        Logger.Info($"{teamc},{c}", "TB");
                        for (var i = 0; teamc > i; i++)
                        {
                            Logger.Info($"team{i}", "TB");
                            playerlist.Clear();
                            for (var i2 = 0; c > i2; i2++)
                            {
                                if (ap.Count == 0) continue;
                                var player = ap[rand.Next(0, ap.Count)];
                                playerlist.Add(player.PlayerId);
                                Logger.Info($"{player.PlayerId}", "TB");
                                ap.Remove(player);
                            }
                            Main.TaskBattleTeams.Add(new List<byte>(playerlist));
                        }
                    }
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (Options.EnableGM.GetBool() && pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        {
                            PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                            PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                            PlayerControl.LocalPlayer.Data.IsDead = true;
                        }
                        else
                        {
                            pc.RpcSetCustomRole(CustomRoles.TaskPlayerB);
                            pc.RpcSetRole(Options.TaskBattleCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate);
                        }
                    }
                }
                else
                {
                    RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Shapeshifter };
                    foreach (var roleTypes in RoleTypesList)
                    {
                        var roleOpt = Main.NormalOptions.roleOptions;
                        int numRoleTypes = GetRoleTypesCount(roleTypes);
                        roleOpt.SetRoleRate(roleTypes, numRoleTypes, numRoleTypes > 0 ? 100 : 0);
                    }

                    List<PlayerControl> AllPlayers = new();
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        AllPlayers.Add(pc);
                    }

                    if (Options.EnableGM.GetBool())
                    {
                        AllPlayers.RemoveAll(x => x == PlayerControl.LocalPlayer);
                        PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                        PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                        PlayerControl.LocalPlayer.Data.IsDead = true;
                    }
                    Dictionary<(byte, byte), RoleTypes> rolesMap = new();
                    foreach (var (role, info) in CustomRoleManager.AllRolesInfo)
                    {
                        if (info.IsDesyncImpostor)
                        {
                            AssignDesyncRole(role, AllPlayers, senders, rolesMap, BaseRole: info.BaseRoleType.Invoke());
                        }
                    }
                    MakeDesyncSender(senders, rolesMap);
                }
            }
            //以下、バニラ側の役職割り当てが入る
        }
        public static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            RpcSetRoleReplacer.Release(); //保存していたSetRoleRpcを一気に書く
            RpcSetRoleReplacer.senders.Do(kvp => kvp.Value.SendMessage());

            // 不要なオブジェクトの削除
            RpcSetRoleReplacer.senders = null;
            RpcSetRoleReplacer.OverriddenSenderList = null;
            RpcSetRoleReplacer.StoragedData = null;

            //Utils.ApplySuffix();

            var rand = IRandom.Instance;

            List<PlayerControl> Crewmates = new();
            List<PlayerControl> Impostors = new();
            List<PlayerControl> Scientists = new();
            List<PlayerControl> Engineers = new();
            List<PlayerControl> GuardianAngels = new();
            List<PlayerControl> Shapeshifters = new();

            foreach (var pc in Main.AllPlayerControls)
            {
                pc.Data.IsDead = false; //プレイヤーの死を解除する
                var state = PlayerState.GetByPlayerId(pc.PlayerId);
                if (state.MainRole != CustomRoles.NotAssigned) continue; //既にカスタム役職が割り当てられていればスキップ
                var role = CustomRoles.NotAssigned;
                switch (pc.Data.Role.Role)
                {
                    case RoleTypes.Crewmate:
                        Crewmates.Add(pc);
                        role = CustomRoles.Crewmate;
                        break;
                    case RoleTypes.Impostor:
                        Impostors.Add(pc);
                        role = CustomRoles.Impostor;
                        break;
                    case RoleTypes.Scientist:
                        Scientists.Add(pc);
                        role = CustomRoles.Scientist;
                        break;
                    case RoleTypes.Engineer:
                        Engineers.Add(pc);
                        role = CustomRoles.Engineer;
                        break;
                    case RoleTypes.GuardianAngel:
                        GuardianAngels.Add(pc);
                        role = CustomRoles.GuardianAngel;
                        break;
                    case RoleTypes.Shapeshifter:
                        Shapeshifters.Add(pc);
                        role = CustomRoles.Shapeshifter;
                        break;
                    default:
                        Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                        break;
                }
                state.SetMainRole(role);
            }

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SetColorPatch.IsAntiGlitchDisabled = true;
                if (!Main.HnSFlag)
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoleTypes.Impostor))
                            pc.RpcSetColor(0);
                        else if (pc.Is(CustomRoleTypes.Crewmate))
                            pc.RpcSetColor(1);
                    }

                //役職設定処理
                AssignCustomRolesFromList(CustomRoles.HASFox, Crewmates);
                AssignCustomRolesFromList(CustomRoles.HASTroll, Crewmates);
                foreach (var pair in PlayerState.AllPlayerStates)
                {
                    //RPCによる同期
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                }
                //色設定処理
                SetColorPatch.IsAntiGlitchDisabled = true;
                GameEndChecker.SetPredicateToHideAndSeek();
            }
            else if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
            {
                AssignCustomRolesFromList(CustomRoles.TaskPlayerB, Crewmates);
                foreach (var pair in PlayerState.AllPlayerStates)
                {
                    //RPCによる同期
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                }

                if (Options.TaskBattleTeamMode.GetBool())
                {
                    foreach (var pc in Main.AllPlayerControls)
                        foreach (var t in Main.TaskBattleTeams)
                        {
                            if (!t.Contains(pc.PlayerId)) continue;
                            foreach (var id in t.Where(id => id != pc.PlayerId))
                                NameColorManager.Add(pc.PlayerId, id);
                        }
                }

                GameEndChecker.SetPredicateToTaskBattle();
            }
            else
            {
                foreach (var role in CustomRolesHelper.AllStandardRoles)
                {
                    if (role.IsVanilla()) continue;
                    if (CustomRoleManager.GetRoleInfo(role)?.IsDesyncImpostor == true) continue;
                    var baseRoleTypes = role.GetRoleTypes() switch
                    {
                        RoleTypes.Impostor => Impostors,
                        RoleTypes.Shapeshifter => Shapeshifters,
                        RoleTypes.Scientist => Scientists,
                        RoleTypes.Engineer => Engineers,
                        RoleTypes.GuardianAngel => GuardianAngels,
                        _ => Crewmates,
                    };
                    AssignCustomRolesFromList(role, baseRoleTypes);
                }
                AssignLoversRoles();
                AddOnsAssignDataOnlyKiller.AssignAddOnsFromList();
                AddOnsAssignDataNotImp.AssignAddOnsFromList();
                AddOnsAssignDataTeamImp.AssignAddOnsFromList();
                AddOnsAssignData.AssignAddOnsFromList();

                foreach (var pair in PlayerState.AllPlayerStates)
                {
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);

                    foreach (var subRole in pair.Value.SubRoles)
                        ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
                }

                CustomRoleManager.CreateInstance();
                foreach (var pc in Main.AllPlayerControls)
                {
                    HudManager.Instance.SetHudActive(true);
                    pc.ResetKillCooldown();

                    //通常モードでかくれんぼをする人用
                    if (Options.IsStandardHAS)
                    {
                        foreach (var seer in Main.AllPlayerControls)
                        {
                            if (seer == pc) continue;
                            if (pc.GetCustomRole().IsImpostor() || pc.IsNeutralKiller()) //変更対象がインポスター陣営orキル可能な第三陣営
                                NameColorManager.Add(seer.PlayerId, pc.PlayerId);
                        }
                    }
                }

                RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Shapeshifter };
                foreach (var roleTypes in RoleTypesList)
                {
                    var roleOpt = Main.NormalOptions.roleOptions;
                    roleOpt.SetRoleRate(roleTypes, 0, 0);
                }
                GameEndChecker.SetPredicateToNormal();
            }
            GameOptionsSender.AllSenders.Clear();
            foreach (var pc in Main.AllPlayerControls)
            {
                GameOptionsSender.AllSenders.Add(
                    new PlayerGameOptionsSender(pc)
                );
            }

            /*
            //インポスターのゴーストロールがクルーになるバグ対策
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Data.Role.IsImpostor || Main.ResetCamPlayerList.Contains(pc.PlayerId))
                {
                    pc.Data.Role.DefaultGhostRole = RoleTypes.ImpostorGhost;
                }
            }
            */
            Utils.CountAlivePlayers(true);
            Utils.SyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;
        }
        private static void AssignDesyncRole(CustomRoles role, List<PlayerControl> AllPlayers, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
        {
            if (!role.IsPresent()) return;

            var hostId = PlayerControl.LocalPlayer.PlayerId;
            var rand = IRandom.Instance;

            for (var i = 0; i < role.GetRealCount(); i++)
            {
                if (AllPlayers.Count <= 0) break;
                var player = AllPlayers[rand.Next(0, AllPlayers.Count)];
                AllPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);

                var selfRole = player.PlayerId == hostId ? hostBaseRole : BaseRole;
                var othersRole = player.PlayerId == hostId ? RoleTypes.Crewmate : RoleTypes.Scientist;

                //Desync役職視点
                foreach (var target in Main.AllPlayerControls)
                {
                    if (player.PlayerId != target.PlayerId)
                    {
                        rolesMap[(player.PlayerId, target.PlayerId)] = othersRole;
                    }
                    else
                    {
                        rolesMap[(player.PlayerId, target.PlayerId)] = selfRole;
                    }
                }

                //他者視点
                foreach (var seer in Main.AllPlayerControls)
                {
                    if (player.PlayerId != seer.PlayerId)
                    {
                        rolesMap[(seer.PlayerId, player.PlayerId)] = othersRole;
                    }
                }
                RpcSetRoleReplacer.OverriddenSenderList.Add(senders[player.PlayerId]);
                //ホスト視点はロール決定
                player.SetRole(othersRole);
                player.Data.IsDead = true;
            }
        }
        public static void MakeDesyncSender(Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap)
        {
            var hostId = PlayerControl.LocalPlayer.PlayerId;
            foreach (var seer in Main.AllPlayerControls)
            {
                var sender = senders[seer.PlayerId];
                foreach (var target in Main.AllPlayerControls)
                {
                    if (rolesMap.TryGetValue((seer.PlayerId, target.PlayerId), out var role))
                    {
                        sender.RpcSetRole(seer, role, target.GetClientId());
                    }
                }
            }
        }

        private static List<PlayerControl> AssignCustomRolesFromList(CustomRoles role, List<PlayerControl> players, int RawCount = -1)
        {
            if (players == null || players.Count <= 0) return null;
            var rand = IRandom.Instance;
            var count = Math.Clamp(RawCount, 0, players.Count);
            if (RawCount == -1) count = Math.Clamp(role.GetRealCount(), 0, players.Count);
            if (count <= 0) return null;
            List<PlayerControl> AssignedPlayers = new();
            SetColorPatch.IsAntiGlitchDisabled = true;
            for (var i = 0; i < count; i++)
            {
                var player = players[rand.Next(0, players.Count)];
                AssignedPlayers.Add(player);
                players.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");

                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    if (player.Is(CustomRoles.HASTroll))
                        player.RpcSetColor(2);
                    else if (player.Is(CustomRoles.HASFox))
                        player.RpcSetColor(3);
                }
            }
            SetColorPatch.IsAntiGlitchDisabled = false;
            return AssignedPlayers;
        }

        private static void AssignLoversRoles(int RawCount = -1)
        {
            if (CustomRoles.ALovers.IsPresent())
            {//Loversを初期化
                Main.ALoversPlayers.Clear();
                Main.isALoversDead = false;
                var allPlayers = new List<PlayerControl>();
                foreach (var player in Main.AllPlayerControls)
                {
                    if (player.Is(CustomRoles.GM)) continue;
                    if (player.Is(CustomRoles.Madonna)) continue;
                    if (player.Is(CustomRoles.Limiter)) continue;
                    allPlayers.Add(player);
                }
                var loversRole = CustomRoles.ALovers;
                var rand = IRandom.Instance;
                var count = Math.Clamp(RawCount, 0, allPlayers.Count);
                if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
                if (count <= 0) return;

                for (var i = 0; i < count; i++)
                {
                    var player = allPlayers[rand.Next(0, allPlayers.Count)];
                    Main.ALoversPlayers.Add(player);
                    allPlayers.Remove(player);
                    PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                    Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
                }
            }
            {
                if (CustomRoles.BLovers.IsPresent())
                {
                    Main.BLoversPlayers.Clear(); Main.isBLoversDead = false;
                    var allPlayers = new List<PlayerControl>();
                    foreach (var player in Main.AllPlayerControls)
                    {
                        if (player.Is(CustomRoles.GM)) continue; if (player.Is(CustomRoles.Madonna)) continue;
                        if (player.Is(CustomRoles.Limiter)) continue; if (player.Is(CustomRoles.ALovers)) continue;
                        allPlayers.Add(player);
                    }
                    var loversRole = CustomRoles.BLovers; var rand = IRandom.Instance; var count = Math.Clamp(RawCount, 0, allPlayers.Count);
                    if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
                    if (count <= 0) return;
                    for (var i = 0; i < count; i++)
                    {
                        var player = allPlayers[rand.Next(0, allPlayers.Count)];
                        Main.BLoversPlayers.Add(player);
                        allPlayers.Remove(player);
                        PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                        Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
                    }
                }
            }
            {
                if (CustomRoles.CLovers.IsPresent())
                {
                    Main.CLoversPlayers.Clear(); Main.isCLoversDead = false;
                    var allPlayers = new List<PlayerControl>();
                    foreach (var player in Main.AllPlayerControls)
                    {
                        if (player.Is(CustomRoles.GM)) continue; if (player.Is(CustomRoles.Madonna)) continue;
                        if (player.Is(CustomRoles.Limiter)) continue; if (player.Is(CustomRoles.ALovers)) continue;
                        if (player.Is(CustomRoles.BLovers)) continue;
                        allPlayers.Add(player);
                    }
                    var loversRole = CustomRoles.CLovers; var rand = IRandom.Instance; var count = Math.Clamp(RawCount, 0, allPlayers.Count);
                    if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
                    if (count <= 0) return;
                    for (var i = 0; i < count; i++)
                    {
                        var player = allPlayers[rand.Next(0, allPlayers.Count)];
                        Main.CLoversPlayers.Add(player);
                        allPlayers.Remove(player);
                        PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                        Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
                    }
                }
            }
            {
                if (CustomRoles.DLovers.IsPresent())
                {
                    Main.DLoversPlayers.Clear(); Main.isDLoversDead = false;
                    var allPlayers = new List<PlayerControl>();
                    foreach (var player in Main.AllPlayerControls)
                    {
                        if (player.Is(CustomRoles.GM)) continue; if (player.Is(CustomRoles.Madonna)) continue;
                        if (player.Is(CustomRoles.Limiter)) continue; if (player.Is(CustomRoles.ALovers)) continue;
                        if (player.Is(CustomRoles.BLovers)) continue; if (player.Is(CustomRoles.CLovers)) continue;
                        allPlayers.Add(player);
                    }
                    var loversRole = CustomRoles.DLovers; var rand = IRandom.Instance; var count = Math.Clamp(RawCount, 0, allPlayers.Count);
                    if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
                    if (count <= 0) return;
                    for (var i = 0; i < count; i++)
                    {
                        var player = allPlayers[rand.Next(0, allPlayers.Count)];
                        Main.DLoversPlayers.Add(player);
                        allPlayers.Remove(player);
                        PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                        Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
                    }
                }
            }
            {
                if (CustomRoles.ELovers.IsPresent())
                {
                    Main.ELoversPlayers.Clear(); Main.isELoversDead = false;
                    var allPlayers = new List<PlayerControl>();
                    foreach (var player in Main.AllPlayerControls)
                    {
                        if (player.Is(CustomRoles.GM)) continue; if (player.Is(CustomRoles.Madonna)) continue;
                        if (player.Is(CustomRoles.Limiter)) continue; if (player.Is(CustomRoles.ALovers)) continue;
                        if (player.Is(CustomRoles.BLovers)) continue; if (player.Is(CustomRoles.CLovers)) continue;
                        if (player.Is(CustomRoles.DLovers)) continue;
                        allPlayers.Add(player);
                    }
                    var loversRole = CustomRoles.ELovers; var rand = IRandom.Instance; var count = Math.Clamp(RawCount, 0, allPlayers.Count);
                    if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
                    if (count <= 0) return;
                    for (var i = 0; i < count; i++)
                    {
                        var player = allPlayers[rand.Next(0, allPlayers.Count)];
                        Main.ELoversPlayers.Add(player);
                        allPlayers.Remove(player);
                        PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                        Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
                    }
                }
            }
            {
                if (CustomRoles.FLovers.IsPresent())
                {
                    Main.FLoversPlayers.Clear(); Main.isFLoversDead = false;
                    var allPlayers = new List<PlayerControl>();
                    foreach (var player in Main.AllPlayerControls)
                    {
                        if (player.Is(CustomRoles.GM)) continue; if (player.Is(CustomRoles.Madonna)) continue;
                        if (player.Is(CustomRoles.Limiter)) continue; if (player.Is(CustomRoles.ALovers)) continue;
                        if (player.Is(CustomRoles.BLovers)) continue; if (player.Is(CustomRoles.CLovers)) continue;
                        if (player.Is(CustomRoles.DLovers)) continue; if (player.Is(CustomRoles.ELovers)) continue;
                        allPlayers.Add(player);
                    }
                    var loversRole = CustomRoles.FLovers; var rand = IRandom.Instance; var count = Math.Clamp(RawCount, 0, allPlayers.Count);
                    if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
                    if (count <= 0) return;
                    for (var i = 0; i < count; i++)
                    {
                        var player = allPlayers[rand.Next(0, allPlayers.Count)];
                        Main.FLoversPlayers.Add(player);
                        allPlayers.Remove(player);
                        PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                        Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
                    }
                }
            }
            {
                if (CustomRoles.GLovers.IsPresent())
                {
                    Main.GLoversPlayers.Clear(); Main.isGLoversDead = false;
                    var allPlayers = new List<PlayerControl>();
                    foreach (var player in Main.AllPlayerControls)
                    {
                        if (player.Is(CustomRoles.GM)) continue; if (player.Is(CustomRoles.Madonna)) continue;
                        if (player.Is(CustomRoles.Limiter)) continue; if (player.Is(CustomRoles.ALovers)) continue;
                        if (player.Is(CustomRoles.BLovers)) continue; if (player.Is(CustomRoles.CLovers)) continue;
                        if (player.Is(CustomRoles.DLovers)) continue; if (player.Is(CustomRoles.ELovers)) continue;
                        if (player.Is(CustomRoles.FLovers)) continue;
                        allPlayers.Add(player);
                    }
                    var loversRole = CustomRoles.GLovers; var rand = IRandom.Instance; var count = Math.Clamp(RawCount, 0, allPlayers.Count);
                    if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
                    if (count <= 0) return;
                    for (var i = 0; i < count; i++)
                    {
                        var player = allPlayers[rand.Next(0, allPlayers.Count)];
                        Main.GLoversPlayers.Add(player);
                        allPlayers.Remove(player);
                        PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                        Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
                    }
                }
            }
            RPC.SyncLoversPlayers();
        }
        public static int GetRoleTypesCount(RoleTypes roleTypes)
        {
            int count = 0;
            foreach (var role in CustomRolesHelper.AllRoles)
            {
                if (CustomRoleManager.GetRoleInfo(role)?.IsDesyncImpostor == true) continue;
                if (role == CustomRoles.Egoist && Main.NormalOptions.GetInt(Int32OptionNames.NumImpostors) <= 1) continue;
                if (role.GetRoleTypes() == roleTypes)
                    count += role.GetRealCount();
            }
            return count;
        }
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
        class RpcSetRoleReplacer
        {
            public static bool doReplace = false;
            public static Dictionary<byte, CustomRpcSender> senders;
            public static List<(PlayerControl, RoleTypes)> StoragedData = new();
            // 役職Desyncなど別の処理でSetRoleRpcを書き込み済みなため、追加の書き込みが不要なSenderのリスト
            public static List<CustomRpcSender> OverriddenSenderList;
            public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType)
            {
                if (doReplace && senders != null)
                {
                    StoragedData.Add((__instance, roleType));
                    return false;
                }
                else return true;
            }
            public static void Release()
            {
                foreach (var sender in senders)
                {
                    if (OverriddenSenderList.Contains(sender.Value)) continue;
                    if (sender.Value.CurrentState != CustomRpcSender.State.InRootMessage)
                        throw new InvalidOperationException("A CustomRpcSender had Invalid State.");

                    foreach (var pair in StoragedData)
                    {
                        pair.Item1.SetRole(pair.Item2);
                        sender.Value.AutoStartRpc(pair.Item1.NetId, (byte)RpcCalls.SetRole, Utils.GetPlayerById(sender.Key).GetClientId())
                            .Write((ushort)pair.Item2)
                            .EndRpc();
                    }
                    sender.Value.EndMessage();
                }
                doReplace = false;
            }
            public static void StartReplace(Dictionary<byte, CustomRpcSender> senders)
            {
                RpcSetRoleReplacer.senders = senders;
                StoragedData = new();
                OverriddenSenderList = new();
                doReplace = true;
            }
        }
    }
}