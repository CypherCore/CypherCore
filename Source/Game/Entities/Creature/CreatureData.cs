// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;
using System;
using System.Collections.Generic;
using Game.Networking.Packets;
using Game.Maps;

namespace Game.Entities
{
    public class CreatureTemplate
    {
        public uint Entry;
        public uint[] DifficultyEntry = new uint[SharedConst.MaxCreatureDifficulties];
        public uint[] KillCredit = new uint[SharedConst.MaxCreatureKillCredit];
        public List<CreatureModel> Models = new();
        public string Name;
        public string FemaleName;
        public string SubName;
        public string TitleAlt;
        public string IconName;
        public uint GossipMenuId;
        public short Minlevel;
        public Dictionary<Difficulty, CreatureLevelScaling> scalingStorage = new();
        public short Maxlevel;
        public int HealthScalingExpansion;
        public uint RequiredExpansion;
        public uint VignetteID; // @todo Read Vignette.db2
        public uint Faction;
        public ulong Npcflag;
        public float SpeedWalk;
        public float SpeedRun;
        public float Scale;
        public CreatureEliteType Rank;
        public uint DmgSchool;
        public uint BaseAttackTime;
        public uint RangeAttackTime;
        public float BaseVariance;
        public float RangeVariance;
        public uint UnitClass;
        public UnitFlags UnitFlags;
        public uint UnitFlags2;
        public uint UnitFlags3;
        public uint DynamicFlags;
        public CreatureFamily Family;
        public Class TrainerClass;
        public CreatureType CreatureType;
        public CreatureTypeFlags TypeFlags;
        public uint TypeFlags2;
        public uint LootId;
        public uint PickPocketId;
        public uint SkinLootId;
        public int[] Resistance = new int[7];
        public uint[] Spells = new uint[8];
        public uint VehicleId;
        public uint MinGold;
        public uint MaxGold;
        public string AIName;
        public uint MovementType;
        public CreatureMovementData Movement = new();
        public float HoverHeight;
        public float ModHealth;
        public float ModHealthExtra;
        public float ModMana;
        public float ModManaExtra;
        public float ModArmor;
        public float ModDamage;
        public float ModExperience;
        public bool RacialLeader;
        public uint MovementId;
        public int CreatureDifficultyID;
        public int WidgetSetID;
        public int WidgetSetUnitConditionID;
        public bool RegenHealth;
        public ulong MechanicImmuneMask;
        public uint SpellSchoolImmuneMask;
        public CreatureFlagsExtra FlagsExtra;
        public uint ScriptID;
        public string StringId;

        public QueryCreatureResponse QueryData;

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

        public int[] GetMinMaxLevel()
        {
            return new[]
            {
                HealthScalingExpansion != (int)Expansion.LevelCurrent ? Minlevel : Minlevel + SharedConst.MaxLevel,
                HealthScalingExpansion != (int)Expansion.LevelCurrent ? Maxlevel : Maxlevel + SharedConst.MaxLevel
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
                return SkillType.Skinning;                          // normal case
        }

        public bool IsExotic()
        {
            return (TypeFlags & CreatureTypeFlags.TameableExotic) != 0;
        }
        public bool IsTameable(bool canTameExotic)
        {
            if (CreatureType != CreatureType.Beast || Family == CreatureFamily.None || !TypeFlags.HasAnyFlag(CreatureTypeFlags.Tameable))
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
            QueryData.Allow = true;

            CreatureStats stats = new();
            stats.Leader = RacialLeader;

            stats.Name[0] = Name;
            stats.NameAlt[0] = FemaleName;

            stats.Flags[0] = (uint)TypeFlags;
            stats.Flags[1] = TypeFlags2;

            stats.CreatureType = (int)CreatureType;
            stats.CreatureFamily = (int)Family;
            stats.Classification = (int)Rank;

            for (uint i = 0; i < SharedConst.MaxCreatureKillCredit; ++i)
                stats.ProxyCreatureID[i] = KillCredit[i];

            foreach (var model in Models)
            {
                stats.Display.TotalProbability += model.Probability;
                stats.Display.CreatureDisplay.Add(new CreatureXDisplay(model.CreatureDisplayID, model.DisplayScale, model.Probability));
            }

            stats.HpMulti = ModHealth;
            stats.EnergyMulti = ModMana;

            stats.CreatureMovementInfoID = MovementId;
            stats.RequiredExpansion = RequiredExpansion;
            stats.HealthScalingExpansion = HealthScalingExpansion;
            stats.VignetteID = VignetteID;
            stats.Class = (int)UnitClass;
            stats.CreatureDifficultyID = CreatureDifficultyID;
            stats.WidgetSetID = WidgetSetID;
            stats.WidgetSetUnitConditionID = WidgetSetUnitConditionID;

            stats.Title = SubName;
            stats.TitleAlt = TitleAlt;
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
        public uint BaseMana;
        public uint AttackPower;
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
        public uint displayid;
        public sbyte equipmentId;
        public float WanderDistance;
        public uint currentwaypoint;
        public uint curhealth;
        public uint curmana;
        public byte movementType;
        public ulong npcflag;
        public uint unit_flags;                                  // enum UnitFlags mask values
        public uint unit_flags2;                                 // enum UnitFlags2 mask values
        public uint unit_flags3;                                 // enum UnitFlags3 mask values
        public uint dynamicflags;

        public CreatureData() : base(SpawnObjectType.Creature) { }
    }

    public class CreatureMovementData
    {
        public CreatureGroundMovementType Ground;
        public CreatureFlightMovementType Flight;
        public bool Swim;
        public bool Rooted;
        public CreatureChaseMovementType Chase;
        public CreatureRandomMovementType Random;
        public uint InteractionPauseTimer;

        public CreatureMovementData()
        {
            Ground = CreatureGroundMovementType.Run;
            Flight = CreatureFlightMovementType.None;
            Swim = true;
            Rooted = false;
            Chase = CreatureChaseMovementType.Run;
            Random = CreatureRandomMovementType.Walk;
            InteractionPauseTimer = WorldConfig.GetUIntValue(WorldCfg.CreatureStopForPlayer);
        }

        public bool IsGroundAllowed() { return Ground != CreatureGroundMovementType.None; }
        public bool IsSwimAllowed() { return Swim; }
        public bool IsFlightAllowed() { return Flight != CreatureFlightMovementType.None; }
        public bool IsRooted() { return Rooted; }

        public CreatureChaseMovementType GetChase() { return Chase; }
        public CreatureRandomMovementType GetRandom() { return Random; }

        public uint GetInteractionPauseTimer() { return InteractionPauseTimer; }

        public override string ToString()
        {
            return $"Ground: {Ground}, Swim: {Swim}, Flight: {Flight} {(Rooted ? ", Rooted" : "")}, Chase: {Chase}, Random: {Random}, InteractionPauseTimer: {InteractionPauseTimer}";
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
    }

    public class CreatureAddon
    {
        public uint path_id;
        public uint mount;
        public uint bytes1;
        public uint bytes2;
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

    public class CreatureLevelScaling
    {
        public short DeltaLevelMin;
        public short DeltaLevelMax;
        public uint ContentTuningID;
    }
}
