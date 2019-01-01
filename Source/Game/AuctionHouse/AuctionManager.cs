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
using Framework.Dynamic;
using Game.DataStorage;
using Game.Entities;
using Game.Mails;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public class AuctionManager : Singleton<AuctionManager>
    {
        const int AH_MINIMUM_DEPOSIT = 100;

        AuctionManager() { }

        public AuctionHouseObject GetAuctionsMap(uint factionTemplateId)
        {
            if (WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionAuction))
                return mNeutralAuctions;

            // teams have linked auction houses
            FactionTemplateRecord uEntry = CliDB.FactionTemplateStorage.LookupByKey(factionTemplateId);
            if (uEntry == null)
                return mNeutralAuctions;
            else if ((uEntry.FactionGroup & (int)FactionMasks.Alliance) != 0)
                return mAllianceAuctions;
            else if ((uEntry.FactionGroup & (int)FactionMasks.Horde) != 0)
                return mHordeAuctions;
            else
                return mNeutralAuctions;
        }

        public ulong GetAuctionDeposit(AuctionHouseRecord entry, uint time, Item pItem, uint count)
        {
            uint MSV = pItem.GetTemplate().GetSellPrice();

            if (MSV <= 0)
                return AH_MINIMUM_DEPOSIT * (uint)WorldConfig.GetFloatValue(WorldCfg.RateAuctionDeposit);

            float multiplier = MathFunctions.CalculatePct((float)entry.DepositRate, 3);
            uint timeHr = (((time / 60) / 60) / 12);
            ulong deposit = (ulong)(((multiplier * MSV * count / 3) * timeHr * 3) * WorldConfig.GetFloatValue(WorldCfg.RateAuctionDeposit));

            Log.outDebug(LogFilter.Auctionhouse, $"MSV:        {MSV}");
            Log.outDebug(LogFilter.Auctionhouse, $"Items:      {count}");
            Log.outDebug(LogFilter.Auctionhouse, $"Multiplier: {multiplier}");
            Log.outDebug(LogFilter.Auctionhouse, $"Deposit:    {deposit}");

            if (deposit < AH_MINIMUM_DEPOSIT * WorldConfig.GetFloatValue(WorldCfg.RateAuctionDeposit))
                return AH_MINIMUM_DEPOSIT * (uint)WorldConfig.GetFloatValue(WorldCfg.RateAuctionDeposit);
            else
                return deposit;
        }

        public void SendAuctionWonMail(AuctionEntry auction, SQLTransaction trans)
        {
            Item item = GetAItem(auction.itemGUIDLow);
            if (!item)
                return;

            uint bidderAccId = 0;
            ObjectGuid bidderGuid = ObjectGuid.Create(HighGuid.Player, auction.bidder);
            Player bidder = Global.ObjAccessor.FindPlayer(bidderGuid);
            // data for gm.log
            string bidderName = "";
            bool logGmTrade = false;

            if (bidder)
            {
                bidderAccId = bidder.GetSession().GetAccountId();
                bidderName = bidder.GetName();
                logGmTrade = bidder.GetSession().HasPermission(RBACPermissions.LogGmTrade);
            }
            else
            {
                bidderAccId = ObjectManager.GetPlayerAccountIdByGUID(bidderGuid);
                logGmTrade = Global.AccountMgr.HasPermission(bidderAccId, RBACPermissions.LogGmTrade, Global.WorldMgr.GetRealm().Id.Realm);

                if (logGmTrade && !ObjectManager.GetPlayerNameByGUID(bidderGuid, out bidderName))
                    bidderName = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);
            }

            if (logGmTrade)
            {
                ObjectGuid ownerGuid = ObjectGuid.Create(HighGuid.Player, auction.owner);
                string ownerName;
                if (!ObjectManager.GetPlayerNameByGUID(ownerGuid, out ownerName))
                    ownerName = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);

                uint ownerAccId = ObjectManager.GetPlayerAccountIdByGUID(ownerGuid);

                Log.outCommand(bidderAccId, $"GM {bidderName} (Account: {bidderAccId}) won item in auction: {item.GetTemplate().GetName()} (Entry: {item.GetEntry()} Count: {item.GetCount()}) and pay money: {auction.bid}. Original owner {ownerName} (Account: {ownerAccId})");
            }

            // receiver exist
            if (bidder || bidderAccId != 0)
            {
                // set owner to bidder (to prevent delete item with sender char deleting)
                // owner in `data` will set at mail receive and item extracting
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ITEM_OWNER);
                stmt.AddValue(0, auction.bidder);
                stmt.AddValue(1, item.GetGUID().GetCounter());
                trans.Append(stmt);

                if (bidder)
                {
                    bidder.GetSession().SendAuctionWonNotification(auction, item);
                    // FIXME: for offline player need also
                    bidder.UpdateCriteria(CriteriaTypes.WonAuctions, 1);
                }

                new MailDraft(auction.BuildAuctionMailSubject(MailAuctionAnswers.Won), AuctionEntry.BuildAuctionMailBody(auction.owner, auction.bid, auction.buyout, 0, 0))
                    .AddItem(item)
                    .SendMailTo(trans, new MailReceiver(bidder, auction.bidder), new MailSender(auction), MailCheckMask.Copied);
            }
            else
            {
                // bidder doesn't exist, delete the item
                Global.AuctionMgr.RemoveAItem(auction.itemGUIDLow, true);
            }
        }

        public void SendAuctionSalePendingMail(AuctionEntry auction, SQLTransaction trans)
        {
            ObjectGuid owner_guid = ObjectGuid.Create(HighGuid.Player, auction.owner);
            Player owner = Global.ObjAccessor.FindPlayer(owner_guid);
            uint owner_accId = ObjectManager.GetPlayerAccountIdByGUID(owner_guid);
            // owner exist (online or offline)
            if (owner || owner_accId != 0)
                new MailDraft(auction.BuildAuctionMailSubject(MailAuctionAnswers.SalePending), AuctionEntry.BuildAuctionMailBody(auction.bidder, auction.bid, auction.buyout, auction.deposit, auction.GetAuctionCut()))
                    .SendMailTo(trans, new MailReceiver(owner, auction.owner), new MailSender(auction), MailCheckMask.Copied);
        }

        //call this method to send mail to auction owner, when auction is successful, it does not clear ram
        public void SendAuctionSuccessfulMail(AuctionEntry auction, SQLTransaction trans)
        {
            ObjectGuid owner_guid = ObjectGuid.Create(HighGuid.Player, auction.owner);
            Player owner = Global.ObjAccessor.FindPlayer(owner_guid);
            uint owner_accId = ObjectManager.GetPlayerAccountIdByGUID(owner_guid);
            Item item = GetAItem(auction.itemGUIDLow);

            // owner exist
            if (owner || owner_accId != 0)
            {
                ulong profit = auction.bid + auction.deposit - auction.GetAuctionCut();

                //FIXME: what do if owner offline
                if (owner && item)
                {
                    owner.UpdateCriteria(CriteriaTypes.GoldEarnedByAuctions, profit);
                    owner.UpdateCriteria(CriteriaTypes.HighestAuctionSold, auction.bid);
                    // send auction owner notification, bidder must be current!
                    owner.GetSession().SendAuctionClosedNotification(auction, WorldConfig.GetIntValue(WorldCfg.MailDeliveryDelay), true, item);
                }

                new MailDraft(auction.BuildAuctionMailSubject(MailAuctionAnswers.Successful), AuctionEntry.BuildAuctionMailBody(auction.bidder, auction.bid, auction.buyout, auction.deposit, auction.GetAuctionCut()))
                    .AddMoney(profit)
                    .SendMailTo(trans, new MailReceiver(owner, auction.owner), new MailSender(auction), MailCheckMask.Copied, WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay));
            }
        }

        //does not clear ram
        public void SendAuctionExpiredMail(AuctionEntry auction, SQLTransaction trans)
        {
            //return an item in auction to its owner by mail
            Item item = GetAItem(auction.itemGUIDLow);
            if (!item)
                return;

            ObjectGuid owner_guid = ObjectGuid.Create(HighGuid.Player, auction.owner);
            Player owner = Global.ObjAccessor.FindPlayer(owner_guid);
            uint owner_accId = ObjectManager.GetPlayerAccountIdByGUID(owner_guid);
            // owner exist
            if (owner || owner_accId != 0)
            {
                if (owner)
                    owner.GetSession().SendAuctionClosedNotification(auction, 0f, false, item);

                new MailDraft(auction.BuildAuctionMailSubject(MailAuctionAnswers.Expired), AuctionEntry.BuildAuctionMailBody(0, 0, auction.buyout, auction.deposit, 0))
                    .AddItem(item)
                    .SendMailTo(trans, new MailReceiver(owner, auction.owner), new MailSender(auction), MailCheckMask.Copied, 0);
            }
            else
            {
                // owner doesn't exist, delete the item
                Global.AuctionMgr.RemoveAItem(auction.itemGUIDLow, true);
            }
        }

        //this function sends mail to old bidder
        public void SendAuctionOutbiddedMail(AuctionEntry auction, ulong newPrice, Player newBidder, SQLTransaction trans)
        {
            ObjectGuid oldBidder_guid = ObjectGuid.Create(HighGuid.Player, auction.bidder);
            Player oldBidder = Global.ObjAccessor.FindPlayer(oldBidder_guid);

            uint oldBidder_accId = 0;
            if (oldBidder == null)
                oldBidder_accId = ObjectManager.GetPlayerAccountIdByGUID(oldBidder_guid);

            Item item = GetAItem(auction.itemGUIDLow);

            // old bidder exist
            if (oldBidder || oldBidder_accId != 0)
            {
                if (oldBidder && item)
                    oldBidder.GetSession().SendAuctionOutBidNotification(auction, item);

                new MailDraft(auction.BuildAuctionMailSubject(MailAuctionAnswers.Outbidded), AuctionEntry.BuildAuctionMailBody(auction.owner, auction.bid, auction.buyout, auction.deposit, auction.GetAuctionCut()))
                    .AddMoney(auction.bid)
                    .SendMailTo(trans, new MailReceiver(oldBidder, auction.bidder), new MailSender(auction), MailCheckMask.Copied);
            }
        }

        //this function sends mail, when auction is cancelled to old bidder
        public void SendAuctionCancelledToBidderMail(AuctionEntry auction, SQLTransaction trans)
        {
            ObjectGuid bidder_guid = ObjectGuid.Create(HighGuid.Player, auction.bidder);
            Player bidder = Global.ObjAccessor.FindPlayer(bidder_guid);

            uint bidder_accId = 0;
            if (!bidder)
                bidder_accId = ObjectManager.GetPlayerAccountIdByGUID(bidder_guid);

            // bidder exist
            if (bidder || bidder_accId != 0)
                new MailDraft(auction.BuildAuctionMailSubject(MailAuctionAnswers.CancelledToBidder), AuctionEntry.BuildAuctionMailBody(auction.owner, auction.bid, auction.buyout, auction.deposit, 0))
                    .AddMoney(auction.bid)
                    .SendMailTo(trans, new MailReceiver(bidder, auction.bidder), new MailSender(auction), MailCheckMask.Copied);
        }

        public void LoadAuctionItems()
        {
            uint oldMSTime = Time.GetMSTime();

            // need to clear in case we are reloading
            mAitems.Clear();

            // data needs to be at first place for Item.LoadFromDB
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_AUCTION_ITEMS);
            SQLResult result = DB.Characters.Query(stmt);

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 auction items. DB table `auctionhouse` or `item_instance` is empty!");
                return;
            }

            uint count = 0;
            do
            {
                ulong itemGuid = result.Read<ulong>(0);
                uint itemEntry = result.Read<uint>(1);

                ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemEntry);
                if (proto == null)
                {
                    Log.outError(LogFilter.Server, "AuctionHouseMgr:LoadAuctionItems: Unknown item (GUID: {0} item entry: {1}) in auction, skipped.", itemGuid, itemEntry);
                    continue;
                }

                Item item = Bag.NewItemOrBag(proto);
                if (!item.LoadFromDB(itemGuid, ObjectGuid.Empty, result.GetFields(), itemEntry))
                    continue;

                AddAItem(item);
                ++count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} auction items in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadAuctions()
        {
            uint oldMSTime = Time.GetMSTime();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_AUCTIONS);
            SQLResult result = DB.Characters.Query(stmt);

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 auctions. DB table `auctionhouse` is empty.");
                return;
            }

            uint count = 0;
            SQLTransaction trans = new SQLTransaction();
            do
            {
                AuctionEntry aItem = new AuctionEntry();
                if (!aItem.LoadFromDB(result.GetFields()))
                {
                    aItem.DeleteFromDB(trans);
                    continue;
                }

                GetAuctionsMap(aItem.factionTemplateId).AddAuction(aItem);
                ++count;
            } while (result.NextRow());

            DB.Characters.CommitTransaction(trans);

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} auctions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));

        }

        public void AddAItem(Item it)
        {
            Cypher.Assert(it);
            Cypher.Assert(!mAitems.ContainsKey(it.GetGUID().GetCounter()));
            mAitems[it.GetGUID().GetCounter()] = it;
        }

        public bool RemoveAItem(ulong id, bool deleteItem = false)
        {
            var item = mAitems.LookupByKey(id);
            if (item == null)
                return false;

            if (deleteItem)
            {
                item.FSetState(ItemUpdateState.Removed);
                item.SaveToDB(null);
            }

            mAitems.Remove(id);
            return true;
        }

        public void Update()
        {
            mHordeAuctions.Update();
            mAllianceAuctions.Update();
            mNeutralAuctions.Update();
        }

        public AuctionHouseRecord GetAuctionHouseEntry(uint factionTemplateId)
        {
            uint houseId = 0;
            return GetAuctionHouseEntry(factionTemplateId, ref houseId);
        }

        public AuctionHouseRecord GetAuctionHouseEntry(uint factionTemplateId, ref uint houseId)
        {
            uint houseid = 7; // goblin auction house

            if (!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionAuction))
            {
                // FIXME: found way for proper auctionhouse selection by another way
                // AuctionHouse.dbc have faction field with _player_ factions associated with auction house races.
                // but no easy way convert creature faction to player race faction for specific city
                switch (factionTemplateId)
                {
                    case 12: houseid = 1; break; // human
                    case 29: houseid = 6; break; // orc, and generic for horde
                    case 55: houseid = 2; break; // dwarf, and generic for alliance
                    case 68: houseid = 4; break; // undead
                    case 80: houseid = 3; break; // n-elf
                    case 104: houseid = 5; break; // trolls
                    case 120: houseid = 7; break; // booty bay, neutral
                    case 474: houseid = 7; break; // gadgetzan, neutral
                    case 855: houseid = 7; break; // everlook, neutral
                    case 1604: houseid = 6; break; // b-elfs,
                    default:                       // for unknown case
                        {
                            FactionTemplateRecord u_entry = CliDB.FactionTemplateStorage.LookupByKey(factionTemplateId);
                            if (u_entry == null)
                                houseid = 7; // goblin auction house
                            else if ((u_entry.FactionGroup & (int)FactionMasks.Alliance) != 0)
                                houseid = 1; // human auction house
                            else if ((u_entry.FactionGroup & (int)FactionMasks.Horde) != 0)
                                houseid = 6; // orc auction house
                            else
                                houseid = 7; // goblin auction house
                            break;
                        }
                }
            }

            houseId = houseid;

            return CliDB.AuctionHouseStorage.LookupByKey(houseid);
        }

        public Item GetAItem(ulong id)
        {
            return mAitems.LookupByKey(id);
        }

        AuctionHouseObject mHordeAuctions = new AuctionHouseObject();
        AuctionHouseObject mAllianceAuctions = new AuctionHouseObject();
        AuctionHouseObject mNeutralAuctions = new AuctionHouseObject();

        Dictionary<ulong, Item> mAitems = new Dictionary<ulong, Item>();
    }

    public class AuctionHouseObject
    {
        public void AddAuction(AuctionEntry auction)
        {
            Cypher.Assert(auction != null);

            AuctionsMap[auction.Id] = auction;
            Global.ScriptMgr.OnAuctionAdd(this, auction);
        }

        public bool RemoveAuction(AuctionEntry auction)
        {
            bool wasInMap = AuctionsMap.Remove(auction.Id) ? true : false;

            Global.ScriptMgr.OnAuctionRemove(this, auction);

            // we need to delete the entry, it is not referenced any more
            auction = null;

            return wasInMap;
        }

        public void Update()
        {
            long curTime = Global.WorldMgr.GetGameTime();
            // Handle expired auctions

            // If storage is empty, no need to update. next == NULL in this case.
            if (AuctionsMap.Empty())
                return;

            SQLTransaction trans = new SQLTransaction();

            foreach (var auction in AuctionsMap.Values)
            {
                // filter auctions expired on next update
                if (auction.expire_time > curTime + 60)
                    continue;

                // Either cancel the auction if there was no bidder
                if (auction.bidder == 0 && auction.bid == 0)
                {
                    Global.AuctionMgr.SendAuctionExpiredMail(auction, trans);
                    Global.ScriptMgr.OnAuctionExpire(this, auction);
                }
                // Or perform the transaction
                else
                {
                    //we should send an "item sold" message if the seller is online
                    //we send the item to the winner
                    //we send the money to the seller
                    Global.AuctionMgr.SendAuctionSuccessfulMail(auction, trans);
                    Global.AuctionMgr.SendAuctionWonMail(auction, trans);
                    Global.ScriptMgr.OnAuctionSuccessful(this, auction);
                }

                // In any case clear the auction
                auction.DeleteFromDB(trans);

                Global.AuctionMgr.RemoveAItem(auction.itemGUIDLow);
                RemoveAuction(auction);
            }

            // Run DB changes
            DB.Characters.CommitTransaction(trans);
        }

        public void BuildListBidderItems(AuctionListBidderItemsResult packet, Player player, ref uint totalcount)
        {
            foreach (var Aentry in AuctionsMap.Values)
            {
                if (Aentry != null && Aentry.bidder == player.GetGUID().GetCounter())
                {
                    Aentry.BuildAuctionInfo(packet.Items, false);
                    ++totalcount;
                }
            }
        }

        public void BuildListOwnerItems(AuctionListOwnerItemsResult packet, Player player, ref uint totalcount)
        {
            foreach (var Aentry in AuctionsMap.Values)
            {
                if (Aentry != null && Aentry.owner == player.GetGUID().GetCounter())
                {
                    Aentry.BuildAuctionInfo(packet.Items, false);
                    ++totalcount;
                }
            }
        }

        public void BuildListAuctionItems(AuctionListItemsResult packet, Player player, string searchedname, uint listfrom, byte levelmin, byte levelmax, bool usable, Optional<AuctionSearchFilters> filters, uint quality)
        {
            long curTime = Global.WorldMgr.GetGameTime();

            foreach (var Aentry in AuctionsMap.Values)
            {
                // Skip expired auctions
                if (Aentry.expire_time < curTime)
                    continue;

                Item item = Global.AuctionMgr.GetAItem(Aentry.itemGUIDLow);
                if (!item)
                    continue;

                ItemTemplate proto = item.GetTemplate();
                if (filters.HasValue)
                {
                    // if we dont want any class filters, Optional is not initialized
                    // if we dont want this class included, SubclassMask is set to FILTER_SKIP_CLASS
                    // if we want this class and did not specify and subclasses, its set to FILTER_SKIP_SUBCLASS
                    // otherwise full restrictions apply
                    if (filters.Value.Classes[(int)proto.GetClass()].SubclassMask == AuctionSearchFilters.FilterType.SkipClass)
                        continue;

                    if (filters.Value.Classes[(int)proto.GetClass()].SubclassMask != AuctionSearchFilters.FilterType.SkipSubclass)
                    {
                        if (!Convert.ToBoolean((int)filters.Value.Classes[(int)proto.GetClass()].SubclassMask & (1u << (int)proto.GetSubClass())))
                            continue;

                        if (!Convert.ToBoolean(filters.Value.Classes[(int)proto.GetClass()].InvTypes[(int)proto.GetSubClass()] & (1u << (int)proto.GetInventoryType())))
                            continue;
                    }
                }

                if (quality != 0xffffffff && (uint)proto.GetQuality() != quality)
                    continue;

                if (levelmin != 0 && (item.GetRequiredLevel() < levelmin || (levelmax != 0 && item.GetRequiredLevel() > levelmax)))
                    continue;

                if (usable && player.CanUseItem(item) != InventoryResult.Ok)
                    continue;

                // Allow search by suffix (ie: of the Monkey) or partial name (ie: Monkey)
                // No need to do any of this if no search term was entered
                if (!string.IsNullOrEmpty(searchedname))
                {
                    string name = proto.GetName(player.GetSession().GetSessionDbcLocale());
                    if (string.IsNullOrEmpty(name))
                        continue;

                    // DO NOT use GetItemEnchantMod(proto.RandomProperty) as it may return a result
                    //  that matches the search but it may not equal item.GetItemRandomPropertyId()
                    //  used in BuildAuctionInfo() which then causes wrong items to be listed
                    int propRefID = item.GetItemRandomPropertyId();
                    if (propRefID != 0)
                    {
                        string suffix = null;
                        // Append the suffix to the name (ie: of the Monkey) if one exists
                        // These are found in ItemRandomProperties.dbc, not ItemRandomSuffix.dbc
                        //  even though the DBC names seem misleading
                        if (propRefID < 0)
                        {
                            ItemRandomSuffixRecord itemRandSuffix = CliDB.ItemRandomSuffixStorage.LookupByKey(-propRefID);
                            if (itemRandSuffix != null)
                                suffix = itemRandSuffix.Name[player.GetSession().GetSessionDbcLocale()];
                        }
                        else
                        {
                            ItemRandomPropertiesRecord itemRandProp = CliDB.ItemRandomPropertiesStorage.LookupByKey(propRefID);
                            if (itemRandProp != null)
                                suffix = itemRandProp.Name[player.GetSession().GetSessionDbcLocale()];
                        }

                        // dbc local name
                        if (!string.IsNullOrEmpty(suffix))
                        {
                            // Append the suffix (ie: of the Monkey) to the name using localization
                            // or default enUS if localization is invalid
                            name += ' ' + suffix;
                        }
                    }
                }

                // Add the item if no search term or if entered search term was found
                if (packet.Items.Count < 50 && packet.TotalCount >= listfrom)
                    Aentry.BuildAuctionInfo(packet.Items, true);

                ++packet.TotalCount;
            }
        }

        public AuctionEntry GetAuction(uint id)
        {
            return AuctionsMap.LookupByKey(id);
        }

        Dictionary<uint, AuctionEntry> AuctionsMap = new Dictionary<uint, AuctionEntry>();
    }

    public class AuctionEntry
    {
        public void BuildAuctionInfo(List<AuctionItem> items, bool listAuctionItems)
        {
            Item item = Global.AuctionMgr.GetAItem(itemGUIDLow);
            if (!item)
            {
                Log.outError(LogFilter.Server, "AuctionEntry:BuildAuctionInfo: Auction {0} has a non-existent item: {1}", Id, itemGUIDLow);
                return;
            }
            AuctionItem auctionItem = new AuctionItem();

            auctionItem.AuctionItemID = (int)Id;
            auctionItem.Item = new ItemInstance(item);
            auctionItem.BuyoutPrice = buyout;
            auctionItem.CensorBidInfo = false;
            auctionItem.CensorServerSideInfo = listAuctionItems;
            auctionItem.Charges = item.GetSpellCharges();
            auctionItem.Count = (int)item.GetCount();
            auctionItem.DeleteReason = 0; // Always 0 ?
            auctionItem.DurationLeft = (int)((expire_time - Time.UnixTime) * Time.InMilliseconds);
            auctionItem.EndTime = (uint)expire_time;
            auctionItem.Flags = 0; // todo
            auctionItem.ItemGuid = item.GetGUID();
            auctionItem.MinBid = startbid;
            auctionItem.Owner = ObjectGuid.Create(HighGuid.Player, owner);
            auctionItem.OwnerAccountID = ObjectGuid.Create(HighGuid.WowAccount, ObjectManager.GetPlayerAccountIdByGUID(auctionItem.Owner));
            auctionItem.MinIncrement = bidder != 0 ? GetAuctionOutBid() : 0;
            auctionItem.Bidder = bidder != 0 ? ObjectGuid.Create(HighGuid.Player, bidder) : ObjectGuid.Empty;
            auctionItem.BidAmount = bidder != 0 ? bid : 0;

            for (EnchantmentSlot c = 0; c < EnchantmentSlot.MaxInspected; c++)
            {
                if (item.GetEnchantmentId(c) == 0)
                    continue;

                auctionItem.Enchantments.Add(new ItemEnchantData((int)item.GetEnchantmentId(c), item.GetEnchantmentDuration(c), (int)item.GetEnchantmentCharges(c), (byte)c));
            }

            byte i = 0;
            foreach (ItemDynamicFieldGems gemData in item.GetGems())
            {
                if (gemData.ItemId != 0)
                {
                    ItemGemData gem = new ItemGemData();
                    gem.Slot = i;
                    gem.Item = new ItemInstance(gemData);
                    auctionItem.Gems.Add(gem);
                }
                ++i;
            }

            items.Add(auctionItem);
        }

        public ulong GetAuctionCut()
        {
            long cut = (long)(MathFunctions.CalculatePct(bid, auctionHouseEntry.ConsignmentRate) * WorldConfig.GetFloatValue(WorldCfg.RateAuctionCut));
            return (ulong)Math.Max(cut, 0);
        }

        /// <summary>
        /// the sum of outbid is (1% from current bid)*5, if bid is very small, it is 1c
        /// </summary>
        /// <returns></returns>
        public ulong GetAuctionOutBid()
        {
            ulong outbid = MathFunctions.CalculatePct(bid, 5);
            return outbid != 0 ? outbid : 1;
        }

        public void DeleteFromDB(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_AUCTION);
            stmt.AddValue(0, Id);
            trans.Append(stmt);
        }

        public void SaveToDB(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_AUCTION);
            stmt.AddValue(0, Id);
            stmt.AddValue(1, auctioneer);
            stmt.AddValue(2, itemGUIDLow);
            stmt.AddValue(3, owner);
            stmt.AddValue(4, buyout);
            stmt.AddValue(5, expire_time);
            stmt.AddValue(6, bidder);
            stmt.AddValue(7, bid);
            stmt.AddValue(8, startbid);
            stmt.AddValue(9, deposit);
            trans.Append(stmt);
        }

        public bool LoadFromDB(SQLFields fields)
        {
            Id = fields.Read<uint>(0);
            auctioneer = fields.Read<ulong>(1);
            itemGUIDLow = fields.Read<ulong>(2);
            itemEntry = fields.Read<uint>(3);
            itemCount = fields.Read<uint>(4);
            owner = fields.Read<ulong>(5);
            buyout = fields.Read<ulong>(6);
            expire_time = fields.Read<uint>(7);
            bidder = fields.Read<ulong>(8);
            bid = fields.Read<ulong>(9);
            startbid = fields.Read<ulong>(10);
            deposit = fields.Read<ulong>(11);

            CreatureData auctioneerData = Global.ObjectMgr.GetCreatureData(auctioneer);
            if (auctioneerData == null)
            {
                Log.outError(LogFilter.Server, "Auction {0} has not a existing auctioneer (GUID : {1})", Id, auctioneer);
                return false;
            }

            CreatureTemplate auctioneerInfo = Global.ObjectMgr.GetCreatureTemplate(auctioneerData.id);
            if (auctioneerInfo == null)
            {
                Log.outError(LogFilter.Server, "Auction {0} has not a existing auctioneer (GUID : {1} Entry: {2})", Id, auctioneer, auctioneerData.id);
                return false;
            }

            factionTemplateId = auctioneerInfo.Faction;
            auctionHouseEntry = Global.AuctionMgr.GetAuctionHouseEntry(factionTemplateId, ref houseId);
            if (auctionHouseEntry == null)
            {
                Log.outError(LogFilter.Server, "Auction {0} has auctioneer (GUID : {1} Entry: {2}) with wrong faction {3}", Id, auctioneer, auctioneerData.id, factionTemplateId);
                return false;
            }

            // check if sold item exists for guid
            // and itemEntry in fact (GetAItem will fail if problematic in result check in AuctionHouseMgr.LoadAuctionItems)
            if (!Global.AuctionMgr.GetAItem(itemGUIDLow))
            {
                Log.outError(LogFilter.Server, "Auction {0} has not a existing item : {1}", Id, itemGUIDLow);
                return false;
            }
            return true;
        }

        public bool LoadFromFieldList(SQLFields fields)
        {
            // Loads an AuctionEntry item from a field list. Unlike "LoadFromDB()", this one
            //  does not require the AuctionEntryMap to have been loaded with items. It simply
            //  acts as a wrapper to fill out an AuctionEntry struct from a field list

            Id = fields.Read<uint>(0);
            auctioneer = fields.Read<uint>(1);
            itemGUIDLow = fields.Read<uint>(2);
            itemEntry = fields.Read<uint>(3);
            itemCount = fields.Read<uint>(4);
            owner = fields.Read<uint>(5);
            buyout = fields.Read<uint>(6);
            expire_time = fields.Read<uint>(7);
            bidder = fields.Read<uint>(8);
            bid = fields.Read<uint>(9);
            startbid = fields.Read<uint>(10);
            deposit = fields.Read<uint>(11);

            CreatureData auctioneerData = Global.ObjectMgr.GetCreatureData(auctioneer);
            if (auctioneerData == null)
            {
                Log.outError(LogFilter.Server, "AuctionEntry:LoadFromFieldList() - Auction {0} has not a existing auctioneer (GUID : {1})", Id, auctioneer);
                return false;
            }

            CreatureTemplate auctioneerInfo = Global.ObjectMgr.GetCreatureTemplate(auctioneerData.id);
            if (auctioneerInfo == null)
            {
                Log.outError(LogFilter.Server, "AuctionEntry:LoadFromFieldList() - Auction {0} has not a existing auctioneer (GUID : {1} Entry: {2})", Id, auctioneer, auctioneerData.id);
                return false;
            }

            factionTemplateId = auctioneerInfo.Faction;
            auctionHouseEntry = Global.AuctionMgr.GetAuctionHouseEntry(factionTemplateId);

            if (auctionHouseEntry == null)
            {
                Log.outError(LogFilter.Server, "AuctionEntry:LoadFromFieldList() - Auction {0} has auctioneer (GUID : {1} Entry: {2}) with wrong faction {3}", Id, auctioneer, auctioneerData.id, factionTemplateId);
                return false;
            }

            return true;
        }

        public string BuildAuctionMailSubject(MailAuctionAnswers response)
        {
            return $"{itemEntry}:0:{response}:{Id}:{itemCount}";
        }

        public static string BuildAuctionMailBody(ulong lowGuid, ulong bid, ulong buyout, ulong deposit, ulong cut)
        {
            return string.Format($"{lowGuid}:{bid}:{buyout}:{deposit}:{cut}");
        }

        // helpers
        public uint GetHouseId() { return houseId; }
        public uint GetHouseFaction() { return auctionHouseEntry.FactionID; }

        public uint Id;
        public ulong auctioneer;                                      // creature low guid
        public ulong itemGUIDLow;
        public uint itemEntry;
        public uint itemCount;
        public ulong owner;
        public ulong startbid;                                        //maybe useless
        public ulong bid;
        public ulong buyout;
        public long expire_time;
        public ulong bidder;
        public ulong deposit;                                         //deposit can be calculated only when creating auction
        public uint etime;
        uint houseId;
        public AuctionHouseRecord auctionHouseEntry;             // in AuctionHouse.dbc
        public uint factionTemplateId;
    }

    public class AuctionSearchFilters
    {
        public enum FilterType : uint
        {
            SkipClass = 0,
            SkipSubclass = 0xFFFFFFFF,
            SkipInvtype = 0xFFFFFFFF
        }

        public Array<SubclassFilter> Classes = new Array<SubclassFilter>((int)ItemClass.Max);

        public class SubclassFilter
        {
            public FilterType SubclassMask = FilterType.SkipClass;
            public Array<uint> InvTypes = new Array<uint>(ItemConst.MaxItemSubclassTotal);
        }
    }
}
