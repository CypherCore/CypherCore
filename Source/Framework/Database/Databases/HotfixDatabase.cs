﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

namespace Framework.Database
{
    public class HotfixDatabase : MySqlBase<HotfixStatements>
    {
        public override void PreparedStatements()
        {
            // Achievement.db2
            PrepareStatement(HotfixStatements.SEL_ACHIEVEMENT, "SELECT Description, Title, Reward, ID, InstanceID, Faction, Supercedes, Category, MinimumCriteria, " +
                "Points, Flags, UiOrder, IconFileID, RewardItemID, CriteriaTree, SharesCriteria, CovenantID FROM achievement");
            PrepareStatement(HotfixStatements.SEL_ACHIEVEMENT_LOCALE, "SELECT ID, Description_lang, Title_lang, Reward_lang FROM achievement_locale WHERE locale = ?");

            // AchievementCategory.db2
            PrepareStatement(HotfixStatements.SEL_ACHIEVEMENT_CATEGORY, "SELECT Name, ID, Parent, UiOrder FROM achievement_category");
            PrepareStatement(HotfixStatements.SEL_ACHIEVEMENT_CATEGORY_LOCALE, "SELECT ID, Name_lang FROM achievement_category_locale WHERE locale = ?");

            // AdventureJournal.db2
            PrepareStatement(HotfixStatements.SEL_ADVENTURE_JOURNAL, "SELECT ID, Name, Description, ButtonText, RewardDescription, ContinueDescription, Type, " +
                "PlayerConditionID, Flags, ButtonActionType, TextureFileDataID, LfgDungeonID, QuestID, BattleMasterListID, PriorityMin, PriorityMax, ItemID, " +
                "ItemQuantity, CurrencyType, CurrencyQuantity, UiMapID, BonusPlayerConditionID1, BonusPlayerConditionID2, BonusValue1, BonusValue2 FROM adventure_journal");
            PrepareStatement(HotfixStatements.SEL_ADVENTURE_JOURNAL_LOCALE, "SELECT ID, Name_lang, Description_lang, ButtonText_lang, RewardDescription_lang, " +
                "ContinueDescription_lang FROM adventure_journal_locale WHERE locale = ?");

            // AdventureMapPoi.db2
            PrepareStatement(HotfixStatements.SEL_ADVENTURE_MAP_POI, "SELECT ID, Title, Description, WorldPositionX, WorldPositionY, Type, PlayerConditionID, QuestID, " +
                "LfgDungeonID, RewardItemID, UiTextureAtlasMemberID, UiTextureKitID, MapID, AreaTableID FROM adventure_map_poi");
            PrepareStatement(HotfixStatements.SEL_ADVENTURE_MAP_POI_LOCALE, "SELECT ID, Title_lang, Description_lang FROM adventure_map_poi_locale WHERE locale = ?");

            // AnimationData.db2
            PrepareStatement(HotfixStatements.SEL_ANIMATION_DATA, "SELECT ID, Fallback, BehaviorTier, BehaviorID, Flags1, Flags2 FROM animation_data");

            // AnimKit.db2
            PrepareStatement(HotfixStatements.SEL_ANIM_KIT, "SELECT ID, OneShotDuration, OneShotStopAnimKitID, LowDefAnimKitID FROM anim_kit");

            // AreaGroupMember.db2
            PrepareStatement(HotfixStatements.SEL_AREA_GROUP_MEMBER, "SELECT ID, AreaID, AreaGroupID FROM area_group_member");

            // AreaTable.db2
            PrepareStatement(HotfixStatements.SEL_AREA_TABLE, "SELECT ID, ZoneName, AreaName, ContinentID, ParentAreaID, AreaBit, SoundProviderPref, " +
                "SoundProviderPrefUnderwater, AmbienceID, UwAmbience, ZoneMusic, UwZoneMusic, ExplorationLevel, IntroSound, UwIntroSound, FactionGroupMask, " +
                "AmbientMultiplier, MountFlags, PvpCombatWorldStateID, WildBattlePetLevelMin, WildBattlePetLevelMax, WindSettingsID, Flags1, Flags2, " +
                "LiquidTypeID1, LiquidTypeID2, LiquidTypeID3, LiquidTypeID4 FROM area_table");
            PrepareStatement(HotfixStatements.SEL_AREA_TABLE_LOCALE, "SELECT ID, AreaName_lang FROM area_table_locale WHERE locale = ?");

            // AreaTrigger.db2            
            PrepareStatement(HotfixStatements.SEL_AREA_TRIGGER, "SELECT Message, PosX, PosY, PosZ, ID, ContinentID, PhaseUseFlags, PhaseID, PhaseGroupID, Radius, " +
                "BoxLength, BoxWidth, BoxHeight, BoxYaw, ShapeType, ShapeID, AreaTriggerActionSetID, Flags FROM area_trigger");
            PrepareStatement(HotfixStatements.SEL_AREA_TRIGGER_LOCALE, "SELECT ID, Message_lang FROM area_trigger_locale WHERE locale = ?");

            // ArmorLocation.db2
            PrepareStatement(HotfixStatements.SEL_ARMOR_LOCATION, "SELECT ID, Clothmodifier, Leathermodifier, Chainmodifier, Platemodifier, Modifier FROM armor_location");

            // Artifact.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT, "SELECT Name, ID, UiTextureKitID, UiNameColor, UiBarOverlayColor, UiBarBackgroundColor, " +
                "ChrSpecializationID, Flags, ArtifactCategoryID, UiModelSceneID, SpellVisualKitID FROM artifact");
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_LOCALE, "SELECT ID, Name_lang FROM artifact_locale WHERE locale = ?");

            // ArtifactAppearance.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_APPEARANCE, "SELECT Name, ID, ArtifactAppearanceSetID, DisplayIndex, UnlockPlayerConditionID, " +
                "ItemAppearanceModifierID, UiSwatchColor, UiModelSaturation, UiModelOpacity, OverrideShapeshiftFormID, OverrideShapeshiftDisplayID, " +
                "UiItemAppearanceID, UiAltItemAppearanceID, Flags, UiCameraID FROM artifact_appearance");
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_APPEARANCE_LOCALE, "SELECT ID, Name_lang FROM artifact_appearance_locale WHERE locale = ?");

            // ArtifactAppearanceSet.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET, "SELECT Name, Description, ID, DisplayIndex, UiCameraID, AltHandUICameraID, " +
                "ForgeAttachmentOverride, Flags, ArtifactID FROM artifact_appearance_set");
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET_LOCALE, "SELECT ID, Name_lang, Description_lang FROM artifact_appearance_set_locale" +
                " WHERE locale = ?");

            // ArtifactCategory.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_CATEGORY, "SELECT ID, XpMultCurrencyID, XpMultCurveID FROM artifact_category");

            // ArtifactPower.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_POWER, "SELECT DisplayPosX, DisplayPosY, ID, ArtifactID, MaxPurchasableRank, Label, Flags, Tier" +
                " FROM artifact_power");

            // ArtifactPowerLink.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_POWER_LINK, "SELECT ID, PowerA, PowerB FROM artifact_power_link");

            // ArtifactPowerPicker.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_POWER_PICKER, "SELECT ID, PlayerConditionID FROM artifact_power_picker");

            // ArtifactPowerRank.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_POWER_RANK, "SELECT ID, RankIndex, SpellID, ItemBonusListID, AuraPointsOverride, ArtifactPowerID" +
                " FROM artifact_power_rank");

            // ArtifactQuestXp.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_QUEST_XP, "SELECT ID, Difficulty1, Difficulty2, Difficulty3, Difficulty4, Difficulty5, Difficulty6, " +
                "Difficulty7, Difficulty8, Difficulty9, Difficulty10 FROM artifact_quest_xp");

            // ArtifactTier.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_TIER, "SELECT ID, ArtifactTier, MaxNumTraits, MaxArtifactKnowledge, KnowledgePlayerCondition, " +
                "MinimumEmpowerKnowledge FROM artifact_tier");

            // ArtifactUnlock.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_UNLOCK, "SELECT ID, PowerID, PowerRank, ItemBonusListID, PlayerConditionID, ArtifactID FROM artifact_unlock");

            // AuctionHouse.db2
            PrepareStatement(HotfixStatements.SEL_AUCTION_HOUSE, "SELECT ID, Name, FactionID, DepositRate, ConsignmentRate FROM auction_house");
            PrepareStatement(HotfixStatements.SEL_AUCTION_HOUSE_LOCALE, "SELECT ID, Name_lang FROM auction_house_locale WHERE locale = ?");

            // AzeriteEmpoweredItem.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_EMPOWERED_ITEM, "SELECT ID, ItemID, AzeriteTierUnlockSetID, AzeritePowerSetID FROM azerite_empowered_item");

            // AzeriteEssence.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_ESSENCE, "SELECT ID, Name, Description, SpecSetID FROM azerite_essence");
            PrepareStatement(HotfixStatements.SEL_AZERITE_ESSENCE_LOCALE, "SELECT ID, Name_lang, Description_lang FROM azerite_essence_locale WHERE locale = ?");

            // AzeriteEssencePower.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_ESSENCE_POWER, "SELECT ID, SourceAlliance, SourceHorde, AzeriteEssenceID, Tier, MajorPowerDescription, " +
                "MinorPowerDescription, MajorPowerActual, MinorPowerActual FROM azerite_essence_power");
            PrepareStatement(HotfixStatements.SEL_AZERITE_ESSENCE_POWER_LOCALE, "SELECT ID, SourceAlliance_lang, SourceHorde_lang FROM azerite_essence_power_locale WHERE locale = ?");

            // AzeriteItem.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_ITEM, "SELECT ID, ItemID FROM azerite_item");

            // AzeriteItemMilestonePower.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_ITEM_MILESTONE_POWER, "SELECT ID, RequiredLevel, AzeritePowerID, Type, AutoUnlock FROM azerite_item_milestone_power");

            // AzeriteKnowledgeMultiplier.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_KNOWLEDGE_MULTIPLIER, "SELECT ID, Multiplier FROM azerite_knowledge_multiplier");

            // AzeriteLevelInfo.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_LEVEL_INFO, "SELECT ID, BaseExperienceToNextLevel, MinimumExperienceToNextLevel, ItemLevel FROM azerite_level_info");

            // AzeritePower.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_POWER, "SELECT ID, SpellID, ItemBonusListID, SpecSetID, Flags FROM azerite_power");

            // AzeritePowerSetMember.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_POWER_SET_MEMBER, "SELECT ID, AzeritePowerSetID, AzeritePowerID, Class, Tier, OrderIndex FROM azerite_power_set_member");

            // AzeriteTierUnlock.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_TIER_UNLOCK, "SELECT ID, ItemCreationContext, Tier, AzeriteLevel, AzeriteTierUnlockSetID FROM azerite_tier_unlock");

            // AzeriteTierUnlockSet.db2
            PrepareStatement(HotfixStatements.SEL_AZERITE_TIER_UNLOCK_SET, "SELECT ID, Flags FROM azerite_tier_unlock_set");

            // BankBagSlotPrices.db2
            PrepareStatement(HotfixStatements.SEL_BANK_BAG_SLOT_PRICES, "SELECT ID, Cost FROM bank_bag_slot_prices");

            // BannedAddons.db2
            PrepareStatement(HotfixStatements.SEL_BANNED_ADDONS, "SELECT ID, Name, Version, Flags FROM banned_addons");

            // BarberShopStyle.db2
            PrepareStatement(HotfixStatements.SEL_BARBER_SHOP_STYLE, "SELECT DisplayName, Description, ID, Type, CostModifier, Race, Sex, Data FROM barber_shop_style");
            PrepareStatement(HotfixStatements.SEL_BARBER_SHOP_STYLE_LOCALE, "SELECT ID, DisplayName_lang, Description_lang FROM barber_shop_style_locale WHERE locale = ?");

            // BattlePetBreedQuality.db2
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_BREED_QUALITY, "SELECT ID, StateMultiplier, QualityEnum FROM battle_pet_breed_quality");

            // BattlePetBreedState.db2
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_BREED_STATE, "SELECT ID, BattlePetStateID, Value, BattlePetBreedID FROM battle_pet_breed_state");

            // BattlePetSpecies.db2
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_SPECIES, "SELECT Description, SourceText, ID, CreatureID, SummonSpellID, IconFileDataID, PetTypeEnum, " +
                "Flags, SourceTypeEnum, CardUIModelSceneID, LoadoutUIModelSceneID FROM battle_pet_species");
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_SPECIES_LOCALE, "SELECT ID, Description_lang, SourceText_lang FROM battle_pet_species_locale WHERE locale = ?");

            // BattlePetSpeciesState.db2
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_SPECIES_STATE, "SELECT ID, BattlePetStateID, Value, BattlePetSpeciesID FROM battle_pet_species_state");

            // BattlemasterList.db2
            PrepareStatement(HotfixStatements.SEL_BATTLEMASTER_LIST, "SELECT ID, Name, GameType, ShortDescription, LongDescription, InstanceType, MinLevel, MaxLevel, " +
                "RatedPlayers, MinPlayers, MaxPlayers, GroupsAllowed, MaxGroupSize, HolidayWorldState, Flags, IconFileDataID FROM battlemaster_list");
            PrepareStatement(HotfixStatements.SEL_BATTLEMASTER_LIST_LOCALE, "SELECT ID, Name_lang, GameType_lang, ShortDescription_lang, LongDescription_lang" +
                " FROM battlemaster_list_locale WHERE locale = ?");

            // BroadcastText.db2
            PrepareStatement(HotfixStatements.SEL_BROADCAST_TEXT, "SELECT `Text`, Text1, ID, LanguageID, ConditionID, EmotesID, Flags, ChatBubbleDurationMs, " +
                "VoiceOverPriorityID, SoundKitID1, SoundKitID2, EmoteID1, EmoteID2, EmoteID3, EmoteDelay1, EmoteDelay2, EmoteDelay3 FROM broadcast_text");
            PrepareStatement(HotfixStatements.SEL_BROADCAST_TEXT_LOCALE, "SELECT ID, Text_lang, Text1_lang FROM broadcast_text_locale WHERE locale = ?");

            // CfgRegions.db2
            PrepareStatement(HotfixStatements.SEL_CFG_REGIONS, "SELECT ID, Tag, RegionID, Raidorigin, RegionGroupMask, ChallengeOrigin FROM cfg_regions");

            // CharTitles.db2
            PrepareStatement(HotfixStatements.SEL_CHAR_TITLES, "SELECT ID, Name, Name1, MaskID, Flags FROM char_titles");
            PrepareStatement(HotfixStatements.SEL_CHAR_TITLES_LOCALE, "SELECT ID, Name_lang, Name1_lang FROM char_titles_locale WHERE locale = ?");

            // CharacterLoadout.db2
            PrepareStatement(HotfixStatements.SEL_CHARACTER_LOADOUT, "SELECT RaceMask, ID, ChrClassID, Purpose, ModID FROM character_loadout");

            // CharacterLoadoutItem.db2
            PrepareStatement(HotfixStatements.SEL_CHARACTER_LOADOUT_ITEM, "SELECT ID, CharacterLoadoutID, ItemID FROM character_loadout_item");

            // ChatChannels.db2
            PrepareStatement(HotfixStatements.SEL_CHAT_CHANNELS, "SELECT Name, Shortcut, ID, Flags, FactionGroup, Ruleset FROM chat_channels");
            PrepareStatement(HotfixStatements.SEL_CHAT_CHANNELS_LOCALE, "SELECT ID, Name_lang, Shortcut_lang FROM chat_channels_locale WHERE locale = ?");

            // ChrClassUiDisplay.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CLASS_UI_DISPLAY, "SELECT ID, ChrClassesID, AdvGuidePlayerConditionID, SplashPlayerConditionID FROM chr_class_ui_display");

            // ChrClasses.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CLASSES, "SELECT Name, Filename, NameMale, NameFemale, PetNameToken, ID, CreateScreenFileDataID, " +
                "SelectScreenFileDataID, IconFileDataID, LowResScreenFileDataID, Flags, StartingLevel, RolesMask, ArmorTypeMask, CinematicSequenceID, " +
                "DefaultSpec, HasStrengthAttackBonus, PrimaryStatPriority, DisplayPower, RangedAttackPowerPerAgility, AttackPowerPerAgility, " +
                "AttackPowerPerStrength, SpellClassSet, DamageBonusStat, HasRelicSlot FROM chr_classes");
            PrepareStatement(HotfixStatements.SEL_CHR_CLASSES_LOCALE, "SELECT ID, Name_lang, NameMale_lang, NameFemale_lang FROM chr_classes_locale" +
                " WHERE locale = ?");

            // ChrClassesXPowerTypes.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CLASSES_X_POWER_TYPES, "SELECT ID, PowerType, ClassID FROM chr_classes_x_power_types");

            // ChrCustomizationChoice.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_CHOICE, "SELECT Name, ID, ChrCustomizationOptionID, ChrCustomizationReqID, SortOrder, UiOrderIndex, " +
                "Flags, AddedInPatch, SwatchColor1, SwatchColor2 FROM chr_customization_choice");
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_CHOICE_LOCALE, "SELECT ID, Name_lang FROM chr_customization_choice_locale WHERE locale = ?");

            // ChrCustomizationDisplayInfo.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_DISPLAY_INFO, "SELECT ID, ShapeshiftFormID, DisplayID, BarberShopMinCameraDistance, " +
                "BarberShopHeightOffset FROM chr_customization_display_info");

            // ChrCustomizationElement.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_ELEMENT, "SELECT ID, ChrCustomizationChoiceID, RelatedChrCustomizationChoiceID, " +
                "ChrCustomizationGeosetID, ChrCustomizationSkinnedModelID, ChrCustomizationMaterialID, ChrCustomizationBoneSetID, " +
                "ChrCustomizationCondModelID, ChrCustomizationDisplayInfoID, ChrCustItemGeoModifyID FROM chr_customization_element");

            // ChrCustomizationOption.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_OPTION, "SELECT Name, ID, SecondaryID, Flags, ChrModelID, SortIndex, ChrCustomizationCategoryID, " +
                "OptionType, BarberShopCostModifier, ChrCustomizationID, Requirement, UiOrderIndex FROM chr_customization_option");
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_OPTION_LOCALE, "SELECT ID, Name_lang FROM chr_customization_option_locale WHERE locale = ?");

            // ChrCustomizationReq.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_REQ, "SELECT ID, Flags, ClassMask, AchievementID, OverrideArchive, ItemModifiedAppearanceID FROM chr_customization_req");

            // ChrCustomizationReqChoice.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CUSTOMIZATION_REQ_CHOICE, "SELECT ID, ChrCustomizationChoiceID, ChrCustomizationReqID FROM chr_customization_req_choice");

            // ChrModel.db2
            PrepareStatement(HotfixStatements.SEL_CHR_MODEL, "SELECT FaceCustomizationOffset1, FaceCustomizationOffset2, FaceCustomizationOffset3, CustomizeOffset1, " +
                "CustomizeOffset2, CustomizeOffset3, ID, Sex, DisplayID, CharComponentTextureLayoutID, Flags, SkeletonFileDataID, ModelFallbackChrModelID, " +
                "TextureFallbackChrModelID, HelmVisFallbackChrModelID, CustomizeScale, CustomizeFacing, CameraDistanceOffset, BarberShopCameraOffsetScale, " +
                "BarberShopCameraHeightOffsetScale, BarberShopCameraRotationOffset FROM chr_model");

            // ChrRaceXChrModel.db2
            PrepareStatement(HotfixStatements.SEL_CHR_RACE_X_CHR_MODEL, "SELECT ID, ChrRacesID, ChrModelID FROM chr_race_x_chr_model");

            // ChrRaces.db2
            PrepareStatement(HotfixStatements.SEL_CHR_RACES, "SELECT ID, ClientPrefix, ClientFileString, Name, NameFemale, NameLowercase, NameFemaleLowercase, " +
                "LoreName, LoreNameFemale, LoreNameLower, LoreNameLowerFemale, LoreDescription, ShortName, ShortNameFemale, ShortNameLower, " +
                "ShortNameLowerFemale, Flags, MaleDisplayID, FemaleDisplayID, HighResMaleDisplayID, HighResFemaleDisplayID, ResSicknessSpellID, " +
                "SplashSoundID, CreateScreenFileDataID, SelectScreenFileDataID, LowResScreenFileDataID, AlteredFormStartVisualKitID1, " +
                "AlteredFormStartVisualKitID2, AlteredFormStartVisualKitID3, AlteredFormFinishVisualKitID1, AlteredFormFinishVisualKitID2, " +
                "AlteredFormFinishVisualKitID3, HeritageArmorAchievementID, StartingLevel, UiDisplayOrder, PlayableRaceBit, FemaleSkeletonFileDataID, " +
                "MaleSkeletonFileDataID, HelmetAnimScalingRaceID, TransmogrifyDisabledSlotMask, AlteredFormCustomizeOffsetFallback1, " +
                "AlteredFormCustomizeOffsetFallback2, AlteredFormCustomizeOffsetFallback3, AlteredFormCustomizeRotationFallback, Unknown910_11, " +
                "Unknown910_12, Unknown910_13, Unknown910_21, Unknown910_22, Unknown910_23, FactionID, CinematicSequenceID, BaseLanguage, CreatureType, " +
                "Alliance, Race_related, UnalteredVisualRaceID, DefaultClassID, NeutralRaceID, MaleModelFallbackRaceID, MaleModelFallbackSex, " +
                "FemaleModelFallbackRaceID, FemaleModelFallbackSex, MaleTextureFallbackRaceID, MaleTextureFallbackSex, FemaleTextureFallbackRaceID, " +
                "FemaleTextureFallbackSex, UnalteredVisualCustomizationRaceID FROM chr_races");
            PrepareStatement(HotfixStatements.SEL_CHR_RACES_LOCALE, "SELECT ID, Name_lang, NameFemale_lang, NameLowercase_lang, NameFemaleLowercase_lang, LoreName_lang, " +
                "LoreNameFemale_lang, LoreNameLower_lang, LoreNameLowerFemale_lang, LoreDescription_lang, ShortName_lang, ShortNameFemale_lang, " +
                "ShortNameLower_lang, ShortNameLowerFemale_lang FROM chr_races_locale WHERE locale = ?");

            // ChrSpecialization.db2
            PrepareStatement(HotfixStatements.SEL_CHR_SPECIALIZATION, "SELECT Name, FemaleName, Description, ID, ClassID, OrderIndex, PetTalentType, Role, Flags, " +
                "SpellIconFileID, PrimaryStatPriority, AnimReplacements, MasterySpellID1, MasterySpellID2 FROM chr_specialization");
            PrepareStatement(HotfixStatements.SEL_CHR_SPECIALIZATION_LOCALE, "SELECT ID, Name_lang, FemaleName_lang, Description_lang FROM chr_specialization_locale" +
                " WHERE locale = ?");

            // CinematicCamera.db2
            PrepareStatement(HotfixStatements.SEL_CINEMATIC_CAMERA, "SELECT ID, OriginX, OriginY, OriginZ, SoundID, OriginFacing, FileDataID FROM cinematic_camera");

            // CinematicSequences.db2
            PrepareStatement(HotfixStatements.SEL_CINEMATIC_SEQUENCES, "SELECT ID, SoundID, Camera1, Camera2, Camera3, Camera4, Camera5, Camera6, Camera7, Camera8 FROM cinematic_sequences");

            // ContentTuning.db2
            PrepareStatement(HotfixStatements.SEL_CONTENT_TUNING, "SELECT ID, MinLevel, MaxLevel, Flags, ExpectedStatModID, DifficultyESMID FROM content_tuning");

            // ConversationLine.db2
            PrepareStatement(HotfixStatements.SEL_CONVERSATION_LINE, "SELECT ID, BroadcastTextID, SpellVisualKitID, AdditionalDuration, NextConversationLineID, " +
                "AnimKitID, SpeechType, StartAnimation, EndAnimation FROM conversation_line");

            // CreatureDisplayInfo.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_DISPLAY_INFO, "SELECT ID, ModelID, SoundID, SizeClass, CreatureModelScale, CreatureModelAlpha, BloodID, " +
                "ExtendedDisplayInfoID, NPCSoundID, ParticleColorID, PortraitCreatureDisplayInfoID, PortraitTextureFileDataID, ObjectEffectPackageID, " +
                "AnimReplacementSetID, Flags, StateSpellVisualKitID, PlayerOverrideScale, PetInstanceScale, UnarmedWeaponType, MountPoofSpellVisualKitID, " +
                "DissolveEffectID, Gender, DissolveOutEffectID, CreatureModelMinLod, TextureVariationFileDataID1, TextureVariationFileDataID2, " +
                "TextureVariationFileDataID3 FROM creature_display_info");

            // CreatureDisplayInfoExtra.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_DISPLAY_INFO_EXTRA, "SELECT ID, DisplayRaceID, DisplaySexID, DisplayClassID, SkinID, FaceID, HairStyleID, " +
                "HairColorID, FacialHairID, Flags, BakeMaterialResourcesID, HDBakeMaterialResourcesID, CustomDisplayOption1, CustomDisplayOption2, " +
                "CustomDisplayOption3 FROM creature_display_info_extra");

            // CreatureFamily.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_FAMILY, "SELECT ID, Name, MinScale, MinScaleLevel, MaxScale, MaxScaleLevel, PetFoodMask, PetTalentType, " +
                "CategoryEnumID, IconFileID, SkillLine1, SkillLine2 FROM creature_family");
            PrepareStatement(HotfixStatements.SEL_CREATURE_FAMILY_LOCALE, "SELECT ID, Name_lang FROM creature_family_locale WHERE locale = ?");

            // CreatureModelData.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_MODEL_DATA, "SELECT ID, GeoBox1, GeoBox2, GeoBox3, GeoBox4, GeoBox5, GeoBox6, Flags, FileDataID, BloodID, " +
                "FootprintTextureID, FootprintTextureLength, FootprintTextureWidth, FootprintParticleScale, FoleyMaterialID, FootstepCameraEffectID, " +
                "DeathThudCameraEffectID, SoundID, SizeClass, CollisionWidth, CollisionHeight, WorldEffectScale, CreatureGeosetDataID, HoverHeight, " +
                "AttachedEffectScale, ModelScale, MissileCollisionRadius, MissileCollisionPush, MissileCollisionRaise, MountHeight, OverrideLootEffectScale, " +
                "OverrideNameScale, OverrideSelectionRadius, TamedPetBaseScale FROM creature_model_data");

            // CreatureType.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_TYPE, "SELECT ID, Name, Flags FROM creature_type");
            PrepareStatement(HotfixStatements.SEL_CREATURE_TYPE_LOCALE, "SELECT ID, Name_lang FROM creature_type_locale WHERE locale = ?");

            // Criteria.db2
            PrepareStatement(HotfixStatements.SEL_CRITERIA, "SELECT ID, Type, Asset, ModifierTreeId, StartEvent, StartAsset, StartTimer, FailEvent, FailAsset, Flags, " +
                "EligibilityWorldStateID, EligibilityWorldStateValue FROM criteria");

            // CriteriaTree.db2
            PrepareStatement(HotfixStatements.SEL_CRITERIA_TREE, "SELECT ID, Description, Parent, Amount, Operator, CriteriaID, OrderIndex, Flags FROM criteria_tree");
            PrepareStatement(HotfixStatements.SEL_CRITERIA_TREE_LOCALE, "SELECT ID, Description_lang FROM criteria_tree_locale WHERE locale = ?");

            // CurrencyContainer.db2
            PrepareStatement(HotfixStatements.SEL_CURRENCY_CONTAINER, "SELECT ID, ContainerName, ContainerDescription, MinAmount, MaxAmount, ContainerIconID, " +
                "ContainerQuality, OnLootSpellVisualKitID, CurrencyTypesID FROM currency_container");
            PrepareStatement(HotfixStatements.SEL_CURRENCY_CONTAINER_LOCALE, "SELECT ID, ContainerName_lang, ContainerDescription_lang FROM currency_container_locale WHERE locale = ?");

            // CurrencyTypes.db2
            PrepareStatement(HotfixStatements.SEL_CURRENCY_TYPES, "SELECT ID, Name, Description, CategoryID, InventoryIconFileID, SpellWeight, SpellCategory, MaxQty, " +
                "MaxEarnablePerWeek, Quality, FactionID, Flags1, Flags2 FROM currency_types");
            PrepareStatement(HotfixStatements.SEL_CURRENCY_TYPES_LOCALE, "SELECT ID, Name_lang, Description_lang FROM currency_types_locale WHERE locale = ?");

            // Curve.db2
            PrepareStatement(HotfixStatements.SEL_CURVE, "SELECT ID, Type, Flags FROM curve");

            // CurvePoint.db2
            PrepareStatement(HotfixStatements.SEL_CURVE_POINT, "SELECT ID, PosX, PosY, CurveID, OrderIndex FROM curve_point");

            // DestructibleModelData.db2
            PrepareStatement(HotfixStatements.SEL_DESTRUCTIBLE_MODEL_DATA, "SELECT ID, State0ImpactEffectDoodadSet, State0AmbientDoodadSet, State1WMO, " +
                "State1DestructionDoodadSet, State1ImpactEffectDoodadSet, State1AmbientDoodadSet, State2WMO, State2DestructionDoodadSet, " +
                "State2ImpactEffectDoodadSet, State2AmbientDoodadSet, State3WMO, State3InitDoodadSet, State3AmbientDoodadSet, EjectDirection, DoNotHighlight, " +
                "State0WMO, HealEffect, HealEffectSpeed, State0NameSet, State1NameSet, State2NameSet, State3NameSet FROM destructible_model_data");

            // Difficulty.db2
            PrepareStatement(HotfixStatements.SEL_DIFFICULTY, "SELECT ID, Name, InstanceType, OrderIndex, OldEnumValue, FallbackDifficultyID, MinPlayers, MaxPlayers, " +
                "Flags, ItemContext, ToggleDifficultyID, GroupSizeHealthCurveID, GroupSizeDmgCurveID, GroupSizeSpellPointsCurveID FROM difficulty");
            PrepareStatement(HotfixStatements.SEL_DIFFICULTY_LOCALE, "SELECT ID, Name_lang FROM difficulty_locale WHERE locale = ?");

            // DungeonEncounter.db2
            PrepareStatement(HotfixStatements.SEL_DUNGEON_ENCOUNTER, "SELECT Name, ID, MapID, DifficultyID, OrderIndex, Bit, CreatureDisplayID, Flags" +
                " FROM dungeon_encounter");
            PrepareStatement(HotfixStatements.SEL_DUNGEON_ENCOUNTER_LOCALE, "SELECT ID, Name_lang FROM dungeon_encounter_locale WHERE locale = ?");

            // DurabilityCosts.db2
            PrepareStatement(HotfixStatements.SEL_DURABILITY_COSTS, "SELECT ID, WeaponSubClassCost1, WeaponSubClassCost2, WeaponSubClassCost3, WeaponSubClassCost4, " +
                "WeaponSubClassCost5, WeaponSubClassCost6, WeaponSubClassCost7, WeaponSubClassCost8, WeaponSubClassCost9, WeaponSubClassCost10, " +
                "WeaponSubClassCost11, WeaponSubClassCost12, WeaponSubClassCost13, WeaponSubClassCost14, WeaponSubClassCost15, WeaponSubClassCost16, " +
                "WeaponSubClassCost17, WeaponSubClassCost18, WeaponSubClassCost19, WeaponSubClassCost20, WeaponSubClassCost21, ArmorSubClassCost1, " +
                "ArmorSubClassCost2, ArmorSubClassCost3, ArmorSubClassCost4, ArmorSubClassCost5, ArmorSubClassCost6, ArmorSubClassCost7, ArmorSubClassCost8" +
                " FROM durability_costs");

            // DurabilityQuality.db2
            PrepareStatement(HotfixStatements.SEL_DURABILITY_QUALITY, "SELECT ID, Data FROM durability_quality");

            // Emotes.db2
            PrepareStatement(HotfixStatements.SEL_EMOTES, "SELECT ID, RaceMask, EmoteSlashCommand, AnimID, EmoteFlags, EmoteSpecProc, EmoteSpecProcParam, EventSoundID, " +
                "SpellVisualKitID, ClassMask FROM emotes");

            // EmotesText.db2
            PrepareStatement(HotfixStatements.SEL_EMOTES_TEXT, "SELECT ID, Name, EmoteID FROM emotes_text");

            // EmotesTextSound.db2
            PrepareStatement(HotfixStatements.SEL_EMOTES_TEXT_SOUND, "SELECT ID, RaceID, ClassID, SexID, SoundID, EmotesTextID FROM emotes_text_sound");

            // ExpectedStat.db2
            PrepareStatement(HotfixStatements.SEL_EXPECTED_STAT, "SELECT ID, ExpansionID, CreatureHealth, PlayerHealth, CreatureAutoAttackDps, CreatureArmor, " +
                "PlayerMana, PlayerPrimaryStat, PlayerSecondaryStat, ArmorConstant, CreatureSpellDamage, Lvl FROM expected_stat");

            // ExpectedStatMod.db2
            PrepareStatement(HotfixStatements.SEL_EXPECTED_STAT_MOD, "SELECT ID, CreatureHealthMod, PlayerHealthMod, CreatureAutoAttackDPSMod, CreatureArmorMod, " +
                "PlayerManaMod, PlayerPrimaryStatMod, PlayerSecondaryStatMod, ArmorConstantMod, CreatureSpellDamageMod FROM expected_stat_mod");

            // Faction.db2
            PrepareStatement(HotfixStatements.SEL_FACTION, "SELECT ReputationRaceMask1, ReputationRaceMask2, ReputationRaceMask3, ReputationRaceMask4, Name, " +
                "Description, ID, ReputationIndex, ParentFactionID, Expansion, FriendshipRepID, Flags, ParagonFactionID, ReputationClassMask1, " +
                "ReputationClassMask2, ReputationClassMask3, ReputationClassMask4, ReputationFlags1, ReputationFlags2, ReputationFlags3, ReputationFlags4, " +
                "ReputationBase1, ReputationBase2, ReputationBase3, ReputationBase4, ReputationMax1, ReputationMax2, ReputationMax3, ReputationMax4, " +
                "ParentFactionMod1, ParentFactionMod2, ParentFactionCap1, ParentFactionCap2 FROM faction");
            PrepareStatement(HotfixStatements.SEL_FACTION_LOCALE, "SELECT ID, Name_lang, Description_lang FROM faction_locale WHERE locale = ?");

            // FactionTemplate.db2
            PrepareStatement(HotfixStatements.SEL_FACTION_TEMPLATE, "SELECT ID, Faction, Flags, FactionGroup, FriendGroup, EnemyGroup, Enemies1, Enemies2, Enemies3, " +
                "Enemies4, Friend1, Friend2, Friend3, Friend4 FROM faction_template");

            // FriendshipRepReaction.db2
            PrepareStatement(HotfixStatements.SEL_FRIENDSHIP_REP_REACTION, "SELECT ID, Reaction, FriendshipRepID, ReactionThreshold FROM friendship_rep_reaction");
            PrepareStatement(HotfixStatements.SEL_FRIENDSHIP_REP_REACTION_LOCALE, "SELECT ID, Reaction_lang FROM friendship_rep_reaction_locale WHERE locale = ?");

            // FriendshipReputation.db2
            PrepareStatement(HotfixStatements.SEL_FRIENDSHIP_REPUTATION, "SELECT Description, ID, FactionID, TextureFileID FROM friendship_reputation");
            PrepareStatement(HotfixStatements.SEL_FRIENDSHIP_REPUTATION_LOCALE, "SELECT ID, Description_lang FROM friendship_reputation_locale WHERE locale = ?");

            // GameobjectArtKit.db2
            PrepareStatement(HotfixStatements.SEL_GAMEOBJECT_ART_KIT, "SELECT ID, AttachModelFileID, TextureVariationFileID1, TextureVariationFileID2, TextureVariationFileID3 FROM gameobject_art_kit");

            // GameobjectDisplayInfo.db2
            PrepareStatement(HotfixStatements.SEL_GAMEOBJECT_DISPLAY_INFO, "SELECT ID, ModelName, GeoBoxMinX, GeoBoxMinY, GeoBoxMinZ, GeoBoxMaxX, GeoBoxMaxY, " +
                "GeoBoxMaxZ, FileDataID, ObjectEffectPackageID, OverrideLootEffectScale, OverrideNameScale FROM gameobject_display_info");

            // Gameobjects.db2
            PrepareStatement(HotfixStatements.SEL_GAMEOBJECTS, "SELECT Name, PosX, PosY, PosZ, Rot1, Rot2, Rot3, Rot4, ID, OwnerID, DisplayID, Scale, TypeID, " +
                "PhaseUseFlags, PhaseID, PhaseGroupID, PropValue1, PropValue2, PropValue3, PropValue4, PropValue5, PropValue6, PropValue7, PropValue8" +
                " FROM gameobjects");
            PrepareStatement(HotfixStatements.SEL_GAMEOBJECTS_LOCALE, "SELECT ID, Name_lang FROM gameobjects_locale WHERE locale = ?");

            // GarrAbility.db2
            PrepareStatement(HotfixStatements.SEL_GARR_ABILITY, "SELECT Name, Description, ID, GarrAbilityCategoryID, GarrFollowerTypeID, IconFileDataID, " +
                "FactionChangeGarrAbilityID, Flags FROM garr_ability");
            PrepareStatement(HotfixStatements.SEL_GARR_ABILITY_LOCALE, "SELECT ID, Name_lang, Description_lang FROM garr_ability_locale WHERE locale = ?");

            // GarrBuilding.db2
            PrepareStatement(HotfixStatements.SEL_GARR_BUILDING, "SELECT ID, HordeName, AllianceName, Description, Tooltip, GarrTypeID, BuildingType, " +
                "HordeGameObjectID, AllianceGameObjectID, GarrSiteID, UpgradeLevel, BuildSeconds, CurrencyTypeID, CurrencyQty, HordeUiTextureKitID, " +
                "AllianceUiTextureKitID, IconFileDataID, AllianceSceneScriptPackageID, HordeSceneScriptPackageID, MaxAssignments, ShipmentCapacity, " +
                "GarrAbilityID, BonusGarrAbilityID, GoldCost, Flags FROM garr_building");
            PrepareStatement(HotfixStatements.SEL_GARR_BUILDING_LOCALE, "SELECT ID, HordeName_lang, AllianceName_lang, Description_lang, Tooltip_lang" +
                " FROM garr_building_locale WHERE locale = ?");

            // GarrBuildingPlotInst.db2
            PrepareStatement(HotfixStatements.SEL_GARR_BUILDING_PLOT_INST, "SELECT MapOffsetX, MapOffsetY, ID, GarrBuildingID, GarrSiteLevelPlotInstID, " +
                "UiTextureAtlasMemberID FROM garr_building_plot_inst");

            // GarrClassSpec.db2
            PrepareStatement(HotfixStatements.SEL_GARR_CLASS_SPEC, "SELECT ClassSpec, ClassSpecMale, ClassSpecFemale, ID, UiTextureAtlasMemberID, GarrFollItemSetID, " +
                "FollowerClassLimit, Flags FROM garr_class_spec");
            PrepareStatement(HotfixStatements.SEL_GARR_CLASS_SPEC_LOCALE, "SELECT ID, ClassSpec_lang, ClassSpecMale_lang, ClassSpecFemale_lang FROM garr_class_spec_locale" +
                " WHERE locale = ?");

            // GarrFollower.db2
            PrepareStatement(HotfixStatements.SEL_GARR_FOLLOWER, "SELECT HordeSourceText, AllianceSourceText, TitleName, ID, GarrTypeID, GarrFollowerTypeID, " +
                "HordeCreatureID, AllianceCreatureID, HordeGarrFollRaceID, AllianceGarrFollRaceID, HordeGarrClassSpecID, AllianceGarrClassSpecID, Quality, " +
                "FollowerLevel, ItemLevelWeapon, ItemLevelArmor, HordeSourceTypeEnum, AllianceSourceTypeEnum, HordeIconFileDataID, AllianceIconFileDataID, " +
                "HordeGarrFollItemSetID, AllianceGarrFollItemSetID, HordeUITextureKitID, AllianceUITextureKitID, Vitality, HordeFlavorGarrStringID, " +
                "AllianceFlavorGarrStringID, HordeSlottingBroadcastTextID, AllySlottingBroadcastTextID, ChrClassID, Flags, Gender FROM garr_follower");
            PrepareStatement(HotfixStatements.SEL_GARR_FOLLOWER_LOCALE, "SELECT ID, HordeSourceText_lang, AllianceSourceText_lang, TitleName_lang FROM garr_follower_locale" +
                " WHERE locale = ?");

            // GarrFollowerXAbility.db2
            PrepareStatement(HotfixStatements.SEL_GARR_FOLLOWER_X_ABILITY, "SELECT ID, OrderIndex, FactionIndex, GarrAbilityID, GarrFollowerID" +
                " FROM garr_follower_x_ability");

            // GarrMission.db2
            PrepareStatement(HotfixStatements.SEL_GARR_MISSION, "SELECT Name, Location, Description, MapPosX, MapPosY, WorldPosX, WorldPosY, ID, GarrTypeID, " +
                "GarrMissionTypeID, GarrFollowerTypeID, MaxFollowers, MissionCost, MissionCostCurrencyTypesID, OfferedGarrMissionTextureID, UiTextureKitID, " +
                "EnvGarrMechanicID, EnvGarrMechanicTypeID, PlayerConditionID, TargetLevel, TargetItemLevel, MissionDuration, TravelDuration, OfferDuration, " +
                "BaseCompletionChance, BaseFollowerXP, OvermaxRewardPackID, FollowerDeathChance, AreaID, Flags, GarrMissionSetID FROM garr_mission");
            PrepareStatement(HotfixStatements.SEL_GARR_MISSION_LOCALE, "SELECT ID, Name_lang, Location_lang, Description_lang FROM garr_mission_locale WHERE locale = ?");

            // GarrPlot.db2
            PrepareStatement(HotfixStatements.SEL_GARR_PLOT, "SELECT ID, Name, PlotType, HordeConstructObjID, AllianceConstructObjID, Flags, UiCategoryID, " +
                "UpgradeRequirement1, UpgradeRequirement2 FROM garr_plot");

            // GarrPlotBuilding.db2
            PrepareStatement(HotfixStatements.SEL_GARR_PLOT_BUILDING, "SELECT ID, GarrPlotID, GarrBuildingID FROM garr_plot_building");

            // GarrPlotInstance.db2
            PrepareStatement(HotfixStatements.SEL_GARR_PLOT_INSTANCE, "SELECT ID, Name, GarrPlotID FROM garr_plot_instance");

            // GarrSiteLevel.db2
            PrepareStatement(HotfixStatements.SEL_GARR_SITE_LEVEL, "SELECT ID, TownHallUiPosX, TownHallUiPosY, GarrSiteID, GarrLevel, MapID, UpgradeMovieID, " +
                "UiTextureKitID, MaxBuildingLevel, UpgradeCost, UpgradeGoldCost FROM garr_site_level");

            // GarrSiteLevelPlotInst.db2
            PrepareStatement(HotfixStatements.SEL_GARR_SITE_LEVEL_PLOT_INST, "SELECT ID, UiMarkerPosX, UiMarkerPosY, GarrSiteLevelID, GarrPlotInstanceID, UiMarkerSize" +
                " FROM garr_site_level_plot_inst");

            // GarrTalentTree.db2
            PrepareStatement(HotfixStatements.SEL_GARR_TALENT_TREE, "SELECT ID, Name, GarrTypeID, ClassID, MaxTiers, UiOrder, Flags, UiTextureKitID, " +
                "GarrTalentTreeType, PlayerConditionID, FeatureTypeIndex, FeatureSubtypeIndex, CurrencyID FROM garr_talent_tree");
            PrepareStatement(HotfixStatements.SEL_GARR_TALENT_TREE_LOCALE, "SELECT ID, Name_lang FROM garr_talent_tree_locale WHERE locale = ?");

            // GemProperties.db2
            PrepareStatement(HotfixStatements.SEL_GEM_PROPERTIES, "SELECT ID, EnchantId, Type, MinItemLevel FROM gem_properties");

            // GlyphBindableSpell.db2
            PrepareStatement(HotfixStatements.SEL_GLYPH_BINDABLE_SPELL, "SELECT ID, SpellID, GlyphPropertiesID FROM glyph_bindable_spell");

            // GlyphProperties.db2
            PrepareStatement(HotfixStatements.SEL_GLYPH_PROPERTIES, "SELECT ID, SpellID, GlyphType, GlyphExclusiveCategoryID, SpellIconFileDataID FROM glyph_properties");

            // GlyphRequiredSpec.db2
            PrepareStatement(HotfixStatements.SEL_GLYPH_REQUIRED_SPEC, "SELECT ID, ChrSpecializationID, GlyphPropertiesID FROM glyph_required_spec");

            // GuildColorBackground.db2
            PrepareStatement(HotfixStatements.SEL_GUILD_COLOR_BACKGROUND, "SELECT ID, Red, Blue, Green FROM guild_color_background");

            // GuildColorBorder.db2
            PrepareStatement(HotfixStatements.SEL_GUILD_COLOR_BORDER, "SELECT ID, Red, Blue, Green FROM guild_color_border");

            // GuildColorEmblem.db2
            PrepareStatement(HotfixStatements.SEL_GUILD_COLOR_EMBLEM, "SELECT ID, Red, Blue, Green FROM guild_color_emblem");

            // GuildPerkSpells.db2
            PrepareStatement(HotfixStatements.SEL_GUILD_PERK_SPELLS, "SELECT ID, SpellID FROM guild_perk_spells");

            // Heirloom.db2
            PrepareStatement(HotfixStatements.SEL_HEIRLOOM, "SELECT SourceText, ID, ItemID, LegacyUpgradedItemID, StaticUpgradedItemID, SourceTypeEnum, Flags, " +
                "LegacyItemID, UpgradeItemID1, UpgradeItemID2, UpgradeItemID3, UpgradeItemID4, UpgradeItemID5, UpgradeItemID6, UpgradeItemBonusListID1, " +
                "UpgradeItemBonusListID2, UpgradeItemBonusListID3, UpgradeItemBonusListID4, UpgradeItemBonusListID5, UpgradeItemBonusListID6 FROM heirloom");
            PrepareStatement(HotfixStatements.SEL_HEIRLOOM_LOCALE, "SELECT ID, SourceText_lang FROM heirloom_locale WHERE locale = ?");

            // Holidays.db2
            PrepareStatement(HotfixStatements.SEL_HOLIDAYS, "SELECT ID, Region, Looping, HolidayNameID, HolidayDescriptionID, Priority, CalendarFilterType, Flags, " +
                "WorldStateExpressionID, Duration1, Duration2, Duration3, Duration4, Duration5, Duration6, Duration7, Duration8, Duration9, Duration10, " +
                "Date1, Date2, Date3, Date4, Date5, Date6, Date7, Date8, Date9, Date10, Date11, Date12, Date13, Date14, Date15, Date16, Date17, Date18, " +
                "Date19, Date20, Date21, Date22, Date23, Date24, Date25, Date26, CalendarFlags1, CalendarFlags2, CalendarFlags3, CalendarFlags4, " +
                "CalendarFlags5, CalendarFlags6, CalendarFlags7, CalendarFlags8, CalendarFlags9, CalendarFlags10, TextureFileDataID1, TextureFileDataID2, " +
                "TextureFileDataID3 FROM holidays");

            // ImportPriceArmor.db2
            PrepareStatement(HotfixStatements.SEL_IMPORT_PRICE_ARMOR, "SELECT ID, ClothModifier, LeatherModifier, ChainModifier, PlateModifier FROM import_price_armor");

            // ImportPriceQuality.db2
            PrepareStatement(HotfixStatements.SEL_IMPORT_PRICE_QUALITY, "SELECT ID, Data FROM import_price_quality");

            // ImportPriceShield.db2
            PrepareStatement(HotfixStatements.SEL_IMPORT_PRICE_SHIELD, "SELECT ID, Data FROM import_price_shield");

            // ImportPriceWeapon.db2
            PrepareStatement(HotfixStatements.SEL_IMPORT_PRICE_WEAPON, "SELECT ID, Data FROM import_price_weapon");

            // Item.db2
            PrepareStatement(HotfixStatements.SEL_ITEM, "SELECT ID, ClassID, SubclassID, Material, InventoryType, RequiredLevel, SheatheType, RandomSelect, " +
                "ItemRandomSuffixGroupID, SoundOverrideSubclassID, ModifiedCraftingReagentItemID, IconFileDataID, ItemGroupSoundsID, ContentTuningID, " +
                "MaxDurability, AmmunitionType, Field_3_4_0_45704_015, DamageType1, DamageType2, DamageType3, DamageType4, DamageType5, Resistances1, " +
                "Resistances2, Resistances3, Resistances4, Resistances5, Resistances6, Resistances7, MinDamage1, MinDamage2, MinDamage3, MinDamage4, " +
                "MinDamage5, MaxDamage1, MaxDamage2, MaxDamage3, MaxDamage4, MaxDamage5 FROM item");

            // ItemAppearance.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_APPEARANCE, "SELECT ID, DisplayType, ItemDisplayInfoID, DefaultIconFileDataID, UiOrder FROM item_appearance");

            // ItemArmorQuality.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_ARMOR_QUALITY, "SELECT ID, Qualitymod1, Qualitymod2, Qualitymod3, Qualitymod4, Qualitymod5, Qualitymod6, " +
                "Qualitymod7 FROM item_armor_quality");

            // ItemArmorShield.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_ARMOR_SHIELD, "SELECT ID, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, Quality7, ItemLevel" +
                " FROM item_armor_shield");

            // ItemArmorTotal.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_ARMOR_TOTAL, "SELECT ID, ItemLevel, Cloth, Leather, Mail, Plate FROM item_armor_total");

            // ItemBagFamily.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_BAG_FAMILY, "SELECT ID, Name FROM item_bag_family");
            PrepareStatement(HotfixStatements.SEL_ITEM_BAG_FAMILY_LOCALE, "SELECT ID, Name_lang FROM item_bag_family_locale WHERE locale = ?");

            // ItemBonus.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_BONUS, "SELECT ID, Value1, Value2, Value3, Value4, ParentItemBonusListID, Type, OrderIndex FROM item_bonus");

            // ItemBonusListLevelDelta.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_BONUS_LIST_LEVEL_DELTA, "SELECT ItemLevelDelta, ID FROM item_bonus_list_level_delta");

            // ItemBonusTreeNode.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_BONUS_TREE_NODE, "SELECT ID, ItemContext, ChildItemBonusTreeID, ChildItemBonusListID, ChildItemLevelSelectorID, " +
                "ParentItemBonusTreeID FROM item_bonus_tree_node");

            // ItemChildEquipment.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_CHILD_EQUIPMENT, "SELECT ID, ChildItemID, ChildItemEquipSlot, ParentItemID FROM item_child_equipment");

            // ItemClass.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_CLASS, "SELECT ID, ClassName, ClassID, PriceModifier, Flags FROM item_class");
            PrepareStatement(HotfixStatements.SEL_ITEM_CLASS_LOCALE, "SELECT ID, ClassName_lang FROM item_class_locale WHERE locale = ?");

            // ItemCurrencyCost.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_CURRENCY_COST, "SELECT ID, ItemID FROM item_currency_cost");

            // ItemDamageAmmo.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_AMMO, "SELECT ID, ItemLevel, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, Quality7" +
                " FROM item_damage_ammo");

            // ItemDamageOneHand.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND, "SELECT ID, ItemLevel, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, Quality7" +
                " FROM item_damage_one_hand");

            // ItemDamageOneHandCaster.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND_CASTER, "SELECT ID, ItemLevel, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, " +
                "Quality7 FROM item_damage_one_hand_caster");

            // ItemDamageTwoHand.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND, "SELECT ID, ItemLevel, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, Quality7" +
                " FROM item_damage_two_hand");

            // ItemDamageTwoHandCaster.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND_CASTER, "SELECT ID, ItemLevel, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, " +
                "Quality7 FROM item_damage_two_hand_caster");

            // ItemDisenchantLoot.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DISENCHANT_LOOT, "SELECT ID, Subclass, Quality, MinLevel, MaxLevel, SkillRequired, ExpansionID, Class" +
                " FROM item_disenchant_loot");

            // ItemEffect.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_EFFECT, "SELECT ID, LegacySlotIndex, TriggerType, Charges, CoolDownMSec, CategoryCoolDownMSec, SpellCategoryID, " +
                "SpellID, ChrSpecializationID, ParentItemID FROM item_effect");

            // ItemExtendedCost.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_EXTENDED_COST, "SELECT ID, RequiredArenaRating, ArenaBracket, Flags, MinFactionID, MinReputation, " +
                "RequiredAchievement, ItemID1, ItemID2, ItemID3, ItemID4, ItemID5, ItemCount1, ItemCount2, ItemCount3, ItemCount4, ItemCount5, CurrencyID1, " +
                "CurrencyID2, CurrencyID3, CurrencyID4, CurrencyID5, CurrencyCount1, CurrencyCount2, CurrencyCount3, CurrencyCount4, CurrencyCount5" +
                " FROM item_extended_cost");

            // ItemLevelSelector.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LEVEL_SELECTOR, "SELECT ID, MinItemLevel, ItemLevelSelectorQualitySetID FROM item_level_selector");

            // ItemLevelSelectorQuality.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY, "SELECT ID, QualityItemBonusListID, Quality, ParentILSQualitySetID" +
                " FROM item_level_selector_quality");

            // ItemLevelSelectorQualitySet.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY_SET, "SELECT ID, IlvlRare, IlvlEpic FROM item_level_selector_quality_set");

            // ItemLimitCategory.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LIMIT_CATEGORY, "SELECT ID, Name, Quantity, Flags FROM item_limit_category");
            PrepareStatement(HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_LOCALE, "SELECT ID, Name_lang FROM item_limit_category_locale WHERE locale = ?");

            // ItemLimitCategoryCondition.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_CONDITION, "SELECT ID, AddQuantity, PlayerConditionID, ParentItemLimitCategoryID" +
                " FROM item_limit_category_condition");

            // ItemModifiedAppearance.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_MODIFIED_APPEARANCE, "SELECT ID, ItemID, ItemAppearanceModifierID, ItemAppearanceID, OrderIndex, " +
                "TransmogSourceTypeEnum FROM item_modified_appearance");

            // ItemModifiedAppearanceExtra.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_MODIFIED_APPEARANCE_EXTRA, "SELECT ID, IconFileDataID, UnequippedIconFileDataID, SheatheType, " +
                "DisplayWeaponSubclassID, DisplayInventoryType FROM item_modified_appearance_extra");

            // ItemNameDescription.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_NAME_DESCRIPTION, "SELECT ID, Description, Color FROM item_name_description");
            PrepareStatement(HotfixStatements.SEL_ITEM_NAME_DESCRIPTION_LOCALE, "SELECT ID, Description_lang FROM item_name_description_locale WHERE locale = ?");

            // ItemPriceBase.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_PRICE_BASE, "SELECT ID, ItemLevel, Armor, Weapon FROM item_price_base");

            // ItemSet.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SET, "SELECT ID, Name, SetFlags, RequiredSkill, RequiredSkillRank, ItemID1, ItemID2, ItemID3, ItemID4, ItemID5, " +
                "ItemID6, ItemID7, ItemID8, ItemID9, ItemID10, ItemID11, ItemID12, ItemID13, ItemID14, ItemID15, ItemID16, ItemID17 FROM item_set");
            PrepareStatement(HotfixStatements.SEL_ITEM_SET_LOCALE, "SELECT ID, Name_lang FROM item_set_locale WHERE locale = ?");

            // ItemSetSpell.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SET_SPELL, "SELECT ID, ChrSpecID, SpellID, Threshold, ItemSetID FROM item_set_spell");

            // ItemSparse.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SPARSE, "SELECT ID, AllowableRace, Description, Display3, Display2, Display1, Display, DmgVariance, " +
                "DurationInInventory, QualityModifier, BagFamily, StartQuestID, ItemRange, StatPercentageOfSocket1, StatPercentageOfSocket2, " +
                "StatPercentageOfSocket3, StatPercentageOfSocket4, StatPercentageOfSocket5, StatPercentageOfSocket6, StatPercentageOfSocket7, " +
                "StatPercentageOfSocket8, StatPercentageOfSocket9, StatPercentageOfSocket10, StatPercentEditor1, StatPercentEditor2, StatPercentEditor3, " +
                "StatPercentEditor4, StatPercentEditor5, StatPercentEditor6, StatPercentEditor7, StatPercentEditor8, StatPercentEditor9, StatPercentEditor10, " +
                "Stackable, MaxCount, RequiredAbility, SellPrice, BuyPrice, VendorStackCount, PriceVariance, PriceRandomValue, Flags1, Flags2, Flags3, " +
                "Flags4, OppositeFactionItemID, ModifiedCraftingReagentItemID, ContentTuningID, PlayerLevelToItemLevelCurveID, MaxDurability, " +
                "ItemNameDescriptionID, RequiredTransmogHoliday, RequiredHoliday, LimitCategory, GemProperties, SocketMatchEnchantmentId, TotemCategoryID, " +
                "InstanceBound, ZoneBound1, ZoneBound2, ItemSet, LockID, PageID, ItemDelay, MinFactionID, RequiredSkillRank, RequiredSkill, ItemLevel, " +
                "AllowableClass, ItemRandomSuffixGroupID, RandomSelect, MinDamage1, MinDamage2, MinDamage3, MinDamage4, MinDamage5, MaxDamage1, MaxDamage2, " +
                "MaxDamage3, MaxDamage4, MaxDamage5, Resistances1, Resistances2, Resistances3, Resistances4, Resistances5, Resistances6, Resistances7, " +
                "ScalingStatDistributionID, StatModifier_bonusAmount1, StatModifier_bonusAmount2, StatModifier_bonusAmount3, StatModifier_bonusAmount4, " +
                "StatModifier_bonusAmount5, StatModifier_bonusAmount6, StatModifier_bonusAmount7, StatModifier_bonusAmount8, StatModifier_bonusAmount9, " +
                "StatModifier_bonusAmount10, ExpansionID, ArtifactID, SpellWeight, SpellWeightCategory, SocketType1, SocketType2, SocketType3, SheatheType, " +
                "Material, PageMaterialID, LanguageID, Bonding, DamageType, StatModifierBonusStat1, StatModifierBonusStat2, StatModifierBonusStat3, " +
                "StatModifierBonusStat4, StatModifierBonusStat5, StatModifierBonusStat6, StatModifierBonusStat7, StatModifierBonusStat8, " +
                "StatModifierBonusStat9, StatModifierBonusStat10, ContainerSlots, MinReputation, RequiredPVPMedal, RequiredPVPRank, InventoryType, " +
                "OverallQualityID, AmmunitionType, RequiredLevel FROM item_sparse");
            PrepareStatement(HotfixStatements.SEL_ITEM_SPARSE_LOCALE, "SELECT ID, Description_lang, Display3_lang, Display2_lang, Display1_lang, Display_lang" +
                " FROM item_sparse_locale WHERE locale = ?");

            // ItemSpec.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SPEC, "SELECT ID, MinLevel, MaxLevel, ItemType, PrimaryStat, SecondaryStat, SpecializationID FROM item_spec");

            // ItemSpecOverride.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SPEC_OVERRIDE, "SELECT ID, SpecID, ItemID FROM item_spec_override");

            // ItemXBonusTree.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_X_BONUS_TREE, "SELECT ID, ItemBonusTreeID, ItemID FROM item_x_bonus_tree");

            // JournalEncounter.db2
            PrepareStatement(HotfixStatements.SEL_JOURNAL_ENCOUNTER, "SELECT ID, Name, Description, MapX, MapY, JournalInstanceID, OrderIndex, FirstSectionID, UiMapID, " +
                "MapDisplayConditionID, Flags, DifficultyMask FROM journal_encounter");
            PrepareStatement(HotfixStatements.SEL_JOURNAL_ENCOUNTER_LOCALE, "SELECT ID, Name_lang, Description_lang FROM journal_encounter_locale WHERE locale = ?");

            // JournalEncounterSection.db2
            PrepareStatement(HotfixStatements.SEL_JOURNAL_ENCOUNTER_SECTION, "SELECT ID, Title, BodyText, JournalEncounterID, OrderIndex, ParentSectionID, " +
                "FirstChildSectionID, NextSiblingSectionID, Type, IconCreatureDisplayInfoID, UiModelSceneID, SpellID, IconFileDataID, Flags, IconFlags, " +
                "DifficultyMask FROM journal_encounter_section");
            PrepareStatement(HotfixStatements.SEL_JOURNAL_ENCOUNTER_SECTION_LOCALE, "SELECT ID, Title_lang, BodyText_lang FROM journal_encounter_section_locale WHERE locale = ?");

            // JournalInstance.db2
            PrepareStatement(HotfixStatements.SEL_JOURNAL_INSTANCE, "SELECT Name, Description, ID, MapID, BackgroundFileDataID, ButtonFileDataID, " +
                "ButtonSmallFileDataID, LoreFileDataID, OrderIndex, Flags, AreaID FROM journal_instance");
            PrepareStatement(HotfixStatements.SEL_JOURNAL_INSTANCE_LOCALE, "SELECT ID, Name_lang, Description_lang FROM journal_instance_locale WHERE locale = ?");

            // JournalTier.db2
            PrepareStatement(HotfixStatements.SEL_JOURNAL_TIER, "SELECT ID, Name FROM journal_tier");
            PrepareStatement(HotfixStatements.SEL_JOURNAL_TIER_LOCALE, "SELECT ID, Name_lang FROM journal_tier_locale WHERE locale = ?");

            // Keychain.db2
            PrepareStatement(HotfixStatements.SEL_KEYCHAIN, "SELECT ID, Key1, Key2, Key3, Key4, Key5, Key6, Key7, Key8, Key9, Key10, Key11, Key12, Key13, Key14, Key15, " +
                "Key16, Key17, Key18, Key19, Key20, Key21, Key22, Key23, Key24, Key25, Key26, Key27, Key28, Key29, Key30, Key31, Key32 FROM keychain");

            // KeystoneAffix.db2
            PrepareStatement(HotfixStatements.SEL_KEYSTONE_AFFIX, "SELECT Name, Description, ID, FiledataID FROM keystone_affix");
            PrepareStatement(HotfixStatements.SEL_KEYSTONE_AFFIX_LOCALE, "SELECT ID, Name_lang, Description_lang FROM keystone_affix_locale WHERE locale = ?");

            // LanguageWords.db2
            PrepareStatement(HotfixStatements.SEL_LANGUAGE_WORDS, "SELECT ID, Word, LanguageID FROM language_words");

            // Languages.db2
            PrepareStatement(HotfixStatements.SEL_LANGUAGES, "SELECT Name, ID, Flags, UiTextureKitID, UiTextureKitElementCount FROM languages");
            PrepareStatement(HotfixStatements.SEL_LANGUAGES_LOCALE, "SELECT ID, Name_lang FROM languages_locale WHERE locale = ?");

            // LfgDungeons.db2
            PrepareStatement(HotfixStatements.SEL_LFG_DUNGEONS, "SELECT ID, Name, Description, MinLevel, MaxLevel, TypeID, Subtype, Faction, IconTextureFileID, " +
                "RewardsBgTextureFileID, PopupBgTextureFileID, ExpansionLevel, MapID, DifficultyID, MinGear, GroupID, OrderIndex, RequiredPlayerConditionId, " +
                "TargetLevel, TargetLevelMin, TargetLevelMax, RandomID, ScenarioID, FinalEncounterID, CountTank, CountHealer, CountDamage, MinCountTank, " +
                "MinCountHealer, MinCountDamage, BonusReputationAmount, MentorItemLevel, MentorCharLevel, Flags1, Flags2 FROM lfg_dungeons");
            PrepareStatement(HotfixStatements.SEL_LFG_DUNGEONS_LOCALE, "SELECT ID, Name_lang, Description_lang FROM lfg_dungeons_locale WHERE locale = ?");

            // Light.db2
            PrepareStatement(HotfixStatements.SEL_LIGHT, "SELECT ID, GameCoordsX, GameCoordsY, GameCoordsZ, GameFalloffStart, GameFalloffEnd, ContinentID, " +
                "LightParamsID1, LightParamsID2, LightParamsID3, LightParamsID4, LightParamsID5, LightParamsID6, LightParamsID7, LightParamsID8 FROM light");

            // LiquidType.db2
            PrepareStatement(HotfixStatements.SEL_LIQUID_TYPE, "SELECT ID, Name, Texture1, Texture2, Texture3, Texture4, Texture5, Texture6, Flags, SoundBank, SoundID, " +
                "SpellID, MaxDarkenDepth, FogDarkenIntensity, AmbDarkenIntensity, DirDarkenIntensity, LightID, ParticleScale, ParticleMovement, " +
                "ParticleTexSlots, MaterialID, MinimapStaticCol, FrameCountTexture1, FrameCountTexture2, FrameCountTexture3, FrameCountTexture4, " +
                "FrameCountTexture5, FrameCountTexture6, Color1, Color2, Float1, Float2, Float3, `Float4`, Float5, Float6, Float7, `Float8`, Float9, Float10, " +
                "Float11, Float12, Float13, Float14, Float15, Float16, Float17, Float18, `Int1`, `Int2`, `Int3`, `Int4`, Coefficient1, Coefficient2, " +
                "Coefficient3, Coefficient4 FROM liquid_type");

            // Lock.db2
            PrepareStatement(HotfixStatements.SEL_LOCK, "SELECT ID, Index1, Index2, Index3, Index4, Index5, Index6, Index7, Index8, Skill1, Skill2, Skill3, Skill4, " +
                "Skill5, Skill6, Skill7, Skill8, Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Action1, Action2, Action3, Action4, Action5, " +
                "Action6, Action7, Action8 FROM `lock`");

            // MailTemplate.db2
            PrepareStatement(HotfixStatements.SEL_MAIL_TEMPLATE, "SELECT ID, Body FROM mail_template");
            PrepareStatement(HotfixStatements.SEL_MAIL_TEMPLATE_LOCALE, "SELECT ID, Body_lang FROM mail_template_locale WHERE locale = ?");

            // Map.db2
            PrepareStatement(HotfixStatements.SEL_MAP, "SELECT ID, Directory, MapName, MapDescription0, MapDescription1, PvpShortDescription, PvpLongDescription, " +
                "MapType, InstanceType, ExpansionID, AreaTableID, LoadingScreenID, TimeOfDayOverride, ParentMapID, CosmeticParentMapID, TimeOffset, " +
                "MinimapIconScale, RaidOffset, CorpseMapID, MaxPlayers, WindSettingsID, ZmpFileDataID, Flags1, Flags2, Flags3 FROM map");
            PrepareStatement(HotfixStatements.SEL_MAP_LOCALE, "SELECT ID, MapName_lang, MapDescription0_lang, MapDescription1_lang, PvpShortDescription_lang, " +
                "PvpLongDescription_lang FROM map_locale WHERE locale = ?");

            // MapChallengeMode.db2
            PrepareStatement(HotfixStatements.SEL_MAP_CHALLENGE_MODE, "SELECT Name, ID, MapID, Flags, ExpansionLevel, RequiredWorldStateID, CriteriaCount1, CriteriaCount2, CriteriaCount3 FROM map_challenge_mode");
            PrepareStatement(HotfixStatements.SEL_MAP_CHALLENGE_MODE_LOCALE, "SELECT ID, Name_lang FROM map_challenge_mode_locale WHERe locale = ?");

            // MapDifficulty.db2
            PrepareStatement(HotfixStatements.SEL_MAP_DIFFICULTY, "SELECT ID, Message, ItemContextPickerID, ContentTuningID, DifficultyID, LockID, ResetInterval, " + 
                "MaxPlayers, ItemContext, Flags, MapID FROM map_difficulty");
            PrepareStatement(HotfixStatements.SEL_MAP_DIFFICULTY_LOCALE, "SELECT ID, Message_lang FROM map_difficulty_locale WHERE locale = ?");

            // MapDifficultyXCondition.db2
            PrepareStatement(HotfixStatements.SEL_MAP_DIFFICULTY_X_CONDITION, "SELECT ID, FailureDescription, PlayerConditionID, OrderIndex, MapDifficultyID" +
                " FROM map_difficulty_x_condition");
            PrepareStatement(HotfixStatements.SEL_MAP_DIFFICULTY_X_CONDITION_LOCALE, "SELECT ID, FailureDescription_lang FROM map_difficulty_x_condition_locale WHERE locale = ?");

            // ModifierTree.db2
            PrepareStatement(HotfixStatements.SEL_MODIFIER_TREE, "SELECT ID, Parent, Operator, Amount, Type, Asset, SecondaryAsset, TertiaryAsset FROM modifier_tree");

            // Mount.db2
            PrepareStatement(HotfixStatements.SEL_MOUNT, "SELECT Name, SourceText, Description, ID, MountTypeID, Flags, SourceTypeEnum, SourceSpellID, " +
                "PlayerConditionID, MountFlyRideHeight, UiModelSceneID FROM mount");
            PrepareStatement(HotfixStatements.SEL_MOUNT_LOCALE, "SELECT ID, Name_lang, SourceText_lang, Description_lang FROM mount_locale WHERE locale = ?");

            // MountCapability.db2
            PrepareStatement(HotfixStatements.SEL_MOUNT_CAPABILITY, "SELECT ID, Flags, ReqRidingSkill, ReqAreaID, ReqSpellAuraID, ReqSpellKnownID, ModSpellAuraID, " +
                "ReqMapID FROM mount_capability");

            // MountTypeXCapability.db2
            PrepareStatement(HotfixStatements.SEL_MOUNT_TYPE_X_CAPABILITY, "SELECT ID, MountTypeID, MountCapabilityID, OrderIndex FROM mount_type_x_capability");

            // MountXDisplay.db2
            PrepareStatement(HotfixStatements.SEL_MOUNT_X_DISPLAY, "SELECT ID, CreatureDisplayInfoID, PlayerConditionID, MountID FROM mount_x_display");

            // Movie.db2
            PrepareStatement(HotfixStatements.SEL_MOVIE, "SELECT ID, Volume, KeyID, AudioFileDataID, SubtitleFileDataID FROM movie");

            // NameGen.db2
            PrepareStatement(HotfixStatements.SEL_NAME_GEN, "SELECT ID, Name, RaceID, Sex FROM name_gen");

            // NamesProfanity.db2
            PrepareStatement(HotfixStatements.SEL_NAMES_PROFANITY, "SELECT ID, Name, Language FROM names_profanity");

            // NamesReserved.db2
            PrepareStatement(HotfixStatements.SEL_NAMES_RESERVED, "SELECT ID, Name FROM names_reserved");

            // NamesReservedLocale.db2
            PrepareStatement(HotfixStatements.SEL_NAMES_RESERVED_LOCALE, "SELECT ID, Name, LocaleMask FROM names_reserved_locale");

            // NumTalentsAtLevel.db2
            PrepareStatement(HotfixStatements.SEL_NUM_TALENTS_AT_LEVEL, "SELECT ID, NumTalents, NumTalentsDeathKnight, NumTalentsDemonHunter FROM num_talents_at_level");

            // OverrideSpellData.db2
            PrepareStatement(HotfixStatements.SEL_OVERRIDE_SPELL_DATA, "SELECT ID, Spells1, Spells2, Spells3, Spells4, Spells5, Spells6, Spells7, Spells8, Spells9, " +
                "Spells10, PlayerActionBarFileDataID, Flags FROM override_spell_data");

            // ParagonReputation.db2
            PrepareStatement(HotfixStatements.SEL_PARAGON_REPUTATION, "SELECT ID, FactionID, LevelThreshold, QuestID FROM paragon_reputation");

            // Phase.db2
            PrepareStatement(HotfixStatements.SEL_PHASE, "SELECT ID, Flags FROM phase");

            // PhaseXPhaseGroup.db2
            PrepareStatement(HotfixStatements.SEL_PHASE_X_PHASE_GROUP, "SELECT ID, PhaseID, PhaseGroupID FROM phase_x_phase_group");

            // PlayerCondition.db2
            PrepareStatement(HotfixStatements.SEL_PLAYER_CONDITION, "SELECT RaceMask, FailureDescription, ID, MinLevel, MaxLevel, ClassMask, SkillLogic, LanguageID, " +
                "MinLanguage, MaxLanguage, MaxFactionID, MaxReputation, ReputationLogic, CurrentPvpFaction, PvpMedal, PrevQuestLogic, CurrQuestLogic, " +
                "CurrentCompletedQuestLogic, SpellLogic, ItemLogic, ItemFlags, AuraSpellLogic, WorldStateExpressionID, WeatherID, PartyStatus, " +
                "LifetimeMaxPVPRank, AchievementLogic, Gender, NativeGender, AreaLogic, LfgLogic, CurrencyLogic, QuestKillID, QuestKillLogic, " +
                "MinExpansionLevel, MaxExpansionLevel, MinAvgItemLevel, MaxAvgItemLevel, MinAvgEquippedItemLevel, MaxAvgEquippedItemLevel, PhaseUseFlags, " +
                "PhaseID, PhaseGroupID, Flags, ChrSpecializationIndex, ChrSpecializationRole, ModifierTreeID, PowerType, PowerTypeComp, PowerTypeValue, " +
                "WeaponSubclassMask, MaxGuildLevel, MinGuildLevel, MaxExpansionTier, MinExpansionTier, MinPVPRank, MaxPVPRank, SkillID1, SkillID2, SkillID3, " +
                "SkillID4, MinSkill1, MinSkill2, MinSkill3, MinSkill4, MaxSkill1, MaxSkill2, MaxSkill3, MaxSkill4, MinFactionID1, MinFactionID2, " +
                "MinFactionID3, MinReputation1, MinReputation2, MinReputation3, PrevQuestID1, PrevQuestID2, PrevQuestID3, PrevQuestID4, CurrQuestID1, " +
                "CurrQuestID2, CurrQuestID3, CurrQuestID4, CurrentCompletedQuestID1, CurrentCompletedQuestID2, CurrentCompletedQuestID3, " +
                "CurrentCompletedQuestID4, SpellID1, SpellID2, SpellID3, SpellID4, ItemID1, ItemID2, ItemID3, ItemID4, ItemCount1, ItemCount2, ItemCount3, " +
                "ItemCount4, Explored1, Explored2, Time1, Time2, AuraSpellID1, AuraSpellID2, AuraSpellID3, AuraSpellID4, AuraStacks1, AuraStacks2, " +
                "AuraStacks3, AuraStacks4, Achievement1, Achievement2, Achievement3, Achievement4, AreaID1, AreaID2, AreaID3, AreaID4, LfgStatus1, " +
                "LfgStatus2, LfgStatus3, LfgStatus4, LfgCompare1, LfgCompare2, LfgCompare3, LfgCompare4, LfgValue1, LfgValue2, LfgValue3, LfgValue4, " +
                "CurrencyID1, CurrencyID2, CurrencyID3, CurrencyID4, CurrencyCount1, CurrencyCount2, CurrencyCount3, CurrencyCount4, QuestKillMonster1, " +
                "QuestKillMonster2, QuestKillMonster3, QuestKillMonster4, QuestKillMonster5, QuestKillMonster6, MovementFlags1, MovementFlags2" +
                " FROM player_condition");
            PrepareStatement(HotfixStatements.SEL_PLAYER_CONDITION_LOCALE, "SELECT ID, FailureDescription_lang FROM player_condition_locale WHERE locale = ?");

            // PowerDisplay.db2
            PrepareStatement(HotfixStatements.SEL_POWER_DISPLAY, "SELECT ID, GlobalStringBaseTag, ActualType, Red, Green, Blue FROM power_display");

            // PowerType.db2
            PrepareStatement(HotfixStatements.SEL_POWER_TYPE, "SELECT ID, NameGlobalStringTag, CostGlobalStringTag, PowerTypeEnum, MinPower, MaxBasePower, CenterPower, " +
                "DefaultPower, DisplayModifier, RegenInterruptTimeMS, RegenPeace, RegenCombat, Flags FROM power_type");

            // PrestigeLevelInfo.db2
            PrepareStatement(HotfixStatements.SEL_PRESTIGE_LEVEL_INFO, "SELECT ID, Name, PrestigeLevel, BadgeTextureFileDataID, Flags, AwardedAchievementID" +
                " FROM prestige_level_info");
            PrepareStatement(HotfixStatements.SEL_PRESTIGE_LEVEL_INFO_LOCALE, "SELECT ID, Name_lang FROM prestige_level_info_locale WHERE locale = ?");

            // PvpDifficulty.db2
            PrepareStatement(HotfixStatements.SEL_PVP_DIFFICULTY, "SELECT ID, RangeIndex, MinLevel, MaxLevel, MapID FROM pvp_difficulty");

            // PvpItem.db2
            PrepareStatement(HotfixStatements.SEL_PVP_ITEM, "SELECT ID, ItemID, ItemLevelDelta FROM pvp_item");

            // PvpTalent.db2
            PrepareStatement(HotfixStatements.SEL_PVP_TALENT, "SELECT Description, ID, SpecID, SpellID, OverridesSpellID, Flags, ActionBarSpellID, PvpTalentCategoryID, " +
                "LevelRequired FROM pvp_talent");
            PrepareStatement(HotfixStatements.SEL_PVP_TALENT_LOCALE, "SELECT ID, Description_lang FROM pvp_talent_locale WHERE locale = ?");

            // PvpTalentCategory.db2
            PrepareStatement(HotfixStatements.SEL_PVP_TALENT_CATEGORY, "SELECT ID, TalentSlotMask FROM pvp_talent_category");

            // PvpTalentSlotUnlock.db2
            PrepareStatement(HotfixStatements.SEL_PVP_TALENT_SLOT_UNLOCK, "SELECT ID, Slot, LevelRequired, DeathKnightLevelRequired, DemonHunterLevelRequired" +
                " FROM pvp_talent_slot_unlock");

            // PvpTier.db2
            PrepareStatement(HotfixStatements.SEL_PVP_TIER, "SELECT ID, Name, MinRating, MaxRating, PrevTier, NextTier, BracketID, `Rank`, RankIconFileDataID" +
                " FROM pvp_tier");
            PrepareStatement(HotfixStatements.SEL_PVP_TIER_LOCALE, "SELECT ID, Name_lang FROM pvp_tier_locale WHERE locale = ?");

            // QuestFactionReward.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_FACTION_REWARD, "SELECT ID, Difficulty1, Difficulty2, Difficulty3, Difficulty4, Difficulty5, Difficulty6, " +
                "Difficulty7, Difficulty8, Difficulty9, Difficulty10 FROM quest_faction_reward");

            // QuestInfo.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_INFO, "SELECT ID, InfoName, Type, Modifiers, Profession FROM quest_info");
            PrepareStatement(HotfixStatements.SEL_QUEST_INFO_LOCALE, "SELECT ID, InfoName_lang FROM quest_info_locale WHERE locale = ?");

            // QuestLineXQuest.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_LINE_X_QUEST, "SELECT ID, QuestLineID, QuestID, OrderIndex FROM quest_line_x_quest");

            // QuestMoneyReward.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_MONEY_REWARD, "SELECT ID, Difficulty1, Difficulty2, Difficulty3, Difficulty4, Difficulty5, Difficulty6, " +
                "Difficulty7, Difficulty8, Difficulty9, Difficulty10 FROM quest_money_reward");

            // QuestPackageItem.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_PACKAGE_ITEM, "SELECT ID, PackageID, ItemID, ItemQuantity, DisplayType FROM quest_package_item");

            // QuestSort.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_SORT, "SELECT ID, SortName, UiOrderIndex FROM quest_sort");
            PrepareStatement(HotfixStatements.SEL_QUEST_SORT_LOCALE, "SELECT ID, SortName_lang FROM quest_sort_locale WHERE locale = ?");

            // QuestV2.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_V2, "SELECT ID, UniqueBitFlag FROM quest_v2");

            // QuestXp.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_XP, "SELECT ID, Difficulty1, Difficulty2, Difficulty3, Difficulty4, Difficulty5, Difficulty6, Difficulty7, " +
                "Difficulty8, Difficulty9, Difficulty10 FROM quest_xp");

            // RandPropPoints.db2
            PrepareStatement(HotfixStatements.SEL_RAND_PROP_POINTS, "SELECT ID, DamageReplaceStat, Epic1, Epic2, Epic3, Epic4, Epic5, Superior1, Superior2, Superior3, " +
                "Superior4, Superior5, Good1, Good2, Good3, Good4, Good5 FROM rand_prop_points");

            // RewardPack.db2
            PrepareStatement(HotfixStatements.SEL_REWARD_PACK, "SELECT ID, CharTitleID, Money, ArtifactXPDifficulty, ArtifactXPMultiplier, ArtifactXPCategoryID, " +
                "TreasurePickerID FROM reward_pack");

            // RewardPackXCurrencyType.db2
            PrepareStatement(HotfixStatements.SEL_REWARD_PACK_X_CURRENCY_TYPE, "SELECT ID, CurrencyTypeID, Quantity, RewardPackID FROM reward_pack_x_currency_type");

            // RewardPackXItem.db2
            PrepareStatement(HotfixStatements.SEL_REWARD_PACK_X_ITEM, "SELECT ID, ItemID, ItemQuantity, RewardPackID FROM reward_pack_x_item");

            // Scenario.db2
            PrepareStatement(HotfixStatements.SEL_SCENARIO, "SELECT ID, Name, AreaTableID, Type, Flags, UiTextureKitID FROM scenario");
            PrepareStatement(HotfixStatements.SEL_SCENARIO_LOCALE, "SELECT ID, Name_lang FROM scenario_locale WHERE locale = ?");

            // ScenarioStep.db2
            PrepareStatement(HotfixStatements.SEL_SCENARIO_STEP, "SELECT ID, Description, Title, ScenarioID, Criteriatreeid, RewardQuestID, RelatedStep, Supersedes, " +
                "OrderIndex, Flags, VisibilityPlayerConditionID, WidgetSetID FROM scenario_step");
            PrepareStatement(HotfixStatements.SEL_SCENARIO_STEP_LOCALE, "SELECT ID, Description_lang, Title_lang FROM scenario_step_locale WHERE locale = ?");

            // SceneScript.db2
            PrepareStatement(HotfixStatements.SEL_SCENE_SCRIPT, "SELECT ID, FirstSceneScriptID, NextSceneScriptID, Unknown915 FROM scene_script");

            // SceneScriptGlobalText.db2
            PrepareStatement(HotfixStatements.SEL_SCENE_SCRIPT_GLOBAL_TEXT, "SELECT ID, Name, Script FROM scene_script_global_text");

            // SceneScriptPackage.db2
            PrepareStatement(HotfixStatements.SEL_SCENE_SCRIPT_PACKAGE, "SELECT ID, Name FROM scene_script_package");

            // SceneScriptText.db2
            PrepareStatement(HotfixStatements.SEL_SCENE_SCRIPT_TEXT, "SELECT ID, Name, Script FROM scene_script_text");

            // SkillLine.db2
            PrepareStatement(HotfixStatements.SEL_SKILL_LINE, "SELECT DisplayName, AlternateVerb, Description, HordeDisplayName, OverrideSourceInfoDisplayName, ID, " +
                "CategoryID, SpellIconFileID, CanLink, ParentSkillLineID, ParentTierIndex, Flags, SpellBookSpellID FROM skill_line");
            PrepareStatement(HotfixStatements.SEL_SKILL_LINE_LOCALE, "SELECT ID, DisplayName_lang, AlternateVerb_lang, Description_lang, HordeDisplayName_lang" +
                " FROM skill_line_locale WHERE locale = ?");

            // SkillLineAbility.db2
            PrepareStatement(HotfixStatements.SEL_SKILL_LINE_ABILITY, "SELECT RaceMask, ID, SkillLine, Spell, MinSkillLineRank, ClassMask, SupercedesSpell, " + 
                "AcquireMethod, TrivialSkillLineRankHigh, TrivialSkillLineRankLow, Flags, NumSkillUps, UniqueBit, TradeSkillCategoryID, SkillupSkillLineID, " +
                "CharacterPoints1, CharacterPoints2 FROM skill_line_ability");

            // SkillRaceClassInfo.db2
            PrepareStatement(HotfixStatements.SEL_SKILL_RACE_CLASS_INFO, "SELECT ID, RaceMask, SkillID, ClassMask, Flags, Availability, MinLevel, SkillTierID" +
                " FROM skill_race_class_info");
                        
            // SoundKit.db2
            PrepareStatement(HotfixStatements.SEL_SOUND_KIT, "SELECT ID, SoundType, VolumeFloat, Flags, MinDistance, DistanceCutoff, EAXDef, SoundKitAdvancedID, " +
                "VolumeVariationPlus, VolumeVariationMinus, PitchVariationPlus, PitchVariationMinus, DialogType, PitchAdjust, BusOverwriteID, MaxInstances" +
                " FROM sound_kit");

            // SpecializationSpells.db2
            PrepareStatement(HotfixStatements.SEL_SPECIALIZATION_SPELLS, "SELECT Description, ID, SpecID, SpellID, OverridesSpellID, DisplayOrder" +
                " FROM specialization_spells");
            PrepareStatement(HotfixStatements.SEL_SPECIALIZATION_SPELLS_LOCALE, "SELECT ID, Description_lang FROM specialization_spells_locale WHERE locale = ?");

            // SpecSetMember.db2
            PrepareStatement(HotfixStatements.SEL_SPEC_SET_MEMBER, "SELECT ID, ChrSpecializationID, SpecSetID FROM spec_set_member");

            // SpellAuraOptions.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_AURA_OPTIONS, "SELECT ID, DifficultyID, CumulativeAura, ProcCategoryRecovery, ProcChance, ProcCharges, " +
                "SpellProcsPerMinuteID, ProcTypeMask1, ProcTypeMask2, SpellID FROM spell_aura_options");

            // SpellAuraRestrictions.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_AURA_RESTRICTIONS, "SELECT ID, DifficultyID, CasterAuraState, TargetAuraState, ExcludeCasterAuraState, " +
                "ExcludeTargetAuraState, CasterAuraSpell, TargetAuraSpell, ExcludeCasterAuraSpell, ExcludeTargetAuraSpell, SpellID" +
                " FROM spell_aura_restrictions");

            // SpellCastTimes.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CAST_TIMES, "SELECT ID, Base, PerLevel, Minimum FROM spell_cast_times");

            // SpellCastingRequirements.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CASTING_REQUIREMENTS, "SELECT ID, SpellID, FacingCasterFlags, MinFactionID, MinReputation, RequiredAreasID, " +
                "RequiredAuraVision, RequiresSpellFocus FROM spell_casting_requirements");

            // SpellCategories.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CATEGORIES, "SELECT ID, DifficultyID, Category, DefenseType, DispelType, Mechanic, PreventionType, " +
                "StartRecoveryCategory, ChargeCategory, SpellID FROM spell_categories");

            // SpellCategory.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CATEGORY, "SELECT ID, Name, Flags, UsesPerWeek, MaxCharges, ChargeRecoveryTime, TypeMask FROM spell_category");
            PrepareStatement(HotfixStatements.SEL_SPELL_CATEGORY_LOCALE, "SELECT ID, Name_lang FROM spell_category_locale WHERE locale = ?");

            // SpellClassOptions.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CLASS_OPTIONS, "SELECT ID, SpellID, ModalNextSpell, SpellClassSet, SpellClassMask1, SpellClassMask2, " +
                "SpellClassMask3, SpellClassMask4 FROM spell_class_options");

            // SpellCooldowns.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_COOLDOWNS, "SELECT ID, DifficultyID, CategoryRecoveryTime, RecoveryTime, StartRecoveryTime, SpellID" +
                " FROM spell_cooldowns");

            // SpellDuration.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_DURATION, "SELECT ID, Duration, DurationPerLevel, MaxDuration FROM spell_duration");

            // SpellEffect.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_EFFECT, "SELECT ID, DifficultyID, EffectIndex, Effect, EffectAmplitude, EffectAttributes, EffectAura, " +
                "EffectAuraPeriod, EffectBasePoints, EffectBonusCoefficient, EffectChainAmplitude, EffectChainTargets, EffectDieSides, EffectItemType, " +
                "EffectMechanic, EffectPointsPerResource, EffectPosFacing, EffectRealPointsPerLevel, EffectTriggerSpell, BonusCoefficientFromAP, " +
                "PvpMultiplier, Coefficient, Variance, ResourceCoefficient, GroupSizeBasePointsCoefficient, EffectMiscValue1, EffectMiscValue2, " +
                "EffectRadiusIndex1, EffectRadiusIndex2, EffectSpellClassMask1, EffectSpellClassMask2, EffectSpellClassMask3, EffectSpellClassMask4, " +
                "ImplicitTarget1, ImplicitTarget2, SpellID FROM spell_effect");

            // SpellEquippedItems.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_EQUIPPED_ITEMS, "SELECT ID, SpellID, EquippedItemClass, EquippedItemInvTypes, EquippedItemSubclass" +
                " FROM spell_equipped_items");

            // SpellFocusObject.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_FOCUS_OBJECT, "SELECT ID, Name FROM spell_focus_object");
            PrepareStatement(HotfixStatements.SEL_SPELL_FOCUS_OBJECT_LOCALE, "SELECT ID, Name_lang FROM spell_focus_object_locale WHERE locale = ?");

            // SpellInterrupts.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_INTERRUPTS, "SELECT ID, DifficultyID, InterruptFlags, AuraInterruptFlags1, AuraInterruptFlags2, " +
                "ChannelInterruptFlags1, ChannelInterruptFlags2, SpellID FROM spell_interrupts");

            // SpellItemEnchantment.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT, "SELECT ID, Name, HordeName, EffectArg1, EffectArg2, EffectArg3, EffectScalingPoints1, " +
                "EffectScalingPoints2, EffectScalingPoints3, GemItemID, TransmogUseConditionID, TransmogCost, IconFileDataID, EffectPointsMin1, " +
                "EffectPointsMin2, EffectPointsMin3, ItemVisual, Flags, RequiredSkillID, RequiredSkillRank, ItemLevel, Charges, Effect1, Effect2, Effect3, " +
                "ScalingClass, ScalingClassRestricted, Condition_ID, MinLevel, MaxLevel FROM spell_item_enchantment");
            PrepareStatement(HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_LOCALE, "SELECT ID, Name_lang, HordeName_lang FROM spell_item_enchantment_locale WHERE locale = ?");

            // SpellItemEnchantmentCondition.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_CONDITION, "SELECT ID, LtOperandType1, LtOperandType2, LtOperandType3, LtOperandType4, " +
                "LtOperandType5, LtOperand1, LtOperand2, LtOperand3, LtOperand4, LtOperand5, Operator1, Operator2, Operator3, Operator4, Operator5, " +
                "RtOperandType1, RtOperandType2, RtOperandType3, RtOperandType4, RtOperandType5, RtOperand1, RtOperand2, RtOperand3, RtOperand4, RtOperand5, " +
                "Logic1, Logic2, Logic3, Logic4, Logic5 FROM spell_item_enchantment_condition");

            // SpellLabel.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_LABEL, "SELECT ID, LabelID, SpellID FROM spell_label");

            // SpellLearnSpell.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_LEARN_SPELL, "SELECT ID, SpellID, LearnSpellID, OverridesSpellID FROM spell_learn_spell");

            // SpellLevels.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_LEVELS, "SELECT ID, DifficultyID, BaseLevel, MaxLevel, SpellLevel, MaxPassiveAuraLevel, SpellID" +
                " FROM spell_levels");

            // SpellMisc.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_MISC, "SELECT ID, DifficultyID, CastingTimeIndex, DurationIndex, RangeIndex, SchoolMask, Speed, LaunchDelay, " +
                "MinDuration, SpellIconFileDataID, ActiveIconFileDataID, ContentTuningID, ShowFutureSpellPlayerConditionID, Attributes1, Attributes2, " +
                "Attributes3, Attributes4, Attributes5, Attributes6, Attributes7, Attributes8, Attributes9, Attributes10, Attributes11, Attributes12, " +
                "Attributes13, Attributes14, SpellID FROM spell_misc");

            // SpellName.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_NAME, "SELECT ID, Name FROM spell_name");
            PrepareStatement(HotfixStatements.SEL_SPELL_NAME_LOCALE, "SELECT ID, Name_lang FROM spell_name_locale WHERE locale = ?");

            // SpellPower.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_POWER, "SELECT ID, OrderIndex, ManaCost, ManaCostPerLevel, ManaPerSecond, PowerDisplayID, AltPowerBarID, " +
                "PowerCostPct, PowerCostMaxPct, PowerPctPerSecond, PowerType, RequiredAuraSpellID, OptionalCost, SpellID FROM spell_power");

            // SpellPowerDifficulty.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_POWER_DIFFICULTY, "SELECT ID, DifficultyID, OrderIndex FROM spell_power_difficulty");

            // SpellProcsPerMinute.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE, "SELECT ID, BaseProcRate, Flags FROM spell_procs_per_minute");

            // SpellProcsPerMinuteMod.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE_MOD, "SELECT ID, Type, Param, Coeff, SpellProcsPerMinuteID FROM spell_procs_per_minute_mod");

            // SpellRadius.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_RADIUS, "SELECT ID, Radius, RadiusPerLevel, RadiusMin, RadiusMax FROM spell_radius");

            // SpellRange.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_RANGE, "SELECT ID, DisplayName, DisplayNameShort, Flags, RangeMin1, RangeMin2, RangeMax1, RangeMax2" +
                " FROM spell_range");
            PrepareStatement(HotfixStatements.SEL_SPELL_RANGE_LOCALE, "SELECT ID, DisplayName_lang, DisplayNameShort_lang FROM spell_range_locale WHERE locale = ?");

            // SpellReagents.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_REAGENTS, "SELECT ID, SpellID, Reagent1, Reagent2, Reagent3, Reagent4, Reagent5, Reagent6, Reagent7, Reagent8, " +
                "ReagentCount1, ReagentCount2, ReagentCount3, ReagentCount4, ReagentCount5, ReagentCount6, ReagentCount7, ReagentCount8 FROM spell_reagents");

            // SpellReagentsCurrency.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_REAGENTS_CURRENCY, "SELECT ID, SpellID, CurrencyTypesID, CurrencyCount FROM spell_reagents_currency");

            // SpellScaling.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_SCALING, "SELECT ID, SpellID, Class, MinScalingLevel, MaxScalingLevel, ScalesFromItemLevel FROM spell_scaling");

            // SpellShapeshift.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_SHAPESHIFT, "SELECT ID, SpellID, StanceBarOrder, ShapeshiftExclude1, ShapeshiftExclude2, ShapeshiftMask1, " +
                "ShapeshiftMask2 FROM spell_shapeshift");

            // SpellShapeshiftForm.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM, "SELECT ID, Name, CreatureType, Flags, AttackIconFileID, BonusActionBar, CombatRoundTime, " +
                "DamageVariance, MountTypeID, CreatureDisplayID1, CreatureDisplayID2, CreatureDisplayID3, CreatureDisplayID4, PresetSpellID1, PresetSpellID2, " +
                "PresetSpellID3, PresetSpellID4, PresetSpellID5, PresetSpellID6, PresetSpellID7, PresetSpellID8 FROM spell_shapeshift_form");
            PrepareStatement(HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM_LOCALE, "SELECT ID, Name_lang FROM spell_shapeshift_form_locale WHERE locale = ?");

            // SpellTargetRestrictions.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_TARGET_RESTRICTIONS, "SELECT ID, DifficultyID, ConeDegrees, MaxTargets, MaxTargetLevel, TargetCreatureType, " +
                "Targets, Width, SpellID FROM spell_target_restrictions");

            // SpellTotems.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_TOTEMS, "SELECT ID, SpellID, RequiredTotemCategoryID1, RequiredTotemCategoryID2, Totem1, Totem2 FROM spell_totems");

            // SpellVisual.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_VISUAL, "SELECT ID, MissileCastOffset1, MissileCastOffset2, MissileCastOffset3, MissileImpactOffset1, " +
                "MissileImpactOffset2, MissileImpactOffset3, AnimEventSoundID, Flags, MissileAttachment, MissileDestinationAttachment, " +
                "MissileCastPositionerID, MissileImpactPositionerID, MissileTargetingKit, HostileSpellVisualID, CasterSpellVisualID, SpellVisualMissileSetID, " +
                "DamageNumberDelay, LowViolenceSpellVisualID, RaidSpellVisualMissileSetID, ReducedUnexpectedCameraMovementSpellVisualID, AreaModel, " +
                "HasMissile FROM spell_visual");

            // SpellVisualEffectName.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_VISUAL_EFFECT_NAME, "SELECT ID, ModelFileDataID, BaseMissileSpeed, Scale, MinAllowedScale, MaxAllowedScale, "+
                "Alpha, Flags, TextureFileDataID, EffectRadius, Type, GenericID, RibbonQualityID, DissolveEffectID, ModelPosition FROM spell_visual_effect_name");
            
            // SpellVisualKit.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_VISUAL_KIT, "SELECT ID, FallbackSpellVisualKitID, DelayMin, DelayMax, FallbackPriority, Flags1, Flags2" +
                " FROM spell_visual_kit");

            // SpellVisualMissile.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_VISUAL_MISSILE, "SELECT CastOffset1, CastOffset2, CastOffset3, ImpactOffset1, ImpactOffset2, ImpactOffset3, ID, " +
                "SpellVisualEffectNameID, SoundEntriesID, Attachment, DestinationAttachment, CastPositionerID, ImpactPositionerID, FollowGroundHeight, " +
                "FollowGroundDropSpeed, FollowGroundApproach, Flags, SpellMissileMotionID, AnimKitID, SpellVisualMissileSetID FROM spell_visual_missile");            

            // SpellXSpellVisual.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_X_SPELL_VISUAL, "SELECT ID, DifficultyID, SpellVisualID, Probability, Flags, Priority, SpellIconFileID, " +
                "ActiveIconFileID, ViewerUnitConditionID, ViewerPlayerConditionID, CasterUnitConditionID, CasterPlayerConditionID, SpellID" +
                " FROM spell_x_spell_visual");

            // SummonProperties.db2
            PrepareStatement(HotfixStatements.SEL_SUMMON_PROPERTIES, "SELECT ID, Control, Faction, Title, Slot, Flags1, Flags2 FROM summon_properties");

            // TactKey.db2
            PrepareStatement(HotfixStatements.SEL_TACT_KEY, "SELECT ID, Key1, Key2, Key3, Key4, Key5, Key6, Key7, Key8, Key9, Key10, Key11, Key12, Key13, Key14, Key15, " +
                "Key16 FROM tact_key");

            // Talent.db2
            PrepareStatement(HotfixStatements.SEL_TALENT, "SELECT ID, Description, TierID, Flags, ColumnIndex, TabID, ClassID, SpecID, SpellID, OverridesSpellID, " +
                "RequiredSpellID, CategoryMask1, CategoryMask2, SpellRank1, SpellRank2, SpellRank3, SpellRank4, SpellRank5, SpellRank6, SpellRank7, " +
                "SpellRank8, SpellRank9, PrereqTalent1, PrereqTalent2, PrereqTalent3, PrereqRank1, PrereqRank2, PrereqRank3 FROM talent");
            PrepareStatement(HotfixStatements.SEL_TALENT_LOCALE, "SELECT ID, Description_lang FROM talent_locale WHERE locale = ?");

            // TaxiNodes.db2
            PrepareStatement(HotfixStatements.SEL_TAXI_NODES, "SELECT Name, PosX, PosY, PosZ, MapOffsetX, MapOffsetY, FlightMapOffsetX, FlightMapOffsetY, ID, " +
                "ContinentID, ConditionID, CharacterBitNumber, Flags, UiTextureKitID, Facing, SpecialIconConditionID, VisibilityConditionID, " +
                "MountCreatureID1, MountCreatureID2 FROM taxi_nodes");
            PrepareStatement(HotfixStatements.SEL_TAXI_NODES_LOCALE, "SELECT ID, Name_lang FROM taxi_nodes_locale WHERE locale = ?");

            // TaxiPath.db2
            PrepareStatement(HotfixStatements.SEL_TAXI_PATH, "SELECT ID, FromTaxiNode, ToTaxiNode, Cost FROM taxi_path");

            // TaxiPathNode.db2
            PrepareStatement(HotfixStatements.SEL_TAXI_PATH_NODE, "SELECT LocX, LocY, LocZ, ID, PathID, NodeIndex, ContinentID, Flags, Delay, ArrivalEventID, " +
                "DepartureEventID FROM taxi_path_node");

            // TotemCategory.db2
            PrepareStatement(HotfixStatements.SEL_TOTEM_CATEGORY, "SELECT ID, Name, TotemCategoryType, TotemCategoryMask FROM totem_category");
            PrepareStatement(HotfixStatements.SEL_TOTEM_CATEGORY_LOCALE, "SELECT ID, Name_lang FROM totem_category_locale WHERE locale = ?");

            // Toy.db2
            PrepareStatement(HotfixStatements.SEL_TOY, "SELECT SourceText, ID, ItemID, Flags, SourceTypeEnum FROM toy");
            PrepareStatement(HotfixStatements.SEL_TOY_LOCALE, "SELECT ID, SourceText_lang FROM toy_locale WHERE locale = ?");

            // TransmogHoliday.db2
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_HOLIDAY, "SELECT ID, RequiredTransmogHoliday FROM transmog_holiday");

            // TransmogSet.db2
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET, "SELECT Name, ID, ClassMask, TrackingQuestID, Flags, TransmogSetGroupID, ItemNameDescriptionID, " +
                "ParentTransmogSetID, ExpansionID, UiOrder FROM transmog_set");
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET_LOCALE, "SELECT ID, Name_lang FROM transmog_set_locale WHERE locale = ?");

            // TransmogSetGroup.db2
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET_GROUP, "SELECT Name, ID FROM transmog_set_group");
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET_GROUP_LOCALE, "SELECT ID, Name_lang FROM transmog_set_group_locale WHERE locale = ?");

            // TransmogSetItem.db2
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET_ITEM, "SELECT ID, TransmogSetID, ItemModifiedAppearanceID, Flags FROM transmog_set_item");

            // TransportAnimation.db2
            PrepareStatement(HotfixStatements.SEL_TRANSPORT_ANIMATION, "SELECT ID, PosX, PosY, PosZ, SequenceID, TimeIndex, TransportID FROM transport_animation");

            // TransportRotation.db2
            PrepareStatement(HotfixStatements.SEL_TRANSPORT_ROTATION, "SELECT ID, Rot1, Rot2, Rot3, Rot4, TimeIndex, GameObjectsID FROM transport_rotation");

            // UiMap.db2
            PrepareStatement(HotfixStatements.SEL_UI_MAP, "SELECT Name, ID, ParentUiMapID, Flags, `System`, Type, BountySetID, BountyDisplayLocation, " +
                "VisibilityPlayerConditionID, HelpTextPosition, BkgAtlasID, LevelRangeMin, LevelRangeMax FROM ui_map");
            PrepareStatement(HotfixStatements.SEL_UI_MAP_LOCALE, "SELECT ID, Name_lang FROM ui_map_locale WHERE locale = ?");

            // UiMapAssignment.db2
            PrepareStatement(HotfixStatements.SEL_UI_MAP_ASSIGNMENT, "SELECT UiMinX, UiMinY, UiMaxX, UiMaxY, Region1X, Region1Y, Region1Z, Region2X, Region2Y, " +
                "Region2Z, ID, UiMapID, OrderIndex, MapID, AreaID, WmoDoodadPlacementID, WmoGroupID FROM ui_map_assignment");

            // UiMapLink.db2
            PrepareStatement(HotfixStatements.SEL_UI_MAP_LINK, "SELECT UiMinX, UiMinY, UiMaxX, UiMaxY, ID, ParentUiMapID, OrderIndex, ChildUiMapID, " +
                "OverrideHighlightFileDataID, OverrideHighlightAtlasID, Flags FROM ui_map_link");

            // UiMapXMapArt.db2
            PrepareStatement(HotfixStatements.SEL_UI_MAP_X_MAP_ART, "SELECT ID, PhaseID, UiMapArtID, UiMapID FROM ui_map_x_map_art");

            // UnitCondition.db2
            PrepareStatement(HotfixStatements.SEL_UNIT_CONDITION, "SELECT ID, Flags, Variable1, Variable2, Variable3, Variable4, Variable5, Variable6, Variable7, " +
                "Variable8, Op1, Op2, Op3, Op4, Op5, Op6, Op7, Op8, Value1, Value2, Value3, Value4, Value5, Value6, Value7, Value8 FROM unit_condition");

            // UnitPowerBar.db2
            PrepareStatement(HotfixStatements.SEL_UNIT_POWER_BAR, "SELECT ID, Name, Cost, OutOfError, ToolTip, MinPower, MaxPower, StartPower, CenterPower, " +
                "RegenerationPeace, RegenerationCombat, BarType, Flags, StartInset, EndInset, FileDataID1, FileDataID2, FileDataID3, FileDataID4, " +
                "FileDataID5, FileDataID6, Color1, Color2, Color3, Color4, Color5, Color6 FROM unit_power_bar");
            PrepareStatement(HotfixStatements.SEL_UNIT_POWER_BAR_LOCALE, "SELECT ID, Name_lang, Cost_lang, OutOfError_lang, ToolTip_lang FROM unit_power_bar_locale" +
                " WHERE locale = ?");

            // Vehicle.db2
            PrepareStatement(HotfixStatements.SEL_VEHICLE, "SELECT ID, Flags, FlagsB, TurnSpeed, PitchSpeed, PitchMin, PitchMax, MouseLookOffsetPitch, " +
                "CameraFadeDistScalarMin, CameraFadeDistScalarMax, CameraPitchOffset, FacingLimitRight, FacingLimitLeft, CameraYawOffset, " +
                "VehicleUIIndicatorID, MissileTargetingID, UiLocomotionType, SeatID1, SeatID2, SeatID3, SeatID4, SeatID5, SeatID6, SeatID7, SeatID8, " +
                "PowerDisplayID1, PowerDisplayID2, PowerDisplayID3 FROM vehicle");

            // VehicleSeat.db2
            PrepareStatement(HotfixStatements.SEL_VEHICLE_SEAT, "SELECT ID, AttachmentOffsetX, AttachmentOffsetY, AttachmentOffsetZ, CameraOffsetX, CameraOffsetY, " +
                "CameraOffsetZ, Flags, FlagsB, FlagsC, AttachmentID, EnterPreDelay, EnterSpeed, EnterGravity, EnterMinDuration, EnterMaxDuration, " +
                "EnterMinArcHeight, EnterMaxArcHeight, EnterAnimStart, EnterAnimLoop, RideAnimStart, RideAnimLoop, RideUpperAnimStart, RideUpperAnimLoop, " +
                "ExitPreDelay, ExitSpeed, ExitGravity, ExitMinDuration, ExitMaxDuration, ExitMinArcHeight, ExitMaxArcHeight, ExitAnimStart, ExitAnimLoop, " +
                "ExitAnimEnd, VehicleEnterAnim, VehicleEnterAnimBone, VehicleExitAnim, VehicleExitAnimBone, VehicleRideAnimLoop, VehicleRideAnimLoopBone, " +
                "PassengerAttachmentID, PassengerYaw, PassengerPitch, PassengerRoll, VehicleEnterAnimDelay, VehicleExitAnimDelay, VehicleAbilityDisplay, " +
                "EnterUISoundID, ExitUISoundID, UiSkinFileDataID, UiSkin, CameraEnteringDelay, CameraEnteringDuration, CameraExitingDelay, " +
                "CameraExitingDuration, CameraPosChaseRate, CameraFacingChaseRate, CameraEnteringZoom, CameraSeatZoomMin, CameraSeatZoomMax, EnterAnimKitID, " +
                "RideAnimKitID, ExitAnimKitID, VehicleEnterAnimKitID, VehicleRideAnimKitID, VehicleExitAnimKitID, CameraModeID FROM vehicle_seat");

            // WmoAreaTable.db2
            PrepareStatement(HotfixStatements.SEL_WMO_AREA_TABLE, "SELECT AreaName, ID, WmoID, NameSetID, WmoGroupID, SoundProviderPref, SoundProviderPrefUnderwater, " +
                "AmbienceID, UwAmbience, ZoneMusic, UwZoneMusic, IntroSound, UwIntroSound, AreaTableID, Flags FROM wmo_area_table");
            PrepareStatement(HotfixStatements.SEL_WMO_AREA_TABLE_LOCALE, "SELECT ID, AreaName_lang FROM wmo_area_table_locale WHERE locale = ?");

            // WorldEffect.db2
            PrepareStatement(HotfixStatements.SEL_WORLD_EFFECT, "SELECT ID, QuestFeedbackEffectID, WhenToDisplay, TargetType, TargetAsset, PlayerConditionID, " +
                "CombatConditionID FROM world_effect");

            // WorldMapOverlay.db2
            PrepareStatement(HotfixStatements.SEL_WORLD_MAP_OVERLAY, "SELECT ID, UiMapArtID, TextureWidth, TextureHeight, OffsetX, OffsetY, HitRectTop, HitRectBottom, " +
                "HitRectLeft, HitRectRight, PlayerConditionID, Flags, AreaID1, AreaID2, AreaID3, AreaID4 FROM world_map_overlay");

            // WorldStateExpression.db2
            PrepareStatement(HotfixStatements.SEL_WORLD_STATE_EXPRESSION, "SELECT ID, Expression FROM world_state_expression");
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
        SEL_AREA_TRIGGER_LOCALE,

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

        SEL_BROADCAST_TEXT,
        SEL_BROADCAST_TEXT_LOCALE,

        SEL_BROADCAST_TEXT_DURATION,

        SEL_CFG_REGIONS,

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

        SEL_CHR_CUSTOMIZATION_REQ_CHOICE,

        SEL_CHR_MODEL,

        SEL_CHR_RACE_X_CHR_MODEL,

        SEL_CHR_RACES,
        SEL_CHR_RACES_LOCALE,

        SEL_CHR_SPECIALIZATION,
        SEL_CHR_SPECIALIZATION_LOCALE,

        SEL_CINEMATIC_CAMERA,

        SEL_CINEMATIC_SEQUENCES,

        SEL_CONTENT_TUNING,

        SEL_CONVERSATION_LINE,

        SEL_CREATURE_DISPLAY_INFO,

        SEL_CREATURE_DISPLAY_INFO_EXTRA,

        SEL_CREATURE_FAMILY,
        SEL_CREATURE_FAMILY_LOCALE,

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

        SEL_FRIENDSHIP_REP_REACTION,
        SEL_FRIENDSHIP_REP_REACTION_LOCALE,

        SEL_FRIENDSHIP_REPUTATION,
        SEL_FRIENDSHIP_REPUTATION_LOCALE,

        SEL_GAMEOBJECT_ART_KIT,

        SEL_GAMEOBJECT_DISPLAY_INFO,

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

        SEL_GLYPH_BINDABLE_SPELL,

        SEL_GLYPH_PROPERTIES,

        SEL_GLYPH_REQUIRED_SPEC,

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

        SEL_ITEM_BONUS_LIST_LEVEL_DELTA,

        SEL_ITEM_BONUS_TREE_NODE,

        SEL_ITEM_CHILD_EQUIPMENT,

        SEL_ITEM_CLASS,
        SEL_ITEM_CLASS_LOCALE,

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

        SEL_MODIFIER_TREE,

        SEL_MOUNT,
        SEL_MOUNT_LOCALE,

        SEL_MOUNT_CAPABILITY,

        SEL_MOUNT_TYPE_X_CAPABILITY,

        SEL_MOUNT_X_DISPLAY,

        SEL_MOVIE,

        SEL_NAME_GEN,

        SEL_NAMES_PROFANITY,

        SEL_NAMES_RESERVED,

        SEL_NAMES_RESERVED_LOCALE,

        SEL_NUM_TALENTS_AT_LEVEL,

        SEL_OVERRIDE_SPELL_DATA,

        SEL_PARAGON_REPUTATION,

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

        SEL_SKILL_LINE,
        SEL_SKILL_LINE_LOCALE,

        SEL_SKILL_LINE_ABILITY,

        SEL_SKILL_RACE_CLASS_INFO,

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

        SEL_SPELL_EQUIPPED_ITEMS,

        SEL_SPELL_FOCUS_OBJECT,
        SEL_SPELL_FOCUS_OBJECT_LOCALE,

        SEL_SPELL_INTERRUPTS,

        SEL_SPELL_ITEM_ENCHANTMENT,
        SEL_SPELL_ITEM_ENCHANTMENT_LOCALE,

        SEL_SPELL_ITEM_ENCHANTMENT_CONDITION,

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

        SEL_UNIT_CONDITION,

        SEL_UNIT_POWER_BAR,
        SEL_UNIT_POWER_BAR_LOCALE,

        SEL_VEHICLE,

        SEL_VEHICLE_SEAT,

        SEL_WMO_AREA_TABLE,
        SEL_WMO_AREA_TABLE_LOCALE,

        SEL_WORLD_EFFECT,

        SEL_WORLD_MAP_OVERLAY,

        SEL_WORLD_STATE_EXPRESSION,

        MAX_HOTFIXDATABASE_STATEMENTS
    }
}
