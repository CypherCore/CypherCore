// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;
using System;
using System.Collections.Generic;
using Game.Networking.Packets;
using Game.Maps;
using Game.DataStorage;

namespace Game.Entities
{
    public class CreatureTemplate
    {
        public uint Entry;
        public uint[] KillCredit = new uint[SharedConst.MaxCreatureKillCredit];
        public List<CreatureModel> Models = new();
        public string Name;
        public string FemaleName;
        public string SubName;
        public string TitleAlt;
        public string IconName;
        public List<uint> GossipMenuIds = new();
        public Dictionary<Difficulty, CreatureDifficulty> difficultyStorage = new();
        public int RequiredExpansion;
        public uint VignetteID;
        public uint Faction;
        public ulong Npcflag;
        public float SpeedWalk;
        public float SpeedRun;
        public float Scale;
        public CreatureClassifications Classification;
        public uint DmgSchool;
        public uint BaseAttackTime;
        public uint RangeAttackTime;
        public float BaseVariance;
        public float RangeVariance;
        public uint UnitClass;
        public UnitFlags UnitFlags;
        public uint UnitFlags2;
        public uint UnitFlags3;
        public CreatureFamily Family;
        public Class TrainerClass;
        public CreatureType CreatureType;
        public int[] Resistance = new int[7];
        public uint[] Spells = new uint[8];
        public uint VehicleId;
        public string AIName;
        public uint MovementType;
        public CreatureMovementData Movement = new();
        public float ModExperience;
        public bool RacialLeader;
        public uint MovementId;
        public int WidgetSetID;
        public int WidgetSetUnitConditionID;
        public bool RegenHealth;
        public int CreatureImmunitiesId;
        public CreatureFlagsExtra FlagsExtra;
        public uint ScriptID;
        public string StringId;

        public QueryCreatureResponse[] QueryData = new QueryCreatureResponse[(int)Locale.Total];

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

            var selectedItr = Models.SelectRandomElementByWeight(model =>
            {
                return model.Probability;
            });

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
                if (modelInfo != null && modelInfo.IsTrigger)
                    return model;
            }

            return CreatureModel.DefaultInvisibleModel;
        }

        public CreatureModel GetFirstVisibleModel()
        {
            foreach (CreatureModel model in Models)
            {
                CreatureModelInfo modelInfo = Global.ObjectMgr.GetCreatureModelInfo(model.CreatureDisplayID);
                if (modelInfo != null && !modelInfo.IsTrigger)
                    return model;
            }

            return CreatureModel.DefaultVisibleModel;
        }

        public bool IsExotic(CreatureDifficulty creatureDifficulty)
        {
            return creatureDifficulty.TypeFlags.HasFlag(CreatureTypeFlags.TameableExotic);
        }
        public bool IsTameable(bool canTameExotic, CreatureDifficulty creatureDifficulty)
        {
            if (CreatureType != CreatureType.Beast || Family == CreatureFamily.None || !creatureDifficulty.TypeFlags.HasFlag(CreatureTypeFlags.Tameable))
                return false;

            // if can tame exotic then can tame any tameable
            return canTameExotic || !IsExotic(creatureDifficulty);
        }

        public void InitializeQueryData()
        {
            for (var loc = Locale.enUS; loc < Locale.Total; ++loc)
            {
                if (!WorldConfig.GetBoolValue(WorldCfg.LoadLocales) && loc != SharedConst.DefaultLocale)
                    continue;

                QueryData[(int)loc] = BuildQueryData(loc, Difficulty.None);
            }
        }

        public QueryCreatureResponse BuildQueryData(Locale locale, Difficulty difficulty)
        {
            CreatureDifficulty creatureDifficulty = GetDifficulty(difficulty);

            var queryTemp = new QueryCreatureResponse();

            queryTemp.CreatureID = Entry;
            queryTemp.Allow = true;

            CreatureStats stats = new();
            stats.Leader = RacialLeader;

            stats.Name[0] = Name;
            stats.NameAlt[0] = FemaleName;

            stats.Flags[0] = (uint)creatureDifficulty.TypeFlags;
            stats.Flags[1] = creatureDifficulty.TypeFlags2;

            stats.CreatureType = (int)CreatureType;
            stats.CreatureFamily = (int)Family;
            stats.Classification = (int)Classification;

            for (uint i = 0; i < SharedConst.MaxCreatureKillCredit; ++i)
                stats.ProxyCreatureID[i] = KillCredit[i];

            foreach (var model in Models)
            {
                stats.Display.TotalProbability += model.Probability;
                stats.Display.CreatureDisplay.Add(new CreatureXDisplay(model.CreatureDisplayID, model.DisplayScale, model.Probability));
            }

            stats.HpMulti = creatureDifficulty.HealthModifier;
            stats.EnergyMulti = creatureDifficulty.ManaModifier;

            stats.CreatureMovementInfoID = MovementId;
            stats.RequiredExpansion = (uint)RequiredExpansion;
            stats.HealthScalingExpansion = creatureDifficulty.HealthScalingExpansion;
            stats.VignetteID = VignetteID;
            stats.Class = (int)UnitClass;
            stats.CreatureDifficultyID = creatureDifficulty.CreatureDifficultyID;
            stats.WidgetSetID = WidgetSetID;
            stats.WidgetSetUnitConditionID = WidgetSetUnitConditionID;

            stats.Title = SubName;
            stats.TitleAlt = TitleAlt;
            stats.CursorName = IconName;

            var items = Global.ObjectMgr.GetCreatureQuestItemList(Entry, difficulty);
            if (items != null)
                stats.QuestItems.AddRange(items);

            var currencies = Global.ObjectMgr.GetCreatureQuestCurrencyList(Entry);
            if (currencies != null)
                stats.QuestCurrencies.AddRange(currencies);

            if (locale != Locale.enUS)
            {
                CreatureLocale creatureLocale = Global.ObjectMgr.GetCreatureLocale(Entry);
                if (creatureLocale != null)
                {
                    string name = stats.Name[0];
                    string nameAlt = stats.NameAlt[0];

                    ObjectManager.GetLocaleString(creatureLocale.Name, locale, ref name);
                    ObjectManager.GetLocaleString(creatureLocale.NameAlt, locale, ref nameAlt);
                    ObjectManager.GetLocaleString(creatureLocale.Title, locale, ref stats.Title);
                    ObjectManager.GetLocaleString(creatureLocale.TitleAlt, locale, ref stats.TitleAlt);
                }
            }

            queryTemp.Stats = stats;
            return queryTemp;
        }

        public CreatureDifficulty GetDifficulty(Difficulty difficulty)
        {
            var creatureDifficulty = difficultyStorage.LookupByKey(difficulty);
            if (creatureDifficulty != null)
                return creatureDifficulty;

            // If there is no data for the difficulty, try to get data for the fallback difficulty
            var difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);
            if (difficultyEntry != null)
                return GetDifficulty((Difficulty)difficultyEntry.FallbackDifficultyID);

            return new CreatureDifficulty();
        }
    }

    public class CreatureBaseStats
    {
        public uint BaseMana;
        public uint AttackPower;
        public uint RangedAttackPower;
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
        public CreatureModel display;
        public sbyte equipmentId;
        public float WanderDistance;
        public uint currentwaypoint;
        public uint curHealthPct;
        public byte movementType;
        public ulong? npcflag;
        public uint? unit_flags;                                  // enum UnitFlags mask values
        public uint? unit_flags2;                                 // enum UnitFlags2 mask values
        public uint? unit_flags3;                                 // enum UnitFlags3 mask values

        public CreatureData() : base(SpawnObjectType.Creature) { }
    }

    public class CreatureMovementData
    {
        public bool HoverInitiallyEnabled;
        public CreatureChaseMovementType Chase;
        public CreatureRandomMovementType Random;
        public uint InteractionPauseTimer;

        public CreatureMovementData()
        {
            Chase = CreatureChaseMovementType.Run;
            Random = CreatureRandomMovementType.Walk;
            InteractionPauseTimer = WorldConfig.GetUIntValue(WorldCfg.CreatureStopForPlayer);
        }

        public CreatureChaseMovementType GetChase() { return Chase; }
        public CreatureRandomMovementType GetRandom() { return Random; }

        public bool IsHoverInitiallyEnabled() { return HoverInitiallyEnabled; }

        public uint GetInteractionPauseTimer() { return InteractionPauseTimer; }

        public override string ToString()
        {
            return $"HoverInitiallyEnabled: {HoverInitiallyEnabled}, Chase: {Chase}, Random: {Random}, InteractionPauseTimer: {InteractionPauseTimer}";
        }
    }

    public class CreatureModelInfo
    {
        public float BoundingRadius;
        public float CombatReach;
        public sbyte gender;
        public uint DisplayIdOtherGender;
        public bool IsTrigger;
    }

    public class CreatureModel
    {
        public static CreatureModel DefaultInvisibleModel = new(11686, 1.0f, 1.0f);
        public static CreatureModel DefaultVisibleModel = new(17519, 1.0f, 1.0f);

        public uint CreatureDisplayID;
        public float DisplayScale;
        public float Probability;

        public CreatureModel() { }
        public CreatureModel(uint creatureDisplayID, float displayScale, float probability)
        {
            CreatureDisplayID = creatureDisplayID;
            DisplayScale = displayScale;
            Probability = probability;
        }
    }

    public class CreatureSummonedData
    {
        public uint? CreatureIDVisibleToSummoner;
        public uint? GroundMountDisplayID;
        public uint? FlyingMountDisplayID;
        public List<uint> DespawnOnQuestsRemoved;
    }

    public class CreatureAddon
    {
        public uint PathId;
        public uint mount;
        public byte standState;
        public byte animTier;
        public byte sheathState;
        public byte pvpFlags;
        public byte visFlags;
        public uint emote;
        public ushort aiAnimKit;
        public ushort movementAnimKit;
        public ushort meleeAnimKit;
        public List<uint> auras = new();
        public VisibilityDistanceType visibilityDistanceType;
    }

    public class VendorItem
    {
        public VendorItem() { }
        public VendorItem(uint _item, int _maxcount, uint _incrtime, uint _ExtendedCost, ItemVendorType _Type)
        {
            item = _item;
            maxcount = (uint)_maxcount;
            incrtime = _incrtime;
            ExtendedCost = _ExtendedCost;
            Type = _Type;
        }

        public uint item;
        public uint maxcount;                                        // 0 for infinity item amount
        public uint incrtime;                                        // time for restore items amount if maxcount != 0
        public uint ExtendedCost;
        public ItemVendorType Type;
        public List<uint> BonusListIDs = new();
        public uint PlayerConditionId;
        public bool IgnoreFiltering;
    }

    public class VendorItemData
    {
        List<VendorItem> m_items = new();

        public VendorItem GetItem(uint slot)
        {
            if (slot >= m_items.Count)
                return null;

            return m_items[(int)slot];
        }
        public bool Empty()
        {
            return m_items.Count == 0;
        }
        public int GetItemCount()
        {
            return m_items.Count;
        }
        public void AddItem(VendorItem vItem)
        {
            m_items.Add(vItem);
        }
        public bool RemoveItem(uint item_id, ItemVendorType type)
        {
            int i = m_items.RemoveAll(p => p.item == item_id && p.Type == type);
            if (i == 0)
                return false;
            else
                return true;
        }
        public VendorItem FindItemCostPair(uint item_id, uint extendedCost, ItemVendorType type)
        {
            return m_items.Find(p => p.item == item_id && p.ExtendedCost == extendedCost && p.Type == type);
        }
        public void Clear()
        {
            m_items.Clear();
        }
    }

    public class CreatureDifficulty
    {
        public short DeltaLevelMin;
        public short DeltaLevelMax;
        public uint ContentTuningID;
        public int HealthScalingExpansion;
        public float HealthModifier;
        public float ManaModifier;
        public float ArmorModifier;
        public float DamageModifier;
        public int CreatureDifficultyID;
        public CreatureTypeFlags TypeFlags;
        public uint TypeFlags2;
        public uint LootID;
        public uint PickPocketLootID;
        public uint SkinLootID;
        public uint GoldMin;
        public uint GoldMax;
        public CreatureStaticFlagsHolder StaticFlags;

        public CreatureDifficulty()
        {
            HealthModifier = 1.0f;
            ManaModifier = 1.0f;
            ArmorModifier = 1.0f;
            DamageModifier = 1.0f;
        }

        // Helpers
        public int GetHealthScalingExpansion()
        {
            return HealthScalingExpansion == (int)Expansion.LevelCurrent ? (int)PlayerConst.CurrentExpansion : HealthScalingExpansion;
        }

        public SkillType GetRequiredLootSkill()
        {
            if (TypeFlags.HasFlag(CreatureTypeFlags.SkinWithHerbalism))
                return SkillType.Herbalism;
            else if (TypeFlags.HasFlag(CreatureTypeFlags.SkinWithMining))
                return SkillType.Mining;
            else if (TypeFlags.HasFlag(CreatureTypeFlags.SkinWithEngineering))
                return SkillType.Engineering;
            else
                return SkillType.Skinning; // Default case
        }
    }
}
