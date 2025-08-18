// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Loots;
using Game.Maps;
using Game.Miscellaneous;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public sealed class ConditionManager : Singleton<ConditionManager>
    {
        ConditionManager()
        {
            for (ConditionSourceType i = 0; i < ConditionSourceType.Max; ++i)
                ConditionStorage[i] = new();
        }

        public GridMapTypeMask GetSearcherTypeMaskForConditionList(List<Condition> conditions)
        {
            if (conditions.Empty())
                return GridMapTypeMask.All;
            //     groupId, typeMask
            Dictionary<uint, GridMapTypeMask> elseGroupSearcherTypeMasks = new();
            foreach (var i in conditions)
            {
                // no point of having not loaded conditions in list
                Cypher.Assert(i.IsLoaded(), "ConditionMgr.GetSearcherTypeMaskForConditionList - not yet loaded condition found in list");
                // group not filled yet, fill with widest mask possible
                if (!elseGroupSearcherTypeMasks.ContainsKey(i.ElseGroup))
                    elseGroupSearcherTypeMasks[i.ElseGroup] = GridMapTypeMask.All;
                // no point of checking anymore, empty mask
                else if (elseGroupSearcherTypeMasks[i.ElseGroup] == 0)
                    continue;

                if (i.ReferenceId != 0) // handle reference
                {
                    var refe = ConditionStorage[ConditionSourceType.ReferenceCondition].LookupByKey(new ConditionId(i.ReferenceId, 0, 0));
                    Cypher.Assert(refe != null, "ConditionMgr.GetSearcherTypeMaskForConditionList - incorrect reference");
                    elseGroupSearcherTypeMasks[i.ElseGroup] &= GetSearcherTypeMaskForConditionList(refe);
                }
                else // handle normal condition
                {
                    // object will match conditions in one ElseGroupStore only when it matches all of them
                    // so, let's find a smallest possible mask which satisfies all conditions
                    elseGroupSearcherTypeMasks[i.ElseGroup] &= i.GetSearcherTypeMaskForCondition();
                }
            }
            // object will match condition when one of the checks in ElseGroupStore is matching
            // so, let's include all possible masks
            GridMapTypeMask mask = 0;
            foreach (var i in elseGroupSearcherTypeMasks)
                mask |= i.Value;

            return mask;
        }

        public bool IsObjectMeetToConditionList(ConditionSourceInfo sourceInfo, List<Condition> conditions)
        {
            //     groupId, groupCheckPassed
            Dictionary<uint, bool> elseGroupStore = new();
            foreach (var condition in conditions)
            {
                Log.outDebug(LogFilter.Condition, "ConditionMgr.IsPlayerMeetToConditionList condType: {0} val1: {1}", condition.ConditionType, condition.ConditionValue1);
                if (condition.IsLoaded())
                {
                    //! Find ElseGroup in ElseGroupStore
                    var added = elseGroupStore.TryAdd(condition.ElseGroup, true);
                    if (!added) //! If another condition in this group was unmatched before this, don't bother checking (the group is false anyway)
                        continue;

                    if (condition.ReferenceId != 0)//handle reference
                    {
                        var refe = ConditionStorage[ConditionSourceType.ReferenceCondition].LookupByKey(new ConditionId(condition.ReferenceId, 0, 0));
                        if (refe != null)
                        {
                            bool condMeets = IsObjectMeetToConditionList(sourceInfo, refe);
                            if (condition.NegativeCondition)
                                condMeets = !condMeets;

                            if (!condMeets)
                                elseGroupStore[condition.ElseGroup] = false;
                        }
                        else
                        {
                            Log.outDebug(LogFilter.Condition, "IsPlayerMeetToConditionList: Reference template -{0} not found",
                                condition.ReferenceId);//checked at loading, should never happen
                        }

                    }
                    else //handle normal condition
                    {
                        if (!condition.Meets(sourceInfo))
                            elseGroupStore[condition.ElseGroup] = false;
                    }
                }
            }
            foreach (var i in elseGroupStore)
                if (i.Value)
                    return true;

            return false;
        }

        public bool IsObjectMeetToConditions(WorldObject obj, List<Condition> conditions)
        {
            ConditionSourceInfo srcInfo = new(obj);
            return IsObjectMeetToConditions(srcInfo, conditions);
        }

        public bool IsObjectMeetToConditions(WorldObject obj1, WorldObject obj2, List<Condition> conditions)
        {
            ConditionSourceInfo srcInfo = new(obj1, obj2);
            return IsObjectMeetToConditions(srcInfo, conditions);
        }

        public bool IsObjectMeetToConditions(ConditionSourceInfo sourceInfo, List<Condition> conditions)
        {
            if (conditions == null)
                return true;

            Log.outDebug(LogFilter.Condition, "ConditionMgr.IsObjectMeetToConditions");
            return IsObjectMeetToConditionList(sourceInfo, conditions);
        }

        public bool CanHaveSourceGroupSet(ConditionSourceType sourceType)
        {
            return sourceType == ConditionSourceType.CreatureLootTemplate ||
                    sourceType == ConditionSourceType.DisenchantLootTemplate ||
                    sourceType == ConditionSourceType.FishingLootTemplate ||
                    sourceType == ConditionSourceType.GameobjectLootTemplate ||
                    sourceType == ConditionSourceType.ItemLootTemplate ||
                    sourceType == ConditionSourceType.MailLootTemplate ||
                    sourceType == ConditionSourceType.MillingLootTemplate ||
                    sourceType == ConditionSourceType.PickpocketingLootTemplate ||
                    sourceType == ConditionSourceType.ProspectingLootTemplate ||
                    sourceType == ConditionSourceType.ReferenceLootTemplate ||
                    sourceType == ConditionSourceType.SkinningLootTemplate ||
                    sourceType == ConditionSourceType.SpellLootTemplate ||
                    sourceType == ConditionSourceType.GossipMenu ||
                    sourceType == ConditionSourceType.GossipMenuOption ||
                    sourceType == ConditionSourceType.VehicleSpell ||
                    sourceType == ConditionSourceType.SpellImplicitTarget ||
                    sourceType == ConditionSourceType.SpellClickEvent ||
                    sourceType == ConditionSourceType.SmartEvent ||
                    sourceType == ConditionSourceType.NpcVendor ||
                    sourceType == ConditionSourceType.Phase ||
                    sourceType == ConditionSourceType.Graveyard ||
                    sourceType == ConditionSourceType.AreaTrigger ||
                    sourceType == ConditionSourceType.TrainerSpell ||
                    sourceType == ConditionSourceType.ObjectIdVisibility ||
                    sourceType == ConditionSourceType.ReferenceCondition;
        }

        public bool CanHaveSourceIdSet(ConditionSourceType sourceType)
        {
            return (sourceType == ConditionSourceType.SmartEvent);
        }

        bool CanHaveConditionType(ConditionSourceType sourceType, ConditionTypes conditionType)
        {
            switch (sourceType)
            {
                case ConditionSourceType.SpawnGroup:
                    switch (conditionType)
                    {
                        case ConditionTypes.None:
                        case ConditionTypes.ActiveEvent:
                        case ConditionTypes.InstanceInfo:
                        case ConditionTypes.Mapid:
                        case ConditionTypes.WorldState:
                        case ConditionTypes.RealmAchievement:
                        case ConditionTypes.DifficultyId:
                        case ConditionTypes.ScenarioStep:
                            return true;
                        default:
                            return false;
                    }
                default:
                    break;
            }
            return true;
        }

        public bool IsObjectMeetingNotGroupedConditions(ConditionSourceType sourceType, uint entry, ConditionSourceInfo sourceInfo)
        {
            if (sourceType > ConditionSourceType.None && sourceType < ConditionSourceType.Max)
            {
                var conditions = ConditionStorage[sourceType].LookupByKey(new ConditionId(0, (int)entry, 0));
                if (conditions != null)
                {
                    Log.outDebug(LogFilter.Condition, "GetConditionsForNotGroupedEntry: found conditions for type {0} and entry {1}", sourceType, entry);
                    return IsObjectMeetToConditions(sourceInfo, conditions);
                }
            }

            return true;
        }

        public bool IsObjectMeetingNotGroupedConditions(ConditionSourceType sourceType, uint entry, WorldObject target0, WorldObject target1 = null, WorldObject target2 = null)
        {
            ConditionSourceInfo conditionSource = new(target0, target1, target2);
            return IsObjectMeetingNotGroupedConditions(sourceType, entry, conditionSource);
        }

        public bool IsMapMeetingNotGroupedConditions(ConditionSourceType sourceType, uint entry, Map map)
        {
            ConditionSourceInfo conditionSource = new(map);
            return IsObjectMeetingNotGroupedConditions(sourceType, entry, conditionSource);
        }

        public bool HasConditionsForNotGroupedEntry(ConditionSourceType sourceType, uint entry)
        {
            if (sourceType > ConditionSourceType.None && sourceType < ConditionSourceType.Max)
                return ConditionStorage[sourceType].ContainsKey(new ConditionId(0, (int)entry, 0));

            return false;
        }

        public bool IsObjectMeetingSpellClickConditions(uint creatureId, uint spellId, WorldObject clicker, WorldObject target)
        {
            var conditions = ConditionStorage[ConditionSourceType.SpellClickEvent].LookupByKey(new ConditionId(creatureId, (int)spellId, 0));
            if (conditions != null)
            {
                Log.outDebug(LogFilter.Condition, "GetConditionsForSpellClickEvent: found conditions for SpellClickEvent entry {0} spell {1}", creatureId, spellId);
                ConditionSourceInfo sourceInfo = new(clicker, target);
                return IsObjectMeetToConditions(sourceInfo, conditions);

            }
            return true;
        }

        public bool HasConditionsForSpellClickEvent(uint creatureId, uint spellId)
        {
            if (ConditionStorage[ConditionSourceType.SpellClickEvent].ContainsKey(new ConditionId(creatureId, (int)spellId, 0)))
            {
                Log.outDebug(LogFilter.Condition, "GetConditionsForSpellClickEvent: found conditions for SpellClickEvent entry {0} spell {1}", creatureId, spellId);
                return true;
            }

            return false;
        }

        public bool IsObjectMeetingVehicleSpellConditions(uint creatureId, uint spellId, Player player, Unit vehicle)
        {
            var conditions = ConditionStorage[ConditionSourceType.VehicleSpell].LookupByKey(new ConditionId(creatureId, (int)spellId, 0));
            if (conditions != null)
            {
                Log.outDebug(LogFilter.Condition, "GetConditionsForVehicleSpell: found conditions for Vehicle entry {0} spell {1}", creatureId, spellId);
                ConditionSourceInfo sourceInfo = new(player, vehicle);
                return IsObjectMeetToConditions(sourceInfo, conditions);
            }

            return true;
        }

        public bool IsObjectMeetingSmartEventConditions(long entryOrGuid, uint eventId, SmartScriptType sourceType, Unit unit, WorldObject baseObject)
        {
            var conditions = ConditionStorage[ConditionSourceType.SmartEvent].LookupByKey(new ConditionId(eventId + 1, (int)entryOrGuid, (uint)sourceType));
            if (conditions != null)
            {
                Log.outDebug(LogFilter.Condition, "GetConditionsForSmartEvent: found conditions for Smart Event entry or guid {0} eventId {1}", entryOrGuid, eventId);
                ConditionSourceInfo sourceInfo = new(unit, baseObject);
                return IsObjectMeetToConditions(sourceInfo, conditions);
            }

            return true;
        }

        public bool IsObjectMeetingVendorItemConditions(uint creatureId, uint itemId, Player player, Creature vendor)
        {
            var conditions = ConditionStorage[ConditionSourceType.NpcVendor].LookupByKey(new ConditionId(creatureId, (int)itemId, 0));
            if (conditions != null)
            {
                Log.outDebug(LogFilter.Condition, "GetConditionsForNpcVendor: found conditions for creature entry {0} item {1}", creatureId, itemId);
                ConditionSourceInfo sourceInfo = new(player, vendor);
                return IsObjectMeetToConditions(sourceInfo, conditions);
            }

            return true;
        }

        public bool IsSpellUsedInSpellClickConditions(uint spellId)
        {
            return spellsUsedInSpellClickConditions.Contains(spellId);
        }

        public List<Condition> GetConditionsForAreaTrigger(uint areaTriggerId, bool isServerSide)
        {
            return ConditionStorage[ConditionSourceType.AreaTrigger].LookupByKey(new ConditionId(areaTriggerId, isServerSide ? 1 : 0, 0));
        }

        public bool IsObjectMeetingTrainerSpellConditions(uint trainerId, uint spellId, Player player)
        {
            var conditions = ConditionStorage[ConditionSourceType.NpcVendor].LookupByKey(new ConditionId(trainerId, (int)spellId, 0));
            if (conditions != null)
            {
                Log.outDebug(LogFilter.Condition, $"GetConditionsForTrainerSpell: found conditions for trainer id {trainerId} spell {spellId}");
                return IsObjectMeetToConditions(player, conditions);
            }

            return true;
        }

        public bool IsObjectMeetingVisibilityByObjectIdConditions(WorldObject obj, WorldObject seer)
        {
            var conditions = ConditionStorage[ConditionSourceType.ObjectIdVisibility].LookupByKey(new ConditionId((uint)obj.GetTypeId(), (int)obj.GetEntry(), 0));
            if (conditions != null)
            {
                Log.outDebug(LogFilter.Condition, $"IsObjectMeetingVisibilityByObjectIdConditions: found conditions for objectType {obj.GetTypeId()} entry {obj.GetEntry()} guid {obj.GetGUID()}");
                return IsObjectMeetToConditions(seer, obj, conditions);
            }
            return true;
        }

        public void LoadConditions(bool isReload = false)
        {
            uint oldMSTime = Time.GetMSTime();

            Clean();

            //must clear all custom handled cases (groupped types) before reload
            if (isReload)
            {
                Global.SpellMgr.UnloadSpellInfoImplicitTargetConditionLists();

                Global.ObjectMgr.UnloadPhaseConditions();
            }

            SQLResult result = DB.World.Query("SELECT SourceTypeOrReferenceId, SourceGroup, SourceEntry, SourceId, ElseGroup, ConditionTypeOrReference, ConditionTarget, " +
                "ConditionValue1, ConditionValue2, ConditionValue3, ConditionStringValue1, NegativeCondition, ErrorType, ErrorTextId, ScriptName FROM conditions");

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 conditions. DB table `conditions` is empty!");
                return;
            }

            uint count = 0;

            do
            {
                Condition cond = new();
                int iSourceTypeOrReferenceId = result.Read<int>(0);
                cond.SourceGroup = result.Read<uint>(1);
                cond.SourceEntry = result.Read<int>(2);
                cond.SourceId = result.Read<uint>(3);
                cond.ElseGroup = result.Read<uint>(4);
                int iConditionTypeOrReference = result.Read<int>(5);
                cond.ConditionTarget = result.Read<byte>(6);
                cond.ConditionValue1 = result.Read<uint>(7);
                cond.ConditionValue2 = result.Read<uint>(8);
                cond.ConditionValue3 = result.Read<uint>(9);
                cond.ConditionStringValue1 = result.Read<string>(10);
                cond.NegativeCondition = result.Read<bool>(11);
                cond.ErrorType = result.Read<uint>(12);
                cond.ErrorTextId = result.Read<uint>(13);
                cond.ScriptId = Global.ObjectMgr.GetScriptId(result.Read<string>(14));

                if (iConditionTypeOrReference >= 0)
                    cond.ConditionType = (ConditionTypes)iConditionTypeOrReference;

                if (iSourceTypeOrReferenceId >= 0)
                    cond.SourceType = (ConditionSourceType)iSourceTypeOrReferenceId;

                if (iConditionTypeOrReference < 0)//it has a reference
                {
                    if (iConditionTypeOrReference == iSourceTypeOrReferenceId)//self referencing, skip
                    {
                        Log.outError(LogFilter.Sql, "Condition reference {1} is referencing self, skipped", iSourceTypeOrReferenceId);
                        continue;
                    }

                    cond.ReferenceId = (uint)Math.Abs(iConditionTypeOrReference);

                    string rowType = "reference template";
                    if (iSourceTypeOrReferenceId >= 0)
                        rowType = "reference";
                    //check for useless data
                    if (cond.ConditionTarget != 0)
                        Log.outError(LogFilter.Sql, "Condition {0} {1} has useless data in ConditionTarget ({2})!", rowType, iSourceTypeOrReferenceId, cond.ConditionTarget);
                    if (cond.ConditionValue1 != 0)
                        Log.outError(LogFilter.Sql, "Condition {0} {1} has useless data in value1 ({2})!", rowType, iSourceTypeOrReferenceId, cond.ConditionValue1);
                    if (cond.ConditionValue2 != 0)
                        Log.outError(LogFilter.Sql, "Condition {0} {1} has useless data in value2 ({2})!", rowType, iSourceTypeOrReferenceId, cond.ConditionValue2);
                    if (cond.ConditionValue3 != 0)
                        Log.outError(LogFilter.Sql, "Condition {0} {1} has useless data in value3 ({2})!", rowType, iSourceTypeOrReferenceId, cond.ConditionValue3);
                    if (cond.SourceEntry != 0 && iSourceTypeOrReferenceId < 0)
                        Log.outError(LogFilter.Sql, "Condition {0} {1} has useless data in SourceEntry ({2})!", rowType, iSourceTypeOrReferenceId, cond.SourceEntry);
                }
                else if (!IsConditionTypeValid(cond))//doesn't have reference, validate ConditionType
                    continue;

                if (iSourceTypeOrReferenceId < 0)//it is a reference template
                {
                    if (cond.SourceGroup != 0)
                        Log.outError(LogFilter.Sql, $"Condition reference template {iSourceTypeOrReferenceId} has useless data in SourceGroup ({cond.SourceGroup})!");
                    if (cond.SourceEntry != 0)
                        Log.outError(LogFilter.Sql, $"Condition reference template {iSourceTypeOrReferenceId} has useless data in SourceEntry ({cond.SourceEntry})!");
                    if (cond.SourceId != 0)
                        Log.outError(LogFilter.Sql, $"Condition reference template {iSourceTypeOrReferenceId} has useless data in SourceId ({cond.SourceId})!");

                    cond.SourceType = ConditionSourceType.ReferenceCondition;
                    cond.SourceGroup = (uint)-iSourceTypeOrReferenceId;
                }
                else if (!IsSourceTypeValid(cond)) //if not a reference and SourceType is invalid, skip
                    continue;

                //Grouping is only allowed for some types (loot templates, gossip menus, gossip items)
                if (cond.SourceGroup != 0 && !CanHaveSourceGroupSet(cond.SourceType))
                {
                    Log.outError(LogFilter.Sql, "{0} has not allowed value of SourceGroup = {1}!", cond.ToString(), cond.SourceGroup);
                    continue;
                }
                if (cond.SourceId != 0 && !CanHaveSourceIdSet(cond.SourceType))
                {
                    Log.outError(LogFilter.Sql, "{0} has not allowed value of SourceId = {1}!", cond.ToString(), cond.SourceId);
                    continue;
                }

                if (cond.ErrorType != 0 && cond.SourceType != ConditionSourceType.Spell)
                {
                    Log.outError(LogFilter.Sql, "{0} can't have ErrorType ({1}), set to 0!", cond.ToString(), cond.ErrorType);
                    cond.ErrorType = 0;
                }

                if (cond.ErrorTextId != 0 && cond.ErrorType == 0)
                {
                    Log.outError(LogFilter.Sql, "{0} has any ErrorType, ErrorTextId ({1}) is set, set to 0!", cond.ToString(), cond.ErrorTextId);
                    cond.ErrorTextId = 0;
                }

                var key = new ConditionId(cond.SourceGroup, cond.SourceEntry, cond.SourceId);
                if (!ConditionStorage[cond.SourceType].ContainsKey(key))
                    ConditionStorage[cond.SourceType][key] = new List<Condition>();

                ConditionStorage[cond.SourceType][key].Add(cond);
                ++count;
            }
            while (result.NextRow());

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.CreatureLootTemplate])
                AddToLootTemplate(id, conditions, LootStorage.Creature.GetLootForConditionFill(id.SourceGroup));

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.DisenchantLootTemplate])
                AddToLootTemplate(id, conditions, LootStorage.Disenchant.GetLootForConditionFill(id.SourceGroup));

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.FishingLootTemplate])
                AddToLootTemplate(id, conditions, LootStorage.Fishing.GetLootForConditionFill(id.SourceGroup));

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.GameobjectLootTemplate])
                AddToLootTemplate(id, conditions, LootStorage.Gameobject.GetLootForConditionFill(id.SourceGroup));

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.ItemLootTemplate])
                AddToLootTemplate(id, conditions, LootStorage.Items.GetLootForConditionFill(id.SourceGroup));

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.MailLootTemplate])
                AddToLootTemplate(id, conditions, LootStorage.Mail.GetLootForConditionFill(id.SourceGroup));

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.MillingLootTemplate])
                AddToLootTemplate(id, conditions, LootStorage.Milling.GetLootForConditionFill(id.SourceGroup));

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.PickpocketingLootTemplate])
                AddToLootTemplate(id, conditions, LootStorage.Pickpocketing.GetLootForConditionFill(id.SourceGroup));

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.ProspectingLootTemplate])
                AddToLootTemplate(id, conditions, LootStorage.Prospecting.GetLootForConditionFill(id.SourceGroup));

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.ReferenceLootTemplate])
                AddToLootTemplate(id, conditions, LootStorage.Reference.GetLootForConditionFill(id.SourceGroup));

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.SkinningLootTemplate])
                AddToLootTemplate(id, conditions, LootStorage.Skinning.GetLootForConditionFill(id.SourceGroup));

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.SpellLootTemplate])
                AddToLootTemplate(id, conditions, LootStorage.Spell.GetLootForConditionFill(id.SourceGroup));

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.GossipMenu])
                AddToGossipMenus(id, conditions);

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.GossipMenuOption])
                AddToGossipMenuItems(id, conditions);

            spellsUsedInSpellClickConditions.Clear();
            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.SpellClickEvent])
                foreach (Condition condition in conditions)
                    if (condition.ConditionType == ConditionTypes.Aura)
                        spellsUsedInSpellClickConditions.Add(condition.ConditionValue1);

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.SpellImplicitTarget])
                foreach (Condition condition in conditions)
                    AddToSpellImplicitTargetConditions(condition);

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.Phase])
                AddToPhases(id, conditions);

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.Graveyard])
                AddToGraveyardData(id, conditions);

            bool isPlayerConditionIdUsedByCondition(int playerConditionId, List<Condition> conditions, Dictionary<ConditionSourceType, Dictionary<ConditionId, List<Condition>>> store)
            {
                return conditions.Any(condition =>
                {
                    if (condition.ConditionType == ConditionTypes.PlayerCondition)
                    {
                        if (condition.ConditionValue1 == playerConditionId)
                            return true;

                        var playerCondList = store[ConditionSourceType.PlayerCondition].LookupByKey(new ConditionId(0, (int)condition.ConditionValue1, 0));
                        if (playerCondList != null)
                            if (isPlayerConditionIdUsedByCondition(playerConditionId, playerCondList, store))
                                return true;
                    }
                    else if (condition.ReferenceId != 0)
                    {
                        var refList = store[ConditionSourceType.ReferenceCondition].LookupByKey(new ConditionId(condition.ReferenceId, 0, 0));
                        if (refList != null)
                            if (isPlayerConditionIdUsedByCondition(playerConditionId, refList, store))
                                return true;
                    }
                    return false;
                });
            }

            foreach (var (id, conditions) in ConditionStorage[ConditionSourceType.PlayerCondition])
            {
                if (isPlayerConditionIdUsedByCondition(id.SourceEntry, conditions, ConditionStorage))
                {
                    Log.outError(LogFilter.Sql, $"[Condition SourceType: CONDITION_SOURCE_TYPE_PLAYER_CONDITION, SourceGroup: {id.SourceGroup}, SourceEntry: {id.SourceEntry}, SourceId: {id.SourceId}] " +
                        $"has a circular reference to player condition id {id.SourceEntry}, removed all conditions for this SourceEntry!");
                    conditions.Clear();
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} conditions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        void AddToLootTemplate(ConditionId id, List<Condition> conditions, LootTemplate loot)
        {
            if (loot == null)
            {
                foreach (Condition condition in conditions)
                    Log.outError(LogFilter.Sql, $"{condition} LootTemplate {condition.SourceGroup} not found.");
                return;
            }

            if (loot.LinkConditions(id, new ConditionsReference(conditions)))
                return;

            foreach (Condition condition in conditions)
                Log.outError(LogFilter.Sql, $"{condition} Item {condition.SourceEntry} not found in LootTemplate {condition.SourceGroup}.");
        }

        void AddToGossipMenus(ConditionId id, List<Condition> conditions)
        {
            var pMenuBounds = Global.ObjectMgr.GetGossipMenusMapBounds(id.SourceGroup);
            if (!pMenuBounds.Empty())
            {
                foreach (var menu in pMenuBounds)
                {
                    if (menu.MenuId == id.SourceGroup && (menu.TextId == id.SourceEntry || id.SourceEntry == 0))
                        menu.Conditions = new(conditions);
                }

                return;
            }

            foreach (Condition condition in conditions)
                Log.outError(LogFilter.Sql, $"{condition} GossipMenu {condition.SourceGroup} not found.");
        }

        void AddToGossipMenuItems(ConditionId id, List<Condition> conditions)
        {
            var pMenuItemBounds = Global.ObjectMgr.GetGossipMenuItemsMapBounds(id.SourceGroup);
            foreach (var gossipMenuItem in pMenuItemBounds)
            {
                if (gossipMenuItem.MenuID == id.SourceGroup && gossipMenuItem.OrderIndex == id.SourceEntry)
                {
                    gossipMenuItem.Conditions = new(conditions);
                    return;
                }
            }

            foreach (Condition condition in conditions)
                Log.outError(LogFilter.Sql, $"{condition} GossipMenuId {condition.SourceGroup} Item {condition.SourceEntry} not found.");
        }

        void AddToSpellImplicitTargetConditions(Condition cond)
        {
            Global.SpellMgr.ForEachSpellInfoDifficulty((uint)cond.SourceEntry, spellInfo =>
            {
                uint conditionEffMask = cond.SourceGroup;
                List<uint> sharedMasks = new();
                foreach (var spellEffectInfo in spellInfo.GetEffects())
                {
                    // additional checks by condition type
                    if ((conditionEffMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0)
                    {
                        switch (cond.ConditionType)
                        {
                            case ConditionTypes.ObjectEntryGuid:
                            {
                                SpellCastTargetFlags implicitTargetMask = SpellInfo.GetTargetFlagMask(spellEffectInfo.TargetA.GetObjectType()) | SpellInfo.GetTargetFlagMask(spellEffectInfo.TargetB.GetObjectType());
                                if (implicitTargetMask.HasFlag(SpellCastTargetFlags.UnitMask) && cond.ConditionValue1 != (uint)TypeId.Unit && cond.ConditionValue1 != (uint)TypeId.Player)
                                {
                                    Log.outError(LogFilter.Sql, $"{cond} in `condition` table - spell {spellInfo.Id} EFFECT_{spellEffectInfo.EffectIndex} - target requires ConditionValue1 to be either TYPEID_UNIT ({(uint)TypeId.Unit}) or TYPEID_PLAYER ({(uint)TypeId.Player})");
                                    return;
                                }

                                if (implicitTargetMask.HasFlag(SpellCastTargetFlags.GameobjectMask) && cond.ConditionValue1 != (uint)TypeId.GameObject)
                                {
                                    Log.outError(LogFilter.Sql, $"{cond} in `condition` table - spell {spellInfo.Id} EFFECT_{spellEffectInfo.EffectIndex} - target requires ConditionValue1 to be TYPEID_GAMEOBJECT ({(uint)TypeId.GameObject})");
                                    return;
                                }

                                if (implicitTargetMask.HasFlag(SpellCastTargetFlags.CorpseMask) && cond.ConditionValue1 != (uint)TypeId.Corpse)
                                {
                                    Log.outError(LogFilter.Sql, $"{cond} in `condition` table - spell {spellInfo.Id} EFFECT_{spellEffectInfo.EffectIndex} - target requires ConditionValue1 to be TYPEID_CORPSE ({(uint)TypeId.Corpse})");
                                    return;
                                }
                                break;
                            }
                            default:
                                break;
                        }
                    }

                    // check if effect is already a part of some shared mask
                    if (sharedMasks.Any(mask => !!Convert.ToBoolean(mask & (1 << (int)spellEffectInfo.EffectIndex))))
                        continue;

                    // build new shared mask with found effect
                    uint sharedMask = (uint)(1 << (int)spellEffectInfo.EffectIndex);
                    for (uint effIndex = spellEffectInfo.EffectIndex + 1; effIndex < spellInfo.GetEffects().Count; ++effIndex)
                        if (spellInfo.GetEffect(effIndex).ImplicitTargetConditions == spellEffectInfo.ImplicitTargetConditions)
                            sharedMask |= (uint)(1 << (int)effIndex);

                    sharedMasks.Add(sharedMask);
                }

                foreach (var effectMask in sharedMasks)
                {
                    // some effect indexes should have same data
                    uint commonMask = (effectMask & conditionEffMask);
                    if (commonMask != 0)
                    {
                        byte firstEffIndex = 0;
                        for (; firstEffIndex < SpellConst.MaxEffects; ++firstEffIndex)
                            if (((1 << firstEffIndex) & effectMask) != 0)
                                break;

                        if (firstEffIndex >= spellInfo.GetEffects().Count)
                            return;

                        // get shared data
                        List<Condition> sharedList = spellInfo.GetEffect(firstEffIndex).ImplicitTargetConditions;

                        // there's already data entry for that sharedMask
                        if (sharedList != null)
                        {
                            // we have overlapping masks in db
                            if (conditionEffMask != effectMask)
                            {
                                Log.outError(LogFilter.Sql, "{0} in `condition` table, has incorrect SourceGroup {1} (spell effectMask) set - " +
                                   "effect masks are overlapping (all SourceGroup values having given bit set must be equal) - ignoring.", cond.ToString(), cond.SourceGroup);
                                return;
                            }
                        }
                        // no data for shared mask, we can create new submask
                        else
                        {
                            // add new list, create new shared mask
                            sharedList = new List<Condition>();
                            bool assigned = false;
                            for (uint i = firstEffIndex; i < spellInfo.GetEffects().Count; ++i)
                            {
                                if (((1 << (int)i) & commonMask) != 0)
                                {
                                    spellInfo.GetEffect(i).ImplicitTargetConditions = sharedList;
                                    assigned = true;
                                }
                            }

                            if (!assigned)
                                break;
                        }
                        sharedList.Add(cond);
                        break;
                    }
                }
            });
        }

        void AddToPhases(ConditionId id, List<Condition> conditions)
        {
            if (id.SourceEntry == 0)
            {
                PhaseInfoStruct phaseInfo = Global.ObjectMgr.GetPhaseInfo(id.SourceGroup);
                if (phaseInfo != null)
                {
                    bool found = false;
                    foreach (uint areaId in phaseInfo.Areas)
                    {
                        List<PhaseAreaInfo> phases = Global.ObjectMgr.GetPhasesForArea(areaId);
                        if (phases != null)
                        {
                            foreach (PhaseAreaInfo phase in phases)
                            {
                                if (phase.PhaseInfo.Id == id.SourceGroup)
                                {
                                    phase.Conditions.AddRange(conditions);
                                    found = true;
                                }
                            }
                        }
                    }

                    if (found)
                        return;
                }
            }
            else
            {
                var phases = Global.ObjectMgr.GetPhasesForArea((uint)id.SourceEntry);
                foreach (PhaseAreaInfo phase in phases)
                {
                    if (phase.PhaseInfo.Id == id.SourceGroup)
                    {
                        phase.Conditions.AddRange(conditions);
                        return;
                    }
                }
            }

            foreach (Condition condition in conditions)
                Log.outError(LogFilter.Sql, $"{condition} Area {condition.SourceEntry} does not have phase {condition.SourceGroup}.");
        }

        void AddToGraveyardData(ConditionId id, List<Condition> conditions)
        {
            GraveyardData graveyard = Global.ObjectMgr.FindGraveyardData((uint)id.SourceEntry, id.SourceGroup);
            if (graveyard != null)
            {
                graveyard.Conditions = new(conditions);
                return;
            }

            foreach (Condition condition in conditions)
                Log.outError(LogFilter.Sql, $"{condition}, Graveyard {condition.SourceEntry} does not have ghostzone {condition.SourceGroup}.");
        }

        bool IsSourceTypeValid(Condition cond)
        {
            switch (cond.SourceType)
            {
                case ConditionSourceType.CreatureLootTemplate:
                {
                    if (!LootStorage.Creature.HaveLootFor(cond.SourceGroup))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table, does not exist in `creature_loot_template`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.DisenchantLootTemplate:
                {
                    if (!LootStorage.Disenchant.HaveLootFor(cond.SourceGroup))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table, does not exist in `disenchant_loot_template`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.FishingLootTemplate:
                {
                    if (!LootStorage.Fishing.HaveLootFor(cond.SourceGroup))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table, does not exist in `fishing_loot_template`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.GameobjectLootTemplate:
                {
                    if (!LootStorage.Gameobject.HaveLootFor(cond.SourceGroup))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table, does not exist in `gameobject_loot_template`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.ItemLootTemplate:
                {
                    if (!LootStorage.Items.HaveLootFor(cond.SourceGroup))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table, does not exist in `item_loot_template`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.MailLootTemplate:
                {
                    if (!LootStorage.Mail.HaveLootFor(cond.SourceGroup))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table, does not exist in `mail_loot_template`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.MillingLootTemplate:
                {
                    if (!LootStorage.Milling.HaveLootFor(cond.SourceGroup))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table, does not exist in `milling_loot_template`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.PickpocketingLootTemplate:
                {
                    if (!LootStorage.Pickpocketing.HaveLootFor(cond.SourceGroup))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table, does not exist in `pickpocketing_loot_template`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.ProspectingLootTemplate:
                {
                    if (!LootStorage.Prospecting.HaveLootFor(cond.SourceGroup))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table, does not exist in `prospecting_loot_template`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.ReferenceLootTemplate:
                {
                    if (!LootStorage.Reference.HaveLootFor(cond.SourceGroup))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table, does not exist in `reference_loot_template`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.SkinningLootTemplate:
                {
                    if (!LootStorage.Skinning.HaveLootFor(cond.SourceGroup))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table, does not exist in `skinning_loot_template`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.SpellLootTemplate:
                {
                    if (!LootStorage.Spell.HaveLootFor(cond.SourceGroup))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table, does not exist in `spell_loot_template`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.SpellImplicitTarget:
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo((uint)cond.SourceEntry, Difficulty.None);
                    if (spellInfo == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceEntry in `condition` table does not exist in `spell.db2`, ignoring.", cond.ToString());
                        return false;
                    }

                    if ((cond.SourceGroup > SpellConst.MaxEffectMask) || cond.SourceGroup == 0)
                    {
                        Log.outError(LogFilter.Sql, "{0} in `condition` table, has incorrect SourceGroup (spell effectMask) set, ignoring.", cond.ToString());
                        return false;
                    }

                    uint origGroup = cond.SourceGroup;

                    foreach (SpellEffectInfo spellEffectInfo in spellInfo.GetEffects())
                    {
                        if (((1 << (int)spellEffectInfo.EffectIndex) & cond.SourceGroup) == 0)
                            continue;

                        if (spellEffectInfo.ChainTargets > 0)
                            continue;

                        switch (spellEffectInfo.TargetA.GetSelectionCategory())
                        {
                            case SpellTargetSelectionCategories.Nearby:
                            case SpellTargetSelectionCategories.Cone:
                            case SpellTargetSelectionCategories.Area:
                            case SpellTargetSelectionCategories.Traj:
                            case SpellTargetSelectionCategories.Line:
                                continue;
                            default:
                                break;
                        }

                        switch (spellEffectInfo.TargetB.GetSelectionCategory())
                        {
                            case SpellTargetSelectionCategories.Nearby:
                            case SpellTargetSelectionCategories.Cone:
                            case SpellTargetSelectionCategories.Area:
                            case SpellTargetSelectionCategories.Traj:
                            case SpellTargetSelectionCategories.Line:
                                continue;
                            default:
                                break;
                        }

                        switch (spellEffectInfo.Effect)
                        {
                            case SpellEffectName.PersistentAreaAura:
                            case SpellEffectName.ApplyAreaAuraParty:
                            case SpellEffectName.ApplyAreaAuraRaid:
                            case SpellEffectName.ApplyAreaAuraFriend:
                            case SpellEffectName.ApplyAreaAuraEnemy:
                            case SpellEffectName.ApplyAreaAuraPet:
                            case SpellEffectName.ApplyAreaAuraOwner:
                            case SpellEffectName.ApplyAuraOnPet:
                            case SpellEffectName.ApplyAreaAuraSummons:
                            case SpellEffectName.ApplyAreaAuraPartyNonrandom:
                                continue;
                            default:
                                break;
                        }

                        Log.outError(LogFilter.Sql, "SourceEntry {0} SourceGroup {1} in `condition` table - spell {2} does not have implicit targets of types: _AREA_, _CONE_, _NEARBY_, _CHAIN_ for effect {3}, SourceGroup needs correction, ignoring.", cond.SourceEntry, origGroup, cond.SourceEntry, spellEffectInfo.EffectIndex);
                        cond.SourceGroup &= ~(1u << (int)spellEffectInfo.EffectIndex);
                    }
                    // all effects were removed, no need to add the condition at all
                    if (cond.SourceGroup == 0)
                        return false;
                    break;
                }
                case ConditionSourceType.CreatureTemplateVehicle:
                {
                    if (Global.ObjectMgr.GetCreatureTemplate((uint)cond.SourceEntry) == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceEntry in `condition` table does not exist in `creature_template`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.Spell:
                case ConditionSourceType.SpellProc:
                {
                    SpellInfo spellProto = Global.SpellMgr.GetSpellInfo((uint)cond.SourceEntry, Difficulty.None);
                    if (spellProto == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceEntry in `condition` table does not exist in `spell.db2`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.QuestAvailable:
                    if (Global.ObjectMgr.GetQuestTemplate((uint)cond.SourceEntry) == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceEntry specifies non-existing quest, skipped.", cond.ToString());
                        return false;
                    }
                    break;
                case ConditionSourceType.VehicleSpell:
                    if (Global.ObjectMgr.GetCreatureTemplate(cond.SourceGroup) == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table does not exist in `creature_template`, ignoring.", cond.ToString());
                        return false;
                    }

                    if (!Global.SpellMgr.HasSpellInfo((uint)cond.SourceEntry, Difficulty.None))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceEntry in `condition` table does not exist in `spell.db2`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                case ConditionSourceType.SpellClickEvent:
                    if (Global.ObjectMgr.GetCreatureTemplate(cond.SourceGroup) == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table does not exist in `creature_template`, ignoring.", cond.ToString());
                        return false;
                    }

                    if (!Global.SpellMgr.HasSpellInfo((uint)cond.SourceEntry, Difficulty.None))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceEntry in `condition` table does not exist in `spell.db2`, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                case ConditionSourceType.NpcVendor:
                {
                    if (Global.ObjectMgr.GetCreatureTemplate(cond.SourceGroup) == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceGroup in `condition` table does not exist in `creature_template`, ignoring.", cond.ToString());
                        return false;
                    }
                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate((uint)cond.SourceEntry);
                    if (itemTemplate == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceEntry in `condition` table item does not exist, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.TerrainSwap:
                    if (!CliDB.MapStorage.ContainsKey((uint)cond.SourceEntry))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceEntry in `condition` table does not exist in Map.db2, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                case ConditionSourceType.Phase:
                    if (cond.SourceEntry != 0 && !CliDB.AreaTableStorage.ContainsKey(cond.SourceEntry))
                    {
                        Log.outError(LogFilter.Sql, "{0} SourceEntry in `condition` table does not exist in AreaTable.db2, ignoring.", cond.ToString());
                        return false;
                    }
                    break;
                case ConditionSourceType.Graveyard:
                    if (Global.ObjectMgr.FindGraveyardData((uint)cond.SourceEntry, cond.SourceGroup) == null)
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString()} SourceEntry in `condition` table, does not exist in `graveyard_zone`, ignoring.");
                        return false;
                    }
                    break;
                case ConditionSourceType.AreaTrigger:
                    if (cond.SourceEntry != 0 && cond.SourceEntry != 1)
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString()} in `condition` table, unexpected SourceEntry value (expected 0 or 1), ignoring.");
                        return false;
                    }
                    if (Global.AreaTriggerDataStorage.GetAreaTriggerTemplate(new AreaTriggerId(cond.SourceGroup, cond.SourceEntry == 1)) == null)
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString()} in `condition` table, does not exist in `areatrigger_template`, ignoring.");
                        return false;
                    }
                    break;
                case ConditionSourceType.ConversationLine:
                    if (Global.ConversationDataStorage.GetConversationLineTemplate((uint)cond.SourceEntry) == null)
                    {
                        Log.outError(LogFilter.Sql, $"{cond} does not exist in `conversation_line_template`, ignoring.");
                        return false;
                    }
                    break;
                case ConditionSourceType.AreatriggerClientTriggered:
                    if (!CliDB.AreaTriggerStorage.ContainsKey(cond.SourceEntry))
                    {
                        Log.outError(LogFilter.Sql, $"{cond} SourceEntry in `condition` table, does not exists in AreaTrigger.db2, ignoring.");
                        return false;
                    }
                    break;
                case ConditionSourceType.TrainerSpell:
                {
                    if (Global.ObjectMgr.GetTrainer(cond.SourceGroup) == null)
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString()} SourceGroup in `condition` table, does not exist in `trainer`, ignoring.");
                        return false;
                    }
                    if (Global.SpellMgr.GetSpellInfo((uint)cond.SourceEntry, Difficulty.None) == null)
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString()} SourceEntry in `condition` table does not exist in `Spell.db2`, ignoring.");
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.ObjectIdVisibility:
                {
                    if (cond.SourceGroup <= 0 || cond.SourceGroup >= (uint)TypeId.Max)
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString()} SourceGroup in `condition` table, is no valid object type, ignoring.");
                        return false;
                    }

                    if (cond.SourceGroup == (uint)TypeId.Unit)
                    {
                        if (Global.ObjectMgr.GetCreatureTemplate((uint)cond.SourceEntry) == null)
                        {
                            Log.outError(LogFilter.Sql, $"{cond.ToString()} SourceEntry in `condition` table, does not exist in `creature_template`, ignoring.");
                            return false;
                        }
                    }
                    else if (cond.SourceGroup == (uint)TypeId.GameObject)
                    {
                        if (Global.ObjectMgr.GetGameObjectTemplate((uint)cond.SourceEntry) == null)
                        {
                            Log.outError(LogFilter.Sql, $"{cond.ToString()} SourceEntry in `condition` table, does not exist in `gameobject_template`, ignoring.");
                            return false;
                        }
                    }
                    else
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString()} SourceGroup in `condition` table, uses unchecked type id, ignoring.");
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.SpawnGroup:
                {
                    SpawnGroupTemplateData spawnGroup = Global.ObjectMgr.GetSpawnGroupData((uint)cond.SourceEntry);
                    if (spawnGroup == null)
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString()} SourceEntry in `condition` table, does not exist in `spawn_group_template`, ignoring.");
                        return false;
                    }
                    if (spawnGroup.flags.HasAnyFlag(SpawnGroupFlags.System))
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString()} in `spawn_group_template` table cannot have SPAWNGROUP_FLAG_SYSTEM flags, ignoring.");
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.SkillLineAbility:
                {
                    var skillLineAbility = CliDB.SkillLineAbilityStorage.LookupByKey(cond.SourceEntry);
                    if (skillLineAbility == null)
                    {
                        Log.outError(LogFilter.Sql, $"{cond} SourceEntry in `condition` table, does not exist in SkillLineAbility.db2, ignoring.");
                        return false;
                    }
                    if (skillLineAbility.AcquireMethod != SkillLineAbilityAcquireMethod.LearnedOrAutomaticCharLevel)
                    {
                        Log.outError(LogFilter.Sql, $"{cond} in SkillLineAbility.db2 does not have AcquireMethod = {SkillLineAbilityAcquireMethod.LearnedOrAutomaticCharLevel} (LearnedOrAutomaticCharLevel), ignoring.");
                        return false;
                    }
                    break;
                }
                case ConditionSourceType.GossipMenu:
                case ConditionSourceType.GossipMenuOption:
                case ConditionSourceType.SmartEvent:
                case ConditionSourceType.PlayerCondition:
                    break;
                default:
                    Log.outError(LogFilter.Sql, $"{cond.ToString()} Invalid ConditionSourceType in `condition` table, ignoring.");
                    return false;
            }

            return true;
        }

        bool IsConditionTypeValid(Condition cond)
        {
            switch (cond.ConditionType)
            {
                case ConditionTypes.Aura:
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(cond.ConditionValue1, Difficulty.None);
                    if (spellInfo == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing spell (Id: {1}), skipped", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }

                    if (cond.ConditionValue2 >= spellInfo.GetEffects().Count)
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString(true)} spell {cond.ConditionValue1} has non existing effect index ({cond.ConditionValue2}) (must be 0..{spellInfo.GetEffects().Count - 1}), skipped.");
                        return false;
                    }

                    if (!spellInfo.GetEffect(cond.ConditionValue2).IsAura())
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString(true)} spell {cond.ConditionValue1} effect index {cond.ConditionValue2} is not an aura, skipped.");
                        return false;
                    }
                    break;
                }
                case ConditionTypes.Item:
                {
                    ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(cond.ConditionValue1);
                    if (proto == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} Item ({1}) does not exist, skipped", cond.ToString(), cond.ConditionValue1);
                        return false;
                    }

                    if (cond.ConditionValue2 == 0)
                    {
                        Log.outError(LogFilter.Sql, "{0} Zero item count in ConditionValue2, skipped", cond.ToString());
                        return false;
                    }
                    break;
                }
                case ConditionTypes.ItemEquipped:
                {
                    ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(cond.ConditionValue1);
                    if (proto == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} Item ({1}) does not exist, skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.Zoneid:
                {
                    AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(cond.ConditionValue1);
                    if (areaEntry == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} Area ({1}) does not exist, skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }

                    if (areaEntry.ParentAreaID != 0 && areaEntry.HasFlag(AreaFlags.IsSubzone))
                    {
                        Log.outError(LogFilter.Sql, "{0} requires to be in area ({1}) which is a subzone but zone expected, skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.ReputationRank:
                {
                    if (!CliDB.FactionStorage.ContainsKey(cond.ConditionValue1))
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing faction ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.Team:
                {
                    if (cond.ConditionValue1 != (uint)Team.Alliance && cond.ConditionValue1 != (uint)Team.Horde)
                    {
                        Log.outError(LogFilter.Sql, "{0} specifies unknown team ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.Skill:
                {
                    SkillLineRecord pSkill = CliDB.SkillLineStorage.LookupByKey(cond.ConditionValue1);
                    if (pSkill == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} specifies non-existing skill ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }

                    if (cond.ConditionValue2 < 1 || cond.ConditionValue2 > Global.WorldMgr.GetConfigMaxSkillValue())
                    {
                        Log.outError(LogFilter.Sql, "{0} specifies skill ({1}) with invalid value ({1}), skipped.", cond.ToString(true), cond.ConditionValue1, cond.ConditionValue2);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.Queststate:
                    if (cond.ConditionValue2 >= (1 << (int)QuestStatus.Max))
                    {
                        Log.outError(LogFilter.Sql, "{0} has invalid state mask ({1}), skipped.", cond.ToString(true), cond.ConditionValue2);
                        return false;
                    }

                    if (Global.ObjectMgr.GetQuestTemplate(cond.ConditionValue1) == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} points to non-existing quest ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                case ConditionTypes.QuestRewarded:
                case ConditionTypes.QuestTaken:
                case ConditionTypes.QuestNone:
                case ConditionTypes.QuestComplete:
                case ConditionTypes.DailyQuestDone:
                {
                    if (Global.ObjectMgr.GetQuestTemplate(cond.ConditionValue1) == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} points to non-existing quest ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.ActiveEvent:
                {
                    var events = Global.GameEventMgr.GetEventMap();
                    if (cond.ConditionValue1 >= events.Length || !events[cond.ConditionValue1].IsValid())
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing event id ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.Achievement:
                {
                    AchievementRecord achievement = CliDB.AchievementStorage.LookupByKey(cond.ConditionValue1);
                    if (achievement == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing achivement id ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.Class:
                {
                    if (Convert.ToBoolean(cond.ConditionValue1 & ~(uint)Class.ClassMaskAllPlayable))
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing classmask ({1}), skipped.", cond.ToString(true), cond.ConditionValue1 & ~(uint)Class.ClassMaskAllPlayable);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.Race:
                {
                    RaceMask<ulong> invalidRaceMask = new RaceMask<ulong>(cond.ConditionValue1 & ~RaceMask.AllPlayable.RawValue);
                    if (!invalidRaceMask.IsEmpty()) // uint32 works thanks to weird index remapping in racemask
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing racemask ({1}), skipped.", cond.ToString(true), cond.ConditionValue1 & ~RaceMask.AllPlayable.RawValue);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.Gender:
                {
                    if (!Player.IsValidGender((Gender)cond.ConditionValue1))
                    {
                        Log.outError(LogFilter.Sql, "{0} has invalid gender ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.Mapid:
                {
                    MapRecord me = CliDB.MapStorage.LookupByKey(cond.ConditionValue1);
                    if (me == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing map ({1}), skipped", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.Spell:
                {
                    if (!Global.SpellMgr.HasSpellInfo(cond.ConditionValue1, Difficulty.None))
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing spell (Id: {1}), skipped", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.Level:
                {
                    if (cond.ConditionValue2 >= (uint)ComparisionType.Max)
                    {
                        Log.outError(LogFilter.Sql, "{0} has invalid ComparisionType ({1}), skipped.", cond.ToString(true), cond.ConditionValue2);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.DrunkenState:
                {
                    if (cond.ConditionValue1 > (uint)DrunkenState.Smashed)
                    {
                        Log.outError(LogFilter.Sql, "{0} has invalid state ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.NearCreature:
                {
                    if (Global.ObjectMgr.GetCreatureTemplate(cond.ConditionValue1) == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing creature template entry ({1}), skipped", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.NearGameobject:
                {
                    if (Global.ObjectMgr.GetGameObjectTemplate(cond.ConditionValue1) == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing gameobject template entry ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.ObjectEntryGuidLegacy:
                {
                    cond.ConditionType = ConditionTypes.ObjectEntryGuid;
                    cond.ConditionValue1 = Legacy.ConvertLegacyTypeID(cond.ConditionValue1);
                    goto case ConditionTypes.ObjectEntryGuid;
                }
                case ConditionTypes.ObjectEntryGuid:
                {
                    switch ((TypeId)cond.ConditionValue1)
                    {
                        case TypeId.Unit:
                            if (cond.ConditionValue2 != 0 && Global.ObjectMgr.GetCreatureTemplate(cond.ConditionValue2) == null)
                            {
                                Log.outError(LogFilter.Sql, "{0} has non existing creature template entry ({1}), skipped.", cond.ToString(true), cond.ConditionValue2);
                                return false;
                            }
                            if (cond.ConditionValue3 != 0)
                            {
                                CreatureData creatureData = Global.ObjectMgr.GetCreatureData(cond.ConditionValue3);
                                if (creatureData != null)
                                {
                                    if (cond.ConditionValue2 != 0 && creatureData.Id != cond.ConditionValue2)
                                    {
                                        Log.outError(LogFilter.Sql, "{0} has guid {1} set but does not match creature entry ({1}), skipped.", cond.ToString(true), cond.ConditionValue3, cond.ConditionValue2);
                                        return false;
                                    }
                                }
                                else
                                {
                                    Log.outError(LogFilter.Sql, "{0} has non existing creature guid ({1}), skipped.", cond.ToString(true), cond.ConditionValue3);
                                    return false;
                                }
                            }
                            break;
                        case TypeId.GameObject:
                            if (cond.ConditionValue2 != 0 && Global.ObjectMgr.GetGameObjectTemplate(cond.ConditionValue2) == null)
                            {
                                Log.outError(LogFilter.Sql, "{0} has non existing gameobject template entry ({1}), skipped.", cond.ToString(true), cond.ConditionValue2);
                                return false;
                            }
                            if (cond.ConditionValue3 != 0)
                            {
                                GameObjectData goData = Global.ObjectMgr.GetGameObjectData(cond.ConditionValue3);
                                if (goData != null)
                                {
                                    if (cond.ConditionValue2 != 0 && goData.Id != cond.ConditionValue2)
                                    {
                                        Log.outError(LogFilter.Sql, "{0} has guid {1} set but does not match gameobject entry ({1}), skipped.", cond.ToString(true), cond.ConditionValue3, cond.ConditionValue2);
                                        return false;
                                    }
                                }
                                else
                                {
                                    Log.outError(LogFilter.Sql, "{0} has non existing gameobject guid ({1}), skipped.", cond.ToString(true), cond.ConditionValue3);
                                    return false;
                                }
                            }
                            break;
                        case TypeId.Player:
                        case TypeId.Corpse:
                            if (cond.ConditionValue2 != 0)
                                LogUselessConditionValue(cond, 2, cond.ConditionValue2);
                            if (cond.ConditionValue3 != 0)
                                LogUselessConditionValue(cond, 3, cond.ConditionValue3);
                            break;
                        default:
                            Log.outError(LogFilter.Sql, "{0} has wrong typeid set ({1}), skipped", cond.ToString(true), cond.ConditionValue1);
                            return false;
                    }
                    break;
                }
                case ConditionTypes.TypeMaskLegacy:
                    cond.ConditionType = ConditionTypes.TypeMask;
                    cond.ConditionValue1 = Legacy.ConvertLegacyTypeMask(cond.ConditionValue1);
                    goto case ConditionTypes.TypeMask;
                case ConditionTypes.TypeMask:
                {
                    if (cond.ConditionValue1 == 0 || Convert.ToBoolean(cond.ConditionValue1 & ~(uint)(TypeMask.Unit | TypeMask.Player | TypeMask.GameObject | TypeMask.Corpse)))
                    {
                        Log.outError(LogFilter.Sql, "{0} has invalid typemask set ({1}), skipped.", cond.ToString(true), cond.ConditionValue2);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.RelationTo:
                {
                    if (cond.ConditionValue1 >= cond.GetMaxAvailableConditionTargets())
                    {
                        Log.outError(LogFilter.Sql, "{0} has invalid ConditionValue1(ConditionTarget selection) ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    if (cond.ConditionValue1 == cond.ConditionTarget)
                    {
                        Log.outError(LogFilter.Sql, "{0} has ConditionValue1(ConditionTarget selection) set to self ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    if (cond.ConditionValue2 >= (uint)RelationType.Max)
                    {
                        Log.outError(LogFilter.Sql, "{0} has invalid ConditionValue2(RelationType) ({1}), skipped.", cond.ToString(true), cond.ConditionValue2);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.ReactionTo:
                {
                    if (cond.ConditionValue1 >= cond.GetMaxAvailableConditionTargets())
                    {
                        Log.outError(LogFilter.Sql, "{0} has invalid ConditionValue1(ConditionTarget selection) ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    if (cond.ConditionValue1 == cond.ConditionTarget)
                    {
                        Log.outError(LogFilter.Sql, "{0} has ConditionValue1(ConditionTarget selection) set to self ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    if (cond.ConditionValue2 == 0)
                    {
                        Log.outError(LogFilter.Sql, "{0} has invalid ConditionValue2(rankMask) ({1}), skipped.", cond.ToString(true), cond.ConditionValue2);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.DistanceTo:
                {
                    if (cond.ConditionValue1 >= cond.GetMaxAvailableConditionTargets())
                    {
                        Log.outError(LogFilter.Sql, "{0} has invalid ConditionValue1(ConditionTarget selection) ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    if (cond.ConditionValue1 == cond.ConditionTarget)
                    {
                        Log.outError(LogFilter.Sql, "{0} has ConditionValue1(ConditionTarget selection) set to self ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    if (cond.ConditionValue3 >= (uint)ComparisionType.Max)
                    {
                        Log.outError(LogFilter.Sql, "{0} has invalid ComparisionType ({1}), skipped.", cond.ToString(true), cond.ConditionValue3);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.HpVal:
                {
                    if (cond.ConditionValue2 >= (uint)ComparisionType.Max)
                    {
                        Log.outError(LogFilter.Sql, "{0} has invalid ComparisionType ({1}), skipped.", cond.ToString(true), cond.ConditionValue2);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.HpPct:
                {
                    if (cond.ConditionValue1 > 100)
                    {
                        Log.outError(LogFilter.Sql, "{0} has too big percent value ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    if (cond.ConditionValue2 >= (uint)ComparisionType.Max)
                    {
                        Log.outError(LogFilter.Sql, "{0} has invalid ComparisionType ({1}), skipped.", cond.ToString(true), cond.ConditionValue2);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.WorldState:
                {
                    if (Global.WorldStateMgr.GetWorldStateTemplate((int)cond.ConditionValue1) == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing world state in value1 ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.PhaseId:
                {
                    if (!CliDB.PhaseStorage.ContainsKey(cond.ConditionValue1))
                    {
                        Log.outError(LogFilter.Sql, "{0} has nonexistent phaseid in value1 ({1}), skipped", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.Title:
                {
                    CharTitlesRecord titleEntry = CliDB.CharTitlesStorage.LookupByKey(cond.ConditionValue1);
                    if (titleEntry == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing title in value1 ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.SpawnmaskDeprecated:
                {
                    Log.outError(LogFilter.Sql, $"{cond.ToString(true)} using deprecated condition type CONDITION_SPAWNMASK.");
                    return false;
                }
                case ConditionTypes.UnitState:
                {
                    if (cond.ConditionValue1 > (uint)UnitState.AllStateSupported)
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing UnitState in value1 ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.CreatureType:
                {
                    if (cond.ConditionValue1 == 0 || cond.ConditionValue1 > (uint)CreatureType.GasCloud)
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing CreatureType in value1 ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.RealmAchievement:
                {
                    AchievementRecord achievement = CliDB.AchievementStorage.LookupByKey(cond.ConditionValue1);
                    if (achievement == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} has non existing realm first achivement id ({1}), skipped.", cond.ToString(), cond.ConditionValue1);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.StandState:
                {
                    bool valid;
                    switch (cond.ConditionValue1)
                    {
                        case 0:
                            valid = cond.ConditionValue2 <= (uint)UnitStandStateType.Submerged;
                            break;
                        case 1:
                            valid = cond.ConditionValue2 <= 1;
                            break;
                        default:
                            valid = false;
                            break;
                    }
                    if (!valid)
                    {
                        Log.outError(LogFilter.Sql, "{0} has non-existing stand state ({1},{2}), skipped.", cond.ToString(true), cond.ConditionValue1, cond.ConditionValue2);
                        return false;
                    }
                    break;
                }
                case ConditionTypes.ObjectiveProgress:
                {
                    QuestObjective obj = Global.ObjectMgr.GetQuestObjective(cond.ConditionValue1);
                    if (obj == null)
                    {
                        Log.outError(LogFilter.Sql, "{0} points to non-existing quest objective ({1}), skipped.", cond.ToString(true), cond.ConditionValue1);
                        return false;
                    }
                    int limit = obj.IsStoringFlag() ? 1 : obj.Amount;
                    if (cond.ConditionValue3 > limit)
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString(true)} has quest objective count {cond.ConditionValue3} in value3, but quest objective {cond.ConditionValue1} has a maximum objective count of {limit}, skipped.");
                        return false;
                    }
                    break;
                }
                case ConditionTypes.PetType:
                    if (cond.ConditionValue1 >= (1 << (int)PetType.Max))
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString(true)} has non-existing pet type {cond.ConditionValue1}, skipped.");
                        return false;
                    }
                    break;
                case ConditionTypes.InstanceInfo:
                    if (cond.ConditionValue3 == (uint)InstanceInfo.GuidData)
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString(true)} has unsupported ConditionValue3 {cond.ConditionValue3} (INSTANCE_INFO_GUID_DATA), skipped.");
                        return false;
                    }
                    break;
                case ConditionTypes.Alive:
                case ConditionTypes.Areaid:
                case ConditionTypes.TerrainSwap:
                case ConditionTypes.InWater:
                case ConditionTypes.Charmed:
                case ConditionTypes.Taxi:
                case ConditionTypes.Gamemaster:
                case ConditionTypes.PrivateObject:
                case ConditionTypes.StringId:
                case ConditionTypes.Label:
                    break;
                case ConditionTypes.DifficultyId:
                    if (!CliDB.DifficultyStorage.ContainsKey(cond.ConditionValue1))
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString(true)} has non existing difficulty in value1 ({cond.ConditionValue1}), skipped.");
                        return false;
                    }
                    break;
                case ConditionTypes.BattlePetCount:
                    if (!CliDB.BattlePetSpeciesStorage.ContainsKey(cond.ConditionValue1))
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString(true)} has non existing BattlePet SpeciesId in value1 ({cond.ConditionValue1}), skipped.");
                        return false;
                    }
                    if (cond.ConditionValue2 > SharedConst.DefaultMaxBattlePetsPerSpecies)
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString(true)} has invalid (greater than {SharedConst.DefaultMaxBattlePetsPerSpecies}) value2 ({cond.ConditionValue2}), skipped.");
                        return false;
                    }
                    if (cond.ConditionValue3 >= (uint)ComparisionType.Max)
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString(true)} has invalid ComparisionType ({cond.ConditionValue3}), skipped.");
                        return false;
                    }
                    break;
                case ConditionTypes.ScenarioStep:
                {
                    if (!CliDB.ScenarioStepStorage.ContainsKey(cond.ConditionValue1))
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString(true)} has non existing ScenarioStep in value1 ({cond.ConditionValue1}), skipped.");
                        return false;
                    }
                    break;
                }
                case ConditionTypes.SceneInProgress:
                {
                    if (!CliDB.SceneScriptPackageStorage.ContainsKey(cond.ConditionValue1))
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString(true)} has non existing SceneScriptPackageId in value1 ({cond.ConditionValue1}), skipped.");
                        return false;
                    }
                    break;
                }
                case ConditionTypes.PlayerCondition:
                {
                    if (!CliDB.PlayerConditionStorage.ContainsKey(cond.ConditionValue1))
                    {
                        Log.outError(LogFilter.Sql, $"{cond.ToString(true)} has non existing PlayerConditionId in value1 ({cond.ConditionValue1}), skipped.");
                        return false;
                    }
                    break;
                }
                default:
                    Log.outError(LogFilter.Sql, $"{cond.ToString()} Invalid ConditionType in `condition` table, ignoring.");
                    return false;
            }

            if (cond.ConditionTarget >= cond.GetMaxAvailableConditionTargets())
            {
                Log.outError(LogFilter.Sql, $"{cond.ToString(true)} in `condition` table, has incorrect ConditionTarget set, ignoring.");
                return false;
            }

            if (cond.ConditionValue1 != 0 && !StaticConditionTypeData[(int)cond.ConditionType].HasConditionValue1)
                LogUselessConditionValue(cond, 1, cond.ConditionValue1);
            if (cond.ConditionValue2 != 0 && !StaticConditionTypeData[(int)cond.ConditionType].HasConditionValue2)
                LogUselessConditionValue(cond, 2, cond.ConditionValue2);
            if (cond.ConditionValue3 != 0 && !StaticConditionTypeData[(int)cond.ConditionType].HasConditionValue3)
                LogUselessConditionValue(cond, 3, cond.ConditionValue3);
            if (!cond.ConditionStringValue1.IsEmpty() && !StaticConditionTypeData[(int)cond.ConditionType].HasConditionStringValue1)
                LogUselessConditionValue(cond, 1, cond.ConditionStringValue1);

            return true;
        }

        void LogUselessConditionValue(Condition cond, byte index, uint value)
        {
            Log.outError(LogFilter.Sql, $"{cond.ToString(true)} has useless data in ConditionValue{index} ({value})!");
        }

        void LogUselessConditionValue(Condition cond, byte index, string value)
        {
            Log.outError(LogFilter.Sql, $"{cond.ToString(true)} has useless data in ConditionStringValue{index} ({value})!");
        }

        void Clean()
        {
            foreach (var (_, conditionsMap) in ConditionStorage)
                conditionsMap.Clear();

            spellsUsedInSpellClickConditions.Clear();
        }

        static bool PlayerConditionCompare(int comparisonType, int value1, int value2)
        {
            switch (comparisonType)
            {
                case 1:
                    return value1 == value2;
                case 2:
                    return value1 != value2;
                case 3:
                    return value1 > value2;
                case 4:
                    return value1 >= value2;
                case 5:
                    return value1 < value2;
                case 6:
                    return value1 <= value2;
                default:
                    break;
            }
            return false;
        }

        static bool PlayerConditionLogic(uint logic, bool[] results)
        {
            Cypher.Assert(results.Length < 8, "Logic array size must be equal to or less than 8");

            for (var i = 0; i < results.Length; ++i)
            {
                if (Convert.ToBoolean((logic >> (16 + i)) & 1))
                    results[i] ^= true;
            }

            bool result = results[0];
            for (var i = 1; i < results.Length; ++i)
            {
                switch ((logic >> (2 * (i - 1))) & 3)
                {
                    case 1:
                        result = result && results[i];
                        break;
                    case 2:
                        result = result || results[i];
                        break;
                    default:
                        break;
                }
            }

            return result;
        }

        public static uint GetPlayerConditionLfgValue(Player player, PlayerConditionLfgStatus status)
        {
            if (player.GetGroup() == null)
                return 0;

            switch (status)
            {
                case PlayerConditionLfgStatus.InLFGDungeon:
                    return Global.LFGMgr.InLfgDungeonMap(player.GetGUID(), player.GetMapId(), player.GetMap().GetDifficultyID()) ? 1 : 0u;
                case PlayerConditionLfgStatus.InLFGRandomDungeon:
                    return Global.LFGMgr.InLfgDungeonMap(player.GetGUID(), player.GetMapId(), player.GetMap().GetDifficultyID()) &&
                        Global.LFGMgr.SelectedRandomLfgDungeon(player.GetGUID()) ? 1 : 0u;
                case PlayerConditionLfgStatus.InLFGFirstRandomDungeon:
                {
                    if (!Global.LFGMgr.InLfgDungeonMap(player.GetGUID(), player.GetMapId(), player.GetMap().GetDifficultyID()))
                        return 0;

                    uint selectedRandomDungeon = Global.LFGMgr.GetSelectedRandomDungeon(player.GetGUID());
                    if (selectedRandomDungeon == 0)
                        return 0;

                    DungeonFinding.LfgReward reward = Global.LFGMgr.GetRandomDungeonReward(selectedRandomDungeon, player.GetLevel());
                    if (reward != null)
                    {
                        Quest quest = Global.ObjectMgr.GetQuestTemplate(reward.firstQuest);
                        if (quest != null)
                            if (player.CanRewardQuest(quest, false))
                                return 1;
                    }
                    return 0;
                }
                case PlayerConditionLfgStatus.PartialClear:
                    break;
                case PlayerConditionLfgStatus.StrangerCount:
                    break;
                case PlayerConditionLfgStatus.VoteKickCount:
                    break;
                case PlayerConditionLfgStatus.BootCount:
                    break;
                case PlayerConditionLfgStatus.GearDiff:
                    break;
                default:
                    break;
            }

            return 0;
        }

        public static bool IsPlayerMeetingCondition(Player player, uint conditionId)
        {
            if (conditionId == 0)
                return true;

            if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.PlayerCondition, conditionId, player))
                return false;

            var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(conditionId);
            if (playerCondition != null)
                return IsPlayerMeetingCondition(player, playerCondition);

            return true;
        }

        public static bool IsPlayerMeetingCondition(Player player, PlayerConditionRecord condition)
        {
            ContentTuningLevels? levels = Global.DB2Mgr.GetContentTuningData(condition.ContentTuningID, player.m_playerData.CtrOptions.GetValue().ConditionalFlags);
            if (levels.HasValue)
            {
                byte minLevel = (byte)(condition.Flags.HasAnyFlag(0x800) ? levels.Value.MinLevelWithDelta : levels.Value.MinLevel);
                byte maxLevel = 0;
                if (!condition.Flags.HasAnyFlag(0x20))
                    maxLevel = (byte)(condition.Flags.HasAnyFlag(0x800) ? levels.Value.MaxLevelWithDelta : levels.Value.MaxLevel);
                if (condition.Flags.HasAnyFlag(0x80))
                {
                    if (minLevel != 0 && player.GetLevel() >= minLevel && (maxLevel == 0 || player.GetLevel() <= maxLevel))
                        return false;

                    if (maxLevel != 0 && player.GetLevel() <= maxLevel && (minLevel == 0 || player.GetLevel() >= minLevel))
                        return false;
                }
                else
                {
                    if (minLevel != 0 && player.GetLevel() < minLevel)
                        return false;

                    if (maxLevel != 0 && player.GetLevel() > maxLevel)
                        return false;
                }
            }

            var raceMask = new RaceMask<long>(condition.RaceMask);
            if (!raceMask.IsEmpty() && raceMask.HasRace(player.GetRace()))
                return false;

            if (condition.ClassMask != 0 && !Convert.ToBoolean(player.GetClassMask() & condition.ClassMask))
                return false;

            if (condition.Gender >= 0 && (int)player.GetGender() != condition.Gender)
                return false;

            if (condition.NativeGender >= 0 && player.GetNativeGender() != (Gender)condition.NativeGender)
                return false;

            if (condition.PowerType != -1 && condition.PowerTypeComp != 0)
            {
                int requiredPowerValue = Convert.ToBoolean(condition.Flags & 4) ? player.GetMaxPower((PowerType)condition.PowerType) : condition.PowerTypeValue;
                if (!PlayerConditionCompare(condition.PowerTypeComp, player.GetPower((PowerType)condition.PowerType), requiredPowerValue))
                    return false;
            }

            if (condition.ChrSpecializationIndex >= 0 || condition.ChrSpecializationRole >= 0)
            {
                ChrSpecializationRecord spec = CliDB.ChrSpecializationStorage.LookupByKey((uint)player.GetPrimarySpecialization());
                if (spec != null)
                {
                    if (condition.ChrSpecializationIndex >= 0 && spec.OrderIndex != condition.ChrSpecializationIndex)
                        return false;

                    if (condition.ChrSpecializationRole >= 0 && spec.Role != condition.ChrSpecializationRole)
                        return false;
                }
            }

            bool[] results;

            if (condition.SkillID[0] != 0 || condition.SkillID[1] != 0 || condition.SkillID[2] != 0 || condition.SkillID[3] != 0)
            {
                results = new bool[condition.SkillID.Length];
                for (var i = 0; i < results.Length; ++i)
                    results[i] = true;

                for (var i = 0; i < condition.SkillID.Length; ++i)
                {
                    if (condition.SkillID[i] != 0)
                    {
                        ushort skillValue = player.GetSkillValue((SkillType)condition.SkillID[i]);
                        results[i] = skillValue != 0 && skillValue > condition.MinSkill[i] && skillValue < condition.MaxSkill[i];
                    }
                }

                if (!PlayerConditionLogic(condition.SkillLogic, results))
                    return false;
            }

            if (condition.LanguageID != 0)
            {
                int languageSkill = 0;
                if (player.HasAuraTypeWithMiscvalue(AuraType.ComprehendLanguage, condition.LanguageID))
                    languageSkill = 300;
                else
                {
                    foreach (var languageDesc in Global.LanguageMgr.GetLanguageDescById((Language)condition.LanguageID))
                        languageSkill = Math.Max(languageSkill, player.GetSkillValue((SkillType)languageDesc.SkillId));
                }

                if (condition.MinLanguage != 0 && languageSkill < condition.MinLanguage)
                    return false;

                if (condition.MaxLanguage != 0 && languageSkill > condition.MaxLanguage)
                    return false;
            }

            if (condition.MinFactionID[0] != 0 && condition.MinFactionID[1] != 0 && condition.MinFactionID[2] != 0 && condition.MaxFactionID != 0)
            {
                if (condition.MinFactionID[0] == 0 && condition.MinFactionID[1] == 0 && condition.MinFactionID[2] == 0)
                {
                    ReputationRank forcedRank = player.GetReputationMgr().GetForcedRankIfAny(condition.MaxFactionID);
                    if (forcedRank != 0)
                    {
                        if ((uint)forcedRank > condition.MaxReputation)
                            return false;
                    }
                    else if (CliDB.FactionStorage.HasRecord(condition.MaxReputation) && (uint)player.GetReputationRank(condition.MaxFactionID) > condition.MaxReputation)
                        return false;
                }
                else
                {
                    results = new bool[condition.MinFactionID.Length + 1];
                    for (var i = 0; i < results.Length; ++i)
                        results[i] = true;

                    for (var i = 0; i < condition.MinFactionID.Length; ++i)
                    {
                        if (CliDB.FactionStorage.HasRecord(condition.MinFactionID[i]))
                        {
                            ReputationRank forcedRank = player.GetReputationMgr().GetForcedRankIfAny(condition.MinFactionID[i]);
                            if (forcedRank != 0)
                                results[i] = (uint)forcedRank >= condition.MinReputation[i];
                            else
                                results[i] = (uint)player.GetReputationRank(condition.MinFactionID[i]) >= condition.MinReputation[i];
                        }
                    }
                    ReputationRank forcedRank1 = player.GetReputationMgr().GetForcedRankIfAny(condition.MaxFactionID);
                    if (forcedRank1 != 0)
                        results[3] = (uint)forcedRank1 <= condition.MaxReputation;
                    else if (CliDB.FactionStorage.HasRecord(condition.MaxReputation))
                        results[3] = (uint)player.GetReputationRank(condition.MaxFactionID) <= condition.MaxReputation;

                    if (!PlayerConditionLogic(condition.ReputationLogic, results))
                        return false;
                }
            }

            if (condition.CurrentPvpFaction != 0)
            {
                byte team;
                if (player.GetMap().IsBattlegroundOrArena())
                    team = player.m_playerData.ArenaFaction;
                else
                    team = (byte)(player.GetTeamId() == BattleGroundTeamId.Alliance ? 1 : 0);

                if (condition.CurrentPvpFaction - 1 != team)
                    return false;
            }

            if (condition.PvpMedal != 0 && !Convert.ToBoolean((1 << (condition.PvpMedal - 1)) & player.m_activePlayerData.PvpMedals))
                return false;

            if (condition.LifetimeMaxPVPRank != 0 && player.m_activePlayerData.LifetimeMaxRank != condition.LifetimeMaxPVPRank)
                return false;

            if (condition.MovementFlags[0] != 0 && !Convert.ToBoolean((uint)player.GetUnitMovementFlags() & condition.MovementFlags[0]))
                return false;

            if (condition.MovementFlags[1] != 0 && !Convert.ToBoolean((uint)player.GetUnitMovementFlags2() & condition.MovementFlags[1]))
                return false;

            if (condition.WeaponSubclassMask != 0)
            {
                Item mainHand = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
                if (mainHand == null || !Convert.ToBoolean((1 << (int)mainHand.GetTemplate().GetSubClass()) & condition.WeaponSubclassMask))
                    return false;
            }

            if (condition.PartyStatus != 0)
            {
                Group group = player.GetGroup();
                switch (condition.PartyStatus)
                {
                    case 1:
                        if (group != null)
                            return false;
                        break;
                    case 2:
                        if (group == null)
                            return false;
                        break;
                    case 3:
                        if (group == null || group.IsRaidGroup())
                            return false;
                        break;
                    case 4:
                        if (group == null || !group.IsRaidGroup())
                            return false;
                        break;
                    case 5:
                        if (group != null && group.IsRaidGroup())
                            return false;
                        break;
                    default:
                        break;
                }
            }

            if (condition.PrevQuestID[0] != 0)
            {
                results = new bool[condition.PrevQuestID.Length];
                for (var i = 0; i < results.Length; ++i)
                    results[i] = true;

                for (var i = 0; i < condition.PrevQuestID.Length; ++i)
                    results[i] = player.IsQuestCompletedBitSet(condition.PrevQuestID[i]);

                if (!PlayerConditionLogic(condition.PrevQuestLogic, results))
                    return false;
            }

            if (condition.CurrQuestID[0] != 0)
            {
                results = new bool[condition.CurrQuestID.Length];
                for (var i = 0; i < results.Length; ++i)
                    results[i] = true;

                for (var i = 0; i < condition.CurrQuestID.Length; ++i)
                {
                    if (condition.CurrQuestID[i] != 0)
                        results[i] = player.FindQuestSlot(condition.CurrQuestID[i]) != SharedConst.MaxQuestLogSize;
                }

                if (!PlayerConditionLogic(condition.CurrQuestLogic, results))
                    return false;
            }

            if (condition.CurrentCompletedQuestID[0] != 0)
            {
                results = new bool[condition.CurrentCompletedQuestID.Length];
                for (var i = 0; i < results.Length; ++i)
                    results[i] = true;

                for (var i = 0; i < condition.CurrentCompletedQuestID.Length; ++i)
                {
                    if (condition.CurrentCompletedQuestID[i] != 0)
                        results[i] = player.GetQuestStatus(condition.CurrentCompletedQuestID[i]) == QuestStatus.Complete;
                }

                if (!PlayerConditionLogic(condition.CurrentCompletedQuestLogic, results))
                    return false;
            }


            if (condition.SpellID[0] != 0)
            {
                results = new bool[condition.SpellID.Length];
                for (var i = 0; i < results.Length; ++i)
                    results[i] = true;

                for (var i = 0; i < condition.SpellID.Length; ++i)
                {
                    if (condition.SpellID[i] != 0)
                        results[i] = player.HasSpell(condition.SpellID[i]);
                }

                if (!PlayerConditionLogic(condition.SpellLogic, results))
                    return false;
            }

            if (condition.ItemID[0] != 0)
            {
                results = new bool[condition.ItemID.Length];
                for (var i = 0; i < results.Length; ++i)
                    results[i] = true;

                for (var i = 0; i < condition.ItemID.Length; ++i)
                {
                    if (condition.ItemID[i] != 0)
                    {
                        ItemSearchLocation where = ItemSearchLocation.Equipment;
                        if ((condition.ItemFlags & 1) != 0)    // include banks
                            where |= ItemSearchLocation.Bank | ItemSearchLocation.ReagentBank | ItemSearchLocation.AccountBank;
                        if ((condition.ItemFlags & 2) == 0)    // ignore inventory
                            where |= ItemSearchLocation.Inventory;

                        uint foundCount = 0;
                        results[i] = !player.ForEachItem(where, item =>
                        {
                            if (item.GetEntry() == condition.ItemID[i])
                            {
                                foundCount += item.GetCount();
                                if (foundCount >= condition.ItemCount[i])
                                    return false;
                            }

                            return true;
                        });

                        if (!results[i] && condition.ItemCount[i] == 1 && Global.DB2Mgr.IsToyItem(condition.ItemID[i]))
                            results[i] = player.GetSession().GetCollectionMgr().HasToy(condition.ItemID[i]);
                    }
                }

                if (!PlayerConditionLogic(condition.ItemLogic, results))
                    return false;
            }

            if (condition.CurrencyID[0] != 0)
            {
                results = new bool[condition.CurrencyID.Length];
                for (var i = 0; i < results.Length; ++i)
                    results[i] = true;

                for (var i = 0; i < condition.CurrencyID.Length; ++i)
                {
                    if (condition.CurrencyID[i] != 0)
                        results[i] = player.GetCurrencyQuantity(condition.CurrencyID[i]) >= condition.CurrencyCount[i];
                }

                if (!PlayerConditionLogic(condition.CurrencyLogic, results))
                    return false;
            }

            if (condition.Explored[0] != 0 || condition.Explored[1] != 0)
            {
                for (var i = 0; i < condition.Explored.Length; ++i)
                {
                    AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(condition.Explored[i]);
                    if (area != null && !player.HasExploredZone(area.Id))
                        return false;
                }
            }

            if (condition.AuraSpellID[0] != 0)
            {
                results = new bool[condition.AuraSpellID.Length];
                for (var i = 0; i < results.Length; ++i)
                    results[i] = true;

                for (var i = 0; i < condition.AuraSpellID.Length; ++i)
                {
                    if (condition.AuraSpellID[i] != 0)
                    {
                        if (condition.AuraStacks[i] != 0)
                            results[i] = player.GetAuraCount(condition.AuraSpellID[i]) >= condition.AuraStacks[i];
                        else
                            results[i] = player.HasAura(condition.AuraSpellID[i]);
                    }
                }

                if (!PlayerConditionLogic(condition.AuraSpellLogic, results))
                    return false;
            }

            if (condition.Time[0] != 0)
            {
                WowTime time0 = new();
                time0.SetPackedTime(condition.Time[0]);

                if (condition.Time[1] != 0)
                {
                    WowTime time1 = new();
                    time1.SetPackedTime(condition.Time[1]);

                    if (!GameTime.GetWowTime().IsInRange(time0, time1))
                        return false;
                }
                else if (GameTime.GetWowTime() != time0)
                    return false;
            }

            if (condition.WorldStateExpressionID != 0)
            {
                var worldStateExpression = CliDB.WorldStateExpressionStorage.LookupByKey(condition.WorldStateExpressionID);
                if (worldStateExpression == null)
                    return false;

                if (!IsMeetingWorldStateExpression(player.GetMap(), worldStateExpression))
                    return false;
            }

            if (condition.WeatherID != 0)
                if (player.GetMap().GetZoneWeather(player.GetZoneId()) != (WeatherState)condition.WeatherID)
                    return false;

            if (condition.Achievement[0] != 0)
            {
                results = new bool[condition.Achievement.Length];
                for (var i = 0; i < results.Length; ++i)
                    results[i] = true;

                for (var i = 0; i < condition.Achievement.Length; ++i)
                {
                    if (condition.Achievement[i] != 0)
                    {
                        // if (condition.Flags & 2) { any character on account completed it } else { current character only }
                        // TODO: part of accountwide achievements
                        results[i] = player.HasAchieved(condition.Achievement[i]);
                    }
                }

                if (!PlayerConditionLogic(condition.AchievementLogic, results))
                    return false;
            }

            if (condition.LfgStatus[0] != 0)
            {
                results = new bool[condition.LfgStatus.Length];
                for (var i = 0; i < results.Length; ++i)
                    results[i] = true;

                for (int i = 0; i < condition.LfgStatus.Length; ++i)
                    if (condition.LfgStatus[i] != 0)
                        results[i] = PlayerConditionCompare(condition.LfgCompare[i], (int)GetPlayerConditionLfgValue(player, (PlayerConditionLfgStatus)condition.LfgStatus[i]), (int)condition.LfgValue[i]);

                if (!PlayerConditionLogic(condition.LfgLogic, results))
                    return false;
            }

            if (condition.AreaID[0] != 0)
            {
                results = new bool[condition.AreaID.Length];
                for (var i = 0; i < results.Length; ++i)
                    results[i] = true;

                for (var i = 0; i < condition.AreaID.Length; ++i)
                    if (condition.AreaID[i] != 0)
                        results[i] = Global.DB2Mgr.IsInArea(player.GetAreaId(), condition.AreaID[i]);

                if (!PlayerConditionLogic(condition.AreaLogic, results))
                    return false;
            }

            if (condition.MinExpansionLevel != -1 && (int)player.GetSession().GetExpansion() < condition.MinExpansionLevel)
                return false;

            if (condition.MaxExpansionLevel != -1 && (int)player.GetSession().GetExpansion() > condition.MaxExpansionLevel)
                return false;

            if (condition.MinExpansionLevel != -1 && condition.MinExpansionTier != -1 && !player.IsGameMaster()
                && ((condition.MinExpansionLevel == WorldConfig.GetIntValue(WorldCfg.Expansion) && condition.MinExpansionTier > 0) /*TODO: implement tier*/
                || condition.MinExpansionLevel > WorldConfig.GetIntValue(WorldCfg.Expansion)))
                return false;

            if (condition.PhaseID != 0 || condition.PhaseGroupID != 0 || condition.PhaseUseFlags != 0)
                if (!PhasingHandler.InDbPhaseShift(player, (PhaseUseFlagsValues)condition.PhaseUseFlags, condition.PhaseID, condition.PhaseGroupID))
                    return false;

            if (condition.QuestKillID != 0)
            {
                Quest quest = Global.ObjectMgr.GetQuestTemplate(condition.QuestKillID);
                ushort questSlot = player.FindQuestSlot(condition.QuestKillID);

                if (quest != null && player.GetQuestStatus(condition.QuestKillID) != QuestStatus.Complete && questSlot < SharedConst.MaxQuestLogSize)
                {
                    results = new bool[condition.QuestKillMonster.Length];
                    for (var i = 0; i < results.Length; ++i)
                        results[i] = true;

                    for (var i = 0; i < condition.QuestKillMonster.Length; ++i)
                    {
                        if (condition.QuestKillMonster[i] != 0)
                        {
                            var questObjective = quest.Objectives.Find(objective => objective.Type == QuestObjectiveType.Monster && objective.ObjectID == condition.QuestKillMonster[i]);

                            if (questObjective != null)
                                results[i] = player.GetQuestSlotObjectiveData(questSlot, questObjective) >= questObjective.Amount;
                        }
                    }

                    if (!PlayerConditionLogic(condition.QuestKillLogic, results))
                        return false;
                }
            }

            if (condition.MinAvgItemLevel != 0 && Math.Floor(player.m_playerData.AvgItemLevel[0]) < condition.MinAvgItemLevel)
                return false;

            if (condition.MaxAvgItemLevel != 0 && Math.Floor(player.m_playerData.AvgItemLevel[0]) > condition.MaxAvgItemLevel)
                return false;

            if (condition.MinAvgEquippedItemLevel != 0 && Math.Floor(player.m_playerData.AvgItemLevel[1]) < condition.MinAvgEquippedItemLevel)
                return false;

            if (condition.MaxAvgEquippedItemLevel != 0 && Math.Floor(player.m_playerData.AvgItemLevel[1]) > condition.MaxAvgEquippedItemLevel)
                return false;

            if (condition.ModifierTreeID != 0 && !player.ModifierTreeSatisfied(condition.ModifierTreeID))
                return false;

            if (condition.CovenantID != 0 && player.m_playerData.CovenantID != condition.CovenantID)
                return false;

            if (condition.TraitNodeEntryID.Any(traitNodeEntryId => traitNodeEntryId != 0))
            {
                var getTraitNodeEntryRank = ushort? (int traitNodeEntryId) =>
                {
                    foreach (var traitConfig in player.m_activePlayerData.TraitConfigs)
                    {
                        if ((TraitConfigType)(int)traitConfig.Type == TraitConfigType.Combat)
                        {
                            if (player.m_activePlayerData.ActiveCombatTraitConfigID != traitConfig.ID
                                || !((TraitCombatConfigFlags)(int)traitConfig.CombatConfigFlags).HasFlag(TraitCombatConfigFlags.ActiveForSpec))
                                continue;
                        }

                        foreach (var traitEntry in traitConfig.Entries)
                            if (traitEntry.TraitNodeEntryID == traitNodeEntryId)
                                return (ushort)traitEntry.Rank;
                    }
                    return null;
                };

                results = new bool[condition.TraitNodeEntryID.Length];
                Array.Fill(results, true);
                for (var i = 0; i < condition.TraitNodeEntryID.Count(); ++i)
                {
                    if (condition.TraitNodeEntryID[i] == 0)
                        continue;

                    var rank = getTraitNodeEntryRank(condition.TraitNodeEntryID[i]);
                    if (!rank.HasValue)
                        results[i] = false;
                    else if (condition.TraitNodeEntryMinRank[i] != 0 && rank < condition.TraitNodeEntryMinRank[i])
                        results[i] = false;
                    else if (condition.TraitNodeEntryMaxRank[i] != 0 && rank > condition.TraitNodeEntryMaxRank[i])
                        results[i] = false;
                }

                if (!PlayerConditionLogic(condition.TraitNodeEntryLogic, results))
                    return false;
            }
            return true;
        }

        public static bool IsMeetingWorldStateExpression(Map map, WorldStateExpressionRecord expression)
        {
            ByteBuffer buffer = new(expression.Expression.ToByteArray());
            if (buffer.GetSize() == 0)
                return false;

            bool enabled = buffer.ReadBool();
            if (!enabled)
                return false;

            bool finalResult = EvalRelOp(buffer, map);
            WorldStateExpressionLogic resultLogic = (WorldStateExpressionLogic)buffer.ReadUInt8();

            while (resultLogic != WorldStateExpressionLogic.None)
            {
                bool secondResult = EvalRelOp(buffer, map);

                switch (resultLogic)
                {
                    case WorldStateExpressionLogic.And:
                        finalResult = finalResult && secondResult;
                        break;
                    case WorldStateExpressionLogic.Or:
                        finalResult = finalResult || secondResult;
                        break;
                    case WorldStateExpressionLogic.Xor:
                        finalResult = finalResult != secondResult;
                        break;
                    default:
                        break;
                }

                if (buffer.GetCurrentStream().Position >= buffer.GetSize())
                    break;

                resultLogic = (WorldStateExpressionLogic)buffer.ReadUInt8();
            }

            return finalResult;
        }

        static int GetUnitConditionVariable(Unit unit, Unit otherUnit, UnitConditionVariable variable, int value)
        {
            switch (variable)
            {
                case UnitConditionVariable.Race:
                    return (int)unit.GetRace();
                case UnitConditionVariable.Class:
                    return (int)unit.GetClass();
                case UnitConditionVariable.Level:
                    return (int)unit.GetLevel();
                case UnitConditionVariable.IsSelf:
                    return unit == otherUnit ? 1 : 0;
                case UnitConditionVariable.IsMyPet:
                    return (otherUnit != null && unit.GetCharmerOrOwnerGUID() == otherUnit.GetGUID()) ? 1 : 0;
                case UnitConditionVariable.IsMaster:
                    return (otherUnit != null && otherUnit.GetCharmerOrOwnerGUID() == unit.GetGUID()) ? 1 : 0;
                case UnitConditionVariable.IsTarget:
                    return (otherUnit != null && otherUnit.GetTarget() == unit.GetGUID()) ? 1 : 0;
                case UnitConditionVariable.CanAssist:
                    return (otherUnit != null && unit.IsValidAssistTarget(otherUnit)) ? 1 : 0;
                case UnitConditionVariable.CanAttack:
                    return (otherUnit != null && unit.IsValidAttackTarget(otherUnit)) ? 1 : 0;
                case UnitConditionVariable.HasPet:
                    return (!unit.GetCharmedGUID().IsEmpty() || !unit.GetMinionGUID().IsEmpty()) ? 1 : 0;
                case UnitConditionVariable.HasWeapon:
                    Player player = unit.ToPlayer();
                    if (player != null)
                        return (player.GetWeaponForAttack(WeaponAttackType.BaseAttack) != null || player.GetWeaponForAttack(WeaponAttackType.OffAttack) != null) ? 1 : 0;
                    return (unit.GetVirtualItemId(0) != 0 || unit.GetVirtualItemId(1) != 0) ? 1 : 0;
                case UnitConditionVariable.HealthPct:
                    return (int)unit.GetHealthPct();
                case UnitConditionVariable.ManaPct:
                    return (int)unit.GetPowerPct(PowerType.Mana);
                case UnitConditionVariable.RagePct:
                    return (int)unit.GetPowerPct(PowerType.Rage);
                case UnitConditionVariable.EnergyPct:
                    return (int)unit.GetPowerPct(PowerType.Energy);
                case UnitConditionVariable.ComboPoints:
                    return unit.GetPower(PowerType.ComboPoints);
                case UnitConditionVariable.HasHelpfulAuraSpell:
                    return unit.GetAuraApplication((uint)value, aurApp => !aurApp.GetFlags().HasFlag(AuraFlags.Negative)) != null ? value : 0;
                case UnitConditionVariable.HasHelpfulAuraDispelType:
                    return unit.GetAuraApplication(aurApp => !aurApp.GetFlags().HasFlag(AuraFlags.Negative) && (int)aurApp.GetBase().GetSpellInfo().Dispel == value) != null ? value : 0;
                case UnitConditionVariable.HasHelpfulAuraMechanic:
                    return unit.GetAuraApplication(aurApp => !aurApp.GetFlags().HasFlag(AuraFlags.Negative) && (aurApp.GetBase().GetSpellInfo().GetSpellMechanicMaskByEffectMask(aurApp.GetEffectMask()) & (1ul << value)) != 0) != null ? value : 0;
                case UnitConditionVariable.HasHarmfulAuraSpell:
                    return unit.GetAuraApplication((uint)value, aurApp => aurApp.GetFlags().HasFlag(AuraFlags.Negative)) != null ? value : 0;
                case UnitConditionVariable.HasHarmfulAuraDispelType:
                    return unit.GetAuraApplication(aurApp => aurApp.GetFlags().HasFlag(AuraFlags.Negative) && (int)aurApp.GetBase().GetSpellInfo().Dispel == value) != null ? value : 0;
                case UnitConditionVariable.HasHarmfulAuraMechanic:
                    return unit.GetAuraApplication(aurApp => aurApp.GetFlags().HasFlag(AuraFlags.Negative) && (aurApp.GetBase().GetSpellInfo().GetSpellMechanicMaskByEffectMask(aurApp.GetEffectMask()) & (1ul << value)) != 0) != null ? value : 0;
                case UnitConditionVariable.HasHarmfulAuraSchool:
                    return unit.GetAuraApplication(aurApp => aurApp.GetFlags().HasFlag(AuraFlags.Negative) && ((int)aurApp.GetBase().GetSpellInfo().GetSchoolMask() & (1 << value)) != 0) != null ? value : 0;
                case UnitConditionVariable.DamagePhysicalPct:
                    break;
                case UnitConditionVariable.DamageHolyPct:
                    break;
                case UnitConditionVariable.DamageFirePct:
                    break;
                case UnitConditionVariable.DamageNaturePct:
                    break;
                case UnitConditionVariable.DamageFrostPct:
                    break;
                case UnitConditionVariable.DamageShadowPct:
                    break;
                case UnitConditionVariable.DamageArcanePct:
                    break;
                case UnitConditionVariable.InCombat:
                    return unit.IsInCombat() ? 1 : 0;
                case UnitConditionVariable.IsMoving:
                    return unit.HasUnitMovementFlag(MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight) ? 1 : 0;
                case UnitConditionVariable.IsCasting:
                case UnitConditionVariable.IsCastingSpell: // this is supposed to return spell id by client code but data always has 0 or 1
                    return unit.GetCurrentSpell(CurrentSpellTypes.Generic) != null ? 1 : 0;
                case UnitConditionVariable.IsChanneling:
                case UnitConditionVariable.IsChannelingSpell: // this is supposed to return spell id by client code but data always has 0 or 1
                    return unit.GetChannelSpellId() != 0 ? 1 : 0;
                case UnitConditionVariable.NumberOfMeleeAttackers:
                    return unit.GetAttackers().Count(attacker =>
                    {
                        float distance = Math.Max(unit.GetCombatReach() + attacker.GetCombatReach() + 1.3333334f, 5.0f);
                        if (unit.HasUnitFlag(UnitFlags.PlayerControlled) || attacker.HasUnitFlag(UnitFlags.PlayerControlled))
                            distance += 1.0f;
                        return unit.GetExactDistSq(attacker) < distance * distance;
                    });
                case UnitConditionVariable.IsAttackingMe:
                    return (otherUnit != null && unit.GetTarget() == otherUnit.GetGUID()) ? 1 : 0;
                case UnitConditionVariable.Range:
                    return otherUnit != null ? (int)unit.GetExactDist(otherUnit) : 0;
                case UnitConditionVariable.InMeleeRange:
                    if (otherUnit != null)
                    {
                        float distance = Math.Max(unit.GetCombatReach() + otherUnit.GetCombatReach() + 1.3333334f, 5.0f);
                        if (unit.HasUnitFlag(UnitFlags.PlayerControlled) || otherUnit.HasUnitFlag(UnitFlags.PlayerControlled))
                            distance += 1.0f;
                        return (unit.GetExactDistSq(otherUnit) < distance * distance) ? 1 : 0;
                    }
                    return 0;
                case UnitConditionVariable.PursuitTime:
                    break;
                case UnitConditionVariable.HasHarmfulAuraCanceledByDamage:
                    return unit.HasNegativeAuraWithInterruptFlag(SpellAuraInterruptFlags.Damage) ? 1 : 0;
                case UnitConditionVariable.HasHarmfulAuraWithPeriodicDamage:
                    return unit.HasAuraType(AuraType.PeriodicDamage) ? 1 : 0;
                case UnitConditionVariable.NumberOfEnemies:
                    return unit.GetThreatManager().GetThreatListSize();
                case UnitConditionVariable.NumberOfFriends:
                    break;
                case UnitConditionVariable.ThreatPhysicalPct:
                    break;
                case UnitConditionVariable.ThreatHolyPct:
                    break;
                case UnitConditionVariable.ThreatFirePct:
                    break;
                case UnitConditionVariable.ThreatNaturePct:
                    break;
                case UnitConditionVariable.ThreatFrostPct:
                    break;
                case UnitConditionVariable.ThreatShadowPct:
                    break;
                case UnitConditionVariable.ThreatArcanePct:
                    break;
                case UnitConditionVariable.IsInterruptible:
                    break;
                case UnitConditionVariable.NumberOfAttackers:
                    return unit.GetAttackers().Count;
                case UnitConditionVariable.NumberOfRangedAttackers:
                    return unit.GetAttackers().Count(attacker =>
                    {
                        float distance = Math.Max(unit.GetCombatReach() + attacker.GetCombatReach() + 1.3333334f, 5.0f);
                        if (unit.HasUnitFlag(UnitFlags.PlayerControlled) || attacker.HasUnitFlag(UnitFlags.PlayerControlled))
                            distance += 1.0f;
                        return unit.GetExactDistSq(attacker) >= distance * distance;
                    });
                case UnitConditionVariable.CreatureType:
                    return (int)unit.GetCreatureType();
                case UnitConditionVariable.IsMeleeAttacking:
                {
                    Unit target = Global.ObjAccessor.GetUnit(unit, unit.GetTarget());
                    if (target != null)
                    {
                        float distance = Math.Max(unit.GetCombatReach() + target.GetCombatReach() + 1.3333334f, 5.0f);
                        if (unit.HasUnitFlag(UnitFlags.PlayerControlled) || target.HasUnitFlag(UnitFlags.PlayerControlled))
                            distance += 1.0f;
                        return (unit.GetExactDistSq(target) < distance * distance) ? 1 : 0;
                    }
                    return 0;
                }
                case UnitConditionVariable.IsRangedAttacking:
                {
                    Unit target = Global.ObjAccessor.GetUnit(unit, unit.GetTarget());
                    if (target != null)
                    {
                        float distance = Math.Max(unit.GetCombatReach() + target.GetCombatReach() + 1.3333334f, 5.0f);
                        if (unit.HasUnitFlag(UnitFlags.PlayerControlled) || target.HasUnitFlag(UnitFlags.PlayerControlled))
                            distance += 1.0f;
                        return (unit.GetExactDistSq(target) >= distance * distance) ? 1 : 0;
                    }
                    return 0;
                }
                case UnitConditionVariable.Health:
                    return (int)unit.GetHealth();
                case UnitConditionVariable.SpellKnown:
                    return unit.HasSpell((uint)value) ? value : 0;
                case UnitConditionVariable.HasHarmfulAuraEffect:
                    return (value >= 0 && value < (int)AuraType.Total && unit.GetAuraEffectsByType((AuraType)value).Any(aurEff => aurEff.GetBase().GetApplicationOfTarget(unit.GetGUID()).GetFlags().HasFlag(AuraFlags.Negative))) ? 1 : 0;
                case UnitConditionVariable.IsImmuneToAreaOfEffect:
                    break;
                case UnitConditionVariable.IsPlayer:
                    return unit.IsPlayer() ? 1 : 0;
                case UnitConditionVariable.DamageMagicPct:
                    break;
                case UnitConditionVariable.DamageTotalPct:
                    break;
                case UnitConditionVariable.ThreatMagicPct:
                    break;
                case UnitConditionVariable.ThreatTotalPct:
                    break;
                case UnitConditionVariable.HasCritter:
                    return unit.GetCritterGUID().IsEmpty() ? 0 : 1;
                case UnitConditionVariable.HasTotemInSlot1:
                    return unit.m_SummonSlot[(int)SummonSlot.Totem].IsEmpty() ? 0 : 1;
                case UnitConditionVariable.HasTotemInSlot2:
                    return unit.m_SummonSlot[(int)SummonSlot.Totem2].IsEmpty() ? 0 : 1;
                case UnitConditionVariable.HasTotemInSlot3:
                    return unit.m_SummonSlot[(int)SummonSlot.Totem3].IsEmpty() ? 0 : 1;
                case UnitConditionVariable.HasTotemInSlot4:
                    return unit.m_SummonSlot[(int)SummonSlot.Totem4].IsEmpty() ? 0 : 1;
                case UnitConditionVariable.HasTotemInSlot5:
                    break;
                case UnitConditionVariable.Creature:
                    return (int)unit.GetEntry();
                case UnitConditionVariable.StringID:
                    break;
                case UnitConditionVariable.HasAura:
                    return unit.HasAura((uint)value) ? value : 0;
                case UnitConditionVariable.IsEnemy:
                    return (otherUnit != null && unit.GetReactionTo(otherUnit) <= ReputationRank.Hostile) ? 1 : 0;
                case UnitConditionVariable.IsSpecMelee:
                    return unit.IsPlayer() && unit.ToPlayer().GetPrimarySpecializationEntry() != null && unit.ToPlayer().GetPrimarySpecializationEntry().HasFlag(ChrSpecializationFlag.Melee) ? 1 : 0;
                case UnitConditionVariable.IsSpecTank:
                    return unit.IsPlayer() && unit.ToPlayer().GetPrimarySpecializationEntry() != null && unit.ToPlayer().GetPrimarySpecializationEntry().GetRole() == ChrSpecializationRole.Tank ? 1 : 0;
                case UnitConditionVariable.IsSpecRanged:
                    return unit.IsPlayer() && unit.ToPlayer().GetPrimarySpecializationEntry() != null && unit.ToPlayer().GetPrimarySpecializationEntry().HasFlag(ChrSpecializationFlag.Ranged) ? 1 : 0;
                case UnitConditionVariable.IsSpecHealer:
                    return unit.IsPlayer() && unit.ToPlayer().GetPrimarySpecializationEntry()?.GetRole() == ChrSpecializationRole.Healer ? 1 : 0;
                case UnitConditionVariable.IsPlayerControlledNPC:
                    return unit.IsCreature() && unit.HasUnitFlag(UnitFlags.PlayerControlled) ? 1 : 0;
                case UnitConditionVariable.IsDying:
                    return unit.GetHealth() == 0 ? 1 : 0;
                case UnitConditionVariable.PathFailCount:
                    break;
                case UnitConditionVariable.IsMounted:
                    return unit.GetMountDisplayId() != 0 ? 1 : 0;
                case UnitConditionVariable.Label:
                    return unit.IsCreature() && unit.ToCreature().HasLabel(value) ? value : 0;
                case UnitConditionVariable.IsMySummon:
                    return (otherUnit != null && (otherUnit.GetCharmerGUID() == unit.GetGUID() || otherUnit.GetCreatorGUID() == unit.GetGUID())) ? 1 : 0;
                case UnitConditionVariable.IsSummoner:
                    return (otherUnit != null && (unit.GetCharmerGUID() == otherUnit.GetGUID() || unit.GetCreatorGUID() == otherUnit.GetGUID())) ? 1 : 0;
                case UnitConditionVariable.IsMyTarget:
                    return (otherUnit != null && unit.GetTarget() == otherUnit.GetGUID()) ? 1 : 0;
                case UnitConditionVariable.Sex:
                    return (int)unit.GetGender();
                case UnitConditionVariable.LevelWithinContentTuning:
                    var levelRange = Global.DB2Mgr.GetContentTuningData((uint)value, 0);
                    if (levelRange.HasValue)
                        return unit.GetLevel() >= levelRange.Value.MinLevel && unit.GetLevel() <= levelRange.Value.MaxLevel ? value : 0;
                    return 0;
                case UnitConditionVariable.IsFlying:
                    return unit.IsFlying() ? 1 : 0;
                case UnitConditionVariable.IsHovering:
                    return unit.IsHovering() ? 1 : 0;
                case UnitConditionVariable.HasHelpfulAuraEffect:
                    return (value >= 0 && value < (int)AuraType.Total && unit.GetAuraEffectsByType((AuraType)value).Any(aurEff => !aurEff.GetBase().GetApplicationOfTarget(unit.GetGUID()).GetFlags().HasFlag(AuraFlags.Negative))) ? 1 : 0;
                case UnitConditionVariable.HasHelpfulAuraSchool:
                    return unit.GetAuraApplication(aurApp =>
                    {
                        return !aurApp.GetFlags().HasFlag(AuraFlags.Negative) && ((int)aurApp.GetBase().GetSpellInfo().GetSchoolMask() & (1 << value)) != 0;
                    }) != null ? value : 0;
                default:
                    break;
            }

            return 0;
        }

        public static bool IsUnitMeetingCondition(Unit unit, Unit otherUnit, UnitConditionRecord condition)
        {
            for (int i = 0; i < condition.Variable.Length; ++i)
            {
                if (condition.Variable[i] == 0)
                    break;

                int unitValue = GetUnitConditionVariable(unit, otherUnit, (UnitConditionVariable)condition.Variable[i], condition.Value[i]);
                bool meets = false;
                switch ((UnitConditionOp)condition.Op[i])
                {
                    case UnitConditionOp.EqualTo:
                        meets = unitValue == condition.Value[i];
                        break;
                    case UnitConditionOp.NotEqualTo:
                        meets = unitValue != condition.Value[i];
                        break;
                    case UnitConditionOp.LessThan:
                        meets = unitValue < condition.Value[i];
                        break;
                    case UnitConditionOp.LessThanOrEqualTo:
                        meets = unitValue <= condition.Value[i];
                        break;
                    case UnitConditionOp.GreaterThan:
                        meets = unitValue > condition.Value[i];
                        break;
                    case UnitConditionOp.GreaterThanOrEqualTo:
                        meets = unitValue >= condition.Value[i];
                        break;
                    default:
                        break;
                }

                if (condition.HasFlag(UnitConditionFlags.LogicOr))
                {
                    if (meets)
                        return true;
                }
                else if (!meets)
                    return false;
            }

            return !condition.HasFlag(UnitConditionFlags.LogicOr);
        }

        static int EvalSingleValue(ByteBuffer buffer, Map map)
        {
            WorldStateExpressionValueType valueType = (WorldStateExpressionValueType)buffer.ReadUInt8();
            int value = 0;

            switch (valueType)
            {
                case WorldStateExpressionValueType.Constant:
                {
                    value = buffer.ReadInt32();
                    break;
                }
                case WorldStateExpressionValueType.WorldState:
                {
                    int worldStateId = buffer.ReadInt32();
                    value = Global.WorldStateMgr.GetValue(worldStateId, map);
                    break;
                }
                case WorldStateExpressionValueType.Function:
                {
                    var functionType = (WorldStateExpressionFunctions)buffer.ReadUInt32();
                    int arg1 = EvalSingleValue(buffer, map);
                    int arg2 = EvalSingleValue(buffer, map);

                    if (functionType >= WorldStateExpressionFunctions.Max)
                        return 0;

                    value = WorldStateExpressionFunction(functionType, map, arg1, arg2);
                    break;
                }
                default:
                    break;
            }

            return value;
        }

        static int WorldStateExpressionFunction(WorldStateExpressionFunctions functionType, Map map, int arg1, int arg2)
        {
            switch (functionType)
            {
                case WorldStateExpressionFunctions.Random:
                    return (int)RandomHelper.URand(Math.Min(arg1, arg2), Math.Max(arg1, arg2));
                case WorldStateExpressionFunctions.Month:
                    return GameTime.GetDateAndTime().Month + 1;
                case WorldStateExpressionFunctions.Day:
                    return GameTime.GetDateAndTime().Day + 1;
                case WorldStateExpressionFunctions.TimeOfDay:
                    DateTime localTime = GameTime.GetDateAndTime();
                    return localTime.Hour * Time.Minute + localTime.Minute;
                case WorldStateExpressionFunctions.Region:
                    return Global.RealmMgr.GetCurrentRealmId().Region;
                case WorldStateExpressionFunctions.ClockHour:
                    int currentHour = GameTime.GetDateAndTime().Hour + 1;
                    return currentHour <= 12 ? (currentHour != 0 ? currentHour : 12) : currentHour - 12;
                case WorldStateExpressionFunctions.OldDifficultyId:
                    var difficulty = CliDB.DifficultyStorage.LookupByKey(map.GetDifficultyID());
                    if (difficulty != null)
                        return difficulty.OldEnumValue;

                    return -1;
                case WorldStateExpressionFunctions.HolidayActive:
                    return Global.GameEventMgr.IsHolidayActive((HolidayIds)arg1) ? 1 : 0;
                case WorldStateExpressionFunctions.TimerCurrentTime:
                    return (int)GameTime.GetGameTime();
                case WorldStateExpressionFunctions.WeekNumber:
                    long now = GameTime.GetGameTime();
                    uint raidOrigin = 1135695600;
                    Cfg_RegionsRecord region = CliDB.CfgRegionsStorage.LookupByKey(Global.RealmMgr.GetCurrentRealmId().Region);
                    if (region != null)
                        raidOrigin = region.Raidorigin;

                    return (int)(now - raidOrigin) / Time.Week;
                case WorldStateExpressionFunctions.DifficultyId:
                    return (int)map.GetDifficultyID();
                case WorldStateExpressionFunctions.WarModeActive:
                    // check if current zone/map is bound to war mode
                    return 0;
                case WorldStateExpressionFunctions.WorldStateExpression:
                    var worldStateExpression = CliDB.WorldStateExpressionStorage.LookupByKey(arg1);
                    if (worldStateExpression != null)
                        return IsMeetingWorldStateExpression(map, worldStateExpression) ? 1 : 0;

                    return 0;
                case WorldStateExpressionFunctions.MersenneRandom:
                    if (arg1 == 1)
                        return 1;

                    //todo fix me
                    // init with predetermined seed                      
                    //std::mt19937 mt(arg2? arg2 : 1);
                    //value = mt() % arg1 + 1;
                    return 0;
                case WorldStateExpressionFunctions.None:
                case WorldStateExpressionFunctions.HolidayStart:
                case WorldStateExpressionFunctions.HolidayLeft:
                case WorldStateExpressionFunctions.Unk13:
                case WorldStateExpressionFunctions.Unk14:
                case WorldStateExpressionFunctions.Unk17:
                case WorldStateExpressionFunctions.Unk18:
                case WorldStateExpressionFunctions.Unk19:
                case WorldStateExpressionFunctions.Unk20:
                case WorldStateExpressionFunctions.Unk21:
                case WorldStateExpressionFunctions.KeystoneAffix:
                case WorldStateExpressionFunctions.Unk24:
                case WorldStateExpressionFunctions.Unk25:
                case WorldStateExpressionFunctions.Unk26:
                case WorldStateExpressionFunctions.Unk27:
                case WorldStateExpressionFunctions.KeystoneLevel:
                case WorldStateExpressionFunctions.Unk29:
                case WorldStateExpressionFunctions.Unk30:
                case WorldStateExpressionFunctions.Unk31:
                case WorldStateExpressionFunctions.Unk32:
                case WorldStateExpressionFunctions.Unk34:
                case WorldStateExpressionFunctions.Unk35:
                case WorldStateExpressionFunctions.Unk36:
                case WorldStateExpressionFunctions.UiWidgetData:
                case WorldStateExpressionFunctions.TimeEventPassed:
                default:
                    return 0;
            }
        }

        static int EvalValue(ByteBuffer buffer, Map map)
        {
            int leftValue = EvalSingleValue(buffer, map);

            WorldStateExpressionOperatorType operatorType = (WorldStateExpressionOperatorType)buffer.ReadUInt8();
            if (operatorType == WorldStateExpressionOperatorType.None)
                return leftValue;

            int rightValue = EvalSingleValue(buffer, map);

            switch (operatorType)
            {
                case WorldStateExpressionOperatorType.Sum:
                    return leftValue + rightValue;
                case WorldStateExpressionOperatorType.Substraction:
                    return leftValue - rightValue;
                case WorldStateExpressionOperatorType.Multiplication:
                    return leftValue * rightValue;
                case WorldStateExpressionOperatorType.Division:
                    return rightValue == 0 ? 0 : leftValue / rightValue;
                case WorldStateExpressionOperatorType.Remainder:
                    return rightValue == 0 ? 0 : leftValue % rightValue;
                default:
                    break;
            }

            return leftValue;
        }

        static bool EvalRelOp(ByteBuffer buffer, Map map)
        {
            int leftValue = EvalValue(buffer, map);

            WorldStateExpressionComparisonType compareLogic = (WorldStateExpressionComparisonType)buffer.ReadUInt8();
            if (compareLogic == WorldStateExpressionComparisonType.None)
                return leftValue != 0;

            int rightValue = EvalValue(buffer, map);

            switch (compareLogic)
            {
                case WorldStateExpressionComparisonType.Equal:
                    return leftValue == rightValue;
                case WorldStateExpressionComparisonType.NotEqual:
                    return leftValue != rightValue;
                case WorldStateExpressionComparisonType.Less:
                    return leftValue < rightValue;
                case WorldStateExpressionComparisonType.LessOrEqual:
                    return leftValue <= rightValue;
                case WorldStateExpressionComparisonType.Greater:
                    return leftValue > rightValue;
                case WorldStateExpressionComparisonType.GreaterOrEqual:
                    return leftValue >= rightValue;
                default:
                    break;
            }

            return false;
        }

        Dictionary<ConditionSourceType, Dictionary<ConditionId, List<Condition>>> ConditionStorage = new();
        List<uint> spellsUsedInSpellClickConditions = new();

        public string[] StaticSourceTypeData =
        {
            "None",
            "Creature Loot",
            "Disenchant Loot",
            "Fishing Loot",
            "GameObject Loot",
            "Item Loot",
            "Mail Loot",
            "Milling Loot",
            "Pickpocketing Loot",
            "Prospecting Loot",
            "Reference Loot",
            "Skinning Loot",
            "Spell Loot",
            "Spell Impl. Target",
            "Gossip Menu",
            "Gossip Menu Option",
            "Creature Vehicle",
            "Spell Expl. Target",
            "Spell Click Event",
            "Quest Accept",
            "Quest Show Mark",
            "Vehicle Spell",
            "SmartScript",
            "Npc Vendor",
            "Spell Proc",
            "Terrain Swap",
            "Phase",
            "Graveyard",
            "AreaTrigger",
            "ConversationLine",
            "AreaTrigger Client Triggered",
            "Trainer Spell",
            "Object Visibility (by ID)",
            "Spawn Group",
            "Player Condition"
        };

        public ConditionTypeInfo[] StaticConditionTypeData =
        {
            new ConditionTypeInfo("None",                 false, false, false, false),
            new ConditionTypeInfo("Aura",                 true, true,  true, false),
            new ConditionTypeInfo("Item Stored",          true, true,  true, false),
            new ConditionTypeInfo("Item Equipped",        true, false, false, false),
            new ConditionTypeInfo("Zone",                 true, false, false, false),
            new ConditionTypeInfo("Reputation",           true, true,  false, false),
            new ConditionTypeInfo("Team",                 true, false, false, false),
            new ConditionTypeInfo("Skill",                true, true,  false, false),
            new ConditionTypeInfo("Quest Rewarded",       true, false, false, false),
            new ConditionTypeInfo("Quest Taken",          true, false, false, false),
            new ConditionTypeInfo("Drunken",              true, false, false, false),
            new ConditionTypeInfo("WorldState",           true, true,  false, false),
            new ConditionTypeInfo("Active Event",         true, false, false, false),
            new ConditionTypeInfo("Instance Info",        true, true,  true, false),
            new ConditionTypeInfo("Quest None",           true, false, false, false),
            new ConditionTypeInfo("Class",                true, false, false, false),
            new ConditionTypeInfo("Race",                 true, false, false, false),
            new ConditionTypeInfo("Achievement",          true, false, false, false),
            new ConditionTypeInfo("Title",                true, false, false, false),
            new ConditionTypeInfo("SpawnMask",            true, false, false, false),
            new ConditionTypeInfo("Gender",               true, false, false, false),
            new ConditionTypeInfo("Unit State",           true, false, false, false),
            new ConditionTypeInfo("Map",                  true, false, false, false),
            new ConditionTypeInfo("Area",                 true, false, false, false),
            new ConditionTypeInfo("CreatureType",         true, false, false, false),
            new ConditionTypeInfo("Spell Known",          true, false, false, false),
            new ConditionTypeInfo("Phase",                true, false, false, false),
            new ConditionTypeInfo("Level",                true, true,  false, false),
            new ConditionTypeInfo("Quest Completed",      true, false, false, false),
            new ConditionTypeInfo("Near Creature",        true, true,  true, false),
            new ConditionTypeInfo("Near GameObject",      true, true,  false, false),
            new ConditionTypeInfo("Object Entry or Guid", true, true,  true, false),
            new ConditionTypeInfo("Object TypeMask",      true, false, false, false),
            new ConditionTypeInfo("Relation",             true, true,  false, false),
            new ConditionTypeInfo("Reaction",             true, true,  false, false),
            new ConditionTypeInfo("Distance",             true, true,  true, false),
            new ConditionTypeInfo("Alive",                false, false, false, false),
            new ConditionTypeInfo("Health Value",         true, true,  false, false),
            new ConditionTypeInfo("Health Pct",           true, true,  false, false),
            new ConditionTypeInfo("Realm Achievement",    true, false, false, false),
            new ConditionTypeInfo("In Water",             false, false, false, false),
            new ConditionTypeInfo("Terrain Swap",         true, false, false, false),
            new ConditionTypeInfo("Sit/stand state",      true, true, false, false),
            new ConditionTypeInfo("Daily Quest Completed",true, false, false, false),
            new ConditionTypeInfo("Charmed",              false, false, false, false),
            new ConditionTypeInfo("Pet type",             true, false, false, false),
            new ConditionTypeInfo("On Taxi",              false, false, false, false),
            new ConditionTypeInfo("Quest state mask",     true, true, false, false),
            new ConditionTypeInfo("Quest objective progress",   true, false, true, false),
            new ConditionTypeInfo("Map Difficulty",       true, false, false, false),
            new ConditionTypeInfo("Is Gamemaster",        true, false, false, false),
            new ConditionTypeInfo("Object Entry or Guid", true, true,  true, false),
            new ConditionTypeInfo("Object TypeMask",      true, false, false, false),
            new ConditionTypeInfo("BattlePet Species Learned", true, true,  true, false),
            new ConditionTypeInfo("On Scenario Step",     true, false, false, false),
            new ConditionTypeInfo("Scene In Progress",    true, false, false, false),
            new ConditionTypeInfo("Player Condition",     true, false, false, false),
            new ConditionTypeInfo("Private Object",       false,false, false, false),
            new ConditionTypeInfo("String ID",            true, false, false, true),
            new ConditionTypeInfo("Label",                true, false, false, false)
        };

        public struct ConditionTypeInfo
        {
            public ConditionTypeInfo(string name, params bool[] args)
            {
                Name = name;
                HasConditionValue1 = args[0];
                HasConditionValue2 = args[1];
                HasConditionValue3 = args[2];
            }

            public string Name;
            public bool HasConditionValue1;
            public bool HasConditionValue2;
            public bool HasConditionValue3;
            public bool HasConditionStringValue1;
        }
    }

    public struct ConditionsReference
    {
        public List<Condition> Conditions = new();

        public ConditionsReference(List<Condition> conditions)
        {
            Conditions = conditions;
        }

        public bool Meets(WorldObject obj)
        {
            return Global.ConditionMgr.IsObjectMeetToConditions(obj, Conditions);
        }

        public bool Meets(WorldObject obj1, WorldObject obj2)
        {
            return Global.ConditionMgr.IsObjectMeetToConditions(obj1, obj2, Conditions);
        }

        public bool Meets(ConditionSourceInfo sourceInfo)
        {
            return Global.ConditionMgr.IsObjectMeetToConditions(sourceInfo, Conditions);
        }

        public bool IsEmpty()
        {
            if (Conditions == null)
                return true;

            return Conditions.Empty();
        }
    }
}
