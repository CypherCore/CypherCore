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

using Framework.Constants;
using Framework.Database;
using Framework.GameMath;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
                _nameValidators[i] = new List<Regex>();
        }

        public void LoadStores()
        {
            foreach (var areaGroupMember in CliDB.AreaGroupMemberStorage.Values)
                _areaGroupMembers.Add(areaGroupMember.AreaGroupID, areaGroupMember.AreaID);

            foreach (ArtifactPowerRecord artifactPower in CliDB.ArtifactPowerStorage.Values)
                _artifactPowers.Add(artifactPower.ArtifactID, artifactPower);

            foreach (ArtifactPowerLinkRecord artifactPowerLink in CliDB.ArtifactPowerLinkStorage.Values)
            {
                _artifactPowerLinks.Add(artifactPowerLink.FromArtifactPowerID, artifactPowerLink.ToArtifactPowerID);
                _artifactPowerLinks.Add(artifactPowerLink.ToArtifactPowerID, artifactPowerLink.FromArtifactPowerID);
            }

            foreach (ArtifactPowerRankRecord artifactPowerRank in CliDB.ArtifactPowerRankStorage.Values)
                _artifactPowerRanks[Tuple.Create((uint)artifactPowerRank.ArtifactPowerID, artifactPowerRank.Rank)] = artifactPowerRank;

            foreach (CharacterFacialHairStylesRecord characterFacialStyle in CliDB.CharacterFacialHairStylesStorage.Values)
                _characterFacialHairStyles.Add(Tuple.Create(characterFacialStyle.RaceID, characterFacialStyle.SexID, (uint)characterFacialStyle.VariationID));

            CharBaseSectionVariation[] sectionToBase = new CharBaseSectionVariation[(int)CharSectionType.Max];
            foreach (CharBaseSectionRecord charBaseSection in CliDB.CharBaseSectionStorage.Values)
            {
                Contract.Assert(charBaseSection.ResolutionVariation < (byte)CharSectionType.Max, $"SECTION_TYPE_MAX ({(byte)CharSectionType.Max}) must be equal to or greater than {charBaseSection.ResolutionVariation + 1}");
                Contract.Assert(charBaseSection.Variation < CharBaseSectionVariation.Max, $"CharBaseSectionVariation.Max {(byte)CharBaseSectionVariation.Max} must be equal to or greater than {charBaseSection.Variation + 1}");

                sectionToBase[charBaseSection.ResolutionVariation] = (CharBaseSectionVariation)charBaseSection.Variation;
            }

            MultiMap<Tuple<byte, byte, CharBaseSectionVariation>, Tuple<byte, byte>> addedSections = new MultiMap<Tuple<byte, byte, CharBaseSectionVariation>, Tuple<byte, byte>>();
            foreach (CharSectionsRecord charSection in CliDB.CharSectionsStorage.Values)
            {
                Contract.Assert(charSection.BaseSection < (byte)CharSectionType.Max, $"SECTION_TYPE_MAX ({(byte)CharSectionType.Max}) must be equal to or greater than {charSection.BaseSection + 1}");

                Tuple<byte, byte, CharBaseSectionVariation> sectionKey = Tuple.Create(charSection.RaceID, charSection.SexID, sectionToBase[charSection.BaseSection]);
                Tuple<byte, byte> sectionCombination = Tuple.Create(charSection.VariationIndex, charSection.ColorIndex);
                if (addedSections.Contains(sectionKey, sectionCombination))
                    continue;

                addedSections.Add(sectionKey, sectionCombination);
                _charSections.Add(sectionKey, charSection);
            }

            foreach (var outfit in CliDB.CharStartOutfitStorage.Values)
                _charStartOutfits[(uint)(outfit.RaceID | (outfit.ClassID << 8) | (outfit.GenderID << 16))] = outfit;

            var powers = new List<ChrClassesXPowerTypesRecord>();
            foreach (var chrClasses in CliDB.ChrClassesXPowerTypesStorage.Values)
                powers.Add(chrClasses);

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
                    _defaultChrSpecializationsByClass[chrSpec.ClassID] = chrSpec;
            }

            foreach (CurvePointRecord curvePoint in CliDB.CurvePointStorage.Values)
            {
                if (CliDB.CurveStorage.ContainsKey(curvePoint.CurveID))
                    _curvePoints.Add(curvePoint.CurveID, curvePoint);
            }

            foreach (var key in _curvePoints.Keys.ToList())
                _curvePoints[key] = _curvePoints[key].OrderBy(point => point.Index).ToList();

            foreach (EmotesTextSoundRecord emoteTextSound in CliDB.EmotesTextSoundStorage.Values)
                _emoteTextSounds[Tuple.Create((uint)emoteTextSound.EmotesTextId, emoteTextSound.RaceId, emoteTextSound.SexId, emoteTextSound.ClassId)] = emoteTextSound;

            foreach (FactionRecord faction in CliDB.FactionStorage.Values)
                if (faction.ParentFactionID != 0)
                    _factionTeams.Add(faction.ParentFactionID, faction.Id);

            foreach (GameObjectDisplayInfoRecord gameObjectDisplayInfo in CliDB.GameObjectDisplayInfoStorage.Values)
            {
                if (gameObjectDisplayInfo.GeoBoxMax.X < gameObjectDisplayInfo.GeoBoxMin.X)
                    Extensions.Swap(ref gameObjectDisplayInfo.GeoBoxMax.X, ref gameObjectDisplayInfo.GeoBoxMin.X);
                if (gameObjectDisplayInfo.GeoBoxMax.Y < gameObjectDisplayInfo.GeoBoxMin.Y)
                    Extensions.Swap(ref gameObjectDisplayInfo.GeoBoxMax.Y, ref gameObjectDisplayInfo.GeoBoxMin.Y);
                if (gameObjectDisplayInfo.GeoBoxMax.Z < gameObjectDisplayInfo.GeoBoxMin.Z)
                    Extensions.Swap(ref gameObjectDisplayInfo.GeoBoxMax.Z, ref gameObjectDisplayInfo.GeoBoxMin.Z);
            }

            foreach (HeirloomRecord heirloom in CliDB.HeirloomStorage.Values)
                _heirlooms[heirloom.ItemID] = heirloom;

            foreach (GlyphBindableSpellRecord glyphBindableSpell in CliDB.GlyphBindableSpellStorage.Values)
                _glyphBindableSpells.Add(glyphBindableSpell.GlyphPropertiesID, glyphBindableSpell.SpellID);

            foreach (GlyphRequiredSpecRecord glyphRequiredSpec in CliDB.GlyphRequiredSpecStorage.Values)
                _glyphRequiredSpecs.Add(glyphRequiredSpec.GlyphPropertiesID, glyphRequiredSpec.ChrSpecializationID);

            foreach (var bonus in CliDB.ItemBonusStorage.Values)
                _itemBonusLists.Add(bonus.BonusListID, bonus);

            foreach (ItemBonusListLevelDeltaRecord itemBonusListLevelDelta in CliDB.ItemBonusListLevelDeltaStorage.Values)
                _itemLevelDeltaToBonusListContainer[itemBonusListLevelDelta.Delta] = itemBonusListLevelDelta.Id;

            foreach (var key in CliDB.ItemBonusTreeNodeStorage.Keys)
            {
                ItemBonusTreeNodeRecord bonusTreeNode = CliDB.ItemBonusTreeNodeStorage[key];
                uint bonusTreeId = bonusTreeNode.BonusTreeID;
                while (bonusTreeNode != null)
                {
                    _itemBonusTrees.Add(bonusTreeId, bonusTreeNode);
                    bonusTreeNode = CliDB.ItemBonusTreeNodeStorage.LookupByKey(bonusTreeNode.SubTreeID);
                }
            }

            foreach (ItemChildEquipmentRecord itemChildEquipment in CliDB.ItemChildEquipmentStorage.Values)
            {
                //ASSERT(_itemChildEquipment.find(itemChildEquipment.ItemID) == _itemChildEquipment.end(), "Item must have max 1 child item.");
                _itemChildEquipment[itemChildEquipment.ItemID] = itemChildEquipment;
            }

            foreach (ItemClassRecord itemClass in CliDB.ItemClassStorage.Values)
            {
                //ASSERT(itemClass->OldEnumValue < _itemClassByOldEnum.size());
                //ASSERT(!_itemClassByOldEnum[itemClass->OldEnumValue]);
                _itemClassByOldEnum[itemClass.OldEnumValue] = itemClass;
            }

            foreach (ItemCurrencyCostRecord itemCurrencyCost in CliDB.ItemCurrencyCostStorage.Values)
                _itemsWithCurrencyCost.Add(itemCurrencyCost.ItemId);

            foreach (ItemLevelSelectorQualityRecord itemLevelSelectorQuality in CliDB.ItemLevelSelectorQualityStorage.Values)
                _itemLevelQualitySelectorQualities.Add(itemLevelSelectorQuality.ItemLevelSelectorQualitySetID, itemLevelSelectorQuality);
            
            foreach (var appearanceMod in CliDB.ItemModifiedAppearanceStorage.Values)
            {
                //ASSERT(appearanceMod.ItemID <= 0xFFFFFF);
                _itemModifiedAppearancesByItem[(uint)((int)appearanceMod.ItemID | (appearanceMod.AppearanceModID << 24))] = appearanceMod;
            }

            foreach (ItemSetSpellRecord itemSetSpell in CliDB.ItemSetSpellStorage.Values)
                _itemSetSpells.Add(itemSetSpell.ItemSetID, itemSetSpell);

            foreach (var itemSpecOverride in CliDB.ItemSpecOverrideStorage.Values)
                _itemSpecOverrides.Add(itemSpecOverride.ItemID, itemSpecOverride);

            foreach (var itemBonusTreeAssignment in CliDB.ItemXBonusTreeStorage.Values)
                _itemToBonusTree.Add(itemBonusTreeAssignment.ItemID, itemBonusTreeAssignment.BonusTreeID);

            foreach (MapDifficultyRecord entry in CliDB.MapDifficultyStorage.Values)
            {
                if (!_mapDifficulties.ContainsKey(entry.MapID))
                    _mapDifficulties[entry.MapID] = new Dictionary<uint, MapDifficultyRecord>();

                _mapDifficulties[entry.MapID][entry.DifficultyID] = entry;
            }
            _mapDifficulties[0][0] = _mapDifficulties[1][0]; // map 0 is missing from MapDifficulty.dbc so we cheat a bit

            foreach (var mount in CliDB.MountStorage.Values)
                _mountsBySpellId[mount.SpellId] = mount;

            foreach (MountTypeXCapabilityRecord mountTypeCapability in CliDB.MountTypeXCapabilityStorage.Values)
                _mountCapabilitiesByType.Add(mountTypeCapability.MountTypeID, mountTypeCapability);

            foreach (var key in _mountCapabilitiesByType.Keys)
                _mountCapabilitiesByType[key].Sort(new MountTypeXCapabilityRecordComparer());

            foreach (MountXDisplayRecord mountDisplay in CliDB.MountXDisplayStorage.Values)
                _mountDisplays.Add(mountDisplay.MountID, mountDisplay);

            foreach (var entry in CliDB.NameGenStorage.Values)
            {
                if (!_nameGenData.ContainsKey(entry.Race))
                {
                    _nameGenData[entry.Race] = new List<NameGenRecord>[2];
                    for (var i = 0; i < 2; ++i)
                        _nameGenData[entry.Race][i] = new List<NameGenRecord>();
                }

                _nameGenData[entry.Race][entry.Sex].Add(entry);
            }

            foreach (var namesProfanity in CliDB.NamesProfanityStorage.Values)
            {
                Contract.Assert(namesProfanity.Language < (int)LocaleConstant.Total || namesProfanity.Language == -1);
                if (namesProfanity.Language != -1)
                    _nameValidators[namesProfanity.Language].Add(new Regex(namesProfanity.Name, RegexOptions.IgnoreCase | RegexOptions.Compiled));
                else
                    for (uint i = 0; i < (int)LocaleConstant.Total; ++i)
                    {
                        if (i == (int)LocaleConstant.None)
                            continue;

                        _nameValidators[i].Add(new Regex(namesProfanity.Name, RegexOptions.IgnoreCase | RegexOptions.Compiled));
                    }
            }

            foreach (var namesReserved in CliDB.NamesReservedStorage.Values)
                _nameValidators[(int)LocaleConstant.Total].Add(new Regex(namesReserved.Name, RegexOptions.IgnoreCase | RegexOptions.Compiled));

            foreach (var namesReserved in CliDB.NamesReservedLocaleStorage.Values)
            {
                Contract.Assert(!Convert.ToBoolean(namesReserved.LocaleMask & ~((1 << (int)LocaleConstant.Total) - 1)));
                for (int i = 0; i < (int)LocaleConstant.Total; ++i)
                {
                    if (i == (int)LocaleConstant.None)
                        continue;

                    if (Convert.ToBoolean(namesReserved.LocaleMask & (1 << i)))
                        _nameValidators[i].Add(new Regex(namesReserved.Name, RegexOptions.IgnoreCase | RegexOptions.Compiled));
                }
            }

            foreach (var group in CliDB.PhaseXPhaseGroupStorage.Values)
            {
                PhaseRecord phase = CliDB.PhaseStorage.LookupByKey(group.PhaseId);
                if (phase != null)
                    _phasesByGroup.Add(group.PhaseGroupID, phase.Id);
            }

            foreach (PowerTypeRecord powerType in CliDB.PowerTypeStorage.Values)
            {
                Contract.Assert(powerType.PowerTypeEnum < PowerType.Max);

                _powerTypes[powerType.PowerTypeEnum] = powerType;
            }

            foreach (PvpRewardRecord pvpReward in CliDB.PvpRewardStorage.Values)
                _pvpRewardPack[Tuple.Create(pvpReward.Prestige, pvpReward.HonorLevel)] = pvpReward.RewardPackID;

            foreach (QuestPackageItemRecord questPackageItem in CliDB.QuestPackageItemStorage.Values)
            {
                if (!_questPackages.ContainsKey(questPackageItem.QuestPackageID))
                    _questPackages[questPackageItem.QuestPackageID] = Tuple.Create(new List<QuestPackageItemRecord>(), new List<QuestPackageItemRecord>());

                if (questPackageItem.FilterType != QuestPackageFilter.Unmatched)
                    _questPackages[questPackageItem.QuestPackageID].Item1.Add(questPackageItem);
                else
                    _questPackages[questPackageItem.QuestPackageID].Item2.Add(questPackageItem);
            }

            foreach (RewardPackXItemRecord rewardPackXItem in CliDB.RewardPackXItemStorage.Values)
                _rewardPackItems.Add(rewardPackXItem.RewardPackID, rewardPackXItem);

            foreach (RulesetItemUpgradeRecord rulesetItemUpgrade in CliDB.RulesetItemUpgradeStorage.Values)
                _rulesetItemUpgrade[rulesetItemUpgrade.ItemID] = rulesetItemUpgrade.ItemUpgradeID;

            foreach (SkillRaceClassInfoRecord entry in CliDB.SkillRaceClassInfoStorage.Values)
            {
                if (CliDB.SkillLineStorage.ContainsKey(entry.SkillID))
                    _skillRaceClassInfoBySkill.Add(entry.SkillID, entry);
            }

            foreach (var specSpells in CliDB.SpecializationSpellsStorage.Values)
                _specializationSpellsBySpec.Add(specSpells.SpecID, specSpells);

            foreach (SpellClassOptionsRecord classOption in CliDB.SpellClassOptionsStorage.Values)
                _spellFamilyNames.Add(classOption.SpellClassSet);

            foreach (var power in CliDB.SpellPowerStorage.Values)
            {
                SpellPowerDifficultyRecord powerDifficulty = CliDB.SpellPowerDifficultyStorage.LookupByKey(power.Id);
                if (powerDifficulty != null)
                {
                    if (!_spellPowerDifficulties.ContainsKey(power.SpellID))
                        _spellPowerDifficulties[power.SpellID] = new Dictionary<uint, List<SpellPowerRecord>>();

                    if (!_spellPowerDifficulties[power.SpellID].ContainsKey(powerDifficulty.DifficultyID))
                        _spellPowerDifficulties[power.SpellID][powerDifficulty.DifficultyID] = new List<SpellPowerRecord>();

                    _spellPowerDifficulties[power.SpellID][powerDifficulty.DifficultyID].Insert(powerDifficulty.PowerIndex, power);
                }
                else
                {
                    if (!_spellPowers.ContainsKey(power.SpellID))
                        _spellPowers[power.SpellID] = new List<SpellPowerRecord>();

                    _spellPowers[power.SpellID].Insert(power.PowerIndex, power);
                }
            }

            foreach (SpellProcsPerMinuteModRecord ppmMod in CliDB.SpellProcsPerMinuteModStorage.Values)
                _spellProcsPerMinuteMods.Add(ppmMod.SpellProcsPerMinuteID, ppmMod);

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

            foreach (TransmogSetItemRecord transmogSetItem in CliDB.TransmogSetItemStorage.Values)
            {
                TransmogSetRecord set = CliDB.TransmogSetStorage.LookupByKey(transmogSetItem.TransmogSetID);
                if (set == null)
                    continue;

                _transmogSetsByItemModifiedAppearance.Add(transmogSetItem.ItemModifiedAppearanceID, set);
                _transmogSetItemsByTransmogSet.Add(transmogSetItem.TransmogSetID, transmogSetItem);
            }

            foreach (WMOAreaTableRecord entry in CliDB.WMOAreaTableStorage.Values)
                _wmoAreaTableLookup[Tuple.Create(entry.WMOID, entry.NameSet, entry.WMOGroupID)] = entry;

            foreach (WorldMapAreaRecord worldMapArea in CliDB.WorldMapAreaStorage.Values)
                _worldMapAreaByAreaID[worldMapArea.AreaID] = worldMapArea;

            ClearNotUsedTables();
        }

        void ClearNotUsedTables()
        {
            CliDB.AreaGroupMemberStorage = null;
            CliDB.ArtifactPowerLinkStorage = null;
            CliDB.ArtifactPowerRankStorage = null;
            CliDB.CharSectionsStorage = null;
            CliDB.ChrClassesXPowerTypesStorage = null;
            CliDB.CurvePointStorage = null;
            CliDB.EmotesTextSoundStorage = null;
            CliDB.GlyphBindableSpellStorage = null;
            CliDB.GlyphRequiredSpecStorage = null;
            CliDB.HeirloomStorage = null;
            CliDB.ItemBonusStorage = null;
            CliDB.ItemBonusListLevelDeltaStorage = null;
            CliDB.ItemBonusTreeNodeStorage = null;
            CliDB.ItemClassStorage = null;
            CliDB.ItemChildEquipmentStorage = null;
            CliDB.ItemCurrencyCostStorage = null;
            CliDB.ItemSetSpellStorage = null;
            CliDB.ItemSpecOverrideStorage = null;
            CliDB.ItemXBonusTreeStorage = null;
            CliDB.MapDifficultyStorage = null;
            CliDB.MountTypeXCapabilityStorage = null;
            CliDB.MountXDisplayStorage = null;
            CliDB.NameGenStorage = null;
            CliDB.NamesProfanityStorage = null;
            CliDB.NamesReservedStorage = null;
            CliDB.NamesReservedLocaleStorage = null;
            CliDB.PhaseXPhaseGroupStorage = null;
            CliDB.PowerTypeStorage = null;
            CliDB.PvpRewardStorage = null;
            CliDB.QuestPackageItemStorage = null;
            CliDB.RewardPackXItemStorage = null;
            CliDB.RulesetItemUpgradeStorage = null;
            CliDB.SpecializationSpellsStorage = null;
            CliDB.SpellPowerDifficultyStorage = null;
            CliDB.SpellPowerStorage = null;
            CliDB.SpellProcsPerMinuteModStorage = null;
            CliDB.ToyStorage = null;
            CliDB.WMOAreaTableStorage = null;
            CliDB.WorldMapAreaStorage = null;
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
                if (!_storage.ContainsKey(tableHash))
                {
                    Log.outError(LogFilter.Sql, "Table `hotfix_data` references unknown DB2 store by hash 0x{0:X} in hotfix id {1}", tableHash, id);
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

        public Dictionary<ulong, int> GetHotfixData() { return _hotfixData; }

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
            if ((gender == Gender.Female || gender == Gender.None) && (forceGender || broadcastText.FemaleText.HasString(SharedConst.DefaultLocale)))
            {
                if (broadcastText.FemaleText.HasString(locale))
                    return broadcastText.FemaleText[locale];

                return broadcastText.FemaleText[SharedConst.DefaultLocale];
            }


            if (broadcastText.MaleText.HasString(locale))
                return broadcastText.MaleText[locale];

            return broadcastText.MaleText[SharedConst.DefaultLocale];
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
                        while (pointIndex < points.Count && points[pointIndex].X <= x)
                            ++pointIndex;
                        if (pointIndex == 0)
                            return points[0].Y;
                        if (pointIndex >= points.Count)
                            return points.Last().Y;
                        float xDiff = points[pointIndex].X - points[pointIndex - 1].X;
                        if (xDiff == 0.0)
                            return points[pointIndex].Y;
                        return (((x - points[pointIndex - 1].X) / xDiff) * (points[pointIndex].Y - points[pointIndex - 1].Y)) + points[pointIndex - 1].Y;
                    }
                case CurveInterpolationMode.Cosine:
                    {
                        int pointIndex = 0;
                        while (pointIndex < points.Count && points[pointIndex].X <= x)
                            ++pointIndex;
                        if (pointIndex == 0)
                            return points[0].Y;
                        if (pointIndex >= points.Count)
                            return points.Last().Y;
                        float xDiff = points[pointIndex].X - points[pointIndex - 1].X;
                        if (xDiff == 0.0)
                            return points[pointIndex].Y;
                        return (float)((points[pointIndex].Y - points[pointIndex - 1].Y) * (1.0f - Math.Cos((x - points[pointIndex - 1].X) / xDiff * Math.PI)) * 0.5f) + points[pointIndex - 1].Y;
                    }
                case CurveInterpolationMode.CatmullRom:
                    {
                        int pointIndex = 1;
                        while (pointIndex < points.Count && points[pointIndex].X <= x)
                            ++pointIndex;
                        if (pointIndex == 1)
                            return points[1].Y;
                        if (pointIndex >= points.Count - 1)
                            return points[points.Count - 2].Y;
                        float xDiff = points[pointIndex].X - points[pointIndex - 1].X;
                        if (xDiff == 0.0)
                            return points[pointIndex].Y;

                        float mu = (x - points[pointIndex - 1].X) / xDiff;
                        float a0 = -0.5f * points[pointIndex - 2].Y + 1.5f * points[pointIndex - 1].Y - 1.5f * points[pointIndex].Y + 0.5f * points[pointIndex + 1].Y;
                        float a1 = points[pointIndex - 2].Y - 2.5f * points[pointIndex - 1].Y + 2.0f * points[pointIndex].Y - 0.5f * points[pointIndex + 1].Y;
                        float a2 = -0.5f * points[pointIndex - 2].Y + 0.5f * points[pointIndex].Y;
                        float a3 = points[pointIndex - 1].Y;

                        return a0 * mu * mu * mu + a1 * mu * mu + a2 * mu + a3;
                    }
                case CurveInterpolationMode.Bezier3:
                    {
                        float xDiff = points[2].X - points[0].X;
                        if (xDiff == 0.0)
                            return points[1].Y;
                        float mu = (x - points[0].X) / xDiff;
                        return ((1.0f - mu) * (1.0f - mu) * points[0].Y) + (1.0f - mu) * 2.0f * mu * points[1].Y + mu * mu * points[2].Y;
                    }
                case CurveInterpolationMode.Bezier4:
                    {
                        float xDiff = points[3].X - points[0].X;
                        if (xDiff == 0.0)
                            return points[1].Y;
                        float mu = (x - points[0].X) / xDiff;
                        return (1.0f - mu) * (1.0f - mu) * (1.0f - mu) * points[0].Y
                            + 3.0f * mu * (1.0f - mu) * (1.0f - mu) * points[1].Y
                            + 3.0f * mu * mu * (1.0f - mu) * points[2].Y
                            + mu * mu * mu * points[3].Y;
                    }
                case CurveInterpolationMode.Bezier:
                    {
                        float xDiff = points.Last().X - points[0].X;
                        if (xDiff == 0.0f)
                            return points.Last().Y;

                        float[] tmp = new float[points.Count];
                        for (int c = 0; c < points.Count; ++c)
                            tmp[c] = points[c].Y;

                        float mu = (x - points[0].X) / xDiff;
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
                    return points[0].Y;
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

        public List<uint> GetItemBonusTree(uint itemId, uint itemBonusTreeMod)
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
                    if (bonusTreeNode.BonusTreeModID != itemBonusTreeMod)
                        continue;

                    if (bonusTreeNode.BonusListID != 0)
                    {
                        bonusListIDs.Add(bonusTreeNode.BonusListID);
                    }
                    else if (bonusTreeNode.ItemLevelSelectorID != 0)
                    {
                        ItemLevelSelectorRecord selector = CliDB.ItemLevelSelectorStorage.LookupByKey(bonusTreeNode.ItemLevelSelectorID);
                        if (selector == null)
                            continue;

                        short delta = (short)(selector.ItemLevel - proto.ItemLevel);

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
                                if (selector.ItemLevel >= selectorQualitySet.ItemLevelMax)
                                    quality = ItemQuality.Epic;
                                else if (selector.ItemLevel >= selectorQualitySet.ItemLevelMin)
                                    quality = ItemQuality.Rare;

                                var itemSelectorQuality = itemSelectorQualities.FirstOrDefault(p => p.Quality < (byte)quality);

                                if (itemSelectorQuality != null)
                                    bonusListIDs.Add(itemSelectorQuality.ItemBonusListID);
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

        public uint GetItemDisplayId(uint itemId, uint appearanceModId)
        {
            ItemModifiedAppearanceRecord modifiedAppearance = GetItemModifiedAppearance(itemId, appearanceModId);
            if (modifiedAppearance != null)
            {
                ItemAppearanceRecord itemAppearance = CliDB.ItemAppearanceStorage.LookupByKey(modifiedAppearance.AppearanceID);
                if (itemAppearance != null)
                    return itemAppearance.DisplayID;
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
                if (dungeon.MapID == mapId && (Difficulty)dungeon.DifficultyID == difficulty)
                    return dungeon;

            return null;
        }

        public uint GetDefaultMapLight(uint mapId)
        {
            foreach (var light in CliDB.LightStorage.Values.Reverse())
            {
                if (light.MapID == mapId && light.Pos.X == 0.0f && light.Pos.Y == 0.0f && light.Pos.Z == 0.0f)
                    return light.Id;
            }

            return 0;
        }

        public uint GetLiquidFlags(uint liquidType)
        {
            LiquidTypeRecord liq = CliDB.LiquidTypeStorage.LookupByKey(liquidType);
            if (liq != null)
                return 1u << liq.LiquidType;

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

        public string GetNameGenEntry(uint race, uint gender, LocaleConstant locale, LocaleConstant defaultLocale)
        {
            Contract.Assert(gender < (int)Gender.None);
            var listNameGen = _nameGenData.LookupByKey(race);
            if (listNameGen == null)
                return "";

            if (listNameGen[gender].Empty())
                return "";

            LocalizedString data = listNameGen[gender].SelectRandom().Name;
            if (data.HasString(locale))
                return data[locale];

            return data[defaultLocale];
        }

        public ResponseCodes ValidateName(string name, LocaleConstant locale)
        {
            foreach (var regex in _nameValidators[(int)locale])
                if (regex.IsMatch(name))
                    return ResponseCodes.CharNameProfane;

            // regexes at TOTAL_LOCALES are loaded from NamesReserved which is not locale specific
            foreach (var regex in _nameValidators[(int)LocaleConstant.Total])
                if (regex.IsMatch(name))
                    return ResponseCodes.CharNameReserved;

            return ResponseCodes.CharNameSuccess;
        }

        public byte GetMaxPrestige()
        {
            byte max = 0;
            foreach (PrestigeLevelInfoRecord prestigeLevelInfo in CliDB.PrestigeLevelInfoStorage.Values)
                if (!prestigeLevelInfo.IsDisabled())
                    max = Math.Max(prestigeLevelInfo.PrestigeLevel, max);

            return max;
        }

        public PVPDifficultyRecord GetBattlegroundBracketByLevel(uint mapid, uint level)
        {
            PVPDifficultyRecord maxEntry = null;              // used for level > max listed level case
            foreach (var entry in CliDB.PVPDifficultyStorage.Values)
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

        public PVPDifficultyRecord GetBattlegroundBracketById(uint mapid, BattlegroundBracketId id)
        {
            foreach (var entry in CliDB.PVPDifficultyStorage.Values)
                if (entry.MapID == mapid && entry.GetBracketId() == id)
                    return entry;

            return null;
        }

        public uint GetRewardPackIDForPvpRewardByHonorLevelAndPrestige(byte honorLevel, byte prestige)
        {
            var value = _pvpRewardPack.LookupByKey(Tuple.Create<uint, uint>(prestige, honorLevel));
            if (value == 0)
                value = _pvpRewardPack.LookupByKey(Tuple.Create<uint, uint>(0, honorLevel));

            return value;
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
                string powerName = powerType.PowerTypeToken;
                if (powerName.ToLower() == name)
                    return powerType;

                powerName = powerName.Replace("_", "");
                if (powerName == name)
                    return powerType;
            }

            return null;
        }

        public List<RewardPackXItemRecord> GetRewardPackItemsByRewardID(uint rewardPackID)
        {
            return _rewardPackItems.LookupByKey(rewardPackID);
        }

        public uint GetRulesetItemUpgrade(uint itemId)
        {
            return _rulesetItemUpgrade.LookupByKey(itemId);
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
                            if (powers[difficultyPower.PowerIndex] == null)
                                powers[difficultyPower.PowerIndex] = difficultyPower;
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
                    if (powers[power.PowerIndex] == null)
                        powers[power.PowerIndex] = power;
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

            if (itemEntry.CategoryType != reqEntry.CategoryType)
                return false;

            return (itemEntry.CategoryMask & reqEntry.CategoryMask) == reqEntry.CategoryMask;
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

        public WMOAreaTableRecord GetWMOAreaTable(int rootId, int adtId, int groupId)
        {
            var wmoAreaTable = _wmoAreaTableLookup.LookupByKey(Tuple.Create((short)rootId, (sbyte)adtId, groupId));
            if (wmoAreaTable != null)
                return wmoAreaTable;

            return null;
        }

        public uint GetVirtualMapForMapAndZone(uint mapId, uint zoneId)
        {
            if (mapId != 530 && mapId != 571 && mapId != 732)   // speed for most cases
                return mapId;

            var worldMapArea = _worldMapAreaByAreaID.LookupByKey(zoneId);
            if (worldMapArea != null)
                return worldMapArea.DisplayMapID >= 0 ? (uint)worldMapArea.DisplayMapID : worldMapArea.MapID;

            return mapId;
        }

        public void Zone2MapCoordinates(uint areaId, ref float x, ref float y)
        {
            var record = _worldMapAreaByAreaID.LookupByKey(areaId);
            if (record == null)
                return;

            Extensions.Swap(ref x, ref y);                                         // at client map coords swapped
            x = x * ((record.LocBottom - record.LocTop) / 100) + record.LocTop;
            y = y * ((record.LocRight - record.LocLeft) / 100) + record.LocLeft;        // client y coord from top to down
        }

        public void Map2ZoneCoordinates(uint areaId, ref float x, ref float y)
        {
            var record = _worldMapAreaByAreaID.LookupByKey(areaId);
            if (record == null)
                return;

            x = (x - record.LocTop) / ((record.LocBottom - record.LocTop) / 100);
            y = (y - record.LocLeft) / ((record.LocRight - record.LocLeft) / 100);    // client y coord from top to down
            Extensions.Swap(ref x, ref y);                                         // client have map coords swapped
        }

        public void DeterminaAlternateMapPosition(uint mapId, float x, float y, float z, out uint newMapId)
        {
            Vector2 pos;
            DeterminaAlternateMapPosition(mapId, x, y, z, out newMapId, out pos);
        }
        public void DeterminaAlternateMapPosition(uint mapId, float x, float y, float z, out uint newMapId, out Vector2 newPos)
        {
            newPos = new Vector2();

            //Contract.Assert(newMapId != 0 || newPos != );
            WorldMapTransformsRecord transformation = null;
            foreach (WorldMapTransformsRecord transform in CliDB.WorldMapTransformsStorage.Values)
            {
                if (transform.MapID != mapId)
                    continue;
                if (transform.AreaID != 0)
                    continue;
                if (Convert.ToBoolean(transform.Flags & (byte)WorldMapTransformsFlags.Dundeon))
                    continue;
                if (transform.RegionMin.X > x || transform.RegionMax.X < x)
                    continue;
                if (transform.RegionMin.Y > y || transform.RegionMax.Y < y)
                    continue;
                if (transform.RegionMin.Z > z || transform.RegionMax.Z < z)
                    continue;

                if (transformation == null || transformation.Priority < transform.Priority)
                    transformation = transform;
            }

            if (transformation == null)
            {
                newMapId = mapId;

                newPos.X = x;
                newPos.Y = y;
                return;
            }

            newMapId = transformation.NewMapID;

            if (Math.Abs(transformation.RegionScale - 1.0f) > 0.001f)
            {
                x = (x - transformation.RegionMin.X) * transformation.RegionScale + transformation.RegionMin.X;
                y = (y - transformation.RegionMin.Y) * transformation.RegionScale + transformation.RegionMin.Y;
            }

            newPos.X = x + transformation.RegionOffset.X;
            newPos.Y = y + transformation.RegionOffset.Y;
        }

        public bool HasItemCurrencyCost(uint itemId) { return _itemsWithCurrencyCost.Contains(itemId); }

        public Dictionary<uint, Dictionary<uint, MapDifficultyRecord>> GetMapDifficulties() { return _mapDifficulties; }

        public void AddDB2<T>(uint tableHash, DB6Storage<T> store) where T : new()
        {
            _storage[tableHash] = store;
        }

        Dictionary<uint, IDB2Storage> _storage = new Dictionary<uint, IDB2Storage>();
        Dictionary<ulong, int> _hotfixData = new Dictionary<ulong, int>();

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
        List<Regex>[] _nameValidators = new List<Regex>[(int)LocaleConstant.Total + 1];
        MultiMap<uint, uint> _phasesByGroup = new MultiMap<uint, uint>();
        Dictionary<PowerType, PowerTypeRecord> _powerTypes = new Dictionary<PowerType, PowerTypeRecord>();
        Dictionary<Tuple<uint /*prestige level*/, uint /*honor level*/>, uint> _pvpRewardPack = new Dictionary<Tuple<uint, uint>, uint>();
        Dictionary<uint, Tuple<List<QuestPackageItemRecord>, List<QuestPackageItemRecord>>> _questPackages = new Dictionary<uint, Tuple<List<QuestPackageItemRecord>, List<QuestPackageItemRecord>>>();
        MultiMap<uint, RewardPackXItemRecord> _rewardPackItems = new MultiMap<uint, RewardPackXItemRecord>();
        Dictionary<uint, uint> _rulesetItemUpgrade = new Dictionary<uint, uint>();
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
        Dictionary<Tuple<short, sbyte, int>, WMOAreaTableRecord> _wmoAreaTableLookup = new Dictionary<Tuple<short, sbyte, int>, WMOAreaTableRecord>();
        Dictionary<uint, WorldMapAreaRecord> _worldMapAreaByAreaID = new Dictionary<uint, WorldMapAreaRecord>();
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

    struct ItemLevelSelectorQualityEntryComparator : IComparer<ItemLevelSelectorQualityRecord>
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
