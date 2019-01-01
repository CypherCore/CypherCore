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

using Framework.Constants;
using Framework.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.DataStorage
{
    public class CliDB
    {
        internal static int LoadedFileCount;
        internal static string DataPath;

        public static void LoadStores(string dataPath, LocaleConstant defaultLocale)
        {
            uint oldMSTime = Time.GetMSTime();
            LoadedFileCount = 0;

            DataPath = dataPath + "/dbc/" + defaultLocale + "/";

            AchievementStorage = DBReader.Read<AchievementRecord>("Achievement.db2", HotfixStatements.SEL_ACHIEVEMENT, HotfixStatements.SEL_ACHIEVEMENT_LOCALE);
            AnimationDataStorage = DBReader.Read<AnimationDataRecord>("AnimationData.db2", HotfixStatements.SEL_ANIMATION_DATA);
            AnimKitStorage = DBReader.Read<AnimKitRecord>("AnimKit.db2", HotfixStatements.SEL_ANIM_KIT);
            AreaGroupMemberStorage = DBReader.Read<AreaGroupMemberRecord>("AreaGroupMember.db2", HotfixStatements.SEL_AREA_GROUP_MEMBER);
            AreaTableStorage = DBReader.Read<AreaTableRecord>("AreaTable.db2", HotfixStatements.SEL_AREA_TABLE, HotfixStatements.SEL_AREA_TABLE_LOCALE);
            AreaTriggerStorage = DBReader.Read<AreaTriggerRecord>("AreaTrigger.db2", HotfixStatements.SEL_AREA_TRIGGER);
            ArmorLocationStorage = DBReader.Read<ArmorLocationRecord>("ArmorLocation.db2", HotfixStatements.SEL_ARMOR_LOCATION);
            ArtifactStorage = DBReader.Read<ArtifactRecord>("Artifact.db2", HotfixStatements.SEL_ARTIFACT, HotfixStatements.SEL_ARTIFACT_APPEARANCE_LOCALE);
            ArtifactAppearanceStorage = DBReader.Read<ArtifactAppearanceRecord>("ArtifactAppearance.db2", HotfixStatements.SEL_ARTIFACT_APPEARANCE, HotfixStatements.SEL_ARTIFACT_APPEARANCE_LOCALE);
            ArtifactAppearanceSetStorage = DBReader.Read<ArtifactAppearanceSetRecord>("ArtifactAppearanceSet.db2", HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET, HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET_LOCALE);
            ArtifactCategoryStorage = DBReader.Read<ArtifactCategoryRecord>("ArtifactCategory.db2", HotfixStatements.SEL_ARTIFACT_CATEGORY);
            ArtifactPowerStorage = DBReader.Read<ArtifactPowerRecord>("ArtifactPower.db2", HotfixStatements.SEL_ARTIFACT_POWER);
            ArtifactPowerLinkStorage = DBReader.Read<ArtifactPowerLinkRecord>("ArtifactPowerLink.db2", HotfixStatements.SEL_ARTIFACT_POWER_LINK);
            ArtifactPowerPickerStorage = DBReader.Read<ArtifactPowerPickerRecord>("ArtifactPowerPicker.db2", HotfixStatements.SEL_ARTIFACT_POWER_PICKER);
            ArtifactPowerRankStorage = DBReader.Read<ArtifactPowerRankRecord>("ArtifactPowerRank.db2", HotfixStatements.SEL_ARTIFACT_POWER_RANK);
            //ArtifactQuestXPStorage = DBReader.Read<ArtifactQuestXPRecord>("ArtifactQuestXP.db2", HotfixStatements.SEL_ARTIFACT_QUEST_XP);
            ArtifactTierStorage = DBReader.Read<ArtifactTierRecord> ("ArtifactTier.db2", HotfixStatements.SEL_ARTIFACT_TIER);
            ArtifactUnlockStorage = DBReader.Read<ArtifactUnlockRecord> ("ArtifactUnlock.db2", HotfixStatements.SEL_ARTIFACT_UNLOCK);
            AuctionHouseStorage = DBReader.Read<AuctionHouseRecord>("AuctionHouse.db2", HotfixStatements.SEL_AUCTION_HOUSE, HotfixStatements.SEL_AUCTION_HOUSE_LOCALE);
            BankBagSlotPricesStorage = DBReader.Read<BankBagSlotPricesRecord>("BankBagSlotPrices.db2", HotfixStatements.SEL_BANK_BAG_SLOT_PRICES);
            //BannedAddOnsStorage = DBReader.Read<BannedAddOnsRecord>("BannedAddons.db2", HotfixStatements.SEL_BANNED_ADDONS);
            BarberShopStyleStorage = DBReader.Read<BarberShopStyleRecord>("BarberShopStyle.db2", HotfixStatements.SEL_BARBER_SHOP_STYLE, HotfixStatements.SEL_BARBER_SHOP_STYLE_LOCALE);
            BattlePetBreedQualityStorage = DBReader.Read<BattlePetBreedQualityRecord>("BattlePetBreedQuality.db2", HotfixStatements.SEL_BATTLE_PET_BREED_QUALITY);
            BattlePetBreedStateStorage = DBReader.Read<BattlePetBreedStateRecord>("BattlePetBreedState.db2", HotfixStatements.SEL_BATTLE_PET_BREED_STATE);
            BattlePetSpeciesStorage = DBReader.Read<BattlePetSpeciesRecord>("BattlePetSpecies.db2", HotfixStatements.SEL_BATTLE_PET_SPECIES, HotfixStatements.SEL_BATTLE_PET_SPECIES_LOCALE);
            BattlePetSpeciesStateStorage = DBReader.Read<BattlePetSpeciesStateRecord>("BattlePetSpeciesState.db2", HotfixStatements.SEL_BATTLE_PET_SPECIES_STATE);
            BattlemasterListStorage = DBReader.Read<BattlemasterListRecord>("BattlemasterList.db2", HotfixStatements.SEL_BATTLEMASTER_LIST, HotfixStatements.SEL_BATTLEMASTER_LIST_LOCALE);
            BroadcastTextStorage = DBReader.Read<BroadcastTextRecord>("BroadcastText.db2", HotfixStatements.SEL_BROADCAST_TEXT, HotfixStatements.SEL_BROADCAST_TEXT_LOCALE);
            CharacterFacialHairStylesStorage = DBReader.Read<CharacterFacialHairStylesRecord>("CharacterFacialHairStyles.db2", HotfixStatements.SEL_CHARACTER_FACIAL_HAIR_STYLES);
            CharBaseSectionStorage = DBReader.Read<CharBaseSectionRecord>("CharBaseSection.db2", HotfixStatements.SEL_CHAR_BASE_SECTION);
            CharSectionsStorage = DBReader.Read<CharSectionsRecord>("CharSections.db2", HotfixStatements.SEL_CHAR_SECTIONS);
            CharStartOutfitStorage = DBReader.Read<CharStartOutfitRecord>("CharStartOutfit.db2", HotfixStatements.SEL_CHAR_START_OUTFIT);
            CharTitlesStorage = DBReader.Read<CharTitlesRecord>("CharTitles.db2", HotfixStatements.SEL_CHAR_TITLES, HotfixStatements.SEL_CHAR_TITLES_LOCALE);
            ChatChannelsStorage = DBReader.Read<ChatChannelsRecord>("ChatChannels.db2", HotfixStatements.SEL_CHAT_CHANNELS, HotfixStatements.SEL_CHAT_CHANNELS_LOCALE);
            ChrClassesStorage = DBReader.Read<ChrClassesRecord>("ChrClasses.db2", HotfixStatements.SEL_CHR_CLASSES, HotfixStatements.SEL_CHR_CLASSES_LOCALE);
            ChrClassesXPowerTypesStorage = DBReader.Read<ChrClassesXPowerTypesRecord>("ChrClassesXPowerTypes.db2", HotfixStatements.SEL_CHR_CLASSES_X_POWER_TYPES);
            ChrRacesStorage = DBReader.Read<ChrRacesRecord>("ChrRaces.db2", HotfixStatements.SEL_CHR_RACES, HotfixStatements.SEL_CHR_RACES_LOCALE);
            ChrSpecializationStorage = DBReader.Read<ChrSpecializationRecord>("ChrSpecialization.db2", HotfixStatements.SEL_CHR_SPECIALIZATION, HotfixStatements.SEL_CHR_SPECIALIZATION_LOCALE);
            CinematicCameraStorage = DBReader.Read<CinematicCameraRecord>("CinematicCamera.db2", HotfixStatements.SEL_CINEMATIC_CAMERA);
            CinematicSequencesStorage = DBReader.Read<CinematicSequencesRecord>("CinematicSequences.db2", HotfixStatements.SEL_CINEMATIC_SEQUENCES);
            ContentTuningStorage = DBReader.Read<ContentTuningRecord>("ContentTuning.db2", HotfixStatements.SEL_CONTENT_TUNING);
            ConversationLineStorage = DBReader.Read<ConversationLineRecord>("ConversationLine.db2", HotfixStatements.SEL_CONVERSATION_LINE);
            CreatureDisplayInfoStorage = DBReader.Read<CreatureDisplayInfoRecord>("CreatureDisplayInfo.db2", HotfixStatements.SEL_CREATURE_DISPLAY_INFO);
            CreatureDisplayInfoExtraStorage = DBReader.Read<CreatureDisplayInfoExtraRecord>("CreatureDisplayInfoExtra.db2", HotfixStatements.SEL_CREATURE_DISPLAY_INFO_EXTRA);
            CreatureFamilyStorage = DBReader.Read<CreatureFamilyRecord>("CreatureFamily.db2", HotfixStatements.SEL_CREATURE_FAMILY, HotfixStatements.SEL_CREATURE_FAMILY_LOCALE);
            CreatureModelDataStorage = DBReader.Read<CreatureModelDataRecord>("CreatureModelData.db2", HotfixStatements.SEL_CREATURE_MODEL_DATA);
            CreatureTypeStorage = DBReader.Read<CreatureTypeRecord>("CreatureType.db2", HotfixStatements.SEL_CREATURE_TYPE, HotfixStatements.SEL_CREATURE_TYPE_LOCALE);
            CriteriaStorage = DBReader.Read<CriteriaRecord>("Criteria.db2", HotfixStatements.SEL_CRITERIA);
            CriteriaTreeStorage = DBReader.Read<CriteriaTreeRecord>("CriteriaTree.db2", HotfixStatements.SEL_CRITERIA_TREE, HotfixStatements.SEL_CRITERIA_TREE_LOCALE);
            CurrencyTypesStorage = DBReader.Read<CurrencyTypesRecord>("CurrencyTypes.db2", HotfixStatements.SEL_CURRENCY_TYPES, HotfixStatements.SEL_CURRENCY_TYPES_LOCALE);
            CurveStorage = DBReader.Read<CurveRecord>("Curve.db2", HotfixStatements.SEL_CURVE);
            CurvePointStorage = DBReader.Read<CurvePointRecord>("CurvePoint.db2", HotfixStatements.SEL_CURVE_POINT);
            DestructibleModelDataStorage = DBReader.Read<DestructibleModelDataRecord>("DestructibleModelData.db2", HotfixStatements.SEL_DESTRUCTIBLE_MODEL_DATA);
            DifficultyStorage = DBReader.Read<DifficultyRecord>("Difficulty.db2", HotfixStatements.SEL_DIFFICULTY, HotfixStatements.SEL_DIFFICULTY_LOCALE);
            DungeonEncounterStorage = DBReader.Read<DungeonEncounterRecord>("DungeonEncounter.db2", HotfixStatements.SEL_DUNGEON_ENCOUNTER, HotfixStatements.SEL_DUNGEON_ENCOUNTER_LOCALE);
            DurabilityCostsStorage = DBReader.Read<DurabilityCostsRecord>("DurabilityCosts.db2", HotfixStatements.SEL_DURABILITY_COSTS);
            DurabilityQualityStorage = DBReader.Read<DurabilityQualityRecord>("DurabilityQuality.db2", HotfixStatements.SEL_DURABILITY_QUALITY);
            EmotesStorage = DBReader.Read<EmotesRecord>("Emotes.db2", HotfixStatements.SEL_EMOTES);
            EmotesTextStorage = DBReader.Read<EmotesTextRecord>("EmotesText.db2", HotfixStatements.SEL_EMOTES_TEXT);
            EmotesTextSoundStorage = DBReader.Read<EmotesTextSoundRecord>("EmotesTextSound.db2", HotfixStatements.SEL_EMOTES_TEXT_SOUND);
            ExpectedStatStorage = DBReader.Read <ExpectedStatRecord>("ExpectedStat.db2", HotfixStatements.SEL_EXPECTED_STAT);
            ExpectedStatModStorage = DBReader.Read <ExpectedStatModRecord>("ExpectedStatMod.db2", HotfixStatements.SEL_EXPECTED_STAT_MOD);
            FactionStorage = DBReader.Read<FactionRecord>("Faction.db2", HotfixStatements.SEL_FACTION, HotfixStatements.SEL_FACTION_LOCALE);
            FactionTemplateStorage = DBReader.Read<FactionTemplateRecord>("FactionTemplate.db2", HotfixStatements.SEL_FACTION_TEMPLATE);
            GameObjectDisplayInfoStorage = DBReader.Read<GameObjectDisplayInfoRecord>("GameObjectDisplayInfo.db2", HotfixStatements.SEL_GAMEOBJECT_DISPLAY_INFO);
            GameObjectsStorage = DBReader.Read<GameObjectsRecord>("GameObjects.db2", HotfixStatements.SEL_GAMEOBJECTS, HotfixStatements.SEL_GAMEOBJECTS_LOCALE);
            GarrAbilityStorage = DBReader.Read<GarrAbilityRecord>("GarrAbility.db2", HotfixStatements.SEL_GARR_ABILITY, HotfixStatements.SEL_GARR_ABILITY_LOCALE);
            GarrBuildingStorage = DBReader.Read<GarrBuildingRecord>("GarrBuilding.db2", HotfixStatements.SEL_GARR_BUILDING, HotfixStatements.SEL_GARR_BUILDING_LOCALE);
            GarrBuildingPlotInstStorage = DBReader.Read<GarrBuildingPlotInstRecord>("GarrBuildingPlotInst.db2", HotfixStatements.SEL_GARR_BUILDING_PLOT_INST);
            GarrClassSpecStorage = DBReader.Read<GarrClassSpecRecord>("GarrClassSpec.db2", HotfixStatements.SEL_GARR_CLASS_SPEC, HotfixStatements.SEL_GARR_CLASS_SPEC_LOCALE);
            GarrFollowerStorage = DBReader.Read<GarrFollowerRecord>("GarrFollower.db2", HotfixStatements.SEL_GARR_FOLLOWER, HotfixStatements.SEL_GARR_FOLLOWER_LOCALE);
            GarrFollowerXAbilityStorage = DBReader.Read<GarrFollowerXAbilityRecord>("GarrFollowerXAbility.db2", HotfixStatements.SEL_GARR_FOLLOWER_X_ABILITY);
            GarrPlotStorage = DBReader.Read<GarrPlotRecord>("GarrPlot.db2", HotfixStatements.SEL_GARR_PLOT);
            GarrPlotBuildingStorage = DBReader.Read<GarrPlotBuildingRecord>("GarrPlotBuilding.db2", HotfixStatements.SEL_GARR_PLOT_BUILDING);
            GarrPlotInstanceStorage = DBReader.Read<GarrPlotInstanceRecord>("GarrPlotInstance.db2", HotfixStatements.SEL_GARR_PLOT_INSTANCE);
            GarrSiteLevelStorage = DBReader.Read<GarrSiteLevelRecord>("GarrSiteLevel.db2", HotfixStatements.SEL_GARR_SITE_LEVEL);
            GarrSiteLevelPlotInstStorage = DBReader.Read<GarrSiteLevelPlotInstRecord>("GarrSiteLevelPlotInst.db2", HotfixStatements.SEL_GARR_SITE_LEVEL_PLOT_INST);
            GemPropertiesStorage = DBReader.Read<GemPropertiesRecord>("GemProperties.db2", HotfixStatements.SEL_GEM_PROPERTIES);
            GlyphBindableSpellStorage = DBReader.Read<GlyphBindableSpellRecord>("GlyphBindableSpell.db2", HotfixStatements.SEL_GLYPH_BINDABLE_SPELL);
            GlyphPropertiesStorage = DBReader.Read<GlyphPropertiesRecord>("GlyphProperties.db2", HotfixStatements.SEL_GLYPH_PROPERTIES);
            GlyphRequiredSpecStorage = DBReader.Read<GlyphRequiredSpecRecord>("GlyphRequiredSpec.db2", HotfixStatements.SEL_GLYPH_REQUIRED_SPEC);
            GuildColorBackgroundStorage = DBReader.Read<GuildColorBackgroundRecord>("GuildColorBackground.db2", HotfixStatements.SEL_GUILD_COLOR_BACKGROUND);
            GuildColorBorderStorage = DBReader.Read<GuildColorBorderRecord>("GuildColorBorder.db2", HotfixStatements.SEL_GUILD_COLOR_BORDER);
            GuildColorEmblemStorage = DBReader.Read<GuildColorEmblemRecord>("GuildColorEmblem.db2", HotfixStatements.SEL_GUILD_COLOR_EMBLEM);
            GuildPerkSpellsStorage = DBReader.Read<GuildPerkSpellsRecord>("GuildPerkSpells.db2", HotfixStatements.SEL_GUILD_PERK_SPELLS);
            HeirloomStorage = DBReader.Read<HeirloomRecord>("Heirloom.db2", HotfixStatements.SEL_HEIRLOOM, HotfixStatements.SEL_HEIRLOOM_LOCALE);
            HolidaysStorage = DBReader.Read<HolidaysRecord>("Holidays.db2", HotfixStatements.SEL_HOLIDAYS);
            ImportPriceArmorStorage = DBReader.Read<ImportPriceArmorRecord>("ImportPriceArmor.db2", HotfixStatements.SEL_IMPORT_PRICE_ARMOR);
            ImportPriceQualityStorage = DBReader.Read<ImportPriceQualityRecord>("ImportPriceQuality.db2", HotfixStatements.SEL_IMPORT_PRICE_QUALITY);
            ImportPriceShieldStorage = DBReader.Read<ImportPriceShieldRecord>("ImportPriceShield.db2", HotfixStatements.SEL_IMPORT_PRICE_SHIELD);
            ImportPriceWeaponStorage = DBReader.Read<ImportPriceWeaponRecord>("ImportPriceWeapon.db2", HotfixStatements.SEL_IMPORT_PRICE_WEAPON);
            ItemAppearanceStorage = DBReader.Read<ItemAppearanceRecord>("ItemAppearance.db2", HotfixStatements.SEL_ITEM_APPEARANCE);
            ItemArmorQualityStorage = DBReader.Read<ItemArmorQualityRecord>("ItemArmorQuality.db2", HotfixStatements.SEL_ITEM_ARMOR_QUALITY);
            ItemArmorShieldStorage = DBReader.Read<ItemArmorShieldRecord>("ItemArmorShield.db2", HotfixStatements.SEL_ITEM_ARMOR_SHIELD);
            ItemArmorTotalStorage = DBReader.Read<ItemArmorTotalRecord>("ItemArmorTotal.db2", HotfixStatements.SEL_ITEM_ARMOR_TOTAL);
            //ItemBagFamilyStorage = DBReader.Read<ItemBagFamilyRecord>("ItemBagFamily.db2", HotfixStatements.SEL_ITEM_BAG_FAMILY, HotfixStatements.SEL_ITEM_BAG_FAMILY_LOCALE);
            ItemBonusStorage = DBReader.Read<ItemBonusRecord>("ItemBonus.db2", HotfixStatements.SEL_ITEM_BONUS);
            ItemBonusListLevelDeltaStorage = DBReader.Read<ItemBonusListLevelDeltaRecord>("ItemBonusListLevelDelta.db2", HotfixStatements.SEL_ITEM_BONUS_LIST_LEVEL_DELTA);
            ItemBonusTreeNodeStorage = DBReader.Read<ItemBonusTreeNodeRecord>("ItemBonusTreeNode.db2", HotfixStatements.SEL_ITEM_BONUS_TREE_NODE);
            ItemChildEquipmentStorage = DBReader.Read<ItemChildEquipmentRecord>("ItemChildEquipment.db2", HotfixStatements.SEL_ITEM_CHILD_EQUIPMENT);
            ItemClassStorage = DBReader.Read<ItemClassRecord>("ItemClass.db2", HotfixStatements.SEL_ITEM_CLASS, HotfixStatements.SEL_ITEM_CLASS_LOCALE);
            ItemCurrencyCostStorage = DBReader.Read<ItemCurrencyCostRecord>("ItemCurrencyCost.db2", HotfixStatements.SEL_ITEM_CURRENCY_COST);
            ItemDamageAmmoStorage = DBReader.Read<ItemDamageRecord>("ItemDamageAmmo.db2", HotfixStatements.SEL_ITEM_DAMAGE_AMMO);
            ItemDamageOneHandStorage = DBReader.Read<ItemDamageRecord>("ItemDamageOneHand.db2", HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND);
            ItemDamageOneHandCasterStorage = DBReader.Read<ItemDamageRecord>("ItemDamageOneHandCaster.db2", HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND_CASTER);
            ItemDamageTwoHandStorage = DBReader.Read<ItemDamageRecord>("ItemDamageTwoHand.db2", HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND);
            ItemDamageTwoHandCasterStorage = DBReader.Read<ItemDamageRecord>("ItemDamageTwoHandCaster.db2", HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND_CASTER);
            ItemDisenchantLootStorage = DBReader.Read<ItemDisenchantLootRecord>("ItemDisenchantLoot.db2", HotfixStatements.SEL_ITEM_DISENCHANT_LOOT);
            ItemEffectStorage = DBReader.Read<ItemEffectRecord>("ItemEffect.db2", HotfixStatements.SEL_ITEM_EFFECT);
            ItemStorage = DBReader.Read<ItemRecord>("Item.db2", HotfixStatements.SEL_ITEM);
            ItemExtendedCostStorage = DBReader.Read<ItemExtendedCostRecord>("ItemExtendedCost.db2", HotfixStatements.SEL_ITEM_EXTENDED_COST);
            ItemLevelSelectorStorage = DBReader.Read<ItemLevelSelectorRecord>("ItemLevelSelector.db2", HotfixStatements.SEL_ITEM_LEVEL_SELECTOR);
            ItemLevelSelectorQualityStorage = DBReader.Read<ItemLevelSelectorQualityRecord>("ItemLevelSelectorQuality.db2", HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY);
            ItemLevelSelectorQualitySetStorage = DBReader.Read<ItemLevelSelectorQualitySetRecord>("ItemLevelSelectorQualitySet.db2", HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY_SET);
            ItemLimitCategoryStorage = DBReader.Read<ItemLimitCategoryRecord>("ItemLimitCategory.db2", HotfixStatements.SEL_ITEM_LIMIT_CATEGORY, HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_LOCALE);
            ItemLimitCategoryConditionStorage = DBReader.Read<ItemLimitCategoryConditionRecord>("ItemLimitCategoryCondition.db2", HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_CONDITION);
            ItemModifiedAppearanceStorage = DBReader.Read<ItemModifiedAppearanceRecord>("ItemModifiedAppearance.db2", HotfixStatements.SEL_ITEM_MODIFIED_APPEARANCE);
            ItemPriceBaseStorage = DBReader.Read<ItemPriceBaseRecord>("ItemPriceBase.db2", HotfixStatements.SEL_ITEM_PRICE_BASE);
            ItemRandomPropertiesStorage = DBReader.Read<ItemRandomPropertiesRecord>("ItemRandomProperties.db2", HotfixStatements.SEL_ITEM_RANDOM_PROPERTIES, HotfixStatements.SEL_ITEM_RANDOM_PROPERTIES_LOCALE);
            ItemRandomSuffixStorage = DBReader.Read<ItemRandomSuffixRecord>("ItemRandomSuffix.db2", HotfixStatements.SEL_ITEM_RANDOM_SUFFIX, HotfixStatements.SEL_ITEM_RANDOM_SUFFIX_LOCALE);
            ItemSearchNameStorage = DBReader.Read<ItemSearchNameRecord>("ItemSearchName.db2", HotfixStatements.SEL_ITEM_SEARCH_NAME, HotfixStatements.SEL_ITEM_SEARCH_NAME_LOCALE);
            ItemSetStorage = DBReader.Read<ItemSetRecord>("ItemSet.db2", HotfixStatements.SEL_ITEM_SET, HotfixStatements.SEL_ITEM_SET_LOCALE);
            ItemSetSpellStorage = DBReader.Read<ItemSetSpellRecord>("ItemSetSpell.db2", HotfixStatements.SEL_ITEM_SET_SPELL);
            ItemSparseStorage = DBReader.Read<ItemSparseRecord>("ItemSparse.db2", HotfixStatements.SEL_ITEM_SPARSE, HotfixStatements.SEL_ITEM_SPARSE_LOCALE);
            ItemSpecStorage = DBReader.Read<ItemSpecRecord>("ItemSpec.db2", HotfixStatements.SEL_ITEM_SPEC);
            ItemSpecOverrideStorage = DBReader.Read<ItemSpecOverrideRecord>("ItemSpecOverride.db2", HotfixStatements.SEL_ITEM_SPEC_OVERRIDE);
            ItemUpgradeStorage = DBReader.Read<ItemUpgradeRecord>("ItemUpgrade.db2", HotfixStatements.SEL_ITEM_UPGRADE);
            ItemXBonusTreeStorage = DBReader.Read<ItemXBonusTreeRecord>("ItemXBonusTree.db2", HotfixStatements.SEL_ITEM_X_BONUS_TREE);
            //KeyChainStorage = DBReader.Read<KeyChainRecord>("KeyChain.db2", HotfixStatements.SEL_KEYCHAIN);
            LFGDungeonsStorage = DBReader.Read<LFGDungeonsRecord>("LFGDungeons.db2", HotfixStatements.SEL_LFG_DUNGEONS, HotfixStatements.SEL_LFG_DUNGEONS_LOCALE);
            LightStorage = DBReader.Read<LightRecord>("Light.db2", HotfixStatements.SEL_LIGHT);
            LiquidTypeStorage = DBReader.Read<LiquidTypeRecord>("LiquidType.db2", HotfixStatements.SEL_LIQUID_TYPE);
            LockStorage = DBReader.Read<LockRecord>("Lock.db2", HotfixStatements.SEL_LOCK);
            MailTemplateStorage = DBReader.Read<MailTemplateRecord>("MailTemplate.db2", HotfixStatements.SEL_MAIL_TEMPLATE, HotfixStatements.SEL_MAIL_TEMPLATE_LOCALE);
            MapStorage = DBReader.Read<MapRecord>("Map.db2", HotfixStatements.SEL_MAP, HotfixStatements.SEL_MAP_LOCALE);
            MapDifficultyStorage = DBReader.Read<MapDifficultyRecord>("MapDifficulty.db2", HotfixStatements.SEL_MAP_DIFFICULTY, HotfixStatements.SEL_MAP_DIFFICULTY_LOCALE);
            ModifierTreeStorage = DBReader.Read<ModifierTreeRecord>("ModifierTree.db2", HotfixStatements.SEL_MODIFIER_TREE);
            MountCapabilityStorage = DBReader.Read<MountCapabilityRecord>("MountCapability.db2", HotfixStatements.SEL_MOUNT_CAPABILITY);
            MountStorage = DBReader.Read<MountRecord>("Mount.db2", HotfixStatements.SEL_MOUNT, HotfixStatements.SEL_MOUNT_LOCALE);
            MountTypeXCapabilityStorage = DBReader.Read<MountTypeXCapabilityRecord>("MountTypeXCapability.db2", HotfixStatements.SEL_MOUNT_TYPE_X_CAPABILITY);
            MountXDisplayStorage = DBReader.Read<MountXDisplayRecord>("MountXDisplay.db2", HotfixStatements.SEL_MOUNT_X_DISPLAY);
            MovieStorage = DBReader.Read<MovieRecord>("Movie.db2", HotfixStatements.SEL_MOVIE);
            NameGenStorage = DBReader.Read<NameGenRecord>("NameGen.db2", HotfixStatements.SEL_NAME_GEN);
            NamesProfanityStorage = DBReader.Read<NamesProfanityRecord>("NamesProfanity.db2", HotfixStatements.SEL_NAMES_PROFANITY);
            NamesReservedStorage = DBReader.Read<NamesReservedRecord>("NamesReserved.db2", HotfixStatements.SEL_NAMES_RESERVED, HotfixStatements.SEL_NAMES_RESERVED_LOCALE);
            NamesReservedLocaleStorage = DBReader.Read<NamesReservedLocaleRecord>("NamesReservedLocale.db2", HotfixStatements.SEL_NAMES_RESERVED_LOCALE);
            NumTalentsAtLevelStorage = DBReader.Read<NumTalentsAtLevelRecord>("NumTalentsAtLevel.db2", HotfixStatements.SEL_NUM_TALENTS_AT_LEVEL);
            OverrideSpellDataStorage = DBReader.Read<OverrideSpellDataRecord>("OverrideSpellData.db2", HotfixStatements.SEL_OVERRIDE_SPELL_DATA);
            PhaseStorage = DBReader.Read<PhaseRecord>("Phase.db2", HotfixStatements.SEL_PHASE);
            PhaseXPhaseGroupStorage = DBReader.Read<PhaseXPhaseGroupRecord>("PhaseXPhaseGroup.db2", HotfixStatements.SEL_PHASE_X_PHASE_GROUP);
            PlayerConditionStorage = DBReader.Read<PlayerConditionRecord>("PlayerCondition.db2", HotfixStatements.SEL_PLAYER_CONDITION, HotfixStatements.SEL_PLAYER_CONDITION_LOCALE);
            PowerDisplayStorage = DBReader.Read<PowerDisplayRecord>("PowerDisplay.db2", HotfixStatements.SEL_POWER_DISPLAY);
            PowerTypeStorage = DBReader.Read<PowerTypeRecord>("PowerType.db2", HotfixStatements.SEL_POWER_TYPE);
            PrestigeLevelInfoStorage = DBReader.Read<PrestigeLevelInfoRecord>("PrestigeLevelInfo.db2", HotfixStatements.SEL_PRESTIGE_LEVEL_INFO, HotfixStatements.SEL_PRESTIGE_LEVEL_INFO_LOCALE);
            PvpDifficultyStorage = DBReader.Read<PvpDifficultyRecord>("PVPDifficulty.db2", HotfixStatements.SEL_PVP_DIFFICULTY);
            PvpItemStorage = DBReader.Read<PvpItemRecord>("PVPItem.db2", HotfixStatements.SEL_PVP_ITEM);
            PvpTalentStorage = DBReader.Read<PvpTalentRecord>("PvpTalent.db2", HotfixStatements.SEL_PVP_TALENT, HotfixStatements.SEL_PVP_TALENT_LOCALE);
            PvpTalentCategoryStorage = DBReader.Read<PvpTalentCategoryRecord>("PvpTalentCategory.db2", HotfixStatements.SEL_PVP_TALENT_CATEGORY);
            PvpTalentSlotUnlockStorage = DBReader.Read<PvpTalentSlotUnlockRecord>("PvpTalentSlotUnlock.db2", HotfixStatements.SEL_PVP_TALENT_SLOT_UNLOCK);
            QuestFactionRewardStorage = DBReader.Read<QuestFactionRewardRecord>("QuestFactionReward.db2", HotfixStatements.SEL_QUEST_FACTION_REWARD);
            QuestMoneyRewardStorage = DBReader.Read<QuestMoneyRewardRecord>("QuestMoneyReward.db2", HotfixStatements.SEL_QUEST_MONEY_REWARD);
            QuestPackageItemStorage = DBReader.Read<QuestPackageItemRecord>("QuestPackageItem.db2", HotfixStatements.SEL_QUEST_PACKAGE_ITEM);
            QuestSortStorage = DBReader.Read<QuestSortRecord>("QuestSort.db2", HotfixStatements.SEL_QUEST_SORT, HotfixStatements.SEL_QUEST_SORT_LOCALE);
            QuestV2Storage = DBReader.Read<QuestV2Record>("QuestV2.db2", HotfixStatements.SEL_QUEST_V2);
            QuestXPStorage = DBReader.Read<QuestXPRecord>("QuestXP.db2", HotfixStatements.SEL_QUEST_XP);
            RandPropPointsStorage = DBReader.Read<RandPropPointsRecord>("RandPropPoints.db2", HotfixStatements.SEL_RAND_PROP_POINTS);
            RewardPackStorage = DBReader.Read<RewardPackRecord>("RewardPack.db2", HotfixStatements.SEL_REWARD_PACK);
            RewardPackXCurrencyTypeStorage = DBReader.Read<RewardPackXCurrencyTypeRecord>("RewardPackXCurrencyType.db2", HotfixStatements.SEL_REWARD_PACK_X_CURRENCY_TYPE);
            RewardPackXItemStorage = DBReader.Read<RewardPackXItemRecord>("RewardPackXItem.db2", HotfixStatements.SEL_REWARD_PACK_X_ITEM);
            RulesetItemUpgradeStorage = DBReader.Read<RulesetItemUpgradeRecord>("RulesetItemUpgrade.db2", HotfixStatements.SEL_RULESET_ITEM_UPGRADE);
            ScalingStatDistributionStorage = DBReader.Read<ScalingStatDistributionRecord>("ScalingStatDistribution.db2", HotfixStatements.SEL_SCALING_STAT_DISTRIBUTION);
            ScenarioStorage = DBReader.Read<ScenarioRecord>("Scenario.db2", HotfixStatements.SEL_SCENARIO, HotfixStatements.SEL_SCENARIO_LOCALE);
            ScenarioStepStorage = DBReader.Read<ScenarioStepRecord>("ScenarioStep.db2", HotfixStatements.SEL_SCENARIO_STEP, HotfixStatements.SEL_SCENARIO_STEP_LOCALE);
            //SceneScriptStorage = DBReader.Read<SceneScriptRecord>("SceneScript.db2", HotfixStatements.SEL_SCENE_SCRIPT);
            SceneScriptGlobalTextStorage = DBReader.Read<SceneScriptGlobalTextRecord>("SceneScriptGlobalText.db2", HotfixStatements.SEL_SCENE_SCRIPT_GLOBAL_TEXT);
            SceneScriptPackageStorage = DBReader.Read<SceneScriptPackageRecord>("SceneScriptPackage.db2", HotfixStatements.SEL_SCENE_SCRIPT_PACKAGE);
            SceneScriptTextStorage = DBReader.Read<SceneScriptTextRecord>("SceneScriptText.db2", HotfixStatements.SEL_SCENE_SCRIPT_TEXT);
            SkillLineStorage = DBReader.Read<SkillLineRecord>("SkillLine.db2", HotfixStatements.SEL_SKILL_LINE, HotfixStatements.SEL_SKILL_LINE_LOCALE);
            SkillLineAbilityStorage = DBReader.Read<SkillLineAbilityRecord>("SkillLineAbility.db2", HotfixStatements.SEL_SKILL_LINE_ABILITY);
            SkillRaceClassInfoStorage = DBReader.Read<SkillRaceClassInfoRecord>("SkillRaceClassInfo.db2", HotfixStatements.SEL_SKILL_RACE_CLASS_INFO);
            SoundKitStorage = DBReader.Read<SoundKitRecord>("SoundKit.db2", HotfixStatements.SEL_SOUND_KIT);
            SpecializationSpellsStorage = DBReader.Read<SpecializationSpellsRecord>("SpecializationSpells.db2", HotfixStatements.SEL_SPECIALIZATION_SPELLS, HotfixStatements.SEL_SPECIALIZATION_SPELLS_LOCALE);
            SpellNameStorage = DBReader.Read<SpellNameRecord>("SpellName.db2", HotfixStatements.SEL_SPELL_NAME, HotfixStatements.SEL_SPELL_NAME_LOCALE);
            SpellAuraOptionsStorage = DBReader.Read<SpellAuraOptionsRecord>("SpellAuraOptions.db2", HotfixStatements.SEL_SPELL_AURA_OPTIONS);
            SpellAuraRestrictionsStorage = DBReader.Read<SpellAuraRestrictionsRecord>("SpellAuraRestrictions.db2", HotfixStatements.SEL_SPELL_AURA_RESTRICTIONS);
            SpellCastTimesStorage = DBReader.Read<SpellCastTimesRecord>("SpellCastTimes.db2", HotfixStatements.SEL_SPELL_CAST_TIMES);
            SpellCastingRequirementsStorage = DBReader.Read<SpellCastingRequirementsRecord>("SpellCastingRequirements.db2", HotfixStatements.SEL_SPELL_CASTING_REQUIREMENTS);
            SpellCategoriesStorage = DBReader.Read<SpellCategoriesRecord>("SpellCategories.db2", HotfixStatements.SEL_SPELL_CATEGORIES);
            SpellCategoryStorage = DBReader.Read<SpellCategoryRecord>("SpellCategory.db2", HotfixStatements.SEL_SPELL_CATEGORY, HotfixStatements.SEL_SPELL_CATEGORY_LOCALE);
            SpellClassOptionsStorage = DBReader.Read<SpellClassOptionsRecord>("SpellClassOptions.db2", HotfixStatements.SEL_SPELL_CLASS_OPTIONS);
            SpellCooldownsStorage = DBReader.Read<SpellCooldownsRecord>("SpellCooldowns.db2", HotfixStatements.SEL_SPELL_COOLDOWNS);
            SpellDurationStorage = DBReader.Read<SpellDurationRecord>("SpellDuration.db2", HotfixStatements.SEL_SPELL_DURATION);
            SpellEffectStorage = DBReader.Read<SpellEffectRecord>("SpellEffect.db2", HotfixStatements.SEL_SPELL_EFFECT);
            SpellEquippedItemsStorage = DBReader.Read<SpellEquippedItemsRecord>("SpellEquippedItems.db2", HotfixStatements.SEL_SPELL_EQUIPPED_ITEMS);
            SpellFocusObjectStorage = DBReader.Read<SpellFocusObjectRecord>("SpellFocusObject.db2", HotfixStatements.SEL_SPELL_FOCUS_OBJECT, HotfixStatements.SEL_SPELL_FOCUS_OBJECT_LOCALE);
            SpellInterruptsStorage = DBReader.Read<SpellInterruptsRecord>("SpellInterrupts.db2", HotfixStatements.SEL_SPELL_INTERRUPTS);
            SpellItemEnchantmentStorage = DBReader.Read<SpellItemEnchantmentRecord>("SpellItemEnchantment.db2", HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT, HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_LOCALE);
            SpellItemEnchantmentConditionStorage = DBReader.Read<SpellItemEnchantmentConditionRecord>("SpellItemEnchantmentCondition.db2", HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_CONDITION);
            SpellLearnSpellStorage = DBReader.Read<SpellLearnSpellRecord>("SpellLearnSpell.db2", HotfixStatements.SEL_SPELL_LEARN_SPELL);
            SpellLevelsStorage = DBReader.Read<SpellLevelsRecord>("SpellLevels.db2", HotfixStatements.SEL_SPELL_LEVELS);
            SpellMiscStorage = DBReader.Read<SpellMiscRecord>("SpellMisc.db2", HotfixStatements.SEL_SPELL_MISC);
            SpellPowerStorage = DBReader.Read<SpellPowerRecord>("SpellPower.db2", HotfixStatements.SEL_SPELL_POWER);
            SpellPowerDifficultyStorage = DBReader.Read<SpellPowerDifficultyRecord>("SpellPowerDifficulty.db2", HotfixStatements.SEL_SPELL_POWER_DIFFICULTY);
            SpellProcsPerMinuteStorage = DBReader.Read<SpellProcsPerMinuteRecord>("SpellProcsPerMinute.db2", HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE);
            SpellProcsPerMinuteModStorage = DBReader.Read<SpellProcsPerMinuteModRecord>("SpellProcsPerMinuteMod.db2", HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE_MOD);
            SpellRadiusStorage = DBReader.Read<SpellRadiusRecord>("SpellRadius.db2", HotfixStatements.SEL_SPELL_RADIUS);
            SpellRangeStorage = DBReader.Read<SpellRangeRecord>("SpellRange.db2", HotfixStatements.SEL_SPELL_RANGE, HotfixStatements.SEL_SPELL_RANGE_LOCALE);
            SpellReagentsStorage = DBReader.Read<SpellReagentsRecord>("SpellReagents.db2", HotfixStatements.SEL_SPELL_REAGENTS);
            SpellScalingStorage = DBReader.Read<SpellScalingRecord>("SpellScaling.db2", HotfixStatements.SEL_SPELL_SCALING);
            SpellShapeshiftStorage = DBReader.Read<SpellShapeshiftRecord>("SpellShapeshift.db2", HotfixStatements.SEL_SPELL_SHAPESHIFT);
            SpellShapeshiftFormStorage = DBReader.Read<SpellShapeshiftFormRecord>("SpellShapeshiftForm.db2", HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM, HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM_LOCALE);
            SpellTargetRestrictionsStorage = DBReader.Read<SpellTargetRestrictionsRecord>("SpellTargetRestrictions.db2", HotfixStatements.SEL_SPELL_TARGET_RESTRICTIONS);
            SpellTotemsStorage = DBReader.Read<SpellTotemsRecord>("SpellTotems.db2", HotfixStatements.SEL_SPELL_TOTEMS);
            SpellXSpellVisualStorage = DBReader.Read<SpellXSpellVisualRecord>("SpellXSpellVisual.db2", HotfixStatements.SEL_SPELL_X_SPELL_VISUAL);
            SummonPropertiesStorage = DBReader.Read<SummonPropertiesRecord>("SummonProperties.db2", HotfixStatements.SEL_SUMMON_PROPERTIES);
            //TactKeyStorage = DBReader.Read<TactKeyRecord>("TactKey.db2", HotfixStatements.SEL_TACT_KEY);
            TalentStorage = DBReader.Read<TalentRecord>("Talent.db2", HotfixStatements.SEL_TALENT, HotfixStatements.SEL_TALENT_LOCALE);
            TaxiNodesStorage = DBReader.Read<TaxiNodesRecord>("TaxiNodes.db2", HotfixStatements.SEL_TAXI_NODES, HotfixStatements.SEL_TAXI_NODES_LOCALE);
            TaxiPathStorage = DBReader.Read<TaxiPathRecord>("TaxiPath.db2", HotfixStatements.SEL_TAXI_PATH);
            TaxiPathNodeStorage = DBReader.Read<TaxiPathNodeRecord>("TaxiPathNode.db2", HotfixStatements.SEL_TAXI_PATH_NODE);
            TotemCategoryStorage = DBReader.Read<TotemCategoryRecord>("TotemCategory.db2", HotfixStatements.SEL_TOTEM_CATEGORY, HotfixStatements.SEL_TOTEM_CATEGORY_LOCALE);
            ToyStorage = DBReader.Read<ToyRecord>("Toy.db2", HotfixStatements.SEL_TOY, HotfixStatements.SEL_TOY_LOCALE);
            TransmogHolidayStorage = DBReader.Read<TransmogHolidayRecord>("TransmogHoliday.db2", HotfixStatements.SEL_TRANSMOG_HOLIDAY);
            TransmogSetStorage = DBReader.Read<TransmogSetRecord>("TransmogSet.db2", HotfixStatements.SEL_TRANSMOG_SET, HotfixStatements.SEL_TRANSMOG_SET_LOCALE);
            TransmogSetGroupStorage = DBReader.Read<TransmogSetGroupRecord>("TransmogSetGroup.db2", HotfixStatements.SEL_TRANSMOG_SET_GROUP, HotfixStatements.SEL_TRANSMOG_SET_GROUP_LOCALE);
            TransmogSetItemStorage = DBReader.Read<TransmogSetItemRecord>("TransmogSetItem.db2", HotfixStatements.SEL_TRANSMOG_SET_ITEM);
            TransportAnimationStorage = DBReader.Read<TransportAnimationRecord>("TransportAnimation.db2", HotfixStatements.SEL_TRANSPORT_ANIMATION);
            TransportRotationStorage = DBReader.Read<TransportRotationRecord>("TransportRotation.db2", HotfixStatements.SEL_TRANSPORT_ROTATION);
            UiMapStorage = DBReader.Read<UiMapRecord>("UiMap.db2", HotfixStatements.SEL_UI_MAP, HotfixStatements.SEL_UI_MAP_LOCALE);
            UiMapAssignmentStorage = DBReader.Read<UiMapAssignmentRecord>("UiMapAssignment.db2", HotfixStatements.SEL_UI_MAP_ASSIGNMENT);
            UiMapLinkStorage = DBReader.Read<UiMapLinkRecord>("UiMapLink.db2", HotfixStatements.SEL_UI_MAP_LINK);
            UiMapXMapArtStorage = DBReader.Read<UiMapXMapArtRecord>("UiMapXMapArt.db2", HotfixStatements.SEL_UI_MAP_X_MAP_ART);
            UnitPowerBarStorage = DBReader.Read<UnitPowerBarRecord>("UnitPowerBar.db2", HotfixStatements.SEL_UNIT_POWER_BAR, HotfixStatements.SEL_UNIT_POWER_BAR_LOCALE);
            VehicleStorage = DBReader.Read<VehicleRecord>("Vehicle.db2", HotfixStatements.SEL_VEHICLE);
            VehicleSeatStorage = DBReader.Read<VehicleSeatRecord>("VehicleSeat.db2", HotfixStatements.SEL_VEHICLE_SEAT);
            WMOAreaTableStorage = DBReader.Read<WMOAreaTableRecord>("WMOAreaTable.db2", HotfixStatements.SEL_WMO_AREA_TABLE, HotfixStatements.SEL_WMO_AREA_TABLE_LOCALE);
            WorldEffectStorage = DBReader.Read<WorldEffectRecord>("WorldEffect.db2", HotfixStatements.SEL_WORLD_EFFECT);
            WorldMapOverlayStorage = DBReader.Read<WorldMapOverlayRecord>("WorldMapOverlay.db2", HotfixStatements.SEL_WORLD_MAP_OVERLAY);
            WorldSafeLocsStorage = DBReader.Read<WorldSafeLocsRecord>("WorldSafeLocs.db2", HotfixStatements.SEL_WORLD_SAFE_LOCS, HotfixStatements.SEL_WORLD_SAFE_LOCS_LOCALE);

            Global.DB2Mgr.LoadStores();

            foreach (var entry in TaxiPathStorage.Values)
            {
                if (!TaxiPathSetBySource.ContainsKey(entry.FromTaxiNode))
                    TaxiPathSetBySource.Add(entry.FromTaxiNode, new Dictionary<uint, TaxiPathBySourceAndDestination>());
                TaxiPathSetBySource[entry.FromTaxiNode][entry.ToTaxiNode] = new TaxiPathBySourceAndDestination(entry.Id, entry.Cost);
            }

            uint pathCount = TaxiPathStorage.Keys.Max() + 1;

            // Calculate path nodes count
            uint[] pathLength = new uint[pathCount];                           // 0 and some other indexes not used
            foreach (TaxiPathNodeRecord entry in TaxiPathNodeStorage.Values)
                if (pathLength[entry.PathID] < entry.NodeIndex + 1)
                    pathLength[entry.PathID] = entry.NodeIndex + 1u;

            // Set path length
            for (uint i = 0; i < pathCount; ++i)
                TaxiPathNodesByPath[i] = new TaxiPathNodeRecord[pathLength[i]];

            // fill data
            foreach (var entry in TaxiPathNodeStorage.Values)
                TaxiPathNodesByPath[entry.PathID][entry.NodeIndex] = entry;

            foreach (var node in TaxiNodesStorage.Values)
            {
                if (!node.Flags.HasAnyFlag(TaxiNodeFlags.Alliance | TaxiNodeFlags.Horde))
                    continue;

                // valid taxi network node
                uint field = (node.Id - 1) / 8;
                byte submask = (byte)(1 << (int)((node.Id - 1) % 8));

                TaxiNodesMask[field] |= submask;
                if (node.Flags.HasAnyFlag(TaxiNodeFlags.Horde))
                    HordeTaxiNodesMask[field] |= submask;
                if (node.Flags.HasAnyFlag(TaxiNodeFlags.Alliance))
                    AllianceTaxiNodesMask[field] |= submask;

                int uiMapId;
                if (!Global.DB2Mgr.GetUiMapPosition(node.Pos.X, node.Pos.Y, node.Pos.Z, node.ContinentID, 0, 0, 0, UiMapSystem.Adventure, false, out uiMapId))
                    Global.DB2Mgr.GetUiMapPosition(node.Pos.X, node.Pos.Y, node.Pos.Z, node.ContinentID, 0, 0, 0, UiMapSystem.Taxi, false, out uiMapId);

                if (uiMapId == 985 || uiMapId == 986)
                    OldContinentsNodesMask[field] |= submask;
            }

            // Check loaded DB2 files proper version
            if (!AreaTableStorage.ContainsKey(10048) ||                // last area added in 8.0.1 (28153)
                !CharTitlesStorage.ContainsKey(633) ||                // last char title added in 8.0.1 (28153)
                !GemPropertiesStorage.ContainsKey(3745) ||            // last gem property added in 8.0.1 (28153)
                !ItemStorage.ContainsKey(164760) ||                   // last item added in 8.0.1 (28153)
                !ItemExtendedCostStorage.ContainsKey(6448) ||         // last item extended cost added in 8.0.1 (28153)
                !MapStorage.ContainsKey(2103) ||                      // last map added in 8.0.1 (28153)
                !SpellNameStorage.ContainsKey(281872))                // last spell added in 8.0.1 (28153)
            {
                Log.outError(LogFilter.Misc, "You have _outdated_ DB2 files. Please extract correct versions from current using client.");
                Global.WorldMgr.ShutdownServ(10, ShutdownMask.Force, ShutdownExitCode.Error);
            }

            Log.outInfo(LogFilter.ServerLoading, "Initialized {0} DB2 data storages in {1} ms", LoadedFileCount, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public static void LoadGameTables(string dataPath)
        {
            uint oldMSTime = Time.GetMSTime();
            LoadedFileCount = 0;

            DataPath = dataPath + "/gt/";

            ArmorMitigationByLvlGameTable = GameTableReader.Read<GtArmorMitigationByLvlRecord>("ArmorMitigationByLvl.txt");
            ArtifactKnowledgeMultiplierGameTable = GameTableReader.Read<GtArtifactKnowledgeMultiplierRecord>("ArtifactKnowledgeMultiplier.txt");
            ArtifactLevelXPGameTable = GameTableReader.Read<GtArtifactLevelXPRecord>("artifactLevelXP.txt");
            BarberShopCostBaseGameTable = GameTableReader.Read<GtBarberShopCostBaseRecord>("BarberShopCostBase.txt");
            BaseMPGameTable = GameTableReader.Read<GtBaseMPRecord>("BaseMp.txt");
            CombatRatingsGameTable = GameTableReader.Read<GtCombatRatingsRecord>("CombatRatings.txt");
            CombatRatingsMultByILvlGameTable = GameTableReader.Read<GtCombatRatingsMultByILvlRecord>("CombatRatingsMultByILvl.txt");
            ItemSocketCostPerLevelGameTable = GameTableReader.Read<GtItemSocketCostPerLevelRecord>("ItemSocketCostPerLevel.txt");
            HpPerStaGameTable = GameTableReader.Read<GtHpPerStaRecord>("HpPerSta.txt");
            NpcDamageByClassGameTable[0] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClass.txt");
            NpcDamageByClassGameTable[1] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClassExp1.txt");
            NpcDamageByClassGameTable[2] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClassExp2.txt");
            NpcDamageByClassGameTable[3] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClassExp3.txt");
            NpcDamageByClassGameTable[4] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClassExp4.txt");
            NpcDamageByClassGameTable[5] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClassExp5.txt");
            NpcDamageByClassGameTable[6] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClassExp6.txt");
            NpcDamageByClassGameTable[7] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClassExp7.txt");
            NpcManaCostScalerGameTable = GameTableReader.Read<GtNpcManaCostScalerRecord>("NPCManaCostScaler.txt");
            NpcTotalHpGameTable[0] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHp.txt");
            NpcTotalHpGameTable[1] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHpExp1.txt");
            NpcTotalHpGameTable[2] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHpExp2.txt");
            NpcTotalHpGameTable[3] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHpExp3.txt");
            NpcTotalHpGameTable[4] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHpExp4.txt");
            NpcTotalHpGameTable[5] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHpExp5.txt");
            NpcTotalHpGameTable[6] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHpExp6.txt");
            NpcTotalHpGameTable[7] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHpExp7.txt");
            SpellScalingGameTable = GameTableReader.Read<GtSpellScalingRecord>("SpellScaling.txt");
            XpGameTable = GameTableReader.Read<GtXpRecord>("xp.txt");

            Log.outInfo(LogFilter.ServerLoading, "Initialized {0} DBC GameTables data stores in {1} ms", LoadedFileCount, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        #region Main Collections
        public static DB6Storage<AchievementRecord> AchievementStorage;
        public static DB6Storage<AnimationDataRecord> AnimationDataStorage;
        public static DB6Storage<AnimKitRecord> AnimKitStorage;
        public static DB6Storage<AreaGroupMemberRecord> AreaGroupMemberStorage;
        public static DB6Storage<AreaTableRecord> AreaTableStorage;
        public static DB6Storage<AreaTriggerRecord> AreaTriggerStorage;
        public static DB6Storage<ArmorLocationRecord> ArmorLocationStorage;
        public static DB6Storage<ArtifactRecord> ArtifactStorage;
        public static DB6Storage<ArtifactAppearanceRecord> ArtifactAppearanceStorage;
        public static DB6Storage<ArtifactAppearanceSetRecord> ArtifactAppearanceSetStorage;
        public static DB6Storage<ArtifactCategoryRecord> ArtifactCategoryStorage;
        public static DB6Storage<ArtifactPowerRecord> ArtifactPowerStorage;
        public static DB6Storage<ArtifactPowerLinkRecord> ArtifactPowerLinkStorage;
        public static DB6Storage<ArtifactPowerPickerRecord> ArtifactPowerPickerStorage;
        public static DB6Storage<ArtifactPowerRankRecord> ArtifactPowerRankStorage;
        //public static DB6Storage<ArtifactQuestXPRecord> ArtifactQuestXPStorage;
        public static DB6Storage<ArtifactTierRecord> ArtifactTierStorage;
        public static DB6Storage<ArtifactUnlockRecord> ArtifactUnlockStorage;
        public static DB6Storage<AuctionHouseRecord> AuctionHouseStorage;
        public static DB6Storage<BankBagSlotPricesRecord> BankBagSlotPricesStorage;
        //public static DB6Storage<BannedAddOnsRecord> BannedAddOnsStorage;
        public static DB6Storage<BarberShopStyleRecord> BarberShopStyleStorage;
        public static DB6Storage<BattlePetBreedQualityRecord> BattlePetBreedQualityStorage;
        public static DB6Storage<BattlePetBreedStateRecord> BattlePetBreedStateStorage;
        public static DB6Storage<BattlePetSpeciesRecord> BattlePetSpeciesStorage;
        public static DB6Storage<BattlePetSpeciesStateRecord> BattlePetSpeciesStateStorage;
        public static DB6Storage<BattlemasterListRecord> BattlemasterListStorage;
        public static DB6Storage<BroadcastTextRecord> BroadcastTextStorage;
        public static DB6Storage<CharacterFacialHairStylesRecord> CharacterFacialHairStylesStorage;
        public static DB6Storage<CharBaseSectionRecord> CharBaseSectionStorage;
        public static DB6Storage<CharSectionsRecord> CharSectionsStorage;
        public static DB6Storage<CharStartOutfitRecord> CharStartOutfitStorage;
        public static DB6Storage<CharTitlesRecord> CharTitlesStorage;
        public static DB6Storage<ChatChannelsRecord> ChatChannelsStorage;
        public static DB6Storage<ChrClassesRecord> ChrClassesStorage;
        public static DB6Storage<ChrClassesXPowerTypesRecord> ChrClassesXPowerTypesStorage;
        public static DB6Storage<ChrRacesRecord> ChrRacesStorage;
        public static DB6Storage<ChrSpecializationRecord> ChrSpecializationStorage;
        public static DB6Storage<CinematicCameraRecord> CinematicCameraStorage;
        public static DB6Storage<CinematicSequencesRecord> CinematicSequencesStorage;
        public static DB6Storage<ContentTuningRecord> ContentTuningStorage;
        public static DB6Storage<ConversationLineRecord> ConversationLineStorage;
        public static DB6Storage<CreatureDisplayInfoRecord> CreatureDisplayInfoStorage;
        public static DB6Storage<CreatureDisplayInfoExtraRecord> CreatureDisplayInfoExtraStorage;
        public static DB6Storage<CreatureFamilyRecord> CreatureFamilyStorage;
        public static DB6Storage<CreatureModelDataRecord> CreatureModelDataStorage;
        public static DB6Storage<CreatureTypeRecord> CreatureTypeStorage;
        public static DB6Storage<CriteriaRecord> CriteriaStorage;
        public static DB6Storage<CriteriaTreeRecord> CriteriaTreeStorage;
        public static DB6Storage<CurrencyTypesRecord> CurrencyTypesStorage;
        public static DB6Storage<CurveRecord> CurveStorage;
        public static DB6Storage<CurvePointRecord> CurvePointStorage;
        public static DB6Storage<DestructibleModelDataRecord> DestructibleModelDataStorage;
        public static DB6Storage<DifficultyRecord> DifficultyStorage;
        public static DB6Storage<DungeonEncounterRecord> DungeonEncounterStorage;
        public static DB6Storage<DurabilityCostsRecord> DurabilityCostsStorage;
        public static DB6Storage<DurabilityQualityRecord> DurabilityQualityStorage;
        public static DB6Storage<EmotesRecord> EmotesStorage;
        public static DB6Storage<EmotesTextRecord> EmotesTextStorage;
        public static DB6Storage<EmotesTextSoundRecord> EmotesTextSoundStorage;
        public static DB6Storage<ExpectedStatRecord> ExpectedStatStorage;
        public static DB6Storage<ExpectedStatModRecord> ExpectedStatModStorage;
        public static DB6Storage<FactionRecord> FactionStorage;
        public static DB6Storage<FactionTemplateRecord> FactionTemplateStorage;
        public static DB6Storage<GameObjectsRecord> GameObjectsStorage;
        public static DB6Storage<GameObjectDisplayInfoRecord> GameObjectDisplayInfoStorage;
        public static DB6Storage<GarrAbilityRecord> GarrAbilityStorage;
        public static DB6Storage<GarrBuildingRecord> GarrBuildingStorage;
        public static DB6Storage<GarrBuildingPlotInstRecord> GarrBuildingPlotInstStorage;
        public static DB6Storage<GarrClassSpecRecord> GarrClassSpecStorage;
        public static DB6Storage<GarrFollowerRecord> GarrFollowerStorage;
        public static DB6Storage<GarrFollowerXAbilityRecord> GarrFollowerXAbilityStorage;
        public static DB6Storage<GarrPlotBuildingRecord> GarrPlotBuildingStorage;
        public static DB6Storage<GarrPlotRecord> GarrPlotStorage;
        public static DB6Storage<GarrPlotInstanceRecord> GarrPlotInstanceStorage;
        public static DB6Storage<GarrSiteLevelRecord> GarrSiteLevelStorage;
        public static DB6Storage<GarrSiteLevelPlotInstRecord> GarrSiteLevelPlotInstStorage;
        public static DB6Storage<GemPropertiesRecord> GemPropertiesStorage;
        public static DB6Storage<GlyphBindableSpellRecord> GlyphBindableSpellStorage;
        public static DB6Storage<GlyphPropertiesRecord> GlyphPropertiesStorage;
        public static DB6Storage<GlyphRequiredSpecRecord> GlyphRequiredSpecStorage;
        public static DB6Storage<GuildColorBackgroundRecord> GuildColorBackgroundStorage;
        public static DB6Storage<GuildColorBorderRecord> GuildColorBorderStorage;
        public static DB6Storage<GuildColorEmblemRecord> GuildColorEmblemStorage;
        public static DB6Storage<GuildPerkSpellsRecord> GuildPerkSpellsStorage;
        public static DB6Storage<HeirloomRecord> HeirloomStorage;
        public static DB6Storage<HolidaysRecord> HolidaysStorage;
        public static DB6Storage<ImportPriceArmorRecord> ImportPriceArmorStorage;
        public static DB6Storage<ImportPriceQualityRecord> ImportPriceQualityStorage;
        public static DB6Storage<ImportPriceShieldRecord> ImportPriceShieldStorage;
        public static DB6Storage<ImportPriceWeaponRecord> ImportPriceWeaponStorage;
        public static DB6Storage<ItemAppearanceRecord> ItemAppearanceStorage;
        public static DB6Storage<ItemArmorQualityRecord> ItemArmorQualityStorage;
        public static DB6Storage<ItemArmorShieldRecord> ItemArmorShieldStorage;
        public static DB6Storage<ItemArmorTotalRecord> ItemArmorTotalStorage;
        //public static DB6Storage<ItemBagFamilyRecord> ItemBagFamilyStorage;
        public static DB6Storage<ItemBonusRecord> ItemBonusStorage;
        public static DB6Storage<ItemBonusListLevelDeltaRecord> ItemBonusListLevelDeltaStorage;
        public static DB6Storage<ItemBonusTreeNodeRecord> ItemBonusTreeNodeStorage;
        public static DB6Storage<ItemClassRecord> ItemClassStorage;
        public static DB6Storage<ItemChildEquipmentRecord> ItemChildEquipmentStorage;
        public static DB6Storage<ItemCurrencyCostRecord> ItemCurrencyCostStorage;
        public static DB6Storage<ItemDamageRecord> ItemDamageAmmoStorage;
        public static DB6Storage<ItemDamageRecord> ItemDamageOneHandStorage;
        public static DB6Storage<ItemDamageRecord> ItemDamageOneHandCasterStorage;
        public static DB6Storage<ItemDamageRecord> ItemDamageTwoHandStorage;
        public static DB6Storage<ItemDamageRecord> ItemDamageTwoHandCasterStorage;
        public static DB6Storage<ItemDisenchantLootRecord> ItemDisenchantLootStorage;
        public static DB6Storage<ItemEffectRecord> ItemEffectStorage;
        public static DB6Storage<ItemRecord> ItemStorage;
        public static DB6Storage<ItemExtendedCostRecord> ItemExtendedCostStorage;
        public static DB6Storage<ItemLevelSelectorRecord> ItemLevelSelectorStorage;
        public static DB6Storage<ItemLevelSelectorQualityRecord> ItemLevelSelectorQualityStorage;
        public static DB6Storage<ItemLevelSelectorQualitySetRecord> ItemLevelSelectorQualitySetStorage;
        public static DB6Storage<ItemLimitCategoryRecord> ItemLimitCategoryStorage;
        public static DB6Storage<ItemLimitCategoryConditionRecord> ItemLimitCategoryConditionStorage;
        public static DB6Storage<ItemModifiedAppearanceRecord> ItemModifiedAppearanceStorage;
        public static DB6Storage<ItemPriceBaseRecord> ItemPriceBaseStorage;
        public static DB6Storage<ItemRandomPropertiesRecord> ItemRandomPropertiesStorage;
        public static DB6Storage<ItemRandomSuffixRecord> ItemRandomSuffixStorage;
        public static DB6Storage<ItemSearchNameRecord> ItemSearchNameStorage;
        public static DB6Storage<ItemSetRecord> ItemSetStorage;
        public static DB6Storage<ItemSetSpellRecord> ItemSetSpellStorage;
        public static DB6Storage<ItemSparseRecord> ItemSparseStorage;
        public static DB6Storage<ItemSpecRecord> ItemSpecStorage;
        public static DB6Storage<ItemSpecOverrideRecord> ItemSpecOverrideStorage;
        public static DB6Storage<ItemUpgradeRecord> ItemUpgradeStorage;
        public static DB6Storage<ItemXBonusTreeRecord> ItemXBonusTreeStorage;
        //public static DB6Storage<KeyChainRecord> KeyChainStorage;
        public static DB6Storage<LFGDungeonsRecord> LFGDungeonsStorage;
        public static DB6Storage<LightRecord> LightStorage;
        public static DB6Storage<LiquidTypeRecord> LiquidTypeStorage;
        public static DB6Storage<LockRecord> LockStorage;
        public static DB6Storage<MailTemplateRecord> MailTemplateStorage;
        public static DB6Storage<MapRecord> MapStorage;
        public static DB6Storage<MapDifficultyRecord> MapDifficultyStorage;
        public static DB6Storage<ModifierTreeRecord> ModifierTreeStorage;
        public static DB6Storage<MountCapabilityRecord> MountCapabilityStorage;
        public static DB6Storage<MountRecord> MountStorage;
        public static DB6Storage<MountTypeXCapabilityRecord> MountTypeXCapabilityStorage;
        public static DB6Storage<MountXDisplayRecord> MountXDisplayStorage;
        public static DB6Storage<MovieRecord> MovieStorage;
        public static DB6Storage<NameGenRecord> NameGenStorage;
        public static DB6Storage<NamesProfanityRecord> NamesProfanityStorage;
        public static DB6Storage<NamesReservedRecord> NamesReservedStorage;
        public static DB6Storage<NamesReservedLocaleRecord> NamesReservedLocaleStorage;
        public static DB6Storage<NumTalentsAtLevelRecord> NumTalentsAtLevelStorage;
        public static DB6Storage<OverrideSpellDataRecord> OverrideSpellDataStorage;
        public static DB6Storage<PhaseRecord> PhaseStorage;
        public static DB6Storage<PhaseXPhaseGroupRecord> PhaseXPhaseGroupStorage;
        public static DB6Storage<PlayerConditionRecord> PlayerConditionStorage;
        public static DB6Storage<PowerDisplayRecord> PowerDisplayStorage;
        public static DB6Storage<PowerTypeRecord> PowerTypeStorage;
        public static DB6Storage<PrestigeLevelInfoRecord> PrestigeLevelInfoStorage;
        public static DB6Storage<PvpDifficultyRecord> PvpDifficultyStorage;
        public static DB6Storage<PvpItemRecord> PvpItemStorage;
        public static DB6Storage<PvpTalentRecord> PvpTalentStorage;
        public static DB6Storage<PvpTalentCategoryRecord> PvpTalentCategoryStorage;
        public static DB6Storage<PvpTalentSlotUnlockRecord> PvpTalentSlotUnlockStorage;
        public static DB6Storage<QuestFactionRewardRecord> QuestFactionRewardStorage;
        public static DB6Storage<QuestMoneyRewardRecord> QuestMoneyRewardStorage;
        public static DB6Storage<QuestPackageItemRecord> QuestPackageItemStorage;
        public static DB6Storage<QuestSortRecord> QuestSortStorage;
        public static DB6Storage<QuestV2Record> QuestV2Storage;
        public static DB6Storage<QuestXPRecord> QuestXPStorage;
        public static DB6Storage<RandPropPointsRecord> RandPropPointsStorage;
        public static DB6Storage<RewardPackRecord> RewardPackStorage;
        public static DB6Storage<RewardPackXCurrencyTypeRecord> RewardPackXCurrencyTypeStorage;
        public static DB6Storage<RewardPackXItemRecord> RewardPackXItemStorage;
        public static DB6Storage<RulesetItemUpgradeRecord> RulesetItemUpgradeStorage;
        public static DB6Storage<ScalingStatDistributionRecord> ScalingStatDistributionStorage;
        public static DB6Storage<ScenarioRecord> ScenarioStorage;
        public static DB6Storage<ScenarioStepRecord> ScenarioStepStorage;
        //public static DB6Storage<SceneScriptRecord> SceneScriptStorage;
        public static DB6Storage<SceneScriptGlobalTextRecord> SceneScriptGlobalTextStorage;
        public static DB6Storage<SceneScriptPackageRecord> SceneScriptPackageStorage;
        public static DB6Storage<SceneScriptTextRecord> SceneScriptTextStorage;
        public static DB6Storage<SkillLineRecord> SkillLineStorage;
        public static DB6Storage<SkillLineAbilityRecord> SkillLineAbilityStorage;
        public static DB6Storage<SkillRaceClassInfoRecord> SkillRaceClassInfoStorage;
        public static DB6Storage<SoundKitRecord> SoundKitStorage;
        public static DB6Storage<SpecializationSpellsRecord> SpecializationSpellsStorage;
        public static DB6Storage<SpellAuraOptionsRecord> SpellAuraOptionsStorage;
        public static DB6Storage<SpellAuraRestrictionsRecord> SpellAuraRestrictionsStorage;
        public static DB6Storage<SpellCastTimesRecord> SpellCastTimesStorage;
        public static DB6Storage<SpellCastingRequirementsRecord> SpellCastingRequirementsStorage;
        public static DB6Storage<SpellCategoriesRecord> SpellCategoriesStorage;
        public static DB6Storage<SpellCategoryRecord> SpellCategoryStorage;
        public static DB6Storage<SpellClassOptionsRecord> SpellClassOptionsStorage;
        public static DB6Storage<SpellCooldownsRecord> SpellCooldownsStorage;
        public static DB6Storage<SpellDurationRecord> SpellDurationStorage;
        public static DB6Storage<SpellEffectRecord> SpellEffectStorage;
        public static DB6Storage<SpellEquippedItemsRecord> SpellEquippedItemsStorage;
        public static DB6Storage<SpellFocusObjectRecord> SpellFocusObjectStorage;
        public static DB6Storage<SpellInterruptsRecord> SpellInterruptsStorage;
        public static DB6Storage<SpellItemEnchantmentRecord> SpellItemEnchantmentStorage;
        public static DB6Storage<SpellItemEnchantmentConditionRecord> SpellItemEnchantmentConditionStorage;
        public static DB6Storage<SpellLearnSpellRecord> SpellLearnSpellStorage;
        public static DB6Storage<SpellLevelsRecord> SpellLevelsStorage;
        public static DB6Storage<SpellMiscRecord> SpellMiscStorage;
        public static DB6Storage<SpellNameRecord> SpellNameStorage;
        public static DB6Storage<SpellPowerRecord> SpellPowerStorage;
        public static DB6Storage<SpellPowerDifficultyRecord> SpellPowerDifficultyStorage;
        public static DB6Storage<SpellProcsPerMinuteRecord> SpellProcsPerMinuteStorage;
        public static DB6Storage<SpellProcsPerMinuteModRecord> SpellProcsPerMinuteModStorage;
        public static DB6Storage<SpellRadiusRecord> SpellRadiusStorage;
        public static DB6Storage<SpellRangeRecord> SpellRangeStorage;
        public static DB6Storage<SpellReagentsRecord> SpellReagentsStorage;
        public static DB6Storage<SpellScalingRecord> SpellScalingStorage;
        public static DB6Storage<SpellShapeshiftRecord> SpellShapeshiftStorage;
        public static DB6Storage<SpellShapeshiftFormRecord> SpellShapeshiftFormStorage;
        public static DB6Storage<SpellTargetRestrictionsRecord> SpellTargetRestrictionsStorage;
        public static DB6Storage<SpellTotemsRecord> SpellTotemsStorage;
        public static DB6Storage<SpellXSpellVisualRecord> SpellXSpellVisualStorage;
        public static DB6Storage<SummonPropertiesRecord> SummonPropertiesStorage;
        //public static DB6Storage<TactKeyRecord> TactKeyStorage;
        public static DB6Storage<TalentRecord> TalentStorage;
        public static DB6Storage<TaxiNodesRecord> TaxiNodesStorage;
        public static DB6Storage<TaxiPathRecord> TaxiPathStorage;
        public static DB6Storage<TaxiPathNodeRecord> TaxiPathNodeStorage;
        public static DB6Storage<TotemCategoryRecord> TotemCategoryStorage;
        public static DB6Storage<ToyRecord> ToyStorage;
        public static DB6Storage<TransmogHolidayRecord> TransmogHolidayStorage;
        public static DB6Storage<TransmogSetRecord> TransmogSetStorage;
        public static DB6Storage<TransmogSetGroupRecord> TransmogSetGroupStorage;
        public static DB6Storage<TransmogSetItemRecord> TransmogSetItemStorage;
        public static DB6Storage<TransportAnimationRecord> TransportAnimationStorage;
        public static DB6Storage<TransportRotationRecord> TransportRotationStorage;
        public static DB6Storage<UiMapRecord> UiMapStorage;
        public static DB6Storage<UiMapAssignmentRecord> UiMapAssignmentStorage;
        public static DB6Storage<UiMapLinkRecord> UiMapLinkStorage;
        public static DB6Storage<UiMapXMapArtRecord> UiMapXMapArtStorage;
        public static DB6Storage<UnitPowerBarRecord> UnitPowerBarStorage;
        public static DB6Storage<VehicleRecord> VehicleStorage;
        public static DB6Storage<VehicleSeatRecord> VehicleSeatStorage;
        public static DB6Storage<WMOAreaTableRecord> WMOAreaTableStorage;
        public static DB6Storage<WorldEffectRecord> WorldEffectStorage;
        public static DB6Storage<WorldMapOverlayRecord> WorldMapOverlayStorage;
        public static DB6Storage<WorldSafeLocsRecord> WorldSafeLocsStorage;
        #endregion

        #region GameTables
        public static GameTable<GtArmorMitigationByLvlRecord> ArmorMitigationByLvlGameTable;
        public static GameTable<GtArtifactKnowledgeMultiplierRecord> ArtifactKnowledgeMultiplierGameTable;
        public static GameTable<GtArtifactLevelXPRecord> ArtifactLevelXPGameTable;
        public static GameTable<GtBarberShopCostBaseRecord> BarberShopCostBaseGameTable;
        public static GameTable<GtBaseMPRecord> BaseMPGameTable;
        public static GameTable<GtCombatRatingsRecord> CombatRatingsGameTable;
        public static GameTable<GtCombatRatingsMultByILvlRecord> CombatRatingsMultByILvlGameTable;
        public static GameTable<GtHpPerStaRecord> HpPerStaGameTable;
        public static GameTable<GtItemSocketCostPerLevelRecord> ItemSocketCostPerLevelGameTable;
        public static GameTable<GtNpcDamageByClassRecord>[] NpcDamageByClassGameTable = new GameTable<GtNpcDamageByClassRecord>[(int)Expansion.Max];
        public static GameTable<GtNpcManaCostScalerRecord> NpcManaCostScalerGameTable;
        public static GameTable<GtNpcTotalHpRecord>[] NpcTotalHpGameTable = new GameTable<GtNpcTotalHpRecord>[(int)Expansion.Max];
        public static GameTable<GtSpellScalingRecord> SpellScalingGameTable;
        public static GameTable<GtXpRecord> XpGameTable;
        #endregion

        #region Taxi Collections
        public static byte[] TaxiNodesMask = new byte[PlayerConst.TaxiMaskSize];
        public static byte[] OldContinentsNodesMask = new byte[PlayerConst.TaxiMaskSize];
        public static byte[] HordeTaxiNodesMask = new byte[PlayerConst.TaxiMaskSize];
        public static byte[] AllianceTaxiNodesMask = new byte[PlayerConst.TaxiMaskSize];
        public static Dictionary<uint, Dictionary<uint, TaxiPathBySourceAndDestination>> TaxiPathSetBySource = new Dictionary<uint, Dictionary<uint, TaxiPathBySourceAndDestination>>();
        public static Dictionary<uint, TaxiPathNodeRecord[]> TaxiPathNodesByPath = new Dictionary<uint, TaxiPathNodeRecord[]>();
        #endregion

        #region Helper Methods
        public static float GetGameTableColumnForClass(dynamic row, Class class_)
        {
            switch (class_)
            {
                case Class.Warrior:
                    return row.Warrior;
                case Class.Paladin:
                    return row.Paladin;
                case Class.Hunter:
                    return row.Hunter;
                case Class.Rogue:
                    return row.Rogue;
                case Class.Priest:
                    return row.Priest;
                case Class.Deathknight:
                    return row.DeathKnight;
                case Class.Shaman:
                    return row.Shaman;
                case Class.Mage:
                    return row.Mage;
                case Class.Warlock:
                    return row.Warlock;
                case Class.Monk:
                    return row.Monk;
                case Class.Druid:
                    return row.Druid;
                case Class.DemonHunter:
                    return row.DemonHunter;
                default:
                    break;
            }

            return 0.0f;
        }

        public static float GetSpellScalingColumnForClass(GtSpellScalingRecord row, int class_)
        {
            switch (class_)
            {
                case (int)Class.Warrior:
                    return row.Warrior;
                case (int)Class.Paladin:
                    return row.Paladin;
                case (int)Class.Hunter:
                    return row.Hunter;
                case (int)Class.Rogue:
                    return row.Rogue;
                case (int)Class.Priest:
                    return row.Priest;
                case (int)Class.Deathknight:
                    return row.DeathKnight;
                case (int)Class.Shaman:
                    return row.Shaman;
                case (int)Class.Mage:
                    return row.Mage;
                case (int)Class.Warlock:
                    return row.Warlock;
                case (int)Class.Monk:
                    return row.Monk;
                case (int)Class.Druid:
                    return row.Druid;
                case (int)Class.DemonHunter:
                    return row.DemonHunter;
                case -1:
                case -7:
                    return row.Item;
                case -2:
                    return row.Consumable;
                case -3:
                    return row.Gem1;
                case -4:
                    return row.Gem2;
                case -5:
                    return row.Gem3;
                case -6:
                    return row.Health;
                case -8:
                    return row.DamageReplaceStat;
                default:
                    break;
            }

            return 0.0f;
        }
        #endregion
    }
}
