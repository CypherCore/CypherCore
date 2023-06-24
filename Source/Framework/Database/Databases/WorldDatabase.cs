// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.
 
namespace Framework.Database
{
    public class WorldDatabase : MySqlBase<WorldStatements>
    {
        public override void PreparedStatements()
        {
            PrepareStatement(WorldStatements.DEL_LINKED_RESPAWN, "DELETE FROM linked_respawn WHERE guid = ? AND linkType  = ?");
            PrepareStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER, "DELETE FROM linked_respawn WHERE linkedGuid = ? AND linkType = ?");
            PrepareStatement(WorldStatements.REP_LINKED_RESPAWN, "REPLACE INTO linked_respawn (guid, linkedGuid, linkType) VALUES (?, ?, ?)");
            PrepareStatement(WorldStatements.SEL_CREATURE_TEXT, "SELECT CreatureID, GroupID, ID, Text, Type, Language, Probability, Emote, Duration, Sound, SoundPlayType, BroadcastTextId, TextRange FROM creature_text");
            PrepareStatement(WorldStatements.SEL_SMART_SCRIPTS, "SELECT entryorguid, source_type, id, link, event_type, event_phase_mask, event_chance, event_flags, event_param1, event_param2, event_param3, event_param4, event_param5, event_param_string, action_type, action_param1, action_param2, action_param3, action_param4, action_param5, action_param6, action_param7, target_type, target_param1, target_param2, target_param3, target_param4, target_x, target_y, target_z, target_o FROM smart_scripts ORDER BY entryorguid, source_type, id, link");
            PrepareStatement(WorldStatements.DEL_GAMEOBJECT, "DELETE FROM gameobject WHERE guid = ?");
            PrepareStatement(WorldStatements.DEL_EVENT_GAMEOBJECT, "DELETE FROM game_event_gameobject WHERE guid = ?");
            PrepareStatement(WorldStatements.INS_GRAVEYARD_ZONE, "INSERT INTO graveyard_zone (ID, GhostZone) VALUES (?, ?)");
            PrepareStatement(WorldStatements.DEL_GRAVEYARD_ZONE, "DELETE FROM graveyard_zone WHERE ID = ? AND GhostZone = ?");
            PrepareStatement(WorldStatements.INS_GAME_TELE, "INSERT INTO game_tele (id, position_x, position_y, position_z, orientation, map, name) VALUES (?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(WorldStatements.DEL_GAME_TELE, "DELETE FROM game_tele WHERE name = ?");
            PrepareStatement(WorldStatements.INS_NPC_VENDOR, "INSERT INTO npc_vendor (entry, item, maxcount, incrtime, extendedcost, type) VALUES(?, ?, ?, ?, ?, ?)");
            PrepareStatement(WorldStatements.DEL_NPC_VENDOR, "DELETE FROM npc_vendor WHERE entry = ? AND item = ? AND type = ?");
            PrepareStatement(WorldStatements.SEL_NPC_VENDOR_REF, "SELECT item, maxcount, incrtime, ExtendedCost, type, BonusListIDs, PlayerConditionID, IgnoreFiltering FROM npc_vendor WHERE entry = ? ORDER BY slot ASC");
            PrepareStatement(WorldStatements.UPD_CREATURE_MOVEMENT_TYPE, "UPDATE creature SET MovementType = ? WHERE guid = ?");
            PrepareStatement(WorldStatements.UPD_CREATURE_FACTION, "UPDATE creature_template SET faction = ? WHERE entry = ?");
            PrepareStatement(WorldStatements.UPD_CREATURE_NPCFLAG, "UPDATE creature_template SET npcflag = ? WHERE entry = ?");
            PrepareStatement(WorldStatements.UPD_CREATURE_POSITION, "UPDATE creature SET position_x = ?, position_y = ?, position_z = ?, orientation = ? WHERE guid = ?");
            PrepareStatement(WorldStatements.UPD_CREATURE_WANDER_DISTANCE, "UPDATE creature SET wander_distance = ?, MovementType = ? WHERE guid = ?");
            PrepareStatement(WorldStatements.UPD_CREATURE_SPAWN_TIME_SECS, "UPDATE creature SET spawntimesecs = ? WHERE guid = ?");
            PrepareStatement(WorldStatements.INS_CREATURE_FORMATION, "INSERT INTO creature_formations (leaderGUID, memberGUID, dist, angle, groupAI) VALUES (?, ?, ?, ?, ?)");
            PrepareStatement(WorldStatements.INS_WAYPOINT_DATA, "INSERT INTO waypoint_data (id, point, position_x, position_y, position_z, orientation) VALUES (?, ?, ?, ?, ?, ?)");
            PrepareStatement(WorldStatements.DEL_WAYPOINT_DATA, "DELETE FROM waypoint_data WHERE id = ? AND point = ?");
            PrepareStatement(WorldStatements.UPD_WAYPOINT_DATA_POINT, "UPDATE waypoint_data SET point = point - 1 WHERE id = ? AND point > ?");
            PrepareStatement(WorldStatements.UPD_WAYPOINT_DATA_POSITION, "UPDATE waypoint_data SET position_x = ?, position_y = ?, position_z = ?, orientation = ? where id = ? AND point = ?");
            PrepareStatement(WorldStatements.UPD_WAYPOINT_DATA_WPGUID, "UPDATE waypoint_data SET wpguid = ? WHERE id = ? and point = ?");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_DATA_MAX_ID, "SELECT MAX(id) FROM waypoint_data");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_DATA_MAX_POINT, "SELECT MAX(point) FROM waypoint_data WHERE id = ?");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_DATA_BY_ID, "SELECT point, position_x, position_y, position_z, orientation, move_type, delay, action, action_chance FROM waypoint_data WHERE id = ? ORDER BY point");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_DATA_POS_BY_ID, "SELECT point, position_x, position_y, position_z, orientation FROM waypoint_data WHERE id = ?");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_DATA_POS_FIRST_BY_ID, "SELECT position_x, position_y, position_z, orientation FROM waypoint_data WHERE point = 1 AND id = ?");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_DATA_POS_LAST_BY_ID, "SELECT position_x, position_y, position_z, orientation FROM waypoint_data WHERE id = ? ORDER BY point DESC LIMIT 1");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_DATA_BY_WPGUID, "SELECT id, point FROM waypoint_data WHERE wpguid = ?");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_DATA_ALL_BY_WPGUID, "SELECT id, point, delay, move_type, action, action_chance FROM waypoint_data WHERE wpguid = ?");
            PrepareStatement(WorldStatements.UPD_WAYPOINT_DATA_ALL_WPGUID, "UPDATE waypoint_data SET wpguid = 0");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_DATA_BY_POS, "SELECT id, point FROM waypoint_data WHERE (abs(position_x - ?) <= ?) and (abs(position_y - ?) <= ?) and (abs(position_z - ?) <= ?)");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_DATA_WPGUID_BY_ID, "SELECT wpguid FROM waypoint_data WHERE id = ? and wpguid <> 0");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_DATA_ACTION, "SELECT DISTINCT action FROM waypoint_data");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_SCRIPTS_MAX_ID, "SELECT MAX(guid) FROM waypoint_scripts");
            PrepareStatement(WorldStatements.INS_CREATURE_ADDON, "INSERT INTO creature_addon(guid, path_id) VALUES (?, ?)");
            PrepareStatement(WorldStatements.UPD_CREATURE_ADDON_PATH, "UPDATE creature_addon SET path_id = ? WHERE guid = ?");
            PrepareStatement(WorldStatements.DEL_CREATURE_ADDON, "DELETE FROM creature_addon WHERE guid = ?");
            PrepareStatement(WorldStatements.SEL_CREATURE_ADDON_BY_GUID, "SELECT guid FROM creature_addon WHERE guid = ?");
            PrepareStatement(WorldStatements.INS_WAYPOINT_SCRIPT, "INSERT INTO waypoint_scripts (guid) VALUES (?)");
            PrepareStatement(WorldStatements.DEL_WAYPOINT_SCRIPT, "DELETE FROM waypoint_scripts WHERE guid = ?");
            PrepareStatement(WorldStatements.UPD_WAYPOINT_SCRIPT_ID, "UPDATE waypoint_scripts SET id = ? WHERE guid = ?");
            PrepareStatement(WorldStatements.UPD_WAYPOINT_SCRIPT_X, "UPDATE waypoint_scripts SET x = ? WHERE guid = ?");
            PrepareStatement(WorldStatements.UPD_WAYPOINT_SCRIPT_Y, "UPDATE waypoint_scripts SET y = ? WHERE guid = ?");
            PrepareStatement(WorldStatements.UPD_WAYPOINT_SCRIPT_Z, "UPDATE waypoint_scripts SET z = ? WHERE guid = ?");
            PrepareStatement(WorldStatements.UPD_WAYPOINT_SCRIPT_O, "UPDATE waypoint_scripts SET o = ? WHERE guid = ?");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_SCRIPT_ID_BY_GUID, "SELECT id FROM waypoint_scripts WHERE guid = ?");
            PrepareStatement(WorldStatements.DEL_CREATURE, "DELETE FROM creature WHERE guid = ?");
            PrepareStatement(WorldStatements.SEL_COMMANDS, "SELECT name, help FROM command");
            PrepareStatement(WorldStatements.SEL_CREATURE_TEMPLATE, "SELECT entry, KillCredit1, KillCredit2, name, femaleName, subname, TitleAlt, IconName, RequiredExpansion, VignetteID, faction, npcflag, speed_walk, speed_run, scale, `rank`, dmgschool, BaseAttackTime, RangeAttackTime, BaseVariance, RangeVariance, unit_class, unit_flags, unit_flags2, unit_flags3, family, trainer_class, type, VehicleId, AIName, MovementType, ctm.Ground, ctm.Swim, ctm.Flight, ctm.Rooted, ctm.Chase, ctm.Random, ctm.InteractionPauseTimer, ExperienceModifier, RacialLeader, movementId, WidgetSetID, WidgetSetUnitConditionID, RegenHealth, mechanic_immune_mask, spell_school_immune_mask, flags_extra, ScriptName, StringId FROM creature_template ct LEFT JOIN creature_template_movement ctm ON ct.entry = ctm.CreatureId WHERE entry = ? OR 1 = ?");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_SCRIPT_BY_ID, "SELECT guid, delay, command, datalong, datalong2, dataint, x, y, z, o FROM waypoint_scripts WHERE id = ?");
            PrepareStatement(WorldStatements.SEL_CREATURE_BY_ID, "SELECT guid FROM creature WHERE id = ?");
            PrepareStatement(WorldStatements.SEL_GAMEOBJECT_NEAREST, "SELECT guid, id, position_x, position_y, position_z, map, (POW(position_x - ?, 2) + POW(position_y - ?, 2) + POW(position_z - ?, 2)) AS order_ FROM gameobject WHERE map = ? AND (POW(position_x - ?, 2) + POW(position_y - ?, 2) + POW(position_z - ?, 2)) <= ? ORDER BY order_");
            PrepareStatement(WorldStatements.SEL_CREATURE_NEAREST, "SELECT guid, id, position_x, position_y, position_z, map, (POW(position_x - ?, 2) + POW(position_y - ?, 2) + POW(position_z - ?, 2)) AS order_ FROM creature WHERE map = ? AND (POW(position_x - ?, 2) + POW(position_y - ?, 2) + POW(position_z - ?, 2)) <= ? ORDER BY order_");
            PrepareStatement(WorldStatements.INS_CREATURE, "INSERT INTO creature (guid, id , map, spawnDifficulties, PhaseId, PhaseGroup, modelid, equipment_id, position_x, position_y, position_z, orientation, spawntimesecs, wander_distance, currentwaypoint, curhealth, curmana, MovementType, npcflag, unit_flags, unit_flags2, unit_flags3) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(WorldStatements.DEL_GAME_EVENT_CREATURE, "DELETE FROM game_event_creature WHERE guid = ?");
            PrepareStatement(WorldStatements.DEL_GAME_EVENT_MODEL_EQUIP, "DELETE FROM game_event_model_equip WHERE guid = ?");
            PrepareStatement(WorldStatements.INS_GAMEOBJECT, "INSERT INTO gameobject (guid, id, map, spawnDifficulties, PhaseId, PhaseGroup, position_x, position_y, position_z, orientation, rotation0, rotation1, rotation2, rotation3, spawntimesecs, animprogress, state) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(WorldStatements.INS_DISABLES, "INSERT INTO disables (entry, sourceType, flags, comment) VALUES (?, ?, ?, ?)");
            PrepareStatement(WorldStatements.SEL_DISABLES, "SELECT entry FROM disables WHERE entry = ? AND sourceType = ?");
            PrepareStatement(WorldStatements.DEL_DISABLES, "DELETE FROM disables WHERE entry = ? AND sourceType = ?");
            PrepareStatement(WorldStatements.UPD_CREATURE_ZONE_AREA_DATA, "UPDATE creature SET zoneId = ?, areaId = ? WHERE guid = ?");
            PrepareStatement(WorldStatements.UPD_GAMEOBJECT_ZONE_AREA_DATA, "UPDATE gameobject SET zoneId = ?, areaId = ? WHERE guid = ?");
            PrepareStatement(WorldStatements.DEL_SPAWNGROUP_MEMBER, "DELETE FROM spawn_group WHERE spawnType = ? AND spawnId = ?");
            PrepareStatement(WorldStatements.DEL_GAMEOBJECT_ADDON, "DELETE FROM gameobject_addon WHERE guid = ?");
            PrepareStatement(WorldStatements.SEL_GUILD_REWARDS_REQ_ACHIEVEMENTS, "SELECT AchievementRequired FROM guild_rewards_req_achievements WHERE ItemID = ?");
            PrepareStatement(WorldStatements.INS_CONDITION, "INSERT INTO conditions (SourceTypeOrReferenceId, SourceGroup, SourceEntry, SourceId, ElseGroup, ConditionTypeOrReference, ConditionTarget, ConditionValue1, ConditionValue2, ConditionValue3, NegativeCondition, ErrorType, ErrorTextId, ScriptName, Comment) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
        }
    }

    public enum WorldStatements
    {
        DEL_LINKED_RESPAWN,
        DEL_LINKED_RESPAWN_MASTER,
        REP_LINKED_RESPAWN,
        SEL_CREATURE_TEXT,
        SEL_SMART_SCRIPTS,
        DEL_GAMEOBJECT,
        DEL_EVENT_GAMEOBJECT,
        INS_GRAVEYARD_ZONE,
        DEL_GRAVEYARD_ZONE,
        INS_GAME_TELE,
        DEL_GAME_TELE,
        INS_NPC_VENDOR,
        DEL_NPC_VENDOR,
        SEL_NPC_VENDOR_REF,
        UPD_CREATURE_MOVEMENT_TYPE,
        UPD_CREATURE_FACTION,
        UPD_CREATURE_NPCFLAG,
        UPD_CREATURE_POSITION,
        UPD_CREATURE_WANDER_DISTANCE,
        UPD_CREATURE_SPAWN_TIME_SECS,
        INS_CREATURE_FORMATION,
        INS_WAYPOINT_DATA,
        DEL_WAYPOINT_DATA,
        UPD_WAYPOINT_DATA_POINT,
        UPD_WAYPOINT_DATA_POSITION,
        UPD_WAYPOINT_DATA_WPGUID,
        UPD_WAYPOINT_DATA_ALL_WPGUID,
        SEL_WAYPOINT_DATA_MAX_ID,
        SEL_WAYPOINT_DATA_BY_ID,
        SEL_WAYPOINT_DATA_POS_BY_ID,
        SEL_WAYPOINT_DATA_POS_FIRST_BY_ID,
        SEL_WAYPOINT_DATA_POS_LAST_BY_ID,
        SEL_WAYPOINT_DATA_BY_WPGUID,
        SEL_WAYPOINT_DATA_ALL_BY_WPGUID,
        SEL_WAYPOINT_DATA_MAX_POINT,
        SEL_WAYPOINT_DATA_BY_POS,
        SEL_WAYPOINT_DATA_WPGUID_BY_ID,
        SEL_WAYPOINT_DATA_ACTION,
        SEL_WAYPOINT_SCRIPTS_MAX_ID,
        UPD_CREATURE_ADDON_PATH,
        INS_CREATURE_ADDON,
        DEL_CREATURE_ADDON,
        SEL_CREATURE_ADDON_BY_GUID,
        INS_WAYPOINT_SCRIPT,
        DEL_WAYPOINT_SCRIPT,
        UPD_WAYPOINT_SCRIPT_ID,
        UPD_WAYPOINT_SCRIPT_X,
        UPD_WAYPOINT_SCRIPT_Y,
        UPD_WAYPOINT_SCRIPT_Z,
        UPD_WAYPOINT_SCRIPT_O,
        SEL_WAYPOINT_SCRIPT_ID_BY_GUID,
        DEL_CREATURE,
        SEL_COMMANDS,
        SEL_CREATURE_TEMPLATE,
        SEL_WAYPOINT_SCRIPT_BY_ID,
        SEL_CREATURE_BY_ID,
        SEL_GAMEOBJECT_NEAREST,
        SEL_CREATURE_NEAREST,
        SEL_GAMEOBJECT_TARGET,
        INS_CREATURE,
        DEL_GAME_EVENT_CREATURE,
        DEL_GAME_EVENT_MODEL_EQUIP,
        INS_GAMEOBJECT,
        SEL_DISABLES,
        INS_DISABLES,
        DEL_DISABLES,
        UPD_CREATURE_ZONE_AREA_DATA,
        UPD_GAMEOBJECT_ZONE_AREA_DATA,
        DEL_SPAWNGROUP_MEMBER,
        DEL_GAMEOBJECT_ADDON,
        SEL_GUILD_REWARDS_REQ_ACHIEVEMENTS,
        INS_CONDITION,

        MAX_WORLDDATABASE_STATEMENTS
    }
}
