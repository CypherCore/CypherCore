/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

namespace Framework.Constants
{
    public class GuildConst
    {
        public const int MaxBankTabs = 8;
        public const int MaxBankSlots = 98;
        public const int BankMoneyLogsTab = 100;

        public const uint WithdrawMoneyUnlimited = 0xFFFFFFFF;
        public const int WithdrawSlotUnlimited = -1;
        public const uint EventLogGuidUndefined = 0xFFFFFFFF;

        public const uint ChallengesTypes = 6;
        public const uint CharterItemId = 5863;

        public const int RankNone = 0xFF;
        public const int MinRanks = 5;
        public const int MaxRanks = 10;

        public const int BankLogMaxRecords = 25;
        public const int EventLogMaxRecords = 100;
        public const int NewsLogMaxRecords = 250;

        public static int[] ChallengeGoldReward = { 0, 250, 1000, 500, 250, 500 };
        public static int[] ChallengeMaxLevelGoldReward = { 0, 125, 500, 250, 125, 250 };
        public static int[] ChallengesMaxCount = { 0, 7, 1, 3, 0, 3 };

        public static uint MinNewsItemLevel = 353;

        public static byte OldMaxLevel = 25;

        public static uint MasterDethroneInactiveDays = 90;
    }

    public enum GuildRankRights
    {
        None = 0x00000000,
        GChatListen = 0x00000001,
        GChatSpeak = 0x00000002,
        OffChatListen = 0x00000004,
        OffChatSpeak = 0x00000008,
        Invite = 0x00000010,
        Remove = 0x00000020,
        Roster = 0x00000040,
        Promote = 0x00000080,
        Demote = 0x00000100,
        Unk200 = 0x00000200,
        Unk400 = 0x00000400,
        Unk800 = 0x00000800,
        SetMotd = 0x00001000,
        EditPublicNote = 0x00002000,
        ViewOffNote = 0x00004000,
        EOffNote = 0x00008000,
        ModifyGuildInfo = 0x00010000,
        WithdrawGoldLock = 0x00020000,                   // remove money withdraw capacity
        WithdrawRepair = 0x00040000,                   // withdraw for repair
        WithdrawGold = 0x00080000,                   // withdraw gold
        CreateGuildEvent = 0x00100000,                   // wotlk
        All = 0x00DDFFBF
    }

    public struct GuildDefaultRanks
    {
        public const int Master = 0;
        public const int Officer = 1;
        public const int Veteran = 2;
        public const int Member = 3;
        public const int Initiate = 4;
    }

    public enum GuildMemberFlags
    {
        None = 0,
        Online = 1,
        AFK = 2,
        DND = 3,
        Mobile = 4
    }

    public enum GuildMemberData
    {
        ZoneId,
        AchievementPoints,
        Level,
    }

    public enum GuildCommandType
    {
        CreateGuild = 0,
        InvitePlayer = 1,
        LeaveGuild = 3,
        GetRoster = 5,
        PromotePlayer = 6,
        DemotePlayer = 7,
        RemovePlayer = 8,
        ChangeLeader = 10,
        EditMOTD = 11,
        GuildChat = 13,
        Founder = 14,
        ChangeRank = 16,
        EditPublicNote = 19,
        ViewTab = 21,
        MoveItem = 22,
        Repair = 25
    }

    public enum GuildCommandError
    {
        Success = 0,
        GuildInternal = 1,
        AlreadyInGuild = 2,
        AlreadyInGuild_S = 3,
        InvitedToGuild = 4,
        AlreadyInvitedToGuild_S = 5,
        NameInvalid = 6,
        NameExists_S = 7,
        LeaderLeave = 8,
        Permissions = 8,
        PlayerNotInGuild = 9,
        PlayerNotInGuild_S = 10,
        PlayerNotFound_S = 11,
        NotAllied = 12,
        RankTooHigh_S = 13,
        RankTooLow_S = 14,
        RanksLocked = 17,
        RankInUse = 18,
        IgnoringYou_S = 19,
        Unk1 = 20,
        WithdrawLimit = 25,
        NotEnoughMoney = 26,
        BankFull = 28,
        ItemNotFound = 29,
        TooMuchMoney = 31,
        WrongTab = 32,
        RequiresAuthenticator = 34,
        BankVoucherFailed = 35,
        TrialAccount = 36,
        UndeletableDueToLevel = 37,
        MoveStarting = 38,
        RepTooLow = 39
    }

    public enum GuildEventLogTypes
    {
        InvitePlayer = 1,
        JoinGuild = 2,
        PromotePlayer = 3,
        DemotePlayer = 4,
        UninvitePlayer = 5,
        LeaveGuild = 6,
    }

    public enum GuildBankEventLogTypes
    {
        DepositItem = 1,
        WithdrawItem = 2,
        MoveItem = 3,
        DepositMoney = 4,
        WithdrawMoney = 5,
        RepairMoney = 6,
        MoveItem2 = 7,
        Unk1 = 8,
        BuySlot = 9,
        CashFlowDeposit = 10
    }

    public enum GuildEmblemError
    {
        Success = 0,
        InvalidTabardColors = 1,
        NoGuild = 2,
        NotGuildMaster = 3,
        NotEnoughMoney = 4,
        InvalidVendor = 5
    }

    public enum GuildBankRights
    {
        ViewTab = 0x01,
        PutItem = 0x02,
        UpdateText = 0x04,

        DepositItem = ViewTab | PutItem,
        Full = -1
    }

    public enum GuildNews
    {
        Achievement = 0,
        PlayerAchievement = 1,
        DungeonEncounter = 2,
        ItemLooted = 3,
        ItemCrafted = 4,
        ItemPurchased = 5,
        LevelUp = 6,
        Create = 7,
        Event = 8
    }

    public enum PetitionTurns
    {
        Ok = 0,
        AlreadyInGuild = 2,
        NeedMoreSignatures = 4,
        GuildPermissions = 11,
        GuildNameInvalid = 12
    }

    public enum PetitionSigns
    {
        Ok = 0,
        AlreadySigned = 1,
        AlreadyInGuild = 2,
        CantSignOwn = 3,
        NotServer = 4,
        Full = 5,
        AlreadySignedOther = 6,
        RestrictedAccount = 7
    }

    public enum CharterTypes
    {
        Guild = 4,
        Arena2v2 = 2,
        Arena3v3 = 3,
        Arena5v5 = 5,
    }

    public struct CharterCosts
    {
        public const uint Guild = 1000;
        public const uint Arena2v2 = 800000;
        public const uint Arena3v3 = 1200000;
        public const uint Arena5v5 = 2000000;
    }

    public enum GuildFinderOptionsInterest
    {
        Questing = 0x01,
        Dungeons = 0x02,
        Raids = 0x04,
        PVP = 0x08,
        RolePlaying = 0x10,
        All = Questing | Dungeons | Raids | PVP | RolePlaying
    }

    public enum GuildFinderOptionsAvailability
    {
        Weekdays = 0x1,
        Weekends = 0x2,
        Always = Weekdays | Weekends
    }

    public enum GuildFinderOptionsRoles
    {
        Tank = 0x1,
        Healer = 0x2,
        DPS = 0x4,
        All = Tank | Healer | DPS
    }

    public enum GuildFinderOptionsLevel
    {
        Any = 0x1,
        Max = 0x2,
        All = Any | Max
    }
}
