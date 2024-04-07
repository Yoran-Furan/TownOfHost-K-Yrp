using System.Collections.Generic;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Neutral;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    class OnGameJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            while (!Options.IsLoaded) System.Threading.Tasks.Task.Delay(1);
            Logger.Info($"{__instance.GameId}に参加", "OnGameJoined");
            Main.playerVersion = new Dictionary<byte, PlayerVersion>();
            RPC.RpcVersionCheck();
            SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);

            ChatUpdatePatch.DoBlockChat = false;
            GameStates.InGame = false;
            ErrorText.Instance.Clear();
            if (AmongUsClient.Instance.AmHost) //以下、ホストのみ実行
            {
                if (Main.NormalOptions.KillCooldown == 0f)
                    Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>());
                if (AURoleOptions.ShapeshifterCooldown == 0f)
                    AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;
            }
        }
    }
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
    class DisconnectInternalPatch
    {
        public static void Prefix(InnerNetClient __instance, DisconnectReasons reason, string stringReason)
        {
            Logger.Info($"切断(理由:{reason}:{stringReason}, ping:{__instance.Ping})", "Session");

            if (GameStates.IsFreePlay && Main.EditMode)
            {
                CustomSpawnSaveandLoadManager.Save();
                Main.EditMode = false;
            }

            if (AmongUsClient.Instance.AmHost && GameStates.InGame)
                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);

            CustomRoleManager.Dispose();
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    class OnPlayerJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            Logger.Info($"{client.PlayerName}(ClientID:{client.Id})(FriendCode:{client.FriendCode})(PuiD:{client.GetHashedPuid()})が参加", "Session");
            if (AmongUsClient.Instance.AmHost && client.FriendCode == "" && Options.KickPlayerFriendCodeNotExist.GetBool())
            {
                AmongUsClient.Instance.KickPlayer(client.Id, false);
                Logger.SendInGame(string.Format(GetString("Message.KickedByNoFriendCode"), client.PlayerName));
                Logger.Info($"フレンドコードがないプレイヤーを{client?.PlayerName}をキックしました。", "Kick");
            }
            if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
            {
                AmongUsClient.Instance.KickPlayer(client.Id, true);
                Logger.Info($"ブロック済みのプレイヤー{client?.PlayerName}({client.FriendCode})をBANしました。", "BAN");
            }
            BanManager.CheckBanPlayer(client);
            BanManager.CheckDenyNamePlayer(client);
            RPC.RpcVersionCheck();
            if (AmongUsClient.Instance.AmHost)
            {
                RPC.RpcSyncRoomTimer();
                RPC.SyncYomiage();
                //_ = new LateTask(() => client.Character.RpcSetNamePrivate($"<line-height=1000%><pos=30><size=2.8><color=green>host:<{Main.ModColor}>{Main.ModName} v{Main.PluginVersion}</color>\n</color><size=2.5>{client.PlayerName}\n</line-height>\n<line-height=30%>", true, force: true), 1.5f);
            }
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    class OnPlayerLeftPatch
    {
        static void Prefix([HarmonyArgument(0)] ClientData data)
        {
            if (CustomRoles.Executioner.IsPresent())
                Executioner.ChangeRoleByTarget(data.Character.PlayerId);
        }
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
        {
            //            Logger.info($"RealNames[{data.Character.PlayerId}]を削除");
            //            main.RealNames.Remove(data.Character.PlayerId);
            if (GameStates.IsInGame)
            {
                if (data.Character.Is(CustomRoles.ALovers) && !data.Character.Data.IsDead)
                    foreach (var lovers in Main.ALoversPlayers.ToArray())
                    {
                        Main.isALoversDead = true;
                        Main.ALoversPlayers.Remove(lovers);
                        PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.ALovers);
                    }
                if (data.Character.Is(CustomRoles.BLovers) && !data.Character.Data.IsDead)
                    foreach (var lovers in Main.BLoversPlayers.ToArray())
                    {
                        Main.isBLoversDead = true;
                        Main.BLoversPlayers.Remove(lovers);
                        PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.BLovers);
                    }
                if (data.Character.Is(CustomRoles.CLovers) && !data.Character.Data.IsDead)
                    foreach (var lovers in Main.CLoversPlayers.ToArray())
                    {
                        Main.isCLoversDead = true;
                        Main.CLoversPlayers.Remove(lovers);
                        PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.CLovers);
                    }
                if (data.Character.Is(CustomRoles.DLovers) && !data.Character.Data.IsDead)
                    foreach (var lovers in Main.DLoversPlayers.ToArray())
                    {
                        Main.isDLoversDead = true;
                        Main.DLoversPlayers.Remove(lovers);
                        PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.DLovers);
                    }
                if (data.Character.Is(CustomRoles.ELovers) && !data.Character.Data.IsDead)
                    foreach (var lovers in Main.ELoversPlayers.ToArray())
                    {
                        Main.isELoversDead = true;
                        Main.ELoversPlayers.Remove(lovers);
                        PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.ELovers);
                    }
                if (data.Character.Is(CustomRoles.FLovers) && !data.Character.Data.IsDead)
                    foreach (var lovers in Main.FLoversPlayers.ToArray())
                    {
                        Main.isFLoversDead = true;
                        Main.FLoversPlayers.Remove(lovers);
                        PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.FLovers);
                    }
                if (data.Character.Is(CustomRoles.GLovers) && !data.Character.Data.IsDead)
                    foreach (var lovers in Main.GLoversPlayers.ToArray())
                    {
                        Main.isGLoversDead = true;
                        Main.GLoversPlayers.Remove(lovers);
                        PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.GLovers);
                    }
                if (data.Character.Is(CustomRoles.MaLovers) && !data.Character.Data.IsDead)
                    foreach (var Mlovers in Main.MaMaLoversPlayers.ToArray())
                    {
                        Main.isMaLoversDead = true;
                        Main.MaMaLoversPlayers.Remove(Mlovers);
                        PlayerState.GetByPlayerId(Mlovers.PlayerId).RemoveSubRole(CustomRoles.MaLovers);
                    }
                var state = PlayerState.GetByPlayerId(data.Character.PlayerId);
                if (state.DeathReason == CustomDeathReason.etc) //死因が設定されていなかったら
                {
                    state.DeathReason = CustomDeathReason.Disconnected;
                    state.SetDead();
                }
                AntiBlackout.OnDisconnect(data.Character.Data);
                PlayerGameOptionsSender.RemoveSender(data.Character);
            }
            Main.playerVersion.Remove(data.Character.PlayerId);
            Logger.Info($"{data.PlayerName}(ClientID:{data.Id})が切断(理由:{reason}, ping:{AmongUsClient.Instance.Ping})", "Session");
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CreatePlayer))]
    class CreatePlayerPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                OptionItem.SyncAllOptions();
                _ = new LateTask(() =>
                {
                    if (client.Character == null) return;
                    if (AmongUsClient.Instance.IsGamePublic) Utils.SendMessage(string.Format(GetString("Message.AnnounceTOH-K"), Main.PluginVersion), client.Character.PlayerId);
                    TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true);
                }, 3f, "Welcome Message");
                if (Options.AutoDisplayLastResult.GetBool() && PlayerState.AllPlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                {
                    _ = new LateTask(() =>
                    {
                        if (!AmongUsClient.Instance.IsGameStarted && client.Character != null)
                        {
                            Main.isChatCommand = true;
                            Utils.ShowLastResult(client.Character.PlayerId);
                        }
                    }, 3f, "DisplayLastRoles");
                }
                if (Options.AutoDisplayKillLog.GetBool() && PlayerState.AllPlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                {
                    _ = new LateTask(() =>
                    {
                        if (!GameStates.IsInGame && client.Character != null)
                        {
                            Main.isChatCommand = true;
                            Utils.ShowKillLog(client.Character.PlayerId);
                        }
                    }, 3f, "DisplayKillLog");
                }
            }
        }
    }
}
