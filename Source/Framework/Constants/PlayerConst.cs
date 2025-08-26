// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Framework.Constants
{
    public struct PlayerConst
    {
        public const Expansion CurrentExpansion = Expansion.TheWarWithin;

        public const int MaxTalentTiers = 7;
        public const int MaxTalentColumns = 4;
        public const int MaxTalentRank = 5;
        public const int MaxPvpTalentSlots = 4;
        public const int MinSpecializationLevel = 10;
        public const int MaxSpecializations = 5;
        public const int InitialSpecializationIndex = 4;
        public const int MaxMasterySpells = 2;

        public const int ReqPrimaryTreeTalents = 31;

        public const int ExploredZonesSize = 240;
        public const int ExploredZonesBits = sizeof(ulong) * 8;
        public const int DataFlagValueBits = sizeof(ulong) * 8;

        public const ulong MaxMoneyAmount = 99999999999UL;
        public const int MaxActionButtons = 180;
        public const int MaxActionButtonActionValue = 0x00FFFFFF + 1;

        public const int MaxDailyQuests = 25;

        public static TimeSpan InfinityCooldownDelay = TimeSpan.FromSeconds(Time.Month);  // used for set "infinity cooldowns" for spells and check
        public const uint infinityCooldownDelayCheck = Time.Month / 2;
        public const int MaxPlayerSummonDelay = 2 * Time.Minute;

        // corpse reclaim times
        public const int DeathExpireStep = (5 * Time.Minute);
        public const int MaxDeathCount = 3;

        public const int MaxCUFProfiles = 5;

        public static uint[] copseReclaimDelay = { 30, 60, 120 };

        public const int MaxRunes = 7;
        public const int MaxRechargingRunes = 3;

        public const int ArtifactsAllWeaponsGeneralWeaponEquippedPassive = 197886;

        public const int MaxArtifactTier = 1;

        public const int MaxHonorLevel = 500;
        public const byte LevelMinHonor = 10;
        public const uint SpellPvpRulesEnabled = 134735;

        //Azerite
        public const uint ItemIdHeartOfAzeroth = 158075;
        public const uint MaxAzeriteItemLevel = 129;
        public const uint MaxAzeriteItemKnowledgeLevel = 30;
        public const uint PlayerConditionIdUnlockedAzeriteEssences = 69048;
        public const uint SpellIdHeartEssenceActionBarOverride = 298554;

        //Warmode
        public const uint WarmodeEnlistedSpellOutside = 269083;

        public const uint SpellExperienceEliminated = 206662;

        public const uint CurrencyMaxCapAncientMana = 2000;

        public const float MaxAreaSpiritHealerRange = 20.0f;

        public const float TeleportMinLoadScreenDistance = 200.0f;
    }

    public struct MoneyConstants
    {
        public const int Copper = 1;
        public const int Silver = Copper * 100;
        public const int Gold = Silver * 100;
    }

    public enum TradeSlots
    {
        Invalid = -1,
        NonTraded = 6,
        TradedCount = 6,
        Count = 7
    }

    public enum Tutorials
    {
        Talent = 0,
        Spec = 1,
        Glyph = 2,
        SpellBook = 3,
        Professions = 4,
        CoreAbilitites = 5,
        PetJournal = 6,
        WhatHasChanged = 7,
        Max = 8
    }

    public enum TradeStatus
    {
        PlayerBusy = 0,
        Proposed = 1,
        Initiated = 2,
        Cancelled = 3,
        Accepted = 4,
        AlreadyTrading = 5,
        NoTarget = 6,
        Unaccepted = 7,
        Complete = 8,
        StateChanged = 9,
        TooFarAway = 10,
        WrongFaction = 11,
        Failed = 12,
        Petition = 13,
        PlayerIgnored = 14,
        Stunned = 15,
        TargetStunned = 16,
        Dead = 17,
        TargetDead = 18,
        LoggingOut = 19,
        TargetLoggingOut = 20,
        RestrictedAccount = 21,
        WrongRealm = 22,
        NotOnTaplist = 23,
        CurrencyNotTradable = 24,
        NotEnoughCurrency = 25,
    }

    public enum RestFlag
    {
        Tavern = 0x01,
        City = 0x02,
        FactionArea = 0x04
    }

    public enum ChatFlags
    {
        None = 0x00,
        AFK = 0x01,
        DND = 0x02,
        GM = 0x04,
        Com = 0x08, // Commentator
        Dev = 0x10,
        BossSound = 0x20, // Plays "RaidBossEmoteWarning" sound on raid boss emote/whisper
        Mobile = 0x40,
        Guide = 0x1000,
        Newcomer = 0x2000,
        Censored = 0x4000,
        Timerunning = 0x8000
    }

    public enum DrunkenState
    {
        Sober = 0,
        Tipsy = 1,
        Drunk = 2,
        Smashed = 3
    }

    public enum SpecResetType
    {
        Talents = 0,
        Specialization = 1,
        Glyphs = 2,
        PetTalents = 3
    }

    public enum MirrorTimerType
    {
        Disabled = -1,
        Fatigue = 0,
        Breath = 1,
        Fire = 2, // feign death
        Max = 3
    }

    public enum TransferAbortReason
    {
        None = 0,
        Error = 1,
        MaxPlayers = 2,   // Transfer ed: Instance Is Full
        NotFound = 3,   // Transfer ed: Instance Not Found
        TooManyInstances = 4,   // You Have Entered Too Many Instances Recently.
        ZoneInCombat = 6,   // Unable To Zone In While An Encounter Is In Progress.
        InsufExpanLvl = 7,   // You Must Have <Tbc, Wotlk> Expansion Installed To Access This Area.
        Difficulty = 8,   // <Normal, Heroic, Epic> Difficulty Mode Is Not Available For %S.
        UniqueMessage = 9,   // Until You'Ve Escaped Tlk'S Grasp, You Cannot Leave This Place!
        TooManyRealmInstances = 10,  // Additional Instances Cannot Be Launched, Please Try Again Later.
        NeedGroup = 11,  // Transfer ed: You Must Be In A Raid Group To Enter This Instance
        NotFound2 = 12,  // Transfer ed: Instance Not Found
        NotFound3 = 13,  // Transfer ed: Instance Not Found
        NotFound4 = 14,  // Transfer ed: Instance Not Found
        RealmOnly = 15,  // All Players In The Party Must Be From The Same Realm To Enter %S.
        MapNotAllowed = 16,  // Map Cannot Be Entered At This Time.
        LockedToDifferentInstance = 18,  // You Are Already Locked To %S
        AlreadyCompletedEncounter = 19,  // You Are Ineligible To Participate In At Least One Encounter In This Instance Because You Are Already Locked To An Instance In Which It Has Been Defeated.
        DifficultyNotFound = 22,  // Client Writes To Console "Unable To Resolve Requested Difficultyid %U To Actual Difficulty For Map %D"
        XrealmZoneDown = 24,  // Transfer ed: Cross-Realm Zone Is Down
        SoloPlayerSwitchDifficulty = 26,  // This Instance Is Already In Progress. You May Only Switch Difficulties From Inside The Instance.
        NotCrossFactionCompatible = 33,  // This instance isn't available for cross-faction groups
    }

    public enum RaidGroupReason
    {
        None = 0,
        Lowlevel = 1, // "You are too low level to enter this instance."
        Only = 2, // "You must be in a raid group to enter this instance."
        Full = 3, // "The instance is full."
        RequirementsUnmatch = 4  // "You do not meet the requirements to enter this instance."
    }

    public enum ResetFailedReason
    {
        Failed = 0,  // "Cannot reset %s.  There are players still inside the instance."
        Offline = 1, // "Cannot reset %s.  There are players offline in your party."
        Zoning = 2   // "Cannot reset %s.  There are players in your party attempting to zone into an instance."
    }

    public enum ActivateTaxiReply
    {
        Ok = 0,
        UnspecifiedServerError = 1,
        NoSuchPath = 2,
        NotEnoughMoney = 3,
        TooFarAway = 4,
        NoVendorNearby = 5,
        NotVisited = 6,
        PlayerBusy = 7,
        PlayerAlreadyMounted = 8,
        PlayerShapeshifted = 9,
        PlayerMoving = 10,
        SameNode = 11,
        NotStanding = 12,
    }

    public enum TaxiNodeStatus
    {
        None = 0,
        Learned = 1,
        Unlearned = 2,
        NotEligible = 3
    }

    public enum PlayerDelayedOperations
    {
        SavePlayer = 0x01,
        ResurrectPlayer = 0x02,
        SpellCastDeserter = 0x04,
        BGMountRestore = 0x08,                     // Flag to restore mount state after teleport from BG
        BGTaxiRestore = 0x10,                     // Flag to restore taxi state after teleport from BG
        BGGroupRestore = 0x20,                     // Flag to restore group state after teleport from BG
        End
    }

    public enum CorpseType
    {
        Bones = 0,
        ResurrectablePVE = 1,
        ResurrectablePVP = 2,
        Max = 3
    }

    public enum CorpseFlags
    {
        None = 0x00,
        Bones = 0x01,
        Unk1 = 0x02,
        PvP = 0x04,
        HideHelm = 0x08,
        HideCloak = 0x10,
        Skinnable = 0x20,
        FFAPvP = 0x40
    }

    public enum ActionButtonUpdateState
    {
        UnChanged = 0,
        Changed = 1,
        New = 2,
        Deleted = 3
    }

    public enum ActionButtonType
    {
        Spell = 0x00,
        C = 0x01,                         // click?
        Eqset = 0x20,
        Dropdown = 0x30,
        Macro = 0x40,
        CMacro = C | Macro,
        Companion = 0x50,
        Mount = 0x60,
        Item = 0x80
    }

    [Flags]
    public enum TeleportToOptions
    {
        None = 0x00,
        GMMode = 0x01,
        NotLeaveTransport = 0x02,
        NotLeaveCombat = 0x04,
        NotUnSummonPet = 0x08,
        Spell = 0x10,
        ReviveAtTeleport = 0x40,
        Seamless = 0x80
    }

    /// Type of environmental damages
    public enum EnviromentalDamage
    {
        Exhausted = 0,
        Drowning = 1,
        Fall = 2,
        Lava = 3,
        Slime = 4,
        Fire = 5,
        FallToVoid = 6                                 // custom case for fall without durability loss
    }

    public enum PlayerUnderwaterState
    {
        None = 0x00,
        InWater = 0x01,             // terrain type is water and player is afflicted by it
        InLava = 0x02,             // terrain type is lava and player is afflicted by it
        InSlime = 0x04,             // terrain type is lava and player is afflicted by it
        InDarkWater = 0x08,             // terrain type is dark water and player is afflicted by it

        ExistTimers = 0x10
    }

    public struct RuneCooldowns
    {
        public const int Base = 10000;
        public const int Miss = 1500;     // cooldown applied on runes when the spell misses
    }

    public enum PlayerFlags : uint
    {
        GroupLeader = 0x01,
        AFK = 0x02,
        DND = 0x04,
        GM = 0x08,
        Ghost = 0x10,
        Resting = 0x20,
        VoiceChat = 0x40,
        Unk7 = 0x80,
        ContestedPVP = 0x100,
        InPVP = 0x200,
        WarModeActive = 0x400,
        WarModeDesired = 0x800,
        PlayedLongTime = 0x1000,
        PlayedTooLong = 0x2000,
        IsOutOfBounds = 0x4000,
        Developer = 0x8000,
        LowLevelRaidEnabled = 0x10000,
        TaxiBenchmark = 0x20000,
        PVPTimer = 0x40000,
        Uber = 0x80000,
        Unk20 = 0x100000,
        Unk21 = 0x200000,
        Commentator2 = 0x400000,
        HidAccountAchievements = 0x800000,
        PetBattlesUnlocked = 0x1000000,
        NoXPGain = 0x2000000,
        Unk26 = 0x4000000,
        AutoDeclineGuild = 0x8000000,
        GuildLevelEnabled = 0x10000000,
        VoidUnlocked = 0x20000000,
        Timewalking = 0x40000000,
        CommentatorCamera = 0x80000000
    }

    public enum PlayerFlagsEx
    {
        ReagentBankUnlocked = 0x01,
        MercenaryMode = 0x02,
        ArtifactForgeCheat = 0x04,
        InPvpCombat = 0x0040,       // Forbids /Follow
        Mentor = 0x0080,
        Newcomer = 0x0100,
        UnlockedAoeLoot = 0x0200
    }

    public enum CharacterFlags
    {
        None = 0x00000000,
        InvisGod = 0x00000001, // Player Has God Invis Enabled
        Resting = 0x00000002, // Player Is Currently Earning Rest Experience
        LockedForTransfer = 0x00000004, // Player Is Locked - For Paid Character Transfer
        Silenced = 0x00000008, // Player's Chat Is Silenced (Can Talk To Gms)
        UberinvisGod = 0x00000010, // Player Has God Uberinvis Enabled
        Beastmaster = 0x00000020, // Beastmaster Is On
        PvpEnabled = 0x00000040, // Pvp Enabled
        PortAfterResurrect = 0x00000080, // World Port After Resurrect
        ResetTalentsOnLogin = 0x00000100, // Clear Talents On Login
        HasPvpRank = 0x00000200, // Player Has A Pvp Rank
        HideHelm = 0x00000400, // Hide Helm
        HideCloak = 0x00000800, // Hide Cloak
        Skinnable = 0x00001000, // Player Is Skinnable
        Ghost = 0x00002000, // Player Is A Ghost
        Rename = 0x00004000, // Set To Force A Rename
        RenameNeedsGmReview = 0x00008000, // Flag Is Set After Rename For Gm Review
        PvpDesired = 0x00010000, // Pvp Desired Flag
        GmMode = 0x00020000, // Gm Mode Enabled
        DeletedByTransfer = 0x00040000, // Deleted By A Character Transfer
        OnUnsafeTransport = 0x00080000, // On Unsafe Transport (Port To Safe Loc On Log In)
        RenameFailed = 0x00100000, // Player Unable To Rename Character
        MountUpgraded = 0x00200000, // Mount Has Been Upgraded
        FriendsListNeedsRepair = 0x00400000, // Friends List Requires A Repair
        ExplorationDataFixed = 0x00800000, // Character Had Their Exploration Data Fixed
        LockedByBilling = 0x01000000, // Locked Due To Billing
        Declined = 0x02000000, // Player Has Russian Declined Name Forms
        Commentator = 0x04000000, // Commentator Mode Enabled
        UberCommentator = 0x08000000, // Uber Commentator Mode Enabled
        XpFixed = 0x10000000, // Player's Xp Has Been Fixed (2.2.X->2.3.0)
        LogPackets = 0x20000000, // Log Player Packets
        CompensateForSpells = 0x40000000  // Compensate For Spells
    }

    public enum CharacterFlags2
    {
        None = 0x00000000,
        Customize = 0x00000001, // Player Has Paid For A Character Re-Customization
        GmSupporterProxy = 0x00000002, // Gmsupportserver Proxy, Not For Gmtool Use
        CanLoadOnNonShipTranport = 0x00000004, // Character Has Been Saved At Least Once Since Saving On Non-Ship Transports Was Added
        BattleMasterMiscIsTaxiId = 0x00000008, // Battle Master Misc Field Is A Taxi Id
        TempPetAutocastSpell1 = 0x00000010, // Temp Pet Autocast Spell 1
        TempPetAutocastSpell2 = 0x00000020, // Temp Pet Autocast Spell 2
        TempPetAutocastSpell3 = 0x00000040, // Temp Pet Autocast Spell 3
        TempPetAutocastSpell4 = 0x00000080, // Temp Pet Autocast Spell 4
        TempPetAggressive = 0x00000100, // Temp Pet Aggressive
        TempPetPassive = 0x00000200, // Temp Pet Passive
        CanInteractWithOtherRealmsInSite = 0x00000400, // Characters Can Interact With Other Realms In The Site
        CanInteractWithOtherRealmsInRegion = 0x00000800, // Characters Can Interact With Other Realms In The Region
        BattleMasterMiscIsAreaId = 0x00001000, // Battle Master Misc Field Is An Area Id
        ReevaluateAccountItemLicenses = 0x00002000, // Account Item Licenses Must Be Reevaluated On Next Login
        BattleMasterMiscIsTransport = 0x00004000, // Battle Master Misc Field Is A Transport
        TalentsResetUsingTalentGroupData = 0x00008000, // Players Talents Have Been Reset Using Talent Group Data
        FactionChange = 0x00010000, // This Character Is Eligible To Change His Faction
        HasChangedRaceOrFaction = 0x00020000, // This Character Has Changed His Race/Faction And Now Requires The World Server To Repair Him
        NoXpGain = 0x00040000, // This Character Has Chosen To Not Gain Xp By Any Means
        RecastOnResummon = 0x00080000, // Recast On Resummon
        RaceChange = 0x00100000, // This Character Is Eligible To Change His Race
        ChangedTempPetAutocastSpell1 = 0x00200000, // Player Has Changed Temp Pet Autocast Spell 1
        ChangedTempPetAutocastSpell2 = 0x00400000, // Player Has Changed Temp Pet Autocast Spell 2
        ChangedTempPetAutocastSpell3 = 0x00800000, // Player Has Changed Temp Pet Autocast Spell 3
        ChangedTempPetAutocastSpell4 = 0x01000000, // Player Has Changed Temp Pet Autocast Spell 4
        ChangedGuildDuringCharacterTransfer = 0x02000000, // Player Has Transferred Guilds During A Pct
        CanUseVoidStorageFeature = 0x04000000, // Player Is Allowed To Use The Void Storage Feature
        BattlePetsConverted = 0x08000000, // Battle Pets Converted
        QuestsFixed = 0x10000000, // Player Has Had His Quests Fixed
        LowLevelRaidEnabled = 0x20000000, // The Player Can Join Raids Even If He's Below The Min Raid Level
        AutoDeclineGuild = 0x40000000  // The Player Will Automatically Decline Guild Invites
    }

    public enum CharacterFlags3
    {
        None = 0x00000000,
        IsBattlePetTraier = 0x00000001, // Player Is A Battle Pet Trainer
        HideAccountAchievements = 0x00000002, // Player Has Opted To Hide His Account And Merged Achievements
        BattlePetsConvertedAndLocked = 0x00000004, // Battle Pet Conversion Complete But Pets Still Locked
        TransferredBetweenBnetAccounts = 0x00000008, // Player Has Performed A Pct Between Two Bnet Accounts
        UpgradeInProgress = 0x00000010, // Character Upgrade In Progress
        BattleMasterMiscIsLfgDungeon = 0x00000020, // Battle Master Misc Field Is A Lfgdungeons Rec Id
        NewPlayerGuide = 0x00000040, // New Player Guide
        LockedByRevokedCharacterUpgrade = 0x00000080, // Character Locked Due To Revoked Upgrade
        NeedsFirstTimeFixup = 0x00000100, // Character Needs First Time Fix Up
        WasBoostedAndHasLimitedSpells = 0x00000200, // Character Was Upgraded And Has Limited Access To Spells
        DidQuestItemCleanup = 0x00000400, // Did Quest Item Cleanup
        ReagentBankUnlocked = 0x00000800, // Reagent Bank Unlocked
        PetBeastmaster = 0x00001000, // Pet Beastmaster Is On
        RechargeOnLogin = 0x00002000, // Recharge On Login
        FixupWodFactionChangeBug = 0x00004000, // Fixup Wod Faction Change Bug
        FixupWodXp = 0x00008000, // Fixup Wod Xp To Set To 0 For Level 90
        CheckForRestorableData = 0x00010000, // Check For Restorable Data
        HasBnetToken = 0x00020000, // Has A Battle.Net Token
        BnetTokenTransactionInProgress = 0x00040000, // Battle.Net Token Transaction In Progress
        LevelWasScaled = 0x00080000, // Player's Level Was Scaled At Last Save
        LockedByRevokedVasTransaction = 0x00100000, // Character Locked Due To Revoked Vas Purchase
        VasProductApplicationInProgress = 0x00200000, // Character Vas Product Application In Progress
        WasRecentlyBoosted = 0x00400000, // Character Was Recently Boosted
        CurrentlyProcessingVasPurchase = 0x00800000, // Character Is Currently Processing A Vas Purchase (Disables Web Purchases)
        LegionSpecWeaponFixedUp = 0x01000000, // Legion Spec Weapon Fixed Up
        SentAchievementHistoryToBi = 0x02000000, // Sent Achievement History To Bi
        GrantedLevelsFromRaf = 0x04000000, // Character Has Been Granted At Least One Single Level Boost From Recruit A Friend System
        WarModeDesired = 0x08000000, // Character Has Opted-In To Warmode
        HonorConvertedToAccountWide = 0x10000000, // Honor Has Been Converted To Account Wide
        KeystoneFixedUpForLegionRollover = 0x20000000, // Keystone Has Been Fixed-Up For Legion Rollover
        MythicPlusSeason1AchievementFixedUp = 0x40000000, // Mythic+ Season 1 Achievement Has Been Fixed-Up
    }

    public enum CharacterFlags4
    {
        None = 0x00000000,
        UsedRpeReset = 0x00000001, // Character Has Used A Rpe Reset Recently
        OnlyRpeResetOrTimerunningEndBoost = 0x00000002, // Character Has Only Ever Used An Rpe Reset Boost Or Timerunning-Season-End Boost
        SelfFound = 0x00000004, // Character Is Self-Found, And Cannot Trade, Use The Auction House, Or Use Most Mail Functions
        TimerunningConversionDone = 0x00000008, // Character Has Been Updated To Normal Characters From A Timerunning Season
        LoggedInByCrawlerOverridingLocks = 0x00000010, // Character Is Being Logged In By Crawler Overriding Locks.
        ProcessedForWarbands = 0x00000020, // Character Has Had Their Information Added To Account-Wide Warband Tracking
        LoggedOutWhileLorewalking = 0x00000040, // Character Last Logged Out While Actively Lorewalking
        NoNeighborhoodInvites = 0x00000080, // Character Does Not Accept Neighborhood Invites
        CheckedFor2ndWaveAccountWideFactions = 0x00000100, // Character Has Been Checked For 2nd Wave Of Account Wide Factions
        WillBeResurrectedInHardcore = 0x00000200, // Character Will Be Resurrected Overriding Hardcore Game Rule
    }

    public enum CharacterRestrictionFlags
    {
        CharacterRestrictionFlagTrialBoost = 0x00000080,
        CharacterRestrictionFlagTrialBoostLocked = 0x00040000,
        CharacterRestrictionFlagExpansionTrial = 0x00080000,
    }

    public enum PlayerLocalFlags
    {
        ControllingPet = 0x01, // Displays "You have an active summon already" when trying to tame new pet
        TrackStealthed = 0x02,
        ReleaseTimer = 0x08,       // Display time till auto release spirit
        NoReleaseWindow = 0x10,        // Display no "release spirit" window at all
        NoPetBar = 0x20,   // CGPetInfo::IsPetBarUsed
        OverrideCameraMinHeight = 0x40,
        NewlyBosstedCharacter = 0x80,
        UsingPartGarrison = 0x100,
        CanUseObjectsMounted = 0x200,
        CanVisitPartyGarrison = 0x400,
        WarMode = 0x800,
        AccountSecured = 0x1000, // Script_IsAccountSecured
        OverrideTransportServerTime = 0x8000,
        MentorRestricted = 0x20000,
        HasAccountBankLock = 0x40000,
        CharacterBankDisabled = 0x80000,
        CharacterBankConversionFailed = 0x100000,
        AccountBankDisabled = 0x200000,
    }

    public enum PlayerFieldByte2Flags
    {
        None = 0x00,
        Stealth = 0x20,
        InvisibilityGlow = 0x40
    }

    public enum ArenaTeams
    {
        None = 0,
        Ally = 2,
        Horde = 4,
        Any = 6
    }

    public enum RestTypes : byte
    {
        XP = 0,
        Honor = 1,
        Max
    }

    public enum PlayerRestState
    {
        Rested = 1,
        Normal = 2,
        RAFLinked = 6
    }

    public enum TextureSection // TODO: Find a better name. Used in CharSections.dbc
    {
        BaseSkin = 0,
        Face = 1,
        FacialHair = 2,
        Hair = 3,
        Underwear = 4,
    }

    public enum AtLoginFlags
    {
        None = 0x00,
        Rename = 0x01,
        ResetSpells = 0x02,
        ResetTalents = 0x04,
        Customize = 0x08,
        ResetPetTalents = 0x10,
        FirstLogin = 0x20,
        ChangeFaction = 0x40,
        ChangeRace = 0x80,
        Resurrect = 0x100
    }

    public enum PlayerSlots
    {
        // first slot for item stored (in any way in player items data)
        Start = 0,
        // last+1 slot for item stored (in any way in player items data)
        End = 105,
        Count = (End - Start)
    }

    public enum AccountBankBagSlots
    {
        Start = 100,
        End = 105
    }

    public enum PlayerTitle : ulong
    {
        Disabled = 0x0000000000000000,
        None = 0x0000000000000001,
        Private = 0x0000000000000002, // 1
        Corporal = 0x0000000000000004, // 2
        SergeantA = 0x0000000000000008, // 3
        MasterSergeant = 0x0000000000000010, // 4
        SergeantMajor = 0x0000000000000020, // 5
        Knight = 0x0000000000000040, // 6
        KnightLieutenant = 0x0000000000000080, // 7
        KnightCaptain = 0x0000000000000100, // 8
        KnightChampion = 0x0000000000000200, // 9
        LieutenantCommander = 0x0000000000000400, // 10
        Commander = 0x0000000000000800, // 11
        Marshal = 0x0000000000001000, // 12
        FieldMarshal = 0x0000000000002000, // 13
        GrandMarshal = 0x0000000000004000, // 14
        Scout = 0x0000000000008000, // 15
        Grunt = 0x0000000000010000, // 16
        SergeantH = 0x0000000000020000, // 17
        SeniorSergeant = 0x0000000000040000, // 18
        FirstSergeant = 0x0000000000080000, // 19
        StoneGuard = 0x0000000000100000, // 20
        BloodGuard = 0x0000000000200000, // 21
        Legionnaire = 0x0000000000400000, // 22
        Centurion = 0x0000000000800000, // 23
        Champion = 0x0000000001000000, // 24
        LieutenantGeneral = 0x0000000002000000, // 25
        General = 0x0000000004000000, // 26
        Warlord = 0x0000000008000000, // 27
        HighWarlord = 0x0000000010000000, // 28
        Gladiator = 0x0000000020000000, // 29
        Duelist = 0x0000000040000000, // 30
        Rival = 0x0000000080000000, // 31
        Challenger = 0x0000000100000000, // 32
        ScarabLord = 0x0000000200000000, // 33
        Conqueror = 0x0000000400000000, // 34
        Justicar = 0x0000000800000000, // 35
        ChampionOfTheNaaru = 0x0000001000000000, // 36
        MercilessGladiator = 0x0000002000000000, // 37
        OfTheShatteredSun = 0x0000004000000000, // 38
        HandOfAdal = 0x0000008000000000, // 39
        VengefulGladiator = 0x0000010000000000, // 40
    }

    [Flags]
    public enum PlayerExtraFlags
    {
        // gm abilities
        GMOn = 0x01,
        AcceptWhispers = 0x04,
        TaxiCheat = 0x08,
        GMInvisible = 0x10,
        GMChat = 0x20,               // Show GM badge in chat messages

        // other states
        PVPDeath = 0x100,            // store PvP death status until corpse creating.

        // Character services markers
        HasRaceChanged = 0x0200,
        GrantedLevelsFromRaf = 0x0400,
        LevelBoosted = 0x0800
    }

    public enum EquipmentSetUpdateState
    {
        Unchanged = 0,
        Changed = 1,
        New = 2,
        Deleted = 3
    }

    public enum CUFBoolOptions
    {
        KeepGroupsTogether,
        DisplayPets,
        DisplayMainTankAndAssist,
        DisplayHealPrediction,
        DisplayAggroHighlight,
        DisplayOnlyDispellableDebuffs,
        DisplayPowerBar,
        DisplayBorder,
        UseClassColors,
        DisplayHorizontalGroups,
        DisplayNonBossDebuffs,
        DynamicPosition,
        Locked,
        Shown,
        AutoActivate2Players,
        AutoActivate3Players,
        AutoActivate5Players,
        AutoActivate10Players,
        AutoActivate15Players,
        AutoActivate25Players,
        AutoActivate40Players,
        AutoActivateSpec1,
        AutoActivateSpec2,
        AutoActivateSpec3,
        AutoActivateSpec4,
        AutoActivatePvp,
        AutoActivatePve,

        BoolOptionsCount,
    }

    public enum DuelCompleteType
    {
        Interrupted = 0,
        Won = 1,
        Fled = 2
    }

    public enum ReferAFriendError
    {
        None = 0,
        NotReferredBy = 1,
        TargetTooHigh = 2,
        InsufficientGrantableLevels = 3,
        TooFar = 4,
        DifferentFaction = 5,
        NotNow = 6,
        GrantLevelMaxI = 7,
        NoTarget = 8,
        NotInGroup = 9,
        SummonLevelMaxI = 10,
        SummonCooldown = 11,
        InsufExpanLvl = 12,
        SummonOfflineS = 13,
        NoXrealm = 14,
        MapIncomingTransferNotAllowed = 15
    }

    public enum PlayerCommandStates
    {
        None = 0x00,
        God = 0x01,
        Casttime = 0x02,
        Cooldown = 0x04,
        Power = 0x08,
        Waterwalk = 0x10
    }

    public enum AttackSwingErr
    {
        NotInRange = 0,
        BadFacing = 1,
        CantAttack = 2,
        DeadTarget = 3
    }

    public enum PlayerLogXPReason
    {
        Kill = 0,
        NoKill = 1
    }

    public enum HeirloomPlayerFlags
    {
        None = 0x00,
        UpgradeLevel1 = 0x01,
        UpgradeLevel2 = 0x02,
        UpgradeLevel3 = 0x04,
        UpgradeLevel4 = 0x08,
        UpgradeLevel5 = 0x10,
        UpgradeLevel6 = 0x20,
    }

    public enum HeirloomItemFlags
    {
        None = 0x00,
        ShowOnlyIfKnown = 0x01,
        Pvp = 0x02
    }

    public enum DeclinedNameResult
    {
        Success = 0,
        Error = 1
    }

    public enum TalentLearnResult
    {
        LearnOk = 0,
        FailedUnknown = 1,
        FailedNotEnoughTalentsInPrimaryTree = 2,
        FailedNoPrimaryTreeSelected = 3,
        FailedCantDoThatRightNow = 4,
        FailedAffectingCombat = 5,
        FailedCantRemoveTalent = 6,
        FailedCantDoThatChallengeModeActive = 7,
        FailedRestArea = 8,
        UnspentTalentPoints = 9,
        InPvpMatch = 10
    }

    public enum TutorialsFlag
    {
        None = 0x00,
        Changed = 0x01,
        LoadedFromDB = 0x02
    }

    [Flags]
    public enum ItemSearchLocation
    {
        Equipment = 0x01,
        Inventory = 0x02,
        Bank = 0x04,
        ReagentBank = 0x08,
        AccountBank = 0x10, // NYI

        Default = Equipment | Inventory,
        Everywhere = Equipment | Inventory | Bank | ReagentBank
    }

    public enum ZonePVPTypeOverride
    {
        None = 0,
        Friendly = 1,
        Hostile = 2,
        Contested = 3,
        Combat = 4
    }

    public enum PlayerCreateMode
    {
        Normal = 0,
        NPE = 1,

        Max
    }

    public enum DuelState
    {
        Challenged,
        Countdown,
        InProgress,
        Completed
    }

    public enum DisplayToastType : byte
    {
        NewItem = 0,
        NewCurrency = 1,
        Money = 2,
        Honor = 3
    }

    public enum DisplayToastMethod : byte
    {
        DoNotDisplay = 0,
        Loot = 1,
        PetBattle = 2,
        PersonalLoot = 3,
        GarrisonMissionLoot = 4,
        QuestUpgrade = 5,
        QuestUpgradeEpic = 6,
        Shipment = 7,
        GarrisonMissionSalvage = 8,
        PvPFactionReward = 9,
        GarrisonCurrency = 10,
        LessAwesomeLoot = 11,
        UpgradedLoot = 12,
        LegendaryLoot = 13,
        InvasionLoot = 14,
        Default = 15,
        QuestComplete = 16,
        RatedPvPReward = 17,
        CorruptedLoot = 19
    }

    public enum AvgItemLevelCategory
    {
        Base = 0,
        EquippedBase = 1,
        EquippedEffective = 2,
        Pvp = 3,
        PvpWeighted = 4,
        EquippedEffectiveWeighted = 5
    }

    [Flags]
    public enum CurrencyDbFlags
    {
        None = 0x00,
        IgnoreMaxQtyOnload = 0x01,
        Reuse1 = 0x02,
        InBackpack = 0x04,
        UnusedInUI = 0x08,
        Reuse2 = 0x10,

        UnusedFlags = (IgnoreMaxQtyOnload | Reuse1 | Reuse2),
        ClientFlags = (0x1F & ~UnusedFlags)
    }

    public enum CurrencyDestroyReason
    {
        Cheat = 0,
        Spell = 1,
        VersionUpdate = 2,
        QuestTurnin = 3,
        Vendor = 4,
        Trade = 5,
        Capped = 6,
        Garrison = 7,
        DroppedToCorpse = 8,
        BonusRoll = 9,
        FactionConversion = 10,
        FulfillCraftingOrder = 11,
        Last = 12
    }

    public enum CurrencyGainSource
    {
        ConvertOldItem = 0,
        ConvertOldPvPCurrency = 1,
        ItemRefund = 2,
        QuestReward = 3,
        Cheat = 4,
        Vendor = 5,
        PvPKillCredit = 6,
        PvPMetaCredit = 7,
        PvPScriptedAward = 8,
        Loot = 9,
        UpdatingVersion = 10,
        LFGReward = 11,
        Trade = 12,
        Spell = 13,
        ItemDeletion = 14,
        RatedBattleground = 15,
        RandomBattleground = 16,
        Arena = 17,
        ExceededMaxQty = 18,
        PvPCompletionBonus = 19,
        Script = 20,
        GuildBankWithdrawal = 21,
        Pushloot = 22,
        GarrisonBuilding = 23,
        PvPDrop = 24,
        GarrisonFollowerActivation = 25,
        GarrisonBuildingRefund = 26,
        GarrisonMissionReward = 27,
        GarrisonResourceOverTime = 28,
        QuestRewardIgnoreCaps = 29,
        GarrisonTalent = 30,
        GarrisonWorldQuestBonus = 31,
        PvPHonorReward = 32,
        BonusRoll = 33,
        AzeriteRespec = 34,
        WorldQuestReward = 35,
        WorldQuestRewardIgnoreCaps = 36,
        FactionConversion = 37,
        DailyQuestReward = 38,
        DailyQuestWarModeReward = 39,
        WeeklyQuestReward = 40,
        WeeklyQuestWarModeReward = 41,
        AccountCopy = 42,
        WeeklyRewardChest = 43,
        GarrisonTalentTreeReset = 44,
        DailyReset = 45,
        AddConduitToCollection = 46,
        Barbershop = 47,
        ConvertItemsToCurrencyValue = 48,
        PvPTeamContribution = 49,
        Transmogrify = 50,
        AuctionDeposit = 51,
        PlayerTrait = 52,
        PhBuffer_53 = 53,
        PhBuffer_54 = 54,
        RenownRepGain = 55,
        CraftingOrder = 56,
        CatalystBalancing = 57,
        CatalystCraft = 58,
        ProfessionInitialAward = 59,
        PlayerTraitRefund = 60,
        Last = 61
    }

    [Flags]
    public enum CurrencyGainFlags
    {
        None = 0x00,
        BonusAward = 0x01,
        DroppedFromDeath = 0x02,
        FromAccountServer = 0x04
    }

    public enum TabardVendorType
    {
        Guild = 0,
        Personal = 1,
    }

    public enum PlayerDataFlag
    {
        ExploredZonesIndex = 1,
        CharacterDataIndex = 2,
        AccountDataIndex = 3,
        CharacterTaxiNodesIndex = 4,
        AccountTaxiNodesIndex = 5,
        AccountCombinedQuestsIndex = 6,
        AccountCombinedQuestRewardsIndex = 7,
        CharacterContentpushIndex = 8,
        CharacterQuestCompletedIndex = 9,
    }
}
