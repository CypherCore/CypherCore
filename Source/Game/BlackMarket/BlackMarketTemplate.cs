// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Collections;
using Framework.Database;
using Game.Networking.Packets;

namespace Game.BlackMarket
{
    public class BlackMarketTemplate
    {
        public float Chance { get; set; }
        public long Duration { get; set; }
        public ItemInstance Item { get; set; }

        public uint MarketID { get; set; }
        public ulong MinBid { get; set; }
        public uint Quantity { get; set; }
        public uint SellerNPC { get; set; }

        public bool LoadFromDB(SQLFields fields)
        {
            MarketID = fields.Read<uint>(0);
            SellerNPC = fields.Read<uint>(1);
            Item = new ItemInstance();
            Item.ItemID = fields.Read<uint>(2);
            Quantity = fields.Read<uint>(3);
            MinBid = fields.Read<ulong>(4);
            Duration = fields.Read<uint>(5);
            Chance = fields.Read<float>(6);

            var bonusListIDsTok = new StringArray(fields.Read<string>(7), ' ');
            List<uint> bonusListIDs = new();

            if (!bonusListIDsTok.IsEmpty())
                foreach (string token in bonusListIDsTok)
                    if (uint.TryParse(token, out uint id))
                        bonusListIDs.Add(id);

            if (!bonusListIDs.Empty())
            {
                Item.ItemBonus = new ItemBonuses();
                Item.ItemBonus.BonusListIDs = bonusListIDs;
            }

            if (Global.ObjectMgr.GetCreatureTemplate(SellerNPC) == null)
            {
                Log.outError(LogFilter.Misc, "Black market template {0} does not have a valid seller. (Entry: {1})", MarketID, SellerNPC);

                return false;
            }

            if (Global.ObjectMgr.GetItemTemplate(Item.ItemID) == null)
            {
                Log.outError(LogFilter.Misc, "Black market template {0} does not have a valid Item. (Entry: {1})", MarketID, Item.ItemID);

                return false;
            }

            return true;
        }
    }
}