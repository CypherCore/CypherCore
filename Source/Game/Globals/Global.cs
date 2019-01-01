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

using Game;
using Game.Achievements;
using Game.AI;
using Game.Arenas;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.BlackMarket;
using Game.Collision;
using Game.DataStorage;
using Game.DungeonFinding;
using Game.Entities;
using Game.Garrisons;
using Game.Groups;
using Game.Guilds;
using Game.Maps;
using Game.PvP;
using Game.Scenarios;
using Game.Scripting;
using Game.SupportSystem;
using Game.Services;

public static class Global
{
    //Main
    public static ObjectAccessor ObjAccessor { get { return ObjectAccessor.Instance; } }
    public static ObjectManager ObjectMgr { get { return ObjectManager.Instance; } }
    public static WorldManager WorldMgr { get { return WorldManager.Instance; } }
    public static RealmManager RealmMgr { get { return RealmManager.Instance; } }
    public static ServiceDispatcher ServiceMgr { get { return ServiceDispatcher.Instance; } }

    //Guild
    public static GuildManager GuildMgr { get { return GuildManager.Instance; } }
    public static GuildFinderManager GuildFinderMgr { get { return GuildFinderManager.Instance; } }

    //Social
    public static CalendarManager CalendarMgr { get { return CalendarManager.Instance; } }
    public static SocialManager SocialMgr { get { return SocialManager.Instance; } }

    //Scripts
    public static ScriptManager ScriptMgr { get { return ScriptManager.Instance; } }
    public static SmartAIManager SmartAIMgr { get { return SmartAIManager.Instance; } }

    //Groups
    public static GroupManager GroupMgr { get { return GroupManager.Instance; } }
    public static LFGManager LFGMgr { get { return LFGManager.Instance; } }
    public static ArenaTeamManager ArenaTeamMgr { get { return ArenaTeamManager.Instance; } }

    //Maps System
    public static MapManager MapMgr { get { return MapManager.Instance; } }
    public static MMapManager MMapMgr { get { return MMapManager.Instance; } }
    public static VMapManager VMapMgr { get { return VMapManager.Instance; } }
    public static WaypointManager WaypointMgr { get { return WaypointManager.Instance; } }
    public static TransportManager TransportMgr { get { return TransportManager.Instance; } }
    public static InstanceSaveManager InstanceSaveMgr { get { return InstanceSaveManager.Instance; } }
    public static TaxiPathGraph TaxiPathGraph { get { return TaxiPathGraph.Instance; } }
    public static ScenarioManager ScenarioMgr { get { return ScenarioManager.Instance; } }

    //PVP
    public static BattlegroundManager BattlegroundMgr { get { return BattlegroundManager.Instance; } }
    public static OutdoorPvPManager OutdoorPvPMgr { get { return OutdoorPvPManager.Instance; } }
    public static BattleFieldManager BattleFieldMgr { get { return BattleFieldManager.Instance; } }

    //Account
    public static AccountManager AccountMgr { get { return AccountManager.Instance; } }
    public static BNetAccountManager BNetAccountMgr { get { return BNetAccountManager.Instance; } }

    //Garrison
    public static GarrisonManager GarrisonMgr { get { return GarrisonManager.Instance; } }

    //Achievement
    public static AchievementGlobalMgr AchievementMgr { get { return AchievementGlobalMgr.Instance; } }
    public static CriteriaManager CriteriaMgr { get { return CriteriaManager.Instance; } }

    //DataStorage
    public static AreaTriggerDataStorage AreaTriggerDataStorage { get { return AreaTriggerDataStorage.Instance; } }
    public static CharacterTemplateDataStorage CharacterTemplateDataStorage { get { return CharacterTemplateDataStorage.Instance; } }
    public static ConversationDataStorage ConversationDataStorage { get { return ConversationDataStorage.Instance; } }

    //Misc
    public static ConditionManager ConditionMgr { get { return ConditionManager.Instance; } }
    public static DB2Manager DB2Mgr { get { return DB2Manager.Instance; } }
    public static DisableManager DisableMgr { get { return DisableManager.Instance; } }
    public static PoolManager PoolMgr { get { return PoolManager.Instance; } }
    public static WeatherManager WeatherMgr { get { return WeatherManager.Instance; } }

    public static GameEventManager GameEventMgr { get { return GameEventManager.Instance; } }
    public static CreatureTextManager CreatureTextMgr { get { return CreatureTextManager.Instance; } }
    public static AuctionManager AuctionMgr { get { return AuctionManager.Instance; } }

    public static SpellManager SpellMgr { get { return SpellManager.Instance; } }
    public static SupportManager SupportMgr { get { return SupportManager.Instance; } }
    public static WardenCheckManager WardenCheckMgr { get { return WardenCheckManager.Instance; } }
    public static BlackMarketManager BlackMarketMgr { get { return BlackMarketManager.Instance; } }
}
