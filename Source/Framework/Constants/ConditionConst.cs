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
 */﻿

namespace Framework.Constants
{
    public enum ConditionTypes
    {                                // value1           value2         value3
        None = 0,                    // 0                0              0                  Always True
        Aura = 1,                    // Spell_Id         Effindex       Use Target?        True If Player (Or Target, If Value3) Has Aura Of Spell_Id With Effect Effindex
        Item = 2,                    // Item_Id          Count          Bank               True If Has #Count Of Item_Ids (If 'Bank' Is Set It Searches In Bank Slots Too)
        ItemEquipped = 3,            // Item_Id          0              0                  True If Has Item_Id Equipped
        Zoneid = 4,                  // Zone_Id          0              0                  True If In Zone_Id
        ReputationRank = 5,          // Faction_Id       Rankmask       0                  True If Has Min_Rank For Faction_Id
        Team = 6,                    // Player_Team      0,             0                  469 - Alliance, 67 - Horde)
        Skill = 7,                   // Skill_Id         Skill_Value    0                  True If Has Skill_Value For Skill_Id
        QuestRewarded = 8,           // Quest_Id         0              0                  True If Quest_Id Was Rewarded Before
        QuestTaken = 9,              // Quest_Id         0,             0                  True While Quest Active
        DrunkenState = 10,           // Drunkenstate     0,             0                  True If Player Is Drunk Enough
        WorldState = 11,             // Index            Value          0                  True If World Has The Value For The Index
        ActiveEvent = 12,            // Event_Id         0              0                  True If Event Is Active
        InstanceInfo = 13,           // Entry            Data           Type               True If The Instance Info Defined By Type (Enum Instanceinfo) Equals Data.
        QuestNone = 14,              // Quest_Id         0              0                  True If Doesn'T Have Quest Saved
        Class = 15,                  // Class            0              0                  True If Player'S Class Is Equal To Class
        Race = 16,                   // Race             0              0                  True If Player'S Race Is Equal To Race
        Achievement = 17,            // Achievement_Id   0              0                  True If Achievement Is Complete
        Title = 18,                  // Title Id         0              0                  True If Player Has Title
        SpawnmaskDeprecated = 19,    // Deprecated
        Gender = 20,                 // Gender           0              0                  True If Player'S Gender Is Equal To Gender
        UnitState = 21,              // Unitstate        0              0                  True If Unit Has Unitstate
        Mapid = 22,                  // Map_Id           0              0                  True If In Map_Id
        Areaid = 23,                 // Area_Id          0              0                  True If In Area_Id
        CreatureType = 24,           // cinfo.type       0              0                  true if creature_template.type = value1
        Spell = 25,                  // Spell_Id         0              0                  True If Player Has Learned Spell
        PhaseId = 26,                // Phasemask        0              0                  True If Object Is In PhaseId
        Level = 27,                  // Level            Comparisontype 0                  True If Unit'S Level Is Equal To Param1 (Param2 Can Modify The Statement)
        QuestComplete = 28,          // Quest_Id         0              0                  True If Player Has Quest_Id With All Objectives Complete, But Not Yet Rewarded
        NearCreature = 29,           // Creature Entry   Distance       0                  True If There Is A Creature Of Entry In Range
        NearGameobject = 30,         // Gameobject Entry Distance       0                  True If There Is A Gameobject Of Entry In Range
        ObjectEntryGuid = 31,            // Typeid           Entry          0                  True If Object Is Type Typeid And The Entry Is 0 Or Matches Entry Of The Object
        TypeMask = 32,               // Typemask         0              0                  True If Object Is Type Object'S Typemask Matches Provided Typemask
        RelationTo = 33,             // Conditiontarget  Relationtype   0                  True If Object Is In Given Relation With Object Specified By Conditiontarget
        ReactionTo = 34,             // Conditiontarget  Rankmask       0                  True If Object'S Reaction Matches Rankmask Object Specified By Conditiontarget
        DistanceTo = 35,             // Conditiontarget  Distance       Comparisontype     True If Object And Conditiontarget Are Within Distance Given By Parameters
        Alive = 36,                  // 0                0              0                  True If Unit Is Alive
        HpVal = 37,                  // Hpval            Comparisontype 0                  True If Unit'S Hp Matches Given Value
        HpPct = 38,                  // Hppct            Comparisontype 0                  True If Unit'S Hp Matches Given Pct
        RealmAchievement = 39,       // achievement_id   0              0                  true if realm achievement is complete
        InWater = 40,                // 0                0              0                  true if unit in water
        TerrainSwap = 41,            // terrainSwap      0              0                  true if object is in terrainswap
        StandState = 42,             // stateType        state          0                  true if unit matches specified sitstate (0,x: has exactly state x; 1,0: any standing state; 1,1: any sitting state;)
        DailyQuestDone = 43,         // quest id         0              0                  true if daily quest has been completed for the day
        Charmed = 44,                // 0                0              0                  true if unit is currently charmed
        PetType = 45,                // mask             0              0                  true if player has a pet of given type(s)
        Taxi = 46,                   // 0                0              0                  true if player is on taxi
        Queststate = 47,             // quest_id         state_mask     0                  true if player is in any of the provided quest states for the quest (1 = not taken, 2 = completed, 8 = in progress, 32 = failed, 64 = rewarded)
        ObjectiveComplete = 48,      // ID               0              0                  true if player has ID objective complete, but quest not yet rewarded
        DifficultyId = 49,           // Difficulty       0              0                  true is map has difficulty id
        Max
    }

    public enum ConditionSourceType
    {
        None = 0,
        CreatureLootTemplate = 1,
        DisenchantLootTemplate = 2,
        FishingLootTemplate = 3,
        GameobjectLootTemplate = 4,
        ItemLootTemplate = 5,
        MailLootTemplate = 6,
        MillingLootTemplate = 7,
        PickpocketingLootTemplate = 8,
        ProspectingLootTemplate = 9,
        ReferenceLootTemplate = 10,
        SkinningLootTemplate = 11,
        SpellLootTemplate = 12,
        SpellImplicitTarget = 13,
        GossipMenu = 14,
        GossipMenuOption = 15,
        CreatureTemplateVehicle = 16,
        Spell = 17,
        SpellClickEvent = 18,
        QuestAvailable = 19,
        // Condition source type 20 unused
        VehicleSpell = 21,
        SmartEvent = 22,
        NpcVendor = 23,
        SpellProc = 24,
        TerrainSwap = 25,
        Phase = 26,
        Graveyard = 27,
        Max = 28
    }

    public enum RelationType
    {
        Self = 0,
        InParty,
        InRaidOrParty,
        OwnedBy,
        PassengerOf,
        CreatedBy,
        Max
    }

    public enum InstanceInfo
    {
        Data = 0,
        Data64,
        BossState
    }
}
