using AmongUs.GameOptions;
using System.Collections.Generic;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using System.Linq;
using UnityEngine;
using TownOfHost.Modules;
using Hazel;
using System.Text;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;

// コードがクッソ長い!!スパゲッティかよ!!まぁ処理が複雑な役職だからね。仕方ない。
//
// メモ
// ・キルクール増加,キルク減少はバグありなので一回封印。
// 追加したいなぁって思ってるの
//ペンギン
//マジシャン
//ネコカボチャ

public sealed class Alien : RoleBase, IMeetingTimeAlterable, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Alien),
                player => new Alien(player),
                CustomRoles.Alien,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                3340,
                SetupOptionItem,
                "Al",
                introSound: () => GetIntroSound(RoleTypes.Shapeshifter)
            );
    public Alien(PlayerControl player)
: base(
    RoleInfo,
    player
    )
    {
        MV = OptionMV.GetInt();
        EH = OptionEH.GetInt();
        Li = OptionLi.GetInt();
        N = OptionN.GetInt();
        P = OptionP.GetInt();
        S = OptionS.GetInt();
        R = OptionR.GetInt();
        NM = OptionNM.GetInt();
        TT = OptionTT.GetInt();
        TR = OptionTR.GetInt();
        M = OptionM.GetInt();
        PK = OptionPK.GetInt();
        Mo = OptionMo.GetInt();
        modeNone = true;
        modeV = false;
        modeE = false;
        modeL = false;
        modeN = false;
        modeP = false;
        modeS = false;
        modeR = false;
        modeNM = false;
        modeTT = false;
        modeTR = false;
        modeM = false;
        modePK = false;
        modeMo = false;
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        TTMtg = OptionTTMtg.GetInt();
        NMc = OptionNMc.GetInt();
        V = OptionV.GetFloat();
        Hitoku = OptionHitoku.GetBool();
        L = OptionL.GetFloat();
        Rtarget = 111;
        TTKaese = OptionTTKaese.GetBool();
        Sd = Optionsd.GetInt();
        Count = 0;
        DeathReasonTairo = OptionDeathReasonTairo.GetBool();
        AdditionalVote = OptionAdditionalVote.GetInt();
        Madseer = OptionMadseer.GetBool();
        Workhorseseer = OptionWorkhorseseer.GetBool();
    }

    //ヴァンパイア
    private static OptionItem OptionMV;
    private static OptionItem OptionV;
    private static float V;
    private static int MV;
    static bool modeV;
    //イビルハッカー
    private static OptionItem OptionEH;
    private static int EH;
    static bool modeE;
    //リミッター
    private static OptionItem OptionLi;
    private static OptionItem OptionL;
    private static float L;
    private static int Li;
    static bool modeL;
    //ノーマル
    private static OptionItem OptionN;
    private static int N;
    static bool modeN;
    //パペッティア
    private static OptionItem OptionP;
    private static int P;
    static bool modeP;
    //リモートキラー
    private static OptionItem OptionR;
    private static int R;
    static bool modeR;
    private static byte Rtarget;
    //ステルス
    private static OptionItem OptionS;
    private static int S;
    static bool modeS;
    private static OptionItem Optionsd;
    private float Sd;
    private float darkenTimer;
    private PlayerControl[] darkenedPlayers;
    private SystemTypes? darkenedRoom = null;
    //ノイズメーカー
    private static OptionItem OptionNM;
    private static OptionItem OptionNMc;
    private static int NM;
    static bool modeNM;
    private static int NMc;
    //タイムシーフ
    private static OptionItem OptionTT;
    private static OptionItem OptionTTMtg;
    private static int TT;
    static bool modeTT;
    private static int TTMtg;
    private static OptionItem OptionTTKaese;
    public static bool TTKaese;
    public bool RevertOnDie => TTKaese;
    static int Count;
    //大狼
    public static OptionItem OptionTR;
    public static OptionItem OptionDeathReasonTairo;
    private static int TR;
    public static bool modeTR;
    public static bool DeathReasonTairo;
    //メイヤー
    public static OptionItem OptionM;
    private static OptionItem OptionAdditionalVote;
    private static int M;
    public static bool modeM;
    public static int AdditionalVote;
    //モグラ
    private static OptionItem OptionMo;
    public static int Mo;
    public static bool modeMo;
    //プログレスキラー
    private static OptionItem OptionPK;
    public static OptionItem OptionMadseer;
    public static OptionItem OptionWorkhorseseer;
    public static int PK;
    public static bool modePK;
    public static bool Madseer;
    public static bool Workhorseseer;
    //秘匿設定
    static OptionItem OptionHitoku;
    static bool Hitoku;
    static bool modeNone;
    enum OptionName
    {
        Hitoku,
        CVampire, VampireKillDelay,
        CEvilHacker,
        CLimiter, blastrange,
        CPape,
        CR,
        CS, StealthDarkenDuration,
        CNomal,
        CNM, Probability,
        CTT, TimeThiefDecreaseMeetingTime, TimeThiefReturnStolenTimeUponDeath,
        CTR, DeathReasonTairo,
        CM, MayorAdditionalVote,
        CMo,
        CPk, Madseer, Workhorseseer
    }
    Dictionary<byte, float> BittenPlayers = new(14);
    private static void SetupOptionItem()
    {
        OptionHitoku = BooleanOptionItem.Create(RoleInfo, 9, OptionName.Hitoku, false, false);
        OptionMV = FloatOptionItem.Create(RoleInfo, 10, OptionName.CVampire, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionV = FloatOptionItem.Create(RoleInfo, 11, OptionName.VampireKillDelay, new(5, 100, 1), 10, false, OptionMV).SetValueFormat(OptionFormat.Seconds);
        OptionEH = FloatOptionItem.Create(RoleInfo, 12, OptionName.CEvilHacker, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionLi = FloatOptionItem.Create(RoleInfo, 13, OptionName.CLimiter, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionL = FloatOptionItem.Create(RoleInfo, 14, OptionName.blastrange, new(0.5f, 20f, 0.5f), 5f, false, OptionLi);
        OptionP = FloatOptionItem.Create(RoleInfo, 15, OptionName.CPape, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionS = FloatOptionItem.Create(RoleInfo, 18, OptionName.CS, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        Optionsd = FloatOptionItem.Create(RoleInfo, 19, OptionName.StealthDarkenDuration, new(0.5f, 5f, 0.5f), 1f, false, OptionS).SetValueFormat(OptionFormat.Seconds);
        OptionR = FloatOptionItem.Create(RoleInfo, 20, OptionName.CR, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionNM = FloatOptionItem.Create(RoleInfo, 21, OptionName.CNM, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionNMc = FloatOptionItem.Create(RoleInfo, 22, OptionName.Probability, new(0, 100, 5), 50, false, OptionNM).SetValueFormat(OptionFormat.Percent);
        OptionTT = FloatOptionItem.Create(RoleInfo, 23, OptionName.CTT, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionTTMtg = FloatOptionItem.Create(RoleInfo, 24, OptionName.TimeThiefDecreaseMeetingTime, new(0, 100, 5), 50, false, OptionTT).SetValueFormat(OptionFormat.Seconds);
        OptionTTKaese = BooleanOptionItem.Create(RoleInfo, 25, OptionName.TimeThiefReturnStolenTimeUponDeath, false, false, OptionTT);
        OptionTR = FloatOptionItem.Create(RoleInfo, 26, OptionName.CTR, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionDeathReasonTairo = BooleanOptionItem.Create(RoleInfo, 27, OptionName.DeathReasonTairo, false, false, OptionTR);
        OptionM = FloatOptionItem.Create(RoleInfo, 28, OptionName.CM, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionAdditionalVote = IntegerOptionItem.Create(RoleInfo, 29, OptionName.MayorAdditionalVote, new(1, 99, 1), 1, false, OptionM).SetValueFormat(OptionFormat.Votes);
        OptionMo = FloatOptionItem.Create(RoleInfo, 30, OptionName.CMo, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionPK = FloatOptionItem.Create(RoleInfo, 31, OptionName.CPk, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionMadseer = BooleanOptionItem.Create(RoleInfo, 32, OptionName.Madseer, false, false, OptionPK);
        OptionWorkhorseseer = BooleanOptionItem.Create(RoleInfo, 33, OptionName.Workhorseseer, false, false, OptionPK);
        OptionN = FloatOptionItem.Create(RoleInfo, 8, OptionName.CNomal, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
    }
    public override string GetProgressText(bool comms = false)
    {
        //初手ターンは確定でNone.
        if (modeNone) return "<size=75%><color=#ff1919>mode:None</color></size>";
        if (!Hitoku && !GameStates.Meeting) return "<size=75%><color=#ff1919>mode:？</color></size>";
        if (!Player.IsAlive()) return "";
        if (modeV) return "<size=75%><color=#ff1919>mode:" + GetString("Vampire") + "</color></size>";
        if (modeE) return "<size=75%><color=#ff1919>mode:" + GetString("EvilHacker") + "</color></size>";
        if (modeL) return "<size=75%><color=#ff1919>mode:" + GetString("Limiter") + "</color></size>";
        if (modeP) return "<size=75%><color=#ff1919>mode:" + GetString("Puppeteer") + "</color></size>";
        if (modeS) return "<size=75%><color=#ff1919>mode:" + GetString("Stealth") + "</color></size>";
        if (modeR) return "<size=75%><color=#8f00ce>mode:" + GetString("Remotekiller") + "</color></size>";
        if (modeNM) return "<size=75%><color=#ff1919>mode:" + GetString("Noisemaker") + "</color></size>";
        if (modeTT) return "<size=75%><color=#ff1919>mode:" + GetString("TimeThief") + "</color></size>";
        if (modeTR) return "<size=75%><color=#ff1919>mode:" + GetString("Tairou") + "</color></size>";
        if (modeM) return "<size=75%><color=#204d42>mode:" + GetString("Mayor") + "</color></size>";
        if (modeMo) return "<size=75%><color=#ff1919>mode:" + GetString("Mole") + "</color></size>";
        if (modePK) return "<size=75%><color=#ff1919>mode:" + GetString("ProgressKiller") + "</color></size>";
        if (modeN) return "<size=75%><color=#ff1919>mode:Normal</color></size>";
        return "<size=75%><color=#ff1919>mode:None</color></size>";
    }
    public override void OnReportDeadBody(PlayerControl player, GameData.PlayerInfo __)
    {
        if (!Player.IsAlive())//死んでたらこれより後の処理しない。
        {
            return;
        }
        //とりま状態リセット
        Rtarget = 111;
        Puppets.Clear();
        SendRPC(byte.MaxValue, 0);
        foreach (var targetId in BittenPlayers.Keys)
        {
            var target = Utils.GetPlayerById(targetId);
            KillBitten(target, true);
        }
        BittenPlayers.Clear();
        //会議始めに何の能力だったかを伝える(霊界にも伝える)
        //会議中に切り替えれるようにしたからいらないっ
        /*        foreach (var pc in Main.AllPlayerControls.Where(pc => !pc.IsAlive() || pc.Is(CustomRoles.Alien)))
        {
            //メイヤーの時のみ通知秘匿にかかわらず通知
            if (modeM) Utils.SendMessage("今...君の能力は...\nメイヤーだよ。\nこの会議での投票数が増えるから気を付けてね。", pc.PlayerId);

            if (!Hitoku)//秘匿がないならそもそも伝えない。
            {
                if (modeV) Utils.SendMessage("さっきまでの君の能力は...\nヴァンパイアだったよ。", pc.PlayerId);
                if (modeE) Utils.SendMessage("さっきまでの君の能力は...\nイビルハッカーだったよ。", pc.PlayerId);
                if (modeL) Utils.SendMessage("さっきまでの君の能力は...\nリミッターだったよ。", pc.PlayerId);
                if (modeP) Utils.SendMessage("さっきまでの君の能力は...\nパペッティアだったよ。", pc.PlayerId);
                if (modeS) Utils.SendMessage("さっきまでの君の能力は...\nステルスだったよ。", pc.PlayerId);
                if (modeR) Utils.SendMessage("さっきまでの君の能力は...\nリモートキラーだったよ。", pc.PlayerId);
                if (modeNM) Utils.SendMessage("さっきまでの君の能力は...\nノイズメーカーだったよ。", pc.PlayerId);
                if (modeTT) Utils.SendMessage("さっきまでの君の能力は...\nタイムシーフだったよ。", pc.PlayerId);
                if (modeTR) Utils.SendMessage("さっきまでの君の能力は...\n大狼だったよ。", pc.PlayerId);
                if (modeMo) Utils.SendMessage("さっきまでの君の能力は...\nモグラだったよ。", pc.PlayerId);
                if (modePK) Utils.SendMessage("さっきまでの君の能力は...\nプログレスキラーだったよ。", pc.PlayerId);
                if (modeN) Utils.SendMessage("さっきまでの君は...\nノーマル状態だったよ。", pc.PlayerId);
            }
        }*/

        //modeE(イビルハッカーの時だけ)
        if (modeE)
        {
            var admins = AdminProvider.CalculateAdmin();
            var builder = new StringBuilder(512);

            // 送信するメッセージを生成
            foreach (var admin in admins)
            {
                var entry = admin.Value;
                if (entry.TotalPlayers <= 0)
                {
                    continue;
                }
                // インポスターがいるなら星マークを付ける
                if (entry.NumImpostors > 0)
                {
                    builder.Append('★');
                }
                // 部屋名と合計プレイヤー数を表記
                builder.Append(DestroyableSingleton<TranslationController>.Instance.GetString(entry.Room));
                builder.Append(": ");
                builder.Append(entry.TotalPlayers);
                // 死体があったら死体の数を書く
                if (entry.NumDeadBodies > 0)
                {
                    builder.Append('(').Append(Translator.GetString("Deadbody"));
                    builder.Append('×').Append(entry.NumDeadBodies).Append(')');
                }
                builder.Append('\n');
            }

            // 送信
            var message = builder.ToString();
            var title = Utils.ColorString(Color.green, Translator.GetString("LastAdminInfo"));

            _ = new LateTask(() =>
            {
                if (GameStates.IsInGame)
                {
                    Utils.SendMessage(message, Player.PlayerId, title, false);
                }
            }, 4f, "EvilHacker Admin Message");
            return;
        }
    }

    /// 次ターンの能力を決める。
    public override void AfterMeetingTasks()
    {
        if (!Player.IsAlive())//死んでたら処理しない。
        {
            return;
        }
        //とりま全モード一旦false
        modeNone = false;
        modeV = false;
        modeE = false;
        modeL = false;
        modeN = false;
        modeP = false;
        modeS = false;
        modeR = false;
        modeTT = false;
        modeNM = false;
        modeTR = false;
        modeM = false;
        modeMo = false;
        modePK = false;

        int Count = MV + EH + Li + P + S + R + NM + TT + TR + M + Mo + PK + N;
        int chance = IRandom.Instance.Next(1, Count);
        //ランダム
        if (chance <= MV)
        {
            modeV = true;
            Logger.Info("Alienはヴァンパイアになりました。", "Alien");
        }
        else
        if (chance <= MV + EH)
        {
            Logger.Info("Alienはイビルハッカーになりました。", "Alien");
            modeE = true;
        }
        else
        if (chance <= MV + EH + Li)
        {
            Logger.Info("Alienはリミッターになりました。", "Alien");
            modeL = true;
        }
        else
        if (chance <= MV + EH + Li + P)
        {
            Logger.Info("Alienはパペッティアになりました。", "Alien");
            modeP = true;
        }
        else
        if (chance <= MV + EH + Li + P + S)
        {
            Logger.Info("Alienはステルスになりました。", "Alien");
            modeS = true;
        }
        else
        if (chance <= MV + EH + Li + P + S + R)
        {
            Logger.Info("Alienはリモートキラーになりました。", "Alien");
            modeR = true;
        }
        else
        if (chance <= MV + EH + Li + P + S + R + NM)
        {
            Logger.Info("Alienはノイズメーカーになりました。", "Alien");
            modeNM = true;
        }
        else
        if (chance <= MV + EH + Li + P + S + R + NM + TT)
        {
            Logger.Info("Alienはタイムシーフになりました。", "Alien");
            modeTT = true;
        }
        else
        if (chance <= MV + EH + Li + P + S + R + NM + TT + TR)
        {
            Logger.Info("Alienは大狼になりました。", "Alien");
            modeTR = true;
        }
        else
        if (chance <= MV + EH + Li + P + S + R + NM + TT + TR + M)
        {
            Logger.Info("Alienはメイヤーになりました。", "Alien");
            modeM = true;
        }
        else
        if (chance <= MV + EH + Li + P + S + R + NM + TT + TR + M + Mo)
        {
            Logger.Info("Alienはモグラになりました。", "Alien");
            modeMo = true;
        }
        else
        if (chance <= MV + EH + Li + P + S + R + NM + TT + TR + M + Mo + PK)
        {
            Logger.Info("Alienはプログレスキラーになりました。", "Alien");
            modePK = true;
        }
        else//どれにもあてはまらないならとりあえずノーマル
        {
            Logger.Info("ｴｰﾘｱﾝﾜﾀｼｴｰﾘｱﾝ", "Alien");
            modeN = true;
        }
    }
    public override void OnStartMeeting()
    {
        if (AmongUsClient.Instance.AmHost)
        {
            ResetDarkenState();
        }
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (modeV)//ヴァンパイアの時のみ実行
        {
            if (!info.CanKill) return; //キル出来ない相手には無効
            var (killer, target) = info.AttemptTuple;
            if (target.Is(CustomRoles.Bait)) return;
            if (info.IsFakeSuicide) return;
            if (!BittenPlayers.ContainsKey(target.PlayerId))
            {
                killer.SetKillCooldown();
                BittenPlayers.Add(target.PlayerId, 0f);
            }
            info.DoKill = false;
        }

        //リミッターの時のみ実行。
        if (modeL)
        {
            var Targets = new List<PlayerControl>(Main.AllAlivePlayerControls);
            foreach (var tage in Targets)
            {
                info.DoKill = false;
                var distance = Vector3.Distance(Player.transform.position, tage.transform.position);
                if (distance > L) continue;
                PlayerState.GetByPlayerId(tage.PlayerId).DeathReason = CustomDeathReason.Bombed;
                tage.SetRealKiller(tage);
                tage.RpcMurderPlayer(tage, true);
                RPC.PlaySoundRPC(tage.PlayerId, Sounds.KillSound);
            }
        }

        //パペッティアの時のみ実行。
        if (modeP)
        {
            var (puppeteer, target) = info.AttemptTuple;

            Puppets[target.PlayerId] = this;
            SendRPC(target.PlayerId, 1);
            puppeteer.SetKillCooldown();
            Utils.NotifyRoles(SpecifySeer: puppeteer);
            info.DoKill = false;
        }

        //ステルスの時のみ
        if (modeS)
        {
            // キルできない，もしくは普通のキルじゃないならreturn
            if (!info.CanKill || !info.DoKill || info.IsSuicide || info.IsAccident || info.IsFakeSuicide)
            {
                return;
            }
            var playersToDarken = FindPlayersInSameRoom(info.AttemptTarget);
            if (playersToDarken == null)
            {
                Logger.Info("部屋の当たり判定を取得できないため暗転を行いません", "Alien.S");
                return;
            }
            DarkenPlayers(playersToDarken);
        }
        //リモートキラー
        if (modeR)
        {
            if (!info.CanKill) return; //キル出来ない相手には無効
            var (killer, target) = info.AttemptTuple;
            if (target.Is(CustomRoles.Bait)) return;
            if (info.IsFakeSuicide) return;
            //登録
            Rtarget = target.PlayerId;
            killer.RpcProtectedMurderPlayer(target);
            info.DoKill = false;
        }
        //ノイズメーカー
        if (modeNM)
        {
            if (!info.IsSuicide)
            {
                (var killer, var target) = info.AttemptTuple;
                int chance = IRandom.Instance.Next(1, 101);
                if (chance <= NMc)
                {
                    Logger.Info($"{killer?.Data?.PlayerName}: フラ全体通知成功", "Noisemaker");
                    foreach (var player in PlayerControl.AllPlayerControls)
                    {
                        player.KillFlash();
                    }
                }
                else
                {
                    Logger.Info($"{killer?.Data?.PlayerName}: フラ全体通知失敗", "Noisemaker");
                }
            }
        }
        if (modeTT)//タイムシーフはタイムシーフモード中じゃないと会議時間を減らさない。
        {
            if (!info.IsSuicide)
            {
                Count++;//キルが成功したらカウントを1増やす。
            }
        }
    }
    public override void OnFixedUpdate(PlayerControl _)
    {
        //ヴァンパイアの時のみ実行
        if (modeV)
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask) return;

            foreach (var (targetId, timer) in BittenPlayers.ToArray())
            {
                if (timer >= V)
                {
                    var target = Utils.GetPlayerById(targetId);
                    KillBitten(target);
                    BittenPlayers.Remove(targetId);
                }
                else
                {
                    BittenPlayers[targetId] += Time.fixedDeltaTime;
                }
            }
        }
        if (modeS)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                return;
            }
            // 誰かを暗転させているとき
            if (darkenedPlayers != null)
            {
                // タイマーを減らす
                darkenTimer -= Time.fixedDeltaTime;
                // タイマーが0になったらみんなの視界を戻してタイマーと暗転プレイヤーをリセットする
                if (darkenTimer <= 0)
                {
                    ResetDarkenState();
                }
            }
        }
    }
    /// <summary>
    /// そもそもヴァンパイアモードの時しか呼び出されないはず。
    /// </summary>
    /// <param name="target"></param>
    /// <param name="isButton"></param>
    private void KillBitten(PlayerControl target, bool isButton = false)
    {
        var vampire = Player;
        if (target.IsAlive())
        {
            PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Bite;
            target.SetRealKiller(vampire);
            CustomRoleManager.OnCheckMurder(
                vampire, target,
                target, target
            );
            Logger.Info($"Alienに噛まれている{target.name}を自爆させました。", "Alien.Va");
            if (!isButton && vampire.IsAlive())
            {
                RPC.PlaySoundRPC(vampire.PlayerId, Sounds.KillSound);
            }
        }
        else
        {
            Logger.Info($"Alienに噛まれている{target.name}はすでに死んでいました。", "Alien.Va");
        }
    }
    //これより↓パペッティア
    private static Dictionary<byte, Alien> Puppets = new(15);
    public override void OnDestroy()
    {
        Puppets.Clear();
    }

    private enum RPC_type
    {
        SyncPuppet,
        StealthDarken
    }

    private void SendRPC(byte targetId, byte typeId)
    {
        using var sender = CreateSender();

        sender.Writer.Write((byte)RPC_type.SyncPuppet);
        sender.Writer.Write(typeId);
        sender.Writer.Write(targetId);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        switch ((RPC_type)reader.ReadByte())
        {
            case RPC_type.StealthDarken:
                var roomId = reader.ReadByte();
                darkenedRoom = roomId == byte.MaxValue ? null : (SystemTypes)roomId;
                break;

            case RPC_type.SyncPuppet:
                var typeId = reader.ReadByte();
                var targetId = reader.ReadByte();

                switch (typeId)
                {
                    case 0: //Dictionaryのクリア
                        Puppets.Clear();
                        break;
                    case 1: //Dictionaryに追加
                        Puppets[targetId] = this;
                        break;
                    case 2: //DictionaryのKey削除
                        Puppets.Remove(targetId);
                        break;
                }
                break;
        }
    }
    public void OnFixedUpdateOthers(PlayerControl puppet)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (Puppets.TryGetValue(puppet.PlayerId, out var puppeteer))
            CheckPuppetKillA(puppet);
    }
    private void CheckPuppetKillA(PlayerControl puppet)
    {
        if (!puppet.IsAlive())
        {
            Puppets.Remove(puppet.PlayerId);
            SendRPC(puppet.PlayerId, 2);
        }
        else
        {
            var puppetPos = puppet.transform.position;//puppetの位置
            Dictionary<PlayerControl, float> targetDistance = new();
            foreach (var pc in Main.AllAlivePlayerControls.ToArray())
            {
                if (pc.PlayerId != puppet.PlayerId && !pc.Is(CountTypes.Impostor))
                {
                    var dis = Vector2.Distance(puppetPos, pc.transform.position);
                    targetDistance.Add(pc, dis);
                }
            }
            if (targetDistance.Keys.Count <= 0) return;

            var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
            var target = min.Key;
            var KillRange = NormalGameOptionsV07.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
            if (min.Value <= KillRange && puppet.CanMove && target.CanMove)
            {
                RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
                target.SetRealKiller(Player);
                puppet.RpcMurderPlayer(target);
                Utils.MarkEveryoneDirtySettings();
                Puppets.Remove(puppet.PlayerId);
                SendRPC(puppet.PlayerId, 2);
                Utils.NotifyRoles();
            }
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        if (modeP)
        {
            seen ??= seer;
            if (!(Puppets.ContainsValue(this) &&
                Puppets.ContainsKey(seen.PlayerId))) return "";
            return Utils.ColorString(RoleInfo.RoleColor, "◆");
        }
        else
        if (modeS)
        {
            if (seer != Player || seen != Player || !darkenedRoom.HasValue)
            {
                return base.GetSuffix(seer, seen);
            }
            return string.Format(Translator.GetString("StealthDarkened"), DestroyableSingleton<TranslationController>.Instance.GetString(darkenedRoom.Value));
        }
        if (modePK)
        {
            seen ??= seer;
            if (Madseer && seen.Is(CustomRoleTypes.Madmate) && seer.Is(CustomRoles.Alien) && seer != seen)
            {
                if (seen.GetPlayerTaskState().IsTaskFinished)
                    return Utils.ColorString(RoleInfo.RoleColor, "☆");
            }
            return "";
        }
        else return "";
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (modePK)
        {
            //seenが省略の場合seer
            seen ??= seer;

            if (seer.Is(CustomRoles.Alien) && !seen.Is(CustomRoleTypes.Madmate) && seer != seen)
            {
                if (seen.GetPlayerTaskState().IsTaskFinished)
                    return Utils.ColorString(RoleInfo.RoleColor, "〇");
            }
            return "";
        }
        return "";
    }
    //これより↓ステルス
    private IEnumerable<PlayerControl> FindPlayersInSameRoom(PlayerControl killedPlayer)
    {
        var room = killedPlayer.GetPlainShipRoom();
        if (room == null)
        {
            return null;
        }
        var roomArea = room.roomArea;
        var roomName = room.RoomId;
        RpcDarken(roomName);
        return Main.AllAlivePlayerControls.Where(player => player != Player && player.Collider.IsTouching(roomArea));
    }
    /// <summary>渡されたプレイヤーを<see cref="darkenDuration"/>秒分視界ゼロにする</summary>
    private void DarkenPlayers(IEnumerable<PlayerControl> playersToDarken)
    {
        darkenedPlayers = playersToDarken.ToArray();
        foreach (var player in playersToDarken)
        {
            PlayerState.GetByPlayerId(player.PlayerId).IsBlackOut = true;
            player.MarkDirtySettings();
        }
    }
    private void RpcDarken(SystemTypes? roomType)
    {
        Logger.Info($"暗転させている部屋を{roomType?.ToString() ?? "null"}に設定", "Alien.S");
        darkenedRoom = roomType;
        using var sender = CreateSender();
        sender.Writer.Write((byte)RPC_type.StealthDarken);
        sender.Writer.Write((byte?)roomType ?? byte.MaxValue);
    }
    private void ResetDarkenState()
    {
        if (darkenedPlayers != null)
        {
            foreach (var player in darkenedPlayers)
            {
                PlayerState.GetByPlayerId(player.PlayerId).IsBlackOut = false;
                player.MarkDirtySettings();
            }
            darkenedPlayers = null;
        }
        darkenTimer = Sd;
        RpcDarken(null);
        Utils.NotifyRoles(SpecifySeer: Player);
    }
    //リモートキラー
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (modeR)
        {
            var user = physics.myPlayer;
            if (Rtarget != 111 && Player.PlayerId == user.PlayerId)
            {
                var target = Utils.GetPlayerById(Rtarget);
                if (!target.IsAlive()) return true;
                _ = new LateTask(() =>
                {
                    target.SetRealKiller(user);
                    user.RpcMurderPlayer(target, true);
                }, 1f);
                RPC.PlaySoundRPC(user.PlayerId, Sounds.KillSound);
                Rtarget = 111;
                return false;
            }
        }
        if (modeMo)//モグラ
        {
            _ = new LateTask(() =>
            {
                if (Main.NormalOptions.MapId == 0)
                {
                    int chance = IRandom.Instance.Next(1, 15);
                    if (chance == 1) Player.RpcSnapToForced(new Vector2(4.3f, -0.3f));
                    if (chance == 2) Player.RpcSnapToForced(new Vector2(9.4f, -6.4f));
                    if (chance == 3) Player.RpcSnapToForced(new Vector2(2.5f, -10.0f));
                    if (chance == 4) Player.RpcSnapToForced(new Vector2(8.8f, 3.3f));
                    if (chance == 5) Player.RpcSnapToForced(new Vector2(16.0f, -3.2f));
                    if (chance == 6) Player.RpcSnapToForced(new Vector2(16.2f, -6.0f));
                    if (chance == 7) Player.RpcSnapToForced(new Vector2(9.5f, -14.3f));
                    if (chance == 8) Player.RpcSnapToForced(new Vector2(-9.7f, -8.1f));
                    if (chance == 9) Player.RpcSnapToForced(new Vector2(-10.6f, -4.2f));
                    if (chance == 10) Player.RpcSnapToForced(new Vector2(-12.5f, -6.9f));
                    if (chance == 11) Player.RpcSnapToForced(new Vector2(-15.2f, 2.5f));
                    if (chance == 12) Player.RpcSnapToForced(new Vector2(-21.9f, -3.1f));
                    if (chance == 13) Player.RpcSnapToForced(new Vector2(-20.7f, -7.0f));
                    if (chance == 14) Player.RpcSnapToForced(new Vector2(-15.3f, -13.7f));

                };
                if (Main.NormalOptions.MapId == 1)
                {
                    int chance = IRandom.Instance.Next(1, 12);
                    if (chance == 1) Player.RpcSnapToForced(new Vector2(23.8f, -1.9f));
                    if (chance == 2) Player.RpcSnapToForced(new Vector2(15.4f, -1.8f));
                    if (chance == 3) Player.RpcSnapToForced(new Vector2(4.3f, 0.5f));
                    if (chance == 4) Player.RpcSnapToForced(new Vector2(-6.2f, 3.6f));
                    if (chance == 5) Player.RpcSnapToForced(new Vector2(0.5f, 10.7f));
                    if (chance == 6) Player.RpcSnapToForced(new Vector2(11.6f, 13.8f));
                    if (chance == 7) Player.RpcSnapToForced(new Vector2(6.8f, 3.1f));
                    if (chance == 8) Player.RpcSnapToForced(new Vector2(13.3f, 20.1f));
                    if (chance == 9) Player.RpcSnapToForced(new Vector2(17.8f, 25.2f));
                    if (chance == 10) Player.RpcSnapToForced(new Vector2(22.4f, 17.2f));
                    if (chance == 11) Player.RpcSnapToForced(new Vector2(23.9f, 7.2f));
                };
                if (Main.NormalOptions.MapId == 2)
                {
                    int chance = IRandom.Instance.Next(1, 13);
                    if (chance == 1) Player.RpcSnapToForced(new Vector2(19.0f, -24.8f));
                    if (chance == 2) Player.RpcSnapToForced(new Vector2(20.1f, -25.0f));
                    if (chance == 3) Player.RpcSnapToForced(new Vector2(30.9f, -11.9f));
                    if (chance == 4) Player.RpcSnapToForced(new Vector2(33.0f, -9.6f));
                    if (chance == 5) Player.RpcSnapToForced(new Vector2(23.8f, -7.7f));
                    if (chance == 6) Player.RpcSnapToForced(new Vector2(9.6f, -7.7f));
                    if (chance == 7) Player.RpcSnapToForced(new Vector2(2.0f, -9.5f));
                    if (chance == 8) Player.RpcSnapToForced(new Vector2(6.9f, -14.4f));
                    if (chance == 9) Player.RpcSnapToForced(new Vector2(3.5f, -16.6f));
                    if (chance == 10) Player.RpcSnapToForced(new Vector2(12.2f, -18.8f));
                    if (chance == 11) Player.RpcSnapToForced(new Vector2(16.4f, -19.6f));
                    if (chance == 12) Player.RpcSnapToForced(new Vector2(22.0f, -12.2f));

                };
                if (Main.NormalOptions.MapId == 4)
                {
                    int chance = IRandom.Instance.Next(1, 13);
                    if (chance == 1) Player.RpcSnapToForced(new Vector2(-22.0f, -1.6f));
                    if (chance == 2) Player.RpcSnapToForced(new Vector2(-12.6f, 8.5f));
                    if (chance == 3) Player.RpcSnapToForced(new Vector2(-15.7f, -11.7f));
                    if (chance == 4) Player.RpcSnapToForced(new Vector2(-2.7f, -9.3f));
                    if (chance == 5) Player.RpcSnapToForced(new Vector2(0.2f, -2.5f));
                    if (chance == 6) Player.RpcSnapToForced(new Vector2(7.0f, -3.7f));
                    if (chance == 7) Player.RpcSnapToForced(new Vector2(9.8f, 3.1f));
                    if (chance == 8) Player.RpcSnapToForced(new Vector2(3.6f, 6.9f));
                    if (chance == 9) Player.RpcSnapToForced(new Vector2(12.7f, 5.9f));
                    if (chance == 10) Player.RpcSnapToForced(new Vector2(23.2f, 8.3f));
                    if (chance == 11) Player.RpcSnapToForced(new Vector2(24.0f, -1.4f));
                    if (chance == 12) Player.RpcSnapToForced(new Vector2(30.4f, -3.6f));
                }
                if (Main.NormalOptions.MapId == 5)
                {
                    int chance = IRandom.Instance.Next(1, 11);
                    if (chance == 1) Player.RpcSnapToForced(new Vector2(-15.4f, -9.9f));
                    if (chance == 2) Player.RpcSnapToForced(new Vector2(1.3f, -10.6f));
                    if (chance == 3) Player.RpcSnapToForced(new Vector2(15.2f, -16.4f));
                    if (chance == 4) Player.RpcSnapToForced(new Vector2(22.8f, -8.5f));
                    if (chance == 5) Player.RpcSnapToForced(new Vector2(25.2f, 11.0f));
                    if (chance == 6) Player.RpcSnapToForced(new Vector2(9.4f, 0.6f));
                    if (chance == 7) Player.RpcSnapToForced(new Vector2(-12.2f, 8.0f));
                    if (chance == 8) Player.RpcSnapToForced(new Vector2(-16.9f, -2.6f));
                    if (chance == 9) Player.RpcSnapToForced(new Vector2(-2.5f, -9.0f));
                    if (chance == 10) Player.RpcSnapToForced(new Vector2(2.9f, 0.9f));
                };

            }, 1.0f, "TP");
            return true;

        }
        return true;
    }
    public override bool CantVentIdo(PlayerPhysics physics, int ventId)
    {
        if (modeMo) return false;
        return true;
    }
    //タイムシーフ
    public int CalculateMeetingTimeDelta()
    {
        var sec = -(TTMtg * Count);
        return sec;
    }
    //大老
    public override CustomRoles GetFtResults(PlayerControl player) => modeTR ? CustomRoles.Alien : CustomRoles.Crewmate;
    //メイヤー
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        if (voterId == Player.PlayerId && modeM)
        {
            numVotes = AdditionalVote + 1;
        }
        return (votedForId, numVotes, doVote);
    }
}