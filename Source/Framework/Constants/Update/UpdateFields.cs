/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
    public enum ObjectFields
    {
        Guid = 0x000, // Size: 4, Flags: Public
        Data = 0x004, // Size: 4, Flags: Public
        Type = 0x008, // Size: 1, Flags: Public
        Entry = 0x009, // Size: 1, Flags: Dynamic
        DynamicFlags = 0x00a, // Size: 1, Flags: Dynamic, Urgent
        ScaleX = 0x00b, // Size: 1, Flags: Public
        End = 0x00c,
    }

    public enum ItemFields
    {
        Owner = ObjectFields.End + 0x000, // Size: 4, Flags: Public
        Contained = ObjectFields.End + 0x004, // Size: 4, Flags: Public
        Creator = ObjectFields.End + 0x008, // Size: 4, Flags: Public
        GiftCreator = ObjectFields.End + 0x00c, // Size: 4, Flags: Public
        StackCount = ObjectFields.End + 0x010, // Size: 1, Flags: Owner
        Duration = ObjectFields.End + 0x011, // Size: 1, Flags: Owner
        SpellCharges = ObjectFields.End + 0x012, // Size: 5, Flags: Owner
        Flags = ObjectFields.End + 0x017, // Size: 1, Flags: Public
        Enchantment = ObjectFields.End + 0x018, // Size: 39, Flags: Public
        PropertySeed = ObjectFields.End + 0x03f, // Size: 1, Flags: Public
        RandomPropertiesId = ObjectFields.End + 0x040, // Size: 1, Flags: Public
        Durability = ObjectFields.End + 0x041, // Size: 1, Flags: Owner
        MaxDurability = ObjectFields.End + 0x042, // Size: 1, Flags: Owner
        CreatePlayedTime = ObjectFields.End + 0x043, // Size: 1, Flags: Public
        ModifiersMask = ObjectFields.End + 0x044, // Size: 1, Flags: Owner
        Context = ObjectFields.End + 0x045, // Size: 1, Flags: Public
        ArtifactXp = ObjectFields.End + 0x046, // Size: 2, Flags: OWNER
        AppearanceModId = ObjectFields.End + 0x048, // Size: 1, Flags: OWNER
        End = ObjectFields.End + 0x049
    }

    public enum ItemDynamicFields
    {
        Modifiers = 0x000, //  Flags: Owner
        BonusListIds = 0x001, //  Flags: Owner, 0x100
        ArtifactPowers = 0x002, // Flags: OWNER
        Gems = 0x003, // Flags: OWNER
        RelicTalentData = 0x004, // Flags: OWNER
        End = 0x005
    }

    public enum ContainerFields
    {
        Slot1 = ItemFields.End + 0x000, // Size: 144, Flags: Public
        NumSlots = ItemFields.End + 0x090, // Size: 1, Flags: Public
        End = ItemFields.End + 0x091
    }

    public enum UnitFields
    {
        Charm = ObjectFields.End + 0x000, // Size: 4, Flags: Public
        Summon = ObjectFields.End + 0x004, // Size: 4, Flags: Public
        Critter = ObjectFields.End + 0x008, // Size: 4, Flags: Private
        CharmedBy = ObjectFields.End + 0x00c, // Size: 4, Flags: Public
        SummonedBy = ObjectFields.End + 0x010, // Size: 4, Flags: Public
        CreatedBy = ObjectFields.End + 0x014, // Size: 4, Flags: Public
        DemonCreator = ObjectFields.End + 0x018, // Size: 4, Flags: Public
        Target = ObjectFields.End + 0x01c, // Size: 4, Flags: Public
        BattlePetCompanionGuid = ObjectFields.End + 0x020, // Size: 4, Flags: Public
        BattlePetDbId = ObjectFields.End + 0x024, // Size: 2, Flags: Public
        ChannelData = ObjectFields.End + 0x026, // Size: 2, Flags: PUBLIC, URGENT
        SummonedByHomeRealm = ObjectFields.End + 0x028, // Size: 1, Flags: Public
        Bytes0 = ObjectFields.End + 0x029, // Size: 1, Flags: Public
        DisplayPower = ObjectFields.End + 0x02a, // Size: 1, Flags: Public
        OverrideDisplayPowerId = ObjectFields.End + 0x02b, // Size: 1, Flags: Public
        Health = ObjectFields.End + 0x02c, // Size: 2, Flags: Public
        Power = ObjectFields.End + 0x02e, // Size: 6, Flags: Public, UrgentSelfOnly
        MaxHealth = ObjectFields.End + 0x034, // Size: 2, Flags: Public
        MaxPower = ObjectFields.End + 0x036, // Size: 6, Flags: Public
        PowerRegenFlatModifier = ObjectFields.End + 0x03c, // Size: 6, Flags: Private, Owner, UnitAll
        PowerRegenInterruptedFlatModifier = ObjectFields.End + 0x042, // Size: 6, Flags: Private, Owner, UnitAll
        Level = ObjectFields.End + 0x048, // Size: 1, Flags: Public
        EffectiveLevel = ObjectFields.End + 0x049, // Size: 1, Flags: Public
        ScalingLevelMin = ObjectFields.End + 0x04a, // Size: 1, Flags: Public
        ScalingLevelMax = ObjectFields.End + 0x04b, // Size: 1, Flags: Public
        ScalingLevelDelta = ObjectFields.End + 0x04c, // Size: 1, Flags: Public
        FactionTemplate = ObjectFields.End + 0x04d, // Size: 1, Flags: Public
        VirtualItemSlotId = ObjectFields.End + 0x04e, // Size: 6, Flags: Public
        Flags = ObjectFields.End + 0x054, // Size: 1, Flags: Public, Urgent
        Flags2 = ObjectFields.End + 0x055, // Size: 1, Flags: Public, Urgent
        Flags3 = ObjectFields.End + 0x056, // Size: 1, Flags: Public, Urgent
        AuraState = ObjectFields.End + 0x057, // Size: 1, Flags: Public
        BaseAttackTime = ObjectFields.End + 0x058, // Size: 2, Flags: Public
        RangedAttackTime = ObjectFields.End + 0x05a, // Size: 1, Flags: Private
        BoundingRadius = ObjectFields.End + 0x05b, // Size: 1, Flags: Public
        CombatReach = ObjectFields.End + 0x05c, // Size: 1, Flags: Public
        DisplayId = ObjectFields.End + 0x05d, // Size: 1, Flags: Dynamic, Urgent
        NativeDisplayId = ObjectFields.End + 0x05e, // Size: 1, Flags: Public, Urgent
        MountDisplayId = ObjectFields.End + 0x05f, // Size: 1, Flags: Public, Urgent
        MinDamage = ObjectFields.End + 0x060, // Size: 1, Flags: Private, Owner, SpecialInfo
        MaxDamage = ObjectFields.End + 0x061, // Size: 1, Flags: Private, Owner, SpecialInfo
        MinOffHandDamage = ObjectFields.End + 0x062, // Size: 1, Flags: Private, Owner, SpecialInfo
        MaxOffHandDamage = ObjectFields.End + 0x063, // Size: 1, Flags: Private, Owner, SpecialInfo
        Bytes1 = ObjectFields.End + 0x064, // Size: 1, Flags: Public
        PetNumber = ObjectFields.End + 0x065, // Size: 1, Flags: Public
        PetNameTimestamp = ObjectFields.End + 0x066, // Size: 1, Flags: Public
        PetExperience = ObjectFields.End + 0x067, // Size: 1, Flags: Owner
        PetNextLevelExp = ObjectFields.End + 0x068, // Size: 1, Flags: Owner
        ModCastSpeed = ObjectFields.End + 0x069, // Size: 1, Flags: Public
        ModCastHaste = ObjectFields.End + 0x06a, // Size: 1, Flags: Public
        ModHaste = ObjectFields.End + 0x06b, // Size: 1, Flags: Public
        ModRangedHaste = ObjectFields.End + 0x06c, // Size: 1, Flags: Public
        ModHasteRegen = ObjectFields.End + 0x06d, // Size: 1, Flags: Public
        ModTimeRate = ObjectFields.End + 0x06e, // Size: 1, Flags: Public
        CreatedBySpell = ObjectFields.End + 0x06f, // Size: 1, Flags: Public
        NpcFlags = ObjectFields.End + 0x070, // Size: 2, Flags: Public, Dynamic
        NpcEmotestate = ObjectFields.End + 0x072, // Size: 1, Flags: Public
        Stat = ObjectFields.End + 0x073, // Size: 4, Flags: Private, Owner
        PosStat = ObjectFields.End + 0x077, // Size: 4, Flags: Private, Owner
        NegStat = ObjectFields.End + 0x07b, // Size: 4, Flags: Private, Owner
        Resistances = ObjectFields.End + 0x07f, // Size: 7, Flags: Private, Owner, SpecialInfo
        ResistanceBuffModsPositive = ObjectFields.End + 0x086, // Size: 7, Flags: Private, Owner
        ResistanceBuffModsNegative = ObjectFields.End + 0x08d, // Size: 7, Flags: Private, Owner
        ModBonusArmor = ObjectFields.End + 0x094, // Size: 1, Flags: Private, Owner
        BaseMana = ObjectFields.End + 0x095, // Size: 1, Flags: Public
        BaseHealth = ObjectFields.End + 0x096, // Size: 1, Flags: Private, Owner
        Bytes2 = ObjectFields.End + 0x097, // Size: 1, Flags: Public
        AttackPower = ObjectFields.End + 0x098, // Size: 1, Flags: Private, Owner
        AttackPowerModPos = ObjectFields.End + 0x099, // Size: 1, Flags: Private, Owner
        AttackPowerModNeg = ObjectFields.End + 0x09a, // Size: 1, Flags: Private, Owner
        AttackPowerMultiplier = ObjectFields.End + 0x09b, // Size: 1, Flags: Private, Owner
        RangedAttackPower = ObjectFields.End + 0x09c, // Size: 1, Flags: Private, Owner
        RangedAttackPowerModPos = ObjectFields.End + 0x09d, // Size: 1, Flags: Private, Owner
        RangedAttackPowerModNeg = ObjectFields.End + 0x09e, // Size: 1, Flags: Private, Owner
        RangedAttackPowerMultiplier = ObjectFields.End + 0x09f, // Size: 1, Flags: Private, Owner
        AttackSpeedAura = ObjectFields.End + 0x0a0, // Size: 1, Flags: Private, Owner
        MinRangedDamage = ObjectFields.End + 0x0a1, // Size: 1, Flags: Private, Owner
        MaxRangedDamage = ObjectFields.End + 0x0a2, // Size: 1, Flags: Private, Owner
        PowerCostModifier = ObjectFields.End + 0x0a3, // Size: 7, Flags: Private, Owner
        PowerCostMultiplier = ObjectFields.End + 0x0aa, // Size: 7, Flags: Private, Owner
        Maxhealthmodifier = ObjectFields.End + 0x0b1, // Size: 1, Flags: Private, Owner
        HoverHeight = ObjectFields.End + 0x0b2, // Size: 1, Flags: Public
        MinItemLevelCutoff = ObjectFields.End + 0x0b3, // Size: 1, Flags: Public
        MinItemLevel = ObjectFields.End + 0x0b4, // Size: 1, Flags: Public
        Maxitemlevel = ObjectFields.End + 0x0b5, // Size: 1, Flags: Public
        WildBattlepetLevel = ObjectFields.End + 0x0b6, // Size: 1, Flags: Public
        BattlepetCompanionNameTimestamp = ObjectFields.End + 0x0b7, // Size: 1, Flags: Public
        InteractSpellid = ObjectFields.End + 0x0b8, // Size: 1, Flags: Public
        StateSpellVisualId = ObjectFields.End + 0x0b9, // Size: 1, Flags: Dynamic, Urgent
        StateAnimId = ObjectFields.End + 0x0ba, // Size: 1, Flags: Dynamic, Urgent
        StateAnimKitId = ObjectFields.End + 0x0bb, // Size: 1, Flags: Dynamic, Urgent
        StateWorldEffectId = ObjectFields.End + 0x0bc, // Size: 4, Flags: Dynamic, Urgent
        ScaleDuration = ObjectFields.End + 0x0c0, // Size: 1, Flags: Public
        LooksLikeMountId = ObjectFields.End + 0x0c1, // Size: 1, Flags: Public
        LooksLikeCreatureId = ObjectFields.End + 0x0c2, // Size: 1, Flags: Public
        LookAtControllerId = ObjectFields.End + 0x0c3, // Size: 1, Flags: Public
        LookAtControllerTarget = ObjectFields.End + 0x0c4, // Size: 4, Flags: Public
        End = ObjectFields.End + 0x0c8
    }

    public enum UnitDynamicFields
    {
        PassiveSpells = 0x000, //  Flags: Public, Urgent
        WorldEffects = 0x001, //  Flags: Public, Urgent
        ChannelObjects = 0x002, // Flags: PUBLIC, URGENT
        End = 0x003
    }

    public enum PlayerFields
    {
        DuelArbiter = UnitFields.End + 0x000, // Size: 4, Flags: Public
        WowAccount = UnitFields.End + 0x004, // Size: 4, Flags: Public
        LootTargetGuid = UnitFields.End + 0x008, // Size: 4, Flags: Public
        Flags = UnitFields.End + 0x00c, // Size: 1, Flags: Public
        FlagsEx = UnitFields.End + 0x00d, // Size: 1, Flags: Public
        GuildRank = UnitFields.End + 0x00e, // Size: 1, Flags: Public
        GuildDeleteDate = UnitFields.End + 0x00f, // Size: 1, Flags: Public
        GuildLevel = UnitFields.End + 0x010, // Size: 1, Flags: Public
        Bytes = UnitFields.End + 0x011, // Size: 1, Flags: Public
        Bytes2 = UnitFields.End + 0x012, // Size: 1, Flags: Public
        Bytes3 = UnitFields.End + 0x013, // Size: 1, Flags: Public
        Bytes4 = UnitFields.End + 0x014, // Size: 1, Flags: Public
        DuelTeam = UnitFields.End + 0x015, // Size: 1, Flags: Public
        GuildTimestamp = UnitFields.End + 0x016, // Size: 1, Flags: Public
        QuestLog = UnitFields.End + 0x017, // Size: 800, Flags: PartyMember
        VisibleItem = UnitFields.End + 0x337, // Size: 38, Flags: Public
        ChosenTitle = UnitFields.End + 0x35d, // Size: 1, Flags: Public
        FakeInebriation = UnitFields.End + 0x35e, // Size: 1, Flags: Public
        VirtualRealm = UnitFields.End + 0x35f, // Size: 1, Flags: Public
        CurrentSpecId = UnitFields.End + 0x360, // Size: 1, Flags: Public
        TaxiMountAnimKitId = UnitFields.End + 0x361, // Size: 1, Flags: Public
        AvgItemLevel = UnitFields.End + 0x362, // Size: 4, Flags: Public
        CurrentBattlePetBreedQuality = UnitFields.End + 0x366, // Size: 1, Flags: Public
        Prestige = UnitFields.End + 0x367, // Size: 1, Flags: Public
        HonorLevel = UnitFields.End + 0x368, // Size: 1, Flags: Public
        InvSlotHead = UnitFields.End + 0x369, // Size: 748, Flags: Private
        EndNotSelf = UnitFields.End + 0x369,

        Farsight = UnitFields.End + 0x655, // Size: 4, Flags: Private
        SummonedBattlePetId = UnitFields.End + 0x659, // Size: 4, Flags: Private
        KnownTitles = UnitFields.End + 0x65d, // Size: 12, Flags: Private
        Coinage = UnitFields.End + 0x669, // Size: 2, Flags: Private
        Xp = UnitFields.End + 0x66b, // Size: 1, Flags: Private
        NextLevelXp = UnitFields.End + 0x66c, // Size: 1, Flags: Private
        SkillLineId = UnitFields.End + 0x66d, // Size: 448, Flags: Private
        SkillLineStep = UnitFields.End + 0x6AD,
        SkillLineRank = UnitFields.End + 0x6ED,
        SkillLineSubStartRank = UnitFields.End + 0x72D,
        SkillLineMaxRank = UnitFields.End + 0x76D,
        SkillLineTempBonus = UnitFields.End + 0x7AD,
        SkillLinePermBonus = UnitFields.End + 0x7ED,
        CharacterPoints = UnitFields.End + 0x82d, // Size: 1, Flags: Private
        MaxTalentTiers = UnitFields.End + 0x82e, // Size: 1, Flags: Private
        TrackCreatures = UnitFields.End + 0x82f, // Size: 1, Flags: Private
        TrackResources = UnitFields.End + 0x830, // Size: 1, Flags: Private
        Expertise = UnitFields.End + 0x831, // Size: 1, Flags: Private
        OffhandExpertise = UnitFields.End + 0x832, // Size: 1, Flags: Private
        RangedExpertise = UnitFields.End + 0x833, // Size: 1, Flags: Private
        CombatRatingExpertise = UnitFields.End + 0x834, // Size: 1, Flags: Private
        BlockPercentage = UnitFields.End + 0x835, // Size: 1, Flags: Private
        DodgePercentage = UnitFields.End + 0x836, // Size: 1, Flags: Private
        DodgePercentageFromAttribute = UnitFields.End + 0x837, // Size: 1, Flags: Private
        ParryPercentage = UnitFields.End + 0x838, // Size: 1, Flags: Private
        ParryPercentageFromAttribute = UnitFields.End + 0x839, // Size: 1, Flags: Private
        CritPercentage = UnitFields.End + 0x83a, // Size: 1, Flags: Private
        RangedCritPercentage = UnitFields.End + 0x83b, // Size: 1, Flags: Private
        OffhandCritPercentage = UnitFields.End + 0x83c, // Size: 1, Flags: Private
        SpellCritPercentage1 = UnitFields.End + 0x83d, // Size: 1, Flags: Private
        ShieldBlock = UnitFields.End + 0x83e, // Size: 1, Flags: Private
        ShieldBlockCritPercentage = UnitFields.End + 0x83f, // Size: 1, Flags: Private
        Mastery = UnitFields.End + 0x840, // Size: 1, Flags: Private
        Speed = UnitFields.End + 0x841, // Size: 1, Flags: Private
        Lifesteal = UnitFields.End + 0x842, // Size: 1, Flags: Private
        Avoidance = UnitFields.End + 0x843, // Size: 1, Flags: Private
        Sturdiness = UnitFields.End + 0x844, // Size: 1, Flags: Private
        Versatility = UnitFields.End + 0x845, // Size: 1, Flags: Private
        VersatilityBonus = UnitFields.End + 0x846, // Size: 1, Flags: Private
        FieldPvpPowerDamage = UnitFields.End + 0x847, // Size: 1, Flags: Private
        FieldPvpPowerHealing = UnitFields.End + 0x848, // Size: 1, Flags: Private
        ExploredZones1 = UnitFields.End + 0x849, // Size: 320, Flags: PRIVATE
        RestInfo = UnitFields.End + 0x989, // Size: 4, Flags: Private
        ModDamageDonePos = UnitFields.End + 0x98d, // Size: 7, Flags: Private
        ModDamageDoneNeg = UnitFields.End + 0x994, // Size: 7, Flags: Private
        ModDamageDonePct = UnitFields.End + 0x99b, // Size: 7, Flags: Private
        ModHealingDonePos = UnitFields.End + 0x9a2, // Size: 1, Flags: Private
        ModHealingPct = UnitFields.End + 0x9a3, // Size: 1, Flags: Private
        ModHealingDonePct = UnitFields.End + 0x9a4, // Size: 1, Flags: Private
        ModPeriodicHealingDonePercent = UnitFields.End + 0x9a5, // Size: 1, Flags: Private
        WeaponDmgMultipliers = UnitFields.End + 0x9a6, // Size: 3, Flags: Private
        WeaponAtkSpeedMultipliers = UnitFields.End + 0x9a9, // Size: 3, Flags: Private
        ModSpellPowerPct = UnitFields.End + 0x9ac, // Size: 1, Flags: Private
        ModResiliencePercent = UnitFields.End + 0x9ad, // Size: 1, Flags: Private
        OverrideSpellPowerByApPct = UnitFields.End + 0x9ae, // Size: 1, Flags: Private
        OverrideApBySpellPowerPercent = UnitFields.End + 0x9af, // Size: 1, Flags: Private
        ModTargetResistance = UnitFields.End + 0x9b0, // Size: 1, Flags: Private
        ModTargetPhysicalResistance = UnitFields.End + 0x9b1, // Size: 1, Flags: Private
        LocalFlags = UnitFields.End + 0x9b2, // Size: 1, Flags: Private
        FieldBytes = UnitFields.End + 0x9b3, // Size: 1, Flags: Private
        SelfResSpell = UnitFields.End + 0x9b4, // Size: 1, Flags: Private
        PvpMedals = UnitFields.End + 0x9b5, // Size: 1, Flags: Private
        BuyBackPrice1 = UnitFields.End + 0x9b6, // Size: 12, Flags: Private
        BuyBackTimestamp1 = UnitFields.End + 0x9c2, // Size: 12, Flags: Private
        Kills = UnitFields.End + 0x9ce, // Size: 1, Flags: Private
        LifetimeHonorableKills = UnitFields.End + 0x9cf, // Size: 1, Flags: Private
        WatchedFactionIndex = UnitFields.End + 0x9d0, // Size: 1, Flags: Private
        CombatRating1 = UnitFields.End + 0x9d1, // Size: 32, Flags: Private
        ArenaTeamInfo11 = UnitFields.End + 0x9f1, // Size: 42, Flags: Private
        MaxLevel = UnitFields.End + 0xa1b, // Size: 1, Flags: Private
        ScalingLevelDelta = UnitFields.End + 0xa1c, // Size: 1, Flags: Private
        MaxCreatureScalingLevel = UnitFields.End + 0xa1d, // Size: 1, Flags: Private
        NoReagentCost1 = UnitFields.End + 0xa1e, // Size: 4, Flags: Private
        PetSpellPower = UnitFields.End + 0xa22, // Size: 1, Flags: Private
        Researching1 = UnitFields.End + 0xa23, // Size: 10, Flags: Private
        ProfessionSkillLine1 = UnitFields.End + 0xa2d, // Size: 2, Flags: Private
        UiHitModifier = UnitFields.End + 0xa2f, // Size: 1, Flags: Private
        UiSpellHitModifier = UnitFields.End + 0xa30, // Size: 1, Flags: Private
        HomeRealmTimeOffset = UnitFields.End + 0xa31, // Size: 1, Flags: Private
        ModPetHaste = UnitFields.End + 0xa32, // Size: 1, Flags: Private
        FieldBytes2 = UnitFields.End + 0xa33, // Size: 1, Flags: Private
        FieldBytes3 = UnitFields.End + 0xa34, // Size: 1, Flags: Private, UrgentSelfOnly
        LfgBonusFactionId = UnitFields.End + 0xa35, // Size: 1, Flags: Private
        LootSpecId = UnitFields.End + 0xa36, // Size: 1, Flags: Private
        OverrideZonePvpType = UnitFields.End + 0xa37, // Size: 1, Flags: Private, UrgentSelfOnly
        BagSlotFlags = UnitFields.End + 0xa38, // Size: 4, Flags: Private
        BankBagSlotFlags = UnitFields.End + 0xa3c, // Size: 7, Flags: Private
        InsertItemsLeftToRight = UnitFields.End + 0xa43, // Size: 1, Flags: Private
        QuestCompleted = UnitFields.End + 0xa44, // Size: 1750, Flags: Private
        Honor = UnitFields.End + 0x111a, // Size: 1, Flags: Private
        HonorNextLevel = UnitFields.End + 0x111b, // Size: 1, Flags: Private
        End = UnitFields.End + 0x111C
    }

    public enum PlayerDynamicFields
    {
        ReserachSite = UnitDynamicFields.End + 0x000, //  Flags: Private
        ResearchSiteProgress = UnitDynamicFields.End + 0x001, //  Flags: Private
        DailyQuests = UnitDynamicFields.End + 0x002, //  Flags: Private
        AvailableQuestLineXQuestId = UnitDynamicFields.End + 0x003, //  Flags: Private
        Heirlooms = UnitDynamicFields.End + 0x004, //  Flags: Private
        HeirloomsFlags = UnitDynamicFields.End + 0x005, // Flags: PRIVATE
        Toys = UnitDynamicFields.End + 0x006, //  Flags: Private
        Transmog = UnitDynamicFields.End + 0x007, // Flags: PRIVATE
        ConditionalTransmog = UnitDynamicFields.End + 0x008, // Flags: PRIVATE
        CharacterRestrictions = UnitDynamicFields.End + 0x009, // Flags: PRIVATE
        SpellPctModByLabel = UnitDynamicFields.End + 0x00A, // Flags: PRIVATE
        SpellFlatModByLabel = UnitDynamicFields.End + 0x00B, // Flags: PRIVATE
        ArenaCooldowns = UnitDynamicFields.End + 0x00C, // Flags: PUBLIC
        End = UnitDynamicFields.End + 0x00D
    }

    public enum GameObjectFields
    {
        CreatedBy = ObjectFields.End + 0x000, // Size: 4, Flags: Public
        DisplayId = ObjectFields.End + 0x004, // Size: 1, Flags: Dynamic, Urgent
        Flags = ObjectFields.End + 0x005, // Size: 1, Flags: Public, Urgent
        ParentRotation = ObjectFields.End + 0x006, // Size: 4, Flags: Public
        Faction = ObjectFields.End + 0x00a, // Size: 1, Flags: Public
        Level = ObjectFields.End + 0x00b, // Size: 1, Flags: Public
        Bytes1 = ObjectFields.End + 0x00c, // Size: 1, Flags: Public, Urgent
        SpellVisualId = ObjectFields.End + 0x00d, // Size: 1, Flags: Public, Dynamic, Urgent
        StateSpellVisualId = ObjectFields.End + 0x00e, // Size: 1, Flags: Dynamic, Urgent
        StateAnumId = ObjectFields.End + 0x00f, // Size: 1, Flags: Dynamic, Urgent
        StateAnimKitId = ObjectFields.End + 0x010, // Size: 1, Flags: Dynamic, Urgent
        StateWorldEffectId = ObjectFields.End + 0x011, // Size: 4, Flags: Dynamic, Urgent
        End = ObjectFields.End + 0x015
    }

    public enum GameObjectDynamicFields
    {
        EnableDoodadSets = 0x000, // Flags: PUBLIC
        End = 0x001
    }

    public enum DynamicObjectFields
    {
        Caster = ObjectFields.End + 0x000, // Size: 4, Flags: Public
        Type = ObjectFields.End + 0x004, // Size: 1, Flags: Public
        SpellXSpellVisualId = ObjectFields.End + 0x005, // Size: 1, Flags: Public
        SpellId = ObjectFields.End + 0x006, // Size: 1, Flags: Public
        Radius = ObjectFields.End + 0x007, // Size: 1, Flags: Public
        CastTime = ObjectFields.End + 0x008, // Size: 1, Flags: Public
        End = ObjectFields.End + 0x009
    }

    public enum CorpseFields
    {
        Owner = ObjectFields.End + 0x000, // Size: 4, Flags: Public
        Party = ObjectFields.End + 0x004, // Size: 4, Flags: Public
        DisplayId = ObjectFields.End + 0x008, // Size: 1, Flags: Public
        Item = ObjectFields.End + 0x009, // Size: 19, Flags: Public
        Bytes1 = ObjectFields.End + 0x01c, // Size: 1, Flags: Public
        Bytes2 = ObjectFields.End + 0x01d, // Size: 1, Flags: Public
        Flags = ObjectFields.End + 0x01e, // Size: 1, Flags: Public
        DynamicFlags = ObjectFields.End + 0x01f, // Size: 1, Flags: Dynamic
        FactionTemplate = ObjectFields.End + 0x020, // Size: 1, Flags: Public
        CustomDisplayOption = ObjectFields.End + 0x021, // Size: 1, Flags: PUBLIC
        End = ObjectFields.End + 0x022
    }

    public enum AreaTriggerFields
    {
        OverrideScaleCurve = ObjectFields.End + 0x000, // Size: 7, Flags: Public, Urgent
        ExtraScaleCurve = ObjectFields.End + 0x007, // Size: 7, Flags: Public, Urgent
        Caster = ObjectFields.End + 0x00e, // Size: 4, Flags: Public
        Duration = ObjectFields.End + 0x012, // Size: 1, Flags: Public
        TimeToTarget = ObjectFields.End + 0x013, // Size: 1, Flags: Public, Urgent
        TimeToTargetScale = ObjectFields.End + 0x014, // Size: 1, Flags: Public, Urgent
        TimeToTargetExtraScale = ObjectFields.End + 0x015, // Size: 1, Flags: Public, Urgent
        SpellId = ObjectFields.End + 0x016, // Size: 1, Flags: Public
        SpellForVisuals = ObjectFields.End + 0x017, // Size: 1, Flags: PUBLIC
        SpellXSpellVisualId = ObjectFields.End + 0x018, // Size: 1, Flags: Dynamic
        BoundsRadius2d = ObjectFields.End + 0x019, // Size: 1, Flags: Dynamic, Urgent
        DecalPropertiesId = ObjectFields.End + 0x01A, // Size: 1, Flags: Public
        CreatingEffectGuid = ObjectFields.End + 0x01B, // Size: 4, Flags: PUBLIC
        End = ObjectFields.End + 0x01F
    }

    public enum SceneObjectFields
    {
        ScriptPackageId = ObjectFields.End + 0x000, // Size: 1, Flags: Public
        RndSeedVal = ObjectFields.End + 0x001, // Size: 1, Flags: Public
        Createdby = ObjectFields.End + 0x002, // Size: 4, Flags: Public
        SceneType = ObjectFields.End + 0x006, // Size: 1, Flags: Public
        End = ObjectFields.End + 0x007
    }

    public enum ConversationFields
    {
        LastLineEndTime = ObjectFields.End + 0x000, // Size: 1, Flags: DYNAMIC
        End = ObjectFields.End + 0x001
    }

    public enum ConversationDynamicFields
    {
        Actors = 0x000, //  Flags: Public
        Lines = 0x001, //  Flags: 0x100
        End = 0x002
    }
}
