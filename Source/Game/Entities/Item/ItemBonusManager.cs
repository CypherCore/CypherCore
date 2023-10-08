// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Game.Entities
{
    class ItemBonusMgr
    {
        static MultiMap<uint /*azeriteUnlockMappingSetId*/, AzeriteUnlockMappingRecord> _azeriteUnlockMappings = new();
        static MultiMap<uint /*itemBonusTreeId*/, ChallengeModeItemBonusOverrideRecord> _challengeModeItemBonusOverrides = new();
        static MultiMap<uint /*itemBonusListId*/, ItemBonusRecord> _itemBonusLists = new();
        static MultiMap<int, ItemBonusListGroupEntryRecord> _itemBonusListGroupEntries = new();
        static Dictionary<short /*itemLevelDelta*/, uint /*itemBonusListId*/> _itemLevelDeltaToBonusListContainer = new();
        static SortedMultiMap<uint /*itemLevelSelectorQualitySetId*/, ItemLevelSelectorQualityRecord> _itemLevelQualitySelectorQualities = new();
        static MultiMap<uint /*itemBonusTreeId*/, ItemBonusTreeNodeRecord> _itemBonusTrees = new();
        static MultiMap<uint /*itemId*/, uint /*itemBonusTreeId*/> _itemToBonusTree = new();

        public static void Load()
        {
            foreach (var azeriteUnlockMapping in CliDB.AzeriteUnlockMappingStorage.Values)
                _azeriteUnlockMappings.Add(azeriteUnlockMapping.AzeriteUnlockMappingSetID, azeriteUnlockMapping);

            foreach (var challengeModeItemBonusOverride in CliDB.ChallengeModeItemBonusOverrideStorage.Values)
                _challengeModeItemBonusOverrides.Add(challengeModeItemBonusOverride.SrcItemBonusTreeID, challengeModeItemBonusOverride);

            foreach (var bonus in CliDB.ItemBonusStorage.Values)
                _itemBonusLists.Add(bonus.ParentItemBonusListID, bonus);

            foreach (var bonusListGroupEntry in CliDB.ItemBonusListGroupEntryStorage.Values)
                _itemBonusListGroupEntries.Add(bonusListGroupEntry.ItemBonusListGroupID, bonusListGroupEntry);

            foreach (var itemBonusListLevelDelta in CliDB.ItemBonusListLevelDeltaStorage.Values)
                _itemLevelDeltaToBonusListContainer[itemBonusListLevelDelta.ItemLevelDelta] = itemBonusListLevelDelta.Id;

            foreach (var itemLevelSelectorQuality in CliDB.ItemLevelSelectorQualityStorage.Values)
                _itemLevelQualitySelectorQualities.Add(itemLevelSelectorQuality.ParentILSQualitySetID, itemLevelSelectorQuality);

            foreach (var bonusTreeNode in CliDB.ItemBonusTreeNodeStorage.Values)
                _itemBonusTrees.Add(bonusTreeNode.ParentItemBonusTreeID, bonusTreeNode);

            foreach (var itemBonusTreeAssignment in CliDB.ItemXBonusTreeStorage.Values)
                _itemToBonusTree.Add(itemBonusTreeAssignment.ItemID, itemBonusTreeAssignment.ItemBonusTreeID);
        }

        public static ItemContext GetContextForPlayer(MapDifficultyRecord mapDifficulty, Player player)
        {
            ItemContext evalContext(ItemContext currentContext, ItemContext newContext)
            {
                if (newContext == ItemContext.None)
                    newContext = currentContext;
                else if (newContext == ItemContext.ForceToNone)
                    newContext = ItemContext.None;
                return newContext;
            }

            ItemContext context = ItemContext.None;
            var difficulty = CliDB.DifficultyStorage.LookupByKey(mapDifficulty.DifficultyID);
            if (difficulty != null)
                context = evalContext(context, (ItemContext)difficulty.ItemContext);

            context = evalContext(context, (ItemContext)mapDifficulty.ItemContext);

            if (mapDifficulty.ItemContextPickerID != 0)
            {
                uint contentTuningId = Global.DB2Mgr.GetRedirectedContentTuningId((uint)mapDifficulty.ContentTuningID, player.m_playerData.CtrOptions.GetValue().ContentTuningConditionMask);

                ItemContextPickerEntryRecord selectedPickerEntry = null;
                foreach (var itemContextPickerEntry in CliDB.ItemContextPickerEntryStorage.Values)
                {
                    if (itemContextPickerEntry.ItemContextPickerID != mapDifficulty.ItemContextPickerID)
                        continue;

                    if (itemContextPickerEntry.PVal <= 0)
                        continue;

                    bool meetsPlayerCondition = false;
                    if (player != null)
                    {
                        var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(itemContextPickerEntry.PlayerConditionID);
                        if (playerCondition != null)
                            meetsPlayerCondition = ConditionManager.IsPlayerMeetingCondition(player, playerCondition);
                    }

                    if ((itemContextPickerEntry.Flags & 0x1) != 0)
                        meetsPlayerCondition = !meetsPlayerCondition;

                    if (!meetsPlayerCondition)
                        continue;

                    if (itemContextPickerEntry.LabelID != 0 && !Global.DB2Mgr.HasContentTuningLabel(contentTuningId, itemContextPickerEntry.LabelID))
                        continue;

                    if (selectedPickerEntry == null || selectedPickerEntry.OrderIndex < itemContextPickerEntry.OrderIndex)
                        selectedPickerEntry = itemContextPickerEntry;
                }

                if (selectedPickerEntry != null)
                    context = evalContext(context, (ItemContext)selectedPickerEntry.ItemCreationContext);
            }

            return context;
        }

        public static List<ItemBonusRecord> GetItemBonuses(uint bonusListId)
        {
            return _itemBonusLists.LookupByKey(bonusListId);
        }

        public static uint GetItemBonusListForItemLevelDelta(short delta)
        {
            return _itemLevelDeltaToBonusListContainer.LookupByKey(delta);
        }

        public static bool CanApplyBonusTreeToItem(ItemTemplate itemTemplate, uint itemBonusTreeId, ItemBonusGenerationParams generationParams)
        {
            var bonusTree = CliDB.ItemBonusTreeStorage.LookupByKey(itemBonusTreeId);
            if (bonusTree != null)
            {
                if (bonusTree.InventoryTypeSlotMask != 0)
                    if ((1 << (int)itemTemplate.GetInventoryType() & bonusTree.InventoryTypeSlotMask) == 0)
                        return false;

                if ((bonusTree.Flags & 0x8) != 0 && !itemTemplate.HasFlag(ItemFlags2.CasterWeapon))
                    return false;
                if ((bonusTree.Flags & 0x10) != 0 && itemTemplate.HasFlag(ItemFlags2.CasterWeapon))
                    return false;
                if ((bonusTree.Flags & 0x20) != 0 && !itemTemplate.HasFlag(ItemFlags4.CcTrinket))
                    return false;
                if ((bonusTree.Flags & 0x40) != 0 && itemTemplate.HasFlag(ItemFlags4.CcTrinket))
                    return false;

                if ((bonusTree.Flags & 0x4) != 0)
                    return true;
            }

            var bonusTreeNodes = _itemBonusTrees.LookupByKey(itemBonusTreeId);
            if (!bonusTreeNodes.Empty())
            {
                bool anyNodeMatched = false;
                foreach (var bonusTreeNode in bonusTreeNodes)
                {
                    if (bonusTreeNode.MinMythicPlusLevel > 0)
                        continue;

                    ItemContext nodeContext = (ItemContext)bonusTreeNode.ItemContext;
                    if (nodeContext == ItemContext.None || nodeContext == generationParams.Context)
                    {
                        if (anyNodeMatched)
                            return false;

                        anyNodeMatched = true;
                    }
                }
            }

            return true;
        }

        public static uint GetBonusTreeIdOverride(uint itemBonusTreeId, ItemBonusGenerationParams generationParams)
        {
            // TODO: configure seasons globally
            var mythicPlusSeason = CliDB.MythicPlusSeasonStorage.LookupByKey(0);
            if (mythicPlusSeason != null)
            {
                int selectedLevel = -1;
                int selectedMilestoneSeason = -1;
                ChallengeModeItemBonusOverrideRecord selectedItemBonusOverride = null;
                foreach (var itemBonusOverride in _challengeModeItemBonusOverrides.LookupByKey(itemBonusTreeId))
                {
                    if (itemBonusOverride.Type != 0)
                        continue;

                    if (itemBonusOverride.Value > generationParams.MythicPlusKeystoneLevel.GetValueOrDefault(-1))
                        continue;

                    if (itemBonusOverride.MythicPlusSeasonID != 0)
                    {
                        var overrideSeason = CliDB.MythicPlusSeasonStorage.LookupByKey(itemBonusOverride.MythicPlusSeasonID);
                        if (overrideSeason == null)
                            continue;

                        if (mythicPlusSeason.MilestoneSeason < overrideSeason.MilestoneSeason)
                            continue;

                        if (selectedMilestoneSeason > overrideSeason.MilestoneSeason)
                            continue;

                        if (selectedMilestoneSeason == overrideSeason.MilestoneSeason)
                            if (selectedLevel > itemBonusOverride.Value)
                                continue;

                        selectedMilestoneSeason = overrideSeason.MilestoneSeason;
                    }
                    else if (selectedLevel > itemBonusOverride.Value)
                        continue;

                    selectedLevel = itemBonusOverride.Value;
                    selectedItemBonusOverride = itemBonusOverride;
                }

                if (selectedItemBonusOverride != null && selectedItemBonusOverride.DstItemBonusTreeID != 0)
                    itemBonusTreeId = (uint)selectedItemBonusOverride.DstItemBonusTreeID;
            }

            // TODO: configure seasons globally
            var pvpSeason = CliDB.PvpSeasonStorage.LookupByKey(0);
            if (pvpSeason != null)
            {
                int selectedLevel = -1;
                int selectedMilestoneSeason = -1;
                ChallengeModeItemBonusOverrideRecord selectedItemBonusOverride = null;
                foreach (var itemBonusOverride in _challengeModeItemBonusOverrides.LookupByKey(itemBonusTreeId))
                {
                    if (itemBonusOverride.Type != 1)
                        continue;

                    if (itemBonusOverride.Value > generationParams.PvpTier.GetValueOrDefault(-1))
                        continue;

                    if (itemBonusOverride.PvPSeasonID != 0)
                    {
                        var overrideSeason = CliDB.PvpSeasonStorage.LookupByKey(itemBonusOverride.PvPSeasonID);
                        if (overrideSeason == null)
                            continue;

                        if (pvpSeason.MilestoneSeason < overrideSeason.MilestoneSeason)
                            continue;

                        if (selectedMilestoneSeason > overrideSeason.MilestoneSeason)
                            continue;

                        if (selectedMilestoneSeason == overrideSeason.MilestoneSeason)
                            if (selectedLevel > itemBonusOverride.Value)
                                continue;

                        selectedMilestoneSeason = overrideSeason.MilestoneSeason;
                    }
                    else if (selectedLevel > itemBonusOverride.Value)
                        continue;

                    selectedLevel = itemBonusOverride.Value;
                    selectedItemBonusOverride = itemBonusOverride;
                }

                if (selectedItemBonusOverride != null && selectedItemBonusOverride.DstItemBonusTreeID != 0)
                    itemBonusTreeId = (uint)selectedItemBonusOverride.DstItemBonusTreeID;
            }

            return itemBonusTreeId;
        }

        public static void ApplyBonusTreeHelper(ItemTemplate itemTemplate, uint itemBonusTreeId, ItemBonusGenerationParams generationParams, int sequenceLevel, ref uint itemLevelSelectorId, List<uint> bonusListIDs)
        {
            uint originalItemBonusTreeId = itemBonusTreeId;

            // override bonus tree with season specific values
            itemBonusTreeId = GetBonusTreeIdOverride(itemBonusTreeId, generationParams);

            if (!CanApplyBonusTreeToItem(itemTemplate, itemBonusTreeId, generationParams))
                return;

            var treeList = _itemBonusTrees.LookupByKey(itemBonusTreeId);
            if (treeList.Empty())
                return;

            foreach (var bonusTreeNode in treeList)
            {
                ItemContext nodeContext = (ItemContext)bonusTreeNode.ItemContext;
                ItemContext requiredContext = nodeContext != ItemContext.ForceToNone ? nodeContext : ItemContext.None;
                if (nodeContext != ItemContext.None && generationParams.Context != requiredContext)
                    continue;

                if (generationParams.MythicPlusKeystoneLevel != 0)
                {
                    if (bonusTreeNode.MinMythicPlusLevel != 0 && generationParams.MythicPlusKeystoneLevel < bonusTreeNode.MinMythicPlusLevel)
                        continue;

                    if (bonusTreeNode.MaxMythicPlusLevel != 0 && generationParams.MythicPlusKeystoneLevel > bonusTreeNode.MaxMythicPlusLevel)
                        continue;
                }

                if (bonusTreeNode.ChildItemBonusTreeID != 0)
                    ApplyBonusTreeHelper(itemTemplate, bonusTreeNode.ChildItemBonusTreeID, generationParams, sequenceLevel, ref itemLevelSelectorId, bonusListIDs);
                else if (bonusTreeNode.ChildItemBonusListID != 0)
                    bonusListIDs.Add(bonusTreeNode.ChildItemBonusListID);
                else if (bonusTreeNode.ChildItemLevelSelectorID != 0)
                    itemLevelSelectorId = bonusTreeNode.ChildItemLevelSelectorID;
                else if (bonusTreeNode.ChildItemBonusListGroupID != 0)
                {
                    int resolvedSequenceLevel = sequenceLevel;
                    switch (originalItemBonusTreeId)
                    {
                        case 4001:
                            resolvedSequenceLevel = 1;
                            break;
                        case 4079:
                            if (generationParams.MythicPlusKeystoneLevel != 0)
                            {
                                switch (bonusTreeNode.IblGroupPointsModSetID)
                                {
                                    case 2909: // MythicPlus_End_of_Run levels 2-8
                                        resolvedSequenceLevel = (int)Global.DB2Mgr.GetCurveValueAt(62951, generationParams.MythicPlusKeystoneLevel.Value);
                                        break;
                                    case 2910: // MythicPlus_End_of_Run levels 9-16
                                        resolvedSequenceLevel = (int)Global.DB2Mgr.GetCurveValueAt(62952, generationParams.MythicPlusKeystoneLevel.Value);
                                        break;
                                    case 2911: // MythicPlus_End_of_Run levels 17-20
                                        resolvedSequenceLevel = (int)Global.DB2Mgr.GetCurveValueAt(62954, generationParams.MythicPlusKeystoneLevel.Value);
                                        break;
                                    case 3007: // MythicPlus_Jackpot (weekly reward) levels 2-7
                                        resolvedSequenceLevel = (int)Global.DB2Mgr.GetCurveValueAt(64388, generationParams.MythicPlusKeystoneLevel.Value);
                                        break;
                                    case 3008: // MythicPlus_Jackpot (weekly reward) levels 8-15
                                        resolvedSequenceLevel = (int)Global.DB2Mgr.GetCurveValueAt(64389, generationParams.MythicPlusKeystoneLevel.Value);
                                        break;
                                    case 3009: // MythicPlus_Jackpot (weekly reward) levels 16-20
                                        resolvedSequenceLevel = (int)Global.DB2Mgr.GetCurveValueAt(64395, generationParams.MythicPlusKeystoneLevel.Value);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            break;
                        case 4125:
                            resolvedSequenceLevel = 2;
                            break;
                        case 4126:
                            resolvedSequenceLevel = 3;
                            break;
                        case 4127:
                            resolvedSequenceLevel = 4;
                            break;
                        case 4128:
                            switch (generationParams.Context)
                            {
                                case ItemContext.RaidNormal:
                                case ItemContext.RaidRaidFinder:
                                case ItemContext.RaidHeroic:
                                    resolvedSequenceLevel = 2;
                                    break;
                                case ItemContext.RaidMythic:
                                    resolvedSequenceLevel = 6;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case 4140:
                            switch (generationParams.Context)
                            {
                                case ItemContext.DungeonNormal:
                                    resolvedSequenceLevel = 2;
                                    break;
                                case ItemContext.DungeonHeroic:
                                    resolvedSequenceLevel = 4;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        default:
                            break;
                    }

                    foreach (var bonusListGroupEntry in _itemBonusListGroupEntries.LookupByKey(bonusTreeNode.ChildItemBonusListGroupID))
                    {
                        if ((resolvedSequenceLevel > 0 || bonusListGroupEntry.SequenceValue <= 0) && resolvedSequenceLevel != bonusListGroupEntry.SequenceValue)
                            continue;

                        itemLevelSelectorId = (uint)bonusListGroupEntry.ItemLevelSelectorID;
                        bonusListIDs.Add((uint)bonusListGroupEntry.ItemBonusListID);
                        break;
                    }
                }
            }
        }

        public static uint GetAzeriteUnlockBonusList(ushort azeriteUnlockMappingSetId, ushort minItemLevel, InventoryType inventoryType)
        {
            AzeriteUnlockMappingRecord selectedAzeriteUnlockMapping = null;
            foreach (var azeriteUnlockMapping in _azeriteUnlockMappings.LookupByKey(azeriteUnlockMappingSetId))
            {
                if (minItemLevel < azeriteUnlockMapping.ItemLevel)
                    continue;

                if (selectedAzeriteUnlockMapping != null && selectedAzeriteUnlockMapping.ItemLevel > azeriteUnlockMapping.ItemLevel)
                    continue;

                selectedAzeriteUnlockMapping = azeriteUnlockMapping;
            }

            if (selectedAzeriteUnlockMapping != null)
            {
                switch (inventoryType)
                {
                    case InventoryType.Head:
                        return selectedAzeriteUnlockMapping.ItemBonusListHead;
                    case InventoryType.Shoulders:
                        return selectedAzeriteUnlockMapping.ItemBonusListShoulders;
                    case InventoryType.Chest:
                    case InventoryType.Robe:
                        return selectedAzeriteUnlockMapping.ItemBonusListChest;
                    default:
                        break;
                }
            }

            return 0;
        }

        public static List<uint> GetBonusListsForItem(uint itemId, ItemBonusGenerationParams generationParams)
        {
            List<uint> bonusListIDs = new();

            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);
            if (itemTemplate == null)
                return bonusListIDs;

            uint itemLevelSelectorId = 0;

            foreach (var itemBonusTreeId in _itemToBonusTree.LookupByKey(itemId))
                ApplyBonusTreeHelper(itemTemplate, itemBonusTreeId, generationParams, 0, ref itemLevelSelectorId, bonusListIDs);

            var selector = CliDB.ItemLevelSelectorStorage.LookupByKey(itemLevelSelectorId);
            if (selector != null)
            {
                short delta = (short)(selector.MinItemLevel - itemTemplate.GetBaseItemLevel());

                uint bonus = GetItemBonusListForItemLevelDelta(delta);
                if (bonus != 0)
                    bonusListIDs.Add(bonus);

                var selectorQualitySet = CliDB.ItemLevelSelectorQualitySetStorage.LookupByKey(selector.ItemLevelSelectorQualitySetID);
                if (selectorQualitySet != null)
                {
                    var itemSelectorQualities = _itemLevelQualitySelectorQualities.LookupByKey(selector.ItemLevelSelectorQualitySetID);
                    if (!itemSelectorQualities.Empty())
                    {
                        ItemQuality quality = ItemQuality.Uncommon;
                        if (selector.MinItemLevel >= selectorQualitySet.IlvlEpic)
                            quality = ItemQuality.Epic;
                        else if (selector.MinItemLevel >= selectorQualitySet.IlvlRare)
                            quality = ItemQuality.Rare;

                        var itemSelectorQuality = itemSelectorQualities.First(record => record.Quality < (sbyte)quality);

                        if (itemSelectorQuality != null)
                            bonusListIDs.Add(itemSelectorQuality.QualityItemBonusListID);
                    }
                }

                uint azeriteUnlockBonusListId = GetAzeriteUnlockBonusList(selector.AzeriteUnlockMappingSet, selector.MinItemLevel, itemTemplate.GetInventoryType());
                if (azeriteUnlockBonusListId != 0)
                    bonusListIDs.Add(azeriteUnlockBonusListId);
            }

            return bonusListIDs;
        }

        public static void VisitItemBonusTree(uint itemBonusTreeId, Action<ItemBonusTreeNodeRecord> visitor)
        {
            var treeItr = _itemBonusTrees.LookupByKey(itemBonusTreeId);
            if (treeItr.Empty())
                return;

            foreach (var bonusTreeNode in treeItr)
            {
                visitor(bonusTreeNode);
                if (bonusTreeNode.ChildItemBonusTreeID != 0)
                    VisitItemBonusTree(bonusTreeNode.ChildItemBonusTreeID, visitor);
            }
        }

        public static List<uint> GetAllBonusListsForTree(uint itemBonusTreeId)
        {
            List<uint> bonusListIDs = new();
            VisitItemBonusTree(itemBonusTreeId, bonusTreeNode =>
            {
                if (bonusTreeNode.ChildItemBonusListID != 0)
                    bonusListIDs.Add(bonusTreeNode.ChildItemBonusListID);
            });

            return bonusListIDs;
        }

        public struct ItemBonusGenerationParams
        {
            public ItemBonusGenerationParams(ItemContext context, int? mythicPlusKeystoneLevel = null, int? pvpTier = null)
            {
                Context = context;
                MythicPlusKeystoneLevel = mythicPlusKeystoneLevel;
                PvpTier = pvpTier;
            }

            public ItemContext Context;
            public int? MythicPlusKeystoneLevel;
            public int? PvpTier;
        }
    }
}
