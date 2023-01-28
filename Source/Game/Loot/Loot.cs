// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Loots
{
	public class LootItem
	{
		public List<ObjectGuid> allowedGUIDs = new();
		public List<uint> BonusListIDs = new();
		public List<Condition> conditions = new(); // additional loot condition
		public ItemContext context;
		public byte count;
		public bool follow_loot_rules;
		public bool freeforall; // free for all
		public bool is_blocked;
		public bool is_counted;
		public bool is_looted;
		public bool is_underthreshold;

		public uint itemid;
		public uint LootListId;
		public bool needs_quest; // quest drop
		public uint randomBonusListId;
		public ObjectGuid rollWinnerGUID; // Stores the guid of person who won loot, if his bags are full only he can see the Item in loot list!

		public LootItem()
		{
		}

		public LootItem(LootStoreItem li)
		{
			itemid     = li.itemid;
			conditions = li.conditions;

			ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemid);
			freeforall        = proto != null && proto.HasFlag(ItemFlags.MultiDrop);
			follow_loot_rules = !li.needs_quest || (proto != null && proto.FlagsCu.HasAnyFlag(ItemFlagsCustom.FollowLootRules));

			needs_quest = li.needs_quest;

			randomBonusListId = ItemEnchantmentManager.GenerateItemRandomBonusListId(itemid);
		}

        /// <summary>
        ///  Basic checks for player/Item compatibility - if false no chance to see the Item in the loot - used only for loot generation
        /// </summary>
        /// <param name="player"></param>
        /// <param name="loot"></param>
        /// <returns></returns>
        public bool AllowedForPlayer(Player player, Loot loot)
		{
			return AllowedForPlayer(player, loot, itemid, needs_quest, follow_loot_rules, false, conditions);
		}

		public static bool AllowedForPlayer(Player player, Loot loot, uint itemid, bool needs_quest, bool follow_loot_rules, bool strictUsabilityCheck, List<Condition> conditions)
		{
			// DB conditions check
			if (!Global.ConditionMgr.IsObjectMeetToConditions(player, conditions))
				return false;

			ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(itemid);

			if (pProto == null)
				return false;

			// not show loot for not own team
			if (pProto.HasFlag(ItemFlags2.FactionHorde) &&
			    player.GetTeam() != Team.Horde)
				return false;

			if (pProto.HasFlag(ItemFlags2.FactionAlliance) &&
			    player.GetTeam() != Team.Alliance)
				return false;

			// Master looter can see all items even if the character can't loot them
			if (loot != null &&
			    loot.GetLootMethod() == LootMethod.MasterLoot &&
			    follow_loot_rules &&
			    loot.GetLootMasterGUID() == player.GetGUID())
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
			if (!pProto.FlagsCu.HasAnyFlag(ItemFlagsCustom.IgnoreQuestStatus) &&
			    ((needs_quest || (pProto.GetStartQuest() != 0 && player.GetQuestStatus(pProto.GetStartQuest()) != QuestStatus.None)) && !player.HasQuestForItem(itemid)))
				return false;

			if (strictUsabilityCheck)
			{
				if ((pProto.IsWeapon() || pProto.IsArmor()) &&
				    !pProto.IsUsableByLootSpecialization(player, true))
					return false;

				if (player.CanRollNeedForItem(pProto, null, false) != InventoryResult.Ok)
					return false;
			}

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

					if (ffaItemItr != null &&
					    !ffaItemItr.is_looted)
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
					if (!loot.roundRobinPlayer.IsEmpty() &&
					    loot.roundRobinPlayer != player.GetGUID())
						return null;

					return LootSlotType.AllowLoot;
				case LootMethod.MasterLoot:
					if (is_underthreshold)
					{
						if (!loot.roundRobinPlayer.IsEmpty() &&
						    loot.roundRobinPlayer != player.GetGUID())
							return null;

						return LootSlotType.AllowLoot;
					}

					return loot.GetLootMasterGUID() == player.GetGUID() ? LootSlotType.Master : LootSlotType.Locked;
				case LootMethod.GroupLoot:
				case LootMethod.NeedBeforeGreed:
					if (is_underthreshold)
						if (!loot.roundRobinPlayer.IsEmpty() &&
						    loot.roundRobinPlayer != player.GetGUID())
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

		public List<ObjectGuid> GetAllowedLooters()
		{
			return allowedGUIDs;
		}
	}

	public class NotNormalLootItem
	{
		public bool is_looted;
		public byte LootListId; // position in quest_items or items;

		public NotNormalLootItem()
		{
			LootListId = 0;
			is_looted  = false;
		}

		public NotNormalLootItem(byte _index, bool _islooted = false)
		{
			LootListId = _index;
			is_looted  = _islooted;
		}
	}

	public class PlayerRollVote
	{
		public byte RollNumber;
		public RollVote Vote;

		public PlayerRollVote()
		{
			Vote       = RollVote.NotValid;
			RollNumber = 0;
		}
	}

	public class LootRoll
	{
		private static TimeSpan LOOT_ROLL_TIMEOUT = TimeSpan.FromMinutes(1);
		private DateTime _endTime = DateTime.MinValue;
		private bool _isStarted;
		private Loot _loot;
		private LootItem _lootItem;

		private Map _map;
		private Dictionary<ObjectGuid, PlayerRollVote> _rollVoteMap = new();
		private RollMask _voteMask;

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

		// Send the roll for the whole group
		private void SendStartRoll()
		{
			ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(_lootItem.itemid);

			foreach (var (playerGuid, roll) in _rollVoteMap)
			{
				if (roll.Vote != RollVote.NotEmitedYet)
					continue;

				Player player = Global.ObjAccessor.GetPlayer(_map, playerGuid);

				if (player == null)
					continue;

				StartLootRoll startLootRoll = new();
				startLootRoll.LootObj    = _loot.GetGUID();
				startLootRoll.MapID      = (int)_map.GetId();
				startLootRoll.RollTime   = (uint)LOOT_ROLL_TIMEOUT.TotalMilliseconds;
				startLootRoll.Method     = _loot.GetLootMethod();
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
			lootRoll.LootObj    = _loot.GetGUID();
			lootRoll.Player     = targetGuid;
			lootRoll.Roll       = rollNumber;
			lootRoll.RollType   = rollType;
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
			lootRollWon.LootObj  = _loot.GetGUID();
			lootRollWon.Winner   = targetGuid;
			lootRollWon.Roll     = rollNumber;
			lootRollWon.RollType = rollType;
			FillPacket(lootRollWon.Item);
			lootRollWon.Item.UIType = LootSlotType.Locked;
			lootRollWon.MainSpec    = true; // offspec rolls not implemented
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
			lootItem.Quantity          = _lootItem.count;
			lootItem.LootListID        = (byte)_lootItem.LootListId;
			lootItem.CanTradeToTapList = _lootItem.allowedGUIDs.Count > 1;
			lootItem.Loot              = new ItemInstance(_lootItem);
		}

		// Try to start the group roll for the specified Item (it may fail for quest Item or any condition
		// If this method return false the roll have to be removed from the container to avoid any problem
		public bool TryToStart(Map map, Loot loot, uint lootListId, ushort enchantingSkill)
		{
			if (!_isStarted)
			{
				if (lootListId >= loot.items.Count)
					return false;

				_map = map;

				// initialize the data needed for the roll
				_lootItem = loot.items[(int)lootListId];

				_loot                = loot;
				_lootItem.is_blocked = true; // block the Item while rolling

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
				ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(_lootItem.itemid);
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
					_endTime   = GameTime.Now() + LOOT_ROLL_TIMEOUT;
					_isStarted = true;

					return true;
				}

				// no need to start roll if one or less player can loot this Item so place it under threshold
				_lootItem.is_underthreshold = true;
				_lootItem.is_blocked        = false;
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

        /**
		 * \brief Check if all player have voted and return true in that case. Also return current winner.
		 * \param winnerItr > will be different than _rollCoteMap.end() if winner exist. (Someone voted greed or need)
		 * \returns true if all players voted
		 */
        private bool AllPlayerVoted(ref KeyValuePair<ObjectGuid, PlayerRollVote> winnerPair)
		{
			uint notVoted      = 0;
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
							isSomeoneNeed = true; // first passage will force to set winner because need is prioritized
							winnerPair    = pair;
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

			ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(_lootItem.itemid);
			uint         itemLevel    = Item.GetItemLevel(itemTemplate, bonusData, 1, 0, 0, 0, 0, false, 0);

			return Item.GetDisenchantLoot(itemTemplate, (uint)bonusData.Quality, itemLevel);
		}

		// terminate the roll
		private void Finish(KeyValuePair<ObjectGuid, PlayerRollVote> winnerPair)
		{
			_lootItem.is_blocked = false;

			if (winnerPair.Value == null)
			{
				SendAllPassed();
			}
			else
			{
				_lootItem.rollWinnerGUID = winnerPair.Key;

				SendLootRollWon(winnerPair.Key, winnerPair.Value.RollNumber, winnerPair.Value.Vote);

				Player player = Global.ObjAccessor.FindConnectedPlayer(winnerPair.Key);

				if (player != null)
				{
					if (winnerPair.Value.Vote == RollVote.Need)
						player.UpdateCriteria(CriteriaType.RollNeed, _lootItem.itemid, winnerPair.Value.RollNumber);
					else if (winnerPair.Value.Vote == RollVote.Disenchant)
						player.UpdateCriteria(CriteriaType.CastSpell, 13262);
					else
						player.UpdateCriteria(CriteriaType.RollGreed, _lootItem.itemid, winnerPair.Value.RollNumber);

					if (winnerPair.Value.Vote == RollVote.Disenchant)
					{
						var  disenchant = GetItemDisenchantLoot();
						Loot loot       = new(_map, _loot.GetOwnerGUID(), LootType.Disenchanting, null);
						loot.FillLoot(disenchant.Id, LootStorage.Disenchant, player, true, false, LootModes.Default, ItemContext.None);

						if (!loot.AutoStore(player, ItemConst.NullBag, ItemConst.NullSlot, true))
							for (uint i = 0; i < loot.items.Count; ++i)
							{
								LootItem disenchantLoot = loot.LootItemInSlot(i, player);

								if (disenchantLoot != null)
									player.SendItemRetrievalMail(disenchantLoot.itemid, disenchantLoot.count, disenchantLoot.context);
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
	}

	public class Loot
	{
		private List<ObjectGuid> _allowedLooters = new();
		private uint _dungeonEncounterId;

		// Loot GUID
		private ObjectGuid _guid;
		private ItemContext _itemContext;
		private ObjectGuid _lootMaster;
		private LootMethod _lootMethod;
		private ObjectGuid _owner;                         // The WorldObject that holds this loot
		private Dictionary<uint, LootRoll> _rolls = new(); // used if an Item is under rolling
		private bool _wasOpened;                           // true if at least one player received the loot content
		public uint gold;

		public List<LootItem> items = new();
		public LootType loot_type; // required for Achievement system
		private MultiMap<ObjectGuid, NotNormalLootItem> PlayerFFAItems = new();

		private List<ObjectGuid> PlayersLooting = new();
		public ObjectGuid roundRobinPlayer; // GUID of the player having the Round-Robin ownership for the loot. If 0, round robin owner has released.
		public byte unlootedCount;

		public Loot(Map map, ObjectGuid owner, LootType type, Group group)
		{
			loot_type    = type;
			_guid        = map ? ObjectGuid.Create(HighGuid.LootObject, map.GetId(), 0, map.GenerateLowGuid(HighGuid.LootObject)) : ObjectGuid.Empty;
			_owner       = owner;
			_itemContext = ItemContext.None;
			_lootMethod  = group != null ? group.GetLootMethod() : LootMethod.FreeForAll;
			_lootMaster  = group != null ? group.GetMasterLooterGuid() : ObjectGuid.Empty;
		}

		// Inserts the Item into the loot (called by LootTemplate processors)
		public void AddItem(LootStoreItem item)
		{
			ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(item.itemid);

			if (proto == null)
				return;

			uint count  = RandomHelper.URand(item.mincount, item.maxcount);
			uint stacks = (uint)(count / proto.GetMaxStackSize() + (Convert.ToBoolean(count % proto.GetMaxStackSize()) ? 1 : 0));

			for (uint i = 0; i < stacks && items.Count < SharedConst.MaxNRLootItems; ++i)
			{
				LootItem generatedLoot = new(item);
				generatedLoot.context    = _itemContext;
				generatedLoot.count      = (byte)Math.Min(count, proto.GetMaxStackSize());
				generatedLoot.LootListId = (uint)items.Count;

				if (_itemContext != 0)
				{
					List<uint> bonusListIDs = Global.DB2Mgr.GetDefaultItemBonusTree(generatedLoot.itemid, _itemContext);
					generatedLoot.BonusListIDs.AddRange(bonusListIDs);
				}

				items.Add(generatedLoot);
				count -= proto.GetMaxStackSize();
			}
		}

		public bool AutoStore(Player player, byte bag, byte slot, bool broadcast = false, bool createdByPlayer = false)
		{
			bool allLooted = true;

			for (uint i = 0; i < items.Count; ++i)
			{
				LootItem lootItem = LootItemInSlot(i, player, out NotNormalLootItem ffaitem);

				if (lootItem == null ||
				    lootItem.is_looted)
					continue;

				if (!lootItem.HasAllowedLooter(GetGUID()))
					continue;

				if (lootItem.is_blocked)
					continue;

				// dont allow protected Item to be looted by someone else
				if (!lootItem.rollWinnerGUID.IsEmpty() &&
				    lootItem.rollWinnerGUID != GetGUID())
					continue;

				List<ItemPosCount> dest = new();
				InventoryResult    msg  = player.CanStoreNewItem(bag, slot, dest, lootItem.itemid, lootItem.count);

				if (msg != InventoryResult.Ok &&
				    slot != ItemConst.NullSlot)
					msg = player.CanStoreNewItem(bag, ItemConst.NullSlot, dest, lootItem.itemid, lootItem.count);

				if (msg != InventoryResult.Ok &&
				    bag != ItemConst.NullBag)
					msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, lootItem.itemid, lootItem.count);

				if (msg != InventoryResult.Ok)
				{
					player.SendEquipError(msg, null, null, lootItem.itemid);
					allLooted = false;

					continue;
				}

				if (ffaitem != null)
					ffaitem.is_looted = true;

				if (!lootItem.freeforall)
					lootItem.is_looted = true;

				--unlootedCount;

				Item pItem = player.StoreNewItem(dest, lootItem.itemid, true, lootItem.randomBonusListId, null, lootItem.context, lootItem.BonusListIDs);
				player.SendNewItem(pItem, lootItem.count, false, createdByPlayer, broadcast);
				player.ApplyItemLootedSpell(pItem, true);
			}

			return allLooted;
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

			tab.Process(this, store.IsRatesAllowed(), (byte)lootMode, 0); // Processing is done there, callback via Loot.AddItem()

			// Setting access rights for group loot case
			Group group = lootOwner.GetGroup();

			if (!personal &&
			    group != null)
			{
				if (loot_type == LootType.Corpse)
					roundRobinPlayer = lootOwner.GetGUID();

				for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
				{
					Player player = refe.GetSource();

					if (player) // should actually be looted object instead of lootOwner but looter has to be really close so doesnt really matter
						if (player.IsAtGroupRewardDistance(lootOwner))
							FillNotNormalLootFor(player);
				}

				foreach (LootItem item in items)
				{
					if (!item.follow_loot_rules ||
					    item.freeforall)
						continue;

					ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(item.itemid);

					if (proto != null)
					{
						if (proto.GetQuality() < group.GetLootThreshold())
							item.is_underthreshold = true;
						else
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
				PlayerFFAItems[player.GetGUID()] = ffaItems;
		}

		public void NotifyItemRemoved(byte lootListId, Map map)
		{
			// notify all players that are looting this that the Item was removed
			// convert the index to the Slot the player sees
			for (int i = 0; i < PlayersLooting.Count; ++i)
			{
				LootItem item = items[lootListId];

				if (!item.GetAllowedLooters().Contains(PlayersLooting[i]))
					continue;

				Player player = Global.ObjAccessor.GetPlayer(map, PlayersLooting[i]);

				if (player)
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

					for (uint lootListId = 0; lootListId < items.Count; ++lootListId)
					{
						LootItem item = items[(int)lootListId];

						if (!item.is_blocked)
							continue;

						LootRoll lootRoll = new();
						var      inserted = _rolls.TryAdd(lootListId, lootRoll);

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

			LootItem item      = items[(int)lootListId];
			bool     is_looted = item.is_looted;

			if (item.freeforall)
			{
				var itemList = PlayerFFAItems.LookupByKey(player.GetGUID());

				if (itemList != null)
					foreach (NotNormalLootItem notNormalLootItem in itemList)
						if (notNormalLootItem.LootListId == lootListId)
						{
							is_looted = notNormalLootItem.is_looted;
							ffaItem   = notNormalLootItem;

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
			if (gold != 0)
				return true;

			foreach (LootItem item in items)
				if (!item.is_looted &&
				    item.follow_loot_rules &&
				    !item.freeforall &&
				    item.conditions.Empty())
					return true;

			return false;
		}

		// return true if there is any FFA, quest or conditional Item for the player.
		public bool HasItemFor(Player player)
		{
			// quest items
			foreach (LootItem lootItem in items)
				if (!lootItem.is_looted &&
				    !lootItem.follow_loot_rules &&
				    lootItem.GetAllowedLooters().Contains(player.GetGUID()))
					return true;

			var ffaItems = GetPlayerFFAItems().LookupByKey(player.GetGUID());

			if (ffaItems != null)
			{
				bool hasFfaItem = ffaItems.Any(ffaItem => !ffaItem.is_looted);

				if (hasFfaItem)
					return true;
			}

			return false;
		}

		// return true if there is any Item over the group threshold (i.e. not underthreshold).
		public bool HasOverThresholdItem()
		{
			for (byte i = 0; i < items.Count; ++i)
				if (!items[i].is_looted &&
				    !items[i].is_underthreshold &&
				    !items[i].freeforall)
					return true;

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

				LootItemData lootItem = new();
				lootItem.LootListID = (byte)item.LootListId;
				lootItem.UIType     = uiType.Value;
				lootItem.Quantity   = item.count;
				lootItem.Loot       = new ItemInstance(item);
				packet.Items.Add(lootItem);
			}
		}

		public void NotifyLootList(Map map)
		{
			LootList lootList = new();

			lootList.Owner   = GetOwnerGUID();
			lootList.LootObj = GetGUID();

			if (GetLootMethod() == LootMethod.MasterLoot &&
			    HasOverThresholdItem())
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

		public bool IsLooted()
		{
			return gold == 0 && unlootedCount == 0;
		}

		public void AddLooter(ObjectGuid guid)
		{
			PlayersLooting.Add(guid);
		}

		public void RemoveLooter(ObjectGuid guid)
		{
			PlayersLooting.Remove(guid);
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
			return PlayerFFAItems;
		}
	}

	public class AELootResult
	{
		private Dictionary<Item, int> _byItem = new();

		private List<ResultValue> _byOrder = new();

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
				value.item               = item;
				value.count              = count;
				value.lootType           = lootType;
				value.dungeonEncounterId = dungeonEncounterId;
				_byOrder.Add(value);
			}
		}

		public List<ResultValue> GetByOrder()
		{
			return _byOrder;
		}

		public struct ResultValue
		{
			public Item item;
			public byte count;
			public LootType lootType;
			public uint dungeonEncounterId;
		}
	}
}