using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class Mayor : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Mayor),
            player => new Mayor(player),
            CustomRoles.Mayor,
            () => OptionHasPortableButton.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20200,
            SetupOptionItem,
            "my",
            "#204d42",
            introSound: () => GetIntroSound(RoleTypes.Crewmate),
            from: From.TownOfUs
        );
    public Mayor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        AdditionalVote = OptionAdditionalVote.GetInt();
        HasPortableButton = OptionHasPortableButton.GetBool();
        NumOfUseButton = OptionNumOfUseButton.GetInt();
        Kakusei = OptionKakusei.GetBool();
        Count = OptionCount.GetInt();
        KadditionaVote = OptionKadditionaVote.GetInt();

        LeftButtonCount = NumOfUseButton;
    }

    private static OptionItem OptionAdditionalVote;
    private static OptionItem OptionKakusei;
    private static OptionItem OptionCount;
    private static OptionItem OptionKadditionaVote;
    private static OptionItem OptionHasPortableButton;
    private static OptionItem OptionNumOfUseButton;
    enum OptionName
    {
        MayorAdditionalVote,
        MayorHasPortableButton,
        Kakusei,
        KakuseiCount,
        KaddionaVote,
        MayorNumOfUseButton,
    }
    public static int AdditionalVote;
    public static bool HasPortableButton;
    public static bool Kakusei;
    public static int Count;
    public static int KadditionaVote;
    public static int NumOfUseButton;

    public int LeftButtonCount;
    private static void SetupOptionItem()
    {
        OptionAdditionalVote = IntegerOptionItem.Create(RoleInfo, 10, OptionName.MayorAdditionalVote, new(0, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Votes);
        OptionKakusei = BooleanOptionItem.Create(RoleInfo, 11, OptionName.Kakusei, false, false);
        OptionCount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.KakuseiCount, new(1, 15, 1), 6, false, OptionKakusei)
            .SetValueFormat(OptionFormat.Players);
        OptionKadditionaVote = IntegerOptionItem.Create(RoleInfo, 13, OptionName.MayorAdditionalVote, new(1, 99, 1), 1, false, OptionKakusei)
            .SetValueFormat(OptionFormat.Votes);
        OptionHasPortableButton = BooleanOptionItem.Create(RoleInfo, 14, OptionName.MayorHasPortableButton, false, false);
        OptionNumOfUseButton = IntegerOptionItem.Create(RoleInfo, 15, OptionName.MayorNumOfUseButton, new(1, 99, 1), 1, false, OptionHasPortableButton)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown =
            LeftButtonCount <= 0
            ? 255f
            : opt.GetInt(Int32OptionNames.EmergencyCooldown);
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override void OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (Is(reporter) && target == null) //ボタン
            LeftButtonCount--;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (LeftButtonCount > 0)
        {
            var user = physics.myPlayer;
            physics.RpcBootFromVent(ventId);
            user?.ReportDeadBody(null);
        }

        return false;
    }
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        if (voterId == Player.PlayerId && Count >= Utils.AllAlivePlayersCount && Kakusei)
        {
            numVotes = AdditionalVote + KadditionaVote + 1;
        }
        else
        if (voterId == Player.PlayerId)
        {
            numVotes = AdditionalVote + 1;
        }
        return (votedForId, numVotes, doVote);
    }
    public override void AfterMeetingTasks()
    {
        if (HasPortableButton)
            Player.RpcResetAbilityCooldown();
    }
}