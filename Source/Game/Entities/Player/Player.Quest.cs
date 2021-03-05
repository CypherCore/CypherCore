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
using Game.Conditions;
using Game.DataStorage;
using Game.Groups;
using Game.Mails;
using Game.Maps;
using Game.Misc;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Entities
{
    public partial class Player
    {
        public uint GetSharedQuestID() { return m_sharedQuestId; }
        public ObjectGuid GetPlayerSharingQuest() { return m_playerSharingQuest; }
        public void SetQuestSharingInfo(ObjectGuid guid, uint id) { m_playerSharingQuest = guid; m_sharedQuestId = id; }
        public void ClearQuestSharingInfo() { m_playerSharingQuest = ObjectGuid.Empty; m_sharedQuestId = 0; }

        private uint GetInGameTime() { return m_ingametime; }
        public void SetInGameTime(uint time) { m_ingametime = time; }

        private void AddTimedQuest(uint questId) { m_timedquests.Add(questId); }
        public void RemoveTimedQuest(uint questId) { m_timedquests.Remove(questId); }

        public List<uint> GetRewardedQuests() { return m_RewardedQuests; }
        private Dictionary<uint, QuestStatusData> GetQuestStatusMap() { return m_QuestStatus; }

        public int GetQuestMinLevel(Quest quest)
        {
            var questLevels = Global.DB2Mgr.GetContentTuningData(quest.ContentTuningId, m_playerData.CtrOptions.GetValue().ContentTuningConditionMask);
            if (questLevels.HasValue)
            {
                var race = CliDB.ChrRacesStorage.LookupByKey(GetRace());
                var raceFaction = CliDB.FactionTemplateStorage.LookupByKey(race.FactionID);
                var questFactionGroup = CliDB.ContentTuningStorage.LookupByKey(quest.ContentTuningId).GetScalingFactionGroup();
                if (questFactionGroup != 0 && raceFaction.FactionGroup != questFactionGroup)
                    return questLevels.Value.MaxLevel;

                return questLevels.Value.MinLevelWithDelta;
            }

            return 0;
        }

        public int GetQuestLevel(Quest quest)
        {
            if (quest == null)
                return 0;

            var questLevels = Global.DB2Mgr.GetContentTuningData(quest.ContentTuningId, m_playerData.CtrOptions.GetValue().ContentTuningConditionMask);
            if (questLevels.HasValue)
            {
                var minLevel = GetQuestMinLevel(quest);
                int maxLevel = questLevels.Value.MaxLevel;
                var level = (int)GetLevel();
                if (level >= minLevel)
                    return Math.Min(level, maxLevel);
                return minLevel;
            }

            return 0;
        }

        public int GetRewardedQuestCount() { return m_RewardedQuests.Count; }

        public void LearnQuestRewardedSpells(Quest quest)
        {
            //wtf why is rewardspell a uint if it can me -1
            var spell_id = Convert.ToInt32(quest.RewardSpell);
            var src_spell_id = quest.SourceSpellID;

            // skip quests without rewarded spell
            if (spell_id == 0)
                return;

            // if RewSpellCast = -1 we remove aura do to SrcSpell from player.
            if (spell_id == -1 && src_spell_id != 0)
            {
                RemoveAurasDueToSpell(src_spell_id);
                return;
            }

            var spellInfo = Global.SpellMgr.GetSpellInfo((uint)spell_id, Difficulty.None);
            if (spellInfo == null)
                return;

            // check learned spells state
            var found = false;
            foreach (var eff in spellInfo.GetEffects())
            {
                if (eff != null && eff.Effect == SpellEffectName.LearnSpell && !HasSpell(eff.TriggerSpell))
                {
                    found = true;
                    break;
                }
            }

            // skip quests with not teaching spell or already known spell
            if (!found)
                return;

            var effect = spellInfo.GetEffect(0);
            if (effect == null)
                return;

            var learned_0 = effect.TriggerSpell;
            if (!HasSpell(learned_0))
            {
                found = false;
                var skills = Global.SpellMgr.GetSkillLineAbilityMapBounds(learned_0);
                foreach (var skillLine in skills)
                {
                    if (skillLine.AcquireMethod == AbilityLearnType.RewardedFromQuest)
                    {
                        found = true;
                        break;
                    }
                }

                // profession specialization can be re-learned from npc
                if (!found)
                    return;
            }

            CastSpell(this, (uint)spell_id, true);
        }

        public void LearnQuestRewardedSpells()
        {
            // learn spells received from quest completing
            foreach (var questId in m_RewardedQuests)
            {
                var quest = Global.ObjectMgr.GetQuestTemplate(questId);
                if (quest == null)
                    continue;

                LearnQuestRewardedSpells(quest);
            }
        }

        public void DailyReset()
        {
            foreach (var questId in m_activePlayerData.DailyQuestsCompleted)
            {
                var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questId);
                if (questBit != 0)
                    SetQuestCompletedBit(questBit, false);
            }

            var dailyQuestsReset = new DailyQuestsReset();
            dailyQuestsReset.Count = m_activePlayerData.DailyQuestsCompleted.Size();
            SendPacket(dailyQuestsReset);

            ClearDynamicUpdateFieldValues(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.DailyQuestsCompleted));

            m_DFQuests.Clear(); // Dungeon Finder Quests.

            // DB data deleted in caller
            m_DailyQuestChanged = false;
            m_lastDailyQuestTime = 0;

            if (_garrison != null)
                _garrison.ResetFollowerActivationLimit();
        }

        public void ResetWeeklyQuestStatus()
        {
            if (m_weeklyquests.Empty())
                return;

            foreach (var questId in m_weeklyquests)
            {
                var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questId);
                if (questBit != 0)
                    SetQuestCompletedBit(questBit, false);
            }

            m_weeklyquests.Clear();
            // DB data deleted in caller
            m_WeeklyQuestChanged = false;

        }

        public void ResetSeasonalQuestStatus(ushort event_id)
        {
            var eventList = m_seasonalquests.LookupByKey(event_id);
            if (eventList.Empty())
                return;

            foreach (var questId in eventList)
            {
                var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questId);
                if (questBit != 0)
                    SetQuestCompletedBit(questBit, false);
            }

            m_seasonalquests.Remove(event_id);
            // DB data deleted in caller
            m_SeasonalQuestChanged = false;
        }

        public void ResetMonthlyQuestStatus()
        {
            if (m_monthlyquests.Empty())
                return;

            foreach (var questId in m_monthlyquests)
            {
                var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questId);
                if (questBit != 0)
                    SetQuestCompletedBit(questBit, false);
            }

            m_monthlyquests.Clear();
            // DB data deleted in caller
            m_MonthlyQuestChanged = false;
        }

        public bool CanInteractWithQuestGiver(WorldObject questGiver)
        {
            switch (questGiver.GetTypeId())
            {
                case TypeId.Unit:
                    return GetNPCIfCanInteractWith(questGiver.GetGUID(), NPCFlags.QuestGiver, NPCFlags2.None) != null;
                case TypeId.GameObject:
                    return GetGameObjectIfCanInteractWith(questGiver.GetGUID(), GameObjectTypes.QuestGiver) != null;
                case TypeId.Player:
                    return IsAlive() && questGiver.ToPlayer().IsAlive();
                case TypeId.Item:
                    return IsAlive();
                default:
                    break;
            }
            return false;
        }

        public bool IsQuestRewarded(uint quest_id)
        {
            return m_RewardedQuests.Contains(quest_id);
        }

        public void PrepareQuestMenu(ObjectGuid guid)
        {
            List<uint> objectQR;
            List<uint> objectQIR;

            // pets also can have quests
            var creature = ObjectAccessor.GetCreatureOrPetOrVehicle(this, guid);
            if (creature != null)
            {
                objectQR = Global.ObjectMgr.GetCreatureQuestRelationBounds(creature.GetEntry());
                objectQIR = Global.ObjectMgr.GetCreatureQuestInvolvedRelationBounds(creature.GetEntry());
            }
            else
            {
                //we should obtain map from GetMap() in 99% of cases. Special case
                //only for quests which cast teleport spells on player
                var _map = IsInWorld ? GetMap() : Global.MapMgr.FindMap(GetMapId(), GetInstanceId());
                Cypher.Assert(_map != null);
                var gameObject = _map.GetGameObject(guid);
                if (gameObject != null)
                {
                    objectQR = Global.ObjectMgr.GetGOQuestRelationBounds(gameObject.GetEntry());
                    objectQIR = Global.ObjectMgr.GetGOQuestInvolvedRelationBounds(gameObject.GetEntry());
                }
                else
                    return;
            }

            var qm = PlayerTalkClass.GetQuestMenu();
            qm.ClearMenu();

            foreach (var quest_id in objectQIR)
            {
                var status = GetQuestStatus(quest_id);
                if (status == QuestStatus.Complete)
                    qm.AddMenuItem(quest_id, 4);
                else if (status == QuestStatus.Incomplete)
                    qm.AddMenuItem(quest_id, 2);
            }

            foreach (var quest_id in objectQR)
            {
                var quest = Global.ObjectMgr.GetQuestTemplate(quest_id);
                if (quest == null)
                    continue;

                if (!CanTakeQuest(quest, false))
                    continue;

                if (quest.IsAutoComplete())
                    qm.AddMenuItem(quest_id, 4);
                else if (GetQuestStatus(quest_id) == QuestStatus.None)
                    qm.AddMenuItem(quest_id, 2);
            }
        }

        public void SendPreparedQuest(WorldObject source)
        {
            var questMenu = PlayerTalkClass.GetQuestMenu();
            if (questMenu.IsEmpty())
                return;

            // single element case
            if (questMenu.GetMenuItemCount() == 1)
            {
                var qmi0 = questMenu.GetItem(0);
                var questId = qmi0.QuestId;

                // Auto open
                var quest = Global.ObjectMgr.GetQuestTemplate(questId);
                if (quest != null)
                {
                    if (qmi0.QuestIcon == 4)
                    {
                        PlayerTalkClass.SendQuestGiverRequestItems(quest, source.GetGUID(), CanRewardQuest(quest, false), true);
                        return;
                    }
                    // Send completable on repeatable and autoCompletable quest if player don't have quest
                    // @todo verify if check for !quest.IsDaily() is really correct (possibly not)
                    else
                    {
                        if (!source.HasQuest(questId) && !source.HasInvolvedQuest(questId))
                        {
                            PlayerTalkClass.SendCloseGossip();
                            return;
                        }

                        if (!source.IsTypeId(TypeId.Unit) || source.ToUnit().HasNpcFlag(NPCFlags.Gossip))
                        {
                            if (quest.IsAutoAccept() && CanAddQuest(quest, true) && CanTakeQuest(quest, true))
                                AddQuestAndCheckCompletion(quest, source);

                            if (quest.IsAutoComplete() && quest.IsRepeatable() && !quest.IsDailyOrWeekly())
                                PlayerTalkClass.SendQuestGiverRequestItems(quest, source.GetGUID(), CanCompleteRepeatableQuest(quest), true);
                            else
                                PlayerTalkClass.SendQuestGiverQuestDetails(quest, source.GetGUID(), true, false);
                            return;
                        }
                    }
                }
            }

            PlayerTalkClass.SendQuestGiverQuestListMessage(source);
        }

        public bool IsActiveQuest(uint quest_id)
        {
            return m_QuestStatus.ContainsKey(quest_id);
        }

        public Quest GetNextQuest(ObjectGuid guid, Quest quest)
        {
            List<uint> objectQR;
            var nextQuestID = quest.NextQuestInChain;

            switch (guid.GetHigh())
            {
                case HighGuid.Player:
                    Cypher.Assert(quest.HasFlag(QuestFlags.AutoComplete));
                    return Global.ObjectMgr.GetQuestTemplate(nextQuestID);
                case HighGuid.Creature:
                case HighGuid.Pet:
                case HighGuid.Vehicle:
                    {
                        var creature = ObjectAccessor.GetCreatureOrPetOrVehicle(this, guid);
                        if (creature != null)
                            objectQR = Global.ObjectMgr.GetCreatureQuestRelationBounds(creature.GetEntry());
                        else
                            return null;
                        break;
                    }
                case HighGuid.GameObject:
                    {
                        //we should obtain map from GetMap() in 99% of cases. Special case
                        //only for quests which cast teleport spells on player
                        var _map = IsInWorld ? GetMap() : Global.MapMgr.FindMap(GetMapId(), GetInstanceId());
                        Cypher.Assert(_map != null);
                        var gameObject = _map.GetGameObject(guid);
                        if (gameObject != null)
                            objectQR = Global.ObjectMgr.GetGOQuestRelationBounds(gameObject.GetEntry());
                        else
                            return null;
                        break;
                    }
                default:
                    return null;
            }

            // for unit and go state
            foreach (var id in objectQR)
            {
                if (id == nextQuestID)
                    return Global.ObjectMgr.GetQuestTemplate(nextQuestID);
            }

            return null;
        }

        public bool CanSeeStartQuest(Quest quest)
        {
            if (!Global.DisableMgr.IsDisabledFor(DisableType.Quest, quest.Id, this) && SatisfyQuestClass(quest, false) && SatisfyQuestRace(quest, false) &&
                SatisfyQuestSkill(quest, false) && SatisfyQuestExclusiveGroup(quest, false) && SatisfyQuestReputation(quest, false) &&
                SatisfyQuestDependentQuests(quest, false) && SatisfyQuestDay(quest, false) && SatisfyQuestWeek(quest, false) &&
                SatisfyQuestMonth(quest, false) && SatisfyQuestSeasonal(quest, false))
            {
                return GetLevel() + WorldConfig.GetIntValue(WorldCfg.QuestHighLevelHideDiff) >= GetQuestMinLevel(quest);
            }

            return false;
        }

        public bool CanTakeQuest(Quest quest, bool msg)
        {
            return !Global.DisableMgr.IsDisabledFor(DisableType.Quest, quest.Id, this)
                && SatisfyQuestStatus(quest, msg) && SatisfyQuestExclusiveGroup(quest, msg)
                && SatisfyQuestClass(quest, msg) && SatisfyQuestRace(quest, msg) && SatisfyQuestLevel(quest, msg)
                && SatisfyQuestSkill(quest, msg) && SatisfyQuestReputation(quest, msg)
                && SatisfyQuestDependentQuests(quest, msg) && SatisfyQuestTimed(quest, msg)
                && SatisfyQuestDay(quest, msg)
                && SatisfyQuestWeek(quest, msg) && SatisfyQuestMonth(quest, msg)
                && SatisfyQuestSeasonal(quest, msg) && SatisfyQuestConditions(quest, msg);
        }

        public bool CanAddQuest(Quest quest, bool msg)
        {
            if (!SatisfyQuestLog(msg))
                return false;

            var srcitem = quest.SourceItemId;
            if (srcitem > 0)
            {
                var count = quest.SourceItemIdCount;
                var dest = new List<ItemPosCount>();
                InventoryResult msg2 = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, srcitem, count);

                // player already have max number (in most case 1) source item, no additional item needed and quest can be added.
                if (msg2 == InventoryResult.ItemMaxCount)
                    return true;

                if (msg2 != InventoryResult.Ok)
                {
                    SendEquipError(msg2, null, null, srcitem);
                    return false;
                }
            }
            return true;
        }

        public bool CanCompleteQuest(uint quest_id)
        {
            if (quest_id != 0)
            {
                var qInfo = Global.ObjectMgr.GetQuestTemplate(quest_id);
                if (qInfo == null)
                    return false;

                if (!qInfo.IsRepeatable() && GetQuestRewardStatus(quest_id))
                    return false;                                   // not allow re-complete quest

                // auto complete quest
                if ((qInfo.IsAutoComplete() || qInfo.Flags.HasAnyFlag(QuestFlags.AutoComplete)) && CanTakeQuest(qInfo, false))
                    return true;

                var q_status = m_QuestStatus.LookupByKey(quest_id);
                if (q_status == null)
                    return false;

                if (q_status.Status == QuestStatus.Incomplete)
                {
                    foreach (var obj in qInfo.Objectives)
                    {
                        if (!obj.Flags.HasAnyFlag(QuestObjectiveFlags.Optional) && !obj.Flags.HasAnyFlag(QuestObjectiveFlags.PartOfProgressBar))
                        {
                            if (!IsQuestObjectiveComplete(obj))
                                return false;
                        }
                    }

                    if (qInfo.HasSpecialFlag(QuestSpecialFlags.Timed) && q_status.Timer == 0)
                        return false;

                    return true;
                }
            }
            return false;
        }

        public bool CanCompleteRepeatableQuest(Quest quest)
        {
            // Solve problem that player don't have the quest and try complete it.
            // if repeatable she must be able to complete event if player don't have it.
            // Seem that all repeatable quest are DELIVER Flag so, no need to add more.
            if (!CanTakeQuest(quest, false))
                return false;

            if (quest.HasSpecialFlag(QuestSpecialFlags.Deliver))
                foreach (var obj in quest.Objectives)
                    if (obj.Type == QuestObjectiveType.Item && !HasItemCount((uint)obj.ObjectID, (uint)obj.Amount))
                        return false;

            if (!CanRewardQuest(quest, false))
                return false;

            return true;
        }

        public bool CanRewardQuest(Quest quest, bool msg)
        {
            // not auto complete quest and not completed quest (only cheating case, then ignore without message)
            if (!quest.IsDFQuest() && !quest.IsAutoComplete() && GetQuestStatus(quest.Id) != QuestStatus.Complete)
                return false;

            // daily quest can't be rewarded (25 daily quest already completed)
            if (!SatisfyQuestDay(quest, true) || !SatisfyQuestWeek(quest, true) || !SatisfyQuestMonth(quest, true) || !SatisfyQuestSeasonal(quest, true))
                return false;

            // rewarded and not repeatable quest (only cheating case, then ignore without message)
            if (GetQuestRewardStatus(quest.Id))
                return false;

            // prevent receive reward with quest items in bank
            if (quest.HasSpecialFlag(QuestSpecialFlags.Deliver))
            {
                foreach (var obj in quest.Objectives)
                {
                    if (obj.Type != QuestObjectiveType.Item)
                        continue;

                    if (GetItemCount((uint)obj.ObjectID) < obj.Amount)
                    {
                        if (msg)
                            SendEquipError(InventoryResult.ItemNotFound, null, null, (uint)obj.ObjectID);
                        return false;
                    }
                }
            }

            foreach (var obj in quest.Objectives)
            {
                switch (obj.Type)
                {
                    case QuestObjectiveType.Currency:
                        if (!HasCurrency((uint)obj.ObjectID, (uint)obj.Amount))
                            return false;
                        break;
                    case QuestObjectiveType.Money:
                        if (!HasEnoughMoney(obj.Amount))
                            return false;
                        break;
                }
            }

            return true;
        }

        public bool CanRewardQuest(Quest quest, LootItemType rewardType, uint rewardId, bool msg)
        {
            // prevent receive reward with quest items in bank or for not completed quest
            if (!CanRewardQuest(quest, msg))
                return false;

            var dest = new List<ItemPosCount>();
            if (quest.GetRewChoiceItemsCount() > 0)
            {
                switch (rewardType)
                {
                    case LootItemType.Item:
                        for (uint i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
                        {
                            if (quest.RewardChoiceItemId[i] != 0 && quest.RewardChoiceItemType[i] == LootItemType.Item && quest.RewardChoiceItemId[i] == rewardId)
                            {
                                InventoryResult res = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, quest.RewardChoiceItemId[i], quest.RewardChoiceItemCount[i]);
                                if (res != InventoryResult.Ok)
                                {
                                    if (msg)
                                        SendQuestFailed(quest.Id, res);

                                    return false;
                                }
                            }
                        }
                        break;
                    case LootItemType.Currency:
                        break;
                }
            }

            if (quest.GetRewItemsCount() > 0)
            {
                for (uint i = 0; i < quest.GetRewItemsCount(); ++i)
                {
                    if (quest.RewardItemId[i] != 0)
                    {
                        InventoryResult res = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, quest.RewardItemId[i], quest.RewardItemCount[i]);
                        if (res != InventoryResult.Ok)
                        {
                            if (msg)
                                SendQuestFailed(quest.Id, res);

                            return false;
                        }
                    }
                }
            }

            // QuestPackageItem.db2
            if (quest.PackageID != 0)
            {
                var hasFilteredQuestPackageReward = false;
                var questPackageItems = Global.DB2Mgr.GetQuestPackageItems(quest.PackageID);
                if (questPackageItems != null)
                {
                    foreach (var questPackageItem in questPackageItems)
                    {
                        if (questPackageItem.ItemID != rewardId)
                            continue;

                        if (CanSelectQuestPackageItem(questPackageItem))
                        {
                            hasFilteredQuestPackageReward = true;
                            InventoryResult res = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, questPackageItem.ItemID, questPackageItem.ItemQuantity);
                            if (res != InventoryResult.Ok)
                            {
                                SendEquipError(res, null, null, questPackageItem.ItemID);
                                return false;
                            }
                        }
                    }
                }

                if (!hasFilteredQuestPackageReward)
                {
                    var questPackageItems1 = Global.DB2Mgr.GetQuestPackageItemsFallback(quest.PackageID);
                    if (questPackageItems1 != null)
                    {
                        foreach (var questPackageItem in questPackageItems1)
                        {
                            if (questPackageItem.ItemID != rewardId)
                                continue;

                            InventoryResult res = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, questPackageItem.ItemID, questPackageItem.ItemQuantity);
                            if (res != InventoryResult.Ok)
                            {
                                SendEquipError(res, null, null, questPackageItem.ItemID);
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        public void AddQuestAndCheckCompletion(Quest quest, WorldObject questGiver)
        {
            AddQuest(quest, questGiver);

            foreach (var obj in quest.Objectives)
                if (obj.Type == QuestObjectiveType.CriteriaTree)
                    if (m_questObjectiveCriteriaMgr.HasCompletedObjective(obj))
                        KillCreditCriteriaTreeObjective(obj);

            if (CanCompleteQuest(quest.Id))
                CompleteQuest(quest.Id);

            if (!questGiver)
                return;

            switch (questGiver.GetTypeId())
            {
                case TypeId.Unit:
                    PlayerTalkClass.ClearMenus();
                    questGiver.ToCreature().GetAI().QuestAccept(this, quest);
                    break;
                case TypeId.Item:
                case TypeId.Container:
                case TypeId.AzeriteItem:
                case TypeId.AzeriteEmpoweredItem:
                    {
                        var item = (Item)questGiver;
                        Global.ScriptMgr.OnQuestAccept(this, item, quest);

                        // destroy not required for quest finish quest starting item
                        var destroyItem = true;
                        foreach (var obj in quest.Objectives)
                        {
                            if (obj.Type == QuestObjectiveType.Item && obj.ObjectID == item.GetEntry() && item.GetTemplate().GetMaxCount() > 0)
                            {
                                destroyItem = false;
                                break;
                            }
                        }

                        if (destroyItem)
                            DestroyItem(item.GetBagSlot(), item.GetSlot(), true);

                        break;
                    }
                case TypeId.GameObject:
                    PlayerTalkClass.ClearMenus();
                    questGiver.ToGameObject().GetAI().QuestAccept(this, quest);
                    break;
                default:
                    break;
            }
        }

        public void AddQuest(Quest quest, WorldObject questGiver)
        {
            var log_slot = FindQuestSlot(0);

            if (log_slot >= SharedConst.MaxQuestLogSize) // Player does not have any free slot in the quest log
                return;

            var quest_id = quest.Id;

            if (!m_QuestStatus.ContainsKey(quest_id))
                m_QuestStatus[quest_id] = new QuestStatusData();

            var questStatusData = m_QuestStatus.LookupByKey(quest_id);
            var oldStatus = questStatusData.Status;

            // check for repeatable quests status reset
            questStatusData.Status = QuestStatus.Incomplete;

            var maxStorageIndex = 0;
            foreach (var obj in quest.Objectives)
                if (obj.StorageIndex > maxStorageIndex)
                    maxStorageIndex = obj.StorageIndex;

            questStatusData.ObjectiveData = new int[maxStorageIndex + 1];

            GiveQuestSourceItem(quest);

            foreach (var obj in quest.Objectives)
            {
                switch (obj.Type)
                {
                    case QuestObjectiveType.MinReputation:
                    case QuestObjectiveType.MaxReputation:
                        var factionEntry = CliDB.FactionStorage.LookupByKey(obj.ObjectID);
                        if (factionEntry != null)
                            GetReputationMgr().SetVisible(factionEntry);
                        break;
                    case QuestObjectiveType.CriteriaTree:
                        m_questObjectiveCriteriaMgr.ResetCriteriaTree((uint)obj.ObjectID);
                        break;
                    default:
                        break;
                }
            }

            uint qtime = 0;
            if (quest.HasSpecialFlag(QuestSpecialFlags.Timed))
            {
                var limittime = quest.LimitTime;

                // shared timed quest
                if (questGiver != null && questGiver.IsTypeId(TypeId.Player))
                    limittime = questGiver.ToPlayer().m_QuestStatus[quest_id].Timer / Time.InMilliseconds;

                AddTimedQuest(quest_id);
                questStatusData.Timer = limittime * Time.InMilliseconds;
                qtime = (uint)(Time.UnixTime + limittime);
            }
            else
                questStatusData.Timer = 0;

            if (quest.HasFlag(QuestFlags.Pvp))
            {
                pvpInfo.IsHostile = true;
                UpdatePvPState();
            }

            if (quest.SourceSpellID > 0)
            {
                var spellInfo = Global.SpellMgr.GetSpellInfo(quest.SourceSpellID, GetMap().GetDifficultyID());
                Unit caster = this;
                if (questGiver != null && questGiver.IsTypeMask(TypeMask.Unit) && !quest.HasFlag(QuestFlags.PlayerCastOnAccept) && !spellInfo.HasTargetType(Targets.UnitCaster) && !spellInfo.HasTargetType(Targets.DestCasterSummon))
                {
                    var unit = questGiver.ToUnit();
                    if (unit != null)
                        caster = unit;
                }

                caster.CastSpell(this, spellInfo, true);
            }

            SetQuestSlot(log_slot, quest_id, qtime);

            AdjustQuestReqItemCount(quest);

            m_QuestStatusSave[quest_id] = QuestSaveType.Default;

            StartCriteriaTimer(CriteriaTimedTypes.Quest, quest_id);

            SendQuestUpdate(quest_id);

            Global.ScriptMgr.OnQuestStatusChange(this, quest_id);
            Global.ScriptMgr.OnQuestStatusChange(this, quest, oldStatus, questStatusData.Status);
        }

        public void CompleteQuest(uint quest_id)
        {
            if (quest_id != 0)
            {
                SetQuestStatus(quest_id, QuestStatus.Complete);

                var log_slot = FindQuestSlot(quest_id);
                if (log_slot < SharedConst.MaxQuestLogSize)
                    SetQuestSlotState(log_slot, QuestSlotStateMask.Complete);

                var qInfo = Global.ObjectMgr.GetQuestTemplate(quest_id);
                if (qInfo != null)
                    if (qInfo.HasFlag(QuestFlags.Tracking))
                        RewardQuest(qInfo, LootItemType.Item, 0, this, false);
            }
        }

        public void IncompleteQuest(uint quest_id)
        {
            if (quest_id != 0)
            {
                SetQuestStatus(quest_id, QuestStatus.Incomplete);

                var log_slot = FindQuestSlot(quest_id);
                if (log_slot < SharedConst.MaxQuestLogSize)
                    RemoveQuestSlotState(log_slot, QuestSlotStateMask.Complete);
            }
        }

        public uint GetQuestMoneyReward(Quest quest)
        {
            return (uint)(quest.MoneyValue(this) * WorldConfig.GetFloatValue(WorldCfg.RateMoneyQuest));
        }

        public uint GetQuestXPReward(Quest quest)
        {
            var rewarded = IsQuestRewarded(quest.Id) && !quest.IsDFQuest();

            // Not give XP in case already completed once repeatable quest
            if (rewarded)
                return 0;

            var XP = (uint)(quest.XPValue(this) * WorldConfig.GetFloatValue(WorldCfg.RateXpQuest));

            // handle SPELL_AURA_MOD_XP_QUEST_PCT auras
            var ModXPPctAuras = GetAuraEffectsByType(AuraType.ModXpQuestPct);
            foreach (var eff in ModXPPctAuras)
                MathFunctions.AddPct(ref XP, eff.GetAmount());

            return XP;
        }

        public bool CanSelectQuestPackageItem(QuestPackageItemRecord questPackageItem)
        {
            var rewardProto = Global.ObjectMgr.GetItemTemplate(questPackageItem.ItemID);
            if (rewardProto == null)
                return false;

            if ((rewardProto.GetFlags2().HasAnyFlag(ItemFlags2.FactionAlliance) && GetTeam() != Team.Alliance) ||
                (rewardProto.GetFlags2().HasAnyFlag(ItemFlags2.FactionHorde) && GetTeam() != Team.Horde))
                return false;

            switch (questPackageItem.DisplayType)
            {
                case QuestPackageFilter.LootSpecialization:
                    return rewardProto.IsUsableByLootSpecialization(this, true);
                case QuestPackageFilter.Class:
                    return rewardProto.ItemSpecClassMask == 0 || (rewardProto.ItemSpecClassMask & GetClassMask()) != 0;
                case QuestPackageFilter.Everyone:
                    return true;
                default:
                    break;
            }

            return false;
        }

        public void RewardQuestPackage(uint questPackageId, uint onlyItemId = 0)
        {
            var hasFilteredQuestPackageReward = false;
            var questPackageItems = Global.DB2Mgr.GetQuestPackageItems(questPackageId);
            if (questPackageItems != null)
            {
                foreach (var questPackageItem in questPackageItems)
                {
                    if (onlyItemId != 0 && questPackageItem.ItemID != onlyItemId)
                        continue;

                    if (CanSelectQuestPackageItem(questPackageItem))
                    {
                        hasFilteredQuestPackageReward = true;
                        var dest = new List<ItemPosCount>();
                        if (CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, questPackageItem.ItemID, questPackageItem.ItemQuantity) == InventoryResult.Ok)
                        {
                            Item item = StoreNewItem(dest, questPackageItem.ItemID, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(questPackageItem.ItemID));
                            SendNewItem(item, questPackageItem.ItemQuantity, true, false);
                        }
                    }
                }
            }

            if (!hasFilteredQuestPackageReward)
            {
                var questPackageItemsFallback = Global.DB2Mgr.GetQuestPackageItemsFallback(questPackageId);
                if (questPackageItemsFallback != null)
                {
                    foreach (var questPackageItem in questPackageItemsFallback)
                    {
                        if (onlyItemId != 0 && questPackageItem.ItemID != onlyItemId)
                            continue;

                        var dest = new List<ItemPosCount>();
                        if (CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, questPackageItem.ItemID, questPackageItem.ItemQuantity) == InventoryResult.Ok)
                        {
                            Item item = StoreNewItem(dest, questPackageItem.ItemID, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(questPackageItem.ItemID));
                            SendNewItem(item, questPackageItem.ItemQuantity, true, false);
                        }
                    }
                }
            }
        }

        public void RewardQuest(Quest quest, LootItemType rewardType, uint rewardId, WorldObject questGiver, bool announce = true)
        {
            //this THING should be here to protect code from quest, which cast on player far teleport as a reward
            //should work fine, cause far teleport will be executed in Update()
            SetCanDelayTeleport(true);

            var questId = quest.Id;
            var oldStatus = GetQuestStatus(questId);

            foreach (var obj in quest.Objectives)
            {
                switch (obj.Type)
                {
                    case QuestObjectiveType.Item:
                        DestroyItemCount((uint)obj.ObjectID, (uint)obj.Amount, true);
                        break;
                    case QuestObjectiveType.Currency:
                        ModifyCurrency((CurrencyTypes)obj.ObjectID, -obj.Amount, false, true);
                        break;
                }
            }

            if (!quest.FlagsEx.HasAnyFlag(QuestFlagsEx.KeepAdditionalItems))
            {
                for (byte i = 0; i < SharedConst.QuestItemDropCount; ++i)
                {
                    if (quest.ItemDrop[i] != 0)
                    {
                        var count = quest.ItemDropQuantity[i];
                        DestroyItemCount(quest.ItemDrop[i], count != 0 ? count : 9999, true);
                    }
                }
            }

            RemoveTimedQuest(questId);

            if (quest.GetRewItemsCount() > 0)
            {
                for (uint i = 0; i < quest.GetRewItemsCount(); ++i)
                {
                    var itemId = quest.RewardItemId[i];
                    if (itemId != 0)
                    {
                        var dest = new List<ItemPosCount>();
                        if (CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemId, quest.RewardItemCount[i]) == InventoryResult.Ok)
                        {
                            Item item = StoreNewItem(dest, itemId, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(itemId));
                            SendNewItem(item, quest.RewardItemCount[i], true, false);
                        }
                        else if (quest.IsDFQuest())
                            SendItemRetrievalMail(itemId, quest.RewardItemCount[i], ItemContext.QuestReward);
                    }
                }
            }

            switch (rewardType)
            {
                case LootItemType.Item:
                    var rewardProto = Global.ObjectMgr.GetItemTemplate(rewardId);
                    if (rewardProto != null && quest.GetRewChoiceItemsCount() != 0)
                    {
                        for (uint i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
                        {
                            if (quest.RewardChoiceItemId[i] != 0 && quest.RewardChoiceItemType[i] == LootItemType.Item && quest.RewardChoiceItemId[i] == rewardId)
                            {
                                var dest = new List<ItemPosCount>();
                                if (CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, rewardId, quest.RewardChoiceItemCount[i]) == InventoryResult.Ok)
                                {
                                    Item item = StoreNewItem(dest, rewardId, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(rewardId));
                                    SendNewItem(item, quest.RewardChoiceItemCount[i], true, false);
                                }
                            }
                        }
                    }


                    // QuestPackageItem.db2
                    if (rewardProto != null && quest.PackageID != 0)
                        RewardQuestPackage(quest.PackageID, rewardId);
                    break;
                case LootItemType.Currency:
                    if (CliDB.CurrencyTypesStorage.HasRecord(rewardId) && quest.GetRewChoiceItemsCount() != 0)
                    {
                        for (uint i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
                            if (quest.RewardChoiceItemId[i] != 0 && quest.RewardChoiceItemType[i] == LootItemType.Currency && quest.RewardChoiceItemId[i] == rewardId)
                                ModifyCurrency((CurrencyTypes)quest.RewardChoiceItemId[i], (int)quest.RewardChoiceItemCount[i]);
                    }
                    break;
            }

            for (byte i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
            {
                if (quest.RewardCurrencyId[i] != 0)
                    ModifyCurrency((CurrencyTypes)quest.RewardCurrencyId[i], (int)quest.RewardCurrencyCount[i]);
            }

            var skill = quest.RewardSkillId;
            if (skill != 0)
                UpdateSkillPro(skill, 1000, quest.RewardSkillPoints);

            var log_slot = FindQuestSlot(questId);
            if (log_slot < SharedConst.MaxQuestLogSize)
                SetQuestSlot(log_slot, 0);

            var XP = GetQuestXPReward(quest);

            var moneyRew = 0;
            if (GetLevel() < WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
                GiveXP(XP, null);
            else
                moneyRew = (int)(quest.GetRewMoneyMaxLevel() * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));

            moneyRew += (int)GetQuestMoneyReward(quest);

            if (moneyRew != 0)
            {
                ModifyMoney(moneyRew);

                if (moneyRew > 0)
                    UpdateCriteria(CriteriaTypes.MoneyFromQuestReward, (uint)(moneyRew));
            }

            // honor reward
            var honor = quest.CalculateHonorGain(GetLevel());
            if (honor != 0)
                RewardHonor(null, 0, (int)honor);

            // title reward
            if (quest.RewardTitleId != 0)
            {
                var titleEntry = CliDB.CharTitlesStorage.LookupByKey(quest.RewardTitleId);
                if (titleEntry != null)
                    SetTitle(titleEntry);
            }

            // Send reward mail
            var mail_template_id = quest.RewardMailTemplateId;
            if (mail_template_id != 0)
            {
                var trans = new SQLTransaction();
                // @todo Poor design of mail system
                var questMailSender = quest.RewardMailSenderEntry;
                if (questMailSender != 0)
                    new MailDraft(mail_template_id).SendMailTo(trans, this, new MailSender(questMailSender), MailCheckMask.HasBody, quest.RewardMailDelay);
                else
                    new MailDraft(mail_template_id).SendMailTo(trans, this, new MailSender(questGiver), MailCheckMask.HasBody, quest.RewardMailDelay);
                DB.Characters.CommitTransaction(trans);
            }

            if (quest.IsDaily() || quest.IsDFQuest())
            {
                SetDailyQuestStatus(questId);
                if (quest.IsDaily())
                {
                    UpdateCriteria(CriteriaTypes.CompleteDailyQuest, questId);
                    UpdateCriteria(CriteriaTypes.CompleteDailyQuestDaily, questId);
                }
            }
            else if (quest.IsWeekly())
                SetWeeklyQuestStatus(questId);
            else if (quest.IsMonthly())
                SetMonthlyQuestStatus(questId);
            else if (quest.IsSeasonal())
                SetSeasonalQuestStatus(questId);

            RemoveActiveQuest(questId, false);
            if (quest.CanIncreaseRewardedQuestCounters())
                SetRewardedQuest(questId);

            SendQuestReward(quest, questGiver?.ToCreature(), XP, !announce);

            RewardReputation(quest);

            // cast spells after mark quest complete (some spells have quest completed state requirements in spell_area data)
            if (quest.RewardSpell > 0)
            {
                var spellInfo = Global.SpellMgr.GetSpellInfo(quest.RewardSpell, GetMap().GetDifficultyID());
                Unit caster = this;
                if (questGiver != null && questGiver.IsTypeMask(TypeMask.Unit) && !quest.HasFlag(QuestFlags.PlayerCastOnComplete) && !spellInfo.HasTargetType(Targets.UnitCaster))
                {
                    var unit = questGiver.ToUnit();
                    if (unit != null)
                        caster = unit;
                }

                caster.CastSpell(this, spellInfo, true);
            }
            else
            {
                foreach (var displaySpell in quest.RewardDisplaySpell)
                {
                    var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(displaySpell.PlayerConditionId);
                    if (playerCondition != null)
                        if (!ConditionManager.IsPlayerMeetingCondition(this, playerCondition))
                            continue;

                    var spellInfo = Global.SpellMgr.GetSpellInfo(displaySpell.SpellId, GetMap().GetDifficultyID());
                    Unit caster = this;
                    if (questGiver && questGiver.IsTypeMask(TypeMask.Unit) && !quest.HasFlag(QuestFlags.PlayerCastOnComplete) && !spellInfo.HasTargetType(Targets.UnitCaster))
                    {
                        var unit = questGiver.ToUnit();
                        if (unit)
                            caster = unit;
                    }

                    caster.CastSpell(this, spellInfo, true);
                }
            }

            if (quest.QuestSortID > 0)
                UpdateCriteria(CriteriaTypes.CompleteQuestsInZone, quest.Id);

            UpdateCriteria(CriteriaTypes.CompleteQuestCount);
            UpdateCriteria(CriteriaTypes.CompleteQuest, quest.Id);
            UpdateCriteria(CriteriaTypes.CompleteQuestAccumulate, 1);

            // make full db save
            SaveToDB(false);

            var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questId);
            if (questBit != 0)
                SetQuestCompletedBit(questBit, true);

            if (quest.HasFlag(QuestFlags.Pvp))
            {
                pvpInfo.IsHostile = pvpInfo.IsInHostileArea || HasPvPForcingQuest();
                UpdatePvPState();
            }

            SendQuestUpdate(questId);
            SendQuestGiverStatusMultiple();

            //lets remove flag for delayed teleports
            SetCanDelayTeleport(false);

            Global.ScriptMgr.OnQuestStatusChange(this, questId);
            Global.ScriptMgr.OnQuestStatusChange(this, quest, oldStatus, QuestStatus.Rewarded);
        }

        public void SetRewardedQuest(uint quest_id)
        {
            m_RewardedQuests.Add(quest_id);
            m_RewardedQuestsSave[quest_id] = QuestSaveType.Default;
        }

        public void FailQuest(uint questId)
        {
            var quest = Global.ObjectMgr.GetQuestTemplate(questId);
            if (quest != null)
            {
                var qStatus = GetQuestStatus(questId);

                // we can only fail incomplete quest or...
                if (qStatus != QuestStatus.Incomplete)
                {
                    // completed timed quest with no requirements
                    if (qStatus != QuestStatus.Complete || !quest.HasSpecialFlag(QuestSpecialFlags.Timed) || !quest.HasSpecialFlag(QuestSpecialFlags.CompletedAtStart))
                        return;
                }

                SetQuestStatus(questId, QuestStatus.Failed);

                var log_slot = FindQuestSlot(questId);

                if (log_slot < SharedConst.MaxQuestLogSize)
                {
                    SetQuestSlotTimer(log_slot, 1);
                    SetQuestSlotState(log_slot, QuestSlotStateMask.Fail);
                }

                if (quest.HasSpecialFlag(QuestSpecialFlags.Timed))
                {
                    var q_status = m_QuestStatus[questId];

                    RemoveTimedQuest(questId);
                    q_status.Timer = 0;

                    SendQuestTimerFailed(questId);
                }
                else
                    SendQuestFailed(questId);

                // Destroy quest items on quest failure.
                foreach (var obj in quest.Objectives)
                {
                    if (obj.Type == QuestObjectiveType.Item)
                    {
                        var itemTemplate = Global.ObjectMgr.GetItemTemplate((uint)obj.ObjectID);
                        if (itemTemplate != null)
                            if (itemTemplate.GetBonding() == ItemBondingType.Quest)
                                DestroyItemCount((uint)obj.ObjectID, (uint)obj.Amount, true, true);
                    }
                }

                // Destroy items received during the quest.
                for (byte i = 0; i < SharedConst.QuestItemDropCount; ++i)
                {
                    var itemTemplate = Global.ObjectMgr.GetItemTemplate(quest.ItemDrop[i]);
                    if (itemTemplate != null)
                        if (quest.ItemDropQuantity[i] != 0 && itemTemplate.GetBonding() == ItemBondingType.Quest)
                            DestroyItemCount(quest.ItemDrop[i], quest.ItemDropQuantity[i], true, true);
                }
            }
        }

        public void AbandonQuest(uint questId)
        {
            var quest = Global.ObjectMgr.GetQuestTemplate(questId);
            if (quest != null)
            {
                // Destroy quest items on quest abandon.
                foreach (var obj in quest.Objectives)
                {
                    if (obj.Type == QuestObjectiveType.Item)
                    {
                        var itemTemplate = Global.ObjectMgr.GetItemTemplate((uint)obj.ObjectID);
                        if (itemTemplate != null)
                            if (itemTemplate.GetBonding() == ItemBondingType.Quest)
                                DestroyItemCount((uint)obj.ObjectID, (uint)obj.Amount, true, true);
                    }
                }

                // Destroy items received during the quest.
                for (byte i = 0; i < SharedConst.QuestItemDropCount; ++i)
                {
                    var itemTemplate = Global.ObjectMgr.GetItemTemplate(quest.ItemDrop[i]);
                    if (itemTemplate != null)
                        if (quest.ItemDropQuantity[i] != 0 && itemTemplate.GetBonding() == ItemBondingType.Quest)
                            DestroyItemCount(quest.ItemDrop[i], quest.ItemDropQuantity[i], true, true);
                }
            }
        }

        public bool SatisfyQuestSkill(Quest qInfo, bool msg)
        {
            var skill = qInfo.RequiredSkillId;

            // skip 0 case RequiredSkill
            if (skill == 0)
                return true;

            // check skill value
            if (GetSkillValue((SkillType)skill) < qInfo.RequiredSkillPoints)
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.None);
                    Log.outDebug(LogFilter.Server, "SatisfyQuestSkill: Sent QuestFailedReason.None (questId: {0}) because player does not have required skill value.", qInfo.Id);
                }

                return false;
            }

            return true;
        }

        public bool SatisfyQuestLevel(Quest qInfo, bool msg)
        {
            if (GetLevel() < GetQuestMinLevel(qInfo))
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.FailedLowLevel);
                    Log.outDebug(LogFilter.Server, "SatisfyQuestLevel: Sent QuestFailedReasons.FailedLowLevel (questId: {0}) because player does not have required (min) level.", qInfo.Id);
                }
                return false;
            }

            if (qInfo.MaxLevel > 0 && GetLevel() > qInfo.MaxLevel)
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.None); // There doesn't seem to be a specific response for too high player level
                    Log.outDebug(LogFilter.Server, "SatisfyQuestLevel: Sent QuestFailedReasons.None (questId: {0}) because player does not have required (max) level.", qInfo.Id);
                }
                return false;
            }
            return true;
        }

        public bool SatisfyQuestLog(bool msg)
        {
            // exist free slot
            if (FindQuestSlot(0) < SharedConst.MaxQuestLogSize)
                return true;

            if (msg)
                SendPacket(new QuestLogFull());

            return false;
        }

        private bool SatisfyQuestDependentQuests(Quest qInfo, bool msg)
        {
            return SatisfyQuestPreviousQuest(qInfo, msg) && SatisfyQuestDependentPreviousQuests(qInfo, msg);
        }

        public bool SatisfyQuestPreviousQuest(Quest qInfo, bool msg)
        {
            // No previous quest (might be first quest in a series)
            if (qInfo.PrevQuestId == 0)
                return true;

            var prevId = (uint)Math.Abs(qInfo.PrevQuestId);
            // If positive previous quest rewarded, return true
            if (qInfo.PrevQuestId > 0 && m_RewardedQuests.Contains(prevId))
                return true;

            // If negative previous quest active, return true
            if (qInfo.PrevQuestId < 0 && GetQuestStatus(prevId) == QuestStatus.Incomplete)
                return true;

            // Has positive prev. quest in non-rewarded state
            // and negative prev. quest in non-active state
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.None);
                Log.outDebug(LogFilter.Misc, $"Player.SatisfyQuestPreviousQuest: Sent QUEST_ERR_NONE (QuestID: {qInfo.Id}) because player '{GetName()}' ({GetGUID()}) doesn't have required quest {prevId}.");
            }

            return false;
        }

        private bool SatisfyQuestDependentPreviousQuests(Quest qInfo, bool msg)
        {
            // No previous quest (might be first quest in a series)
            if (qInfo.DependentPreviousQuests.Empty())
                return true;

            foreach (var prevId in qInfo.DependentPreviousQuests)
            {
                // checked in startup
                var questInfo = Global.ObjectMgr.GetQuestTemplate(prevId);

                // If any of the previous quests completed, return true
                if (IsQuestRewarded(prevId))
                {
                    // skip one-from-all exclusive group
                    if (questInfo.ExclusiveGroup >= 0)
                        return true;

                    // each-from-all exclusive group (< 0)
                    // can be start if only all quests in prev quest exclusive group completed and rewarded
                    var bounds = Global.ObjectMgr.GetExclusiveQuestGroupBounds(questInfo.ExclusiveGroup);
                    foreach (var exclusiveQuestId in bounds)
                    {
                        // skip checked quest id, only state of other quests in group is interesting
                        if (exclusiveQuestId == prevId)
                            continue;

                        // alternative quest from group also must be completed and rewarded (reported)
                        if (!IsQuestRewarded(exclusiveQuestId))
                        {
                            if (msg)
                            {
                                SendCanTakeQuestResponse(QuestFailedReasons.None);
                                Log.outDebug(LogFilter.Misc, $"Player.SatisfyQuestDependentPreviousQuests: Sent QUEST_ERR_NONE (QuestID: {qInfo.Id}) because player '{GetName()}' ({GetGUID()}) doesn't have the required quest (1).");
                            }

                            return false;
                        }
                    }

                    return true;
                }
            }

            // Has only prev. quests in non-rewarded state
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.None);
                Log.outDebug(LogFilter.Misc, $"Player.SatisfyQuestDependentPreviousQuests: Sent QUEST_ERR_NONE (QuestID: {qInfo.Id}) because player '{GetName()}' ({GetGUID()}) doesn't have required quest (2).");
            }

            return false;
        }

        public bool SatisfyQuestClass(Quest qInfo, bool msg)
        {
            var reqClass = qInfo.AllowableClasses;

            if (reqClass == 0)
                return true;

            if ((reqClass & GetClassMask()) == 0)
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.None);
                    Log.outDebug(LogFilter.Server, "SatisfyQuestClass: Sent QuestFailedReason.None (questId: {0}) because player does not have required class.", qInfo.Id);
                }

                return false;
            }

            return true;
        }

        public bool SatisfyQuestRace(Quest qInfo, bool msg)
        {
            var reqraces = qInfo.AllowableRaces;
            if (reqraces == -1)
                return true;

            if ((reqraces & (long)SharedConst.GetMaskForRace(GetRace())) == 0)
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.FailedWrongRace);
                    Log.outDebug(LogFilter.Server, "SatisfyQuestRace: Sent QuestFailedReasons.FailedWrongRace (questId: {0}) because player does not have required race.", qInfo.Id);

                }
                return false;
            }
            return true;
        }

        public bool SatisfyQuestReputation(Quest qInfo, bool msg)
        {
            var fIdMin = qInfo.RequiredMinRepFaction;      //Min required rep
            if (fIdMin != 0 && GetReputationMgr().GetReputation(fIdMin) < qInfo.RequiredMinRepValue)
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.None);
                    Log.outDebug(LogFilter.Server, "SatisfyQuestReputation: Sent QuestFailedReason.None (questId: {0}) because player does not have required reputation (min).", qInfo.Id);
                }
                return false;
            }

            var fIdMax = qInfo.RequiredMaxRepFaction;      //Max required rep
            if (fIdMax != 0 && GetReputationMgr().GetReputation(fIdMax) >= qInfo.RequiredMaxRepValue)
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.None);
                    Log.outDebug(LogFilter.Server, "SatisfyQuestReputation: Sent QuestFailedReason.None (questId: {0}) because player does not have required reputation (max).", qInfo.Id);
                }
                return false;
            }

            /* @todo 6.x investigate if it's still needed
            // ReputationObjective2 does not seem to be an objective requirement but a requirement
            // to be able to accept the quest
                    uint fIdObj = qInfo.RequiredFactionId2;
                    if (fIdObj != 0 && GetReputationMgr().GetReputation(fIdObj) >= qInfo.RequiredFactionValue2)
                    {
                        if (msg)
                        {
                            SendCanTakeQuestResponse(QuestFailedReasons.DontHaveReq);
                            Log.outDebug(LogFilter.Misc, "SatisfyQuestReputation: Sent QuestFailedReason.None (questId: {0}) because player does not have required reputation (ReputationObjective2).", qInfo.Id);
                        }
                        return false;
                    }
                    */
            return true;
        }

        public bool SatisfyQuestStatus(Quest qInfo, bool msg)
        {
            if (GetQuestStatus(qInfo.Id) == QuestStatus.Rewarded)
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.AlreadyDone);
                    Log.outDebug(LogFilter.Misc, "Player.SatisfyQuestStatus: Sent QUEST_STATUS_REWARDED (QuestID: {0}) because player '{1}' ({2}) quest status is already REWARDED.",
                        qInfo.Id, GetName(), GetGUID().ToString());
                }
                return false;
            }

            if (GetQuestStatus(qInfo.Id) != QuestStatus.None)
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.AlreadyOn1);
                    Log.outDebug(LogFilter.Server, "SatisfyQuestStatus: Sent QuestFailedReasons.AlreadyOn1 (questId: {0}) because player quest status is not NONE.", qInfo.Id);
                }
                return false;
            }
            return true;
        }

        public bool SatisfyQuestConditions(Quest qInfo, bool msg)
        {
            if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.QuestAvailable, qInfo.Id, this))
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.None);
                    Log.outDebug(LogFilter.Server, "SatisfyQuestConditions: Sent QuestFailedReason.None (questId: {0}) because player does not meet conditions.", qInfo.Id);
                }
                Log.outDebug(LogFilter.Condition, "SatisfyQuestConditions: conditions not met for quest {0}", qInfo.Id);
                return false;
            }
            return true;
        }

        public bool SatisfyQuestTimed(Quest qInfo, bool msg)
        {
            if (!m_timedquests.Empty() && qInfo.HasSpecialFlag(QuestSpecialFlags.Timed))
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.OnlyOneTimed);
                    Log.outDebug(LogFilter.Server, "SatisfyQuestTimed: Sent QuestFailedReasons.OnlyOneTimed (questId: {0}) because player is already on a timed quest.", qInfo.Id);
                }
                return false;
            }
            return true;
        }

        public bool SatisfyQuestExclusiveGroup(Quest qInfo, bool msg)
        {
            // non positive exclusive group, if > 0 then can be start if any other quest in exclusive group already started/completed
            if (qInfo.ExclusiveGroup <= 0)
                return true;

            var range = Global.ObjectMgr.GetExclusiveQuestGroupBounds(qInfo.ExclusiveGroup);
            // always must be found if qInfo.ExclusiveGroup != 0

            foreach (var exclude_Id in range)
            {
                // skip checked quest id, only state of other quests in group is interesting
                if (exclude_Id == qInfo.Id)
                    continue;

                // not allow have daily quest if daily quest from exclusive group already recently completed
                var Nquest = Global.ObjectMgr.GetQuestTemplate(exclude_Id);
                if (!SatisfyQuestDay(Nquest, false) || !SatisfyQuestWeek(Nquest, false) || !SatisfyQuestSeasonal(Nquest, false))
                {
                    if (msg)
                    {
                        SendCanTakeQuestResponse(QuestFailedReasons.None);
                        Log.outDebug(LogFilter.Server, "SatisfyQuestExclusiveGroup: Sent QuestFailedReason.None (questId: {0}) because player already did daily quests in exclusive group.", qInfo.Id);
                    }

                    return false;
                }

                // alternative quest already started or completed - but don't check rewarded states if both are repeatable
                if (GetQuestStatus(exclude_Id) != QuestStatus.None || (!(qInfo.IsRepeatable() && Nquest.IsRepeatable()) && GetQuestRewardStatus(exclude_Id)))
                {
                    if (msg)
                    {
                        SendCanTakeQuestResponse(QuestFailedReasons.None);
                        Log.outDebug(LogFilter.Server, "SatisfyQuestExclusiveGroup: Sent QuestFailedReason.None (questId: {0}) because player already did quest in exclusive group.", qInfo.Id);
                    }
                    return false;
                }
            }
            return true;
        }

        public bool SatisfyQuestDay(Quest qInfo, bool msg)
        {
            if (!qInfo.IsDaily() && !qInfo.IsDFQuest())
                return true;

            if (qInfo.IsDFQuest())
            {
                if (m_DFQuests.Contains(qInfo.Id))
                    return false;

                return true;
            }

            return m_activePlayerData.DailyQuestsCompleted.FindIndex(qInfo.Id) == -1;
        }

        public bool SatisfyQuestWeek(Quest qInfo, bool msg)
        {
            if (!qInfo.IsWeekly() || m_weeklyquests.Empty())
                return true;

            // if not found in cooldown list
            return !m_weeklyquests.Contains(qInfo.Id);
        }

        public bool SatisfyQuestSeasonal(Quest qInfo, bool msg)
        {
            if (!qInfo.IsSeasonal() || m_seasonalquests.Empty())
                return true;

            var list = m_seasonalquests.LookupByKey(qInfo.GetEventIdForQuest());
            if (list == null || list.Empty())
                return true;

            // if not found in cooldown list
            return !list.Contains(qInfo.Id);
        }

        public bool SatisfyQuestMonth(Quest qInfo, bool msg)
        {
            if (!qInfo.IsMonthly() || m_monthlyquests.Empty())
                return true;

            // if not found in cooldown list
            return !m_monthlyquests.Contains(qInfo.Id);
        }

        public bool GiveQuestSourceItem(Quest quest)
        {
            var srcitem = quest.SourceItemId;
            if (srcitem > 0)
            {
                var count = quest.SourceItemIdCount;
                if (count <= 0)
                    count = 1;

                var dest = new List<ItemPosCount>();
                InventoryResult msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, srcitem, count);
                if (msg == InventoryResult.Ok)
                {
                    Item item = StoreNewItem(dest, srcitem, true);
                    SendNewItem(item, count, true, false);
                    return true;
                }
                // player already have max amount required item, just report success
                if (msg == InventoryResult.ItemMaxCount)
                    return true;

                SendEquipError(msg, null, null, srcitem);
                return false;
            }

            return true;
        }

        public bool TakeQuestSourceItem(uint questId, bool msg)
        {
            var quest = Global.ObjectMgr.GetQuestTemplate(questId);
            if (quest != null)
            {
                var srcItemId = quest.SourceItemId;
                var item = Global.ObjectMgr.GetItemTemplate(srcItemId);

                if (srcItemId > 0)
                {
                    var count = quest.SourceItemIdCount;
                    if (count <= 0)
                        count = 1;

                    // exist two cases when destroy source quest item not possible:
                    // a) non un-equippable item (equipped non-empty bag, for example)
                    // b) when quest is started from an item and item also is needed in
                    // the end as RequiredItemId
                    InventoryResult res = CanUnequipItems(srcItemId, count);
                    if (res != InventoryResult.Ok)
                    {
                        if (msg)
                            SendEquipError(res, null, null, srcItemId);
                        return false;
                    }

                    var destroyItem = true;
                    if (item.GetStartQuest() == questId)
                    {
                        foreach (var obj in quest.Objectives)
                            if (obj.Type == QuestObjectiveType.Item && srcItemId == obj.ObjectID)
                                destroyItem = false;
                    }

                    if (destroyItem)
                        DestroyItemCount(srcItemId, count, true, true);
                }
            }

            return true;
        }

        public bool GetQuestRewardStatus(uint quest_id)
        {
            var qInfo = Global.ObjectMgr.GetQuestTemplate(quest_id);
            if (qInfo != null)
            {
                if (qInfo.IsSeasonal() && !qInfo.IsRepeatable())
                    return !SatisfyQuestSeasonal(qInfo, false);

                // for repeatable quests: rewarded field is set after first reward only to prevent getting XP more than once
                if (!qInfo.IsRepeatable())
                    return IsQuestRewarded(quest_id);

                return false;
            }
            return false;
        }

        public QuestStatus GetQuestStatus(uint questId)
        {
            if (questId != 0)
            {
                var questStatusData = m_QuestStatus.LookupByKey(questId);
                if (questStatusData != null)
                    return questStatusData.Status;

                if (GetQuestRewardStatus(questId))
                    return QuestStatus.Rewarded;
            }
            return QuestStatus.None;
        }

        public bool CanShareQuest(uint quest_id)
        {
            var qInfo = Global.ObjectMgr.GetQuestTemplate(quest_id);
            if (qInfo != null && qInfo.HasFlag(QuestFlags.Sharable))
            {
                var questStatusData = m_QuestStatus.LookupByKey(quest_id);
                if (questStatusData != null)
                {
                    // in pool and not currently available (wintergrasp weekly, dalaran weekly) - can't share
                    if (Global.PoolMgr.IsPartOfAPool<Quest>(quest_id) != 0 && !Global.PoolMgr.IsSpawnedObject<Quest>(quest_id))
                    {
                        SendPushToPartyResponse(this, QuestPushReason.NotDaily);
                        return false;
                    }

                    return true;
                }
            }
            return false;
        }

        public void SetQuestStatus(uint questId, QuestStatus status, bool update = true)
        {
            var quest = Global.ObjectMgr.GetQuestTemplate(questId);
            if (quest != null)
            {
                if (!m_QuestStatus.ContainsKey(questId))
                    m_QuestStatus[questId] = new QuestStatusData();

                var oldStatus = m_QuestStatus[questId].Status;
                m_QuestStatus[questId].Status = status;
                if (!quest.IsAutoComplete())
                    m_QuestStatusSave[questId] = QuestSaveType.Default;

                Global.ScriptMgr.OnQuestStatusChange(this, questId);
                Global.ScriptMgr.OnQuestStatusChange(this, quest, oldStatus, status);
            }

            if (update)
                SendQuestUpdate(questId);
        }

        public void RemoveActiveQuest(uint quest_id, bool update = true)
        {
            if (m_QuestStatus.ContainsKey(quest_id))
            {
                m_QuestStatus.Remove(quest_id);
                m_QuestStatusSave[quest_id] = QuestSaveType.Delete;
            }

            if (update)
                SendQuestUpdate(quest_id);
        }

        public void RemoveRewardedQuest(uint questId, bool update = true)
        {
            if (m_RewardedQuests.Contains(questId))
            {
                m_RewardedQuests.Remove(questId);
                m_RewardedQuestsSave[questId] = QuestSaveType.ForceDelete;
            }

            var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questId);
            if (questBit != 0)
                SetQuestCompletedBit(questBit, false);

            // Remove seasonal quest also
            var qInfo = Global.ObjectMgr.GetQuestTemplate(questId);
            if (qInfo.IsSeasonal())
            {
                var eventId = qInfo.GetEventIdForQuest();
                if (m_seasonalquests.ContainsKey(eventId))
                {
                    m_seasonalquests.Remove(eventId, questId);
                    m_SeasonalQuestChanged = true;
                }
            }

            if (update)
                SendQuestUpdate(questId);
        }

        private void SendQuestUpdate(uint questid)
        {
            uint zone, area;
            GetZoneAndAreaId(out zone, out area);

            var saBounds = Global.SpellMgr.GetSpellAreaForQuestAreaMapBounds(area, questid);
            if (!saBounds.Empty())
            {
                foreach (var spell in saBounds)
                {
                    if (spell.flags.HasAnyFlag(SpellAreaFlag.AutoRemove) && !spell.IsFitToRequirements(this, zone, area))
                        RemoveAurasDueToSpell(spell.spellId);
                    else if (spell.flags.HasAnyFlag(SpellAreaFlag.AutoCast) && !spell.flags.HasAnyFlag(SpellAreaFlag.IgnoreAutocastOnQuestStatusChange))
                        if (!HasAura(spell.spellId))
                            CastSpell(this, spell.spellId, true);
                }
            }

            UpdateForQuestWorldObjects();
            PhasingHandler.OnConditionChange(this);
        }

        public QuestGiverStatus GetQuestDialogStatus(WorldObject questgiver)
        {
            List<uint> qr;
            List<uint> qir;

            switch (questgiver.GetTypeId())
            {
                case TypeId.GameObject:
                    {
                        var questStatus = questgiver.ToGameObject().GetAI().GetDialogStatus(this);
                        if (questStatus != QuestGiverStatus.ScriptedDefault)
                            return questStatus;
                        qr = Global.ObjectMgr.GetGOQuestRelationBounds(questgiver.GetEntry());
                        qir = Global.ObjectMgr.GetGOQuestInvolvedRelationBounds(questgiver.GetEntry());
                        break;
                    }
                case TypeId.Unit:
                    {
                        var questStatus = questgiver.ToCreature().GetAI().GetDialogStatus(this);
                        if (questStatus != QuestGiverStatus.ScriptedDefault)
                            return questStatus;
                        qr = Global.ObjectMgr.GetCreatureQuestRelationBounds(questgiver.GetEntry());
                        qir = Global.ObjectMgr.GetCreatureQuestInvolvedRelationBounds(questgiver.GetEntry());
                        break;
                    }
                default:
                    // it's impossible, but check
                    Log.outError(LogFilter.Player, "GetQuestDialogStatus called for unexpected type {0}", questgiver.GetTypeId());
                    return QuestGiverStatus.None;
            }

            var result = QuestGiverStatus.None;

            foreach (var questId in qir)
            {
                var quest = Global.ObjectMgr.GetQuestTemplate(questId);
                if (quest == null)
                    continue;

                switch (GetQuestStatus(questId))
                {
                    case QuestStatus.Complete:
                        if (quest.GetQuestTag() == QuestTagType.CovenantCalling)
                            result |= quest.HasFlag(QuestFlags.HideRewardPoi) ? QuestGiverStatus.CovenantCallingRewardCompleteNoPOI : QuestGiverStatus.CovenantCallingRewardCompletePOI;
                        else if (quest.HasFlagEx(QuestFlagsEx.LegendaryQuest))
                            result |= quest.HasFlag(QuestFlags.HideRewardPoi) ? QuestGiverStatus.LegendaryRewardCompleteNoPOI : QuestGiverStatus.LegendaryRewardCompletePOI;
                        else
                            result |= quest.HasFlag(QuestFlags.HideRewardPoi) ? QuestGiverStatus.RewardCompleteNoPOI : QuestGiverStatus.RewardCompletePOI;
                        break;
                    case QuestStatus.Incomplete:
                        if (quest.GetQuestTag() == QuestTagType.CovenantCalling)
                            result |= QuestGiverStatus.CovenantCallingReward;
                        else
                            result |= QuestGiverStatus.Reward;
                        break;
                    default:
                        break;
                }

                if (quest.IsAutoComplete() && CanTakeQuest(quest, false) && quest.IsRepeatable() && !quest.IsDailyOrWeekly())
                {
                    if (GetLevel() <= (GetQuestLevel(quest) + WorldConfig.GetIntValue(WorldCfg.QuestLowLevelHideDiff)))
                        result |= QuestGiverStatus.RepeatableTurnin;
                    else
                        result |= QuestGiverStatus.TrivialRepeatableTurnin;
                }
            }

            foreach (var questId in qr)
            {
                var quest = Global.ObjectMgr.GetQuestTemplate(questId);
                if (quest == null)
                    continue;

                if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.QuestAvailable, quest.Id, this))
                    continue;

                if (GetQuestStatus(questId) == QuestStatus.None)
                {
                    if (CanSeeStartQuest(quest))
                    {
                        if (SatisfyQuestLevel(quest, false))
                        {
                            if (GetLevel() <= (GetQuestLevel(quest) + WorldConfig.GetIntValue(WorldCfg.QuestLowLevelHideDiff)))
                            {
                                if (quest.GetQuestTag() == QuestTagType.CovenantCalling)
                                    result |= QuestGiverStatus.CovenantCallingQuest;
                                else if (quest.HasFlagEx(QuestFlagsEx.LegendaryQuest))
                                    result |= QuestGiverStatus.LegendaryQuest;
                                else if (quest.IsDaily())
                                    result |= QuestGiverStatus.DailyQuest;
                                else
                                    result |= QuestGiverStatus.Quest;
                            }
                            else if (quest.IsDaily())
                                result |= QuestGiverStatus.TrivialDailyQuest;
                            else
                                result |= QuestGiverStatus.Trivial;
                        }
                        else
                            result |= QuestGiverStatus.Future;
                    }
                }
            }

            return result;
        }

        public ushort GetReqKillOrCastCurrentCount(uint quest_id, int entry)
        {
            var qInfo = Global.ObjectMgr.GetQuestTemplate(quest_id);
            if (qInfo == null)
                return 0;

            foreach (var obj in qInfo.Objectives)
                if (obj.ObjectID == entry)
                    return (ushort)GetQuestObjectiveData(qInfo, obj.StorageIndex);

            return 0;
        }

        public void AdjustQuestReqItemCount(Quest quest)
        {
            if (quest.HasSpecialFlag(QuestSpecialFlags.Deliver))
            {
                foreach (var obj in quest.Objectives)
                {
                    if (obj.Type != QuestObjectiveType.Item)
                        continue;

                    var reqItemCount = (uint)obj.Amount;
                    uint curItemCount = GetItemCount((uint)obj.ObjectID, true);
                    SetQuestObjectiveData(obj, (int)Math.Min(curItemCount, reqItemCount));
                }
            }
        }

        public ushort FindQuestSlot(uint quest_id)
        {
            for (ushort i = 0; i < SharedConst.MaxQuestLogSize; ++i)
                if (GetQuestSlotQuestId(i) == quest_id)
                    return i;

            return SharedConst.MaxQuestLogSize;
        }

        public uint GetQuestSlotQuestId(ushort slot)
        {
            return m_playerData.QuestLog[slot].QuestID;
        }

        public uint GetQuestSlotState(ushort slot, byte counter)
        {
            return m_playerData.QuestLog[slot].StateFlags;
        }

        public ushort GetQuestSlotCounter(ushort slot, byte counter)
        {
            if (counter < SharedConst.MaxQuestCounts)
                return m_playerData.QuestLog[slot].ObjectiveProgress[counter];

            return 0;
        }

        public uint GetQuestSlotTime(ushort slot)
        {
            return m_playerData.QuestLog[slot].EndTime;
        }

        public void SetQuestSlot(ushort slot, uint quest_id, uint timer = 0)
        {
            var questLogField = m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.QuestLog, slot);
            SetUpdateFieldValue(questLogField.ModifyValue(questLogField.QuestID), quest_id);
            SetUpdateFieldValue(questLogField.ModifyValue(questLogField.StateFlags), 0u);

            for (var i = 0; i < SharedConst.MaxQuestCounts; ++i)
                SetUpdateFieldValue(ref questLogField.ModifyValue(questLogField.ObjectiveProgress, i), (ushort)0);

            SetUpdateFieldValue(questLogField.ModifyValue(questLogField.EndTime), timer);
        }

        public void SetQuestSlotCounter(ushort slot, byte counter, ushort count)
        {
            if (counter >= SharedConst.MaxQuestCounts)
                return;

            var questLog = m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.QuestLog, slot);
            SetUpdateFieldValue(ref questLog.ModifyValue(questLog.ObjectiveProgress, counter), count);
        }

        public void SetQuestSlotState(ushort slot, QuestSlotStateMask state)
        {
            var questLogField = m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.QuestLog, slot);
            SetUpdateFieldFlagValue(questLogField.ModifyValue(questLogField.StateFlags), (uint)state);
        }

        public void RemoveQuestSlotState(ushort slot, QuestSlotStateMask state)
        {
            var questLogField = m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.QuestLog, slot);
            RemoveUpdateFieldFlagValue(questLogField.ModifyValue(questLogField.StateFlags), (uint)state);
        }

        public void SetQuestSlotTimer(ushort slot, uint timer)
        {
            var questLog = m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.QuestLog, slot);
            SetUpdateFieldValue(questLog.ModifyValue(questLog.EndTime), timer);
        }

        private void SetQuestSlotObjectiveFlag(ushort slot, sbyte objectiveIndex)
        {
            var questLog = m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.QuestLog, slot);
            SetUpdateFieldFlagValue(questLog.ModifyValue(questLog.ObjectiveFlags), 1u << objectiveIndex);
        }

        private void RemoveQuestSlotObjectiveFlag(ushort slot, sbyte objectiveIndex)
        {
            var questLog = m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.QuestLog, slot);
            RemoveUpdateFieldFlagValue(questLog.ModifyValue(questLog.ObjectiveFlags), 1u << objectiveIndex);
        }

        private void SetQuestCompletedBit(uint questBit, bool completed)
        {
            if (questBit == 0)
                return;

            var fieldOffset = (questBit - 1) >> 6;
            if (fieldOffset >= PlayerConst.QuestsCompletedBitsSize)
                return;

            var flag = 1ul << (((int)questBit - 1) & 63);
            if (completed)
                SetUpdateFieldFlagValue(ref m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.QuestCompleted, (int)fieldOffset), flag);
            else
                RemoveUpdateFieldFlagValue(ref m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.QuestCompleted, (int)fieldOffset), flag);
        }

        public void AreaExploredOrEventHappens(uint questId)
        {
            if (questId != 0)
            {
                var log_slot = FindQuestSlot(questId);
                if (log_slot < SharedConst.MaxQuestLogSize)
                {
                    Log.outError(LogFilter.Player, "Deprecated function AreaExploredOrEventHappens called for quest {0}", questId);
                    /* @todo
                    This function was previously used for area triggers but now those are a part of quest objective system
                    Currently this function is used to complete quests with no objectives (needs verifying) so probably rename it?

                    QuestStatusData& q_status = m_QuestStatus[questId];

                    if (!q_status.Explored)
                    {
                        q_status.Explored = true;
                        m_QuestStatusSave[questId] = QUEST_DEFAULT_SAVE_TYPE;
                        
                        // if we cannot complete quest send exploration succeded (to mark exploration on client)
                        if (!CanCompleteQuest(questId))
                            SendQuestComplete(questId)
                    }*/
                }
                if (CanCompleteQuest(questId))
                    CompleteQuest(questId);
            }
        }

        public void GroupEventHappens(uint questId, WorldObject pEventObject)
        {
            var group = GetGroup();
            if (group)
            {
                for (var refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                {
                    var player = refe.GetSource();

                    // for any leave or dead (with not released body) group member at appropriate distance
                    if (player && player.IsAtGroupRewardDistance(pEventObject) && !player.GetCorpse())
                        player.AreaExploredOrEventHappens(questId);
                }
            }
            else
                AreaExploredOrEventHappens(questId);
        }

        public void ItemAddedQuestCheck(uint entry, uint count)
        {
            for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            {
                var questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                var q_status = m_QuestStatus[questid];

                if (q_status.Status != QuestStatus.Incomplete)
                    continue;

                var qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null || !qInfo.HasSpecialFlag(QuestSpecialFlags.Deliver))
                    continue;

                foreach (var obj in qInfo.Objectives)
                {
                    if (obj.Type != QuestObjectiveType.Item)
                        continue;

                    var reqItem = obj.ObjectID;
                    if (reqItem == entry)
                    {
                        var reqItemCount = obj.Amount;
                        var curItemCount = GetQuestObjectiveData(qInfo, obj.StorageIndex);
                        if (curItemCount < reqItemCount)
                        {
                            var newItemCount = (int)Math.Min(curItemCount + count, reqItemCount);
                            SetQuestObjectiveData(obj, newItemCount);

                            //SendQuestUpdateAddItem(qInfo, j, additemcount);
                            // FIXME: verify if there's any packet sent updating item
                        }

                        if (CanCompleteQuest(questid))
                            CompleteQuest(questid);

                        return;
                    }
                }
            }
            UpdateForQuestWorldObjects();
        }

        public void ItemRemovedQuestCheck(uint entry, uint count)
        {
            for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            {
                var questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;
                var qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null)
                    continue;
                if (!qInfo.HasSpecialFlag(QuestSpecialFlags.Deliver))
                    continue;

                foreach (var obj in qInfo.Objectives)
                {
                    if (obj.Type != QuestObjectiveType.Item)
                        continue;

                    var reqItem = obj.ObjectID;
                    if (reqItem == entry)
                    {
                        var reqItemCount = (uint)obj.Amount;
                        var curItemCount = GetQuestObjectiveData(qInfo, obj.StorageIndex);

                        if (curItemCount >= reqItemCount) // we may have more than what the status shows
                            curItemCount = (int)GetItemCount(entry, false);

                        var newItemCount = (int)((count > curItemCount) ? 0 : curItemCount - count);

                        if (newItemCount < reqItemCount)
                        {
                            SetQuestObjectiveData(obj, newItemCount);
                            IncompleteQuest(questid);
                        }
                        return;
                    }
                }
            }
            UpdateForQuestWorldObjects();
        }

        public void KilledMonster(CreatureTemplate cInfo, ObjectGuid guid)
        {
            Cypher.Assert(cInfo != null);

            if (cInfo.Entry != 0)
                KilledMonsterCredit(cInfo.Entry, guid);

            for (byte i = 0; i < 2; ++i)
                if (cInfo.KillCredit[i] != 0)
                    KilledMonsterCredit(cInfo.KillCredit[i]);
        }

        public void KilledMonsterCredit(uint entry, ObjectGuid guid = default)
        {
            ushort addKillCount = 1;
            var real_entry = entry;
            Creature killed = null;
            if (!guid.IsEmpty())
            {
                killed = GetMap().GetCreature(guid);
                if (killed != null && killed.GetEntry() != 0)
                    real_entry = killed.GetEntry();
            }

            StartCriteriaTimer(CriteriaTimedTypes.Creature, real_entry);   // MUST BE CALLED FIRST
            UpdateCriteria(CriteriaTypes.KillCreature, real_entry, addKillCount, 0, killed);

            for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            {
                var questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                var qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null)
                    continue;

                // just if !ingroup || !noraidgroup || raidgroup
                var q_status = m_QuestStatus[questid];
                if (q_status.Status == QuestStatus.Incomplete && (GetGroup() == null || !GetGroup().IsRaidGroup() || qInfo.IsAllowedInRaid(GetMap().GetDifficultyID())))
                {
                    if (qInfo.HasSpecialFlag(QuestSpecialFlags.Kill))// && !qInfo.HasSpecialFlag(QuestSpecialFlags.Cast))
                    {
                        foreach (var obj in qInfo.Objectives)
                        {
                            if (obj.Type != QuestObjectiveType.Monster)
                                continue;

                            var reqkill = obj.ObjectID;
                            if (reqkill == real_entry)
                            {
                                var curKillCount = GetQuestObjectiveData(qInfo, obj.StorageIndex);
                                if (curKillCount < obj.Amount)
                                {
                                    SetQuestObjectiveData(obj, curKillCount + addKillCount);
                                    SendQuestUpdateAddCredit(qInfo, guid, obj, (uint)(curKillCount + addKillCount));
                                }

                                if (CanCompleteQuest(questid))
                                    CompleteQuest(questid);

                                // same objective target can be in many active quests, but not in 2 objectives for single quest (code optimization).
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void KilledPlayerCredit()
        {
            ushort addKillCount = 1;

            for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            {
                var questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                var qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null)
                    continue;

                // just if !ingroup || !noraidgroup || raidgroup
                var q_status = m_QuestStatus[questid];
                if (q_status.Status == QuestStatus.Incomplete && (GetGroup() == null || !GetGroup().IsRaidGroup() || qInfo.IsAllowedInRaid(GetMap().GetDifficultyID())))
                {
                    foreach (var obj in qInfo.Objectives)
                    {
                        if (obj.Type != QuestObjectiveType.PlayerKills)
                            continue;

                        var curKillCount = GetQuestObjectiveData(qInfo, obj.StorageIndex);
                        if (curKillCount < obj.Amount)
                        {
                            SetQuestObjectiveData(obj, curKillCount + addKillCount);
                            SendQuestUpdateAddPlayer(qInfo, (uint)(curKillCount + addKillCount));
                        }

                        if (CanCompleteQuest(questid))
                            CompleteQuest(questid);

                        // Quest can't have more than one player kill objective (code optimisation)
                        break;
                    }
                }
            }
        }

        public void KillCreditGO(uint entry, ObjectGuid guid = default)
        {
            ushort addCastCount = 1;
            for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            {
                var questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                var qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null)
                    continue;

                var q_status = m_QuestStatus[questid];

                if (q_status.Status == QuestStatus.Incomplete)
                {
                    if (qInfo.HasSpecialFlag(QuestSpecialFlags.Cast))
                    {
                        foreach (var obj in qInfo.Objectives)
                        {
                            if (obj.Type != QuestObjectiveType.GameObject)
                                continue;

                            var reqTarget = obj.ObjectID;

                            // other not this creature/GO related objectives
                            if (reqTarget != entry)
                                continue;

                            var curCastCount = GetQuestObjectiveData(qInfo, obj.StorageIndex);
                            if (curCastCount < obj.Amount)
                            {
                                SetQuestObjectiveData(obj, curCastCount + addCastCount);
                                SendQuestUpdateAddCredit(qInfo, guid, obj, (uint)(curCastCount + addCastCount));
                            }

                            if (CanCompleteQuest(questid))
                                CompleteQuest(questid);

                            // same objective target can be in many active quests, but not in 2 objectives for single quest (code optimization).
                            break;
                        }
                    }
                }
            }
        }

        public void KillCreditCriteriaTreeObjective(QuestObjective questObjective)
        {
            if (questObjective.Type != QuestObjectiveType.CriteriaTree)
                return;

            if (GetQuestStatus(questObjective.QuestID) == QuestStatus.Incomplete)
            {
                SetQuestObjectiveData(questObjective, 1);
                SendQuestUpdateAddCreditSimple(questObjective);

                if (CanCompleteQuest(questObjective.QuestID))
                    CompleteQuest(questObjective.QuestID);
            }
        }

        public void TalkedToCreature(uint entry, ObjectGuid guid)
        {
            ushort addTalkCount = 1;
            for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            {
                var questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                var qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null)
                    continue;

                var q_status = m_QuestStatus[questid];

                if (q_status.Status == QuestStatus.Incomplete)
                {
                    if (qInfo.HasSpecialFlag(QuestSpecialFlags.Kill | QuestSpecialFlags.Cast | QuestSpecialFlags.Speakto))
                    {
                        foreach (var obj in qInfo.Objectives)
                        {
                            if (obj.Type != QuestObjectiveType.TalkTo)
                                continue;

                            var reqTarget = obj.ObjectID;
                            if (reqTarget == entry)
                            {
                                var curTalkCount = GetQuestObjectiveData(qInfo, obj.StorageIndex);
                                if (curTalkCount < obj.Amount)
                                {
                                    SetQuestObjectiveData(obj, curTalkCount + addTalkCount);
                                    SendQuestUpdateAddCredit(qInfo, guid, obj, (uint)(curTalkCount + addTalkCount));
                                }

                                if (CanCompleteQuest(questid))
                                    CompleteQuest(questid);

                                // Quest can't have more than one objective for the same creature (code optimisation)
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void MoneyChanged(ulong value)
        {
            for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            {
                var questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                var qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null)
                    continue;

                foreach (var obj in qInfo.Objectives)
                {
                    if (obj.Type != QuestObjectiveType.Money)
                        continue;

                    var q_status = m_QuestStatus[questid];
                    if (q_status.Status == QuestStatus.Incomplete)
                    {
                        if ((long)value >= obj.Amount)
                        {
                            if (CanCompleteQuest(questid))
                                CompleteQuest(questid);
                        }
                    }
                    else if (q_status.Status == QuestStatus.Complete)
                    {
                        if ((long)value < obj.Amount)
                            IncompleteQuest(questid);
                    }
                }
            }
        }

        public void ReputationChanged(FactionRecord FactionRecord)
        {
            for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            {
                var questid = GetQuestSlotQuestId(i);
                if (questid != 0)
                {
                    var qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                    if (qInfo != null)
                    {
                        var q_status = m_QuestStatus[questid];

                        foreach (var obj in qInfo.Objectives)
                        {
                            if (obj.ObjectID != FactionRecord.Id)
                                continue;

                            if (obj.Type == QuestObjectiveType.MinReputation)
                            {
                                if (q_status.Status == QuestStatus.Incomplete)
                                {
                                    if (GetReputationMgr().GetReputation(FactionRecord) >= obj.Amount)
                                        if (CanCompleteQuest(questid))
                                            CompleteQuest(questid);
                                }
                                else if (q_status.Status == QuestStatus.Complete)
                                {
                                    if (GetReputationMgr().GetReputation(FactionRecord) < obj.Amount)
                                        IncompleteQuest(questid);
                                }
                            }
                            else if (obj.Type == QuestObjectiveType.MaxReputation)
                            {
                                if (q_status.Status == QuestStatus.Incomplete)
                                {
                                    if (GetReputationMgr().GetReputation(FactionRecord) <= obj.Amount)
                                        if (CanCompleteQuest(questid))
                                            CompleteQuest(questid);
                                }
                                else if (q_status.Status == QuestStatus.Complete)
                                {
                                    if (GetReputationMgr().GetReputation(FactionRecord) > obj.Amount)
                                        IncompleteQuest(questid);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CurrencyChanged(uint currencyId, int change)
        {
            for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            {
                var questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                var qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null)
                    continue;

                foreach (var obj in qInfo.Objectives)
                {
                    if (obj.ObjectID != currencyId)
                        continue;

                    var q_status = m_QuestStatus[questid];
                    if (obj.Type == QuestObjectiveType.Currency || obj.Type == QuestObjectiveType.HaveCurrency)
                    {
                        long value = GetCurrency(currencyId);
                        if (obj.Type == QuestObjectiveType.HaveCurrency)
                            SetQuestObjectiveData(obj, (int)Math.Min(value, obj.Amount));

                        if (q_status.Status == QuestStatus.Incomplete)
                        {
                            if (value >= obj.Amount)
                            {
                                if (CanCompleteQuest(questid))
                                    CompleteQuest(questid);
                            }
                        }
                        else if (q_status.Status == QuestStatus.Complete)
                        {
                            if (value < obj.Amount)
                                IncompleteQuest(questid);
                        }
                    }
                    else if (obj.Type == QuestObjectiveType.ObtainCurrency && change > 0) // currency losses are not accounted for in this objective type
                    {
                        long currentProgress = GetQuestObjectiveData(qInfo, obj.StorageIndex);
                        SetQuestObjectiveData(obj, (int)Math.Max(Math.Min(currentProgress + change, obj.Amount), 0));
                        if (CanCompleteQuest(questid))
                            CompleteQuest(questid);
                    }
                }
            }
        }

        public bool HasQuestForItem(uint itemid)
        {
            for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            {
                var questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                var q_status = m_QuestStatus.LookupByKey(questid);
                if (q_status == null)
                    continue;

                if (q_status.Status == QuestStatus.Incomplete)
                {
                    var qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                    if (qInfo == null)
                        continue;

                    // hide quest if player is in raid-group and quest is no raid quest
                    if (GetGroup() != null && GetGroup().IsRaidGroup() && !qInfo.IsAllowedInRaid(GetMap().GetDifficultyID()))
                        if (!InBattleground()) //there are two ways.. we can make every bg-quest a raidquest, or add this code here.. i don't know if this can be exploited by other quests, but i think all other quests depend on a specific area.. but keep this in mind, if something strange happens later
                            continue;

                    // There should be no mixed ReqItem/ReqSource drop
                    // This part for ReqItem drop
                    foreach (var obj in qInfo.Objectives)
                    {
                        if (obj.Type == QuestObjectiveType.Item && itemid == obj.ObjectID && GetQuestObjectiveData(qInfo, obj.StorageIndex) < obj.Amount)
                            return true;
                    }
                    // This part - for ReqSource
                    for (byte j = 0; j < SharedConst.QuestItemDropCount; ++j)
                    {
                        // examined item is a source item
                        if (qInfo.ItemDrop[j] == itemid)
                        {
                            var pProto = Global.ObjectMgr.GetItemTemplate(itemid);

                            // 'unique' item
                            if (pProto.GetMaxCount() != 0 && GetItemCount(itemid, true) < pProto.GetMaxCount())
                                return true;

                            // allows custom amount drop when not 0
                            if (qInfo.ItemDropQuantity[j] != 0)
                            {
                                if (GetItemCount(itemid, true) < qInfo.ItemDropQuantity[j])
                                    return true;
                            }
                            else if (GetItemCount(itemid, true) < pProto.GetMaxStackSize())
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        public int GetQuestObjectiveData(Quest quest, sbyte storageIndex)
        {
            if (storageIndex < 0)
                Log.outError(LogFilter.Player, "GetQuestObjectiveData: called for quest {0} with invalid StorageIndex {1} (objective data is not tracked)", quest.Id, storageIndex);

            var status = m_QuestStatus.LookupByKey(quest.Id);
            if (status == null)
            {
                Log.outError(LogFilter.Player, "GetQuestObjectiveData: player {0} ({1}) doesn't have quest status data for quest {2}", GetName(), GetGUID().ToString(), quest.Id);
                return 0;
            }

            if (storageIndex >= status.ObjectiveData.Length)
            {
                Log.outError(LogFilter.Player, "GetQuestObjectiveData: player {0} ({1}) quest {2} out of range StorageIndex {3}", GetName(), GetGUID().ToString(), quest.Id, storageIndex);
                return 0;
            }

            return status.ObjectiveData[storageIndex];
        }

        private bool IsQuestObjectiveProgressComplete(Quest quest)
        {
            float progress = 0;
            foreach (var obj in quest.Objectives)
            {
                if (obj.Flags.HasAnyFlag(QuestObjectiveFlags.PartOfProgressBar))
                {
                    progress += GetQuestObjectiveData(quest, obj.StorageIndex) * obj.ProgressBarWeight;
                    if (progress >= 100)
                        return true;
                }
            }
            return false;
        }

        public bool IsQuestObjectiveComplete(QuestObjective objective)
        {
            var quest = Global.ObjectMgr.GetQuestTemplate(objective.QuestID);
            //ASSERT(quest);

            switch (objective.Type)
            {
                case QuestObjectiveType.Monster:
                case QuestObjectiveType.Item:
                case QuestObjectiveType.GameObject:
                case QuestObjectiveType.PlayerKills:
                case QuestObjectiveType.TalkTo:
                case QuestObjectiveType.WinPvpPetBattles:
                case QuestObjectiveType.HaveCurrency:
                case QuestObjectiveType.ObtainCurrency:
                    if (GetQuestObjectiveData(quest, objective.StorageIndex) < objective.Amount)
                        return false;
                    break;
                case QuestObjectiveType.MinReputation:
                    if (GetReputationMgr().GetReputation((uint)objective.ObjectID) < objective.Amount)
                        return false;
                    break;
                case QuestObjectiveType.MaxReputation:
                    if (GetReputationMgr().GetReputation((uint)objective.ObjectID) > objective.Amount)
                        return false;
                    break;
                case QuestObjectiveType.Money:
                    if (!HasEnoughMoney(objective.Amount))
                        return false;
                    break;
                case QuestObjectiveType.AreaTrigger:
                case QuestObjectiveType.CriteriaTree:
                    if (GetQuestObjectiveData(quest, objective.StorageIndex) == 0)
                        return false;
                    break;
                case QuestObjectiveType.LearnSpell:
                    if (!HasSpell((uint)objective.ObjectID))
                        return false;
                    break;
                case QuestObjectiveType.Currency:
                    if (!HasCurrency((uint)objective.ObjectID, (uint)objective.Amount))
                        return false;
                    break;
                case QuestObjectiveType.ProgressBar:
                    if (!IsQuestObjectiveProgressComplete(quest))
                        return false;
                    break;
                default:
                    Log.outError(LogFilter.Player, "Player.CanCompleteQuest: Player '{0}' ({1}) tried to complete a quest (ID: {2}) with an unknown objective type {3}",
                        GetName(), GetGUID().ToString(), objective.QuestID, objective.Type);
                    return false;
            }

            return true;
        }

        public void SetQuestObjectiveData(QuestObjective objective, int data)
        {
            if (objective.StorageIndex < 0)
            {
                Log.outError(LogFilter.Player, "SetQuestObjectiveData: called for quest {0} with invalid StorageIndex {1} (objective data is not tracked)", objective.QuestID, objective.StorageIndex);
                return;
            }

            var status = m_QuestStatus.LookupByKey(objective.QuestID);
            if (status == null)
            {
                Log.outError(LogFilter.Player, "SetQuestObjectiveData: player {0} ({1}) doesn't have quest status data for quest {2}", GetName(), GetGUID().ToString(), objective.QuestID);
                return;
            }

            if (objective.StorageIndex >= status.ObjectiveData.Length)
            {
                Log.outError(LogFilter.Player, "SetQuestObjectiveData: player {0} ({1}) quest {2} out of range StorageIndex {3}", GetName(), GetGUID().ToString(), objective.QuestID, objective.StorageIndex);
                return;
            }

            // No change
            var oldData = status.ObjectiveData[objective.StorageIndex];
            if (oldData == data)
                return;

            var quest = Global.ObjectMgr.GetQuestTemplate(objective.QuestID);
            if (quest != null)
                Global.ScriptMgr.OnQuestObjectiveChange(this, quest, objective, oldData, data);

            // Set data
            status.ObjectiveData[objective.StorageIndex] = data;

            // Add to save
            m_QuestStatusSave[objective.QuestID] = QuestSaveType.Default;

            // Update quest fields
            var log_slot = FindQuestSlot(objective.QuestID);
            if (log_slot < SharedConst.MaxQuestLogSize)
            {
                if (!objective.IsStoringFlag())
                    SetQuestSlotCounter(log_slot, (byte)objective.StorageIndex, (ushort)status.ObjectiveData[objective.StorageIndex]);
                else if (data != 0)
                    SetQuestSlotObjectiveFlag(log_slot, objective.StorageIndex);
                else
                    RemoveQuestSlotObjectiveFlag(log_slot, objective.StorageIndex);
            }
        }

        public void SendQuestComplete(Quest quest)
        {
            if (quest != null)
            {
                var data = new QuestUpdateComplete();
                data.QuestID = quest.Id;
                SendPacket(data);
            }
        }

        public void SendQuestReward(Quest quest, Creature questGiver, uint xp, bool hideChatMessage)
        {
            var questId = quest.Id;
            Global.GameEventMgr.HandleQuestComplete(questId);

            uint moneyReward;

            if (GetLevel() < WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
            {
                moneyReward = GetQuestMoneyReward(quest);
            }
            else // At max level, increase gold reward
            {
                xp = 0;
                moneyReward = (uint)(GetQuestMoneyReward(quest) + (int)(quest.GetRewMoneyMaxLevel() * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney)));
            }

            var packet = new QuestGiverQuestComplete();

            packet.QuestID = questId;
            packet.MoneyReward = moneyReward;
            packet.XPReward = xp;
            packet.SkillLineIDReward = quest.RewardSkillId;
            packet.NumSkillUpsReward = quest.RewardSkillPoints;

            if (questGiver)
            {
                if (questGiver.IsGossip())
                    packet.LaunchGossip = true;
                else if (questGiver.IsQuestGiver())
                    packet.LaunchQuest = true;
                else if (quest.NextQuestInChain != 0 && !quest.HasFlag(QuestFlags.AutoComplete))
                    packet.UseQuestReward = true;
            }

            packet.HideChatMessage = hideChatMessage;

            SendPacket(packet);
        }

        public void SendQuestFailed(uint questId, InventoryResult reason = InventoryResult.Ok)
        {
            if (questId != 0)
            {
                var questGiverQuestFailed = new QuestGiverQuestFailed();
                questGiverQuestFailed.QuestID = questId;
                questGiverQuestFailed.Reason = reason; // failed reason (valid reasons: 4, 16, 50, 17, other values show default message)
                SendPacket(questGiverQuestFailed);
            }
        }

        public void SendQuestTimerFailed(uint questId)
        {
            if (questId != 0)
            {
                var questUpdateFailedTimer = new QuestUpdateFailedTimer();
                questUpdateFailedTimer.QuestID = questId;
                SendPacket(questUpdateFailedTimer);
            }
        }

        public void SendCanTakeQuestResponse(QuestFailedReasons reason, bool sendErrorMessage = true, string reasonText = "")
        {
            var questGiverInvalidQuest = new QuestGiverInvalidQuest();

            questGiverInvalidQuest.Reason = reason;
            questGiverInvalidQuest.SendErrorMessage = sendErrorMessage;
            questGiverInvalidQuest.ReasonText = reasonText;

            SendPacket(questGiverInvalidQuest);
        }

        public void SendQuestConfirmAccept(Quest quest, Player receiver)
        {
            if (!receiver)
                return;

            var packet = new QuestConfirmAcceptResponse();

            packet.QuestTitle = quest.LogTitle;

            var loc_idx = receiver.GetSession().GetSessionDbLocaleIndex();
            if (loc_idx != Locale.enUS)
            {
                var questLocale = Global.ObjectMgr.GetQuestLocale(quest.Id);
                if (questLocale != null)
                    ObjectManager.GetLocaleString(questLocale.LogTitle, loc_idx, ref packet.QuestTitle);
            }

            packet.QuestID = quest.Id;
            packet.InitiatedBy = GetGUID();

            receiver.SendPacket(packet);
        }

        public void SendPushToPartyResponse(Player player, QuestPushReason reason)
        {
            if (player != null)
            {
                var data = new QuestPushResultResponse();
                data.SenderGUID = player.GetGUID();
                data.Result = reason;
                SendPacket(data);
            }
        }

        private void SendQuestUpdateAddCredit(Quest quest, ObjectGuid guid, QuestObjective obj, uint count)
        {
            var packet = new QuestUpdateAddCredit();
            packet.VictimGUID = guid;
            packet.QuestID = quest.Id;
            packet.ObjectID = obj.ObjectID;
            packet.Count = (ushort)count;
            packet.Required = (ushort)obj.Amount;
            packet.ObjectiveType = (byte)obj.Type;
            SendPacket(packet);
        }

        public void SendQuestUpdateAddCreditSimple(QuestObjective obj)
        {
            var packet = new QuestUpdateAddCreditSimple();
            packet.QuestID = obj.QuestID;
            packet.ObjectID = obj.ObjectID;
            packet.ObjectiveType = obj.Type;
            SendPacket(packet);
        }

        public void SendQuestUpdateAddPlayer(Quest quest, uint newCount)
        {
            var packet = new QuestUpdateAddPvPCredit();
            packet.QuestID = quest.Id;
            packet.Count = (ushort)newCount;
            SendPacket(packet);
        }

        public void SendQuestGiverStatusMultiple()
        {
            var response = new QuestGiverStatusMultiple();

            foreach (var itr in m_clientGUIDs)
            {
                if (itr.IsAnyTypeCreature())
                {
                    // need also pet quests case support
                    var questgiver = ObjectAccessor.GetCreatureOrPetOrVehicle(this, itr);
                    if (!questgiver || questgiver.IsHostileTo(this))
                        continue;

                    if (!questgiver.HasNpcFlag(NPCFlags.QuestGiver))
                        continue;

                    response.QuestGiver.Add(new QuestGiverInfo(questgiver.GetGUID(), GetQuestDialogStatus(questgiver)));
                }
                else if (itr.IsGameObject())
                {
                    var questgiver = GetMap().GetGameObject(itr);
                    if (!questgiver || questgiver.GetGoType() != GameObjectTypes.QuestGiver)
                        continue;

                    response.QuestGiver.Add(new QuestGiverInfo(questgiver.GetGUID(), GetQuestDialogStatus(questgiver)));
                }
            }

            SendPacket(response);
        }

        public bool HasPvPForcingQuest()
        {
            for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            {
                var questId = GetQuestSlotQuestId(i);
                if (questId == 0)
                    continue;

                var quest = Global.ObjectMgr.GetQuestTemplate(questId);
                if (quest == null)
                    continue;

                if (quest.HasFlag(QuestFlags.Pvp))
                    return true;
            }

            return false;
        }

        public bool HasQuestForGO(int GOId)
        {
            for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            {
                var questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                var qs = m_QuestStatus.LookupByKey(questid);
                if (qs == null)
                    continue;

                if (qs.Status == QuestStatus.Incomplete)
                {
                    var qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                    if (qInfo == null)
                        continue;

                    if (GetGroup() != null && GetGroup().IsRaidGroup() && !qInfo.IsAllowedInRaid(GetMap().GetDifficultyID()))
                        continue;

                    foreach (var obj in qInfo.Objectives)
                    {
                        if (obj.Type != QuestObjectiveType.GameObject) //skip non GO case
                            continue;

                        if (GOId == obj.ObjectID && GetQuestObjectiveData(qInfo, obj.StorageIndex) < obj.Amount)
                            return true;
                    }
                }
            }
            return false;
        }

        public void UpdateForQuestWorldObjects()
        {
            if (m_clientGUIDs.Empty())
                return;

            var udata = new UpdateData(GetMapId());
            UpdateObject packet;
            foreach (var guid in m_clientGUIDs)
            {
                if (guid.IsGameObject())
                {
                    var obj = ObjectAccessor.GetGameObject(this, guid);
                    if (obj != null)
                    {
                        switch (obj.GetGoType())
                        {
                            case GameObjectTypes.QuestGiver:
                            case GameObjectTypes.Chest:
                            case GameObjectTypes.Goober:
                            case GameObjectTypes.Generic:
                                if (Global.ObjectMgr.IsGameObjectForQuests(obj.GetEntry()))
                                {
                                    var objMask = new ObjectFieldData();
                                    var goMask = new GameObjectFieldData();
                                    objMask.MarkChanged(obj.m_objectData.DynamicFlags);
                                    obj.BuildValuesUpdateForPlayerWithMask(udata, objMask._changesMask, goMask._changesMask, this);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                else if (guid.IsCreatureOrVehicle())
                {
                    var obj = ObjectAccessor.GetCreatureOrPetOrVehicle(this, guid);
                    if (obj == null)
                        continue;

                    // check if this unit requires quest specific flags
                    if (!obj.HasNpcFlag(NPCFlags.SpellClick))
                        continue;

                    var clickPair = Global.ObjectMgr.GetSpellClickInfoMapBounds(obj.GetEntry());
                    foreach (var spell in clickPair)
                    {
                        //! This code doesn't look right, but it was logically converted to condition system to do the exact
                        //! same thing it did before. It definitely needs to be overlooked for intended functionality.
                        var conds = Global.ConditionMgr.GetConditionsForSpellClickEvent(obj.GetEntry(), spell.spellId);
                        if (conds != null)
                        {
                            var buildUpdateBlock = false;
                            for (var i = 0; i < conds.Count && !buildUpdateBlock; ++i)
                                if (conds[i].ConditionType == ConditionTypes.QuestRewarded || conds[i].ConditionType == ConditionTypes.QuestTaken)
                                    buildUpdateBlock = true;

                            if (buildUpdateBlock)
                            {
                                var objMask = new ObjectFieldData();
                                var unitMask = new UnitData();
                                unitMask.MarkChanged(obj.m_unitData.NpcFlags, 0); // NpcFlags[0] has UNIT_NPC_FLAG_SPELLCLICK
                                obj.BuildValuesUpdateForPlayerWithMask(udata, objMask._changesMask, unitMask._changesMask, this);
                                break;
                            }
                        }
                    }
                }
            }
            udata.BuildPacket(out packet);
            SendPacket(packet);
        }

        private void SetDailyQuestStatus(uint quest_id)
        {
            var qQuest = Global.ObjectMgr.GetQuestTemplate(quest_id);
            if (qQuest != null)
            {
                if (!qQuest.IsDFQuest())
                {
                    AddDynamicUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.DailyQuestsCompleted), quest_id);
                    m_lastDailyQuestTime = Time.UnixTime;              // last daily quest time
                    m_DailyQuestChanged = true;

                }
                else
                {
                    m_DFQuests.Add(quest_id);
                    m_lastDailyQuestTime = Time.UnixTime;
                    m_DailyQuestChanged = true;
                }
            }
        }

        public bool IsDailyQuestDone(uint quest_id)
        {
            return m_activePlayerData.DailyQuestsCompleted.FindIndex(quest_id) >= 0;
        }

        private void SetWeeklyQuestStatus(uint quest_id)
        {
            m_weeklyquests.Add(quest_id);
            m_WeeklyQuestChanged = true;
        }

        private void SetSeasonalQuestStatus(uint quest_id)
        {
            var quest = Global.ObjectMgr.GetQuestTemplate(quest_id);
            if (quest == null)
                return;

            m_seasonalquests.Add(quest.GetEventIdForQuest(), quest_id);
            m_SeasonalQuestChanged = true;
        }

        private void SetMonthlyQuestStatus(uint quest_id)
        {
            m_monthlyquests.Add(quest_id);
            m_MonthlyQuestChanged = true;
        }

        private void PushQuests()
        {
            foreach (var quest in Global.ObjectMgr.GetQuestTemplatesAutoPush())
            {
                if (quest.GetQuestTag() != 0 && quest.GetQuestTag() != QuestTagType.Tag)
                    continue;

                if (!quest.IsUnavailable() && CanTakeQuest(quest, false))
                    AddQuestAndCheckCompletion(quest, null);
            }
        }
    }
}
