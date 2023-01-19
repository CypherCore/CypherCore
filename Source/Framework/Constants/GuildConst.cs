// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Framework.Constants
{
    public class GuildConst
    {
        public const int MaxBankTabs = 8;
        public const int MaxBankSlots = 98;
        public const int BankMoneyLogsTab = 100;

        public const ulong MoneyLimit = 100000000000;
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

    [Flags]
    public enum GuildRankRights
    {
        None = 0x00,
        GChatListen = 0x01,
        GChatSpeak = 0x02,
        OffChatListen = 0x04,
        OffChatSpeak = 0x08,
        Invite = 0x10,
        Remove = 0x20,
        Roster = 0x40,
        Promote = 0x80,
        Demote = 0x100,
        Unk200 = 0x200,
        Unk400 = 0x400,
        Unk800 = 0x800,
        SetMotd = 0x1000,
        EditPublicNote = 0x2000,
        ViewOffNote = 0x4000,
        EOffNote = 0x8000,
        ModifyGuildInfo = 0x10000,
        WithdrawGoldLock = 0x20000,                   // remove money withdraw capacity
        WithdrawRepair = 0x40000,                   // withdraw for repair
        WithdrawGold = 0x80000,                   // withdraw gold
        CreateGuildEvent = 0x100000,                   // wotlk
        InAuthenticatedRank = 0x200000,
        EditGuildBankTabInfo = 0x400000,
        Officer = 0x800000,
        All = 0x00DDFFBF
    }

    public enum GuildRankId
    {
        GuildMaster = 0
    }

    public enum GuildRankOrder
    {

    }

    [Flags]
    public enum GuildMemberFlags
    {
        None = 0x00,
        Online = 0x01,
        AFK = 0x02,
        DND = 0x04,
        Mobile = 0x08
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
        GuildNameInvalid = 12,
        HasRestriction = 13
    }

    public enum PetitionSigns
    {
        Ok = 0,
        AlreadySigned = 1,
        AlreadyInGuild = 2,
        CantSignOwn = 3,
        NotServer = 5,
        Full = 8,
        AlreadySignedOther = 10,
        RestrictedAccountTrial = 11,
        HasRestriction = 13
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
}
