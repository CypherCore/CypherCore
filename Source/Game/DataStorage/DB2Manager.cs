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
using Framework.GameMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Game.DataStorage
{
    public class DB2Manager : Singleton<DB2Manager>
    {
        DB2Manager()
        {
            for (uint i = 0; i < (int)Class.Max; ++i)
            {
                _powersByClass[i] = new uint[(int)PowerType.Max];

                for (uint j = 0; j < (int)PowerType.Max; ++j)
                    _powersByClass[i][j] = (uint)PowerType.Max;
            }

            for (uint i = 0; i < (int)LocaleConstant.Total + 1; ++i)
                _nameValidators[i] = new List<string>();
        }

        public void LoadStores()
        {
            foreach (var areaGroupMember in CliDB.AreaGroupMemberStorage.Values)
                _areaGroupMembers.Add(areaGroupMember.AreaGroupID, areaGroupMember.AreaID);

            CliDB.AreaGroupMemberStorage.Clear();

            foreach (ArtifactPowerRecord artifactPower in CliDB.ArtifactPowerStorage.Values)
                _artifactPowers.Add(artifactPower.ArtifactID, artifactPower);

            foreach (ArtifactPowerLinkRecord artifactPowerLink in CliDB.ArtifactPowerLinkStorage.Values)
            {
                _artifactPowerLinks.Add(artifactPowerLink.PowerA, artifactPowerLink.PowerB);
                _artifactPowerLinks.Add(artifactPowerLink.PowerB, artifactPowerLink.PowerA);
            }

            CliDB.ArtifactPowerLinkStorage.Clear();

            foreach (ArtifactPowerRankRecord artifactPowerRank in CliDB.ArtifactPowerRankStorage.Values)
                _artifactPowerRanks[Tuple.Create((uint)artifactPowerRank.ArtifactPowerID, artifactPowerRank.RankIndex)] = artifactPowerRank;

            CliDB.ArtifactPowerRankStorage.Clear();

            foreach (CharacterFacialHairStylesRecord characterFacialStyle in CliDB.CharacterFacialHairStylesStorage.Values)
                _characterFacialHairStyles.Add(Tuple.Create(characterFacialStyle.RaceID, characterFacialStyle.SexID, (uint)characterFacialStyle.VariationID));

            CliDB.CharacterFacialHairStylesStorage.Clear();

            CharBaseSectionVariation[] sectionToBase = new CharBaseSectionVariation[(int)CharSectionType.Max];
            foreach (CharBaseSectionRecord charBaseSection in CliDB.CharBaseSectionStorage.Values)
            {
                Cypher.Assert(charBaseSection.ResolutionVariationEnum < (byte)CharSectionType.Max, $"SECTION_TYPE_MAX ({(byte)CharSectionType.Max}) must be equal to or greater than {charBaseSection.ResolutionVariationEnum + 1}");
                Cypher.Assert(charBaseSection.VariationEnum < CharBaseSectionVariation.Max, $"CharBaseSectionVariation.Max {(byte)CharBaseSectionVariation.Max} must be equal to or greater than {charBaseSection.VariationEnum + 1}");

                sectionToBase[charBaseSection.ResolutionVariationEnum] = charBaseSection.VariationEnum;
            }

            CliDB.CharBaseSectionStorage.Clear();

            MultiMap<Tuple<byte, byte, CharBaseSectionVariation>, Tuple<byte, byte>> addedSections = new MultiMap<Tuple<byte, byte, CharBaseSectionVariation>, Tuple<byte, byte>>();
            foreach (CharSectionsRecord charSection in CliDB.CharSectionsStorage.Values)
            {
                Cypher.Assert(charSection.BaseSection < (byte)CharSectionType.Max, $"SECTION_TYPE_MAX ({(byte)CharSectionType.Max}) must be equal to or greater than {charSection.BaseSection + 1}");

                Tuple<byte, byte, CharBaseSectionVariation> sectionKey = Tuple.Create(charSection.RaceID, charSection.SexID, sectionToBase[charSection.BaseSection]);
                Tuple<byte, byte> sectionCombination = Tuple.Create(charSection.VariationIndex, charSection.ColorIndex);
                if (addedSections.Contains(sectionKey, sectionCombination))
                    continue;

                addedSections.Add(sectionKey, sectionCombination);
                _charSections.Add(sectionKey, charSection);
            }

            CliDB.CharSectionsStorage.Clear();

            foreach (var outfit in CliDB.CharStartOutfitStorage.Values)
                _charStartOutfits[(uint)((byte)outfit.RaceID | (outfit.ClassID << 8) | (outfit.SexID << 16))] = outfit;

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

            foreach (ChrSpecializationRecord chrSpec in CliDB.ChrSpecializationStorage.Values)
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

                if (chrSpec.Flags.HasAnyFlag(ChrSpecializationFlag.Recommended))
                    _defaultChrSpecializationsByClass[(uint)chrSpec.ClassID] = chrSpec;
            }

            foreach (CurvePointRecord curvePoint in CliDB.CurvePointStorage.Values)
            {
                if (CliDB.CurveStorage.ContainsKey(curvePoint.CurveID))
                    _curvePoints.Add(curvePoint.CurveID, curvePoint);
            }

            CliDB.CurvePointStorage.Clear();

            foreach (var key in _curvePoints.Keys.ToList())
                _curvePoints[key] = _curvePoints[key].OrderBy(point => point.OrderIndex).ToList();

            foreach (EmotesTextSoundRecord emoteTextSound in CliDB.EmotesTextSoundStorage.Values)
                _emoteTextSounds[Tuple.Create((uint)emoteTextSound.EmotesTextId, emoteTextSound.RaceId, emoteTextSound.SexId, emoteTextSound.ClassId)] = emoteTextSound;

            CliDB.EmotesTextSoundStorage.Clear();

            foreach (ExpectedStatRecord expectedStat in CliDB.ExpectedStatStorage.Values)
                _expectedStatsByLevel[Tuple.Create(expectedStat.Lvl, expectedStat.ExpansionID)] = expectedStat;

            CliDB.ExpectedStatStorage.Clear();

            foreach (FactionRecord faction in CliDB.FactionStorage.Values)
                if (faction.ParentFactionID != 0)
                    _factionTeams.Add(faction.ParentFactionID, faction.Id);

            foreach (GameObjectDisplayInfoRecord gameObjectDisplayInfo in CliDB.GameObjectDisplayInfoStorage.Values)
            {
                if (gameObjectDisplayInfo.GeoBoxMax.X < gameObjectDisplayInfo.GeoBoxMin.X)
                    Extensions.Swap(ref gameObjectDisplayInfo.GeoBox[3], ref gameObjectDisplayInfo.GeoBox[0]);
                if (gameObjectDisplayInfo.GeoBoxMax.Y < gameObjectDisplayInfo.GeoBoxMin.Y)
                    Extensions.Swap(ref gameObjectDisplayInfo.GeoBox[4], ref gameObjectDisplayInfo.GeoBox[1]);
                if (gameObjectDisplayInfo.GeoBoxMax.Z < gameObjectDisplayInfo.GeoBoxMin.Z)
                    Extensions.Swap(ref gameObjectDisplayInfo.GeoBox[5], ref gameObjectDisplayInfo.GeoBox[2]);
            }

            foreach (HeirloomRecord heirloom in CliDB.HeirloomStorage.Values)
                _heirlooms[heirloom.ItemID] = heirloom;

            CliDB.HeirloomStorage.Clear();

            foreach (GlyphBindableSpellRecord glyphBindableSpell in CliDB.GlyphBindableSpellStorage.Values)
                _glyphBindableSpells.Add((uint)glyphBindableSpell.GlyphPropertiesID, (uint)glyphBindableSpell.SpellID);

            CliDB.GlyphBindableSpellStorage.Clear();

            foreach (GlyphRequiredSpecRecord glyphRequiredSpec in CliDB.GlyphRequiredSpecStorage.Values)
                _glyphRequiredSpecs.Add(glyphRequiredSpec.GlyphPropertiesID, glyphRequiredSpec.ChrSpecializationID);

            CliDB.GlyphRequiredSpecStorage.Clear();

            foreach (var bonus in CliDB.ItemBonusStorage.Values)
                _itemBonusLists.Add(bonus.ParentItemBonusListID, bonus);

            CliDB.ItemBonusStorage.Clear();

            foreach (ItemBonusListLevelDeltaRecord itemBonusListLevelDelta in CliDB.ItemBonusListLevelDeltaStorage.Values)
                _itemLevelDeltaToBonusListContainer[itemBonusListLevelDelta.ItemLevelDelta] = itemBonusListLevelDelta.Id;

            CliDB.ItemBonusListLevelDeltaStorage.Clear();

            foreach (var key in CliDB.ItemBonusTreeNodeStorage.Keys)
            {
                ItemBonusTreeNodeRecord bonusTreeNode = CliDB.ItemBonusTreeNodeStorage[key];
                uint bonusTreeId = bonusTreeNode.ParentItemBonusTreeID;
                while (bonusTreeNode != null)
                {
                    _itemBonusTrees.Add(bonusTreeId, bonusTreeNode);
                    bonusTreeNode = CliDB.ItemBonusTreeNodeStorage.LookupByKey(bonusTreeNode.ChildItemBonusTreeID);
                }
            }

            CliDB.ItemBonusTreeNodeStorage.Clear();

            foreach (ItemChildEquipmentRecord itemChildEquipment in CliDB.ItemChildEquipmentStorage.Values)
            {
                //ASSERT(_itemChildEquipment.find(itemChildEquipment.ParentItemID) == _itemChildEquipment.end(), "Item must have max 1 child item.");
                _itemChildEquipment[itemChildEquipment.ParentItemID] = itemChildEquipment;
            }

            CliDB.ItemChildEquipmentStorage.Clear();

            foreach (ItemClassRecord itemClass in CliDB.ItemClassStorage.Values)
            {
                //ASSERT(itemClass.ClassID < _itemClassByOldEnum.size());
                //ASSERT(!_itemClassByOldEnum[itemClass.ClassID]);
                _itemClassByOldEnum[itemClass.ClassID] = itemClass;
            }

            CliDB.ItemClassStorage.Clear();

            foreach (ItemCurrencyCostRecord itemCurrencyCost in CliDB.ItemCurrencyCostStorage.Values)
                _itemsWithCurrencyCost.Add(itemCurrencyCost.ItemID);

            CliDB.ItemCurrencyCostStorage.Clear();

            foreach (ItemLimitCategoryConditionRecord condition in CliDB.ItemLimitCategoryConditionStorage.Values)
                _itemCategoryConditions.Add(condition.ParentItemLimitCategoryID, condition);

            foreach (ItemLevelSelectorQualityRecord itemLevelSelectorQuality in CliDB.ItemLevelSelectorQualityStorage.Values)
                _itemLevelQualitySelectorQualities.Add((uint)itemLevelSelectorQuality.ParentILSQualitySetID, itemLevelSelectorQuality);

            CliDB.ItemLevelSelectorQualityStorage.Clear();

            foreach (var appearanceMod in CliDB.ItemModifiedAppearanceStorage.Values)
            {
                //ASSERT(appearanceMod.ItemID <= 0xFFFFFF);
                _itemModifiedAppearancesByItem[(uint)((int)appearanceMod.ItemID | (appearanceMod.ItemAppearanceModifierID << 24))] = appearanceMod;
            }

            foreach (ItemSetSpellRecord itemSetSpell in CliDB.ItemSetSpellStorage.Values)
                _itemSetSpells.Add(itemSetSpell.ItemSetID, itemSetSpell);

            CliDB.ItemSetSpellStorage.Clear();

            foreach (var itemSpecOverride in CliDB.ItemSpecOverrideStorage.Values)
                _itemSpecOverrides.Add(itemSpecOverride.ItemID, itemSpecOverride);

            CliDB.ItemSpecOverrideStorage.Clear();

            foreach (var itemBonusTreeAssignment in CliDB.ItemXBonusTreeStorage.Values)
                _itemToBonusTree.Add(itemBonusTreeAssignment.ItemID, itemBonusTreeAssignment.ItemBonusTreeID);

            CliDB.ItemXBonusTreeStorage.Clear();

            foreach (MapDifficultyRecord entry in CliDB.MapDifficultyStorage.Values)
            {
                if (!_mapDifficulties.ContainsKey(entry.MapID))
                    _mapDifficulties[entry.MapID] = new Dictionary<uint, MapDifficultyRecord>();

                _mapDifficulties[entry.MapID][entry.DifficultyID] = entry;
            }
            _mapDifficulties[0][0] = _mapDifficulties[1][0]; // map 0 is missing from MapDifficulty.dbc so we cheat a bit

            CliDB.MapDifficultyStorage.Clear();

            foreach (var mount in CliDB.MountStorage.Values)
                _mountsBySpellId[mount.SourceSpellID] = mount;

            foreach (MountTypeXCapabilityRecord mountTypeCapability in CliDB.MountTypeXCapabilityStorage.Values)
                _mountCapabilitiesByType.Add(mountTypeCapability.MountTypeID, mountTypeCapability);

            CliDB.MountTypeXCapabilityStorage.Clear();

            foreach (var key in _mountCapabilitiesByType.Keys)
                _mountCapabilitiesByType[key].Sort(new MountTypeXCapabilityRecordComparer());

            foreach (MountXDisplayRecord mountDisplay in CliDB.MountXDisplayStorage.Values)
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
                Cypher.Assert(namesProfanity.Language < (int)LocaleConstant.Total || namesProfanity.Language == -1);
                if (namesProfanity.Language != -1)
                    _nameValidators[namesProfanity.Language].Add(namesProfanity.Name);
                else
                    for (uint i = 0; i < (int)LocaleConstant.Total; ++i)
                    {
                        if (i == (int)LocaleConstant.None)
                            continue;

                        _nameValidators[i].Add(namesProfanity.Name);
                    }
            }

            CliDB.NamesProfanityStorage.Clear();

            foreach (var namesReserved in CliDB.NamesReservedStorage.Values)
                _nameValidators[(int)LocaleConstant.Total].Add(namesReserved.Name);

            CliDB.NamesReservedStorage.Clear();

            foreach (var namesReserved in CliDB.NamesReservedLocaleStorage.Values)
            {
                Cypher.Assert(!Convert.ToBoolean(namesReserved.LocaleMask & ~((1 << (int)LocaleConstant.Total) - 1)));
                for (int i = 0; i < (int)LocaleConstant.Total; ++i)
                {
                    if (i == (int)LocaleConstant.None)
                        continue;

                    if (Convert.ToBoolean(namesReserved.LocaleMask & (1 << i)))
                        _nameValidators[i].Add(namesReserved.Name);
                }
            }
            CliDB.NamesReservedLocaleStorage.Clear();

            foreach (var group in CliDB.PhaseXPhaseGroupStorage.Values)
            {
                PhaseRecord phase = CliDB.PhaseStorage.LookupByKey(group.PhaseId);
                if (phase != null)
                    _phasesByGroup.Add(group.PhaseGroupID, phase.Id);
            }
            CliDB.PhaseXPhaseGroupStorage.Clear();

            foreach (PowerTypeRecord powerType in CliDB.PowerTypeStorage.Values)
            {
                Cypher.Assert(powerType.PowerTypeEnum < PowerType.Max);

                _powerTypes[powerType.PowerTypeEnum] = powerType;
            }

            foreach (PvpItemRecord pvpItem in CliDB.PvpItemStorage.Values)
                _pvpItemBonus[pvpItem.ItemID] = pvpItem.ItemLevelDelta;

            foreach (PvpTalentSlotUnlockRecord talentUnlock in CliDB.PvpTalentSlotUnlockStorage.Values)
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

            foreach (QuestPackageItemRecord questPackageItem in CliDB.QuestPackageItemStorage.Values)
            {
                if (!_questPackages.ContainsKey(questPackageItem.PackageID))
                    _questPackages[questPackageItem.PackageID] = Tuple.Create(new List<QuestPackageItemRecord>(), new List<QuestPackageItemRecord>());

                if (questPackageItem.DisplayType != QuestPackageFilter.Unmatched)
                    _questPackages[questPackageItem.PackageID].Item1.Add(questPackageItem);
                else
                    _questPackages[questPackageItem.PackageID].Item2.Add(questPackageItem);
            }

            CliDB.QuestPackageItemStorage.Clear();

            foreach (RewardPackXCurrencyTypeRecord rewardPackXCurrencyType in CliDB.RewardPackXCurrencyTypeStorage.Values)
                _rewardPackCurrencyTypes.Add(rewardPackXCurrencyType.RewardPackID, rewardPackXCurrencyType);

            CliDB.RewardPackXCurrencyTypeStorage.Clear();

            foreach (RewardPackXItemRecord rewardPackXItem in CliDB.RewardPackXItemStorage.Values)
                _rewardPackItems.Add(rewardPackXItem.RewardPackID, rewardPackXItem);

            CliDB.RewardPackXItemStorage.Clear();

            foreach (RulesetItemUpgradeRecord rulesetItemUpgrade in CliDB.RulesetItemUpgradeStorage.Values)
                _rulesetItemUpgrade[rulesetItemUpgrade.ItemID] = rulesetItemUpgrade.ItemUpgradeID;

            CliDB.RulesetItemUpgradeStorage.Clear();

            foreach (SkillLineAbilityRecord skillLineAbility in CliDB.SkillLineAbilityStorage.Values)
                _skillLineAbilitiesBySkillupSkill.Add(skillLineAbility.SkillupSkillLineID != 0 ? skillLineAbility.SkillupSkillLineID : skillLineAbility.SkillLine, skillLineAbility);

            foreach (SkillRaceClassInfoRecord entry in CliDB.SkillRaceClassInfoStorage.Values)
            {
                if (CliDB.SkillLineStorage.ContainsKey(entry.SkillID))
                    _skillRaceClassInfoBySkill.Add((uint)entry.SkillID, entry);
            }

            foreach (var specSpells in CliDB.SpecializationSpellsStorage.Values)
                _specializationSpellsBySpec.Add(specSpells.SpecID, specSpells);

            CliDB.SpecializationSpellsStorage.Clear();

            foreach (SpellClassOptionsRecord classOption in CliDB.SpellClassOptionsStorage.Values)
                _spellFamilyNames.Add(classOption.SpellClassSet);

            foreach (SpellPowerRecord power in CliDB.SpellPowerStorage.Values)
            {
                SpellPowerDifficultyRecord powerDifficulty = CliDB.SpellPowerDifficultyStorage.LookupByKey(power.Id);
                if (powerDifficulty != null)
                {
                    if (!_spellPowerDifficulties.ContainsKey(power.SpellID))
                        _spellPowerDifficulties[power.SpellID] = new Dictionary<uint, List<SpellPowerRecord>>();

                    if (!_spellPowerDifficulties[power.SpellID].ContainsKey(powerDifficulty.DifficultyID))
                        _spellPowerDifficulties[power.SpellID][powerDifficulty.DifficultyID] = new List<SpellPowerRecord>();

                    _spellPowerDifficulties[power.SpellID][powerDifficulty.DifficultyID].Insert(powerDifficulty.OrderIndex, power);
                }
                else
                {
                    if (!_spellPowers.ContainsKey(power.SpellID))
                        _spellPowers[power.SpellID] = new List<SpellPowerRecord>();

                    _spellPowers[power.SpellID].Insert(power.OrderIndex, power);
                }
            }

            CliDB.SpellPowerStorage.Clear();

            foreach (SpellProcsPerMinuteModRecord ppmMod in CliDB.SpellProcsPerMinuteModStorage.Values)
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

            foreach (TalentRecord talentInfo in CliDB.TalentStorage.Values)
            {
                //ASSERT(talentInfo.ClassID < MAX_CLASSES);
                //ASSERT(talentInfo.TierID < MAX_TALENT_TIERS, "MAX_TALENT_TIERS must be at least {0}", talentInfo.TierID);
                //ASSERT(talentInfo.ColumnIndex < MAX_TALENT_COLUMNS, "MAX_TALENT_COLUMNS must be at least {0}", talentInfo.ColumnIndex);
                _talentsByPosition[talentInfo.ClassID][talentInfo.TierID][talentInfo.ColumnIndex].Add(talentInfo);
            }

            foreach (ToyRecord toy in CliDB.ToyStorage.Values)
                _toys.Add(toy.ItemID);

            CliDB.ToyStorage.Clear();

            foreach (TransmogSetItemRecord transmogSetItem in CliDB.TransmogSetItemStorage.Values)
            {
                TransmogSetRecord set = CliDB.TransmogSetStorage.LookupByKey(transmogSetItem.TransmogSetID);
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

            MultiMap<int, UiMapAssignmentRecord> uiMapAssignmentByUiMap = new MultiMap<int, UiMapAssignmentRecord>();
            foreach (UiMapAssignmentRecord uiMapAssignment in CliDB.UiMapAssignmentStorage.Values)
            {
                uiMapAssignmentByUiMap.Add(uiMapAssignment.UiMapID, uiMapAssignment);
                UiMapRecord uiMap = CliDB.UiMapStorage.LookupByKey(uiMapAssignment.UiMapID);
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

            Dictionary<Tuple<int, uint>, UiMapLinkRecord> uiMapLinks = new Dictionary<Tuple<int, uint>, UiMapLinkRecord>();
            foreach (UiMapLinkRecord uiMapLink in CliDB.UiMapLinkStorage.Values)
                uiMapLinks[Tuple.Create(uiMapLink.ParentUiMapID, (uint)uiMapLink.ChildUiMapID)] = uiMapLink;

            foreach (UiMapRecord uiMap in CliDB.UiMapStorage.Values)
            {
                UiMapBounds bounds = new UiMapBounds();
                UiMapRecord parentUiMap = CliDB.UiMapStorage.LookupByKey(uiMap.ParentUiMapID);
                if (parentUiMap != null)
                {
                    if (Convert.ToBoolean(parentUiMap.Flags & 0x80))
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

                    float parentXsize = parentUiMapAssignment.Region[1].X - parentUiMapAssignment.Region[0].X;
                    float parentYsize = parentUiMapAssignment.Region[1].Y - parentUiMapAssignment.Region[0].Y;
                    float bound0scale = (uiMapAssignment.Region[1].X - parentUiMapAssignment.Region[0].X) / parentXsize;
                    float bound0 = ((1.0f - bound0scale) * parentUiMapAssignment.UiMax.Y) + (bound0scale * parentUiMapAssignment.UiMin.Y);
                    float bound2scale = (uiMapAssignment.Region[0].X - parentUiMapAssignment.Region[0].X) / parentXsize;
                    float bound2 = ((1.0f - bound2scale) * parentUiMapAssignment.UiMax.Y) + (bound2scale * parentUiMapAssignment.UiMin.Y);
                    float bound1scale = (uiMapAssignment.Region[1].Y - parentUiMapAssignment.Region[0].Y) / parentYsize;
                    float bound1 = ((1.0f - bound1scale) * parentUiMapAssignment.UiMax.X) + (bound1scale * parentUiMapAssignment.UiMin.X);
                    float bound3scale = (uiMapAssignment.Region[0].Y - parentUiMapAssignment.Region[0].Y) / parentYsize;
                    float bound3 = ((1.0f - bound3scale) * parentUiMapAssignment.UiMax.X) + (bound3scale * parentUiMapAssignment.UiMin.X);
                    if ((bound3 - bound1) > 0.0f || (bound2 - bound0) > 0.0f)
                    {
                        bounds.Bounds[0] = bound0;
                        bounds.Bounds[1] = bound1;
                        bounds.Bounds[2] = bound2;
                        bounds.Bounds[3] = bound3;
                        bounds.IsUiAssignment = true;
                    }
                }

                UiMapLinkRecord uiMapLink = uiMapLinks.LookupByKey(Tuple.Create(uiMap.ParentUiMapID, uiMap.Id));
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

            foreach (UiMapXMapArtRecord uiMapArt in CliDB.UiMapXMapArtStorage.Values)
                if (uiMapArt.PhaseID != 0)
                    _uiMapPhases.Add(uiMapArt.PhaseID);

            foreach (WMOAreaTableRecord entry in CliDB.WMOAreaTableStorage.Values)
                _wmoAreaTableLookup[Tuple.Create((short)entry.WmoID, (sbyte)entry.NameSetID, entry.WmoGroupID)] = entry;

            CliDB.WMOAreaTableStorage.Clear();
        }

        public IDB2Storage GetStorage(uint type)
        {
            return _storage.LookupByKey(type);
        }

        public void LoadHotfixData()
        {
            uint oldMSTime = Time.GetMSTime();

            SQLResult result = DB.Hotfix.Query("SELECT Id, TableHash, RecordId, Deleted FROM hotfix_data ORDER BY Id");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 hotfix info entries.");
                return;
            }

            Dictionary<Tuple<uint, int>, bool> deletedRecords = new Dictionary<Tuple<uint, int>, bool>();

            uint count = 0;
            do
            {
                uint id = result.Read<uint>(0);
                uint tableHash = result.Read<uint>(1);
                int recordId = result.Read<int>(2);
                bool deleted = result.Read<bool>(3);
                if (!_storage.ContainsKey(tableHash) && !_hotfixBlob.ContainsKey(Tuple.Create(tableHash, recordId)))
                {
                    Log.outError(LogFilter.Sql, "Table `hotfix_data` references unknown DB2 store by hash 0x{0:X} and has no reference to `hotfix_blob` in hotfix id {1}", tableHash, id);
                    continue;
                }

                _hotfixData[MathFunctions.MakePair64(id, tableHash)] = recordId;
                deletedRecords[Tuple.Create(tableHash, recordId)] = deleted;

                ++count;
            } while (result.NextRow());

            foreach (var itr in deletedRecords)
            {
                if (itr.Value)
                {
                    var store = _storage.LookupByKey(itr.Key.Item1);
                    if (store != null)
                        store.EraseRecord((uint)itr.Key.Item2);
                }
            }

            Log.outInfo(LogFilter.Server, "Loaded {0} hotfix info entries in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadHotfixBlob()
        {
            uint oldMSTime = Time.GetMSTime();
            _hotfixBlob.Clear();

            SQLResult result = DB.Hotfix.Query("SELECT TableHash, RecordId, `Blob` FROM hotfix_blob ORDER BY TableHash");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 hotfix blob entries.");
                return;
            }

            do
            {
                uint tableHash = result.Read<uint>(0);
                var storeItr = _storage.LookupByKey(tableHash);
                if (storeItr != null)
                {
                    Log.outError(LogFilter.ServerLoading, $"Table hash 0x{tableHash:X} points to a loaded DB2 store {nameof(storeItr)}, fill related table instead of hotfix_blob");
                    continue;
                }

                int recordId = result.Read<int>(1);
                _hotfixBlob[Tuple.Create(tableHash, recordId)] = result.Read<byte[]>(2);
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {_hotfixBlob.Count} hotfix blob records in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public Dictionary<ulong, int> GetHotfixData() { return _hotfixData; }

        public byte[] GetHotfixBlobData(uint tableHash, int recordId)
        {
            return _hotfixBlob.LookupByKey(Tuple.Create(tableHash, recordId));
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

                AreaTableRecord objectArea = CliDB.AreaTableStorage.LookupByKey(objectAreaId);
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

        public string GetBroadcastTextValue(BroadcastTextRecord broadcastText, LocaleConstant locale = LocaleConstant.enUS, Gender gender = Gender.Male, bool forceGender = false)
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

        public bool HasCharacterFacialHairStyle(Race race, Gender gender, uint variationId)
        {
            return _characterFacialHairStyles.Contains(Tuple.Create((byte)race, (byte)gender, variationId));
        }

        public bool HasCharSections(Race race, Gender gender, CharBaseSectionVariation variation)
        {
            return _charSections.ContainsKey(Tuple.Create((byte)race, (byte)gender, variation));
        }

        public CharSectionsRecord GetCharSectionEntry(Race race, Gender gender, CharBaseSectionVariation variation, byte variationIndex, byte colorIndex)
        {
            var list = _charSections.LookupByKey(Tuple.Create((byte)race, (byte)gender, variation));
            foreach (var charSection in list)
                if (charSection.VariationIndex == variationIndex && charSection.ColorIndex == colorIndex)
                    return charSection;

            return null;
        }

        public CharStartOutfitRecord GetCharStartOutfitEntry(uint race, uint class_, uint gender)
        {
            return _charStartOutfits.LookupByKey(race | (class_ << 8) | (gender << 16));
        }

        public string GetClassName(Class class_, LocaleConstant locale = LocaleConstant.enUS)
        {
            ChrClassesRecord classEntry = CliDB.ChrClassesStorage.LookupByKey(class_);
            if (classEntry == null)
                return "";

            if (classEntry.Name[locale][0] != '\0')
                return classEntry.Name[locale];

            return classEntry.Name[LocaleConstant.enUS];
        }

        public uint GetPowerIndexByClass(PowerType powerType, Class classId)
        {
            return _powersByClass[(int)classId][(int)powerType];
        }

        public string GetChrRaceName(Race race, LocaleConstant locale = LocaleConstant.enUS)
        {
            ChrRacesRecord raceEntry = CliDB.ChrRacesStorage.LookupByKey(race);
            if (raceEntry == null)
                return "";

            if (raceEntry.Name[locale][0] != '\0')
                return raceEntry.Name[locale];

            return raceEntry.Name[LocaleConstant.enUS];
        }

        public ChrSpecializationRecord GetChrSpecializationByIndex(Class class_, uint index)
        {
            return _chrSpecializationsByIndex[(int)class_][index];
        }

        public ChrSpecializationRecord GetDefaultChrSpecializationForClass(Class class_)
        {
            return _defaultChrSpecializationsByClass.LookupByKey(class_);
        }

        public string GetCreatureFamilyPetName(CreatureFamily petfamily, LocaleConstant locale)
        {
            if (petfamily == CreatureFamily.None)
                return null;

            CreatureFamilyRecord petFamily = CliDB.CreatureFamilyStorage.LookupByKey(petfamily);
            if (petFamily == null)
                return "";

            return petFamily.Name[locale][0] != '\0' ? petFamily.Name[locale] : "";
        }

        static CurveInterpolationMode DetermineCurveType(CurveRecord curve, List<CurvePointRecord> points)
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

            CurveRecord curve = CliDB.CurveStorage.LookupByKey(curveId);
            switch (DetermineCurveType(curve, points))
            {
                case CurveInterpolationMode.Linear:
                    {
                        int pointIndex = 0;
                        while (pointIndex < points.Count && points[pointIndex].Pos.X <= x)
                            ++pointIndex;
                        if (pointIndex == 0)
                            return points[0].Pos.Y;
                        if (pointIndex >= points.Count)
                            return points.Last().Pos.Y;
                        float xDiff = points[pointIndex].Pos.X - points[pointIndex - 1].Pos.X;
                        if (xDiff == 0.0)
                            return points[pointIndex].Pos.Y;
                        return (((x - points[pointIndex - 1].Pos.X) / xDiff) * (points[pointIndex].Pos.Y - points[pointIndex - 1].Pos.Y)) + points[pointIndex - 1].Pos.Y;
                    }
                case CurveInterpolationMode.Cosine:
                    {
                        int pointIndex = 0;
                        while (pointIndex < points.Count && points[pointIndex].Pos.X <= x)
                            ++pointIndex;
                        if (pointIndex == 0)
                            return points[0].Pos.Y;
                        if (pointIndex >= points.Count)
                            return points.Last().Pos.Y;
                        float xDiff = points[pointIndex].Pos.X - points[pointIndex - 1].Pos.X;
                        if (xDiff == 0.0)
                            return points[pointIndex].Pos.Y;
                        return (float)((points[pointIndex].Pos.Y - points[pointIndex - 1].Pos.Y) * (1.0f - Math.Cos((x - points[pointIndex - 1].Pos.X) / xDiff * Math.PI)) * 0.5f) + points[pointIndex - 1].Pos.Y;
                    }
                case CurveInterpolationMode.CatmullRom:
                    {
                        int pointIndex = 1;
                        while (pointIndex < points.Count && points[pointIndex].Pos.X <= x)
                            ++pointIndex;
                        if (pointIndex == 1)
                            return points[1].Pos.Y;
                        if (pointIndex >= points.Count - 1)
                            return points[points.Count - 2].Pos.Y;
                        float xDiff = points[pointIndex].Pos.X - points[pointIndex - 1].Pos.X;
                        if (xDiff == 0.0)
                            return points[pointIndex].Pos.Y;

                        float mu = (x - points[pointIndex - 1].Pos.X) / xDiff;
                        float a0 = -0.5f * points[pointIndex - 2].Pos.Y + 1.5f * points[pointIndex - 1].Pos.Y - 1.5f * points[pointIndex].Pos.Y + 0.5f * points[pointIndex + 1].Pos.Y;
                        float a1 = points[pointIndex - 2].Pos.Y - 2.5f * points[pointIndex - 1].Pos.Y + 2.0f * points[pointIndex].Pos.Y - 0.5f * points[pointIndex + 1].Pos.Y;
                        float a2 = -0.5f * points[pointIndex - 2].Pos.Y + 0.5f * points[pointIndex].Pos.Y;
                        float a3 = points[pointIndex - 1].Pos.Y;

                        return a0 * mu * mu * mu + a1 * mu * mu + a2 * mu + a3;
                    }
                case CurveInterpolationMode.Bezier3:
                    {
                        float xDiff = points[2].Pos.X - points[0].Pos.X;
                        if (xDiff == 0.0)
                            return points[1].Pos.Y;
                        float mu = (x - points[0].Pos.X) / xDiff;
                        return ((1.0f - mu) * (1.0f - mu) * points[0].Pos.Y) + (1.0f - mu) * 2.0f * mu * points[1].Pos.Y + mu * mu * points[2].Pos.Y;
                    }
                case CurveInterpolationMode.Bezier4:
                    {
                        float xDiff = points[3].Pos.X - points[0].Pos.X;
                        if (xDiff == 0.0)
                            return points[1].Pos.Y;
                        float mu = (x - points[0].Pos.X) / xDiff;
                        return (1.0f - mu) * (1.0f - mu) * (1.0f - mu) * points[0].Pos.Y
                            + 3.0f * mu * (1.0f - mu) * (1.0f - mu) * points[1].Pos.Y
                            + 3.0f * mu * mu * (1.0f - mu) * points[2].Pos.Y
                            + mu * mu * mu * points[3].Pos.Y;
                    }
                case CurveInterpolationMode.Bezier:
                    {
                        float xDiff = points.Last().Pos.X - points[0].Pos.X;
                        if (xDiff == 0.0f)
                            return points.Last().Pos.Y;

                        float[] tmp = new float[points.Count];
                        for (int c = 0; c < points.Count; ++c)
                            tmp[c] = points[c].Pos.Y;

                        float mu = (x - points[0].Pos.X) / xDiff;
                        int i = points.Count - 1;
                        while (i > 0)
                        {
                            for (int k = 0; k < i; ++k)
                            {
                                float val = tmp[k] + mu * (tmp[k + 1] - tmp[k]);
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

            ExpectedStatModRecord[] mods = new ExpectedStatModRecord[3];
            ContentTuningRecord contentTuning = CliDB.ContentTuningStorage.LookupByKey(contentTuningId);
            if (contentTuning != null)
            {
                mods[0] = CliDB.ExpectedStatModStorage.LookupByKey(contentTuning.ExpectedStatModID);
                mods[1] = CliDB.ExpectedStatModStorage.LookupByKey(contentTuning.DifficultyESMID);
            }
            switch (unitClass)
            {
                case Class.Warrior:
                    mods[2] = CliDB.ExpectedStatModStorage.LookupByKey(4);
                    break;
                case Class.Paladin:
                    mods[2] = CliDB.ExpectedStatModStorage.LookupByKey(2);
                    break;
                case Class.Rogue:
                    mods[2] = CliDB.ExpectedStatModStorage.LookupByKey(3);
                    break;
                case Class.Mage:
                    mods[2] = CliDB.ExpectedStatModStorage.LookupByKey(1);
                    break;
                default:
                    break;
            }
            float value = 0.0f;
            switch (stat)
            {
                case ExpectedStatType.CreatureHealth:
                    value = mods.Sum(expectedStatMod => expectedStatRecord.CreatureHealth * (expectedStatMod != null ? expectedStatMod.CreatureHealthMod : 1.0f));
                    break;
                case ExpectedStatType.PlayerHealth:
                    value = mods.Sum(expectedStatMod => expectedStatRecord.PlayerHealth * (expectedStatMod != null ? expectedStatMod.PlayerHealthMod : 1.0f));
                    break;
                case ExpectedStatType.CreatureAutoAttackDps:
                    value = mods.Sum(expectedStatMod => expectedStatRecord.CreatureAutoAttackDps * (expectedStatMod != null ? expectedStatMod.CreatureAutoAttackDPSMod : 1.0f));
                    break;
                case ExpectedStatType.CreatureArmor:
                    value = mods.Sum(expectedStatMod => expectedStatRecord.CreatureArmor * (expectedStatMod != null ? expectedStatMod.CreatureArmorMod : 1.0f));
                    break;
                case ExpectedStatType.PlayerMana:
                    value = mods.Sum(expectedStatMod => expectedStatRecord.PlayerMana * (expectedStatMod != null ? expectedStatMod.PlayerManaMod : 1.0f));
                    break;
                case ExpectedStatType.PlayerPrimaryStat:
                    value = mods.Sum(expectedStatMod => expectedStatRecord.PlayerPrimaryStat * (expectedStatMod != null ? expectedStatMod.PlayerPrimaryStatMod : 1.0f));
                    break;
                case ExpectedStatType.PlayerSecondaryStat:
                    value = mods.Sum(expectedStatMod => expectedStatRecord.PlayerSecondaryStat * (expectedStatMod != null ? expectedStatMod.PlayerSecondaryStatMod : 1.0f));
                    break;
                case ExpectedStatType.ArmorConstant:
                    value = mods.Sum(expectedStatMod => expectedStatRecord.ArmorConstant * (expectedStatMod != null ? expectedStatMod.ArmorConstantMod : 1.0f));
                    break;
                case ExpectedStatType.None:
                    break;
                case ExpectedStatType.CreatureSpellDamage:
                    value = mods.Sum(expectedStatMod => expectedStatRecord.CreatureSpellDamage * (expectedStatMod != null ? expectedStatMod.CreatureSpellDamageMod : 1.0f));
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

        public HeirloomRecord GetHeirloomByItemId(uint itemId)
        {
            return _heirlooms.LookupByKey(itemId);
        }

        public List<uint> GetGlyphBindableSpells(uint glyphPropertiesId)
        {
            return _glyphBindableSpells.LookupByKey(glyphPropertiesId);
        }

        public List<uint> GetGlyphRequiredSpecs(uint glyphPropertiesId)
        {
            return _glyphRequiredSpecs.LookupByKey(glyphPropertiesId);
        }

        public List<ItemBonusRecord> GetItemBonusList(uint bonusListId)
        {
            return _itemBonusLists.LookupByKey(bonusListId);
        }

        public uint GetItemBonusListForItemLevelDelta(short delta)
        {
            return _itemLevelDeltaToBonusListContainer.LookupByKey(delta);
        }

        public List<uint> GetItemBonusTree(uint itemId, uint itemContext)
        {
            List<uint> bonusListIDs = new List<uint>();

            ItemSparseRecord proto = CliDB.ItemSparseStorage.LookupByKey(itemId);
            if (proto == null)
                return bonusListIDs;

            var itemIdRange = _itemToBonusTree.LookupByKey(itemId);
            if (itemIdRange.Empty())
                return bonusListIDs;

            foreach (var itemTreeId in itemIdRange)
            {
                var treeList = _itemBonusTrees.LookupByKey(itemTreeId);
                if (treeList.Empty())
                    continue;

                foreach (ItemBonusTreeNodeRecord bonusTreeNode in treeList)
                {
                    if (bonusTreeNode.ItemContext != itemContext)
                        continue;

                    if (bonusTreeNode.ChildItemBonusListID != 0)
                    {
                        bonusListIDs.Add(bonusTreeNode.ChildItemBonusListID);
                    }
                    else if (bonusTreeNode.ChildItemLevelSelectorID != 0)
                    {
                        ItemLevelSelectorRecord selector = CliDB.ItemLevelSelectorStorage.LookupByKey(bonusTreeNode.ChildItemLevelSelectorID);
                        if (selector == null)
                            continue;

                        short delta = (short)(selector.MinItemLevel - proto.ItemLevel);

                        uint bonus = GetItemBonusListForItemLevelDelta(delta);
                        if (bonus != 0)
                            bonusListIDs.Add(bonus);

                        ItemLevelSelectorQualitySetRecord selectorQualitySet = CliDB.ItemLevelSelectorQualitySetStorage.LookupByKey(selector.ItemLevelSelectorQualitySetID);
                        if (selectorQualitySet != null)
                        {
                            var itemSelectorQualities = _itemLevelQualitySelectorQualities.LookupByKey(selector.ItemLevelSelectorQualitySetID);
                            if (itemSelectorQualities != null)
                            {
                                ItemQuality quality = ItemQuality.Uncommon;
                                if (selector.MinItemLevel >= selectorQualitySet.IlvlEpic)
                                    quality = ItemQuality.Epic;
                                else if (selector.MinItemLevel >= selectorQualitySet.IlvlRare)
                                    quality = ItemQuality.Rare;

                                var itemSelectorQuality = itemSelectorQualities.FirstOrDefault(p => p.Quality < (byte)quality);

                                if (itemSelectorQuality != null)
                                    bonusListIDs.Add(itemSelectorQuality.QualityItemBonusListID);
                            }
                        }
                    }
                }
            }

            return bonusListIDs;
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
            ItemModifiedAppearanceRecord modifiedAppearance = GetItemModifiedAppearance(itemId, appearanceModId);
            if (modifiedAppearance != null)
            {
                ItemAppearanceRecord itemAppearance = CliDB.ItemAppearanceStorage.LookupByKey(modifiedAppearance.ItemAppearanceID);
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
            foreach (LFGDungeonsRecord dungeon in CliDB.LFGDungeonsStorage.Values)
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
            LiquidTypeRecord liq = CliDB.LiquidTypeStorage.LookupByKey(liquidType);
            if (liq != null)
                return 1u << liq.SoundBank;

            return 0;
        }

        public MapDifficultyRecord GetDefaultMapDifficulty(uint mapId)
        {
            Difficulty NotUsed = Difficulty.None;
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
                DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(pair.Key);
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
            DifficultyRecord diffEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);
            if (diffEntry == null)
                return GetDefaultMapDifficulty(mapId, ref difficulty);

            Difficulty tmpDiff = difficulty;
            MapDifficultyRecord mapDiff = GetMapDifficultyData(mapId, tmpDiff);
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

        public MountRecord GetMount(uint spellId)
        {
            return _mountsBySpellId.LookupByKey(spellId);
        }

        MountRecord GetMountById(uint id)
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

        public ResponseCodes ValidateName(string name, LocaleConstant locale)
        {
            foreach (var testName in _nameValidators[(int)locale])
                if (testName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return ResponseCodes.CharNameProfane;

            // regexes at TOTAL_LOCALES are loaded from NamesReserved which is not locale specific
            foreach (var testName in _nameValidators[(int)LocaleConstant.Total])
                if (testName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return ResponseCodes.CharNameReserved;

            return ResponseCodes.CharNameSuccess;
        }

        public uint GetNumTalentsAtLevel(uint level, Class playerClass)
        {
            NumTalentsAtLevelRecord numTalentsAtLevel = CliDB.NumTalentsAtLevelStorage.LookupByKey(level);
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
            int slots = 0;
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
            QuestV2Record v2 = CliDB.QuestV2Storage.LookupByKey(questId);
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
            foreach (PowerTypeRecord powerType in CliDB.PowerTypeStorage.Values)
            {
                string powerName = powerType.NameGlobalStringTag;
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

        public uint GetRulesetItemUpgrade(uint itemId)
        {
            return _rulesetItemUpgrade.LookupByKey(itemId);
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
                if (record.RaceMask != 0 && !Convert.ToBoolean(record.RaceMask & (1 << ((byte)race - 1))))
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

        bool IsValidSpellFamiliyName(SpellFamilyNames family)
        {
            return _spellFamilyNames.Contains((byte)family);
        }

        public List<SpellPowerRecord> GetSpellPowers(uint spellId, Difficulty difficulty = Difficulty.None)
        {
            bool notUsed;
            return GetSpellPowers(spellId, difficulty, out notUsed);
        }

        public List<SpellPowerRecord> GetSpellPowers(uint spellId, Difficulty difficulty, out bool hasDifficultyPowers)
        {
            SpellPowerRecord[] powers = new SpellPowerRecord[0];
            hasDifficultyPowers = false;

            var difficultyDic = _spellPowerDifficulties.LookupByKey(spellId);
            if (difficultyDic != null)
            {
                hasDifficultyPowers = true;

                DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);
                while (difficultyEntry != null)
                {
                    var powerDifficultyList = difficultyDic.LookupByKey(difficultyEntry.Id);
                    if (powerDifficultyList != null)
                    {
                        if (powerDifficultyList.Count > powers.Length)
                            Array.Resize(ref powers, powerDifficultyList.Count);

                        foreach (SpellPowerRecord difficultyPower in powerDifficultyList)
                            if (powers[difficultyPower.OrderIndex] == null)
                                powers[difficultyPower.OrderIndex] = difficultyPower;
                    }

                    difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficultyEntry.FallbackDifficultyID);
                }
            }

            var record = _spellPowers.LookupByKey(spellId);
            if (record != null)
            {
                if (record.Count > powers.Length)
                    Array.Resize(ref powers, record.Count);

                foreach (SpellPowerRecord power in record)
                    if (powers[power.OrderIndex] == null)
                        powers[power.OrderIndex] = power;
            }

            return powers.ToList();
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

            TotemCategoryRecord itemEntry = CliDB.TotemCategoryStorage.LookupByKey(itemTotemCategoryId);
            if (itemEntry == null)
                return false;
            TotemCategoryRecord reqEntry = CliDB.TotemCategoryStorage.LookupByKey(requiredTotemCategoryId);
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

        static bool CheckUiMapAssignmentStatus(float x, float y, float z, int mapId, int areaId, int wmoDoodadPlacementId, int wmoGroupId, UiMapAssignmentRecord uiMapAssignment, out UiMapAssignmentStatus status)
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
                        AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);
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
                    MapRecord mapEntry = CliDB.MapStorage.LookupByKey(mapId);
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
                    bool hasDoodadPlacement = false;
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

        UiMapAssignmentRecord FindNearestMapAssignment(float x, float y, float z, int mapId, int areaId, int wmoDoodadPlacementId, int wmoGroupId, UiMapSystem system)
        {
            UiMapAssignmentStatus nearestMapAssignment = new UiMapAssignmentStatus();
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

            AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);
            while (areaEntry != null)
            {
                iterateUiMapAssignments(_uiMapAssignmentByArea[(int)system], (int)areaEntry.Id);
                areaEntry = CliDB.AreaTableStorage.LookupByKey(areaEntry.ParentAreaID);
            }

            if (mapId > 0)
            {
                MapRecord mapEntry = CliDB.MapStorage.LookupByKey(mapId);
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

        Vector2 CalculateGlobalUiMapPosition(int uiMapID, Vector2 uiPosition)
        {
            UiMapRecord uiMap = CliDB.UiMapStorage.LookupByKey(uiMapID);
            while (uiMap != null)
            {
                if (uiMap.Type <= UiMapType.Continent)
                    break;

                UiMapBounds bounds = _uiMapBounds.LookupByKey(uiMap.Id);
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

            UiMapAssignmentRecord uiMapAssignment = FindNearestMapAssignment(x, y, z, mapId, areaId, wmoDoodadPlacementId, wmoGroupId, system);
            if (uiMapAssignment == null)
                return false;

            uiMapId = uiMapAssignment.UiMapID;

            Vector2 relativePosition = new Vector2(0.5f, 0.5f);
            Vector2 regionSize = new Vector2(uiMapAssignment.Region[1].X - uiMapAssignment.Region[0].X, uiMapAssignment.Region[1].Y - uiMapAssignment.Region[0].Y);
            if (regionSize.X > 0.0f)
                relativePosition.X = (x - uiMapAssignment.Region[0].X) / regionSize.X;
            if (regionSize.Y > 0.0f)
                relativePosition.Y = (y - uiMapAssignment.Region[0].Y) / regionSize.Y;

            // x any y are swapped
            Vector2 uiPosition = new Vector2(((1.0f - (1.0f - relativePosition.Y)) * uiMapAssignment.UiMin.X) + ((1.0f - relativePosition.Y) * uiMapAssignment.UiMax.X), ((1.0f - (1.0f - relativePosition.X)) * uiMapAssignment.UiMin.Y) + ((1.0f - relativePosition.X) * uiMapAssignment.UiMax.Y));

            if (!local)
                uiPosition = CalculateGlobalUiMapPosition(uiMapAssignment.UiMapID, uiPosition);

            newPos = uiPosition;
            return true;
        }

        public void Zone2MapCoordinates(uint areaId, ref float x, ref float y)
        {
            AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);
            if (areaEntry == null)
                return;

            foreach (var assignment in _uiMapAssignmentByArea[(int)UiMapSystem.World].LookupByKey(areaId))
            {
                if (assignment.MapID >= 0 && assignment.MapID != areaEntry.ContinentID)
                    continue;

                float tmpY = 1.0f - ((y - assignment.UiMin.Y) / (assignment.UiMax.Y - assignment.UiMin.Y));
                float tmpX = 1.0f - ((x - assignment.UiMin.X) / (assignment.UiMax.X - assignment.UiMin.X));
                y = ((1.0f - tmpY) * assignment.Region[0].X) + (tmpY * assignment.Region[1].X);
                x = ((1.0f - tmpX) * assignment.Region[0].Y) + (tmpX * assignment.Region[1].Y);
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

        Dictionary<uint, IDB2Storage> _storage = new Dictionary<uint, IDB2Storage>();
        Dictionary<ulong, int> _hotfixData = new Dictionary<ulong, int>();
        Dictionary<Tuple<uint, int>, byte[]> _hotfixBlob = new Dictionary<Tuple<uint, int>, byte[]>();

        MultiMap<uint, uint> _areaGroupMembers = new MultiMap<uint, uint>();
        MultiMap<uint, ArtifactPowerRecord> _artifactPowers = new MultiMap<uint, ArtifactPowerRecord>();
        MultiMap<uint, uint> _artifactPowerLinks = new MultiMap<uint, uint>();
        Dictionary<Tuple<uint, byte>, ArtifactPowerRankRecord> _artifactPowerRanks = new Dictionary<Tuple<uint, byte>, ArtifactPowerRankRecord>();
        List<Tuple<byte, byte, uint>> _characterFacialHairStyles = new List<Tuple<byte, byte, uint>>();
        MultiMap<Tuple<byte, byte, CharBaseSectionVariation>, CharSectionsRecord> _charSections = new MultiMap<Tuple<byte, byte, CharBaseSectionVariation>, CharSectionsRecord>();
        Dictionary<uint, CharStartOutfitRecord> _charStartOutfits = new Dictionary<uint, CharStartOutfitRecord>();
        uint[][] _powersByClass = new uint[(int)Class.Max][];
        ChrSpecializationRecord[][] _chrSpecializationsByIndex = new ChrSpecializationRecord[(int)Class.Max + 1][];
        Dictionary<uint, ChrSpecializationRecord> _defaultChrSpecializationsByClass = new Dictionary<uint, ChrSpecializationRecord>();
        MultiMap<uint, CurvePointRecord> _curvePoints = new MultiMap<uint, CurvePointRecord>();
        Dictionary<Tuple<uint, byte, byte, byte>, EmotesTextSoundRecord> _emoteTextSounds = new Dictionary<Tuple<uint, byte, byte, byte>, EmotesTextSoundRecord>();
        Dictionary<Tuple<uint, int>, ExpectedStatRecord> _expectedStatsByLevel = new Dictionary<Tuple<uint, int>, ExpectedStatRecord>();
        MultiMap<uint, uint> _factionTeams = new MultiMap<uint, uint>();
        Dictionary<uint, HeirloomRecord> _heirlooms = new Dictionary<uint, HeirloomRecord>();
        MultiMap<uint, uint> _glyphBindableSpells = new MultiMap<uint, uint>();
        MultiMap<uint, uint> _glyphRequiredSpecs = new MultiMap<uint, uint>();
        MultiMap<uint, ItemBonusRecord> _itemBonusLists = new MultiMap<uint, ItemBonusRecord>();
        Dictionary<short, uint> _itemLevelDeltaToBonusListContainer = new Dictionary<short, uint>();
        MultiMap<uint, ItemBonusTreeNodeRecord> _itemBonusTrees = new MultiMap<uint, ItemBonusTreeNodeRecord>();
        Dictionary<uint, ItemChildEquipmentRecord> _itemChildEquipment = new Dictionary<uint, ItemChildEquipmentRecord>();
        Array<ItemClassRecord> _itemClassByOldEnum = new Array<ItemClassRecord>(19);
        List<uint> _itemsWithCurrencyCost = new List<uint>();
        MultiMap<uint, ItemLimitCategoryConditionRecord>  _itemCategoryConditions = new MultiMap<uint, ItemLimitCategoryConditionRecord>();
        MultiMap<uint, ItemLevelSelectorQualityRecord> _itemLevelQualitySelectorQualities = new MultiMap<uint, ItemLevelSelectorQualityRecord>();
        Dictionary<uint, ItemModifiedAppearanceRecord> _itemModifiedAppearancesByItem = new Dictionary<uint, ItemModifiedAppearanceRecord>();
        MultiMap<uint, uint> _itemToBonusTree = new MultiMap<uint, uint>();
        MultiMap<uint, ItemSetSpellRecord> _itemSetSpells = new MultiMap<uint, ItemSetSpellRecord>();
        MultiMap<uint, ItemSpecOverrideRecord> _itemSpecOverrides = new MultiMap<uint, ItemSpecOverrideRecord>();
        Dictionary<uint, Dictionary<uint, MapDifficultyRecord>> _mapDifficulties = new Dictionary<uint, Dictionary<uint, MapDifficultyRecord>>();
        Dictionary<uint, MountRecord> _mountsBySpellId = new Dictionary<uint, MountRecord>();
        MultiMap<uint, MountTypeXCapabilityRecord> _mountCapabilitiesByType = new MultiMap<uint, MountTypeXCapabilityRecord>();
        MultiMap<uint, MountXDisplayRecord> _mountDisplays = new MultiMap<uint, MountXDisplayRecord>();
        Dictionary<uint, List<NameGenRecord>[]> _nameGenData = new Dictionary<uint, List<NameGenRecord>[]>();
        List<string>[] _nameValidators = new List<string>[(int)LocaleConstant.Total + 1];
        MultiMap<uint, uint> _phasesByGroup = new MultiMap<uint, uint>();
        Dictionary<PowerType, PowerTypeRecord> _powerTypes = new Dictionary<PowerType, PowerTypeRecord>();
        Dictionary<uint, byte> _pvpItemBonus = new Dictionary<uint, byte>();
        PvpTalentSlotUnlockRecord[] _pvpTalentSlotUnlock = new PvpTalentSlotUnlockRecord[PlayerConst.MaxPvpTalentSlots];
        Dictionary<uint, Tuple<List<QuestPackageItemRecord>, List<QuestPackageItemRecord>>> _questPackages = new Dictionary<uint, Tuple<List<QuestPackageItemRecord>, List<QuestPackageItemRecord>>>();
        MultiMap<uint, RewardPackXCurrencyTypeRecord> _rewardPackCurrencyTypes = new MultiMap<uint, RewardPackXCurrencyTypeRecord>();
        MultiMap<uint, RewardPackXItemRecord> _rewardPackItems = new MultiMap<uint, RewardPackXItemRecord>();
        Dictionary<uint, uint> _rulesetItemUpgrade = new Dictionary<uint, uint>();
        MultiMap<uint, SkillLineAbilityRecord> _skillLineAbilitiesBySkillupSkill = new MultiMap<uint, SkillLineAbilityRecord>();
        MultiMap<uint, SkillRaceClassInfoRecord> _skillRaceClassInfoBySkill = new MultiMap<uint, SkillRaceClassInfoRecord>();
        MultiMap<uint, SpecializationSpellsRecord> _specializationSpellsBySpec = new MultiMap<uint, SpecializationSpellsRecord>();
        List<byte> _spellFamilyNames = new List<byte>();
        Dictionary<uint, List<SpellPowerRecord>> _spellPowers = new Dictionary<uint, List<SpellPowerRecord>>();
        Dictionary<uint, Dictionary<uint, List<SpellPowerRecord>>> _spellPowerDifficulties = new Dictionary<uint, Dictionary<uint, List<SpellPowerRecord>>>();
        MultiMap<uint, SpellProcsPerMinuteModRecord> _spellProcsPerMinuteMods = new MultiMap<uint, SpellProcsPerMinuteModRecord>();
        List<TalentRecord>[][][] _talentsByPosition = new List<TalentRecord>[(int)Class.Max][][];
        List<uint> _toys = new List<uint>();
        MultiMap<uint, TransmogSetRecord> _transmogSetsByItemModifiedAppearance = new MultiMap<uint, TransmogSetRecord>();
        MultiMap<uint, TransmogSetItemRecord> _transmogSetItemsByTransmogSet = new MultiMap<uint, TransmogSetItemRecord>();
        Dictionary<int, UiMapBounds> _uiMapBounds = new Dictionary<int, UiMapBounds>();
        MultiMap<int, UiMapAssignmentRecord>[] _uiMapAssignmentByMap = new MultiMap<int, UiMapAssignmentRecord>[(int)UiMapSystem.Max];
        MultiMap<int, UiMapAssignmentRecord>[] _uiMapAssignmentByArea = new MultiMap<int, UiMapAssignmentRecord>[(int)UiMapSystem.Max];
        MultiMap<int, UiMapAssignmentRecord>[] _uiMapAssignmentByWmoDoodadPlacement = new MultiMap<int, UiMapAssignmentRecord>[(int)UiMapSystem.Max];
        MultiMap<int, UiMapAssignmentRecord>[] _uiMapAssignmentByWmoGroup = new MultiMap<int, UiMapAssignmentRecord>[(int)UiMapSystem.Max];
        List<int> _uiMapPhases = new List<int>();
        Dictionary<Tuple<short, sbyte, int>, WMOAreaTableRecord> _wmoAreaTableLookup = new Dictionary<Tuple<short, sbyte, int>, WMOAreaTableRecord>();
    }

    class UiMapBounds
    {
        // these coords are mixed when calculated and used... its a mess
        public float[] Bounds = new float[4];
        public bool IsUiAssignment;
        public bool IsUiLink;
    }

    class UiMapAssignmentStatus
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

        bool IsInside()
        {
            return Outside.DistanceToRegionEdgeSquared < float.Epsilon &&
                Math.Abs(Outside.DistanceToRegionTop) < float.Epsilon &&
                Math.Abs(Outside.DistanceToRegionBottom) < float.Epsilon;
        }

        public static bool operator <(UiMapAssignmentStatus left, UiMapAssignmentStatus right)
        {
            bool leftInside = left.IsInside();
            bool rightInside = right.IsInside();
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

                float leftUiSizeX = left.UiMapAssignment != null ? (left.UiMapAssignment.UiMax.X - left.UiMapAssignment.UiMin.X) : 0.0f;
                float rightUiSizeX = right.UiMapAssignment != null ? (right.UiMapAssignment.UiMax.X - right.UiMapAssignment.UiMin.X) : 0.0f;

                if (leftUiSizeX > float.Epsilon && rightUiSizeX > float.Epsilon)
                {
                    float leftScale = (left.UiMapAssignment.Region[1].X - left.UiMapAssignment.Region[0].X) / leftUiSizeX;
                    float rightScale = (right.UiMapAssignment.Region[1].X - right.UiMapAssignment.Region[0].X) / rightUiSizeX;
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
            bool leftInside = left.IsInside();
            bool rightInside = right.IsInside();
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

                float leftUiSizeX = left.UiMapAssignment != null ? (left.UiMapAssignment.UiMax.X - left.UiMapAssignment.UiMin.X) : 0.0f;
                float rightUiSizeX = right.UiMapAssignment != null ? (right.UiMapAssignment.UiMax.X - right.UiMapAssignment.UiMin.X) : 0.0f;

                if (leftUiSizeX > float.Epsilon && rightUiSizeX > float.Epsilon)
                {
                    float leftScale = (left.UiMapAssignment.Region[1].X - left.UiMapAssignment.Region[0].X) / leftUiSizeX;
                    float rightScale = (right.UiMapAssignment.Region[1].X - right.UiMapAssignment.Region[0].X) / rightUiSizeX;
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

    class ChrClassesXPowerTypesRecordComparer : IComparer<ChrClassesXPowerTypesRecord>
    {
        public int Compare(ChrClassesXPowerTypesRecord left, ChrClassesXPowerTypesRecord right)
        {
            if (left.ClassID != right.ClassID)
                return left.ClassID.CompareTo(right.ClassID);
            return left.PowerType.CompareTo(right.PowerType);
        }
    }

    class MountTypeXCapabilityRecordComparer : IComparer<MountTypeXCapabilityRecord>
    {
        public int Compare(MountTypeXCapabilityRecord left, MountTypeXCapabilityRecord right)
        {
            if (left.MountTypeID == right.MountTypeID)
                return left.OrderIndex.CompareTo(right.OrderIndex);
            return left.Id.CompareTo(right.Id);
        }
    }

    class ItemLevelSelectorQualityRecordComparator : IComparer<ItemLevelSelectorQualityRecord>
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

    enum CurveInterpolationMode
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
