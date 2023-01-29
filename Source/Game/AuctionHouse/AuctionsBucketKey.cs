// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game
{
    public class AuctionsBucketKey : IComparable<AuctionsBucketKey>
	{
		public AuctionsBucketKey(uint itemId, ushort itemLevel, ushort battlePetSpeciesId, ushort suffixItemNameDescriptionId)
		{
			ItemId                      = itemId;
			ItemLevel                   = itemLevel;
			BattlePetSpeciesId          = battlePetSpeciesId;
			SuffixItemNameDescriptionId = suffixItemNameDescriptionId;
		}

		public AuctionsBucketKey(AuctionBucketKey key)
		{
			ItemId                      = key.ItemID;
			ItemLevel                   = key.ItemLevel;
			BattlePetSpeciesId          = (ushort)(key.BattlePetSpeciesID.HasValue ? key.BattlePetSpeciesID.Value : 0);
			SuffixItemNameDescriptionId = (ushort)(key.SuffixItemNameDescriptionID.HasValue ? key.SuffixItemNameDescriptionID.Value : 0);
		}

		public uint ItemId { get; set; }
		public ushort ItemLevel { get; set; }
		public ushort BattlePetSpeciesId { get; set; }
		public ushort SuffixItemNameDescriptionId { get; set; }

		public int CompareTo(AuctionsBucketKey other)
		{
			return ItemId.CompareTo(other.ItemId);
		}

		public static bool operator ==(AuctionsBucketKey right, AuctionsBucketKey left)
		{
			return right.ItemId == left.ItemId && right.ItemLevel == left.ItemLevel && right.BattlePetSpeciesId == left.BattlePetSpeciesId && right.SuffixItemNameDescriptionId == left.SuffixItemNameDescriptionId;
		}

		public static bool operator !=(AuctionsBucketKey right, AuctionsBucketKey left)
		{
			return !(right == left);
		}

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
				return new AuctionsBucketKey(item.GetEntry(),
				                             (ushort)Item.GetItemLevel(itemTemplate, item.GetBonus(), 0, (uint)item.GetRequiredLevel(), 0, 0, 0, false, 0),
				                             (ushort)item.GetModifier(ItemModifier.BattlePetSpeciesId),
				                             (ushort)item.GetBonus().Suffix);
			else
				return ForCommodity(itemTemplate);
		}

		public static AuctionsBucketKey ForCommodity(ItemTemplate itemTemplate)
		{
			return new AuctionsBucketKey(itemTemplate.GetId(), (ushort)itemTemplate.GetBaseItemLevel(), 0, 0);
		}
	}
}