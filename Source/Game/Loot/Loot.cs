// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Loots
{
    public class LootItem
    {
        public uint itemid;
        public uint LootListId;
        public uint randomBonusListId;
        public List<uint> BonusListIDs = new();
        public ItemContext context;
        public ConditionsReference conditions;                               // additional loot condition
        public List<ObjectGuid> allowedGUIDs = new();
        public ObjectGuid rollWinnerGUID;                                   // Stores the guid of person who won loot, if his bags are full only he can see the item in loot list!
        public uint count;
        public LootItemType type;
        public bool is_looted;
        public bool is_blocked;
        public bool freeforall;                          // free for all
        public bool is_underthreshold;
        public bool is_counted;
        public bool needs_quest;                          // quest drop
        public bool follow_loot_rules;

        public LootItem() { }
        public LootItem(LootStoreItem li)
        {
            itemid = li.itemid;
            conditions = li.conditions;
            needs_quest = li.needs_quest;

            switch (li.type)
            {
                case LootStoreItemType.Item:
                    randomBonusListId = ItemEnchantmentManager.GenerateItemRandomBonusListId(itemid);
                    type = LootItemType.Item;
                    ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemid);
                    freeforall = proto != null && proto.HasFlag(ItemFlags.MultiDrop);
                    follow_loot_rules = !li.needs_quest || (proto != null && proto.HasFlag(ItemFlagsCustom.FollowLootRules));
                    break;
                case LootStoreItemType.Currency:
                    type = LootItemType.Currency;
                    freeforall = true;
                    break;
                case LootStoreItemType.TrackingQuest:
                    type = LootItemType.TrackingQuest;
                    freeforall = true;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Basic checks for player/item compatibility - if false no chance to see the item in the loot - used only for loot generation
        /// </summary>
        /// <param name="player"></param>
        /// <param name="loot"></param>
        /// <returns></returns>
        public bool AllowedForPlayer(Player player, Loot loot)
        {
            switch (type)
            {
                case LootItemType.Item:
                    return ItemAllowedForPlayer(player, loot, itemid, needs_quest, follow_loot_rules, false, conditions);
                case LootItemType.Currency:
                    return CurrencyAllowedForPlayer(player, itemid, needs_quest, conditions);
                case LootItemType.TrackingQuest:
                    return TrackingQuestAllowedForPlayer(player, itemid, conditions);
                default:
                    break;
            }
            return false;
        }

        public static bool AllowedForPlayer(Player player, LootStoreItem lootStoreItem, bool strictUsabilityCheck)
        {
            switch (lootStoreItem.type)
            {
                case LootStoreItemType.Item:
                    return ItemAllowedForPlayer(player, null, lootStoreItem.itemid, lootStoreItem.needs_quest,
                        !lootStoreItem.needs_quest || Global.ObjectMgr.GetItemTemplate(lootStoreItem.itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
                        strictUsabilityCheck, lootStoreItem.conditions);
                case LootStoreItemType.Currency:
                    return CurrencyAllowedForPlayer(player, lootStoreItem.itemid, lootStoreItem.needs_quest, lootStoreItem.conditions);
                case LootStoreItemType.TrackingQuest:
                    return TrackingQuestAllowedForPlayer(player, lootStoreItem.itemid, lootStoreItem.conditions);
                default:
                    break;
            }
            return false;
        }

        public static bool ItemAllowedForPlayer(Player player, Loot loot, uint itemid, bool needs_quest, bool follow_loot_rules, bool strictUsabilityCheck, ConditionsReference conditions)
        {
            // DB conditions check
            if (!conditions.Meets(player))
                return false;

            ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(itemid);
            if (pProto == null)
                return false;

            // not show loot for not own team
            if (pProto.HasFlag(ItemFlags2.FactionHorde) && player.GetTeam() != Team.Horde)
                return false;

            if (pProto.HasFlag(ItemFlags2.FactionAlliance) && player.GetTeam() != Team.Alliance)
                return false;

            // Master looter can see all items even if the character can't loot them
            if (loot != null && loot.GetLootMethod() == LootMethod.MasterLoot && follow_loot_rules && loot.GetLootMasterGUID() == player.GetGUID())
                return true;

            // Don't allow loot for players without profession or those who already know the recipe
            if (pProto.HasFlag(ItemFlags.HideUnusableRecipe))
            {
                if (!player.HasSkill((SkillType)pProto.GetRequiredSkill()))
                    return false;

                foreach (var itemEffect in pProto.Effects)
                {
                    if (itemEffect.TriggerType != ItemSpelltriggerType.OnLearn)
                        continue;

                    if (player.HasSpell((uint)itemEffect.SpellID))
                        return false;
                }
            }

            // check quest requirements
            if (!pProto.FlagsCu.HasAnyFlag(ItemFlagsCustom.IgnoreQuestStatus)
                && ((needs_quest || (pProto.GetStartQuest() != 0 && player.GetQuestStatus(pProto.GetStartQuest()) != QuestStatus.None)) && !player.HasQuestForItem(itemid)))
                return false;

            if (strictUsabilityCheck)
            {
                if ((pProto.IsWeapon() || pProto.IsArmor()) && !pProto.IsUsableByLootSpecialization(player, true))
                    return false;

                if (player.CanRollNeedForItem(pProto, null, false) != InventoryResult.Ok)
                    return false;
            }

            return true;
        }

        public static bool CurrencyAllowedForPlayer(Player player, uint currencyId, bool needs_quest, ConditionsReference conditions)
        {
            // DB conditions check
            if (!conditions.Meets(player))
                return false;

            CurrencyTypesRecord currency = CliDB.CurrencyTypesStorage.LookupByKey(currencyId);
            if (currency == null)
                return false;

            // not show loot for not own team
            if (currency.HasFlag(CurrencyTypesFlags.IsHordeOnly) && player.GetTeam() != Team.Horde)
                return false;

            if (currency.HasFlag(CurrencyTypesFlags.IsAllianceOnly) && player.GetTeam() != Team.Alliance)
                return false;

            // check quest requirements
            if (needs_quest && !player.HasQuestForCurrency(currencyId))
                return false;

            return true;
        }

        public static bool TrackingQuestAllowedForPlayer(Player player, uint questId, ConditionsReference conditions)
        {
            // DB conditions check
            if (!conditions.Meets(player))
                return false;

            if (player.IsQuestCompletedBitSet(questId))
                return false;

            return true;
        }

        public void AddAllowedLooter(Player player)
        {
            allowedGUIDs.Add(player.GetGUID());
        }

        public bool HasAllowedLooter(ObjectGuid looter)
        {
            return allowedGUIDs.Contains(looter);
        }

        public LootSlotType? GetUiTypeForPlayer(Player player, Loot loot)
        {
            if (is_looted)
                return null;

            if (!allowedGUIDs.Contains(player.GetGUID()))
                return null;

            if (freeforall)
            {
                var ffaItems = loot.GetPlayerFFAItems().LookupByKey(player.GetGUID());
                if (ffaItems != null)
                {
                    var ffaItemItr = ffaItems.Find(ffaItem => ffaItem.LootListId == LootListId);
                    if (ffaItemItr != null && !ffaItemItr.is_looted)
                        return loot.GetLootMethod() == LootMethod.FreeForAll ? LootSlotType.Owner : LootSlotType.AllowLoot;
                }
                return null;
            }

            if (needs_quest && !follow_loot_rules)
                return loot.GetLootMethod() == LootMethod.FreeForAll ? LootSlotType.Owner : LootSlotType.AllowLoot;

            switch (loot.GetLootMethod())
            {
                case LootMethod.FreeForAll:
                    return LootSlotType.Owner;
                case LootMethod.RoundRobin:
                    if (!loot.roundRobinPlayer.IsEmpty() && loot.roundRobinPlayer != player.GetGUID())
                        return null;

                    return LootSlotType.AllowLoot;
                case LootMethod.MasterLoot:
                    if (is_underthreshold)
                    {
                        if (!loot.roundRobinPlayer.IsEmpty() && loot.roundRobinPlayer != player.GetGUID())
                            return null;

                        return LootSlotType.AllowLoot;
                    }

                    return loot.GetLootMasterGUID() == player.GetGUID() ? LootSlotType.Master : LootSlotType.Locked;
                case LootMethod.GroupLoot:
                case LootMethod.NeedBeforeGreed:
                    if (is_underthreshold)
                        if (!loot.roundRobinPlayer.IsEmpty() && loot.roundRobinPlayer != player.GetGUID())
                            return null;

                    if (is_blocked)
                        return LootSlotType.RollOngoing;

                    if (rollWinnerGUID.IsEmpty()) // all passed
                        return LootSlotType.AllowLoot;

                    if (rollWinnerGUID == player.GetGUID())
                        return LootSlotType.Owner;

                    return null;
                case LootMethod.PersonalLoot:
                    return LootSlotType.Owner;
                default:
                    break;
            }

            return null;
        }

        public List<ObjectGuid> GetAllowedLooters() { return allowedGUIDs; }
    }

    public class NotNormalLootItem
    {
        public byte LootListId;                                          // position in quest_items or items;
        public bool is_looted;

        public NotNormalLootItem()
        {
            LootListId = 0;
            is_looted = false;
        }

        public NotNormalLootItem(byte _index, bool _islooted = false)
        {
            LootListId = _index;
            is_looted = _islooted;
        }
    }

    public class PlayerRollVote
    {
        public RollVote Vote;
        public byte RollNumber;

        public PlayerRollVote()
        {
            Vote = RollVote.NotValid;
            RollNumber = 0;
        }
    }

    public class LootRoll
    {
        static TimeSpan LOOT_ROLL_TIMEOUT = TimeSpan.FromMinutes(1);

        Map m_map;
        Dictionary<ObjectGuid, PlayerRollVote> m_rollVoteMap = new();
        bool m_isStarted;
        LootItem m_lootItem;
        Loot m_loot;
        RollMask m_voteMask;
        DateTime m_endTime = DateTime.MinValue;

        ~LootRoll()
        {
            if (m_isStarted)
                SendAllPassed();

            foreach (var (playerGuid, roll) in m_rollVoteMap)
            {
                if (roll.Vote != RollVote.NotEmitedYet)
                    continue;

                Player player = Global.ObjAccessor.GetPlayer(m_map, playerGuid);
                if (player == null)
                    continue;

                player.RemoveLootRoll(this);
            }
        }

        // Send the roll for the whole group
        void SendStartRoll()
        {
            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(m_lootItem.itemid);
            foreach (var (playerGuid, roll) in m_rollVoteMap)
            {
                if (roll.Vote != RollVote.NotEmitedYet)
                    continue;

                Player player = Global.ObjAccessor.GetPlayer(m_map, playerGuid);
                if (player == null)
                    continue;

                StartLootRoll startLootRoll = new();
                startLootRoll.LootObj = m_loot.GetGUID();
                startLootRoll.MapID = (int)m_map.GetId();
                startLootRoll.RollTime = (uint)LOOT_ROLL_TIMEOUT.TotalMilliseconds;
                startLootRoll.Method = m_loot.GetLootMethod();
                startLootRoll.ValidRolls = m_voteMask;
                // In NEED_BEFORE_GREED need disabled for non-usable item for player
                if (m_loot.GetLootMethod() == LootMethod.NeedBeforeGreed && player.CanRollNeedForItem(itemTemplate, m_map, true) != InventoryResult.Ok)
                    startLootRoll.ValidRolls &= ~RollMask.Need;

                FillPacket(startLootRoll.Item);
                startLootRoll.Item.UIType = LootSlotType.RollOngoing;
                startLootRoll.DungeonEncounterID = m_loot.GetDungeonEncounterId();

                player.SendPacket(startLootRoll);
            }

            // Handle auto pass option
            foreach (var (playerGuid, roll) in m_rollVoteMap)
            {
                if (roll.Vote != RollVote.Pass)
                    continue;

                SendRoll(playerGuid, -1, RollVote.Pass, null);
            }
        }

        // Send all passed message
        void SendAllPassed()
        {
            LootAllPassed lootAllPassed = new();
            lootAllPassed.LootObj = m_loot.GetGUID();
            FillPacket(lootAllPassed.Item);
            lootAllPassed.Item.UIType = LootSlotType.AllowLoot;
            lootAllPassed.DungeonEncounterID = m_loot.GetDungeonEncounterId();
            lootAllPassed.Write();

            foreach (var (playerGuid, roll) in m_rollVoteMap)
            {
                if (roll.Vote != RollVote.NotValid)
                    continue;

                Player player = Global.ObjAccessor.GetPlayer(m_map, playerGuid);
                if (player == null)
                    continue;

                player.SendPacket(lootAllPassed);
            }
        }

        // Send roll of targetGuid to the whole group (included targuetGuid)
        void SendRoll(ObjectGuid targetGuid, int rollNumber, RollVote rollType, ObjectGuid? rollWinner)
        {
            LootRollBroadcast lootRoll = new();
            lootRoll.LootObj = m_loot.GetGUID();
            lootRoll.Player = targetGuid;
            lootRoll.Roll = rollNumber;
            lootRoll.RollType = rollType;
            lootRoll.Autopassed = false;
            FillPacket(lootRoll.Item);
            lootRoll.Item.UIType = LootSlotType.RollOngoing;
            lootRoll.DungeonEncounterID = m_loot.GetDungeonEncounterId();
            lootRoll.Write();

            foreach (var (playerGuid, roll) in m_rollVoteMap)
            {
                if (roll.Vote == RollVote.NotValid)
                    continue;

                if (playerGuid == rollWinner)
                    continue;

                Player player = Global.ObjAccessor.GetPlayer(m_map, playerGuid);
                if (player == null)
                    continue;

                player.SendPacket(lootRoll);
            }

            if (rollWinner.HasValue)
            {
                Player player = Global.ObjAccessor.GetPlayer(m_map, rollWinner.Value);
                if (player != null)
                {
                    lootRoll.Item.UIType = LootSlotType.AllowLoot;
                    lootRoll.Clear();
                    player.SendPacket(lootRoll);
                }
            }
        }

        // Send roll 'value' of the whole group and the winner to the whole group
        void SendLootRollWon(ObjectGuid targetGuid, int rollNumber, RollVote rollType)
        {
            // Send roll values
            foreach (var (playerGuid, roll) in m_rollVoteMap)
            {
                switch (roll.Vote)
                {
                    case RollVote.Pass:
                        break;
                    case RollVote.NotEmitedYet:
                    case RollVote.NotValid:
                        SendRoll(playerGuid, 0, RollVote.Pass, targetGuid);
                        break;
                    default:
                        SendRoll(playerGuid, roll.RollNumber, roll.Vote, targetGuid);
                        break;
                }
            }

            LootRollWon lootRollWon = new();
            lootRollWon.LootObj = m_loot.GetGUID();
            lootRollWon.Winner = targetGuid;
            lootRollWon.Roll = rollNumber;
            lootRollWon.RollType = rollType;
            FillPacket(lootRollWon.Item);
            lootRollWon.Item.UIType = LootSlotType.Locked;
            lootRollWon.DungeonEncounterID = m_loot.GetDungeonEncounterId();
            lootRollWon.MainSpec = true;    // offspec rolls not implemented
            lootRollWon.Write();

            foreach (var (playerGuid, roll) in m_rollVoteMap)
            {
                if (roll.Vote == RollVote.NotValid)
                    continue;

                if (playerGuid == targetGuid)
                    continue;

                Player player1 = Global.ObjAccessor.GetPlayer(m_map, playerGuid);
                if (player1 == null)
                    continue;

                player1.SendPacket(lootRollWon);
            }

            Player player = Global.ObjAccessor.GetPlayer(m_map, targetGuid);
            if (player != null)
            {
                lootRollWon.Item.UIType = LootSlotType.AllowLoot;
                lootRollWon.Clear();
                player.SendPacket(lootRollWon);
            }
        }

        void FillPacket(LootItemData lootItem)
        {
            lootItem.Quantity = m_lootItem.count;
            lootItem.LootListID = (byte)m_lootItem.LootListId;
            lootItem.CanTradeToTapList = m_lootItem.allowedGUIDs.Count > 1;
            lootItem.Loot = new(m_lootItem);
        }

        // Try to start the group roll for the specified item (it may fail for quest item or any condition
        // If this method return false the roll have to be removed from the container to avoid any problem
        public bool TryToStart(Map map, Loot loot, uint lootListId, ushort enchantingSkill)
        {
            if (!m_isStarted)
            {
                if (lootListId >= loot.items.Count)
                    return false;

                m_map = map;

                // initialize the data needed for the roll
                m_lootItem = loot.items[(int)lootListId];

                m_loot = loot;
                m_lootItem.is_blocked = true;                          // block the item while rolling

                uint playerCount = 0;
                foreach (ObjectGuid allowedLooter in m_lootItem.GetAllowedLooters())
                {
                    Player plr = Global.ObjAccessor.GetPlayer(m_map, allowedLooter);
                    if (plr == null || !m_lootItem.HasAllowedLooter(plr.GetGUID()))     // check if player meet the condition to be able to roll this item
                    {
                        m_rollVoteMap[allowedLooter].Vote = RollVote.NotValid;
                        continue;
                    }
                    // initialize player vote map
                    m_rollVoteMap[allowedLooter].Vote = plr.GetPassOnGroupLoot() ? RollVote.Pass : RollVote.NotEmitedYet;
                    if (!plr.GetPassOnGroupLoot())
                        plr.AddLootRoll(this);

                    ++playerCount;
                }

                // initialize item prototype and check enchant possibilities for this group
                ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(m_lootItem.itemid);
                m_voteMask = RollMask.AllMask;
                if (itemTemplate.HasFlag(ItemFlags2.CanOnlyRollGreed))
                    m_voteMask = m_voteMask & ~RollMask.Need;
                var disenchantSkillRequired = GetItemDisenchantSkillRequired();
                if (!disenchantSkillRequired.HasValue || disenchantSkillRequired > enchantingSkill)
                    m_voteMask = m_voteMask & ~RollMask.Disenchant;

                if (playerCount > 1)                                    // check if more than one player can loot this item
                {
                    // start the roll
                    SendStartRoll();
                    m_endTime = GameTime.Now() + LOOT_ROLL_TIMEOUT;
                    m_isStarted = true;
                    return true;
                }
                // no need to start roll if one or less player can loot this item so place it under threshold
                m_lootItem.is_underthreshold = true;
                m_lootItem.is_blocked = false;
            }
            return false;
        }

        // Add vote from playerGuid
        public bool PlayerVote(Player player, RollVote vote)
        {
            ObjectGuid playerGuid = player.GetGUID();
            if (!m_rollVoteMap.TryGetValue(playerGuid, out PlayerRollVote voter))
                return false;

            voter.Vote = vote;

            if (vote != RollVote.Pass && vote != RollVote.NotValid)
                voter.RollNumber = (byte)RandomHelper.URand(1, 100);

            switch (vote)
            {
                case RollVote.Pass:                                // Player choose pass
                {
                    SendRoll(playerGuid, -1, RollVote.Pass, null);
                    break;
                }
                case RollVote.Need:                                // player choose Need
                {
                    SendRoll(playerGuid, 0, RollVote.Need, null);
                    player.UpdateCriteria(CriteriaType.RollAnyNeed, 1);
                    break;
                }
                case RollVote.Greed:                               // player choose Greed
                {
                    SendRoll(playerGuid, -1, RollVote.Greed, null);
                    player.UpdateCriteria(CriteriaType.RollAnyGreed, 1);
                    break;
                }
                case RollVote.Disenchant:                          // player choose Disenchant
                {
                    SendRoll(playerGuid, -1, RollVote.Disenchant, null);
                    player.UpdateCriteria(CriteriaType.RollAnyGreed, 1);
                    break;
                }
                default:                                            // Roll removed case
                    return false;
            }
            return true;
        }

        // check if we can found a winner for this roll or if timer is expired
        public bool UpdateRoll()
        {
            KeyValuePair<ObjectGuid, PlayerRollVote> winner = default;

            if (AllPlayerVoted(ref winner) || m_endTime <= GameTime.Now())
            {
                Finish(winner);
                return true;
            }
            return false;
        }

        public bool IsLootItem(ObjectGuid lootObject, uint lootListId)
        {
            return m_loot.GetGUID() == lootObject && m_lootItem.LootListId == lootListId;
        }

        /**
        * \brief Check if all player have voted and return true in that case. Also return current winner.
        * \param winnerItr > will be different than m_rollCoteMap.end() if winner exist. (Someone voted greed or need)
        * \returns true if all players voted
        **/
        bool AllPlayerVoted(ref KeyValuePair<ObjectGuid, PlayerRollVote> winnerPair)
        {
            uint notVoted = 0;
            bool isSomeoneNeed = false;

            winnerPair = default;
            foreach (var pair in m_rollVoteMap)
            {
                switch (pair.Value.Vote)
                {
                    case RollVote.Need:
                        if (!isSomeoneNeed || winnerPair.Value == null || pair.Value.RollNumber > winnerPair.Value.RollNumber)
                        {
                            isSomeoneNeed = true;                                               // first passage will force to set winner because need is prioritized
                            winnerPair = pair;
                        }
                        break;
                    case RollVote.Greed:
                    case RollVote.Disenchant:
                        if (!isSomeoneNeed)                                                      // if at least one need is detected then winner can't be a greed
                        {
                            if (winnerPair.Value == null || pair.Value.RollNumber > winnerPair.Value.RollNumber)
                                winnerPair = pair;
                        }
                        break;
                    // Explicitly passing excludes a player from winning loot, so no action required.
                    case RollVote.Pass:
                        break;
                    case RollVote.NotEmitedYet:
                        ++notVoted;
                        break;
                    default:
                        break;
                }
            }

            return notVoted == 0;
        }

        uint? GetItemDisenchantLootId()
        {
            ItemInstance itemInstance = new(m_lootItem);

            BonusData bonusData = new(itemInstance);
            if (!bonusData.CanDisenchant)
                return null;

            if (bonusData.DisenchantLootId != 0)
                return bonusData.DisenchantLootId;

            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(m_lootItem.itemid);

            // ignore temporary item level scaling (pvp or timewalking)
            uint itemLevel = Item.GetItemLevel(itemTemplate, bonusData, (uint)bonusData.RequiredLevel, 0, 0, 0, 0, false, 0);

            var disenchantLoot = Item.GetBaseDisenchantLoot(itemTemplate, (uint)bonusData.Quality, itemLevel);
            if (disenchantLoot == null)
                return null;

            return disenchantLoot.Id;
        }

        ushort? GetItemDisenchantSkillRequired()
        {
            ItemInstance itemInstance = new(m_lootItem);

            BonusData bonusData = new(itemInstance);
            if (!bonusData.CanDisenchant)
                return null;

            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(m_lootItem.itemid);

            // ignore temporary item level scaling (pvp or timewalking)
            uint itemLevel = Item.GetItemLevel(itemTemplate, bonusData, (uint)bonusData.RequiredLevel, 0, 0, 0, 0, false, 0);

            var disenchantLoot = Item.GetBaseDisenchantLoot(itemTemplate, (uint)bonusData.Quality, itemLevel);
            if (disenchantLoot == null)
                return null;

            return disenchantLoot.SkillRequired;
        }

        // terminate the roll
        void Finish(KeyValuePair<ObjectGuid, PlayerRollVote> winnerPair)
        {
            m_lootItem.is_blocked = false;
            if (winnerPair.Value == null)
            {
                SendAllPassed();
            }
            else
            {
                m_lootItem.rollWinnerGUID = winnerPair.Key;

                SendLootRollWon(winnerPair.Key, winnerPair.Value.RollNumber, winnerPair.Value.Vote);

                Player player = Global.ObjAccessor.FindConnectedPlayer(winnerPair.Key);
                if (player != null)
                {
                    if (winnerPair.Value.Vote == RollVote.Need)
                        player.UpdateCriteria(CriteriaType.RollNeed, m_lootItem.itemid, winnerPair.Value.RollNumber);
                    else if (winnerPair.Value.Vote == RollVote.Disenchant)
                        player.UpdateCriteria(CriteriaType.CastSpell, 13262);
                    else
                        player.UpdateCriteria(CriteriaType.RollGreed, m_lootItem.itemid, winnerPair.Value.RollNumber);

                    if (winnerPair.Value.Vote == RollVote.Disenchant)
                    {
                        Loot loot = new(m_map, m_loot.GetOwnerGUID(), LootType.Disenchanting, null);
                        loot.FillLoot(GetItemDisenchantLootId().GetValueOrDefault(), LootStorage.Disenchant, player, true, false, LootModes.Default, ItemContext.None);
                        if (!loot.AutoStore(player, ItemConst.NullBag, ItemConst.NullSlot, true))
                        {
                            for (uint i = 0; i < loot.items.Count; ++i)
                            {
                                LootItem disenchantLoot = loot.LootItemInSlot(i, player);
                                if (disenchantLoot != null && disenchantLoot.type == LootItemType.Item)
                                    player.SendItemRetrievalMail(disenchantLoot.itemid, disenchantLoot.count, disenchantLoot.context);
                            }
                        }
                        else
                            m_loot.NotifyItemRemoved((byte)m_lootItem.LootListId, m_map);
                    }
                    else
                        player.StoreLootItem(m_loot.GetOwnerGUID(), (byte)m_lootItem.LootListId, m_loot);
                }
            }
            m_isStarted = false;
        }
    }

    public class Loot
    {
        public Loot(Map map, ObjectGuid owner, LootType type, Group group)
        {
            loot_type = type;
            _guid = map != null ? ObjectGuid.Create(HighGuid.LootObject, map.GetId(), 0, map.GenerateLowGuid(HighGuid.LootObject)) : ObjectGuid.Empty;
            _owner = owner;
            _itemContext = ItemContext.None;
            _lootMethod = group != null ? group.GetLootMethod() : LootMethod.FreeForAll;
            _lootMaster = group != null ? group.GetMasterLooterGuid() : ObjectGuid.Empty;
        }

        // Inserts the item into the loot (called by LootTemplate processors)
        public void AddItem(LootStoreItem item)
        {
            switch (item.type)
            {
                case LootStoreItemType.Item:
                {
                    ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(item.itemid);
                    if (proto == null)
                        return;

                    uint count = RandomHelper.URand(item.mincount, item.maxcount);
                    uint stacks = (uint)(count / proto.GetMaxStackSize() + (Convert.ToBoolean(count % proto.GetMaxStackSize()) ? 1 : 0));

                    for (uint i = 0; i < stacks && items.Count < SharedConst.MaxNRLootItems; ++i)
                    {
                        LootItem generatedLoot = new(item);
                        generatedLoot.context = _itemContext;
                        generatedLoot.count = (byte)Math.Min(count, proto.GetMaxStackSize());
                        generatedLoot.LootListId = (uint)items.Count;
                        generatedLoot.BonusListIDs = ItemBonusMgr.GetBonusListsForItem(generatedLoot.itemid, new(_itemContext));

                        items.Add(generatedLoot);
                        count -= proto.GetMaxStackSize();
                    }
                    break;
                }
                case LootStoreItemType.Currency:
                {
                    LootItem generatedLoot = new(item);
                    generatedLoot.count = RandomHelper.URand(item.mincount, item.maxcount);
                    generatedLoot.LootListId = (uint)items.Count;
                    items.Add(generatedLoot);
                    break;
                }
                case LootStoreItemType.TrackingQuest:
                {
                    LootItem generatedLoot = new(item);
                    generatedLoot.count = 1;
                    generatedLoot.LootListId = (uint)items.Count;
                    items.Add(generatedLoot);
                    break;
                }
                default:
                    break;
            }
        }

        public bool AutoStore(Player player, byte bag, byte slot, bool broadcast = false, bool createdByPlayer = false)
        {
            bool allLooted = true;
            for (uint i = 0; i < items.Count; ++i)
            {
                LootItem lootItem = LootItemInSlot(i, player, out NotNormalLootItem ffaitem);
                if (lootItem == null || lootItem.is_looted)
                    continue;

                if (!lootItem.HasAllowedLooter(GetGUID()))
                    continue;

                if (lootItem.is_blocked)
                    continue;

                // dont allow protected item to be looted by someone else
                if (!lootItem.rollWinnerGUID.IsEmpty() && lootItem.rollWinnerGUID != GetGUID())
                    continue;

                switch (lootItem.type)
                {
                    case LootItemType.Item:
                        List<ItemPosCount> dest = new();
                        InventoryResult msg = player.CanStoreNewItem(bag, slot, dest, lootItem.itemid, lootItem.count);
                        if (msg != InventoryResult.Ok && slot != ItemConst.NullSlot)
                            msg = player.CanStoreNewItem(bag, ItemConst.NullSlot, dest, lootItem.itemid, lootItem.count);
                        if (msg != InventoryResult.Ok && bag != ItemConst.NullBag)
                            msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, lootItem.itemid, lootItem.count);
                        if (msg != InventoryResult.Ok)
                        {
                            player.SendEquipError(msg, null, null, lootItem.itemid);
                            allLooted = false;
                            continue;
                        }

                        Item pItem = player.StoreNewItem(dest, lootItem.itemid, true, lootItem.randomBonusListId, null, lootItem.context, lootItem.BonusListIDs);
                        if (pItem != null)
                        {
                            player.SendNewItem(pItem, lootItem.count, false, createdByPlayer, broadcast, GetDungeonEncounterId());
                            player.ApplyItemLootedSpell(pItem, true);
                        }
                        else
                            player.ApplyItemLootedSpell(Global.ObjectMgr.GetItemTemplate(lootItem.itemid));

                        break;
                    case LootItemType.Currency:
                        player.ModifyCurrency(lootItem.itemid, (int)lootItem.count, CurrencyGainSource.Loot);
                        break;
                    case LootItemType.TrackingQuest:
                        Quest quest = Global.ObjectMgr.GetQuestTemplate(lootItem.itemid);
                        if (quest != null)
                            player.RewardQuest(quest, LootItemType.Item, 0, player, false);
                        break;
                }
                if (ffaitem != null)
                    ffaitem.is_looted = true;

                if (!lootItem.freeforall)
                    lootItem.is_looted = true;

                --unlootedCount;
            }

            return allLooted;
        }

        void AutoStoreTrackingQuests(Player player, List<NotNormalLootItem> ffaItems)
        {
            foreach (NotNormalLootItem ffaItem in ffaItems)
            {
                if (items[ffaItem.LootListId].type != LootItemType.TrackingQuest)
                    continue;

                --unlootedCount;
                ffaItem.is_looted = true;
                Quest quest = Global.ObjectMgr.GetQuestTemplate(items[ffaItem.LootListId].itemid);
                if (quest != null)
                    player.RewardQuest(quest, LootItemType.Item, 0, player, false);
            }
        }


        public void LootMoney()
        {
            gold = 0;
            _changed = true;
        }

        public LootItem GetItemInSlot(uint lootListId)
        {
            if (lootListId < items.Count)
                return items[(int)lootListId];

            return null;
        }

        // Calls processor of corresponding LootTemplate (which handles everything including references)
        public bool FillLoot(uint lootId, LootStore store, Player lootOwner, bool personal, bool noEmptyError = false, LootModes lootMode = LootModes.Default, ItemContext context = 0)
        {
            // Must be provided
            if (lootOwner == null)
                return false;

            LootTemplate tab = store.GetLootFor(lootId);
            if (tab == null)
            {
                if (!noEmptyError)
                    Log.outError(LogFilter.Sql, "Table '{0}' loot id #{1} used but it doesn't have records.", store.GetName(), lootId);
                return false;
            }

            _itemContext = context;

            tab.Process(this, store.IsRatesAllowed(), (byte)lootMode, 0);          // Processing is done there, callback via Loot.AddItem()

            // Setting access rights for group loot case
            Group group = lootOwner.GetGroup();
            if (!personal && group != null)
            {
                if (loot_type == LootType.Corpse)
                    roundRobinPlayer = lootOwner.GetGUID();

                for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                {
                    Player player = refe.GetSource();
                    if (player != null)   // should actually be looted object instead of lootOwner but looter has to be really close so doesnt really matter
                        if (player.IsAtGroupRewardDistance(lootOwner))
                            FillNotNormalLootFor(player);
                }

                foreach (LootItem item in items)
                {
                    if (!item.follow_loot_rules || item.freeforall || item.type != LootItemType.Item)
                        continue;

                    ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(item.itemid);
                    if (proto != null)
                    {
                        if (proto.GetQuality() < group.GetLootThreshold())
                            item.is_underthreshold = true;
                        else
                        {
                            switch (_lootMethod)
                            {
                                case LootMethod.MasterLoot:
                                case LootMethod.GroupLoot:
                                case LootMethod.NeedBeforeGreed:
                                {
                                    item.is_blocked = true;
                                    break;
                                }
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            // ... for personal loot
            else
                FillNotNormalLootFor(lootOwner);

            return true;
        }

        public void Update()
        {
            foreach (var pair in _rolls.ToList())
                if (pair.Value.UpdateRoll())
                    _rolls.Remove(pair.Key);
        }

        public void FillNotNormalLootFor(Player player)
        {
            ObjectGuid plguid = player.GetGUID();
            _allowedLooters.Add(plguid);

            List<NotNormalLootItem> ffaItems = new();

            foreach (LootItem item in items)
            {
                if (!item.AllowedForPlayer(player, this))
                    continue;

                item.AddAllowedLooter(player);

                if (item.freeforall)
                {
                    ffaItems.Add(new NotNormalLootItem((byte)item.LootListId));
                    ++unlootedCount;
                }

                else if (!item.is_counted)
                {
                    item.is_counted = true;
                    ++unlootedCount;
                }
            }

            if (!ffaItems.Empty())
            {
                // TODO: flag immediately for loot that is supposed to be mailed if unlooted, otherwise flag when sending SMSG_LOOT_RESPONSE
                //if (_mailUnlootedItems)
                //    AutoStoreTrackingQuests(player, *ffaItems);

                PlayerFFAItems[player.GetGUID()] = ffaItems;
            }
        }

        public void NotifyItemRemoved(byte lootListId, Map map)
        {
            // notify all players that are looting this that the item was removed
            // convert the index to the slot the player sees
            for (int i = 0; i < PlayersLooting.Count; ++i)
            {
                LootItem item = items[lootListId];
                if (!item.GetAllowedLooters().Contains(PlayersLooting[i]))
                    continue;

                Player player = Global.ObjAccessor.GetPlayer(map, PlayersLooting[i]);
                if (player != null)
                    player.SendNotifyLootItemRemoved(GetGUID(), GetOwnerGUID(), lootListId);
                else
                    PlayersLooting.RemoveAt(i);
            }
        }

        public void NotifyMoneyRemoved(Map map)
        {
            // notify all players that are looting this that the money was removed
            for (var i = 0; i < PlayersLooting.Count; ++i)
            {
                Player player = Global.ObjAccessor.GetPlayer(map, PlayersLooting[i]);
                if (player != null)
                    player.SendNotifyLootMoneyRemoved(GetGUID());
                else
                    PlayersLooting.RemoveAt(i);
            }
        }

        public void OnLootOpened(Map map, Player looter)
        {
            AddLooter(looter.GetGUID());
            if (!_wasOpened)
            {
                _wasOpened = true;

                if (_lootMethod == LootMethod.GroupLoot || _lootMethod == LootMethod.NeedBeforeGreed)
                {
                    ushort maxEnchantingSkill = 0;
                    foreach (ObjectGuid allowedLooterGuid in _allowedLooters)
                    {
                        Player allowedLooter = Global.ObjAccessor.GetPlayer(map, allowedLooterGuid);
                        if (allowedLooter != null)
                            maxEnchantingSkill = Math.Max(maxEnchantingSkill, allowedLooter.GetSkillValue(SkillType.Enchanting));
                    }

                    for (uint lootListId = 0; lootListId < items.Count; ++lootListId)
                    {
                        LootItem item = items[(int)lootListId];
                        if (!item.is_blocked)
                            continue;

                        LootRoll lootRoll = new();
                        var inserted = _rolls.TryAdd(lootListId, lootRoll);
                        if (!lootRoll.TryToStart(map, this, lootListId, maxEnchantingSkill))
                            _rolls.Remove(lootListId);
                    }

                    if (!_rolls.Empty())
                        _changed = true;
                }
                else if (_lootMethod == LootMethod.MasterLoot)
                {
                    if (looter.GetGUID() == _lootMaster)
                    {
                        MasterLootCandidateList masterLootCandidateList = new();
                        masterLootCandidateList.LootObj = GetGUID();
                        masterLootCandidateList.Players = _allowedLooters;
                        looter.SendPacket(masterLootCandidateList);
                    }
                }
            }

            // Flag tracking quests as completed after all items were scanned for this player (some might depend on this quest not being completed)
            //if (!_mailUnlootedItems)
            var ffaItems = PlayerFFAItems.LookupByKey(looter.GetGUID());
            if (ffaItems != null)
                AutoStoreTrackingQuests(looter, ffaItems);
        }

        public bool HasAllowedLooter(ObjectGuid looter)
        {
            return _allowedLooters.Contains(looter);
        }

        public void GenerateMoneyLoot(uint minAmount, uint maxAmount)
        {
            if (maxAmount > 0)
            {
                if (maxAmount <= minAmount)
                    gold = (uint)(maxAmount * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));
                else if ((maxAmount - minAmount) < 32700)
                    gold = (uint)(RandomHelper.URand(minAmount, maxAmount) * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));
                else
                    gold = (uint)(RandomHelper.URand(minAmount >> 8, maxAmount >> 8) * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney)) << 8;
            }
        }

        public LootItem LootItemInSlot(uint lootSlot, Player player)
        {
            return LootItemInSlot(lootSlot, player, out _);
        }

        public LootItem LootItemInSlot(uint lootListId, Player player, out NotNormalLootItem ffaItem)
        {
            ffaItem = null;

            if (lootListId >= items.Count)
                return null;

            LootItem item = items[(int)lootListId];
            bool is_looted = item.is_looted;

            if (item.freeforall)
            {
                var itemList = PlayerFFAItems.LookupByKey(player.GetGUID());
                if (itemList != null)
                {
                    foreach (NotNormalLootItem notNormalLootItem in itemList)
                    {
                        if (notNormalLootItem.LootListId == lootListId)
                        {
                            is_looted = notNormalLootItem.is_looted;
                            ffaItem = notNormalLootItem;
                            break;
                        }
                    }
                }
            }

            if (is_looted)
                return null;

            _changed = true;
            return item;
        }

        // return true if there is any item that is lootable for any player (not quest item, FFA or conditional)
        public bool HasItemForAll()
        {
            // Gold is always lootable
            if (gold != 0)
                return true;

            foreach (LootItem item in items)
                if (!item.is_looted && item.follow_loot_rules && !item.freeforall && item.conditions.IsEmpty())
                    return true;

            return false;
        }

        // return true if there is any FFA, quest or conditional item for the player.
        public bool HasItemFor(Player player)
        {
            // quest items
            foreach (LootItem lootItem in items)
                if (!lootItem.is_looted && !lootItem.follow_loot_rules && lootItem.GetAllowedLooters().Contains(player.GetGUID()))
                    return true;

            var ffaItems = GetPlayerFFAItems().LookupByKey(player.GetGUID());
            if (ffaItems != null && ffaItems.Any(ffaItem => !ffaItem.is_looted))
                return true;

            return false;
        }

        // return true if there is any item over the group threshold (i.e. not underthreshold).
        public bool HasOverThresholdItem()
        {
            for (byte i = 0; i < items.Count; ++i)
            {
                if (!items[i].is_looted && !items[i].is_underthreshold && !items[i].freeforall)
                    return true;
            }

            return false;
        }

        public void BuildLootResponse(LootResponse packet, Player viewer)
        {
            packet.Coins = gold;

            foreach (LootItem item in items)
            {
                var uiType = item.GetUiTypeForPlayer(viewer, this);
                if (!uiType.HasValue)
                    continue;

                switch (item.type)
                {
                    case LootItemType.Item:
                    {
                        LootItemData lootItem = new();
                        lootItem.LootListID = (byte)item.LootListId;
                        lootItem.UIType = uiType.Value;
                        lootItem.Type = (byte)item.type;
                        lootItem.Quantity = item.count;
                        lootItem.Loot = new(item);
                        packet.Items.Add(lootItem);
                        break;
                    }
                    case LootItemType.Currency:
                    {
                        LootCurrency lootCurrency = new();
                        lootCurrency.CurrencyID = item.itemid;
                        lootCurrency.Quantity = item.count;
                        lootCurrency.LootListID = (byte)item.LootListId;
                        lootCurrency.UIType = (byte)uiType.Value;

                        // fake visible quantity for SPELL_AURA_MOD_CURRENCY_CATEGORY_GAIN_PCT - handled in Player::ModifyCurrency
                        lootCurrency.Quantity = (uint)((float)lootCurrency.Quantity * viewer.GetTotalAuraMultiplierByMiscValue(AuraType.ModCurrencyCategoryGainPct, CliDB.CurrencyTypesStorage.LookupByKey(item.itemid).CategoryID));
                        packet.Currencies.Add(lootCurrency);
                        break;
                    }
                    default:
                        break;
                }
            }
        }

        public void NotifyLootList(Map map)
        {
            LootList lootList = new();

            lootList.Owner = GetOwnerGUID();
            lootList.LootObj = GetGUID();

            if (GetLootMethod() == LootMethod.MasterLoot && HasOverThresholdItem())
                lootList.Master = GetLootMasterGUID();

            if (!roundRobinPlayer.IsEmpty())
                lootList.RoundRobinWinner = roundRobinPlayer;

            lootList.Write();

            foreach (ObjectGuid allowedLooterGuid in _allowedLooters)
            {
                Player allowedLooter = Global.ObjAccessor.GetPlayer(map, allowedLooterGuid);
                if (allowedLooter != null)
                    allowedLooter.SendPacket(lootList);
            }
        }

        public bool IsLooted() { return gold == 0 && unlootedCount == 0; }
        public bool IsChanged() { return _changed; }

        public void AddLooter(ObjectGuid guid) { PlayersLooting.Add(guid); }
        public void RemoveLooter(ObjectGuid guid) { PlayersLooting.Remove(guid); }

        public ObjectGuid GetGUID() { return _guid; }
        public ObjectGuid GetOwnerGUID() { return _owner; }
        public ItemContext GetItemContext() { return _itemContext; }
        public void SetItemContext(ItemContext context) { _itemContext = context; }
        public LootMethod GetLootMethod() { return _lootMethod; }

        public ObjectGuid GetLootMasterGUID() { return _lootMaster; }

        public uint GetDungeonEncounterId() { return _dungeonEncounterId; }
        public void SetDungeonEncounterId(uint dungeonEncounterId) { _dungeonEncounterId = dungeonEncounterId; }

        public MultiMap<ObjectGuid, NotNormalLootItem> GetPlayerFFAItems() { return PlayerFFAItems; }

        public List<LootItem> items = new();
        public uint gold;
        public byte unlootedCount;
        public ObjectGuid roundRobinPlayer;                                // GUID of the player having the Round-Robin ownership for the loot. If 0, round robin owner has released.
        public LootType loot_type;                                     // required for achievement system

        List<ObjectGuid> PlayersLooting = new();
        MultiMap<ObjectGuid, NotNormalLootItem> PlayerFFAItems = new();

        // Loot GUID
        ObjectGuid _guid;
        ObjectGuid _owner;                                              // The WorldObject that holds this loot
        ItemContext _itemContext;
        LootMethod _lootMethod;
        Dictionary<uint, LootRoll> _rolls = new();                    // used if an item is under rolling
        ObjectGuid _lootMaster;
        List<ObjectGuid> _allowedLooters = new();
        bool _wasOpened;                                                // true if at least one player received the loot content
        bool _changed;
        uint _dungeonEncounterId;
    }

    public class AELootResult
    {
        public void Add(Item item, byte count, LootType lootType, uint dungeonEncounterId)
        {
            var id = _byItem.LookupByKey(item);
            if (id != 0)
            {
                var resultValue = _byOrder[id];
                resultValue.count += count;
            }
            else
            {
                _byItem[item] = _byOrder.Count;
                ResultValue value;
                value.item = item;
                value.count = count;
                value.lootType = lootType;
                value.dungeonEncounterId = dungeonEncounterId;
                _byOrder.Add(value);
            }
        }

        public List<ResultValue> GetByOrder()
        {
            return _byOrder;
        }

        List<ResultValue> _byOrder = new();
        Dictionary<Item, int> _byItem = new();

        public struct ResultValue
        {
            public Item item;
            public byte count;
            public LootType lootType;
            public uint dungeonEncounterId;
        }
    }
}
