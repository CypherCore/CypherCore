// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Collections;
using Framework.Constants;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Entities
{
	public class CreatureTemplate
	{
		public string AIName;
		public uint BaseAttackTime;
		public float BaseVariance;
		public int CreatureDifficultyID;
		public CreatureType CreatureType;
		public uint[] DifficultyEntry = new uint[SharedConst.MaxCreatureDifficulties];
		public uint DmgSchool;
		public uint DynamicFlags;
		public uint Entry;
		public uint Faction;
		public CreatureFamily Family;
		public string FemaleName;
		public CreatureFlagsExtra FlagsExtra;
		public uint GossipMenuId;
		public int HealthScalingExpansion;
		public float HoverHeight;
		public string IconName;
		public uint[] KillCredit = new uint[SharedConst.MaxCreatureKillCredit];
		public uint LootId;
		public uint MaxGold;
		public short Maxlevel;
		public ulong MechanicImmuneMask;
		public uint MinGold;
		public short Minlevel;
		public float ModArmor;
		public float ModDamage;
		public List<CreatureModel> Models = new();
		public float ModExperience;
		public float ModHealth;
		public float ModHealthExtra;
		public float ModMana;
		public float ModManaExtra;
		public CreatureMovementData Movement = new();
		public uint MovementId;
		public uint MovementType;
		public string Name;
		public ulong Npcflag;
		public uint PickPocketId;

		public QueryCreatureResponse QueryData;
		public bool RacialLeader;
		public uint RangeAttackTime;
		public float RangeVariance;
		public CreatureEliteType Rank;
		public bool RegenHealth;
		public uint RequiredExpansion;
		public int[] Resistance = new int[7];
		public float Scale;
		public Dictionary<Difficulty, CreatureLevelScaling> scalingStorage = new();
		public uint ScriptID;
		public uint SkinLootId;
		public float SpeedRun;
		public float SpeedWalk;
		public uint[] Spells = new uint[8];
		public uint SpellSchoolImmuneMask;
		public string StringId;
		public string SubName;
		public string TitleAlt;
		public Class TrainerClass;
		public CreatureTypeFlags TypeFlags;
		public uint TypeFlags2;
		public uint UnitClass;
		public UnitFlags UnitFlags;
		public uint UnitFlags2;
		public uint UnitFlags3;
		public uint VehicleId;
		public uint VignetteID; // @todo Read Vignette.db2
		public int WidgetSetID;
		public int WidgetSetUnitConditionID;

		public CreatureModel GetModelByIdx(int idx)
		{
			return idx < Models.Count ? Models[idx] : null;
		}

		public CreatureModel GetRandomValidModel()
		{
			if (Models.Empty())
				return null;

			// If only one element, ignore the Probability (even if 0)
			if (Models.Count == 1)
				return Models[0];

			var selectedItr = Models.SelectRandomElementByWeight(model => { return model.Probability; });

			return selectedItr;
		}

		public CreatureModel GetFirstValidModel()
		{
			foreach (CreatureModel model in Models)
				if (model.CreatureDisplayID != 0)
					return model;

			return null;
		}

		public CreatureModel GetModelWithDisplayId(uint displayId)
		{
			foreach (CreatureModel model in Models)
				if (displayId == model.CreatureDisplayID)
					return model;

			return null;
		}

		public CreatureModel GetFirstInvisibleModel()
		{
			foreach (CreatureModel model in Models)
			{
				CreatureModelInfo modelInfo = Global.ObjectMgr.GetCreatureModelInfo(model.CreatureDisplayID);

				if (modelInfo != null &&
				    modelInfo.IsTrigger)
					return model;
			}

			return CreatureModel.DefaultInvisibleModel;
		}

		public CreatureModel GetFirstVisibleModel()
		{
			foreach (CreatureModel model in Models)
			{
				CreatureModelInfo modelInfo = Global.ObjectMgr.GetCreatureModelInfo(model.CreatureDisplayID);

				if (modelInfo != null &&
				    !modelInfo.IsTrigger)
					return model;
			}

			return CreatureModel.DefaultVisibleModel;
		}

		public int[] GetMinMaxLevel()
		{
			return new[]
			       {
				       HealthScalingExpansion != (int)Expansion.LevelCurrent ? Minlevel : Minlevel + SharedConst.MaxLevel, HealthScalingExpansion != (int)Expansion.LevelCurrent ? Maxlevel : Maxlevel + SharedConst.MaxLevel
			       };
		}

		public int GetHealthScalingExpansion()
		{
			return HealthScalingExpansion == (int)Expansion.LevelCurrent ? (int)Expansion.WarlordsOfDraenor : HealthScalingExpansion;
		}

		public SkillType GetRequiredLootSkill()
		{
			if (TypeFlags.HasAnyFlag(CreatureTypeFlags.SkinWithHerbalism))
				return SkillType.Herbalism;
			else if (TypeFlags.HasAnyFlag(CreatureTypeFlags.SkinWithMining))
				return SkillType.Mining;
			else if (TypeFlags.HasAnyFlag(CreatureTypeFlags.SkinWithEngineering))
				return SkillType.Engineering;
			else
				return SkillType.Skinning; // normal case
		}

		public bool IsExotic()
		{
			return (TypeFlags & CreatureTypeFlags.TameableExotic) != 0;
		}

		public bool IsTameable(bool canTameExotic)
		{
			if (CreatureType != CreatureType.Beast ||
			    Family == CreatureFamily.None ||
			    !TypeFlags.HasAnyFlag(CreatureTypeFlags.Tameable))
				return false;

			// if can tame exotic then can tame any tameable
			return canTameExotic || !IsExotic();
		}

		public static int DifficultyIDToDifficultyEntryIndex(uint difficulty)
		{
			switch ((Difficulty)difficulty)
			{
				case Difficulty.None:
				case Difficulty.Normal:
				case Difficulty.Raid10N:
				case Difficulty.Raid40:
				case Difficulty.Scenario3ManN:
				case Difficulty.NormalRaid:
					return -1;
				case Difficulty.Heroic:
				case Difficulty.Raid25N:
				case Difficulty.Scenario3ManHC:
				case Difficulty.HeroicRaid:
					return 0;
				case Difficulty.Raid10HC:
				case Difficulty.MythicKeystone:
				case Difficulty.MythicRaid:
					return 1;
				case Difficulty.Raid25HC:
					return 2;
				case Difficulty.LFR:
				case Difficulty.LFRNew:
				case Difficulty.EventRaid:
				case Difficulty.EventDungeon:
				case Difficulty.EventScenario:
				default:
					return -1;
			}
		}

		public void InitializeQueryData()
		{
			QueryData = new QueryCreatureResponse();

			QueryData.CreatureID = Entry;
			QueryData.Allow      = true;

			CreatureStats stats = new();
			stats.Leader = RacialLeader;

			stats.Name[0]    = Name;
			stats.NameAlt[0] = FemaleName;

			stats.Flags[0] = (uint)TypeFlags;
			stats.Flags[1] = TypeFlags2;

			stats.CreatureType   = (int)CreatureType;
			stats.CreatureFamily = (int)Family;
			stats.Classification = (int)Rank;

			for (uint i = 0; i < SharedConst.MaxCreatureKillCredit; ++i)
				stats.ProxyCreatureID[i] = KillCredit[i];

			foreach (var model in Models)
			{
				stats.Display.TotalProbability += model.Probability;
				stats.Display.CreatureDisplay.Add(new CreatureXDisplay(model.CreatureDisplayID, model.DisplayScale, model.Probability));
			}

			stats.HpMulti     = ModHealth;
			stats.EnergyMulti = ModMana;

			stats.CreatureMovementInfoID   = MovementId;
			stats.RequiredExpansion        = RequiredExpansion;
			stats.HealthScalingExpansion   = HealthScalingExpansion;
			stats.VignetteID               = VignetteID;
			stats.Class                    = (int)UnitClass;
			stats.CreatureDifficultyID     = CreatureDifficultyID;
			stats.WidgetSetID              = WidgetSetID;
			stats.WidgetSetUnitConditionID = WidgetSetUnitConditionID;

			stats.Title      = SubName;
			stats.TitleAlt   = TitleAlt;
			stats.CursorName = IconName;

			var items = Global.ObjectMgr.GetCreatureQuestItemList(Entry);

			if (items != null)
				stats.QuestItems.AddRange(items);

			QueryData.Stats = stats;
		}

		public CreatureLevelScaling GetLevelScaling(Difficulty difficulty)
		{
			var creatureLevelScaling = scalingStorage.LookupByKey(difficulty);

			if (creatureLevelScaling != null)
				return creatureLevelScaling;

			return new CreatureLevelScaling();
		}
	}

	public class CreatureBaseStats
	{
		public uint AttackPower;
		public uint BaseMana;
		public uint RangedAttackPower;

		// Helpers
		public uint GenerateMana(CreatureTemplate info)
		{
			// Mana can be 0.
			if (BaseMana == 0)
				return 0;

			return (uint)Math.Ceiling(BaseMana * info.ModMana * info.ModManaExtra);
		}
	}

	public class CreatureLocale
	{
		public StringArray Name = new((int)Locale.Total);
		public StringArray NameAlt = new((int)Locale.Total);
		public StringArray Title = new((int)Locale.Total);
		public StringArray TitleAlt = new((int)Locale.Total);
	}

	public struct EquipmentItem
	{
		public uint ItemId;
		public ushort AppearanceModId;
		public ushort ItemVisual;
	}

	public class EquipmentInfo
	{
		public EquipmentItem[] Items = new EquipmentItem[SharedConst.MaxEquipmentItems];
	}

	public class CreatureData : SpawnData
	{
		public uint curhealth;
		public uint curmana;
		public uint currentwaypoint;
		public uint displayid;
		public uint dynamicflags;
		public sbyte equipmentId;
		public byte movementType;
		public ulong npcflag;
		public uint unit_flags;  // enum UnitFlags mask values
		public uint unit_flags2; // enum UnitFlags2 mask values
		public uint unit_flags3; // enum UnitFlags3 mask values
		public float WanderDistance;

		public CreatureData() : base(SpawnObjectType.Creature)
		{
		}
	}

	public class CreatureMovementData
	{
		public CreatureChaseMovementType Chase;
		public CreatureFlightMovementType Flight;
		public CreatureGroundMovementType Ground;
		public uint InteractionPauseTimer;
		public CreatureRandomMovementType Random;
		public bool Rooted;
		public bool Swim;

		public CreatureMovementData()
		{
			Ground                = CreatureGroundMovementType.Run;
			Flight                = CreatureFlightMovementType.None;
			Swim                  = true;
			Rooted                = false;
			Chase                 = CreatureChaseMovementType.Run;
			Random                = CreatureRandomMovementType.Walk;
			InteractionPauseTimer = WorldConfig.GetUIntValue(WorldCfg.CreatureStopForPlayer);
		}

		public bool IsGroundAllowed()
		{
			return Ground != CreatureGroundMovementType.None;
		}

		public bool IsSwimAllowed()
		{
			return Swim;
		}

		public bool IsFlightAllowed()
		{
			return Flight != CreatureFlightMovementType.None;
		}

		public bool IsRooted()
		{
			return Rooted;
		}

		public CreatureChaseMovementType GetChase()
		{
			return Chase;
		}

		public CreatureRandomMovementType GetRandom()
		{
			return Random;
		}

		public uint GetInteractionPauseTimer()
		{
			return InteractionPauseTimer;
		}

		public override string ToString()
		{
			return $"Ground: {Ground}, Swim: {Swim}, Flight: {Flight} {(Rooted ? ", Rooted" : "")}, Chase: {Chase}, Random: {Random}, InteractionPauseTimer: {InteractionPauseTimer}";
		}
	}

	public class CreatureModelInfo
	{
		public float BoundingRadius;
		public float CombatReach;
		public uint DisplayIdOtherGender;
		public sbyte gender;
		public bool IsTrigger;
	}

	public class CreatureModel
	{
		public static CreatureModel DefaultInvisibleModel = new(11686, 1.0f, 1.0f);
		public static CreatureModel DefaultVisibleModel = new(17519, 1.0f, 1.0f);

		public uint CreatureDisplayID;
		public float DisplayScale;
		public float Probability;

		public CreatureModel()
		{
		}

		public CreatureModel(uint creatureDisplayID, float displayScale, float probability)
		{
			CreatureDisplayID = creatureDisplayID;
			DisplayScale      = displayScale;
			Probability       = probability;
		}
	}

	public class CreatureSummonedData
	{
		public uint? CreatureIDVisibleToSummoner;
		public uint? FlyingMountDisplayID;
		public uint? GroundMountDisplayID;
	}

	public class CreatureAddon
	{
		public ushort aiAnimKit;
		public byte animTier;
		public List<uint> auras = new();
		public uint emote;
		public ushort meleeAnimKit;
		public uint mount;
		public ushort movementAnimKit;
		public uint path_id;
		public byte pvpFlags;
		public byte sheathState;
		public byte standState;
		public byte visFlags;
		public VisibilityDistanceType visibilityDistanceType;
	}

	public class VendorItem
	{
		public List<uint> BonusListIDs = new();
		public uint ExtendedCost;
		public bool IgnoreFiltering;
		public uint incrtime; // time for restore items amount if maxcount != 0

		public uint item;
		public uint maxcount; // 0 for infinity Item amount
		public uint PlayerConditionId;
		public ItemVendorType Type;

		public VendorItem()
		{
		}

		public VendorItem(uint _item, int _maxcount, uint _incrtime, uint _ExtendedCost, ItemVendorType _Type)
		{
			item         = _item;
			maxcount     = (uint)_maxcount;
			incrtime     = _incrtime;
			ExtendedCost = _ExtendedCost;
			Type         = _Type;
		}
	}

	public class VendorItemData
	{
		private List<VendorItem> _items = new();

		public VendorItem GetItem(uint slot)
		{
			if (slot >= _items.Count)
				return null;

			return _items[(int)slot];
		}

		public bool Empty()
		{
			return _items.Count == 0;
		}

		public int GetItemCount()
		{
			return _items.Count;
		}

		public void AddItem(VendorItem vItem)
		{
			_items.Add(vItem);
		}

		public bool RemoveItem(uint item_id, ItemVendorType type)
		{
			int i = _items.RemoveAll(p => p.item == item_id && p.Type == type);

			if (i == 0)
				return false;
			else
				return true;
		}

		public VendorItem FindItemCostPair(uint item_id, uint extendedCost, ItemVendorType type)
		{
			return _items.Find(p => p.item == item_id && p.ExtendedCost == extendedCost && p.Type == type);
		}

		public void Clear()
		{
			_items.Clear();
		}
	}

	public class CreatureLevelScaling
	{
		public uint ContentTuningID;
		public short DeltaLevelMax;
		public short DeltaLevelMin;
	}
}