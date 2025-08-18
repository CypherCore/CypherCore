// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Framework.Database
{
    public class HotfixDatabase : MySqlBase<HotfixStatements>
    {
        public override void PreparedStatements()
        {
            // Achievement.db2
            PrepareStatement(HotfixStatements.SEL_ACHIEVEMENT, "SELECT Description, Title, Reward, ID, InstanceID, Faction, Supercedes, Category, MinimumCriteria, " +
                "Points, Flags, UiOrder, IconFileID, RewardItemID, CriteriaTree, SharesCriteria, CovenantID, HiddenBeforeDisplaySeason, LegacyAfterTimeEvent" +
                " FROM achievement WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_ACHIEVEMENT_LOCALE, "SELECT ID, Description_lang, Title_lang, Reward_lang FROM achievement_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // AchievementCategory.db2
            PrepareStatement(HotfixStatements.SEL_ACHIEVEMENT_CATEGORY, "SELECT Name, ID, Parent, UiOrder FROM achievement_category WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_ACHIEVEMENT_CATEGORY_LOCALE, "SELECT ID, Name_lang FROM achievement_category_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // AdventureJournal.db2
            PrepareStatement(HotfixStatements.SEL_ADVENTURE_JOURNAL, "SELECT ID, Name, Description, ButtonText, RewardDescription, ContinueDescription, Type, " +
                "PlayerConditionID, Flags, ButtonActionType, TextureFileDataID, LfgDungeonID, QuestID, BattleMasterListID, PriorityMin, PriorityMax, " +
                "CurrencyType, CurrencyQuantity, UiMapID, BonusPlayerConditionID1, BonusPlayerConditionID2, BonusValue1, BonusValue2 FROM adventure_journal" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_ADVENTURE_JOURNAL_LOCALE, "SELECT ID, Name_lang, Description_lang, ButtonText_lang, RewardDescription_lang, " +
                "ContinueDescription_lang FROM adventure_journal_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // AdventureMapPoi.db2
            PrepareStatement(HotfixStatements.SEL_ADVENTURE_MAP_POI, "SELECT ID, Title, Description, WorldPositionX, WorldPositionY, Type, PlayerConditionID, QuestID, " +
                "LfgDungeonID, RewardItemID, UiTextureAtlasMemberID, UiTextureKitID, MapID, AreaTableID FROM adventure_map_poi WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_ADVENTURE_MAP_POI_LOCALE, "SELECT ID, Title_lang, Description_lang FROM adventure_map_poi_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // AnimationData.db2
            PrepareStatement(HotfixStatements.SEL_ANIMATION_DATA, "SELECT ID, Fallback, BehaviorTier, BehaviorID, Flags1, Flags2 FROM animation_data" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // AnimKit.db2
            PrepareStatement(HotfixStatements.SEL_ANIM_KIT, "SELECT ID, OneShotDuration, OneShotStopAnimKitID, LowDefAnimKitID FROM anim_kit" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // AreaGroupMember.db2
            PrepareStatement(HotfixStatements.SEL_AREA_GROUP_MEMBER, "SELECT ID, AreaID, AreaGroupID FROM area_group_member WHERE (`VerifiedBuild` > 0) = ?");

            // AreaTable.db2
            PrepareStatement(HotfixStatements.SEL_AREA_TABLE, "SELECT ID, ZoneName, AreaName, ContinentID, ParentAreaID, AreaBit, SoundProviderPref, " +
                "SoundProviderPrefUnderwater, AmbienceID, UwAmbience, ZoneMusic, UwZoneMusic, IntroSound, UwIntroSound, FactionGroupMask, AmbientMultiplier, " +
                "SoundProviderPrefUnderwater, AmbienceID, UwAmbience, ZoneMusic, UwZoneMusic, IntroSound, UwIntroSound, FactionGroupMask, AmbientMultiplier, " +
                "MountFlags, PvpCombatWorldStateID, WildBattlePetLevelMin, WildBattlePetLevelMax, WindSettingsID, ContentTuningID, Flags1, Flags2, " +
                "LiquidTypeID1, LiquidTypeID2, LiquidTypeID3, LiquidTypeID4 FROM area_table WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_AREA_TABLE_LOCALE, "SELECT ID, AreaName_lang FROM area_table_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // AreaTrigger.db2
            PrepareStatement(HotfixStatements.SEL_AREA_TRIGGER, "SELECT PosX, PosY, PosZ, ID, ContinentID, PhaseUseFlags, PhaseID, PhaseGroupID, Radius, BoxLength, " +
                "BoxWidth, BoxHeight, BoxYaw, ShapeType, ShapeID, AreaTriggerActionSetID, Flags FROM area_trigger WHERE (`VerifiedBuild` > 0) = ?");

            // AreaTriggerActionSet.db2
            PrepareStatement(HotfixStatements.SEL_AREA_TRIGGER_ACTION_SET, "SELECT ID, Flags FROM area_trigger_action_set WHERE (`VerifiedBuild` > 0) = ?");

            // ArmorLocation.db2
            PrepareStatement(HotfixStatements.SEL_ARMOR_LOCATION, "SELECT ID, Clothmodifier, Leathermodifier, Chainmodifier, Platemodifier, Modifier FROM armor_location" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // Artifact.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT, "SELECT Name, ID, UiTextureKitID, UiNameColor, UiBarOverlayColor, UiBarBackgroundColor, " +
                "ChrSpecializationID, Flags, ArtifactCategoryID, UiModelSceneID, SpellVisualKitID FROM artifact WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_LOCALE, "SELECT ID, Name_lang FROM artifact_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // ArtifactAppearance.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_APPEARANCE, "SELECT Name, ID, ArtifactAppearanceSetID, DisplayIndex, UnlockPlayerConditionID, " +
                "ItemAppearanceModifierID, UiSwatchColor, UiModelSaturation, UiModelOpacity, OverrideShapeshiftFormID, OverrideShapeshiftDisplayID, " +
                "UiItemAppearanceID, UiAltItemAppearanceID, Flags, UiCameraID, UsablePlayerConditionID FROM artifact_appearance" +
                    " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_APPEARANCE_LOCALE, "SELECT ID, Name_lang FROM artifact_appearance_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // ArtifactAppearanceSet.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET, "SELECT Name, Description, ID, DisplayIndex, UiCameraID, AltHandUICameraID, " +
                "ForgeAttachmentOverride, Flags, ArtifactID FROM artifact_appearance_set WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET_LOCALE, "SELECT ID, Name_lang, Description_lang FROM artifact_appearance_set_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // ArtifactCategory.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_CATEGORY, "SELECT ID, XpMultCurrencyID, XpMultCurveID FROM artifact_category WHERE (`VerifiedBuild` > 0) = ?");

            // ArtifactPower.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_POWER, "SELECT DisplayPosX, DisplayPosY, ID, ArtifactID, MaxPurchasableRank, Label, Flags, Tier" +
                " FROM artifact_power WHERE (`VerifiedBuild` > 0) = ?");

            // ArtifactPowerLink.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_POWER_LINK, "SELECT ID, PowerA, PowerB FROM artifact_power_link WHERE (`VerifiedBuild` > 0) = ?");

            // ArtifactPowerPicker.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_POWER_PICKER, "SELECT ID, PlayerConditionID FROM artifact_power_picker WHERE (`VerifiedBuild` > 0) = ?");

            // ArtifactPowerRank.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_POWER_RANK, "SELECT ID, RankIndex, SpellID, ItemBonusListID, AuraPointsOverride, ArtifactPowerID" +
                " FROM artifact_power_rank WHERE (`VerifiedBuild` > 0) = ?");

            // ArtifactQuestXp.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_QUEST_XP, "SELECT ID, Difficulty1, Difficulty2, Difficulty3, Difficulty4, Difficulty5, Difficulty6, " +
                "Difficulty7, Difficulty8, Difficulty9, Difficulty10 FROM artifact_quest_xp WHERE (`VerifiedBuild` > 0) = ?");

            // ArtifactTier.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_TIER, "SELECT ID, ArtifactTier, MaxNumTraits, MaxArtifactKnowledge, KnowledgePlayerCondition, " +
                "MinimumEmpowerKnowledge FROM artifact_tier WHERE (`VerifiedBuild` > 0) = ?");

            // ArtifactUnlock.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_UNLOCK, "SELECT ID, PowerID, PowerRank, ItemBonusListID, PlayerConditionID, ArtifactID FROM artifact_unlock" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // AuctionHouse.db2
            PrepareStatement(HotfixStatements.SEL_AUCTION_HOUSE, "SELECT ID, Name, FactionID, DepositRate, ConsignmentRate FROM auction_house" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_AUCTION_HOUSE_LOCALE, "SELECT ID, Name_lang FROM auction_house_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // AzeriteEmpoweredItem.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_EMPOWERED_ITEM, "SELECT ID, ItemID, AzeriteTierUnlockSetID, AzeritePowerSetID FROM azerite_empowered_item" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // AzeriteEssence.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_ESSENCE, "SELECT ID, Name, Description, SpecSetID FROM azerite_essence WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_AZERITE_ESSENCE_LOCALE, "SELECT ID, Name_lang, Description_lang FROM azerite_essence_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // AzeriteEssencePower.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_ESSENCE_POWER, "SELECT ID, SourceAlliance, SourceHorde, AzeriteEssenceID, Tier, MajorPowerDescription, " +
                "MinorPowerDescription, MajorPowerActual, MinorPowerActual FROM azerite_essence_power WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_AZERITE_ESSENCE_POWER_LOCALE, "SELECT ID, SourceAlliance_lang, SourceHorde_lang FROM azerite_essence_power_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // AzeriteItem.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_ITEM, "SELECT ID, ItemID FROM azerite_item WHERE (`VerifiedBuild` > 0) = ?");

            // AzeriteItemMilestonePower.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_ITEM_MILESTONE_POWER, "SELECT ID, RequiredLevel, AzeritePowerID, Type, AutoUnlock" +
                " FROM azerite_item_milestone_power WHERE (`VerifiedBuild` > 0) = ?");

            // AzeriteKnowledgeMultiplier.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_KNOWLEDGE_MULTIPLIER, "SELECT ID, Multiplier FROM azerite_knowledge_multiplier WHERE (`VerifiedBuild` > 0) = ?");

            // AzeriteLevelInfo.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_LEVEL_INFO, "SELECT ID, BaseExperienceToNextLevel, MinimumExperienceToNextLevel, ItemLevel" +
                " FROM azerite_level_info WHERE (`VerifiedBuild` > 0) = ?");

            // AzeritePower.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_POWER, "SELECT ID, SpellID, ItemBonusListID, SpecSetID, Flags FROM azerite_power" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // AzeritePowerSetMember.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_POWER_SET_MEMBER, "SELECT ID, AzeritePowerSetID, AzeritePowerID, Class, Tier, OrderIndex" +
                " FROM azerite_power_set_member WHERE (`VerifiedBuild` > 0) = ?");

            // AzeriteTierUnlock.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_TIER_UNLOCK, "SELECT ID, ItemCreationContext, Tier, AzeriteLevel, AzeriteTierUnlockSetID" +
                " FROM azerite_tier_unlock WHERE (`VerifiedBuild` > 0) = ?");

            // AzeriteTierUnlockSet.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_TIER_UNLOCK_SET, "SELECT ID, Flags FROM azerite_tier_unlock_set WHERE (`VerifiedBuild` > 0) = ?");

            // AzeriteUnlockMapping.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_UNLOCK_MAPPING, "SELECT ID, ItemLevel, ItemBonusListHead, ItemBonusListShoulders, ItemBonusListChest, " +
                "AzeriteUnlockMappingSetID FROM azerite_unlock_mapping WHERE (`VerifiedBuild` > 0) = ?");

            // BankBagSlotPrices.db2
            PrepareStatement(HotfixStatements.SEL_BANK_BAG_SLOT_PRICES, "SELECT ID, Cost FROM bank_bag_slot_prices WHERE (`VerifiedBuild` > 0) = ?");

            // BannedAddons.db2
            PrepareStatement(HotfixStatements.SEL_BANNED_ADDONS, "SELECT ID, Name, Version, Flags FROM banned_addons WHERE (`VerifiedBuild` > 0) = ?");

            // BarberShopStyle.db2
            PrepareStatement(HotfixStatements.SEL_BARBER_SHOP_STYLE, "SELECT ID, DisplayName, Description, Type, CostModifier, Race, Sex, Data FROM barber_shop_style" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_BARBER_SHOP_STYLE_LOCALE, "SELECT ID, DisplayName_lang, Description_lang FROM barber_shop_style_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // BattlePetBreedQuality.db2
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_BREED_QUALITY, "SELECT ID, MaxQualityRoll, StateMultiplier, QualityEnum FROM battle_pet_breed_quality" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // BattlePetBreedState.db2
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_BREED_STATE, "SELECT ID, BattlePetStateID, Value, BattlePetBreedID FROM battle_pet_breed_state" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // BattlePetSpecies.db2
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_SPECIES, "SELECT Description, SourceText, ID, CreatureID, SummonSpellID, IconFileDataID, PetTypeEnum, " +
                "Flags, SourceTypeEnum, CardUIModelSceneID, LoadoutUIModelSceneID, CovenantID FROM battle_pet_species WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_SPECIES_LOCALE, "SELECT ID, Description_lang, SourceText_lang FROM battle_pet_species_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // BattlePetSpeciesState.db2
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_SPECIES_STATE, "SELECT ID, BattlePetStateID, Value, BattlePetSpeciesID FROM battle_pet_species_state" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // BattlemasterList.db2
            PrepareStatement(HotfixStatements.SEL_BATTLEMASTER_LIST, "SELECT ID, Name, GameType, ShortDescription, LongDescription, PvpType, MinLevel, MaxLevel, " +
                "RatedPlayers, MinPlayers, MaxPlayers, GroupsAllowed, MaxGroupSize, HolidayWorldState, Flags, IconFileDataID, RequiredPlayerConditionID" +
                " FROM battlemaster_list WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_BATTLEMASTER_LIST_LOCALE, "SELECT ID, Name_lang, GameType_lang, ShortDescription_lang, LongDescription_lang" +
                " FROM battlemaster_list_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // BattlemasterListXMap.db2
            PrepareStatement(HotfixStatements.SEL_BATTLEMASTER_LIST_X_MAP, "SELECT ID, MapID, BattlemasterListID FROM battlemaster_list_x_map" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // BroadcastText.db2
            PrepareStatement(HotfixStatements.SEL_BROADCAST_TEXT, "SELECT `Text`, Text1, ID, LanguageID, ConditionID, EmotesID, Flags, ChatBubbleDurationMs, " +
                "VoiceOverPriorityID, SoundKitID1, SoundKitID2, EmoteID1, EmoteID2, EmoteID3, EmoteDelay1, EmoteDelay2, EmoteDelay3 FROM broadcast_text" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_BROADCAST_TEXT_LOCALE, "SELECT ID, Text_lang, Text1_lang FROM broadcast_text_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // BroadcastTextDuration.db2
            PrepareStatement(HotfixStatements.SEL_BROADCAST_TEXT_DURATION, "SELECT ID, Locale, Duration, BroadcastTextID FROM broadcast_text_duration" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // CfgCategories.db2
            PrepareStatement(HotfixStatements.SEL_CFG_CATEGORIES, "SELECT ID, Name, LocaleMask, CreateCharsetMask, ExistingCharsetMask, Flags, `Order`" +
                " FROM cfg_categories WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_CFG_CATEGORIES_LOCALE, "SELECT ID, Name_lang FROM cfg_categories_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // CfgRegions.db2
            PrepareStatement(HotfixStatements.SEL_CFG_REGIONS, "SELECT ID, Tag, RegionID, Raidorigin, RegionGroupMask, ChallengeOrigin FROM cfg_regions" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ChallengeModeItemBonusOverride.db2
            PrepareStatement(HotfixStatements.SEL_CHALLENGE_MODE_ITEM_BONUS_OVERRIDE, "SELECT ID, ItemBonusTreeGroupID, DstItemBonusTreeID, Value, " +
                "RequiredTimeEventPassed, RequiredTimeEventNotPassed, SrcItemBonusTreeID FROM challenge_mode_item_bonus_override" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // CharBaseInfo.db2
            PrepareStatement(HotfixStatements.SEL_CHAR_BASE_INFO, "SELECT ID, RaceID, ClassID, OtherFactionRaceID FROM char_base_info WHERE (`VerifiedBuild` > 0) = ?");

            // CharTitles.db2
            PrepareStatement(HotfixStatements.SEL_CHAR_TITLES, "SELECT ID, Name, Name1, MaskID, Flags FROM char_titles WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_CHAR_TITLES_LOCALE, "SELECT ID, Name_lang, Name1_lang FROM char_titles_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // CharacterLoadout.db2
            PrepareStatement(HotfixStatements.SEL_CHARACTER_LOADOUT, "SELECT ID, RaceMask, ChrClassID, Purpose, ItemContext FROM character_loadout" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // CharacterLoadoutItem.db2
            PrepareStatement(HotfixStatements.SEL_CHARACTER_LOADOUT_ITEM, "SELECT ID, CharacterLoadoutID, ItemID FROM character_loadout_item" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ChatChannels.db2
            PrepareStatement(HotfixStatements.SEL_CHAT_CHANNELS, "SELECT ID, Name, Shortcut, Flags, FactionGroup, Ruleset FROM chat_channels" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_CHAT_CHANNELS_LOCALE, "SELECT ID, Name_lang, Shortcut_lang FROM chat_channels_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // ChrClassUiDisplay.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CLASS_UI_DISPLAY, "SELECT ID, ChrClassesID, AdvGuidePlayerConditionID, SplashPlayerConditionID" +
                " FROM chr_class_ui_display WHERE (`VerifiedBuild` > 0) = ?");

            // ChrClasses.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CLASSES, "SELECT Name, Filename, NameMale, NameFemale, PetNameToken, Description, RoleInfoString, DisabledString, " +
                "HyphenatedNameMale, HyphenatedNameFemale, CreateScreenFileDataID, SelectScreenFileDataID, IconFileDataID, LowResScreenFileDataID, Flags, " +
                "SpellTextureBlobFileDataID, ArmorTypeMask, CharStartKitUnknown901, MaleCharacterCreationVisualFallback, " +
                "MaleCharacterCreationIdleVisualFallback, FemaleCharacterCreationVisualFallback, FemaleCharacterCreationIdleVisualFallback, " +
                "CharacterCreationIdleGroundVisualFallback, CharacterCreationGroundVisualFallback, AlteredFormCharacterCreationIdleVisualFallback, " +
                "CharacterCreationAnimLoopWaitTimeMsFallback, CinematicSequenceID, DefaultSpec, ID, PrimaryStatPriority, DisplayPower, " +
                "RangedAttackPowerPerAgility, AttackPowerPerAgility, AttackPowerPerStrength, SpellClassSet, ClassColorR, ClassColorG, ClassColorB, RolesMask" +
                " FROM chr_classes WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_CHR_CLASSES_LOCALE, "SELECT ID, Name_lang, NameMale_lang, NameFemale_lang, Description_lang, RoleInfoString_lang, " +
                "DisabledString_lang, HyphenatedNameMale_lang, HyphenatedNameFemale_lang FROM chr_classes_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // ChrClassesXPowerTypes.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CLASSES_X_POWER_TYPES, "SELECT ID, PowerType, ClassID FROM chr_classes_x_power_types" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ChrCustomizationChoice.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_CHOICE, "SELECT Name, ID, ChrCustomizationOptionID, ChrCustomizationReqID, " +
                "ChrCustomizationVisReqID, SortOrder, UiOrderIndex, Flags, AddedInPatch, SoundKitID, SwatchColor1, SwatchColor2 FROM chr_customization_choice" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_CHOICE_LOCALE, "SELECT ID, Name_lang FROM chr_customization_choice_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // ChrCustomizationDisplayInfo.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_DISPLAY_INFO, "SELECT ID, ShapeshiftFormID, DisplayID, BarberShopMinCameraDistance, " +
                "BarberShopHeightOffset, BarberShopCameraZoomOffset FROM chr_customization_display_info WHERE (`VerifiedBuild` > 0) = ?");

            // ChrCustomizationElement.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_ELEMENT, "SELECT ID, ChrCustomizationChoiceID, RelatedChrCustomizationChoiceID, " +
                "ChrCustomizationGeosetID, ChrCustomizationSkinnedModelID, ChrCustomizationMaterialID, ChrCustomizationBoneSetID, " +
                "ChrCustomizationCondModelID, ChrCustomizationDisplayInfoID, ChrCustItemGeoModifyID, ChrCustomizationVoiceID, AnimKitID, ParticleColorID, " +
                "ChrCustGeoComponentLinkID FROM chr_customization_element WHERE (`VerifiedBuild` > 0) = ?");

            // ChrCustomizationOption.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_OPTION, "SELECT Name, ID, SecondaryID, Flags, ChrModelID, SortIndex, ChrCustomizationCategoryID, " +
                "OptionType, BarberShopCostModifier, ChrCustomizationID, ChrCustomizationReqID, UiOrderIndex, AddedInPatch FROM chr_customization_option" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_OPTION_LOCALE, "SELECT ID, Name_lang FROM chr_customization_option_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // ChrCustomizationReq.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_REQ, "SELECT ID, RaceMask, ReqSource, Flags, ClassMask, RegionGroupMask, AchievementID, QuestID, " +
                "OverrideArchive, ItemModifiedAppearanceID FROM chr_customization_req WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_REQ_LOCALE, "SELECT ID, ReqSource_lang FROM chr_customization_req_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // ChrCustomizationReqChoice.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_REQ_CHOICE, "SELECT ID, ChrCustomizationChoiceID, ChrCustomizationReqID" +
                " FROM chr_customization_req_choice WHERE (`VerifiedBuild` > 0) = ?");

            // ChrModel.db2
            PrepareStatement(HotfixStatements.SEL_CHR_MODEL, "SELECT FaceCustomizationOffset1, FaceCustomizationOffset2, FaceCustomizationOffset3, CustomizeOffset1, " +
                "CustomizeOffset2, CustomizeOffset3, ID, Sex, DisplayID, CharComponentTextureLayoutID, Flags, SkeletonFileDataID, ModelFallbackChrModelID, " +
                "TextureFallbackChrModelID, HelmVisFallbackChrModelID, CustomizeScale, CustomizeFacing, CameraDistanceOffset, BarberShopCameraOffsetScale, " +
                "BarberShopCameraHeightOffsetScale, BarberShopCameraRotationOffset FROM chr_model WHERE (`VerifiedBuild` > 0) = ?");

            // ChrRaceXChrModel.db2
            PrepareStatement(HotfixStatements.SEL_CHR_RACE_X_CHR_MODEL, "SELECT ID, ChrRacesID, ChrModelID, Sex, AllowedTransmogSlots FROM chr_race_x_chr_model" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ChrRaces.db2
            PrepareStatement(HotfixStatements.SEL_CHR_RACES, "SELECT ID, ClientPrefix, ClientFileString, Name, NameFemale, NameLowercase, NameFemaleLowercase, " +
                "LoreName, LoreNameFemale, LoreNameLower, LoreNameLowerFemale, LoreDescription, ShortName, ShortNameFemale, ShortNameLower, " +
                "ShortNameLowerFemale, Flags, FactionID, CinematicSequenceID, ResSicknessSpellID, SplashSoundID, CreateScreenFileDataID, " +
                "SelectScreenFileDataID, LowResScreenFileDataID, AlteredFormStartVisualKitID1, AlteredFormStartVisualKitID2, AlteredFormStartVisualKitID3, " +
                "AlteredFormFinishVisualKitID1, AlteredFormFinishVisualKitID2, AlteredFormFinishVisualKitID3, HeritageArmorAchievementID, StartingLevel, " +
                "UiDisplayOrder, PlayableRaceBit, TransmogrifyDisabledSlotMask, AlteredFormCustomizeOffsetFallback1, AlteredFormCustomizeOffsetFallback2, " +
                "AlteredFormCustomizeOffsetFallback3, AlteredFormCustomizeRotationFallback, Unknown910_11, Unknown910_12, Unknown910_13, Unknown910_21, " +
                "Unknown910_22, Unknown910_23, BaseLanguage, CreatureType, Alliance, RaceRelated, UnalteredVisualRaceID, DefaultClassID, NeutralRaceID, " +
                "MaleModelFallbackRaceID, MaleModelFallbackSex, FemaleModelFallbackRaceID, FemaleModelFallbackSex, MaleTextureFallbackRaceID, " +
                "MaleTextureFallbackSex, FemaleTextureFallbackRaceID, FemaleTextureFallbackSex, HelmetAnimScalingRaceID, UnalteredVisualCustomizationRaceID" +
                " FROM chr_races WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_CHR_RACES_LOCALE, "SELECT ID, Name_lang, NameFemale_lang, NameLowercase_lang, NameFemaleLowercase_lang, LoreName_lang, " +
                "LoreNameFemale_lang, LoreNameLower_lang, LoreNameLowerFemale_lang, LoreDescription_lang, ShortName_lang, ShortNameFemale_lang, " +
                "ShortNameLower_lang, ShortNameLowerFemale_lang FROM chr_races_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // ChrSpecialization.db2
            PrepareStatement(HotfixStatements.SEL_CHR_SPECIALIZATION, "SELECT Name, FemaleName, Description, ID, ClassID, OrderIndex, PetTalentType, Role, Flags, " +
                "SpellIconFileID, PrimaryStatPriority, AnimReplacements, MasterySpellID1, MasterySpellID2 FROM chr_specialization" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_CHR_SPECIALIZATION_LOCALE, "SELECT ID, Name_lang, FemaleName_lang, Description_lang FROM chr_specialization_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // CinematicCamera.db2
            PrepareStatement(HotfixStatements.SEL_CINEMATIC_CAMERA, "SELECT ID, OriginX, OriginY, OriginZ, SoundID, OriginFacing, FileDataID, ConversationID" +
                " FROM cinematic_camera WHERE (`VerifiedBuild` > 0) = ?");

            // CinematicSequences.db2
            PrepareStatement(HotfixStatements.SEL_CINEMATIC_SEQUENCES, "SELECT ID, SoundID, Camera1, Camera2, Camera3, Camera4, Camera5, Camera6, Camera7, Camera8" +
                " FROM cinematic_sequences WHERE (`VerifiedBuild` > 0) = ?");

            // ConditionalChrModel.db2
            PrepareStatement(HotfixStatements.SEL_CONDITIONAL_CHR_MODEL, "SELECT ID, ChrModelID, ChrCustomizationReqID, PlayerConditionID, Flags, " +
                "ChrCustomizationCategoryID FROM conditional_chr_model WHERE (`VerifiedBuild` > 0) = ?");

            // ConditionalContentTuning.db2
            PrepareStatement(HotfixStatements.SEL_CONDITIONAL_CONTENT_TUNING, "SELECT ID, OrderIndex, RedirectContentTuningID, RedirectFlag, ParentContentTuningID" +
                " FROM conditional_content_tuning WHERE (`VerifiedBuild` > 0) = ?");

            // ContentTuning.db2
            PrepareStatement(HotfixStatements.SEL_CONTENT_TUNING, "SELECT ID, Flags, ExpansionID, HealthItemLevelCurveID, DamageItemLevelCurveID, " +
                "HealthPrimaryStatCurveID, DamagePrimaryStatCurveID, MinLevel, MaxLevel, MinLevelType, MaxLevelType, TargetLevelDelta, TargetLevelMaxDelta, " +
                "TargetLevelMin, TargetLevelMax, MinItemLevel, QuestXpMultiplier FROM content_tuning WHERE (`VerifiedBuild` > 0) = ?");

            // ContentTuningXExpected.db2
            PrepareStatement(HotfixStatements.SEL_CONTENT_TUNING_X_EXPECTED, "SELECT ID, ExpectedStatModID, MinMythicPlusSeasonID, MaxMythicPlusSeasonID, " +
                "ContentTuningID FROM content_tuning_x_expected WHERE (`VerifiedBuild` > 0) = ?");

            // ContentTuningXLabel.db2
            PrepareStatement(HotfixStatements.SEL_CONTENT_TUNING_X_LABEL, "SELECT ID, LabelID, ContentTuningID FROM content_tuning_x_label" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ConversationLine.db2
            PrepareStatement(HotfixStatements.SEL_CONVERSATION_LINE, "SELECT ID, BroadcastTextID, Unused1020, SpellVisualKitID, AdditionalDuration, " +
                "NextConversationLineID, AnimKitID, SpeechType, StartAnimation, EndAnimation FROM conversation_line WHERE (`VerifiedBuild` > 0) = ?");

            // CorruptionEffects.db2
            PrepareStatement(HotfixStatements.SEL_CORRUPTION_EFFECTS, "SELECT ID, MinCorruption, Aura, PlayerConditionID, Flags FROM corruption_effects" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // CreatureDisplayInfo.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_DISPLAY_INFO, "SELECT ID, ModelID, SoundID, SizeClass, CreatureModelScale, CreatureModelAlpha, BloodID, " +
                "ExtendedDisplayInfoID, NPCSoundID, ParticleColorID, PortraitCreatureDisplayInfoID, PortraitTextureFileDataID, ObjectEffectPackageID, " +
                "AnimReplacementSetID, Flags, StateSpellVisualKitID, PlayerOverrideScale, PetInstanceScale, UnarmedWeaponType, MountPoofSpellVisualKitID, " +
                "DissolveEffectID, Gender, DissolveOutEffectID, CreatureModelMinLod, ConditionalCreatureModelID, Unknown_1100_1, Unknown_1100_2, " +
                "TextureVariationFileDataID1, TextureVariationFileDataID2, TextureVariationFileDataID3, TextureVariationFileDataID4 FROM creature_display_info" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // CreatureDisplayInfoExtra.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_DISPLAY_INFO_EXTRA, "SELECT ID, DisplayRaceID, DisplaySexID, DisplayClassID, Flags, BakeMaterialResourcesID, " +
                "HDBakeMaterialResourcesID FROM creature_display_info_extra WHERE (`VerifiedBuild` > 0) = ?");

            // CreatureFamily.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_FAMILY, "SELECT ID, Name, MinScale, MinScaleLevel, MaxScale, MaxScaleLevel, PetFoodMask, PetTalentType, " +
                "IconFileID, SkillLine1, SkillLine2 FROM creature_family WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_CREATURE_FAMILY_LOCALE, "SELECT ID, Name_lang FROM creature_family_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // CreatureLabel.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_LABEL, "SELECT ID, LabelID, CreatureDifficultyID FROM creature_label WHERE (`VerifiedBuild` > 0) = ?");

            // CreatureModelData.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_MODEL_DATA, "SELECT ID, GeoBox1, GeoBox2, GeoBox3, GeoBox4, GeoBox5, GeoBox6, Flags, FileDataID, WalkSpeed, " +
                "RunSpeed, BloodID, FootprintTextureID, FootprintTextureLength, FootprintTextureWidth, FootprintParticleScale, FoleyMaterialID, " +
                "FootstepCameraEffectID, DeathThudCameraEffectID, SoundID, SizeClass, CollisionWidth, CollisionHeight, WorldEffectScale, " +
                "CreatureGeosetDataID, HoverHeight, AttachedEffectScale, ModelScale, MissileCollisionRadius, MissileCollisionPush, MissileCollisionRaise, " +
                "MountHeight, OverrideLootEffectScale, OverrideNameScale, OverrideSelectionRadius, TamedPetBaseScale, MountScaleOtherIndex, MountScaleSelf, " +
                "Unknown1100, MountScaleOther1, MountScaleOther2 FROM creature_model_data WHERE (`VerifiedBuild` > 0) = ?");

            // CreatureType.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_TYPE, "SELECT ID, Name, Flags FROM creature_type WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_CREATURE_TYPE_LOCALE, "SELECT ID, Name_lang FROM creature_type_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // Criteria.db2
            PrepareStatement(HotfixStatements.SEL_CRITERIA, "SELECT ID, Type, Asset, ModifierTreeId, StartEvent, StartAsset, StartTimer, FailEvent, FailAsset, Flags, " +
                "EligibilityWorldStateID, EligibilityWorldStateValue FROM criteria WHERE (`VerifiedBuild` > 0) = ?");

            // CriteriaTree.db2
            PrepareStatement(HotfixStatements.SEL_CRITERIA_TREE, "SELECT ID, Description, Parent, Amount, Operator, CriteriaID, OrderIndex, Flags FROM criteria_tree" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_CRITERIA_TREE_LOCALE, "SELECT ID, Description_lang FROM criteria_tree_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // CurrencyContainer.db2
            PrepareStatement(HotfixStatements.SEL_CURRENCY_CONTAINER, "SELECT ID, ContainerName, ContainerDescription, MinAmount, MaxAmount, ContainerIconID, " +
                "ContainerQuality, OnLootSpellVisualKitID, CurrencyTypesID FROM currency_container WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_CURRENCY_CONTAINER_LOCALE, "SELECT ID, ContainerName_lang, ContainerDescription_lang FROM currency_container_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // CurrencyTypes.db2
            PrepareStatement(HotfixStatements.SEL_CURRENCY_TYPES, "SELECT ID, Name, Description, CategoryID, InventoryIconFileID, SpellWeight, SpellCategory, MaxQty, " +
                "MaxEarnablePerWeek, Quality, FactionID, ItemGroupSoundsID, XpQuestDifficulty, AwardConditionID, MaxQtyWorldStateID, " +
                "RechargingAmountPerCycle, RechargingCycleDurationMS, AccountTransferPercentage, OrderIndex, Flags1, Flags2 FROM currency_types WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_CURRENCY_TYPES_LOCALE, "SELECT ID, Name_lang, Description_lang FROM currency_types_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // Curve.db2
            PrepareStatement(HotfixStatements.SEL_CURVE, "SELECT ID, Type, Flags FROM curve WHERE (`VerifiedBuild` > 0) = ?");

            // CurvePoint.db2
            PrepareStatement(HotfixStatements.SEL_CURVE_POINT, "SELECT PosX, PosY, PreSLSquishPosX, PreSLSquishPosY, ID, CurveID, OrderIndex FROM curve_point" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // DestructibleModelData.db2
            PrepareStatement(HotfixStatements.SEL_DESTRUCTIBLE_MODEL_DATA, "SELECT ID, State0ImpactEffectDoodadSet, State0AmbientDoodadSet, State1Wmo, " +
                "State1DestructionDoodadSet, State1ImpactEffectDoodadSet, State1AmbientDoodadSet, State2Wmo, State2DestructionDoodadSet, " +
                "State2ImpactEffectDoodadSet, State2AmbientDoodadSet, State3Wmo, State3InitDoodadSet, State3AmbientDoodadSet, EjectDirection, DoNotHighlight, " +
                "State0Wmo, HealEffect, HealEffectSpeed, State0NameSet, State1NameSet, State2NameSet, State3NameSet FROM destructible_model_data" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // Difficulty.db2
            PrepareStatement(HotfixStatements.SEL_DIFFICULTY, "SELECT ID, Name, InstanceType, OrderIndex, OldEnumValue, FallbackDifficultyID, MinPlayers, MaxPlayers, " +
                "Flags, ItemContext, ToggleDifficultyID, GroupSizeHealthCurveID, GroupSizeDmgCurveID, GroupSizeSpellPointsCurveID, Unknown1105 FROM difficulty" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_DIFFICULTY_LOCALE, "SELECT ID, Name_lang FROM difficulty_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // DungeonEncounter.db2
            PrepareStatement(HotfixStatements.SEL_DUNGEON_ENCOUNTER, "SELECT Name, ID, MapID, DifficultyID, OrderIndex, CompleteWorldStateID, Bit, Flags, " +
                "SpellIconFileID, Faction, Unknown1115 FROM dungeon_encounter WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_DUNGEON_ENCOUNTER_LOCALE, "SELECT ID, Name_lang FROM dungeon_encounter_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // DurabilityCosts.db2
            PrepareStatement(HotfixStatements.SEL_DURABILITY_COSTS, "SELECT ID, WeaponSubClassCost1, WeaponSubClassCost2, WeaponSubClassCost3, WeaponSubClassCost4, " +
                "WeaponSubClassCost5, WeaponSubClassCost6, WeaponSubClassCost7, WeaponSubClassCost8, WeaponSubClassCost9, WeaponSubClassCost10, " +
                "WeaponSubClassCost11, WeaponSubClassCost12, WeaponSubClassCost13, WeaponSubClassCost14, WeaponSubClassCost15, WeaponSubClassCost16, " +
                "WeaponSubClassCost17, WeaponSubClassCost18, WeaponSubClassCost19, WeaponSubClassCost20, WeaponSubClassCost21, ArmorSubClassCost1, " +
                "ArmorSubClassCost2, ArmorSubClassCost3, ArmorSubClassCost4, ArmorSubClassCost5, ArmorSubClassCost6, ArmorSubClassCost7, ArmorSubClassCost8" +
                " FROM durability_costs WHERE (`VerifiedBuild` > 0) = ?");

            // DurabilityQuality.db2
            PrepareStatement(HotfixStatements.SEL_DURABILITY_QUALITY, "SELECT ID, Data FROM durability_quality WHERE (`VerifiedBuild` > 0) = ?");

            // Emotes.db2
            PrepareStatement(HotfixStatements.SEL_EMOTES, "SELECT ID, RaceMask, EmoteSlashCommand, AnimID, EmoteFlags, EmoteSpecProc, EmoteSpecProcParam, EventSoundID, " +
                "SpellVisualKitID, ClassMask FROM emotes WHERE (`VerifiedBuild` > 0) = ?");

            // EmotesText.db2
            PrepareStatement(HotfixStatements.SEL_EMOTES_TEXT, "SELECT ID, Name, EmoteID FROM emotes_text WHERE (`VerifiedBuild` > 0) = ?");

            // EmotesTextSound.db2
            PrepareStatement(HotfixStatements.SEL_EMOTES_TEXT_SOUND, "SELECT ID, RaceID, ClassID, SexID, SoundID, EmotesTextID FROM emotes_text_sound" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ExpectedStat.db2
            PrepareStatement(HotfixStatements.SEL_EXPECTED_STAT, "SELECT ID, ExpansionID, CreatureHealth, PlayerHealth, CreatureAutoAttackDps, CreatureArmor, " +
                "PlayerMana, PlayerPrimaryStat, PlayerSecondaryStat, ArmorConstant, CreatureSpellDamage, Lvl FROM expected_stat" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ExpectedStatMod.db2
            PrepareStatement(HotfixStatements.SEL_EXPECTED_STAT_MOD, "SELECT ID, CreatureHealthMod, PlayerHealthMod, CreatureAutoAttackDPSMod, CreatureArmorMod, " +
                "PlayerManaMod, PlayerPrimaryStatMod, PlayerSecondaryStatMod, ArmorConstantMod, CreatureSpellDamageMod FROM expected_stat_mod" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // Faction.db2
            PrepareStatement(HotfixStatements.SEL_FACTION, "SELECT ID, ReputationRaceMask1, ReputationRaceMask2, ReputationRaceMask3, ReputationRaceMask4, Name, " +
                "Description, ReputationIndex, ParentFactionID, Expansion, FriendshipRepID, Flags, ParagonFactionID, RenownFactionID, RenownCurrencyID, " +
                "ReputationClassMask1, ReputationClassMask2, ReputationClassMask3, ReputationClassMask4, ReputationFlags1, ReputationFlags2, " +
                "ReputationFlags3, ReputationFlags4, ReputationBase1, ReputationBase2, ReputationBase3, ReputationBase4, ReputationMax1, ReputationMax2, " +
                "ReputationMax3, ReputationMax4, ParentFactionMod1, ParentFactionMod2, ParentFactionCap1, ParentFactionCap2 FROM faction" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_FACTION_LOCALE, "SELECT ID, Name_lang, Description_lang FROM faction_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // FactionTemplate.db2
            PrepareStatement(HotfixStatements.SEL_FACTION_TEMPLATE, "SELECT ID, Faction, Flags, FactionGroup, FriendGroup, EnemyGroup, Enemies1, Enemies2, Enemies3, " +
                "Enemies4, Enemies5, Enemies6, Enemies7, Enemies8, Friend1, Friend2, Friend3, Friend4, Friend5, Friend6, Friend7, Friend8" +
                " FROM faction_template WHERE (`VerifiedBuild` > 0) = ?");

            // FlightCapability.db2
            PrepareStatement(HotfixStatements.SEL_FLIGHT_CAPABILITY, "SELECT ID, AirFriction, MaxVel, Unknown1000_2, DoubleJumpVelMod, LiftCoefficient, " +
                "GlideStartMinHeight, AddImpulseMaxSpeed, BankingRateMin, BankingRateMax, PitchingRateDownMin, PitchingRateDownMax, PitchingRateUpMin, " +
                "PitchingRateUpMax, TurnVelocityThresholdMin, TurnVelocityThresholdMax, SurfaceFriction, OverMaxDeceleration, Unknown1000_17, Unknown1000_18, " +
                "Unknown1000_19, Unknown1000_20, Unknown1000_21, LaunchSpeedCoefficient, VigorRegenMaxVelCoefficient, SpellID FROM flight_capability" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // FriendshipRepReaction.db2
            PrepareStatement(HotfixStatements.SEL_FRIENDSHIP_REP_REACTION, "SELECT ID, Reaction, FriendshipRepID, ReactionThreshold, OverrideColor" +
                " FROM friendship_rep_reaction WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_FRIENDSHIP_REP_REACTION_LOCALE, "SELECT ID, Reaction_lang FROM friendship_rep_reaction_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // FriendshipReputation.db2
            PrepareStatement(HotfixStatements.SEL_FRIENDSHIP_REPUTATION, "SELECT Description, StandingModified, StandingChanged, ID, FactionID, TextureFileID, Flags" +
                " FROM friendship_reputation WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_FRIENDSHIP_REPUTATION_LOCALE, "SELECT ID, Description_lang, StandingModified_lang, StandingChanged_lang" +
                " FROM friendship_reputation_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // GameobjectArtKit.db2
            PrepareStatement(HotfixStatements.SEL_GAMEOBJECT_ART_KIT, "SELECT ID, AttachModelFileID, TextureVariationFileID1, TextureVariationFileID2, " +
                "TextureVariationFileID3 FROM gameobject_art_kit WHERE (`VerifiedBuild` > 0) = ?");

            // GameobjectDisplayInfo.db2
            PrepareStatement(HotfixStatements.SEL_GAMEOBJECT_DISPLAY_INFO, "SELECT ID, GeoBoxMinX, GeoBoxMinY, GeoBoxMinZ, GeoBoxMaxX, GeoBoxMaxY, GeoBoxMaxZ, " +
                "FileDataID, ObjectEffectPackageID, OverrideLootEffectScale, OverrideNameScale, AlternateDisplayType, ClientCreatureDisplayInfoID, " +
                "ClientItemID, Unknown1100 FROM gameobject_display_info WHERE (`VerifiedBuild` > 0) = ?");

            // GameobjectLabel.db2
            PrepareStatement(HotfixStatements.SEL_GAMEOBJECT_LABEL, "SELECT ID, LabelID, GameObjectID FROM gameobject_label WHERE (`VerifiedBuild` > 0) = ?");

            // Gameobjects.db2
            PrepareStatement(HotfixStatements.SEL_GAMEOBJECTS, "SELECT Name, PosX, PosY, PosZ, Rot1, Rot2, Rot3, Rot4, ID, OwnerID, DisplayID, Scale, TypeID, " +
                "PhaseUseFlags, PhaseID, PhaseGroupID, Unknown1100, PropValue1, PropValue2, PropValue3, PropValue4, PropValue5, PropValue6, PropValue7, " +
                "PropValue8 FROM gameobjects WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_GAMEOBJECTS_LOCALE, "SELECT ID, Name_lang FROM gameobjects_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // GarrAbility.db2
            PrepareStatement(HotfixStatements.SEL_GARR_ABILITY, "SELECT ID, Name, Description, GarrAbilityCategoryID, GarrFollowerTypeID, IconFileDataID, " +
                "FactionChangeGarrAbilityID, Flags FROM garr_ability WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_GARR_ABILITY_LOCALE, "SELECT ID, Name_lang, Description_lang FROM garr_ability_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // GarrBuilding.db2
            PrepareStatement(HotfixStatements.SEL_GARR_BUILDING, "SELECT ID, HordeName, AllianceName, Description, Tooltip, GarrTypeID, BuildingType, " +
                "HordeGameObjectID, AllianceGameObjectID, GarrSiteID, UpgradeLevel, BuildSeconds, CurrencyTypeID, CurrencyQty, HordeUiTextureKitID, " +
                "AllianceUiTextureKitID, IconFileDataID, AllianceSceneScriptPackageID, HordeSceneScriptPackageID, MaxAssignments, ShipmentCapacity, " +
                "GarrAbilityID, BonusGarrAbilityID, GoldCost, Flags FROM garr_building WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_GARR_BUILDING_LOCALE, "SELECT ID, HordeName_lang, AllianceName_lang, Description_lang, Tooltip_lang" +
                " FROM garr_building_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // GarrBuildingPlotInst.db2
            PrepareStatement(HotfixStatements.SEL_GARR_BUILDING_PLOT_INST, "SELECT MapOffsetX, MapOffsetY, ID, GarrBuildingID, GarrSiteLevelPlotInstID, " +
                "UiTextureAtlasMemberID FROM garr_building_plot_inst WHERE (`VerifiedBuild` > 0) = ?");

            // GarrClassSpec.db2
            PrepareStatement(HotfixStatements.SEL_GARR_CLASS_SPEC, "SELECT ID, ClassSpec, ClassSpecMale, ClassSpecFemale, UiTextureAtlasMemberID, GarrFollItemSetID, " +
                "FollowerClassLimit, Flags FROM garr_class_spec WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_GARR_CLASS_SPEC_LOCALE, "SELECT ID, ClassSpec_lang, ClassSpecMale_lang, ClassSpecFemale_lang FROM garr_class_spec_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // GarrFollower.db2
            PrepareStatement(HotfixStatements.SEL_GARR_FOLLOWER, "SELECT ID, HordeSourceText, AllianceSourceText, TitleName, GarrTypeID, GarrFollowerTypeID, " +
                "HordeCreatureID, AllianceCreatureID, HordeGarrFollRaceID, AllianceGarrFollRaceID, HordeGarrClassSpecID, AllianceGarrClassSpecID, Quality, " +
                "FollowerLevel, ItemLevelWeapon, ItemLevelArmor, HordeSourceTypeEnum, AllianceSourceTypeEnum, HordeIconFileDataID, AllianceIconFileDataID, " +
                "HordeGarrFollItemSetID, AllianceGarrFollItemSetID, HordeUITextureKitID, AllianceUITextureKitID, Vitality, HordeFlavorGarrStringID, " +
                "AllianceFlavorGarrStringID, HordeSlottingBroadcastTextID, AllySlottingBroadcastTextID, ChrClassID, Flags, Gender, AutoCombatantID, " +
                "CovenantID FROM garr_follower WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_GARR_FOLLOWER_LOCALE, "SELECT ID, HordeSourceText_lang, AllianceSourceText_lang, TitleName_lang FROM garr_follower_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // GarrFollowerXAbility.db2
            PrepareStatement(HotfixStatements.SEL_GARR_FOLLOWER_X_ABILITY, "SELECT ID, OrderIndex, FactionIndex, GarrAbilityID, GarrFollowerID" +
                " FROM garr_follower_x_ability WHERE (`VerifiedBuild` > 0) = ?");

            // GarrMission.db2
            PrepareStatement(HotfixStatements.SEL_GARR_MISSION, "SELECT ID, Name, Location, Description, MapPosX, MapPosY, WorldPosX, WorldPosY, GarrTypeID, " +
                "GarrMissionTypeID, GarrFollowerTypeID, MaxFollowers, MissionCost, MissionCostCurrencyTypesID, OfferedGarrMissionTextureID, UiTextureKitID, " +
                "EnvGarrMechanicID, EnvGarrMechanicTypeID, PlayerConditionID, GarrMissionSetID, TargetLevel, TargetItemLevel, MissionDuration, " +
                "TravelDuration, OfferDuration, BaseCompletionChance, BaseFollowerXP, OvermaxRewardPackID, FollowerDeathChance, AreaID, Flags, " +
                "AutoMissionScalar, AutoMissionScalarCurveID, AutoCombatantEnvCasterID FROM garr_mission WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_GARR_MISSION_LOCALE, "SELECT ID, Name_lang, Location_lang, Description_lang FROM garr_mission_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // GarrPlot.db2
            PrepareStatement(HotfixStatements.SEL_GARR_PLOT, "SELECT ID, Name, PlotType, HordeConstructObjID, AllianceConstructObjID, Flags, UiCategoryID, " +
                "UpgradeRequirement1, UpgradeRequirement2 FROM garr_plot WHERE (`VerifiedBuild` > 0) = ?");

            // GarrPlotBuilding.db2
            PrepareStatement(HotfixStatements.SEL_GARR_PLOT_BUILDING, "SELECT ID, GarrPlotID, GarrBuildingID FROM garr_plot_building WHERE (`VerifiedBuild` > 0) = ?");

            // GarrPlotInstance.db2
            PrepareStatement(HotfixStatements.SEL_GARR_PLOT_INSTANCE, "SELECT ID, Name, GarrPlotID FROM garr_plot_instance WHERE (`VerifiedBuild` > 0) = ?");

            // GarrSiteLevel.db2
            PrepareStatement(HotfixStatements.SEL_GARR_SITE_LEVEL, "SELECT ID, TownHallUiPosX, TownHallUiPosY, GarrSiteID, GarrLevel, MapID, UpgradeMovieID, " +
                "UiTextureKitID, MaxBuildingLevel, UpgradeCost, UpgradeGoldCost FROM garr_site_level WHERE (`VerifiedBuild` > 0) = ?");

            // GarrSiteLevelPlotInst.db2
            PrepareStatement(HotfixStatements.SEL_GARR_SITE_LEVEL_PLOT_INST, "SELECT ID, UiMarkerPosX, UiMarkerPosY, GarrSiteLevelID, GarrPlotInstanceID, UiMarkerSize" +
                " FROM garr_site_level_plot_inst WHERE (`VerifiedBuild` > 0) = ?");

            // GarrTalentTree.db2
            PrepareStatement(HotfixStatements.SEL_GARR_TALENT_TREE, "SELECT ID, Name, GarrTypeID, ClassID, MaxTiers, UiOrder, Flags, UiTextureKitID, " +
                "GarrTalentTreeType, PlayerConditionID, FeatureTypeIndex, FeatureSubtypeIndex, CurrencyID FROM garr_talent_tree" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_GARR_TALENT_TREE_LOCALE, "SELECT ID, Name_lang FROM garr_talent_tree_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // GemProperties.db2
            PrepareStatement(HotfixStatements.SEL_GEM_PROPERTIES, "SELECT ID, EnchantId, Type FROM gem_properties WHERE (`VerifiedBuild` > 0) = ?");

            // GlobalCurve.db2
            PrepareStatement(HotfixStatements.SEL_GLOBAL_CURVE, "SELECT ID, CurveID, Type FROM global_curve WHERE (`VerifiedBuild` > 0) = ?");

            // GlyphBindableSpell.db2
            PrepareStatement(HotfixStatements.SEL_GLYPH_BINDABLE_SPELL, "SELECT ID, SpellID, GlyphPropertiesID FROM glyph_bindable_spell WHERE (`VerifiedBuild` > 0) = ?");

            // GlyphProperties.db2
            PrepareStatement(HotfixStatements.SEL_GLYPH_PROPERTIES, "SELECT ID, SpellID, GlyphType, GlyphExclusiveCategoryID, SpellIconFileDataID FROM glyph_properties" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // GlyphRequiredSpec.db2
            PrepareStatement(HotfixStatements.SEL_GLYPH_REQUIRED_SPEC, "SELECT ID, ChrSpecializationID, GlyphPropertiesID FROM glyph_required_spec" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // GossipNpcOption.db2
            PrepareStatement(HotfixStatements.SEL_GOSSIP_NPC_OPTION, "SELECT ID, GossipNpcOption, LFGDungeonsID, TrainerID, GarrFollowerTypeID, CharShipmentID, " +
                "GarrTalentTreeID, UiMapID, UiItemInteractionID, Unknown_1000_8, Unknown_1000_9, CovenantID, GossipOptionID, TraitTreeID, ProfessionID, " +
                "Unknown_1002_14, SkillLineID FROM gossip_npc_option WHERE (`VerifiedBuild` > 0) = ?");

            // GuildColorBackground.db2
            PrepareStatement(HotfixStatements.SEL_GUILD_COLOR_BACKGROUND, "SELECT ID, Red, Blue, Green FROM guild_color_background WHERE (`VerifiedBuild` > 0) = ?");

            // GuildColorBorder.db2
            PrepareStatement(HotfixStatements.SEL_GUILD_COLOR_BORDER, "SELECT ID, Red, Blue, Green FROM guild_color_border WHERE (`VerifiedBuild` > 0) = ?");

            // GuildColorEmblem.db2
            PrepareStatement(HotfixStatements.SEL_GUILD_COLOR_EMBLEM, "SELECT ID, Red, Blue, Green FROM guild_color_emblem WHERE (`VerifiedBuild` > 0) = ?");

            // GuildPerkSpells.db2
            PrepareStatement(HotfixStatements.SEL_GUILD_PERK_SPELLS, "SELECT ID, SpellID FROM guild_perk_spells WHERE (`VerifiedBuild` > 0) = ?");

            // Heirloom.db2
            PrepareStatement(HotfixStatements.SEL_HEIRLOOM, "SELECT SourceText, ID, ItemID, LegacyUpgradedItemID, StaticUpgradedItemID, SourceTypeEnum, Flags, " +
                "LegacyItemID, UpgradeItemID1, UpgradeItemID2, UpgradeItemID3, UpgradeItemID4, UpgradeItemID5, UpgradeItemID6, UpgradeItemBonusListID1, " +
                "UpgradeItemBonusListID2, UpgradeItemBonusListID3, UpgradeItemBonusListID4, UpgradeItemBonusListID5, UpgradeItemBonusListID6 FROM heirloom" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_HEIRLOOM_LOCALE, "SELECT ID, SourceText_lang FROM heirloom_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // Holidays.db2
            PrepareStatement(HotfixStatements.SEL_HOLIDAYS, "SELECT ID, Region, Looping, HolidayNameID, HolidayDescriptionID, Priority, CalendarFilterType, Flags, " +
                "Duration1, Duration2, Duration3, Duration4, Duration5, Duration6, Duration7, Duration8, Duration9, Duration10, Date1, Date2, Date3, Date4, " +
                "Date5, Date6, Date7, Date8, Date9, Date10, Date11, Date12, Date13, Date14, Date15, Date16, Date17, Date18, Date19, Date20, Date21, Date22, " +
                "Date23, Date24, Date25, Date26, CalendarFlags1, CalendarFlags2, CalendarFlags3, CalendarFlags4, CalendarFlags5, CalendarFlags6, " +
                "CalendarFlags7, CalendarFlags8, CalendarFlags9, CalendarFlags10, TextureFileDataID1, TextureFileDataID2, TextureFileDataID3 FROM holidays" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ImportPriceArmor.db2
            PrepareStatement(HotfixStatements.SEL_IMPORT_PRICE_ARMOR, "SELECT ID, ClothModifier, LeatherModifier, ChainModifier, PlateModifier FROM import_price_armor" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ImportPriceQuality.db2
            PrepareStatement(HotfixStatements.SEL_IMPORT_PRICE_QUALITY, "SELECT ID, Data FROM import_price_quality WHERE (`VerifiedBuild` > 0) = ?");

            // ImportPriceShield.db2
            PrepareStatement(HotfixStatements.SEL_IMPORT_PRICE_SHIELD, "SELECT ID, Data FROM import_price_shield WHERE (`VerifiedBuild` > 0) = ?");

            // ImportPriceWeapon.db2
            PrepareStatement(HotfixStatements.SEL_IMPORT_PRICE_WEAPON, "SELECT ID, Data FROM import_price_weapon WHERE (`VerifiedBuild` > 0) = ?");

            // Item.db2
            PrepareStatement(HotfixStatements.SEL_ITEM, "SELECT ID, ClassID, SubclassID, Material, InventoryType, SheatheType, SoundOverrideSubclassID, IconFileDataID, " +
                "ItemGroupSoundsID, ContentTuningID, ModifiedCraftingReagentItemID, CraftingQualityID FROM item WHERE (`VerifiedBuild` > 0) = ?");

            // ItemAppearance.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_APPEARANCE, "SELECT ID, DisplayType, ItemDisplayInfoID, DefaultIconFileDataID, UiOrder, PlayerConditionID" +
                " FROM item_appearance WHERE (`VerifiedBuild` > 0) = ?");

            // ItemArmorQuality.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_ARMOR_QUALITY, "SELECT ID, Qualitymod1, Qualitymod2, Qualitymod3, Qualitymod4, Qualitymod5, Qualitymod6, " +
                "Qualitymod7 FROM item_armor_quality WHERE (`VerifiedBuild` > 0) = ?");

            // ItemArmorShield.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_ARMOR_SHIELD, "SELECT ID, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, Quality7, ItemLevel" +
                " FROM item_armor_shield WHERE (`VerifiedBuild` > 0) = ?");

            // ItemArmorTotal.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_ARMOR_TOTAL, "SELECT ID, ItemLevel, Cloth, Leather, Mail, Plate FROM item_armor_total" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ItemBagFamily.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_BAG_FAMILY, "SELECT ID, Name FROM item_bag_family WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_ITEM_BAG_FAMILY_LOCALE, "SELECT ID, Name_lang FROM item_bag_family_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // ItemBonus.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_BONUS, "SELECT ID, Value1, Value2, Value3, Value4, ParentItemBonusListID, Type, OrderIndex FROM item_bonus" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ItemBonusListGroupEntry.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_BONUS_LIST_GROUP_ENTRY, "SELECT ID, ItemBonusListGroupID, ItemBonusListID, ItemLevelSelectorID, SequenceValue, " +
                "ItemExtendedCostID, PlayerConditionID, Flags, ItemLogicalCostGroupID FROM item_bonus_list_group_entry WHERE (`VerifiedBuild` > 0) = ?");

            // ItemBonusListLevelDelta.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_BONUS_LIST_LEVEL_DELTA, "SELECT ItemLevelDelta, ID FROM item_bonus_list_level_delta" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ItemBonusTree.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_BONUS_TREE, "SELECT ID, Flags, InventoryTypeSlotMask FROM item_bonus_tree WHERE (`VerifiedBuild` > 0) = ?");

            // ItemBonusTreeNode.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_BONUS_TREE_NODE, "SELECT ID, ItemContext, ChildItemBonusTreeID, ChildItemBonusListID, ChildItemLevelSelectorID, " +
                "ChildItemBonusListGroupID, IblGroupPointsModSetID, MinMythicPlusLevel, MaxMythicPlusLevel, ParentItemBonusTreeID FROM item_bonus_tree_node" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ItemChildEquipment.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_CHILD_EQUIPMENT, "SELECT ID, ParentItemID, ChildItemID, ChildItemEquipSlot FROM item_child_equipment" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ItemClass.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_CLASS, "SELECT ID, ClassName, ClassID, PriceModifier, Flags FROM item_class WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_ITEM_CLASS_LOCALE, "SELECT ID, ClassName_lang FROM item_class_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // ItemContextPickerEntry.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_CONTEXT_PICKER_ENTRY, "SELECT ID, ItemCreationContext, OrderIndex, PVal, LabelID, Flags, PlayerConditionID, " +
                "ItemContextPickerID FROM item_context_picker_entry WHERE (`VerifiedBuild` > 0) = ?");

            // ItemCurrencyCost.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_CURRENCY_COST, "SELECT ID, ItemID FROM item_currency_cost WHERE (`VerifiedBuild` > 0) = ?");

            // ItemDamageAmmo.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_AMMO, "SELECT ID, ItemLevel, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, Quality7" +
                " FROM item_damage_ammo WHERE (`VerifiedBuild` > 0) = ?");

            // ItemDamageOneHand.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND, "SELECT ID, ItemLevel, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, Quality7" +
                " FROM item_damage_one_hand WHERE (`VerifiedBuild` > 0) = ?");

            // ItemDamageOneHandCaster.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND_CASTER, "SELECT ID, ItemLevel, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, " +
                "Quality7 FROM item_damage_one_hand_caster WHERE (`VerifiedBuild` > 0) = ?");

            // ItemDamageTwoHand.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND, "SELECT ID, ItemLevel, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, Quality7" +
                " FROM item_damage_two_hand WHERE (`VerifiedBuild` > 0) = ?");

            // ItemDamageTwoHandCaster.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND_CASTER, "SELECT ID, ItemLevel, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, " +
                "Quality7 FROM item_damage_two_hand_caster WHERE (`VerifiedBuild` > 0) = ?");

            // ItemDisenchantLoot.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DISENCHANT_LOOT, "SELECT ID, Subclass, Quality, MinLevel, MaxLevel, SkillRequired, ExpansionID, Class" +
                " FROM item_disenchant_loot WHERE (`VerifiedBuild` > 0) = ?");

            // ItemEffect.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_EFFECT, "SELECT ID, LegacySlotIndex, TriggerType, Charges, CoolDownMSec, CategoryCoolDownMSec, SpellCategoryID, " +
                "SpellID, ChrSpecializationID FROM item_effect WHERE (`VerifiedBuild` > 0) = ?");

            // ItemExtendedCost.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_EXTENDED_COST, "SELECT ID, RequiredArenaRating, ArenaBracket, Flags, MinFactionID, MinReputation, " +
                "RequiredAchievement, ItemID1, ItemID2, ItemID3, ItemID4, ItemID5, ItemCount1, ItemCount2, ItemCount3, ItemCount4, ItemCount5, CurrencyID1, " +
                "CurrencyID2, CurrencyID3, CurrencyID4, CurrencyID5, CurrencyCount1, CurrencyCount2, CurrencyCount3, CurrencyCount4, CurrencyCount5" +
                " FROM item_extended_cost WHERE (`VerifiedBuild` > 0) = ?");

            // ItemLevelSelector.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LEVEL_SELECTOR, "SELECT ID, MinItemLevel, ItemLevelSelectorQualitySetID, AzeriteUnlockMappingSet" +
                " FROM item_level_selector WHERE (`VerifiedBuild` > 0) = ?");

            // ItemLevelSelectorQuality.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY, "SELECT ID, QualityItemBonusListID, Quality, ParentILSQualitySetID" +
                " FROM item_level_selector_quality WHERE (`VerifiedBuild` > 0) = ?");

            // ItemLevelSelectorQualitySet.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY_SET, "SELECT ID, IlvlRare, IlvlEpic FROM item_level_selector_quality_set" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ItemLimitCategory.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LIMIT_CATEGORY, "SELECT ID, Name, Quantity, Flags FROM item_limit_category WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_LOCALE, "SELECT ID, Name_lang FROM item_limit_category_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // ItemLimitCategoryCondition.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_CONDITION, "SELECT ID, AddQuantity, PlayerConditionID, ParentItemLimitCategoryID" +
                " FROM item_limit_category_condition WHERE (`VerifiedBuild` > 0) = ?");

            // ItemModifiedAppearance.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_MODIFIED_APPEARANCE, "SELECT ID, ItemID, ItemAppearanceModifierID, ItemAppearanceID, OrderIndex, " +
                "TransmogSourceTypeEnum, Flags FROM item_modified_appearance WHERE (`VerifiedBuild` > 0) = ?");

            // ItemModifiedAppearanceExtra.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_MODIFIED_APPEARANCE_EXTRA, "SELECT ID, IconFileDataID, UnequippedIconFileDataID, SheatheType, " +
                "DisplayWeaponSubclassID, DisplayInventoryType FROM item_modified_appearance_extra WHERE (`VerifiedBuild` > 0) = ?");

            // ItemNameDescription.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_NAME_DESCRIPTION, "SELECT ID, Description, Color FROM item_name_description WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_ITEM_NAME_DESCRIPTION_LOCALE, "SELECT ID, Description_lang FROM item_name_description_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // ItemPriceBase.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_PRICE_BASE, "SELECT ID, ItemLevel, Armor, Weapon FROM item_price_base WHERE (`VerifiedBuild` > 0) = ?");

            // ItemSearchName.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SEARCH_NAME, "SELECT ID, AllowableRace, Display, OverallQualityID, ExpansionID, MinFactionID, MinReputation, " +
                "AllowableClass, RequiredLevel, RequiredSkill, RequiredSkillRank, RequiredAbility, ItemLevel, Flags1, Flags2, Flags3, Flags4, Flags5" +
                " FROM item_search_name WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_ITEM_SEARCH_NAME_LOCALE, "SELECT ID, Display_lang FROM item_search_name_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // ItemSet.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SET, "SELECT ID, Name, SetFlags, RequiredSkill, RequiredSkillRank, ItemID1, ItemID2, ItemID3, ItemID4, ItemID5, " +
                "ItemID6, ItemID7, ItemID8, ItemID9, ItemID10, ItemID11, ItemID12, ItemID13, ItemID14, ItemID15, ItemID16, ItemID17 FROM item_set" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_ITEM_SET_LOCALE, "SELECT ID, Name_lang FROM item_set_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // ItemSetSpell.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SET_SPELL, "SELECT ID, ChrSpecID, SpellID, Threshold, ItemSetID FROM item_set_spell" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ItemSparse.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SPARSE, "SELECT ID, AllowableRace, Description, Display3, Display2, Display1, Display, ExpansionID, DmgVariance, " +
                "LimitCategory, DurationInInventory, QualityModifier, BagFamily, StartQuestID, LanguageID, ItemRange, StatPercentageOfSocket1, " +
                "StatPercentageOfSocket2, StatPercentageOfSocket3, StatPercentageOfSocket4, StatPercentageOfSocket5, StatPercentageOfSocket6, " +
                "StatPercentageOfSocket7, StatPercentageOfSocket8, StatPercentageOfSocket9, StatPercentageOfSocket10, StatPercentEditor1, StatPercentEditor2, " +
                "StatPercentEditor3, StatPercentEditor4, StatPercentEditor5, StatPercentEditor6, StatPercentEditor7, StatPercentEditor8, StatPercentEditor9, " +
                "StatPercentEditor10, StatModifierBonusStat1, StatModifierBonusStat2, StatModifierBonusStat3, StatModifierBonusStat4, StatModifierBonusStat5, " +
                "StatModifierBonusStat6, StatModifierBonusStat7, StatModifierBonusStat8, StatModifierBonusStat9, StatModifierBonusStat10, Stackable, " +
                "MaxCount, MinReputation, RequiredAbility, SellPrice, BuyPrice, VendorStackCount, PriceVariance, PriceRandomValue, Flags1, Flags2, Flags3, " +
                "Flags4, Flags5, FactionRelated, ModifiedCraftingReagentItemID, ContentTuningID, PlayerLevelToItemLevelCurveID, ItemNameDescriptionID, " +
                "RequiredTransmogHoliday, RequiredHoliday, GemProperties, SocketMatchEnchantmentId, TotemCategoryID, InstanceBound, ZoneBound1, ZoneBound2, " +
                "ItemSet, LockID, PageID, ItemDelay, MinFactionID, RequiredSkillRank, RequiredSkill, ItemLevel, AllowableClass, ArtifactID, SpellWeight, " +
                "SpellWeightCategory, SocketType1, SocketType2, SocketType3, SheatheType, Material, PageMaterialID, Bonding, DamageDamageType, " +
                "ContainerSlots, RequiredPVPMedal, RequiredPVPRank, RequiredLevel, InventoryType, OverallQualityID FROM item_sparse" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_ITEM_SPARSE_LOCALE, "SELECT ID, Description_lang, Display3_lang, Display2_lang, Display1_lang, Display_lang" +
                " FROM item_sparse_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // ItemSpec.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SPEC, "SELECT ID, MinLevel, MaxLevel, ItemType, PrimaryStat, SecondaryStat, SpecializationID FROM item_spec" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // ItemSpecOverride.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SPEC_OVERRIDE, "SELECT ID, SpecID, ItemID FROM item_spec_override WHERE (`VerifiedBuild` > 0) = ?");

            // ItemXBonusTree.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_X_BONUS_TREE, "SELECT ID, ItemBonusTreeID, ItemID FROM item_x_bonus_tree WHERE (`VerifiedBuild` > 0) = ?");

            // ItemXItemEffect.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_X_ITEM_EFFECT, "SELECT ID, ItemEffectID, ItemID FROM item_x_item_effect WHERE (`VerifiedBuild` > 0) = ?");

            // JournalEncounter.db2
            PrepareStatement(HotfixStatements.SEL_JOURNAL_ENCOUNTER, "SELECT Name, Description, MapX, MapY, ID, JournalInstanceID, DungeonEncounterID, OrderIndex, " +
                "FirstSectionID, UiMapID, MapDisplayConditionID, Flags, DifficultyMask FROM journal_encounter WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_JOURNAL_ENCOUNTER_LOCALE, "SELECT ID, Name_lang, Description_lang FROM journal_encounter_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // JournalEncounterSection.db2
            PrepareStatement(HotfixStatements.SEL_JOURNAL_ENCOUNTER_SECTION, "SELECT ID, Title, BodyText, JournalEncounterID, OrderIndex, ParentSectionID, " +
                "FirstChildSectionID, NextSiblingSectionID, Type, IconCreatureDisplayInfoID, UiModelSceneID, SpellID, IconFileDataID, Flags, IconFlags, " +
                "DifficultyMask FROM journal_encounter_section WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_JOURNAL_ENCOUNTER_SECTION_LOCALE, "SELECT ID, Title_lang, BodyText_lang FROM journal_encounter_section_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // JournalInstance.db2
            PrepareStatement(HotfixStatements.SEL_JOURNAL_INSTANCE, "SELECT ID, Name, Description, MapID, BackgroundFileDataID, ButtonFileDataID, " +
                "ButtonSmallFileDataID, LoreFileDataID, Flags, AreaID, CovenantID FROM journal_instance WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_JOURNAL_INSTANCE_LOCALE, "SELECT ID, Name_lang, Description_lang FROM journal_instance_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // JournalTier.db2
            PrepareStatement(HotfixStatements.SEL_JOURNAL_TIER, "SELECT ID, Name, Expansion, PlayerConditionID FROM journal_tier WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_JOURNAL_TIER_LOCALE, "SELECT ID, Name_lang FROM journal_tier_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // Keychain.db2
            PrepareStatement(HotfixStatements.SEL_KEYCHAIN, "SELECT ID, Key1, Key2, Key3, Key4, Key5, Key6, Key7, Key8, Key9, Key10, Key11, Key12, Key13, Key14, Key15, " +
                "Key16, Key17, Key18, Key19, Key20, Key21, Key22, Key23, Key24, Key25, Key26, Key27, Key28, Key29, Key30, Key31, Key32 FROM keychain" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // KeystoneAffix.db2
            PrepareStatement(HotfixStatements.SEL_KEYSTONE_AFFIX, "SELECT Name, Description, ID, FiledataID FROM keystone_affix WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_KEYSTONE_AFFIX_LOCALE, "SELECT ID, Name_lang, Description_lang FROM keystone_affix_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // LanguageWords.db2
            PrepareStatement(HotfixStatements.SEL_LANGUAGE_WORDS, "SELECT ID, Word, LanguageID FROM language_words WHERE (`VerifiedBuild` > 0) = ?");

            // Languages.db2
            PrepareStatement(HotfixStatements.SEL_LANGUAGES, "SELECT Name, ID, Flags, UiTextureKitID, UiTextureKitElementCount, LearningCurveID FROM languages" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_LANGUAGES_LOCALE, "SELECT ID, Name_lang FROM languages_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // LfgDungeons.db2
            PrepareStatement(HotfixStatements.SEL_LFG_DUNGEONS, "SELECT ID, Name, Description, TypeID, Subtype, Faction, IconTextureFileID, RewardsBgTextureFileID, " +
                "PopupBgTextureFileID, ExpansionLevel, MapID, DifficultyID, MinGear, GroupID, OrderIndex, RequiredPlayerConditionId, RandomID, ScenarioID, " +
                "FinalEncounterID, CountTank, CountHealer, CountDamage, MinCountTank, MinCountHealer, MinCountDamage, MaxPremadeCountTank, " +
                "MaxPremadeCountHealer, MaxPremadeCountDamage, BonusReputationAmount, MentorItemLevel, MentorCharLevel, MaxPremadeGroupSize, ContentTuningID, " +
                "Flags1, Flags2 FROM lfg_dungeons WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_LFG_DUNGEONS_LOCALE, "SELECT ID, Name_lang, Description_lang FROM lfg_dungeons_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // Light.db2
            PrepareStatement(HotfixStatements.SEL_LIGHT, "SELECT ID, GameCoordsX, GameCoordsY, GameCoordsZ, GameFalloffStart, GameFalloffEnd, ContinentID, " +
                "LightParamsID1, LightParamsID2, LightParamsID3, LightParamsID4, LightParamsID5, LightParamsID6, LightParamsID7, LightParamsID8 FROM light" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // LiquidType.db2
            PrepareStatement(HotfixStatements.SEL_LIQUID_TYPE, "SELECT ID, Name, Texture1, Texture2, Texture3, Texture4, Texture5, Texture6, Flags, SoundBank, SoundID, " +
                "SpellID, MaxDarkenDepth, FogDarkenIntensity, AmbDarkenIntensity, DirDarkenIntensity, LightID, ParticleScale, ParticleMovement, " +
                "ParticleTexSlots, MaterialID, MinimapStaticCol, FrameCountTexture1, FrameCountTexture2, FrameCountTexture3, FrameCountTexture4, " +
                "FrameCountTexture5, FrameCountTexture6, Color1, Color2, Float1, Float2, Float3, `Float4`, Float5, Float6, Float7, `Float8`, Float9, Float10, " +
                "Float11, Float12, Float13, Float14, Float15, Float16, Float17, Float18, `Int1`, `Int2`, `Int3`, `Int4`, Coefficient1, Coefficient2, " +
                "Coefficient3, Coefficient4 FROM liquid_type WHERE (`VerifiedBuild` > 0) = ?");

            // Location.db2
            PrepareStatement(HotfixStatements.SEL_LOCATION, "SELECT ID, PosX, PosY, PosZ, Rot1, Rot2, Rot3 FROM location WHERE (`VerifiedBuild` > 0) = ?");

            // Lock.db2
            PrepareStatement(HotfixStatements.SEL_LOCK, "SELECT ID, Flags, Index1, Index2, Index3, Index4, Index5, Index6, Index7, Index8, Skill1, Skill2, Skill3, " +
                "Skill4, Skill5, Skill6, Skill7, Skill8, Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Action1, Action2, Action3, Action4, Action5, " +
                "Action6, Action7, Action8 FROM `lock` WHERE (`VerifiedBuild` > 0) = ?");

            // MailTemplate.db2
            PrepareStatement(HotfixStatements.SEL_MAIL_TEMPLATE, "SELECT ID, Body FROM mail_template WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_MAIL_TEMPLATE_LOCALE, "SELECT ID, Body_lang FROM mail_template_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // Map.db2
            PrepareStatement(HotfixStatements.SEL_MAP, "SELECT ID, Directory, MapName, MapDescription0, MapDescription1, PvpShortDescription, PvpLongDescription, " +
                "CorpseX, CorpseY, MapType, InstanceType, ExpansionID, AreaTableID, LoadingScreenID, TimeOfDayOverride, ParentMapID, CosmeticParentMapID, " +
                "TimeOffset, MinimapIconScale, CorpseMapID, MaxPlayers, WindSettingsID, ZmpFileDataID, WdtFileDataID, NavigationMaxDistance, " +
                "PreloadFileDataID, Flags1, Flags2, Flags3 FROM map WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_MAP_LOCALE, "SELECT ID, MapName_lang, MapDescription0_lang, MapDescription1_lang, PvpShortDescription_lang, " +
                "PvpLongDescription_lang FROM map_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // MapChallengeMode.db2
            PrepareStatement(HotfixStatements.SEL_MAP_CHALLENGE_MODE, "SELECT Name, ID, MapID, Flags, ExpansionLevel, RequiredWorldStateID, CriteriaCount1, " +
                "CriteriaCount2, CriteriaCount3 FROM map_challenge_mode WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_MAP_CHALLENGE_MODE_LOCALE, "SELECT ID, Name_lang FROM map_challenge_mode_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // MapDifficulty.db2
            PrepareStatement(HotfixStatements.SEL_MAP_DIFFICULTY, "SELECT Message, ID, DifficultyID, LockID, ResetInterval, MaxPlayers, ItemContext, " +
                "ItemContextPickerID, Flags, ContentTuningID, WorldStateExpressionID, MapID FROM map_difficulty WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_MAP_DIFFICULTY_LOCALE, "SELECT ID, Message_lang FROM map_difficulty_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // MapDifficultyXCondition.db2
            PrepareStatement(HotfixStatements.SEL_MAP_DIFFICULTY_X_CONDITION, "SELECT ID, FailureDescription, PlayerConditionID, OrderIndex, MapDifficultyID" +
                " FROM map_difficulty_x_condition WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_MAP_DIFFICULTY_X_CONDITION_LOCALE, "SELECT ID, FailureDescription_lang FROM map_difficulty_x_condition_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // MawPower.db2
            PrepareStatement(HotfixStatements.SEL_MAW_POWER, "SELECT ID, SpellID, MawPowerRarityID FROM maw_power WHERE (`VerifiedBuild` > 0) = ?");

            // ModifierTree.db2
            PrepareStatement(HotfixStatements.SEL_MODIFIER_TREE, "SELECT ID, Parent, Operator, Amount, Type, Asset, SecondaryAsset, TertiaryAsset FROM modifier_tree" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // Mount.db2
            PrepareStatement(HotfixStatements.SEL_MOUNT, "SELECT Name, SourceText, Description, ID, MountTypeID, Flags, SourceTypeEnum, SourceSpellID, " +
                "PlayerConditionID, MountFlyRideHeight, UiModelSceneID, MountSpecialRiderAnimKitID, MountSpecialSpellVisualKitID FROM mount" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_MOUNT_LOCALE, "SELECT ID, Name_lang, SourceText_lang, Description_lang FROM mount_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // MountCapability.db2
            PrepareStatement(HotfixStatements.SEL_MOUNT_CAPABILITY, "SELECT ID, Flags, ReqRidingSkill, ReqAreaID, ReqSpellAuraID, ReqSpellKnownID, ModSpellAuraID, " +
                "ReqMapID, PlayerConditionID, FlightCapabilityID, DriveCapabilityID FROM mount_capability WHERE (`VerifiedBuild` > 0) = ?");

            // MountEquipment.db2
            PrepareStatement(HotfixStatements.SEL_MOUNT_EQUIPMENT, "SELECT ID, Item, BuffSpell, Unknown820, LearnedBySpell FROM mount_equipment" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // MountTypeXCapability.db2
            PrepareStatement(HotfixStatements.SEL_MOUNT_TYPE_X_CAPABILITY, "SELECT ID, MountTypeID, MountCapabilityID, OrderIndex FROM mount_type_x_capability" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // MountXDisplay.db2
            PrepareStatement(HotfixStatements.SEL_MOUNT_X_DISPLAY, "SELECT ID, CreatureDisplayInfoID, PlayerConditionID, Unknown1100, MountID FROM mount_x_display" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // Movie.db2
            PrepareStatement(HotfixStatements.SEL_MOVIE, "SELECT ID, Volume, KeyID, AudioFileDataID, SubtitleFileDataID, SubtitleFileFormat FROM movie" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // MythicPlusSeason.db2
            PrepareStatement(HotfixStatements.SEL_MYTHIC_PLUS_SEASON, "SELECT ID, MilestoneSeason, StartTimeEvent, ExpansionLevel, HeroicLFGDungeonMinGear" +
                " FROM mythic_plus_season WHERE (`VerifiedBuild` > 0) = ?");

            // NameGen.db2
            PrepareStatement(HotfixStatements.SEL_NAME_GEN, "SELECT ID, Name, RaceID, Sex FROM name_gen WHERE (`VerifiedBuild` > 0) = ?");

            // NamesProfanity.db2
            PrepareStatement(HotfixStatements.SEL_NAMES_PROFANITY, "SELECT ID, Name, Language FROM names_profanity WHERE (`VerifiedBuild` > 0) = ?");

            // NamesReserved.db2
            PrepareStatement(HotfixStatements.SEL_NAMES_RESERVED, "SELECT ID, Name FROM names_reserved WHERE (`VerifiedBuild` > 0) = ?");

            // NamesReservedLocale.db2
            PrepareStatement(HotfixStatements.SEL_NAMES_RESERVED_LOCALE, "SELECT ID, Name, LocaleMask FROM names_reserved_locale WHERE (`VerifiedBuild` > 0) = ?");

            // NumTalentsAtLevel.db2
            PrepareStatement(HotfixStatements.SEL_NUM_TALENTS_AT_LEVEL, "SELECT ID, NumTalents, NumTalentsDeathKnight, NumTalentsDemonHunter, Unknown1115" +
                " FROM num_talents_at_level WHERE (`VerifiedBuild` > 0) = ?");

            // OverrideSpellData.db2
            PrepareStatement(HotfixStatements.SEL_OVERRIDE_SPELL_DATA, "SELECT ID, Spells1, Spells2, Spells3, Spells4, Spells5, Spells6, Spells7, Spells8, Spells9, " +
                "Spells10, PlayerActionBarFileDataID, Flags FROM override_spell_data WHERE (`VerifiedBuild` > 0) = ?");

            // ParagonReputation.db2
            PrepareStatement(HotfixStatements.SEL_PARAGON_REPUTATION, "SELECT ID, FactionID, LevelThreshold, QuestID FROM paragon_reputation" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // Path.db2
            PrepareStatement(HotfixStatements.SEL_PATH, "SELECT ID, Type, SplineType, Red, Green, Blue, Alpha, Flags FROM path WHERE (`VerifiedBuild` > 0) = ?");

            // PathNode.db2
            PrepareStatement(HotfixStatements.SEL_PATH_NODE, "SELECT ID, PathID, Sequence, LocationID FROM path_node WHERE (`VerifiedBuild` > 0) = ?");

            // PathProperty.db2
            PrepareStatement(HotfixStatements.SEL_PATH_PROPERTY, "SELECT ID, PathID, PropertyIndex, Value FROM path_property WHERE (`VerifiedBuild` > 0) = ?");

            // Phase.db2
            PrepareStatement(HotfixStatements.SEL_PHASE, "SELECT ID, Flags FROM phase WHERE (`VerifiedBuild` > 0) = ?");

            // PhaseXPhaseGroup.db2
            PrepareStatement(HotfixStatements.SEL_PHASE_X_PHASE_GROUP, "SELECT ID, PhaseID, PhaseGroupID FROM phase_x_phase_group WHERE (`VerifiedBuild` > 0) = ?");

            // PlayerCondition.db2
            PrepareStatement(HotfixStatements.SEL_PLAYER_CONDITION, "SELECT ID, RaceMask, FailureDescription, MinLevel, MaxLevel, ClassMask, SkillLogic, LanguageID, " +
                "MinLanguage, MaxLanguage, MaxFactionID, MaxReputation, ReputationLogic, CurrentPvpFaction, PvpMedal, PrevQuestLogic, CurrQuestLogic, " +
                "CurrentCompletedQuestLogic, SpellLogic, ItemLogic, ItemFlags, AuraSpellLogic, WorldStateExpressionID, WeatherID, PartyStatus, " +
                "LifetimeMaxPVPRank, AchievementLogic, Gender, NativeGender, AreaLogic, LfgLogic, CurrencyLogic, QuestKillID, QuestKillLogic, " +
                "MinExpansionLevel, MaxExpansionLevel, MinAvgItemLevel, MaxAvgItemLevel, MinAvgEquippedItemLevel, MaxAvgEquippedItemLevel, PhaseUseFlags, " +
                "PhaseID, PhaseGroupID, Flags, ChrSpecializationIndex, ChrSpecializationRole, ModifierTreeID, PowerType, PowerTypeComp, PowerTypeValue, " +
                "WeaponSubclassMask, MaxGuildLevel, MinGuildLevel, MaxExpansionTier, MinExpansionTier, MinPVPRank, MaxPVPRank, ContentTuningID, CovenantID, " +
                "TraitNodeEntryLogic, SkillID1, SkillID2, SkillID3, SkillID4, MinSkill1, MinSkill2, MinSkill3, MinSkill4, MaxSkill1, MaxSkill2, MaxSkill3, " +
                "MaxSkill4, MinFactionID1, MinFactionID2, MinFactionID3, MinReputation1, MinReputation2, MinReputation3, PrevQuestID1, PrevQuestID2, " +
                "PrevQuestID3, PrevQuestID4, CurrQuestID1, CurrQuestID2, CurrQuestID3, CurrQuestID4, CurrentCompletedQuestID1, CurrentCompletedQuestID2, " +
                "CurrentCompletedQuestID3, CurrentCompletedQuestID4, SpellID1, SpellID2, SpellID3, SpellID4, ItemID1, ItemID2, ItemID3, ItemID4, ItemCount1, " +
                "ItemCount2, ItemCount3, ItemCount4, Explored1, Explored2, Time1, Time2, AuraSpellID1, AuraSpellID2, AuraSpellID3, AuraSpellID4, AuraStacks1, " +
                "AuraStacks2, AuraStacks3, AuraStacks4, Achievement1, Achievement2, Achievement3, Achievement4, AreaID1, AreaID2, AreaID3, AreaID4, " +
                "LfgStatus1, LfgStatus2, LfgStatus3, LfgStatus4, LfgCompare1, LfgCompare2, LfgCompare3, LfgCompare4, LfgValue1, LfgValue2, LfgValue3, " +
                "LfgValue4, CurrencyID1, CurrencyID2, CurrencyID3, CurrencyID4, CurrencyCount1, CurrencyCount2, CurrencyCount3, CurrencyCount4, " +
                "QuestKillMonster1, QuestKillMonster2, QuestKillMonster3, QuestKillMonster4, QuestKillMonster5, QuestKillMonster6, MovementFlags1, " +
                "MovementFlags2, TraitNodeEntryID1, TraitNodeEntryID2, TraitNodeEntryID3, TraitNodeEntryID4, TraitNodeEntryMinRank1, TraitNodeEntryMinRank2, " +
                "TraitNodeEntryMinRank3, TraitNodeEntryMinRank4, TraitNodeEntryMaxRank1, TraitNodeEntryMaxRank2, TraitNodeEntryMaxRank3, " +
                "TraitNodeEntryMaxRank4 FROM player_condition WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_PLAYER_CONDITION_LOCALE, "SELECT ID, FailureDescription_lang FROM player_condition_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // PowerDisplay.db2
            PrepareStatement(HotfixStatements.SEL_POWER_DISPLAY, "SELECT ID, GlobalStringBaseTag, ActualType, Red, Green, Blue FROM power_display" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // PowerType.db2
            PrepareStatement(HotfixStatements.SEL_POWER_TYPE, "SELECT NameGlobalStringTag, CostGlobalStringTag, ID, PowerTypeEnum, MinPower, MaxBasePower, CenterPower, " +
                "DefaultPower, DisplayModifier, RegenInterruptTimeMS, RegenPeace, RegenCombat, Flags FROM power_type WHERE (`VerifiedBuild` > 0) = ?");

            // PrestigeLevelInfo.db2
            PrepareStatement(HotfixStatements.SEL_PRESTIGE_LEVEL_INFO, "SELECT ID, Name, PrestigeLevel, BadgeTextureFileDataID, Flags, AwardedAchievementID" +
                " FROM prestige_level_info WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_PRESTIGE_LEVEL_INFO_LOCALE, "SELECT ID, Name_lang FROM prestige_level_info_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // PvpDifficulty.db2
            PrepareStatement(HotfixStatements.SEL_PVP_DIFFICULTY, "SELECT ID, RangeIndex, MinLevel, MaxLevel, MapID FROM pvp_difficulty WHERE (`VerifiedBuild` > 0) = ?");

            // PvpItem.db2
            PrepareStatement(HotfixStatements.SEL_PVP_ITEM, "SELECT ID, ItemID, ItemLevelDelta FROM pvp_item WHERE (`VerifiedBuild` > 0) = ?");

            // PvpStat.db2
            PrepareStatement(HotfixStatements.SEL_PVP_STAT, "SELECT Description, ID, MapID FROM pvp_stat WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_PVP_STAT_LOCALE, "SELECT ID, Description_lang FROM pvp_stat_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // PvpSeason.db2
            PrepareStatement(HotfixStatements.SEL_PVP_SEASON, "SELECT ID, MilestoneSeason, AllianceAchievementID, HordeAchievementID FROM pvp_season" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // PvpTalent.db2
            PrepareStatement(HotfixStatements.SEL_PVP_TALENT, "SELECT Description, ID, SpecID, SpellID, OverridesSpellID, Flags, ActionBarSpellID, PvpTalentCategoryID, " +
                "LevelRequired, PlayerConditionID FROM pvp_talent WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_PVP_TALENT_LOCALE, "SELECT ID, Description_lang FROM pvp_talent_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // PvpTalentCategory.db2
            PrepareStatement(HotfixStatements.SEL_PVP_TALENT_CATEGORY, "SELECT ID, TalentSlotMask FROM pvp_talent_category WHERE (`VerifiedBuild` > 0) = ?");

            // PvpTalentSlotUnlock.db2
            PrepareStatement(HotfixStatements.SEL_PVP_TALENT_SLOT_UNLOCK, "SELECT ID, Slot, LevelRequired, DeathKnightLevelRequired, DemonHunterLevelRequired" +
                " FROM pvp_talent_slot_unlock WHERE (`VerifiedBuild` > 0) = ?");

            // PvpTier.db2
            PrepareStatement(HotfixStatements.SEL_PVP_TIER, "SELECT Name, ID, MinRating, MaxRating, PrevTier, NextTier, BracketID, `Rank`, RankIconFileDataID" +
                " FROM pvp_tier WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_PVP_TIER_LOCALE, "SELECT ID, Name_lang FROM pvp_tier_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // QuestFactionReward.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_FACTION_REWARD, "SELECT ID, Difficulty1, Difficulty2, Difficulty3, Difficulty4, Difficulty5, Difficulty6, " +
                "Difficulty7, Difficulty8, Difficulty9, Difficulty10 FROM quest_faction_reward WHERE (`VerifiedBuild` > 0) = ?");

            // QuestInfo.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_INFO, "SELECT ID, InfoName, Type, Modifiers, Profession FROM quest_info WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_QUEST_INFO_LOCALE, "SELECT ID, InfoName_lang FROM quest_info_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // QuestLineXQuest.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_LINE_X_QUEST, "SELECT ID, QuestLineID, QuestID, OrderIndex, Flags, Unknown1110 FROM quest_line_x_quest" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // QuestMoneyReward.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_MONEY_REWARD, "SELECT ID, Difficulty1, Difficulty2, Difficulty3, Difficulty4, Difficulty5, Difficulty6, " +
                "Difficulty7, Difficulty8, Difficulty9, Difficulty10 FROM quest_money_reward WHERE (`VerifiedBuild` > 0) = ?");

            // QuestPackageItem.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_PACKAGE_ITEM, "SELECT ID, PackageID, ItemID, ItemQuantity, DisplayType FROM quest_package_item" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // QuestSort.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_SORT, "SELECT ID, SortName, UiOrderIndex, Flags FROM quest_sort WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_QUEST_SORT_LOCALE, "SELECT ID, SortName_lang FROM quest_sort_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // QuestV2.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_V2, "SELECT ID, UniqueBitFlag, UiQuestDetailsTheme FROM quest_v2 WHERE (`VerifiedBuild` > 0) = ?");

            // QuestXp.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_XP, "SELECT ID, Difficulty1, Difficulty2, Difficulty3, Difficulty4, Difficulty5, Difficulty6, Difficulty7, " +
                "Difficulty8, Difficulty9, Difficulty10 FROM quest_xp WHERE (`VerifiedBuild` > 0) = ?");

            // RandPropPoints.db2
            PrepareStatement(HotfixStatements.SEL_RAND_PROP_POINTS, "SELECT ID, DamageReplaceStatF, DamageSecondaryF, DamageReplaceStat, DamageSecondary, EpicF1, " +
                "EpicF2, EpicF3, EpicF4, EpicF5, SuperiorF1, SuperiorF2, SuperiorF3, SuperiorF4, SuperiorF5, GoodF1, GoodF2, GoodF3, GoodF4, GoodF5, Epic1, " +
                "Epic2, Epic3, Epic4, Epic5, Superior1, Superior2, Superior3, Superior4, Superior5, Good1, Good2, Good3, Good4, Good5 FROM rand_prop_points" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // RewardPack.db2
            PrepareStatement(HotfixStatements.SEL_REWARD_PACK, "SELECT ID, CharTitleID, Money, ArtifactXPDifficulty, ArtifactXPMultiplier, ArtifactXPCategoryID, " +
                "TreasurePickerID FROM reward_pack WHERE (`VerifiedBuild` > 0) = ?");

            // RewardPackXCurrencyType.db2
            PrepareStatement(HotfixStatements.SEL_REWARD_PACK_X_CURRENCY_TYPE, "SELECT ID, CurrencyTypeID, Quantity, RewardPackID FROM reward_pack_x_currency_type" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // RewardPackXItem.db2
            PrepareStatement(HotfixStatements.SEL_REWARD_PACK_X_ITEM, "SELECT ID, ItemID, ItemQuantity, RewardPackID FROM reward_pack_x_item" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // Scenario.db2
            PrepareStatement(HotfixStatements.SEL_SCENARIO, "SELECT ID, Name, AreaTableID, Type, Flags, UiTextureKitID FROM scenario WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_SCENARIO_LOCALE, "SELECT ID, Name_lang FROM scenario_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // ScenarioStep.db2
            PrepareStatement(HotfixStatements.SEL_SCENARIO_STEP, "SELECT Description, Title, ID, ScenarioID, Criteriatreeid, RewardQuestID, RelatedStep, Supersedes, " +
                "OrderIndex, Flags, VisibilityPlayerConditionID, WidgetSetID FROM scenario_step WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_SCENARIO_STEP_LOCALE, "SELECT ID, Description_lang, Title_lang FROM scenario_step_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // SceneScript.db2
            PrepareStatement(HotfixStatements.SEL_SCENE_SCRIPT, "SELECT ID, FirstSceneScriptID, NextSceneScriptID, Unknown915 FROM scene_script" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // SceneScriptGlobalText.db2
            PrepareStatement(HotfixStatements.SEL_SCENE_SCRIPT_GLOBAL_TEXT, "SELECT ID, Name, Script FROM scene_script_global_text WHERE (`VerifiedBuild` > 0) = ?");

            // SceneScriptPackage.db2
            PrepareStatement(HotfixStatements.SEL_SCENE_SCRIPT_PACKAGE, "SELECT ID, Name, Unknown915 FROM scene_script_package WHERE (`VerifiedBuild` > 0) = ?");

            // SceneScriptText.db2
            PrepareStatement(HotfixStatements.SEL_SCENE_SCRIPT_TEXT, "SELECT ID, Name, Script FROM scene_script_text WHERE (`VerifiedBuild` > 0) = ?");

            // ServerMessages.db2
            PrepareStatement(HotfixStatements.SEL_SERVER_MESSAGES, "SELECT ID, `Text` FROM server_messages WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_SERVER_MESSAGES_LOCALE, "SELECT ID, Text_lang FROM server_messages_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // SkillLine.db2
            PrepareStatement(HotfixStatements.SEL_SKILL_LINE, "SELECT DisplayName, AlternateVerb, Description, HordeDisplayName, OverrideSourceInfoDisplayName, ID, " +
                "CategoryID, SpellIconFileID, CanLink, ParentSkillLineID, ParentTierIndex, Flags, SpellBookSpellID, ExpansionNameSharedStringID, " +
                "HordeExpansionNameSharedStringID FROM skill_line WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_SKILL_LINE_LOCALE, "SELECT ID, DisplayName_lang, AlternateVerb_lang, Description_lang, HordeDisplayName_lang" +
                " FROM skill_line_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // SkillLineAbility.db2
            PrepareStatement(HotfixStatements.SEL_SKILL_LINE_ABILITY, "SELECT RaceMask, AbilityVerb, AbilityAllVerb, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, " +
                "SupercedesSpell, AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, " +
                "SkillupSkillLineID FROM skill_line_ability WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_SKILL_LINE_ABILITY_LOCALE, "SELECT ID, AbilityVerb_lang, AbilityAllVerb_lang FROM skill_line_ability_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // SkillLineXTraitTree.db2
            PrepareStatement(HotfixStatements.SEL_SKILL_LINE_X_TRAIT_TREE, "SELECT ID, SkillLineID, TraitTreeID, OrderIndex FROM skill_line_x_trait_tree" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // SkillRaceClassInfo.db2
            PrepareStatement(HotfixStatements.SEL_SKILL_RACE_CLASS_INFO, "SELECT ID, RaceMask, SkillID, ClassMask, Flags, Availability, MinLevel, SkillTierID" +
                " FROM skill_race_class_info WHERE (`VerifiedBuild` > 0) = ?");

            // SoulbindConduitRank.db2
            PrepareStatement(HotfixStatements.SEL_SOULBIND_CONDUIT_RANK, "SELECT ID, RankIndex, SpellID, AuraPointsOverride, SoulbindConduitID" +
                " FROM soulbind_conduit_rank WHERE (`VerifiedBuild` > 0) = ?");

            // SoundKit.db2
            PrepareStatement(HotfixStatements.SEL_SOUND_KIT, "SELECT ID, SoundType, VolumeFloat, Flags, MinDistance, DistanceCutoff, EAXDef, SoundKitAdvancedID, " +
                "VolumeVariationPlus, VolumeVariationMinus, PitchVariationPlus, PitchVariationMinus, DialogType, PitchAdjust, BusOverwriteID, MaxInstances, " +
                "SoundMixGroupID FROM sound_kit WHERE (`VerifiedBuild` > 0) = ?");

            // SpecializationSpells.db2
            PrepareStatement(HotfixStatements.SEL_SPECIALIZATION_SPELLS, "SELECT Description, ID, SpecID, SpellID, OverridesSpellID, DisplayOrder" +
                " FROM specialization_spells WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_SPECIALIZATION_SPELLS_LOCALE, "SELECT ID, Description_lang FROM specialization_spells_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // SpecSetMember.db2
            PrepareStatement(HotfixStatements.SEL_SPEC_SET_MEMBER, "SELECT ID, ChrSpecializationID, SpecSetID FROM spec_set_member WHERE (`VerifiedBuild` > 0) = ?");

            // SpellAuraOptions.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_AURA_OPTIONS, "SELECT ID, DifficultyID, CumulativeAura, ProcCategoryRecovery, ProcChance, ProcCharges, " +
                "SpellProcsPerMinuteID, ProcTypeMask1, ProcTypeMask2, SpellID FROM spell_aura_options WHERE (`VerifiedBuild` > 0) = ?");

            // SpellAuraRestrictions.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_AURA_RESTRICTIONS, "SELECT ID, DifficultyID, CasterAuraState, TargetAuraState, ExcludeCasterAuraState, " +
                "ExcludeTargetAuraState, CasterAuraSpell, TargetAuraSpell, ExcludeCasterAuraSpell, ExcludeTargetAuraSpell, CasterAuraType, TargetAuraType, " +
                "ExcludeCasterAuraType, ExcludeTargetAuraType, SpellID FROM spell_aura_restrictions WHERE (`VerifiedBuild` > 0) = ?");

            // SpellCastTimes.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CAST_TIMES, "SELECT ID, Base, Minimum FROM spell_cast_times WHERE (`VerifiedBuild` > 0) = ?");

            // SpellCastingRequirements.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CASTING_REQUIREMENTS, "SELECT ID, SpellID, FacingCasterFlags, MinFactionID, MinReputation, RequiredAreasID, " +
                "RequiredAuraVision, RequiresSpellFocus FROM spell_casting_requirements WHERE (`VerifiedBuild` > 0) = ?");

            // SpellCategories.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CATEGORIES, "SELECT ID, DifficultyID, Category, DefenseType, DispelType, Mechanic, PreventionType, " +
                "StartRecoveryCategory, ChargeCategory, SpellID FROM spell_categories WHERE (`VerifiedBuild` > 0) = ?");

            // SpellCategory.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CATEGORY, "SELECT ID, Name, Flags, UsesPerWeek, MaxCharges, ChargeRecoveryTime, TypeMask FROM spell_category" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_SPELL_CATEGORY_LOCALE, "SELECT ID, Name_lang FROM spell_category_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // SpellClassOptions.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CLASS_OPTIONS, "SELECT ID, SpellID, ModalNextSpell, SpellClassSet, SpellClassMask1, SpellClassMask2, " +
                "SpellClassMask3, SpellClassMask4 FROM spell_class_options WHERE (`VerifiedBuild` > 0) = ?");

            // SpellCooldowns.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_COOLDOWNS, "SELECT ID, DifficultyID, CategoryRecoveryTime, RecoveryTime, StartRecoveryTime, AuraSpellID, " +
                "SpellID FROM spell_cooldowns WHERE (`VerifiedBuild` > 0) = ?");

            // SpellDuration.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_DURATION, "SELECT ID, Duration, MaxDuration FROM spell_duration WHERE (`VerifiedBuild` > 0) = ?");

            // SpellEffect.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_EFFECT, "SELECT ID, EffectAura, DifficultyID, EffectIndex, Effect, EffectAmplitude, EffectAttributes, " +
                "EffectAuraPeriod, EffectBonusCoefficient, EffectChainAmplitude, EffectChainTargets, EffectItemType, EffectMechanic, EffectPointsPerResource, " +
                "EffectPosFacing, EffectRealPointsPerLevel, EffectTriggerSpell, BonusCoefficientFromAP, PvpMultiplier, Coefficient, Variance, " +
                "ResourceCoefficient, GroupSizeBasePointsCoefficient, EffectBasePoints, ScalingClass, EffectMiscValue1, EffectMiscValue2, EffectRadiusIndex1, " +
                "EffectRadiusIndex2, EffectSpellClassMask1, EffectSpellClassMask2, EffectSpellClassMask3, EffectSpellClassMask4, ImplicitTarget1, " +
                "ImplicitTarget2, SpellID FROM spell_effect WHERE (`VerifiedBuild` > 0) = ?");

            // SpellEmpower.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_EMPOWER, "SELECT ID, SpellID, Unused1000 FROM spell_empower WHERE (`VerifiedBuild` > 0) = ?");

            // SpellEmpowerStage.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_EMPOWER_STAGE, "SELECT ID, Stage, DurationMs, SpellEmpowerID FROM spell_empower_stage" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // SpellEquippedItems.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_EQUIPPED_ITEMS, "SELECT ID, SpellID, EquippedItemClass, EquippedItemInvTypes, EquippedItemSubclass" +
                " FROM spell_equipped_items WHERE (`VerifiedBuild` > 0) = ?");

            // SpellFocusObject.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_FOCUS_OBJECT, "SELECT ID, Name FROM spell_focus_object WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_SPELL_FOCUS_OBJECT_LOCALE, "SELECT ID, Name_lang FROM spell_focus_object_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // SpellInterrupts.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_INTERRUPTS, "SELECT ID, DifficultyID, InterruptFlags, AuraInterruptFlags1, AuraInterruptFlags2, " +
                "ChannelInterruptFlags1, ChannelInterruptFlags2, SpellID FROM spell_interrupts WHERE (`VerifiedBuild` > 0) = ?");

            // SpellItemEnchantment.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT, "SELECT ID, Name, HordeName, Duration, EffectArg1, EffectArg2, EffectArg3, Flags, " +
                "EffectScalingPoints1, EffectScalingPoints2, EffectScalingPoints3, IconFileDataID, MinItemLevel, MaxItemLevel, TransmogUseConditionID, " +
                "TransmogCost, EffectPointsMin1, EffectPointsMin2, EffectPointsMin3, ItemVisual, RequiredSkillID, RequiredSkillRank, ItemLevel, Charges, " +
                "Effect1, Effect2, Effect3, ScalingClass, ScalingClassRestricted, ConditionID, MinLevel, MaxLevel FROM spell_item_enchantment" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_LOCALE, "SELECT ID, Name_lang, HordeName_lang FROM spell_item_enchantment_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // SpellItemEnchantmentCondition.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_CONDITION, "SELECT ID, LtOperandType1, LtOperandType2, LtOperandType3, LtOperandType4, " +
                "LtOperandType5, LtOperand1, LtOperand2, LtOperand3, LtOperand4, LtOperand5, Operator1, Operator2, Operator3, Operator4, Operator5, " +
                "RtOperandType1, RtOperandType2, RtOperandType3, RtOperandType4, RtOperandType5, RtOperand1, RtOperand2, RtOperand3, RtOperand4, RtOperand5, " +
                "Logic1, Logic2, Logic3, Logic4, Logic5 FROM spell_item_enchantment_condition WHERE (`VerifiedBuild` > 0) = ?");

            // SpellKeyboundOverride.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_KEYBOUND_OVERRIDE, "SELECT ID, `Function`, Type, Data, Flags FROM spell_keybound_override" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // SpellLabel.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_LABEL, "SELECT ID, LabelID, SpellID FROM spell_label WHERE (`VerifiedBuild` > 0) = ?");

            // SpellLearnSpell.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_LEARN_SPELL, "SELECT ID, SpellID, LearnSpellID, OverridesSpellID FROM spell_learn_spell" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // SpellLevels.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_LEVELS, "SELECT ID, DifficultyID, MaxLevel, MaxPassiveAuraLevel, BaseLevel, SpellLevel, SpellID" +
                " FROM spell_levels WHERE (`VerifiedBuild` > 0) = ?");

            // SpellMisc.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_MISC, "SELECT ID, Attributes1, Attributes2, Attributes3, Attributes4, Attributes5, Attributes6, Attributes7, " +
                "Attributes8, Attributes9, Attributes10, Attributes11, Attributes12, Attributes13, Attributes14, Attributes15, Attributes16, DifficultyID, " +
                "CastingTimeIndex, DurationIndex, PvPDurationIndex, RangeIndex, SchoolMask, Speed, LaunchDelay, MinDuration, SpellIconFileDataID, " +
                "ActiveIconFileDataID, ContentTuningID, ShowFutureSpellPlayerConditionID, SpellVisualScript, ActiveSpellVisualScript, SpellID FROM spell_misc" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // SpellName.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_NAME, "SELECT ID, Name FROM spell_name WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_SPELL_NAME_LOCALE, "SELECT ID, Name_lang FROM spell_name_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // SpellPower.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_POWER, "SELECT ID, OrderIndex, ManaCost, ManaCostPerLevel, ManaPerSecond, PowerDisplayID, AltPowerBarID, " +
                "PowerCostPct, PowerCostMaxPct, OptionalCostPct, PowerPctPerSecond, PowerType, RequiredAuraSpellID, OptionalCost, SpellID FROM spell_power" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // SpellPowerDifficulty.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_POWER_DIFFICULTY, "SELECT ID, DifficultyID, OrderIndex FROM spell_power_difficulty" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // SpellProcsPerMinute.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE, "SELECT ID, BaseProcRate, Flags FROM spell_procs_per_minute WHERE (`VerifiedBuild` > 0) = ?");

            // SpellProcsPerMinuteMod.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE_MOD, "SELECT ID, Type, Param, Coeff, SpellProcsPerMinuteID FROM spell_procs_per_minute_mod" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // SpellRadius.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_RADIUS, "SELECT ID, Radius, RadiusPerLevel, RadiusMin, RadiusMax FROM spell_radius" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // SpellRange.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_RANGE, "SELECT ID, DisplayName, DisplayNameShort, Flags, RangeMin1, RangeMin2, RangeMax1, RangeMax2" +
                " FROM spell_range WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_SPELL_RANGE_LOCALE, "SELECT ID, DisplayName_lang, DisplayNameShort_lang FROM spell_range_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // SpellReagents.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_REAGENTS, "SELECT ID, SpellID, Reagent1, Reagent2, Reagent3, Reagent4, Reagent5, Reagent6, Reagent7, Reagent8, " +
                "ReagentCount1, ReagentCount2, ReagentCount3, ReagentCount4, ReagentCount5, ReagentCount6, ReagentCount7, ReagentCount8, " +
                "ReagentRecraftCount1, ReagentRecraftCount2, ReagentRecraftCount3, ReagentRecraftCount4, ReagentRecraftCount5, ReagentRecraftCount6, " +
                "ReagentRecraftCount7, ReagentRecraftCount8, ReagentSource1, ReagentSource2, ReagentSource3, ReagentSource4, ReagentSource5, ReagentSource6, " +
                "ReagentSource7, ReagentSource8 FROM spell_reagents WHERE (`VerifiedBuild` > 0) = ?");

            // SpellReagentsCurrency.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_REAGENTS_CURRENCY, "SELECT ID, SpellID, CurrencyTypesID, CurrencyCount FROM spell_reagents_currency" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // SpellScaling.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_SCALING, "SELECT ID, SpellID, MinScalingLevel, MaxScalingLevel, ScalesFromItemLevel FROM spell_scaling" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // SpellShapeshift.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_SHAPESHIFT, "SELECT ID, SpellID, StanceBarOrder, ShapeshiftExclude1, ShapeshiftExclude2, ShapeshiftMask1, " +
                "ShapeshiftMask2 FROM spell_shapeshift WHERE (`VerifiedBuild` > 0) = ?");

            // SpellShapeshiftForm.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM, "SELECT ID, Name, CreatureDisplayID, CreatureType, Flags, AttackIconFileID, BonusActionBar, " +
                "CombatRoundTime, DamageVariance, MountTypeID, PresetSpellID1, PresetSpellID2, PresetSpellID3, PresetSpellID4, PresetSpellID5, " +
                "PresetSpellID6, PresetSpellID7, PresetSpellID8 FROM spell_shapeshift_form WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM_LOCALE, "SELECT ID, Name_lang FROM spell_shapeshift_form_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // SpellTargetRestrictions.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_TARGET_RESTRICTIONS, "SELECT ID, DifficultyID, ConeDegrees, MaxTargets, MaxTargetLevel, TargetCreatureType, " +
                "Targets, Width, SpellID FROM spell_target_restrictions WHERE (`VerifiedBuild` > 0) = ?");

            // SpellTotems.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_TOTEMS, "SELECT ID, SpellID, RequiredTotemCategoryID1, RequiredTotemCategoryID2, Totem1, Totem2" +
                " FROM spell_totems WHERE (`VerifiedBuild` > 0) = ?");

            // SpellVisual.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_VISUAL, "SELECT ID, MissileCastOffset1, MissileCastOffset2, MissileCastOffset3, MissileImpactOffset1, " +
                "MissileImpactOffset2, MissileImpactOffset3, AnimEventSoundID, Flags, MissileAttachment, MissileDestinationAttachment, " +
                "MissileCastPositionerID, MissileImpactPositionerID, MissileTargetingKit, HostileSpellVisualID, CasterSpellVisualID, SpellVisualMissileSetID, " +
                "DamageNumberDelay, LowViolenceSpellVisualID, RaidSpellVisualMissileSetID, ReducedUnexpectedCameraMovementSpellVisualID FROM spell_visual" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // SpellVisualEffectName.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_VISUAL_EFFECT_NAME, "SELECT ID, ModelFileDataID, BaseMissileSpeed, Scale, MinAllowedScale, MaxAllowedScale, " +
                "Alpha, Flags, TextureFileDataID, EffectRadius, Type, GenericID, RibbonQualityID, DissolveEffectID, ModelPosition, Unknown901, Unknown1100" +
                " FROM spell_visual_effect_name WHERE (`VerifiedBuild` > 0) = ?");

            // SpellVisualKit.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_VISUAL_KIT, "SELECT ID, ClutterLevel, FallbackSpellVisualKitId, DelayMin, DelayMax, Flags1, Flags2" +
                " FROM spell_visual_kit WHERE (`VerifiedBuild` > 0) = ?");

            // SpellVisualMissile.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_VISUAL_MISSILE, "SELECT CastOffset1, CastOffset2, CastOffset3, ImpactOffset1, ImpactOffset2, ImpactOffset3, ID, " +
                "SpellVisualEffectNameID, SoundEntriesID, Attachment, DestinationAttachment, CastPositionerID, ImpactPositionerID, FollowGroundHeight, " +
                "FollowGroundDropSpeed, FollowGroundApproach, Flags, SpellMissileMotionID, AnimKitID, ClutterLevel, DecayTimeAfterImpact, Unused1100, " +
                "SpellVisualMissileSetID FROM spell_visual_missile WHERE (`VerifiedBuild` > 0) = ?");

            // SpellXSpellVisual.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_X_SPELL_VISUAL, "SELECT ID, DifficultyID, SpellVisualID, Probability, Flags, Priority, SpellIconFileID, " +
                "ActiveIconFileID, ViewerUnitConditionID, ViewerPlayerConditionID, CasterUnitConditionID, CasterPlayerConditionID, SpellID" +
                " FROM spell_x_spell_visual WHERE (`VerifiedBuild` > 0) = ?");

            // SummonProperties.db2
            PrepareStatement(HotfixStatements.SEL_SUMMON_PROPERTIES, "SELECT ID, Control, Faction, Title, Slot, Flags1, Flags2 FROM summon_properties" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TactKey.db2
            PrepareStatement(HotfixStatements.SEL_TACT_KEY, "SELECT ID, Key1, Key2, Key3, Key4, Key5, Key6, Key7, Key8, Key9, Key10, Key11, Key12, Key13, Key14, Key15, " +
                "Key16 FROM tact_key WHERE (`VerifiedBuild` > 0) = ?");

            // Talent.db2
            PrepareStatement(HotfixStatements.SEL_TALENT, "SELECT ID, Description, TierID, Flags, ColumnIndex, TabID, ClassID, SpecID, SpellID, OverridesSpellID, " +
                "RequiredSpellID, CategoryMask1, CategoryMask2, SpellRank1, SpellRank2, SpellRank3, SpellRank4, SpellRank5, SpellRank6, SpellRank7, " +
                "SpellRank8, SpellRank9, PrereqTalent1, PrereqTalent2, PrereqTalent3, PrereqRank1, PrereqRank2, PrereqRank3 FROM talent" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_TALENT_LOCALE, "SELECT ID, Description_lang FROM talent_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // TaxiNodes.db2
            PrepareStatement(HotfixStatements.SEL_TAXI_NODES, "SELECT Name, PosX, PosY, PosZ, MapOffsetX, MapOffsetY, FlightMapOffsetX, FlightMapOffsetY, ID, " +
                "ContinentID, ConditionID, CharacterBitNumber, Flags, UiTextureKitID, MinimapAtlasMemberID, Facing, SpecialIconConditionID, " +
                "VisibilityConditionID, MountCreatureID1, MountCreatureID2 FROM taxi_nodes WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_TAXI_NODES_LOCALE, "SELECT ID, Name_lang FROM taxi_nodes_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // TaxiPath.db2
            PrepareStatement(HotfixStatements.SEL_TAXI_PATH, "SELECT ID, FromTaxiNode, ToTaxiNode, Cost FROM taxi_path WHERE (`VerifiedBuild` > 0) = ?");

            // TaxiPathNode.db2
            PrepareStatement(HotfixStatements.SEL_TAXI_PATH_NODE, "SELECT LocX, LocY, LocZ, ID, PathID, NodeIndex, ContinentID, Flags, Delay, ArrivalEventID, " +
                "DepartureEventID FROM taxi_path_node WHERE (`VerifiedBuild` > 0) = ?");

            // TotemCategory.db2
            PrepareStatement(HotfixStatements.SEL_TOTEM_CATEGORY, "SELECT ID, Name, TotemCategoryType, TotemCategoryMask FROM totem_category" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_TOTEM_CATEGORY_LOCALE, "SELECT ID, Name_lang FROM totem_category_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // Toy.db2
            PrepareStatement(HotfixStatements.SEL_TOY, "SELECT SourceText, ID, ItemID, Flags, SourceTypeEnum FROM toy WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_TOY_LOCALE, "SELECT ID, SourceText_lang FROM toy_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // TransmogHoliday.db2
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_HOLIDAY, "SELECT ID, RequiredTransmogHoliday FROM transmog_holiday WHERE (`VerifiedBuild` > 0) = ?");

            // TraitCond.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_COND, "SELECT ID, CondType, TraitTreeID, GrantedRanks, QuestID, AchievementID, SpecSetID, TraitNodeGroupID, " +
                "TraitNodeID, TraitNodeEntryID, TraitCurrencyID, SpentAmountRequired, Flags, RequiredLevel, FreeSharedStringID, SpendMoreSharedStringID, " +
                "TraitCondAccountElementID FROM trait_cond WHERE (`VerifiedBuild` > 0) = ?");

            // TraitCost.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_COST, "SELECT InternalName, ID, Amount, TraitCurrencyID FROM trait_cost WHERE (`VerifiedBuild` > 0) = ?");

            // TraitCurrency.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_CURRENCY, "SELECT ID, Type, CurrencyTypesID, Flags, Icon FROM trait_currency WHERE (`VerifiedBuild` > 0) = ?");

            // TraitCurrencySource.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_CURRENCY_SOURCE, "SELECT Requirement, ID, TraitCurrencyID, Amount, QuestID, AchievementID, PlayerLevel, " +
                "TraitNodeEntryID, OrderIndex FROM trait_currency_source WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_TRAIT_CURRENCY_SOURCE_LOCALE, "SELECT ID, Requirement_lang FROM trait_currency_source_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // TraitDefinition.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_DEFINITION, "SELECT OverrideName, OverrideSubtext, OverrideDescription, ID, SpellID, OverrideIcon, " +
                "OverridesSpellID, VisibleSpellID FROM trait_definition WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_TRAIT_DEFINITION_LOCALE, "SELECT ID, OverrideName_lang, OverrideSubtext_lang, OverrideDescription_lang" +
                " FROM trait_definition_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // TraitDefinitionEffectPoints.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_DEFINITION_EFFECT_POINTS, "SELECT ID, TraitDefinitionID, EffectIndex, OperationType, CurveID" +
                " FROM trait_definition_effect_points WHERE (`VerifiedBuild` > 0) = ?");

            // TraitEdge.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_EDGE, "SELECT ID, VisualStyle, LeftTraitNodeID, RightTraitNodeID, Type FROM trait_edge" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TraitNode.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_NODE, "SELECT ID, TraitTreeID, PosX, PosY, Type, Flags, TraitSubTreeID FROM trait_node" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TraitNodeEntry.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_NODE_ENTRY, "SELECT ID, TraitDefinitionID, MaxRanks, NodeEntryType, TraitSubTreeID FROM trait_node_entry" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TraitNodeEntryXTraitCond.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_NODE_ENTRY_X_TRAIT_COND, "SELECT ID, TraitCondID, TraitNodeEntryID FROM trait_node_entry_x_trait_cond" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TraitNodeEntryXTraitCost.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_NODE_ENTRY_X_TRAIT_COST, "SELECT ID, TraitNodeEntryID, TraitCostID FROM trait_node_entry_x_trait_cost" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TraitNodeGroup.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_NODE_GROUP, "SELECT ID, TraitTreeID, Flags FROM trait_node_group WHERE (`VerifiedBuild` > 0) = ?");

            // TraitNodeGroupXTraitCond.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_NODE_GROUP_X_TRAIT_COND, "SELECT ID, TraitCondID, TraitNodeGroupID FROM trait_node_group_x_trait_cond" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TraitNodeGroupXTraitCost.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_NODE_GROUP_X_TRAIT_COST, "SELECT ID, TraitNodeGroupID, TraitCostID FROM trait_node_group_x_trait_cost" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TraitNodeGroupXTraitNode.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_NODE_GROUP_X_TRAIT_NODE, "SELECT ID, TraitNodeGroupID, TraitNodeID, `Index` FROM trait_node_group_x_trait_node" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TraitNodeXTraitCond.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_NODE_X_TRAIT_COND, "SELECT ID, TraitCondID, TraitNodeID FROM trait_node_x_trait_cond" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TraitNodeXTraitCost.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_NODE_X_TRAIT_COST, "SELECT ID, TraitNodeID, TraitCostID FROM trait_node_x_trait_cost" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TraitNodeXTraitNodeEntry.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_NODE_X_TRAIT_NODE_ENTRY, "SELECT ID, TraitNodeID, TraitNodeEntryID, `Index` FROM trait_node_x_trait_node_entry" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TraitSubTree.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_SUB_TREE, "SELECT Name, Description, ID, UiTextureAtlasElementID, TraitTreeID FROM trait_sub_tree" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_TRAIT_SUB_TREE_LOCALE, "SELECT ID, Name_lang, Description_lang FROM trait_sub_tree_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // TraitTree.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_TREE, "SELECT ID, TraitSystemID, Unused1000_1, FirstTraitNodeID, PlayerConditionID, Flags, Unused1000_2, " +
                "Unused1000_3 FROM trait_tree WHERE (`VerifiedBuild` > 0) = ?");

            // TraitTreeLoadout.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_TREE_LOADOUT, "SELECT ID, TraitTreeID, ChrSpecializationID FROM trait_tree_loadout" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TraitTreeLoadoutEntry.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_TREE_LOADOUT_ENTRY, "SELECT ID, TraitTreeLoadoutID, SelectedTraitNodeID, SelectedTraitNodeEntryID, NumPoints, " +
                "OrderIndex FROM trait_tree_loadout_entry WHERE (`VerifiedBuild` > 0) = ?");

            // TraitTreeXTraitCost.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_TREE_X_TRAIT_COST, "SELECT ID, TraitTreeID, TraitCostID FROM trait_tree_x_trait_cost" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TraitTreeXTraitCurrency.db2
            PrepareStatement(HotfixStatements.SEL_TRAIT_TREE_X_TRAIT_CURRENCY, "SELECT ID, `Index`, TraitTreeID, TraitCurrencyID FROM trait_tree_x_trait_currency" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TransmogIllusion.db2
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_ILLUSION, "SELECT ID, UnlockConditionID, TransmogCost, SpellItemEnchantmentID, Flags FROM transmog_illusion" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TransmogSet.db2
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET, "SELECT Name, ID, ClassMask, TrackingQuestID, Flags, TransmogSetGroupID, ItemNameDescriptionID, " +
                "ParentTransmogSetID, Unknown810, ExpansionID, PatchID, UiOrder, PlayerConditionID FROM transmog_set WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET_LOCALE, "SELECT ID, Name_lang FROM transmog_set_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // TransmogSetGroup.db2
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET_GROUP, "SELECT ID, Name FROM transmog_set_group WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET_GROUP_LOCALE, "SELECT ID, Name_lang FROM transmog_set_group_locale WHERE (`VerifiedBuild` > 0) = ?" +
                " AND locale = ?");

            // TransmogSetItem.db2
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET_ITEM, "SELECT ID, TransmogSetID, ItemModifiedAppearanceID, Flags FROM transmog_set_item" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TransportAnimation.db2
            PrepareStatement(HotfixStatements.SEL_TRANSPORT_ANIMATION, "SELECT ID, PosX, PosY, PosZ, SequenceID, TimeIndex, TransportID FROM transport_animation" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // TransportRotation.db2
            PrepareStatement(HotfixStatements.SEL_TRANSPORT_ROTATION, "SELECT ID, Rot1, Rot2, Rot3, Rot4, TimeIndex, GameObjectsID FROM transport_rotation" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // UiMap.db2
            PrepareStatement(HotfixStatements.SEL_UI_MAP, "SELECT Name, ID, ParentUiMapID, Flags, `System`, Type, BountySetID, BountyDisplayLocation, " +
                "VisibilityPlayerConditionID2, VisibilityPlayerConditionID, HelpTextPosition, BkgAtlasID, AlternateUiMapGroup, ContentTuningID, " +
                "AdventureMapTextureKitID, MapArtZoneTextPosition FROM ui_map WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_UI_MAP_LOCALE, "SELECT ID, Name_lang FROM ui_map_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // UiMapAssignment.db2
            PrepareStatement(HotfixStatements.SEL_UI_MAP_ASSIGNMENT, "SELECT UiMinX, UiMinY, UiMaxX, UiMaxY, Region1X, Region1Y, Region1Z, Region2X, Region2Y, " +
                "Region2Z, ID, UiMapID, OrderIndex, MapID, AreaID, WmoDoodadPlacementID, WmoGroupID FROM ui_map_assignment WHERE (`VerifiedBuild` > 0) = ?");

            // UiMapLink.db2
            PrepareStatement(HotfixStatements.SEL_UI_MAP_LINK, "SELECT UiMinX, UiMinY, UiMaxX, UiMaxY, ID, ParentUiMapID, OrderIndex, ChildUiMapID, PlayerConditionID, " +
                "OverrideHighlightFileDataID, OverrideHighlightAtlasID, Flags FROM ui_map_link WHERE (`VerifiedBuild` > 0) = ?");

            // UiMapXMapArt.db2
            PrepareStatement(HotfixStatements.SEL_UI_MAP_X_MAP_ART, "SELECT ID, PhaseID, UiMapArtID, UiMapID FROM ui_map_x_map_art WHERE (`VerifiedBuild` > 0) = ?");

            // UiSplashScreen.db2
            PrepareStatement(HotfixStatements.SEL_UI_SPLASH_SCREEN, "SELECT ID, Header, TopLeftFeatureTitle, TopLeftFeatureDesc, BottomLeftFeatureTitle, " +
                "BottomLeftFeatureDesc, RightFeatureTitle, RightFeatureDesc, AllianceQuestID, HordeQuestID, ScreenType, TextureKitID, SoundKitID, " +
                "PlayerConditionID, CharLevelConditionID, RequiredTimeEventPassed FROM ui_splash_screen WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_UI_SPLASH_SCREEN_LOCALE, "SELECT ID, Header_lang, TopLeftFeatureTitle_lang, TopLeftFeatureDesc_lang, " +
                "BottomLeftFeatureTitle_lang, BottomLeftFeatureDesc_lang, RightFeatureTitle_lang, RightFeatureDesc_lang FROM ui_splash_screen_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // UnitCondition.db2
            PrepareStatement(HotfixStatements.SEL_UNIT_CONDITION, "SELECT ID, Flags, Variable1, Variable2, Variable3, Variable4, Variable5, Variable6, Variable7, " +
                "Variable8, Op1, Op2, Op3, Op4, Op5, Op6, Op7, Op8, Value1, Value2, Value3, Value4, Value5, Value6, Value7, Value8 FROM unit_condition" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // UnitPowerBar.db2
            PrepareStatement(HotfixStatements.SEL_UNIT_POWER_BAR, "SELECT ID, Name, Cost, OutOfError, ToolTip, MinPower, MaxPower, StartPower, CenterPower, " +
                "RegenerationPeace, RegenerationCombat, BarType, Flags, StartInset, EndInset, FileDataID1, FileDataID2, FileDataID3, FileDataID4, " +
                "FileDataID5, FileDataID6, Color1, Color2, Color3, Color4, Color5, Color6 FROM unit_power_bar WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_UNIT_POWER_BAR_LOCALE, "SELECT ID, Name_lang, Cost_lang, OutOfError_lang, ToolTip_lang FROM unit_power_bar_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // Vehicle.db2
            PrepareStatement(HotfixStatements.SEL_VEHICLE, "SELECT ID, Flags, FlagsB, TurnSpeed, PitchSpeed, PitchMin, PitchMax, MouseLookOffsetPitch, " +
                "CameraFadeDistScalarMin, CameraFadeDistScalarMax, CameraPitchOffset, FacingLimitRight, FacingLimitLeft, CameraYawOffset, " +
                "VehicleUIIndicatorID, MissileTargetingID, VehiclePOITypeID, SeatID1, SeatID2, SeatID3, SeatID4, SeatID5, SeatID6, SeatID7, SeatID8, " +
                "PowerDisplayID1, PowerDisplayID2, PowerDisplayID3 FROM vehicle WHERE (`VerifiedBuild` > 0) = ?");

            // VehicleSeat.db2
            PrepareStatement(HotfixStatements.SEL_VEHICLE_SEAT, "SELECT ID, AttachmentOffsetX, AttachmentOffsetY, AttachmentOffsetZ, CameraOffsetX, CameraOffsetY, " +
                "CameraOffsetZ, Flags, FlagsB, FlagsC, AttachmentID, EnterPreDelay, EnterSpeed, EnterGravity, EnterMinDuration, EnterMaxDuration, " +
                "EnterMinArcHeight, EnterMaxArcHeight, EnterAnimStart, EnterAnimLoop, RideAnimStart, RideAnimLoop, RideUpperAnimStart, RideUpperAnimLoop, " +
                "ExitPreDelay, ExitSpeed, ExitGravity, ExitMinDuration, ExitMaxDuration, ExitMinArcHeight, ExitMaxArcHeight, ExitAnimStart, ExitAnimLoop, " +
                "ExitAnimEnd, VehicleEnterAnim, VehicleEnterAnimBone, VehicleExitAnim, VehicleExitAnimBone, VehicleRideAnimLoop, VehicleRideAnimLoopBone, " +
                "PassengerAttachmentID, PassengerYaw, PassengerPitch, PassengerRoll, VehicleEnterAnimDelay, VehicleExitAnimDelay, VehicleAbilityDisplay, " +
                "EnterUISoundID, ExitUISoundID, UiSkinFileDataID, CameraEnteringDelay, CameraEnteringDuration, CameraExitingDelay, CameraExitingDuration, " +
                "CameraPosChaseRate, CameraFacingChaseRate, CameraEnteringZoom, CameraSeatZoomMin, CameraSeatZoomMax, EnterAnimKitID, RideAnimKitID, " +
                "ExitAnimKitID, VehicleEnterAnimKitID, VehicleRideAnimKitID, VehicleExitAnimKitID, CameraModeID FROM vehicle_seat" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // Vignette.db2
            PrepareStatement(HotfixStatements.SEL_VIGNETTE, "SELECT ID, Name, PlayerConditionID, VisibleTrackingQuestID, QuestFeedbackEffectID, Flags, MaxHeight, " +
                "MinHeight, VignetteType, RewardQuestID, UiWidgetSetID, UiMapPinInfoID, ObjectiveType FROM vignette WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_VIGNETTE_LOCALE, "SELECT ID, Name_lang FROM vignette_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // WarbandScene.db2
            PrepareStatement(HotfixStatements.SEL_WARBAND_SCENE, "SELECT Name, Description, Source, PositionX, PositionY, PositionZ, LookAtX, LookAtY, LookAtZ, ID, " +
                "MapID, Fov, TimeOfDay, Flags, SoundAmbienceID, Quality, TextureKit, DefaultScenePriority, SourceType FROM warband_scene" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_WARBAND_SCENE_LOCALE, "SELECT ID, Name_lang, Description_lang, Source_lang FROM warband_scene_locale" +
                " WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // WmoAreaTable.db2
            PrepareStatement(HotfixStatements.SEL_WMO_AREA_TABLE, "SELECT AreaName, ID, WmoID, NameSetID, WmoGroupID, SoundProviderPref, SoundProviderPrefUnderwater, " +
                "AmbienceID, UwAmbience, ZoneMusic, UwZoneMusic, IntroSound, UwIntroSound, AreaTableID, Flags FROM wmo_area_table" +
                " WHERE (`VerifiedBuild` > 0) = ?");
            PrepareStatement(HotfixStatements.SEL_WMO_AREA_TABLE_LOCALE, "SELECT ID, AreaName_lang FROM wmo_area_table_locale WHERE (`VerifiedBuild` > 0) = ? AND locale = ?");

            // WorldEffect.db2
            PrepareStatement(HotfixStatements.SEL_WORLD_EFFECT, "SELECT ID, QuestFeedbackEffectID, WhenToDisplay, TargetType, TargetAsset, PlayerConditionID, " +
                "CombatConditionID FROM world_effect WHERE (`VerifiedBuild` > 0) = ?");

            // WorldMapOverlay.db2
            PrepareStatement(HotfixStatements.SEL_WORLD_MAP_OVERLAY, "SELECT ID, UiMapArtID, TextureWidth, TextureHeight, OffsetX, OffsetY, HitRectTop, HitRectBottom, " +
                "HitRectLeft, HitRectRight, PlayerConditionID, Flags, AreaID1, AreaID2, AreaID3, AreaID4 FROM world_map_overlay" +
                " WHERE (`VerifiedBuild` > 0) = ?");

            // WorldStateExpression.db2
            PrepareStatement(HotfixStatements.SEL_WORLD_STATE_EXPRESSION, "SELECT ID, Expression FROM world_state_expression WHERE (`VerifiedBuild` > 0) = ?");
        }
    }

    public enum HotfixStatements
    {
        None = 0,

        SEL_ACHIEVEMENT,
        SEL_ACHIEVEMENT_LOCALE,

        SEL_ACHIEVEMENT_CATEGORY,
        SEL_ACHIEVEMENT_CATEGORY_LOCALE,

        SEL_ADVENTURE_JOURNAL,
        SEL_ADVENTURE_JOURNAL_LOCALE,

        SEL_ADVENTURE_MAP_POI,
        SEL_ADVENTURE_MAP_POI_LOCALE,

        SEL_ANIMATION_DATA,

        SEL_ANIM_KIT,

        SEL_AREA_GROUP_MEMBER,

        SEL_AREA_TABLE,
        SEL_AREA_TABLE_LOCALE,

        SEL_AREA_TRIGGER,

        SEL_AREA_TRIGGER_ACTION_SET,

        SEL_ARMOR_LOCATION,

        SEL_ARTIFACT,
        SEL_ARTIFACT_LOCALE,

        SEL_ARTIFACT_APPEARANCE,
        SEL_ARTIFACT_APPEARANCE_LOCALE,

        SEL_ARTIFACT_APPEARANCE_SET,
        SEL_ARTIFACT_APPEARANCE_SET_LOCALE,

        SEL_ARTIFACT_CATEGORY,

        SEL_ARTIFACT_POWER,

        SEL_ARTIFACT_POWER_LINK,

        SEL_ARTIFACT_POWER_PICKER,

        SEL_ARTIFACT_POWER_RANK,

        SEL_ARTIFACT_QUEST_XP,

        SEL_ARTIFACT_TIER,

        SEL_ARTIFACT_UNLOCK,

        SEL_AUCTION_HOUSE,
        SEL_AUCTION_HOUSE_LOCALE,

        SEL_AZERITE_EMPOWERED_ITEM,

        SEL_AZERITE_ESSENCE,
        SEL_AZERITE_ESSENCE_LOCALE,

        SEL_AZERITE_ESSENCE_POWER,
        SEL_AZERITE_ESSENCE_POWER_LOCALE,

        SEL_AZERITE_ITEM,

        SEL_AZERITE_ITEM_MILESTONE_POWER,

        SEL_AZERITE_KNOWLEDGE_MULTIPLIER,

        SEL_AZERITE_LEVEL_INFO,

        SEL_AZERITE_POWER,

        SEL_AZERITE_POWER_SET_MEMBER,

        SEL_AZERITE_TIER_UNLOCK,

        SEL_AZERITE_TIER_UNLOCK_SET,

        SEL_AZERITE_UNLOCK_MAPPING,

        SEL_BANK_BAG_SLOT_PRICES,

        SEL_BANNED_ADDONS,

        SEL_BARBER_SHOP_STYLE,
        SEL_BARBER_SHOP_STYLE_LOCALE,

        SEL_BATTLE_PET_BREED_QUALITY,

        SEL_BATTLE_PET_BREED_STATE,

        SEL_BATTLE_PET_SPECIES,
        SEL_BATTLE_PET_SPECIES_LOCALE,

        SEL_BATTLE_PET_SPECIES_STATE,

        SEL_BATTLEMASTER_LIST,
        SEL_BATTLEMASTER_LIST_LOCALE,

        SEL_BATTLEMASTER_LIST_X_MAP,

        SEL_BROADCAST_TEXT,
        SEL_BROADCAST_TEXT_LOCALE,

        SEL_BROADCAST_TEXT_DURATION,

        SEL_CFG_CATEGORIES,
        SEL_CFG_CATEGORIES_LOCALE,

        SEL_CFG_REGIONS,

        SEL_CHALLENGE_MODE_ITEM_BONUS_OVERRIDE,

        SEL_CHAR_BASE_INFO,

        SEL_CHAR_TITLES,
        SEL_CHAR_TITLES_LOCALE,

        SEL_CHARACTER_LOADOUT,

        SEL_CHARACTER_LOADOUT_ITEM,

        SEL_CHAT_CHANNELS,
        SEL_CHAT_CHANNELS_LOCALE,

        SEL_CHR_CLASS_UI_DISPLAY,

        SEL_CHR_CLASSES,
        SEL_CHR_CLASSES_LOCALE,

        SEL_CHR_CLASSES_X_POWER_TYPES,

        SEL_CHR_CUSTOMIZATION_CHOICE,
        SEL_CHR_CUSTOMIZATION_CHOICE_LOCALE,

        SEL_CHR_CUSTOMIZATION_DISPLAY_INFO,

        SEL_CHR_CUSTOMIZATION_ELEMENT,

        SEL_CHR_CUSTOMIZATION_OPTION,
        SEL_CHR_CUSTOMIZATION_OPTION_LOCALE,

        SEL_CHR_CUSTOMIZATION_REQ,
        SEL_CHR_CUSTOMIZATION_REQ_LOCALE,

        SEL_CHR_CUSTOMIZATION_REQ_CHOICE,

        SEL_CHR_MODEL,

        SEL_CHR_RACE_X_CHR_MODEL,

        SEL_CHR_RACES,
        SEL_CHR_RACES_LOCALE,

        SEL_CHR_SPECIALIZATION,
        SEL_CHR_SPECIALIZATION_LOCALE,

        SEL_CINEMATIC_CAMERA,

        SEL_CINEMATIC_SEQUENCES,

        SEL_CONDITIONAL_CHR_MODEL,

        SEL_CONDITIONAL_CONTENT_TUNING,

        SEL_CONTENT_TUNING,

        SEL_CONTENT_TUNING_X_EXPECTED,

        SEL_CONTENT_TUNING_X_LABEL,

        SEL_CONVERSATION_LINE,

        SEL_CORRUPTION_EFFECTS,

        SEL_CREATURE_DISPLAY_INFO,

        SEL_CREATURE_DISPLAY_INFO_EXTRA,

        SEL_CREATURE_FAMILY,
        SEL_CREATURE_FAMILY_LOCALE,

        SEL_CREATURE_LABEL,

        SEL_CREATURE_MODEL_DATA,

        SEL_CREATURE_TYPE,
        SEL_CREATURE_TYPE_LOCALE,

        SEL_CRITERIA,

        SEL_CRITERIA_TREE,
        SEL_CRITERIA_TREE_LOCALE,

        SEL_CURRENCY_CONTAINER,
        SEL_CURRENCY_CONTAINER_LOCALE,

        SEL_CURRENCY_TYPES,
        SEL_CURRENCY_TYPES_LOCALE,

        SEL_CURVE,

        SEL_CURVE_POINT,

        SEL_DESTRUCTIBLE_MODEL_DATA,

        SEL_DIFFICULTY,
        SEL_DIFFICULTY_LOCALE,

        SEL_DUNGEON_ENCOUNTER,
        SEL_DUNGEON_ENCOUNTER_LOCALE,

        SEL_DURABILITY_COSTS,

        SEL_DURABILITY_QUALITY,

        SEL_EMOTES,

        SEL_EMOTES_TEXT,

        SEL_EMOTES_TEXT_SOUND,

        SEL_EXPECTED_STAT,

        SEL_EXPECTED_STAT_MOD,

        SEL_FACTION,
        SEL_FACTION_LOCALE,

        SEL_FACTION_TEMPLATE,

        SEL_FLIGHT_CAPABILITY,

        SEL_FRIENDSHIP_REP_REACTION,
        SEL_FRIENDSHIP_REP_REACTION_LOCALE,

        SEL_FRIENDSHIP_REPUTATION,
        SEL_FRIENDSHIP_REPUTATION_LOCALE,

        SEL_GAMEOBJECT_ART_KIT,

        SEL_GAMEOBJECT_DISPLAY_INFO,

        SEL_GAMEOBJECT_LABEL,

        SEL_GAMEOBJECTS,
        SEL_GAMEOBJECTS_LOCALE,

        SEL_GARR_ABILITY,
        SEL_GARR_ABILITY_LOCALE,

        SEL_GARR_BUILDING,
        SEL_GARR_BUILDING_LOCALE,

        SEL_GARR_BUILDING_PLOT_INST,

        SEL_GARR_CLASS_SPEC,
        SEL_GARR_CLASS_SPEC_LOCALE,

        SEL_GARR_FOLLOWER,
        SEL_GARR_FOLLOWER_LOCALE,

        SEL_GARR_FOLLOWER_X_ABILITY,

        SEL_GARR_MISSION,
        SEL_GARR_MISSION_LOCALE,

        SEL_GARR_PLOT,

        SEL_GARR_PLOT_BUILDING,

        SEL_GARR_PLOT_INSTANCE,

        SEL_GARR_SITE_LEVEL,

        SEL_GARR_SITE_LEVEL_PLOT_INST,

        SEL_GARR_TALENT_TREE,
        SEL_GARR_TALENT_TREE_LOCALE,

        SEL_GEM_PROPERTIES,

        SEL_GLOBAL_CURVE,

        SEL_GLYPH_BINDABLE_SPELL,

        SEL_GLYPH_PROPERTIES,

        SEL_GLYPH_REQUIRED_SPEC,

        SEL_GOSSIP_NPC_OPTION,

        SEL_GUILD_COLOR_BACKGROUND,

        SEL_GUILD_COLOR_BORDER,

        SEL_GUILD_COLOR_EMBLEM,

        SEL_GUILD_PERK_SPELLS,

        SEL_HEIRLOOM,
        SEL_HEIRLOOM_LOCALE,

        SEL_HOLIDAYS,

        SEL_IMPORT_PRICE_ARMOR,

        SEL_IMPORT_PRICE_QUALITY,

        SEL_IMPORT_PRICE_SHIELD,

        SEL_IMPORT_PRICE_WEAPON,

        SEL_ITEM,

        SEL_ITEM_APPEARANCE,

        SEL_ITEM_ARMOR_QUALITY,

        SEL_ITEM_ARMOR_SHIELD,

        SEL_ITEM_ARMOR_TOTAL,

        SEL_ITEM_BAG_FAMILY,
        SEL_ITEM_BAG_FAMILY_LOCALE,

        SEL_ITEM_BONUS,

        SEL_ITEM_BONUS_LIST_GROUP_ENTRY,

        SEL_ITEM_BONUS_LIST_LEVEL_DELTA,

        SEL_ITEM_BONUS_TREE,

        SEL_ITEM_BONUS_TREE_NODE,

        SEL_ITEM_CHILD_EQUIPMENT,

        SEL_ITEM_CLASS,
        SEL_ITEM_CLASS_LOCALE,

        SEL_ITEM_CONTEXT_PICKER_ENTRY,

        SEL_ITEM_CURRENCY_COST,

        SEL_ITEM_DAMAGE_AMMO,

        SEL_ITEM_DAMAGE_ONE_HAND,

        SEL_ITEM_DAMAGE_ONE_HAND_CASTER,

        SEL_ITEM_DAMAGE_TWO_HAND,

        SEL_ITEM_DAMAGE_TWO_HAND_CASTER,

        SEL_ITEM_DISENCHANT_LOOT,

        SEL_ITEM_EFFECT,

        SEL_ITEM_EXTENDED_COST,

        SEL_ITEM_LEVEL_SELECTOR,

        SEL_ITEM_LEVEL_SELECTOR_QUALITY,

        SEL_ITEM_LEVEL_SELECTOR_QUALITY_SET,

        SEL_ITEM_LIMIT_CATEGORY,
        SEL_ITEM_LIMIT_CATEGORY_LOCALE,

        SEL_ITEM_LIMIT_CATEGORY_CONDITION,

        SEL_ITEM_MODIFIED_APPEARANCE,

        SEL_ITEM_MODIFIED_APPEARANCE_EXTRA,

        SEL_ITEM_NAME_DESCRIPTION,
        SEL_ITEM_NAME_DESCRIPTION_LOCALE,

        SEL_ITEM_PRICE_BASE,

        SEL_ITEM_SEARCH_NAME,
        SEL_ITEM_SEARCH_NAME_LOCALE,

        SEL_ITEM_SET,
        SEL_ITEM_SET_LOCALE,

        SEL_ITEM_SET_SPELL,

        SEL_ITEM_SPARSE,
        SEL_ITEM_SPARSE_LOCALE,

        SEL_ITEM_SPEC,

        SEL_ITEM_SPEC_OVERRIDE,

        SEL_ITEM_X_BONUS_TREE,

        SEL_ITEM_X_ITEM_EFFECT,

        SEL_JOURNAL_ENCOUNTER,
        SEL_JOURNAL_ENCOUNTER_LOCALE,

        SEL_JOURNAL_ENCOUNTER_SECTION,
        SEL_JOURNAL_ENCOUNTER_SECTION_LOCALE,

        SEL_JOURNAL_INSTANCE,
        SEL_JOURNAL_INSTANCE_LOCALE,

        SEL_JOURNAL_TIER,
        SEL_JOURNAL_TIER_LOCALE,

        SEL_KEYCHAIN,

        SEL_KEYSTONE_AFFIX,
        SEL_KEYSTONE_AFFIX_LOCALE,

        SEL_LANGUAGE_WORDS,

        SEL_LANGUAGES,
        SEL_LANGUAGES_LOCALE,

        SEL_LFG_DUNGEONS,
        SEL_LFG_DUNGEONS_LOCALE,

        SEL_LIGHT,

        SEL_LIQUID_TYPE,

        SEL_LOCATION,

        SEL_LOCK,

        SEL_MAIL_TEMPLATE,
        SEL_MAIL_TEMPLATE_LOCALE,

        SEL_MAP,
        SEL_MAP_LOCALE,

        SEL_MAP_CHALLENGE_MODE,
        SEL_MAP_CHALLENGE_MODE_LOCALE,

        SEL_MAP_DIFFICULTY,
        SEL_MAP_DIFFICULTY_LOCALE,

        SEL_MAP_DIFFICULTY_X_CONDITION,
        SEL_MAP_DIFFICULTY_X_CONDITION_LOCALE,

        SEL_MAW_POWER,

        SEL_MODIFIER_TREE,

        SEL_MOUNT,
        SEL_MOUNT_LOCALE,

        SEL_MOUNT_CAPABILITY,

        SEL_MOUNT_EQUIPMENT,

        SEL_MOUNT_TYPE_X_CAPABILITY,

        SEL_MOUNT_X_DISPLAY,

        SEL_MOVIE,

        SEL_MYTHIC_PLUS_SEASON,

        SEL_NAME_GEN,

        SEL_NAMES_PROFANITY,

        SEL_NAMES_RESERVED,

        SEL_NAMES_RESERVED_LOCALE,

        SEL_NUM_TALENTS_AT_LEVEL,

        SEL_OVERRIDE_SPELL_DATA,

        SEL_PARAGON_REPUTATION,

        SEL_PATH,

        SEL_PATH_NODE,

        SEL_PATH_PROPERTY,

        SEL_PHASE,

        SEL_PHASE_X_PHASE_GROUP,

        SEL_PLAYER_CONDITION,
        SEL_PLAYER_CONDITION_LOCALE,

        SEL_POWER_DISPLAY,

        SEL_POWER_TYPE,

        SEL_PRESTIGE_LEVEL_INFO,
        SEL_PRESTIGE_LEVEL_INFO_LOCALE,

        SEL_PVP_DIFFICULTY,

        SEL_PVP_ITEM,

        SEL_PVP_STAT,
        SEL_PVP_STAT_LOCALE,

        SEL_PVP_SEASON,

        SEL_PVP_TALENT,
        SEL_PVP_TALENT_LOCALE,

        SEL_PVP_TALENT_CATEGORY,

        SEL_PVP_TALENT_SLOT_UNLOCK,

        SEL_PVP_TIER,
        SEL_PVP_TIER_LOCALE,

        SEL_QUEST_FACTION_REWARD,

        SEL_QUEST_INFO,
        SEL_QUEST_INFO_LOCALE,

        SEL_QUEST_LINE_X_QUEST,

        SEL_QUEST_MONEY_REWARD,

        SEL_QUEST_PACKAGE_ITEM,

        SEL_QUEST_SORT,
        SEL_QUEST_SORT_LOCALE,

        SEL_QUEST_V2,

        SEL_QUEST_XP,

        SEL_RAND_PROP_POINTS,

        SEL_REWARD_PACK,

        SEL_REWARD_PACK_X_CURRENCY_TYPE,

        SEL_REWARD_PACK_X_ITEM,

        SEL_SCENARIO,
        SEL_SCENARIO_LOCALE,

        SEL_SCENARIO_STEP,
        SEL_SCENARIO_STEP_LOCALE,

        SEL_SCENE_SCRIPT,

        SEL_SCENE_SCRIPT_GLOBAL_TEXT,

        SEL_SCENE_SCRIPT_PACKAGE,

        SEL_SCENE_SCRIPT_TEXT,

        SEL_SERVER_MESSAGES,
        SEL_SERVER_MESSAGES_LOCALE,

        SEL_SKILL_LINE,
        SEL_SKILL_LINE_LOCALE,

        SEL_SKILL_LINE_ABILITY,
        SEL_SKILL_LINE_ABILITY_LOCALE,

        SEL_SKILL_LINE_X_TRAIT_TREE,

        SEL_SKILL_RACE_CLASS_INFO,

        SEL_SOULBIND_CONDUIT_RANK,

        SEL_SOUND_KIT,

        SEL_SPECIALIZATION_SPELLS,
        SEL_SPECIALIZATION_SPELLS_LOCALE,

        SEL_SPEC_SET_MEMBER,

        SEL_SPELL_AURA_OPTIONS,

        SEL_SPELL_AURA_RESTRICTIONS,

        SEL_SPELL_CAST_TIMES,

        SEL_SPELL_CASTING_REQUIREMENTS,

        SEL_SPELL_CATEGORIES,

        SEL_SPELL_CATEGORY,
        SEL_SPELL_CATEGORY_LOCALE,

        SEL_SPELL_CLASS_OPTIONS,

        SEL_SPELL_COOLDOWNS,

        SEL_SPELL_DURATION,

        SEL_SPELL_EFFECT,

        SEL_SPELL_EMPOWER,

        SEL_SPELL_EMPOWER_STAGE,

        SEL_SPELL_EQUIPPED_ITEMS,

        SEL_SPELL_FOCUS_OBJECT,
        SEL_SPELL_FOCUS_OBJECT_LOCALE,

        SEL_SPELL_INTERRUPTS,

        SEL_SPELL_ITEM_ENCHANTMENT,
        SEL_SPELL_ITEM_ENCHANTMENT_LOCALE,

        SEL_SPELL_ITEM_ENCHANTMENT_CONDITION,

        SEL_SPELL_KEYBOUND_OVERRIDE,

        SEL_SPELL_LABEL,

        SEL_SPELL_LEARN_SPELL,

        SEL_SPELL_LEVELS,

        SEL_SPELL_MISC,

        SEL_SPELL_NAME,
        SEL_SPELL_NAME_LOCALE,

        SEL_SPELL_POWER,

        SEL_SPELL_POWER_DIFFICULTY,

        SEL_SPELL_PROCS_PER_MINUTE,

        SEL_SPELL_PROCS_PER_MINUTE_MOD,

        SEL_SPELL_RADIUS,

        SEL_SPELL_RANGE,
        SEL_SPELL_RANGE_LOCALE,

        SEL_SPELL_REAGENTS,

        SEL_SPELL_REAGENTS_CURRENCY,

        SEL_SPELL_SCALING,

        SEL_SPELL_SHAPESHIFT,

        SEL_SPELL_SHAPESHIFT_FORM,
        SEL_SPELL_SHAPESHIFT_FORM_LOCALE,

        SEL_SPELL_TARGET_RESTRICTIONS,

        SEL_SPELL_TOTEMS,

        SEL_SPELL_VISUAL,

        SEL_SPELL_VISUAL_EFFECT_NAME,

        SEL_SPELL_VISUAL_KIT,

        SEL_SPELL_VISUAL_MISSILE,

        SEL_SPELL_X_SPELL_VISUAL,

        SEL_SUMMON_PROPERTIES,

        SEL_TACT_KEY,

        SEL_TALENT,
        SEL_TALENT_LOCALE,

        SEL_TAXI_NODES,
        SEL_TAXI_NODES_LOCALE,

        SEL_TAXI_PATH,

        SEL_TAXI_PATH_NODE,

        SEL_TOTEM_CATEGORY,
        SEL_TOTEM_CATEGORY_LOCALE,

        SEL_TOY,
        SEL_TOY_LOCALE,

        SEL_TRANSMOG_HOLIDAY,

        SEL_TRAIT_COND,

        SEL_TRAIT_COST,

        SEL_TRAIT_CURRENCY,

        SEL_TRAIT_CURRENCY_SOURCE,
        SEL_TRAIT_CURRENCY_SOURCE_LOCALE,

        SEL_TRAIT_DEFINITION,
        SEL_TRAIT_DEFINITION_LOCALE,

        SEL_TRAIT_DEFINITION_EFFECT_POINTS,

        SEL_TRAIT_EDGE,

        SEL_TRAIT_NODE,

        SEL_TRAIT_NODE_ENTRY,

        SEL_TRAIT_NODE_ENTRY_X_TRAIT_COND,

        SEL_TRAIT_NODE_ENTRY_X_TRAIT_COST,

        SEL_TRAIT_NODE_GROUP,

        SEL_TRAIT_NODE_GROUP_X_TRAIT_COND,

        SEL_TRAIT_NODE_GROUP_X_TRAIT_COST,

        SEL_TRAIT_NODE_GROUP_X_TRAIT_NODE,

        SEL_TRAIT_NODE_X_TRAIT_COND,

        SEL_TRAIT_NODE_X_TRAIT_COST,

        SEL_TRAIT_NODE_X_TRAIT_NODE_ENTRY,

        SEL_TRAIT_SUB_TREE,
        SEL_TRAIT_SUB_TREE_LOCALE,

        SEL_TRAIT_TREE,

        SEL_TRAIT_TREE_LOADOUT,

        SEL_TRAIT_TREE_LOADOUT_ENTRY,

        SEL_TRAIT_TREE_X_TRAIT_COST,

        SEL_TRAIT_TREE_X_TRAIT_CURRENCY,

        SEL_TRANSMOG_ILLUSION,

        SEL_TRANSMOG_SET,
        SEL_TRANSMOG_SET_LOCALE,

        SEL_TRANSMOG_SET_GROUP,
        SEL_TRANSMOG_SET_GROUP_LOCALE,

        SEL_TRANSMOG_SET_ITEM,

        SEL_TRANSPORT_ANIMATION,

        SEL_TRANSPORT_ROTATION,

        SEL_UI_MAP,
        SEL_UI_MAP_LOCALE,

        SEL_UI_MAP_ASSIGNMENT,

        SEL_UI_MAP_LINK,

        SEL_UI_MAP_X_MAP_ART,

        SEL_UI_SPLASH_SCREEN,
        SEL_UI_SPLASH_SCREEN_LOCALE,

        SEL_UNIT_CONDITION,

        SEL_UNIT_POWER_BAR,
        SEL_UNIT_POWER_BAR_LOCALE,

        SEL_VEHICLE,

        SEL_VEHICLE_SEAT,

        SEL_VIGNETTE,
        SEL_VIGNETTE_LOCALE,

        SEL_WARBAND_SCENE,
        SEL_WARBAND_SCENE_LOCALE,

        SEL_WMO_AREA_TABLE,
        SEL_WMO_AREA_TABLE_LOCALE,

        SEL_WORLD_EFFECT,

        SEL_WORLD_MAP_OVERLAY,

        SEL_WORLD_STATE_EXPRESSION,

        MAX_HOTFIXDATABASE_STATEMENTS
    }
}
