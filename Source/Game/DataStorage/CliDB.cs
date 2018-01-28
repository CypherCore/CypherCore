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

            AchievementStorage = DB6Reader.Read<AchievementRecord>("Achievement.db2", DB6Metas.AchievementMeta, HotfixStatements.SEL_ACHIEVEMENT, HotfixStatements.SEL_ACHIEVEMENT_LOCALE);
            AnimKitStorage = DB6Reader.Read<AnimKitRecord>("AnimKit.db2", DB6Metas.AnimKitMeta, HotfixStatements.SEL_ANIM_KIT);
            AreaGroupMemberStorage = DB6Reader.Read<AreaGroupMemberRecord>("AreaGroupMember.db2", DB6Metas.AreaGroupMemberMeta, HotfixStatements.SEL_AREA_GROUP_MEMBER);
            AreaTableStorage = DB6Reader.Read<AreaTableRecord>("AreaTable.db2", DB6Metas.AreaTableMeta, HotfixStatements.SEL_AREA_TABLE, HotfixStatements.SEL_AREA_TABLE_LOCALE);
            AreaTriggerStorage = DB6Reader.Read<AreaTriggerRecord>("AreaTrigger.db2", DB6Metas.AreaTriggerMeta, HotfixStatements.SEL_AREA_TRIGGER);
            ArmorLocationStorage = DB6Reader.Read<ArmorLocationRecord>("ArmorLocation.db2", DB6Metas.ArmorLocationMeta, HotfixStatements.SEL_ARMOR_LOCATION);
            ArtifactStorage = DB6Reader.Read<ArtifactRecord>("Artifact.db2", DB6Metas.ArtifactMeta, HotfixStatements.SEL_ARTIFACT, HotfixStatements.SEL_ARTIFACT_APPEARANCE_LOCALE);
            ArtifactAppearanceStorage = DB6Reader.Read<ArtifactAppearanceRecord>("ArtifactAppearance.db2", DB6Metas.ArtifactAppearanceMeta, HotfixStatements.SEL_ARTIFACT_APPEARANCE, HotfixStatements.SEL_ARTIFACT_APPEARANCE_LOCALE);
            ArtifactAppearanceSetStorage = DB6Reader.Read<ArtifactAppearanceSetRecord>("ArtifactAppearanceSet.db2", DB6Metas.ArtifactAppearanceSetMeta, HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET, HotfixStatements.SEL_ARTIFACT_APPEARANCE_SET_LOCALE);
            ArtifactCategoryStorage = DB6Reader.Read<ArtifactCategoryRecord>("ArtifactCategory.db2", DB6Metas.ArtifactCategoryMeta, HotfixStatements.SEL_ARTIFACT_CATEGORY);
            ArtifactPowerStorage = DB6Reader.Read<ArtifactPowerRecord>("ArtifactPower.db2", DB6Metas.ArtifactPowerMeta, HotfixStatements.SEL_ARTIFACT_POWER);
            ArtifactPowerLinkStorage = DB6Reader.Read<ArtifactPowerLinkRecord>("ArtifactPowerLink.db2", DB6Metas.ArtifactPowerLinkMeta, HotfixStatements.SEL_ARTIFACT_POWER_LINK);
            ArtifactPowerPickerStorage = DB6Reader.Read<ArtifactPowerPickerRecord>("ArtifactPowerPicker.db2", DB6Metas.ArtifactPowerPickerMeta, HotfixStatements.SEL_ARTIFACT_POWER_PICKER);
            ArtifactPowerRankStorage = DB6Reader.Read<ArtifactPowerRankRecord>("ArtifactPowerRank.db2", DB6Metas.ArtifactPowerRankMeta, HotfixStatements.SEL_ARTIFACT_POWER_RANK);
            //ArtifactQuestXPStorage = DB6Reader.Read<ArtifactQuestXPRecord>("ArtifactQuestXP.db2", DB6Metas.ArtifactQuestXPMeta, HotfixStatements.SEL_ARTIFACT_QUEST_XP);
            AuctionHouseStorage = DB6Reader.Read<AuctionHouseRecord>("AuctionHouse.db2", DB6Metas.AuctionHouseMeta, HotfixStatements.SEL_AUCTION_HOUSE, HotfixStatements.SEL_AUCTION_HOUSE_LOCALE);
            BankBagSlotPricesStorage = DB6Reader.Read<BankBagSlotPricesRecord>("BankBagSlotPrices.db2", DB6Metas.BankBagSlotPricesMeta, HotfixStatements.SEL_BANK_BAG_SLOT_PRICES);
            //BannedAddOnsStorage = DB6Reader.Read<BannedAddOnsRecord>("BannedAddons.db2", DB6Metas.BannedAddOnsMeta, HotfixStatements.SEL_BANNED_ADDONS);
            BarberShopStyleStorage = DB6Reader.Read<BarberShopStyleRecord>("BarberShopStyle.db2", DB6Metas.BarberShopStyleMeta, HotfixStatements.SEL_BARBER_SHOP_STYLE, HotfixStatements.SEL_BARBER_SHOP_STYLE_LOCALE);
            BattlePetBreedQualityStorage = DB6Reader.Read<BattlePetBreedQualityRecord>("BattlePetBreedQuality.db2", DB6Metas.BattlePetBreedQualityMeta, HotfixStatements.SEL_BATTLE_PET_BREED_QUALITY);
            BattlePetBreedStateStorage = DB6Reader.Read<BattlePetBreedStateRecord>("BattlePetBreedState.db2", DB6Metas.BattlePetBreedStateMeta, HotfixStatements.SEL_BATTLE_PET_BREED_STATE);
            BattlePetSpeciesStorage = DB6Reader.Read<BattlePetSpeciesRecord>("BattlePetSpecies.db2", DB6Metas.BattlePetSpeciesMeta, HotfixStatements.SEL_BATTLE_PET_SPECIES, HotfixStatements.SEL_BATTLE_PET_SPECIES_LOCALE);
            BattlePetSpeciesStateStorage = DB6Reader.Read<BattlePetSpeciesStateRecord>("BattlePetSpeciesState.db2", DB6Metas.BattlePetSpeciesStateMeta, HotfixStatements.SEL_BATTLE_PET_SPECIES_STATE);
            BattlemasterListStorage = DB6Reader.Read<BattlemasterListRecord>("BattlemasterList.db2", DB6Metas.BattlemasterListMeta, HotfixStatements.SEL_BATTLEMASTER_LIST, HotfixStatements.SEL_BATTLEMASTER_LIST_LOCALE);
            BroadcastTextStorage = DB6Reader.Read<BroadcastTextRecord>("BroadcastText.db2", DB6Metas.BroadcastTextMeta, HotfixStatements.SEL_BROADCAST_TEXT, HotfixStatements.SEL_BROADCAST_TEXT_LOCALE);
            CharacterFacialHairStylesStorage = DB6Reader.Read<CharacterFacialHairStylesRecord>("CharacterFacialHairStyles.db2", DB6Metas.CharacterFacialHairStylesMeta, HotfixStatements.SEL_CHARACTER_FACIAL_HAIR_STYLES);
            CharBaseSectionStorage = DB6Reader.Read<CharBaseSectionRecord>("CharBaseSection.db2", DB6Metas.CharBaseSectionMeta, HotfixStatements.SEL_CHAR_BASE_SECTION);
            CharSectionsStorage = DB6Reader.Read<CharSectionsRecord>("CharSections.db2", DB6Metas.CharSectionsMeta, HotfixStatements.SEL_CHAR_SECTIONS);
            CharStartOutfitStorage = DB6Reader.Read<CharStartOutfitRecord>("CharStartOutfit.db2", DB6Metas.CharStartOutfitMeta, HotfixStatements.SEL_CHAR_START_OUTFIT);
            CharTitlesStorage = DB6Reader.Read<CharTitlesRecord>("CharTitles.db2", DB6Metas.CharTitlesMeta, HotfixStatements.SEL_CHAR_TITLES, HotfixStatements.SEL_CHAR_TITLES_LOCALE);
            ChatChannelsStorage = DB6Reader.Read<ChatChannelsRecord>("ChatChannels.db2", DB6Metas.ChatChannelsMeta, HotfixStatements.SEL_CHAT_CHANNELS, HotfixStatements.SEL_CHAT_CHANNELS_LOCALE);
            ChrClassesStorage = DB6Reader.Read<ChrClassesRecord>("ChrClasses.db2", DB6Metas.ChrClassesMeta, HotfixStatements.SEL_CHR_CLASSES, HotfixStatements.SEL_CHR_CLASSES_LOCALE);
            ChrClassesXPowerTypesStorage = DB6Reader.Read<ChrClassesXPowerTypesRecord>("ChrClassesXPowerTypes.db2", DB6Metas.ChrClassesXPowerTypesMeta, HotfixStatements.SEL_CHR_CLASSES_X_POWER_TYPES);
            ChrRacesStorage = DB6Reader.Read<ChrRacesRecord>("ChrRaces.db2", DB6Metas.ChrRacesMeta, HotfixStatements.SEL_CHR_RACES, HotfixStatements.SEL_CHR_RACES_LOCALE);
            ChrSpecializationStorage = DB6Reader.Read<ChrSpecializationRecord>("ChrSpecialization.db2", DB6Metas.ChrSpecializationMeta, HotfixStatements.SEL_CHR_SPECIALIZATION, HotfixStatements.SEL_CHR_SPECIALIZATION_LOCALE);
            CinematicCameraStorage = DB6Reader.Read<CinematicCameraRecord>("CinematicCamera.db2", DB6Metas.CinematicCameraMeta, HotfixStatements.SEL_CINEMATIC_CAMERA);
            CinematicSequencesStorage = DB6Reader.Read<CinematicSequencesRecord>("CinematicSequences.db2", DB6Metas.CinematicSequencesMeta, HotfixStatements.SEL_CINEMATIC_SEQUENCES);
            ConversationLineStorage = DB6Reader.Read<ConversationLineRecord>("ConversationLine.db2", DB6Metas.ConversationLineMeta, HotfixStatements.SEL_CONVERSATION_LINE);
            CreatureDisplayInfoStorage = DB6Reader.Read<CreatureDisplayInfoRecord>("CreatureDisplayInfo.db2", DB6Metas.CreatureDisplayInfoMeta, HotfixStatements.SEL_CREATURE_DISPLAY_INFO);
            CreatureDisplayInfoExtraStorage = DB6Reader.Read<CreatureDisplayInfoExtraRecord>("CreatureDisplayInfoExtra.db2", DB6Metas.CreatureDisplayInfoExtraMeta, HotfixStatements.SEL_CREATURE_DISPLAY_INFO_EXTRA);
            CreatureFamilyStorage = DB6Reader.Read<CreatureFamilyRecord>("CreatureFamily.db2", DB6Metas.CreatureFamilyMeta, HotfixStatements.SEL_CREATURE_FAMILY, HotfixStatements.SEL_CREATURE_FAMILY_LOCALE);
            CreatureModelDataStorage = DB6Reader.Read<CreatureModelDataRecord>("CreatureModelData.db2", DB6Metas.CreatureModelDataMeta, HotfixStatements.SEL_CREATURE_MODEL_DATA);
            CreatureTypeStorage = DB6Reader.Read<CreatureTypeRecord>("CreatureType.db2", DB6Metas.CreatureTypeMeta, HotfixStatements.SEL_CREATURE_TYPE, HotfixStatements.SEL_CREATURE_TYPE_LOCALE);
            CriteriaStorage = DB6Reader.Read<CriteriaRecord>("Criteria.db2", DB6Metas.CriteriaMeta, HotfixStatements.SEL_CRITERIA);
            CriteriaTreeStorage = DB6Reader.Read<CriteriaTreeRecord>("CriteriaTree.db2", DB6Metas.CriteriaTreeMeta, HotfixStatements.SEL_CRITERIA_TREE, HotfixStatements.SEL_CRITERIA_TREE_LOCALE);
            CurrencyTypesStorage = DB6Reader.Read<CurrencyTypesRecord>("CurrencyTypes.db2", DB6Metas.CurrencyTypesMeta, HotfixStatements.SEL_CURRENCY_TYPES, HotfixStatements.SEL_CURRENCY_TYPES_LOCALE);
            CurveStorage = DB6Reader.Read<CurveRecord>("Curve.db2", DB6Metas.CurveMeta, HotfixStatements.SEL_CURVE);
            CurvePointStorage = DB6Reader.Read<CurvePointRecord>("CurvePoint.db2", DB6Metas.CurvePointMeta, HotfixStatements.SEL_CURVE_POINT);
            DestructibleModelDataStorage = DB6Reader.Read<DestructibleModelDataRecord>("DestructibleModelData.db2", DB6Metas.DestructibleModelDataMeta, HotfixStatements.SEL_DESTRUCTIBLE_MODEL_DATA);
            DifficultyStorage = DB6Reader.Read<DifficultyRecord>("Difficulty.db2", DB6Metas.DifficultyMeta, HotfixStatements.SEL_DIFFICULTY, HotfixStatements.SEL_DIFFICULTY_LOCALE);
            DungeonEncounterStorage = DB6Reader.Read<DungeonEncounterRecord>("DungeonEncounter.db2", DB6Metas.DungeonEncounterMeta, HotfixStatements.SEL_DUNGEON_ENCOUNTER, HotfixStatements.SEL_DUNGEON_ENCOUNTER_LOCALE);
            DurabilityCostsStorage = DB6Reader.Read<DurabilityCostsRecord>("DurabilityCosts.db2", DB6Metas.DurabilityCostsMeta, HotfixStatements.SEL_DURABILITY_COSTS);
            DurabilityQualityStorage = DB6Reader.Read<DurabilityQualityRecord>("DurabilityQuality.db2", DB6Metas.DurabilityQualityMeta, HotfixStatements.SEL_DURABILITY_QUALITY);
            EmotesStorage = DB6Reader.Read<EmotesRecord>("Emotes.db2", DB6Metas.EmotesMeta, HotfixStatements.SEL_EMOTES);
            EmotesTextStorage = DB6Reader.Read<EmotesTextRecord>("EmotesText.db2", DB6Metas.EmotesTextMeta, HotfixStatements.SEL_EMOTES_TEXT, HotfixStatements.SEL_EMOTES_TEXT_LOCALE);
            EmotesTextSoundStorage = DB6Reader.Read<EmotesTextSoundRecord>("EmotesTextSound.db2", DB6Metas.EmotesTextSoundMeta, HotfixStatements.SEL_EMOTES_TEXT_SOUND);
            FactionStorage = DB6Reader.Read<FactionRecord>("Faction.db2", DB6Metas.FactionMeta, HotfixStatements.SEL_FACTION, HotfixStatements.SEL_FACTION_LOCALE);
            FactionTemplateStorage = DB6Reader.Read<FactionTemplateRecord>("FactionTemplate.db2", DB6Metas.FactionTemplateMeta, HotfixStatements.SEL_FACTION_TEMPLATE);
            GameObjectsStorage = DB6Reader.Read<GameObjectsRecord>("GameObjects.db2", DB6Metas.GameObjectsMeta, HotfixStatements.SEL_GAMEOBJECTS, HotfixStatements.SEL_GAMEOBJECTS_LOCALE);
            GameObjectDisplayInfoStorage = DB6Reader.Read<GameObjectDisplayInfoRecord>("GameObjectDisplayInfo.db2", DB6Metas.GameObjectDisplayInfoMeta, HotfixStatements.SEL_GAMEOBJECT_DISPLAY_INFO);
            GarrAbilityStorage = DB6Reader.Read<GarrAbilityRecord>("GarrAbility.db2", DB6Metas.GarrAbilityMeta, HotfixStatements.SEL_GARR_ABILITY, HotfixStatements.SEL_GARR_ABILITY_LOCALE);
            GarrBuildingStorage = DB6Reader.Read<GarrBuildingRecord>("GarrBuilding.db2", DB6Metas.GarrBuildingMeta, HotfixStatements.SEL_GARR_BUILDING, HotfixStatements.SEL_GARR_BUILDING_LOCALE);
            GarrBuildingPlotInstStorage = DB6Reader.Read<GarrBuildingPlotInstRecord>("GarrBuildingPlotInst.db2", DB6Metas.GarrBuildingPlotInstMeta, HotfixStatements.SEL_GARR_BUILDING_PLOT_INST);
            GarrClassSpecStorage = DB6Reader.Read<GarrClassSpecRecord>("GarrClassSpec.db2", DB6Metas.GarrClassSpecMeta, HotfixStatements.SEL_GARR_CLASS_SPEC, HotfixStatements.SEL_GARR_CLASS_SPEC_LOCALE);
            GarrFollowerStorage = DB6Reader.Read<GarrFollowerRecord>("GarrFollower.db2", DB6Metas.GarrFollowerMeta, HotfixStatements.SEL_GARR_FOLLOWER, HotfixStatements.SEL_GARR_FOLLOWER_LOCALE);
            GarrFollowerXAbilityStorage = DB6Reader.Read<GarrFollowerXAbilityRecord>("GarrFollowerXAbility.db2", DB6Metas.GarrFollowerXAbilityMeta, HotfixStatements.SEL_GARR_FOLLOWER_X_ABILITY);
            GarrPlotBuildingStorage = DB6Reader.Read<GarrPlotBuildingRecord>("GarrPlotBuilding.db2", DB6Metas.GarrPlotBuildingMeta, HotfixStatements.SEL_GARR_PLOT_BUILDING);
            GarrPlotStorage = DB6Reader.Read<GarrPlotRecord>("GarrPlot.db2", DB6Metas.GarrPlotMeta, HotfixStatements.SEL_GARR_PLOT, HotfixStatements.SEL_GARR_PLOT_LOCALE);
            GarrPlotInstanceStorage = DB6Reader.Read<GarrPlotInstanceRecord>("GarrPlotInstance.db2", DB6Metas.GarrPlotInstanceMeta, HotfixStatements.SEL_GARR_PLOT_INSTANCE, HotfixStatements.SEL_GARR_PLOT_INSTANCE_LOCALE);
            GarrSiteLevelStorage = DB6Reader.Read<GarrSiteLevelRecord>("GarrSiteLevel.db2", DB6Metas.GarrSiteLevelMeta, HotfixStatements.SEL_GARR_SITE_LEVEL);
            GarrSiteLevelPlotInstStorage = DB6Reader.Read<GarrSiteLevelPlotInstRecord>("GarrSiteLevelPlotInst.db2", DB6Metas.GarrSiteLevelPlotInstMeta, HotfixStatements.SEL_GARR_SITE_LEVEL_PLOT_INST);
            GemPropertiesStorage = DB6Reader.Read<GemPropertiesRecord>("GemProperties.db2", DB6Metas.GemPropertiesMeta, HotfixStatements.SEL_GEM_PROPERTIES);
            GlyphBindableSpellStorage = DB6Reader.Read<GlyphBindableSpellRecord>("GlyphBindableSpell.db2", DB6Metas.GlyphBindableSpellMeta, HotfixStatements.SEL_GLYPH_BINDABLE_SPELL);
            GlyphPropertiesStorage = DB6Reader.Read<GlyphPropertiesRecord>("GlyphProperties.db2", DB6Metas.GlyphPropertiesMeta, HotfixStatements.SEL_GLYPH_PROPERTIES);
            GlyphRequiredSpecStorage = DB6Reader.Read<GlyphRequiredSpecRecord>("GlyphRequiredSpec.db2", DB6Metas.GlyphRequiredSpecMeta, HotfixStatements.SEL_GLYPH_REQUIRED_SPEC);
            GuildColorBackgroundStorage = DB6Reader.Read<GuildColorBackgroundRecord>("GuildColorBackground.db2", DB6Metas.GuildColorBackgroundMeta, HotfixStatements.SEL_GUILD_COLOR_BACKGROUND);
            GuildColorBorderStorage = DB6Reader.Read<GuildColorBorderRecord>("GuildColorBorder.db2", DB6Metas.GuildColorBorderMeta, HotfixStatements.SEL_GUILD_COLOR_BORDER);
            GuildColorEmblemStorage = DB6Reader.Read<GuildColorEmblemRecord>("GuildColorEmblem.db2", DB6Metas.GuildColorEmblemMeta, HotfixStatements.SEL_GUILD_COLOR_EMBLEM);
            GuildPerkSpellsStorage = DB6Reader.Read<GuildPerkSpellsRecord>("GuildPerkSpells.db2", DB6Metas.GuildPerkSpellsMeta, HotfixStatements.SEL_GUILD_PERK_SPELLS);
            HeirloomStorage = DB6Reader.Read<HeirloomRecord>("Heirloom.db2", DB6Metas.HeirloomMeta, HotfixStatements.SEL_HEIRLOOM, HotfixStatements.SEL_HEIRLOOM_LOCALE);
            HolidaysStorage = DB6Reader.Read<HolidaysRecord>("Holidays.db2", DB6Metas.HolidaysMeta, HotfixStatements.SEL_HOLIDAYS);
            ImportPriceArmorStorage = DB6Reader.Read<ImportPriceArmorRecord>("ImportPriceArmor.db2", DB6Metas.ImportPriceArmorMeta, HotfixStatements.SEL_IMPORT_PRICE_ARMOR);
            ImportPriceQualityStorage = DB6Reader.Read<ImportPriceQualityRecord>("ImportPriceQuality.db2", DB6Metas.ImportPriceQualityMeta, HotfixStatements.SEL_IMPORT_PRICE_QUALITY);
            ImportPriceShieldStorage = DB6Reader.Read<ImportPriceShieldRecord>("ImportPriceShield.db2", DB6Metas.ImportPriceShieldMeta, HotfixStatements.SEL_IMPORT_PRICE_SHIELD);
            ImportPriceWeaponStorage = DB6Reader.Read<ImportPriceWeaponRecord>("ImportPriceWeapon.db2", DB6Metas.ImportPriceWeaponMeta, HotfixStatements.SEL_IMPORT_PRICE_WEAPON);
            ItemAppearanceStorage = DB6Reader.Read<ItemAppearanceRecord>("ItemAppearance.db2", DB6Metas.ItemAppearanceMeta, HotfixStatements.SEL_ITEM_APPEARANCE);
            ItemArmorQualityStorage = DB6Reader.Read<ItemArmorQualityRecord>("ItemArmorQuality.db2", DB6Metas.ItemArmorQualityMeta, HotfixStatements.SEL_ITEM_ARMOR_QUALITY);
            ItemArmorShieldStorage = DB6Reader.Read<ItemArmorShieldRecord>("ItemArmorShield.db2", DB6Metas.ItemArmorShieldMeta, HotfixStatements.SEL_ITEM_ARMOR_SHIELD);
            ItemArmorTotalStorage = DB6Reader.Read<ItemArmorTotalRecord>("ItemArmorTotal.db2", DB6Metas.ItemArmorTotalMeta, HotfixStatements.SEL_ITEM_ARMOR_TOTAL);
            //ItemBagFamilyStorage = DB6Reader.Read<ItemBagFamilyRecord>("ItemBagFamily.db2", DB6Metas.ItemBagFamilyMeta, HotfixStatements.SEL_ITEM_BAG_FAMILY, HotfixStatements.SEL_ITEM_BAG_FAMILY_LOCALE);
            ItemBonusStorage = DB6Reader.Read<ItemBonusRecord>("ItemBonus.db2", DB6Metas.ItemBonusMeta, HotfixStatements.SEL_ITEM_BONUS);
            ItemBonusListLevelDeltaStorage = DB6Reader.Read<ItemBonusListLevelDeltaRecord>("ItemBonusListLevelDelta.db2", DB6Metas.ItemBonusListLevelDeltaMeta, HotfixStatements.SEL_ITEM_BONUS_LIST_LEVEL_DELTA);
            ItemBonusTreeNodeStorage = DB6Reader.Read<ItemBonusTreeNodeRecord>("ItemBonusTreeNode.db2", DB6Metas.ItemBonusTreeNodeMeta, HotfixStatements.SEL_ITEM_BONUS_TREE_NODE);
            ItemChildEquipmentStorage = DB6Reader.Read<ItemChildEquipmentRecord>("ItemChildEquipment.db2", DB6Metas.ItemChildEquipmentMeta, HotfixStatements.SEL_ITEM_CHILD_EQUIPMENT);
            ItemClassStorage = DB6Reader.Read<ItemClassRecord>("ItemClass.db2", DB6Metas.ItemClassMeta, HotfixStatements.SEL_ITEM_CLASS, HotfixStatements.SEL_ITEM_CLASS_LOCALE);
            ItemCurrencyCostStorage = DB6Reader.Read<ItemCurrencyCostRecord>("ItemCurrencyCost.db2", DB6Metas.ItemCurrencyCostMeta, HotfixStatements.SEL_ITEM_CURRENCY_COST);
            ItemDamageAmmoStorage = DB6Reader.Read<ItemDamageRecord>("ItemDamageAmmo.db2", DB6Metas.ItemDamageAmmoMeta, HotfixStatements.SEL_ITEM_DAMAGE_AMMO);
            ItemDamageOneHandStorage = DB6Reader.Read<ItemDamageRecord>("ItemDamageOneHand.db2", DB6Metas.ItemDamageOneHandMeta, HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND);
            ItemDamageOneHandCasterStorage = DB6Reader.Read<ItemDamageRecord>("ItemDamageOneHandCaster.db2", DB6Metas.ItemDamageOneHandCasterMeta, HotfixStatements.SEL_ITEM_DAMAGE_ONE_HAND_CASTER);
            ItemDamageTwoHandStorage = DB6Reader.Read<ItemDamageRecord>("ItemDamageTwoHand.db2", DB6Metas.ItemDamageTwoHandMeta, HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND);
            ItemDamageTwoHandCasterStorage = DB6Reader.Read<ItemDamageRecord>("ItemDamageTwoHandCaster.db2", DB6Metas.ItemDamageTwoHandCasterMeta, HotfixStatements.SEL_ITEM_DAMAGE_TWO_HAND_CASTER);
            ItemDisenchantLootStorage = DB6Reader.Read<ItemDisenchantLootRecord>("ItemDisenchantLoot.db2", DB6Metas.ItemDisenchantLootMeta, HotfixStatements.SEL_ITEM_DISENCHANT_LOOT);
            ItemEffectStorage = DB6Reader.Read<ItemEffectRecord>("ItemEffect.db2", DB6Metas.ItemEffectMeta, HotfixStatements.SEL_ITEM_EFFECT);
            ItemStorage = DB6Reader.Read<ItemRecord>("Item.db2", DB6Metas.ItemMeta, HotfixStatements.SEL_ITEM);
            ItemExtendedCostStorage = DB6Reader.Read<ItemExtendedCostRecord>("ItemExtendedCost.db2", DB6Metas.ItemExtendedCostMeta, HotfixStatements.SEL_ITEM_EXTENDED_COST);
            ItemLevelSelectorStorage = DB6Reader.Read<ItemLevelSelectorRecord>("ItemLevelSelector.db2", DB6Metas.ItemLevelSelectorMeta, HotfixStatements.SEL_ITEM_LEVEL_SELECTOR);
            ItemLevelSelectorQualityStorage = DB6Reader.Read<ItemLevelSelectorQualityRecord>("ItemLevelSelectorQuality.db2", DB6Metas.ItemLevelSelectorQualityMeta, HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY);
            ItemLevelSelectorQualitySetStorage = DB6Reader.Read<ItemLevelSelectorQualitySetRecord>("ItemLevelSelectorQualitySet.db2", DB6Metas.ItemLevelSelectorQualitySetMeta, HotfixStatements.SEL_ITEM_LEVEL_SELECTOR_QUALITY_SET);
            ItemLimitCategoryStorage = DB6Reader.Read<ItemLimitCategoryRecord>("ItemLimitCategory.db2", DB6Metas.ItemLimitCategoryMeta, HotfixStatements.SEL_ITEM_LIMIT_CATEGORY, HotfixStatements.SEL_ITEM_LIMIT_CATEGORY_LOCALE);
            ItemModifiedAppearanceStorage = DB6Reader.Read<ItemModifiedAppearanceRecord>("ItemModifiedAppearance.db2", DB6Metas.ItemModifiedAppearanceMeta, HotfixStatements.SEL_ITEM_MODIFIED_APPEARANCE);
            ItemPriceBaseStorage = DB6Reader.Read<ItemPriceBaseRecord>("ItemPriceBase.db2", DB6Metas.ItemPriceBaseMeta, HotfixStatements.SEL_ITEM_PRICE_BASE);
            ItemRandomPropertiesStorage = DB6Reader.Read<ItemRandomPropertiesRecord>("ItemRandomProperties.db2", DB6Metas.ItemRandomPropertiesMeta, HotfixStatements.SEL_ITEM_RANDOM_PROPERTIES, HotfixStatements.SEL_ITEM_RANDOM_PROPERTIES_LOCALE);
            ItemRandomSuffixStorage = DB6Reader.Read<ItemRandomSuffixRecord>("ItemRandomSuffix.db2", DB6Metas.ItemRandomSuffixMeta, HotfixStatements.SEL_ITEM_RANDOM_SUFFIX, HotfixStatements.SEL_ITEM_RANDOM_SUFFIX_LOCALE);
            ItemSearchNameStorage = DB6Reader.Read<ItemSearchNameRecord>("ItemSearchName.db2", DB6Metas.ItemSearchNameMeta, HotfixStatements.SEL_ITEM_SEARCH_NAME, HotfixStatements.SEL_ITEM_SEARCH_NAME_LOCALE);
            ItemSetStorage = DB6Reader.Read<ItemSetRecord>("ItemSet.db2", DB6Metas.ItemSetMeta, HotfixStatements.SEL_ITEM_SET, HotfixStatements.SEL_ITEM_SET_LOCALE);
            ItemSetSpellStorage = DB6Reader.Read<ItemSetSpellRecord>("ItemSetSpell.db2", DB6Metas.ItemSetSpellMeta, HotfixStatements.SEL_ITEM_SET_SPELL);
            ItemSparseStorage = DB6Reader.Read<ItemSparseRecord>("ItemSparse.db2", DB6Metas.ItemSparseMeta, HotfixStatements.SEL_ITEM_SPARSE, HotfixStatements.SEL_ITEM_SPARSE_LOCALE);
            ItemSpecStorage = DB6Reader.Read<ItemSpecRecord>("ItemSpec.db2", DB6Metas.ItemSpecMeta, HotfixStatements.SEL_ITEM_SPEC);
            ItemSpecOverrideStorage = DB6Reader.Read<ItemSpecOverrideRecord>("ItemSpecOverride.db2", DB6Metas.ItemSpecOverrideMeta, HotfixStatements.SEL_ITEM_SPEC_OVERRIDE);
            ItemUpgradeStorage = DB6Reader.Read<ItemUpgradeRecord>("ItemUpgrade.db2", DB6Metas.ItemUpgradeMeta, HotfixStatements.SEL_ITEM_UPGRADE);
            ItemXBonusTreeStorage = DB6Reader.Read<ItemXBonusTreeRecord>("ItemXBonusTree.db2", DB6Metas.ItemXBonusTreeMeta, HotfixStatements.SEL_ITEM_X_BONUS_TREE);
            //KeyChainStorage = DB6Reader.Read<KeyChainRecord>("KeyChain.db2", DB6Metas.KeyChainMeta, HotfixStatements.SEL_KEY_CHAIN);
            LFGDungeonsStorage = DB6Reader.Read<LFGDungeonsRecord>("LFGDungeons.db2", DB6Metas.LFGDungeonsMeta, HotfixStatements.SEL_LFG_DUNGEONS, HotfixStatements.SEL_LFG_DUNGEONS_LOCALE);
            LightStorage = DB6Reader.Read<LightRecord>("Light.db2", DB6Metas.LightMeta, HotfixStatements.SEL_LIGHT);
            LiquidTypeStorage = DB6Reader.Read<LiquidTypeRecord>("LiquidType.db2", DB6Metas.LiquidTypeMeta, HotfixStatements.SEL_LIQUID_TYPE, HotfixStatements.SEL_LIQUID_TYPE_LOCALE);
            LockStorage = DB6Reader.Read<LockRecord>("Lock.db2", DB6Metas.LockMeta, HotfixStatements.SEL_LOCK);
            MailTemplateStorage = DB6Reader.Read<MailTemplateRecord>("MailTemplate.db2", DB6Metas.MailTemplateMeta, HotfixStatements.SEL_MAIL_TEMPLATE, HotfixStatements.SEL_MAIL_TEMPLATE_LOCALE);
            MapStorage = DB6Reader.Read<MapRecord>("Map.db2", DB6Metas.MapMeta, HotfixStatements.SEL_MAP, HotfixStatements.SEL_MAP_LOCALE);
            MapDifficultyStorage = DB6Reader.Read<MapDifficultyRecord>("MapDifficulty.db2", DB6Metas.MapDifficultyMeta, HotfixStatements.SEL_MAP_DIFFICULTY, HotfixStatements.SEL_MAP_DIFFICULTY_LOCALE);
            ModifierTreeStorage = DB6Reader.Read<ModifierTreeRecord>("ModifierTree.db2", DB6Metas.ModifierTreeMeta, HotfixStatements.SEL_MODIFIER_TREE);
            MountCapabilityStorage = DB6Reader.Read<MountCapabilityRecord>("MountCapability.db2", DB6Metas.MountCapabilityMeta, HotfixStatements.SEL_MOUNT_CAPABILITY);
            MountStorage = DB6Reader.Read<MountRecord>("Mount.db2", DB6Metas.MountMeta, HotfixStatements.SEL_MOUNT, HotfixStatements.SEL_MOUNT_LOCALE);
            MountTypeXCapabilityStorage = DB6Reader.Read<MountTypeXCapabilityRecord>("MountTypeXCapability.db2", DB6Metas.MountTypeXCapabilityMeta, HotfixStatements.SEL_MOUNT_TYPE_X_CAPABILITY);
            MountXDisplayStorage = DB6Reader.Read<MountXDisplayRecord>("MountXDisplay.db2", DB6Metas.MountXDisplayMeta, HotfixStatements.SEL_MOUNT_X_DISPLAY);
            MovieStorage = DB6Reader.Read<MovieRecord>("Movie.db2", DB6Metas.MovieMeta, HotfixStatements.SEL_MOVIE);
            NameGenStorage = DB6Reader.Read<NameGenRecord>("NameGen.db2", DB6Metas.NameGenMeta, HotfixStatements.SEL_NAME_GEN, HotfixStatements.SEL_NAME_GEN_LOCALE);
            NamesProfanityStorage = DB6Reader.Read<NamesProfanityRecord>("NamesProfanity.db2", DB6Metas.NamesProfanityMeta, HotfixStatements.SEL_NAMES_PROFANITY);
            NamesReservedStorage = DB6Reader.Read<NamesReservedRecord>("NamesReserved.db2", DB6Metas.NamesReservedMeta, HotfixStatements.SEL_NAMES_RESERVED, HotfixStatements.SEL_NAMES_RESERVED_LOCALE);
            NamesReservedLocaleStorage = DB6Reader.Read<NamesReservedLocaleRecord>("NamesReservedLocale.db2", DB6Metas.NamesReservedLocaleMeta, HotfixStatements.SEL_NAMES_RESERVED_LOCALE);
            OverrideSpellDataStorage = DB6Reader.Read<OverrideSpellDataRecord>("OverrideSpellData.db2", DB6Metas.OverrideSpellDataMeta, HotfixStatements.SEL_OVERRIDE_SPELL_DATA);
            PhaseStorage = DB6Reader.Read<PhaseRecord>("Phase.db2", DB6Metas.PhaseMeta, HotfixStatements.SEL_PHASE);
            PhaseXPhaseGroupStorage = DB6Reader.Read<PhaseXPhaseGroupRecord>("PhaseXPhaseGroup.db2", DB6Metas.PhaseXPhaseGroupMeta, HotfixStatements.SEL_PHASE_X_PHASE_GROUP);
            PlayerConditionStorage = DB6Reader.Read<PlayerConditionRecord>("PlayerCondition.db2", DB6Metas.PlayerConditionMeta, HotfixStatements.SEL_PLAYER_CONDITION, HotfixStatements.SEL_PLAYER_CONDITION_LOCALE);
            PowerDisplayStorage = DB6Reader.Read<PowerDisplayRecord>("PowerDisplay.db2", DB6Metas.PowerDisplayMeta, HotfixStatements.SEL_POWER_DISPLAY);
            PowerTypeStorage = DB6Reader.Read<PowerTypeRecord>("PowerType.db2", DB6Metas.PowerTypeMeta, HotfixStatements.SEL_POWER_TYPE);
            PrestigeLevelInfoStorage = DB6Reader.Read<PrestigeLevelInfoRecord>("PrestigeLevelInfo.db2", DB6Metas.PrestigeLevelInfoMeta, HotfixStatements.SEL_PRESTIGE_LEVEL_INFO, HotfixStatements.SEL_PRESTIGE_LEVEL_INFO_LOCALE);
            PVPDifficultyStorage = DB6Reader.Read<PVPDifficultyRecord>("PVPDifficulty.db2", DB6Metas.PvpDifficultyMeta, HotfixStatements.SEL_PVP_DIFFICULTY);
            PvpRewardStorage = DB6Reader.Read<PvpRewardRecord>("PvpReward.db2", DB6Metas.PvpRewardMeta, HotfixStatements.SEL_PVP_REWARD);
            QuestFactionRewardStorage = DB6Reader.Read<QuestFactionRewardRecord>("QuestFactionReward.db2", DB6Metas.QuestFactionRewardMeta, HotfixStatements.SEL_QUEST_FACTION_REWARD);
            QuestMoneyRewardStorage = DB6Reader.Read<QuestMoneyRewardRecord>("QuestMoneyReward.db2", DB6Metas.QuestMoneyRewardMeta, HotfixStatements.SEL_QUEST_MONEY_REWARD);
            QuestPackageItemStorage = DB6Reader.Read<QuestPackageItemRecord>("QuestPackageItem.db2", DB6Metas.QuestPackageItemMeta, HotfixStatements.SEL_QUEST_PACKAGE_ITEM);
            QuestSortStorage = DB6Reader.Read<QuestSortRecord>("QuestSort.db2", DB6Metas.QuestSortMeta, HotfixStatements.SEL_QUEST_SORT, HotfixStatements.SEL_QUEST_SORT_LOCALE);
            QuestV2Storage = DB6Reader.Read<QuestV2Record>("QuestV2.db2", DB6Metas.QuestV2Meta, HotfixStatements.SEL_QUEST_V2);
            QuestXPStorage = DB6Reader.Read<QuestXPRecord>("QuestXP.db2", DB6Metas.QuestXPMeta, HotfixStatements.SEL_QUEST_XP);
            RandPropPointsStorage = DB6Reader.Read<RandPropPointsRecord>("RandPropPoints.db2", DB6Metas.RandPropPointsMeta, HotfixStatements.SEL_RAND_PROP_POINTS);
            RewardPackStorage = DB6Reader.Read<RewardPackRecord>("RewardPack.db2", DB6Metas.RewardPackMeta, HotfixStatements.SEL_REWARD_PACK);
            RewardPackXItemStorage = DB6Reader.Read<RewardPackXItemRecord>("RewardPackXItem.db2", DB6Metas.RewardPackXItemMeta, HotfixStatements.SEL_REWARD_PACK_X_ITEM);
            RulesetItemUpgradeStorage = DB6Reader.Read<RulesetItemUpgradeRecord>("RulesetItemUpgrade.db2", DB6Metas.RulesetItemUpgradeMeta, HotfixStatements.SEL_RULESET_ITEM_UPGRADE);
            ScalingStatDistributionStorage = DB6Reader.Read<ScalingStatDistributionRecord>("ScalingStatDistribution.db2", DB6Metas.ScalingStatDistributionMeta, HotfixStatements.SEL_SCALING_STAT_DISTRIBUTION);
            ScenarioStorage = DB6Reader.Read<ScenarioRecord>("Scenario.db2", DB6Metas.ScenarioMeta, HotfixStatements.SEL_SCENARIO, HotfixStatements.SEL_SCENARIO_LOCALE);
            ScenarioStepStorage = DB6Reader.Read<ScenarioStepRecord>("ScenarioStep.db2", DB6Metas.ScenarioStepMeta, HotfixStatements.SEL_SCENARIO_STEP, HotfixStatements.SEL_SCENARIO_STEP_LOCALE);
            //SceneScriptStorage = DB6Reader.Read<SceneScriptRecord>("SceneScript.db2", DB6Metas.SceneScriptMeta, HotfixStatements.SEL_SCENE_SCRIPT);
            SceneScriptPackageStorage = DB6Reader.Read<SceneScriptPackageRecord>("SceneScriptPackage.db2", DB6Metas.SceneScriptPackageMeta, HotfixStatements.SEL_SCENE_SCRIPT_PACKAGE);
            SkillLineStorage = DB6Reader.Read<SkillLineRecord>("SkillLine.db2", DB6Metas.SkillLineMeta, HotfixStatements.SEL_SKILL_LINE, HotfixStatements.SEL_SKILL_LINE_LOCALE);
            SkillLineAbilityStorage = DB6Reader.Read<SkillLineAbilityRecord>("SkillLineAbility.db2", DB6Metas.SkillLineAbilityMeta, HotfixStatements.SEL_SKILL_LINE_ABILITY);
            SkillRaceClassInfoStorage = DB6Reader.Read<SkillRaceClassInfoRecord>("SkillRaceClassInfo.db2", DB6Metas.SkillRaceClassInfoMeta, HotfixStatements.SEL_SKILL_RACE_CLASS_INFO);
            SoundKitStorage = DB6Reader.Read<SoundKitRecord>("SoundKit.db2", DB6Metas.SoundKitMeta, HotfixStatements.SEL_SOUND_KIT);
            SpecializationSpellsStorage = DB6Reader.Read<SpecializationSpellsRecord>("SpecializationSpells.db2", DB6Metas.SpecializationSpellsMeta, HotfixStatements.SEL_SPECIALIZATION_SPELLS, HotfixStatements.SEL_SPECIALIZATION_SPELLS_LOCALE);
            SpellStorage = DB6Reader.Read<SpellRecord>("Spell.db2", DB6Metas.SpellMeta, HotfixStatements.SEL_SPELL, HotfixStatements.SEL_SPELL_LOCALE);
            SpellAuraOptionsStorage = DB6Reader.Read<SpellAuraOptionsRecord>("SpellAuraOptions.db2", DB6Metas.SpellAuraOptionsMeta, HotfixStatements.SEL_SPELL_AURA_OPTIONS);
            SpellAuraRestrictionsStorage = DB6Reader.Read<SpellAuraRestrictionsRecord>("SpellAuraRestrictions.db2", DB6Metas.SpellAuraRestrictionsMeta, HotfixStatements.SEL_SPELL_AURA_RESTRICTIONS);
            SpellCastTimesStorage = DB6Reader.Read<SpellCastTimesRecord>("SpellCastTimes.db2", DB6Metas.SpellCastTimesMeta, HotfixStatements.SEL_SPELL_CAST_TIMES);
            SpellCastingRequirementsStorage = DB6Reader.Read<SpellCastingRequirementsRecord>("SpellCastingRequirements.db2", DB6Metas.SpellCastingRequirementsMeta, HotfixStatements.SEL_SPELL_CASTING_REQUIREMENTS);
            SpellCategoriesStorage = DB6Reader.Read<SpellCategoriesRecord>("SpellCategories.db2", DB6Metas.SpellCategoriesMeta, HotfixStatements.SEL_SPELL_CATEGORIES);
            SpellCategoryStorage = DB6Reader.Read<SpellCategoryRecord>("SpellCategory.db2", DB6Metas.SpellCategoryMeta, HotfixStatements.SEL_SPELL_CATEGORY, HotfixStatements.SEL_SPELL_CATEGORY_LOCALE);
            SpellClassOptionsStorage = DB6Reader.Read<SpellClassOptionsRecord>("SpellClassOptions.db2", DB6Metas.SpellClassOptionsMeta, HotfixStatements.SEL_SPELL_CLASS_OPTIONS);
            SpellCooldownsStorage = DB6Reader.Read<SpellCooldownsRecord>("SpellCooldowns.db2", DB6Metas.SpellCooldownsMeta, HotfixStatements.SEL_SPELL_COOLDOWNS);
            SpellDurationStorage = DB6Reader.Read<SpellDurationRecord>("SpellDuration.db2", DB6Metas.SpellDurationMeta, HotfixStatements.SEL_SPELL_DURATION);
            SpellEffectStorage = DB6Reader.Read<SpellEffectRecord>("SpellEffect.db2", DB6Metas.SpellEffectMeta, HotfixStatements.SEL_SPELL_EFFECT);
            SpellEffectScalingStorage = DB6Reader.Read<SpellEffectScalingRecord>("SpellEffectScaling.db2", DB6Metas.SpellEffectScalingMeta, HotfixStatements.SEL_SPELL_EFFECT_SCALING);
            SpellEquippedItemsStorage = DB6Reader.Read<SpellEquippedItemsRecord>("SpellEquippedItems.db2", DB6Metas.SpellEquippedItemsMeta, HotfixStatements.SEL_SPELL_EQUIPPED_ITEMS);
            SpellFocusObjectStorage = DB6Reader.Read<SpellFocusObjectRecord>("SpellFocusObject.db2", DB6Metas.SpellFocusObjectMeta, HotfixStatements.SEL_SPELL_FOCUS_OBJECT, HotfixStatements.SEL_SPELL_FOCUS_OBJECT_LOCALE);
            SpellInterruptsStorage = DB6Reader.Read<SpellInterruptsRecord>("SpellInterrupts.db2", DB6Metas.SpellInterruptsMeta, HotfixStatements.SEL_SPELL_INTERRUPTS);
            SpellItemEnchantmentStorage = DB6Reader.Read<SpellItemEnchantmentRecord>("SpellItemEnchantment.db2", DB6Metas.SpellItemEnchantmentMeta, HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT, HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_LOCALE);
            SpellItemEnchantmentConditionStorage = DB6Reader.Read<SpellItemEnchantmentConditionRecord>("SpellItemEnchantmentCondition.db2", DB6Metas.SpellItemEnchantmentConditionMeta, HotfixStatements.SEL_SPELL_ITEM_ENCHANTMENT_CONDITION);
            SpellLearnSpellStorage = DB6Reader.Read<SpellLearnSpellRecord>("SpellLearnSpell.db2", DB6Metas.SpellLearnSpellMeta, HotfixStatements.SEL_SPELL_LEARN_SPELL);
            SpellLevelsStorage = DB6Reader.Read<SpellLevelsRecord>("SpellLevels.db2", DB6Metas.SpellLevelsMeta, HotfixStatements.SEL_SPELL_LEVELS);
            SpellMiscStorage = DB6Reader.Read<SpellMiscRecord>("SpellMisc.db2", DB6Metas.SpellMiscMeta, HotfixStatements.SEL_SPELL_MISC);
            SpellPowerStorage = DB6Reader.Read<SpellPowerRecord>("SpellPower.db2", DB6Metas.SpellPowerMeta, HotfixStatements.SEL_SPELL_POWER);
            SpellPowerDifficultyStorage = DB6Reader.Read<SpellPowerDifficultyRecord>("SpellPowerDifficulty.db2", DB6Metas.SpellPowerDifficultyMeta, HotfixStatements.SEL_SPELL_POWER_DIFFICULTY);
            SpellProcsPerMinuteStorage = DB6Reader.Read<SpellProcsPerMinuteRecord>("SpellProcsPerMinute.db2", DB6Metas.SpellProcsPerMinuteMeta, HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE);
            SpellProcsPerMinuteModStorage = DB6Reader.Read<SpellProcsPerMinuteModRecord>("SpellProcsPerMinuteMod.db2", DB6Metas.SpellProcsPerMinuteModMeta, HotfixStatements.SEL_SPELL_PROCS_PER_MINUTE_MOD);
            SpellRadiusStorage = DB6Reader.Read<SpellRadiusRecord>("SpellRadius.db2", DB6Metas.SpellRadiusMeta, HotfixStatements.SEL_SPELL_RADIUS);
            SpellRangeStorage = DB6Reader.Read<SpellRangeRecord>("SpellRange.db2", DB6Metas.SpellRangeMeta, HotfixStatements.SEL_SPELL_RANGE, HotfixStatements.SEL_SPELL_RANGE_LOCALE);
            SpellReagentsStorage = DB6Reader.Read<SpellReagentsRecord>("SpellReagents.db2", DB6Metas.SpellReagentsMeta, HotfixStatements.SEL_SPELL_REAGENTS);
            SpellScalingStorage = DB6Reader.Read<SpellScalingRecord>("SpellScaling.db2", DB6Metas.SpellScalingMeta, HotfixStatements.SEL_SPELL_SCALING);
            SpellShapeshiftStorage = DB6Reader.Read<SpellShapeshiftRecord>("SpellShapeshift.db2", DB6Metas.SpellShapeshiftMeta, HotfixStatements.SEL_SPELL_SHAPESHIFT);
            SpellShapeshiftFormStorage = DB6Reader.Read<SpellShapeshiftFormRecord>("SpellShapeshiftForm.db2", DB6Metas.SpellShapeshiftFormMeta, HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM, HotfixStatements.SEL_SPELL_SHAPESHIFT_FORM_LOCALE);
            SpellTargetRestrictionsStorage = DB6Reader.Read<SpellTargetRestrictionsRecord>("SpellTargetRestrictions.db2", DB6Metas.SpellTargetRestrictionsMeta, HotfixStatements.SEL_SPELL_TARGET_RESTRICTIONS);
            SpellTotemsStorage = DB6Reader.Read<SpellTotemsRecord>("SpellTotems.db2", DB6Metas.SpellTotemsMeta, HotfixStatements.SEL_SPELL_TOTEMS);
            SpellXSpellVisualStorage = DB6Reader.Read<SpellXSpellVisualRecord>("SpellXSpellVisual.db2", DB6Metas.SpellXSpellVisualMeta, HotfixStatements.SEL_SPELL_X_SPELL_VISUAL);
            SummonPropertiesStorage = DB6Reader.Read<SummonPropertiesRecord>("SummonProperties.db2", DB6Metas.SummonPropertiesMeta, HotfixStatements.SEL_SUMMON_PROPERTIES);
            //TactKeyStorage = DB6Reader.Read<TactKeyRecord>("TactKey.db2", DB6Metas.TactKeyMeta, HotfixStatements.SEL_TACT_KEY);
            TalentStorage = DB6Reader.Read<TalentRecord>("Talent.db2", DB6Metas.TalentMeta, HotfixStatements.SEL_TALENT, HotfixStatements.SEL_TALENT_LOCALE);
            TaxiNodesStorage = DB6Reader.Read<TaxiNodesRecord>("TaxiNodes.db2", DB6Metas.TaxiNodesMeta, HotfixStatements.SEL_TAXI_NODES, HotfixStatements.SEL_TAXI_NODES_LOCALE);
            TaxiPathStorage = DB6Reader.Read<TaxiPathRecord>("TaxiPath.db2", DB6Metas.TaxiPathMeta, HotfixStatements.SEL_TAXI_PATH);
            TaxiPathNodeStorage = DB6Reader.Read<TaxiPathNodeRecord>("TaxiPathNode.db2", DB6Metas.TaxiPathNodeMeta, HotfixStatements.SEL_TAXI_PATH_NODE);
            TotemCategoryStorage = DB6Reader.Read<TotemCategoryRecord>("TotemCategory.db2", DB6Metas.TotemCategoryMeta, HotfixStatements.SEL_TOTEM_CATEGORY, HotfixStatements.SEL_TOTEM_CATEGORY_LOCALE);
            ToyStorage = DB6Reader.Read<ToyRecord>("Toy.db2", DB6Metas.ToyMeta, HotfixStatements.SEL_TOY, HotfixStatements.SEL_TOY_LOCALE);
            TransmogHolidayStorage = DB6Reader.Read<TransmogHolidayRecord>("TransmogHoliday.db2", DB6Metas.TransmogHolidayMeta, HotfixStatements.SEL_TRANSMOG_HOLIDAY);
            TransmogSetStorage = DB6Reader.Read<TransmogSetRecord>("TransmogSet.db2", DB6Metas.TransmogSetMeta, HotfixStatements.SEL_TRANSMOG_SET, HotfixStatements.SEL_TRANSMOG_SET_LOCALE);
            TransmogSetGroupStorage = DB6Reader.Read<TransmogSetGroupRecord>("TransmogSetGroup.db2", DB6Metas.TransmogSetGroupMeta, HotfixStatements.SEL_TRANSMOG_SET_GROUP, HotfixStatements.SEL_TRANSMOG_SET_GROUP_LOCALE);
            TransmogSetItemStorage = DB6Reader.Read<TransmogSetItemRecord>("TransmogSetItem.db2", DB6Metas.TransmogSetItemMeta, HotfixStatements.SEL_TRANSMOG_SET_ITEM);
            TransportAnimationStorage = DB6Reader.Read<TransportAnimationRecord>("TransportAnimation.db2", DB6Metas.TransportAnimationMeta, HotfixStatements.SEL_TRANSPORT_ANIMATION);
            TransportRotationStorage = DB6Reader.Read<TransportRotationRecord>("TransportRotation.db2", DB6Metas.TransportRotationMeta, HotfixStatements.SEL_TRANSPORT_ROTATION);
            UnitPowerBarStorage = DB6Reader.Read<UnitPowerBarRecord>("UnitPowerBar.db2", DB6Metas.UnitPowerBarMeta, HotfixStatements.SEL_UNIT_POWER_BAR, HotfixStatements.SEL_UNIT_POWER_BAR_LOCALE);
            VehicleStorage = DB6Reader.Read<VehicleRecord>("Vehicle.db2", DB6Metas.VehicleMeta, HotfixStatements.SEL_VEHICLE);
            VehicleSeatStorage = DB6Reader.Read<VehicleSeatRecord>("VehicleSeat.db2", DB6Metas.VehicleSeatMeta, HotfixStatements.SEL_VEHICLE_SEAT);
            WMOAreaTableStorage = DB6Reader.Read<WMOAreaTableRecord>("WMOAreaTable.db2", DB6Metas.WMOAreaTableMeta, HotfixStatements.SEL_WMO_AREA_TABLE, HotfixStatements.SEL_WMO_AREA_TABLE_LOCALE);
            WorldEffectStorage = DB6Reader.Read<WorldEffectRecord>("WorldEffect.db2", DB6Metas.WorldEffectMeta, HotfixStatements.SEL_WORLD_EFFECT);
            WorldMapAreaStorage = DB6Reader.Read<WorldMapAreaRecord>("WorldMapArea.db2", DB6Metas.WorldMapAreaMeta, HotfixStatements.SEL_WORLD_MAP_AREA);
            WorldMapOverlayStorage = DB6Reader.Read<WorldMapOverlayRecord>("WorldMapOverlay.db2", DB6Metas.WorldMapOverlayMeta, HotfixStatements.SEL_WORLD_MAP_OVERLAY);
            WorldMapTransformsStorage = DB6Reader.Read<WorldMapTransformsRecord>("WorldMapTransforms.db2", DB6Metas.WorldMapTransformsMeta, HotfixStatements.SEL_WORLD_MAP_TRANSFORMS);
            WorldSafeLocsStorage = DB6Reader.Read<WorldSafeLocsRecord>("WorldSafeLocs.db2", DB6Metas.WorldSafeLocsMeta, HotfixStatements.SEL_WORLD_SAFE_LOCS, HotfixStatements.SEL_WORLD_SAFE_LOCS_LOCALE);

            foreach (var entry in TaxiPathStorage.Values)
            {
                if (!TaxiPathSetBySource.ContainsKey(entry.From))
                    TaxiPathSetBySource.Add(entry.From, new Dictionary<uint, TaxiPathBySourceAndDestination>());
                TaxiPathSetBySource[entry.From][entry.To] = new TaxiPathBySourceAndDestination(entry.Id, entry.Cost);
            }

            uint pathCount = TaxiPathStorage.Keys.Max() + 1;

            // Calculate path nodes count
            uint[] pathLength = new uint[pathCount];                           // 0 and some other indexes not used
            foreach (TaxiPathNodeRecord entry in CliDB.TaxiPathNodeStorage.Values)
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
                byte field = (byte)((node.Id - 1) / 8);
                byte submask = (byte)(1 << (int)((node.Id - 1) % 8));

                TaxiNodesMask[field] |= submask;
                if (node.Flags.HasAnyFlag(TaxiNodeFlags.Horde))
                    HordeTaxiNodesMask[field] |= submask;
                if (node.Flags.HasAnyFlag(TaxiNodeFlags.Alliance))
                    AllianceTaxiNodesMask[field] |= submask;

                uint nodeMap;
                Global.DB2Mgr.DeterminaAlternateMapPosition(node.MapID, node.Pos.X, node.Pos.Y, node.Pos.Z, out nodeMap);
                if (nodeMap < 2)
                    OldContinentsNodesMask[field] |= submask;
            }

            Global.DB2Mgr.LoadStores();

            // Check loaded DB2 files proper version
            if (!AreaTableStorage.ContainsKey(8485) ||                // last area (areaflag) added in 7.0.3 (22594)
                !CharTitlesStorage.ContainsKey(486) ||                // last char title added in 7.0.3 (22594)
                !GemPropertiesStorage.ContainsKey(3363) ||            // last gem property added in 7.0.3 (22594)
                !ItemStorage.ContainsKey(142526) ||                   // last item added in 7.0.3 (22594)
                !ItemExtendedCostStorage.ContainsKey(6125) ||         // last item extended cost added in 7.0.3 (22594)
                !MapStorage.ContainsKey(1670) ||                      // last map added in 7.0.3 (22594)
                !SpellStorage.ContainsKey(231371))                    // last spell added in 7.0.3 (22594)
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
            HonorLevelGameTable = GameTableReader.Read<GtHonorLevelRecord>("HonorLevel.txt");
            HpPerStaGameTable = GameTableReader.Read<GtHpPerStaRecord>("HpPerSta.txt");
            NpcDamageByClassGameTable[0] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClass.txt");
            NpcDamageByClassGameTable[1] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClassExp1.txt");
            NpcDamageByClassGameTable[2] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClassExp2.txt");
            NpcDamageByClassGameTable[3] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClassExp3.txt");
            NpcDamageByClassGameTable[4] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClassExp4.txt");
            NpcDamageByClassGameTable[5] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClassExp5.txt");
            NpcDamageByClassGameTable[6] = GameTableReader.Read<GtNpcDamageByClassRecord>("NpcDamageByClassExp6.txt");
            NpcManaCostScalerGameTable = GameTableReader.Read<GtNpcManaCostScalerRecord>("NPCManaCostScaler.txt");
            NpcTotalHpGameTable[0] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHp.txt");
            NpcTotalHpGameTable[1] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHpExp1.txt");
            NpcTotalHpGameTable[2] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHpExp2.txt");
            NpcTotalHpGameTable[3] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHpExp3.txt");
            NpcTotalHpGameTable[4] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHpExp4.txt");
            NpcTotalHpGameTable[5] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHpExp5.txt");
            NpcTotalHpGameTable[6] = GameTableReader.Read<GtNpcTotalHpRecord>("NpcTotalHpExp6.txt");
            SpellScalingGameTable = GameTableReader.Read<GtSpellScalingRecord>("SpellScaling.txt");
            XpGameTable = GameTableReader.Read<GtXpRecord>("xp.txt");

            Log.outInfo(LogFilter.ServerLoading, "Initialized {0} DBC GameTables data stores in {1} ms", LoadedFileCount, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        #region Main Collections
        public static DB6Storage<AchievementRecord> AchievementStorage;
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
        public static DB6Storage<OverrideSpellDataRecord> OverrideSpellDataStorage;
        public static DB6Storage<PhaseRecord> PhaseStorage;
        public static DB6Storage<PhaseXPhaseGroupRecord> PhaseXPhaseGroupStorage;
        public static DB6Storage<PlayerConditionRecord> PlayerConditionStorage;
        public static DB6Storage<PowerDisplayRecord> PowerDisplayStorage;
        public static DB6Storage<PowerTypeRecord> PowerTypeStorage;
        public static DB6Storage<PrestigeLevelInfoRecord> PrestigeLevelInfoStorage;
        public static DB6Storage<PVPDifficultyRecord> PVPDifficultyStorage;
        public static DB6Storage<PvpRewardRecord> PvpRewardStorage;
        public static DB6Storage<QuestFactionRewardRecord> QuestFactionRewardStorage;
        public static DB6Storage<QuestMoneyRewardRecord> QuestMoneyRewardStorage;
        public static DB6Storage<QuestPackageItemRecord> QuestPackageItemStorage;
        public static DB6Storage<QuestSortRecord> QuestSortStorage;
        public static DB6Storage<QuestV2Record> QuestV2Storage;
        public static DB6Storage<QuestXPRecord> QuestXPStorage;
        public static DB6Storage<RandPropPointsRecord> RandPropPointsStorage;
        public static DB6Storage<RewardPackRecord> RewardPackStorage;
        public static DB6Storage<RewardPackXItemRecord> RewardPackXItemStorage;
        public static DB6Storage<RulesetItemUpgradeRecord> RulesetItemUpgradeStorage;
        public static DB6Storage<ScalingStatDistributionRecord> ScalingStatDistributionStorage;
        public static DB6Storage<ScenarioRecord> ScenarioStorage;
        public static DB6Storage<ScenarioStepRecord> ScenarioStepStorage;
        //public static DB6Storage<SceneScriptRecord> SceneScriptStorage;
        public static DB6Storage<SceneScriptPackageRecord> SceneScriptPackageStorage;
        public static DB6Storage<SkillLineRecord> SkillLineStorage;
        public static DB6Storage<SkillLineAbilityRecord> SkillLineAbilityStorage;
        public static DB6Storage<SkillRaceClassInfoRecord> SkillRaceClassInfoStorage;
        public static DB6Storage<SoundKitRecord> SoundKitStorage;
        public static DB6Storage<SpecializationSpellsRecord> SpecializationSpellsStorage;
        public static DB6Storage<SpellRecord> SpellStorage;
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
        public static DB6Storage<SpellEffectScalingRecord> SpellEffectScalingStorage;
        public static DB6Storage<SpellEquippedItemsRecord> SpellEquippedItemsStorage;
        public static DB6Storage<SpellFocusObjectRecord> SpellFocusObjectStorage;
        public static DB6Storage<SpellInterruptsRecord> SpellInterruptsStorage;
        public static DB6Storage<SpellItemEnchantmentRecord> SpellItemEnchantmentStorage;
        public static DB6Storage<SpellItemEnchantmentConditionRecord> SpellItemEnchantmentConditionStorage;
        public static DB6Storage<SpellLearnSpellRecord> SpellLearnSpellStorage;
        public static DB6Storage<SpellLevelsRecord> SpellLevelsStorage;
        public static DB6Storage<SpellMiscRecord> SpellMiscStorage;
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
        public static DB6Storage<UnitPowerBarRecord> UnitPowerBarStorage;
        public static DB6Storage<VehicleRecord> VehicleStorage;
        public static DB6Storage<VehicleSeatRecord> VehicleSeatStorage;
        public static DB6Storage<WMOAreaTableRecord> WMOAreaTableStorage;
        public static DB6Storage<WorldEffectRecord> WorldEffectStorage;
        public static DB6Storage<WorldMapAreaRecord> WorldMapAreaStorage;
        public static DB6Storage<WorldMapOverlayRecord> WorldMapOverlayStorage;
        public static DB6Storage<WorldMapTransformsRecord> WorldMapTransformsStorage;
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
        public static GameTable<GtHonorLevelRecord> HonorLevelGameTable;
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
                default:
                    break;
            }

            return 0.0f;
        }
        #endregion
    }
}
