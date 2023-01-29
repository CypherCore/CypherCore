// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game
{
    public class AuctionsBucketData
    {
        public class Sorter : IComparer<AuctionsBucketData>
        {
            private readonly Locale _locale;
            private readonly int _sortCount;
            private readonly AuctionSortDef[] _sorts;

            public Sorter(Locale locale, AuctionSortDef[] sorts, int sortCount)
            {
                _locale = locale;
                _sorts = sorts;
                _sortCount = sortCount;
            }

            public int Compare(AuctionsBucketData left, AuctionsBucketData right)
            {
                for (var i = 0; i < _sortCount; ++i)
                {
                    long ordering = CompareColumns(_sorts[i].SortOrder, left, right);

                    if (ordering != 0)
                        return (ordering < 0).CompareTo(!_sorts[i].ReverseSort);
                }

                return left.Key != right.Key ? 1 : 0;
            }

            private long CompareColumns(AuctionHouseSortOrder column, AuctionsBucketData left, AuctionsBucketData right)
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
        }

        public List<AuctionPosting> Auctions { get; set; } = new();
        public string[] FullName { get; set; } = new string[(int)Locale.Total];
        public byte InventoryType { get; set; }

        // filter helpers
        public byte ItemClass { get; set; }
        public (uint Id, uint Count)[] ItemModifiedAppearanceId { get; set; } = new (uint Id, uint Count)[4]; // for uncollected search
        public byte ItemSubClass { get; set; }
        public AuctionsBucketKey Key { get; set; }
        public byte MaxBattlePetLevel { get; set; } = 0;
        public byte MinBattlePetLevel { get; set; } = 0;
        public ulong MinPrice { get; set; } // for sort
        public uint[] QualityCounts { get; set; } = new uint[(int)ItemQuality.Max];
        public AuctionHouseFilterMask QualityMask { get; set; }
        public byte RequiredLevel { get; set; } = 0; // for usable search
        public byte SortLevel { get; set; } = 0;

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
                    }
                }

                bucketInfo.ContainsOwnerItem = bucketInfo.ContainsOwnerItem || auction.Owner == player.GetGUID();
            }

            bucketInfo.ContainsOnlyCollectedAppearances = true;

            foreach (var appearance in ItemModifiedAppearanceId)
                if (appearance.Id != 0)
                {
                    bucketInfo.ItemModifiedAppearanceIDs.Add(appearance.Id);

                    if (!player.GetSession().GetCollectionMgr().HasItemAppearance(appearance.Id).PermAppearance)
                        bucketInfo.ContainsOnlyCollectedAppearances = false;
                }
        }
    }
}