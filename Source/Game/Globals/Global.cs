// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game;
using Game.Achievements;
using Game.AI;
using Game.Arenas;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.BlackMarket;
using Game.Cache;
using Game.Chat;
using Game.Collision;
using Game.DataStorage;
using Game.DungeonFinding;
using Game.Entities;
using Game.Garrisons;
using Game.Groups;
using Game.Loots;
using Game.Maps;
using Game.Networking;
using Game.PvP;
using Game.Scenarios;
using Game.Scripting;
using Game.Services;
using Game.SupportSystem;

public static class Global
{
    //Main
    public static ObjectAccessor ObjAccessor { get { return ObjectAccessor.Instance; } }
    public static ObjectManager ObjectMgr { get { return ObjectManager.Instance; } }
    public static WorldManager WorldMgr { get { return WorldManager.Instance; } }
    public static RealmManager RealmMgr { get { return RealmManager.Instance; } }
    public static WorldServiceManager ServiceMgr { get { return WorldServiceManager.Instance; } }
    public static WorldSocketManager WorldSocketMgr { get { return WorldSocketManager.Instance; } }

    //Guild
    public static PetitionManager PetitionMgr { get { return PetitionManager.Instance; } }
    public static GuildManager GuildMgr { get { return GuildManager.Instance; } }

    //Social
    public static CalendarManager CalendarMgr { get { return CalendarManager.Instance; } }
    public static SocialManager SocialMgr { get { return SocialManager.Instance; } }
    public static WhoListStorageManager WhoListStorageMgr { get { return WhoListStorageManager.Instance; } }

    //Scripts
    public static ScriptManager ScriptMgr { get { return ScriptManager.Instance; } }
    public static SmartAIManager SmartAIMgr { get { return SmartAIManager.Instance; } }

    //Groups
    public static GroupManager GroupMgr { get { return GroupManager.Instance; } }
    public static LFGManager LFGMgr { get { return LFGManager.Instance; } }
    public static ArenaTeamManager ArenaTeamMgr { get { return ArenaTeamManager.Instance; } }

    //Maps System
    public static TerrainManager TerrainMgr { get { return TerrainManager.Instance; } }
    public static MapManager MapMgr { get { return MapManager.Instance; } }
    public static MMapManager MMapMgr { get { return MMapManager.Instance; } }
    public static VMapManager VMapMgr { get { return VMapManager.Instance; } }
    public static WaypointManager WaypointMgr { get { return WaypointManager.Instance; } }
    public static TransportManager TransportMgr { get { return TransportManager.Instance; } }
    public static InstanceLockManager InstanceLockMgr { get { return InstanceLockManager.Instance; } }
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
    public static CharacterCache CharacterCacheStorage { get { return CharacterCache.Instance; } }
    public static LootItemStorage LootItemStorage { get { return LootItemStorage.Instance; } }

    //Misc
    public static ConditionManager ConditionMgr { get { return ConditionManager.Instance; } }
    public static DB2Manager DB2Mgr { get { return DB2Manager.Instance; } }
    public static DisableManager DisableMgr { get { return DisableManager.Instance; } }
    public static PoolManager PoolMgr { get { return PoolManager.Instance; } }
    public static QuestPoolManager QuestPoolMgr { get { return QuestPoolManager.Instance; } }
    public static WeatherManager WeatherMgr { get { return WeatherManager.Instance; } }

    public static GameEventManager GameEventMgr { get { return GameEventManager.Instance; } }
    public static LanguageManager LanguageMgr { get { return LanguageManager.Instance; } }
    public static CreatureTextManager CreatureTextMgr { get { return CreatureTextManager.Instance; } }
    public static AuctionManager AuctionHouseMgr { get { return AuctionManager.Instance; } }

    public static SpellManager SpellMgr { get { return SpellManager.Instance; } }
    public static SupportManager SupportMgr { get { return SupportManager.Instance; } }
    public static WardenCheckManager WardenCheckMgr { get { return WardenCheckManager.Instance; } }
    public static BlackMarketManager BlackMarketMgr { get { return BlackMarketManager.Instance; } }

    public static WorldStateManager WorldStateMgr { get { return WorldStateManager.Instance; } }
}
