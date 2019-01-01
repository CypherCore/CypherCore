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
    public enum ObjectFields
    {
        Guid = 0x000, // Size: 4, Flags: Public
        Entry = 0x004, // Size: 1, Flags: Dynamic
        DynamicFlags = 0x005, // Size: 1, Flags: Dynamic, Urgent
        ScaleX = 0x006, // Size: 1, Flags: Public
        End = 0x007,
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
        End = 0x004
    }

    public enum ContainerFields
    {
        Slot1 = ItemFields.End + 0x000, // Size: 144, Flags: Public
        NumSlots = ItemFields.End + 0x090, // Size: 1, Flags: Public
        End = ItemFields.End + 0x091
    }

    public enum AzeriteEmpoweredItemFields
    {
        Selections = ItemFields.End + 0x000, // Size: 4, Flags: PUBLIC
        End = ItemFields.End + 0x004,
    }

    public enum AzeriteEmpoweredItemDynamicFields
    {
        End = ItemDynamicFields.End + 0x000,
    }

    public enum AzeriteItemFields
    {
        Xp = ItemFields.End + 0x000, // Size: 2, Flags: PUBLIC
        Level = ItemFields.End + 0x002, // Size: 1, Flags: PUBLIC
        AuraLevel = ItemFields.End + 0x003, // Size: 1, Flags: PUBLIC
        KnowledgeLevel = ItemFields.End + 0x004, // Size: 1, Flags: OWNER
        DebugKnowledgeWeek = ItemFields.End + 0x005, // Size: 1, Flags: OWNER
        End = ItemFields.End + 0x006,
    }

    public enum AzeriteItemDynamicFields
    {
        End = ItemDynamicFields.End + 0x000,
    }

    public enum UnitFields
    {
        Charm = ObjectFields.End + 0x000, // Size: 4, Flags: Public
        Summon = ObjectFields.End + 0x004, // Size: 4, Flags: Public
        Critter = ObjectFields.End + 0x008, // Size: 4, Flags: Privaten
        CharmedBy = ObjectFields.End + 0x00c, // Size: 4, Flags: Public
        SummonedBy = ObjectFields.End + 0x010, // Size: 4, Flags: Public
        CreatedBy = ObjectFields.End + 0x014, // Size: 4, Flags: Public
        DemonCreator = ObjectFields.End + 0x018, // Size: 4, Flags: Public
        LookAtControllerTarget = ObjectFields.End + 0x01c, // Size: 4, Flags: Public
        Target = ObjectFields.End + 0x020, // Size: 4, Flags: Public
        BattlePetCompanionGuid = ObjectFields.End + 0x024, // Size: 4, Flags: Public
        BattlePetDbId = ObjectFields.End + 0x028, // Size: 2, Flags: Public
        ChannelData = ObjectFields.End + 0x02a, // Size: 2, Flags: Public, Urgent
        SummonedByHomeRealm = ObjectFields.End + 0x02c, // Size: 1, Flags: Public
        Bytes0 = ObjectFields.End + 0x02d, // Size: 1, Flags: Public
        DisplayPower = ObjectFields.End + 0x02e, // Size: 1, Flags: Public
        OverrideDisplayPowerId = ObjectFields.End + 0x02f, // Size: 1, Flags: Public
        Health = ObjectFields.End + 0x030, // Size: 2, Flags: Public
        Power = ObjectFields.End + 0x032, // Size: 6, Flags: Public, UrgentSelfOnly
        MaxHealth = ObjectFields.End + 0x038, // Size: 2, Flags: Public
        MaxPower = ObjectFields.End + 0x03a, // Size: 6, Flags: Public
        PowerRegenFlatModifier = ObjectFields.End + 0x040, // Size: 6, Flags: Private, Owner, UnitAll
        PowerRegenInterruptedFlatModifier = ObjectFields.End + 0x046, // Size: 6, Flags: Private, Owner, UnitAll
        Level = ObjectFields.End + 0x04c, // Size: 1, Flags: Public
        EffectiveLevel = ObjectFields.End + 0x04d, // Size: 1, Flags: Public
        ContentTuningId = ObjectFields.End + 0x04e, // Size: 1, Flags: Public
        ScalingLevelMin = ObjectFields.End + 0x04f, // Size: 1, Flags: Public
        ScalingLevelMax = ObjectFields.End + 0x050, // Size: 1, Flags: Public
        ScalingLevelDelta = ObjectFields.End + 0x051, // Size: 1, Flags: Public
        ScalingFactionGroup = ObjectFields.End + 0x052, // Size: 1, Flags: Public
        ScalingHealthItemLevelCurveId = ObjectFields.End + 0x053, // Size: 1, Flags: Public
        ScalingDamageItemLevelCurveId = ObjectFields.End + 0x054, // Size: 1, Flags: Public
        FactionTemplate = ObjectFields.End + 0x055, // Size: 1, Flags: Public
        VirtualItemSlotId = ObjectFields.End + 0x056, // Size: 6, Flags: Public
        Flags = ObjectFields.End + 0x05c, // Size: 1, Flags: Public, Urgent
        Flags2 = ObjectFields.End + 0x05d, // Size: 1, Flags: Public, Urgent
        Flags3 = ObjectFields.End + 0x05e, // Size: 1, Flags: Public, UrgentScalingLevelDelta
        AuraState = ObjectFields.End + 0x05f, // Size: 1, Flags: Public
        BaseAttackTime = ObjectFields.End + 0x060, // Size: 2, Flags: Public
        RangedAttackTime = ObjectFields.End + 0x062, // Size: 1, Flags: Private
        BoundingRadius = ObjectFields.End + 0x063, // Size: 1, Flags: Public
        CombatReach = ObjectFields.End + 0x064, // Size: 1, Flags: Public
        DisplayId = ObjectFields.End + 0x065, // Size: 1, Flags: Dynamic, Urgent
        DisplayScale = ObjectFields.End + 0x066, // Size: 1, Flags: Dynamic, Urgent
        NativeDisplayId = ObjectFields.End + 0x067, // Size: 1, Flags: Public, Urgent
        NativeXDisplayScale = ObjectFields.End + 0x068, // Size: 1, Flags: Public, Urgent
        MountDisplayId = ObjectFields.End + 0x069, // Size: 1, Flags: Public, Urgent
        MinDamage = ObjectFields.End + 0x06a, // Size: 1, Flags: Private, Owner, SpecialInfo
        MaxDamage = ObjectFields.End + 0x06b, // Size: 1, Flags: Private, Owner, SpecialInfo
        MinOffHandDamage = ObjectFields.End + 0x06c, // Size: 1, Flags: Private, Owner, SpecialInfo
        MaxOffHandDamage = ObjectFields.End + 0x06d, // Size: 1, Flags: Private, Owner, SpecialInfo
        Bytes1 = ObjectFields.End + 0x06e, // Size: 1, Flags: Public
        PetNumber = ObjectFields.End + 0x06f, // Size: 1, Flags: Public
        PetNameTimestamp = ObjectFields.End + 0x070, // Size: 1, Flags: Public
        PetExperience = ObjectFields.End + 0x071, // Size: 1, Flags: Owner
        PetNextLevelExp = ObjectFields.End + 0x072, // Size: 1, Flags: Owner
        ModCastSpeed = ObjectFields.End + 0x073, // Size: 1, Flags: Public
        ModCastHaste = ObjectFields.End + 0x074, // Size: 1, Flags: Public
        ModHaste = ObjectFields.End + 0x075, // Size: 1, Flags: Public
        ModRangedHaste = ObjectFields.End + 0x076, // Size: 1, Flags: Public
        ModHasteRegen = ObjectFields.End + 0x077, // Size: 1, Flags: Public
        ModTimeRate = ObjectFields.End + 0x078, // Size: 1, Flags: Public
        CreatedBySpell = ObjectFields.End + 0x079, // Size: 1, Flags: Public
        NpcFlags = ObjectFields.End + 0x07a, // Size: 2, Flags: Public, Dynamic
        NpcEmotestate = ObjectFields.End + 0x07c, // Size: 1, Flags: Public
        Stat = ObjectFields.End + 0x07d, // Size: 4, Flags: Private, Owner
        PosStat = ObjectFields.End + 0x081, // Size: 4, Flags: Private, Owner
        NegStat = ObjectFields.End + 0x085, // Size: 4, Flags: Private, Owner
        Resistances = ObjectFields.End + 0x089, // Size: 7, Flags: Private, Owner, SpecialInfo
        BonusResistanceMods = ObjectFields.End + 0x090, // Size: 7, Flags: Private, Owner
        BaseMana = ObjectFields.End + 0x097, // Size: 1, Flags: Public
        BaseHealth = ObjectFields.End + 0x098, // Size: 1, Flags: Private, Owner
        Bytes2 = ObjectFields.End + 0x099, // Size: 1, Flags: Public
        AttackPower = ObjectFields.End + 0x09a, // Size: 1, Flags: Private, Owner
        AttackPowerModPos = ObjectFields.End + 0x09b, // Size: 1, Flags: Private, Owner
        AttackPowerModNeg = ObjectFields.End + 0x09c, // Size: 1, Flags: Private, Owner
        AttackPowerMultiplier = ObjectFields.End + 0x09d, // Size: 1, Flags: Private, Owner
        RangedAttackPower = ObjectFields.End + 0x09e, // Size: 1, Flags: Private, Owner
        RangedAttackPowerModPos = ObjectFields.End + 0x09f, // Size: 1, Flags: Private, Owner
        RangedAttackPowerModNeg = ObjectFields.End + 0x0a0, // Size: 1, Flags: Private, Owner
        RangedAttackPowerMultiplier = ObjectFields.End + 0x0a1, // Size: 1, Flags: Private, Owner
        MainHandWeaponAttackPower = ObjectFields.End + 0x0a2, // Size: 1, Flags: Private, Owner
        OffHandWeaponAttackPower = ObjectFields.End + 0x0a3, // Size: 1, Flags: Private, Owner
        RangedHandWeaponAttackPower = ObjectFields.End + 0x0a4, // Size: 1, Flags: Private, Owner
        AttackSpeedAura = ObjectFields.End + 0x0a5, // Size: 1, Flags: Private, Owner
        Lifesteal = ObjectFields.End + 0x0a6, // Size: 1, Flags: Private, Owner
        MinRangedDamage = ObjectFields.End + 0x0a7, // Size: 1, Flags: Private, Owner
        MaxRangedDamage = ObjectFields.End + 0x0a8, // Size: 1, Flags: Private, Owner
        PowerCostModifier = ObjectFields.End + 0x0a9, // Size: 7, Flags: Private, Owner
        PowerCostMultiplier = ObjectFields.End + 0x0b0, // Size: 7, Flags: Private, Owner
        MaxHealthModifier = ObjectFields.End + 0x0b7, // Size: 1, Flags: Private, Owner
        HoverHeight = ObjectFields.End + 0x0b8, // Size: 1, Flags: Public
        MinItemLevelCutoff = ObjectFields.End + 0x0b9, // Size: 1, Flags: Public
        MinItemLevel = ObjectFields.End + 0x0ba, // Size: 1, Flags: Public
        MaxItemlevel = ObjectFields.End + 0x0bb, // Size: 1, Flags: Public
        WildBattlepetLevel = ObjectFields.End + 0x0bc, // Size: 1, Flags: Public
        BattlepetCompanionNameTimestamp = ObjectFields.End + 0x0bd, // Size: 1, Flags: Public
        InteractSpellid = ObjectFields.End + 0x0be, // Size: 1, Flags: Public
        StateSpellVisualId = ObjectFields.End + 0x0bf, // Size: 1, Flags: Dynamic, Urgent
        StateAnimId = ObjectFields.End + 0x0c0, // Size: 1, Flags: Dynamic, Urgent
        StateAnimKitId = ObjectFields.End + 0x0c1, // Size: 1, Flags: Dynamic, Urgent
        StateWorldEffectId = ObjectFields.End + 0x0c2, // Size: 4, Flags: Dynamic, Urgent
        ScaleDuration = ObjectFields.End + 0x0c6, // Size: 1, Flags: Public
        LooksLikeMountId = ObjectFields.End + 0x0c7, // Size: 1, Flags: Public
        LooksLikeCreatureId = ObjectFields.End + 0x0c8, // Size: 1, Flags: Public
        LookAtControllerId = ObjectFields.End + 0x0c9, // Size: 1, Flags: Public
        GuildGuid = ObjectFields.End + 0x0ca, // Size: 4, Flags: Public
        End = ObjectFields.End + 0x0ce
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
        QuestLog = UnitFields.End + 0x017, // Size: 1600, Flags: PartyMember
        VisibleItem = UnitFields.End + 0x657, // Size: 38, Flags: Public
        ChosenTitle = UnitFields.End + 0x67d, // Size: 1, Flags: Public
        FakeInebriation = UnitFields.End + 0x67e, // Size: 1, Flags: Public
        VirtualRealm = UnitFields.End + 0x67f, // Size: 1, Flags: Public
        CurrentSpecId = UnitFields.End + 0x680, // Size: 1, Flags: Public
        TaxiMountAnimKitId = UnitFields.End + 0x681, // Size: 1, Flags: Public
        AvgItemLevel = UnitFields.End + 0x682, // Size: 4, Flags: Public
        CurrentBattlePetBreedQuality = UnitFields.End + 0x686, // Size: 1, Flags: Public
        HonorLevel = UnitFields.End + 0x687, // Size: 1, Flags: Public
        End = UnitFields.End + 0x688
    }

    public enum PlayerDynamicFields
    {
        ArenaCooldowns = UnitDynamicFields.End + 0x000, // Flags: Public
        End = UnitDynamicFields.End + 0x001,
    }

    public enum ActivePlayerFields
    {
        InvSlotHead = PlayerFields.End + 0x000, // Size: 780, Flags: Public
        Farsight = PlayerFields.End + 0x30c, // Size: 4, Flags: Public
        SummonedBattlePetId = PlayerFields.End + 0x310, // Size: 4, Flags: Public
        KnownTitles = PlayerFields.End + 0x314, // Size: 12, Flags: Public
        Coinage = PlayerFields.End + 0x320, // Size: 2, Flags: Public
        Xp = PlayerFields.End + 0x322, // Size: 1, Flags: Public
        NextLevelXp = PlayerFields.End + 0x323, // Size: 1, Flags: Public
        TrialXp = PlayerFields.End + 0x324, // Size: 1, Flags: Public
        SkillLineId = PlayerFields.End + 0x325, // Size: 128, Flags: Public
        SkillLineStep = PlayerFields.End + 0x3a5, // Size: 128, Flags: Public
        SkillLineRank = PlayerFields.End + 0x425, // Size: 128, Flags: Public
        SkillLineStartRank = PlayerFields.End + 0x4a5, // Size: 128, Flags: Public
        SkillLineMaxRank = PlayerFields.End + 0x525, // Size: 128, Flags: Public
        SkillLineTempBonus = PlayerFields.End + 0x5a5, // Size: 128, Flags: Public
        SkillLinePermBonus = PlayerFields.End + 0x625, // Size: 128, Flags: Public
        CharacterPoints = PlayerFields.End + 0x6a5, // Size: 1, Flags: Public
        MaxTalentTiers = PlayerFields.End + 0x6a6, // Size: 1, Flags: Public
        TrackCreatures = PlayerFields.End + 0x6a7, // Size: 1, Flags: Public
        TrackResources = PlayerFields.End + 0x6a8, // Size: 2, Flags: Public
        Expertise = PlayerFields.End + 0x6aa, // Size: 1, Flags: Public
        OffhandExpertise = PlayerFields.End + 0x6ab, // Size: 1, Flags: Public
        RangedExpertise = PlayerFields.End + 0x6ac, // Size: 1, Flags: Public
        CombatRatingExpertise = PlayerFields.End + 0x6ad, // Size: 1, Flags: Public
        BlockPercentage = PlayerFields.End + 0x6ae, // Size: 1, Flags: Public
        DodgePercentage = PlayerFields.End + 0x6af, // Size: 1, Flags: Public
        DodgePercentageFromAttribute = PlayerFields.End + 0x6b0, // Size: 1, Flags: Public
        ParryPercentage = PlayerFields.End + 0x6b1, // Size: 1, Flags: Public
        ParryPercentageFromAttribute = PlayerFields.End + 0x6b2, // Size: 1, Flags: Public
        CritPercentage = PlayerFields.End + 0x6b3, // Size: 1, Flags: Public
        RangedCritPercentage = PlayerFields.End + 0x6b4, // Size: 1, Flags: Public
        OffhandCritPercentage = PlayerFields.End + 0x6b5, // Size: 1, Flags: Public
        SpellCritPercentage1 = PlayerFields.End + 0x6b6, // Size: 1, Flags: Public
        ShieldBlock = PlayerFields.End + 0x6b7, // Size: 1, Flags: Public
        ShieldBlockCritPercentage = PlayerFields.End + 0x6b8, // Size: 1, Flags: Public
        Mastery = PlayerFields.End + 0x6b9, // Size: 1, Flags: Public
        Speed = PlayerFields.End + 0x6ba, // Size: 1, Flags: Public
        Avoidance = PlayerFields.End + 0x6bb, // Size: 1, Flags: Public
        Sturdiness = PlayerFields.End + 0x6bc, // Size: 1, Flags: Public
        Versatility = PlayerFields.End + 0x6bd, // Size: 1, Flags: Public
        VersatilityBonus = PlayerFields.End + 0x6be, // Size: 1, Flags: Public
        PvpPowerDamage = PlayerFields.End + 0x6bf, // Size: 1, Flags: Public
        PvpPowerHealing = PlayerFields.End + 0x6c0, // Size: 1, Flags: Public
        ExploredZones = PlayerFields.End + 0x6c1, // Size: 320, Flags: Public
        RestInfo = PlayerFields.End + 0x801, // Size: 4, Flags: Public
        ModDamageDonePos = PlayerFields.End + 0x805, // Size: 7, Flags: Public
        ModDamageDoneNeg = PlayerFields.End + 0x80c, // Size: 7, Flags: Public
        ModDamageDonePct = PlayerFields.End + 0x813, // Size: 7, Flags: Public
        ModHealingDonePos = PlayerFields.End + 0x81a, // Size: 1, Flags: Public
        ModHealingPct = PlayerFields.End + 0x81b, // Size: 1, Flags: Public
        ModHealingDonePct = PlayerFields.End + 0x81c, // Size: 1, Flags: Public
        ModPeriodicHealingDonePercent = PlayerFields.End + 0x81d, // Size: 1, Flags: Public
        WeaponDmgMultipliers = PlayerFields.End + 0x81e, // Size: 3, Flags: Public
        WeaponAtkSpeedMultipliers = PlayerFields.End + 0x821, // Size: 3, Flags: Public
        ModSpellPowerPct = PlayerFields.End + 0x824, // Size: 1, Flags: Public
        ModResiliencePercent = PlayerFields.End + 0x825, // Size: 1, Flags: Public
        OverrideSpellPowerByApPct = PlayerFields.End + 0x826, // Size: 1, Flags: Public
        OverrideApBySpellPowerPercent = PlayerFields.End + 0x827, // Size: 1, Flags: Public
        ModTargetResistance = PlayerFields.End + 0x828, // Size: 1, Flags: Public
        ModTargetPhysicalResistance = PlayerFields.End + 0x829, // Size: 1, Flags: Public
        LocalFlags = PlayerFields.End + 0x82a, // Size: 1, Flags: Public
        Bytes = PlayerFields.End + 0x82b, // Size: 1, Flags: Public
        PvpMedals = PlayerFields.End + 0x82c, // Size: 1, Flags: Public
        BuyBackPrice = PlayerFields.End + 0x82d, // Size: 12, Flags: Public
        BuyBackTimestamp = PlayerFields.End + 0x839, // Size: 12, Flags: Public
        Kills = PlayerFields.End + 0x845, // Size: 1, Flags: Public
        LifetimeHonorableKills = PlayerFields.End + 0x846, // Size: 1, Flags: Public
        WatchedFactionIndex = PlayerFields.End + 0x847, // Size: 1, Flags: Public
        CombatRating = PlayerFields.End + 0x848, // Size: 32, Flags: Public
        ArenaTeamInfo = PlayerFields.End + 0x868, // Size: 54, Flags: Public
        MaxLevel = PlayerFields.End + 0x89e, // Size: 1, Flags: Public
        ScalingPlayerLevelDelta = PlayerFields.End + 0x89f, // Size: 1, Flags: Public
        MaxCreatureScalingLevel = PlayerFields.End + 0x8a0, // Size: 1, Flags: Public
        NoReagentCost = PlayerFields.End + 0x8a1, // Size: 4, Flags: Public
        PetSpellPower = PlayerFields.End + 0x8a5, // Size: 1, Flags: Public
        ProfessionSkillLine = PlayerFields.End + 0x8a6, // Size: 2, Flags: Public
        UiHitModifier = PlayerFields.End + 0x8a8, // Size: 1, Flags: Public
        UiSpellHitModifier = PlayerFields.End + 0x8a9, // Size: 1, Flags: Public
        HomeRealmTimeOffset = PlayerFields.End + 0x8aa, // Size: 1, Flags: Public
        ModPetHaste = PlayerFields.End + 0x8ab, // Size: 1, Flags: Public
        Bytes2 = PlayerFields.End + 0x8ac, // Size: 1, Flags: Public
        Bytes3 = PlayerFields.End + 0x8ad, // Size: 1, Flags: Public, UrgentSelfOnly
        LfgBonusFactionId = PlayerFields.End + 0x8ae, // Size: 1, Flags: Public
        LootSpecId = PlayerFields.End + 0x8af, // Size: 1, Flags: Public
        OverrideZonePvpType = PlayerFields.End + 0x8b0, // Size: 1, Flags: Public, UrgentSelfOnly
        BagSlotFlags = PlayerFields.End + 0x8b1, // Size: 4, Flags: Public
        BankBagSlotFlags = PlayerFields.End + 0x8b5, // Size: 7, Flags: Public
        InsertItemsLeftToRight = PlayerFields.End + 0x8bc, // Size: 1, Flags: Public
        QuestCompleted = PlayerFields.End + 0x8bd, // Size: 1750, Flags: Public
        Honor = PlayerFields.End + 0xf93, // Size: 1, Flags: Public
        HonorNextLevel = PlayerFields.End + 0xf94, // Size: 1, Flags: Public
        PvpTierMaxFromWins = PlayerFields.End + 0xf95, // Size: 1, Flags: Public
        PvpLastWeeksTierMaxFromWins = PlayerFields.End + 0xf96, // Size: 1, Flags: Public
        End = PlayerFields.End + 0xf97,
    }

    public enum ActivePlayerDynamicFields
    {
        ReserachSite = PlayerDynamicFields.End + 0x000, // Flags: Public
        ResearchSiteProgress = PlayerDynamicFields.End + 0x001, // Flags: Public
        DailyQuests = PlayerDynamicFields.End + 0x002, // Flags: Public
        AvailableQuestLineXQuestId = PlayerDynamicFields.End + 0x003, // Flags: Public
        Heirlooms = PlayerDynamicFields.End + 0x005, // Flags: Public
        HeirloomFlags = PlayerDynamicFields.End + 0x006, // Flags: Public
        Toys = PlayerDynamicFields.End + 0x007, // Flags: Public
        Transmog = PlayerDynamicFields.End + 0x008, // Flags: Public
        ConditionalTransmog = PlayerDynamicFields.End + 0x009, // Flags: Public
        SelfResSpells = PlayerDynamicFields.End + 0x00a, // Flags: Public
        CharacterRestrictions = PlayerDynamicFields.End + 0x00b, // Flags: Public
        SpellPctModByLabel = PlayerDynamicFields.End + 0x00c, // Flags: Public
        SpellFlatModByLabel = PlayerDynamicFields.End + 0x00d, // Flags: Public
        Reserach = PlayerDynamicFields.End + 0x00e, // Flags: Public
        End = PlayerDynamicFields.End + 0x00f,
    }

    public enum GameObjectFields
    {
        CreatedBy = ObjectFields.End + 0x000, // Size: 4, Flags: Public
        GuildGuid = ObjectFields.End + 0x004, // Size: 4, Flags: Public
        DisplayId = ObjectFields.End + 0x008, // Size: 1, Flags: Dynamic, Urgent
        Flags = ObjectFields.End + 0x009, // Size: 1, Flags: Public, Urgent
        ParentRotation = ObjectFields.End + 0x00a, // Size: 4, Flags: Public
        Faction = ObjectFields.End + 0x00e, // Size: 1, Flags: Public
        Level = ObjectFields.End + 0x00f, // Size: 1, Flags: Public
        Bytes1 = ObjectFields.End + 0x010, // Size: 1, Flags: Public, Urgent
        SpellVisualId = ObjectFields.End + 0x011, // Size: 1, Flags: Public, Dynamic, Urgent
        StateSpellVisualId = ObjectFields.End + 0x012, // Size: 1, Flags: Dynamic, Urgent
        StateAnimId = ObjectFields.End + 0x013, // Size: 1, Flags: Dynamic, Urgent
        StateAnimKitId = ObjectFields.End + 0x014, // Size: 1, Flags: Dynamic, Urgent
        StateWorldEffectId = ObjectFields.End + 0x015, // Size: 4, Flags: Dynamic, Urgent
        CustomParam = ObjectFields.End + 0x019, // Size: 1, Flags: Public, Urgent
        End = ObjectFields.End + 0x01a,
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
        GuildGuid = ObjectFields.End + 0x008, // Size: 4, Flags: PUBLIC
        DisplayId = ObjectFields.End + 0x00C, // Size: 1, Flags: Public
        Item = ObjectFields.End + 0x00D, // Size: 19, Flags: Public
        Bytes1 = ObjectFields.End + 0x020, // Size: 1, Flags: Public
        Bytes2 = ObjectFields.End + 0x021, // Size: 1, Flags: Public
        Flags = ObjectFields.End + 0x022, // Size: 1, Flags: Public
        DynamicFlags = ObjectFields.End + 0x023, // Size: 1, Flags: Dynamic
        FactionTemplate = ObjectFields.End + 0x024, // Size: 1, Flags: Public
        CustomDisplayOption = ObjectFields.End + 0x025, // Size: 1, Flags: PUBLIC
        End = ObjectFields.End + 0x026
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
