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
using Game.PvP;
using Game.Scenarios;
using Game.Scripting;
using Game.Services;
using Game.SupportSystem;

public static class Global
{
    //Main
    public static ObjectAccessor ObjAccessor => ObjectAccessor.Instance;
    public static ObjectManager ObjectMgr => ObjectManager.Instance;
    public static WorldManager WorldMgr => WorldManager.Instance;
    public static RealmManager RealmMgr => RealmManager.Instance;
    public static WorldServiceManager ServiceMgr => WorldServiceManager.Instance;

    //Guild
    public static PetitionManager PetitionMgr => PetitionManager.Instance;
    public static GuildManager GuildMgr => GuildManager.Instance;

    //Social
    public static CalendarManager CalendarMgr => CalendarManager.Instance;
    public static SocialManager SocialMgr => SocialManager.Instance;
    public static WhoListStorageManager WhoListStorageMgr => WhoListStorageManager.Instance;

    //Scripts
    public static ScriptManager ScriptMgr => ScriptManager.Instance;
    public static SmartAIManager SmartAIMgr => SmartAIManager.Instance;

    //Groups
    public static GroupManager GroupMgr => GroupManager.Instance;
    public static LFGManager LFGMgr => LFGManager.Instance;
    public static ArenaTeamManager ArenaTeamMgr => ArenaTeamManager.Instance;

    //Maps System
    public static TerrainManager TerrainMgr => TerrainManager.Instance;
    public static MapManager MapMgr => MapManager.Instance;
    public static MMapManager MMapMgr => MMapManager.Instance;
    public static VMapManager VMapMgr => VMapManager.Instance;
    public static WaypointManager WaypointMgr => WaypointManager.Instance;
    public static TransportManager TransportMgr => TransportManager.Instance;
    public static InstanceLockManager InstanceLockMgr => InstanceLockManager.Instance;
    public static ScenarioManager ScenarioMgr => ScenarioManager.Instance;

    //PVP
    public static BattlegroundManager BattlegroundMgr => BattlegroundManager.Instance;
    public static OutdoorPvPManager OutdoorPvPMgr => OutdoorPvPManager.Instance;
    public static BattleFieldManager BattleFieldMgr => BattleFieldManager.Instance;

    //Account
    public static AccountManager AccountMgr => AccountManager.Instance;
    public static BNetAccountManager BNetAccountMgr => BNetAccountManager.Instance;

    //Garrison
    public static GarrisonManager GarrisonMgr => GarrisonManager.Instance;

    //Achievement
    public static AchievementGlobalMgr AchievementMgr => AchievementGlobalMgr.Instance;
    public static CriteriaManager CriteriaMgr => CriteriaManager.Instance;

    //DataStorage
    public static AreaTriggerDataStorage AreaTriggerDataStorage => AreaTriggerDataStorage.Instance;
    public static CharacterTemplateDataStorage CharacterTemplateDataStorage => CharacterTemplateDataStorage.Instance;
    public static ConversationDataStorage ConversationDataStorage => ConversationDataStorage.Instance;
    public static CharacterCache CharacterCacheStorage => CharacterCache.Instance;
    public static LootItemStorage LootItemStorage => LootItemStorage.Instance;

    //Misc
    public static ConditionManager ConditionMgr => ConditionManager.Instance;
    public static DB2Manager DB2Mgr => DB2Manager.Instance;
    public static DisableManager DisableMgr => DisableManager.Instance;
    public static PoolManager PoolMgr => PoolManager.Instance;
    public static QuestPoolManager QuestPoolMgr => QuestPoolManager.Instance;
    public static WeatherManager WeatherMgr => WeatherManager.Instance;

    public static GameEventManager GameEventMgr => GameEventManager.Instance;
    public static LanguageManager LanguageMgr => LanguageManager.Instance;
    public static CreatureTextManager CreatureTextMgr => CreatureTextManager.Instance;
    public static AuctionManager AuctionHouseMgr => AuctionManager.Instance;

    public static SpellManager SpellMgr => SpellManager.Instance;
    public static SupportManager SupportMgr => SupportManager.Instance;
    public static WardenCheckManager WardenCheckMgr => WardenCheckManager.Instance;
    public static BlackMarketManager BlackMarketMgr => BlackMarketManager.Instance;

    public static WorldStateManager WorldStateMgr => WorldStateManager.Instance;
}