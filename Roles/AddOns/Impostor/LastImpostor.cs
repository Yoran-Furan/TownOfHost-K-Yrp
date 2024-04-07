using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Impostor
{
    public static class LastImpostor
    {
        private static readonly int Id = 79100;
        public static byte currentId = byte.MaxValue;
        public static OptionItem KillCooldown;
        //ゲッサー
        public static OptionItem GiveGuesser;
        public static OptionItem CanGuessTime; public static OptionItem OwnCanGuessTime; public static OptionItem TryHideMsg;
        public static OptionItem ICanGuessVanilla; public static OptionItem ICanGuessNakama; public static OptionItem ICanGuessTaskDoneSnitch;
        public static OptionItem ICanWhiteCrew; public static OptionItem AddTama;
        //ディレクター
        public static OptionItem GiveDire;
        public static OptionItem comms; public static OptionItem PercentGage; public static OptionItem Meeting;
        public static OptionItem PonkotuPercernt;
        //ウォッチャー
        public static OptionItem GiveWatcher;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.LastImpostor, new(1, 1, 1), fromtext: "<color=#ffffff>From:<color=#00bfff>Town_Of_Host</color></size>");
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 1f), 15f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastImpostor])
                .SetValueFormat(OptionFormat.Seconds);
            GiveGuesser = BooleanOptionItem.Create(Id + 11, "GiveGuesser", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastImpostor]);
            CanGuessTime = FloatOptionItem.Create(Id + 12, "CanGuessTime", new(1, 15, 1), 3, TabGroup.Addons, false).SetParent(GiveGuesser)
                .SetValueFormat(OptionFormat.Players);
            AddTama = BooleanOptionItem.Create(Id + 9, "Addtama", false, TabGroup.Addons, false).SetParent(GiveGuesser);
            OwnCanGuessTime = FloatOptionItem.Create(Id + 13, "OwnCanGuessTime", new(1, 15, 1), 1, TabGroup.Addons, false).SetParent(GiveGuesser)
                    .SetValueFormat(OptionFormat.Players);
            TryHideMsg = BooleanOptionItem.Create(Id + 14, "TryHideMsg", true, TabGroup.Addons, false).SetParent(GiveGuesser);
            ICanGuessVanilla = BooleanOptionItem.Create(Id + 15, "CanGuessVanilla", true, TabGroup.Addons, false).SetParent(GiveGuesser);
            ICanGuessNakama = BooleanOptionItem.Create(Id + 16, "CanGuessNakama", true, TabGroup.Addons, false).SetParent(GiveGuesser);
            ICanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 17, "CanGuessTaskDoneSnitch", false, TabGroup.Addons, false).SetParent(GiveGuesser);
            ICanWhiteCrew = BooleanOptionItem.Create(Id + 18, "CanWhiteCrew", false, TabGroup.Addons, false).SetParent(GiveGuesser);
            GiveDire = BooleanOptionItem.Create(Id + 19, "GiveDire", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastImpostor]);
            PercentGage = BooleanOptionItem.Create(Id + 20, "PercentGage", false, TabGroup.Addons, false).SetParent(GiveDire);
            PonkotuPercernt = BooleanOptionItem.Create(Id + 21, "PonkotuPercernt", true, TabGroup.Addons, false).SetParent(PercentGage);
            comms = BooleanOptionItem.Create(Id + 22, "CanseeComms", false, TabGroup.Addons, false).SetParent(GiveDire);
            Meeting = BooleanOptionItem.Create(Id + 23, "CanseeMeeting", false, TabGroup.Addons, false).SetParent(GiveDire);
            GiveWatcher = BooleanOptionItem.Create(Id + 24, "GiveWat", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastImpostor]);
        }
        public static void Init() => currentId = byte.MaxValue;
        public static void Add(byte id) => currentId = id;
        public static void SetKillCooldown()
        {
            if (currentId == byte.MaxValue) return;
            Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
        }
        public static bool CanBeLastImpostor(PlayerControl pc)
        {
            if (!pc.IsAlive() || pc.Is(CustomRoles.LastImpostor) || !pc.Is(CustomRoleTypes.Impostor))
            {
                return false;
            }
            if (pc.GetRoleClass() is IImpostor impostor)
            {
                return impostor.CanBeLastImpostor;
            }
            return true;
        }
        public static void SetSubRole()
        {
            //ラストインポスターがすでにいれば処理不要
            if (currentId != byte.MaxValue) return;
            if (CurrentGameMode == CustomGameMode.HideAndSeek
            || !CustomRoles.LastImpostor.IsPresent() || Main.AliveImpostorCount != 1)
                return;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (CanBeLastImpostor(pc))
                {
                    pc.RpcSetCustomRole(CustomRoles.LastImpostor);
                    Add(pc.PlayerId);
                    SetKillCooldown();
                    pc.SyncSettings();
                    Utils.NotifyRoles();
                    break;
                }
            }
        }
    }
}