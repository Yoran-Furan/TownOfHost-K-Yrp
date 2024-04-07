using System.Text;

namespace TownOfHost.Roles.Core.Descriptions;

public abstract class RoleDescription
{
    public RoleDescription(SimpleRoleInfo roleInfo)
    {
        RoleInfo = roleInfo;
    }

    public SimpleRoleInfo RoleInfo { get; }
    /// <summary>イントロなどで表示される短い文</summary>
    public abstract string Blurb { get; }
    /// <summary>
    /// ヘルプコマンドで使用される長い説明文<br/>
    /// AmongUs2023.7.12時点で，Impostor, Crewmateに関してはバニラ側でロング説明文が未実装のため「タスクを行う」と表示される
    /// </summary>
    public abstract string Description { get; }
    public string FullFormatHelp
    {
        get
        {
            var builder = new StringBuilder(256);
            //役職とイントロ
            builder.AppendFormat("<b><line-height=2.0pic><size={0}>{1}\n", FirstHeaderSize, Translator.GetRoleString(RoleInfo.RoleName.ToString()).Color(RoleInfo.RoleColor.ToReadableColor()));
            builder.AppendFormat("<line-height=1.8pic><size={0}>{1}</b>\n", InfoSize, Blurb.Color(RoleInfo.RoleColor.ToReadableColor()));
            // 陣営
            builder.AppendFormat("<size={0}>{1}:", SecondSize, Translator.GetString("Team"));
            var roleTeam = RoleInfo.CustomRoleType == CustomRoleTypes.Madmate ? CustomRoleTypes.Impostor : RoleInfo.CustomRoleType;
            builder.AppendFormat("<b><size={0}>{1}</b>    ", SecondSize, Translator.GetString($"CustomRoleTypes.{roleTeam}"));
            // バニラ置き換え役職
            builder.AppendFormat("<size={0}>{1}", SecondSize, Translator.GetString("Basis"));
            builder.AppendFormat("<line-height=1.3pic><size={0}>:{1}\n", SecondSize, Translator.GetString(RoleInfo.BaseRoleType.Invoke().ToString()));
            //From
            if (RoleInfo.From != From.None) builder.AppendFormat("<line-height=1.3pic><size={0}>{1}\n", SecondSize, Utils.GetFrom(RoleInfo));

            //説明
            builder.AppendFormat("<line-height=1.3pic><size={0}>\n", BlankLineSize);
            builder.AppendFormat("<size={0}>{1}\n", BodySize, Description);
            //設定
            var sb = new StringBuilder();
            Utils.ShowChildrenSettings(Options.CustomRoleSpawnChances[RoleInfo.RoleName], ref sb);
            if (RoleInfo.CustomRoleType == CustomRoleTypes.Madmate)
            {
                string rule = "┣ ";
                string ruleFooter = "┗ ";
                sb.Append($"{Options.MadMateOption.GetName()}: {Options.MadMateOption.GetString().Color(Palette.ImpostorRed)}\n");
                if (Options.MadMateOption.GetBool())
                {
                    sb.Append($"{rule}{Options.MadmateCanFixLightsOut.GetName()}: {Options.MadmateCanFixLightsOut.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateCanFixComms.GetName()}: {Options.MadmateCanFixComms.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateHasSun.GetName()}: {Options.MadmateHasSun.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateHasMoon.GetName()}: {Options.MadmateHasMoon.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateCanSeeKillFlash.GetName()}: {Options.MadmateCanSeeKillFlash.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateCanSeeOtherVotes.GetName()}: {Options.MadmateCanSeeOtherVotes.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateCanSeeDeathReason.GetName()}: {Options.MadmateCanSeeDeathReason.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateRevengeCrewmate.GetName()}: {Options.MadmateRevengeCrewmate.GetString()}\n");
                    if (Options.MadmateRevengeCrewmate.GetBool())
                    {
                        sb.Append($"┃ {rule}{Options.MadNekomataCanImp.GetName()}: {Options.MadNekomataCanImp.GetString()}\n");
                        sb.Append($"┃ {rule}{Options.MadNekomataCanMad.GetName()}: {Options.MadNekomataCanMad.GetString()}\n");
                        sb.Append($"┃ {rule}{Options.MadNekomataCanCrew.GetName()}: {Options.MadNekomataCanCrew.GetString()}\n");
                        sb.Append($"┃ {ruleFooter}{Options.MadNekomataCanNeu.GetName()}: {Options.MadNekomataCanNeu.GetString()}\n");
                    }
                    sb.Append($"{rule}{Options.MadmateVentCooldown.GetName()}: {Options.MadmateVentCooldown.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateVentMaxTime.GetName()}: {Options.MadmateVentMaxTime.GetString()}\n");
                    sb.Append($"{ruleFooter}{Options.MadmateCanMovedByVent.GetName()}: {Options.MadmateCanMovedByVent.GetString()}\n");
                }
            }
            if (sb.ToString() != "")
            {
                builder.AppendFormat("<size={0}>\n", BlankLineSize);
                builder.AppendFormat("<size={0}>{1}\n", InfoSize, Translator.GetString("Settings"));
                builder.AppendFormat("<line-height=1.3pic><size={0}>{1}", BodySize, sb);
            }
            return builder.ToString();
        }
    }

    public const string FirstHeaderSize = "150%";
    public const string InfoSize = "90%";
    public const string SecondHeaderSize = "80%";
    public const string SecondSize = "70%";
    public const string BodySize = "60%";
    public const string BlankLineSize = "30%";
}
