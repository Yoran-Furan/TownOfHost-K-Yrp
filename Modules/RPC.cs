using System;
using System.Linq;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public enum CustomRPC
    {
        VersionCheck = 80,
        RequestRetryVersionCheck = 81,
        SyncCustomSettings = 100,
        SetDeathReason,
        EndGame,
        PlaySound,
        SetCustomRole,
        SetNameColorData,
        SetRealKiller,
        SetLoversPlayers,
        SetMaLovers,
        SyncRoomTimer,
        SyncYomiage,
        Guess,
        GuessKill,
        CustomRoleSync
    }
    public enum Sounds
    {
        KillSound,
        TaskComplete
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    internal class RPCHandlerPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
        {
            RpcCalls rpcType = (RpcCalls)callId;
            Logger.Info($"{__instance?.Data?.PlayerId}({__instance?.Data?.PlayerName}):{callId}({RPC.GetRpcName(callId)})", "ReceiveRPC");
            MessageReader subReader = MessageReader.Get(reader);
            switch (rpcType)
            {
                case RpcCalls.SetName: //SetNameRPC
                    string name = subReader.ReadString();
                    if (subReader.BytesRemaining > 0 && subReader.ReadBoolean()) return false;
                    Logger.Info("名前変更:" + __instance.GetNameWithRole() + " => " + name, "SetName");
                    break;
                case RpcCalls.SetRole: //SetNameRPC
                    RoleTypes role = (RoleTypes)subReader.ReadUInt16();
                    Logger.Info("役職:" + __instance.GetRealName() + " => " + role, "SetRole");
                    break;
                case RpcCalls.SendChat:
                    var text = subReader.ReadString();
                    Logger.Info($"{__instance.GetNameWithRole()}:{text}", "ReceiveChat");
                    ChatCommands.OnReceiveChat(__instance, text, out var canceled);
                    if (canceled) return false;
                    break;
                case RpcCalls.StartMeeting:
                    PlayerControl p = Utils.GetPlayerById(subReader.ReadByte());
                    Logger.Info($"{__instance.GetNameWithRole()} => {p?.GetNameWithRole() ?? "null"}", "StartMeeting");
                    break;
            }
            if (__instance.PlayerId != 0
                && Enum.IsDefined(typeof(CustomRPC), (int)callId)
                && !(callId == (byte)CustomRPC.VersionCheck || callId == (byte)CustomRPC.RequestRetryVersionCheck)) //ホストではなく、CustomRPCで、VersionCheckではない
            {
                Logger.Warn($"{__instance?.Data?.PlayerName}:{callId}({RPC.GetRpcName(callId)}) ホスト以外から送信されたためキャンセルしました。", "CustomRPC");
                if (AmongUsClient.Instance.AmHost)
                {
                    AmongUsClient.Instance.KickPlayer(__instance.GetClientId(), false);
                    Logger.Warn($"不正なRPCを受信したため{__instance?.Data?.PlayerName}をキックしました。", "Kick");
                    Logger.SendInGame(string.Format(GetString("Warning.InvalidRpc"), __instance?.Data?.PlayerName));
                }
                return false;
            }
            return true;
        }
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
        {
            if (DebugModeManager.EnableTOHkDebugMode.GetBool()) Logger.Info(callId + $"{(callId < (byte)CustomRPC.VersionCheck ? (RpcCalls)callId : (CustomRPC)callId)}" + "RPCを受け取りました！", "RPC");
            //CustomRPC以外は処理しない
            if (callId < (byte)CustomRPC.VersionCheck) return;

            CustomRPC rpcType = (CustomRPC)callId;
            switch (rpcType)
            {
                case CustomRPC.VersionCheck:
                    try
                    {
                        Version version = Version.Parse(reader.ReadString());
                        string tag = reader.ReadString();
                        string forkId = 3 <= version.Major ? reader.ReadString() : Main.OriginalForkId;
                        Main.playerVersion[__instance.PlayerId] = new PlayerVersion(version, tag, forkId);
                    }
                    catch
                    {
                        Logger.Warn($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): バージョン情報が無効です", "RpcVersionCheck");
                        _ = new LateTask(() =>
                        {
                            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequestRetryVersionCheck, SendOption.Reliable, __instance.GetClientId());
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                        }, 1f, "Retry Version Check Task");
                    }
                    break;
                case CustomRPC.RequestRetryVersionCheck:
                    RPC.RpcVersionCheck();
                    break;
                case CustomRPC.SyncCustomSettings:
                    foreach (OptionItem co in OptionItem.AllOptions)
                        //すべてのカスタムオプションについてインデックス値で受信
                        co.SetValue(reader.ReadPackedInt32());
                    break;
                case CustomRPC.SetDeathReason:
                    RPC.GetDeathReason(reader);
                    break;
                case CustomRPC.EndGame:
                    RPC.EndGame(reader);
                    break;
                case CustomRPC.PlaySound:
                    byte playerID = reader.ReadByte();
                    Sounds sound = (Sounds)reader.ReadByte();
                    RPC.PlaySound(playerID, sound);
                    break;
                case CustomRPC.SetCustomRole:
                    byte CustomRoleTargetId = reader.ReadByte();
                    CustomRoles role = (CustomRoles)reader.ReadPackedInt32();
                    RPC.SetCustomRole(CustomRoleTargetId, role);
                    break;
                case CustomRPC.SetNameColorData:
                    NameColorManager.ReceiveRPC(reader);
                    break;
                case CustomRPC.SetLoversPlayers:
                    Main.ALoversPlayers.Clear();
                    Main.BLoversPlayers.Clear();
                    Main.CLoversPlayers.Clear();
                    Main.DLoversPlayers.Clear();
                    Main.ELoversPlayers.Clear();
                    Main.FLoversPlayers.Clear();
                    Main.GLoversPlayers.Clear();
                    int Acount = reader.ReadInt32();
                    int Bcount = reader.ReadInt32();
                    int Ccount = reader.ReadInt32();
                    int Dcount = reader.ReadInt32();
                    int Ecount = reader.ReadInt32();
                    int Fcount = reader.ReadInt32();
                    int Gcount = reader.ReadInt32();
                    for (int i = 0; i < Acount; i++)
                        Main.ALoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                    for (int i = 0; i < Bcount; i++)
                        Main.BLoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                    for (int i = 0; i < Ccount; i++)
                        Main.CLoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                    for (int i = 0; i < Dcount; i++)
                        Main.DLoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                    for (int i = 0; i < Ecount; i++)
                        Main.ELoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                    for (int i = 0; i < Fcount; i++)
                        Main.FLoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                    for (int i = 0; i < Gcount; i++)
                        Main.GLoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                    break;
                case CustomRPC.SetMaLovers:
                    Main.MaMaLoversPlayers.Clear();
                    int Macount = reader.ReadInt32();
                    for (int i = 0; i < Macount; i++)
                        Main.MaMaLoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                    break;
                case CustomRPC.SetRealKiller:
                    byte targetId = reader.ReadByte();
                    byte killerId = reader.ReadByte();
                    RPC.SetRealKiller(targetId, killerId);
                    break;
                case CustomRPC.SyncRoomTimer:
                    float timer = 0;
                    if (float.TryParse(reader.ReadString(), out timer))
                        _ = GameStartManagerPatch.SetTimer(timer - 0.5f);
                    else _ = GameStartManagerPatch.SetTimer(0);
                    Logger.Info($"{timer - 0.5f}", "settimer");
                    break;
                case CustomRPC.Guess:
                    GuessManager.ReceiveRPC(reader, __instance);
                    break;
                case CustomRPC.GuessKill:
                    GuessManager.RpcClientGuess(Utils.GetPlayerById(reader.ReadByte()));
                    break;
                case CustomRPC.SyncYomiage:
                    if (Main.SyncYomiage.Value)
                    {
                        ChatCommands.YomiageS.Clear();
                        foreach (PlayerControl pc in Main.AllPlayerControls)
                            ChatCommands.YomiageS[reader.ReadInt32()] = reader.ReadString();
                    }
                    break;
                case CustomRPC.CustomRoleSync:
                    CustomRoleManager.DispatchRpc(reader);
                    break;
            }
        }
    }

    internal static class RPC
    {
        //SyncCustomSettingsRPC Sender
        public static void SyncCustomSettingsRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncCustomSettings, SendOption.Reliable, -1);
            foreach (OptionItem co in OptionItem.AllOptions)
                //すべてのカスタムオプションについてインデックス値で送信
                writer.WritePacked(co.GetValue());
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void PlaySoundRPC(byte PlayerID, Sounds sound)
        {
            if (AmongUsClient.Instance.AmHost)
                PlaySound(PlayerID, sound);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlaySound, SendOption.Reliable, -1);
            writer.Write(PlayerID);
            writer.Write((byte)sound);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ExileAsync(PlayerControl player)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            player.Exiled();
        }
        public static async void RpcVersionCheck()
        {
            while (PlayerControl.LocalPlayer == null) await Task.Delay(500);
            MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VersionCheck, SendOption.Reliable);
            writer.Write(Main.PluginVersion);
            writer.Write($"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})");
            writer.Write(Main.ForkId);
            writer.EndMessage();
            Main.playerVersion[PlayerControl.LocalPlayer.PlayerId] = new PlayerVersion(Main.PluginVersion, $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})", Main.ForkId);
        }
        public static void RpcSyncRoomTimer()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoomTimer, SendOption.Reliable, -1);
            writer.Write($"{GameStartManagerPatch.GetTimer()}");
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SendDeathReason(byte playerId, CustomDeathReason deathReason)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDeathReason, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write((int)deathReason);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SyncYomiage()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncYomiage, SendOption.Reliable, -1);
            foreach (System.Collections.Generic.KeyValuePair<int, string> data in ChatCommands.YomiageS)
            {
                writer.Write(data.Key);
                writer.Write(data.Value);
            }
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void GetDeathReason(MessageReader reader)
        {
            byte playerId = reader.ReadByte();
            CustomDeathReason deathReason = (CustomDeathReason)reader.ReadInt32();
            PlayerState state = PlayerState.GetByPlayerId(playerId);
            state.DeathReason = deathReason;
            state.IsDead = true;
        }

        public static void EndGame(MessageReader reader)
        {
            try
            {
                CustomWinnerHolder.ReadFrom(reader);
            }
            catch (Exception ex)
            {
                Logger.Error($"正常にEndGameを行えませんでした。\n{ex}", "EndGame", false);
            }
        }
        public static void PlaySound(byte playerID, Sounds sound)
        {
            if (PlayerControl.LocalPlayer.PlayerId == playerID)
                switch (sound)
                {
                    case Sounds.KillSound:
                        _ = SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.KillSfx, false, 0.8f);
                        break;
                    case Sounds.TaskComplete:
                        _ = SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskCompleteSound, false, 0.8f);
                        break;
                    default:
                        break;
                }
        }
        public static void SetCustomRole(byte targetId, CustomRoles role)
        {
            RoleBase roleClass = CustomRoleManager.GetByPlayerId(targetId);
            if (roleClass != null && role < CustomRoles.NotAssigned)
            {
                PlayerControl player = roleClass.Player;
                roleClass.Dispose();
                CustomRoleManager.CreateInstance(role, player);
            }
            else if (role < CustomRoles.NotAssigned)
            {
                PlayerState.GetByPlayerId(targetId).SetMainRole(role);
                CustomRoleManager.CreateInstance(role, Utils.GetPlayerById(targetId));
            }
            else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole
                PlayerState.GetByPlayerId(targetId).SetSubRole(role);

            HudManager.Instance.SetHudActive(true);
            if (PlayerControl.LocalPlayer.PlayerId == targetId) RemoveDisableDevicesPatch.UpdateDisableDevices();
        }
        public static void SyncLoversPlayers()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetLoversPlayers, SendOption.Reliable, -1);
            writer.Write(Main.ALoversPlayers.Count);
            writer.Write(Main.BLoversPlayers.Count);
            writer.Write(Main.CLoversPlayers.Count);
            writer.Write(Main.DLoversPlayers.Count);
            writer.Write(Main.ELoversPlayers.Count);
            writer.Write(Main.FLoversPlayers.Count);
            writer.Write(Main.GLoversPlayers.Count);
            foreach (PlayerControl lp in Main.ALoversPlayers) writer.Write(lp.PlayerId);
            foreach (PlayerControl lp in Main.BLoversPlayers) writer.Write(lp.PlayerId);
            foreach (PlayerControl lp in Main.CLoversPlayers) writer.Write(lp.PlayerId);
            foreach (PlayerControl lp in Main.DLoversPlayers) writer.Write(lp.PlayerId);
            foreach (PlayerControl lp in Main.ELoversPlayers) writer.Write(lp.PlayerId);
            foreach (PlayerControl lp in Main.FLoversPlayers) writer.Write(lp.PlayerId);
            foreach (PlayerControl lp in Main.GLoversPlayers) writer.Write(lp.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SyncMaLoversPlayers()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMaLovers, SendOption.Reliable, -1);
            writer.Write(Main.MaMaLoversPlayers.Count);
            foreach (PlayerControl lp in Main.MaMaLoversPlayers)
                writer.Write(lp.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SendRpcLogger(uint targetNetId, byte callId, int targetClientId = -1)
        {
            if (!DebugModeManager.AmDebugger) return;
            string rpcName = GetRpcName(callId);
            string from = targetNetId.ToString();
            string target = targetClientId.ToString();
            try
            {
                target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId).PlayerName;
                from = Main.AllPlayerControls.FirstOrDefault(c => c.NetId == targetNetId)?.Data?.PlayerName;
            }
            catch { }
            Logger.Info($"FromNetID:{targetNetId}({from}) TargetClientID:{targetClientId}({target}) CallID:{callId}({rpcName})", "SendRPC");
        }
        public static string GetRpcName(byte callId)
        {
            string rpcName;
            if ((rpcName = Enum.GetName(typeof(RpcCalls), callId)) != null) { }
            else if ((rpcName = Enum.GetName(typeof(CustomRPC), callId)) != null) { }
            else rpcName = callId.ToString();
            return rpcName;
        }
        public static void SetRealKiller(byte targetId, byte killerId)
        {
            PlayerState state = PlayerState.GetByPlayerId(targetId);
            state.RealKiller.Item1 = DateTime.Now;
            state.RealKiller.Item2 = killerId;

            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRealKiller, SendOption.Reliable, -1);
            writer.Write(targetId);
            writer.Write(killerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpc))]
    internal class StartRpcPatch
    {
        public static void Prefix(InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId)
        {
            RPC.SendRpcLogger(targetNetId, callId);
        }
    }
    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpcImmediately))]
    internal class StartRpcImmediatelyPatch
    {
        public static void Prefix(InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId, [HarmonyArgument(3)] int targetClientId = -1)
        {
            RPC.SendRpcLogger(targetNetId, callId, targetClientId);
        }
    }
}