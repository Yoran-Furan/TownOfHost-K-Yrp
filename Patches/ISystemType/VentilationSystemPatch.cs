using HarmonyLib;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using System.Linq;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.UpdateSystem))]
class VentilationSystemUpdateSystemPatch
{
    public static bool Prefix([HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        ushort opId;
        VentilationSystem.Operation op;
        byte ventId;
        {
            var newReader = MessageReader.Get(msgReader);
            opId = newReader.ReadUInt16();
            op = (VentilationSystem.Operation)newReader.ReadByte();
            ventId = newReader.ReadByte();
            newReader.Recycle();
        }

        foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
        {
            roleClass.OnVentilationSystemUpdate(player, op, ventId);
        }

        //タスクを持っていないならベント掃除をなかったことにする 多分次リリースに入れる？
        //return Utils.HasTasks(player.Data);

        if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
            return false; //タスバトだとベント掃除で追い出されないように～

        return true;
    }
}
