/*
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
        public LootItem() { }
        public LootItem(LootStoreItem li)
        {
            itemid = li.itemid;
            conditions = li.conditions;

            ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemid);
            freeforall = proto != null && proto.HasFlag(ItemFlags.MultiDrop);
            follow_loot_rules = !li.needs_quest || (proto != null && proto.FlagsCu.HasAnyFlag(ItemFlagsCustom.FollowLootRules));

            needs_quest = li.needs_quest;

            randomBonusListId = ItemEnchantmentManager.GenerateItemRandomBonusListId(itemid);
        }

        public bool AllowedForPlayer(Player player, bool isGivenByMasterLooter = false)
        {
            // DB conditions check
            if (!Global.ConditionMgr.IsObjectMeetToConditions(player, conditions))
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
            if (!isGivenByMasterLooter && player.GetGroup() && player.GetGroup().GetMasterLooterGuid() == player.GetGUID())
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

            // Don't allow to loot soulbound recipes that the player has already learned
            if (pProto.GetClass() == ItemClass.Recipe && pProto.GetBonding() == ItemBondingType.OnAcquire)
            {
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

            return true;
        }

        public void AddAllowedLooter(Player player)
        {
            allowedGUIDs.Add(player.GetGUID());
        }

        public List<ObjectGuid> GetAllowedLooters() { return allowedGUIDs; }

        public uint itemid;
        public uint LootListId;
        public uint randomBonusListId;
        public List<uint> BonusListIDs = new();
        public ItemContext context;
        public List<Condition> conditions = new();                               // additional loot condition
        public List<ObjectGuid> allowedGUIDs = new();
        public ObjectGuid rollWinnerGUID;                                   // Stores the guid of person who won loot, if his bags are full only he can see the item in loot list!
        public byte count;
        public bool is_looted;
        public bool is_blocked;
        public bool freeforall;                          // free for all
        public bool is_underthreshold;
        public bool is_counted;
        public bool needs_quest;                          // quest drop
        public bool follow_loot_rules;
    }

    public class NotNormalLootItem
    {
        public byte index;                                          // position in quest_items or items;
        public bool is_looted;

        public NotNormalLootItem()
        {
            index = 0;
            is_looted = false;
        }

        public NotNormalLootItem(byte _index, bool _islooted = false)
        {
            index = _index;
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
        uint m_lootListId;
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
                if (!player)
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
                if (m_loot.GetLootMethod() == LootMethod.NeedBeforeGreed && player.CanRollForItemInLFG(itemTemplate, m_map) != InventoryResult.Ok)
                    startLootRoll.ValidRolls &= ~RollMask.Need;

                FillPacket(startLootRoll.Item);
                startLootRoll.Item.UIType = LootSlotType.RollOngoing;

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
            lootItem.LootListID = (byte)(m_lootListId + 1);
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
                m_lootListId = lootListId;
                m_lootItem.is_blocked = true;                          // block the item while rolling

                uint playerCount = 0;
                foreach (ObjectGuid allowedLooter in m_lootItem.GetAllowedLooters())
                {
                    Player plr = Global.ObjAccessor.GetPlayer(m_map, allowedLooter);
                    if (!plr || !m_lootItem.AllowedForPlayer(plr))     // check if player meet the condition to be able to roll this item
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
                var disenchant = GetItemDisenchantLoot();
                if (disenchant == null || disenchant.SkillRequired > enchantingSkill)
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
            return m_loot.GetGUID() == lootObject && m_lootListId == lootListId;
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

        ItemDisenchantLootRecord GetItemDisenchantLoot()
        {
            ItemInstance itemInstance = new(m_lootItem);

            BonusData bonusData = new(itemInstance);
            if (!bonusData.CanDisenchant)
                return null;

            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(m_lootItem.itemid);
            uint itemLevel = Item.GetItemLevel(itemTemplate, bonusData, 1, 0, 0, 0, 0, false, 0);
            return Item.GetDisenchantLoot(itemTemplate, (uint)bonusData.Quality, itemLevel);
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
                        var disenchant = GetItemDisenchantLoot();
                        Loot loot = new(m_map, m_loot.GetOwnerGUID(), LootType.Disenchanting, null);
                        loot.FillLoot(disenchant.Id, LootStorage.Disenchant, player, true, false, LootModes.Default, ItemContext.None);
                        if (!loot.AutoStore(player, ItemConst.NullBag, ItemConst.NullSlot, true))
                        {
                            uint maxSlot = loot.GetMaxSlotInLootFor(player);
                            for (uint i = 0; i < maxSlot; ++i)
                            {
                                LootItem disenchantLoot = loot.LootItemInSlot(i, player);
                                if (disenchantLoot != null)
                                    player.SendItemRetrievalMail(disenchantLoot.itemid, disenchantLoot.count, disenchantLoot.context);
                            }
                        }
                        else
                            m_loot.NotifyItemRemoved((byte)m_lootItem.LootListId, m_map);
                    }
                    else
                        player.StoreLootItem(m_loot.GetOwnerGUID(), (byte)m_lootListId, m_loot);
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
            maxDuplicates = 1;
            _guid = map ? ObjectGuid.Create(HighGuid.LootObject, map.GetId(), 0, map.GenerateLowGuid(HighGuid.LootObject)) : ObjectGuid.Empty;
            _owner = owner;
            _itemContext = ItemContext.None;
            _lootMethod = group != null ? group.GetLootMethod() : LootMethod.FreeForAll;
            _lootMaster = group != null ? group.GetMasterLooterGuid() : ObjectGuid.Empty;
        }

        // Inserts the item into the loot (called by LootTemplate processors)
        public void AddItem(LootStoreItem item, Player player)
        {
            ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(item.itemid);
            if (proto == null)
                return;

            uint count = RandomHelper.URand(item.mincount, item.maxcount);
            uint stacks = (uint)(count / proto.GetMaxStackSize() + (Convert.ToBoolean(count % proto.GetMaxStackSize()) ? 1 : 0));

            List<LootItem> lootItems = item.needs_quest ? quest_items : items;
            uint limit = (uint)(item.needs_quest ? SharedConst.MaxNRQuestItems : SharedConst.MaxNRLootItems);

            for (uint i = 0; i < stacks && lootItems.Count < limit; ++i)
            {
                LootItem generatedLoot = new(item);
                generatedLoot.context = _itemContext;
                generatedLoot.count = (byte)Math.Min(count, proto.GetMaxStackSize());
                generatedLoot.LootListId = (uint)lootItems.Count;
                if (_itemContext != 0)
                {
                    List<uint> bonusListIDs = Global.DB2Mgr.GetDefaultItemBonusTree(generatedLoot.itemid, _itemContext);
                    generatedLoot.BonusListIDs.AddRange(bonusListIDs);
                }
                lootItems.Add(generatedLoot);
                count -= proto.GetMaxStackSize();

                // In some cases, a dropped item should be visible/lootable only for some players in group
                bool canSeeItemInLootWindow = false;
                Group group = player.GetGroup();
                if (group != null)
                {
                    for (GroupReference itr = group.GetFirstMember(); itr != null; itr = itr.Next())
                    {
                        Player member = itr.GetSource();
                        if (member != null)
                            if (generatedLoot.AllowedForPlayer(member))
                                canSeeItemInLootWindow = true;
                    }
                }
                else if (generatedLoot.AllowedForPlayer(player))
                    canSeeItemInLootWindow = true;

                if (!canSeeItemInLootWindow)
                    continue;

                // non-conditional one-player only items are counted here,
                // free for all items are counted in FillFFALoot(),
                // non-ffa conditionals are counted in FillNonQuestNonFFAConditionalLoot()
                if (!item.needs_quest && item.conditions.Empty() && !proto.HasFlag(ItemFlags.MultiDrop))
                    ++unlootedCount;
            }
        }

        public bool AutoStore(Player player, byte bag, byte slot, bool broadcast, bool createdByPlayer = false)
        {
            bool allLooted = true;
            uint max_slot = GetMaxSlotInLootFor(player);
            for (uint i = 0; i < max_slot; ++i)
            {
                NotNormalLootItem qitem = null;
                NotNormalLootItem ffaitem = null;
                NotNormalLootItem conditem = null;

                LootItem lootItem = LootItemInSlot(i, player, out qitem, out ffaitem, out conditem);
                if (lootItem == null || lootItem.is_looted)
                    continue;

                if (!lootItem.AllowedForPlayer(player))
                    continue;

                if (qitem == null && lootItem.is_blocked)
                    continue;

                // dont allow protected item to be looted by someone else
                if (!lootItem.rollWinnerGUID.IsEmpty() && lootItem.rollWinnerGUID != GetGUID())
                    continue;

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

                if (qitem != null)
                    qitem.is_looted = true;
                else if (ffaitem != null)
                    ffaitem.is_looted = true;
                else if (conditem != null)
                    conditem.is_looted = true;

                if (!lootItem.freeforall)
                    lootItem.is_looted = true;

                --unlootedCount;

                Item pItem = player.StoreNewItem(dest, lootItem.itemid, true, lootItem.randomBonusListId, null, lootItem.context, lootItem.BonusListIDs);
                player.SendNewItem(pItem, lootItem.count, false, createdByPlayer, broadcast);
                player.ApplyItemLootedSpell(pItem, true);
            }

            return allLooted;
        }

        public LootItem GetItemInSlot(uint lootSlot)
        {
            if (lootSlot < items.Count)
                return items[(int)lootSlot];

            lootSlot -= (uint)items.Count;
            if (lootSlot < quest_items.Count)
                return quest_items[(int)lootSlot];

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

            tab.Process(this, store.IsRatesAllowed(), (byte)lootMode, 0, lootOwner);          // Processing is done there, callback via Loot.AddItem()

            // Setting access rights for group loot case
            Group group = lootOwner.GetGroup();
            if (!personal && group != null)
            {
                roundRobinPlayer = lootOwner.GetGUID();

                for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                {
                    Player player = refe.GetSource();
                    if (player)   // should actually be looted object instead of lootOwner but looter has to be really close so doesnt really matter
                        if (player.IsAtGroupRewardDistance(lootOwner))
                            FillNotNormalLootFor(player);
                }

                void processLootItem(LootItem item)
                {
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
                };

                foreach (LootItem item in items)
                {
                    if (item.freeforall)
                        continue;

                    processLootItem(item);
                }

                foreach (LootItem item in quest_items)
                {
                    if (!item.follow_loot_rules)
                        continue;

                    processLootItem(item);
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

        void FillNotNormalLootFor(Player player)
        {
            ObjectGuid plguid = player.GetGUID();
            _allowedLooters.Add(plguid);

            var questItemList = PlayerQuestItems.LookupByKey(plguid);
            if (questItemList.Empty())
                FillQuestLoot(player);

            questItemList = PlayerFFAItems.LookupByKey(plguid);
            if (questItemList.Empty())
                FillFFALoot(player);

            questItemList = PlayerNonQuestNonFFAConditionalItems.LookupByKey(plguid);
            if (questItemList.Empty())
                FillNonQuestNonFFAConditionalLoot(player);
        }

        List<NotNormalLootItem> FillFFALoot(Player player)
        {
            List<NotNormalLootItem> ql = new();

            for (byte i = 0; i < items.Count; ++i)
            {
                LootItem item = items[i];
                if (!item.is_looted && item.freeforall && item.AllowedForPlayer(player))
                {
                    ql.Add(new NotNormalLootItem(i));
                    ++unlootedCount;
                }
            }
            if (ql.Empty())
                return null;


            PlayerFFAItems[player.GetGUID()] = ql;
            return ql;
        }

        List<NotNormalLootItem> FillQuestLoot(Player player)
        {
            if (items.Count == SharedConst.MaxNRLootItems)
                return null;

            List<NotNormalLootItem> ql = new();

            for (byte i = 0; i < quest_items.Count; ++i)
            {
                LootItem item = quest_items[i];

                if (!item.is_looted && (item.AllowedForPlayer(player) || (item.follow_loot_rules && player.GetGroup() && ((GetLootMethod() == LootMethod.MasterLoot && player.GetGroup().GetMasterLooterGuid() == player.GetGUID()) || GetLootMethod() != LootMethod.MasterLoot))))
                {
                    item.AddAllowedLooter(player);

                    ql.Add(new NotNormalLootItem(i));

                    // quest items get blocked when they first appear in a
                    // player's quest vector
                    //
                    // increase once if one looter only, looter-times if free for all
                    if (item.freeforall || !item.is_blocked)
                        ++unlootedCount;
                    if (!player.GetGroup() || GetLootMethod() != LootMethod.GroupLoot && GetLootMethod() != LootMethod.RoundRobin)
                        item.is_blocked = true;

                    if (items.Count + ql.Count == SharedConst.MaxNRLootItems)
                        break;
                }
            }
            if (ql.Empty())
                return null;

            PlayerQuestItems[player.GetGUID()] = ql;
            return ql;
        }

        List<NotNormalLootItem> FillNonQuestNonFFAConditionalLoot(Player player)
        {
            List<NotNormalLootItem> ql = new();

            for (byte i = 0; i < items.Count; ++i)
            {
                LootItem item = items[i];
                if (!item.is_looted && !item.freeforall && item.AllowedForPlayer(player))
                {
                    item.AddAllowedLooter(player);
                    if (!item.conditions.Empty())
                    {
                        ql.Add(new NotNormalLootItem(i));
                        if (!item.is_counted)
                        {
                            ++unlootedCount;
                            item.is_counted = true;
                        }
                    }
                }
            }
            if (ql.Empty())
                return null;

            PlayerNonQuestNonFFAConditionalItems[player.GetGUID()] = ql;
            return ql;
        }

        public void NotifyItemRemoved(byte lootIndex, Map map)
        {
            // notify all players that are looting this that the item was removed
            // convert the index to the slot the player sees
            for (int i = 0; i < PlayersLooting.Count; ++i)
            {
                Player player = Global.ObjAccessor.GetPlayer(map, PlayersLooting[i]);
                if (player != null)
                    player.SendNotifyLootItemRemoved(GetGUID(), GetOwnerGUID(), lootIndex);
                else
                    PlayersLooting.RemoveAt(i);
            }
        }

        public void NotifyQuestItemRemoved(byte questIndex, Map map)
        {
            // when a free for all questitem is looted
            // all players will get notified of it being removed
            // (other questitems can be looted by each group member)
            // bit inefficient but isn't called often
            for (var i = 0; i < PlayersLooting.Count; ++i)
            {
                Player player = Global.ObjAccessor.GetPlayer(map, PlayersLooting[i]);
                if (player)
                {
                    var pql = PlayerQuestItems.LookupByKey(player.GetGUID());
                    if (!pql.Empty())
                    {
                        byte j;
                        for (j = 0; j < pql.Count; ++j)
                            if (pql[j].index == questIndex)
                                break;

                        if (j < pql.Count)
                            player.SendNotifyLootItemRemoved(GetGUID(), GetOwnerGUID(), (byte)(items.Count + j));
                    }
                }
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

        public void OnLootOpened(Map map, ObjectGuid looter)
        {
            AddLooter(looter);
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

                    uint lootListId = 0;
                    for (; lootListId < items.Count; ++lootListId)
                    {
                        LootItem item = items[(int)lootListId];
                        if (!item.is_blocked)
                            continue;

                        LootRoll lootRoll = new();
                        var inserted = _rolls.TryAdd(lootListId, lootRoll);
                        if (!lootRoll.TryToStart(map, this, lootListId, maxEnchantingSkill))
                            _rolls.Remove(lootListId);
                    }

                    for (; lootListId - items.Count < quest_items.Count; ++lootListId)
                    {
                        LootItem item = quest_items[(int)lootListId - items.Count];
                        if (!item.is_blocked)
                            continue;

                        LootRoll lootRoll = new();
                        var inserted = _rolls.TryAdd(lootListId, lootRoll);
                        if (!lootRoll.TryToStart(map, this, lootListId, maxEnchantingSkill))
                            _rolls.Remove(lootListId);
                    }
                }
                else if (_lootMethod == LootMethod.MasterLoot)
                {
                    if (looter == _lootMaster)
                    {
                        Player lootMaster = Global.ObjAccessor.GetPlayer(map, looter);
                        if (lootMaster != null)
                        {
                            MasterLootCandidateList masterLootCandidateList = new();
                            masterLootCandidateList.LootObj = GetGUID();
                            masterLootCandidateList.Players = _allowedLooters;
                            lootMaster.SendPacket(masterLootCandidateList);
                        }
                    }
                }
            }
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
            return LootItemInSlot(lootSlot, player, out _, out _, out _);
        }
        public LootItem LootItemInSlot(uint lootSlot, Player player, out NotNormalLootItem qitem, out NotNormalLootItem ffaitem, out NotNormalLootItem conditem)
        {
            qitem = null;
            ffaitem = null;
            conditem = null;

            LootItem item = null;
            bool is_looted = true;
            if (lootSlot >= items.Count)
            {
                int questSlot = (int)(lootSlot - items.Count);
                var questItems = PlayerQuestItems.LookupByKey(player.GetGUID());
                if (!questItems.Empty())
                {
                    NotNormalLootItem qitem2 = questItems[questSlot];
                    if (qitem2 != null)
                    {
                        qitem = qitem2;
                        item = quest_items[qitem2.index];
                        is_looted = qitem2.is_looted;
                    }
                }
            }
            else
            {
                item = items[(int)lootSlot];
                is_looted = item.is_looted;
                if (item.freeforall)
                {
                    var questItemList = PlayerFFAItems.LookupByKey(player.GetGUID());
                    if (!questItemList.Empty())
                    {
                        foreach (var c in questItemList)
                        {
                            if (c.index == lootSlot)
                            {
                                NotNormalLootItem ffaitem2 = c;
                                ffaitem = ffaitem2;
                                is_looted = ffaitem2.is_looted;
                                break;
                            }
                        }
                    }
                }
                else if (!item.conditions.Empty())
                {
                    var questItemList = PlayerNonQuestNonFFAConditionalItems.LookupByKey(player.GetGUID());
                    if (!questItemList.Empty())
                    {
                        foreach (var iter in questItemList)
                        {
                            if (iter.index == lootSlot)
                            {
                                NotNormalLootItem conditem2 = iter;
                                conditem = conditem2;
                                is_looted = conditem2.is_looted;
                                break;
                            }
                        }
                    }
                }
            }

            if (is_looted)
                return null;

            return item;
        }

        public uint GetMaxSlotInLootFor(Player player)
        {
            var questItemList = PlayerQuestItems.LookupByKey(player.GetGUID());
            return (uint)(items.Count + questItemList.Count);
        }

        // return true if there is any item that is lootable for any player (not quest item, FFA or conditional)
        public bool HasItemForAll()
        {
            // Gold is always lootable
            if (gold != 0)
                return true;

            foreach (LootItem item in items)
                if (!item.is_looted && !item.freeforall && item.conditions.Empty())
                    return true;

            return false;
        }

        // return true if there is any FFA, quest or conditional item for the player.
        public bool HasItemFor(Player player)
        {
            var lootPlayerQuestItems = GetPlayerQuestItems();
            var q_list = lootPlayerQuestItems.LookupByKey(player.GetGUID());
            if (!q_list.Empty())
            {
                foreach (var qi in q_list)
                {
                    LootItem item = quest_items[qi.index];
                    if (!qi.is_looted && !item.is_looted)
                        return true;
                }
            }

            var lootPlayerFFAItems = GetPlayerFFAItems();
            var ffa_list = lootPlayerFFAItems.LookupByKey(player.GetGUID());
            if (!ffa_list.Empty())
            {
                foreach (var fi in ffa_list)
                {
                    LootItem item = items[fi.index];
                    if (!fi.is_looted && !item.is_looted)
                        return true;
                }
            }

            var lootPlayerNonQuestNonFFAConditionalItems = GetPlayerNonQuestNonFFAConditionalItems();
            var conditional_list = lootPlayerNonQuestNonFFAConditionalItems.LookupByKey(player.GetGUID());
            if (!conditional_list.Empty())
            {
                foreach (var ci in conditional_list)
                {
                    LootItem item = items[ci.index];
                    if (!ci.is_looted && !item.is_looted)
                        return true;
                }
            }

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

        public void BuildLootResponse(LootResponse packet, Player viewer, PermissionTypes permission)
        {
            if (permission == PermissionTypes.None)
                return;

            packet.Coins = gold;

            switch (permission)
            {
                case PermissionTypes.Group:
                case PermissionTypes.Master:
                case PermissionTypes.Restricted:
                {
                    // if you are not the round-robin group looter, you can only see
                    // blocked rolled items and quest items, and !ffa items
                    for (byte i = 0; i < items.Count; ++i)
                    {
                        if (!items[i].is_looted && !items[i].freeforall && items[i].conditions.Empty() && items[i].AllowedForPlayer(viewer))
                        {
                            LootSlotType slot_type;

                            if (items[i].is_blocked) // for ML & restricted is_blocked = !is_underthreshold
                            {
                                switch (permission)
                                {
                                    case PermissionTypes.Group:
                                        slot_type = LootSlotType.RollOngoing;
                                        break;
                                    case PermissionTypes.Master:
                                    {
                                        if (viewer.GetGroup() && viewer.GetGroup().GetMasterLooterGuid() == viewer.GetGUID())
                                            slot_type = LootSlotType.Master;
                                        else
                                            slot_type = LootSlotType.Locked;
                                        break;
                                    }
                                    case PermissionTypes.Restricted:
                                        slot_type = LootSlotType.Locked;
                                        break;
                                    default:
                                        continue;
                                }
                            }
                            else if (roundRobinPlayer.IsEmpty() || viewer.GetGUID() == roundRobinPlayer || !items[i].is_underthreshold)
                            {
                                // no round robin owner or he has released the loot
                                // or it IS the round robin group owner
                                // => item is lootable
                                slot_type = LootSlotType.AllowLoot;
                            }
                            else if (!items[i].rollWinnerGUID.IsEmpty())
                            {
                                if (items[i].rollWinnerGUID == viewer.GetGUID())
                                    slot_type = LootSlotType.Owner;
                                else
                                    continue;
                            }
                            else
                                // item shall not be displayed.
                                continue;

                            LootItemData lootItem = new();
                            lootItem.LootListID = (byte)(i + 1);
                            lootItem.UIType = slot_type;
                            lootItem.Quantity = items[i].count;
                            lootItem.Loot = new ItemInstance(items[i]);
                            packet.Items.Add(lootItem);
                        }
                    }
                    break;
                }
                case PermissionTypes.RoundRobin:
                {
                    for (var i = 0; i < items.Count; ++i)
                    {
                        if (!items[i].is_looted && !items[i].freeforall && items[i].conditions.Empty() && items[i].AllowedForPlayer(viewer))
                        {
                            if (!roundRobinPlayer.IsEmpty() && viewer.GetGUID() != roundRobinPlayer)
                                // item shall not be displayed.
                                continue;

                            LootItemData lootItem = new();
                            lootItem.LootListID = (byte)(i + 1);
                            lootItem.UIType = LootSlotType.AllowLoot;
                            lootItem.Quantity = items[i].count;
                            lootItem.Loot = new(items[i]);
                            packet.Items.Add(lootItem);
                        }
                    }
                    break;
                }
                case PermissionTypes.All:
                case PermissionTypes.Owner:
                {
                    for (byte i = 0; i < items.Count; ++i)
                    {
                        if (!items[i].is_looted && !items[i].freeforall && items[i].conditions.Empty() && items[i].AllowedForPlayer(viewer))
                        {
                            LootItemData lootItem = new();
                            lootItem.LootListID = (byte)(i + 1);
                            lootItem.UIType = (permission == PermissionTypes.Owner ? LootSlotType.Owner : LootSlotType.AllowLoot);
                            lootItem.Quantity = items[i].count;
                            lootItem.Loot = new ItemInstance(items[i]);
                            packet.Items.Add(lootItem);
                        }
                    }
                    break;
                }
                default:
                    return;
            }

            LootSlotType slotType = permission == PermissionTypes.Owner ? LootSlotType.Owner : LootSlotType.AllowLoot;
            var lootPlayerQuestItems = GetPlayerQuestItems();
            var q_list = lootPlayerQuestItems.LookupByKey(viewer.GetGUID());
            if (!q_list.Empty())
            {
                for (var i = 0; i < q_list.Count; ++i)
                {
                    NotNormalLootItem qi = q_list[i];
                    LootItem item = quest_items[qi.index];
                    if (!qi.is_looted && !item.is_looted)
                    {
                        LootItemData lootItem = new();
                        lootItem.LootListID = (byte)(items.Count + i + 1);
                        lootItem.Quantity = item.count;
                        lootItem.Loot = new ItemInstance(item);

                        switch (permission)
                        {
                            case PermissionTypes.Master:
                                lootItem.UIType = LootSlotType.Master;
                                break;
                            case PermissionTypes.Restricted:
                                lootItem.UIType = item.is_blocked ? LootSlotType.Locked : LootSlotType.AllowLoot;
                                break;
                            case PermissionTypes.Group:
                            case PermissionTypes.RoundRobin:
                                if (!item.is_blocked)
                                    lootItem.UIType = LootSlotType.AllowLoot;
                                else
                                    lootItem.UIType = LootSlotType.RollOngoing;
                                break;
                            default:
                                lootItem.UIType = slotType;
                                break;
                        }

                        packet.Items.Add(lootItem);
                    }
                }
            }

            var lootPlayerFFAItems = GetPlayerFFAItems();
            var ffa_list = lootPlayerFFAItems.LookupByKey(viewer.GetGUID());
            if (!ffa_list.Empty())
            {
                foreach (var fi in ffa_list)
                {
                    LootItem item = items[fi.index];
                    if (!fi.is_looted && !item.is_looted)
                    {
                        LootItemData lootItem = new();
                        lootItem.LootListID = (byte)(fi.index + 1);
                        lootItem.UIType = slotType;
                        lootItem.Quantity = item.count;
                        lootItem.Loot = new ItemInstance(item);
                        packet.Items.Add(lootItem);
                    }
                }
            }

            var lootPlayerNonQuestNonFFAConditionalItems = GetPlayerNonQuestNonFFAConditionalItems();
            var conditional_list = lootPlayerNonQuestNonFFAConditionalItems.LookupByKey(viewer.GetGUID());
            if (!conditional_list.Empty())
            {
                foreach (var ci in conditional_list)
                {
                    LootItem item = items[ci.index];
                    if (!ci.is_looted && !item.is_looted)
                    {
                        LootItemData lootItem = new();
                        lootItem.LootListID = (byte)(ci.index + 1);
                        lootItem.Quantity = item.count;
                        lootItem.Loot = new ItemInstance(item);

                        if (item.follow_loot_rules)
                        {
                            switch (permission)
                            {
                                case PermissionTypes.Master:
                                    lootItem.UIType = LootSlotType.Master;
                                    break;
                                case PermissionTypes.Restricted:
                                    lootItem.UIType = item.is_blocked ? LootSlotType.Locked : LootSlotType.AllowLoot;
                                    break;
                                case PermissionTypes.Group:
                                case PermissionTypes.RoundRobin:
                                    if (!item.is_blocked)
                                        lootItem.UIType = LootSlotType.AllowLoot;
                                    else
                                        lootItem.UIType = LootSlotType.RollOngoing;
                                    break;
                                default:
                                    lootItem.UIType = slotType;
                                    break;
                            }
                        }
                        else
                            lootItem.UIType = slotType;

                        packet.Items.Add(lootItem);
                    }
                }
            }
        }

        public void Clear()
        {
            PlayerQuestItems.Clear();

            PlayerFFAItems.Clear();

            PlayerNonQuestNonFFAConditionalItems.Clear();

            foreach (ObjectGuid playerGuid in PlayersLooting)
            {
                Player player = Global.ObjAccessor.FindConnectedPlayer(playerGuid);
                if (player != null)
                    player.GetSession().DoLootRelease(this);
            }

            PlayersLooting.Clear();
            items.Clear();
            quest_items.Clear();
            gold = 0;
            unlootedCount = 0;
            roundRobinPlayer = ObjectGuid.Empty;
            _itemContext = 0;
            _rolls.Clear();
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

        public bool Empty() { return items.Empty() && gold == 0; }
        public bool IsLooted() { return gold == 0 && unlootedCount == 0; }

        public void AddLooter(ObjectGuid guid) { PlayersLooting.Add(guid); }
        public void RemoveLooter(ObjectGuid guid) { PlayersLooting.Remove(guid); }

        public ObjectGuid GetGUID() { return _guid; }
        public ObjectGuid GetOwnerGUID() { return _owner; }
        public LootMethod GetLootMethod() { return _lootMethod; }

        public ObjectGuid GetLootMasterGUID() { return _lootMaster; }

        public MultiMap<ObjectGuid, NotNormalLootItem> GetPlayerQuestItems() { return PlayerQuestItems; }
        public MultiMap<ObjectGuid, NotNormalLootItem> GetPlayerFFAItems() { return PlayerFFAItems; }
        public MultiMap<ObjectGuid, NotNormalLootItem> GetPlayerNonQuestNonFFAConditionalItems() { return PlayerNonQuestNonFFAConditionalItems; }

        public List<LootItem> items = new();
        public List<LootItem> quest_items = new();
        public uint gold;
        public byte unlootedCount;
        public ObjectGuid roundRobinPlayer;                                // GUID of the player having the Round-Robin ownership for the loot. If 0, round robin owner has released.
        public LootType loot_type;                                     // required for achievement system
        public byte maxDuplicates;                                    // Max amount of items with the same entry that can drop (default is 1; on 25 man raid mode 3)

        List<ObjectGuid> PlayersLooting = new();
        MultiMap<ObjectGuid, NotNormalLootItem> PlayerQuestItems = new();
        MultiMap<ObjectGuid, NotNormalLootItem> PlayerFFAItems = new();
        MultiMap<ObjectGuid, NotNormalLootItem> PlayerNonQuestNonFFAConditionalItems = new();

        // Loot GUID
        ObjectGuid _guid;
        ObjectGuid _owner;                                              // The WorldObject that holds this loot
        ItemContext _itemContext;
        LootMethod _lootMethod;
        Dictionary<uint, LootRoll> _rolls = new();                    // used if an item is under rolling
        ObjectGuid _lootMaster;
        List<ObjectGuid> _allowedLooters = new();
        bool _wasOpened;                                                // true if at least one player received the loot content
    }

    public class AELootResult
    {
        public void Add(Item item, byte count, LootType lootType)
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
        }
    }
}
