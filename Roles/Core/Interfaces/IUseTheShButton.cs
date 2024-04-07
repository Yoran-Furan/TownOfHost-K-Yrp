namespace TownOfHost.Roles.Core.Interfaces;

/// <summary>
///ワンクリックシェイプボタンを使う役職
/// <summary>
public interface IUseTheShButton
{
    public void Shape(PlayerControl Player)
    {
        if (!AmongUsClient.Instance.AmHost || Player.shapeshifting || !UseOCButton) return;
        PlayerSkinPatch.Save(Player);
        Player.RpcShapeshift(PlayerControl.LocalPlayer, false);
        ResetSkin(Player);
    }

    public bool CheckShapeshift(PlayerControl Player, PlayerControl target)
    {
        if (target.PlayerId == Player.PlayerId && UseOCButton && AmongUsClient.Instance.AmHost)
        {
            if (GameStates.IsInTask && !Utils.IsActive(SystemTypes.MushroomMixupSabotage))
                OnClick();
            Player.RpcRejectShapeshift();
            return false;
        }
        return true;
    }
    public void ResetSkin(PlayerControl Player)
    {
        if (!UseOCButton) return;
        Player.RpcShapeshift(PlayerControl.LocalPlayer, false);
        var sd = PlayerSkinPatch.Load(Player);
        Player.RpcSetColor((byte)sd.Item2);
        Player.RpcSetHat(sd.Item3);
        Player.RpcSetSkin(sd.Item4);
        Player.RpcSetVisor(sd.Item5);
        Player.RpcSetPet(sd.Item8);
        Player.RpcSetName(sd.Item1);
        _ = new LateTask(() =>
        {
            Player.RpcSetHat(sd.Item3);
            Player.RpcSetSkin(sd.Item4);
            Player.RpcSetVisor(sd.Item5);
            Utils.NotifyRoles(false, Player, NoCache: true);
        }, 0.3f);
    }
    public void ResetS(PlayerControl Player)
    {
        if (!UseOCButton || !AmongUsClient.Instance.AmHost) return;
        var sd = PlayerSkinPatch.Load(Player);
        Player.RpcSetHat(sd.Item3);
        Player.RpcSetSkin(sd.Item4);
        Player.RpcSetVisor(sd.Item5);
    }

    public void OnClick()
    { }
    /// <summary>ワンクリックボタンが使えるか</summary>
    public bool UseOCButton => true;
}
