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
            PrepareStatement(WorldStatements.SEL_SMART_SCRIPTS, "SELECT entryorguid, source_type, id, link, Difficulties, event_type, event_phase_mask, event_chance, event_flags, event_param1, event_param2, event_param3, event_param4, event_param5, event_param_string, action_type, action_param1, action_param2, action_param3, action_param4, action_param5, action_param6, action_param7, action_param_string, target_type, target_param1, target_param2, target_param3, target_param4, target_param_string, target_x, target_y, target_z, target_o FROM smart_scripts ORDER BY entryorguid, source_type, id, link");
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
            PrepareStatement(WorldStatements.SEL_WAYPOINT_PATH, "SELECT PathId, MoveType, Flags, Velocity FROM waypoint_path WHERE PathId = ? OR 1 = ?");
            PrepareStatement(WorldStatements.INS_WAYPOINT_PATH, "INSERT INTO waypoint_path (PathId, MoveType, Flags, Velocity, Comment) VALUES (?, ?, ?, ?, ?)");
            PrepareStatement(WorldStatements.INS_WAYPOINT_PATH_NODE, "INSERT INTO waypoint_path_node (PathId, NodeId, PositionX, PositionY, PositionZ, Orientation) VALUES (?, ?, ?, ?, ?, ?)");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_PATH_NODE, "SELECT PathId, NodeId, PositionX, PositionY, PositionZ, Orientation, Delay FROM waypoint_path_node WHERE PathId = ? OR 1 = ? ORDER BY NodeId");
            PrepareStatement(WorldStatements.DEL_WAYPOINT_PATH_NODE, "DELETE FROM waypoint_path_node WHERE PathId = ? AND NodeId = ?");
            PrepareStatement(WorldStatements.UPD_WAYPOINT_PATH_NODE, "UPDATE waypoint_path_node SET NodeId = NodeId - 1 WHERE PathId = ? AND NodeId > ?");
            PrepareStatement(WorldStatements.UPD_WAYPOINT_PATH_NODE_POSITION, "UPDATE waypoint_path_node SET PositionX = ?, PositionY = ?, PositionZ = ?, Orientation = ? WHERE PathId = ? AND NodeId = ?");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_PATH_NODE_MAX_PATHID, "SELECT MAX(PathId) FROM waypoint_path_node");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_PATH_NODE_MAX_NODEID, "SELECT MAX(NodeId) FROM waypoint_path_node WHERE PathId = ?");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_PATH_NODE_POS_BY_PATHID, "SELECT NodeId, PositionX, PositionY, PositionZ, Orientation FROM waypoint_path_node WHERE PathId = ?");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_PATH_NODE_POS_FIRST_BY_PATHID, "SELECT PositionX, PositionY, PositionZ, Orientation FROM waypoint_path_node WHERE NodeId = 1 AND PathId = ?");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_PATH_NODE_POS_LAST_BY_PATHID, "SELECT PositionX, PositionY, PositionZ, Orientation FROM waypoint_path_node WHERE PathId = ? ORDER BY NodeId DESC LIMIT 1");
            PrepareStatement(WorldStatements.SEL_WAYPOINT_PATH_NODE_BY_POS, "SELECT PathId, NodeId FROM waypoint_path_node WHERE (abs(PositionX - ?) <= ?) and (abs(PositionY - ?) <= ?) and (abs(PositionZ - ?) <= ?)");
            PrepareStatement(WorldStatements.INS_CREATURE_ADDON, "INSERT INTO creature_addon(guid, PathId) VALUES (?, ?)");
            PrepareStatement(WorldStatements.UPD_CREATURE_ADDON_PATH, "UPDATE creature_addon SET PathId = ? WHERE guid = ?");
            PrepareStatement(WorldStatements.DEL_CREATURE_ADDON, "DELETE FROM creature_addon WHERE guid = ?");
            PrepareStatement(WorldStatements.SEL_CREATURE_ADDON_BY_GUID, "SELECT guid FROM creature_addon WHERE guid = ?");
            PrepareStatement(WorldStatements.DEL_CREATURE, "DELETE FROM creature WHERE guid = ?");
            PrepareStatement(WorldStatements.SEL_COMMANDS, "SELECT name, help FROM command");
            PrepareStatement(WorldStatements.SEL_CREATURE_TEMPLATE, "SELECT entry, KillCredit1, KillCredit2, name, femaleName, subname, TitleAlt, IconName, RequiredExpansion, VignetteID, faction, npcflag, speed_walk, speed_run, scale, Classification, dmgschool, BaseAttackTime, RangeAttackTime, BaseVariance, RangeVariance, unit_class, unit_flags, unit_flags2, unit_flags3, family, trainer_class, type, VehicleId, AIName, MovementType, ctm.HoverInitiallyEnabled, ctm.Chase, ctm.Random, ctm.InteractionPauseTimer, ExperienceModifier, RacialLeader, movementId, WidgetSetID, WidgetSetUnitConditionID, RegenHealth, CreatureImmunitiesId, flags_extra, ScriptName, StringId FROM creature_template ct LEFT JOIN creature_template_movement ctm ON ct.entry = ctm.CreatureId WHERE entry = ? OR 1 = ?");
            PrepareStatement(WorldStatements.SEL_CREATURE_BY_ID, "SELECT guid FROM creature WHERE id = ?");
            PrepareStatement(WorldStatements.SEL_GAMEOBJECT_NEAREST, "SELECT guid, id, position_x, position_y, position_z, map, (POW(position_x - ?, 2) + POW(position_y - ?, 2) + POW(position_z - ?, 2)) AS order_ FROM gameobject WHERE map = ? AND (POW(position_x - ?, 2) + POW(position_y - ?, 2) + POW(position_z - ?, 2)) <= ? ORDER BY order_");
            PrepareStatement(WorldStatements.SEL_CREATURE_NEAREST, "SELECT guid, id, position_x, position_y, position_z, map, (POW(position_x - ?, 2) + POW(position_y - ?, 2) + POW(position_z - ?, 2)) AS order_ FROM creature WHERE map = ? AND (POW(position_x - ?, 2) + POW(position_y - ?, 2) + POW(position_z - ?, 2)) <= ? ORDER BY order_");
            PrepareStatement(WorldStatements.INS_CREATURE, "INSERT INTO creature (guid, id , map, spawnDifficulties, phaseUseFlags, PhaseId, PhaseGroup, terrainSwapMap, modelid, equipment_id, position_x, position_y, position_z, orientation, spawntimesecs, wander_distance, currentwaypoint, curHealthPct, MovementType, npcflag, unit_flags, unit_flags2, unit_flags3, ScriptName, StringId) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(WorldStatements.DEL_GAME_EVENT_CREATURE, "DELETE FROM game_event_creature WHERE guid = ?");
            PrepareStatement(WorldStatements.DEL_GAME_EVENT_MODEL_EQUIP, "DELETE FROM game_event_model_equip WHERE guid = ?");
            PrepareStatement(WorldStatements.INS_GAMEOBJECT, "INSERT INTO gameobject (guid, id, map, spawnDifficulties, phaseUseFlags, PhaseId, PhaseGroup, terrainSwapMap, position_x, position_y, position_z, orientation, rotation0, rotation1, rotation2, rotation3, spawntimesecs, animprogress, state, ScriptName, StringId) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
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
        SEL_WAYPOINT_PATH,
        INS_WAYPOINT_PATH,
        SEL_WAYPOINT_PATH_NODE,
        INS_WAYPOINT_PATH_NODE,
        DEL_WAYPOINT_PATH_NODE,
        UPD_WAYPOINT_PATH_NODE,
        UPD_WAYPOINT_PATH_NODE_POSITION,
        SEL_WAYPOINT_PATH_NODE_MAX_PATHID,
        SEL_WAYPOINT_PATH_NODE_POS_BY_PATHID,
        SEL_WAYPOINT_PATH_NODE_POS_FIRST_BY_PATHID,
        SEL_WAYPOINT_PATH_NODE_POS_LAST_BY_PATHID,
        SEL_WAYPOINT_PATH_NODE_MAX_NODEID,
        SEL_WAYPOINT_PATH_NODE_BY_POS,
        UPD_CREATURE_ADDON_PATH,
        INS_CREATURE_ADDON,
        DEL_CREATURE_ADDON,
        SEL_CREATURE_ADDON_BY_GUID,
        DEL_CREATURE,
        SEL_COMMANDS,
        SEL_CREATURE_TEMPLATE,
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
