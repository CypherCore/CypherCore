// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Loots
{
    public class Loot
    {
        public uint Gold { get; set; }

        public List<LootItem> Items = new();
        public LootType Loot_type { get; set; }          // required for Achievement system
        public ObjectGuid RoundRobinPlayer; // GUID of the player having the Round-Robin ownership for the loot. If 0, round robin owner has released.
        public byte UnlootedCount { get; set; }
        private readonly List<ObjectGuid> _allowedLooters = new();
        private readonly LootMethod _lootMethod;
        private readonly Dictionary<uint, LootRoll> _rolls = new(); // used if an Item is under rolling
        private readonly MultiMap<ObjectGuid, NotNormalLootItem> _playerFFAItems = new();

        private readonly List<ObjectGuid> _playersLooting = new();
        private uint _dungeonEncounterId;

        // Loot GUID
        private ObjectGuid _guid;
        private ItemContext _itemContext;
        private ObjectGuid _lootMaster;
        private ObjectGuid _owner; // The WorldObject that holds this loot
        private bool _wasOpened;   // true if at least one player received the loot content

        public Loot(Map map, ObjectGuid owner, LootType type, Group group)
        {
            Loot_type = type;
            _guid = map ? ObjectGuid.Create(HighGuid.LootObject, map.GetId(), 0, map.GenerateLowGuid(HighGuid.LootObject)) : ObjectGuid.Empty;
            _owner = owner;
            _itemContext = ItemContext.None;
            _lootMethod = group != null ? group.GetLootMethod() : LootMethod.FreeForAll;
            _lootMaster = group != null ? group.GetMasterLooterGuid() : ObjectGuid.Empty;
        }

        // Inserts the Item into the loot (called by LootTemplate processors)
        public void AddItem(LootStoreItem item)
        {
            ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(item.itemid);

            if (proto == null)
                return;

            uint count = RandomHelper.URand(item.mincount, item.maxcount);
            uint stacks = (uint)(count / proto.GetMaxStackSize() + (Convert.ToBoolean(count % proto.GetMaxStackSize()) ? 1 : 0));

            for (uint i = 0; i < stacks && Items.Count < SharedConst.MaxNRLootItems; ++i)
            {
                LootItem generatedLoot = new(item);
                generatedLoot.Context = _itemContext;
                generatedLoot.Count = (byte)Math.Min(count, proto.GetMaxStackSize());
                generatedLoot.LootListId = (uint)Items.Count;

                if (_itemContext != 0)
                {
                    List<uint> bonusListIDs = Global.DB2Mgr.GetDefaultItemBonusTree(generatedLoot.Itemid, _itemContext);
                    generatedLoot.BonusListIDs.AddRange(bonusListIDs);
                }

                Items.Add(generatedLoot);
                count -= proto.GetMaxStackSize();
            }
        }

        public bool AutoStore(Player player, byte bag, byte slot, bool broadcast = false, bool createdByPlayer = false)
        {
            bool allLooted = true;

            for (uint i = 0; i < Items.Count; ++i)
            {
                LootItem lootItem = LootItemInSlot(i, player, out NotNormalLootItem ffaitem);

                if (lootItem == null ||
                    lootItem.Is_looted)
                    continue;

                if (!lootItem.HasAllowedLooter(GetGUID()))
                    continue;

                if (lootItem.Is_blocked)
                    continue;

                // dont allow protected Item to be looted by someone else
                if (!lootItem.RollWinnerGUID.IsEmpty() &&
                    lootItem.RollWinnerGUID != GetGUID())
                    continue;

                List<ItemPosCount> dest = new();
                InventoryResult msg = player.CanStoreNewItem(bag, slot, dest, lootItem.Itemid, lootItem.Count);

                if (msg != InventoryResult.Ok &&
                    slot != ItemConst.NullSlot)
                    msg = player.CanStoreNewItem(bag, ItemConst.NullSlot, dest, lootItem.Itemid, lootItem.Count);

                if (msg != InventoryResult.Ok &&
                    bag != ItemConst.NullBag)
                    msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, lootItem.Itemid, lootItem.Count);

                if (msg != InventoryResult.Ok)
                {
                    player.SendEquipError(msg, null, null, lootItem.Itemid);
                    allLooted = false;

                    continue;
                }

                if (ffaitem != null)
                    ffaitem.Is_looted = true;

                if (!lootItem.Freeforall)
                    lootItem.Is_looted = true;

                --UnlootedCount;

                Item pItem = player.StoreNewItem(dest, lootItem.Itemid, true, lootItem.RandomBonusListId, null, lootItem.Context, lootItem.BonusListIDs);
                player.SendNewItem(pItem, lootItem.Count, false, createdByPlayer, broadcast);
                player.ApplyItemLootedSpell(pItem, true);
            }

            return allLooted;
        }

        public LootItem GetItemInSlot(uint lootListId)
        {
            if (lootListId < Items.Count)
                return Items[(int)lootListId];

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
                    Log.outError(LogFilter.Sql, "Table '{0}' loot Id #{1} used but it doesn't have records.", store.GetName(), lootId);

                return false;
            }

            _itemContext = context;

            tab.Process(this, store.IsRatesAllowed(), (byte)lootMode, 0); // Processing is done there, callback via Loot.AddItem()

            // Setting access rights for group loot case
            Group group = lootOwner.GetGroup();

            if (!personal &&
                group != null)
            {
                if (Loot_type == LootType.Corpse)
                    RoundRobinPlayer = lootOwner.GetGUID();

                for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                {
                    Player player = refe.GetSource();

                    if (player) // should actually be looted object instead of lootOwner but looter has to be really close so doesnt really matter
                        if (player.IsAtGroupRewardDistance(lootOwner))
                            FillNotNormalLootFor(player);
                }

                foreach (LootItem item in Items)
                {
                    if (!item.Follow_loot_rules ||
                        item.Freeforall)
                        continue;

                    ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(item.Itemid);

                    if (proto != null)
                    {
                        if (proto.GetQuality() < group.GetLootThreshold())
                            item.Is_underthreshold = true;
                        else
                            switch (_lootMethod)
                            {
                                case LootMethod.MasterLoot:
                                case LootMethod.GroupLoot:
                                case LootMethod.NeedBeforeGreed:
                                    {
                                        item.Is_blocked = true;

                                        break;
                                    }
                                default:
                                    break;
                            }
                    }
                }
            }
            // ... for personal loot
            else
            {
                FillNotNormalLootFor(lootOwner);
            }

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

            foreach (LootItem item in Items)
            {
                if (!item.AllowedForPlayer(player, this))
                    continue;

                item.AddAllowedLooter(player);

                if (item.Freeforall)
                {
                    ffaItems.Add(new NotNormalLootItem((byte)item.LootListId));
                    ++UnlootedCount;
                }

                else if (!item.Is_counted)
                {
                    item.Is_counted = true;
                    ++UnlootedCount;
                }
            }

            if (!ffaItems.Empty())
                _playerFFAItems[player.GetGUID()] = ffaItems;
        }

        public void NotifyItemRemoved(byte lootListId, Map map)
        {
            // notify all players that are looting this that the Item was removed
            // convert the index to the Slot the player sees
            for (int i = 0; i < _playersLooting.Count; ++i)
            {
                LootItem item = Items[lootListId];

                if (!item.GetAllowedLooters().Contains(_playersLooting[i]))
                    continue;

                Player player = Global.ObjAccessor.GetPlayer(map, _playersLooting[i]);

                if (player)
                    player.SendNotifyLootItemRemoved(GetGUID(), GetOwnerGUID(), lootListId);
                else
                    _playersLooting.RemoveAt(i);
            }
        }

        public void NotifyMoneyRemoved(Map map)
        {
            // notify all players that are looting this that the money was removed
            for (var i = 0; i < _playersLooting.Count; ++i)
            {
                Player player = Global.ObjAccessor.GetPlayer(map, _playersLooting[i]);

                if (player != null)
                    player.SendNotifyLootMoneyRemoved(GetGUID());
                else
                    _playersLooting.RemoveAt(i);
            }
        }

        public void OnLootOpened(Map map, ObjectGuid looter)
        {
            AddLooter(looter);

            if (!_wasOpened)
            {
                _wasOpened = true;

                if (_lootMethod == LootMethod.GroupLoot ||
                    _lootMethod == LootMethod.NeedBeforeGreed)
                {
                    ushort maxEnchantingSkill = 0;

                    foreach (ObjectGuid allowedLooterGuid in _allowedLooters)
                    {
                        Player allowedLooter = Global.ObjAccessor.GetPlayer(map, allowedLooterGuid);

                        if (allowedLooter != null)
                            maxEnchantingSkill = Math.Max(maxEnchantingSkill, allowedLooter.GetSkillValue(SkillType.Enchanting));
                    }

                    for (uint lootListId = 0; lootListId < Items.Count; ++lootListId)
                    {
                        LootItem item = Items[(int)lootListId];

                        if (!item.Is_blocked)
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

        public bool HasAllowedLooter(ObjectGuid looter)
        {
            return _allowedLooters.Contains(looter);
        }

        public void GenerateMoneyLoot(uint minAmount, uint maxAmount)
        {
            if (maxAmount > 0)
            {
                if (maxAmount <= minAmount)
                    Gold = (uint)(maxAmount * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));
                else if ((maxAmount - minAmount) < 32700)
                    Gold = (uint)(RandomHelper.URand(minAmount, maxAmount) * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));
                else
                    Gold = (uint)(RandomHelper.URand(minAmount >> 8, maxAmount >> 8) * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney)) << 8;
            }
        }

        public LootItem LootItemInSlot(uint lootSlot, Player player)
        {
            return LootItemInSlot(lootSlot, player, out _);
        }

        public LootItem LootItemInSlot(uint lootListId, Player player, out NotNormalLootItem ffaItem)
        {
            ffaItem = null;

            if (lootListId >= Items.Count)
                return null;

            LootItem item = Items[(int)lootListId];
            bool is_looted = item.Is_looted;

            if (item.Freeforall)
            {
                var itemList = _playerFFAItems.LookupByKey(player.GetGUID());

                if (itemList != null)
                    foreach (NotNormalLootItem notNormalLootItem in itemList)
                        if (notNormalLootItem.LootListId == lootListId)
                        {
                            is_looted = notNormalLootItem.Is_looted;
                            ffaItem = notNormalLootItem;

                            break;
                        }
            }

            if (is_looted)
                return null;

            return item;
        }

        // return true if there is any Item that is lootable for any player (not quest Item, FFA or conditional)
        public bool HasItemForAll()
        {
            // Gold is always lootable
            if (Gold != 0)
                return true;

            foreach (LootItem item in Items)
                if (!item.Is_looted &&
                    item.Follow_loot_rules &&
                    !item.Freeforall &&
                    item.Conditions.Empty())
                    return true;

            return false;
        }

        // return true if there is any FFA, quest or conditional Item for the player.
        public bool HasItemFor(Player player)
        {
            // quest items
            foreach (LootItem lootItem in Items)
                if (!lootItem.Is_looted &&
                    !lootItem.Follow_loot_rules &&
                    lootItem.GetAllowedLooters().Contains(player.GetGUID()))
                    return true;

            var ffaItems = GetPlayerFFAItems().LookupByKey(player.GetGUID());

            if (ffaItems != null)
            {
                bool hasFfaItem = ffaItems.Any(ffaItem => !ffaItem.Is_looted);

                if (hasFfaItem)
                    return true;
            }

            return false;
        }

        // return true if there is any Item over the group threshold (i.e. not underthreshold).
        public bool HasOverThresholdItem()
        {
            for (byte i = 0; i < Items.Count; ++i)
                if (!Items[i].Is_looted &&
                    !Items[i].Is_underthreshold &&
                    !Items[i].Freeforall)
                    return true;

            return false;
        }

        public void BuildLootResponse(LootResponse packet, Player viewer)
        {
            packet.Coins = Gold;

            foreach (LootItem item in Items)
            {
                var uiType = item.GetUiTypeForPlayer(viewer, this);

                if (!uiType.HasValue)
                    continue;

                LootItemData lootItem = new();
                lootItem.LootListID = (byte)item.LootListId;
                lootItem.UIType = uiType.Value;
                lootItem.Quantity = item.Count;
                lootItem.Loot = new ItemInstance(item);
                packet.Items.Add(lootItem);
            }
        }

        public void NotifyLootList(Map map)
        {
            LootList lootList = new();

            lootList.Owner = GetOwnerGUID();
            lootList.LootObj = GetGUID();

            if (GetLootMethod() == LootMethod.MasterLoot &&
                HasOverThresholdItem())
                lootList.Master = GetLootMasterGUID();

            if (!RoundRobinPlayer.IsEmpty())
                lootList.RoundRobinWinner = RoundRobinPlayer;

            lootList.Write();

            foreach (ObjectGuid allowedLooterGuid in _allowedLooters)
            {
                Player allowedLooter = Global.ObjAccessor.GetPlayer(map, allowedLooterGuid);

                allowedLooter?.SendPacket(lootList);
            }
        }

        public bool IsLooted()
        {
            return Gold == 0 && UnlootedCount == 0;
        }

        public void AddLooter(ObjectGuid guid)
        {
            _playersLooting.Add(guid);
        }

        public void RemoveLooter(ObjectGuid guid)
        {
            _playersLooting.Remove(guid);
        }

        public ObjectGuid GetGUID()
        {
            return _guid;
        }

        public ObjectGuid GetOwnerGUID()
        {
            return _owner;
        }

        public ItemContext GetItemContext()
        {
            return _itemContext;
        }

        public void SetItemContext(ItemContext context)
        {
            _itemContext = context;
        }

        public LootMethod GetLootMethod()
        {
            return _lootMethod;
        }

        public ObjectGuid GetLootMasterGUID()
        {
            return _lootMaster;
        }

        public uint GetDungeonEncounterId()
        {
            return _dungeonEncounterId;
        }

        public void SetDungeonEncounterId(uint dungeonEncounterId)
        {
            _dungeonEncounterId = dungeonEncounterId;
        }

        public MultiMap<ObjectGuid, NotNormalLootItem> GetPlayerFFAItems()
        {
            return _playerFFAItems;
        }
    }
}