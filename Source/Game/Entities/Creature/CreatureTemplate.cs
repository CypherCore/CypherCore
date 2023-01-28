using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Networking.Packets;

namespace Game.Entities;

public class CreatureTemplate
{
    public CreatureType CreatureType;
    public string AIName { get; set; }
    public uint BaseAttackTime { get; set; }
    public float BaseVariance { get; set; }
    public int CreatureDifficultyID { get; set; }
    public uint[] DifficultyEntry { get; set; } = new uint[SharedConst.MaxCreatureDifficulties];
    public uint DmgSchool { get; set; }
    public uint DynamicFlags { get; set; }
    public uint Entry { get; set; }
    public uint Faction { get; set; }
    public CreatureFamily Family { get; set; }
    public string FemaleName { get; set; }
    public CreatureFlagsExtra FlagsExtra { get; set; }
    public uint GossipMenuId { get; set; }
    public int HealthScalingExpansion { get; set; }
    public float HoverHeight { get; set; }
    public string IconName { get; set; }
    public uint[] KillCredit { get; set; } = new uint[SharedConst.MaxCreatureKillCredit];
    public uint LootId { get; set; }
    public uint MaxGold { get; set; }
    public short Maxlevel { get; set; }
    public ulong MechanicImmuneMask { get; set; }
    public uint MinGold { get; set; }
    public short Minlevel { get; set; }
    public float ModArmor { get; set; }
    public float ModDamage { get; set; }
    public List<CreatureModel> Models { get; set; } = new();
    public float ModExperience { get; set; }
    public float ModHealth { get; set; }
    public float ModHealthExtra { get; set; }
    public float ModMana { get; set; }
    public float ModManaExtra { get; set; }
    public CreatureMovementData Movement { get; set; } = new();
    public uint MovementId { get; set; }
    public uint MovementType { get; set; }
    public string Name { get; set; }
    public ulong Npcflag { get; set; }
    public uint PickPocketId { get; set; }

    public QueryCreatureResponse QueryData { get; set; }
    public bool RacialLeader { get; set; }
    public uint RangeAttackTime { get; set; }
    public float RangeVariance { get; set; }
    public CreatureEliteType Rank { get; set; }
    public bool RegenHealth { get; set; }
    public uint RequiredExpansion { get; set; }
    public int[] Resistance { get; set; } = new int[7];
    public float Scale { get; set; }
    public Dictionary<Difficulty, CreatureLevelScaling> ScalingStorage { get; set; } = new();
    public uint ScriptID { get; set; }
    public uint SkinLootId { get; set; }
    public float SpeedRun { get; set; }
    public float SpeedWalk { get; set; }
    public uint[] Spells { get; set; } = new uint[8];
    public uint SpellSchoolImmuneMask { get; set; }
    public string StringId { get; set; }
    public string SubName { get; set; }
    public string TitleAlt { get; set; }
    public Class TrainerClass { get; set; }
    public CreatureTypeFlags TypeFlags { get; set; }
    public uint TypeFlags2 { get; set; }
    public uint UnitClass { get; set; }
    public UnitFlags UnitFlags { get; set; }
    public uint UnitFlags2 { get; set; }
    public uint UnitFlags3 { get; set; }
    public uint VehicleId { get; set; }
    public uint VignetteID { get; set; } // @todo Read Vignette.db2
    public int WidgetSetID { get; set; }
    public int WidgetSetUnitConditionID { get; set; }

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
        var creatureLevelScaling = ScalingStorage.LookupByKey(difficulty);

        if (creatureLevelScaling != null)
            return creatureLevelScaling;

        return new CreatureLevelScaling();
    }
}