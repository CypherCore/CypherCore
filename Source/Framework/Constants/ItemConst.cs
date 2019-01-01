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
    public struct ItemConst
    {
        public const int MaxDamages = 2;                           // changed in 3.1.0
        public const int MaxGemSockets = 3;
        public const int MaxSpells = 5;
        public const int MaxStats = 10;
        public const int MaxBagSize = 36;
        public const byte NullBag = 0;
        public const byte NullSlot = 255;
        public const int MaxOutfitItems = 24;
        public const int MaxItemExtCostItems = 5;
        public const int MaxItemExtCostCurrencies = 5;
        public const int MaxItemRandomProperties = 5;
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
            SocketColor.Prismatic,
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
            SocketColor.RelicHoly
        };

        public static ItemModifier[] AppearanceModifierSlotBySpec =
        {
            ItemModifier.TransmogAppearanceSpec1,
            ItemModifier.TransmogAppearanceSpec2,
            ItemModifier.TransmogAppearanceSpec3,
            ItemModifier.TransmogAppearanceSpec4
        };

        public const uint AppearanceModifierMaskSpecSpecific =
            (1 << (int)ItemModifier.TransmogAppearanceSpec1) |
            (1 << (int)ItemModifier.TransmogAppearanceSpec2) |
            (1 << (int)ItemModifier.TransmogAppearanceSpec3) |
            (1 << (int)ItemModifier.TransmogAppearanceSpec4);

        public static ItemModifier[] IllusionModifierSlotBySpec =
            {
            ItemModifier.EnchantIllusionSpec1,
            ItemModifier.EnchantIllusionSpec2,
            ItemModifier.EnchantIllusionSpec3,
            ItemModifier.EnchantIllusionSpec4
        };

        public const uint IllusionModifierMaskSpecSpecific =
            (1 << (int)ItemModifier.EnchantIllusionSpec1) |
            (1 << (int)ItemModifier.EnchantIllusionSpec2) |
            (1 << (int)ItemModifier.EnchantIllusionSpec3) |
            (1 << (int)ItemModifier.EnchantIllusionSpec4);
    }

    public struct InventorySlots
    {
        public const byte BagStart = 19;
        public const byte BagEnd = 23;
        public const byte ItemStart = 23;
        public const byte ItemEnd = 47;

        public const byte BankItemStart = 47;
        public const byte BankItemEnd = 75;
        public const byte BankBagStart = 75;
        public const byte BankBagEnd = 82;

        public const byte BuyBackStart = 82;
        public const byte BuyBackEnd = 94;
        public const byte ReagentStart = 94;
        public const byte ReagentEnd = 192;
        public const byte ChildEquipmentStart = 192;
        public const byte ChildEquipmentEnd = 195;

        public const byte Bag0 = 255;
        public const byte DefaultSize = 16;
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
        Meta = 0x00001,
        Red = 0x00002,
        Yellow = 0x00004,
        Blue = 0x00008,
        Hydraulic = 0x00010, // Not Used
        Cogwheel = 0x00020,
        Prismatic = 0x0000e,
        RelicIron = 0x00040,
        RelicBlood = 0x00080,
        RelicShadow = 0x00100,
        RelicFel = 0x00200,
        RelicArcane = 0x00400,
        RelicFrost = 0x00800,
        RelicFire = 0x01000,
        RelicWater = 0x02000,
        RelicLife = 0x04000,
        RelicWind = 0x08000,
        RelicHoly = 0x10000,

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
        HitTakenMeleeRating = 22,
        HitTakenRangedRating = 23,
        HitTakenSpellRating = 24,
        CritTakenMeleeRating = 25,
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
        CrAmplify = 58,
        CrMultistrike = 59,
        CrReadiness = 60,
        CrSpeed = 61,
        CrLifesteal = 62,
        CrAvoidance = 63,
        CrSturdiness = 64,
        CrUnused7 = 65,
        CrCleave = 66,
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
        ChanceOnHit = 2,
        Soulstone = 4,
        /*
         * ItemSpelltriggerType 5 might have changed on 2.4.3/3.0.3: Such auras
         * will be applied on item pickup and removed on item loss - maybe on the
         * other hand the item is destroyed if the aura is removed ("removed on
         * death" of spell 57348 makes me think so)
         */
        OnObtain = 5,
        LearnSpellId = 6,                   // used in itemtemplate.spell2 with spellid with SPELLGENERICLEARN in spell1
        Max = 7
    }

    public enum BuyBankSlotResult
    {
        FailedTooMany = 0,
        InsufficientFunds = 1,
        NotBanker = 2,
        OK = 3
    }

    public enum EnchantmentSlotMask : ushort
    {
        CanSouldBound = 0x01,
        Unk1 = 0x02,
        Unk2 = 0x04,
        Unk3 = 0x08,
        Collectable = 0x100,
        HideIfNotCollected = 0x200,
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
        ScalingStatDistributionFixedLevel = 9,
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
        ChallengeKeystoneIsCharged = 22,
        ArtifactKnowledgeLevel = 23,
        ArtifactTier = 24,

        Max
    }

    public enum ItemBonusType : byte
    {
        ItemLevel = 1,
        Stat = 2,
        Quality = 3,
        Description = 4,
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
        OverrideRequiredLevel = 18
    }

    enum ItemContext : byte
    {
        None = 0,
        DungeonNormal = 1,
        DungeonHeroic = 2,
        RaidNormal = 3,
        RaidRaidFinder = 4,
        RaidHeroic = 5,
        RaidMythic = 6,
        PvpUnranked1 = 7,
        PvpRanked1 = 8,
        ScenarioNormal = 9,
        ScenarioHeroic = 10,
        QuestReward = 11,
        Store = 12,
        TradeSkill = 13,
        Vendor = 14,
        BlackMarket = 15,
        ChallengeMode1 = 16,
        DungeonLvlUp1 = 17,
        DungeonLvlUp2 = 18,
        DungeonLvlUp3 = 19,
        DungeonLvlUp4 = 20,
        ForceToNone = 21,
        Timewalker = 22,
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
        ChallengeMode2 = 33,
        ChallengeMode3 = 34,
        ChallengeModeJackpot = 35,
        WorldQuest7 = 36,
        WorldQuest8 = 37,
        PvpRanked2 = 38,
        PvpRanked3 = 39,
        PvpRanked4 = 40,
        PvpUnranked2 = 41,
        WorldQuest9 = 42,
        WorldQuest10 = 43,
        PvpRanked5 = 44,
        PvpRanked6 = 45,
        PvpRanked7 = 46,
        PvpUnranked3 = 47,
        PvpUnranked4 = 48,
        PvpUnranked5 = 49,
        PvpUnranked6 = 50,
        PvpUnranked7 = 51,
        PvpRanked8 = 52,
        WorldQuest11 = 53,
        WorldQuest12 = 54,
        WorldQuest13 = 55,
        PvpRankedJackpot = 56,
        TournamentRealm = 57
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

    public enum InventoryType : byte
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
        Max = 29
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
        Max = 19
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
        Max = 10
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
        Max = 11
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

        Max = 12

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
        Max = 12
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
        Max = 12
    }

    public enum ItemSubClassReagent
    {
        Reagent = 0,
        Keystone = 1,
        Max = 2
    }

    public enum ItemSubClassProjectile
    {
        Wand = 0, // Obsolete
        Bolt = 1, // Obsolete
        Arrow = 2,
        Bullet = 3,
        Thrown = 4,  // Obsolete
        Max = 5
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
        Max = 18
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
        Max = 14
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
        Max = 6
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
        Unk11 = 0x00010000, // ?
        Unk12 = 0x00020000, // ?
        Unk13 = 0x00040000, // ?
        Child = 0x00080000,
        Unk15 = 0x00100000, // ?
        NewItem = 0x00200000, // Item glows in inventory
        Unk17 = 0x00400000, // ?
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
        MultiLootQuest = 0x100,
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
        IgnoreForAuras = 0x100000,
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

    public enum ItemFlags3
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
        CanStoreEnchants = 0x1000000
    }

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
        EquipNone3 = 84,
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
        CantDoThatRightNow = 102,// You Can'T Do That Right Now.
        BagFull6 = 103,// That Bag Is Full.
        CantBeScrapped = 104,// You Can'T Scrap That Item
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
        CantFindItem = 1,
        CantSellItem = 2,       // Merchant Doesn'T Like That Item
        CantFindVendor = 3,       // Merchant Doesn'T Like You
        YouDontOwnThatItem = 4,       // You Don'T Own That Item
        Unk = 5,       // Nothing Appears...
        OnlyEmptyBag = 6        // Can Only Do With Empty Bags
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
    }

    public enum CurrencyFlags
    {
        Tradeable = 0x01,
        // ...
        HighPrecision = 0x08,
        // ...
    }

    public enum CurrencyTypes
    {
        JusticePoints = 395,
        ValorPoints = 396,
        ApexisCrystals = 823,
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
}
