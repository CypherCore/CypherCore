// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.BattlePets;
using Game.DataStorage;
using Game.Entities;
using Game.Mails;
using Game.Networking.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public class AuctionManager : Singleton<AuctionManager>
    {
        const int MIN_AUCTION_TIME = 12 * Time.Hour;

        AuctionHouseObject mHordeAuctions;
        AuctionHouseObject mAllianceAuctions;
        AuctionHouseObject mNeutralAuctions;
        AuctionHouseObject mGoblinAuctions;

        Dictionary<ObjectGuid, PlayerPendingAuctions> _pendingAuctionsByPlayer = new();

        Dictionary<ObjectGuid, Item> _itemsByGuid = new();

        uint _replicateIdGenerator;

        Dictionary<ObjectGuid, PlayerThrottleObject> _playerThrottleObjects = new();
        DateTime _playerThrottleObjectsCleanupTime;

        AuctionManager()
        {
            mHordeAuctions = new AuctionHouseObject(6);
            mAllianceAuctions = new AuctionHouseObject(2);
            mNeutralAuctions = new AuctionHouseObject(1);
            mGoblinAuctions = new AuctionHouseObject(7);
            _replicateIdGenerator = 0;
            _playerThrottleObjectsCleanupTime = GameTime.Now() + TimeSpan.FromHours(1);
        }

        public AuctionHouseObject GetAuctionsMap(uint factionTemplateId)
        {
            if (WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionAuction))
                return mNeutralAuctions;

            // teams have linked auction houses
            FactionTemplateRecord uEntry = CliDB.FactionTemplateStorage.LookupByKey(factionTemplateId);
            if (uEntry == null)
                return mNeutralAuctions;
            else if (uEntry.FactionGroup.HasAnyFlag((byte)FactionMasks.Alliance))
                return mAllianceAuctions;
            else if (uEntry.FactionGroup.HasAnyFlag((byte)FactionMasks.Horde))
                return mHordeAuctions;
            else
                return mNeutralAuctions;
        }

        public AuctionHouseObject GetAuctionsById(uint auctionHouseId)
        {
            switch (auctionHouseId)
            {
                case 1:
                    return mNeutralAuctions;
                case 2:
                    return mAllianceAuctions;
                case 6:
                    return mHordeAuctions;
                case 7:
                    return mGoblinAuctions;
                default:
                    break;
            }
            return mNeutralAuctions;
        }

        public Item GetAItem(ObjectGuid itemGuid)
        {
            return _itemsByGuid.LookupByKey(itemGuid);
        }

        public ulong GetCommodityAuctionDeposit(ItemTemplate item, TimeSpan time, uint quantity)
        {
            uint sellPrice = item.GetSellPrice();
            return (ulong)((Math.Ceiling(Math.Floor(Math.Max(0.15 * quantity * sellPrice, 100.0)) / MoneyConstants.Silver) * MoneyConstants.Silver) * (time.Minutes / (MIN_AUCTION_TIME / Time.Minute)));
        }

        public ulong GetItemAuctionDeposit(Player player, Item item, TimeSpan time)
        {
            uint sellPrice = item.GetSellPrice(player);
            return (ulong)((Math.Ceiling(Math.Floor(Math.Max(sellPrice * 0.15, 100.0)) / MoneyConstants.Silver) * MoneyConstants.Silver) * (time.Minutes / (MIN_AUCTION_TIME / Time.Minute)));
        }

        public string BuildItemAuctionMailSubject(AuctionMailType type, AuctionPosting auction)
        {
            return BuildAuctionMailSubject(auction.Items[0].GetEntry(), type, auction.Id, auction.GetTotalItemCount(),
                auction.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId), auction.Items[0].GetContext(), auction.Items[0].GetBonusListIDs());
        }

        public string BuildCommodityAuctionMailSubject(AuctionMailType type, uint itemId, uint itemCount)
        {
            return BuildAuctionMailSubject(itemId, type, 0, itemCount, 0, ItemContext.None, null);
        }

        public string BuildAuctionMailSubject(uint itemId, AuctionMailType type, uint auctionId, uint itemCount, uint battlePetSpeciesId, ItemContext context, List<uint> bonusListIds)
        {
            string str = $"{itemId}:0:{(uint)type}:{auctionId}:{itemCount}:{battlePetSpeciesId}:0:0:0:0:{(uint)context}:{bonusListIds.Count}";

            foreach (var bonusListId in bonusListIds)
                str += ':' + bonusListId;

            return str;
        }

        public string BuildAuctionWonMailBody(ObjectGuid guid, ulong bid, ulong buyout)
        {
            return $"{guid}:{bid}:{buyout}:0";
        }

        public string BuildAuctionSoldMailBody(ObjectGuid guid, ulong bid, ulong buyout, uint deposit, ulong consignment)
        {
            return $"{guid}:{bid}:{buyout}:{deposit}:{consignment}:0";
        }

        public string BuildAuctionInvoiceMailBody(ObjectGuid guid, ulong bid, ulong buyout, uint deposit, ulong consignment, uint moneyDelay, uint eta)
        {
            return $"{guid}:{bid}:{buyout}:{deposit}:{consignment}:{moneyDelay}:{eta}:0";
        }

        public void LoadAuctions()
        {
            uint oldMSTime = Time.GetMSTime();

            // need to clear in case we are reloading
            _itemsByGuid.Clear();

            SQLResult result = DB.Characters.Query(CharacterDatabase.GetPreparedStatement(CharStatements.SEL_AUCTION_ITEMS));
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 auctions. DB table `auctionhouse` is empty.");
                return;
            }

            // data needs to be at first place for Item.LoadFromDB
            uint count = 0;
            MultiMap<uint, Item> itemsByAuction = new();
            MultiMap<uint, ObjectGuid> biddersByAuction = new();

            do
            {
                ulong itemGuid = result.Read<ulong>(0);
                uint itemEntry = result.Read<uint>(1);

                ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemEntry);
                if (proto == null)
                {
                    Log.outError(LogFilter.Misc, $"AuctionHouseMgr.LoadAuctionItems: Unknown item (GUID: {itemGuid} item entry: #{itemEntry}) in auction, skipped.");
                    continue;
                }

                Item item = Item.NewItemOrBag(proto);
                if (!item.LoadFromDB(itemGuid, ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(52)), result.GetFields(), itemEntry))
                {
                    item.Dispose();
                    continue;
                }

                uint auctionId = result.Read<uint>(53);
                itemsByAuction.Add(auctionId, item);

                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} auction items in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");

            oldMSTime = Time.GetMSTime();
            count = 0;

            result = DB.Characters.Query(CharacterDatabase.GetPreparedStatement(CharStatements.SEL_AUCTION_BIDDERS));
            if (!result.IsEmpty())
            {
                do
                {
                    biddersByAuction.Add(result.Read<uint>(0), ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1)));

                } while (result.NextRow());
            }

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} auction bidders in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");

            oldMSTime = Time.GetMSTime();
            count = 0;

            result = DB.Characters.Query(CharacterDatabase.GetPreparedStatement(CharStatements.SEL_AUCTIONS));
            if (!result.IsEmpty())
            {
                SQLTransaction trans = new();
                do
                {
                    AuctionPosting auction = new();
                    auction.Id = result.Read<uint>(0);
                    uint auctionHouseId = result.Read<uint>(1);

                    AuctionHouseObject auctionHouse = GetAuctionsById(auctionHouseId);
                    if (auctionHouse == null)
                    {
                        Log.outError(LogFilter.Misc, $"Auction {auction.Id} has wrong auctionHouseId {auctionHouseId}");
                        PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_AUCTION);
                        stmt.AddValue(0, auction.Id);
                        trans.Append(stmt);
                        continue;
                    }

                    if (!itemsByAuction.ContainsKey(auction.Id))
                    {
                        Log.outError(LogFilter.Misc, $"Auction {auction.Id} has no items");
                        PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_AUCTION);
                        stmt.AddValue(0, auction.Id);
                        trans.Append(stmt);
                        continue;
                    }

                    auction.Items = itemsByAuction[auction.Id];
                    auction.Owner = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(2));
                    auction.OwnerAccount = ObjectGuid.Create(HighGuid.WowAccount, Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(auction.Owner));
                    ulong bidder = result.Read<ulong>(3);
                    if (bidder != 0)
                        auction.Bidder = ObjectGuid.Create(HighGuid.Player, bidder);

                    auction.MinBid = result.Read<ulong>(4);
                    auction.BuyoutOrUnitPrice = result.Read<ulong>(5);
                    auction.Deposit = result.Read<ulong>(6);
                    auction.BidAmount = result.Read<ulong>(7);
                    auction.StartTime = Time.UnixTimeToDateTime(result.Read<long>(8));
                    auction.EndTime = Time.UnixTimeToDateTime(result.Read<long>(9));
                    auction.ServerFlags = (AuctionPostingServerFlag)result.Read<byte>(10);

                    if (biddersByAuction.ContainsKey(auction.Id))
                        auction.BidderHistory = biddersByAuction[auction.Id];

                    auctionHouse.AddAuction(null, auction);

                    ++count;
                } while (result.NextRow());

                DB.Characters.CommitTransaction(trans);
            }

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} auctions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public void AddAItem(Item item)
        {
            Cypher.Assert(item != null);
            Cypher.Assert(!_itemsByGuid.ContainsKey(item.GetGUID()));
            _itemsByGuid[item.GetGUID()] = item;
        }

        public bool RemoveAItem(ObjectGuid itemGuid, bool deleteItem = false, SQLTransaction trans = null)
        {
            var item = _itemsByGuid.LookupByKey(itemGuid);
            if (item == null)
                return false;

            if (deleteItem)
            {
                item.FSetState(ItemUpdateState.Removed);
                item.SaveToDB(trans);
            }

            _itemsByGuid.Remove(itemGuid);
            return true;
        }

        public bool PendingAuctionAdd(Player player, uint auctionHouseId, uint auctionId, ulong deposit)
        {
            if (!_pendingAuctionsByPlayer.ContainsKey(player.GetGUID()))
                _pendingAuctionsByPlayer[player.GetGUID()] = new PlayerPendingAuctions();


            var pendingAuction = _pendingAuctionsByPlayer[player.GetGUID()];
            // Get deposit so far
            ulong totalDeposit = 0;
            foreach (PendingAuctionInfo thisAuction in pendingAuction.Auctions)
                totalDeposit += thisAuction.Deposit;

            // Add this deposit
            totalDeposit += deposit;

            if (!player.HasEnoughMoney(totalDeposit))
                return false;

            pendingAuction.Auctions.Add(new PendingAuctionInfo(auctionId, auctionHouseId, deposit));
            return true;
        }

        public int PendingAuctionCount(Player player)
        {
            var itr = _pendingAuctionsByPlayer.LookupByKey(player.GetGUID());
            if (itr != null)
                return itr.Auctions.Count;

            return 0;
        }

        public void PendingAuctionProcess(Player player)
        {
            var playerPendingAuctions = _pendingAuctionsByPlayer.LookupByKey(player.GetGUID());
            if (playerPendingAuctions == null)
                return;

            ulong totaldeposit = 0;
            var auctionIndex = 0;
            for (; auctionIndex < playerPendingAuctions.Auctions.Count; ++auctionIndex)
            {
                var pendingAuction = playerPendingAuctions.Auctions[auctionIndex];
                if (!player.HasEnoughMoney(totaldeposit + pendingAuction.Deposit))
                    break;

                totaldeposit += pendingAuction.Deposit;
            }

            // expire auctions we cannot afford
            if (auctionIndex < playerPendingAuctions.Auctions.Count)
            {
                SQLTransaction trans = new();

                do
                {
                    PendingAuctionInfo pendingAuction = playerPendingAuctions.Auctions[auctionIndex];
                    AuctionPosting auction = GetAuctionsById(pendingAuction.AuctionHouseId).GetAuction(pendingAuction.AuctionId);
                    if (auction != null)
                        auction.EndTime = GameTime.GetSystemTime();

                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_AUCTION_EXPIRATION);
                    stmt.AddValue(0, (uint)GameTime.GetGameTime());
                    stmt.AddValue(1, pendingAuction.AuctionId);
                    trans.Append(stmt);
                    ++auctionIndex;
                } while (auctionIndex < playerPendingAuctions.Auctions.Count);

                DB.Characters.CommitTransaction(trans);
            }

            _pendingAuctionsByPlayer.Remove(player.GetGUID());
            player.ModifyMoney(-(long)totaldeposit);
        }

        public void UpdatePendingAuctions()
        {
            foreach (var pair in _pendingAuctionsByPlayer)
            {
                ObjectGuid playerGUID = pair.Key;
                Player player = Global.ObjAccessor.FindConnectedPlayer(playerGUID);
                if (player != null)
                {
                    // Check if there were auctions since last update process if not
                    if (PendingAuctionCount(player) == pair.Value.LastAuctionsSize)
                        PendingAuctionProcess(player);
                    else
                        _pendingAuctionsByPlayer[playerGUID].LastAuctionsSize = PendingAuctionCount(player);
                }
                else
                {
                    // Expire any auctions that we couldn't get a deposit for
                    Log.outWarn(LogFilter.Auctionhouse, $"Player {playerGUID} was offline, unable to retrieve deposit!");

                    SQLTransaction trans = new();
                    foreach (PendingAuctionInfo pendingAuction in pair.Value.Auctions)
                    {
                        AuctionPosting auction = GetAuctionsById(pendingAuction.AuctionHouseId).GetAuction(pendingAuction.AuctionId);
                        if (auction != null)
                            auction.EndTime = GameTime.GetSystemTime();

                        PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_AUCTION_EXPIRATION);
                        stmt.AddValue(0, (uint)GameTime.GetGameTime());
                        stmt.AddValue(1, pendingAuction.AuctionId);
                        trans.Append(stmt);
                    }
                    DB.Characters.CommitTransaction(trans);
                    _pendingAuctionsByPlayer.Remove(playerGUID);
                }
            }
        }

        public void Update()
        {
            mHordeAuctions.Update();
            mAllianceAuctions.Update();
            mNeutralAuctions.Update();
            mGoblinAuctions.Update();

            DateTime now = GameTime.Now();
            if (now >= _playerThrottleObjectsCleanupTime)
            {
                foreach (var pair in _playerThrottleObjects.ToList())
                {
                    if (pair.Value.PeriodEnd < now)
                        _playerThrottleObjects.Remove(pair.Key);
                }

                _playerThrottleObjectsCleanupTime = now + TimeSpan.FromHours(1);
            }
        }

        public uint GenerateReplicationId()
        {
            return ++_replicateIdGenerator;
        }

        public AuctionThrottleResult CheckThrottle(Player player, bool addonTainted, AuctionCommand command = AuctionCommand.SellItem)
        {
            DateTime now = GameTime.Now();

            if (!_playerThrottleObjects.ContainsKey(player.GetGUID()))
                _playerThrottleObjects[player.GetGUID()] = new PlayerThrottleObject();

            var throttleObject = _playerThrottleObjects[player.GetGUID()];
            if (now > throttleObject.PeriodEnd)
            {
                throttleObject.PeriodEnd = now + TimeSpan.FromMinutes(1);
                throttleObject.QueriesRemaining = 100;
            }

            if (throttleObject.QueriesRemaining == 0)
            {
                player.GetSession().SendAuctionCommandResult(0, command, AuctionResult.AuctionHouseBusy, throttleObject.PeriodEnd - now);
                return new AuctionThrottleResult(TimeSpan.Zero, true);
            }

            if ((--throttleObject.QueriesRemaining) == 0)
                return new AuctionThrottleResult(throttleObject.PeriodEnd - now, false);
            else
                return new AuctionThrottleResult(TimeSpan.FromMilliseconds(WorldConfig.GetIntValue(addonTainted ? WorldCfg.AuctionTaintedSearchDelay : WorldCfg.AuctionSearchDelay)), false);
        }

        public AuctionHouseRecord GetAuctionHouseEntry(uint factionTemplateId)
        {
            uint houseId = 0;
            return GetAuctionHouseEntry(factionTemplateId, ref houseId);
        }

        public AuctionHouseRecord GetAuctionHouseEntry(uint factionTemplateId, ref uint houseId)
        {
            uint houseid = 1; // Auction House

            if (!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionAuction))
            {
                // FIXME: found way for proper auctionhouse selection by another way
                // AuctionHouse.dbc have faction field with _player_ factions associated with auction house races.
                // but no easy way convert creature faction to player race faction for specific city
                switch (factionTemplateId)
                {
                    case 120:
                        houseid = 7;
                        break; // booty bay, Blackwater Auction House
                    case 474:
                        houseid = 7;
                        break; // gadgetzan, Blackwater Auction House
                    case 855:
                        houseid = 7;
                        break; // everlook, Blackwater Auction House
                    default:                       // default
                    {
                        FactionTemplateRecord u_entry = CliDB.FactionTemplateStorage.LookupByKey(factionTemplateId);
                        if (u_entry == null)
                            houseid = 1; // Auction House
                        else if ((u_entry.FactionGroup & (int)FactionMasks.Alliance) != 0)
                            houseid = 2; // Alliance Auction House
                        else if ((u_entry.FactionGroup & (int)FactionMasks.Horde) != 0)
                            houseid = 6; // Horde Auction House
                        else
                            houseid = 1; // Auction House
                        break;
                    }
                }
            }

            houseId = houseid;

            return CliDB.AuctionHouseStorage.LookupByKey(houseid);
        }

        class PendingAuctionInfo
        {
            public uint AuctionId;
            public uint AuctionHouseId;
            public ulong Deposit;

            public PendingAuctionInfo(uint auctionId, uint auctionHouseId, ulong deposit)
            {
                AuctionId = auctionId;
                AuctionHouseId = auctionHouseId;
                Deposit = deposit;
            }
        }

        class PlayerPendingAuctions
        {
            public List<PendingAuctionInfo> Auctions = new();
            public int LastAuctionsSize;
        }

        class PlayerThrottleObject
        {
            public DateTime PeriodEnd;
            public byte QueriesRemaining = 100;
        }
    }

    public class AuctionHouseObject
    {
        public AuctionHouseObject(uint auctionHouseId)
        {
            _auctionHouse = CliDB.AuctionHouseStorage.LookupByKey(auctionHouseId);
        }

        public uint GetAuctionHouseId()
        {
            return _auctionHouse.Id;
        }

        public AuctionPosting GetAuction(uint auctionId)
        {
            return _itemsByAuctionId.LookupByKey(auctionId);
        }

        public void AddAuction(SQLTransaction trans, AuctionPosting auction)
        {
            AuctionsBucketKey key = AuctionsBucketKey.ForItem(auction.Items[0]);

            AuctionsBucketData bucket = _buckets.LookupByKey(key);
            if (bucket == null)
            {
                // we don't have any item for this key yet, create new bucket
                bucket = new AuctionsBucketData();
                bucket.Key = key;

                ItemTemplate itemTemplate = auction.Items[0].GetTemplate();
                bucket.ItemClass = (byte)itemTemplate.GetClass();
                bucket.ItemSubClass = (byte)itemTemplate.GetSubClass();
                bucket.InventoryType = (byte)itemTemplate.GetInventoryType();
                bucket.RequiredLevel = (byte)auction.Items[0].GetRequiredLevel();
                switch (itemTemplate.GetClass())
                {
                    case ItemClass.Weapon:
                    case ItemClass.Armor:
                        bucket.SortLevel = (byte)key.ItemLevel;
                        break;
                    case ItemClass.Container:
                        bucket.SortLevel = (byte)itemTemplate.GetContainerSlots();
                        break;
                    case ItemClass.Gem:
                    case ItemClass.ItemEnhancement:
                        bucket.SortLevel = (byte)itemTemplate.GetBaseItemLevel();
                        break;
                    case ItemClass.Consumable:
                        bucket.SortLevel = Math.Max((byte)1, bucket.RequiredLevel);
                        break;
                    case ItemClass.Miscellaneous:
                    case ItemClass.BattlePets:
                        bucket.SortLevel = 1;
                        break;
                    case ItemClass.Recipe:
                        bucket.SortLevel = (byte)((ItemSubClassRecipe)itemTemplate.GetSubClass() != ItemSubClassRecipe.Book ? itemTemplate.GetRequiredSkillRank() : (uint)itemTemplate.GetBaseRequiredLevel());
                        break;
                    default:
                        break;
                }

                for (Locale locale = Locale.enUS; locale < Locale.Total; ++locale)
                {
                    if (locale == Locale.None)
                        continue;

                    bucket.FullName[(int)locale] = auction.Items[0].GetName(locale);
                }

                _buckets.Add(key, bucket);
            }

            // update cache fields
            ulong priceToDisplay = auction.BuyoutOrUnitPrice != 0 ? auction.BuyoutOrUnitPrice : auction.BidAmount;
            if (bucket.MinPrice == 0 || priceToDisplay < bucket.MinPrice)
                bucket.MinPrice = priceToDisplay;

            var itemModifiedAppearance = auction.Items[0].GetItemModifiedAppearance();
            if (itemModifiedAppearance != null)
            {
                int index = 0;
                for (var i = 0; i < bucket.ItemModifiedAppearanceId.Length; ++i)
                {
                    if (bucket.ItemModifiedAppearanceId[i].Id == itemModifiedAppearance.Id)
                    {
                        index = i;
                        break;
                    }
                }

                bucket.ItemModifiedAppearanceId[index] = (itemModifiedAppearance.Id, bucket.ItemModifiedAppearanceId[index].Item2 + 1);
            }

            uint quality;

            if (auction.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId) == 0)
                quality = (byte)auction.Items[0].GetQuality();
            else
            {
                quality = (auction.Items[0].GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF;
                foreach (Item item in auction.Items)
                {
                    byte battlePetLevel = (byte)item.GetModifier(ItemModifier.BattlePetLevel);
                    if (bucket.MinBattlePetLevel == 0)
                        bucket.MinBattlePetLevel = battlePetLevel;
                    else if (bucket.MinBattlePetLevel > battlePetLevel)
                        bucket.MinBattlePetLevel = battlePetLevel;

                    bucket.MaxBattlePetLevel = Math.Max(bucket.MaxBattlePetLevel, battlePetLevel);
                    bucket.SortLevel = bucket.MaxBattlePetLevel;
                }
            }

            bucket.QualityMask |= (AuctionHouseFilterMask)((uint)AuctionHouseFilterMask.PoorQuality << (int)quality);
            ++bucket.QualityCounts[quality];

            if (trans != null)
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_AUCTION);
                stmt.AddValue(0, auction.Id);
                stmt.AddValue(1, _auctionHouse.Id);
                stmt.AddValue(2, auction.Owner.GetCounter());
                stmt.AddValue(3, ObjectGuid.Empty.GetCounter());
                stmt.AddValue(4, auction.MinBid);
                stmt.AddValue(5, auction.BuyoutOrUnitPrice);
                stmt.AddValue(6, auction.Deposit);
                stmt.AddValue(7, auction.BidAmount);
                stmt.AddValue(8, Time.DateTimeToUnixTime(auction.StartTime));
                stmt.AddValue(9, Time.DateTimeToUnixTime(auction.EndTime));
                stmt.AddValue(10, (byte)auction.ServerFlags);
                trans.Append(stmt);

                foreach (Item item in auction.Items)
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_AUCTION_ITEMS);
                    stmt.AddValue(0, auction.Id);
                    stmt.AddValue(1, item.GetGUID().GetCounter());
                    trans.Append(stmt);
                }
            }

            foreach (Item item in auction.Items)
                Global.AuctionHouseMgr.AddAItem(item);

            auction.Bucket = bucket;
            _playerOwnedAuctions.Add(auction.Owner, auction.Id);
            foreach (ObjectGuid bidder in auction.BidderHistory)
                _playerBidderAuctions.Add(bidder, auction.Id);

            _itemsByAuctionId[auction.Id] = auction;

            AuctionPosting.Sorter insertSorter = new(Locale.enUS, [new AuctionSortDef(AuctionHouseSortOrder.Price, false)]);
            var auctionIndex = bucket.Auctions.BinarySearch(auction, insertSorter);
            if (auctionIndex < 0)
                auctionIndex = ~auctionIndex;
            bucket.Auctions.Insert(auctionIndex, auction);

            Global.ScriptMgr.OnAuctionAdd(this, auction);
        }

        public void RemoveAuction(SQLTransaction trans, AuctionPosting auction, AuctionPosting auctionPosting = null)
        {
            AuctionsBucketData bucket = auction.Bucket;

            bucket.Auctions.RemoveAll(auct => auct.Id == auction.Id);
            if (!bucket.Auctions.Empty())
            {
                // update cache fields
                ulong priceToDisplay = auction.BuyoutOrUnitPrice != 0 ? auction.BuyoutOrUnitPrice : auction.BidAmount;
                if (bucket.MinPrice == priceToDisplay)
                {
                    bucket.MinPrice = ulong.MaxValue;
                    foreach (AuctionPosting remainingAuction in bucket.Auctions)
                        bucket.MinPrice = Math.Min(bucket.MinPrice, remainingAuction.BuyoutOrUnitPrice != 0 ? remainingAuction.BuyoutOrUnitPrice : remainingAuction.BidAmount);
                }

                var itemModifiedAppearance = auction.Items[0].GetItemModifiedAppearance();
                if (itemModifiedAppearance != null)
                {
                    int index = -1;
                    for (var i = 0; i < bucket.ItemModifiedAppearanceId.Length; ++i)
                    {
                        if (bucket.ItemModifiedAppearanceId[i].Item1 == itemModifiedAppearance.Id)
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index != -1)
                        if (--bucket.ItemModifiedAppearanceId[index].Count == 0)
                            bucket.ItemModifiedAppearanceId[index].Id = 0;
                }

                uint quality;

                if (auction.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId) == 0)
                {
                    quality = (uint)auction.Items[0].GetQuality();
                }
                else
                {
                    quality = (auction.Items[0].GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF;
                    bucket.MinBattlePetLevel = 0;
                    bucket.MaxBattlePetLevel = 0;
                    foreach (AuctionPosting remainingAuction in bucket.Auctions)
                    {
                        foreach (Item item in remainingAuction.Items)
                        {
                            byte battlePetLevel = (byte)item.GetModifier(ItemModifier.BattlePetLevel);
                            if (bucket.MinBattlePetLevel == 0)
                                bucket.MinBattlePetLevel = battlePetLevel;
                            else if (bucket.MinBattlePetLevel > battlePetLevel)
                                bucket.MinBattlePetLevel = battlePetLevel;

                            bucket.MaxBattlePetLevel = Math.Max(bucket.MaxBattlePetLevel, battlePetLevel);
                        }
                    }
                }

                if (--bucket.QualityCounts[quality] == 0)
                    bucket.QualityMask &= (AuctionHouseFilterMask)((uint)AuctionHouseFilterMask.PoorQuality << (int)quality);
            }
            else
                _buckets.Remove(bucket.Key);

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_AUCTION);
            stmt.AddValue(0, auction.Id);
            trans.Append(stmt);

            foreach (Item item in auction.Items)
                Global.AuctionHouseMgr.RemoveAItem(item.GetGUID());

            Global.ScriptMgr.OnAuctionRemove(this, auction);

            _playerOwnedAuctions.Remove(auction.Owner, auction.Id);
            foreach (ObjectGuid bidder in auction.BidderHistory)
                _playerBidderAuctions.Remove(bidder, auction.Id);

            _itemsByAuctionId.Remove(auction.Id);
        }

        public void Update()
        {
            DateTime curTime = GameTime.GetSystemTime();
            DateTime curTimeSteady = GameTime.Now();
            ///- Handle expired auctions

            // Clear expired throttled players
            foreach (var key in _replicateThrottleMap.Keys.ToList())
                if (_replicateThrottleMap[key].NextAllowedReplication <= curTimeSteady)
                    _replicateThrottleMap.Remove(key);

            foreach (var key in _commodityQuotes.Keys.ToList())
                if (_commodityQuotes[key].ValidTo < curTimeSteady)
                    _commodityQuotes.Remove(key);

            if (_itemsByAuctionId.Empty())
                return;

            SQLTransaction trans = new();

            foreach (var auction in _itemsByAuctionId.Values.ToList())
            {
                ///- filter auctions expired on next update
                if (auction.EndTime > curTime.AddMinutes(1))
                    continue;

                ///- Either cancel the auction if there was no bidder
                if (auction.Bidder.IsEmpty())
                {
                    SendAuctionExpired(auction, trans);
                    Global.ScriptMgr.OnAuctionExpire(this, auction);

                    RemoveAuction(trans, auction);
                }
                ///- Or perform the transaction
                else
                {
                    // Copy data before freeing AuctionPosting in auctionHouse->RemoveAuction
                    // Because auctionHouse->SendAuctionWon can unload items if bidder is offline
                    // we need to RemoveAuction before sending mails
                    AuctionPosting copy = auction;
                    RemoveAuction(trans, auction);

                    //we should send an "item sold" message if the seller is online
                    //we send the item to the winner
                    //we send the money to the seller
                    SendAuctionSold(copy, null, trans);
                    SendAuctionWon(copy, null, trans);
                    Global.ScriptMgr.OnAuctionSuccessful(this, auction);
                }
            }

            // Run DB changes
            DB.Characters.CommitTransaction(trans);
        }

        public void BuildListBuckets(AuctionListBucketsResult listBucketsResult, Player player, string name, byte minLevel, byte maxLevel, AuctionHouseFilterMask filters, AuctionSearchClassFilters classFilters,
            byte[] knownPetBits, byte maxKnownPetLevel, uint offset, AuctionSortDef[] sorts)
        {
            List<uint> knownAppearanceIds = new();
            BitArray knownPetSpecies = new(knownPetBits);
            // prepare uncollected filter for more efficient searches
            if (filters.HasFlag(AuctionHouseFilterMask.UncollectedOnly))
            {
                knownAppearanceIds = player.GetSession().GetCollectionMgr().GetAppearanceIds();
                if (knownPetSpecies.Length < CliDB.BattlePetSpeciesStorage.GetNumRows())
                    knownPetSpecies.Length = (int)CliDB.BattlePetSpeciesStorage.GetNumRows();
            }

            var sorter = new AuctionsBucketData.Sorter(player.GetSession().GetSessionDbcLocale(), sorts);
            var builder = new AuctionsResultBuilder<AuctionsBucketData>(offset, sorter, AuctionHouseResultLimits.Browse);

            foreach (var bucket in _buckets)
            {
                AuctionsBucketData bucketData = bucket.Value;
                if (!name.IsEmpty())
                {
                    if (filters.HasFlag(AuctionHouseFilterMask.ExactMatch))
                    {
                        if (bucketData.FullName[(int)player.GetSession().GetSessionDbcLocale()] != name)
                            continue;
                    }
                    else
                    {
                        if (!bucketData.FullName[(int)player.GetSession().GetSessionDbcLocale()].Contains(name))
                            continue;
                    }
                }

                if (minLevel != 0 && bucketData.RequiredLevel < minLevel)
                    continue;

                if (maxLevel != 0 && bucketData.RequiredLevel > maxLevel)
                    continue;

                if (!filters.HasFlag(bucketData.QualityMask))
                    continue;

                if (classFilters != null)
                {
                    // if we dont want any class filters, Optional is not initialized
                    // if we dont want this class included, SubclassMask is set to FILTER_SKIP_CLASS
                    // if we want this class and did not specify and subclasses, its set to FILTER_SKIP_SUBCLASS
                    // otherwise full restrictions apply
                    if (classFilters.Classes[bucketData.ItemClass].SubclassMask == AuctionSearchClassFilters.FilterType.SkipClass)
                        continue;

                    if (classFilters.Classes[bucketData.ItemClass].SubclassMask != AuctionSearchClassFilters.FilterType.SkipSubclass)
                    {
                        if (!classFilters.Classes[bucketData.ItemClass].SubclassMask.HasAnyFlag((AuctionSearchClassFilters.FilterType)(1 << bucketData.ItemSubClass)))
                            continue;

                        if (!classFilters.Classes[bucketData.ItemClass].InvTypes[bucketData.ItemSubClass].HasAnyFlag(1u << bucketData.InventoryType))
                            continue;
                    }
                }

                if (filters.HasFlag(AuctionHouseFilterMask.UncollectedOnly))
                {
                    // appearances - by ItemAppearanceId, not ItemModifiedAppearanceId
                    if (bucketData.InventoryType != (byte)InventoryType.NonEquip && bucketData.InventoryType != (byte)InventoryType.Bag)
                    {
                        bool hasAll = true;
                        foreach (var bucketAppearance in bucketData.ItemModifiedAppearanceId)
                        {
                            var itemModifiedAppearance = CliDB.ItemModifiedAppearanceStorage.LookupByKey(bucketAppearance.Item1);
                            if (itemModifiedAppearance != null)
                            {
                                if (!knownAppearanceIds.Contains((uint)itemModifiedAppearance.ItemAppearanceID))
                                {
                                    hasAll = false;
                                    break;
                                }
                            }
                        }

                        if (hasAll)
                            continue;
                    }
                    // caged pets
                    else if (bucket.Key.BattlePetSpeciesId != 0)
                    {
                        if (knownPetSpecies.Get(bucket.Key.BattlePetSpeciesId))
                            continue;
                    }
                    // toys
                    else if (Global.DB2Mgr.IsToyItem(bucket.Key.ItemId))
                    {
                        if (player.GetSession().GetCollectionMgr().HasToy(bucket.Key.ItemId))
                            continue;
                    }
                    // mounts
                    // recipes
                    // pet items
                    else if (bucketData.ItemClass == (int)ItemClass.Consumable || bucketData.ItemClass == (int)ItemClass.Recipe || bucketData.ItemClass == (int)ItemClass.Miscellaneous)
                    {
                        ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(bucket.Key.ItemId);
                        if (itemTemplate.Effects.Count >= 2 && (itemTemplate.Effects[0].SpellID == 483 || itemTemplate.Effects[0].SpellID == 55884))
                        {
                            if (player.HasSpell((uint)itemTemplate.Effects[1].SpellID))
                                continue;

                            var battlePetSpecies = BattlePetMgr.GetBattlePetSpeciesBySpell((uint)itemTemplate.Effects[1].SpellID);
                            if (battlePetSpecies != null)
                                if (knownPetSpecies.Get((int)battlePetSpecies.Id))
                                    continue;
                        }
                    }
                }

                if (filters.HasFlag(AuctionHouseFilterMask.UsableOnly))
                {
                    if (bucketData.RequiredLevel != 0 && player.GetLevel() < bucketData.RequiredLevel)
                        continue;

                    if (player.CanUseItem(Global.ObjectMgr.GetItemTemplate(bucket.Key.ItemId), true) != InventoryResult.Ok)
                        continue;

                    // cannot learn caged pets whose level exceeds highest level of currently owned pet
                    if (bucketData.MinBattlePetLevel != 0 && bucketData.MinBattlePetLevel > maxKnownPetLevel)
                        continue;
                }

                if (filters.HasFlag(AuctionHouseFilterMask.CurrentExpansionOnly))
                {
                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(bucket.Key.ItemId);
                    if (itemTemplate.GetRequiredExpansion() != WorldConfig.GetIntValue(WorldCfg.Expansion))
                        continue;
                }

                // TODO: this one needs to access loot history to know highest item level for every inventory type
                //if (filters.HasFlag(AuctionHouseFilterMask.UpgradesOnly))
                //{
                //}

                builder.AddItem(bucketData);
            }

            foreach (AuctionsBucketData resultBucket in builder.GetResultRange())
            {
                BucketInfo bucketInfo = new();
                resultBucket.BuildBucketInfo(bucketInfo, player);
                listBucketsResult.Buckets.Add(bucketInfo);
            }

            listBucketsResult.HasMoreResults = builder.HasMoreResults();
        }

        public void BuildListBuckets(AuctionListBucketsResult listBucketsResult, Player player, AuctionBucketKey[] keys, AuctionSortDef[] sorts)
        {
            List<AuctionsBucketData> buckets = new();
            foreach (AuctionBucketKey key in keys)
            {
                var bucketData = _buckets.LookupByKey(new AuctionsBucketKey(key));
                if (bucketData != null)
                    buckets.Add(bucketData);
            }

            AuctionsBucketData.Sorter sorter = new(player.GetSession().GetSessionDbcLocale(), sorts);
            buckets.Sort(sorter);

            foreach (AuctionsBucketData resultBucket in buckets)
            {
                BucketInfo bucketInfo = new();
                resultBucket.BuildBucketInfo(bucketInfo, player);
                listBucketsResult.Buckets.Add(bucketInfo);
            }

            listBucketsResult.HasMoreResults = false;
        }

        public void BuildListBiddedItems(AuctionListBiddedItemsResult listBidderItemsResult, Player player, uint offset, AuctionSortDef[] sorts)
        {
            // always full list
            List<AuctionPosting> auctions = new();
            foreach (var auctionId in _playerBidderAuctions.LookupByKey(player.GetGUID()))
            {
                AuctionPosting auction = _itemsByAuctionId.LookupByKey(auctionId);
                if (auction != null)
                    auctions.Add(auction);
            }

            AuctionPosting.Sorter sorter = new(player.GetSession().GetSessionDbcLocale(), sorts);
            auctions.Sort(sorter);

            foreach (var resultAuction in auctions)
            {
                AuctionItem auctionItem = new();
                resultAuction.BuildAuctionItem(auctionItem, true, true, true, false);
                listBidderItemsResult.Items.Add(auctionItem);
            }

            listBidderItemsResult.HasMoreResults = false;
        }

        public void BuildListAuctionItems(AuctionListItemsResult listItemsResult, Player player, AuctionsBucketKey bucketKey, uint offset, AuctionSortDef[] sorts)
        {
            listItemsResult.TotalCount = 0;
            AuctionsBucketData bucket = _buckets.LookupByKey(bucketKey);
            if (bucket != null)
            {
                var sorter = new AuctionPosting.Sorter(player.GetSession().GetSessionDbcLocale(), sorts);
                var builder = new AuctionsResultBuilder<AuctionPosting>(offset, sorter, AuctionHouseResultLimits.Items);

                foreach (var auction in bucket.Auctions)
                {
                    builder.AddItem(auction);
                    foreach (Item item in auction.Items)
                        listItemsResult.TotalCount += item.GetCount();
                }

                foreach (AuctionPosting resultAuction in builder.GetResultRange())
                {
                    AuctionItem auctionItem = new();
                    resultAuction.BuildAuctionItem(auctionItem, false, false, resultAuction.OwnerAccount != player.GetSession().GetAccountGUID(), resultAuction.Bidder.IsEmpty());
                    listItemsResult.Items.Add(auctionItem);
                }

                listItemsResult.HasMoreResults = builder.HasMoreResults();
            }
        }

        public void BuildListAuctionItems(AuctionListItemsResult listItemsResult, Player player, uint itemId, uint offset, AuctionSortDef[] sorts)
        {
            var sorter = new AuctionPosting.Sorter(player.GetSession().GetSessionDbcLocale(), sorts);
            var builder = new AuctionsResultBuilder<AuctionPosting>(offset, sorter, AuctionHouseResultLimits.Items);

            listItemsResult.TotalCount = 0;
            var bucketData = _buckets.LookupByKey(new AuctionsBucketKey(itemId, 0, 0, 0));
            if (bucketData != null)
            {
                foreach (AuctionPosting auction in bucketData.Auctions)
                {
                    builder.AddItem(auction);
                    foreach (Item item in auction.Items)
                        listItemsResult.TotalCount += item.GetCount();
                }
            }

            foreach (AuctionPosting resultAuction in builder.GetResultRange())
            {
                AuctionItem auctionItem = new();
                resultAuction.BuildAuctionItem(auctionItem, false, true, resultAuction.OwnerAccount != player.GetSession().GetAccountGUID(),
                    resultAuction.Bidder.IsEmpty());

                listItemsResult.Items.Add(auctionItem);
            }

            listItemsResult.HasMoreResults = builder.HasMoreResults();
        }

        public void BuildListOwnedItems(AuctionListOwnedItemsResult listOwnerItemsResult, Player player, uint offset, AuctionSortDef[] sorts)
        {
            // always full list
            List<AuctionPosting> auctions = new();
            foreach (var auctionId in _playerOwnedAuctions.LookupByKey(player.GetGUID()))
            {
                AuctionPosting auction = _itemsByAuctionId.LookupByKey(auctionId);
                if (auction != null)
                    auctions.Add(auction);
            }

            AuctionPosting.Sorter sorter = new(player.GetSession().GetSessionDbcLocale(), sorts);
            auctions.Sort(sorter);

            foreach (var resultAuction in auctions)
            {
                AuctionItem auctionItem = new();
                resultAuction.BuildAuctionItem(auctionItem, true, true, false, false);
                listOwnerItemsResult.Items.Add(auctionItem);
            }

            listOwnerItemsResult.HasMoreResults = false;
        }

        public void BuildReplicate(AuctionReplicateResponse replicateResponse, Player player, uint global, uint cursor, uint tombstone, uint count)
        {
            DateTime curTime = GameTime.Now();

            var throttleData = _replicateThrottleMap.LookupByKey(player.GetGUID());
            if (throttleData == null)
            {
                throttleData = new PlayerReplicateThrottleData();
                throttleData.NextAllowedReplication = curTime + TimeSpan.FromSeconds(WorldConfig.GetIntValue(WorldCfg.AuctionReplicateDelay));
                throttleData.Global = Global.AuctionHouseMgr.GenerateReplicationId();
            }
            else
            {
                if (throttleData.Global != global || throttleData.Cursor != cursor || throttleData.Tombstone != tombstone)
                    return;

                if (!throttleData.IsReplicationInProgress() && throttleData.NextAllowedReplication > curTime)
                    return;
            }

            if (_itemsByAuctionId.Empty() || count == 0)
                return;

            var keyIndex = _itemsByAuctionId.IndexOfKey(cursor);
            foreach (var pair in _itemsByAuctionId.Skip(keyIndex))
            {
                AuctionItem auctionItem = new();
                pair.Value.BuildAuctionItem(auctionItem, false, true, true, pair.Value.Bidder.IsEmpty());
                replicateResponse.Items.Add(auctionItem);
                if (--count == 0)
                    break;
            }

            replicateResponse.ChangeNumberGlobal = throttleData.Global;
            replicateResponse.ChangeNumberCursor = throttleData.Cursor = !replicateResponse.Items.Empty() ? replicateResponse.Items.Last().AuctionID : 0;
            replicateResponse.ChangeNumberTombstone = throttleData.Tombstone = count == 0 ? _itemsByAuctionId.First().Key : 0;
            _replicateThrottleMap[player.GetGUID()] = throttleData;
        }

        public ulong CalculateAuctionHouseCut(ulong bidAmount)
        {
            return (ulong)Math.Max((long)(MathFunctions.CalculatePct(bidAmount, _auctionHouse.ConsignmentRate) * WorldConfig.GetFloatValue(WorldCfg.RateAuctionCut)), 0);
        }

        public CommodityQuote CreateCommodityQuote(Player player, uint itemId, uint quantity)
        {
            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);
            if (itemTemplate == null)
                return null;

            var bucketData = _buckets.LookupByKey(AuctionsBucketKey.ForCommodity(itemTemplate));
            if (bucketData == null)
                return null;

            ulong totalPrice = 0;
            uint remainingQuantity = quantity;
            foreach (AuctionPosting auction in bucketData.Auctions)
            {
                if (auction.Owner == player.GetGUID() || auction.OwnerAccount == player.GetSession().GetAccountGUID())
                    continue;

                foreach (Item auctionItem in auction.Items)
                {
                    if (auctionItem.GetCount() >= remainingQuantity)
                    {
                        totalPrice += auction.BuyoutOrUnitPrice * remainingQuantity;
                        remainingQuantity = 0;
                        break;
                    }

                    totalPrice += auction.BuyoutOrUnitPrice * auctionItem.GetCount();
                    remainingQuantity -= auctionItem.GetCount();
                }
            }

            // not enough items on auction house
            if (remainingQuantity != 0)
                return null;

            if (!player.HasEnoughMoney(totalPrice))
                return null;

            CommodityQuote quote = _commodityQuotes[player.GetGUID()];
            quote.TotalPrice = totalPrice;
            quote.Quantity = quantity;
            quote.ValidTo = GameTime.Now() + TimeSpan.FromSeconds(30);
            return quote;
        }

        public void CancelCommodityQuote(ObjectGuid guid)
        {
            _commodityQuotes.Remove(guid);
        }

        public bool BuyCommodity(SQLTransaction trans, Player player, uint itemId, uint quantity, TimeSpan delayForNextAction)
        {
            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);
            if (itemTemplate == null)
                return false;

            var bucketItr = _buckets.LookupByKey(AuctionsBucketKey.ForCommodity(itemTemplate));
            if (bucketItr == null)
            {
                player.GetSession().SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);
                return false;
            }

            var quote = _commodityQuotes.LookupByKey(player.GetGUID());
            if (quote == null)
            {
                player.GetSession().SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);
                return false;
            }

            ulong totalPrice = 0;
            uint remainingQuantity = quantity;
            List<AuctionPosting> auctions = new();
            for (var i = 0; i < bucketItr.Auctions.Count;)
            {
                AuctionPosting auction = bucketItr.Auctions[i++];
                if (auction.Owner == player.GetGUID() || auction.OwnerAccount == player.GetSession().GetAccountGUID())
                    continue;

                auctions.Add(auction);
                foreach (Item auctionItem in auction.Items)
                {
                    if (auctionItem.GetCount() >= remainingQuantity)
                    {
                        totalPrice += auction.BuyoutOrUnitPrice * remainingQuantity;
                        remainingQuantity = 0;
                        i = bucketItr.Auctions.Count;
                        break;
                    }

                    totalPrice += auction.BuyoutOrUnitPrice * auctionItem.GetCount();
                    remainingQuantity -= auctionItem.GetCount();
                }
            }

            // not enough items on auction house
            if (remainingQuantity != 0)
            {
                player.GetSession().SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);
                return false;
            }

            // something was bought between creating quote and finalizing transaction
            // but we allow lower price if new items were posted at lower price
            if (totalPrice > quote.TotalPrice)
            {
                player.GetSession().SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);
                return false;
            }

            if (!player.HasEnoughMoney(totalPrice))
            {
                player.GetSession().SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);
                return false;
            }

            ObjectGuid? uniqueSeller = new();

            // prepare items
            List<MailedItemsBatch> items = [new MailedItemsBatch()];

            remainingQuantity = quantity;
            List<int> removedItemsFromAuction = new();

            for (var i = 0; i < bucketItr.Auctions.Count;)
            {
                AuctionPosting auction = bucketItr.Auctions[i++];
                if (auction.Owner == player.GetGUID() || auction.OwnerAccount == player.GetSession().GetAccountGUID())
                    continue;

                if (!uniqueSeller.HasValue)
                    uniqueSeller = auction.Owner;
                else if (uniqueSeller != auction.Owner)
                    uniqueSeller = ObjectGuid.Empty;

                uint boughtFromAuction = 0;
                int removedItems = 0;
                foreach (Item auctionItem in auction.Items)
                {
                    MailedItemsBatch itemsBatch = items.Last();
                    if (itemsBatch.IsFull())
                    {
                        items.Add(new MailedItemsBatch());
                        itemsBatch = items.Last();
                    }

                    if (auctionItem.GetCount() >= remainingQuantity)
                    {
                        Item clonedItem = auctionItem.CloneItem(remainingQuantity, player);
                        if (clonedItem == null)
                        {
                            player.GetSession().SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);
                            return false;
                        }

                        auctionItem.SetCount(auctionItem.GetCount() - remainingQuantity);
                        auctionItem.FSetState(ItemUpdateState.Changed);
                        auctionItem.SaveToDB(trans);
                        itemsBatch.AddItem(clonedItem, auction.BuyoutOrUnitPrice);
                        boughtFromAuction += remainingQuantity;
                        remainingQuantity = 0;
                        i = bucketItr.Auctions.Count;
                        break;
                    }

                    itemsBatch.AddItem(auctionItem, auction.BuyoutOrUnitPrice);
                    boughtFromAuction += auctionItem.GetCount();
                    remainingQuantity -= auctionItem.GetCount();
                    ++removedItems;
                }

                removedItemsFromAuction.Add(removedItems);

                if (player.GetSession().HasPermission(RBACPermissions.LogGmTrade))
                {
                    uint bidderAccId = player.GetSession().GetAccountId();
                    if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(auction.Owner, out string ownerName))
                        ownerName = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);

                    Log.outCommand(bidderAccId, $"GM {player.GetName()} (Account: {bidderAccId}) bought commodity in auction: {items[0].Items[0].GetName(Global.WorldMgr.GetDefaultDbcLocale())} (Entry: {items[0].Items[0].GetEntry()} " +
                        $"Count: {boughtFromAuction}) and pay money: {auction.BuyoutOrUnitPrice * boughtFromAuction}. Original owner {ownerName} (Account: {Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(auction.Owner)})");
                }

                ulong auctionHouseCut = CalculateAuctionHouseCut(auction.BuyoutOrUnitPrice * boughtFromAuction);
                ulong depositPart = Global.AuctionHouseMgr.GetCommodityAuctionDeposit(items[0].Items[0].GetTemplate(), (auction.EndTime - auction.StartTime), boughtFromAuction);
                ulong profit = auction.BuyoutOrUnitPrice * boughtFromAuction + depositPart - auctionHouseCut;

                Player owner = Global.ObjAccessor.FindConnectedPlayer(auction.Owner);
                if (owner != null)
                {
                    owner.UpdateCriteria(CriteriaType.MoneyEarnedFromAuctions, profit);
                    owner.UpdateCriteria(CriteriaType.HighestAuctionSale, profit);
                    owner.GetSession().SendAuctionClosedNotification(auction, (float)WorldConfig.GetIntValue(WorldCfg.MailDeliveryDelay), true);
                }

                new MailDraft(Global.AuctionHouseMgr.BuildCommodityAuctionMailSubject(AuctionMailType.Sold, itemId, boughtFromAuction),
                    Global.AuctionHouseMgr.BuildAuctionSoldMailBody(player.GetGUID(), auction.BuyoutOrUnitPrice * boughtFromAuction, boughtFromAuction, (uint)depositPart, auctionHouseCut))
                    .AddMoney(profit)
                    .SendMailTo(trans, new MailReceiver(Global.ObjAccessor.FindConnectedPlayer(auction.Owner), auction.Owner), new MailSender(this), MailCheckMask.Copied, WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay));
            }

            player.ModifyMoney(-(long)totalPrice);
            player.SaveInventoryAndGoldToDB(trans);

            foreach (MailedItemsBatch batch in items)
            {
                MailDraft mail = new(Global.AuctionHouseMgr.BuildCommodityAuctionMailSubject(AuctionMailType.Won, itemId, batch.Quantity),
                    Global.AuctionHouseMgr.BuildAuctionWonMailBody(uniqueSeller.Value, batch.TotalPrice, batch.Quantity));

                for (int i = 0; i < batch.ItemsCount; ++i)
                {
                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_AUCTION_ITEMS_BY_ITEM);
                    stmt.AddValue(0, batch.Items[i].GetGUID().GetCounter());
                    trans.Append(stmt);

                    batch.Items[i].SetOwnerGUID(player.GetGUID());
                    batch.Items[i].SaveToDB(trans);
                    mail.AddItem(batch.Items[i]);
                }

                mail.SendMailTo(trans, player, new MailSender(this), MailCheckMask.Copied);
            }

            AuctionWonNotification packet = new();
            packet.Info.Initialize(_auctionHouse.Id, auctions[0], items[0].Items[0]);
            player.SendPacket(packet);

            for (int i = 0; i < auctions.Count; ++i)
            {
                if (removedItemsFromAuction[i] == auctions[i].Items.Count)
                    RemoveAuction(trans, auctions[i]); // bought all items
                else if (removedItemsFromAuction[i] != 0)
                {
                    var lastRemovedItemIndex = removedItemsFromAuction[i];
                    for (var c = 0; c != removedItemsFromAuction[i]; ++c)
                    {
                        Global.AuctionHouseMgr.RemoveAItem(auctions[i].Items[c].GetGUID());
                    }

                    auctions[i].Items.RemoveRange(0, lastRemovedItemIndex);
                }
            }

            return true;
        }

        // this function notified old bidder that his bid is no longer highest
        public void SendAuctionOutbid(AuctionPosting auction, ObjectGuid newBidder, ulong newBidAmount, SQLTransaction trans)
        {
            Player oldBidder = Global.ObjAccessor.FindConnectedPlayer(auction.Bidder);

            // old bidder exist
            if ((oldBidder != null || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Bidder)))// && !sAuctionBotConfig.IsBotChar(auction.Bidder))
            {
                if (oldBidder != null)
                {
                    AuctionOutbidNotification packet = new();
                    packet.BidAmount = newBidAmount;
                    packet.MinIncrement = AuctionPosting.CalculateMinIncrement(newBidAmount);
                    packet.Info.AuctionID = auction.Id;
                    packet.Info.Bidder = newBidder;
                    packet.Info.Item = new ItemInstance(auction.Items[0]);
                    oldBidder.SendPacket(packet);
                }

                new MailDraft(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Outbid, auction), "")
                    .AddMoney(auction.BidAmount)
                    .SendMailTo(trans, new MailReceiver(oldBidder, auction.Bidder), new MailSender(this), MailCheckMask.Copied);
            }
        }

        public void SendAuctionWon(AuctionPosting auction, Player bidder, SQLTransaction trans)
        {
            uint bidderAccId;
            if (bidder == null)
                bidder = Global.ObjAccessor.FindConnectedPlayer(auction.Bidder); // try lookup bidder when called from .Update

            // data for gm.log
            string bidderName = "";
            bool logGmTrade = auction.ServerFlags.HasFlag(AuctionPostingServerFlag.GmLogBuyer);

            if (bidder != null)
            {
                bidderAccId = bidder.GetSession().GetAccountId();
                bidderName = bidder.GetName();
            }
            else
            {
                bidderAccId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(auction.Bidder);

                if (logGmTrade && !Global.CharacterCacheStorage.GetCharacterNameByGuid(auction.Bidder, out bidderName))
                    bidderName = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);
            }

            if (logGmTrade)
            {
                if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(auction.Owner, out string ownerName))
                    ownerName = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);

                uint ownerAccId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(auction.Owner);

                Log.outCommand(bidderAccId, $"GM {bidderName} (Account: {bidderAccId}) won item in auction: {auction.Items[0].GetName(Global.WorldMgr.GetDefaultDbcLocale())} (Entry: {auction.Items[0].GetEntry()}" +
                    $" Count: {auction.GetTotalItemCount()}) and pay money: {auction.BidAmount}. Original owner {ownerName} (Account: {ownerAccId})");
            }

            // receiver exist
            if ((bidder != null || bidderAccId != 0))// && !sAuctionBotConfig.IsBotChar(auction.Bidder))
            {
                MailDraft mail = new(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Won, auction),
                    Global.AuctionHouseMgr.BuildAuctionWonMailBody(auction.Owner, auction.BidAmount, auction.BuyoutOrUnitPrice));

                // set owner to bidder (to prevent delete item with sender char deleting)
                // owner in `data` will set at mail receive and item extracting
                foreach (Item item in auction.Items)
                {
                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ITEM_OWNER);
                    stmt.AddValue(0, auction.Bidder.GetCounter());
                    stmt.AddValue(1, item.GetGUID().GetCounter());
                    trans.Append(stmt);

                    mail.AddItem(item);
                }

                if (bidder != null)
                {
                    AuctionWonNotification packet = new();
                    packet.Info.Initialize(_auctionHouse.Id, auction, auction.Items[0]);
                    bidder.SendPacket(packet);

                    // FIXME: for offline player need also
                    bidder.UpdateCriteria(CriteriaType.AuctionsWon, 1);
                }

                mail.SendMailTo(trans, new MailReceiver(bidder, auction.Bidder), new MailSender(this), MailCheckMask.Copied);
            }
            else
            {
                // bidder doesn't exist, delete the item
                foreach (Item item in auction.Items)
                    Global.AuctionHouseMgr.RemoveAItem(item.GetGUID(), true, trans);
            }
        }

        //call this method to send mail to auction owner, when auction is successful, it does not clear ram
        public void SendAuctionSold(AuctionPosting auction, Player owner, SQLTransaction trans)
        {
            if (owner == null)
                owner = Global.ObjAccessor.FindConnectedPlayer(auction.Owner);

            // owner exist
            if ((owner != null || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Owner)))// && !sAuctionBotConfig.IsBotChar(auction.Owner))
            {
                ulong auctionHouseCut = CalculateAuctionHouseCut(auction.BidAmount);
                ulong profit = auction.BidAmount + auction.Deposit - auctionHouseCut;

                //FIXME: what do if owner offline
                if (owner != null)
                {
                    owner.UpdateCriteria(CriteriaType.MoneyEarnedFromAuctions, profit);
                    owner.UpdateCriteria(CriteriaType.HighestAuctionSale, auction.BidAmount);
                    //send auction owner notification, bidder must be current!
                    owner.GetSession().SendAuctionClosedNotification(auction, (float)WorldConfig.GetIntValue(WorldCfg.MailDeliveryDelay), true);
                }

                new MailDraft(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Sold, auction),
                    Global.AuctionHouseMgr.BuildAuctionSoldMailBody(auction.Bidder, auction.BidAmount, auction.BuyoutOrUnitPrice, (uint)auction.Deposit, auctionHouseCut))
                    .AddMoney(profit)
                    .SendMailTo(trans, new MailReceiver(owner, auction.Owner), new MailSender(this), MailCheckMask.Copied, WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay));
            }
        }

        public void SendAuctionExpired(AuctionPosting auction, SQLTransaction trans)
        {
            Player owner = Global.ObjAccessor.FindConnectedPlayer(auction.Owner);
            // owner exist
            if ((owner != null || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Owner)))// && !sAuctionBotConfig.IsBotChar(auction.Owner))
            {
                if (owner != null)
                    owner.GetSession().SendAuctionClosedNotification(auction, 0.0f, false);

                int itemIndex = 0;
                while (itemIndex < auction.Items.Count)
                {
                    MailDraft mail = new(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Expired, auction), "");

                    for (int i = 0; i < SharedConst.MaxMailItems && itemIndex < auction.Items.Count; ++i, ++itemIndex)
                        mail.AddItem(auction.Items[itemIndex]);

                    mail.SendMailTo(trans, new MailReceiver(owner, auction.Owner), new MailSender(this), MailCheckMask.Copied, 0);
                }
            }
            else
            {
                // owner doesn't exist, delete the item
                foreach (Item item in auction.Items)
                    Global.AuctionHouseMgr.RemoveAItem(item.GetGUID(), true, trans);
            }
        }

        public void SendAuctionRemoved(AuctionPosting auction, Player owner, SQLTransaction trans)
        {
            int itemIndex = 0;
            while (itemIndex < auction.Items.Count)
            {
                MailDraft draft = new(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Cancelled, auction), "");

                for (int i = 0; i < SharedConst.MaxMailItems && itemIndex < auction.Items.Count; ++i, ++itemIndex)
                    draft.AddItem(auction.Items[itemIndex]);

                draft.SendMailTo(trans, owner, new MailSender(this), MailCheckMask.Copied);
            }
        }

        //this function sends mail, when auction is cancelled to old bidder
        public void SendAuctionCancelledToBidder(AuctionPosting auction, SQLTransaction trans)
        {
            Player bidder = Global.ObjAccessor.FindConnectedPlayer(auction.Bidder);

            // bidder exist
            if ((bidder != null || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Bidder)))// && !sAuctionBotConfig.IsBotChar(auction.Bidder))
                new MailDraft(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Removed, auction), "")
                .AddMoney(auction.BidAmount)
                .SendMailTo(trans, new MailReceiver(bidder, auction.Bidder), new MailSender(this), MailCheckMask.Copied);
        }

        public void SendAuctionInvoice(AuctionPosting auction, Player owner, SQLTransaction trans)
        {
            if (owner == null)
                owner = Global.ObjAccessor.FindConnectedPlayer(auction.Owner);

            // owner exist (online or offline)
            if ((owner != null || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Owner)))// && !sAuctionBotConfig.IsBotChar(auction.Owner))
            {
                WowTime eta = GameTime.GetUtcWowTime();
                eta += TimeSpan.FromSeconds(WorldConfig.GetIntValue(WorldCfg.MailDeliveryDelay));
                if (owner != null)
                    eta += owner.GetSession().GetTimezoneOffset();

                new MailDraft(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Invoice, auction),
                    Global.AuctionHouseMgr.BuildAuctionInvoiceMailBody(auction.Bidder, auction.BidAmount, auction.BuyoutOrUnitPrice, (uint)auction.Deposit,
                        CalculateAuctionHouseCut(auction.BidAmount), WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay), eta.GetPackedTime()))
                    .SendMailTo(trans, new MailReceiver(owner, auction.Owner), new MailSender(this), MailCheckMask.Copied);
            }
        }

        class PlayerReplicateThrottleData
        {
            public uint Global;
            public uint Cursor;
            public uint Tombstone;
            public DateTime NextAllowedReplication = DateTime.MinValue;

            public bool IsReplicationInProgress() { return Cursor != Tombstone && Global != 0; }
        }

        class MailedItemsBatch
        {
            public Item[] Items = new Item[SharedConst.MaxMailItems];
            public ulong TotalPrice;
            public uint Quantity;

            public int ItemsCount;

            public bool IsFull() { return ItemsCount >= Items.Length; }

            public void AddItem(Item item, ulong unitPrice)
            {
                Items[ItemsCount++] = item;
                Quantity += item.GetCount();
                TotalPrice += unitPrice * item.GetCount();
            }
        }

        AuctionHouseRecord _auctionHouse;

        SortedList<uint, AuctionPosting> _itemsByAuctionId = new(); // ordered for replicate
        SortedDictionary<AuctionsBucketKey, AuctionsBucketData> _buckets = new();// ordered for search by itemid only
        Dictionary<ObjectGuid, CommodityQuote> _commodityQuotes = new();

        MultiMap<ObjectGuid, uint> _playerOwnedAuctions = new();
        MultiMap<ObjectGuid, uint> _playerBidderAuctions = new();

        // Map of throttled players for GetAll, and throttle expiry time
        // Stored here, rather than player object to maintain persistence after logout
        Dictionary<ObjectGuid, PlayerReplicateThrottleData> _replicateThrottleMap = new();
    }

    public class AuctionPosting
    {
        public uint Id;
        public AuctionsBucketData Bucket;

        public List<Item> Items = new();
        public ObjectGuid Owner;
        public ObjectGuid OwnerAccount;
        public ObjectGuid Bidder;
        public ulong MinBid;
        public ulong BuyoutOrUnitPrice;
        public ulong Deposit;
        public ulong BidAmount;
        public DateTime StartTime = DateTime.MinValue;
        public DateTime EndTime = DateTime.MinValue;
        public AuctionPostingServerFlag ServerFlags;

        public List<ObjectGuid> BidderHistory = new();

        public bool IsCommodity()
        {
            return Items.Count > 1 || Items[0].GetTemplate().GetMaxStackSize() > 1;
        }

        public uint GetTotalItemCount()
        {
            return (uint)Items.Sum(item => { return item.GetCount(); });
        }

        public void BuildAuctionItem(AuctionItem auctionItem, bool alwaysSendItem, bool sendKey, bool censorServerInfo, bool censorBidInfo)
        {
            // SMSG_AUCTION_LIST_BIDDER_ITEMS_RESULT, SMSG_AUCTION_LIST_ITEMS_RESULT (if not commodity), SMSG_AUCTION_LIST_OWNER_ITEMS_RESULT, SMSG_AUCTION_REPLICATE_RESPONSE (if not commodity)
            //auctionItem.Item - here to unify comment

            // all (not optional<>)
            auctionItem.Count = (int)GetTotalItemCount();
            auctionItem.Flags = Items[0].m_itemData.DynamicFlags;
            auctionItem.AuctionID = Id;
            auctionItem.Owner = Owner;

            // prices set when filled
            if (IsCommodity())
            {
                if (alwaysSendItem)
                    auctionItem.Item = new ItemInstance(Items[0]);

                auctionItem.UnitPrice = BuyoutOrUnitPrice;
            }
            else
            {
                auctionItem.Item = new ItemInstance(Items[0]);
                auctionItem.Charges = Items[0].GetSpellCharges();
                for (EnchantmentSlot enchantmentSlot = 0; enchantmentSlot < EnchantmentSlot.MaxInspected; enchantmentSlot++)
                {
                    uint enchantId = Items[0].GetEnchantmentId(enchantmentSlot);
                    if (enchantId == 0)
                        continue;

                    auctionItem.Enchantments.Add(new ItemEnchantData(enchantId, Items[0].GetEnchantmentDuration(enchantmentSlot), Items[0].GetEnchantmentCharges(enchantmentSlot), (byte)enchantmentSlot));
                }

                for (byte i = 0; i < Items[0].m_itemData.Gems.Size(); ++i)
                {
                    SocketedGem gemData = Items[0].m_itemData.Gems[i];
                    if (gemData.ItemId != 0)
                    {
                        ItemGemData gem = new();
                        gem.Slot = i;
                        gem.Item = new ItemInstance(gemData);
                        auctionItem.Gems.Add(gem);
                    }
                }

                if (MinBid != 0)
                    auctionItem.MinBid = MinBid;

                ulong minIncrement = CalculateMinIncrement();
                if (minIncrement != 0)
                    auctionItem.MinIncrement = minIncrement;

                if (BuyoutOrUnitPrice != 0)
                    auctionItem.BuyoutPrice = BuyoutOrUnitPrice;
            }

            // all (not optional<>)
            auctionItem.DurationLeft = (int)Math.Max((EndTime - GameTime.GetSystemTime()).ToMilliseconds(), 0L);
            auctionItem.DeleteReason = 0;

            // SMSG_AUCTION_LIST_ITEMS_RESULT (only if owned)
            auctionItem.CensorServerSideInfo = censorServerInfo;
            auctionItem.ItemGuid = IsCommodity() ? ObjectGuid.Empty : Items[0].GetGUID();
            auctionItem.OwnerAccountID = OwnerAccount;
            auctionItem.EndTime = (uint)Time.DateTimeToUnixTime(EndTime);

            // SMSG_AUCTION_LIST_BIDDER_ITEMS_RESULT, SMSG_AUCTION_LIST_ITEMS_RESULT (if has bid), SMSG_AUCTION_LIST_OWNER_ITEMS_RESULT, SMSG_AUCTION_REPLICATE_RESPONSE (if has bid)
            auctionItem.CensorBidInfo = censorBidInfo;
            if (!Bidder.IsEmpty())
            {
                auctionItem.Bidder = Bidder;
                auctionItem.BidAmount = BidAmount;
            }

            // SMSG_AUCTION_LIST_BIDDER_ITEMS_RESULT, SMSG_AUCTION_LIST_OWNER_ITEMS_RESULT, SMSG_AUCTION_REPLICATE_RESPONSE (if commodity)
            if (sendKey)
                auctionItem.AuctionBucketKey = new(AuctionsBucketKey.ForItem(Items[0]));

            // all
            if (!Items[0].m_itemData.Creator._value.IsEmpty())
                auctionItem.Creator = Items[0].m_itemData.Creator;
        }

        public static ulong CalculateMinIncrement(ulong bidAmount)
        {
            return MathFunctions.CalculatePct(bidAmount / MoneyConstants.Silver, 5) * MoneyConstants.Silver;
        }

        public ulong CalculateMinIncrement() { return CalculateMinIncrement(BidAmount); }

        public class Sorter : IComparer<AuctionPosting>
        {
            public Sorter(Locale locale, AuctionSortDef[] sorts)
            {
                _locale = locale;
                _sorts = sorts;
            }

            public int Compare(AuctionPosting left, AuctionPosting right)
            {
                foreach (AuctionSortDef sort in _sorts)
                {
                    long ordering = CompareColumns(sort.SortOrder, left, right);
                    if (ordering != 0)
                        return (ordering < 0).CompareTo(!sort.ReverseSort);
                }

                // Auctions are processed in LIFO order
                if (left.StartTime != right.StartTime)
                    return left.StartTime.CompareTo(right.StartTime);

                return left.Id.CompareTo(right.Id);
            }

            long CompareColumns(AuctionHouseSortOrder column, AuctionPosting left, AuctionPosting right)
            {
                switch (column)
                {
                    case AuctionHouseSortOrder.Price:
                    {
                        ulong leftPrice = left.BuyoutOrUnitPrice != 0 ? left.BuyoutOrUnitPrice : (left.BidAmount != 0 ? left.BidAmount : left.MinBid);
                        ulong rightPrice = right.BuyoutOrUnitPrice != 0 ? right.BuyoutOrUnitPrice : (right.BidAmount != 0 ? right.BidAmount : right.MinBid);
                        return (long)(leftPrice - rightPrice);
                    }
                    case AuctionHouseSortOrder.Name:
                        return left.Bucket.FullName[(int)_locale].CompareTo(right.Bucket.FullName[(int)_locale]);
                    case AuctionHouseSortOrder.Level:
                    {
                        int leftLevel = left.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId) == 0 ? left.Bucket.SortLevel : (int)left.Items[0].GetModifier(ItemModifier.BattlePetLevel);
                        int rightLevel = right.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId) == 0 ? right.Bucket.SortLevel : (int)right.Items[0].GetModifier(ItemModifier.BattlePetLevel);
                        return leftLevel - rightLevel;
                    }
                    case AuctionHouseSortOrder.Bid:
                        return (long)(left.BidAmount - right.BidAmount);
                    case AuctionHouseSortOrder.Buyout:
                        return (long)(left.BuyoutOrUnitPrice - right.BuyoutOrUnitPrice);
                    case AuctionHouseSortOrder.TimeRemaining:
                        return (long)(left.EndTime - right.EndTime).TotalMilliseconds;
                    default:
                        break;
                }

                return 0;
            }

            Locale _locale;
            AuctionSortDef[] _sorts;
        }
    }



    public class AuctionsBucketData
    {
        public AuctionsBucketKey Key;

        // filter helpers
        public byte ItemClass;
        public byte ItemSubClass;
        public byte InventoryType;
        public AuctionHouseFilterMask QualityMask;
        public uint[] QualityCounts = new uint[(int)ItemQuality.Max];
        public ulong MinPrice; // for sort
        public (uint Id, uint Count)[] ItemModifiedAppearanceId = new (uint Id, uint Count)[4]; // for uncollected search
        public byte RequiredLevel; // for usable search
        public ushort SortLevel;
        public byte MinBattlePetLevel;
        public byte MaxBattlePetLevel;
        public string[] FullName = new string[(int)Locale.Total];

        public List<AuctionPosting> Auctions = new();

        public void BuildBucketInfo(BucketInfo bucketInfo, Player player)
        {
            bucketInfo.Key = new AuctionBucketKey(Key);
            bucketInfo.MinPrice = MinPrice;
            bucketInfo.RequiredLevel = RequiredLevel;
            bucketInfo.TotalQuantity = 0;

            foreach (AuctionPosting auction in Auctions)
            {
                foreach (Item item in auction.Items)
                {
                    bucketInfo.TotalQuantity += (int)item.GetCount();

                    if (Key.BattlePetSpeciesId != 0)
                    {
                        uint breedData = item.GetModifier(ItemModifier.BattlePetBreedData);
                        uint breedId = breedData & 0xFFFFFF;
                        byte quality = (byte)((breedData >> 24) & 0xFF);
                        byte level = (byte)(item.GetModifier(ItemModifier.BattlePetLevel));

                        bucketInfo.MaxBattlePetQuality = bucketInfo.MaxBattlePetQuality.HasValue ? Math.Max(bucketInfo.MaxBattlePetQuality.Value, quality) : quality;
                        bucketInfo.MaxBattlePetLevel = bucketInfo.MaxBattlePetLevel.HasValue ? Math.Max(bucketInfo.MaxBattlePetLevel.Value, level) : level;
                        bucketInfo.BattlePetBreedID = (byte)breedId;
                        if (bucketInfo.BattlePetLevelMask.HasValue)
                            bucketInfo.BattlePetLevelMask = 0;

                        bucketInfo.BattlePetLevelMask = bucketInfo.BattlePetLevelMask | 1u << (level - 1);
                    }
                }

                bucketInfo.ContainsOwnerItem = bucketInfo.ContainsOwnerItem || auction.Owner == player.GetGUID();
            }

            bucketInfo.ContainsOnlyCollectedAppearances = true;
            foreach (var appearance in ItemModifiedAppearanceId)
            {
                if (appearance.Item1 != 0)
                {
                    bucketInfo.ItemModifiedAppearanceIDs.Add(appearance.Item1);
                    if (!player.GetSession().GetCollectionMgr().HasItemAppearance(appearance.Item1).PermAppearance)
                        bucketInfo.ContainsOnlyCollectedAppearances = false;
                }
            }
        }

        public class Sorter : IComparer<AuctionsBucketData>
        {
            public Sorter(Locale locale, AuctionSortDef[] sorts)
            {
                _locale = locale;
                _sorts = sorts;
            }

            public int Compare(AuctionsBucketData left, AuctionsBucketData right)
            {
                foreach (AuctionSortDef sort in _sorts)
                {
                    long ordering = CompareColumns(sort.SortOrder, left, right);
                    if (ordering != 0)
                        return (ordering < 0).CompareTo(!sort.ReverseSort);
                }

                return left.Key != right.Key ? 1 : 0;
            }

            long CompareColumns(AuctionHouseSortOrder column, AuctionsBucketData left, AuctionsBucketData right)
            {
                switch (column)
                {
                    case AuctionHouseSortOrder.Price:
                    case AuctionHouseSortOrder.Bid:
                    case AuctionHouseSortOrder.Buyout:
                        return (long)(left.MinPrice - right.MinPrice);
                    case AuctionHouseSortOrder.Name:
                        return left.FullName[(int)_locale].CompareTo(right.FullName[(int)_locale]);
                    case AuctionHouseSortOrder.Level:
                        return left.SortLevel - right.SortLevel;
                    default:
                        break;
                }

                return 0;
            }

            Locale _locale;
            AuctionSortDef[] _sorts;
        }
    }

    public class CommodityQuote
    {
        public ulong TotalPrice;
        public uint Quantity;
        public DateTime ValidTo = DateTime.MinValue;
    }

    public class AuctionThrottleResult
    {
        public TimeSpan DelayUntilNext;
        public bool Throttled;

        public AuctionThrottleResult(TimeSpan delayUntilNext, bool throttled)
        {
            DelayUntilNext = delayUntilNext;
            Throttled = throttled;
        }
    }

    public class AuctionsBucketKey : IComparable<AuctionsBucketKey>
    {
        public uint ItemId { get; set; }
        public ushort ItemLevel { get; set; }
        public ushort BattlePetSpeciesId { get; set; }
        public ushort SuffixItemNameDescriptionId { get; set; }

        public AuctionsBucketKey(uint itemId, ushort itemLevel, ushort battlePetSpeciesId, ushort suffixItemNameDescriptionId)
        {
            ItemId = itemId;
            ItemLevel = itemLevel;
            BattlePetSpeciesId = battlePetSpeciesId;
            SuffixItemNameDescriptionId = suffixItemNameDescriptionId;
        }

        public AuctionsBucketKey(AuctionBucketKey key)
        {
            ItemId = key.ItemID;
            ItemLevel = key.ItemLevel;
            BattlePetSpeciesId = key.BattlePetSpeciesID.GetValueOrDefault(0);
            SuffixItemNameDescriptionId = key.ItemSuffix.GetValueOrDefault(0);
        }

        public int CompareTo(AuctionsBucketKey other)
        {
            return ItemId.CompareTo(other.ItemId);
        }

        public static bool operator ==(AuctionsBucketKey right, AuctionsBucketKey left)
        {
            return right.ItemId == left.ItemId
                && right.ItemLevel == left.ItemLevel
                && right.BattlePetSpeciesId == left.BattlePetSpeciesId
                && right.SuffixItemNameDescriptionId == left.SuffixItemNameDescriptionId;
        }
        public static bool operator !=(AuctionsBucketKey right, AuctionsBucketKey left) { return !(right == left); }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return ItemId.GetHashCode() ^ ItemLevel.GetHashCode() ^ BattlePetSpeciesId.GetHashCode() ^ SuffixItemNameDescriptionId.GetHashCode();
        }

        public static AuctionsBucketKey ForItem(Item item)
        {
            ItemTemplate itemTemplate = item.GetTemplate();
            if (itemTemplate.GetMaxStackSize() == 1)
            {
                return new AuctionsBucketKey(item.GetEntry(), (ushort)Item.GetItemLevel(itemTemplate, item.GetBonus(), 0, (uint)item.GetRequiredLevel(), 0, 0, 0, false, 0),
                    (ushort)item.GetModifier(ItemModifier.BattlePetSpeciesId), (ushort)item.GetBonus().Suffix);
            }
            else
                return ForCommodity(itemTemplate);
        }

        public static AuctionsBucketKey ForCommodity(ItemTemplate itemTemplate)
        {
            return new AuctionsBucketKey(itemTemplate.GetId(), (ushort)itemTemplate.GetBaseItemLevel(), 0, 0);
        }
    }

    public class AuctionSearchClassFilters
    {
        public SubclassFilter[] Classes = new SubclassFilter[(int)ItemClass.Max];

        public AuctionSearchClassFilters()
        {
            for (var i = 0; i < (int)ItemClass.Max; ++i)
                Classes[i] = new SubclassFilter();
        }

        public class SubclassFilter
        {
            public FilterType SubclassMask;
            public ulong[] InvTypes = new ulong[ItemConst.MaxItemSubclassTotal];
        }

        public enum FilterType : uint
        {
            SkipClass = 0,
            SkipSubclass = 0xFFFFFFFF,
            SkipInvtype = 0xFFFFFFFF
        }
    }

    class AuctionsResultBuilder<T>
    {
        uint _offset;
        IComparer<T> _sorter;
        AuctionHouseResultLimits _maxResults;
        List<T> _items = new();
        bool _hasMoreResults;

        public AuctionsResultBuilder(uint offset, IComparer<T> sorter, AuctionHouseResultLimits maxResults)
        {
            _offset = offset;
            _sorter = sorter;
            _maxResults = maxResults;
            _hasMoreResults = false;
        }

        public void AddItem(T item)
        {
            var index = _items.BinarySearch(item, _sorter);
            if (index < 0)
                index = ~index;
            _items.Insert(index, item);
            if (_items.Count > (int)_maxResults + _offset)
            {
                _items.RemoveAt(_items.Count - 1);
                _hasMoreResults = true;
            }
        }

        public Span<T> GetResultRange()
        {
            Span<T> h = _items.ToArray();
            return h[(int)_offset..];
        }

        public bool HasMoreResults()
        {
            return _hasMoreResults;
        }
    }
}
