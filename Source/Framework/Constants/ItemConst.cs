// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Framework.Constants
{
    public struct ItemConst
    {
        public const int MaxDamages = 2;                           // changed in 3.1.0
        public const int MaxGemSockets = 3;
        public const int MaxSpells = 5;
        public const int MaxStats = 10;
        public const int MaxBagSize = 98;
        public const byte NullBag = 0;
        public const byte NullSlot = 255;
        public const int MaxOutfitItems = 24;
        public const int MaxItemExtCostItems = 5;
        public const int MaxItemExtCostCurrencies = 5;
        public const int MaxItemEnchantmentEffects = 3;
        public const int MaxProtoSpells = 5;
        public const int MaxEquipmentSetIndex = 20;

        public const int MaxItemSubclassTotal = 21;

        public const int MaxItemSetItems = 17;
        public const int MaxItemSetSpells = 8;

        public static uint[] ItemQualityColors =
        {
            0xff9d9d9d, // GREY
            0xffffffff, // WHITE
            0xff1eff00, // GREEN
            0xff0070dd, // BLUE
            0xffa335ee, // PURPLE
            0xffff8000, // ORANGE
            0xffe6cc80, // LIGHT YELLOW
            0xffe6cc80  // LIGHT YELLOW
        };

        public static SocketColor[] SocketColorToGemTypeMask =
        {
            0,
            SocketColor.Meta,
            SocketColor.Red,
            SocketColor.Yellow,
            SocketColor.Blue,
            SocketColor.Hydraulic,
            SocketColor.Cogwheel,
            SocketColor.Red | SocketColor.Yellow | SocketColor.Blue,
            SocketColor.RelicIron,
            SocketColor.RelicBlood,
            SocketColor.RelicShadow,
            SocketColor.RelicFel,
            SocketColor.RelicArcane,
            SocketColor.RelicFrost,
            SocketColor.RelicFire,
            SocketColor.RelicWater,
            SocketColor.RelicLife,
            SocketColor.RelicWind,
            SocketColor.RelicHoly,
            SocketColor.PunchcardRed,
            SocketColor.PunchcardYellow,
            SocketColor.PunchcardBlue,
            SocketColor.DominationBlood | SocketColor.DominationFrost | SocketColor.DominationUnholy,
            SocketColor.Cypher,
            SocketColor.Tinker,
            SocketColor.Primordial,
            SocketColor.Fragrance,
            SocketColor.SingingThunder,
            SocketColor.SingingSea,
            SocketColor.SingingWind
        };

        public static ItemModifier[] AppearanceModifierSlotBySpec =
        {
            ItemModifier.TransmogAppearanceSpec1,
            ItemModifier.TransmogAppearanceSpec2,
            ItemModifier.TransmogAppearanceSpec3,
            ItemModifier.TransmogAppearanceSpec4,
            ItemModifier.TransmogAppearanceSpec5
        };

        public static ItemModifier[] IllusionModifierSlotBySpec =
        {
            ItemModifier.EnchantIllusionSpec1,
            ItemModifier.EnchantIllusionSpec2,
            ItemModifier.EnchantIllusionSpec3,
            ItemModifier.EnchantIllusionSpec4,
            ItemModifier.EnchantIllusionSpec5
        };

        public static ItemModifier[] SecondaryAppearanceModifierSlotBySpec =
        {
            ItemModifier.TransmogSecondaryAppearanceSpec1,
            ItemModifier.TransmogSecondaryAppearanceSpec2,
            ItemModifier.TransmogSecondaryAppearanceSpec3,
            ItemModifier.TransmogSecondaryAppearanceSpec4,
            ItemModifier.TransmogSecondaryAppearanceSpec5
        };

        public static InventoryType[] InventoryTypesEquipable =
        {
            InventoryType.Weapon,
            InventoryType.Shield,
            InventoryType.Ranged,
            InventoryType.Weapon2Hand,
            InventoryType.WeaponMainhand,
            InventoryType.WeaponOffhand,
            InventoryType.Holdable,
            InventoryType.Thrown,
            InventoryType.RangedRight,
            InventoryType.ProfessionTool
        };
    }

    public struct ProfessionSlots
    {
        public const byte Profession1Tool = 19;
        public const byte Profession1Gear1 = 20;
        public const byte Profession1Gear2 = 21;
        public const byte Profession2Tool = 22;
        public const byte Profession2Gear1 = 23;
        public const byte Profession2Gear2 = 24;
        public const byte CookingTool = 25;
        public const byte CookingGear1 = 26;
        public const byte FishingTool = 27;
        public const byte FishingGear1 = 28;
        public const byte FishingGear2 = 29;

        public const byte End = 30;
        public const byte Start = Profession1Tool;
        public const byte MaxCount = Profession2Tool - Profession1Tool;
    }

    public struct InventorySlots
    {
        public const byte BagStart = 30;
        public const byte BagEnd = 34;

        public const byte ReagentBagStart = 34;
        public const byte ReagentBagEnd = 35;

        public const byte ItemStart = 35;
        public const byte ItemEnd = 63;

        public const byte BankItemStart = 63;
        public const byte BankItemEnd = 91;

        public const byte BankBagStart = 91;
        public const byte BankBagEnd = 98;

        public const byte BuyBackStart = 98;
        public const byte BuyBackEnd = 110;

        public const byte ReagentStart = 110;
        public const byte ReagentEnd = 208;

        public const byte ChildEquipmentStart = 208;
        public const byte ChildEquipmentEnd = 211;

        public const byte Bag0 = 255;
        public const byte DefaultSize = 16;
    }

    enum EquipableSpellSlots
    {
        OffensiveSlot1 = 211,
        OffensiveSlot2 = 212,
        OffensiveSlot3 = 213,
        OffensiveSlot4 = 214,
        UtilitySlot1 = 215,
        UtilitySlot2 = 216,
        UtilitySlot3 = 217,
        UtilitySlot4 = 218,
        DefensiveSlot1 = 219,
        DefensiveSlot2 = 220,
        DefensiveSlot3 = 221,
        DefensiveSlot4 = 222,
        WeaponSlot1 = 223,
        WeaponSlot2 = 224,
        WeaponSlot3 = 225,
        WeaponSlot4 = 226,
    }

    public struct EquipmentSlot
    {
        public const byte Start = 0;
        public const byte Head = 0;
        public const byte Neck = 1;
        public const byte Shoulders = 2;
        public const byte Shirt = 3;
        public const byte Chest = 4;
        public const byte Waist = 5;
        public const byte Legs = 6;
        public const byte Feet = 7;
        public const byte Wrist = 8;
        public const byte Hands = 9;
        public const byte Finger1 = 10;
        public const byte Finger2 = 11;
        public const byte Trinket1 = 12;
        public const byte Trinket2 = 13;
        public const byte Cloak = 14;
        public const byte MainHand = 15;
        public const byte OffHand = 16;
        public const byte Ranged = 17;
        public const byte Tabard = 18;
        public const byte End = 19;
    }

    public enum SocketColor
    {
        Meta = 0X01,
        Red = 0X02,
        Yellow = 0X04,
        Blue = 0X08,
        Hydraulic = 0X10,
        Cogwheel = 0X20,
        RelicIron = 0X40,
        RelicBlood = 0X80,
        RelicShadow = 0X100,
        RelicFel = 0X200,
        RelicArcane = 0X400,
        RelicFrost = 0X800,
        RelicFire = 0X1000,
        RelicWater = 0X2000,
        RelicLife = 0X4000,
        RelicWind = 0X8000,
        RelicHoly = 0X10000,
        PunchcardRed = 0X20000,
        PunchcardYellow = 0X40000,
        PunchcardBlue = 0X80000,
        DominationBlood = 0X100000,
        DominationFrost = 0X200000,
        DominationUnholy = 0X400000,
        Cypher = 0X800000,
        Tinker = 0x01000000,
        Primordial = 0x02000000,
        Fragrance = 0x04000000,
        SingingThunder = 0x08000000,
        SingingSea = 0x10000000,
        SingingWind = 0x20000000,

        Standard = (Red | Yellow | Blue)
    }

    public enum ItemExtendedCostFlags
    {
        RequireGuild = 0x01,
        RequireSeasonEarned1 = 0x02,
        RequireSeasonEarned2 = 0x04,
        RequireSeasonEarned3 = 0x08,
        RequireSeasonEarned4 = 0x10,
        RequireSeasonEarned5 = 0x20,
    }

    public enum ItemModType
    {
        Mana = 0,
        Health = 1,
        Agility = 3,
        Strength = 4,
        Intellect = 5,
        Spirit = 6,
        Stamina = 7,
        DefenseSkillRating = 12,
        DodgeRating = 13,
        ParryRating = 14,
        BlockRating = 15,
        HitMeleeRating = 16,
        HitRangedRating = 17,
        HitSpellRating = 18,
        CritMeleeRating = 19,
        CritRangedRating = 20,
        CritSpellRating = 21,
        Corruption = 22,
        CorruptionResistance = 23,
        ModifiedCraftingStat1 = 24,
        ModifiedCraftingStat2 = 25,
        CritTakenRangedRating = 26,
        CritTakenSpellRating = 27,
        HasteMeleeRating = 28,
        HasteRangedRating = 29,
        HasteSpellRating = 30,
        HitRating = 31,
        CritRating = 32,
        HitTakenRating = 33,
        CritTakenRating = 34,
        ResilienceRating = 35,
        HasteRating = 36,
        ExpertiseRating = 37,
        AttackPower = 38,
        RangedAttackPower = 39,
        Versatility = 40,
        SpellHealingDone = 41,
        SpellDamageDone = 42,
        ManaRegeneration = 43,
        ArmorPenetrationRating = 44,
        SpellPower = 45,
        HealthRegen = 46,
        SpellPenetration = 47,
        BlockValue = 48,
        MasteryRating = 49,
        ExtraArmor = 50,
        FireResistance = 51,
        FrostResistance = 52,
        HolyResistance = 53,
        ShadowResistance = 54,
        NatureResistance = 55,
        ArcaneResistance = 56,
        PvpPower = 57,
        Unused0 = 58,
        Unused1 = 59,
        Unused3 = 60,
        CrSpeed = 61,
        CrLifesteal = 62,
        CrAvoidance = 63,
        CrSturdiness = 64,
        CrUnused7 = 65,
        Unused27 = 66,
        CrUnused9 = 67,
        CrUnused10 = 68,
        CrUnused11 = 69,
        CrUnused12 = 70,
        AgiStrInt = 71,
        AgiStr = 72,
        AgiInt = 73,
        StrInt = 74
    }

    public enum ItemSpelltriggerType : sbyte
    {
        OnUse = 0,                  // use after equip cooldown
        OnEquip = 1,
        OnProc = 2,
        SummonedBySpell = 3,
        OnDeath = 4,
        OnPickup = 5,
        OnLearn = 6,                   // used in itemtemplate.spell2 with spellid with SPELLGENERICLEARN in spell1
        OnLooted = 7,
        TeachMount = 8,
        OnPickupForced = 9,
        OnLootedForced = 10,
        Max
    }

    public enum BuyBankSlotResult
    {
        FailedTooMany = 0,
        InsufficientFunds = 1,
        NotBanker = 2,
        OK = 3
    }

    [Flags]
    public enum SpellItemEnchantmentFlags : ushort
    {
        Soulbound = 0x01,
        DoNotLog = 0x02,
        MainhandOnly = 0x04,
        AllowEnteringArena = 0x08,
        DoNotSaveToDB = 0x10,
        ScaleAsAGem = 0x20,
        DisableInChallengeModes = 0x40,
        DisableInProvingGrounds = 0x80,
        AllowTransmog = 0x100,
        HideUntilCollected = 0x200,
    }

    public enum ItemModifier
    {
        TransmogAppearanceAllSpecs = 0,
        TransmogAppearanceSpec1 = 1,
        UpgradeId = 2,
        BattlePetSpeciesId = 3,
        BattlePetBreedData = 4, // (Breedid) | (Breedquality << 24)
        BattlePetLevel = 5,
        BattlePetDisplayId = 6,
        EnchantIllusionAllSpecs = 7,
        ArtifactAppearanceId = 8,
        TimewalkerLevel = 9,
        EnchantIllusionSpec1 = 10,
        TransmogAppearanceSpec2 = 11,
        EnchantIllusionSpec2 = 12,
        TransmogAppearanceSpec3 = 13,
        EnchantIllusionSpec3 = 14,
        TransmogAppearanceSpec4 = 15,
        EnchantIllusionSpec4 = 16,
        ChallengeMapChallengeModeId = 17,
        ChallengeKeystoneLevel = 18,
        ChallengeKeystoneAffixId1 = 19,
        ChallengeKeystoneAffixId2 = 20,
        ChallengeKeystoneAffixId3 = 21,
        ChallengeKeystoneAffixId4 = 22,
        ArtifactKnowledgeLevel = 23,
        ArtifactTier = 24,
        TransmogAppearanceSpec5 = 25,
        PvpRating = 26,
        EnchantIllusionSpec5 = 27,
        ContentTuningId = 28,
        ChangeModifiedCraftingStat1 = 29,
        ChangeModifiedCraftingStat2 = 30,
        TransmogSecondaryAppearanceAllSpecs = 31,
        TransmogSecondaryAppearanceSpec1 = 32,
        TransmogSecondaryAppearanceSpec2 = 33,
        TransmogSecondaryAppearanceSpec3 = 34,
        TransmogSecondaryAppearanceSpec4 = 35,
        TransmogSecondaryAppearanceSpec5 = 36,
        SoulbindConduitRank = 37,
        CraftingQualityId = 38,
        CraftingSkillLineAbilityId = 39,
        CraftingDataId = 40,
        CraftingSkillReagents = 41,
        CraftingSkillWatermark = 42,
        CraftingReagentSlot0 = 43,
        CraftingReagentSlot1 = 44,
        CraftingReagentSlot2 = 45,
        CraftingReagentSlot3 = 46,
        CraftingReagentSlot4 = 47,
        CraftingReagentSlot5 = 48,
        CraftingReagentSlot6 = 49,
        CraftingReagentSlot7 = 50,
        CraftingReagentSlot8 = 51,
        CraftingReagentSlot9 = 52,
        CraftingReagentSlot10 = 53,
        CraftingReagentSlot11 = 54,
        CraftingReagentSlot12 = 55,
        CraftingReagentSlot13 = 56,
        CraftingReagentSlot14 = 57,
        Reforge = 58,

        Max
    }

    public enum ItemBonusType : byte
    {
        ItemLevel = 1,
        Stat = 2,
        Quality = 3,
        NameSubtitle = 4, // Text under name
        Suffix = 5,
        Socket = 6,
        Appearance = 7,
        RequiredLevel = 8,
        DisplayToastMethod = 9,
        RepairCostMuliplier = 10,
        ScalingStatDistribution = 11,
        DisenchantLootId = 12,
        ScalingStatDistributionFixed = 13,
        ItemLevelCanIncrease = 14, // Displays a + next to item level indicating it can warforge
        RandomEnchantment = 15, // Responsible for showing "<Random additional stats>" or "+%d Rank Random Minor Trait" in the tooltip before item is obtained
        Bounding = 16,
        RelicType = 17,
        OverrideRequiredLevel = 18,
        AzeriteTierUnlockSet = 19,
        ScrappingLootId = 20,
        OverrideCanDisenchant = 21,
        OverrideCanScrap = 22,
        ItemEffectId = 23,
        ModifiedCraftingStat = 25,
        RequiredLevelCurve = 27,
        DescriptionText = 30,             // Item Description
        OverrideName = 31,             // Itemnamedescription Id
        ItemBonusListGroup = 34,
        ItemLimitCategory = 35,
        ItemConversion = 37,
        ItemHistorySlot = 38,
    }

    public enum ItemCollectionType : byte
    {
        None = 0,
        Toy = 1,
        Heirloom = 2,
        Transmog = 3,
        TransmogSetFavorite = 4,
        RuneforgeLegendaryAbility = 5,
        TransmogIllusion = 6,
        WarbandScene = 7,
    }

    public enum ItemContext : byte
    {
        None = 0,
        DungeonNormal = 1,
        DungeonHeroic = 2,
        RaidNormal = 3,
        RaidRaidFinder = 4,
        RaidHeroic = 5,
        RaidMythic = 6,
        PVPUnranked1 = 7,
        PVPRanked1Unrated = 8,
        ScenarioNormal = 9,
        ScenarioHeroic = 10,
        QuestReward = 11,
        InGameStore = 12,
        TradeSkill = 13,
        Vendor = 14,
        BlackMarket = 15,
        MythicPlusEndofRun = 16,
        DungeonLvlUp1 = 17,
        DungeonLvlUp2 = 18,
        DungeonLvlUp3 = 19,
        DungeonLvlUp4 = 20,
        ForceToNone = 21,
        Timewalking = 22,
        DungeonMythic = 23,
        PvpHonorReward = 24,
        WorldQuest1 = 25,
        WorldQuest2 = 26,
        WorldQuest3 = 27,
        WorldQuest4 = 28,
        WorldQuest5 = 29,
        WorldQuest6 = 30,
        MissionReward1 = 31,
        MissionReward2 = 32,
        MythicPlusEndofRunTimeChest = 33,
        MythicPlusTimewalkingEndofRun = 34,
        MythicPlusJackpot = 35,
        WorldQuest7 = 36,
        WorldQuest8 = 37,
        PVPRanked2Combatant = 38,
        PVPRanked4Challenger = 39,
        PVPRanked6Rival = 40,
        PVPUnranked2 = 41,
        WorldQuest9 = 42,
        WorldQuest10 = 43,
        PVPRanked8Duelist = 44,
        PVPRanked9Elite = 45,
        PVPRanked3Combatant = 46,
        PVPUnranked3 = 47,
        PVPUnranked4 = 48,
        PVPUnranked5 = 49,
        PVPUnranked6 = 50,
        PVPUnranked7 = 51,
        PVPRanked5Challenger = 52,
        WorldQuest11 = 53,
        WorldQuest12 = 54,
        WorldQuest13 = 55,
        PVPRankedJackpot = 56,
        TournamentRealm1 = 57,
        Relinquished = 58,
        LegendaryForge = 59,
        QuestBonusLoot = 60,
        CharacterBoostDragonflight70 = 61,
        CharacterBoostShadowlands50 = 62,
        LegendaryCrafting1 = 63,
        LegendaryCrafting2 = 64,
        LegendaryCrafting3 = 65,
        LegendaryCrafting4 = 66,
        LegendaryCrafting5 = 67,
        LegendaryCrafting6 = 68,
        LegendaryCrafting7 = 69,
        LegendaryCrafting8 = 70,
        LegendaryCrafting9 = 71,
        WeeklyRewardsAdditional = 72,
        WeeklyRewardsConcession = 73,
        WorldQuestJackpot = 74,
        NewCharacter = 75,
        WarMode = 76,
        PvPBrawl1 = 77,
        PvPBrawl2 = 78,
        Torghast = 79,
        CorpseRecovery = 80,
        WorldBoss = 81,
        RaidNormalExtended = 82,
        RaidRaidFinderExtended = 83,
        RaidHeroicExtended = 84,
        RaidMythicExtended = 85,
        CharacterBoostShadowlands60 = 86,
        MythicPlusTimewalkingEndofRunTimeChest = 87,
        PvpRanked7Rival = 88,
        RaidNormalExtended2 = 89,
        RaidFinderExtended2 = 90,
        RaidHeroicExtended2 = 91,
        RaidMythicExtended2 = 92,
        RaidNormalExtended3 = 93,
        RaidFinderExtended3 = 94,
        RaidHeroicExtended3 = 95,
        RaidMythicExtended3 = 96,
        TemplateCharacter1 = 97,
        TemplateCharacter2 = 98,
        TemplateCharacter3 = 99,
        TemplateCharacter4 = 100,
        DungeonNormalJackpot = 101,
        DungeonHeroicJackpot = 102,
        DungeonMythicJackpot = 103,
        Delves1 = 104,
        Timerunning = 105,
        Delves2 = 106,
        Delves3 = 107,
        DelvesJackpot = 108,
        DelvesKey1 = 109,
        DelvesKey2 = 110,
        DelvesKey3 = 111,
        DelvesKey4 = 112,
        DelvesKey5 = 113,
        DelvesKey6 = 114,
        DelvesKey7 = 115,
        DelvesKey8 = 116,
        DelvesBounty1 = 117,
        DelvesBounty2 = 118,
        DelvesBounty3 = 119,
        DelvesBounty4 = 120,
        DelvesBounty5 = 121,
        DelvesBounty6 = 122,
        DelvesBounty7 = 123,
        DelvesBounty8 = 124,
        DelvesLevelUp1 = 125,
        DelvesLevelUp2 = 126,
        DelvesLevelUp3 = 127,
        DelvesLevelUp4 = 128,
        DelvesBonus1 = 129,
        DelvesBonus2 = 130,
        DelvesBonus3 = 131,
        DelvesBonus4 = 132,
        DelvesBonus5 = 133,
        DelvesBonus6 = 134,
        DelvesBonus7 = 135,
        DelvesBonus8 = 136,
        DelvesBonus9 = 137,
        DelvesBonus10 = 138,
        DungeonBonus1 = 139,
        DungeonBonus2 = 140,
        DungeonBonus3 = 141,
        DungeonBonus4 = 142,
        DungeonBonus5 = 143,
        DungeonBonus6 = 144,
        DungeonBonus7 = 145,
        DungeonBonus8 = 146,
        DungeonBonus9 = 147,
        DungeonBonus10 = 148,
        RaidBonus1 = 149,
        RaidBonus2 = 150,
        RaidBonus3 = 151,
        RaidBonus4 = 152,
        RaidBonus5 = 153,
        RaidBonus6 = 154,
        RaidBonus7 = 155,
        RaidBonus8 = 156,
        RaidBonus9 = 157,
        RaidBonus10 = 158,
        DungeonHardMode1 = 159,
        DungeonHardMode2 = 160,
        DungeonHardMode3 = 161,
        TournamentRealm2 = 162,
        TournamentRealm3 = 163,
        TournamentRealm4 = 164,

        Max
    }

    public enum ItemEnchantmentType : byte
    {
        None = 0,
        CombatSpell = 1,
        Damage = 2,
        EquipSpell = 3,
        Resistance = 4,
        Stat = 5,
        Totem = 6,
        UseSpell = 7,
        PrismaticSocket = 8,
        ArtifactPowerBonusRankByType = 9,
        ArtifactPowerBonusRankByID = 10,
        BonusListID = 11,
        BonusListCurve = 12,
        ArtifactPowerBonusRankPicker = 13
    }

    public enum BagFamilyMask
    {
        None = 0x00,
        Arrows = 0x01,
        Bullets = 0x02,
        SoulShards = 0x04,
        LeatherworkingSupp = 0x08,
        InscriptionSupp = 0x10,
        Herbs = 0x20,
        EnchantingSupp = 0x40,
        EngineeringSupp = 0x80,
        Keys = 0x100,
        Gems = 0x200,
        MiningSupp = 0x400,
        SoulboundEquipment = 0x800,
        VanityPets = 0x1000,
        CurrencyTokens = 0x2000,
        QuestItems = 0x4000,
        FishingSupp = 0x8000,
        CookingSupp = 0x10000
    }

    public enum InventoryType : sbyte
    {
        NonEquip = 0,
        Head = 1,
        Neck = 2,
        Shoulders = 3,
        Body = 4,
        Chest = 5,
        Waist = 6,
        Legs = 7,
        Feet = 8,
        Wrists = 9,
        Hands = 10,
        Finger = 11,
        Trinket = 12,
        Weapon = 13,
        Shield = 14,
        Ranged = 15,
        Cloak = 16,
        Weapon2Hand = 17,
        Bag = 18,
        Tabard = 19,
        Robe = 20,
        WeaponMainhand = 21,
        WeaponOffhand = 22,
        Holdable = 23,
        Ammo = 24,
        Thrown = 25,
        RangedRight = 26,
        Quiver = 27,
        Relic = 28,
        ProfessionTool = 29,
        ProfessionGear = 30,
        EquipableSpellOffensive = 31,
        EquipableSpellUtility = 32,
        EquipableSpellDefensive = 33,
        EquipableSpellMobility = 34,
        Max
    }

    public enum VisibleEquipmentSlot
    {
        Head = 0,
        Shoulder = 2,
        Shirt = 3,
        Chest = 4,
        Belt = 5,
        Pants = 6,
        Boots = 7,
        Wrist = 8,
        Gloves = 9,
        Back = 14,
        Tabard = 18
    }

    public enum ItemBondingType
    {
        None = 0,
        OnAcquire = 1,
        OnEquip = 2,
        OnUse = 3,
        Quest = 4,
        Unused1 = 5,
        Unused2 = 6,
        WowAccount = 7,
        BnetAccount = 8,
        BnetAccountUntilEquipped = 9,
    }

    public enum ItemClass : sbyte
    {
        None = -1,
        Consumable = 0,
        Container = 1,
        Weapon = 2,
        Gem = 3,
        Armor = 4,
        Reagent = 5,
        Projectile = 6,
        TradeGoods = 7,
        ItemEnhancement = 8,
        Recipe = 9,
        Money = 10, // Obsolete
        Quiver = 11,
        Quest = 12,
        Key = 13,
        Permanent = 14, // Obsolete
        Miscellaneous = 15,
        Glyph = 16,
        BattlePets = 17,
        WowToken = 18,
        Profession = 19,
        Max
    }

    public enum ItemSubClassConsumable
    {
        Consumable = 0,
        Potion = 1,
        Elixir = 2,
        Flask = 3,
        Scroll = 4,
        FoodDrink = 5,
        ItemEnhancement = 6,
        Bandage = 7,
        ConsumableOther = 8,
        VantusRune = 9,
        UtilityCurio = 10,
        CombatCurio = 11,

        Max
    }

    public enum ItemSubClassContainer
    {
        Container = 0,
        SoulContainer = 1,
        HerbContainer = 2,
        EnchantingContainer = 3,
        EngineeringContainer = 4,
        GemContainer = 5,
        MiningContainer = 6,
        LeatherworkingContainer = 7,
        InscriptionContainer = 8,
        TackleContainer = 9,
        CookingContainer = 10,
        ReagentContainer = 11,
        Max
    }

    public enum ItemSubClassWeapon
    {
        Axe = 0,  // One-Handed Axes
        Axe2 = 1,  // Two-Handed Axes
        Bow = 2,
        Gun = 3,
        Mace = 4,  // One-Handed Maces
        Mace2 = 5,  // Two-Handed Maces
        Polearm = 6,
        Sword = 7,  // One-Handed Swords
        Sword2 = 8,  // Two-Handed Swords
        Warglaives = 9,
        Staff = 10,
        Exotic = 11, // One-Handed Exotics
        Exotic2 = 12, // Two-Handed Exotics
        Fist = 13,
        Miscellaneous = 14,
        Dagger = 15,
        Thrown = 16,
        Spear = 17,
        Crossbow = 18,
        Wand = 19,
        FishingPole = 20,

        MaskRanged = (1 << Bow) | (1 << Gun) | (1 << Crossbow),

        Max = 21

    }

    public enum ItemSubClassGem
    {
        Intellect = 0,
        Agility = 1,
        Strength = 2,
        Stamina = 3,
        Spirit = 4,
        CriticalStrike = 5,
        Mastery = 6,
        Haste = 7,
        Versatility = 8,
        Other = 9,
        MultipleStats = 10,
        ArtifactRelic = 11,
        Max
    }

    public enum ItemSubClassArmor
    {
        Miscellaneous = 0,
        Cloth = 1,
        Leather = 2,
        Mail = 3,
        Plate = 4,
        Cosmetic = 5,
        Shield = 6,
        Libram = 7,
        Idol = 8,
        Totem = 9,
        Sigil = 10,
        Relic = 11,
        Max
    }

    public enum ItemSubClassReagent
    {
        Reagent = 0,
        Keystone = 1,
        ContextToken = 2,
        Max
    }

    public enum ItemSubClassProjectile
    {
        Wand = 0, // Obsolete
        Bolt = 1, // Obsolete
        Arrow = 2,
        Bullet = 3,
        Thrown = 4,  // Obsolete
        Max
    }

    public enum ItemSubClassTradeGoods
    {
        TradeGoods = 0,
        Parts = 1,
        Explosives = 2,
        Devices = 3,
        Jewelcrafting = 4,
        Cloth = 5,
        Leather = 6,
        MetalStone = 7,
        Meat = 8,
        Herb = 9,
        Elemental = 10,
        TradeGoodsOther = 11,
        Enchanting = 12,
        Material = 13,
        Enchantment = 14,
        WeaponEnchantment = 15,
        Inscription = 16,
        ExplosivesDevices = 17,
        OptionalReagent = 18,
        FinishingReagent = 19,
        Max
    }

    public enum ItemSubclassItemEnhancement
    {
        Head = 0,
        Neck = 1,
        Shoulder = 2,
        Cloak = 3,
        Chest = 4,
        Wrist = 5,
        Hands = 6,
        Waist = 7,
        Legs = 8,
        Feet = 9,
        Finger = 10,
        Weapon = 11,
        TwoHandedWeapon = 12,
        ShieldOffHand = 13,
        Misc = 14,
        Max
    }

    public enum ItemSubClassRecipe
    {
        Book = 0,
        LeatherworkingPattern = 1,
        TailoringPattern = 2,
        EngineeringSchematic = 3,
        Blacksmithing = 4,
        CookingRecipe = 5,
        AlchemyRecipe = 6,
        FirstAidManual = 7,
        EnchantingFormula = 8,
        FishingManual = 9,
        JewelcraftingRecipe = 10,
        InscriptionTechnique = 11,
        Max = 12
    }

    public enum ItemSubClassMoney
    {
        Money = 0,  // Obsolete
        Max = 1
    }

    public enum ItemSubClassQuiver
    {
        Quiver0 = 0, // Obsolete
        Quiver1 = 1, // Obsolete
        Quiver = 2,
        AmmoPouch = 3,
        Max = 4,
    }

    public enum ItemSubClassQuest
    {
        Quest = 0,
        Unk3 = 3, // 1 Item (33604)
        Unk8 = 8, // 2 Items (37445, 49700)
        Max = 9
    }

    public enum ItemSubClassKey
    {
        Key = 0,
        Lockpick = 1,
        Max = 2
    }

    public enum ItemSubClassPermanent
    {
        Permanent = 0,
        Max = 1
    }

    public enum ItemSubClassJunk
    {
        Junk = 0,
        Reagent = 1,
        CompanionPet = 2,
        Holiday = 3,
        Other = 4,
        Mount = 5,
        MountEquipment = 6,
        Max
    }

    public enum ItemSubClassGlyph
    {
        Warrior = 1,
        Paladin = 2,
        Hunter = 3,
        Rogue = 4,
        Priest = 5,
        DeathKnight = 6,
        Shaman = 7,
        Mage = 8,
        Warlock = 9,
        Monk = 10,
        Druid = 11,
        DemonHunter = 12,
        Max = 13
    }

    public enum ItemSubclassBattlePet
    {
        BattlePet = 0,
        Max = 1
    }

    public enum ItemSubclassWowToken
    {
        WowToken = 0,
        Max = 1
    }

    public enum ItemSubclassProfession
    {
        Blacksmithing = 0,
        Leatherworking = 1,
        Alchemy = 2,
        Herbalism = 3,
        Cooking = 4,
        Mining = 5,
        Tailoring = 6,
        Engineering = 7,
        Enchanting = 8,
        Fishing = 9,
        Skinning = 10,
        Jewelcrafting = 11,
        Inscription = 12,
        Archaeology = 13,
        Max
    }

    public enum ItemQuality
    {
        Poor = 0,                 //Grey
        Normal = 1,                 //White
        Uncommon = 2,                 //Green
        Rare = 3,                 //Blue
        Epic = 4,                 //Purple
        Legendary = 5,                 //Orange
        Artifact = 6,                 //Light Yellow
        Heirloom = 7,
        Max = 8
    }

    public enum ItemFieldFlags : uint
    {
        Soulbound = 0x01, // Item Is Soulbound And Cannot Be Traded <<--
        Translated = 0x02, // Item text will not read as garbage when player does not know the language
        Unlocked = 0x04, // Item Had Lock But Can Be Opened Now
        Wrapped = 0x08, // Item Is Wrapped And Contains Another Item
        Unk2 = 0x10, // ?
        Unk3 = 0x20, // ?
        Unk4 = 0x40, // ?
        Unk5 = 0x80, // ?
        BopTradeable = 0x100, // Allows Trading Soulbound Items
        Readable = 0x200, // Opens Text Page When Right Clicked
        Unk6 = 0x400, // ?
        Unk7 = 0x800, // ?
        Refundable = 0x1000, // Item Can Be Returned To Vendor For Its Original Cost (Extended Cost)
        Unk8 = 0x2000, // ?
        Unk9 = 0x4000, // ?
        Unk10 = 0x8000, // ?
        Unk11 = 0X0010000, // ?
        Unk12 = 0X0020000, // ?
        Unk13 = 0X0040000, // ?
        Child = 0X0080000,
        Unk15 = 0x00100000, // ?
        NewItem = 0x00200000, // Item glows in inventory
        AzeriteEmpoweredItemViewed = 0x00400000, // Won't play azerite powers animation when viewing it
        Unk18 = 0x00800000, // ?
        Unk19 = 0x01000000, // ?
        Unk20 = 0x02000000, // ?
        Unk21 = 0x04000000, // ?
        Unk22 = 0x08000000, // ?
        Unk23 = 0x10000000, // ?
        Unk24 = 0x20000000, // ?
        Unk25 = 0x40000000, // ?
        Unk26 = 0x80000000 // ?
    }

    public enum ItemZoneFlags
    {
        Equipped = 0x1
    }

    [Flags]
    public enum ItemFlags : long
    {
        NoPickup = 0x01,
        Conjured = 0x02, // Conjured Item
        HasLoot = 0x04, // Item Can Be Right Clicked To Open For Loot
        HeroicTooltip = 0x08, // Makes Green "Heroic" Text Appear On Item
        Deprecated = 0x10, // Cannot Equip Or Use
        NoUserDestroy = 0x20, // Item Can Not Be Destroyed, Except By Using Spell (Item Can Be Reagent For Spell)
        Playercast = 0x40, // Item's spells are castable by players
        NoEquipCooldown = 0x80, // No Default 30 Seconds Cooldown When Equipped
        Legacy = 0x100, // Effects are disabled
        IsWrapper = 0x200, // Item Can Wrap Other Items
        UsesResources = 0x400,
        MultiDrop = 0x800, // Looting This Item Does Not Remove It From Available Loot
        ItemPurchaseRecord = 0x1000, // Item Can Be Returned To Vendor For Its Original Cost (Extended Cost)
        Petition = 0x2000, // Item Is Guild Or Arena Charter
        HasText = 0x4000, // Only readable items have this (but not all)
        NoDisenchant = 0x8000,
        RealDuration = 0x10000,
        NoCreator = 0x20000,
        IsProspectable = 0x40000, // Item Can Be Prospected
        UniqueEquippable = 0x80000, // You Can Only Equip One Of These
        DisableAutoQuotes = 0x100000, // Disables quotes around item description in tooltip
        IgnoreDefaultArenaRestrictions = 0x200000, // Item Can Be Used During Arena Match
        NoDurabilityLoss = 0x400000, // Some Thrown weapons have it (and only Thrown) but not all
        UseWhenShapeshifted = 0x800000, // Item Can Be Used In Shapeshift Forms
        HasQuestGlow = 0x1000000,
        HideUnusableRecipe = 0x2000000, // Profession Recipes: Can Only Be Looted If You Meet Requirements And Don'T Already Know It
        NotUseableInArena = 0x4000000, // Item Cannot Be Used In Arena
        IsBoundToAccount = 0x8000000, // Item Binds To Account And Can Be Sent Only To Your Own Characters
        NoReagentCost = 0x10000000, // Spell Is Cast Ignoring Reagents
        IsMillable = 0x20000000, // Item Can Be Milled
        ReportToGuildChat = 0x40000000,
        NoProgressiveLoot = 0x80000000
    }

    public enum ItemFlags2 : uint
    {
        FactionHorde = 0x01,
        FactionAlliance = 0x02,
        DontIgnoreBuyPrice = 0x04, // When Item Uses Extended Cost, Gold Is Also Required
        ClassifyAsCaster = 0x08,
        ClassifyAsPhysical = 0x10,
        EveryoneCanRollNeed = 0x20,
        NoTradeBindOnAcquire = 0x40,
        CanTradeBindOnAcquire = 0x80,
        CanOnlyRollGreed = 0x100,
        CasterWeapon = 0x200,
        DeleteOnLogin = 0x400,
        InternalItem = 0x800,
        NoVendorValue = 0x1000,
        ShowBeforeDiscovered = 0x2000,
        OverrideGoldCost = 0x4000,
        IgnoreDefaultRatedBgRestrictions = 0x8000,
        NotUsableInRatedBg = 0x10000,
        BnetAccountTradeOk = 0x20000,
        ConfirmBeforeUse = 0x40000,
        ReevaluateBondingOnTransform = 0x80000,
        NoTransformOnChargeDepletion = 0x100000,
        NoAlterItemVisual = 0x200000,
        NoSourceForItemVisual = 0x400000,
        IgnoreQualityForItemVisualSource = 0x800000,
        NoDurability = 0x1000000,
        RoleTank = 0x2000000,
        RoleHealer = 0x4000000,
        RoleDamage = 0x8000000,
        CanDropInChallengeMode = 0x10000000,
        NeverStackInLootUi = 0x20000000,
        DisenchantToLootTable = 0x40000000,
        UsedInATradeskill = 0x80000000
    }

    public enum ItemFlags3 : uint
    {
        DontDestroyOnQuestAccept = 0x01,
        ItemCanBeUpgraded = 0x02,
        UpgradeFromItemOverridesDropUpgrade = 0x04,
        AlwaysFfaInLoot = 0x08,
        HideUpgradeLevelsIfNotUpgraded = 0x10,
        UpdateInteractions = 0x20,
        UpdateDoesntLeaveProgressiveWinHistory = 0x40,
        IgnoreItemHistoryTracker = 0x80,
        IgnoreItemLevelCapInPvp = 0x100,
        DisplayAsHeirloom = 0x200, // Item Appears As Having Heirloom Quality Ingame Regardless Of Its Real Quality (Does Not Affect Stat Calculation)
        SkipUseCheckOnPickup = 0x400,
        Obsolete = 0x800,
        DontDisplayInGuildNews = 0x1000, // Item Is Not Included In The Guild News Panel
        PvpTournamentGear = 0x2000,
        RequiresStackChangeLog = 0x4000,
        UnusedFlag = 0x8000,
        HideNameSuffix = 0x10000,
        PushLoot = 0x20000,
        DontReportLootLogToParty = 0x40000,
        AlwaysAllowDualWield = 0x80000,
        Obliteratable = 0x100000,
        ActsAsTransmogHiddenVisualOption = 0x200000,
        ExpireOnWeeklyReset = 0x400000,
        DoesntShowUpInTransmogUntilCollected = 0x800000,
        CanStoreEnchants = 0x1000000,
        HideQuestItemFromObjectTooltip = 0x2000000,
        DoNotToast = 0x4000000,
        IgnoreCreationContextForProgressiveWinHistory = 0x8000000,
        ForceAllSpecsForItemHistory = 0x10000000,
        SaveOnConsume = 0x20000000,
        ContainerSavesPlayerData = 0x40000000,
        NoVoidStorage = 0x80000000
    }

    public enum ItemFlags4
    {
        HandleOnUseEffectImmediately = 0x01,
        AlwaysShowItemLevelInTooltip = 0x02,
        ShowsGenerationWithRandomStats = 0x04,
        ActivateOnEquipEffectsWhenTransmogrified = 0x08,
        EnforceTransmogWithChildItem = 0x10,
        Scrapable = 0x20,
        BypassRepRequirementsForTransmog = 0x40,
        DisplayOnlyOnDefinedRaces = 0x80,
        RegulatedCommodity = 0x100,
        CreateLootImmediately = 0x200,
        GenerateLootSpecItem = 0x400,
        HiddenInRewardsSummaries = 0x800,
        DisallowWhileLevelLinked = 0x1000,
        DisallowEnchant = 0x2000,
        SquishUsingItemLevelAsPlayerLevel = 0x4000,
        AlwaysShowPriceInTooltip = 0x8000,
        CosmeticItem = 0x10000,
        NoSpellEffectTooltipPrefixes = 0x20000,
        IgnoreCosmeticCollectionBehavior = 0x40000,
        NpcOnly = 0x80000,
        NotRestorable = 0x100000,
        DontDisplayAsCraftingReagent = 0x200000,
        DisplayReagentQualityAsCraftedQuality = 0x400000,
        NoSalvage = 0x800000,
        Recraftable = 0x1000000,
        CcTrinket = 0x2000000,
        KeepThroughFactionChange = 0x4000000,
        NotMulticraftable = 0x8000000,
        DontReportLootLogToSelf = 0x10000000,
    }

    [Flags]
    public enum ItemFlagsCustom
    {
        Unused = 0x0001,
        IgnoreQuestStatus = 0x0002,   // No quest status will be checked when this item drops
        FollowLootRules = 0x0004    // Item will always follow group/master/need before greed looting rules
    }

    public enum InventoryResult
    {
        Ok = 0,
        CantEquipLevelI = 1,  // You Must Reach Level %D To Use That Item.
        CantEquipSkill = 2,  // You Aren'T Skilled Enough To Use That Item.
        WrongSlot = 3,  // That Item Does Not Go In That Slot.
        BagFull = 4,  // That Bag Is Full.
        BagInBag = 5,  // Can'T Put Non-Empty Bags In Other Bags.
        TradeEquippedBag = 6,  // You Can'T Trade Equipped Bags.
        AmmoOnly = 7,  // Only Ammo Can Go There.
        ProficiencyNeeded = 8,  // You Do Not Have The Required Proficiency For That Item.
        NoSlotAvailable = 9,  // No Equipment Slot Is Available For That Item.
        CantEquipEver = 10, // You Can Never Use That Item.
        CantEquipEver2 = 11, // You Can Never Use That Item.
        NoSlotAvailable2 = 12, // No Equipment Slot Is Available For That Item.
        Equipped2handed = 13, // Cannot Equip That With A Two-Handed Weapon.
        TwoHandSkillNotFound = 14, // You Cannot Dual-Wield
        WrongBagType = 15, // That Item Doesn'T Go In That Container.
        WrongBagType2 = 16, // That Item Doesn'T Go In That Container.
        ItemMaxCount = 17, // You Can'T Carry Any More Of Those Items.
        NoSlotAvailable3 = 18, // No Equipment Slot Is Available For That Item.
        CantStack = 19, // This Item Cannot Stack.
        NotEquippable = 20, // This Item Cannot Be Equipped.
        CantSwap = 21, // These Items Can'T Be Swapped.
        SlotEmpty = 22, // That Slot Is Empty.
        ItemNotFound = 23, // The Item Was Not Found.
        DropBoundItem = 24, // You Can'T Drop A Soulbound Item.
        OutOfRange = 25, // Out Of Range.
        TooFewToSplit = 26, // Tried To Split More Than Number In Stack.
        SplitFailed = 27, // Couldn'T Split Those Items.
        SpellFailedReagentsGeneric = 28, // Missing Reagent
        CantTradeGold = 29, // Gold May Only Be Offered By One Trader.
        NotEnoughMoney = 30, // You Don'T Have Enough Money.
        NotABag = 31, // Not A Bag.
        DestroyNonemptyBag = 32, // You Can Only Do That With Empty Bags.
        NotOwner = 33, // You Don'T Own That Item.
        OnlyOneQuiver = 34, // You Can Only Equip One Quiver.
        NoBankSlot = 35, // You Must Purchase That Bag Slot First
        NoBankHere = 36, // You Are Too Far Away From A Bank.
        ItemLocked = 37, // Item Is Locked.
        GenericStunned = 38, // You Are Stunned
        PlayerDead = 39, // You Can'T Do That When You'Re Dead.
        ClientLockedOut = 40, // You Can'T Do That Right Now.
        InternalBagError = 41, // Internal Bag Error
        OnlyOneBolt = 42, // You Can Only Equip One Quiver.
        OnlyOneAmmo = 43, // You Can Only Equip One Ammo Pouch.
        CantWrapStackable = 44, // Stackable Items Can'T Be Wrapped.
        CantWrapEquipped = 45, // Equipped Items Can'T Be Wrapped.
        CantWrapWrapped = 46, // Wrapped Items Can'T Be Wrapped.
        CantWrapBound = 47, // Bound Items Can'T Be Wrapped.
        CantWrapUnique = 48, // Unique Items Can'T Be Wrapped.
        CantWrapBags = 49, // Bags Can'T Be Wrapped.
        LootGone = 50, // Already Looted
        InvFull = 51, // Inventory Is Full.
        BankFull = 52, // Your Bank Is Full
        VendorSoldOut = 53, // That Item Is Currently Sold Out.
        BagFull2 = 54, // That Bag Is Full.
        ItemNotFound2 = 55, // The Item Was Not Found.
        CantStack2 = 56, // This Item Cannot Stack.
        BagFull3 = 57, // That Bag Is Full.
        VendorSoldOut2 = 58, // That Item Is Currently Sold Out.
        ObjectIsBusy = 59, // That Object Is Busy.
        CantBeDisenchanted = 60, // Item Cannot Be Disenchanted
        NotInCombat = 61, // You Can'T Do That While In Combat
        NotWhileDisarmed = 62, // You Can'T Do That While Disarmed
        BagFull4 = 63, // That Bag Is Full.
        CantEquipRank = 64, // You Don'T Have The Required Rank For That Item
        CantEquipReputation = 65, // You Don'T Have The Required Reputation For That Item
        TooManySpecialBags = 66, // You Cannot Equip Another Bag Of That Type
        LootCantLootThatNow = 67, // You Can'T Loot That Item Now.
        ItemUniqueEquippable = 68, // You Cannot Equip More Than One Of Those.
        VendorMissingTurnins = 69, // You Do Not Have The Required Items For That Purchase
        NotEnoughHonorPoints = 70, // You Don'T Have Enough Honor Points
        NotEnoughArenaPoints = 71, // You Don'T Have Enough Arena Points
        ItemMaxCountSocketed = 72, // You Have The Maximum Number Of Those Gems In Your Inventory Or Socketed Into Items.
        MailBoundItem = 73, // You Can'T Mail Soulbound Items.
        InternalBagError2 = 74, // Internal Bag Error
        BagFull5 = 75, // That Bag Is Full.
        ItemMaxCountEquippedSocketed = 76, // You Have The Maximum Number Of Those Gems Socketed Into Equipped Items.
        ItemUniqueEquippableSocketed = 77, // You Cannot Socket More Than One Of Those Gems Into A Single Item.
        TooMuchGold = 78, // At Gold Limit
        NotDuringArenaMatch = 79, // You Can'T Do That While In An Arena Match
        TradeBoundItem = 80, // You Can'T Trade A Soulbound Item.
        CantEquipRating = 81, // You Don'T Have The Personal, Team, Or Battleground Rating Required To Buy That Item
        EventAutoequipBindConfirm = 82,
        NotSameAccount = 83, // Account-Bound Items Can Only Be Given To Your Own Characters.
        None = 84,
        ItemMaxLimitCategoryCountExceededIs = 85, // You Can Only Carry %D %S
        ItemMaxLimitCategorySocketedExceededIs = 86, // You Can Only Equip %D |4item:Items In The %S Category
        ScalingStatItemLevelExceeded = 87, // Your Level Is Too High To Use That Item
        PurchaseLevelTooLow = 88, // You Must Reach Level %D To Purchase That Item.
        CantEquipNeedTalent = 89, // You Do Not Have The Required Talent To Equip That.
        ItemMaxLimitCategoryEquippedExceededIs = 90, // You Can Only Equip %D |4item:Items In The %S Category
        ShapeshiftFormCannotEquip = 91, // Cannot Equip Item In This Form
        ItemInventoryFullSatchel = 92, // Your Inventory Is Full. Your Satchel Has Been Delivered To Your Mailbox.
        ScalingStatItemLevelTooLow = 93, // Your Level Is Too Low To Use That Item
        CantBuyQuantity = 94, // You Can'T Buy The Specified Quantity Of That Item.
        ItemIsBattlePayLocked = 95, // Your Purchased Item Is Still Waiting To Be Unlocked
        ReagentBankFull = 96, // Your Reagent Bank Is Full
        ReagentBankLocked = 97,
        WrongBagType3 = 98, // That Item Doesn'T Go In That Container.
        CantUseItem = 99, // You Can'T Use That Item.
        CantBeObliterated = 100,// You Can'T Obliterate That Item
        GuildBankConjuredItem = 101,// You Cannot Store Conjured Items In The Guild Bank
        BagFull6 = 102,// That bag is full.
        BagFull7 = 103,// That bag is full.
        CantBeScrapped = 104,// You can't scrap that item
        BagFull8 = 105,// That bag is full.
        NotInPetBattle = 106,// You cannot do that while in a pet battle
        BagFull9 = 107,// That bag is full.
        CantDoThatRightNow = 108,// You can't do that right now.
        CantDoThatRightNow2 = 109,// You can't do that right now.
        NotInNPE = 110,// Not available during the tutorial
        ItemCooldown = 111,// Item is not ready yet.
        NotInRatedBattleground = 112,// You can't do that in a rated battleground.
        EquipableSpellsSlotsFull = 113,
        CantBeRecrafted = 114,// You can't recraft that itemv
        ReagentBagWrongSlot = 115,// Reagent Bags can only be placed in the reagent bag slot.
        SlotOnlyReagentBag = 116,// Only Reagent Bags can be placed in the reagent bag slot.
        ReagentBagItemType = 117,// Only Reagents can be placed in Reagent Bags.
        CantBulkSellItemWithRefund = 118,// Items that can be refunded can't be bulk sold.
        NoSoulboundItemInAccountBank = 119,// Soulbound items cannot be stored in the Warband Bank.
        CantDoThatRightNow3 = 120,// You Can't Do That Right Now.
        NoAccountInventoryLock = 121,// Your Character Cannot Complete This Action.
        TooManyAccountBankTabs = 122,// You Have Already Purchased The Maximum Amount Of Tabs.
        NoAccountBankHere = 123,// You Are Too Far Away From A Warband Bank.
        NoRefundableItemInAccountBank = 124,// Refundable Items Cannot Be Stored In The Warband Bank.
        NoImmediateContainerInAccountBank = 125,// You Cannot Place That In The Warband Bank.
        NoOpenImmediateContainerInAccountBank = 126,// You Cannot Open This While It Is In The Warband Bank.
        CantDeleteInAccountBank = 127,// Items Must Be Moved Out Of The Warband Bank To Be Deleted.
        BankNotAccessible = 128,// This Character Does Not Have Access To This Bank.
        CantTradeAccountItem = 129,// You Can't Trade An Item From The Warband Bank.
        AccountMoneyLocked = 130,// You cannot withdraw or deposit gold from the warband bank currently; please try again later.
    }

    public enum BankType
    {
        Character = 0,
        Guild = 1,
        Account = 2
    }

    public enum BuyResult
    {
        CantFindItem = 0,
        ItemAlreadySold = 1,
        NotEnoughtMoney = 2,
        SellerDontLikeYou = 4,
        DistanceTooFar = 5,
        ItemSoldOut = 7,
        CantCarryMore = 8,
        RankRequire = 11,
        ReputationRequire = 12
    }

    public enum SellResult
    {
        CantFindItem = 1, // The item was not found.
        CantSellItem = 2, // The merchant doesn't want that item.
        CantFindVendor = 3, // The merchant doesn't like you.
        YouDontOwnThatItem = 4, // You don't own that item.
        Unk = 5,       // Nothing Appears...
        OnlyEmptyBag = 6, // You can only do that with empty bags.
        CantSellToThisMerchant = 7,        // You cannot sell items to this merchant.
        MustRepairDurability = 8,       // DESCRIPTION You must repair that item's durability to use it.
        VendorRefuseScappableAzerite = 9,       // DESCRIPTION The merchant doesn't want that item. Bring it to the Scrapper to extract Titan Residuum.
        InternalBagError = 10,      // DESCRIPTION Internal Bag Error
    }

    public enum EnchantmentSlot
    {
        Perm = 0,
        Temp = 1,
        Sock1 = 2,
        Sock2 = 3,
        Sock3 = 4,
        Bonus = 5,
        Prismatic = 6,                    // added at apply special permanent enchantment
        Use = 7,

        MaxInspected = 8,

        Prop0 = 8,                   // used with RandomSuffix
        Prop1 = 9,                   // used with RandomSuffix
        Prop2 = 10,                   // used with RandomSuffix and RandomProperty
        Prop3 = 11,                   // used with RandomProperty
        Prop4 = 12,                   // used with RandomProperty
        Max = 13
    }

    public enum ItemUpdateState
    {
        Unchanged = 0,
        Changed = 1,
        New = 2,
        Removed = 3
    }

    public enum ItemVendorType
    {
        None = 0,
        Item = 1,
        Currency = 2,
        Spell = 3,
        MawPower = 4
    }

    public enum CurrencyTypes
    {
        JusticePoints = 395,
        ValorPoints = 396,
        ApexisCrystals = 823,
        Azerite = 1553,
        AncientMana = 1155
    }

    public enum PlayerCurrencyState
    {
        Unchanged = 0,
        Changed = 1,
        New = 2,
        Removed = 3     //not removed just set count == 0
    }

    public enum ItemTransmogrificationWeaponCategory
    {
        // Two-handed
        Melee2H,
        Ranged,

        // One-handed
        AxeMaceSword1H,
        Dagger,
        Fist,

        Invalid
    }

    public enum AzeriteEssenceActivateResult
    {
        None = 0,
        EssenceNotUnlocked = 2,    // Arg: AzeriteEssenceID
        CantDoThatRightNow = 3,
        AffectingCombat = 4,
        CantRemoveEssence = 5,    // Arg: SpellID of active essence on cooldown
        ChallengeModeActive = 6,
        NotInRestArea = 7,
        ConditionFailed = 8,
        SlotLocked = 9,
        NotAtForge = 10,
        HeartLevelTooLow = 11,   // Arg: RequiredLevel
        NotEquipped = 12
    }

    [Flags]
    public enum BagSlotFlags
    {
        None = 0x00,
        DisableAutoSort = 0x01,
        PriorityEquipment = 0x02,
        PriorityConsumables = 0x04,
        PriorityTradeGoods = 0x08,
        PriorityJunk = 0x10,
        PriorityQuestItems = 0x20,
        ExcludeJunkSell = 0x40,
    }

    public enum LootStoreItemType
    {
        Item = 0,
        Reference = 1,
        Currency = 2,
        TrackingQuest = 3
    }
}
