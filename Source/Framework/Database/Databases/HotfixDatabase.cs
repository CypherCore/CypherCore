/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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
            PrepareStatement(HotfixStatements.SEL_ACHIEVEMENT, "SELECT Title, Description, Reward, Flags, InstanceID, Supercedes, Category, UiOrder, SharesCriteria, " +
                "Faction, Points, MinimumCriteria, ID, IconFileID, CriteriaTree FROM achievement ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_ACHIEVEMENT_LOCALE, "SELECT ID, Title_lang, Description_lang, Reward_lang FROM achievement_locale WHERE locale = ?");

            // AnimKit.db2
            PrepareStatement(HotfixStatements.SEL_ANIM_KIT, "SELECT ID, OneShotDuration, OneShotStopAnimKitID, LowDefAnimKitID FROM anim_kit ORDER BY ID DESC");

            // AreaGroupMember.db2
            PrepareStatement(HotfixStatements.SEL_AREA_GROUP_MEMBER, "SELECT ID, AreaID, AreaGroupID FROM area_group_member ORDER BY ID DESC");

            // AreaTable.db2
            PrepareStatement(HotfixStatements.SEL_AREA_TABLE, "SELECT ID, ZoneName, AreaName, Flags1, Flags2, AmbientMultiplier, ContinentID, ParentAreaID, AreaBit, " +
                "AmbienceID, ZoneMusic, IntroSound, LiquidTypeID1, LiquidTypeID2, LiquidTypeID3, LiquidTypeID4, UwZoneMusic, UwAmbience, " +
                "PvpCombatWorldStateID, SoundProviderPref, SoundProviderPrefUnderwater, ExplorationLevel, FactionGroupMask, MountFlags, " +
                "WildBattlePetLevelMin, WildBattlePetLevelMax, WindSettingsID, UwIntroSound FROM area_table ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_AREA_TABLE_LOCALE, "SELECT ID, AreaName_lang FROM area_table_locale WHERE locale = ?");

            // AreaTrigger.db2
            PrepareStatement(HotfixStatements.SEL_AREA_TRIGGER, "SELECT PosX, PosY, PosZ, Radius, BoxLength, BoxWidth, BoxHeight, BoxYaw, ContinentID, PhaseID, " +
                "PhaseGroupID, ShapeID, AreaTriggerActionSetID, PhaseUseFlags, ShapeType, Flags, ID FROM area_trigger ORDER BY ID DESC");

            // ArmorLocation.db2
            PrepareStatement(HotfixStatements.SEL_ARMOR_LOCATION, "SELECT ID, Clothmodifier, Leathermodifier, Chainmodifier, Platemodifier, Modifier FROM armor_location" +
                " ORDER BY ID DESC");

            // Artifact.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT, "SELECT ID, Name, UiBarOverlayColor, UiBarBackgroundColor, UiNameColor, UiTextureKitID, " +
                "ChrSpecializationID, ArtifactCategoryID, Flags, UiModelSceneID, SpellVisualKitID FROM artifact ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_LOCALE, "SELECT ID, Name_lang FROM artifact_locale WHERE locale = ?");

            // ArtifactAppearance.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_APPEARANCE, "SELECT Name, UiSwatchColor, UiModelSaturation, UiModelOpacity, OverrideShapeshiftDisplayID, " +
                "ArtifactAppearanceSetID, UiCameraID, DisplayIndex, ItemAppearanceModifierID, Flags, OverrideShapeshiftFormID, ID, UnlockPlayerConditionID, " +
                "UiItemAppearanceID, UiAltItemAppearanceID FROM artifact_appearance ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_APPEARANCE_LOCALE, "SELECT ID, Name_lang FROM artifact_appearance_locale WHERE locale = ?");

            // ArtifactAppearanceSet.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET, "SELECT Name, Description, UiCameraID, AltHandUICameraID, DisplayIndex, " +
                "ForgeAttachmentOverride, Flags, ID, ArtifactID FROM artifact_appearance_set ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET_LOCALE, "SELECT ID, Name_lang, Description_lang FROM artifact_appearance_set_locale" +
                " WHERE locale = ?");

            // ArtifactCategory.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_CATEGORY, "SELECT ID, XpMultCurrencyID, XpMultCurveID FROM artifact_category ORDER BY ID DESC");

            // ArtifactPower.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_POWER, "SELECT PosX, PosY, ArtifactID, Flags, MaxPurchasableRank, Tier, ID, Label FROM artifact_power" +
                " ORDER BY ID DESC");

            // ArtifactPowerLink.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_POWER_LINK, "SELECT ID, PowerA, PowerB FROM artifact_power_link ORDER BY ID DESC");

            // ArtifactPowerPicker.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_POWER_PICKER, "SELECT ID, PlayerConditionID FROM artifact_power_picker ORDER BY ID DESC");

            // ArtifactPowerRank.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_POWER_RANK, "SELECT ID, SpellID, AuraPointsOverride, ItemBonusListID, RankIndex, ArtifactPowerID" +
                " FROM artifact_power_rank ORDER BY ID DESC");

            // ArtifactQuestXp.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_QUEST_XP, "SELECT ID, Difficulty1, Difficulty2, Difficulty3, Difficulty4, Difficulty5, Difficulty6, " +
                "Difficulty7, Difficulty8, Difficulty9, Difficulty10 FROM artifact_quest_xp ORDER BY ID DESC");

            // ArtifactTier.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_TIER, "SELECT ID, ArtifactTier, MaxNumTraits, MaxArtifactKnowledge, KnowledgePlayerCondition, " +
                "MinimumEmpowerKnowledge FROM artifact_tier ORDER BY ID DESC");

            // ArtifactUnlock.db2
            PrepareStatement(HotfixStatements.SEL_ARTIFACT_UNLOCK, "SELECT ID, ItemBonusListID, PowerRank, PowerID, PlayerConditionID, ArtifactID FROM artifact_unlock" +
                " ORDER BY ID DESC");

            // AuctionHouse.db2
            PrepareStatement(HotfixStatements.SEL_AUCTION_HOUSE, "SELECT ID, Name, FactionID, DepositRate, ConsignmentRate FROM auction_house ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_AUCTION_HOUSE_LOCALE, "SELECT ID, Name_lang FROM auction_house_locale WHERE locale = ?");

            // BankBagSlotPrices.db2
            PrepareStatement(HotfixStatements.SEL_BANK_BAG_SLOT_PRICES, "SELECT ID, Cost FROM bank_bag_slot_prices ORDER BY ID DESC");

            // BannedAddons.db2
            PrepareStatement(HotfixStatements.SEL_BANNED_ADDONS, "SELECT ID, Name, Version, Flags FROM banned_addons ORDER BY ID DESC");

            // BarberShopStyle.db2
            PrepareStatement(HotfixStatements.SEL_BARBER_SHOP_STYLE, "SELECT DisplayName, Description, CostModifier, Type, Race, Sex, Data, ID FROM barber_shop_style" +
                " ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_BARBER_SHOP_STYLE_LOCALE, "SELECT ID, DisplayName_lang, Description_lang FROM barber_shop_style_locale WHERE locale = ?");

            // BattlePetBreedQuality.db2
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_BREED_QUALITY, "SELECT ID, StateMultiplier, QualityEnum FROM battle_pet_breed_quality ORDER BY ID DESC");

            // BattlePetBreedState.db2
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_BREED_STATE, "SELECT ID, Value, BattlePetStateID, BattlePetBreedID FROM battle_pet_breed_state" +
                " ORDER BY ID DESC");

            // BattlePetSpecies.db2
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_SPECIES, "SELECT SourceText, Description, CreatureID, IconFileDataID, SummonSpellID, Flags, PetTypeEnum, " +
                "SourceTypeEnum, ID, CardUIModelSceneID, LoadoutUIModelSceneID FROM battle_pet_species ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_SPECIES_LOCALE, "SELECT ID, SourceText_lang, Description_lang FROM battle_pet_species_locale WHERE locale = ?");

            // BattlePetSpeciesState.db2
            PrepareStatement(HotfixStatements.SEL_BATTLE_PET_SPECIES_STATE, "SELECT ID, Value, BattlePetStateID, BattlePetSpeciesID FROM battle_pet_species_state" +
                " ORDER BY ID DESC");

            // BattlemasterList.db2
            PrepareStatement(HotfixStatements.SEL_BATTLEMASTER_LIST, "SELECT ID, Name, GameType, ShortDescription, LongDescription, IconFileDataID, MapID1, MapID2, " +
                "MapID3, MapID4, MapID5, MapID6, MapID7, MapID8, MapID9, MapID10, MapID11, MapID12, MapID13, MapID14, MapID15, MapID16, HolidayWorldState, " +
                "RequiredPlayerConditionID, InstanceType, GroupsAllowed, MaxGroupSize, MinLevel, MaxLevel, RatedPlayers, MinPlayers, MaxPlayers, Flags" +
                " FROM battlemaster_list ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_BATTLEMASTER_LIST_LOCALE, "SELECT ID, Name_lang, GameType_lang, ShortDescription_lang, LongDescription_lang" +
                " FROM battlemaster_list_locale WHERE locale = ?");

            // BroadcastText.db2
            PrepareStatement(HotfixStatements.SEL_BROADCAST_TEXT, "SELECT ID, Text, Text1, EmoteID1, EmoteID2, EmoteID3, EmoteDelay1, EmoteDelay2, EmoteDelay3, " +
                "EmotesID, LanguageID, Flags, ConditionID, SoundEntriesID1, SoundEntriesID2 FROM broadcast_text ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_BROADCAST_TEXT_LOCALE, "SELECT ID, Text_lang, Text1_lang FROM broadcast_text_locale WHERE locale = ?");

            // CharacterFacialHairStyles.db2
            PrepareStatement(HotfixStatements.SEL_CHARACTER_FACIAL_HAIR_STYLES, "SELECT ID, Geoset1, Geoset2, Geoset3, Geoset4, Geoset5, RaceID, SexID, VariationID" +
                " FROM character_facial_hair_styles ORDER BY ID DESC");

            // CharBaseSection.db2
            PrepareStatement(HotfixStatements.SEL_CHAR_BASE_SECTION, "SELECT ID, VariationEnum, ResolutionVariationEnum, LayoutResType FROM char_base_section" +
                " ORDER BY ID DESC");

            // CharSections.db2
            PrepareStatement(HotfixStatements.SEL_CHAR_SECTIONS, "SELECT ID, MaterialResourcesID1, MaterialResourcesID2, MaterialResourcesID3, Flags, RaceID, SexID, " +
                "BaseSection, VariationIndex, ColorIndex FROM char_sections ORDER BY ID DESC");

            // CharStartOutfit.db2
            PrepareStatement(HotfixStatements.SEL_CHAR_START_OUTFIT, "SELECT ID, ItemID1, ItemID2, ItemID3, ItemID4, ItemID5, ItemID6, ItemID7, ItemID8, ItemID9, " +
                "ItemID10, ItemID11, ItemID12, ItemID13, ItemID14, ItemID15, ItemID16, ItemID17, ItemID18, ItemID19, ItemID20, ItemID21, ItemID22, ItemID23, " +
                "ItemID24, PetDisplayID, ClassID, SexID, OutfitID, PetFamilyID, RaceID FROM char_start_outfit ORDER BY ID DESC");

            // CharTitles.db2
            PrepareStatement(HotfixStatements.SEL_CHAR_TITLES, "SELECT ID, Name, Name1, MaskID, Flags FROM char_titles ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_CHAR_TITLES_LOCALE, "SELECT ID, Name_lang, Name1_lang FROM char_titles_locale WHERE locale = ?");

            // ChatChannels.db2
            PrepareStatement(HotfixStatements.SEL_CHAT_CHANNELS, "SELECT ID, Name, Shortcut, Flags, FactionGroup FROM chat_channels ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_CHAT_CHANNELS_LOCALE, "SELECT ID, Name_lang, Shortcut_lang FROM chat_channels_locale WHERE locale = ?");

            // ChrClasses.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CLASSES, "SELECT PetNameToken, Name, NameFemale, NameMale, Filename, CreateScreenFileDataID, " +
                "SelectScreenFileDataID, LowResScreenFileDataID, IconFileDataID, StartingLevel, Flags, CinematicSequenceID, DefaultSpec, DisplayPower, " +
                "SpellClassSet, AttackPowerPerStrength, AttackPowerPerAgility, RangedAttackPowerPerAgility, PrimaryStatPriority, ID FROM chr_classes" +
                " ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_CHR_CLASSES_LOCALE, "SELECT ID, Name_lang, NameFemale_lang, NameMale_lang FROM chr_classes_locale WHERE locale = ?");

            // ChrClassesXPowerTypes.db2
            PrepareStatement(HotfixStatements.SEL_CHR_CLASSES_X_POWER_TYPES, "SELECT ID, PowerType, ClassID FROM chr_classes_x_power_types ORDER BY ID DESC");

            // ChrRaces.db2
            PrepareStatement(HotfixStatements.SEL_CHR_RACES, "SELECT ClientPrefix, ClientFileString, Name, NameFemale, NameLowercase, NameFemaleLowercase, Flags, " +
                "MaleDisplayId, FemaleDisplayId, CreateScreenFileDataID, SelectScreenFileDataID, MaleCustomizeOffset1, MaleCustomizeOffset2, " +
                "MaleCustomizeOffset3, FemaleCustomizeOffset1, FemaleCustomizeOffset2, FemaleCustomizeOffset3, LowResScreenFileDataID, StartingLevel, " +
                "UiDisplayOrder, FactionID, ResSicknessSpellID, SplashSoundID, CinematicSequenceID, BaseLanguage, CreatureType, Alliance, RaceRelated, " +
                "UnalteredVisualRaceID, CharComponentTextureLayoutID, DefaultClassID, NeutralRaceID, DisplayRaceID, CharComponentTexLayoutHiResID, ID, " +
                "HighResMaleDisplayId, HighResFemaleDisplayId, HeritageArmorAchievementID, MaleSkeletonFileDataID, FemaleSkeletonFileDataID, " +
                "AlteredFormStartVisualKitID1, AlteredFormStartVisualKitID2, AlteredFormStartVisualKitID3, AlteredFormFinishVisualKitID1, " +
                "AlteredFormFinishVisualKitID2, AlteredFormFinishVisualKitID3 FROM chr_races ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_CHR_RACES_LOCALE, "SELECT ID, Name_lang, NameFemale_lang, NameLowercase_lang, NameFemaleLowercase_lang" +
                " FROM chr_races_locale WHERE locale = ?");

            // ChrSpecialization.db2
            PrepareStatement(HotfixStatements.SEL_CHR_SPECIALIZATION, "SELECT Name, FemaleName, Description, MasterySpellID1, MasterySpellID2, ClassID, OrderIndex, " +
                "PetTalentType, Role, PrimaryStatPriority, ID, SpellIconFileID, Flags, AnimReplacements FROM chr_specialization ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_CHR_SPECIALIZATION_LOCALE, "SELECT ID, Name_lang, FemaleName_lang, Description_lang FROM chr_specialization_locale" +
                " WHERE locale = ?");

            // CinematicCamera.db2
            PrepareStatement(HotfixStatements.SEL_CINEMATIC_CAMERA, "SELECT ID, SoundID, OriginX, OriginY, OriginZ, OriginFacing, FileDataID FROM cinematic_camera" +
                " ORDER BY ID DESC");

            // CinematicSequences.db2
            PrepareStatement(HotfixStatements.SEL_CINEMATIC_SEQUENCES, "SELECT ID, SoundID, Camera1, Camera2, Camera3, Camera4, Camera5, Camera6, Camera7, Camera8" +
                " FROM cinematic_sequences ORDER BY ID DESC");

            // ConversationLine.db2
            PrepareStatement(HotfixStatements.SEL_CONVERSATION_LINE, "SELECT ID, BroadcastTextID, SpellVisualKitID, AdditionalDuration, NextConversationLineID, " +
                "AnimKitID, SpeechType, StartAnimation, EndAnimation FROM conversation_line ORDER BY ID DESC");

            // CreatureDisplayInfo.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_DISPLAY_INFO, "SELECT ID, CreatureModelScale, ModelID, NPCSoundID, SizeClass, Flags, Gender, " +
                "ExtendedDisplayInfoID, PortraitTextureFileDataID, CreatureModelAlpha, SoundID, PlayerOverrideScale, PortraitCreatureDisplayInfoID, BloodID, " +
                "ParticleColorID, CreatureGeosetData, ObjectEffectPackageID, AnimReplacementSetID, UnarmedWeaponType, StateSpellVisualKitID, " +
                "PetInstanceScale, MountPoofSpellVisualKitID, TextureVariationFileDataID1, TextureVariationFileDataID2, TextureVariationFileDataID3" +
                " FROM creature_display_info ORDER BY ID DESC");

            // CreatureDisplayInfoExtra.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_DISPLAY_INFO_EXTRA, "SELECT ID, BakeMaterialResourcesID, HDBakeMaterialResourcesID, DisplayRaceID, " +
                "DisplaySexID, DisplayClassID, SkinID, FaceID, HairStyleID, HairColorID, FacialHairID, CustomDisplayOption1, CustomDisplayOption2, " +
                "CustomDisplayOption3, Flags FROM creature_display_info_extra ORDER BY ID DESC");

            // CreatureFamily.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_FAMILY, "SELECT ID, Name, MinScale, MaxScale, IconFileID, SkillLine1, SkillLine2, PetFoodMask, " +
                "MinScaleLevel, MaxScaleLevel, PetTalentType FROM creature_family ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_CREATURE_FAMILY_LOCALE, "SELECT ID, Name_lang FROM creature_family_locale WHERE locale = ?");

            // CreatureModelData.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_MODEL_DATA, "SELECT ID, ModelScale, FootprintTextureLength, FootprintTextureWidth, FootprintParticleScale, " +
                "CollisionWidth, CollisionHeight, MountHeight, GeoBox1, GeoBox2, GeoBox3, GeoBox4, GeoBox5, GeoBox6, WorldEffectScale, AttachedEffectScale, " +
                "MissileCollisionRadius, MissileCollisionPush, MissileCollisionRaise, OverrideLootEffectScale, OverrideNameScale, OverrideSelectionRadius, " +
                "TamedPetBaseScale, HoverHeight, Flags, FileDataID, SizeClass, BloodID, FootprintTextureID, FoleyMaterialID, FootstepCameraEffectID, " +
                "DeathThudCameraEffectID, SoundID, CreatureGeosetDataID FROM creature_model_data ORDER BY ID DESC");

            // CreatureType.db2
            PrepareStatement(HotfixStatements.SEL_CREATURE_TYPE, "SELECT ID, Name, Flags FROM creature_type ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_CREATURE_TYPE_LOCALE, "SELECT ID, Name_lang FROM creature_type_locale WHERE locale = ?");

            // Criteria.db2
            PrepareStatement(HotfixStatements.SEL_CRITERIA, "SELECT ID, Asset, StartAsset, FailAsset, ModifierTreeId, StartTimer, EligibilityWorldStateID, Type, " +
                "StartEvent, FailEvent, Flags, EligibilityWorldStateValue FROM criteria ORDER BY ID DESC");

            // CriteriaTree.db2
            PrepareStatement(HotfixStatements.SEL_CRITERIA_TREE, "SELECT ID, Description, Amount, Flags, Operator, CriteriaID, Parent, OrderIndex FROM criteria_tree" +
                " ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_CRITERIA_TREE_LOCALE, "SELECT ID, Description_lang FROM criteria_tree_locale WHERE locale = ?");

            // CurrencyTypes.db2
            PrepareStatement(HotfixStatements.SEL_CURRENCY_TYPES, "SELECT ID, Name, Description, MaxQty, MaxEarnablePerWeek, Flags, CategoryID, SpellCategory, Quality, " +
                "InventoryIconFileID, SpellWeight FROM currency_types ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_CURRENCY_TYPES_LOCALE, "SELECT ID, Name_lang, Description_lang FROM currency_types_locale WHERE locale = ?");

            // Curve.db2
            PrepareStatement(HotfixStatements.SEL_CURVE, "SELECT ID, Type, Flags FROM curve ORDER BY ID DESC");

            // CurvePoint.db2
            PrepareStatement(HotfixStatements.SEL_CURVE_POINT, "SELECT ID, PosX, PosY, CurveID, OrderIndex FROM curve_point ORDER BY ID DESC");

            // DestructibleModelData.db2
            PrepareStatement(HotfixStatements.SEL_DESTRUCTIBLE_MODEL_DATA, "SELECT ID, State0Wmo, State1Wmo, State2Wmo, State3Wmo, HealEffectSpeed, " +
                "State0ImpactEffectDoodadSet, State0AmbientDoodadSet, State0NameSet, State1DestructionDoodadSet, State1ImpactEffectDoodadSet, " +
                "State1AmbientDoodadSet, State1NameSet, State2DestructionDoodadSet, State2ImpactEffectDoodadSet, State2AmbientDoodadSet, State2NameSet, " +
                "State3InitDoodadSet, State3AmbientDoodadSet, State3NameSet, EjectDirection, DoNotHighlight, HealEffect FROM destructible_model_data" +
                " ORDER BY ID DESC");

            // Difficulty.db2
            PrepareStatement(HotfixStatements.SEL_DIFFICULTY, "SELECT ID, Name, GroupSizeHealthCurveID, GroupSizeDmgCurveID, GroupSizeSpellPointsCurveID, " +
                "FallbackDifficultyID, InstanceType, MinPlayers, MaxPlayers, OldEnumValue, Flags, ToggleDifficultyID, ItemContext, OrderIndex FROM difficulty" +
                " ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_DIFFICULTY_LOCALE, "SELECT ID, Name_lang FROM difficulty_locale WHERE locale = ?");

            // DungeonEncounter.db2
            PrepareStatement(HotfixStatements.SEL_DUNGEON_ENCOUNTER, "SELECT Name, CreatureDisplayID, MapID, DifficultyID, Bit, Flags, ID, OrderIndex, SpellIconFileID" +
                " FROM dungeon_encounter ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_DUNGEON_ENCOUNTER_LOCALE, "SELECT ID, Name_lang FROM dungeon_encounter_locale WHERE locale = ?");

            // DurabilityCosts.db2
            PrepareStatement(HotfixStatements.SEL_DURABILITY_COSTS, "SELECT ID, WeaponSubClassCost1, WeaponSubClassCost2, WeaponSubClassCost3, WeaponSubClassCost4, " +
                "WeaponSubClassCost5, WeaponSubClassCost6, WeaponSubClassCost7, WeaponSubClassCost8, WeaponSubClassCost9, WeaponSubClassCost10, " +
                "WeaponSubClassCost11, WeaponSubClassCost12, WeaponSubClassCost13, WeaponSubClassCost14, WeaponSubClassCost15, WeaponSubClassCost16, " +
                "WeaponSubClassCost17, WeaponSubClassCost18, WeaponSubClassCost19, WeaponSubClassCost20, WeaponSubClassCost21, ArmorSubClassCost1, " +
                "ArmorSubClassCost2, ArmorSubClassCost3, ArmorSubClassCost4, ArmorSubClassCost5, ArmorSubClassCost6, ArmorSubClassCost7, ArmorSubClassCost8" +
                " FROM durability_costs ORDER BY ID DESC");

            // DurabilityQuality.db2
            PrepareStatement(HotfixStatements.SEL_DURABILITY_QUALITY, "SELECT ID, Data FROM durability_quality ORDER BY ID DESC");

            // Emotes.db2
            PrepareStatement(HotfixStatements.SEL_EMOTES, "SELECT ID, RaceMask, EmoteSlashCommand, EmoteFlags, SpellVisualKitID, AnimID, EmoteSpecProc, ClassMask, " +
                "EmoteSpecProcParam, EventSoundID FROM emotes ORDER BY ID DESC");

            // EmotesText.db2
            PrepareStatement(HotfixStatements.SEL_EMOTES_TEXT, "SELECT ID, Name, EmoteID FROM emotes_text ORDER BY ID DESC");

            // EmotesTextSound.db2
            PrepareStatement(HotfixStatements.SEL_EMOTES_TEXT_SOUND, "SELECT ID, RaceID, SexID, ClassID, SoundID, EmotesTextID FROM emotes_text_sound ORDER BY ID DESC");

            // Faction.db2
            PrepareStatement(HotfixStatements.SEL_FACTION, "SELECT ReputationRaceMask1, ReputationRaceMask2, ReputationRaceMask3, ReputationRaceMask4, Name, " +
                "Description, ID, ReputationBase1, ReputationBase2, ReputationBase3, ReputationBase4, ParentFactionMod1, ParentFactionMod2, ReputationMax1, " +
                "ReputationMax2, ReputationMax3, ReputationMax4, ReputationIndex, ReputationClassMask1, ReputationClassMask2, ReputationClassMask3, " +
                "ReputationClassMask4, ReputationFlags1, ReputationFlags2, ReputationFlags3, ReputationFlags4, ParentFactionID, ParagonFactionID, " +
                "ParentFactionCap1, ParentFactionCap2, Expansion, Flags, FriendshipRepID FROM faction ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_FACTION_LOCALE, "SELECT ID, Name_lang, Description_lang FROM faction_locale WHERE locale = ?");

            // FactionTemplate.db2
            PrepareStatement(HotfixStatements.SEL_FACTION_TEMPLATE, "SELECT ID, Faction, Flags, Enemies1, Enemies2, Enemies3, Enemies4, Friend1, Friend2, Friend3, " +
                "Friend4, FactionGroup, FriendGroup, EnemyGroup FROM faction_template ORDER BY ID DESC");

            // GameobjectDisplayInfo.db2
            PrepareStatement(HotfixStatements.SEL_GAMEOBJECT_DISPLAY_INFO, "SELECT ID, FileDataID, GeoBoxMinX, GeoBoxMinY, GeoBoxMinZ, GeoBoxMaxX, GeoBoxMaxY, " +
                "GeoBoxMaxZ, OverrideLootEffectScale, OverrideNameScale, ObjectEffectPackageID FROM gameobject_display_info ORDER BY ID DESC");

            // Gameobjects.db2
            PrepareStatement(HotfixStatements.SEL_GAMEOBJECTS, "SELECT Name, PosX, PosY, PosZ, Rot1, Rot2, Rot3, Rot4, Scale, PropValue1, PropValue2, PropValue3, " +
                "PropValue4, PropValue5, PropValue6, PropValue7, PropValue8, OwnerID, DisplayID, PhaseID, PhaseGroupID, PhaseUseFlags, TypeID, ID" +
                " FROM gameobjects ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_GAMEOBJECTS_LOCALE, "SELECT ID, Name_lang FROM gameobjects_locale WHERE locale = ?");

            // GarrAbility.db2
            PrepareStatement(HotfixStatements.SEL_GARR_ABILITY, "SELECT Name, Description, IconFileDataID, Flags, FactionChangeGarrAbilityID, GarrAbilityCategoryID, " +
                "GarrFollowerTypeID, ID FROM garr_ability ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_GARR_ABILITY_LOCALE, "SELECT ID, Name_lang, Description_lang FROM garr_ability_locale WHERE locale = ?");

            // GarrBuilding.db2
            PrepareStatement(HotfixStatements.SEL_GARR_BUILDING, "SELECT ID, AllianceName, HordeName, Description, Tooltip, HordeGameObjectID, AllianceGameObjectID, " +
                "IconFileDataID, CurrencyTypeID, HordeUiTextureKitID, AllianceUiTextureKitID, AllianceSceneScriptPackageID, HordeSceneScriptPackageID, " +
                "GarrAbilityID, BonusGarrAbilityID, GoldCost, GarrSiteID, BuildingType, UpgradeLevel, Flags, ShipmentCapacity, GarrTypeID, BuildSeconds, " +
                "CurrencyQty, MaxAssignments FROM garr_building ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_GARR_BUILDING_LOCALE, "SELECT ID, AllianceName_lang, HordeName_lang, Description_lang, Tooltip_lang" +
                " FROM garr_building_locale WHERE locale = ?");

            // GarrBuildingPlotInst.db2
            PrepareStatement(HotfixStatements.SEL_GARR_BUILDING_PLOT_INST, "SELECT MapOffsetX, MapOffsetY, UiTextureAtlasMemberID, GarrSiteLevelPlotInstID, " +
                "GarrBuildingID, ID FROM garr_building_plot_inst ORDER BY ID DESC");

            // GarrClassSpec.db2
            PrepareStatement(HotfixStatements.SEL_GARR_CLASS_SPEC, "SELECT ClassSpec, ClassSpecMale, ClassSpecFemale, UiTextureAtlasMemberID, GarrFollItemSetID, " +
                "FollowerClassLimit, Flags, ID FROM garr_class_spec ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_GARR_CLASS_SPEC_LOCALE, "SELECT ID, ClassSpec_lang, ClassSpecMale_lang, ClassSpecFemale_lang FROM garr_class_spec_locale" +
                " WHERE locale = ?");

            // GarrFollower.db2
            PrepareStatement(HotfixStatements.SEL_GARR_FOLLOWER, "SELECT HordeSourceText, AllianceSourceText, TitleName, HordeCreatureID, AllianceCreatureID, " +
                "HordeIconFileDataID, AllianceIconFileDataID, HordeSlottingBroadcastTextID, AllySlottingBroadcastTextID, HordeGarrFollItemSetID, " +
                "AllianceGarrFollItemSetID, ItemLevelWeapon, ItemLevelArmor, HordeUITextureKitID, AllianceUITextureKitID, GarrFollowerTypeID, " +
                "HordeGarrFollRaceID, AllianceGarrFollRaceID, Quality, HordeGarrClassSpecID, AllianceGarrClassSpecID, FollowerLevel, Gender, Flags, " +
                "HordeSourceTypeEnum, AllianceSourceTypeEnum, GarrTypeID, Vitality, ChrClassID, HordeFlavorGarrStringID, AllianceFlavorGarrStringID, ID" +
                " FROM garr_follower ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_GARR_FOLLOWER_LOCALE, "SELECT ID, HordeSourceText_lang, AllianceSourceText_lang, TitleName_lang FROM garr_follower_locale" +
                " WHERE locale = ?");

            // GarrFollowerXAbility.db2
            PrepareStatement(HotfixStatements.SEL_GARR_FOLLOWER_X_ABILITY, "SELECT ID, GarrAbilityID, FactionIndex, GarrFollowerID FROM garr_follower_x_ability" +
                " ORDER BY ID DESC");

            // GarrPlot.db2
            PrepareStatement(HotfixStatements.SEL_GARR_PLOT, "SELECT ID, Name, AllianceConstructObjID, HordeConstructObjID, UiCategoryID, PlotType, Flags, " +
                "UpgradeRequirement1, UpgradeRequirement2 FROM garr_plot ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_GARR_PLOT_LOCALE, "SELECT ID, Name_lang FROM garr_plot_locale WHERE locale = ?");

            // GarrPlotBuilding.db2
            PrepareStatement(HotfixStatements.SEL_GARR_PLOT_BUILDING, "SELECT ID, GarrPlotID, GarrBuildingID FROM garr_plot_building ORDER BY ID DESC");

            // GarrPlotInstance.db2
            PrepareStatement(HotfixStatements.SEL_GARR_PLOT_INSTANCE, "SELECT ID, Name, GarrPlotID FROM garr_plot_instance ORDER BY ID DESC");

            // GarrSiteLevel.db2
            PrepareStatement(HotfixStatements.SEL_GARR_SITE_LEVEL, "SELECT ID, TownHallUiPosX, TownHallUiPosY, MapID, UiTextureKitID, UpgradeMovieID, UpgradeCost, " +
                "UpgradeGoldCost, GarrLevel, GarrSiteID, MaxBuildingLevel FROM garr_site_level ORDER BY ID DESC");

            // GarrSiteLevelPlotInst.db2
            PrepareStatement(HotfixStatements.SEL_GARR_SITE_LEVEL_PLOT_INST, "SELECT ID, UiMarkerPosX, UiMarkerPosY, GarrSiteLevelID, GarrPlotInstanceID, UiMarkerSize" +
                " FROM garr_site_level_plot_inst ORDER BY ID DESC");

            // GemProperties.db2
            PrepareStatement(HotfixStatements.SEL_GEM_PROPERTIES, "SELECT ID, Type, EnchantId, MinItemLevel FROM gem_properties ORDER BY ID DESC");

            // GlyphBindableSpell.db2
            PrepareStatement(HotfixStatements.SEL_GLYPH_BINDABLE_SPELL, "SELECT ID, SpellID, GlyphPropertiesID FROM glyph_bindable_spell ORDER BY ID DESC");

            // GlyphProperties.db2
            PrepareStatement(HotfixStatements.SEL_GLYPH_PROPERTIES, "SELECT ID, SpellID, SpellIconID, GlyphType, GlyphExclusiveCategoryID FROM glyph_properties" +
                " ORDER BY ID DESC");

            // GlyphRequiredSpec.db2
            PrepareStatement(HotfixStatements.SEL_GLYPH_REQUIRED_SPEC, "SELECT ID, ChrSpecializationID, GlyphPropertiesID FROM glyph_required_spec ORDER BY ID DESC");

            // GuildColorBackground.db2
            PrepareStatement(HotfixStatements.SEL_GUILD_COLOR_BACKGROUND, "SELECT ID, Red, Green, Blue FROM guild_color_background ORDER BY ID DESC");

            // GuildColorBorder.db2
            PrepareStatement(HotfixStatements.SEL_GUILD_COLOR_BORDER, "SELECT ID, Red, Green, Blue FROM guild_color_border ORDER BY ID DESC");

            // GuildColorEmblem.db2
            PrepareStatement(HotfixStatements.SEL_GUILD_COLOR_EMBLEM, "SELECT ID, Red, Green, Blue FROM guild_color_emblem ORDER BY ID DESC");

            // GuildPerkSpells.db2
            PrepareStatement(HotfixStatements.SEL_GUILD_PERK_SPELLS, "SELECT ID, SpellID FROM guild_perk_spells ORDER BY ID DESC");

            // Heirloom.db2
            PrepareStatement(HotfixStatements.SEL_HEIRLOOM, "SELECT SourceText, ItemID, LegacyItemID, LegacyUpgradedItemID, StaticUpgradedItemID, UpgradeItemID1, " +
                "UpgradeItemID2, UpgradeItemID3, UpgradeItemBonusListID1, UpgradeItemBonusListID2, UpgradeItemBonusListID3, Flags, SourceTypeEnum, ID" +
                " FROM heirloom ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_HEIRLOOM_LOCALE, "SELECT ID, SourceText_lang FROM heirloom_locale WHERE locale = ?");

            // Holidays.db2
            PrepareStatement(HotfixStatements.SEL_HOLIDAYS, "SELECT ID, Date1, Date2, Date3, Date4, Date5, Date6, Date7, Date8, Date9, Date10, Date11, Date12, Date13, " +
                "Date14, Date15, Date16, Duration1, Duration2, Duration3, Duration4, Duration5, Duration6, Duration7, Duration8, Duration9, Duration10, " +
                "Region, Looping, CalendarFlags1, CalendarFlags2, CalendarFlags3, CalendarFlags4, CalendarFlags5, CalendarFlags6, CalendarFlags7, " +
                "CalendarFlags8, CalendarFlags9, CalendarFlags10, Priority, CalendarFilterType, Flags, HolidayNameID, HolidayDescriptionID, " +
                "TextureFileDataID1, TextureFileDataID2, TextureFileDataID3 FROM holidays ORDER BY ID DESC");

            // ImportPriceArmor.db2
            PrepareStatement(HotfixStatements.SEL_IMPORT_PRICE_ARMOR, "SELECT ID, ClothModifier, LeatherModifier, ChainModifier, PlateModifier FROM import_price_armor" +
                " ORDER BY ID DESC");

            // ImportPriceQuality.db2
            PrepareStatement(HotfixStatements.SEL_IMPORT_PRICE_QUALITY, "SELECT ID, Data FROM import_price_quality ORDER BY ID DESC");

            // ImportPriceShield.db2
            PrepareStatement(HotfixStatements.SEL_IMPORT_PRICE_SHIELD, "SELECT ID, Data FROM import_price_shield ORDER BY ID DESC");

            // ImportPriceWeapon.db2
            PrepareStatement(HotfixStatements.SEL_IMPORT_PRICE_WEAPON, "SELECT ID, Data FROM import_price_weapon ORDER BY ID DESC");

            // Item.db2
            PrepareStatement(HotfixStatements.SEL_ITEM, "SELECT ID, IconFileDataID, ClassID, SubclassID, SoundOverrideSubclassID, Material, InventoryType, SheatheType, " +
                "ItemGroupSoundsID FROM item ORDER BY ID DESC");

            // ItemAppearance.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_APPEARANCE, "SELECT ID, ItemDisplayInfoID, DefaultIconFileDataID, UiOrder, DisplayType FROM item_appearance" +
                " ORDER BY ID DESC");

            // ItemArmorQuality.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_ARMOR_QUALITY, "SELECT ID, Qualitymod1, Qualitymod2, Qualitymod3, Qualitymod4, Qualitymod5, Qualitymod6, " +
                "Qualitymod7, ItemLevel FROM item_armor_quality ORDER BY ID DESC");

            // ItemArmorShield.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_ARMOR_SHIELD, "SELECT ID, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, Quality7, ItemLevel" +
                " FROM item_armor_shield ORDER BY ID DESC");

            // ItemArmorTotal.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_ARMOR_TOTAL, "SELECT ID, Cloth, Leather, Mail, Plate, ItemLevel FROM item_armor_total ORDER BY ID DESC");

            // ItemBagFamily.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_BAG_FAMILY, "SELECT ID, Name FROM item_bag_family ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_ITEM_BAG_FAMILY_LOCALE, "SELECT ID, Name_lang FROM item_bag_family_locale WHERE locale = ?");

            // ItemBonus.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_BONUS, "SELECT ID, Value1, Value2, Value3, ParentItemBonusListID, Type, OrderIndex FROM item_bonus" +
                " ORDER BY ID DESC");

            // ItemBonusListLevelDelta.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_BONUS_LIST_LEVEL_DELTA, "SELECT ItemLevelDelta, ID FROM item_bonus_list_level_delta ORDER BY ID DESC");

            // ItemBonusTreeNode.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_BONUS_TREE_NODE, "SELECT ID, ChildItemBonusTreeID, ChildItemBonusListID, ChildItemLevelSelectorID, ItemContext, " +
                "ParentItemBonusTreeID FROM item_bonus_tree_node ORDER BY ID DESC");

            // ItemChildEquipment.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_CHILD_EQUIPMENT, "SELECT ID, ChildItemID, ChildItemEquipSlot, ParentItemID FROM item_child_equipment" +
                " ORDER BY ID DESC");

            // ItemClass.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_CLASS, "SELECT ID, ClassName, PriceModifier, ClassID, Flags FROM item_class ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_ITEM_CLASS_LOCALE, "SELECT ID, ClassName_lang FROM item_class_locale WHERE locale = ?");

            // ItemCurrencyCost.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_CURRENCY_COST, "SELECT ID, ItemID FROM item_currency_cost ORDER BY ID DESC");

            // ItemDamageAmmo.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_AMMO, "SELECT ID, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, Quality7, ItemLevel" +
                " FROM item_damage_ammo ORDER BY ID DESC");

            // ItemDamageOneHand.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND, "SELECT ID, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, Quality7, ItemLevel" +
                " FROM item_damage_one_hand ORDER BY ID DESC");

            // ItemDamageOneHandCaster.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND_CASTER, "SELECT ID, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, Quality7, " +
                "ItemLevel FROM item_damage_one_hand_caster ORDER BY ID DESC");

            // ItemDamageTwoHand.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND, "SELECT ID, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, Quality7, ItemLevel" +
                " FROM item_damage_two_hand ORDER BY ID DESC");

            // ItemDamageTwoHandCaster.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND_CASTER, "SELECT ID, Quality1, Quality2, Quality3, Quality4, Quality5, Quality6, Quality7, " +
                "ItemLevel FROM item_damage_two_hand_caster ORDER BY ID DESC");

            // ItemDisenchantLoot.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_DISENCHANT_LOOT, "SELECT ID, MinLevel, MaxLevel, SkillRequired, Subclass, Quality, ExpansionID, Class" +
                " FROM item_disenchant_loot ORDER BY ID DESC");

            // ItemEffect.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_EFFECT, "SELECT ID, SpellID, CoolDownMSec, CategoryCoolDownMSec, Charges, SpellCategoryID, ChrSpecializationID, " +
                "LegacySlotIndex, TriggerType, ParentItemID FROM item_effect ORDER BY ID DESC");

            // ItemExtendedCost.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_EXTENDED_COST, "SELECT ID, ItemID1, ItemID2, ItemID3, ItemID4, ItemID5, CurrencyCount1, CurrencyCount2, " +
                "CurrencyCount3, CurrencyCount4, CurrencyCount5, ItemCount1, ItemCount2, ItemCount3, ItemCount4, ItemCount5, RequiredArenaRating, " +
                "CurrencyID1, CurrencyID2, CurrencyID3, CurrencyID4, CurrencyID5, ArenaBracket, MinFactionID, MinReputation, Flags, RequiredAchievement" +
                " FROM item_extended_cost ORDER BY ID DESC");

            // ItemLevelSelector.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LEVEL_SELECTOR, "SELECT ID, MinItemLevel, ItemLevelSelectorQualitySetID FROM item_level_selector ORDER BY ID DESC");

            // ItemLevelSelectorQuality.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY, "SELECT ID, QualityItemBonusListID, Quality, ParentILSQualitySetID" +
                " FROM item_level_selector_quality ORDER BY ID DESC");

            // ItemLevelSelectorQualitySet.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY_SET, "SELECT ID, IlvlRare, IlvlEpic FROM item_level_selector_quality_set ORDER BY ID DESC");

            // ItemLimitCategory.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LIMIT_CATEGORY, "SELECT ID, Name, Quantity, Flags FROM item_limit_category ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_LOCALE, "SELECT ID, Name_lang FROM item_limit_category_locale WHERE locale = ?");

            // ItemLimitCategoryCondition.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_CONDITION, "SELECT ID, AddQuantity, PlayerConditionID, ParentItemLimitCategoryID " +
                " FROM item_limit_category_condition ORDER BY ID DESC");

            // ItemModifiedAppearance.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_MODIFIED_APPEARANCE, "SELECT ItemID, ID, ItemAppearanceModifierID, ItemAppearanceID, OrderIndex, " +
                "TransmogSourceTypeEnum FROM item_modified_appearance ORDER BY ID DESC");

            // ItemPriceBase.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_PRICE_BASE, "SELECT ID, Armor, Weapon, ItemLevel FROM item_price_base ORDER BY ID DESC");

            // ItemRandomProperties.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_RANDOM_PROPERTIES, "SELECT ID, Name, Enchantment1, Enchantment2, Enchantment3, Enchantment4, Enchantment5" +
                " FROM item_random_properties ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_ITEM_RANDOM_PROPERTIES_LOCALE, "SELECT ID, Name_lang FROM item_random_properties_locale WHERE locale = ?");

            // ItemRandomSuffix.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_RANDOM_SUFFIX, "SELECT ID, Name, Enchantment1, Enchantment2, Enchantment3, Enchantment4, Enchantment5, " +
                "AllocationPct1, AllocationPct2, AllocationPct3, AllocationPct4, AllocationPct5 FROM item_random_suffix ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_ITEM_RANDOM_SUFFIX_LOCALE, "SELECT ID, Name_lang FROM item_random_suffix_locale WHERE locale = ?");

            // ItemSearchName.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SEARCH_NAME, "SELECT AllowableRace, Display, ID, Flags1, Flags2, Flags3, ItemLevel, OverallQualityID, " +
                "ExpansionID, RequiredLevel, MinFactionID, MinReputation, AllowableClass, RequiredSkill, RequiredSkillRank, RequiredAbility" +
                " FROM item_search_name ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_ITEM_SEARCH_NAME_LOCALE, "SELECT ID, Display_lang FROM item_search_name_locale WHERE locale = ?");

            // ItemSet.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SET, "SELECT ID, Name, ItemID1, ItemID2, ItemID3, ItemID4, ItemID5, ItemID6, ItemID7, ItemID8, ItemID9, " +
                "ItemID10, ItemID11, ItemID12, ItemID13, ItemID14, ItemID15, ItemID16, ItemID17, RequiredSkillRank, RequiredSkill, SetFlags FROM item_set" +
                " ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_ITEM_SET_LOCALE, "SELECT ID, Name_lang FROM item_set_locale WHERE locale = ?");

            // ItemSetSpell.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SET_SPELL, "SELECT ID, SpellID, ChrSpecID, Threshold, ItemSetID FROM item_set_spell ORDER BY ID DESC");

            // ItemSparse.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SPARSE, "SELECT ID, AllowableRace, Display, Display1, Display2, Display3, Description, Flags1, Flags2, Flags3, " +
                "Flags4, PriceRandomValue, PriceVariance, VendorStackCount, BuyPrice, SellPrice, RequiredAbility, MaxCount, Stackable, StatPercentEditor1, " +
                "StatPercentEditor2, StatPercentEditor3, StatPercentEditor4, StatPercentEditor5, StatPercentEditor6, StatPercentEditor7, StatPercentEditor8, " +
                "StatPercentEditor9, StatPercentEditor10, StatPercentageOfSocket1, StatPercentageOfSocket2, StatPercentageOfSocket3, StatPercentageOfSocket4, " +
                "StatPercentageOfSocket5, StatPercentageOfSocket6, StatPercentageOfSocket7, StatPercentageOfSocket8, StatPercentageOfSocket9, " +
                "StatPercentageOfSocket10, ItemRange, BagFamily, QualityModifier, DurationInInventory, DmgVariance, AllowableClass, ItemLevel, RequiredSkill, " +
                "RequiredSkillRank, MinFactionID, ItemStatValue1, ItemStatValue2, ItemStatValue3, ItemStatValue4, ItemStatValue5, ItemStatValue6, " +
                "ItemStatValue7, ItemStatValue8, ItemStatValue9, ItemStatValue10, ScalingStatDistributionID, ItemDelay, PageID, StartQuestID, LockID, " +
                "RandomSelect, ItemRandomSuffixGroupID, ItemSet, ZoneBound, InstanceBound, TotemCategoryID, SocketMatchEnchantmentId, GemProperties, " +
                "LimitCategory, RequiredHoliday, RequiredTransmogHoliday, ItemNameDescriptionID, OverallQualityID, InventoryType, RequiredLevel, " +
                "RequiredPVPRank, RequiredPVPMedal, MinReputation, ContainerSlots, StatModifierBonusStat1, StatModifierBonusStat2, StatModifierBonusStat3, " +
                "StatModifierBonusStat4, StatModifierBonusStat5, StatModifierBonusStat6, StatModifierBonusStat7, StatModifierBonusStat8, " +
                "StatModifierBonusStat9, StatModifierBonusStat10, DamageDamageType, Bonding, LanguageID, PageMaterialID, Material, SheatheType, SocketType1, " +
                "SocketType2, SocketType3, SpellWeightCategory, SpellWeight, ArtifactID, ExpansionID FROM item_sparse ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_ITEM_SPARSE_LOCALE, "SELECT ID, Display_lang, Display1_lang, Display2_lang, Display3_lang, Description_lang" +
                " FROM item_sparse_locale WHERE locale = ?");

            // ItemSpec.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SPEC, "SELECT ID, SpecializationID, MinLevel, MaxLevel, ItemType, PrimaryStat, SecondaryStat FROM item_spec" +
                " ORDER BY ID DESC");

            // ItemSpecOverride.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_SPEC_OVERRIDE, "SELECT ID, SpecID, ItemID FROM item_spec_override ORDER BY ID DESC");

            // ItemUpgrade.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_UPGRADE, "SELECT ID, CurrencyAmount, PrerequisiteID, CurrencyType, ItemUpgradePathID, ItemLevelIncrement" +
                " FROM item_upgrade ORDER BY ID DESC");

            // ItemXBonusTree.db2
            PrepareStatement(HotfixStatements.SEL_ITEM_X_BONUS_TREE, "SELECT ID, ItemBonusTreeID, ItemID FROM item_x_bonus_tree ORDER BY ID DESC");

            // Keychain.db2
            PrepareStatement(HotfixStatements.SEL_KEYCHAIN, "SELECT ID, Key1, Key2, Key3, Key4, Key5, Key6, Key7, Key8, Key9, Key10, Key11, Key12, Key13, Key14, Key15, " +
                "Key16, Key17, Key18, Key19, Key20, Key21, Key22, Key23, Key24, Key25, Key26, Key27, Key28, Key29, Key30, Key31, Key32 FROM keychain" +
                " ORDER BY ID DESC");

            // LfgDungeons.db2
            PrepareStatement(HotfixStatements.SEL_LFG_DUNGEONS, "SELECT ID, Name, Description, Flags, MinGear, MaxLevel, TargetLevelMax, MapID, RandomID, ScenarioID, " +
                "FinalEncounterID, BonusReputationAmount, MentorItemLevel, RequiredPlayerConditionId, MinLevel, TargetLevel, TargetLevelMin, DifficultyID, " +
                "TypeID, Faction, ExpansionLevel, OrderIndex, GroupID, CountTank, CountHealer, CountDamage, MinCountTank, MinCountHealer, MinCountDamage, " +
                "Subtype, MentorCharLevel, IconTextureFileID, RewardsBgTextureFileID, PopupBgTextureFileID FROM lfg_dungeons ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_LFG_DUNGEONS_LOCALE, "SELECT ID, Name_lang, Description_lang FROM lfg_dungeons_locale WHERE locale = ?");

            // Light.db2
            PrepareStatement(HotfixStatements.SEL_LIGHT, "SELECT ID, GameCoordsX, GameCoordsY, GameCoordsZ, GameFalloffStart, GameFalloffEnd, ContinentID, " +
                "LightParamsID1, LightParamsID2, LightParamsID3, LightParamsID4, LightParamsID5, LightParamsID6, LightParamsID7, LightParamsID8 FROM light" +
                " ORDER BY ID DESC");

            // LiquidType.db2
            PrepareStatement(HotfixStatements.SEL_LIQUID_TYPE, "SELECT ID, Name, Texture1, Texture2, Texture3, Texture4, Texture5, Texture6, SpellID, MaxDarkenDepth, " +
                "FogDarkenIntensity, AmbDarkenIntensity, DirDarkenIntensity, ParticleScale, Color1, Color2, Float1, Float2, Float3, `Float4`, Float5, Float6, " +
                "Float7, `Float8`, Float9, Float10, Float11, Float12, Float13, Float14, Float15, Float16, Float17, Float18, `Int1`, `Int2`, `Int3`, `Int4`, " +
                "Flags, LightID, SoundBank, ParticleMovement, ParticleTexSlots, MaterialID, FrameCountTexture1, FrameCountTexture2, FrameCountTexture3, " +
                "FrameCountTexture4, FrameCountTexture5, FrameCountTexture6, SoundID FROM liquid_type ORDER BY ID DESC");

            // Lock.db2
            PrepareStatement(HotfixStatements.SEL_LOCK, "SELECT ID, Index1, Index2, Index3, Index4, Index5, Index6, Index7, Index8, Skill1, Skill2, Skill3, Skill4, " +
                "Skill5, Skill6, Skill7, Skill8, Type1, Type2, Type3, Type4, Type5, Type6, Type7, Type8, Action1, Action2, Action3, Action4, Action5, " +
                "Action6, Action7, Action8 FROM `lock` ORDER BY ID DESC");

            // MailTemplate.db2
            PrepareStatement(HotfixStatements.SEL_MAIL_TEMPLATE, "SELECT ID, Body FROM mail_template ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_MAIL_TEMPLATE_LOCALE, "SELECT ID, Body_lang FROM mail_template_locale WHERE locale = ?");

            // Map.db2
            PrepareStatement(HotfixStatements.SEL_MAP, "SELECT ID, Directory, MapName, MapDescription0, MapDescription1, PvpShortDescription, PvpLongDescription, " +
                "Flags1, Flags2, MinimapIconScale, CorpseX, CorpseY, AreaTableID, LoadingScreenID, CorpseMapID, TimeOfDayOverride, ParentMapID, " +
                "CosmeticParentMapID, WindSettingsID, InstanceType, MapType, ExpansionID, MaxPlayers, TimeOffset FROM map ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_MAP_LOCALE, "SELECT ID, MapName_lang, MapDescription0_lang, MapDescription1_lang, PvpShortDescription_lang, " +
                "PvpLongDescription_lang FROM map_locale WHERE locale = ?");

            // MapDifficulty.db2
            PrepareStatement(HotfixStatements.SEL_MAP_DIFFICULTY, "SELECT ID, Message, DifficultyID, ResetInterval, MaxPlayers, LockID, Flags, ItemContext, " +
                "ItemContextPickerID, MapID FROM map_difficulty ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_MAP_DIFFICULTY_LOCALE, "SELECT ID, Message_lang FROM map_difficulty_locale WHERE locale = ?");

            // ModifierTree.db2
            PrepareStatement(HotfixStatements.SEL_MODIFIER_TREE, "SELECT ID, Asset, SecondaryAsset, Parent, Type, TertiaryAsset, Operator, Amount FROM modifier_tree" +
                " ORDER BY ID DESC");

            // Mount.db2
            PrepareStatement(HotfixStatements.SEL_MOUNT, "SELECT Name, Description, SourceText, SourceSpellID, MountFlyRideHeight, MountTypeID, Flags, SourceTypeEnum, " +
                "ID, PlayerConditionID, UiModelSceneID FROM mount ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_MOUNT_LOCALE, "SELECT ID, Name_lang, Description_lang, SourceText_lang FROM mount_locale WHERE locale = ?");

            // MountCapability.db2
            PrepareStatement(HotfixStatements.SEL_MOUNT_CAPABILITY, "SELECT ReqSpellKnownID, ModSpellAuraID, ReqRidingSkill, ReqAreaID, ReqMapID, Flags, ID, " +
                "ReqSpellAuraID FROM mount_capability ORDER BY ID DESC");

            // MountTypeXCapability.db2
            PrepareStatement(HotfixStatements.SEL_MOUNT_TYPE_X_CAPABILITY, "SELECT ID, MountTypeID, MountCapabilityID, OrderIndex FROM mount_type_x_capability" +
                " ORDER BY ID DESC");

            // MountXDisplay.db2
            PrepareStatement(HotfixStatements.SEL_MOUNT_X_DISPLAY, "SELECT ID, CreatureDisplayInfoID, PlayerConditionID, MountID FROM mount_x_display ORDER BY ID DESC");

            // Movie.db2
            PrepareStatement(HotfixStatements.SEL_MOVIE, "SELECT ID, AudioFileDataID, SubtitleFileDataID, Volume, KeyID FROM movie ORDER BY ID DESC");

            // NameGen.db2
            PrepareStatement(HotfixStatements.SEL_NAME_GEN, "SELECT ID, Name, RaceID, Sex FROM name_gen ORDER BY ID DESC");

            // NamesProfanity.db2
            PrepareStatement(HotfixStatements.SEL_NAMES_PROFANITY, "SELECT ID, Name, Language FROM names_profanity ORDER BY ID DESC");

            // NamesReserved.db2
            PrepareStatement(HotfixStatements.SEL_NAMES_RESERVED, "SELECT ID, Name FROM names_reserved ORDER BY ID DESC");

            // NamesReservedLocale.db2
            PrepareStatement(HotfixStatements.SEL_NAMES_RESERVED_LOCALE, "SELECT ID, Name, LocaleMask FROM names_reserved_locale ORDER BY ID DESC");

            // OverrideSpellData.db2
            PrepareStatement(HotfixStatements.SEL_OVERRIDE_SPELL_DATA, "SELECT ID, Spells1, Spells2, Spells3, Spells4, Spells5, Spells6, Spells7, Spells8, Spells9, " +
                "Spells10, PlayerActionBarFileDataID, Flags FROM override_spell_data ORDER BY ID DESC");

            // Phase.db2
            PrepareStatement(HotfixStatements.SEL_PHASE, "SELECT ID, Flags FROM phase ORDER BY ID DESC");

            // PhaseXPhaseGroup.db2
            PrepareStatement(HotfixStatements.SEL_PHASE_X_PHASE_GROUP, "SELECT ID, PhaseID, PhaseGroupID FROM phase_x_phase_group ORDER BY ID DESC");

            // PlayerCondition.db2
            PrepareStatement(HotfixStatements.SEL_PLAYER_CONDITION, "SELECT RaceMask, FailureDescription, ID, Flags, MinLevel, MaxLevel, ClassMask, Gender, " +
                "NativeGender, SkillLogic, LanguageID, MinLanguage, MaxLanguage, MaxFactionID, MaxReputation, ReputationLogic, CurrentPvpFaction, MinPVPRank, " +
                "MaxPVPRank, PvpMedal, PrevQuestLogic, CurrQuestLogic, CurrentCompletedQuestLogic, SpellLogic, ItemLogic, ItemFlags, AuraSpellLogic, " +
                "WorldStateExpressionID, WeatherID, PartyStatus, LifetimeMaxPVPRank, AchievementLogic, LfgLogic, AreaLogic, CurrencyLogic, QuestKillID, " +
                "QuestKillLogic, MinExpansionLevel, MaxExpansionLevel, MinExpansionTier, MaxExpansionTier, MinGuildLevel, MaxGuildLevel, PhaseUseFlags, " +
                "PhaseID, PhaseGroupID, MinAvgItemLevel, MaxAvgItemLevel, MinAvgEquippedItemLevel, MaxAvgEquippedItemLevel, ChrSpecializationIndex, " +
                "ChrSpecializationRole, PowerType, PowerTypeComp, PowerTypeValue, ModifierTreeID, WeaponSubclassMask, SkillID1, SkillID2, SkillID3, SkillID4, " +
                "MinSkill1, MinSkill2, MinSkill3, MinSkill4, MaxSkill1, MaxSkill2, MaxSkill3, MaxSkill4, MinFactionID1, MinFactionID2, MinFactionID3, " +
                "MinReputation1, MinReputation2, MinReputation3, PrevQuestID1, PrevQuestID2, PrevQuestID3, PrevQuestID4, CurrQuestID1, CurrQuestID2, " +
                "CurrQuestID3, CurrQuestID4, CurrentCompletedQuestID1, CurrentCompletedQuestID2, CurrentCompletedQuestID3, CurrentCompletedQuestID4, " +
                "SpellID1, SpellID2, SpellID3, SpellID4, ItemID1, ItemID2, ItemID3, ItemID4, ItemCount1, ItemCount2, ItemCount3, ItemCount4, Explored1, " +
                "Explored2, Time1, Time2, AuraSpellID1, AuraSpellID2, AuraSpellID3, AuraSpellID4, AuraStacks1, AuraStacks2, AuraStacks3, AuraStacks4, " +
                "Achievement1, Achievement2, Achievement3, Achievement4, LfgStatus1, LfgStatus2, LfgStatus3, LfgStatus4, LfgCompare1, LfgCompare2, " +
                "LfgCompare3, LfgCompare4, LfgValue1, LfgValue2, LfgValue3, LfgValue4, AreaID1, AreaID2, AreaID3, AreaID4, CurrencyID1, CurrencyID2, " +
                "CurrencyID3, CurrencyID4, CurrencyCount1, CurrencyCount2, CurrencyCount3, CurrencyCount4, QuestKillMonster1, QuestKillMonster2, " +
                "QuestKillMonster3, QuestKillMonster4, QuestKillMonster5, QuestKillMonster6, MovementFlags1, MovementFlags2 FROM player_condition" +
                " ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_PLAYER_CONDITION_LOCALE, "SELECT ID, FailureDescription_lang FROM player_condition_locale WHERE locale = ?");

            // PowerDisplay.db2
            PrepareStatement(HotfixStatements.SEL_POWER_DISPLAY, "SELECT ID, GlobalStringBaseTag, ActualType, Red, Green, Blue FROM power_display ORDER BY ID DESC");

            // PowerType.db2
            PrepareStatement(HotfixStatements.SEL_POWER_TYPE, "SELECT ID, NameGlobalStringTag, CostGlobalStringTag, RegenPeace, RegenCombat, MaxBasePower, " +
                "RegenInterruptTimeMS, Flags, PowerTypeEnum, MinPower, CenterPower, DefaultPower, DisplayModifier FROM power_type ORDER BY ID DESC");

            // PrestigeLevelInfo.db2
            PrepareStatement(HotfixStatements.SEL_PRESTIGE_LEVEL_INFO, "SELECT ID, Name, BadgeTextureFileDataID, PrestigeLevel, Flags FROM prestige_level_info" +
                " ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_PRESTIGE_LEVEL_INFO_LOCALE, "SELECT ID, Name_lang FROM prestige_level_info_locale WHERE locale = ?");

            // PvpDifficulty.db2
            PrepareStatement(HotfixStatements.SEL_PVP_DIFFICULTY, "SELECT ID, RangeIndex, MinLevel, MaxLevel, MapID FROM pvp_difficulty ORDER BY ID DESC");

            // PvpItem.db2
            PrepareStatement(HotfixStatements.SEL_PVP_ITEM, "SELECT ID, ItemID, ItemLevelDelta FROM pvp_item ORDER BY ID DESC");

            // PvpReward.db2
            PrepareStatement(HotfixStatements.SEL_PVP_REWARD, "SELECT ID, HonorLevel, PrestigeLevel, RewardPackID FROM pvp_reward ORDER BY ID DESC");

            // PvpTalent.db2
            PrepareStatement(HotfixStatements.SEL_PVP_TALENT, "SELECT ID, Description, SpellID, OverridesSpellID, ActionBarSpellID, TierID, ColumnIndex, Flags, " +
                "ClassID, SpecID, Role FROM pvp_talent ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_PVP_TALENT_LOCALE, "SELECT ID, Description_lang FROM pvp_talent_locale WHERE locale = ?");

            // PvpTalentUnlock.db2
            PrepareStatement(HotfixStatements.SEL_PVP_TALENT_UNLOCK, "SELECT ID, TierID, ColumnIndex, HonorLevel FROM pvp_talent_unlock ORDER BY ID DESC");

            // QuestFactionReward.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_FACTION_REWARD, "SELECT ID, Difficulty1, Difficulty2, Difficulty3, Difficulty4, Difficulty5, Difficulty6, " +
                "Difficulty7, Difficulty8, Difficulty9, Difficulty10 FROM quest_faction_reward ORDER BY ID DESC");

            // QuestMoneyReward.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_MONEY_REWARD, "SELECT ID, Difficulty1, Difficulty2, Difficulty3, Difficulty4, Difficulty5, Difficulty6, " +
                "Difficulty7, Difficulty8, Difficulty9, Difficulty10 FROM quest_money_reward ORDER BY ID DESC");

            // QuestPackageItem.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_PACKAGE_ITEM, "SELECT ID, ItemID, PackageID, DisplayType, ItemQuantity FROM quest_package_item ORDER BY ID DESC");

            // QuestSort.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_SORT, "SELECT ID, SortName, UiOrderIndex FROM quest_sort ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_QUEST_SORT_LOCALE, "SELECT ID, SortName_lang FROM quest_sort_locale WHERE locale = ?");

            // QuestV2.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_V2, "SELECT ID, UniqueBitFlag FROM quest_v2 ORDER BY ID DESC");

            // QuestXp.db2
            PrepareStatement(HotfixStatements.SEL_QUEST_XP, "SELECT ID, Difficulty1, Difficulty2, Difficulty3, Difficulty4, Difficulty5, Difficulty6, Difficulty7, " +
                "Difficulty8, Difficulty9, Difficulty10 FROM quest_xp ORDER BY ID DESC");

            // RandPropPoints.db2
            PrepareStatement(HotfixStatements.SEL_RAND_PROP_POINTS, "SELECT ID, Epic1, Epic2, Epic3, Epic4, Epic5, Superior1, Superior2, Superior3, Superior4, " +
                "Superior5, Good1, Good2, Good3, Good4, Good5 FROM rand_prop_points ORDER BY ID DESC");

            // RewardPack.db2
            PrepareStatement(HotfixStatements.SEL_REWARD_PACK, "SELECT ID, Money, ArtifactXPMultiplier, ArtifactXPDifficulty, ArtifactXPCategoryID, CharTitleID, " +
                "TreasurePickerID FROM reward_pack ORDER BY ID DESC");

            // RewardPackXCurrencyType.db2
            PrepareStatement(HotfixStatements.SEL_REWARD_PACK_X_CURRENCY_TYPE, "SELECT ID, CurrencyTypeID, Quantity, RewardPackID FROM reward_pack_x_currency_type" +
                " ORDER BY ID DESC");

            // RewardPackXItem.db2
            PrepareStatement(HotfixStatements.SEL_REWARD_PACK_X_ITEM, "SELECT ID, ItemID, ItemQuantity, RewardPackID FROM reward_pack_x_item ORDER BY ID DESC");

            // RulesetItemUpgrade.db2
            PrepareStatement(HotfixStatements.SEL_RULESET_ITEM_UPGRADE, "SELECT ID, ItemID, ItemUpgradeID FROM ruleset_item_upgrade ORDER BY ID DESC");

            // SandboxScaling.db2
            PrepareStatement(HotfixStatements.SEL_SANDBOX_SCALING, "SELECT ID, MinLevel, MaxLevel, Flags FROM sandbox_scaling ORDER BY ID DESC");

            // ScalingStatDistribution.db2
            PrepareStatement(HotfixStatements.SEL_SCALING_STAT_DISTRIBUTION, "SELECT ID, PlayerLevelToItemLevelCurveID, MinLevel, MaxLevel" +
                " FROM scaling_stat_distribution ORDER BY ID DESC");

            // Scenario.db2
            PrepareStatement(HotfixStatements.SEL_SCENARIO, "SELECT ID, Name, AreaTableID, Flags, Type FROM scenario ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_SCENARIO_LOCALE, "SELECT ID, Name_lang FROM scenario_locale WHERE locale = ?");

            // ScenarioStep.db2
            PrepareStatement(HotfixStatements.SEL_SCENARIO_STEP, "SELECT ID, Description, Title, ScenarioID, Supersedes, RewardQuestID, OrderIndex, Flags, " +
                "Criteriatreeid, RelatedStep FROM scenario_step ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_SCENARIO_STEP_LOCALE, "SELECT ID, Description_lang, Title_lang FROM scenario_step_locale WHERE locale = ?");

            // SceneScript.db2
            PrepareStatement(HotfixStatements.SEL_SCENE_SCRIPT, "SELECT ID, FirstSceneScriptID, NextSceneScriptID FROM scene_script ORDER BY ID DESC");

            // SceneScriptGlobalText.db2
            PrepareStatement(HotfixStatements.SEL_SCENE_SCRIPT_GLOBAL_TEXT, "SELECT ID, Name, Script FROM scene_script_global_text ORDER BY ID DESC");

            // SceneScriptPackage.db2
            PrepareStatement(HotfixStatements.SEL_SCENE_SCRIPT_PACKAGE, "SELECT ID, Name FROM scene_script_package ORDER BY ID DESC");

            // SceneScriptText.db2
            PrepareStatement(HotfixStatements.SEL_SCENE_SCRIPT_TEXT, "SELECT ID, Name, Script FROM scene_script_text ORDER BY ID DESC");

            // SkillLine.db2
            PrepareStatement(HotfixStatements.SEL_SKILL_LINE, "SELECT ID, DisplayName, Description, AlternateVerb, Flags, CategoryID, CanLink, SpellIconFileID, " +
                "ParentSkillLineID FROM skill_line ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_SKILL_LINE_LOCALE, "SELECT ID, DisplayName_lang, Description_lang, AlternateVerb_lang FROM skill_line_locale" +
                " WHERE locale = ?");

            // SkillLineAbility.db2
            PrepareStatement(HotfixStatements.SEL_SKILL_LINE_ABILITY, "SELECT RaceMask, ID, Spell, SupercedesSpell, SkillLine, TrivialSkillLineRankHigh, " +
                "TrivialSkillLineRankLow, UniqueBit, TradeSkillCategoryID, NumSkillUps, ClassMask, MinSkillLineRank, AcquireMethod, Flags" +
                " FROM skill_line_ability ORDER BY ID DESC");

            // SkillRaceClassInfo.db2
            PrepareStatement(HotfixStatements.SEL_SKILL_RACE_CLASS_INFO, "SELECT ID, RaceMask, SkillID, Flags, SkillTierID, Availability, MinLevel, ClassMask" +
                " FROM skill_race_class_info ORDER BY ID DESC");

            // SoundKit.db2
            PrepareStatement(HotfixStatements.SEL_SOUND_KIT, "SELECT ID, VolumeFloat, MinDistance, DistanceCutoff, Flags, SoundEntriesAdvancedID, SoundType, " +
                "DialogType, EAXDef, VolumeVariationPlus, VolumeVariationMinus, PitchVariationPlus, PitchVariationMinus, PitchAdjust, BusOverwriteID, " +
                "MaxInstances FROM sound_kit ORDER BY ID DESC");

            // SpecializationSpells.db2
            PrepareStatement(HotfixStatements.SEL_SPECIALIZATION_SPELLS, "SELECT Description, SpellID, OverridesSpellID, SpecID, DisplayOrder, ID" +
                " FROM specialization_spells ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_SPECIALIZATION_SPELLS_LOCALE, "SELECT ID, Description_lang FROM specialization_spells_locale WHERE locale = ?");

            // Spell.db2
            PrepareStatement(HotfixStatements.SEL_SPELL, "SELECT ID, Name, NameSubtext, Description, AuraDescription FROM spell ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_SPELL_LOCALE, "SELECT ID, Name_lang, NameSubtext_lang, Description_lang, AuraDescription_lang FROM spell_locale" +
                " WHERE locale = ?");

            // SpellAuraOptions.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_AURA_OPTIONS, "SELECT ID, ProcCharges, ProcTypeMask, ProcCategoryRecovery, CumulativeAura, " +
                "SpellProcsPerMinuteID, DifficultyID, ProcChance, SpellID FROM spell_aura_options ORDER BY ID DESC");

            // SpellAuraRestrictions.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_AURA_RESTRICTIONS, "SELECT ID, CasterAuraSpell, TargetAuraSpell, ExcludeCasterAuraSpell, " +
                "ExcludeTargetAuraSpell, DifficultyID, CasterAuraState, TargetAuraState, ExcludeCasterAuraState, ExcludeTargetAuraState, SpellID" +
                " FROM spell_aura_restrictions ORDER BY ID DESC");

            // SpellCastTimes.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CAST_TIMES, "SELECT ID, Base, Minimum, PerLevel FROM spell_cast_times ORDER BY ID DESC");

            // SpellCastingRequirements.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CASTING_REQUIREMENTS, "SELECT ID, SpellID, MinFactionID, RequiredAreasID, RequiresSpellFocus, " +
                "FacingCasterFlags, MinReputation, RequiredAuraVision FROM spell_casting_requirements ORDER BY ID DESC");

            // SpellCategories.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CATEGORIES, "SELECT ID, Category, StartRecoveryCategory, ChargeCategory, DifficultyID, DefenseType, DispelType, " +
                "Mechanic, PreventionType, SpellID FROM spell_categories ORDER BY ID DESC");

            // SpellCategory.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CATEGORY, "SELECT ID, Name, ChargeRecoveryTime, Flags, UsesPerWeek, MaxCharges, TypeMask FROM spell_category" +
                " ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_SPELL_CATEGORY_LOCALE, "SELECT ID, Name_lang FROM spell_category_locale WHERE locale = ?");

            // SpellClassOptions.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_CLASS_OPTIONS, "SELECT ID, SpellID, SpellClassMask1, SpellClassMask2, SpellClassMask3, SpellClassMask4, " +
                "SpellClassSet, ModalNextSpell FROM spell_class_options ORDER BY ID DESC");

            // SpellCooldowns.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_COOLDOWNS, "SELECT ID, CategoryRecoveryTime, RecoveryTime, StartRecoveryTime, DifficultyID, SpellID" +
                " FROM spell_cooldowns ORDER BY ID DESC");

            // SpellDuration.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_DURATION, "SELECT ID, Duration, MaxDuration, DurationPerLevel FROM spell_duration ORDER BY ID DESC");

            // SpellEffect.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_EFFECT, "SELECT ID, Effect, EffectBasePoints, EffectIndex, EffectAura, DifficultyID, EffectAmplitude, " +
                "EffectAuraPeriod, EffectBonusCoefficient, EffectChainAmplitude, EffectChainTargets, EffectDieSides, EffectItemType, EffectMechanic, " +
                "EffectPointsPerResource, EffectRealPointsPerLevel, EffectTriggerSpell, EffectPosFacing, EffectAttributes, BonusCoefficientFromAP, " +
                "PvpMultiplier, Coefficient, Variance, ResourceCoefficient, GroupSizeBasePointsCoefficient, EffectSpellClassMask1, EffectSpellClassMask2, " +
                "EffectSpellClassMask3, EffectSpellClassMask4, EffectMiscValue1, EffectMiscValue2, EffectRadiusIndex1, EffectRadiusIndex2, ImplicitTarget1, " +
                "ImplicitTarget2, SpellID FROM spell_effect ORDER BY ID DESC");

            // SpellEquippedItems.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_EQUIPPED_ITEMS, "SELECT ID, SpellID, EquippedItemInvTypes, EquippedItemSubclass, EquippedItemClass" +
                " FROM spell_equipped_items ORDER BY ID DESC");

            // SpellFocusObject.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_FOCUS_OBJECT, "SELECT ID, Name FROM spell_focus_object ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_SPELL_FOCUS_OBJECT_LOCALE, "SELECT ID, Name_lang FROM spell_focus_object_locale WHERE locale = ?");

            // SpellInterrupts.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_INTERRUPTS, "SELECT ID, DifficultyID, InterruptFlags, AuraInterruptFlags1, AuraInterruptFlags2, " +
                "ChannelInterruptFlags1, ChannelInterruptFlags2, SpellID FROM spell_interrupts ORDER BY ID DESC");

            // SpellItemEnchantment.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT, "SELECT ID, Name, EffectArg1, EffectArg2, EffectArg3, EffectScalingPoints1, " +
                "EffectScalingPoints2, EffectScalingPoints3, TransmogCost, IconFileDataID, EffectPointsMin1, EffectPointsMin2, EffectPointsMin3, ItemVisual, " +
                "Flags, RequiredSkillID, RequiredSkillRank, ItemLevel, Charges, Effect1, Effect2, Effect3, ConditionID, MinLevel, MaxLevel, ScalingClass, " +
                "ScalingClassRestricted, TransmogPlayerConditionID FROM spell_item_enchantment ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_LOCALE, "SELECT ID, Name_lang FROM spell_item_enchantment_locale WHERE locale = ?");

            // SpellItemEnchantmentCondition.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_CONDITION, "SELECT ID, LtOperand1, LtOperand2, LtOperand3, LtOperand4, LtOperand5, " +
                "LtOperandType1, LtOperandType2, LtOperandType3, LtOperandType4, LtOperandType5, Operator1, Operator2, Operator3, Operator4, Operator5, " +
                "RtOperandType1, RtOperandType2, RtOperandType3, RtOperandType4, RtOperandType5, RtOperand1, RtOperand2, RtOperand3, RtOperand4, RtOperand5, " +
                "Logic1, Logic2, Logic3, Logic4, Logic5 FROM spell_item_enchantment_condition ORDER BY ID DESC");

            // SpellLearnSpell.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_LEARN_SPELL, "SELECT ID, SpellID, LearnSpellID, OverridesSpellID FROM spell_learn_spell ORDER BY ID DESC");

            // SpellLevels.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_LEVELS, "SELECT ID, BaseLevel, MaxLevel, SpellLevel, DifficultyID, MaxPassiveAuraLevel, SpellID" +
                " FROM spell_levels ORDER BY ID DESC");

            // SpellMisc.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_MISC, "SELECT ID, CastingTimeIndex, DurationIndex, RangeIndex, SchoolMask, SpellIconFileDataID, Speed, " +
                "ActiveIconFileDataID, LaunchDelay, DifficultyID, Attributes1, Attributes2, Attributes3, Attributes4, Attributes5, Attributes6, Attributes7, " +
                "Attributes8, Attributes9, Attributes10, Attributes11, Attributes12, Attributes13, Attributes14, SpellID FROM spell_misc ORDER BY ID DESC");

            // SpellPower.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_POWER, "SELECT ManaCost, PowerCostPct, PowerPctPerSecond, RequiredAuraSpellID, PowerCostMaxPct, OrderIndex, " +
                "PowerType, ID, ManaCostPerLevel, ManaPerSecond, OptionalCost, PowerDisplayID, AltPowerBarID, SpellID FROM spell_power ORDER BY ID DESC");

            // SpellPowerDifficulty.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_POWER_DIFFICULTY, "SELECT DifficultyID, OrderIndex, ID FROM spell_power_difficulty ORDER BY ID DESC");

            // SpellProcsPerMinute.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE, "SELECT ID, BaseProcRate, Flags FROM spell_procs_per_minute ORDER BY ID DESC");

            // SpellProcsPerMinuteMod.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE_MOD, "SELECT ID, Coeff, Param, Type, SpellProcsPerMinuteID FROM spell_procs_per_minute_mod" +
                " ORDER BY ID DESC");

            // SpellRadius.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_RADIUS, "SELECT ID, Radius, RadiusPerLevel, RadiusMin, RadiusMax FROM spell_radius ORDER BY ID DESC");

            // SpellRange.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_RANGE, "SELECT ID, DisplayName, DisplayNameShort, RangeMin1, RangeMin2, RangeMax1, RangeMax2, Flags" +
                " FROM spell_range ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_SPELL_RANGE_LOCALE, "SELECT ID, DisplayName_lang, DisplayNameShort_lang FROM spell_range_locale WHERE locale = ?");

            // SpellReagents.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_REAGENTS, "SELECT ID, SpellID, Reagent1, Reagent2, Reagent3, Reagent4, Reagent5, Reagent6, Reagent7, Reagent8, " +
                "ReagentCount1, ReagentCount2, ReagentCount3, ReagentCount4, ReagentCount5, ReagentCount6, ReagentCount7, ReagentCount8 FROM spell_reagents" +
                " ORDER BY ID DESC");

            // SpellScaling.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_SCALING, "SELECT ID, SpellID, ScalesFromItemLevel, Class, MinScalingLevel, MaxScalingLevel FROM spell_scaling" +
                " ORDER BY ID DESC");

            // SpellShapeshift.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_SHAPESHIFT, "SELECT ID, SpellID, ShapeshiftExclude1, ShapeshiftExclude2, ShapeshiftMask1, ShapeshiftMask2, " +
                "StanceBarOrder FROM spell_shapeshift ORDER BY ID DESC");

            // SpellShapeshiftForm.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM, "SELECT ID, Name, DamageVariance, Flags, CombatRoundTime, MountTypeID, CreatureType, " +
                "BonusActionBar, AttackIconFileID, CreatureDisplayID1, CreatureDisplayID2, CreatureDisplayID3, CreatureDisplayID4, PresetSpellID1, " +
                "PresetSpellID2, PresetSpellID3, PresetSpellID4, PresetSpellID5, PresetSpellID6, PresetSpellID7, PresetSpellID8 FROM spell_shapeshift_form" +
                " ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM_LOCALE, "SELECT ID, Name_lang FROM spell_shapeshift_form_locale WHERE locale = ?");

            // SpellTargetRestrictions.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_TARGET_RESTRICTIONS, "SELECT ID, ConeDegrees, Width, Targets, TargetCreatureType, DifficultyID, MaxTargets, " +
                "MaxTargetLevel, SpellID FROM spell_target_restrictions ORDER BY ID DESC");

            // SpellTotems.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_TOTEMS, "SELECT ID, SpellID, Totem1, Totem2, RequiredTotemCategoryID1, RequiredTotemCategoryID2" +
                " FROM spell_totems ORDER BY ID DESC");

            // SpellXSpellVisual.db2
            PrepareStatement(HotfixStatements.SEL_SPELL_X_SPELL_VISUAL, "SELECT SpellVisualID, ID, Probability, CasterPlayerConditionID, CasterUnitConditionID, " +
                "ViewerPlayerConditionID, ViewerUnitConditionID, SpellIconFileID, ActiveIconFileID, Flags, DifficultyID, Priority, SpellID" +
                " FROM spell_x_spell_visual ORDER BY ID DESC");

            // SummonProperties.db2
            PrepareStatement(HotfixStatements.SEL_SUMMON_PROPERTIES, "SELECT ID, Flags, Control, Faction, Title, Slot FROM summon_properties ORDER BY ID DESC");

            // TactKey.db2
            PrepareStatement(HotfixStatements.SEL_TACT_KEY, "SELECT ID, Key1, Key2, Key3, Key4, Key5, Key6, Key7, Key8, Key9, Key10, Key11, Key12, Key13, Key14, Key15, " +
                "Key16 FROM tact_key ORDER BY ID DESC");

            // Talent.db2
            PrepareStatement(HotfixStatements.SEL_TALENT, "SELECT ID, Description, SpellID, OverridesSpellID, SpecID, TierID, ColumnIndex, Flags, CategoryMask1, " +
                "CategoryMask2, ClassID FROM talent ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_TALENT_LOCALE, "SELECT ID, Description_lang FROM talent_locale WHERE locale = ?");

            // TaxiNodes.db2
            PrepareStatement(HotfixStatements.SEL_TAXI_NODES, "SELECT ID, Name, PosX, PosY, PosZ, MountCreatureID1, MountCreatureID2, MapOffsetX, MapOffsetY, Facing, " +
                "FlightMapOffsetX, FlightMapOffsetY, ContinentID, ConditionID, CharacterBitNumber, Flags, UiTextureKitID, SpecialIconConditionID" +
                " FROM taxi_nodes ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_TAXI_NODES_LOCALE, "SELECT ID, Name_lang FROM taxi_nodes_locale WHERE locale = ?");

            // TaxiPath.db2
            PrepareStatement(HotfixStatements.SEL_TAXI_PATH, "SELECT FromTaxiNode, ToTaxiNode, ID, Cost FROM taxi_path ORDER BY ID DESC");

            // TaxiPathNode.db2
            PrepareStatement(HotfixStatements.SEL_TAXI_PATH_NODE, "SELECT LocX, LocY, LocZ, PathID, ContinentID, NodeIndex, ID, Flags, Delay, ArrivalEventID, " +
                "DepartureEventID FROM taxi_path_node ORDER BY ID DESC");

            // TotemCategory.db2
            PrepareStatement(HotfixStatements.SEL_TOTEM_CATEGORY, "SELECT ID, Name, TotemCategoryMask, TotemCategoryType FROM totem_category ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_TOTEM_CATEGORY_LOCALE, "SELECT ID, Name_lang FROM totem_category_locale WHERE locale = ?");

            // Toy.db2
            PrepareStatement(HotfixStatements.SEL_TOY, "SELECT SourceText, ItemID, Flags, SourceTypeEnum, ID FROM toy ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_TOY_LOCALE, "SELECT ID, SourceText_lang FROM toy_locale WHERE locale = ?");

            // TransmogHoliday.db2
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_HOLIDAY, "SELECT ID, RequiredTransmogHoliday FROM transmog_holiday ORDER BY ID DESC");

            // TransmogSet.db2
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET, "SELECT Name, ParentTransmogSetID, UiOrder, ExpansionID, ID, Flags, TrackingQuestID, ClassMask, " +
                "ItemNameDescriptionID, TransmogSetGroupID FROM transmog_set ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET_LOCALE, "SELECT ID, Name_lang FROM transmog_set_locale WHERE locale = ?");

            // TransmogSetGroup.db2
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET_GROUP, "SELECT Name, ID FROM transmog_set_group ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET_GROUP_LOCALE, "SELECT ID, Name_lang FROM transmog_set_group_locale WHERE locale = ?");

            // TransmogSetItem.db2
            PrepareStatement(HotfixStatements.SEL_TRANSMOG_SET_ITEM, "SELECT ID, TransmogSetID, ItemModifiedAppearanceID, Flags FROM transmog_set_item ORDER BY ID DESC");

            // TransportAnimation.db2
            PrepareStatement(HotfixStatements.SEL_TRANSPORT_ANIMATION, "SELECT ID, TimeIndex, PosX, PosY, PosZ, SequenceID, TransportID FROM transport_animation" +
                " ORDER BY ID DESC");

            // TransportRotation.db2
            PrepareStatement(HotfixStatements.SEL_TRANSPORT_ROTATION, "SELECT ID, TimeIndex, Rot1, Rot2, Rot3, Rot4, GameObjectsID FROM transport_rotation" +
                " ORDER BY ID DESC");

            // UnitPowerBar.db2
            PrepareStatement(HotfixStatements.SEL_UNIT_POWER_BAR, "SELECT ID, Name, Cost, OutOfError, ToolTip, RegenerationPeace, RegenerationCombat, FileDataID1, " +
                "FileDataID2, FileDataID3, FileDataID4, FileDataID5, FileDataID6, Color1, Color2, Color3, Color4, Color5, Color6, StartInset, EndInset, " +
                "StartPower, Flags, CenterPower, BarType, MinPower, MaxPower FROM unit_power_bar ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_UNIT_POWER_BAR_LOCALE, "SELECT ID, Name_lang, Cost_lang, OutOfError_lang, ToolTip_lang FROM unit_power_bar_locale" +
                " WHERE locale = ?");

            // Vehicle.db2
            PrepareStatement(HotfixStatements.SEL_VEHICLE, "SELECT ID, Flags, TurnSpeed, PitchSpeed, PitchMin, PitchMax, MouseLookOffsetPitch, CameraFadeDistScalarMin, " +
                "CameraFadeDistScalarMax, CameraPitchOffset, FacingLimitRight, FacingLimitLeft, CameraYawOffset, SeatID1, SeatID2, SeatID3, SeatID4, SeatID5, " +
                "SeatID6, SeatID7, SeatID8, VehicleUIIndicatorID, PowerDisplayID1, PowerDisplayID2, PowerDisplayID3, FlagsB, UiLocomotionType, " +
                "MissileTargetingID FROM vehicle ORDER BY ID DESC");

            // VehicleSeat.db2
            PrepareStatement(HotfixStatements.SEL_VEHICLE_SEAT, "SELECT ID, Flags, FlagsB, FlagsC, AttachmentOffsetX, AttachmentOffsetY, AttachmentOffsetZ, " +
                "EnterPreDelay, EnterSpeed, EnterGravity, EnterMinDuration, EnterMaxDuration, EnterMinArcHeight, EnterMaxArcHeight, ExitPreDelay, ExitSpeed, " +
                "ExitGravity, ExitMinDuration, ExitMaxDuration, ExitMinArcHeight, ExitMaxArcHeight, PassengerYaw, PassengerPitch, PassengerRoll, " +
                "VehicleEnterAnimDelay, VehicleExitAnimDelay, CameraEnteringDelay, CameraEnteringDuration, CameraExitingDelay, CameraExitingDuration, " +
                "CameraOffsetX, CameraOffsetY, CameraOffsetZ, CameraPosChaseRate, CameraFacingChaseRate, CameraEnteringZoom, CameraSeatZoomMin, " +
                "CameraSeatZoomMax, UiSkinFileDataID, EnterAnimStart, EnterAnimLoop, RideAnimStart, RideAnimLoop, RideUpperAnimStart, RideUpperAnimLoop, " +
                "ExitAnimStart, ExitAnimLoop, ExitAnimEnd, VehicleEnterAnim, VehicleExitAnim, VehicleRideAnimLoop, EnterAnimKitID, RideAnimKitID, " +
                "ExitAnimKitID, VehicleEnterAnimKitID, VehicleRideAnimKitID, VehicleExitAnimKitID, CameraModeID, AttachmentID, PassengerAttachmentID, " +
                "VehicleEnterAnimBone, VehicleExitAnimBone, VehicleRideAnimLoopBone, VehicleAbilityDisplay, EnterUISoundID, ExitUISoundID FROM vehicle_seat" +
                " ORDER BY ID DESC");

            // WmoAreaTable.db2
            PrepareStatement(HotfixStatements.SEL_WMO_AREA_TABLE, "SELECT AreaName, WmoGroupID, AmbienceID, ZoneMusic, IntroSound, AreaTableID, UwIntroSound, " +
                "UwAmbience, NameSetID, SoundProviderPref, SoundProviderPrefUnderwater, Flags, ID, UwZoneMusic, WmoID FROM wmo_area_table ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_WMO_AREA_TABLE_LOCALE, "SELECT ID, AreaName_lang FROM wmo_area_table_locale WHERE locale = ?");

            // WorldEffect.db2
            PrepareStatement(HotfixStatements.SEL_WORLD_EFFECT, "SELECT ID, TargetAsset, CombatConditionID, TargetType, WhenToDisplay, QuestFeedbackEffectID, " +
                "PlayerConditionID FROM world_effect ORDER BY ID DESC");

            // WorldMapArea.db2
            PrepareStatement(HotfixStatements.SEL_WORLD_MAP_AREA, "SELECT AreaName, LocLeft, LocRight, LocTop, LocBottom, Flags, MapID, AreaID, DisplayMapID, " +
                "DefaultDungeonFloor, ParentWorldMapID, LevelRangeMin, LevelRangeMax, BountySetID, BountyDisplayLocation, ID, VisibilityPlayerConditionID" +
                " FROM world_map_area ORDER BY ID DESC");

            // WorldMapOverlay.db2
            PrepareStatement(HotfixStatements.SEL_WORLD_MAP_OVERLAY, "SELECT TextureName, ID, TextureWidth, TextureHeight, MapAreaID, OffsetX, OffsetY, HitRectTop, " +
                "HitRectLeft, HitRectBottom, HitRectRight, PlayerConditionID, Flags, AreaID1, AreaID2, AreaID3, AreaID4 FROM world_map_overlay" +
                " ORDER BY ID DESC");

            // WorldMapTransforms.db2
            PrepareStatement(HotfixStatements.SEL_WORLD_MAP_TRANSFORMS, "SELECT ID, RegionMinX, RegionMinY, RegionMinZ, RegionMaxX, RegionMaxY, RegionMaxZ, " +
                "RegionOffsetX, RegionOffsetY, RegionScale, MapID, AreaID, NewMapID, NewDungeonMapID, NewAreaID, Flags, Priority FROM world_map_transforms" +
                " ORDER BY ID DESC");

            // WorldSafeLocs.db2
            PrepareStatement(HotfixStatements.SEL_WORLD_SAFE_LOCS, "SELECT ID, AreaName, LocX, LocY, LocZ, Facing, MapID FROM world_safe_locs ORDER BY ID DESC");
            PrepareStatement(HotfixStatements.SEL_WORLD_SAFE_LOCS_LOCALE, "SELECT ID, AreaName_lang FROM world_safe_locs_locale WHERE locale = ?");
        }
    }

    public enum HotfixStatements
    {
        None = 0, 

        SEL_ACHIEVEMENT,
        SEL_ACHIEVEMENT_LOCALE,

        SEL_ANIM_KIT,

        SEL_AREA_GROUP_MEMBER,

        SEL_AREA_TABLE,
        SEL_AREA_TABLE_LOCALE,

        SEL_AREA_TRIGGER,

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

        SEL_CHARACTER_FACIAL_HAIR_STYLES,

        SEL_CHAR_BASE_SECTION,

        SEL_CHAR_SECTIONS,

        SEL_CHAR_START_OUTFIT,

        SEL_CHAR_TITLES,
        SEL_CHAR_TITLES_LOCALE,

        SEL_CHAT_CHANNELS,
        SEL_CHAT_CHANNELS_LOCALE,

        SEL_CHR_CLASSES,
        SEL_CHR_CLASSES_LOCALE,

        SEL_CHR_CLASSES_X_POWER_TYPES,

        SEL_CHR_RACES,
        SEL_CHR_RACES_LOCALE,

        SEL_CHR_SPECIALIZATION,
        SEL_CHR_SPECIALIZATION_LOCALE,

        SEL_CINEMATIC_CAMERA,

        SEL_CINEMATIC_SEQUENCES,

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

        SEL_FACTION,
        SEL_FACTION_LOCALE,

        SEL_FACTION_TEMPLATE,

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

        SEL_GARR_PLOT,
        SEL_GARR_PLOT_LOCALE,

        SEL_GARR_PLOT_BUILDING,

        SEL_GARR_PLOT_INSTANCE,

        SEL_GARR_SITE_LEVEL,

        SEL_GARR_SITE_LEVEL_PLOT_INST,

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

        SEL_ITEM_PRICE_BASE,

        SEL_ITEM_RANDOM_PROPERTIES,
        SEL_ITEM_RANDOM_PROPERTIES_LOCALE,

        SEL_ITEM_RANDOM_SUFFIX,
        SEL_ITEM_RANDOM_SUFFIX_LOCALE,

        SEL_ITEM_SEARCH_NAME,
        SEL_ITEM_SEARCH_NAME_LOCALE,

        SEL_ITEM_SET,
        SEL_ITEM_SET_LOCALE,

        SEL_ITEM_SET_SPELL,

        SEL_ITEM_SPARSE,
        SEL_ITEM_SPARSE_LOCALE,

        SEL_ITEM_SPEC,

        SEL_ITEM_SPEC_OVERRIDE,

        SEL_ITEM_UPGRADE,

        SEL_ITEM_X_BONUS_TREE,

        SEL_KEYCHAIN,

        SEL_LFG_DUNGEONS,
        SEL_LFG_DUNGEONS_LOCALE,

        SEL_LIGHT,

        SEL_LIQUID_TYPE,

        SEL_LOCK,

        SEL_MAIL_TEMPLATE,
        SEL_MAIL_TEMPLATE_LOCALE,

        SEL_MAP,
        SEL_MAP_LOCALE,

        SEL_MAP_DIFFICULTY,
        SEL_MAP_DIFFICULTY_LOCALE,

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

        SEL_OVERRIDE_SPELL_DATA,

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

        SEL_PVP_REWARD,

        SEL_PVP_TALENT,
        SEL_PVP_TALENT_LOCALE,

        SEL_PVP_TALENT_UNLOCK,

        SEL_QUEST_FACTION_REWARD,

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

        SEL_RULESET_ITEM_UPGRADE,

        SEL_SANDBOX_SCALING,

        SEL_SCALING_STAT_DISTRIBUTION,

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

        SEL_SPELL,
        SEL_SPELL_LOCALE,

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

        SEL_SPELL_LEARN_SPELL,

        SEL_SPELL_LEVELS,

        SEL_SPELL_MISC,

        SEL_SPELL_POWER,

        SEL_SPELL_POWER_DIFFICULTY,

        SEL_SPELL_PROCS_PER_MINUTE,

        SEL_SPELL_PROCS_PER_MINUTE_MOD,

        SEL_SPELL_RADIUS,

        SEL_SPELL_RANGE,
        SEL_SPELL_RANGE_LOCALE,

        SEL_SPELL_REAGENTS,

        SEL_SPELL_SCALING,

        SEL_SPELL_SHAPESHIFT,

        SEL_SPELL_SHAPESHIFT_FORM,
        SEL_SPELL_SHAPESHIFT_FORM_LOCALE,

        SEL_SPELL_TARGET_RESTRICTIONS,

        SEL_SPELL_TOTEMS,

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

        SEL_UNIT_POWER_BAR,
        SEL_UNIT_POWER_BAR_LOCALE,

        SEL_VEHICLE,

        SEL_VEHICLE_SEAT,

        SEL_WMO_AREA_TABLE,
        SEL_WMO_AREA_TABLE_LOCALE,

        SEL_WORLD_EFFECT,

        SEL_WORLD_MAP_AREA,

        SEL_WORLD_MAP_OVERLAY,

        SEL_WORLD_MAP_TRANSFORMS,

        SEL_WORLD_SAFE_LOCS,
        SEL_WORLD_SAFE_LOCS_LOCALE,

        MAX_HOTFIXDATABASE_STATEMENTS
    }
}
