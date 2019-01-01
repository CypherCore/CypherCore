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
using Game.Conditions;
using Game.DataStorage;
using Game.Groups;
using Game.Mails;
using Game.Maps;
using Game.Misc;
using Game.Network.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Entities
{
    public partial class Player
    {
        public ObjectGuid GetDivider() { return m_divider; }
        public void SetDivider(ObjectGuid guid) { m_divider = guid; }

        uint GetInGameTime() { return m_ingametime; }
        public void SetInGameTime(uint time) { m_ingametime = time; }

        void AddTimedQuest(uint questId) { m_timedquests.Add(questId); }
        public void RemoveTimedQuest(uint questId) { m_timedquests.Remove(questId); }

        public List<uint> getRewardedQuests() { return m_RewardedQuests; }
        Dictionary<uint, QuestStatusData> getQuestStatusMap() { return m_QuestStatus; }

        public int GetQuestMinLevel(Quest quest)
        {
            if (quest.Level == -1 && quest.ScalingFactionGroup != 0)
            {
                ChrRacesRecord race = CliDB.ChrRacesStorage.LookupByKey(GetRace());
                FactionTemplateRecord raceFaction = CliDB.FactionTemplateStorage.LookupByKey(race.FactionID);
                if (raceFaction == null || raceFaction.FactionGroup != quest.ScalingFactionGroup)
                    return quest.MaxScalingLevel;
            }
            return quest.MinLevel;
        }

        public int GetQuestLevel(Quest quest)
        {
            if (quest == null)
                return 0;

            if (quest.Level == -1)
            {
                int minLevel = GetQuestMinLevel(quest);
                int maxLevel = quest.MaxScalingLevel;
                int level = (int)getLevel();
                if (level >= minLevel)
                    return Math.Min(level, maxLevel);
                return minLevel;
            }
            return quest.Level;
        }

        public int GetRewardedQuestCount() { return m_RewardedQuests.Count; }

        public void LearnQuestRewardedSpells(Quest quest)
        {
            //wtf why is rewardspell a uint if it can me -1
            int spell_id = Convert.ToInt32(quest.RewardSpell);
            uint src_spell_id = quest.SourceSpellID;

            // skip quests without rewarded spell
            if (spell_id == 0)
                return;

            // if RewSpellCast = -1 we remove aura do to SrcSpell from player.
            if (spell_id == -1 && src_spell_id != 0)
            {
                RemoveAurasDueToSpell(src_spell_id);
                return;
            }

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo((uint)spell_id);
            if (spellInfo == null)
                return;

            // check learned spells state
            bool found = false;
            foreach (SpellEffectInfo eff in spellInfo.GetEffectsForDifficulty(Difficulty.None))
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

            SpellEffectInfo effect = spellInfo.GetEffect(0);
            if (effect == null)
                return;

            uint learned_0 = effect.TriggerSpell;
            if (!HasSpell(learned_0))
            {
                SpellInfo learnedInfo = Global.SpellMgr.GetSpellInfo(learned_0);
                if (learnedInfo == null)
                    return;

                // profession specialization can be re-learned from npc
                if (learnedInfo.GetEffect(0).Effect == SpellEffectName.TradeSkill && learnedInfo.GetEffect(1).Effect == 0 && learnedInfo.SpellLevel == 0)
                    return;
            }

            CastSpell(this, (uint)spell_id, true);
        }

        public void LearnQuestRewardedSpells()
        {
            // learn spells received from quest completing
            foreach (var questId in m_RewardedQuests)
            {
                Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
                if (quest == null)
                    continue;

                LearnQuestRewardedSpells(quest);
            }
        }

        public void DailyReset()
        {
            foreach (uint questId in GetDynamicValues(ActivePlayerDynamicFields.DailyQuests))
            {
                uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questId);
                if (questBit != 0)
                    SetQuestCompletedBit(questBit, false);
            }

            DailyQuestsReset dailyQuestsReset = new DailyQuestsReset();
            dailyQuestsReset.Count = GetDynamicValues(ActivePlayerDynamicFields.DailyQuests).Length;
            SendPacket(dailyQuestsReset);

            ClearDynamicValue(ActivePlayerDynamicFields.DailyQuests);

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

            foreach (uint questId in m_weeklyquests)
            {
                uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questId);
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

            foreach (uint questId in eventList)
            {
                uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questId);
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

            foreach (uint questId in m_monthlyquests)
            {
                uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questId);
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
                    return GetNPCIfCanInteractWith(questGiver.GetGUID(), NPCFlags.QuestGiver) != null;
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
            Creature creature = ObjectAccessor.GetCreatureOrPetOrVehicle(this, guid);
            if (creature != null)
            {
                objectQR = Global.ObjectMgr.GetCreatureQuestRelationBounds(creature.GetEntry());
                objectQIR = Global.ObjectMgr.GetCreatureQuestInvolvedRelationBounds(creature.GetEntry());
            }
            else
            {
                //we should obtain map from GetMap() in 99% of cases. Special case
                //only for quests which cast teleport spells on player
                Map _map = IsInWorld ? GetMap() : Global.MapMgr.FindMap(GetMapId(), GetInstanceId());
                Cypher.Assert(_map != null);
                GameObject gameObject = _map.GetGameObject(guid);
                if (gameObject != null)
                {
                    objectQR = Global.ObjectMgr.GetGOQuestRelationBounds(gameObject.GetEntry());
                    objectQIR = Global.ObjectMgr.GetGOQuestInvolvedRelationBounds(gameObject.GetEntry());
                }
                else
                    return;
            }

            QuestMenu qm = PlayerTalkClass.GetQuestMenu();
            qm.ClearMenu();

            foreach (var quest_id in objectQIR)
            {
                QuestStatus status = GetQuestStatus(quest_id);
                if (status == QuestStatus.Complete)
                    qm.AddMenuItem(quest_id, 4);
                else if (status == QuestStatus.Incomplete)
                    qm.AddMenuItem(quest_id, 4);
            }

            foreach (var quest_id in objectQR)
            {
                Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);
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
            QuestMenu questMenu = PlayerTalkClass.GetQuestMenu();
            if (questMenu.IsEmpty())
                return;

            // single element case
            if (questMenu.GetMenuItemCount() == 1)
            {
                QuestMenuItem qmi0 = questMenu.GetItem(0);
                uint questId = qmi0.QuestId;

                // Auto open
                Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
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
                        if (!source.hasQuest(questId) && !source.hasInvolvedQuest(questId))
                        {
                            PlayerTalkClass.SendCloseGossip();
                            return;
                        }

                        if (!source.IsTypeId(TypeId.Unit) || source.HasFlag64(UnitFields.NpcFlags, NPCFlags.Gossip))
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
            uint nextQuestID = quest.NextQuestInChain;

            switch (guid.GetHigh())
            {
                case HighGuid.Player:
                    Cypher.Assert(quest.HasFlag(QuestFlags.AutoComplete));
                    return Global.ObjectMgr.GetQuestTemplate(nextQuestID);
                case HighGuid.Creature:
                case HighGuid.Pet:
                case HighGuid.Vehicle:
                    {
                        Creature creature = ObjectAccessor.GetCreatureOrPetOrVehicle(this, guid);
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
                        Map _map = IsInWorld ? GetMap() : Global.MapMgr.FindMap(GetMapId(), GetInstanceId());
                        Cypher.Assert(_map != null);
                        GameObject gameObject = _map.GetGameObject(guid);
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
                SatisfyQuestPreviousQuest(quest, false) && SatisfyQuestNextChain(quest, false) &&
                SatisfyQuestPrevChain(quest, false) && SatisfyQuestDay(quest, false) && SatisfyQuestWeek(quest, false) &&
                SatisfyQuestMonth(quest, false) && SatisfyQuestSeasonal(quest, false))
            {
                return getLevel() + WorldConfig.GetIntValue(WorldCfg.QuestHighLevelHideDiff) >= GetQuestMinLevel(quest);
            }

            return false;
        }

        public bool CanTakeQuest(Quest quest, bool msg)
        {
            return !Global.DisableMgr.IsDisabledFor(DisableType.Quest, quest.Id, this)
                && SatisfyQuestStatus(quest, msg) && SatisfyQuestExclusiveGroup(quest, msg)
                && SatisfyQuestClass(quest, msg) && SatisfyQuestRace(quest, msg) && SatisfyQuestLevel(quest, msg)
                && SatisfyQuestSkill(quest, msg) && SatisfyQuestReputation(quest, msg)
                && SatisfyQuestPreviousQuest(quest, msg) && SatisfyQuestTimed(quest, msg)
                && SatisfyQuestNextChain(quest, msg) && SatisfyQuestPrevChain(quest, msg)
                && SatisfyQuestDay(quest, msg) && SatisfyQuestWeek(quest, msg)
                && SatisfyQuestMonth(quest, msg) && SatisfyQuestSeasonal(quest, msg)
                && SatisfyQuestConditions(quest, msg);
        }

        public bool CanAddQuest(Quest quest, bool msg)
        {
            if (!SatisfyQuestLog(msg))
                return false;

            uint srcitem = quest.SourceItemId;
            if (srcitem > 0)
            {
                uint count = quest.SourceItemIdCount;
                List<ItemPosCount> dest = new List<ItemPosCount>();
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
                Quest qInfo = Global.ObjectMgr.GetQuestTemplate(quest_id);
                if (qInfo == null)
                    return false;

                if (!qInfo.IsRepeatable() && m_RewardedQuests.Contains(quest_id))
                    return false;                                   // not allow re-complete quest

                // auto complete quest
                if ((qInfo.IsAutoComplete() || qInfo.Flags.HasAnyFlag(QuestFlags.AutoComplete)) && CanTakeQuest(qInfo, false))
                    return true;

                var q_status = m_QuestStatus.LookupByKey(quest_id);
                if (q_status == null)
                    return false;

                if (q_status.Status == QuestStatus.Incomplete)
                {
                    foreach (QuestObjective obj in qInfo.Objectives)
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
                foreach (QuestObjective obj in quest.Objectives)
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
                foreach (QuestObjective obj in quest.Objectives)
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

            foreach (QuestObjective obj in quest.Objectives)
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

        public void AddQuestAndCheckCompletion(Quest quest, WorldObject questGiver)
        {
            AddQuest(quest, questGiver);

            foreach (QuestObjective obj in quest.Objectives)
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
                    Global.ScriptMgr.OnQuestAccept(this, (questGiver.ToCreature()), quest);
                    questGiver.ToCreature().GetAI().sQuestAccept(this, quest);
                    break;
                case TypeId.Item:
                case TypeId.Container:
                    {
                        Item item = (Item)questGiver;
                        Global.ScriptMgr.OnQuestAccept(this, item, quest);

                        // destroy not required for quest finish quest starting item
                        bool destroyItem = true;
                        foreach (QuestObjective obj in quest.Objectives)
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
                    Global.ScriptMgr.OnQuestAccept(this, questGiver.ToGameObject(), quest);
                    questGiver.ToGameObject().GetAI().QuestAccept(this, quest);
                    break;
                default:
                    break;
            }
        }

        public bool CanRewardQuest(Quest quest, uint reward, bool msg)
        {
            // prevent receive reward with quest items in bank or for not completed quest
            if (!CanRewardQuest(quest, msg))
                return false;

            List<ItemPosCount> dest = new List<ItemPosCount>();
            if (quest.GetRewChoiceItemsCount() > 0)
            {
                for (uint i = 0; i < quest.GetRewChoiceItemsCount(); ++i)
                {
                    if (quest.RewardChoiceItemId[i] != 0 && quest.RewardChoiceItemId[i] == reward)
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
                bool hasFilteredQuestPackageReward = false;
                var questPackageItems = Global.DB2Mgr.GetQuestPackageItems(quest.PackageID);
                if (questPackageItems != null)
                {
                    foreach (var questPackageItem in questPackageItems)
                    {
                        if (questPackageItem.ItemID != reward)
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
                    List<QuestPackageItemRecord> questPackageItems1 = Global.DB2Mgr.GetQuestPackageItemsFallback(quest.PackageID);
                    if (questPackageItems1 != null)
                    {
                        foreach (QuestPackageItemRecord questPackageItem in questPackageItems1)
                        {
                            if (questPackageItem.ItemID != reward)
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

        public void AddQuest(Quest quest, WorldObject questGiver)
        {
            ushort log_slot = FindQuestSlot(0);

            if (log_slot >= SharedConst.MaxQuestLogSize) // Player does not have any free slot in the quest log
                return;

            uint quest_id = quest.Id;

            if (!m_QuestStatus.ContainsKey(quest_id))
                m_QuestStatus[quest_id] = new QuestStatusData();

            QuestStatusData questStatusData = m_QuestStatus.LookupByKey(quest_id);
            QuestStatus oldStatus = questStatusData.Status;

            // check for repeatable quests status reset
            questStatusData.Status = QuestStatus.Incomplete;

            int maxStorageIndex = 0;
            foreach (QuestObjective obj in quest.Objectives)
                if (obj.StorageIndex > maxStorageIndex)
                    maxStorageIndex = obj.StorageIndex;

            questStatusData.ObjectiveData = new int[maxStorageIndex + 1];

            GiveQuestSourceItem(quest);
            AdjustQuestReqItemCount(quest);

            foreach (QuestObjective obj in quest.Objectives)
            {
                switch (obj.Type)
                {
                    case QuestObjectiveType.MinReputation:
                    case QuestObjectiveType.MaxReputation:
                        FactionRecord factionEntry = CliDB.FactionStorage.LookupByKey(obj.ObjectID);
                        if (factionEntry != null)
                            GetReputationMgr().SetVisible(factionEntry);
                        break;
                    case QuestObjectiveType.CriteriaTree:
                        if (quest.HasFlagEx(QuestFlagsEx.ClearProgressOfCriteriaTreeObjectivesOnAccept))
                            m_questObjectiveCriteriaMgr.ResetCriteriaTree((uint)obj.ObjectID);
                        break;
                    default:
                        break;
                }
            }

            uint qtime = 0;
            if (quest.HasSpecialFlag(QuestSpecialFlags.Timed))
            {
                uint limittime = quest.LimitTime;

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

            SetQuestSlot(log_slot, quest_id, qtime);

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

                ushort log_slot = FindQuestSlot(quest_id);
                if (log_slot < SharedConst.MaxQuestLogSize)
                    SetQuestSlotState(log_slot, QuestSlotStateMask.Complete);

                Quest qInfo = Global.ObjectMgr.GetQuestTemplate(quest_id);
                if (qInfo != null)
                    if (qInfo.HasFlag(QuestFlags.Tracking))
                        RewardQuest(qInfo, 0, this, false);
            }
        }

        public void IncompleteQuest(uint quest_id)
        {
            if (quest_id != 0)
            {
                SetQuestStatus(quest_id, QuestStatus.Incomplete);

                ushort log_slot = FindQuestSlot(quest_id);
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
            bool rewarded = m_RewardedQuests.Contains(quest.Id);

            // Not give XP in case already completed once repeatable quest
            if (rewarded && !quest.IsDFQuest())
                return 0;

            uint XP = (uint)(quest.XPValue(this) * WorldConfig.GetFloatValue(WorldCfg.RateXpQuest));

            // handle SPELL_AURA_MOD_XP_QUEST_PCT auras
            var ModXPPctAuras = GetAuraEffectsByType(AuraType.ModXpQuestPct);
            foreach (var eff in ModXPPctAuras)
                MathFunctions.AddPct(ref XP, eff.GetAmount());

            return XP;
        }

        public bool CanSelectQuestPackageItem(QuestPackageItemRecord questPackageItem)
        {
            ItemTemplate rewardProto = Global.ObjectMgr.GetItemTemplate(questPackageItem.ItemID);
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
                    return rewardProto.ItemSpecClassMask == 0 || (rewardProto.ItemSpecClassMask & getClassMask()) != 0;
                case QuestPackageFilter.Everyone:
                    return true;
                default:
                    break;
            }

            return false;
        }

        public void RewardQuestPackage(uint questPackageId, uint onlyItemId = 0)
        {
            bool hasFilteredQuestPackageReward = false;
            var questPackageItems = Global.DB2Mgr.GetQuestPackageItems(questPackageId);
            if (questPackageItems != null)
            {
                foreach (QuestPackageItemRecord questPackageItem in questPackageItems)
                {
                    if (onlyItemId != 0 && questPackageItem.ItemID != onlyItemId)
                        continue;

                    if (CanSelectQuestPackageItem(questPackageItem))
                    {
                        hasFilteredQuestPackageReward = true;
                        List<ItemPosCount> dest = new List<ItemPosCount>();
                        if (CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, questPackageItem.ItemID, questPackageItem.ItemQuantity) == InventoryResult.Ok)
                        {
                            Item item = StoreNewItem(dest, questPackageItem.ItemID, true, ItemEnchantment.GenerateItemRandomPropertyId(questPackageItem.ItemID));
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
                    foreach (QuestPackageItemRecord questPackageItem in questPackageItemsFallback)
                    {
                        if (onlyItemId != 0 && questPackageItem.ItemID != onlyItemId)
                            continue;

                        List<ItemPosCount> dest = new List<ItemPosCount>();
                        if (CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, questPackageItem.ItemID, questPackageItem.ItemQuantity) == InventoryResult.Ok)
                        {
                            Item item = StoreNewItem(dest, questPackageItem.ItemID, true, ItemEnchantment.GenerateItemRandomPropertyId(questPackageItem.ItemID));
                            SendNewItem(item, questPackageItem.ItemQuantity, true, false);
                        }
                    }
                }
            }
        }

        public void RewardQuest(Quest quest, uint reward, WorldObject questGiver, bool announce = true)
        {
            //this THING should be here to protect code from quest, which cast on player far teleport as a reward
            //should work fine, cause far teleport will be executed in Update()
            SetCanDelayTeleport(true);

            uint quest_id = quest.Id;
            QuestStatus oldStatus = GetQuestStatus(quest_id);

            foreach (QuestObjective obj in quest.Objectives)
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
                        uint count = quest.ItemDropQuantity[i];
                        DestroyItemCount(quest.ItemDrop[i], count != 0 ? count : 9999, true);
                    }
                }
            }

            RemoveTimedQuest(quest_id);

            ItemTemplate rewardProto = Global.ObjectMgr.GetItemTemplate(reward);
            if (rewardProto != null && quest.GetRewChoiceItemsCount() != 0)
            {
                for (uint i = 0; i < quest.GetRewChoiceItemsCount(); ++i)
                {
                    if (quest.RewardChoiceItemId[i] != 0 && quest.RewardChoiceItemId[i] == reward)
                    {
                        List<ItemPosCount> dest = new List<ItemPosCount>();
                        if (CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, reward, quest.RewardChoiceItemCount[i]) == InventoryResult.Ok)
                        {
                            Item item = StoreNewItem(dest, reward, true, ItemEnchantment.GenerateItemRandomPropertyId(reward));
                            SendNewItem(item, quest.RewardChoiceItemCount[i], true, false);
                        }
                    }
                }
            }

            // QuestPackageItem.db2
            if (rewardProto != null && quest.PackageID != 0)
                RewardQuestPackage(quest.PackageID, reward);

            if (quest.GetRewItemsCount() > 0)
            {
                for (uint i = 0; i < quest.GetRewItemsCount(); ++i)
                {
                    uint itemId = quest.RewardItemId[i];
                    if (itemId != 0)
                    {
                        List<ItemPosCount> dest = new List<ItemPosCount>();
                        if (CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemId, quest.RewardItemCount[i]) == InventoryResult.Ok)
                        {
                            Item item = StoreNewItem(dest, itemId, true, ItemEnchantment.GenerateItemRandomPropertyId(itemId));
                            SendNewItem(item, quest.RewardItemCount[i], true, false);
                        }
                        else if (quest.IsDFQuest())
                            SendItemRetrievalMail(quest.RewardItemId[i], quest.RewardItemCount[i]);
                    }
                }
            }

            for (byte i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
            {
                if (quest.RewardCurrencyId[i] != 0)
                    ModifyCurrency((CurrencyTypes)quest.RewardCurrencyId[i], (int)quest.RewardCurrencyCount[i]);
            }

            uint skill = quest.RewardSkillId;
            if (skill != 0)
                UpdateSkillPro(skill, 1000, quest.RewardSkillPoints);

            RewardReputation(quest);

            ushort log_slot = FindQuestSlot(quest_id);
            if (log_slot < SharedConst.MaxQuestLogSize)
                SetQuestSlot(log_slot, 0);

            uint XP = GetQuestXPReward(quest);

            int moneyRew = 0;
            if (getLevel() < WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
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
            uint honor = quest.CalculateHonorGain(getLevel());
            if (honor != 0)
                RewardHonor(null, 0, (int)honor);

            // title reward
            if (quest.RewardTitleId != 0)
            {
                CharTitlesRecord titleEntry = CliDB.CharTitlesStorage.LookupByKey(quest.RewardTitleId);
                if (titleEntry != null)
                    SetTitle(titleEntry);
            }

            // Send reward mail
            uint mail_template_id = quest.RewardMailTemplateId;
            if (mail_template_id != 0)
            {
                SQLTransaction trans = new SQLTransaction();
                // @todo Poor design of mail system
                uint questMailSender = quest.RewardMailSenderEntry;
                if (questMailSender != 0)
                    new MailDraft(mail_template_id).SendMailTo(trans, this, new MailSender(questMailSender), MailCheckMask.HasBody, quest.RewardMailDelay);
                else
                    new MailDraft(mail_template_id).SendMailTo(trans, this, new MailSender(questGiver), MailCheckMask.HasBody, quest.RewardMailDelay);
                DB.Characters.CommitTransaction(trans);
            }

            if (quest.IsDaily() || quest.IsDFQuest())
            {
                SetDailyQuestStatus(quest_id);
                if (quest.IsDaily())
                {
                    UpdateCriteria(CriteriaTypes.CompleteDailyQuest, quest_id);
                    UpdateCriteria(CriteriaTypes.CompleteDailyQuestDaily, quest_id);
                }
            }
            else if (quest.IsWeekly())
                SetWeeklyQuestStatus(quest_id);
            else if (quest.IsMonthly())
                SetMonthlyQuestStatus(quest_id);
            else if (quest.IsSeasonal())
                SetSeasonalQuestStatus(quest_id);

            RemoveActiveQuest(quest_id, false);
            if (quest.CanIncreaseRewardedQuestCounters())
                SetRewardedQuest(quest_id);

            // StoreNewItem, mail reward, etc. save data directly to the database
            // to prevent exploitable data desynchronisation we save the quest status to the database too
            // (to prevent rewarding this quest another time while rewards were already given out)
            _SaveQuestStatus(null);

            SendQuestReward(quest, questGiver?.ToCreature(), XP, !announce);

            // cast spells after mark quest complete (some spells have quest completed state requirements in spell_area data)
            if (quest.RewardSpell > 0)
            {
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(quest.RewardSpell);
                if (questGiver && questGiver.isTypeMask(TypeMask.Unit) 
                    && !spellInfo.HasEffect(Difficulty.None, SpellEffectName.LearnSpell) 
                    && !spellInfo.HasEffect(Difficulty.None, SpellEffectName.CreateItem) 
                    && !spellInfo.HasEffect(Difficulty.None, SpellEffectName.ApplyAura))
                {
                    Unit unit = questGiver.ToUnit();
                    if (unit)
                        unit.CastSpell(this, quest.RewardSpell, true);
                }
                else
                    CastSpell(this, quest.RewardSpell, true);
            }
            else
            {
                for (uint i = 0; i < SharedConst.QuestRewardDisplaySpellCount; ++i)
                {
                    if (quest.RewardDisplaySpell[i] > 0)
                    {
                        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(quest.RewardDisplaySpell[i]);
                        if (questGiver && questGiver.IsTypeId(TypeId.Unit)
                            && !spellInfo.HasEffect(Difficulty.None, SpellEffectName.LearnSpell)
                            && !spellInfo.HasEffect(Difficulty.None, SpellEffectName.CreateItem))
                        {
                            Unit unit = questGiver.ToUnit();
                            if (unit)
                                unit.CastSpell(this, quest.RewardDisplaySpell[i], true);
                        }
                        else
                            CastSpell(this, quest.RewardDisplaySpell[i], true);
                    }
                }
            }

            if (quest.QuestSortID > 0)
                UpdateCriteria(CriteriaTypes.CompleteQuestsInZone, (ulong)quest.QuestSortID);

            UpdateCriteria(CriteriaTypes.CompleteQuestCount);
            UpdateCriteria(CriteriaTypes.CompleteQuest, quest.Id);

            uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);
            if (questBit != 0)
                SetQuestCompletedBit(questBit, true);

            if (quest.HasFlag(QuestFlags.Pvp))
            {
                pvpInfo.IsHostile = pvpInfo.IsInHostileArea || HasPvPForcingQuest();
                UpdatePvPState();
            }

            SendQuestUpdate(quest_id);
            SendQuestGiverStatusMultiple();

            //lets remove flag for delayed teleports
            SetCanDelayTeleport(false);

            Global.ScriptMgr.OnQuestStatusChange(this, quest_id);
            Global.ScriptMgr.OnQuestStatusChange(this, quest, oldStatus, QuestStatus.Rewarded);
        }

        public void SetRewardedQuest(uint quest_id)
        {
            m_RewardedQuests.Add(quest_id);
            m_RewardedQuestsSave[quest_id] = QuestSaveType.Default;
        }

        public void FailQuest(uint questId)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
            if (quest != null)
            {
                // Already complete quests shouldn't turn failed.
                if (GetQuestStatus(questId) == QuestStatus.Complete && !quest.HasSpecialFlag(QuestSpecialFlags.Timed))
                    return;

                // You can't fail a quest if you don't have it, or if it's already rewarded.
                if (GetQuestStatus(questId) == QuestStatus.None || GetQuestStatus(questId) == QuestStatus.Rewarded)
                    return;

                SetQuestStatus(questId, QuestStatus.Failed);

                ushort log_slot = FindQuestSlot(questId);

                if (log_slot < SharedConst.MaxQuestLogSize)
                {
                    SetQuestSlotTimer(log_slot, 1);
                    SetQuestSlotState(log_slot, QuestSlotStateMask.Fail);
                }

                if (quest.HasSpecialFlag(QuestSpecialFlags.Timed))
                {
                    QuestStatusData q_status = m_QuestStatus[questId];

                    RemoveTimedQuest(questId);
                    q_status.Timer = 0;

                    SendQuestTimerFailed(questId);
                }
                else
                    SendQuestFailed(questId);

                // Destroy quest items on quest failure.
                foreach (QuestObjective obj in quest.Objectives)
                {
                    if (obj.Type == QuestObjectiveType.Item)
                    {
                        ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate((uint)obj.ObjectID);
                        if (itemTemplate != null)
                            if (itemTemplate.GetBonding() == ItemBondingType.Quest)
                                DestroyItemCount((uint)obj.ObjectID, (uint)obj.Amount, true, true);
                    }
                }

                // Destroy items received during the quest.
                for (byte i = 0; i < SharedConst.QuestItemDropCount; ++i)
                {
                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(quest.ItemDrop[i]);
                    if (itemTemplate != null)
                        if (quest.ItemDropQuantity[i] != 0 && itemTemplate.GetBonding() == ItemBondingType.Quest)
                            DestroyItemCount(quest.ItemDrop[i], quest.ItemDropQuantity[i], true, true);
                }
            }
        }

        public void AbandonQuest(uint questId)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
            if (quest != null)
            {
                // Destroy quest items on quest abandon.
                foreach (QuestObjective obj in quest.Objectives)
                {
                    if (obj.Type == QuestObjectiveType.Item)
                    {
                        ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate((uint)obj.ObjectID);
                        if (itemTemplate != null)
                            if (itemTemplate.GetBonding() == ItemBondingType.Quest)
                                DestroyItemCount((uint)obj.ObjectID, (uint)obj.Amount, true, true);
                    }
                }

                // Destroy items received during the quest.
                for (byte i = 0; i < SharedConst.QuestItemDropCount; ++i)
                {
                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(quest.ItemDrop[i]);
                    if (itemTemplate != null)
                        if (quest.ItemDropQuantity[i] != 0 && itemTemplate.GetBonding() == ItemBondingType.Quest)
                            DestroyItemCount(quest.ItemDrop[i], quest.ItemDropQuantity[i], true, true);
                }
            }
        }

        public bool SatisfyQuestSkill(Quest qInfo, bool msg)
        {
            uint skill = qInfo.RequiredSkillId;

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
            if (getLevel() < GetQuestMinLevel(qInfo))
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.FailedLowLevel);
                    Log.outDebug(LogFilter.Server, "SatisfyQuestLevel: Sent QuestFailedReasons.FailedLowLevel (questId: {0}) because player does not have required (min) level.", qInfo.Id);
                }
                return false;
            }

            if (qInfo.MaxLevel > 0 && getLevel() > qInfo.MaxLevel)
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

        public bool SatisfyQuestPreviousQuest(Quest qInfo, bool msg)
        {
            // No previous quest (might be first quest in a series)
            if (qInfo.prevQuests.Empty())
                return true;

            foreach (var prev in qInfo.prevQuests)
            {
                uint prevId = (uint)Math.Abs(prev);

                Quest qPrevInfo = Global.ObjectMgr.GetQuestTemplate(prevId);

                if (qPrevInfo != null)
                {
                    // If any of the positive previous quests completed, return true
                    if (prev > 0 && m_RewardedQuests.Contains(prevId))
                    {
                        // skip one-from-all exclusive group
                        if (qPrevInfo.ExclusiveGroup >= 0)
                            return true;

                        // each-from-all exclusive group (< 0)
                        // can be start if only all quests in prev quest exclusive group completed and rewarded
                        var range = Global.ObjectMgr._exclusiveQuestGroups.LookupByKey(qPrevInfo.ExclusiveGroup);
                        foreach (var exclude_Id in range)
                        {
                            // skip checked quest id, only state of other quests in group is interesting
                            if (exclude_Id == prevId)
                                continue;

                            // alternative quest from group also must be completed and rewarded (reported)
                            if (!m_RewardedQuests.Contains(exclude_Id))
                            {
                                if (msg)
                                {
                                    SendCanTakeQuestResponse(QuestFailedReasons.None);
                                    Log.outDebug(LogFilter.Server, "SatisfyQuestPreviousQuest: Sent QuestFailedReason.None (questId: {0}) because player does not have required quest (1).", qInfo.Id);
                                }
                                return false;
                            }
                        }
                        return true;
                    }

                    // If any of the negative previous quests active, return true
                    if (prev < 0 && GetQuestStatus(prevId) != QuestStatus.None)
                    {
                        // skip one-from-all exclusive group
                        if (qPrevInfo.ExclusiveGroup >= 0)
                            return true;

                        // each-from-all exclusive group (< 0)
                        // can be start if only all quests in prev quest exclusive group active
                        var range = Global.ObjectMgr._exclusiveQuestGroups.LookupByKey(qPrevInfo.ExclusiveGroup);
                        foreach (var exclude_Id in range)
                        {
                            // skip checked quest id, only state of other quests in group is interesting
                            if (exclude_Id == prevId)
                                continue;

                            // alternative quest from group also must be active
                            if (GetQuestStatus(exclude_Id) != QuestStatus.None)
                            {
                                if (msg)
                                {
                                    SendCanTakeQuestResponse(QuestFailedReasons.None);
                                    Log.outDebug(LogFilter.Server, "SatisfyQuestPreviousQuest: Sent QuestFailedReason.None (questId: {0}) because player does not have required quest (2).", qInfo.Id);

                                }
                                return false;
                            }
                        }
                        return true;
                    }
                }
            }

            // Has only positive prev. quests in non-rewarded state
            // and negative prev. quests in non-active state
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.None);
                Log.outDebug(LogFilter.Server, "SatisfyQuestPreviousQuest: Sent QuestFailedReason.None (questId: {0}) because player does not have required quest (3).", qInfo.Id);
            }

            return false;
        }

        public bool SatisfyQuestClass(Quest qInfo, bool msg)
        {
            uint reqClass = qInfo.AllowableClasses;

            if (reqClass == 0)
                return true;

            if ((reqClass & getClassMask()) == 0)
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
            long reqraces = qInfo.AllowableRaces;
            if (reqraces == -1)
                return true;

            if ((reqraces & (long)getRaceMask()) == 0)
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
            uint fIdMin = qInfo.RequiredMinRepFaction;      //Min required rep
            if (fIdMin != 0 && GetReputationMgr().GetReputation(fIdMin) < qInfo.RequiredMinRepValue)
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.None);
                    Log.outDebug(LogFilter.Server, "SatisfyQuestReputation: Sent QuestFailedReason.None (questId: {0}) because player does not have required reputation (min).", qInfo.Id);
                }
                return false;
            }

            uint fIdMax = qInfo.RequiredMaxRepFaction;      //Max required rep
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

            var range = Global.ObjectMgr._exclusiveQuestGroups.LookupByKey(qInfo.ExclusiveGroup);
            // always must be found if qInfo.ExclusiveGroup != 0

            foreach (var exclude_Id in range)
            {
                // skip checked quest id, only state of other quests in group is interesting
                if (exclude_Id == qInfo.Id)
                    continue;

                // not allow have daily quest if daily quest from exclusive group already recently completed
                Quest Nquest = Global.ObjectMgr.GetQuestTemplate(exclude_Id);
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
                if (GetQuestStatus(exclude_Id) != QuestStatus.None || (!(qInfo.IsRepeatable() && Nquest.IsRepeatable()) && m_RewardedQuests.Contains(exclude_Id)))
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

        public bool SatisfyQuestNextChain(Quest qInfo, bool msg)
        {
            uint nextQuest = qInfo.NextQuestInChain;
            if (nextQuest == 0)
                return true;

            // next quest in chain already started or completed
            if (GetQuestStatus(nextQuest) != QuestStatus.None) // GetQuestStatus returns QuestStatus.CompleteD for rewarded quests
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.None);
                    Log.outDebug(LogFilter.Server, "SatisfyQuestNextChain: Sent QuestFailedReason.None (questId: {0}) because player already did or started next quest in chain.", qInfo.Id);
                }
                return false;
            }
            return true;
        }

        public bool SatisfyQuestPrevChain(Quest qInfo, bool msg)
        {
            // No previous quest in chain
            if (qInfo.prevChainQuests.Empty())
                return true;

            foreach (var questId in qInfo.prevChainQuests)
            {
                var questStatusData = m_QuestStatus.LookupByKey(questId);

                // If any of the previous quests in chain active, return false
                if (questStatusData != null && questStatusData.Status != QuestStatus.None)
                {
                    if (msg)
                    {
                        SendCanTakeQuestResponse(QuestFailedReasons.None);
                        Log.outDebug(LogFilter.Server, "SatisfyQuestNextChain: Sent QuestFailedReason.None (questId: {0}) because player already did or started next quest in chain.", qInfo.Id);
                    }
                    return false;
                }
            }

            // No previous quest in chain active
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

            var dailies = GetDynamicValues(ActivePlayerDynamicFields.DailyQuests);
            foreach (var dailyQuestId in dailies)
                if (dailyQuestId == qInfo.Id)
                    return false;

            return true;
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

            ushort eventId = Global.GameEventMgr.GetEventIdForQuest(qInfo);
            if (!m_seasonalquests.ContainsKey(eventId) || m_seasonalquests[eventId].Empty())
                return true;

            // if not found in cooldown list
            return !m_seasonalquests[eventId].Contains(qInfo.Id);
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
            uint srcitem = quest.SourceItemId;
            if (srcitem > 0)
            {
                uint count = quest.SourceItemIdCount;
                if (count <= 0)
                    count = 1;

                List<ItemPosCount> dest = new List<ItemPosCount>();
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
            Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
            if (quest != null)
            {
                uint srcItemId = quest.SourceItemId;
                ItemTemplate item = Global.ObjectMgr.GetItemTemplate(srcItemId);

                if (srcItemId > 0)
                {
                    uint count = quest.SourceItemIdCount;
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

                    bool destroyItem = true;
                    if (item.GetStartQuest() == questId)
                    {
                        foreach (QuestObjective obj in quest.Objectives)
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
            Quest qInfo = Global.ObjectMgr.GetQuestTemplate(quest_id);
            if (qInfo != null)
            {
                if (qInfo.IsSeasonal() && !qInfo.IsRepeatable())
                {
                    ushort eventId = Global.GameEventMgr.GetEventIdForQuest(qInfo);
                    if (m_seasonalquests.ContainsKey(eventId))
                        return m_seasonalquests[eventId].Contains(quest_id);

                    return false;
                }

                // for repeatable quests: rewarded field is set after first reward only to prevent getting XP more than once
                if (!qInfo.IsRepeatable())
                    return m_RewardedQuests.Contains(quest_id);

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

                Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
                if (quest != null)
                {
                    if (quest.IsSeasonal() && !quest.IsRepeatable())
                    {
                        ushort eventId = Global.GameEventMgr.GetEventIdForQuest(quest);
                        if (!m_seasonalquests.ContainsKey(eventId) || !m_seasonalquests[eventId].Contains(questId))
                            return QuestStatus.None;
                    }
                    if (!quest.IsRepeatable() && m_RewardedQuests.Contains(questId))
                        return QuestStatus.Rewarded;
                }
            }
            return QuestStatus.None;
        }

        public bool CanShareQuest(uint quest_id)
        {
            Quest qInfo = Global.ObjectMgr.GetQuestTemplate(quest_id);
            return qInfo != null && qInfo.HasFlag(QuestFlags.Sharable) && IsActiveQuest(quest_id);
        }

        public void SetQuestStatus(uint questId, QuestStatus status, bool update = true)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
            if (quest != null)
            {
                QuestStatus oldStatus = m_QuestStatus[questId].Status;
                if (!m_QuestStatus.ContainsKey(questId))
                    m_QuestStatus[questId] = new QuestStatusData();

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

            uint questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(questId);
            if (questBit != 0)
                SetQuestCompletedBit(questBit, false);

            if (update)
                SendQuestUpdate(questId);
        }

        void SendQuestUpdate(uint questid)
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
                        QuestGiverStatus questStatus = (QuestGiverStatus)Global.ScriptMgr.GetDialogStatus(this, questgiver.ToGameObject());
                        if (questStatus != QuestGiverStatus.ScriptedNoStatus)
                            return questStatus;
                        qr = Global.ObjectMgr.GetGOQuestRelationBounds(questgiver.GetEntry());
                        qir = Global.ObjectMgr.GetGOQuestInvolvedRelationBounds(questgiver.GetEntry());
                        break;
                    }
                case TypeId.Unit:
                    {
                        QuestGiverStatus questStatus = (QuestGiverStatus)Global.ScriptMgr.GetDialogStatus(this, questgiver.ToCreature());
                        if (questStatus != QuestGiverStatus.ScriptedNoStatus)
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

            QuestGiverStatus result = QuestGiverStatus.None;

            foreach (var questId in qir)
            {
                QuestGiverStatus result2 = QuestGiverStatus.None;
                Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
                if (quest == null)
                    continue;

                if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.QuestAvailable, quest.Id, this))
                    continue;

                QuestStatus status = GetQuestStatus(questId);
                if (status == QuestStatus.Complete && !GetQuestRewardStatus(questId))
                    result2 = QuestGiverStatus.Reward;
                else if (status == QuestStatus.Incomplete)
                    result2 = QuestGiverStatus.Incomplete;

                if (quest.IsAutoComplete() && CanTakeQuest(quest, false) && quest.IsRepeatable() && !quest.IsDailyOrWeekly())
                    result2 = QuestGiverStatus.RewardRep;

                if (result2 > result)
                    result = result2;
            }

            foreach (var questId in qr)
            {
                QuestGiverStatus result2 = QuestGiverStatus.None;
                Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
                if (quest == null)
                    continue;

                if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.QuestAvailable, quest.Id, this))
                    continue;

                QuestStatus status = GetQuestStatus(questId);
                if (status == QuestStatus.None)
                {
                    if (CanSeeStartQuest(quest))
                    {
                        if (SatisfyQuestLevel(quest, false))
                        {
                            if (getLevel() <= (GetQuestLevel(quest) + WorldConfig.GetIntValue(WorldCfg.QuestLowLevelHideDiff)))
                            {
                                if (quest.IsDaily())
                                    result2 = QuestGiverStatus.AvailableRep;
                                else
                                    result2 = QuestGiverStatus.Available;
                            }
                            else
                                result2 = QuestGiverStatus.LowLevelAvailable;
                        }
                        else
                            result2 = QuestGiverStatus.Unavailable;
                    }
                }

                if (result2 > result)
                    result = result2;
            }

            return result;
        }

        public ushort GetReqKillOrCastCurrentCount(uint quest_id, int entry)
        {
            Quest qInfo = Global.ObjectMgr.GetQuestTemplate(quest_id);
            if (qInfo == null)
                return 0;

            foreach (QuestObjective obj in qInfo.Objectives)
                if (obj.ObjectID == entry)
                    return (ushort)GetQuestObjectiveData(qInfo, obj.StorageIndex);

            return 0;
        }

        public void AdjustQuestReqItemCount(Quest quest)
        {
            if (quest.HasSpecialFlag(QuestSpecialFlags.Deliver))
            {
                foreach (QuestObjective obj in quest.Objectives)
                {
                    if (obj.Type != QuestObjectiveType.Item)
                        continue;

                    uint reqItemCount = (uint)obj.Amount;
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
            return GetUInt32Value(PlayerFields.QuestLog + (slot * QuestSlotOffsets.Max) + QuestSlotOffsets.Id);
        }

        public uint GetQuestSlotState(ushort slot, byte counter)
        {
            return GetUInt32Value(PlayerFields.QuestLog + (slot * QuestSlotOffsets.Max) + QuestSlotOffsets.State);
        }

        public ushort GetQuestSlotCounter(ushort slot, byte counter)
        {
            if (counter < SharedConst.MaxQuestCounts)
                return GetUInt16Value(PlayerFields.QuestLog + slot * QuestSlotOffsets.Max + QuestSlotOffsets.Counts + counter /2, (byte)(counter % 2));

            return 0;
        }

        public uint GetQuestSlotTime(ushort slot)
        {
            return GetUInt32Value(PlayerFields.QuestLog + (slot * QuestSlotOffsets.Max) + QuestSlotOffsets.Time);
        }

        public void SetQuestSlot(ushort slot, uint quest_id, uint timer = 0)
        {
            SetUInt32Value(PlayerFields.QuestLog + (slot * QuestSlotOffsets.Max) + QuestSlotOffsets.Id, quest_id);
            SetUInt32Value(PlayerFields.QuestLog + (slot * QuestSlotOffsets.Max) + QuestSlotOffsets.State, 0);
            for (int i = 0; i < SharedConst.MaxQuestCounts / 2; ++i)
                SetUInt32Value(PlayerFields.QuestLog + slot * QuestSlotOffsets.Max + QuestSlotOffsets.Counts + i, 0);
            SetUInt32Value(PlayerFields.QuestLog + (slot * QuestSlotOffsets.Max) + QuestSlotOffsets.Time, timer);
        }

        public void SetQuestSlotCounter(ushort slot, byte counter, ushort count)
        {
            if (counter >= SharedConst.MaxQuestCounts)
                return;

            SetUInt16Value(PlayerFields.QuestLog + slot * QuestSlotOffsets.Max + QuestSlotOffsets.Counts + counter / 2, (byte)(counter % 2), count);
        }

        public void SetQuestSlotState(ushort slot, QuestSlotStateMask state)
        {
            SetFlag(PlayerFields.QuestLog + (slot * QuestSlotOffsets.Max) + QuestSlotOffsets.State, state);
        }

        public void RemoveQuestSlotState(ushort slot, QuestSlotStateMask state)
        {
            RemoveFlag(PlayerFields.QuestLog + (slot * QuestSlotOffsets.Max) + QuestSlotOffsets.State, state);
        }

        public void SetQuestSlotTimer(ushort slot, uint timer)
        {
            SetUInt32Value(PlayerFields.QuestLog + (slot * QuestSlotOffsets.Max) + QuestSlotOffsets.Time, timer);
        }

        void SetQuestCompletedBit(uint questBit, bool completed)
        {
            if (questBit == 0)
                return;

            int fieldOffset = ((int)questBit - 1) >> 5;
            if (fieldOffset >= PlayerConst.QuestsCompletedBitsSize)
                return;

            ApplyModFlag(ActivePlayerFields.QuestCompleted + fieldOffset, (uint)(1 << (((int)questBit - 1) & 31)), completed);
        }

        public void AreaExploredOrEventHappens(uint questId)
        {
            if (questId != 0)
            {
                ushort log_slot = FindQuestSlot(questId);
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
                        SetQuestSlotState(log_slot, QUEST_STATE_COMPLETE);
                        SendQuestComplete(questId);
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
                for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.next())
                {
                    Player player = refe.GetSource();

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
                uint questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                QuestStatusData q_status = m_QuestStatus[questid];

                if (q_status.Status != QuestStatus.Incomplete)
                    continue;

                Quest qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null || !qInfo.HasSpecialFlag(QuestSpecialFlags.Deliver))
                    continue;

                foreach (QuestObjective obj in qInfo.Objectives)
                {
                    if (obj.Type != QuestObjectiveType.Item)
                        continue;

                    int reqItem = obj.ObjectID;
                    if (reqItem == entry)
                    {
                        int reqItemCount = obj.Amount;
                        int curItemCount = GetQuestObjectiveData(qInfo, obj.StorageIndex);
                        if (curItemCount < reqItemCount)
                        {
                            int newItemCount = (int)Math.Min(curItemCount + count, reqItemCount);
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
                uint questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;
                Quest qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null)
                    continue;
                if (!qInfo.HasSpecialFlag(QuestSpecialFlags.Deliver))
                    continue;

                foreach (QuestObjective obj in qInfo.Objectives)
                {
                    if (obj.Type != QuestObjectiveType.Item)
                        continue;

                    int reqItem = obj.ObjectID;
                    if (reqItem == entry)
                    {
                        uint reqItemCount = (uint)obj.Amount;
                        int curItemCount = GetQuestObjectiveData(qInfo, obj.StorageIndex);

                        if (curItemCount >= reqItemCount) // we may have more than what the status shows
                            curItemCount = (int)GetItemCount(entry, false);

                        int newItemCount = (int)((count > curItemCount) ? 0 : curItemCount - count);

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

        public void KilledMonsterCredit(uint entry, ObjectGuid guid = default(ObjectGuid))
        {
            ushort addKillCount = 1;
            uint real_entry = entry;
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
                uint questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                Quest qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null)
                    continue;

                // just if !ingroup || !noraidgroup || raidgroup
                QuestStatusData q_status = m_QuestStatus[questid];
                if (q_status.Status == QuestStatus.Incomplete && (GetGroup() == null || !GetGroup().isRaidGroup() || qInfo.IsAllowedInRaid(GetMap().GetDifficultyID())))
                {
                    if (qInfo.HasSpecialFlag(QuestSpecialFlags.Kill))// && !qInfo.HasSpecialFlag(QuestSpecialFlags.Cast))
                    {
                        foreach (QuestObjective obj in qInfo.Objectives)
                        {
                            if (obj.Type != QuestObjectiveType.Monster)
                                continue;

                            int reqkill = obj.ObjectID;
                            if (reqkill == real_entry)
                            {
                                int curKillCount = GetQuestObjectiveData(qInfo, obj.StorageIndex);
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
                uint questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                Quest qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null)
                    continue;

                // just if !ingroup || !noraidgroup || raidgroup
                QuestStatusData q_status = m_QuestStatus[questid];
                if (q_status.Status == QuestStatus.Incomplete && (GetGroup() == null || !GetGroup().isRaidGroup() || qInfo.IsAllowedInRaid(GetMap().GetDifficultyID())))
                {
                    foreach (QuestObjective obj in qInfo.Objectives)
                    {
                        if (obj.Type != QuestObjectiveType.PlayerKills)
                            continue;

                        int curKillCount = GetQuestObjectiveData(qInfo, obj.StorageIndex);
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

        public void KillCreditGO(uint entry, ObjectGuid guid = default(ObjectGuid))
        {
            ushort addCastCount = 1;
            for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            {
                uint questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                Quest qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null)
                    continue;

                QuestStatusData q_status = m_QuestStatus[questid];

                if (q_status.Status == QuestStatus.Incomplete)
                {
                    if (qInfo.HasSpecialFlag(QuestSpecialFlags.Cast))
                    {
                        foreach (QuestObjective obj in qInfo.Objectives)
                        {
                            if (obj.Type != QuestObjectiveType.GameObject)
                                continue;

                            int reqTarget = obj.ObjectID;

                            // other not this creature/GO related objectives
                            if (reqTarget != entry)
                                continue;

                            int curCastCount = GetQuestObjectiveData(qInfo, obj.StorageIndex);
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
                uint questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                Quest qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null)
                    continue;

                QuestStatusData q_status = m_QuestStatus[questid];

                if (q_status.Status == QuestStatus.Incomplete)
                {
                    if (qInfo.HasSpecialFlag(QuestSpecialFlags.Kill | QuestSpecialFlags.Cast | QuestSpecialFlags.Speakto))
                    {
                        foreach (QuestObjective obj in qInfo.Objectives)
                        {
                            if (obj.Type != QuestObjectiveType.TalkTo)
                                continue;

                            int reqTarget = obj.ObjectID;
                            if (reqTarget == entry)
                            {
                                int curTalkCount = GetQuestObjectiveData(qInfo, obj.StorageIndex);
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
                uint questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                Quest qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null)
                    continue;

                foreach (QuestObjective obj in qInfo.Objectives)
                {
                    if (obj.Type != QuestObjectiveType.Money)
                        continue;

                    QuestStatusData q_status = m_QuestStatus[questid];
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
                uint questid = GetQuestSlotQuestId(i);
                if (questid != 0)
                {
                    Quest qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                    if (qInfo != null)
                    {
                        QuestStatusData q_status = m_QuestStatus[questid];

                        foreach (QuestObjective obj in qInfo.Objectives)
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

        void CurrencyChanged(uint currencyId, int change)
        {
            for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            {
                uint questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                Quest qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                if (qInfo == null)
                    continue;

                foreach (QuestObjective obj in qInfo.Objectives)
                {
                    if (obj.ObjectID != currencyId)
                        continue;

                    QuestStatusData q_status = m_QuestStatus[questid];
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
                uint questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                var q_status = m_QuestStatus.LookupByKey(questid);
                if (q_status == null)
                    continue;

                if (q_status.Status == QuestStatus.Incomplete)
                {
                    Quest qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                    if (qInfo == null)
                        continue;

                    // hide quest if player is in raid-group and quest is no raid quest
                    if (GetGroup() != null && GetGroup().isRaidGroup() && !qInfo.IsAllowedInRaid(GetMap().GetDifficultyID()))
                        if (!InBattleground()) //there are two ways.. we can make every bg-quest a raidquest, or add this code here.. i don't know if this can be exploited by other quests, but i think all other quests depend on a specific area.. but keep this in mind, if something strange happens later
                            continue;

                    // There should be no mixed ReqItem/ReqSource drop
                    // This part for ReqItem drop
                    foreach (QuestObjective obj in qInfo.Objectives)
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
                            ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(itemid);

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

        bool IsQuestObjectiveProgressComplete(Quest quest)
        {
            float progress = 0;
            foreach (QuestObjective obj in quest.Objectives)
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
            Quest quest = Global.ObjectMgr.GetQuestTemplate(objective.QuestID);
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
            int oldData = status.ObjectiveData[objective.StorageIndex];
            if (oldData == data)
                return;

            Quest quest = Global.ObjectMgr.GetQuestTemplate(objective.QuestID);
            if (quest != null)
                Global.ScriptMgr.OnQuestObjectiveChange(this, quest, objective, oldData, data);

            // Set data
            status.ObjectiveData[objective.StorageIndex] = data;

            // Add to save
            m_QuestStatusSave[objective.QuestID] = QuestSaveType.Default;

            // Update quest fields
            ushort log_slot = FindQuestSlot(objective.QuestID);
            if (log_slot < SharedConst.MaxQuestLogSize)
            {
                if (!objective.IsStoringFlag())
                    SetQuestSlotCounter(log_slot, (byte)objective.StorageIndex, (ushort)status.ObjectiveData[objective.StorageIndex]);
                else if (data != 0)
                    SetQuestSlotState(log_slot, (QuestSlotStateMask)(256 << objective.StorageIndex));
                else
                    RemoveQuestSlotState(log_slot, (QuestSlotStateMask)(256 << objective.StorageIndex));
            }
        }

        public void SendQuestComplete(Quest quest)
        {
            if (quest != null)
            {
                QuestUpdateComplete data = new QuestUpdateComplete();
                data.QuestID = quest.Id;
                SendPacket(data);
            }
        }

        public void SendQuestReward(Quest quest, Creature questGiver, uint xp, bool hideChatMessage)
        {
            uint questId = quest.Id;
            Global.GameEventMgr.HandleQuestComplete(questId);

            uint moneyReward;

            if (getLevel() < WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
            {
                moneyReward = GetQuestMoneyReward(quest);
            }
            else // At max level, increase gold reward
            {
                xp = 0;
                moneyReward = (uint)(GetQuestMoneyReward(quest) + (int)(quest.GetRewMoneyMaxLevel() * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney)));
            }

            QuestGiverQuestComplete packet = new QuestGiverQuestComplete();

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
                QuestGiverQuestFailed questGiverQuestFailed = new QuestGiverQuestFailed();
                questGiverQuestFailed.QuestID = questId;
                questGiverQuestFailed.Reason = reason; // failed reason (valid reasons: 4, 16, 50, 17, other values show default message)
                SendPacket(questGiverQuestFailed);
            }
        }

        public void SendQuestTimerFailed(uint questId)
        {
            if (questId != 0)
            {
                QuestUpdateFailedTimer questUpdateFailedTimer = new QuestUpdateFailedTimer();
                questUpdateFailedTimer.QuestID = questId;
                SendPacket(questUpdateFailedTimer);
            }
        }

        public void SendCanTakeQuestResponse(QuestFailedReasons reason, bool sendErrorMessage = true, string reasonText = "")
        {
            QuestGiverInvalidQuest questGiverInvalidQuest = new QuestGiverInvalidQuest();

            questGiverInvalidQuest.Reason = reason;
            questGiverInvalidQuest.SendErrorMessage = sendErrorMessage;
            questGiverInvalidQuest.ReasonText = reasonText;

            SendPacket(questGiverInvalidQuest);
        }

        public void SendQuestConfirmAccept(Quest quest, Player receiver)
        {
            if (!receiver)
                return;

            QuestConfirmAcceptResponse packet = new QuestConfirmAcceptResponse();

            packet.QuestTitle = quest.LogTitle;

            LocaleConstant loc_idx = receiver.GetSession().GetSessionDbLocaleIndex();
            if (loc_idx != LocaleConstant.enUS)
            {
                QuestTemplateLocale questLocale = Global.ObjectMgr.GetQuestLocale(quest.Id);
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
                QuestPushResultResponse data = new QuestPushResultResponse();
                data.SenderGUID = player.GetGUID();
                data.Result = reason;
                SendPacket(data);
            }
        }

        void SendQuestUpdateAddCredit(Quest quest, ObjectGuid guid, QuestObjective obj, uint count)
        {
            QuestUpdateAddCredit packet = new QuestUpdateAddCredit();
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
            QuestUpdateAddCreditSimple packet = new QuestUpdateAddCreditSimple();
            packet.QuestID = obj.QuestID;
            packet.ObjectID = obj.ObjectID;
            packet.ObjectiveType = obj.Type;
            SendPacket(packet);
        }

        public void SendQuestUpdateAddPlayer(Quest quest, uint newCount)
        {
            QuestUpdateAddPvPCredit packet = new QuestUpdateAddPvPCredit();
            packet.QuestID = quest.Id;
            packet.Count = (ushort)newCount;
            SendPacket(packet);
        }

        public void SendQuestGiverStatusMultiple()
        {
            QuestGiverStatusMultiple response = new QuestGiverStatusMultiple();

            foreach (var itr in m_clientGUIDs)
            {
                if (itr.IsAnyTypeCreature())
                {
                    // need also pet quests case support
                    Creature questgiver = ObjectAccessor.GetCreatureOrPetOrVehicle(this, itr);
                    if (!questgiver || questgiver.IsHostileTo(this))
                        continue;

                    if (!questgiver.HasFlag64(UnitFields.NpcFlags, NPCFlags.QuestGiver))
                        continue;

                    response.QuestGiver.Add(new QuestGiverInfo(questgiver.GetGUID(), GetQuestDialogStatus(questgiver)));
                }
                else if (itr.IsGameObject())
                {
                    GameObject questgiver = GetMap().GetGameObject(itr);
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
                uint questId = GetQuestSlotQuestId(i);
                if (questId == 0)
                    continue;

                Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
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
                uint questid = GetQuestSlotQuestId(i);
                if (questid == 0)
                    continue;

                var qs = m_QuestStatus.LookupByKey(questid);
                if (qs == null)
                    continue;

                if (qs.Status == QuestStatus.Incomplete)
                {
                    Quest qInfo = Global.ObjectMgr.GetQuestTemplate(questid);
                    if (qInfo == null)
                        continue;

                    if (GetGroup() != null && GetGroup().isRaidGroup() && !qInfo.IsAllowedInRaid(GetMap().GetDifficultyID()))
                        continue;

                    foreach (QuestObjective obj in qInfo.Objectives)
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

            UpdateData udata = new UpdateData(GetMapId());
            UpdateObject packet;
            foreach (var guid in m_clientGUIDs)
            {
                if (guid.IsGameObject())
                {
                    GameObject obj = ObjectAccessor.GetGameObject(this, guid);
                    if (obj != null)
                        obj.BuildValuesUpdateBlockForPlayer(udata, this);
                }
                else if (guid.IsCreatureOrVehicle())
                {
                    Creature obj = ObjectAccessor.GetCreatureOrPetOrVehicle(this, guid);
                    if (obj == null)
                        continue;

                    // check if this unit requires quest specific flags
                    if (!obj.HasFlag64(UnitFields.NpcFlags, NPCFlags.SpellClick))
                        continue;

                    var clickPair = Global.ObjectMgr.GetSpellClickInfoMapBounds(obj.GetEntry());
                    foreach (var spell in clickPair)
                    {
                        //! This code doesn't look right, but it was logically converted to condition system to do the exact
                        //! same thing it did before. It definitely needs to be overlooked for intended functionality.
                        List<Condition> conds = Global.ConditionMgr.GetConditionsForSpellClickEvent(obj.GetEntry(), spell.spellId);
                        if (conds != null)
                        {
                            bool buildUpdateBlock = false;
                            for (var i = 0; i < conds.Count && !buildUpdateBlock; ++i)
                                if (conds[i].ConditionType == ConditionTypes.QuestRewarded || conds[i].ConditionType == ConditionTypes.QuestTaken)
                                    buildUpdateBlock = true;

                            if (buildUpdateBlock)
                            {
                                obj.BuildValuesUpdateBlockForPlayer(udata, this);
                                break;
                            }
                        }
                    }
                }
            }
            udata.BuildPacket(out packet);
            SendPacket(packet);
        }

        void SetDailyQuestStatus(uint quest_id)
        {
            Quest qQuest = Global.ObjectMgr.GetQuestTemplate(quest_id);
            if (qQuest != null)
            {
                if (!qQuest.IsDFQuest())
                {
                    AddDynamicValue(ActivePlayerDynamicFields.DailyQuests, quest_id);
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
            bool found = false;
            if (Global.ObjectMgr.GetQuestTemplate(quest_id) != null)
            {
                var dailies = GetDynamicValues(ActivePlayerDynamicFields.DailyQuests);
                foreach (uint dailyQuestId in dailies)
                {
                    if (dailyQuestId == quest_id)
                    {
                        found = true;
                        break;
                    }
                }
            }

            return found;
        }

        void SetWeeklyQuestStatus(uint quest_id)
        {
            m_weeklyquests.Add(quest_id);
            m_WeeklyQuestChanged = true;
        }

        void SetSeasonalQuestStatus(uint quest_id)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);
            if (quest == null)
                return;

            m_seasonalquests.Add(Global.GameEventMgr.GetEventIdForQuest(quest), quest_id);
            m_SeasonalQuestChanged = true;
        }

        void SetMonthlyQuestStatus(uint quest_id)
        {
            m_monthlyquests.Add(quest_id);
            m_MonthlyQuestChanged = true;
        }
    }
}
