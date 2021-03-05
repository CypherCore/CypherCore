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

using Framework.Constants;
using Framework.Database;
using Framework.GameMath;
using Game.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Game.DataStorage
{
    public class DB2Manager : Singleton<DB2Manager>
    {
        private DB2Manager()
        {
            for (uint i = 0; i < (int)Class.Max; ++i)
            {
                _powersByClass[i] = new uint[(int)PowerType.Max];

                for (uint j = 0; j < (int)PowerType.Max; ++j)
                    _powersByClass[i][j] = (uint)PowerType.Max;
            }

            for (uint i = 0; i < (int)Locale.Total + 1; ++i)
                _nameValidators[i] = new List<string>();

            for (var i = 0; i < (int)Locale.Total; ++i)
            {
                _hotfixBlob[i] = new Dictionary<(uint tableHas, int recordId), byte[]>();
                _hotfixOptionalData[i] = new MultiMap<(uint tableHas, int recordId), HotfixOptionalData>();
            }
        }

        public void LoadStores()
        {
            foreach (var areaGroupMember in CliDB.AreaGroupMemberStorage.Values)
                _areaGroupMembers.Add(areaGroupMember.AreaGroupID, areaGroupMember.AreaID);

            CliDB.AreaGroupMemberStorage.Clear();

            foreach (var artifactPower in CliDB.ArtifactPowerStorage.Values)
                _artifactPowers.Add(artifactPower.ArtifactID, artifactPower);

            foreach (var artifactPowerLink in CliDB.ArtifactPowerLinkStorage.Values)
            {
                _artifactPowerLinks.Add(artifactPowerLink.PowerA, artifactPowerLink.PowerB);
                _artifactPowerLinks.Add(artifactPowerLink.PowerB, artifactPowerLink.PowerA);
            }

            CliDB.ArtifactPowerLinkStorage.Clear();

            foreach (var artifactPowerRank in CliDB.ArtifactPowerRankStorage.Values)
                _artifactPowerRanks[Tuple.Create(artifactPowerRank.ArtifactPowerID, artifactPowerRank.RankIndex)] = artifactPowerRank;

            CliDB.ArtifactPowerRankStorage.Clear();

            foreach (var azeriteEmpoweredItem in CliDB.AzeriteEmpoweredItemStorage.Values)
                _azeriteEmpoweredItems[azeriteEmpoweredItem.ItemID] = azeriteEmpoweredItem;

            CliDB.AzeriteEmpoweredItemStorage.Clear();

            foreach (var azeriteEssencePower in CliDB.AzeriteEssencePowerStorage.Values)
                _azeriteEssencePowersByIdAndRank[((uint)azeriteEssencePower.AzeriteEssenceID, (uint)azeriteEssencePower.Tier)] = azeriteEssencePower;

            CliDB.AzeriteEssencePowerStorage.Clear();

            foreach (var azeriteItemMilestonePower in CliDB.AzeriteItemMilestonePowerStorage.Values)
                _azeriteItemMilestonePowers.Add(azeriteItemMilestonePower);

            _azeriteItemMilestonePowers = _azeriteItemMilestonePowers.OrderBy(p => p.RequiredLevel).ToList();

            uint azeriteEssenceSlot = 0;
            foreach (var azeriteItemMilestonePower in _azeriteItemMilestonePowers)
            {
                var type = (AzeriteItemMilestoneType)azeriteItemMilestonePower.Type;
                if (type == AzeriteItemMilestoneType.MajorEssence || type == AzeriteItemMilestoneType.MinorEssence)
                {
                    //ASSERT(azeriteEssenceSlot < MAX_AZERITE_ESSENCE_SLOT);
                    _azeriteItemMilestonePowerByEssenceSlot[azeriteEssenceSlot] = azeriteItemMilestonePower;
                    ++azeriteEssenceSlot;
                }
            }

            foreach (var azeritePowerSetMember in CliDB.AzeritePowerSetMemberStorage.Values)
                if (CliDB.AzeritePowerStorage.ContainsKey(azeritePowerSetMember.AzeritePowerID))
                    _azeritePowers.Add(azeritePowerSetMember.AzeritePowerSetID, azeritePowerSetMember);

            CliDB.AzeritePowerSetMemberStorage.Clear();

            foreach (var azeriteTierUnlock in CliDB.AzeriteTierUnlockStorage.Values)
            {
                var key = (azeriteTierUnlock.AzeriteTierUnlockSetID, (ItemContext)azeriteTierUnlock.ItemCreationContext);

                if (!_azeriteTierUnlockLevels.ContainsKey(key))
                    _azeriteTierUnlockLevels[key] = new byte[SharedConst.MaxAzeriteEmpoweredTier];

                _azeriteTierUnlockLevels[key][azeriteTierUnlock.Tier] = azeriteTierUnlock.AzeriteLevel;
            }

            var azeriteUnlockMappings = new MultiMap<uint, AzeriteUnlockMappingRecord>();
            foreach (var azeriteUnlockMapping in CliDB.AzeriteUnlockMappingStorage.Values)
                azeriteUnlockMappings.Add(azeriteUnlockMapping.AzeriteUnlockMappingSetID, azeriteUnlockMapping);

            foreach (var battlemaster in CliDB.BattlemasterListStorage.Values)
            {
                if (battlemaster.MaxLevel < battlemaster.MinLevel)
                {
                    Log.outError(LogFilter.ServerLoading, $"Battlemaster ({battlemaster.Id}) contains bad values for MinLevel ({battlemaster.MinLevel}) and MaxLevel ({battlemaster.MaxLevel}). Swapping values.");
                    MathFunctions.Swap(ref battlemaster.MaxLevel, ref battlemaster.MinLevel);
                }
                if (battlemaster.MaxPlayers < battlemaster.MinPlayers)
                {
                    Log.outError(LogFilter.ServerLoading, $"Battlemaster ({battlemaster.Id}) contains bad values for MinPlayers ({battlemaster.MinPlayers}) and MaxPlayers ({battlemaster.MaxPlayers}). Swapping values.");
                    MathFunctions.Swap(ref battlemaster.MaxPlayers, ref battlemaster.MinPlayers);
                }
            }

            var powers = new List<ChrClassesXPowerTypesRecord>();
            foreach (var chrClasses in CliDB.ChrClassesXPowerTypesStorage.Values)
                powers.Add(chrClasses);

            CliDB.ChrClassesXPowerTypesStorage.Clear();

            powers.Sort(new ChrClassesXPowerTypesRecordComparer());
            foreach (var power in powers)
            {
                uint index = 0;
                for (uint j = 0; j < (int)PowerType.Max; ++j)
                    if (_powersByClass[power.ClassID][j] != (int)PowerType.Max)
                        ++index;

                _powersByClass[power.ClassID][power.PowerType] = index;
            }

            foreach (var customizationChoice in CliDB.ChrCustomizationChoiceStorage.Values)
                _chrCustomizationChoicesByOption.Add(customizationChoice.ChrCustomizationOptionID, customizationChoice);

            var shapeshiftFormByModel = new MultiMap<uint, Tuple<uint, byte>>();
            var displayInfoByCustomizationChoice = new Dictionary<uint, ChrCustomizationDisplayInfoRecord>();

            // build shapeshift form model lookup
            foreach (var customizationElement in CliDB.ChrCustomizationElementStorage.Values)
            {
                var customizationDisplayInfo = CliDB.ChrCustomizationDisplayInfoStorage.LookupByKey(customizationElement.ChrCustomizationDisplayInfoID);
                if (customizationDisplayInfo != null)
                {
                    var customizationChoice = CliDB.ChrCustomizationChoiceStorage.LookupByKey(customizationElement.ChrCustomizationChoiceID);
                    if (customizationChoice != null)
                    {
                        displayInfoByCustomizationChoice[customizationElement.ChrCustomizationChoiceID] = customizationDisplayInfo;
                        var customizationOption = CliDB.ChrCustomizationOptionStorage.LookupByKey(customizationChoice.ChrCustomizationOptionID);
                        if (customizationOption != null)
                            shapeshiftFormByModel.Add(customizationOption.ChrModelID, Tuple.Create(customizationOption.Id, (byte)customizationDisplayInfo.ShapeshiftFormID));
                    }
                }
            }

            var customizationOptionsByModel = new MultiMap<uint, ChrCustomizationOptionRecord>();
            foreach (var customizationOption in CliDB.ChrCustomizationOptionStorage.Values)
                customizationOptionsByModel.Add(customizationOption.ChrModelID, customizationOption);

            foreach (var reqChoice in CliDB.ChrCustomizationReqChoiceStorage.Values)
            {
                var customizationChoice = CliDB.ChrCustomizationChoiceStorage.LookupByKey(reqChoice.ChrCustomizationChoiceID);
                if (customizationChoice != null)
                {
                    if (!_chrCustomizationRequiredChoices.ContainsKey(reqChoice.ChrCustomizationReqID))
                        _chrCustomizationRequiredChoices[reqChoice.ChrCustomizationReqID] = new MultiMap<uint, uint>();

                    _chrCustomizationRequiredChoices[reqChoice.ChrCustomizationReqID].Add(customizationChoice.ChrCustomizationOptionID, reqChoice.ChrCustomizationChoiceID);
                }
            }

            var parentRaces = new Dictionary<uint, uint>();
            foreach (var chrRace in CliDB.ChrRacesStorage.Values)
                if (chrRace.UnalteredVisualRaceID != 0)
                    parentRaces[(uint)chrRace.UnalteredVisualRaceID] = chrRace.Id;

            foreach (var raceModel in CliDB.ChrRaceXChrModelStorage.Values)
            {
                var model = CliDB.ChrModelStorage.LookupByKey(raceModel.ChrModelID);
                if (model != null)
                {
                    _chrModelsByRaceAndGender[Tuple.Create((byte)raceModel.ChrRacesID, (byte)model.Sex)] = model;

                    var customizationOptionsForModel = customizationOptionsByModel.LookupByKey(model.Id);
                    if (customizationOptionsForModel != null)
                    {
                        _chrCustomizationOptionsByRaceAndGender.AddRange(Tuple.Create((byte)raceModel.ChrRacesID, (byte)model.Sex), customizationOptionsForModel);

                        var parentRace = parentRaces.LookupByKey(raceModel.ChrRacesID);
                        if (parentRace != 0)
                            _chrCustomizationOptionsByRaceAndGender.AddRange(Tuple.Create((byte)parentRace, (byte)model.Sex), customizationOptionsForModel);
                    }

                    // link shapeshift displays to race/gender/form
                    foreach (var shapeshiftOptionsForModel in shapeshiftFormByModel.LookupByKey(model.Id))
                    {
                        var data = new ShapeshiftFormModelData();
                        data.OptionID = shapeshiftOptionsForModel.Item1;
                        data.Choices = _chrCustomizationChoicesByOption.LookupByKey(shapeshiftOptionsForModel.Item1);
                        if (!data.Choices.Empty())
                        {
                            for (var i = 0; i < data.Choices.Count; ++i)
                                data.Displays.Add(displayInfoByCustomizationChoice.LookupByKey(data.Choices[i].Id));
                        }

                        _chrCustomizationChoicesForShapeshifts[Tuple.Create((byte)raceModel.ChrRacesID, (byte)model.Sex, shapeshiftOptionsForModel.Item2)] = data;
                    }
                }
            }

            foreach (var chrSpec in CliDB.ChrSpecializationStorage.Values)
            {
                //ASSERT(chrSpec.ClassID < MAX_CLASSES);
                //ASSERT(chrSpec.OrderIndex < MAX_SPECIALIZATIONS);

                uint storageIndex = chrSpec.ClassID;
                if (chrSpec.Flags.HasAnyFlag(ChrSpecializationFlag.PetOverrideSpec))
                {
                    //ASSERT(!chrSpec.ClassID);
                    storageIndex = (int)Class.Max;
                }
                if (_chrSpecializationsByIndex[storageIndex] == null)
                    _chrSpecializationsByIndex[storageIndex] = new ChrSpecializationRecord[PlayerConst.MaxSpecializations];

                _chrSpecializationsByIndex[storageIndex][chrSpec.OrderIndex] = chrSpec;
            }

            foreach (var contentTuningXExpectedStat in CliDB.ContentTuningXExpectedStorage.Values)
            {
                var expectedStatMod = CliDB.ExpectedStatModStorage.LookupByKey(contentTuningXExpectedStat.ExpectedStatModID);
                if (expectedStatMod != null)
                    _expectedStatModsByContentTuning.Add(contentTuningXExpectedStat.ContentTuningID, expectedStatMod);
            }

            foreach (var curvePoint in CliDB.CurvePointStorage.Values)
            {
                if (CliDB.CurveStorage.ContainsKey(curvePoint.CurveID))
                    _curvePoints.Add(curvePoint.CurveID, curvePoint);
            }

            CliDB.CurvePointStorage.Clear();

            foreach (var key in _curvePoints.Keys.ToList())
                _curvePoints[key] = _curvePoints[key].OrderBy(point => point.OrderIndex).ToList();

            foreach (var emoteTextSound in CliDB.EmotesTextSoundStorage.Values)
                _emoteTextSounds[Tuple.Create((uint)emoteTextSound.EmotesTextId, emoteTextSound.RaceId, emoteTextSound.SexId, emoteTextSound.ClassId)] = emoteTextSound;

            CliDB.EmotesTextSoundStorage.Clear();

            foreach (var expectedStat in CliDB.ExpectedStatStorage.Values)
                _expectedStatsByLevel[Tuple.Create(expectedStat.Lvl, expectedStat.ExpansionID)] = expectedStat;

            CliDB.ExpectedStatStorage.Clear();

            foreach (var faction in CliDB.FactionStorage.Values)
                if (faction.ParentFactionID != 0)
                    _factionTeams.Add(faction.ParentFactionID, faction.Id);

            foreach (var gameObjectDisplayInfo in CliDB.GameObjectDisplayInfoStorage.Values)
            {
                if (gameObjectDisplayInfo.GeoBoxMax.X < gameObjectDisplayInfo.GeoBoxMin.X)
                    Extensions.Swap(ref gameObjectDisplayInfo.GeoBox[3], ref gameObjectDisplayInfo.GeoBox[0]);
                if (gameObjectDisplayInfo.GeoBoxMax.Y < gameObjectDisplayInfo.GeoBoxMin.Y)
                    Extensions.Swap(ref gameObjectDisplayInfo.GeoBox[4], ref gameObjectDisplayInfo.GeoBox[1]);
                if (gameObjectDisplayInfo.GeoBoxMax.Z < gameObjectDisplayInfo.GeoBoxMin.Z)
                    Extensions.Swap(ref gameObjectDisplayInfo.GeoBox[5], ref gameObjectDisplayInfo.GeoBox[2]);
            }

            foreach (var heirloom in CliDB.HeirloomStorage.Values)
                _heirlooms[heirloom.ItemID] = heirloom;

            CliDB.HeirloomStorage.Clear();

            foreach (var glyphBindableSpell in CliDB.GlyphBindableSpellStorage.Values)
                _glyphBindableSpells.Add((uint)glyphBindableSpell.GlyphPropertiesID, (uint)glyphBindableSpell.SpellID);

            CliDB.GlyphBindableSpellStorage.Clear();

            foreach (var glyphRequiredSpec in CliDB.GlyphRequiredSpecStorage.Values)
                _glyphRequiredSpecs.Add(glyphRequiredSpec.GlyphPropertiesID, glyphRequiredSpec.ChrSpecializationID);

            CliDB.GlyphRequiredSpecStorage.Clear();

            foreach (var bonus in CliDB.ItemBonusStorage.Values)
                _itemBonusLists.Add(bonus.ParentItemBonusListID, bonus);

            CliDB.ItemBonusStorage.Clear();

            foreach (var itemBonusListLevelDelta in CliDB.ItemBonusListLevelDeltaStorage.Values)
                _itemLevelDeltaToBonusListContainer[itemBonusListLevelDelta.ItemLevelDelta] = itemBonusListLevelDelta.Id;

            CliDB.ItemBonusListLevelDeltaStorage.Clear();

            foreach (var bonusTreeNode in CliDB.ItemBonusTreeNodeStorage.Values)
                _itemBonusTrees.Add(bonusTreeNode.ParentItemBonusTreeID, bonusTreeNode);

            CliDB.ItemBonusTreeNodeStorage.Clear();

            foreach (var itemChildEquipment in CliDB.ItemChildEquipmentStorage.Values)
            {
                //ASSERT(_itemChildEquipment.find(itemChildEquipment.ParentItemID) == _itemChildEquipment.end(), "Item must have max 1 child item.");
                _itemChildEquipment[itemChildEquipment.ParentItemID] = itemChildEquipment;
            }

            CliDB.ItemChildEquipmentStorage.Clear();

            foreach (var itemClass in CliDB.ItemClassStorage.Values)
            {
                //ASSERT(itemClass.ClassID < _itemClassByOldEnum.size());
                //ASSERT(!_itemClassByOldEnum[itemClass.ClassID]);
                _itemClassByOldEnum[itemClass.ClassID] = itemClass;
            }

            CliDB.ItemClassStorage.Clear();

            foreach (var itemCurrencyCost in CliDB.ItemCurrencyCostStorage.Values)
                _itemsWithCurrencyCost.Add(itemCurrencyCost.ItemID);

            CliDB.ItemCurrencyCostStorage.Clear();

            foreach (var condition in CliDB.ItemLimitCategoryConditionStorage.Values)
                _itemCategoryConditions.Add(condition.ParentItemLimitCategoryID, condition);

            foreach (var itemLevelSelectorQuality in CliDB.ItemLevelSelectorQualityStorage.Values)
                _itemLevelQualitySelectorQualities.Add((uint)itemLevelSelectorQuality.ParentILSQualitySetID, itemLevelSelectorQuality);

            CliDB.ItemLevelSelectorQualityStorage.Clear();

            foreach (var appearanceMod in CliDB.ItemModifiedAppearanceStorage.Values)
            {
                //ASSERT(appearanceMod.ItemID <= 0xFFFFFF);
                _itemModifiedAppearancesByItem[(uint)((int)appearanceMod.ItemID | (appearanceMod.ItemAppearanceModifierID << 24))] = appearanceMod;
            }

            foreach (var itemSetSpell in CliDB.ItemSetSpellStorage.Values)
                _itemSetSpells.Add(itemSetSpell.ItemSetID, itemSetSpell);

            CliDB.ItemSetSpellStorage.Clear();

            foreach (var itemSpecOverride in CliDB.ItemSpecOverrideStorage.Values)
                _itemSpecOverrides.Add(itemSpecOverride.ItemID, itemSpecOverride);

            CliDB.ItemSpecOverrideStorage.Clear();

            foreach (var itemBonusTreeAssignment in CliDB.ItemXBonusTreeStorage.Values)
                _itemToBonusTree.Add(itemBonusTreeAssignment.ItemID, itemBonusTreeAssignment.ItemBonusTreeID);

            CliDB.ItemXBonusTreeStorage.Clear();

            foreach (var pair in _azeriteEmpoweredItems)
                LoadAzeriteEmpoweredItemUnlockMappings(azeriteUnlockMappings, pair.Key);

            foreach (var entry in CliDB.MapDifficultyStorage.Values)
            {
                if (!_mapDifficulties.ContainsKey(entry.MapID))
                    _mapDifficulties[entry.MapID] = new Dictionary<uint, MapDifficultyRecord>();

                _mapDifficulties[entry.MapID][entry.DifficultyID] = entry;
            }
            _mapDifficulties[0][0] = _mapDifficulties[1][0]; // map 0 is missing from MapDifficulty.dbc so we cheat a bit

            CliDB.MapDifficultyStorage.Clear();

            var mapDifficultyConditions = new List<MapDifficultyXConditionRecord>();
            foreach (var mapDifficultyCondition in CliDB.MapDifficultyXConditionStorage.Values)
                mapDifficultyConditions.Add(mapDifficultyCondition);

            mapDifficultyConditions = mapDifficultyConditions.OrderBy(p => p.OrderIndex).ToList();

            foreach (var mapDifficultyCondition in mapDifficultyConditions)
            {
                var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(mapDifficultyCondition.PlayerConditionID);
                if (playerCondition != null)
                    _mapDifficultyConditions.Add(mapDifficultyCondition.MapDifficultyID, Tuple.Create(mapDifficultyCondition.Id, playerCondition));
            }

            foreach (var mount in CliDB.MountStorage.Values)
                _mountsBySpellId[mount.SourceSpellID] = mount;

            foreach (var mountTypeCapability in CliDB.MountTypeXCapabilityStorage.Values)
                _mountCapabilitiesByType.Add(mountTypeCapability.MountTypeID, mountTypeCapability);

            CliDB.MountTypeXCapabilityStorage.Clear();

            foreach (var key in _mountCapabilitiesByType.Keys)
                _mountCapabilitiesByType[key].Sort(new MountTypeXCapabilityRecordComparer());

            foreach (var mountDisplay in CliDB.MountXDisplayStorage.Values)
                _mountDisplays.Add(mountDisplay.MountID, mountDisplay);

            CliDB.MountXDisplayStorage.Clear();

            foreach (var entry in CliDB.NameGenStorage.Values)
            {
                if (!_nameGenData.ContainsKey(entry.RaceID))
                {
                    _nameGenData[entry.RaceID] = new List<NameGenRecord>[2];
                    for (var i = 0; i < 2; ++i)
                        _nameGenData[entry.RaceID][i] = new List<NameGenRecord>();
                }

                _nameGenData[entry.RaceID][entry.Sex].Add(entry);
            }

            CliDB.NameGenStorage.Clear();

            foreach (var namesProfanity in CliDB.NamesProfanityStorage.Values)
            {
                Cypher.Assert(namesProfanity.Language < (int)Locale.Total || namesProfanity.Language == -1);
                if (namesProfanity.Language != -1)
                    _nameValidators[namesProfanity.Language].Add(namesProfanity.Name);
                else
                    for (uint i = 0; i < (int)Locale.Total; ++i)
                    {
                        if (i == (int)Locale.None)
                            continue;

                        _nameValidators[i].Add(namesProfanity.Name);
                    }
            }

            CliDB.NamesProfanityStorage.Clear();

            foreach (var namesReserved in CliDB.NamesReservedStorage.Values)
                _nameValidators[(int)Locale.Total].Add(namesReserved.Name);

            CliDB.NamesReservedStorage.Clear();

            foreach (var namesReserved in CliDB.NamesReservedLocaleStorage.Values)
            {
                Cypher.Assert(!Convert.ToBoolean(namesReserved.LocaleMask & ~((1 << (int)Locale.Total) - 1)));
                for (var i = 0; i < (int)Locale.Total; ++i)
                {
                    if (i == (int)Locale.None)
                        continue;

                    if (Convert.ToBoolean(namesReserved.LocaleMask & (1 << i)))
                        _nameValidators[i].Add(namesReserved.Name);
                }
            }
            CliDB.NamesReservedLocaleStorage.Clear();

            foreach (var group in CliDB.PhaseXPhaseGroupStorage.Values)
            {
                var phase = CliDB.PhaseStorage.LookupByKey(group.PhaseId);
                if (phase != null)
                    _phasesByGroup.Add(group.PhaseGroupID, phase.Id);
            }
            CliDB.PhaseXPhaseGroupStorage.Clear();

            foreach (var powerType in CliDB.PowerTypeStorage.Values)
            {
                Cypher.Assert(powerType.PowerTypeEnum < PowerType.Max);

                _powerTypes[powerType.PowerTypeEnum] = powerType;
            }

            foreach (var pvpItem in CliDB.PvpItemStorage.Values)
                _pvpItemBonus[pvpItem.ItemID] = pvpItem.ItemLevelDelta;

            foreach (var talentUnlock in CliDB.PvpTalentSlotUnlockStorage.Values)
            {
                Cypher.Assert(talentUnlock.Slot < (1 << PlayerConst.MaxPvpTalentSlots));
                for (byte i = 0; i < PlayerConst.MaxPvpTalentSlots; ++i)
                {
                    if (Convert.ToBoolean(talentUnlock.Slot & (1 << i)))
                    {
                        Cypher.Assert(_pvpTalentSlotUnlock[i] == null);
                        _pvpTalentSlotUnlock[i] = talentUnlock;
                    }
                }
            }

            foreach (var questPackageItem in CliDB.QuestPackageItemStorage.Values)
            {
                if (!_questPackages.ContainsKey(questPackageItem.PackageID))
                    _questPackages[questPackageItem.PackageID] = Tuple.Create(new List<QuestPackageItemRecord>(), new List<QuestPackageItemRecord>());

                if (questPackageItem.DisplayType != QuestPackageFilter.Unmatched)
                    _questPackages[questPackageItem.PackageID].Item1.Add(questPackageItem);
                else
                    _questPackages[questPackageItem.PackageID].Item2.Add(questPackageItem);
            }

            CliDB.QuestPackageItemStorage.Clear();

            foreach (var rewardPackXCurrencyType in CliDB.RewardPackXCurrencyTypeStorage.Values)
                _rewardPackCurrencyTypes.Add(rewardPackXCurrencyType.RewardPackID, rewardPackXCurrencyType);

            CliDB.RewardPackXCurrencyTypeStorage.Clear();

            foreach (var rewardPackXItem in CliDB.RewardPackXItemStorage.Values)
                _rewardPackItems.Add(rewardPackXItem.RewardPackID, rewardPackXItem);

            CliDB.RewardPackXItemStorage.Clear();

            foreach (var skill in CliDB.SkillLineStorage.Values)
            {
                if (skill.ParentSkillLineID != 0)
                    _skillLinesByParentSkillLine.Add(skill.ParentSkillLineID, skill);
            }

            foreach (var skillLineAbility in CliDB.SkillLineAbilityStorage.Values)
                _skillLineAbilitiesBySkillupSkill.Add(skillLineAbility.SkillupSkillLineID != 0 ? skillLineAbility.SkillupSkillLineID : skillLineAbility.SkillLine, skillLineAbility);

            foreach (var entry in CliDB.SkillRaceClassInfoStorage.Values)
            {
                if (CliDB.SkillLineStorage.ContainsKey(entry.SkillID))
                    _skillRaceClassInfoBySkill.Add((uint)entry.SkillID, entry);
            }

            foreach (var specSpells in CliDB.SpecializationSpellsStorage.Values)
                _specializationSpellsBySpec.Add(specSpells.SpecID, specSpells);

            CliDB.SpecializationSpellsStorage.Clear();

            foreach (var specSetMember in CliDB.SpecSetMemberStorage.Values)
                _specsBySpecSet.Add(Tuple.Create((int)specSetMember.SpecSetID, (uint)specSetMember.ChrSpecializationID));

            foreach (var classOption in CliDB.SpellClassOptionsStorage.Values)
                _spellFamilyNames.Add(classOption.SpellClassSet);

            foreach (var ppmMod in CliDB.SpellProcsPerMinuteModStorage.Values)
                _spellProcsPerMinuteMods.Add(ppmMod.SpellProcsPerMinuteID, ppmMod);

            CliDB.SpellProcsPerMinuteModStorage.Clear();

            for (var i = 0; i < (int)Class.Max; ++i)
            {
                _talentsByPosition[i] = new List<TalentRecord>[PlayerConst.MaxTalentTiers][];
                for (var x = 0; x < PlayerConst.MaxTalentTiers; ++x)
                {
                    _talentsByPosition[i][x] = new List<TalentRecord>[PlayerConst.MaxTalentColumns];

                    for (var c = 0; c < PlayerConst.MaxTalentColumns; ++c)
                        _talentsByPosition[i][x][c] = new List<TalentRecord>();
                }
            }

            foreach (var talentInfo in CliDB.TalentStorage.Values)
            {
                //ASSERT(talentInfo.ClassID < MAX_CLASSES);
                //ASSERT(talentInfo.TierID < MAX_TALENT_TIERS, "MAX_TALENT_TIERS must be at least {0}", talentInfo.TierID);
                //ASSERT(talentInfo.ColumnIndex < MAX_TALENT_COLUMNS, "MAX_TALENT_COLUMNS must be at least {0}", talentInfo.ColumnIndex);
                _talentsByPosition[talentInfo.ClassID][talentInfo.TierID][talentInfo.ColumnIndex].Add(talentInfo);
            }

            foreach (var toy in CliDB.ToyStorage.Values)
                _toys.Add(toy.ItemID);

            CliDB.ToyStorage.Clear();

            foreach (var transmogSetItem in CliDB.TransmogSetItemStorage.Values)
            {
                var set = CliDB.TransmogSetStorage.LookupByKey(transmogSetItem.TransmogSetID);
                if (set == null)
                    continue;

                _transmogSetsByItemModifiedAppearance.Add(transmogSetItem.ItemModifiedAppearanceID, set);
                _transmogSetItemsByTransmogSet.Add(transmogSetItem.TransmogSetID, transmogSetItem);
            }

            CliDB.TransmogSetItemStorage.Clear();

            for (var i = 0; i < (int)UiMapSystem.Max; ++i)
            {
                _uiMapAssignmentByMap[i] = new MultiMap<int, UiMapAssignmentRecord>();
                _uiMapAssignmentByArea[i] = new MultiMap<int, UiMapAssignmentRecord>();
                _uiMapAssignmentByWmoDoodadPlacement[i] = new MultiMap<int, UiMapAssignmentRecord>();
                _uiMapAssignmentByWmoGroup[i] = new MultiMap<int, UiMapAssignmentRecord>();
            }

            var uiMapAssignmentByUiMap = new MultiMap<int, UiMapAssignmentRecord>();
            foreach (var uiMapAssignment in CliDB.UiMapAssignmentStorage.Values)
            {
                uiMapAssignmentByUiMap.Add(uiMapAssignment.UiMapID, uiMapAssignment);
                var uiMap = CliDB.UiMapStorage.LookupByKey(uiMapAssignment.UiMapID);
                if (uiMap != null)
                {
                    //ASSERT(uiMap.System < MAX_UI_MAP_SYSTEM, $"MAX_TALENT_TIERS must be at least {uiMap.System + 1}");
                    if (uiMapAssignment.MapID >= 0)
                        _uiMapAssignmentByMap[uiMap.System].Add(uiMapAssignment.MapID, uiMapAssignment);
                    if (uiMapAssignment.AreaID != 0)
                        _uiMapAssignmentByArea[uiMap.System].Add(uiMapAssignment.AreaID, uiMapAssignment);
                    if (uiMapAssignment.WmoDoodadPlacementID != 0)
                        _uiMapAssignmentByWmoDoodadPlacement[uiMap.System].Add(uiMapAssignment.WmoDoodadPlacementID, uiMapAssignment);
                    if (uiMapAssignment.WmoGroupID != 0)
                        _uiMapAssignmentByWmoGroup[uiMap.System].Add(uiMapAssignment.WmoGroupID, uiMapAssignment);
                }
            }

            var uiMapLinks = new Dictionary<Tuple<int, uint>, UiMapLinkRecord>();
            foreach (var uiMapLink in CliDB.UiMapLinkStorage.Values)
                uiMapLinks[Tuple.Create(uiMapLink.ParentUiMapID, (uint)uiMapLink.ChildUiMapID)] = uiMapLink;

            foreach (var uiMap in CliDB.UiMapStorage.Values)
            {
                var bounds = new UiMapBounds();
                var parentUiMap = CliDB.UiMapStorage.LookupByKey(uiMap.ParentUiMapID);
                if (parentUiMap != null)
                {
                    if (parentUiMap.GetFlags().HasAnyFlag(UiMapFlag.NoWorldPositions))
                        continue;
                    UiMapAssignmentRecord uiMapAssignment = null;
                    UiMapAssignmentRecord parentUiMapAssignment = null;
                    foreach (var uiMapAssignmentForMap in uiMapAssignmentByUiMap.LookupByKey(uiMap.Id))
                    {
                        if (uiMapAssignmentForMap.MapID >= 0 &&
                            uiMapAssignmentForMap.Region[1].X - uiMapAssignmentForMap.Region[0].X > 0 &&
                            uiMapAssignmentForMap.Region[1].Y - uiMapAssignmentForMap.Region[0].Y > 0)
                        {
                            uiMapAssignment = uiMapAssignmentForMap;
                            break;
                        }
                    }
                    if (uiMapAssignment == null)
                        continue;

                    foreach (var uiMapAssignmentForMap in uiMapAssignmentByUiMap.LookupByKey(uiMap.ParentUiMapID))
                    {
                        if (uiMapAssignmentForMap.MapID == uiMapAssignment.MapID &&
                            uiMapAssignmentForMap.Region[1].X - uiMapAssignmentForMap.Region[0].X > 0 &&
                            uiMapAssignmentForMap.Region[1].Y - uiMapAssignmentForMap.Region[0].Y > 0)
                        {
                            parentUiMapAssignment = uiMapAssignmentForMap;
                            break;
                        }
                    }
                    if (parentUiMapAssignment == null)
                        continue;

                    var parentXsize = parentUiMapAssignment.Region[1].X - parentUiMapAssignment.Region[0].X;
                    var parentYsize = parentUiMapAssignment.Region[1].Y - parentUiMapAssignment.Region[0].Y;
                    var bound0scale = (uiMapAssignment.Region[1].X - parentUiMapAssignment.Region[0].X) / parentXsize;
                    var bound0 = ((1.0f - bound0scale) * parentUiMapAssignment.UiMax.Y) + (bound0scale * parentUiMapAssignment.UiMin.Y);
                    var bound2scale = (uiMapAssignment.Region[0].X - parentUiMapAssignment.Region[0].X) / parentXsize;
                    var bound2 = ((1.0f - bound2scale) * parentUiMapAssignment.UiMax.Y) + (bound2scale * parentUiMapAssignment.UiMin.Y);
                    var bound1scale = (uiMapAssignment.Region[1].Y - parentUiMapAssignment.Region[0].Y) / parentYsize;
                    var bound1 = ((1.0f - bound1scale) * parentUiMapAssignment.UiMax.X) + (bound1scale * parentUiMapAssignment.UiMin.X);
                    var bound3scale = (uiMapAssignment.Region[0].Y - parentUiMapAssignment.Region[0].Y) / parentYsize;
                    var bound3 = ((1.0f - bound3scale) * parentUiMapAssignment.UiMax.X) + (bound3scale * parentUiMapAssignment.UiMin.X);
                    if ((bound3 - bound1) > 0.0f || (bound2 - bound0) > 0.0f)
                    {
                        bounds.Bounds[0] = bound0;
                        bounds.Bounds[1] = bound1;
                        bounds.Bounds[2] = bound2;
                        bounds.Bounds[3] = bound3;
                        bounds.IsUiAssignment = true;
                    }
                }

                var uiMapLink = uiMapLinks.LookupByKey(Tuple.Create(uiMap.ParentUiMapID, uiMap.Id));
                if (uiMapLink != null)
                {
                    bounds.IsUiAssignment = false;
                    bounds.IsUiLink = true;
                    bounds.Bounds[0] = uiMapLink.UiMin.Y;
                    bounds.Bounds[1] = uiMapLink.UiMin.X;
                    bounds.Bounds[2] = uiMapLink.UiMax.Y;
                    bounds.Bounds[3] = uiMapLink.UiMax.X;
                }

                _uiMapBounds[(int)uiMap.Id] = bounds;
            }

            foreach (var uiMapArt in CliDB.UiMapXMapArtStorage.Values)
                if (uiMapArt.PhaseID != 0)
                    _uiMapPhases.Add(uiMapArt.PhaseID);

            foreach (var entry in CliDB.WMOAreaTableStorage.Values)
                _wmoAreaTableLookup[Tuple.Create((short)entry.WmoID, (sbyte)entry.NameSetID, entry.WmoGroupID)] = entry;

            CliDB.WMOAreaTableStorage.Clear();
        }

        public IDB2Storage GetStorage(uint type)
        {
            return _storage.LookupByKey(type);
        }

        public void LoadHotfixData()
        {
            var oldMSTime = Time.GetMSTime();

            var result = DB.Hotfix.Query("SELECT Id, TableHash, RecordId, Status FROM hotfix_data ORDER BY Id");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 hotfix info entries.");
                return;
            }

            var deletedRecords = new Dictionary<(uint tableHash, int recordId), bool>();

            uint count = 0;
            do
            {
                var id = result.Read<int>(0);
                var tableHash = result.Read<uint>(1);
                var recordId = result.Read<int>(2);
                var status = (HotfixRecord.Status)result.Read<byte>(3);
                if (status == HotfixRecord.Status.Valid && !_storage.ContainsKey(tableHash))
                {
                    if (!_hotfixBlob.Any(p => p.ContainsKey((tableHash, recordId))))
                    {
                        Log.outError(LogFilter.Sql, $"Table `hotfix_data` references unknown DB2 store by hash 0x{tableHash:X} and has no reference to `hotfix_blob` in hotfix id {id} with RecordID: {recordId}");
                        continue;
                    }
                }

                
                var hotfixRecord = new HotfixRecord();
                hotfixRecord.TableHash = tableHash;
                hotfixRecord.RecordID = recordId;
                hotfixRecord.HotfixID = id;
                hotfixRecord.HotfixStatus = status;
                _hotfixData.Add(hotfixRecord);
                deletedRecords[(tableHash, recordId)] = status == HotfixRecord.Status.RecordRemoved;

                ++count;
            } while (result.NextRow());

            foreach (var itr in deletedRecords)
            {
                if (itr.Value)
                {
                    var store = _storage.LookupByKey(itr.Key.tableHash);
                    if (store != null)
                        store.EraseRecord((uint)itr.Key.recordId);
                }
            }

            Log.outInfo(LogFilter.Server, "Loaded {0} hotfix info entries in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadHotfixBlob(BitSet availableDb2Locales)
        {
            var oldMSTime = Time.GetMSTime();

            var result = DB.Hotfix.Query("SELECT TableHash, RecordId, locale, `Blob` FROM hotfix_blob ORDER BY TableHash");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 hotfix blob entries.");
                return;
            }

            uint hotfixBlobCount = 0;
            do
            {
                var tableHash = result.Read<uint>(0);
                var storeItr = _storage.LookupByKey(tableHash);
                if (storeItr != null)
                {
                    Log.outError(LogFilter.Sql, $"Table hash 0x{tableHash:X} points to a loaded DB2 store {nameof(storeItr)}, fill related table instead of hotfix_blob");
                    continue;
                }

                var recordId = result.Read<int>(1);
                var localeName = result.Read<string>(2);

                var locale = localeName.ToEnum<Locale>();
                if (!SharedConst.IsValidLocale(locale))
                {
                    Log.outError(LogFilter.Sql, $"`hotfix_blob` contains invalid locale: {localeName} at TableHash: 0x{tableHash:X} and RecordID: {recordId}");
                    continue;
                }

                if (!availableDb2Locales[(int)locale])
                    continue;

                _hotfixBlob[(int)locale][(tableHash, recordId)] = result.Read<byte[]>(3);
                hotfixBlobCount++;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {hotfixBlobCount} hotfix blob records in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public void LoadHotfixOptionalData(BitSet availableDb2Locales)
        {
            // Register allowed optional data keys
            _allowedHotfixOptionalData.Add(CliDB.BroadcastTextStorage.GetTableHash(), Tuple.Create(CliDB.TactKeyStorage.GetTableHash(), (AllowedHotfixOptionalData)ValidateBroadcastTextTactKeyOptionalData));

            var oldMSTime = Time.GetMSTime();

            var result = DB.Hotfix.Query("SELECT TableHash, RecordId, locale, `Key`, `Data` FROM hotfix_optional_data ORDER BY TableHash");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 hotfix optional data records.");
                return;
            }

            uint hotfixOptionalDataCount = 0;
            do
            {
                var tableHash = result.Read<uint>(0);
                var allowedHotfixes = _allowedHotfixOptionalData.LookupByKey(tableHash);
                if (allowedHotfixes.Empty())
                {
                    Log.outError(LogFilter.Sql, $"Table `hotfix_optional_data` references DB2 store by hash 0x{tableHash:X} that is not allowed to have optional data");
                    continue;
                }

                var recordId = result.Read<uint>(1);
                var db2storage = _storage.LookupByKey(tableHash);
                if (db2storage == null)
                {
                    Log.outError(LogFilter.Sql, $"Table `hotfix_optional_data` references unknown DB2 store by hash 0x{tableHash:X} with RecordID: {recordId}");
                    continue;
                }

                var localeName = result.Read<string>(2);
                var locale = localeName.ToEnum<Locale>();

                if (!SharedConst.IsValidLocale(locale))
                {
                    Log.outError(LogFilter.Sql, $"`hotfix_optional_data` contains invalid locale: {localeName} at TableHash: 0x{tableHash:X} and RecordID: {recordId}");
                    continue;
                }

                if (!availableDb2Locales[(int)locale])
                    continue;

                var optionalData = new HotfixOptionalData();
                optionalData.Key = result.Read<uint>(3);
                var allowedHotfixItr = allowedHotfixes.Find(v =>
                {
                    return v.Item1 == optionalData.Key;
                });
                if (allowedHotfixItr == null)
                {
                    Log.outError(LogFilter.Sql, $"Table `hotfix_optional_data` references non-allowed optional data key 0x{optionalData.Key:X} for DB2 store by hash 0x{tableHash:X} and RecordID: {recordId}");
                    continue;
                }

                optionalData.Data = result.Read<byte[]>(4);
                if (!allowedHotfixItr.Item2(optionalData.Data))
                {
                    Log.outError(LogFilter.Sql, $"Table `hotfix_optional_data` contains invalid data for DB2 store 0x{tableHash:X}, RecordID: {recordId} and Key: 0x{optionalData.Key:X}");
                    continue;
                }

                _hotfixOptionalData[(int)locale].Add((tableHash, (int)recordId), optionalData);
                hotfixOptionalDataCount++;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {hotfixOptionalDataCount} hotfix optional data records in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public bool ValidateBroadcastTextTactKeyOptionalData(byte[] data)
        {
            return data.Length == 8 + 16;
        }
        
        public uint GetHotfixCount() { return (uint)_hotfixData.Count; }
        
        public List<HotfixRecord> GetHotfixData() { return _hotfixData; }

        public byte[] GetHotfixBlobData(uint tableHash, int recordId, Locale locale)
        {
            Cypher.Assert(SharedConst.IsValidLocale(locale), "Locale {locale} is invalid locale");

            return _hotfixBlob[(int)locale].LookupByKey((tableHash, recordId));
        }

        public List<HotfixOptionalData> GetHotfixOptionalData(uint tableHash, uint recordId, Locale locale)
        {
            Cypher.Assert(SharedConst.IsValidLocale(locale), $"Locale {locale} is invalid locale");

            return _hotfixOptionalData[(int)locale].LookupByKey((tableHash, (int)recordId));
        }
        
        public uint GetEmptyAnimStateID()
        {
            return (uint)CliDB.AnimationDataStorage.Count;
        }

        public List<uint> GetAreasForGroup(uint areaGroupId)
        {
            return _areaGroupMembers.LookupByKey(areaGroupId);
        }

        public bool IsInArea(uint objectAreaId, uint areaId)
        {
            do
            {
                if (objectAreaId == areaId)
                    return true;

                var objectArea = CliDB.AreaTableStorage.LookupByKey(objectAreaId);
                if (objectArea == null)
                    break;

                objectAreaId = objectArea.ParentAreaID;
            } while (objectAreaId != 0);

            return false;
        }

        public List<ArtifactPowerRecord> GetArtifactPowers(byte artifactId)
        {
            return _artifactPowers.LookupByKey(artifactId);
        }

        public List<uint> GetArtifactPowerLinks(uint artifactPowerId)
        {
            return _artifactPowerLinks.LookupByKey(artifactPowerId);
        }

        public ArtifactPowerRankRecord GetArtifactPowerRank(uint artifactPowerId, byte rank)
        {
            return _artifactPowerRanks.LookupByKey(Tuple.Create(artifactPowerId, rank));
        }

        public AzeriteEmpoweredItemRecord GetAzeriteEmpoweredItem(uint itemId)
        {
            return _azeriteEmpoweredItems.LookupByKey(itemId);
        }

        public bool IsAzeriteItem(uint itemId)
        {
            return CliDB.AzeriteItemStorage.Any(pair => pair.Value.ItemID == itemId);
        }

        public AzeriteEssencePowerRecord GetAzeriteEssencePower(uint azeriteEssenceId, uint rank)
        {
            return _azeriteEssencePowersByIdAndRank.LookupByKey((azeriteEssenceId, rank));
        }

        public List<AzeriteItemMilestonePowerRecord> GetAzeriteItemMilestonePowers()
        {
            return _azeriteItemMilestonePowers;
        }

        public AzeriteItemMilestonePowerRecord GetAzeriteItemMilestonePower(int slot)
        {
            //ASSERT(slot < MAX_AZERITE_ESSENCE_SLOT, "Slot %u must be lower than MAX_AZERITE_ESSENCE_SLOT (%u)", uint32(slot), MAX_AZERITE_ESSENCE_SLOT);
            return _azeriteItemMilestonePowerByEssenceSlot[slot];
        }

        public List<AzeritePowerSetMemberRecord> GetAzeritePowers(uint itemId)
        {
            var azeriteEmpoweredItem = GetAzeriteEmpoweredItem(itemId);
            if (azeriteEmpoweredItem != null)
                return _azeritePowers.LookupByKey(azeriteEmpoweredItem.AzeritePowerSetID);

            return null;
        }

        public uint GetRequiredAzeriteLevelForAzeritePowerTier(uint azeriteUnlockSetId, ItemContext context, uint tier)
        {
            //ASSERT(tier < MAX_AZERITE_EMPOWERED_TIER);
            var levels = _azeriteTierUnlockLevels.LookupByKey((azeriteUnlockSetId, context));
            if (levels != null)
                return levels[tier];

            var azeriteTierUnlockSet = CliDB.AzeriteTierUnlockSetStorage.LookupByKey(azeriteUnlockSetId);
            if (azeriteTierUnlockSet != null && azeriteTierUnlockSet.Flags.HasAnyFlag(AzeriteTierUnlockSetFlags.Default))
            {
                levels = _azeriteTierUnlockLevels.LookupByKey((azeriteUnlockSetId, ItemContext.None));
                if (levels != null)
                    return levels[tier];
            }

            return (uint)CliDB.AzeriteLevelInfoStorage.Count;
        }

        public string GetBroadcastTextValue(BroadcastTextRecord broadcastText, Locale locale = Locale.enUS, Gender gender = Gender.Male, bool forceGender = false)
        {
            if ((gender == Gender.Female || gender == Gender.None) && (forceGender || broadcastText.Text1.HasString(SharedConst.DefaultLocale)))
            {
                if (broadcastText.Text1.HasString(locale))
                    return broadcastText.Text1[locale];

                return broadcastText.Text1[SharedConst.DefaultLocale];
            }

            if (broadcastText.Text.HasString(locale))
                return broadcastText.Text[locale];

            return broadcastText.Text[SharedConst.DefaultLocale];
        }

        public string GetClassName(Class class_, Locale locale = Locale.enUS)
        {
            var classEntry = CliDB.ChrClassesStorage.LookupByKey(class_);
            if (classEntry == null)
                return "";

            if (classEntry.Name[locale][0] != '\0')
                return classEntry.Name[locale];

            return classEntry.Name[Locale.enUS];
        }

        public uint GetPowerIndexByClass(PowerType powerType, Class classId)
        {
            return _powersByClass[(int)classId][(int)powerType];
        }

        public List<ChrCustomizationChoiceRecord> GetCustomiztionChoices(uint chrCustomizationOptionId)
        {
            return _chrCustomizationChoicesByOption.LookupByKey(chrCustomizationOptionId);
        }

        public List<ChrCustomizationOptionRecord> GetCustomiztionOptions(Race race, Gender gender)
        {
            return _chrCustomizationOptionsByRaceAndGender.LookupByKey(Tuple.Create((byte)race, (byte)gender));
        }

        public MultiMap<uint, uint> GetRequiredCustomizationChoices(uint chrCustomizationReqId)
        {
            return _chrCustomizationRequiredChoices.LookupByKey(chrCustomizationReqId);
        }

        public ChrModelRecord GetChrModel(Race race, Gender gender)
        {
            return _chrModelsByRaceAndGender.LookupByKey(Tuple.Create((byte)race, (byte)gender));
        }

        public string GetChrRaceName(Race race, Locale locale = Locale.enUS)
        {
            var raceEntry = CliDB.ChrRacesStorage.LookupByKey(race);
            if (raceEntry == null)
                return "";

            if (raceEntry.Name[locale][0] != '\0')
                return raceEntry.Name[locale];

            return raceEntry.Name[Locale.enUS];
        }

        public ChrSpecializationRecord GetChrSpecializationByIndex(Class class_, uint index)
        {
            return _chrSpecializationsByIndex[(int)class_][index];
        }

        public ChrSpecializationRecord GetDefaultChrSpecializationForClass(Class class_)
        {
            return GetChrSpecializationByIndex(class_, PlayerConst.InitialSpecializationIndex);
        }

        public ContentTuningLevels? GetContentTuningData(uint contentTuningId, uint replacementConditionMask, bool forItem = false)
        {
            var contentTuning = CliDB.ContentTuningStorage.LookupByKey(contentTuningId);
            if (contentTuning == null)
                return null;

            if (forItem && contentTuning.GetFlags().HasFlag(ContentTuningFlag.DisabledForItem))
                return null;

            int getLevelAdjustment(ContentTuningCalcType type) => type switch
            {
                ContentTuningCalcType.PlusOne => 1,
                ContentTuningCalcType.PlusMaxLevelForExpansion => (int)Global.ObjectMgr.GetMaxLevelForExpansion((Expansion)WorldConfig.GetUIntValue(WorldCfg.Expansion)),
                _ => 0
            };

            var levels = new ContentTuningLevels();
            levels.MinLevel = (short)(contentTuning.MinLevel + getLevelAdjustment((ContentTuningCalcType)contentTuning.MinLevelType));
            levels.MaxLevel = (short)(contentTuning.MaxLevel + getLevelAdjustment((ContentTuningCalcType)contentTuning.MaxLevelType));
            levels.MinLevelWithDelta = (short)Math.Clamp(levels.MinLevel + contentTuning.TargetLevelDelta, 1, SharedConst.MaxLevel);
            levels.MaxLevelWithDelta = (short)Math.Clamp(levels.MaxLevel + contentTuning.TargetLevelMaxDelta, 1, SharedConst.MaxLevel);

            // clamp after calculating levels with delta (delta can bring "overflown" level back into correct range)
            levels.MinLevel = (short)Math.Clamp((int)levels.MinLevel, 1, SharedConst.MaxLevel);
            levels.MaxLevel = (short)Math.Clamp((int)levels.MaxLevel, 1, SharedConst.MaxLevel);

            if (contentTuning.TargetLevelMin != 0)
                levels.TargetLevelMin = (short)contentTuning.TargetLevelMin;
            else
                levels.TargetLevelMin = levels.MinLevelWithDelta;

            if (contentTuning.TargetLevelMax != 0)
                levels.TargetLevelMax = (short)contentTuning.TargetLevelMax;
            else
                levels.TargetLevelMax = levels.MaxLevelWithDelta;

            return levels;
        }

        public string GetCreatureFamilyPetName(CreatureFamily petfamily, Locale locale)
        {
            if (petfamily == CreatureFamily.None)
                return null;

            var petFamily = CliDB.CreatureFamilyStorage.LookupByKey(petfamily);
            if (petFamily == null)
                return "";

            return petFamily.Name[locale][0] != '\0' ? petFamily.Name[locale] : "";
        }

        private static CurveInterpolationMode DetermineCurveType(CurveRecord curve, List<CurvePointRecord> points)
        {
            switch (curve.Type)
            {
                case 1:
                    return points.Count < 4 ? CurveInterpolationMode.Cosine : CurveInterpolationMode.CatmullRom;
                case 2:
                    {
                        switch (points.Count)
                        {
                            case 1:
                                return CurveInterpolationMode.Constant;
                            case 2:
                                return CurveInterpolationMode.Linear;
                            case 3:
                                return CurveInterpolationMode.Bezier3;
                            case 4:
                                return CurveInterpolationMode.Bezier4;
                            default:
                                break;
                        }
                        return CurveInterpolationMode.Bezier;
                    }
                case 3:
                    return CurveInterpolationMode.Cosine;
                default:
                    break;
            }

            return points.Count != 1 ? CurveInterpolationMode.Linear : CurveInterpolationMode.Constant;
        }

        public float GetCurveValueAt(uint curveId, float x)
        {
            var points = _curvePoints.LookupByKey(curveId);
            if (points.Empty())
                return 0.0f;

            var curve = CliDB.CurveStorage.LookupByKey(curveId);
            switch (DetermineCurveType(curve, points))
            {
                case CurveInterpolationMode.Linear:
                    {
                        var pointIndex = 0;
                        while (pointIndex < points.Count && points[pointIndex].Pos.X <= x)
                            ++pointIndex;
                        if (pointIndex == 0)
                            return points[0].Pos.Y;
                        if (pointIndex >= points.Count)
                            return points.Last().Pos.Y;
                        var xDiff = points[pointIndex].Pos.X - points[pointIndex - 1].Pos.X;
                        if (xDiff == 0.0)
                            return points[pointIndex].Pos.Y;
                        return (((x - points[pointIndex - 1].Pos.X) / xDiff) * (points[pointIndex].Pos.Y - points[pointIndex - 1].Pos.Y)) + points[pointIndex - 1].Pos.Y;
                    }
                case CurveInterpolationMode.Cosine:
                    {
                        var pointIndex = 0;
                        while (pointIndex < points.Count && points[pointIndex].Pos.X <= x)
                            ++pointIndex;
                        if (pointIndex == 0)
                            return points[0].Pos.Y;
                        if (pointIndex >= points.Count)
                            return points.Last().Pos.Y;
                        var xDiff = points[pointIndex].Pos.X - points[pointIndex - 1].Pos.X;
                        if (xDiff == 0.0)
                            return points[pointIndex].Pos.Y;
                        return (float)((points[pointIndex].Pos.Y - points[pointIndex - 1].Pos.Y) * (1.0f - Math.Cos((x - points[pointIndex - 1].Pos.X) / xDiff * Math.PI)) * 0.5f) + points[pointIndex - 1].Pos.Y;
                    }
                case CurveInterpolationMode.CatmullRom:
                    {
                        var pointIndex = 1;
                        while (pointIndex < points.Count && points[pointIndex].Pos.X <= x)
                            ++pointIndex;
                        if (pointIndex == 1)
                            return points[1].Pos.Y;
                        if (pointIndex >= points.Count - 1)
                            return points[points.Count - 2].Pos.Y;
                        var xDiff = points[pointIndex].Pos.X - points[pointIndex - 1].Pos.X;
                        if (xDiff == 0.0)
                            return points[pointIndex].Pos.Y;

                        var mu = (x - points[pointIndex - 1].Pos.X) / xDiff;
                        var a0 = -0.5f * points[pointIndex - 2].Pos.Y + 1.5f * points[pointIndex - 1].Pos.Y - 1.5f * points[pointIndex].Pos.Y + 0.5f * points[pointIndex + 1].Pos.Y;
                        var a1 = points[pointIndex - 2].Pos.Y - 2.5f * points[pointIndex - 1].Pos.Y + 2.0f * points[pointIndex].Pos.Y - 0.5f * points[pointIndex + 1].Pos.Y;
                        var a2 = -0.5f * points[pointIndex - 2].Pos.Y + 0.5f * points[pointIndex].Pos.Y;
                        var a3 = points[pointIndex - 1].Pos.Y;

                        return a0 * mu * mu * mu + a1 * mu * mu + a2 * mu + a3;
                    }
                case CurveInterpolationMode.Bezier3:
                    {
                        var xDiff = points[2].Pos.X - points[0].Pos.X;
                        if (xDiff == 0.0)
                            return points[1].Pos.Y;
                        var mu = (x - points[0].Pos.X) / xDiff;
                        return ((1.0f - mu) * (1.0f - mu) * points[0].Pos.Y) + (1.0f - mu) * 2.0f * mu * points[1].Pos.Y + mu * mu * points[2].Pos.Y;
                    }
                case CurveInterpolationMode.Bezier4:
                    {
                        var xDiff = points[3].Pos.X - points[0].Pos.X;
                        if (xDiff == 0.0)
                            return points[1].Pos.Y;
                        var mu = (x - points[0].Pos.X) / xDiff;
                        return (1.0f - mu) * (1.0f - mu) * (1.0f - mu) * points[0].Pos.Y
                            + 3.0f * mu * (1.0f - mu) * (1.0f - mu) * points[1].Pos.Y
                            + 3.0f * mu * mu * (1.0f - mu) * points[2].Pos.Y
                            + mu * mu * mu * points[3].Pos.Y;
                    }
                case CurveInterpolationMode.Bezier:
                    {
                        var xDiff = points.Last().Pos.X - points[0].Pos.X;
                        if (xDiff == 0.0f)
                            return points.Last().Pos.Y;

                        var tmp = new float[points.Count];
                        for (var c = 0; c < points.Count; ++c)
                            tmp[c] = points[c].Pos.Y;

                        var mu = (x - points[0].Pos.X) / xDiff;
                        var i = points.Count - 1;
                        while (i > 0)
                        {
                            for (var k = 0; k < i; ++k)
                            {
                                var val = tmp[k] + mu * (tmp[k + 1] - tmp[k]);
                                tmp[k] = val;
                            }
                            --i;
                        }
                        return tmp[0];
                    }
                case CurveInterpolationMode.Constant:
                    return points[0].Pos.Y;
                default:
                    break;
            }

            return 0.0f;
        }

        public EmotesTextSoundRecord GetTextSoundEmoteFor(uint emote, Race race, Gender gender, Class class_)
        {
            var emoteTextSound = _emoteTextSounds.LookupByKey(Tuple.Create(emote, (byte)race, (byte)gender, (byte)class_));
            if (emoteTextSound != null)
                return emoteTextSound;

            emoteTextSound = _emoteTextSounds.LookupByKey(Tuple.Create(emote, (byte)race, (byte)gender, 0));
            if (emoteTextSound != null)
                return emoteTextSound;

            return null;
        }

        public float EvaluateExpectedStat(ExpectedStatType stat, uint level, int expansion, uint contentTuningId, Class unitClass)
        {
            var expectedStatRecord = _expectedStatsByLevel.LookupByKey(Tuple.Create(level, expansion));
            if (expectedStatRecord == null)
                expectedStatRecord = _expectedStatsByLevel.LookupByKey(Tuple.Create(level, -2));
            if (expectedStatRecord == null)
                return 1.0f;

            ExpectedStatModRecord classMod = null;
            switch (unitClass)
            {
                case Class.Warrior:
                    classMod = CliDB.ExpectedStatModStorage.LookupByKey(4);
                    break;
                case Class.Paladin:
                    classMod = CliDB.ExpectedStatModStorage.LookupByKey(2);
                    break;
                case Class.Rogue:
                    classMod = CliDB.ExpectedStatModStorage.LookupByKey(3);
                    break;
                case Class.Mage:
                    classMod = CliDB.ExpectedStatModStorage.LookupByKey(1);
                    break;
                default:
                    break;
            }

            var contentTuningMods = _expectedStatModsByContentTuning.LookupByKey(contentTuningId);
            var value = 0.0f;
            switch (stat)
            {
                case ExpectedStatType.CreatureHealth:
                    value = expectedStatRecord.CreatureHealth;
                    if (!contentTuningMods.Empty())
                        value *= contentTuningMods.Sum(expectedStatMod => expectedStatMod != null ? expectedStatMod.CreatureHealthMod : 1.0f);
                    if (classMod != null)
                        value *= classMod.CreatureHealthMod;
                    break;
                case ExpectedStatType.PlayerHealth:
                    value = expectedStatRecord.PlayerHealth;
                    if (!contentTuningMods.Empty())
                        value *= contentTuningMods.Sum(expectedStatMod => expectedStatMod != null ? expectedStatMod.PlayerHealthMod : 1.0f);
                    if (classMod != null)
                        value *= classMod.PlayerHealthMod;
                    break;
                case ExpectedStatType.CreatureAutoAttackDps:
                    value = expectedStatRecord.CreatureAutoAttackDps;
                    if (!contentTuningMods.Empty())
                        value *= contentTuningMods.Sum(expectedStatMod => expectedStatMod != null ? expectedStatMod.CreatureAutoAttackDPSMod : 1.0f);
                    if (classMod != null)
                        value *= classMod.CreatureAutoAttackDPSMod;
                    break;
                case ExpectedStatType.CreatureArmor:
                    value = expectedStatRecord.CreatureArmor;
                    if (!contentTuningMods.Empty())
                        value *= contentTuningMods.Sum(expectedStatMod => expectedStatMod != null ? expectedStatMod.CreatureArmorMod : 1.0f);
                    if (classMod != null)
                        value *= classMod.CreatureArmorMod;
                    break;
                case ExpectedStatType.PlayerMana:
                    value = expectedStatRecord.PlayerMana;
                    if (!contentTuningMods.Empty())
                        value *= contentTuningMods.Sum(expectedStatMod => expectedStatMod != null ? expectedStatMod.PlayerManaMod : 1.0f);
                    if (classMod != null)
                        value *= classMod.PlayerManaMod;
                    break;
                case ExpectedStatType.PlayerPrimaryStat:
                    value = expectedStatRecord.PlayerPrimaryStat;
                    if (!contentTuningMods.Empty())
                        value *= contentTuningMods.Sum(expectedStatMod => expectedStatMod != null ? expectedStatMod.PlayerPrimaryStatMod : 1.0f);
                    if (classMod != null)
                        value *= classMod.PlayerPrimaryStatMod;
                    break;
                case ExpectedStatType.PlayerSecondaryStat:
                    value = expectedStatRecord.PlayerSecondaryStat;
                    if (!contentTuningMods.Empty())
                        value *= contentTuningMods.Sum(expectedStatMod => expectedStatMod != null ? expectedStatMod.PlayerSecondaryStatMod : 1.0f);
                    if (classMod != null)
                        value *= classMod.PlayerSecondaryStatMod;
                    break;
                case ExpectedStatType.ArmorConstant:
                    value = expectedStatRecord.ArmorConstant;
                    if (!contentTuningMods.Empty())
                        value *= contentTuningMods.Sum(expectedStatMod => expectedStatMod != null ? expectedStatMod.ArmorConstantMod : 1.0f);
                    if (classMod != null)
                        value *= classMod.ArmorConstantMod;
                    break;
                case ExpectedStatType.None:
                    break;
                case ExpectedStatType.CreatureSpellDamage:
                    value = expectedStatRecord.CreatureSpellDamage;
                    if (!contentTuningMods.Empty())
                        value *= contentTuningMods.Sum(expectedStatMod => expectedStatMod != null ? expectedStatMod.CreatureSpellDamageMod : 1.0f);
                    if (classMod != null)
                        value *= classMod.CreatureSpellDamageMod;
                    break;
                default:
                    break;
            }
            return value;
        }

        public List<uint> GetFactionTeamList(uint faction)
        {
            return _factionTeams.LookupByKey(faction);
        }

        public uint GetGlobalCurveId(GlobalCurve globalCurveType)
        {
            foreach (var globalCurveEntry in CliDB.GlobalCurveStorage.Values)
                if (globalCurveEntry.Type == globalCurveType)
                    return globalCurveEntry.CurveID;

            return 0;
        }
        
        public List<uint> GetGlyphBindableSpells(uint glyphPropertiesId)
        {
            return _glyphBindableSpells.LookupByKey(glyphPropertiesId);
        }

        public List<uint> GetGlyphRequiredSpecs(uint glyphPropertiesId)
        {
            return _glyphRequiredSpecs.LookupByKey(glyphPropertiesId);
        }

        public HeirloomRecord GetHeirloomByItemId(uint itemId)
        {
            return _heirlooms.LookupByKey(itemId);
        }

        public List<ItemBonusRecord> GetItemBonusList(uint bonusListId)
        {
            return _itemBonusLists.LookupByKey(bonusListId);
        }

        public uint GetItemBonusListForItemLevelDelta(short delta)
        {
            return _itemLevelDeltaToBonusListContainer.LookupByKey(delta);
        }

        private void VisitItemBonusTree(uint itemBonusTreeId, bool visitChildren, Action<ItemBonusTreeNodeRecord> visitor)
        {
            var bonusTreeNodeList = _itemBonusTrees.LookupByKey(itemBonusTreeId);
            if (bonusTreeNodeList.Empty())
                return;

            foreach (var bonusTreeNode in bonusTreeNodeList)
            {
                visitor(bonusTreeNode);
                if (visitChildren && bonusTreeNode.ChildItemBonusTreeID != 0)
                    VisitItemBonusTree(bonusTreeNode.ChildItemBonusTreeID, true, visitor);
            }
        }

        public List<uint> GetDefaultItemBonusTree(uint itemId, ItemContext itemContext)
        {
            var bonusListIDs = new List<uint>();

            var proto = CliDB.ItemSparseStorage.LookupByKey(itemId);
            if (proto == null)
                return bonusListIDs;

            var itemIdRange = _itemToBonusTree.LookupByKey(itemId);
            if (itemIdRange == null)
                return bonusListIDs;

            ushort itemLevelSelectorId = 0;
            foreach (var itemBonusTreeId in itemIdRange)
            {
                uint matchingNodes = 0;
                VisitItemBonusTree(itemBonusTreeId, false, bonusTreeNode =>
                {
                    if ((ItemContext)bonusTreeNode.ItemContext == ItemContext.None || itemContext == (ItemContext)bonusTreeNode.ItemContext)
                        ++matchingNodes;
                });

                if (matchingNodes != 1)
                    continue;

                VisitItemBonusTree(itemBonusTreeId, true, bonusTreeNode =>
                {
                    var requiredContext = (ItemContext)bonusTreeNode.ItemContext != ItemContext.ForceToNone ? (ItemContext)bonusTreeNode.ItemContext : ItemContext.None;
                    if ((ItemContext)bonusTreeNode.ItemContext != ItemContext.None && itemContext != requiredContext)
                        return;

                    if (bonusTreeNode.ChildItemBonusListID != 0)
                    {
                        bonusListIDs.Add(bonusTreeNode.ChildItemBonusListID);
                    }
                    else if (bonusTreeNode.ChildItemLevelSelectorID != 0)
                    {
                        itemLevelSelectorId = bonusTreeNode.ChildItemLevelSelectorID;
                    }
                });
            }
            var selector = CliDB.ItemLevelSelectorStorage.LookupByKey(itemLevelSelectorId);
            if (selector != null)
            {
                var delta = (short)(selector.MinItemLevel - proto.ItemLevel);

                var bonus = GetItemBonusListForItemLevelDelta(delta);
                if (bonus != 0)
                    bonusListIDs.Add(bonus);

                var selectorQualitySet = CliDB.ItemLevelSelectorQualitySetStorage.LookupByKey(selector.ItemLevelSelectorQualitySetID);
                if (selectorQualitySet != null)
                {
                    var itemSelectorQualities = _itemLevelQualitySelectorQualities.LookupByKey(selector.ItemLevelSelectorQualitySetID);
                    if (itemSelectorQualities != null)
                    {
                        var quality = ItemQuality.Uncommon;
                        if (selector.MinItemLevel >= selectorQualitySet.IlvlEpic)
                            quality = ItemQuality.Epic;
                        else if (selector.MinItemLevel >= selectorQualitySet.IlvlRare)
                            quality = ItemQuality.Rare;

                        var itemSelectorQuality = itemSelectorQualities.Find(p => p.Quality < (sbyte)quality);

                        if (itemSelectorQuality != null)
                            bonusListIDs.Add(itemSelectorQuality.QualityItemBonusListID);
                    }
                }

                var azeriteUnlockMapping = _azeriteUnlockMappings.LookupByKey((proto.Id, itemContext));
                if (azeriteUnlockMapping != null)
                {
                    switch (proto.inventoryType)
                    {
                        case InventoryType.Head:
                            bonusListIDs.Add(azeriteUnlockMapping.ItemBonusListHead);
                            break;
                        case InventoryType.Shoulders:
                            bonusListIDs.Add(azeriteUnlockMapping.ItemBonusListShoulders);
                            break;
                        case InventoryType.Chest:
                        case InventoryType.Robe:
                            bonusListIDs.Add(azeriteUnlockMapping.ItemBonusListChest);
                            break;
                    }
                }
            }

            return bonusListIDs;
        }

        private void LoadAzeriteEmpoweredItemUnlockMappings(MultiMap<uint, AzeriteUnlockMappingRecord> azeriteUnlockMappingsBySet, uint itemId)
        {
            var itemIdRange = _itemToBonusTree.LookupByKey(itemId);
            if (itemIdRange == null)
                return;

            foreach (var itemTreeItr in itemIdRange)
            {
                VisitItemBonusTree(itemTreeItr, true, bonusTreeNode =>
                {
                    if (bonusTreeNode.ChildItemBonusListID == 0 && bonusTreeNode.ChildItemLevelSelectorID != 0)
                    {
                        var selector = CliDB.ItemLevelSelectorStorage.LookupByKey(bonusTreeNode.ChildItemLevelSelectorID);
                        if (selector == null)
                            return;

                        var azeriteUnlockMappings = azeriteUnlockMappingsBySet.LookupByKey(selector.AzeriteUnlockMappingSet);
                        if (azeriteUnlockMappings != null)
                        {
                            AzeriteUnlockMappingRecord selectedAzeriteUnlockMapping = null;
                            foreach (var azeriteUnlockMapping in azeriteUnlockMappings)
                            {
                                if (azeriteUnlockMapping.ItemLevel > selector.MinItemLevel ||
                                    (selectedAzeriteUnlockMapping != null && selectedAzeriteUnlockMapping.ItemLevel > azeriteUnlockMapping.ItemLevel))
                                    continue;

                                selectedAzeriteUnlockMapping = azeriteUnlockMapping;
                            }

                            if (selectedAzeriteUnlockMapping != null)
                                _azeriteUnlockMappings[(itemId, (ItemContext)bonusTreeNode.ItemContext)] = selectedAzeriteUnlockMapping;
                        }
                    }
                });
            }
        }

        public ItemChildEquipmentRecord GetItemChildEquipment(uint itemId)
        {
            return _itemChildEquipment.LookupByKey(itemId);
        }

        public ItemClassRecord GetItemClassByOldEnum(ItemClass itemClass)
        {
            return _itemClassByOldEnum[(int)itemClass];
        }

        public List<ItemLimitCategoryConditionRecord> GetItemLimitCategoryConditions(uint categoryId)
        {
            return _itemCategoryConditions.LookupByKey(categoryId);
        }

        public uint GetItemDisplayId(uint itemId, uint appearanceModId)
        {
            var modifiedAppearance = GetItemModifiedAppearance(itemId, appearanceModId);
            if (modifiedAppearance != null)
            {
                var itemAppearance = CliDB.ItemAppearanceStorage.LookupByKey(modifiedAppearance.ItemAppearanceID);
                if (itemAppearance != null)
                    return itemAppearance.ItemDisplayInfoID;
            }

            return 0;
        }

        public ItemModifiedAppearanceRecord GetItemModifiedAppearance(uint itemId, uint appearanceModId)
        {
            var itemModifiedAppearance = _itemModifiedAppearancesByItem.LookupByKey(itemId | (appearanceModId << 24));
            if (itemModifiedAppearance != null)
                return itemModifiedAppearance;

            // Fall back to unmodified appearance
            if (appearanceModId != 0)
            {
                itemModifiedAppearance = _itemModifiedAppearancesByItem.LookupByKey(itemId);
                if (itemModifiedAppearance != null)
                    return itemModifiedAppearance;
            }

            return null;
        }

        public ItemModifiedAppearanceRecord GetDefaultItemModifiedAppearance(uint itemId)
        {
            return _itemModifiedAppearancesByItem.LookupByKey(itemId);
        }

        public List<ItemSetSpellRecord> GetItemSetSpells(uint itemSetId)
        {
            return _itemSetSpells.LookupByKey(itemSetId);
        }

        public List<ItemSpecOverrideRecord> GetItemSpecOverrides(uint itemId)
        {
            return _itemSpecOverrides.LookupByKey(itemId);
        }

        public LFGDungeonsRecord GetLfgDungeon(uint mapId, Difficulty difficulty)
        {
            foreach (var dungeon in CliDB.LFGDungeonsStorage.Values)
                if (dungeon.MapID == mapId && dungeon.DifficultyID == difficulty)
                    return dungeon;

            return null;
        }

        public uint GetDefaultMapLight(uint mapId)
        {
            foreach (var light in CliDB.LightStorage.Values.Reverse())
            {
                if (light.ContinentID == mapId && light.GameCoords.X == 0.0f && light.GameCoords.Y == 0.0f && light.GameCoords.Z == 0.0f)
                    return light.Id;
            }

            return 0;
        }

        public uint GetLiquidFlags(uint liquidType)
        {
            var liq = CliDB.LiquidTypeStorage.LookupByKey(liquidType);
            if (liq != null)
                return 1u << liq.SoundBank;

            return 0;
        }

        public MapDifficultyRecord GetDefaultMapDifficulty(uint mapId)
        {
            var NotUsed = Difficulty.None;
            return GetDefaultMapDifficulty(mapId, ref NotUsed);
        }
        public MapDifficultyRecord GetDefaultMapDifficulty(uint mapId, ref Difficulty difficulty)
        {
            var dicMapDiff = _mapDifficulties.LookupByKey(mapId);
            if (dicMapDiff == null)
                return null;

            if (dicMapDiff.Empty())
                return null;

            foreach (var pair in dicMapDiff)
            {
                var difficultyEntry = CliDB.DifficultyStorage.LookupByKey(pair.Key);
                if (difficultyEntry == null)
                    continue;

                if (difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.Default))
                {
                    difficulty = (Difficulty)pair.Key;
                    return pair.Value;
                }
            }

            difficulty = (Difficulty)dicMapDiff.First().Key;

            return dicMapDiff.First().Value;
        }

        public MapDifficultyRecord GetMapDifficultyData(uint mapId, Difficulty difficulty)
        {
            var dictionaryMapDiff = _mapDifficulties.LookupByKey(mapId);
            if (dictionaryMapDiff == null)
                return null;

            var mapDifficulty = dictionaryMapDiff.LookupByKey(difficulty);
            if (mapDifficulty == null)
                return null;

            return mapDifficulty;
        }

        public MapDifficultyRecord GetDownscaledMapDifficultyData(uint mapId, ref Difficulty difficulty)
        {
            var diffEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);
            if (diffEntry == null)
                return GetDefaultMapDifficulty(mapId, ref difficulty);

            var tmpDiff = difficulty;
            var mapDiff = GetMapDifficultyData(mapId, tmpDiff);
            while (mapDiff == null)
            {
                tmpDiff = (Difficulty)diffEntry.FallbackDifficultyID;
                diffEntry = CliDB.DifficultyStorage.LookupByKey(tmpDiff);
                if (diffEntry == null)
                    return GetDefaultMapDifficulty(mapId, ref difficulty);

                // pull new data
                mapDiff = GetMapDifficultyData(mapId, tmpDiff); // we are 10 normal or 25 normal
            }

            difficulty = tmpDiff;
            return mapDiff;
        }

        public List<Tuple<uint, PlayerConditionRecord>> GetMapDifficultyConditions(uint mapDifficultyId)
        {
            return _mapDifficultyConditions.LookupByKey(mapDifficultyId);
        }
        
        public MountRecord GetMount(uint spellId)
        {
            return _mountsBySpellId.LookupByKey(spellId);
        }

        public MountRecord GetMountById(uint id)
        {
            return CliDB.MountStorage.LookupByKey(id);
        }

        public List<MountTypeXCapabilityRecord> GetMountCapabilities(uint mountType)
        {
            return _mountCapabilitiesByType.LookupByKey(mountType);
        }

        public List<MountXDisplayRecord> GetMountDisplays(uint mountId)
        {
            return _mountDisplays.LookupByKey(mountId);
        }

        public string GetNameGenEntry(uint race, uint gender)
        {
            Cypher.Assert(gender < (int)Gender.None);
            var listNameGen = _nameGenData.LookupByKey(race);
            if (listNameGen == null)
                return "";

            if (listNameGen[gender].Empty())
                return "";

            return listNameGen[gender].SelectRandom().Name;
        }

        public ResponseCodes ValidateName(string name, Locale locale)
        {
            foreach (var testName in _nameValidators[(int)locale])
                if (testName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return ResponseCodes.CharNameProfane;

            // regexes at TOTAL_LOCALES are loaded from NamesReserved which is not locale specific
            foreach (var testName in _nameValidators[(int)Locale.Total])
                if (testName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return ResponseCodes.CharNameReserved;

            return ResponseCodes.CharNameSuccess;
        }

        public uint GetNumTalentsAtLevel(uint level, Class playerClass)
        {
            var numTalentsAtLevel = CliDB.NumTalentsAtLevelStorage.LookupByKey(level);
            if (numTalentsAtLevel == null)
                numTalentsAtLevel = CliDB.NumTalentsAtLevelStorage.LastOrDefault().Value;
            if (numTalentsAtLevel != null)
            {
                switch (playerClass)
                {
                    case Class.Deathknight:
                        return numTalentsAtLevel.NumTalentsDeathKnight;
                    case Class.DemonHunter:
                        return numTalentsAtLevel.NumTalentsDemonHunter;
                    default:
                        return numTalentsAtLevel.NumTalents;
                }
            }
            return 0;
        }

        public PvpDifficultyRecord GetBattlegroundBracketByLevel(uint mapid, uint level)
        {
            PvpDifficultyRecord maxEntry = null;              // used for level > max listed level case
            foreach (var entry in CliDB.PvpDifficultyStorage.Values)
            {
                // skip unrelated and too-high brackets
                if (entry.MapID != mapid || entry.MinLevel > level)
                    continue;

                // exactly fit
                if (entry.MaxLevel >= level)
                    return entry;

                // remember for possible out-of-range case (search higher from existed)
                if (maxEntry == null || maxEntry.MaxLevel < entry.MaxLevel)
                    maxEntry = entry;
            }

            return maxEntry;
        }

        public PvpDifficultyRecord GetBattlegroundBracketById(uint mapid, BattlegroundBracketId id)
        {
            foreach (var entry in CliDB.PvpDifficultyStorage.Values)
                if (entry.MapID == mapid && entry.GetBracketId() == id)
                    return entry;

            return null;
        }

        public uint GetRequiredLevelForPvpTalentSlot(byte slot, Class class_)
        {
            Cypher.Assert(slot < PlayerConst.MaxPvpTalentSlots);
            if (_pvpTalentSlotUnlock[slot] != null)
            {
                switch (class_)
                {
                    case Class.Deathknight:
                        return _pvpTalentSlotUnlock[slot].DeathKnightLevelRequired;
                    case Class.DemonHunter:
                        return _pvpTalentSlotUnlock[slot].DemonHunterLevelRequired;
                    default:
                        break;
                }
                return _pvpTalentSlotUnlock[slot].LevelRequired;
            }

            return 0;
        }

        public int GetPvpTalentNumSlotsAtLevel(uint level, Class class_)
        {
            var slots = 0;
            for (byte slot = 0; slot < PlayerConst.MaxPvpTalentSlots; ++slot)
                if (level >= GetRequiredLevelForPvpTalentSlot(slot, class_))
                    ++slots;
            return slots;
        }

        public List<QuestPackageItemRecord> GetQuestPackageItems(uint questPackageID)
        {
            if( _questPackages.ContainsKey(questPackageID))
                return _questPackages[questPackageID].Item1;

            return null;
        }

        public List<QuestPackageItemRecord> GetQuestPackageItemsFallback(uint questPackageID)
        {
            return _questPackages.LookupByKey(questPackageID).Item2;
        }

        public uint GetQuestUniqueBitFlag(uint questId)
        {
            var v2 = CliDB.QuestV2Storage.LookupByKey(questId);
            if (v2 == null)
                return 0;

            return v2.UniqueBitFlag;
        }

        public List<uint> GetPhasesForGroup(uint group)
        {
            return _phasesByGroup.LookupByKey(group);
        }

        public PowerTypeRecord GetPowerTypeEntry(PowerType power)
        {
            if (!_powerTypes.ContainsKey(power))
                return null;

            return _powerTypes[power];
        }

        public PowerTypeRecord GetPowerTypeByName(string name)
        {
            foreach (var powerType in CliDB.PowerTypeStorage.Values)
            {
                var powerName = powerType.NameGlobalStringTag;
                if (powerName.ToLower() == name)
                    return powerType;

                powerName = powerName.Replace("_", "");
                if (powerName == name)
                    return powerType;
            }

            return null;
        }

        public byte GetPvpItemLevelBonus(uint itemId)
        {
            return _pvpItemBonus.LookupByKey(itemId);
        }

        public List<RewardPackXCurrencyTypeRecord> GetRewardPackCurrencyTypesByRewardID(uint rewardPackID)
        {
            return _rewardPackCurrencyTypes.LookupByKey(rewardPackID);
        }

        public List<RewardPackXItemRecord> GetRewardPackItemsByRewardID(uint rewardPackID)
        {
            return _rewardPackItems.LookupByKey(rewardPackID);
        }

        public ShapeshiftFormModelData GetShapeshiftFormModelData(Race race, Gender gender, ShapeShiftForm form)
        {
            return _chrCustomizationChoicesForShapeshifts.LookupByKey(Tuple.Create((byte)race, (byte)gender, (byte)form));
        }
        
        public List<SkillLineRecord> GetSkillLinesForParentSkill(uint parentSkillId)
        {
            return _skillLinesByParentSkillLine.LookupByKey(parentSkillId);
        }

        public List<SkillLineAbilityRecord> GetSkillLineAbilitiesBySkill(uint skillId)
        {
            return _skillLineAbilitiesBySkillupSkill.LookupByKey(skillId);
        }

        public SkillRaceClassInfoRecord GetSkillRaceClassInfo(uint skill, Race race, Class class_)
        {
            var bounds = _skillRaceClassInfoBySkill.LookupByKey(skill);
            foreach (var record in bounds)
            {
                if (record.RaceMask != 0 && !Convert.ToBoolean(record.RaceMask & SharedConst.GetMaskForRace(race)))
                    continue;
                if (record.ClassMask != 0 && !Convert.ToBoolean(record.ClassMask & (1 << ((byte)class_ - 1))))
                    continue;

                return record;
            }

            return null;
        }

        public List<SpecializationSpellsRecord> GetSpecializationSpells(uint specId)
        {
            return _specializationSpellsBySpec.LookupByKey(specId);
        }

        public bool IsSpecSetMember(int specSetId, uint specId)
        {
            return _specsBySpecSet.Contains(Tuple.Create(specSetId, specId));
        }

        private bool IsValidSpellFamiliyName(SpellFamilyNames family)
        {
            return _spellFamilyNames.Contains((byte)family);
        }

        public List<SpellProcsPerMinuteModRecord> GetSpellProcsPerMinuteMods(uint spellprocsPerMinuteId)
        {
            return _spellProcsPerMinuteMods.LookupByKey(spellprocsPerMinuteId);
        }

        public List<TalentRecord> GetTalentsByPosition(Class class_, uint tier, uint column)
        {
            return _talentsByPosition[(int)class_][tier][column];
        }

        public bool IsTotemCategoryCompatibleWith(uint itemTotemCategoryId, uint requiredTotemCategoryId)
        {
            if (requiredTotemCategoryId == 0)
                return true;
            if (itemTotemCategoryId == 0)
                return false;

            var itemEntry = CliDB.TotemCategoryStorage.LookupByKey(itemTotemCategoryId);
            if (itemEntry == null)
                return false;
            var reqEntry = CliDB.TotemCategoryStorage.LookupByKey(requiredTotemCategoryId);
            if (reqEntry == null)
                return false;

            if (itemEntry.TotemCategoryType != reqEntry.TotemCategoryType)
                return false;

            return (itemEntry.TotemCategoryMask & reqEntry.TotemCategoryMask) == reqEntry.TotemCategoryMask;
        }

        public bool IsToyItem(uint toy)
        {
            return _toys.Contains(toy);
        }

        public List<TransmogSetRecord> GetTransmogSetsForItemModifiedAppearance(uint itemModifiedAppearanceId)
        {
            return _transmogSetsByItemModifiedAppearance.LookupByKey(itemModifiedAppearanceId);
        }

        public List<TransmogSetItemRecord> GetTransmogSetItems(uint transmogSetId)
        {
            return _transmogSetItemsByTransmogSet.LookupByKey(transmogSetId);
        }

        private static bool CheckUiMapAssignmentStatus(float x, float y, float z, int mapId, int areaId, int wmoDoodadPlacementId, int wmoGroupId, UiMapAssignmentRecord uiMapAssignment, out UiMapAssignmentStatus status)
        {
            status = new UiMapAssignmentStatus();
            status.UiMapAssignment = uiMapAssignment;
            // x,y not in region
            if (x < uiMapAssignment.Region[0].X || x > uiMapAssignment.Region[1].X || y < uiMapAssignment.Region[0].Y || y > uiMapAssignment.Region[1].Y)
            {
                float xDiff, yDiff;
                if (x >= uiMapAssignment.Region[0].X)
                {
                    xDiff = 0.0f;
                    if (x > uiMapAssignment.Region[1].X)
                        xDiff = x - uiMapAssignment.Region[0].X;
                }
                else
                    xDiff = uiMapAssignment.Region[0].X - x;

                if (y >= uiMapAssignment.Region[0].Y)
                {
                    yDiff = 0.0f;
                    if (y > uiMapAssignment.Region[1].Y)
                        yDiff = y - uiMapAssignment.Region[0].Y;
                }
                else
                    yDiff = uiMapAssignment.Region[0].Y - y;

                status.Outside.DistanceToRegionEdgeSquared = xDiff * xDiff + yDiff * yDiff;
            }
            else
            {
                status.Inside.DistanceToRegionCenterSquared =
                    (x - (uiMapAssignment.Region[0].X + uiMapAssignment.Region[1].X) * 0.5f) * (x - (uiMapAssignment.Region[0].X + uiMapAssignment.Region[1].X) * 0.5f)
                    + (y - (uiMapAssignment.Region[0].Y + uiMapAssignment.Region[1].Y) * 0.5f) * (y - (uiMapAssignment.Region[0].Y + uiMapAssignment.Region[1].Y) * 0.5f);
                status.Outside.DistanceToRegionEdgeSquared = 0.0f;
            }

            // z not in region
            if (z < uiMapAssignment.Region[0].Z || z > uiMapAssignment.Region[1].Z)
            {
                if (z < uiMapAssignment.Region[1].Z)
                {
                    if (z < uiMapAssignment.Region[0].Z)
                        status.Outside.DistanceToRegionBottom = Math.Min(uiMapAssignment.Region[0].Z - z, 10000.0f);
                }
                else
                    status.Outside.DistanceToRegionTop = Math.Min(z - uiMapAssignment.Region[1].Z, 10000.0f);
            }
            else
            {
                status.Outside.DistanceToRegionTop = 0.0f;
                status.Outside.DistanceToRegionBottom = 0.0f;
                status.Inside.DistanceToRegionBottom = Math.Min(uiMapAssignment.Region[0].Z - z, 10000.0f);
            }

            if (areaId != 0 && uiMapAssignment.AreaID != 0)
            {
                sbyte areaPriority = 0;
                if (areaId != 0)
                {
                    while (areaId != uiMapAssignment.AreaID)
                    {
                        var areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);
                        if (areaEntry != null)
                        {
                            areaId = areaEntry.ParentAreaID;
                            ++areaPriority;
                        }
                        else
                            return false;
                    }
                }
                else
                    return false;

                status.AreaPriority = areaPriority;
            }

            if (mapId >= 0 && uiMapAssignment.MapID >= 0)
            {
                if (mapId != uiMapAssignment.MapID)
                {
                    var mapEntry = CliDB.MapStorage.LookupByKey(mapId);
                    if (mapEntry != null)
                    {
                        if (mapEntry.ParentMapID == uiMapAssignment.MapID)
                            status.MapPriority = 1;
                        else if (mapEntry.CosmeticParentMapID == uiMapAssignment.MapID)
                            status.MapPriority = 2;
                        else
                            return false;
                    }
                    else
                        return false;
                }
                else
                    status.MapPriority = 0;
            }

            if (wmoGroupId != 0 || wmoDoodadPlacementId != 0)
            {
                if (uiMapAssignment.WmoGroupID != 0 || uiMapAssignment.WmoDoodadPlacementID != 0)
                {
                    var hasDoodadPlacement = false;
                    if (wmoDoodadPlacementId != 0 && uiMapAssignment.WmoDoodadPlacementID != 0)
                    {
                        if (wmoDoodadPlacementId != uiMapAssignment.WmoDoodadPlacementID)
                            return false;

                        hasDoodadPlacement = true;
                    }

                    if (wmoGroupId != 0 && uiMapAssignment.WmoGroupID != 0)
                    {
                        if (wmoGroupId != uiMapAssignment.WmoGroupID)
                            return false;

                        if (hasDoodadPlacement)
                            status.WmoPriority = 0;
                        else
                            status.WmoPriority = 2;
                    }
                    else if (hasDoodadPlacement)
                        status.WmoPriority = 1;
                }
            }

            return true;
        }

        private UiMapAssignmentRecord FindNearestMapAssignment(float x, float y, float z, int mapId, int areaId, int wmoDoodadPlacementId, int wmoGroupId, UiMapSystem system)
        {
            var nearestMapAssignment = new UiMapAssignmentStatus();
            var iterateUiMapAssignments = new Action<MultiMap<int, UiMapAssignmentRecord>, int>((assignments, id) =>
            {
                foreach (var assignment in assignments.LookupByKey(id))
                {
                    UiMapAssignmentStatus status;
                    if (CheckUiMapAssignmentStatus(x, y, z, mapId, areaId, wmoDoodadPlacementId, wmoGroupId, assignment, out status))
                        if (status < nearestMapAssignment)
                            nearestMapAssignment = status;
                }
            });

            iterateUiMapAssignments(_uiMapAssignmentByWmoGroup[(int)system], wmoGroupId);
            iterateUiMapAssignments(_uiMapAssignmentByWmoDoodadPlacement[(int)system], wmoDoodadPlacementId);

            var areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);
            while (areaEntry != null)
            {
                iterateUiMapAssignments(_uiMapAssignmentByArea[(int)system], (int)areaEntry.Id);
                areaEntry = CliDB.AreaTableStorage.LookupByKey(areaEntry.ParentAreaID);
            }

            if (mapId > 0)
            {
                var mapEntry = CliDB.MapStorage.LookupByKey(mapId);
                if (mapEntry != null)
                {
                    iterateUiMapAssignments(_uiMapAssignmentByMap[(int)system], (int)mapEntry.Id);
                    if (mapEntry.ParentMapID >= 0)
                        iterateUiMapAssignments(_uiMapAssignmentByMap[(int)system], mapEntry.ParentMapID);
                    if (mapEntry.CosmeticParentMapID >= 0)
                        iterateUiMapAssignments(_uiMapAssignmentByMap[(int)system], mapEntry.CosmeticParentMapID);
                }
            }

            return nearestMapAssignment.UiMapAssignment;
        }

        private Vector2 CalculateGlobalUiMapPosition(int uiMapID, Vector2 uiPosition)
        {
            var uiMap = CliDB.UiMapStorage.LookupByKey(uiMapID);
            while (uiMap != null)
            {
                if (uiMap.Type <= UiMapType.Continent)
                    break;

                var bounds = _uiMapBounds.LookupByKey(uiMap.Id);
                if (bounds == null || !bounds.IsUiAssignment)
                    break;

                uiPosition.X = ((1.0f - uiPosition.X) * bounds.Bounds[1]) + (bounds.Bounds[3] * uiPosition.X);
                uiPosition.Y = ((1.0f - uiPosition.Y) * bounds.Bounds[0]) + (bounds.Bounds[2] * uiPosition.Y);

                uiMap = CliDB.UiMapStorage.LookupByKey(uiMap.ParentUiMapID);
            }

            return uiPosition;
        }

        public bool GetUiMapPosition(float x, float y, float z, int mapId, int areaId, int wmoDoodadPlacementId, int wmoGroupId, UiMapSystem system, bool local, out Vector2 newPos)
        {
            int throwaway;
            return GetUiMapPosition(x, y, z, mapId, areaId, wmoDoodadPlacementId, wmoGroupId, system, local, out throwaway, out newPos);
        }

        public bool GetUiMapPosition(float x, float y, float z, int mapId, int areaId, int wmoDoodadPlacementId, int wmoGroupId, UiMapSystem system, bool local, out int uiMapId)
        {
            Vector2 throwaway;
            return GetUiMapPosition(x, y, z, mapId, areaId, wmoDoodadPlacementId, wmoGroupId, system, local, out uiMapId, out throwaway);
        }

        public bool GetUiMapPosition(float x, float y, float z, int mapId, int areaId, int wmoDoodadPlacementId, int wmoGroupId, UiMapSystem system, bool local, out int uiMapId, out Vector2 newPos)
        {
            uiMapId = -1;
            newPos = new Vector2();

            var uiMapAssignment = FindNearestMapAssignment(x, y, z, mapId, areaId, wmoDoodadPlacementId, wmoGroupId, system);
            if (uiMapAssignment == null)
                return false;

            uiMapId = uiMapAssignment.UiMapID;

            var relativePosition = new Vector2(0.5f, 0.5f);
            var regionSize = new Vector2(uiMapAssignment.Region[1].X - uiMapAssignment.Region[0].X, uiMapAssignment.Region[1].Y - uiMapAssignment.Region[0].Y);
            if (regionSize.X > 0.0f)
                relativePosition.X = (x - uiMapAssignment.Region[0].X) / regionSize.X;
            if (regionSize.Y > 0.0f)
                relativePosition.Y = (y - uiMapAssignment.Region[0].Y) / regionSize.Y;

            // x any y are swapped
            var uiPosition = new Vector2(((1.0f - (1.0f - relativePosition.Y)) * uiMapAssignment.UiMin.X) + ((1.0f - relativePosition.Y) * uiMapAssignment.UiMax.X), ((1.0f - (1.0f - relativePosition.X)) * uiMapAssignment.UiMin.Y) + ((1.0f - relativePosition.X) * uiMapAssignment.UiMax.Y));

            if (!local)
                uiPosition = CalculateGlobalUiMapPosition(uiMapAssignment.UiMapID, uiPosition);

            newPos = uiPosition;
            return true;
        }

        public void Zone2MapCoordinates(uint areaId, ref float x, ref float y)
        {
            var areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);
            if (areaEntry == null)
                return;

            foreach (var assignment in _uiMapAssignmentByArea[(int)UiMapSystem.World].LookupByKey(areaId))
            {
                if (assignment.MapID >= 0 && assignment.MapID != areaEntry.ContinentID)
                    continue;

                var tmpY = (y - assignment.UiMax.Y) / (assignment.UiMin.Y - assignment.UiMax.Y);
                var tmpX = (x - assignment.UiMax.X) / (assignment.UiMin.X - assignment.UiMax.X);
                x = assignment.Region[0].X + tmpY * (assignment.Region[1].X - assignment.Region[0].X);
                y = assignment.Region[0].Y + tmpX * (assignment.Region[1].Y - assignment.Region[0].Y);
                break;
            }
        }

        public void Map2ZoneCoordinates(int areaId, ref float x, ref float y)
        {
            Vector2 zoneCoords;
            if (!GetUiMapPosition(x, y, 0.0f, -1, areaId, 0, 0, UiMapSystem.World, true, out zoneCoords))
                return;

            x = zoneCoords.Y * 100.0f;
            y = zoneCoords.X * 100.0f;
        }

        public bool IsUiMapPhase(int phaseId)
        {
            return _uiMapPhases.Contains(phaseId);
        }

        public WMOAreaTableRecord GetWMOAreaTable(int rootId, int adtId, int groupId)
        {
            var wmoAreaTable = _wmoAreaTableLookup.LookupByKey(Tuple.Create((short)rootId, (sbyte)adtId, groupId));
            if (wmoAreaTable != null)
                return wmoAreaTable;

            return null;
        }

        public bool HasItemCurrencyCost(uint itemId) { return _itemsWithCurrencyCost.Contains(itemId); }

        public Dictionary<uint, Dictionary<uint, MapDifficultyRecord>> GetMapDifficulties() { return _mapDifficulties; }

        public void AddDB2<T>(uint tableHash, DB6Storage<T> store) where T : new()
        {
            _storage[tableHash] = store;
        }

        private delegate bool AllowedHotfixOptionalData(byte[] data);

        private Dictionary<uint, IDB2Storage> _storage = new Dictionary<uint, IDB2Storage>();
        private List<HotfixRecord> _hotfixData = new List<HotfixRecord>();
        private Dictionary<(uint tableHash, int recordId), byte[]>[] _hotfixBlob = new Dictionary<(uint tableHash, int recordId), byte[]>[(int)Locale.Total];
        private MultiMap<uint, Tuple<uint, AllowedHotfixOptionalData>> _allowedHotfixOptionalData = new MultiMap<uint, Tuple<uint, AllowedHotfixOptionalData>>();
        private MultiMap<(uint tableHash, int recordId), HotfixOptionalData>[]_hotfixOptionalData = new MultiMap<(uint tableHash, int recordId), HotfixOptionalData>[(int)Locale.Total];

        private MultiMap<uint, uint> _areaGroupMembers = new MultiMap<uint, uint>();
        private MultiMap<uint, ArtifactPowerRecord> _artifactPowers = new MultiMap<uint, ArtifactPowerRecord>();
        private MultiMap<uint, uint> _artifactPowerLinks = new MultiMap<uint, uint>();
        private Dictionary<Tuple<uint, byte>, ArtifactPowerRankRecord> _artifactPowerRanks = new Dictionary<Tuple<uint, byte>, ArtifactPowerRankRecord>();
        private Dictionary<uint, AzeriteEmpoweredItemRecord> _azeriteEmpoweredItems = new Dictionary<uint, AzeriteEmpoweredItemRecord>();
        private Dictionary<(uint azeriteEssenceId, uint rank), AzeriteEssencePowerRecord> _azeriteEssencePowersByIdAndRank = new Dictionary<(uint azeriteEssenceId, uint rank), AzeriteEssencePowerRecord>();
        private List<AzeriteItemMilestonePowerRecord> _azeriteItemMilestonePowers = new List<AzeriteItemMilestonePowerRecord>();
        private AzeriteItemMilestonePowerRecord[] _azeriteItemMilestonePowerByEssenceSlot = new AzeriteItemMilestonePowerRecord[SharedConst.MaxAzeriteEssenceSlot];
        private MultiMap<uint, AzeritePowerSetMemberRecord> _azeritePowers = new MultiMap<uint, AzeritePowerSetMemberRecord>();
        private Dictionary<(uint azeriteUnlockSetId, ItemContext itemContext), byte[]> _azeriteTierUnlockLevels = new Dictionary<(uint azeriteUnlockSetId, ItemContext itemContext), byte[]>();
        private Dictionary<(uint itemId, ItemContext itemContext), AzeriteUnlockMappingRecord> _azeriteUnlockMappings = new Dictionary<(uint itemId, ItemContext itemContext), AzeriteUnlockMappingRecord>();
        private uint[][] _powersByClass = new uint[(int)Class.Max][];
        private MultiMap<uint, ChrCustomizationChoiceRecord> _chrCustomizationChoicesByOption = new MultiMap<uint, ChrCustomizationChoiceRecord>();
        private Dictionary<Tuple<byte, byte>, ChrModelRecord> _chrModelsByRaceAndGender = new Dictionary<Tuple<byte, byte>, ChrModelRecord>();
        private Dictionary<Tuple<byte, byte, byte>, ShapeshiftFormModelData> _chrCustomizationChoicesForShapeshifts = new Dictionary<Tuple<byte, byte, byte>, ShapeshiftFormModelData>();
        private MultiMap<Tuple<byte, byte>, ChrCustomizationOptionRecord> _chrCustomizationOptionsByRaceAndGender = new MultiMap<Tuple<byte, byte>, ChrCustomizationOptionRecord>();
        private Dictionary<uint, MultiMap<uint, uint>> _chrCustomizationRequiredChoices = new Dictionary<uint, MultiMap<uint, uint>>();
        private ChrSpecializationRecord[][] _chrSpecializationsByIndex = new ChrSpecializationRecord[(int)Class.Max + 1][];
        private MultiMap<uint, CurvePointRecord> _curvePoints = new MultiMap<uint, CurvePointRecord>();
        private Dictionary<Tuple<uint, byte, byte, byte>, EmotesTextSoundRecord> _emoteTextSounds = new Dictionary<Tuple<uint, byte, byte, byte>, EmotesTextSoundRecord>();
        private Dictionary<Tuple<uint, int>, ExpectedStatRecord> _expectedStatsByLevel = new Dictionary<Tuple<uint, int>, ExpectedStatRecord>();
        private MultiMap<uint, ExpectedStatModRecord> _expectedStatModsByContentTuning = new MultiMap<uint, ExpectedStatModRecord>();
        private MultiMap<uint, uint> _factionTeams = new MultiMap<uint, uint>();
        private Dictionary<uint, HeirloomRecord> _heirlooms = new Dictionary<uint, HeirloomRecord>();
        private MultiMap<uint, uint> _glyphBindableSpells = new MultiMap<uint, uint>();
        private MultiMap<uint, uint> _glyphRequiredSpecs = new MultiMap<uint, uint>();
        private MultiMap<uint, ItemBonusRecord> _itemBonusLists = new MultiMap<uint, ItemBonusRecord>();
        private Dictionary<short, uint> _itemLevelDeltaToBonusListContainer = new Dictionary<short, uint>();
        private MultiMap<uint, ItemBonusTreeNodeRecord> _itemBonusTrees = new MultiMap<uint, ItemBonusTreeNodeRecord>();
        private Dictionary<uint, ItemChildEquipmentRecord> _itemChildEquipment = new Dictionary<uint, ItemChildEquipmentRecord>();
        private ItemClassRecord[] _itemClassByOldEnum = new ItemClassRecord[19];
        private List<uint> _itemsWithCurrencyCost = new List<uint>();
        private MultiMap<uint, ItemLimitCategoryConditionRecord>  _itemCategoryConditions = new MultiMap<uint, ItemLimitCategoryConditionRecord>();
        private MultiMap<uint, ItemLevelSelectorQualityRecord> _itemLevelQualitySelectorQualities = new MultiMap<uint, ItemLevelSelectorQualityRecord>();
        private Dictionary<uint, ItemModifiedAppearanceRecord> _itemModifiedAppearancesByItem = new Dictionary<uint, ItemModifiedAppearanceRecord>();
        private MultiMap<uint, uint> _itemToBonusTree = new MultiMap<uint, uint>();
        private MultiMap<uint, ItemSetSpellRecord> _itemSetSpells = new MultiMap<uint, ItemSetSpellRecord>();
        private MultiMap<uint, ItemSpecOverrideRecord> _itemSpecOverrides = new MultiMap<uint, ItemSpecOverrideRecord>();
        private Dictionary<uint, Dictionary<uint, MapDifficultyRecord>> _mapDifficulties = new Dictionary<uint, Dictionary<uint, MapDifficultyRecord>>();
        private MultiMap<uint, Tuple<uint, PlayerConditionRecord>> _mapDifficultyConditions = new MultiMap<uint, Tuple<uint, PlayerConditionRecord>>();
        private Dictionary<uint, MountRecord> _mountsBySpellId = new Dictionary<uint, MountRecord>();
        private MultiMap<uint, MountTypeXCapabilityRecord> _mountCapabilitiesByType = new MultiMap<uint, MountTypeXCapabilityRecord>();
        private MultiMap<uint, MountXDisplayRecord> _mountDisplays = new MultiMap<uint, MountXDisplayRecord>();
        private Dictionary<uint, List<NameGenRecord>[]> _nameGenData = new Dictionary<uint, List<NameGenRecord>[]>();
        private List<string>[] _nameValidators = new List<string>[(int)Locale.Total + 1];
        private MultiMap<uint, uint> _phasesByGroup = new MultiMap<uint, uint>();
        private Dictionary<PowerType, PowerTypeRecord> _powerTypes = new Dictionary<PowerType, PowerTypeRecord>();
        private Dictionary<uint, byte> _pvpItemBonus = new Dictionary<uint, byte>();
        private PvpTalentSlotUnlockRecord[] _pvpTalentSlotUnlock = new PvpTalentSlotUnlockRecord[PlayerConst.MaxPvpTalentSlots];
        private Dictionary<uint, Tuple<List<QuestPackageItemRecord>, List<QuestPackageItemRecord>>> _questPackages = new Dictionary<uint, Tuple<List<QuestPackageItemRecord>, List<QuestPackageItemRecord>>>();
        private MultiMap<uint, RewardPackXCurrencyTypeRecord> _rewardPackCurrencyTypes = new MultiMap<uint, RewardPackXCurrencyTypeRecord>();
        private MultiMap<uint, RewardPackXItemRecord> _rewardPackItems = new MultiMap<uint, RewardPackXItemRecord>();
        private MultiMap<uint, SkillLineRecord> _skillLinesByParentSkillLine = new MultiMap<uint, SkillLineRecord>();
        private MultiMap<uint, SkillLineAbilityRecord> _skillLineAbilitiesBySkillupSkill = new MultiMap<uint, SkillLineAbilityRecord>();
        private MultiMap<uint, SkillRaceClassInfoRecord> _skillRaceClassInfoBySkill = new MultiMap<uint, SkillRaceClassInfoRecord>();
        private MultiMap<uint, SpecializationSpellsRecord> _specializationSpellsBySpec = new MultiMap<uint, SpecializationSpellsRecord>();
        private List<Tuple<int, uint>> _specsBySpecSet = new List<Tuple<int, uint>>();
        private List<byte> _spellFamilyNames = new List<byte>();
        private MultiMap<uint, SpellProcsPerMinuteModRecord> _spellProcsPerMinuteMods = new MultiMap<uint, SpellProcsPerMinuteModRecord>();
        private List<TalentRecord>[][][] _talentsByPosition = new List<TalentRecord>[(int)Class.Max][][];
        private List<uint> _toys = new List<uint>();
        private MultiMap<uint, TransmogSetRecord> _transmogSetsByItemModifiedAppearance = new MultiMap<uint, TransmogSetRecord>();
        private MultiMap<uint, TransmogSetItemRecord> _transmogSetItemsByTransmogSet = new MultiMap<uint, TransmogSetItemRecord>();
        private Dictionary<int, UiMapBounds> _uiMapBounds = new Dictionary<int, UiMapBounds>();
        private MultiMap<int, UiMapAssignmentRecord>[] _uiMapAssignmentByMap = new MultiMap<int, UiMapAssignmentRecord>[(int)UiMapSystem.Max];
        private MultiMap<int, UiMapAssignmentRecord>[] _uiMapAssignmentByArea = new MultiMap<int, UiMapAssignmentRecord>[(int)UiMapSystem.Max];
        private MultiMap<int, UiMapAssignmentRecord>[] _uiMapAssignmentByWmoDoodadPlacement = new MultiMap<int, UiMapAssignmentRecord>[(int)UiMapSystem.Max];
        private MultiMap<int, UiMapAssignmentRecord>[] _uiMapAssignmentByWmoGroup = new MultiMap<int, UiMapAssignmentRecord>[(int)UiMapSystem.Max];
        private List<int> _uiMapPhases = new List<int>();
        private Dictionary<Tuple<short, sbyte, int>, WMOAreaTableRecord> _wmoAreaTableLookup = new Dictionary<Tuple<short, sbyte, int>, WMOAreaTableRecord>();
    }

    internal class UiMapBounds
    {
        // these coords are mixed when calculated and used... its a mess
        public float[] Bounds = new float[4];
        public bool IsUiAssignment;
        public bool IsUiLink;
    }

    internal class UiMapAssignmentStatus
    {
        public UiMapAssignmentRecord UiMapAssignment;
        public InsideStruct Inside;
        public OutsideStruct Outside;
        public sbyte MapPriority;
        public sbyte AreaPriority;
        public sbyte WmoPriority;

        public UiMapAssignmentStatus()
        {
            Inside = new InsideStruct();
            Outside = new OutsideStruct();
            MapPriority = 3;
            AreaPriority = -1;
            WmoPriority = 3;
        }

        // distances if inside
        public class InsideStruct
        {
            public float DistanceToRegionCenterSquared = float.MaxValue;
            public float DistanceToRegionBottom = float.MaxValue;
        }

        // distances if outside
        public class OutsideStruct
        {
            public float DistanceToRegionEdgeSquared = float.MaxValue;
            public float DistanceToRegionTop = float.MaxValue;
            public float DistanceToRegionBottom = float.MaxValue;
        }

        private bool IsInside()
        {
            return Outside.DistanceToRegionEdgeSquared < float.Epsilon &&
                Math.Abs(Outside.DistanceToRegionTop) < float.Epsilon &&
                Math.Abs(Outside.DistanceToRegionBottom) < float.Epsilon;
        }

        public static bool operator <(UiMapAssignmentStatus left, UiMapAssignmentStatus right)
        {
            var leftInside = left.IsInside();
            var rightInside = right.IsInside();
            if (leftInside != rightInside)
                return leftInside;

            if (left.UiMapAssignment != null && right.UiMapAssignment != null &&
                left.UiMapAssignment.UiMapID == right.UiMapAssignment.UiMapID &&
                left.UiMapAssignment.OrderIndex != right.UiMapAssignment.OrderIndex)
                return left.UiMapAssignment.OrderIndex < right.UiMapAssignment.OrderIndex;

            if (left.WmoPriority != right.WmoPriority)
                return left.WmoPriority < right.WmoPriority;

            if (left.AreaPriority != right.AreaPriority)
                return left.AreaPriority < right.AreaPriority;

            if (left.MapPriority != right.MapPriority)
                return left.MapPriority < right.MapPriority;

            if (leftInside)
            {
                if (left.Inside.DistanceToRegionBottom != right.Inside.DistanceToRegionBottom)
                    return left.Inside.DistanceToRegionBottom < right.Inside.DistanceToRegionBottom;

                var leftUiSizeX = left.UiMapAssignment != null ? (left.UiMapAssignment.UiMax.X - left.UiMapAssignment.UiMin.X) : 0.0f;
                var rightUiSizeX = right.UiMapAssignment != null ? (right.UiMapAssignment.UiMax.X - right.UiMapAssignment.UiMin.X) : 0.0f;

                if (leftUiSizeX > float.Epsilon && rightUiSizeX > float.Epsilon)
                {
                    var leftScale = (left.UiMapAssignment.Region[1].X - left.UiMapAssignment.Region[0].X) / leftUiSizeX;
                    var rightScale = (right.UiMapAssignment.Region[1].X - right.UiMapAssignment.Region[0].X) / rightUiSizeX;
                    if (leftScale != rightScale)
                        return leftScale < rightScale;
                }

                if (left.Inside.DistanceToRegionCenterSquared != right.Inside.DistanceToRegionCenterSquared)
                    return left.Inside.DistanceToRegionCenterSquared < right.Inside.DistanceToRegionCenterSquared;
            }
            else
            {
                if (left.Outside.DistanceToRegionTop != right.Outside.DistanceToRegionTop)
                    return left.Outside.DistanceToRegionTop < right.Outside.DistanceToRegionTop;

                if (left.Outside.DistanceToRegionBottom != right.Outside.DistanceToRegionBottom)
                    return left.Outside.DistanceToRegionBottom < right.Outside.DistanceToRegionBottom;

                if (left.Outside.DistanceToRegionEdgeSquared != right.Outside.DistanceToRegionEdgeSquared)
                    return left.Outside.DistanceToRegionEdgeSquared < right.Outside.DistanceToRegionEdgeSquared;
            }

            return true;
        }

        public static bool operator >(UiMapAssignmentStatus left, UiMapAssignmentStatus right)
        {
            var leftInside = left.IsInside();
            var rightInside = right.IsInside();
            if (leftInside != rightInside)
                return leftInside;

            if (left.UiMapAssignment != null && right.UiMapAssignment != null &&
                left.UiMapAssignment.UiMapID == right.UiMapAssignment.UiMapID &&
                left.UiMapAssignment.OrderIndex != right.UiMapAssignment.OrderIndex)
                return left.UiMapAssignment.OrderIndex > right.UiMapAssignment.OrderIndex;

            if (left.WmoPriority != right.WmoPriority)
                return left.WmoPriority > right.WmoPriority;

            if (left.AreaPriority != right.AreaPriority)
                return left.AreaPriority > right.AreaPriority;

            if (left.MapPriority != right.MapPriority)
                return left.MapPriority > right.MapPriority;

            if (leftInside)
            {
                if (left.Inside.DistanceToRegionBottom != right.Inside.DistanceToRegionBottom)
                    return left.Inside.DistanceToRegionBottom > right.Inside.DistanceToRegionBottom;

                var leftUiSizeX = left.UiMapAssignment != null ? (left.UiMapAssignment.UiMax.X - left.UiMapAssignment.UiMin.X) : 0.0f;
                var rightUiSizeX = right.UiMapAssignment != null ? (right.UiMapAssignment.UiMax.X - right.UiMapAssignment.UiMin.X) : 0.0f;

                if (leftUiSizeX > float.Epsilon && rightUiSizeX > float.Epsilon)
                {
                    var leftScale = (left.UiMapAssignment.Region[1].X - left.UiMapAssignment.Region[0].X) / leftUiSizeX;
                    var rightScale = (right.UiMapAssignment.Region[1].X - right.UiMapAssignment.Region[0].X) / rightUiSizeX;
                    if (leftScale != rightScale)
                        return leftScale > rightScale;
                }

                if (left.Inside.DistanceToRegionCenterSquared != right.Inside.DistanceToRegionCenterSquared)
                    return left.Inside.DistanceToRegionCenterSquared > right.Inside.DistanceToRegionCenterSquared;
            }
            else
            {
                if (left.Outside.DistanceToRegionTop != right.Outside.DistanceToRegionTop)
                    return left.Outside.DistanceToRegionTop > right.Outside.DistanceToRegionTop;

                if (left.Outside.DistanceToRegionBottom != right.Outside.DistanceToRegionBottom)
                    return left.Outside.DistanceToRegionBottom > right.Outside.DistanceToRegionBottom;

                if (left.Outside.DistanceToRegionEdgeSquared != right.Outside.DistanceToRegionEdgeSquared)
                    return left.Outside.DistanceToRegionEdgeSquared > right.Outside.DistanceToRegionEdgeSquared;
            }

            return true;
        }
    }

    public class HotfixRecord
    {
        public uint TableHash;
        public int RecordID;
        public int HotfixID;
        public Status HotfixStatus = Status.Invalid;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(TableHash);
            data.WriteInt32(RecordID);
            data.WriteInt32(HotfixID);
        }

        public void Read(WorldPacket data)
        {
            TableHash = data.ReadUInt32();
            RecordID = data.ReadInt32();
            HotfixID = data.ReadInt32();
        }

        public enum Status
        {
            Valid           = 1,
            RecordRemoved   = 2,
            Invalid         = 3
        }
    }

    public class HotfixOptionalData
    {
        public uint Key;
        public byte[] Data;
    }

    internal class ChrClassesXPowerTypesRecordComparer : IComparer<ChrClassesXPowerTypesRecord>
    {
        public int Compare(ChrClassesXPowerTypesRecord left, ChrClassesXPowerTypesRecord right)
        {
            if (left.ClassID != right.ClassID)
                return left.ClassID.CompareTo(right.ClassID);
            return left.PowerType.CompareTo(right.PowerType);
        }
    }

    internal class MountTypeXCapabilityRecordComparer : IComparer<MountTypeXCapabilityRecord>
    {
        public int Compare(MountTypeXCapabilityRecord left, MountTypeXCapabilityRecord right)
        {
            if (left.MountTypeID == right.MountTypeID)
                return left.OrderIndex.CompareTo(right.OrderIndex);
            return left.Id.CompareTo(right.Id);
        }
    }

    internal class ItemLevelSelectorQualityRecordComparator : IComparer<ItemLevelSelectorQualityRecord>
    {
        public bool Compare(ItemLevelSelectorQualityRecord left, ItemQuality quality)
        {
            return left.Quality < (byte)quality;
        }

        public int Compare(ItemLevelSelectorQualityRecord left, ItemLevelSelectorQualityRecord right)
        {
            return left.Quality.CompareTo(right.Quality);
        }
    }

    public struct ContentTuningLevels
    {
        public short MinLevel;
        public short MaxLevel;
        public short MinLevelWithDelta;
        public short MaxLevelWithDelta;
        public short TargetLevelMin;
        public short TargetLevelMax;
    }

    public class ShapeshiftFormModelData
    {
        public uint OptionID;
        public List<ChrCustomizationChoiceRecord> Choices = new List<ChrCustomizationChoiceRecord>();
        public List<ChrCustomizationDisplayInfoRecord> Displays = new List<ChrCustomizationDisplayInfoRecord>();
    }

    internal enum CurveInterpolationMode
    {
        Linear = 0,
        Cosine = 1,
        CatmullRom = 2,
        Bezier3 = 3,
        Bezier4 = 4,
        Bezier = 5,
        Constant = 6,
    }
}
