// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;

namespace Game.Entities
{
	public class ItemEnchantmentManager
	{
		private static Dictionary<uint, RandomBonusListIds> _storage = new();

		public static void LoadItemRandomBonusListTemplates()
		{
			uint oldMSTime = Time.GetMSTime();

			_storage.Clear();

			//                                         0   1            2
			SQLResult result = DB.World.Query("SELECT Id, BonusListID, Chance FROM item_random_bonus_list_template");

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.Player, "Loaded 0 Item Enchantment definitions. DB table `item_enchantment_template` is empty.");

				return;
			}

			uint count = 0;

			do
			{
				uint  id          = result.Read<uint>(0);
				uint  bonusListId = result.Read<uint>(1);
				float chance      = result.Read<float>(2);

				if (Global.DB2Mgr.GetItemBonusList(bonusListId) == null)
				{
					Log.outError(LogFilter.Sql, $"Bonus list {bonusListId} used in `item_random_bonus_list_template` by id {id} doesn't have exist in ItemBonus.db2");

					continue;
				}

				if (chance < 0.000001f ||
				    chance > 100.0f)
				{
					Log.outError(LogFilter.Sql, $"Bonus list {bonusListId} used in `item_random_bonus_list_template` by id {id} has invalid chance {chance}");

					continue;
				}

				if (!_storage.ContainsKey(id))
					_storage[id] = new RandomBonusListIds();

				RandomBonusListIds ids = _storage[id];
				ids.BonusListIDs.Add(bonusListId);
				ids.Chances.Add(chance);

				++count;
			} while (result.NextRow());

			Log.outInfo(LogFilter.Player, $"Loaded {count} Random Item bonus list definitions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
		}

		public static uint GenerateItemRandomBonusListId(uint item_id)
		{
			ItemTemplate itemProto = Global.ObjectMgr.GetItemTemplate(item_id);

			if (itemProto == null)
				return 0;

			// Item must have one from this field values not null if it can have random enchantments
			if (itemProto.RandomBonusListTemplateId == 0)
				return 0;

			var tab = _storage.LookupByKey(itemProto.RandomBonusListTemplateId);

			if (tab == null)
			{
				Log.outError(LogFilter.Sql, $"Item RandomBonusListTemplateId id {itemProto.RandomBonusListTemplateId} used in `item_template_addon` but it does not have records in `item_random_bonus_list_template` table.");

				return 0;
			}

			//todo fix me this is ulgy
			return tab.BonusListIDs.SelectRandomElementByWeight(x => (float)tab.Chances[tab.BonusListIDs.IndexOf(x)]);
		}

		public static float GetRandomPropertyPoints(uint itemLevel, ItemQuality quality, InventoryType inventoryType, uint subClass)
		{
			uint propIndex;

			switch (inventoryType)
			{
				case InventoryType.Head:
				case InventoryType.Body:
				case InventoryType.Chest:
				case InventoryType.Legs:
				case InventoryType.Ranged:
				case InventoryType.Weapon2Hand:
				case InventoryType.Robe:
				case InventoryType.Thrown:
					propIndex = 0;

					break;
				case InventoryType.RangedRight:
					if ((ItemSubClassWeapon)subClass == ItemSubClassWeapon.Wand)
						propIndex = 3;
					else
						propIndex = 0;

					break;
				case InventoryType.Weapon:
				case InventoryType.WeaponMainhand:
				case InventoryType.WeaponOffhand:
					propIndex = 3;

					break;
				case InventoryType.Shoulders:
				case InventoryType.Waist:
				case InventoryType.Feet:
				case InventoryType.Hands:
				case InventoryType.Trinket:
					propIndex = 1;

					break;
				case InventoryType.Neck:
				case InventoryType.Wrists:
				case InventoryType.Finger:
				case InventoryType.Shield:
				case InventoryType.Cloak:
				case InventoryType.Holdable:
					propIndex = 2;

					break;
				case InventoryType.Relic:
					propIndex = 4;

					break;
				default:
					return 0;
			}

			RandPropPointsRecord randPropPointsEntry = CliDB.RandPropPointsStorage.LookupByKey(itemLevel);

			if (randPropPointsEntry == null)
				return 0;

			switch (quality)
			{
				case ItemQuality.Uncommon:
					return randPropPointsEntry.GoodF[propIndex];
				case ItemQuality.Rare:
				case ItemQuality.Heirloom:
					return randPropPointsEntry.SuperiorF[propIndex];
				case ItemQuality.Epic:
				case ItemQuality.Legendary:
				case ItemQuality.Artifact:
					return randPropPointsEntry.EpicF[propIndex];
			}

			return 0;
		}
	}

	public class RandomBonusListIds
	{
		public List<uint> BonusListIDs = new();
		public List<double> Chances = new();
	}
}