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

using Framework.Collections;
using Framework.Constants;
using System;
using System.Collections.Generic;
using Framework.Dynamic;

namespace Game.Entities
{
    public class CreatureTemplate
    {
        public uint Entry;
        public uint[] DifficultyEntry = new uint[SharedConst.MaxCreatureDifficulties];
        public uint[] KillCredit = new uint[SharedConst.MaxCreatureKillCredit];
        public List<CreatureModel> Models = new List<CreatureModel>();
        public string Name;
        public string FemaleName;
        public string SubName;
        public string TitleAlt;
        public string IconName;
        public uint GossipMenuId;
        public short Minlevel;
        public Optional<CreatureLevelScaling> levelScaling;
        public short Maxlevel;
        public int HealthScalingExpansion;
        public uint RequiredExpansion;
        public uint VignetteID; // @todo Read Vignette.db2
        public uint Faction;
        public NPCFlags Npcflag;
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
        public InhabitType InhabitType;
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
        public bool RegenHealth;
        public uint MechanicImmuneMask;
        public CreatureFlagsExtra FlagsExtra;
        public uint ScriptID;

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

        public SkillType GetRequiredLootSkill()
        {
            if (TypeFlags.HasAnyFlag(CreatureTypeFlags.HerbSkinningSkill))
                return SkillType.Herbalism;
            else if (TypeFlags.HasAnyFlag(CreatureTypeFlags.MiningSkinningSkill))
                return SkillType.Mining;
            else if (TypeFlags.HasAnyFlag(CreatureTypeFlags.EngineeringSkinningSkill))
                return SkillType.Engineering;
            else
                return SkillType.Skinning;                          // normal case
        }

        public bool IsExotic()
        {
            return (TypeFlags & CreatureTypeFlags.ExoticPet) != 0;
        }
        public bool IsTameable(bool canTameExotic)
        {
            if (CreatureType != CreatureType.Beast || Family == CreatureFamily.None || !TypeFlags.HasAnyFlag(CreatureTypeFlags.TameablePet))
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
    }

    public class CreatureBaseStats
    {
        public uint[] BaseHealth = new uint[(int)Expansion.Max];
        public uint BaseMana;
        public uint BaseArmor;
        public uint AttackPower;
        public uint RangedAttackPower;
        public float[] BaseDamage = new float[(int)Expansion.Max];

        // Helpers
        public uint GenerateHealth(CreatureTemplate info)
        {
            return (uint)Math.Ceiling(BaseHealth[info.HealthScalingExpansion] * info.ModHealth * info.ModHealthExtra);
        }

        public uint GenerateMana(CreatureTemplate info)
        {
            // Mana can be 0.
            if (BaseMana == 0)
                return 0;

            return (uint)Math.Ceiling(BaseMana * info.ModMana * info.ModManaExtra);
        }

        public float GenerateArmor(CreatureTemplate info)
        {
            return (float)Math.Ceiling(BaseArmor * info.ModArmor);
        }

        public float GenerateBaseDamage(CreatureTemplate info)
        {
            return BaseDamage[info.HealthScalingExpansion];
        }
    }

    public class CreatureLocale
    {
        public StringArray Name = new StringArray((int)LocaleConstant.Total);
        public StringArray NameAlt = new StringArray((int)LocaleConstant.Total);
        public StringArray Title = new StringArray((int)LocaleConstant.Total);
        public StringArray TitleAlt = new StringArray((int)LocaleConstant.Total);
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

    public class CreatureData
    {
        public uint id;                                           // entry in creature_template
        public ushort mapid;
        public uint displayid;
        public int equipmentId;
        public float posX;
        public float posY;
        public float posZ;
        public float orientation;
        public uint spawntimesecs;
        public float spawndist;
        public uint currentwaypoint;
        public uint curhealth;
        public uint curmana;
        public byte movementType;
        public List<Difficulty> spawnDifficulties = new List<Difficulty>();
        public ulong npcflag;
        public uint unit_flags;     // enum UnitFlags mask values
        public uint unit_flags2;    // enum UnitFlags2 mask values
        public uint unit_flags3;    // enum UnitFlags3 mask values
        public uint dynamicflags;
        public PhaseUseFlagsValues phaseUseFlags;
        public uint phaseId;
        public uint phaseGroup;
        public int terrainSwapMap;
        public uint ScriptId;
        public bool dbData;
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
        public static CreatureModel DefaultInvisibleModel = new CreatureModel(11686, 1.0f, 1.0f);
        public static CreatureModel DefaultVisibleModel = new CreatureModel(17519, 1.0f, 1.0f);

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
        public uint[] auras;
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
        public List<uint> BonusListIDs = new List<uint>();
        public uint PlayerConditionId;
        public bool IgnoreFiltering;

        //helpers
        public bool IsGoldRequired(ItemTemplate pProto) { return Convert.ToBoolean(pProto.GetFlags2() & ItemFlags2.DontIgnoreBuyPrice) || ExtendedCost == 0; }
    }

    public class VendorItemData
    {
        List<VendorItem> m_items = new List<VendorItem>();

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

    public struct CreatureLevelScaling
    {
        public ushort MinLevel;
        public ushort MaxLevel;
        public short DeltaLevelMin;
        public short DeltaLevelMax;
    }
}
