// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Loots
{
    public class LootRoll
    {
        private static readonly TimeSpan _lootRollTimeout = TimeSpan.FromMinutes(1);
        private readonly Dictionary<ObjectGuid, PlayerRollVote> _rollVoteMap = new();
        private DateTime _endTime = DateTime.MinValue;
        private bool _isStarted;
        private Loot _loot;
        private LootItem _lootItem;

        private Map _map;
        private RollMask _voteMask;

        // Try to start the group roll for the specified Item (it may fail for quest Item or any condition
        // If this method return false the roll have to be removed from the container to avoid any problem
        public bool TryToStart(Map map, Loot loot, uint lootListId, ushort enchantingSkill)
        {
            if (!_isStarted)
            {
                if (lootListId >= loot.Items.Count)
                    return false;

                _map = map;

                // initialize the _data needed for the roll
                _lootItem = loot.Items[(int)lootListId];

                _loot = loot;
                _lootItem.Is_blocked = true; // block the Item while rolling

                uint playerCount = 0;

                foreach (ObjectGuid allowedLooter in _lootItem.GetAllowedLooters())
                {
                    Player plr = Global.ObjAccessor.GetPlayer(_map, allowedLooter);

                    if (!plr ||
                        !_lootItem.HasAllowedLooter(plr.GetGUID())) // check if player meet the condition to be able to roll this Item
                    {
                        _rollVoteMap[allowedLooter].Vote = RollVote.NotValid;

                        continue;
                    }

                    // initialize player vote map
                    _rollVoteMap[allowedLooter].Vote = plr.GetPassOnGroupLoot() ? RollVote.Pass : RollVote.NotEmitedYet;

                    if (!plr.GetPassOnGroupLoot())
                        plr.AddLootRoll(this);

                    ++playerCount;
                }

                // initialize Item prototype and check enchant possibilities for this group
                ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(_lootItem.Itemid);
                _voteMask = RollMask.AllMask;

                if (itemTemplate.HasFlag(ItemFlags2.CanOnlyRollGreed))
                    _voteMask = _voteMask & ~RollMask.Need;

                var disenchant = GetItemDisenchantLoot();

                if (disenchant == null ||
                    disenchant.SkillRequired > enchantingSkill)
                    _voteMask = _voteMask & ~RollMask.Disenchant;

                if (playerCount > 1) // check if more than one player can loot this Item
                {
                    // start the roll
                    SendStartRoll();
                    _endTime = GameTime.Now() + _lootRollTimeout;
                    _isStarted = true;

                    return true;
                }

                // no need to start roll if one or less player can loot this Item so place it under threshold
                _lootItem.Is_underthreshold = true;
                _lootItem.Is_blocked = false;
            }

            return false;
        }

        // Add vote from playerGuid
        public bool PlayerVote(Player player, RollVote vote)
        {
            ObjectGuid playerGuid = player.GetGUID();

            if (!_rollVoteMap.TryGetValue(playerGuid, out PlayerRollVote voter))
                return false;

            voter.Vote = vote;

            if (vote != RollVote.Pass &&
                vote != RollVote.NotValid)
                voter.RollNumber = (byte)RandomHelper.URand(1, 100);

            switch (vote)
            {
                case RollVote.Pass: // Player choose pass
                    {
                        SendRoll(playerGuid, -1, RollVote.Pass, null);

                        break;
                    }
                case RollVote.Need: // player choose Need
                    {
                        SendRoll(playerGuid, 0, RollVote.Need, null);
                        player.UpdateCriteria(CriteriaType.RollAnyNeed, 1);

                        break;
                    }
                case RollVote.Greed: // player choose Greed
                    {
                        SendRoll(playerGuid, -1, RollVote.Greed, null);
                        player.UpdateCriteria(CriteriaType.RollAnyGreed, 1);

                        break;
                    }
                case RollVote.Disenchant: // player choose Disenchant
                    {
                        SendRoll(playerGuid, -1, RollVote.Disenchant, null);
                        player.UpdateCriteria(CriteriaType.RollAnyGreed, 1);

                        break;
                    }
                default: // Roll removed case
                    return false;
            }

            return true;
        }

        // check if we can found a winner for this roll or if timer is expired
        public bool UpdateRoll()
        {
            KeyValuePair<ObjectGuid, PlayerRollVote> winner = default;

            if (AllPlayerVoted(ref winner) ||
                _endTime <= GameTime.Now())
            {
                Finish(winner);

                return true;
            }

            return false;
        }

        public bool IsLootItem(ObjectGuid lootObject, uint lootListId)
        {
            return _loot.GetGUID() == lootObject && _lootItem.LootListId == lootListId;
        }

        // Send the roll for the whole group
        private void SendStartRoll()
        {
            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(_lootItem.Itemid);

            foreach (var (playerGuid, roll) in _rollVoteMap)
            {
                if (roll.Vote != RollVote.NotEmitedYet)
                    continue;

                Player player = Global.ObjAccessor.GetPlayer(_map, playerGuid);

                if (player == null)
                    continue;

                StartLootRoll startLootRoll = new();
                startLootRoll.LootObj = _loot.GetGUID();
                startLootRoll.MapID = (int)_map.GetId();
                startLootRoll.RollTime = (uint)_lootRollTimeout.TotalMilliseconds;
                startLootRoll.Method = _loot.GetLootMethod();
                startLootRoll.ValidRolls = _voteMask;

                // In NEED_BEFORE_GREED need disabled for non-usable Item for player
                if (_loot.GetLootMethod() == LootMethod.NeedBeforeGreed &&
                    player.CanRollNeedForItem(itemTemplate, _map, true) != InventoryResult.Ok)
                    startLootRoll.ValidRolls &= ~RollMask.Need;

                FillPacket(startLootRoll.Item);
                startLootRoll.Item.UIType = LootSlotType.RollOngoing;

                player.SendPacket(startLootRoll);
            }

            // Handle auto pass option
            foreach (var (playerGuid, roll) in _rollVoteMap)
            {
                if (roll.Vote != RollVote.Pass)
                    continue;

                SendRoll(playerGuid, -1, RollVote.Pass, null);
            }
        }

        // Send all passed message
        private void SendAllPassed()
        {
            LootAllPassed lootAllPassed = new();
            lootAllPassed.LootObj = _loot.GetGUID();
            FillPacket(lootAllPassed.Item);
            lootAllPassed.Item.UIType = LootSlotType.AllowLoot;
            lootAllPassed.Write();

            foreach (var (playerGuid, roll) in _rollVoteMap)
            {
                if (roll.Vote != RollVote.NotValid)
                    continue;

                Player player = Global.ObjAccessor.GetPlayer(_map, playerGuid);

                if (player == null)
                    continue;

                player.SendPacket(lootAllPassed);
            }
        }

        // Send roll of targetGuid to the whole group (included targuetGuid)
        private void SendRoll(ObjectGuid targetGuid, int rollNumber, RollVote rollType, ObjectGuid? rollWinner)
        {
            LootRollBroadcast lootRoll = new();
            lootRoll.LootObj = _loot.GetGUID();
            lootRoll.Player = targetGuid;
            lootRoll.Roll = rollNumber;
            lootRoll.RollType = rollType;
            lootRoll.Autopassed = false;
            FillPacket(lootRoll.Item);
            lootRoll.Item.UIType = LootSlotType.RollOngoing;
            lootRoll.Write();

            foreach (var (playerGuid, roll) in _rollVoteMap)
            {
                if (roll.Vote == RollVote.NotValid)
                    continue;

                if (playerGuid == rollWinner)
                    continue;

                Player player = Global.ObjAccessor.GetPlayer(_map, playerGuid);

                if (player == null)
                    continue;

                player.SendPacket(lootRoll);
            }

            if (rollWinner.HasValue)
            {
                Player player = Global.ObjAccessor.GetPlayer(_map, rollWinner.Value);

                if (player != null)
                {
                    lootRoll.Item.UIType = LootSlotType.AllowLoot;
                    lootRoll.Clear();
                    player.SendPacket(lootRoll);
                }
            }
        }

        // Send roll 'value' of the whole group and the winner to the whole group
        private void SendLootRollWon(ObjectGuid targetGuid, int rollNumber, RollVote rollType)
        {
            // Send roll values
            foreach (var (playerGuid, roll) in _rollVoteMap)
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

            LootRollWon lootRollWon = new();
            lootRollWon.LootObj = _loot.GetGUID();
            lootRollWon.Winner = targetGuid;
            lootRollWon.Roll = rollNumber;
            lootRollWon.RollType = rollType;
            FillPacket(lootRollWon.Item);
            lootRollWon.Item.UIType = LootSlotType.Locked;
            lootRollWon.MainSpec = true; // offspec rolls not implemented
            lootRollWon.Write();

            foreach (var (playerGuid, roll) in _rollVoteMap)
            {
                if (roll.Vote == RollVote.NotValid)
                    continue;

                if (playerGuid == targetGuid)
                    continue;

                Player player1 = Global.ObjAccessor.GetPlayer(_map, playerGuid);

                if (player1 == null)
                    continue;

                player1.SendPacket(lootRollWon);
            }

            Player player = Global.ObjAccessor.GetPlayer(_map, targetGuid);

            if (player != null)
            {
                lootRollWon.Item.UIType = LootSlotType.AllowLoot;
                lootRollWon.Clear();
                player.SendPacket(lootRollWon);
            }
        }

        private void FillPacket(LootItemData lootItem)
        {
            lootItem.Quantity = _lootItem.Count;
            lootItem.LootListID = (byte)_lootItem.LootListId;
            lootItem.CanTradeToTapList = _lootItem.AllowedGUIDs.Count > 1;
            lootItem.Loot = new ItemInstance(_lootItem);
        }

        /**
		 * \brief Check if all player have voted and return true in that case. Also return current winner.
		 * \param winnerItr > will be different than _rollCoteMap.end() if winner exist. (Someone voted greed or need)
		 * \returns true if all players voted
		 */
        private bool AllPlayerVoted(ref KeyValuePair<ObjectGuid, PlayerRollVote> winnerPair)
        {
            uint notVoted = 0;
            bool isSomeoneNeed = false;

            winnerPair = default;

            foreach (var pair in _rollVoteMap)
                switch (pair.Value.Vote)
                {
                    case RollVote.Need:
                        if (!isSomeoneNeed ||
                            winnerPair.Value == null ||
                            pair.Value.RollNumber > winnerPair.Value.RollNumber)
                        {
                            isSomeoneNeed = true; // first passage will Force to set winner because need is prioritized
                            winnerPair = pair;
                        }

                        break;
                    case RollVote.Greed:
                    case RollVote.Disenchant:
                        if (!isSomeoneNeed) // if at least one need is detected then winner can't be a greed
                            if (winnerPair.Value == null ||
                                pair.Value.RollNumber > winnerPair.Value.RollNumber)
                                winnerPair = pair;

                        break;
                    // Explicitly passing excludes a player from winning loot, so no Action required.
                    case RollVote.Pass:
                        break;
                    case RollVote.NotEmitedYet:
                        ++notVoted;

                        break;
                    default:
                        break;
                }

            return notVoted == 0;
        }

        private ItemDisenchantLootRecord GetItemDisenchantLoot()
        {
            ItemInstance itemInstance = new(_lootItem);

            BonusData bonusData = new(itemInstance);

            if (!bonusData.CanDisenchant)
                return null;

            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(_lootItem.Itemid);
            uint itemLevel = Item.GetItemLevel(itemTemplate, bonusData, 1, 0, 0, 0, 0, false, 0);

            return Item.GetDisenchantLoot(itemTemplate, (uint)bonusData.Quality, itemLevel);
        }

        // terminate the roll
        private void Finish(KeyValuePair<ObjectGuid, PlayerRollVote> winnerPair)
        {
            _lootItem.Is_blocked = false;

            if (winnerPair.Value == null)
            {
                SendAllPassed();
            }
            else
            {
                _lootItem.RollWinnerGUID = winnerPair.Key;

                SendLootRollWon(winnerPair.Key, winnerPair.Value.RollNumber, winnerPair.Value.Vote);

                Player player = Global.ObjAccessor.FindConnectedPlayer(winnerPair.Key);

                if (player != null)
                {
                    if (winnerPair.Value.Vote == RollVote.Need)
                        player.UpdateCriteria(CriteriaType.RollNeed, _lootItem.Itemid, winnerPair.Value.RollNumber);
                    else if (winnerPair.Value.Vote == RollVote.Disenchant)
                        player.UpdateCriteria(CriteriaType.CastSpell, 13262);
                    else
                        player.UpdateCriteria(CriteriaType.RollGreed, _lootItem.Itemid, winnerPair.Value.RollNumber);

                    if (winnerPair.Value.Vote == RollVote.Disenchant)
                    {
                        var disenchant = GetItemDisenchantLoot();
                        Loot loot = new(_map, _loot.GetOwnerGUID(), LootType.Disenchanting, null);
                        loot.FillLoot(disenchant.Id, LootStorage.Disenchant, player, true, false, LootModes.Default, ItemContext.None);

                        if (!loot.AutoStore(player, ItemConst.NullBag, ItemConst.NullSlot, true))
                            for (uint i = 0; i < loot.Items.Count; ++i)
                            {
                                LootItem disenchantLoot = loot.LootItemInSlot(i, player);

                                if (disenchantLoot != null)
                                    player.SendItemRetrievalMail(disenchantLoot.Itemid, disenchantLoot.Count, disenchantLoot.Context);
                            }
                        else
                            _loot.NotifyItemRemoved((byte)_lootItem.LootListId, _map);
                    }
                    else
                    {
                        player.StoreLootItem(_loot.GetOwnerGUID(), (byte)_lootItem.LootListId, _loot);
                    }
                }
            }

            _isStarted = false;
        }

        ~LootRoll()
        {
            if (_isStarted)
                SendAllPassed();

            foreach (var (playerGuid, roll) in _rollVoteMap)
            {
                if (roll.Vote != RollVote.NotEmitedYet)
                    continue;

                Player player = Global.ObjAccessor.GetPlayer(_map, playerGuid);

                if (!player)
                    continue;

                player.RemoveLootRoll(this);
            }
        }
    }
}