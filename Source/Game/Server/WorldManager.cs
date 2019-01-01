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
using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Game.BattlePets;
using Game.Chat;
using Game.Collision;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Network;
using Game.Network.Packets;
using Game.Spells;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public class WorldManager : Singleton<WorldManager>
    {
        WorldManager()
        {
            foreach (WorldTimers timer in Enum.GetValues(typeof(WorldTimers)))
                m_timers[timer] = new IntervalTimer();

            m_allowedSecurityLevel = AccountTypes.Player;
            m_gameTime = Time.UnixTime;
            m_startTime = m_gameTime;

            _realm = new Realm();
        }

        public Player FindPlayerInZone(uint zone)
        {
            foreach (var session in m_sessions)
            {
                Player player = session.Value.GetPlayer();
                if (player == null)
                    continue;

                if (player.IsInWorld && player.GetZoneId() == zone)
                {
                    // Used by the weather system. We return the player to broadcast the change weather message to him and all players in the zone.
                    return player;
                }
            }
            return null;
        }

        public bool IsClosed()
        {
            return m_isClosed;
        }

        public void SetClosed(bool val)
        {
            m_isClosed = val;
            Global.ScriptMgr.OnOpenStateChange(!val);
        }

        public void SetMotd(string motd)
        {
            Global.ScriptMgr.OnMotdChange(motd);

            m_motd.Clear();
            m_motd.AddRange(motd.Split('@'));
        }

        public List<string> GetMotd()
        {
            return m_motd;
        }

        public WorldSession FindSession(uint id)
        {
            return m_sessions.LookupByKey(id);
        }

        bool RemoveSession(uint id)
        {
            // Find the session, kick the user, but we can't delete session at this moment to prevent iterator invalidation
            var session = m_sessions.LookupByKey(id);

            if (session != null)
            {
                if (session.PlayerLoading())
                    return false;

                session.KickPlayer();
            }

            return true;
        }

        public void AddSession(WorldSession s)
        {
            addSessQueue.Enqueue(s);
        }

        public void AddInstanceSocket(WorldSocket sock, ulong connectToKey)
        {
            _linkSocketQueue.Enqueue(Tuple.Create(sock, connectToKey));
        }

        void AddSession_(WorldSession s)
        {
            Cypher.Assert(s != null);

            //NOTE - Still there is race condition in WorldSession* being used in the Sockets

            // kick already loaded player with same account (if any) and remove session
            // if player is in loading and want to load again, return
            if (!RemoveSession(s.GetAccountId()))
            {
                s.KickPlayer();
                return;
            }

            // decrease session counts only at not reconnection case
            bool decrease_session = true;

            // if session already exist, prepare to it deleting at next world update
            // NOTE - KickPlayer() should be called on "old" in RemoveSession()
            {
                var old = m_sessions.LookupByKey(s.GetAccountId());
                if (old != null)
                {
                    // prevent decrease sessions count if session queued
                    if (RemoveQueuedPlayer(old))
                        decrease_session = false;
                }
            }

            m_sessions[s.GetAccountId()] = s;

            int Sessions = GetActiveAndQueuedSessionCount();
            uint pLimit = GetPlayerAmountLimit();
            int QueueSize = GetQueuedSessionCount(); //number of players in the queue

            //so we don't count the user trying to
            //login as a session and queue the socket that we are using
            if (decrease_session)
                --Sessions;

            if (pLimit > 0 && Sessions >= pLimit && !s.HasPermission(RBACPermissions.SkipQueue) && !HasRecentlyDisconnected(s))
            {
                AddQueuedPlayer(s);
                UpdateMaxSessionCounters();
                Log.outInfo(LogFilter.Server, "PlayerQueue: Account id {0} is in Queue Position ({1}).", s.GetAccountId(), ++QueueSize);
                return;
            }

            s.InitializeSession();

            UpdateMaxSessionCounters();

            // Updates the population
            if (pLimit > 0)
            {
                float popu = GetActiveSessionCount();              // updated number of users on the server
                popu /= pLimit;
                popu *= 2;
                Log.outInfo(LogFilter.Server, "Server Population ({0}).", popu);
            }
        }

        void ProcessLinkInstanceSocket(Tuple<WorldSocket, ulong> linkInfo)
        {
            if (!linkInfo.Item1.IsOpen())
                return;

            ConnectToKey key = new ConnectToKey();
            key.Raw = linkInfo.Item2;

            WorldSession session = FindSession(key.AccountId);
            if (!session || session.GetConnectToInstanceKey() != linkInfo.Item2)
            {
                linkInfo.Item1.SendAuthResponseError(BattlenetRpcErrorCode.TimedOut);
                linkInfo.Item1.CloseSocket();
                return;
            }

            linkInfo.Item1.SetWorldSession(session);
            session.AddInstanceConnection(linkInfo.Item1);
            session.HandleContinuePlayerLogin();
        }

        bool HasRecentlyDisconnected(WorldSession session)
        {
            if (session == null)
                return false;
            uint tolerance = 0;
            if (tolerance != 0)
            {
                foreach (var disconnect in m_disconnects)
                {
                    if ((disconnect.Value - Time.UnixTime) < tolerance)
                    {
                        if (disconnect.Key == session.GetAccountId())
                            return true;
                    }
                    else
                        m_disconnects.Remove(disconnect.Key);
                }
            }
            return false;
        }

        uint GetQueuePos(WorldSession sess)
        {
            uint position = 1;

            foreach (var iter in m_QueuedPlayer)
            {
                if (iter != sess)
                    ++position;
                else
                    return position;
            }
            return 0;
        }

        void AddQueuedPlayer(WorldSession sess)
        {
            sess.SetInQueue(true);
            m_QueuedPlayer.Add(sess);

            // The 1st SMSG_AUTH_RESPONSE needs to contain other info too.
            sess.SendAuthResponse(BattlenetRpcErrorCode.Ok, true, GetQueuePos(sess));
        }

        bool RemoveQueuedPlayer(WorldSession sess)
        {
            // sessions count including queued to remove (if removed_session set)
            int sessions = GetActiveSessionCount();

            uint position = 1;

            // search to remove and count skipped positions
            bool found = false;

            foreach (var iter in m_QueuedPlayer)
            {
                if (iter != sess)
                    ++position;
                else
                {
                    sess.SetInQueue(false);
                    sess.ResetTimeOutTime();
                    m_QueuedPlayer.Remove(iter);
                    found = true;                                   // removing queued session
                    break;
                }
            }

            // iter point to next socked after removed or end()
            // position store position of removed socket and then new position next socket after removed

            // if session not queued then we need decrease sessions count
            if (!found && sessions != 0)
                --sessions;

            // accept first in queue
            if ((m_playerLimit == 0 || sessions < m_playerLimit) && !m_QueuedPlayer.Empty())
            {
                WorldSession pop_sess = m_QueuedPlayer.First();
                pop_sess.InitializeSession();

                m_QueuedPlayer.RemoveAt(0);

                // update iter to point first queued socket or end() if queue is empty now
                position = 1;
            }

            // update position from iter to end()
            // iter point to first not updated socket, position store new position
            foreach (var iter in m_QueuedPlayer)
            {
                iter.SendAuthWaitQue(++position);
            }

            return found;
        }

        public void SetInitialWorldSettings()
        {
            LoadRealmInfo();

            LoadConfigSettings();

            // Initialize Allowed Security Level
            LoadDBAllowedSecurityLevel();

            Global.ObjectMgr.SetHighestGuids();

            if (!Global.MapMgr.ExistMapAndVMap(0, -6240.32f, 331.033f) || !Global.MapMgr.ExistMapAndVMap(0, -8949.95f, -132.493f)
                || !Global.MapMgr.ExistMapAndVMap(1, -618.518f, -4251.67f) || !Global.MapMgr.ExistMapAndVMap(0, 1676.35f, 1677.45f)
                || !Global.MapMgr.ExistMapAndVMap(1, 10311.3f, 832.463f) || !Global.MapMgr.ExistMapAndVMap(1, -2917.58f, -257.98f)
                || (WorldConfig.GetIntValue(WorldCfg.Expansion) != 0 && (!Global.MapMgr.ExistMapAndVMap(530, 10349.6f, -6357.29f) || !Global.MapMgr.ExistMapAndVMap(530, -3961.64f, -13931.2f))))
            {
                Log.outError(LogFilter.ServerLoading, "Unable to load critical files - server shutting down !!!");
                ShutdownServ(0, 0, ShutdownExitCode.Error);
                return;
            }

            // Initialize pool manager
            Global.PoolMgr.Initialize();

            // Initialize game event manager
            Global.GameEventMgr.Initialize();

            Log.outInfo(LogFilter.ServerLoading, "Loading Cypher Strings...");
            Global.ObjectMgr.LoadCypherStrings();

            // not send custom type REALM_FFA_PVP to realm list
            RealmType server_type = IsFFAPvPRealm() ? RealmType.PVP : (RealmType)WorldConfig.GetIntValue(WorldCfg.GameType);
            uint realm_zone = WorldConfig.GetUIntValue(WorldCfg.RealmZone);

            DB.Login.Execute("UPDATE realmlist SET icon = {0}, timezone = {1} WHERE id = '{2}'", (byte)server_type, realm_zone, _realm.Id.Realm);      // One-time query

            Log.outInfo(LogFilter.ServerLoading, "Initialize DataStorage...");
            // Load DB2s
            CliDB.LoadStores(_dataPath, m_defaultDbcLocale);

            Log.outInfo(LogFilter.ServerLoading, "Loading hotfix blobs...");
            Global.DB2Mgr.LoadHotfixBlob();

            Log.outInfo(LogFilter.ServerLoading, "Loading hotfix info...");
            Global.DB2Mgr.LoadHotfixData();

            //- Load M2 fly by cameras
            M2Storage.LoadM2Cameras(_dataPath);

            //- Load GameTables
            CliDB.LoadGameTables(_dataPath);

            //Load weighted graph on taxi nodes path
            Global.TaxiPathGraph.Initialize();

            MultiMap<uint, uint> mapData = new MultiMap<uint, uint>();
            foreach (MapRecord mapEntry in CliDB.MapStorage.Values)
            {
                if (mapEntry.ParentMapID != -1)
                    mapData.Add((uint)mapEntry.ParentMapID, mapEntry.Id);
            }

            Global.MapMgr.InitializeParentMapData(mapData);

            Global.VMapMgr.Initialize(mapData);
            Global.MMapMgr.Initialize(mapData);

            Log.outInfo(LogFilter.ServerLoading, "Loading SpellInfo Storage...");
            Global.SpellMgr.LoadSpellInfoStore();

            Log.outInfo(LogFilter.ServerLoading, "Loading SpellInfo corrections...");
            Global.SpellMgr.LoadSpellInfoCorrections();

            Log.outInfo(LogFilter.ServerLoading, "Loading SkillLineAbility Data...");
            Global.SpellMgr.LoadSkillLineAbilityMap();

            Log.outInfo(LogFilter.ServerLoading, "Loading SpellInfo custom attributes...");
            Global.SpellMgr.LoadSpellInfoCustomAttributes();

            Log.outInfo(LogFilter.ServerLoading, "Loading SpellInfo diminishing infos...");
            Global.SpellMgr.LoadSpellInfoDiminishing();

            Log.outInfo(LogFilter.ServerLoading, "Loading SpellInfo immunity infos...");
            Global.SpellMgr.LoadSpellInfoImmunities();

            Log.outInfo(LogFilter.ServerLoading, "Loading PetFamilySpellsStore Data...");
            Global.SpellMgr.LoadPetFamilySpellsStore();

            Log.outInfo(LogFilter.ServerLoading, "Loading Spell Totem models...");
            Global.SpellMgr.LoadSpellTotemModel();

            Log.outInfo(LogFilter.ServerLoading, "Loading GameObject models...");
            GameObjectModel.LoadGameObjectModelList();

            Log.outInfo(LogFilter.ServerLoading, "Loading Script Names...");
            Global.ObjectMgr.LoadScriptNames();

            Log.outInfo(LogFilter.ServerLoading, "Loading Instance Template...");
            Global.ObjectMgr.LoadInstanceTemplate();

            // Must be called before `creature_respawn`/`gameobject_respawn` tables
            Log.outInfo(LogFilter.ServerLoading, "Loading instances...");
            Global.InstanceSaveMgr.LoadInstances();

            Log.outInfo(LogFilter.ServerLoading, "Loading Localization strings...");
            uint oldMSTime = Time.GetMSTime();
            Global.ObjectMgr.LoadCreatureLocales();
            Global.ObjectMgr.LoadGameObjectLocales();
            Global.ObjectMgr.LoadQuestTemplateLocale();
            Global.ObjectMgr.LoadQuestGreetingLocales();
            Global.ObjectMgr.LoadQuestOfferRewardLocale();
            Global.ObjectMgr.LoadQuestRequestItemsLocale();
            Global.ObjectMgr.LoadQuestObjectivesLocale();
            Global.ObjectMgr.LoadPageTextLocales();
            Global.ObjectMgr.LoadGossipMenuItemsLocales();
            Global.ObjectMgr.LoadPointOfInterestLocales();
            Log.outInfo(LogFilter.ServerLoading, "Localization strings loaded in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));

            Log.outInfo(LogFilter.ServerLoading, "Loading Account Roles and Permissions...");
            Global.AccountMgr.LoadRBAC();

            Log.outInfo(LogFilter.ServerLoading, "Loading Page Texts...");
            Global.ObjectMgr.LoadPageTexts();

            Log.outInfo(LogFilter.ServerLoading, "Loading GameObject Template...");
            Global.ObjectMgr.LoadGameObjectTemplate();

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Object template addons...");
            Global.ObjectMgr.LoadGameObjectTemplateAddons();

            Log.outInfo(LogFilter.ServerLoading, "Loading Transport Templates...");
            Global.TransportMgr.LoadTransportTemplates();

            Log.outInfo(LogFilter.ServerLoading, "Loading Transport animations and rotations...");
            Global.TransportMgr.LoadTransportAnimationAndRotation();

            Log.outInfo(LogFilter.ServerLoading, "Loading Spell Rank Data...");
            Global.SpellMgr.LoadSpellRanks();

            Log.outInfo(LogFilter.ServerLoading, "Loading Spell Required Data...");
            Global.SpellMgr.LoadSpellRequired();

            Log.outInfo(LogFilter.ServerLoading, "Loading Spell Group types...");
            Global.SpellMgr.LoadSpellGroups();

            Log.outInfo(LogFilter.ServerLoading, "Loading Spell Learn Skills...");
            Global.SpellMgr.LoadSpellLearnSkills();

            Log.outInfo(LogFilter.ServerLoading, "Loading SpellInfo SpellSpecific and AuraState...");
            Global.SpellMgr.LoadSpellInfoSpellSpecificAndAuraState();

            Log.outInfo(LogFilter.ServerLoading, "Loading Spell Learn Spells...");
            Global.SpellMgr.LoadSpellLearnSpells();

            Log.outInfo(LogFilter.ServerLoading, "Loading Spell Proc conditions and data...");
            Global.SpellMgr.LoadSpellProcs();

            Log.outInfo(LogFilter.ServerLoading, "Loading Aggro Spells Definitions...");
            Global.SpellMgr.LoadSpellThreats();

            Log.outInfo(LogFilter.ServerLoading, "Loading Spell Group Stack Rules...");
            Global.SpellMgr.LoadSpellGroupStackRules();

            Log.outInfo(LogFilter.ServerLoading, "Loading NPC Texts...");
            Global.ObjectMgr.LoadNPCText();

            Log.outInfo(LogFilter.ServerLoading, "Loading Enchant Spells Proc datas...");
            Global.SpellMgr.LoadSpellEnchantProcData();

            Log.outInfo(LogFilter.ServerLoading, "Loading Item Random Enchantments Table...");
            ItemEnchantment.LoadRandomEnchantmentsTable();

            Log.outInfo(LogFilter.ServerLoading, "Loading Disables");                         // must be before loading quests and items
            Global.DisableMgr.LoadDisables();

            Log.outInfo(LogFilter.ServerLoading, "Loading Items...");                         // must be after LoadRandomEnchantmentsTable and LoadPageTexts
            Global.ObjectMgr.LoadItemTemplates();

            Log.outInfo(LogFilter.ServerLoading, "Loading Item set names...");                // must be after LoadItemPrototypes
            Global.ObjectMgr.LoadItemTemplateAddon();

            Log.outInfo(LogFilter.ServerLoading, "Loading Item Scripts...");                 // must be after LoadItemPrototypes
            Global.ObjectMgr.LoadItemScriptNames();

            Log.outInfo(LogFilter.ServerLoading, "Loading Creature Model Based Info Data...");
            Global.ObjectMgr.LoadCreatureModelInfo();

            Log.outInfo(LogFilter.ServerLoading, "Loading Creature templates...");
            Global.ObjectMgr.LoadCreatureTemplates();

            Log.outInfo(LogFilter.ServerLoading, "Loading Equipment templates...");
            Global.ObjectMgr.LoadEquipmentTemplates();

            Log.outInfo(LogFilter.ServerLoading, "Loading Creature template addons...");
            Global.ObjectMgr.LoadCreatureTemplateAddons();

            Log.outInfo(LogFilter.ServerLoading, "Loading Creature template scaling...");
            Global.ObjectMgr.LoadCreatureScalingData();

            Log.outInfo(LogFilter.ServerLoading, "Loading Reputation Reward Rates...");
            Global.ObjectMgr.LoadReputationRewardRate();

            Log.outInfo(LogFilter.ServerLoading, "Loading Creature Reputation OnKill Data...");
            Global.ObjectMgr.LoadReputationOnKill();

            Log.outInfo(LogFilter.ServerLoading, "Loading Reputation Spillover Data...");
            Global.ObjectMgr.LoadReputationSpilloverTemplate();

            Log.outInfo(LogFilter.ServerLoading, "Loading Points Of Interest Data...");
            Global.ObjectMgr.LoadPointsOfInterest();

            Log.outInfo(LogFilter.ServerLoading, "Loading Creature Base Stats...");
            Global.ObjectMgr.LoadCreatureClassLevelStats();

            Log.outInfo(LogFilter.ServerLoading, "Loading Creature Data...");
            Global.ObjectMgr.LoadCreatures();

            Log.outInfo(LogFilter.ServerLoading, "Loading Temporary Summon Data...");
            Global.ObjectMgr.LoadTempSummons();                               // must be after LoadCreatureTemplates() and LoadGameObjectTemplates()

            Log.outInfo(LogFilter.ServerLoading, "Loading pet levelup spells...");
            Global.SpellMgr.LoadPetLevelupSpellMap();

            Log.outInfo(LogFilter.ServerLoading, "Loading pet default spells additional to levelup spells...");
            Global.SpellMgr.LoadPetDefaultSpells();

            Log.outInfo(LogFilter.ServerLoading, "Loading Creature Addon Data...");
            Global.ObjectMgr.LoadCreatureAddons();

            Log.outInfo(LogFilter.ServerLoading, "Loading GameObjects...");
            Global.ObjectMgr.LoadGameobjects();

            Log.outInfo(LogFilter.ServerLoading, "Loading GameObject Addon Data...");
            Global.ObjectMgr.LoadGameObjectAddons();                          // must be after LoadGameObjectTemplate() and LoadGameobjects()

            Log.outInfo(LogFilter.ServerLoading, "Loading GameObject Quest Items...");
            Global.ObjectMgr.LoadGameObjectQuestItems();

            Log.outInfo(LogFilter.ServerLoading, "Loading Creature Quest Items...");
            Global.ObjectMgr.LoadCreatureQuestItems();

            Log.outInfo(LogFilter.ServerLoading, "Loading Creature Linked Respawn...");
            Global.ObjectMgr.LoadLinkedRespawn();                             // must be after LoadCreatures(), LoadGameObjects()

            Log.outInfo(LogFilter.ServerLoading, "Loading Weather Data...");
            Global.WeatherMgr.LoadWeatherData();

            Log.outInfo(LogFilter.ServerLoading, "Loading Quests...");
            Global.ObjectMgr.LoadQuests();

            Log.outInfo(LogFilter.ServerLoading, "Checking Quest Disables");
            Global.DisableMgr.CheckQuestDisables();                           // must be after loading quests

            Log.outInfo(LogFilter.ServerLoading, "Loading Quest POI");
            Global.ObjectMgr.LoadQuestPOI();

            Log.outInfo(LogFilter.ServerLoading, "Loading Quests Starters and Enders...");
            Global.ObjectMgr.LoadQuestStartersAndEnders();                    // must be after quest load

            Log.outInfo(LogFilter.ServerLoading, "Loading Quest Greetings...");
            Global.ObjectMgr.LoadQuestGreetings();

            Log.outInfo(LogFilter.ServerLoading, "Loading Objects Pooling Data...");
            Global.PoolMgr.LoadFromDB();

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Data...");               // must be after loading pools fully
            Global.GameEventMgr.LoadFromDB();

            Log.outInfo(LogFilter.ServerLoading, "Loading NPCSpellClick Data..."); // must be after LoadQuests
            Global.ObjectMgr.LoadNPCSpellClickSpells();

            Log.outInfo(LogFilter.ServerLoading, "Loading Vehicle Template Accessories...");
            Global.ObjectMgr.LoadVehicleTemplateAccessories();                // must be after LoadCreatureTemplates() and LoadNPCSpellClickSpells()

            Log.outInfo(LogFilter.ServerLoading, "Loading Vehicle Accessories...");
            Global.ObjectMgr.LoadVehicleAccessories();                       // must be after LoadCreatureTemplates() and LoadNPCSpellClickSpells()

            Log.outInfo(LogFilter.ServerLoading, "Loading SpellArea Data...");                // must be after quest load
            Global.SpellMgr.LoadSpellAreas();

            Log.outInfo(LogFilter.ServerLoading, "Loading AreaTrigger definitions...");
            Global.ObjectMgr.LoadAreaTriggerTeleports();

            Log.outInfo(LogFilter.ServerLoading, "Loading Access Requirements...");
            Global.ObjectMgr.LoadAccessRequirements();                        // must be after item template load

            Log.outInfo(LogFilter.ServerLoading, "Loading Quest Area Triggers...");
            Global.ObjectMgr.LoadQuestAreaTriggers();                         // must be after LoadQuests

            Log.outInfo(LogFilter.ServerLoading, "Loading Tavern Area Triggers...");
            Global.ObjectMgr.LoadTavernAreaTriggers();

            Log.outInfo(LogFilter.ServerLoading, "Loading AreaTrigger script names...");
            Global.ObjectMgr.LoadAreaTriggerScripts();

            Log.outInfo(LogFilter.ServerLoading, "Loading LFG entrance positions..."); // Must be after areatriggers
            Global.LFGMgr.LoadLFGDungeons();

            Log.outInfo(LogFilter.ServerLoading, "Loading Dungeon boss data...");
            Global.ObjectMgr.LoadInstanceEncounters();

            Log.outInfo(LogFilter.ServerLoading, "Loading LFG rewards...");
            Global.LFGMgr.LoadRewards();

            Log.outInfo(LogFilter.ServerLoading, "Loading Graveyard-zone links...");
            Global.ObjectMgr.LoadGraveyardZones();

            Log.outInfo(LogFilter.ServerLoading, "Loading spell pet auras...");
            Global.SpellMgr.LoadSpellPetAuras();

            Log.outInfo(LogFilter.ServerLoading, "Loading Spell target coordinates...");
            Global.SpellMgr.LoadSpellTargetPositions();

            Log.outInfo(LogFilter.ServerLoading, "Loading enchant custom attributes...");
            Global.SpellMgr.LoadEnchantCustomAttr();

            Log.outInfo(LogFilter.ServerLoading, "Loading linked spells...");
            Global.SpellMgr.LoadSpellLinked();

            Log.outInfo(LogFilter.ServerLoading, "Loading Player Create Data...");
            Global.ObjectMgr.LoadPlayerInfo();

            Log.outInfo(LogFilter.ServerLoading, "Loading Exploration BaseXP Data...");
            Global.ObjectMgr.LoadExplorationBaseXP();

            Log.outInfo(LogFilter.ServerLoading, "Loading Pet Name Parts...");
            Global.ObjectMgr.LoadPetNames();

            Log.outInfo(LogFilter.ServerLoading, "Loading AreaTrigger Templates...");
            Global.AreaTriggerDataStorage.LoadAreaTriggerTemplates();

            Log.outInfo(LogFilter.ServerLoading, "Loading Conversation Templates...");
            Global.ConversationDataStorage.LoadConversationTemplates();

            Log.outInfo(LogFilter.ServerLoading, "Loading Scenes Templates...");
            Global.ObjectMgr.LoadSceneTemplates();

            Log.outInfo(LogFilter.ServerLoading, "Loading Player Choices...");
            Global.ObjectMgr.LoadPlayerChoices();

            Log.outInfo(LogFilter.ServerLoading, "Loading Player Choices Locales...");
            Global.ObjectMgr.LoadPlayerChoicesLocale();

            CharacterDatabaseCleaner.CleanDatabase();

            Log.outInfo(LogFilter.ServerLoading, "Loading the max pet number...");
            Global.ObjectMgr.LoadPetNumber();

            Log.outInfo(LogFilter.ServerLoading, "Loading pet level stats...");
            Global.ObjectMgr.LoadPetLevelInfo();

            Log.outInfo(LogFilter.ServerLoading, "Loading Player level dependent mail rewards...");
            Global.ObjectMgr.LoadMailLevelRewards();

            // Loot tables
            Loots.LootManager.LoadLootTables();

            Log.outInfo(LogFilter.ServerLoading, "Loading Skill Discovery Table...");
            SkillDiscovery.LoadSkillDiscoveryTable();

            Log.outInfo(LogFilter.ServerLoading, "Loading Skill Extra Item Table...");
            SkillExtraItems.LoadSkillExtraItemTable();

            Log.outInfo(LogFilter.ServerLoading, "Loading Skill Perfection Data Table...");
            SkillPerfectItems.LoadSkillPerfectItemTable();

            Log.outInfo(LogFilter.ServerLoading, "Loading Skill Fishing base level requirements...");
            Global.ObjectMgr.LoadFishingBaseSkillLevel();

            Log.outInfo(LogFilter.ServerLoading, "Loading skill tier info...");
            Global.ObjectMgr.LoadSkillTiers();

            Log.outInfo(LogFilter.ServerLoading, "Loading Criteria Modifier trees...");
            Global.CriteriaMgr.LoadCriteriaModifiersTree();
            Log.outInfo(LogFilter.ServerLoading, "Loading Criteria Lists...");
            Global.CriteriaMgr.LoadCriteriaList();
            Log.outInfo(LogFilter.ServerLoading, "Loading Criteria Data...");
            Global.CriteriaMgr.LoadCriteriaData();
            Log.outInfo(LogFilter.ServerLoading, "Loading Achievements...");
            Global.AchievementMgr.LoadAchievementReferenceList();
            Log.outInfo(LogFilter.ServerLoading, "Loading Achievement Rewards...");
            Global.AchievementMgr.LoadRewards();
            Log.outInfo(LogFilter.ServerLoading, "Loading Achievement Reward Locales...");
            Global.AchievementMgr.LoadRewardLocales();
            Log.outInfo(LogFilter.ServerLoading, "Loading Completed Achievements...");
            Global.AchievementMgr.LoadCompletedAchievements();

            // Load dynamic data tables from the database
            Log.outInfo(LogFilter.ServerLoading, "Loading Item Auctions...");
            Global.AuctionMgr.LoadAuctionItems();

            Log.outInfo(LogFilter.ServerLoading, "Loading Auctions...");
            Global.AuctionMgr.LoadAuctions();

            if (WorldConfig.GetBoolValue(WorldCfg.BlackmarketEnabled))
            {
                Log.outInfo(LogFilter.ServerLoading, "Loading Black Market Templates...");
                Global.BlackMarketMgr.LoadTemplates();

                Log.outInfo(LogFilter.ServerLoading, "Loading Black Market Auctions...");
                Global.BlackMarketMgr.LoadAuctions();
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Guild rewards...");
            Global.GuildMgr.LoadGuildRewards();

            Log.outInfo(LogFilter.ServerLoading, "Loading Guilds...");
            Global.GuildMgr.LoadGuilds();

            Global.GuildFinderMgr.LoadFromDB();

            Log.outInfo(LogFilter.ServerLoading, "Loading ArenaTeams...");
            Global.ArenaTeamMgr.LoadArenaTeams();

            Log.outInfo(LogFilter.ServerLoading, "Loading Groups...");
            Global.GroupMgr.LoadGroups();

            Log.outInfo(LogFilter.ServerLoading, "Loading ReservedNames...");
            Global.ObjectMgr.LoadReservedPlayersNames();

            Log.outInfo(LogFilter.ServerLoading, "Loading GameObjects for quests...");
            Global.ObjectMgr.LoadGameObjectForQuests();

            Log.outInfo(LogFilter.ServerLoading, "Loading BattleMasters...");
            Global.BattlegroundMgr.LoadBattleMastersEntry();

            Log.outInfo(LogFilter.ServerLoading, "Loading GameTeleports...");
            Global.ObjectMgr.LoadGameTele();

            Log.outInfo(LogFilter.ServerLoading, "Loading Trainers...");
            Global.ObjectMgr.LoadTrainers();                                // must be after load CreatureTemplate

            Log.outInfo(LogFilter.ServerLoading, "Loading Creature default trainers...");
            Global.ObjectMgr.LoadCreatureDefaultTrainers();

            Log.outInfo(LogFilter.ServerLoading, "Loading Gossip menu...");
            Global.ObjectMgr.LoadGossipMenu();

            Log.outInfo(LogFilter.ServerLoading, "Loading Gossip menu options...");
            Global.ObjectMgr.LoadGossipMenuItems();                         // must be after LoadTrainers

            Log.outInfo(LogFilter.ServerLoading, "Loading Vendors...");
            Global.ObjectMgr.LoadVendors();                                  // must be after load CreatureTemplate and ItemTemplate

            Log.outInfo(LogFilter.ServerLoading, "Loading Waypoints...");
            Global.WaypointMgr.Load();

            Log.outInfo(LogFilter.ServerLoading, "Loading SmartAI Waypoints...");
            Global.SmartAIMgr.LoadWaypointFromDB();

            Log.outInfo(LogFilter.ServerLoading, "Loading Creature Formations...");
            FormationMgr.LoadCreatureFormations();

            Log.outInfo(LogFilter.ServerLoading, "Loading World States...");              // must be loaded before Battleground, outdoor PvP and conditions
            LoadWorldStates();

            Global.ObjectMgr.LoadPhases();

            Log.outInfo(LogFilter.ServerLoading, "Loading Conditions...");
            Global.ConditionMgr.LoadConditions();

            Log.outInfo(LogFilter.ServerLoading, "Loading faction change achievement pairs...");
            Global.ObjectMgr.LoadFactionChangeAchievements();

            Log.outInfo(LogFilter.ServerLoading, "Loading faction change spell pairs...");
            Global.ObjectMgr.LoadFactionChangeSpells();

            Log.outInfo(LogFilter.ServerLoading, "Loading faction change item pairs...");
            Global.ObjectMgr.LoadFactionChangeItems();

            Log.outInfo(LogFilter.ServerLoading, "Loading faction change quest pairs...");
            Global.ObjectMgr.LoadFactionChangeQuests();

            Log.outInfo(LogFilter.ServerLoading, "Loading faction change reputation pairs...");
            Global.ObjectMgr.LoadFactionChangeReputations();

            Log.outInfo(LogFilter.ServerLoading, "Loading faction change title pairs...");
            Global.ObjectMgr.LoadFactionChangeTitles();

            Log.outInfo(LogFilter.ServerLoading, "Loading mount definitions...");
            CollectionMgr.LoadMountDefinitions();

            Log.outInfo(LogFilter.ServerLoading, "Loading GM bugs...");
            Global.SupportMgr.LoadBugTickets();

            Log.outInfo(LogFilter.ServerLoading, "Loading GM complaints...");
            Global.SupportMgr.LoadComplaintTickets();

            Log.outInfo(LogFilter.ServerLoading, "Loading GM suggestions...");
            Global.SupportMgr.LoadSuggestionTickets();

            //Log.outInfo(LogFilter.ServerLoading, "Loading GM surveys...");
            //Global.SupportMgr.LoadSurveys();

            Log.outInfo(LogFilter.ServerLoading, "Loading garrison info...");
            Global.GarrisonMgr.Initialize();

            // Handle outdated emails (delete/return)
            Log.outInfo(LogFilter.ServerLoading, "Returning old mails...");
            Global.ObjectMgr.ReturnOrDeleteOldMails(false);

            Log.outInfo(LogFilter.ServerLoading, "Loading Autobroadcasts...");
            LoadAutobroadcasts();

            // Load and initialize scripts
            Global.ObjectMgr.LoadSpellScripts();                              // must be after load Creature/Gameobject(Template/Data)
            Global.ObjectMgr.LoadEventScripts();                              // must be after load Creature/Gameobject(Template/Data)
            Global.ObjectMgr.LoadWaypointScripts();

            Log.outInfo(LogFilter.ServerLoading, "Loading spell script names...");
            Global.ObjectMgr.LoadSpellScriptNames();

            Log.outInfo(LogFilter.ServerLoading, "Loading Creature Texts...");
            Global.CreatureTextMgr.LoadCreatureTexts();

            Log.outInfo(LogFilter.ServerLoading, "Loading Creature Text Locales...");
            Global.CreatureTextMgr.LoadCreatureTextLocales();

            Log.outInfo(LogFilter.ServerLoading, "Initializing Scripts...");
            Global.ScriptMgr.Initialize();
            Global.ScriptMgr.OnConfigLoad(false);                                // must be done after the ScriptMgr has been properly initialized

            Log.outInfo(LogFilter.ServerLoading, "Validating spell scripts...");
            Global.ObjectMgr.ValidateSpellScripts();

            Log.outInfo(LogFilter.ServerLoading, "Loading SmartAI scripts...");
            Global.SmartAIMgr.LoadFromDB();

            Log.outInfo(LogFilter.ServerLoading, "Loading Calendar data...");
            Global.CalendarMgr.LoadFromDB();

            // Initialize game time and timers
            Log.outInfo(LogFilter.ServerLoading, "Initialize game time and timers");
            m_gameTime = Time.UnixTime;
            m_startTime = m_gameTime;

            DB.Login.Execute("INSERT INTO uptime (realmid, starttime, uptime, revision) VALUES({0}, {1}, 0, '{2}')", _realm.Id.Realm, m_startTime, "");       // One-time query

            m_timers[WorldTimers.Auctions].SetInterval(Time.Minute * Time.InMilliseconds);
            m_timers[WorldTimers.AuctionsPending].SetInterval(250);

            //Update "uptime" table based on configuration entry in minutes.
            m_timers[WorldTimers.UpTime].SetInterval(10 * Time.Minute * Time.InMilliseconds);
            //erase corpses every 20 minutes
            m_timers[WorldTimers.Corpses].SetInterval(20 * Time.Minute * Time.InMilliseconds);
            m_timers[WorldTimers.CleanDB].SetInterval(WorldConfig.GetIntValue(WorldCfg.LogdbClearinterval) * Time.Minute * Time.InMilliseconds);
            m_timers[WorldTimers.AutoBroadcast].SetInterval(WorldConfig.GetIntValue(WorldCfg.AutoBroadcastInterval));
            // check for chars to delete every day
            m_timers[WorldTimers.DeleteChars].SetInterval(Time.Day * Time.InMilliseconds);
            // for AhBot
            m_timers[WorldTimers.AhBot].SetInterval(WorldConfig.GetIntValue(WorldCfg.AhbotUpdateInterval) * Time.InMilliseconds); // every 20 sec
            m_timers[WorldTimers.GuildSave].SetInterval(WorldConfig.GetIntValue(WorldCfg.GuildSaveInterval) * Time.Minute * Time.InMilliseconds);

            m_timers[WorldTimers.Blackmarket].SetInterval(10 * Time.InMilliseconds);

            blackmarket_timer = 0;

            //to set mailtimer to return mails every day between 4 and 5 am
            //mailtimer is increased when updating auctions
            //one second is 1000 -(tested on win system)
            // @todo Get rid of magic numbers
            var localTime = Time.UnixTimeToDateTime(m_gameTime).ToLocalTime();
            mail_timer = ((((localTime.Hour + 20) % 24) * Time.Hour * Time.InMilliseconds) / m_timers[WorldTimers.Auctions].GetInterval());
            //1440
            mail_timer_expires = ((Time.Day * Time.InMilliseconds) / (m_timers[(int)WorldTimers.Auctions].GetInterval()));
            Log.outInfo(LogFilter.ServerLoading, "Mail timer set to: {0}, mail return is called every {1} minutes", mail_timer, mail_timer_expires);

            //- Initialize MapManager
            Log.outInfo(LogFilter.ServerLoading, "Starting Map System");
            Global.MapMgr.Initialize();

            Log.outInfo(LogFilter.ServerLoading, "Starting Game Event system...");
            uint nextGameEvent = Global.GameEventMgr.StartSystem();
            m_timers[WorldTimers.Events].SetInterval(nextGameEvent);    //depend on next event

            // Delete all characters which have been deleted X days before
            Player.DeleteOldCharacters();

            // Delete all custom channels which haven't been used for PreserveCustomChannelDuration days.
            Channel.CleanOldChannelsInDB();

            Log.outInfo(LogFilter.ServerLoading, "Initializing Opcodes...");
            PacketManager.Initialize();

            Log.outInfo(LogFilter.ServerLoading, "Starting Arena Season...");
            Global.GameEventMgr.StartArenaSeason();

            Global.SupportMgr.Initialize();

            // Initialize Battlegrounds
            Log.outInfo(LogFilter.ServerLoading, "Starting BattlegroundSystem");
            Global.BattlegroundMgr.LoadBattlegroundTemplates();

            // Initialize outdoor pvp
            Log.outInfo(LogFilter.ServerLoading, "Starting Outdoor PvP System");
            Global.OutdoorPvPMgr.InitOutdoorPvP();

            // Initialize Battlefield
            Log.outInfo(LogFilter.ServerLoading, "Starting Battlefield System");
            Global.BattleFieldMgr.InitBattlefield();

            Log.outInfo(LogFilter.ServerLoading, "Loading Transports...");
            Global.TransportMgr.SpawnContinentTransports();

            // Initialize Warden
            Log.outInfo(LogFilter.ServerLoading, "Loading Warden Checks...");
            Global.WardenCheckMgr.LoadWardenChecks();

            Log.outInfo(LogFilter.ServerLoading, "Loading Warden Action Overrides...");
            Global.WardenCheckMgr.LoadWardenOverrides();

            Log.outInfo(LogFilter.ServerLoading, "Deleting expired bans...");
            DB.Login.Execute("DELETE FROM ip_banned WHERE unbandate <= UNIX_TIMESTAMP() AND unbandate<>bandate");      // One-time query

            Log.outInfo(LogFilter.ServerLoading, "Calculate next daily quest reset time...");
            InitDailyQuestResetTime();

            Log.outInfo(LogFilter.ServerLoading, "Calculate next weekly quest reset time...");
            InitWeeklyQuestResetTime();

            Log.outInfo(LogFilter.ServerLoading, "Calculate next monthly quest reset time...");
            InitMonthlyQuestResetTime();

            Log.outInfo(LogFilter.ServerLoading, "Calculate random Battlegroundreset time...");
            InitRandomBGResetTime();

            Log.outInfo(LogFilter.ServerLoading, "Calculate Guild cap reset time...");
            InitGuildResetTime();

            Log.outInfo(LogFilter.ServerLoading, "Calculate next currency reset time...");
            InitCurrencyResetTime();

            LoadCharacterInfoStorage();

            Log.outInfo(LogFilter.ServerLoading, "Loading race and class expansion requirements...");
            Global.ObjectMgr.LoadRaceAndClassExpansionRequirements();

            Log.outInfo(LogFilter.ServerLoading, "Loading character templates...");
            Global.CharacterTemplateDataStorage.LoadCharacterTemplates();

            Log.outInfo(LogFilter.ServerLoading, "Loading realm names...");
            Global.ObjectMgr.LoadRealmNames();

            Log.outInfo(LogFilter.ServerLoading, "Loading battle pets info...");
            BattlePetMgr.Initialize();

            Log.outInfo(LogFilter.ServerLoading, "Loading scenarios");
            Global.ScenarioMgr.LoadDB2Data();
            Global.ScenarioMgr.LoadDBData();

            Log.outInfo(LogFilter.ServerLoading, "Loading scenario poi data");
            Global.ScenarioMgr.LoadScenarioPOI();

            // Preload all cells, if required for the base maps
            if (WorldConfig.GetBoolValue(WorldCfg.BasemapLoadGrids))
            {
                Global.MapMgr.DoForAllMaps(map =>
                {
                    if (!map.Instanceable())
                    {
                        Log.outInfo(LogFilter.ServerLoading, "Pre-loading base map data for map {0}", map.GetId());
                        map.LoadAllCells();
                    }
                });
            }

            Log.SetRealmId(_realm.Id.Realm);
        }

        public void LoadConfigSettings(bool reload = false)
        {
            WorldConfig.Load(reload);

            m_defaultDbcLocale = (LocaleConstant)ConfigMgr.GetDefaultValue("DBC.Locale", 0);

            if (m_defaultDbcLocale >= LocaleConstant.Total || m_defaultDbcLocale == LocaleConstant.None)
            {
                Log.outError(LogFilter.ServerLoading, "Incorrect DBC.Locale! Must be >= 0 and < {0} and not {1} (set to 0)", LocaleConstant.Total, LocaleConstant.None);
                m_defaultDbcLocale = LocaleConstant.enUS;
            }

            Log.outInfo(LogFilter.ServerLoading, "Using {0} DBC Locale", m_defaultDbcLocale);

            Global.WorldMgr.SetPlayerAmountLimit((uint)ConfigMgr.GetDefaultValue("PlayerLimit", 100));
            Global.WorldMgr.SetMotd(ConfigMgr.GetDefaultValue("Motd", "Welcome to a Cypher Core Server."));

            if (reload)
            {
                Global.SupportMgr.SetSupportSystemStatus(WorldConfig.GetBoolValue(WorldCfg.SupportEnabled));
                Global.SupportMgr.SetTicketSystemStatus(WorldConfig.GetBoolValue(WorldCfg.SupportTicketsEnabled));
                Global.SupportMgr.SetBugSystemStatus(WorldConfig.GetBoolValue(WorldCfg.SupportBugsEnabled));
                Global.SupportMgr.SetComplaintSystemStatus(WorldConfig.GetBoolValue(WorldCfg.SupportComplaintsEnabled));
                Global.SupportMgr.SetSuggestionSystemStatus(WorldConfig.GetBoolValue(WorldCfg.SupportSuggestionsEnabled));

                Global.MapMgr.SetMapUpdateInterval(WorldConfig.GetIntValue(WorldCfg.IntervalMapupdate));
                Global.MapMgr.SetGridCleanUpDelay(WorldConfig.GetUIntValue(WorldCfg.IntervalGridclean));

                m_timers[WorldTimers.UpTime].SetInterval(WorldConfig.GetIntValue(WorldCfg.UptimeUpdate) * Time.Minute * Time.InMilliseconds);
                m_timers[WorldTimers.UpTime].Reset();

                m_timers[WorldTimers.CleanDB].SetInterval(WorldConfig.GetIntValue(WorldCfg.LogdbClearinterval) * Time.Minute * Time.InMilliseconds);
                m_timers[WorldTimers.CleanDB].Reset();


                m_timers[WorldTimers.AutoBroadcast].SetInterval(WorldConfig.GetIntValue(WorldCfg.AutoBroadcastInterval));
                m_timers[WorldTimers.AutoBroadcast].Reset();
            }

            for (byte i = 0; i < (int)UnitMoveType.Max; ++i)
                SharedConst.playerBaseMoveSpeed[i] = SharedConst.baseMoveSpeed[i] * WorldConfig.GetFloatValue(WorldCfg.RateMovespeed);

            var rateCreatureAggro = WorldConfig.GetFloatValue(WorldCfg.RateCreatureAggro);
            //visibility on continents
            m_MaxVisibleDistanceOnContinents = ConfigMgr.GetDefaultValue("Visibility.Distance.Continents", SharedConst.DefaultVisibilityDistance);
            if (m_MaxVisibleDistanceOnContinents < 45 * rateCreatureAggro)
            {
                Log.outError(LogFilter.ServerLoading, "Visibility.Distance.Continents can't be less max aggro radius {0}", 45 * rateCreatureAggro);
                m_MaxVisibleDistanceOnContinents = 45 * rateCreatureAggro;
            }
            else if (m_MaxVisibleDistanceOnContinents > SharedConst.MaxVisibilityDistance)
            {
                Log.outError(LogFilter.ServerLoading, "Visibility.Distance.Continents can't be greater {0}", SharedConst.MaxVisibilityDistance);
                m_MaxVisibleDistanceOnContinents = SharedConst.MaxVisibilityDistance;
            }

            //visibility in instances
            m_MaxVisibleDistanceInInstances = ConfigMgr.GetDefaultValue("Visibility.Distance.Instances", SharedConst.DefaultVisibilityInstance);
            if (m_MaxVisibleDistanceInInstances < 45 * rateCreatureAggro)
            {
                Log.outError(LogFilter.ServerLoading, "Visibility.Distance.Instances can't be less max aggro radius {0}", 45 * rateCreatureAggro);
                m_MaxVisibleDistanceInInstances = 45 * rateCreatureAggro;
            }
            else if (m_MaxVisibleDistanceInInstances > SharedConst.MaxVisibilityDistance)
            {
                Log.outError(LogFilter.ServerLoading, "Visibility.Distance.Instances can't be greater {0}", SharedConst.MaxVisibilityDistance);
                m_MaxVisibleDistanceInInstances = SharedConst.MaxVisibilityDistance;
            }

            //visibility in BG/Arenas
            m_MaxVisibleDistanceInBGArenas = ConfigMgr.GetDefaultValue("Visibility.Distance.BGArenas", SharedConst.DefaultVisibilityBGAreans);
            if (m_MaxVisibleDistanceInBGArenas < 45 * rateCreatureAggro)
            {
                Log.outError(LogFilter.ServerLoading, "Visibility.Distance.BGArenas can't be less max aggro radius {0}", 45 * rateCreatureAggro);
                m_MaxVisibleDistanceInBGArenas = 45 * rateCreatureAggro;
            }
            else if (m_MaxVisibleDistanceInBGArenas > SharedConst.MaxVisibilityDistance)
            {
                Log.outError(LogFilter.ServerLoading, "Visibility.Distance.BGArenas can't be greater {0}", SharedConst.MaxVisibilityDistance);
                m_MaxVisibleDistanceInBGArenas = SharedConst.MaxVisibilityDistance;
            }

            m_visibility_notify_periodOnContinents = ConfigMgr.GetDefaultValue("Visibility.Notify.Period.OnContinents", SharedConst.DefaultVisibilityNotifyPeriod);
            m_visibility_notify_periodInInstances = ConfigMgr.GetDefaultValue("Visibility.Notify.Period.InInstances", SharedConst.DefaultVisibilityNotifyPeriod);
            m_visibility_notify_periodInBGArenas = ConfigMgr.GetDefaultValue("Visibility.Notify.Period.InBGArenas", SharedConst.DefaultVisibilityNotifyPeriod);

            string dataPath = ConfigMgr.GetDefaultValue("DataDir", "./");
            if (reload)
            {
                if (dataPath != _dataPath)
                    Log.outError(LogFilter.ServerLoading, "DataDir option can't be changed at worldserver.conf reload, using current value ({0}).", _dataPath);
            }
            else
            {
                _dataPath = dataPath;
                Log.outInfo(LogFilter.ServerLoading, "Using DataDir {0}", _dataPath);
            }

            Log.outInfo(LogFilter.ServerLoading, @"WORLD: MMap data directory is: {0}\mmaps", _dataPath);

            bool EnableIndoor = ConfigMgr.GetDefaultValue("vmap.EnableIndoorCheck", true);
            bool EnableLOS = ConfigMgr.GetDefaultValue("vmap.EnableLOS", true);
            bool EnableHeight = ConfigMgr.GetDefaultValue("vmap.EnableHeight", true);

            if (!EnableHeight)
                Log.outError(LogFilter.ServerLoading, "VMap height checking Disabled! Creatures movements and other various things WILL be broken! Expect no support.");

            Global.VMapMgr.setEnableLineOfSightCalc(EnableLOS);
            Global.VMapMgr.setEnableHeightCalc(EnableHeight);

            Log.outInfo(LogFilter.ServerLoading, "VMap support included. LineOfSight: {0}, getHeight: {1}, indoorCheck: {2}", EnableLOS, EnableHeight, EnableIndoor);
            Log.outInfo(LogFilter.ServerLoading, @"VMap data directory is: {0}\vmaps", GetDataPath());
        }

        void ResetTimeDiffRecord()
        {
            if (m_updateTimeCount != 1)
                return;

            m_currentTime = Time.GetMSTime();
        }

        void RecordTimeDiff(string text)
        {
            if (m_updateTimeCount != 1)
                return;

            uint thisTime = Time.GetMSTime();
            uint diff = Time.GetMSTimeDiff(m_currentTime, thisTime);

            if (diff > WorldConfig.GetIntValue(WorldCfg.MinLogUpdate))
                Log.outInfo(LogFilter.Server, "Difftime {0}: {1}.", text, diff);

            m_currentTime = thisTime;
        }

        public void LoadAutobroadcasts()
        {
            uint oldMSTime = Time.GetMSTime();

            m_Autobroadcasts.Clear();

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_AUTOBROADCAST);
            stmt.AddValue(0, _realm.Id.Realm);

            SQLResult result = DB.Login.Query(stmt);
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 autobroadcasts definitions. DB table `autobroadcast` is empty for this realm!");
                return;
            }

            do
            {
                byte id = result.Read<byte>(0);

                m_Autobroadcasts[id] = new Autobroadcast(result.Read<string>(2), result.Read<byte>(1));

            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} autobroadcast definitions in {1} ms", m_Autobroadcasts.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void Update(uint diff)
        {
            m_updateTime = diff;

            if (diff > 100)
            {
                if (m_updateTimeSum > 60000)
                {
                    Log.outDebug(LogFilter.Server, "Update time diff: {0}. Players online: {1}.", m_updateTimeSum / m_updateTimeCount, GetActiveSessionCount());
                    m_updateTimeSum = m_updateTime;
                    m_updateTimeCount = 1;
                }
                else
                {
                    m_updateTimeSum += m_updateTime;
                    ++m_updateTimeCount;
                }
            }

            // Update the different timers
            for (WorldTimers i = 0; i < WorldTimers.Max; ++i)
            {
                if (m_timers[i].GetCurrent() >= 0)
                    m_timers[i].Update(diff);
                else
                    m_timers[i].SetCurrent(0);
            }

            _UpdateGameTime();

            // Handle daily quests reset time
            if (m_gameTime > m_NextDailyQuestReset)
            {
                DailyReset();
                InitDailyQuestResetTime(false);
            }

            // Handle weekly quests reset time
            if (m_gameTime > m_NextWeeklyQuestReset)
                ResetWeeklyQuests();

            // Handle monthly quests reset time
            if (m_gameTime > m_NextMonthlyQuestReset)
                ResetMonthlyQuests();

            if (m_gameTime > m_NextRandomBGReset)
                ResetRandomBG();

            if (m_gameTime > m_NextGuildReset)
                ResetGuildCap();

            if (m_gameTime > m_NextCurrencyReset)
                ResetCurrencyWeekCap();

            //Handle auctions when the timer has passed
            if (m_timers[WorldTimers.Auctions].Passed())
            {
                m_timers[WorldTimers.Auctions].Reset();

                // Update mails (return old mails with item, or delete them)
                if (++mail_timer > mail_timer_expires)
                {
                    mail_timer = 0;
                    Global.ObjectMgr.ReturnOrDeleteOldMails(true);
                }

                // Handle expired auctions
                Global.AuctionMgr.Update();
            }

            if (m_timers[WorldTimers.AuctionsPending].Passed())
            {
                m_timers[WorldTimers.AuctionsPending].Reset();

                //Global.AuctionMgr.UpdatePendingAuctions();
            }

            if (m_timers[WorldTimers.Blackmarket].Passed())
            {
                m_timers[WorldTimers.Blackmarket].Reset();

                //- Update blackmarket, refresh auctions if necessary
                if ((blackmarket_timer * m_timers[WorldTimers.Blackmarket].GetInterval() >= WorldConfig.GetIntValue(WorldCfg.BlackmarketUpdatePeriod) * Time.Hour * Time.InMilliseconds) || blackmarket_timer == 0)
                {
                    Global.BlackMarketMgr.RefreshAuctions();
                    blackmarket_timer = 1; // timer is 0 on startup
                }
                else
                {
                    ++blackmarket_timer;
                    Global.BlackMarketMgr.Update();
                }
            }

            //Handle session updates when the timer has passed
            ResetTimeDiffRecord();
            UpdateSessions(diff);
            RecordTimeDiff("UpdateSessions");

            // <li> Update uptime table
            if (m_timers[WorldTimers.UpTime].Passed())
            {
                uint tmpDiff = (uint)(m_gameTime - m_startTime);
                uint maxOnlinePlayers = GetMaxPlayerCount();

                m_timers[WorldTimers.UpTime].Reset();

                PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_UPTIME_PLAYERS);

                stmt.AddValue(0, tmpDiff);
                stmt.AddValue(1, maxOnlinePlayers);
                stmt.AddValue(2, _realm.Id.Realm);
                stmt.AddValue(3, m_startTime);

                DB.Login.Execute(stmt);
            }

            // <li> Clean logs table
            if (WorldConfig.GetIntValue(WorldCfg.LogdbCleartime) > 0) // if not enabled, ignore the timer
            {
                if (m_timers[WorldTimers.CleanDB].Passed())
                {
                    m_timers[WorldTimers.CleanDB].Reset();

                    PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_OLD_LOGS);
                    stmt.AddValue(0, WorldConfig.GetIntValue(WorldCfg.LogdbCleartime));
                    stmt.AddValue(1, 0);
                    stmt.AddValue(2, GetRealm().Id.Realm);

                    DB.Login.Execute(stmt);
                }
            }

            ResetTimeDiffRecord();
            Global.MapMgr.Update(diff);
            RecordTimeDiff("UpdateMapMgr");

            if (WorldConfig.GetBoolValue(WorldCfg.AutoBroadcast))
            {
                if (m_timers[WorldTimers.AutoBroadcast].Passed())
                {
                    m_timers[WorldTimers.AutoBroadcast].Reset();
                    SendAutoBroadcast();
                }
            }

            Global.BattlegroundMgr.Update(diff);
            RecordTimeDiff("UpdateBattlegroundMgr");

            Global.OutdoorPvPMgr.Update(diff);
            RecordTimeDiff("UpdateOutdoorPvPMgr");

            Global.BattleFieldMgr.Update(diff);
            RecordTimeDiff("BattlefieldMgr");

            //- Delete all characters which have been deleted X days before
            if (m_timers[WorldTimers.DeleteChars].Passed())
            {
                m_timers[WorldTimers.DeleteChars].Reset();
                Player.DeleteOldCharacters();
            }

            Global.LFGMgr.Update(diff);
            RecordTimeDiff("UpdateLFGMgr");

            Global.GroupMgr.Update(diff);
            RecordTimeDiff("GroupMgr");

            // execute callbacks from sql queries that were queued recently
            ProcessQueryCallbacks();
            RecordTimeDiff("ProcessQueryCallbacks");

            // Erase corpses once every 20 minutes
            if (m_timers[WorldTimers.Corpses].Passed())
            {
                m_timers[WorldTimers.Corpses].Reset();
                Global.MapMgr.DoForAllMaps(map => map.RemoveOldCorpses());
            }

            // Process Game events when necessary
            if (m_timers[WorldTimers.Events].Passed())
            {
                m_timers[WorldTimers.Events].Reset();                   // to give time for Update() to be processed
                uint nextGameEvent = Global.GameEventMgr.Update();
                m_timers[WorldTimers.Events].SetInterval(nextGameEvent);
                m_timers[WorldTimers.Events].Reset();
            }

            if (m_timers[WorldTimers.GuildSave].Passed())
            {
                m_timers[WorldTimers.GuildSave].Reset();
                Global.GuildMgr.SaveGuilds();
            }

            // update the instance reset times
            Global.InstanceSaveMgr.Update();

            Global.ScriptMgr.OnWorldUpdate(diff);
        }

        public void ForceGameEventUpdate()
        {
            m_timers[WorldTimers.Events].Reset();                   // to give time for Update() to be processed
            uint nextGameEvent = Global.GameEventMgr.Update();
            m_timers[WorldTimers.Events].SetInterval(nextGameEvent);
            m_timers[WorldTimers.Events].Reset();
        }

        public void SendGlobalMessage(ServerPacket packet, WorldSession self = null, Team team = 0)
        {
            foreach (var session in m_sessions.Values)
            {
                if (session.GetPlayer() != null && session.GetPlayer().IsInWorld && session != self &&
                    (team == 0 || session.GetPlayer().GetTeam() == team))
                {
                    session.SendPacket(packet);
                }
            }
        }

        public void SendGlobalGMMessage(ServerPacket packet, WorldSession self = null, Team team = 0)
        {
            foreach (var session in m_sessions.Values)
            {
                // check if session and can receive global GM Messages and its not self
                if (session == null || session == self || !session.HasPermission(RBACPermissions.ReceiveGlobalGmTextmessage))
                    continue;

                // Player should be in world
                Player player = session.GetPlayer();
                if (player == null || !player.IsInWorld)
                    continue;

                // Send only to same team, if team is given
                if (team == 0 || player.GetTeam() == team)
                    session.SendPacket(packet);
            }
        }

        // Send a System Message to all players (except self if mentioned)
        public void SendWorldText(CypherStrings string_id, params object[] args)
        {
            WorldWorldTextBuilder wt_builder = new WorldWorldTextBuilder((uint)string_id, args);
            var wt_do = new LocalizedPacketListDo(wt_builder);
            foreach (var session in m_sessions.Values)
            {
                if (session == null || !session.GetPlayer() || !session.GetPlayer().IsInWorld)
                    continue;

                wt_do.Invoke(session.GetPlayer());
            }
        }

        // Send a System Message to all GMs (except self if mentioned)
        public void SendGMText(CypherStrings string_id, params object[] args)
        {
            var wt_builder = new WorldWorldTextBuilder((uint)string_id, args);
            var wt_do = new LocalizedPacketListDo(wt_builder);
            foreach (var session in m_sessions.Values)
            {
                // Session should have permissions to receive global gm messages
                if (session == null || !session.HasPermission(RBACPermissions.ReceiveGlobalGmTextmessage))
                    continue;

                // Player should be in world
                Player player = session.GetPlayer();
                if (!player || !player.IsInWorld)
                    continue;

                wt_do.Invoke(player);
            }
        }

        // Send a packet to all players (or players selected team) in the zone (except self if mentioned)
        public bool SendZoneMessage(uint zone, ServerPacket packet, WorldSession self = null, uint team = 0)
        {
            bool foundPlayerToSend = false;
            foreach (var session in m_sessions.Values)
            {
                if (session != null && session.GetPlayer() && session.GetPlayer().IsInWorld &&
                    session.GetPlayer().GetZoneId() == zone && session != self && (team == 0 || (uint)session.GetPlayer().GetTeam() == team))
                {
                    session.SendPacket(packet);
                    foundPlayerToSend = true;
                }
            }

            return foundPlayerToSend;
        }

        // Send a System Message to all players in the zone (except self if mentioned)
        public void SendZoneText(uint zone, string text, WorldSession self = null, uint team = 0)
        {
            ChatPkt data = new ChatPkt();
            data.Initialize(ChatMsg.System, Language.Universal, null, null, text);
            SendZoneMessage(zone, data, self, team);
        }

        public void KickAll()
        {
            m_QueuedPlayer.Clear();                                 // prevent send queue update packet and login queued sessions

            // session not removed at kick and will removed in next update tick
            foreach (var session in m_sessions.Values)
                session.KickPlayer();
        }

        void KickAllLess(AccountTypes sec)
        {
            // session not removed at kick and will removed in next update tick
            foreach (var session in m_sessions.Values)
                if (session.GetSecurity() < sec)
                    session.KickPlayer();
        }

        /// Ban an account or ban an IP address, duration will be parsed using TimeStringToSecs if it is positive, otherwise permban
        public BanReturn BanAccount(BanMode mode, string nameOrIP, string duration, string reason, string author)
        {
            uint duration_secs = Time.TimeStringToSecs(duration);
            return BanAccount(mode, nameOrIP, duration_secs, reason, author);
        }

        /// Ban an account or ban an IP address, duration is in seconds if positive, otherwise permban
        public BanReturn BanAccount(BanMode mode, string nameOrIP, uint duration_secs, string reason, string author)
        {
            // Prevent banning an already banned account
            if (mode == BanMode.Account && Global.AccountMgr.IsBannedAccount(nameOrIP))
                return BanReturn.Exists;

            SQLResult resultAccounts;
            PreparedStatement stmt = null;

            // Update the database with ban information
            switch (mode)
            {
                case BanMode.IP:
                    // No SQL injection with prepared statements
                    stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BY_IP);
                    stmt.AddValue(0, nameOrIP);
                    resultAccounts = DB.Login.Query(stmt);
                    stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_IP_BANNED);
                    stmt.AddValue(0, nameOrIP);
                    stmt.AddValue(1, duration_secs);
                    stmt.AddValue(2, author);
                    stmt.AddValue(3, reason);
                    DB.Login.Execute(stmt);
                    break;
                case BanMode.Account:
                    // No SQL injection with prepared statements
                    stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_ID_BY_NAME);
                    stmt.AddValue(0, nameOrIP);
                    resultAccounts = DB.Login.Query(stmt);
                    break;
                case BanMode.Character:
                    // No SQL injection with prepared statements
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ACCOUNT_BY_NAME);
                    stmt.AddValue(0, nameOrIP);
                    resultAccounts = DB.Characters.Query(stmt);
                    break;
                default:
                    return BanReturn.SyntaxError;
            }

            if (resultAccounts == null)
            {
                if (mode == BanMode.IP)
                    return BanReturn.Success;                             // ip correctly banned but nobody affected (yet)
                else
                    return BanReturn.Notfound;                            // Nobody to ban
            }

            // Disconnect all affected players (for IP it can be several)
            SQLTransaction trans = new SQLTransaction();
            do
            {
                uint account = resultAccounts.Read<uint>(0);

                if (mode != BanMode.IP)
                {
                    // make sure there is only one active ban
                    stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_NOT_BANNED);
                    stmt.AddValue(0, account);
                    trans.Append(stmt);
                    // No SQL injection with prepared statements
                    stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_ACCOUNT_BANNED);
                    stmt.AddValue(0, account);
                    stmt.AddValue(1, duration_secs);
                    stmt.AddValue(2, author);
                    stmt.AddValue(3, reason);
                    trans.Append(stmt);
                }

                WorldSession sess = FindSession(account);
                if (sess)
                {
                    if (sess.GetPlayerName() != author)
                        sess.KickPlayer();
                }
            } while (resultAccounts.NextRow());

            DB.Login.CommitTransaction(trans);

            return BanReturn.Success;
        }

        /// Remove a ban from an account or IP address
        public bool RemoveBanAccount(BanMode mode, string nameOrIP)
        {
            PreparedStatement stmt = null;
            if (mode == BanMode.IP)
            {
                stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_IP_NOT_BANNED);
                stmt.AddValue(0, nameOrIP);
                DB.Login.Execute(stmt);
            }
            else
            {
                uint account = 0;
                if (mode == BanMode.Account)
                    account = Global.AccountMgr.GetId(nameOrIP);
                else if (mode == BanMode.Character)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ACCOUNT_BY_NAME);
                    stmt.AddValue(0, nameOrIP);

                    SQLResult result = DB.Characters.Query(stmt);
                    if (!result.IsEmpty())
                        account = result.Read<uint>(0);
                }

                if (account == 0)
                    return false;

                //NO SQL injection as account is uint32
                stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_NOT_BANNED);
                stmt.AddValue(0, account);
                DB.Login.Execute(stmt);
            }
            return true;
        }

        /// Ban an account or ban an IP address, duration will be parsed using TimeStringToSecs if it is positive, otherwise permban
        public BanReturn BanCharacter(string name, string duration, string reason, string author)
        {
            Player pBanned = Global.ObjAccessor.FindConnectedPlayerByName(name);
            ObjectGuid guid;

            uint duration_secs = Time.TimeStringToSecs(duration);

            // Pick a player to ban if not online
            if (!pBanned)
            {
                guid = ObjectManager.GetPlayerGUIDByName(name);
                if (guid.IsEmpty())
                    return BanReturn.Notfound;                                    // Nobody to ban
            }
            else
                guid = pBanned.GetGUID();

            //Use transaction in order to ensure the order of the queries
            SQLTransaction trans = new SQLTransaction();

            // make sure there is only one active ban
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER_BAN);
            stmt.AddValue(0, guid.GetCounter());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_BAN);
            stmt.AddValue(0, guid.GetCounter());
            stmt.AddValue(1, duration_secs);
            stmt.AddValue(2, author);
            stmt.AddValue(3, reason);
            trans.Append(stmt);
            DB.Characters.CommitTransaction(trans);

            if (pBanned)
                pBanned.GetSession().KickPlayer();

            return BanReturn.Success;
        }

        // Remove a ban from a character
        public bool RemoveBanCharacter(string name)
        {
            Player pBanned = Global.ObjAccessor.FindConnectedPlayerByName(name);
            ObjectGuid guid;

            // Pick a player to ban if not online
            if (!pBanned)
            {
                guid = ObjectManager.GetPlayerGUIDByName(name);
                if (guid.IsEmpty())
                    return false;                                    // Nobody to ban
            }
            else
                guid = pBanned.GetGUID();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER_BAN);
            stmt.AddValue(0, guid.GetCounter());
            DB.Characters.Execute(stmt);
            return true;
        }

        void _UpdateGameTime()
        {
            // update the time
            long thisTime = Time.UnixTime;
            uint elapsed = (uint)(thisTime - m_gameTime);
            m_gameTime = thisTime;

            //- if there is a shutdown timer
            if (!IsStopped && m_ShutdownTimer > 0 && elapsed > 0)
            {
                //- ... and it is overdue, stop the world
                if (m_ShutdownTimer <= elapsed)
                {
                    if (!m_ShutdownMask.HasAnyFlag(ShutdownMask.Idle) || GetActiveAndQueuedSessionCount() == 0)
                        IsStopped = true;                         // exist code already set
                    else
                        m_ShutdownTimer = 1;                        // minimum timer value to wait idle state
                }
                //- ... else decrease it and if necessary display a shutdown countdown to the users
                else
                {
                    m_ShutdownTimer -= elapsed;

                    ShutdownMsg();
                }
            }
        }

        public void ShutdownServ(uint time, ShutdownMask options, ShutdownExitCode exitcode, string reason = "")
        {
            // ignore if server shutdown at next tick
            if (IsStopped)
                return;

            m_ShutdownMask = options;
            m_ExitCode = exitcode;

            // If the shutdown time is 0, set m_stopEvent (except if shutdown is 'idle' with remaining sessions)
            if (time == 0)
            {
                if (!options.HasAnyFlag(ShutdownMask.Idle) || GetActiveAndQueuedSessionCount() == 0)
                    IsStopped = true;                             // exist code already set
                else
                    m_ShutdownTimer = 1;                            //So that the session count is re-evaluated at next world tick
            }
            // Else set the shutdown timer and warn users
            else
            {
                m_ShutdownTimer = time;
                ShutdownMsg(true, null, reason);
            }

            Global.ScriptMgr.OnShutdownInitiate(exitcode, options);
        }

        public void ShutdownMsg(bool show = false, Player player = null, string reason = "")
        {
            // not show messages for idle shutdown mode
            if (m_ShutdownMask.HasAnyFlag(ShutdownMask.Idle))
                return;

            // Display a message every 12 hours, hours, 5 minutes, minute, 5 seconds and finally seconds
            if (show ||
                (m_ShutdownTimer < 5 * Time.Minute && (m_ShutdownTimer % 15) == 0) || // < 5 min; every 15 sec
                (m_ShutdownTimer < 15 * Time.Minute && (m_ShutdownTimer % Time.Minute) == 0) || // < 15 min ; every 1 min
                (m_ShutdownTimer < 30 * Time.Minute && (m_ShutdownTimer % (5 * Time.Minute)) == 0) || // < 30 min ; every 5 min
                (m_ShutdownTimer < 12 * Time.Hour && (m_ShutdownTimer % Time.Hour) == 0) || // < 12 h ; every 1 h
                (m_ShutdownTimer > 12 * Time.Hour && (m_ShutdownTimer % (12 * Time.Hour)) == 0)) // > 12 h ; every 12 h
            {
                var str = Time.secsToTimeString(m_ShutdownTimer);
                if (!reason.IsEmpty())
                    str += " - " + reason;

                ServerMessageType msgid = m_ShutdownMask.HasAnyFlag(ShutdownMask.Restart) ? ServerMessageType.RestartTime : ServerMessageType.ShutdownTime;

                SendServerMessage(msgid, str, player);
                Log.outDebug(LogFilter.Server, "Server is {0} in {1}", (m_ShutdownMask.HasAnyFlag(ShutdownMask.Restart) ? "restart" : "shuttingdown"), str);
            }
        }

        public uint ShutdownCancel()
        {
            // nothing cancel or too later
            if (m_ShutdownTimer == 0 || IsStopped)
                return 0;

            ServerMessageType msgid = m_ShutdownMask.HasAnyFlag(ShutdownMask.Restart) ? ServerMessageType.RestartCancelled : ServerMessageType.ShutdownCancelled;

            uint oldTimer = m_ShutdownTimer;
            m_ShutdownMask = 0;
            m_ShutdownTimer = 0;
            m_ExitCode = (byte)ShutdownExitCode.Shutdown;                       // to default value
            SendServerMessage(msgid);

            Log.outDebug(LogFilter.Server, "Server {0} cancelled.", (m_ShutdownMask.HasAnyFlag(ShutdownMask.Restart) ? "restart" : "shutdown"));

            Global.ScriptMgr.OnShutdownCancel();
            return oldTimer;
        }

        public void SendServerMessage(ServerMessageType messageID, string stringParam = "", Player player = null)
        {
            ChatServerMessage packet = new ChatServerMessage();
            packet.MessageID = (int)messageID;
            if (messageID <= ServerMessageType.String)
                packet.StringParam = stringParam;

            if (player)
                player.SendPacket(packet);
            else
                SendGlobalMessage(packet);
        }

        public void UpdateSessions(uint diff)
        {
            Tuple<WorldSocket, ulong> linkInfo;
            while (_linkSocketQueue.TryDequeue(out linkInfo))
                ProcessLinkInstanceSocket(linkInfo);

            // Add new sessions
            WorldSession sess;
            while (addSessQueue.TryDequeue(out sess))
                AddSession_(sess);

            // Then send an update signal to remaining ones
            foreach (var pair in m_sessions)
            {
                WorldSession session = pair.Value;
                WorldSessionFilter updater = new WorldSessionFilter(session);
                if (!session.Update(diff, updater))    // As interval = 0
                {
                    if (!RemoveQueuedPlayer(session) && session != null && WorldConfig.GetIntValue(WorldCfg.IntervalDisconnectTolerance) != 0)
                        m_disconnects[session.GetAccountId()] = Time.UnixTime;

                    RemoveQueuedPlayer(session);
                    m_sessions.TryRemove(pair.Key, out WorldSession tempSession);
                    session.Dispose();
                }
            }
        }

        void SendAutoBroadcast()
        {
            if (m_Autobroadcasts.Empty())
                return;

            var pair = m_Autobroadcasts.SelectRandomElementByWeight(autoPair => autoPair.Value.Weight);

            uint abcenter = WorldConfig.GetUIntValue(WorldCfg.AutoBroadcastCenter);

            if (abcenter == 0)
                SendWorldText(CypherStrings.AutoBroadcast, pair.Value.Message);
            else if (abcenter == 1)
                SendGlobalMessage(new PrintNotification(pair.Value.Message));
            else if (abcenter == 2)
            {
                SendWorldText(CypherStrings.AutoBroadcast, pair.Value.Message);
                SendGlobalMessage(new PrintNotification(pair.Value.Message));
            }

            Log.outDebug(LogFilter.Misc, "AutoBroadcast: '{0}'", pair.Value.Message);
        }

        public void UpdateRealmCharCount(uint accountId)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_COUNT);
            stmt.AddValue(0, accountId);
            _queryProcessor.AddQuery(DB.Characters.AsyncQuery(stmt).WithCallback(_UpdateRealmCharCount));
        }

        void _UpdateRealmCharCount(SQLResult result)
        { 
            if (!result.IsEmpty())
            {
                uint Id = result.Read<uint>(0);
                uint charCount = result.Read<uint>(1);

                SQLTransaction trans = new SQLTransaction();

                PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_REALM_CHARACTERS_BY_REALM);
                stmt.AddValue(0, Id);
                stmt.AddValue(1, _realm.Id.Realm);
                trans.Append(stmt);

                stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_REALM_CHARACTERS);
                stmt.AddValue(0, charCount);
                stmt.AddValue(1, Id);
                stmt.AddValue(2, _realm.Id.Realm);
                trans.Append(stmt);

                DB.Login.CommitTransaction(trans);
            }
        }

        void InitWeeklyQuestResetTime()
        {
            long wstime = getWorldState(WorldStates.WeeklyQuestResetTime);
            long curtime = Time.UnixTime;
            m_NextWeeklyQuestReset = wstime < curtime ? curtime : wstime;
        }

        void InitDailyQuestResetTime(bool loading = true)
        {
            long mostRecentQuestTime = 0;

            if (loading)
            {
                SQLResult result = DB.Characters.Query("SELECT MAX(time) FROM character_queststatus_daily");
                if (!result.IsEmpty())
                {
                    mostRecentQuestTime = result.Read<uint>(0);
                }
            }


            // FIX ME: client not show day start time
            long curTime = Time.UnixTime;

            // current day reset time
            long curDayResetTime = Time.GetNextResetUnixTime(WorldConfig.GetIntValue(WorldCfg.DailyQuestResetTimeHour));

            // last reset time before current moment
            long resetTime = (curTime < curDayResetTime) ? curDayResetTime - Time.Day : curDayResetTime;

            // need reset (if we have quest time before last reset time (not processed by some reason)
            if (mostRecentQuestTime != 0 && mostRecentQuestTime <= resetTime)
                m_NextDailyQuestReset = mostRecentQuestTime;
            else // plan next reset time
                m_NextDailyQuestReset = (curTime >= curDayResetTime) ? curDayResetTime + Time.Day : curDayResetTime;
        }

        void InitMonthlyQuestResetTime()
        {
            long wstime = getWorldState(WorldStates.MonthlyQuestResetTime);
            long curtime = Time.UnixTime;
            m_NextMonthlyQuestReset = wstime < curtime ? curtime : wstime;
        }

        void InitRandomBGResetTime()
        {
            long bgtime = getWorldState(WorldStates.BGDailyResetTime);
            if (bgtime == 0)
                m_NextRandomBGReset = Time.UnixTime;         // game time not yet init

            // generate time by config
            long curTime = Time.UnixTime;

            // current day reset time
            long nextDayResetTime = Time.GetNextResetUnixTime(WorldConfig.GetIntValue(WorldCfg.RandomBgResetHour));

            // next reset time before current moment
            if (curTime >= nextDayResetTime)
                nextDayResetTime += Time.Day;

            // normalize reset time
            m_NextRandomBGReset = bgtime < curTime ? nextDayResetTime - Time.Day : nextDayResetTime;

            if (bgtime == 0)
                setWorldState(WorldStates.BGDailyResetTime, (ulong)m_NextRandomBGReset);
        }

        void InitGuildResetTime()
        {
            long gtime = getWorldState(WorldStates.GuildDailyResetTime);
            if (gtime == 0)
                m_NextGuildReset = Time.UnixTime;         // game time not yet init

            long curTime = Time.UnixTime;
            var nextDayResetTime = Time.GetNextResetUnixTime(WorldConfig.GetIntValue(WorldCfg.GuildResetHour));

            if (curTime >= nextDayResetTime)
                nextDayResetTime += Time.Day;

            // normalize reset time
            m_NextGuildReset = gtime < curTime ? nextDayResetTime - Time.Day : nextDayResetTime;

            if (gtime == 0)
                setWorldState(WorldStates.GuildDailyResetTime, (ulong)m_NextGuildReset);
        }

        void InitCurrencyResetTime()
        {
            long currencytime = getWorldState(WorldStates.CurrencyResetTime);
            if (currencytime == 0)
                m_NextCurrencyReset = Time.UnixTime;         // game time not yet init

            // generate time by config
            long curTime = Time.UnixTime;

            var nextWeekResetTime = Time.GetNextResetUnixTime(WorldConfig.GetIntValue(WorldCfg.CurrencyResetDay), WorldConfig.GetIntValue(WorldCfg.CurrencyResetHour));

            // next reset time before current moment
            if (curTime >= nextWeekResetTime)
                nextWeekResetTime += WorldConfig.GetIntValue(WorldCfg.CurrencyResetInterval) * Time.Day;

            // normalize reset time
            m_NextCurrencyReset = currencytime < curTime ? nextWeekResetTime - WorldConfig.GetIntValue(WorldCfg.CurrencyResetInterval) * Time.Day : nextWeekResetTime;

            if (currencytime == 0)
                setWorldState(WorldStates.CurrencyResetTime, (ulong)m_NextCurrencyReset);
        }

        void DailyReset()
        {
            Log.outInfo(LogFilter.Server, "Daily quests reset for all characters.");

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_RESET_CHARACTER_QUESTSTATUS_DAILY);
            DB.Characters.Execute(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER_GARRISON_FOLLOWER_ACTIVATIONS);
            stmt.AddValue(0, 1);
            DB.Characters.Execute(stmt);

            foreach (var session in m_sessions.Values)
                if (session.GetPlayer() != null)
                    session.GetPlayer().DailyReset();

            // change available dailies
            Global.PoolMgr.ChangeDailyQuests();
        }

        void ResetCurrencyWeekCap()
        {
            DB.Characters.Execute("UPDATE `character_currency` SET `WeeklyQuantity` = 0");

            foreach (var session in m_sessions.Values)
                if (session.GetPlayer() != null)
                    session.GetPlayer().ResetCurrencyWeekCap();

            m_NextCurrencyReset = m_NextCurrencyReset + Time.Day * WorldConfig.GetIntValue(WorldCfg.CurrencyResetInterval);
            setWorldState(WorldStates.CurrencyResetTime, (ulong)m_NextCurrencyReset);
        }

        public void LoadDBAllowedSecurityLevel()
        {
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_REALMLIST_SECURITY_LEVEL);
            stmt.AddValue(0, _realm.Id.Realm);
            SQLResult result = DB.Login.Query(stmt);

            if (!result.IsEmpty())
                SetPlayerSecurityLimit((AccountTypes)result.Read<byte>(0));
        }

        public void SetPlayerSecurityLimit(AccountTypes accountType)
        {
            AccountTypes security = accountType < AccountTypes.Console ? accountType : AccountTypes.Player;
            bool update = security > m_allowedSecurityLevel;
            m_allowedSecurityLevel = security;
            if (update)
                KickAllLess(m_allowedSecurityLevel);
        }

        void ResetWeeklyQuests()
        {
            Log.outInfo(LogFilter.Server, "Weekly quests reset for all characters.");

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_RESET_CHARACTER_QUESTSTATUS_WEEKLY);
            DB.Characters.Execute(stmt);

            foreach (var session in m_sessions.Values)
                if (session.GetPlayer() != null)
                    session.GetPlayer().ResetWeeklyQuestStatus();

            m_NextWeeklyQuestReset = (m_NextWeeklyQuestReset + Time.Week);
            setWorldState(WorldStates.WeeklyQuestResetTime, (ulong)m_NextWeeklyQuestReset);

            // change available weeklies
            Global.PoolMgr.ChangeWeeklyQuests();
        }

        void ResetMonthlyQuests()
        {
            Log.outInfo(LogFilter.Server, "Monthly quests reset for all characters.");

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_RESET_CHARACTER_QUESTSTATUS_MONTHLY);
            DB.Characters.Execute(stmt);

            foreach (var session in m_sessions.Values)
                if (session.GetPlayer() != null)
                    session.GetPlayer().ResetMonthlyQuestStatus();

            long curTime = Time.UnixTime;

            // current day reset time
            long curDayResetTime = Time.GetNextResetUnixTime(30, 1, 0);

            // last reset time before current moment
            long nextMonthResetTime = (curTime < curDayResetTime) ? curDayResetTime - Time.Day : curDayResetTime;

            // plan next reset time
            m_NextMonthlyQuestReset = (curTime >= nextMonthResetTime) ? nextMonthResetTime + Time.Month : nextMonthResetTime;

            setWorldState(WorldStates.MonthlyQuestResetTime, (ulong)m_NextMonthlyQuestReset);
        }

        public void ResetEventSeasonalQuests(ushort event_id)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_RESET_CHARACTER_QUESTSTATUS_SEASONAL_BY_EVENT);
            stmt.AddValue(0, event_id);
            DB.Characters.Execute(stmt);

            foreach (var session in m_sessions.Values)
                if (session.GetPlayer())
                    session.GetPlayer().ResetSeasonalQuestStatus(event_id);
        }

        void ResetRandomBG()
        {
            Log.outInfo(LogFilter.Server, "Random BG status reset for all characters.");

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_BATTLEGROUND_RANDOM_ALL);
            DB.Characters.Execute(stmt);

            foreach (var session in m_sessions.Values)
                if (session.GetPlayer())
                    session.GetPlayer().SetRandomWinner(false);

            m_NextRandomBGReset = m_NextRandomBGReset + Time.Day;
            setWorldState(WorldStates.BGDailyResetTime, (ulong)m_NextRandomBGReset);
        }

        void ResetGuildCap()
        {
            m_NextGuildReset = (m_NextGuildReset + Time.Day);
            setWorldState(WorldStates.GuildDailyResetTime, (ulong)m_NextGuildReset);
            ulong week = getWorldState(WorldStates.GuildWeeklyResetTime);
            week = week < 7 ? week + 1 : 1;

            Log.outInfo(LogFilter.Server, "Guild Daily Cap reset. Week: {0}", week == 1);
            setWorldState(WorldStates.GuildWeeklyResetTime, week);
            Global.GuildMgr.ResetTimes(week == 1);
        }

        void UpdateMaxSessionCounters()
        {
            m_maxActiveSessionCount = Math.Max(m_maxActiveSessionCount, (uint)(m_sessions.Count - m_QueuedPlayer.Count));
            m_maxQueuedSessionCount = Math.Max(m_maxQueuedSessionCount, (uint)m_QueuedPlayer.Count);
        }

        public string LoadDBVersion()
        {
            var DBVersion = "Unknown world database.";

            SQLResult result = DB.World.Query("SELECT db_version, cache_id, hotfix_cache_id FROM version LIMIT 1");
            if (!result.IsEmpty())
            {
                DBVersion = result.Read<string>(0);
                // will be overwrite by config values if different and non-0
                WorldConfig.SetValue(WorldCfg.ClientCacheVersion, result.Read<uint>(1));
                WorldConfig.SetValue(WorldCfg.HotfixCacheVersion, result.Read<uint>(2));
            }

            return DBVersion;
        }

        void UpdateAreaDependentAuras()
        {
            foreach (var session in m_sessions.Values)
            {
                if (session.GetPlayer() != null && session.GetPlayer().IsInWorld)
                {
                    session.GetPlayer().UpdateAreaDependentAuras(session.GetPlayer().GetAreaId());
                    session.GetPlayer().UpdateZoneDependentAuras(session.GetPlayer().GetZoneId());
                }
            }
        }

        void LoadWorldStates()
        {
            uint oldMSTime = Time.GetMSTime();

            SQLResult result = DB.Characters.Query("SELECT entry, value FROM worldstates");

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 world states. DB table `worldstates` is empty!");
                return;
            }

            uint count = 0;
            do
            {
                m_worldstates[result.Read<uint>(0)] = result.Read<uint>(1);
                ++count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} world states in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void setWorldState(WorldStates index, ulong value)
        {
            setWorldState((uint)index, value);
        }

        public void setWorldState(uint index, object value)
        {
            PreparedStatement stmt;

            if (m_worldstates.ContainsKey(index))
            {
                if (m_worldstates[index] == Convert.ToUInt32(value))
                    return;

                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_WORLDSTATE);
                stmt.AddValue(0, Convert.ToUInt32(value));
                stmt.AddValue(1, index);
            }
            else
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_WORLDSTATE);

                stmt.AddValue(0, index);
                stmt.AddValue(1, Convert.ToUInt32(value));
            }
            DB.Characters.Execute(stmt);
            m_worldstates[index] = Convert.ToUInt32(value);
        }

        public uint getWorldState(WorldStates index)
        {
            return getWorldState((uint)index);
        }

        public uint getWorldState(uint index)
        {
            return m_worldstates.LookupByKey(index);
        }

        void ProcessQueryCallbacks()
        {
            _queryProcessor.ProcessReadyQueries();
        }

        public CharacterInfo GetCharacterInfo(ObjectGuid guid) { return _characterInfoStorage.LookupByKey(guid); }

        public void LoadCharacterInfoStorage()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading character name data");

            _characterInfoStorage.Clear();

            SQLResult result = DB.Characters.Query("SELECT guid, name, account, race, gender, class, level, deleteDate FROM characters");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "No character name data loaded, empty query");
                return;
            }

            uint count = 0;
            do
            {
                AddCharacterInfo(ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0)), result.Read<uint>(2), result.Read<string>(1),
                    result.Read<byte>(4), result.Read<byte>(3), result.Read<byte>(5), result.Read<byte>(6), result.Read<uint>(7) != 0);
                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded name data for {0} characters", count);
        }

        public void AddCharacterInfo(ObjectGuid guid, uint accountId, string name, byte gender, byte race, byte playerClass, byte level, bool isDeleted)
        {
            CharacterInfo data = new CharacterInfo();
            data.Name = name;
            data.AccountId = accountId;
            data.RaceID = (Race)race;
            data.Sex = (Gender)gender;
            data.ClassID = (Class)playerClass;
            data.Level = level;
            data.IsDeleted = isDeleted;
            _characterInfoStorage[guid] = data;
        }

        public void DeleteCharacterInfo(ObjectGuid guid) { _characterInfoStorage.Remove(guid); }

        public void UpdateCharacterInfo(ObjectGuid guid, string name, Gender gender = Gender.None, Race race = Race.None)
        {
            if (!_characterInfoStorage.ContainsKey(guid))
                return;

            var charData = _characterInfoStorage[guid];
            charData.Name = name;

            if (gender != Gender.None)
                charData.Sex = gender;

            if (race != Race.None)
                charData.RaceID = race;

            InvalidatePlayer data = new InvalidatePlayer();
            data.Guid = guid;
            SendGlobalMessage(data);
        }

        public void UpdateCharacterInfoLevel(ObjectGuid guid, byte level)
        {
            if (!_characterInfoStorage.ContainsKey(guid))
                return;

            _characterInfoStorage[guid].Level = level;
        }

        public void UpdateCharacterInfoAccount(ObjectGuid guid, uint accountId)
        {
            if (!_characterInfoStorage.ContainsKey(guid))
                return;

            _characterInfoStorage[guid].AccountId = accountId;
        }

        public void UpdateCharacterInfoDeleted(ObjectGuid guid, bool deleted, string name = null)
        {
            if (!_characterInfoStorage.ContainsKey(guid))
                return;

            _characterInfoStorage[guid].IsDeleted = deleted;

            if (!string.IsNullOrEmpty(name))
                _characterInfoStorage[guid].Name = name;
        }

        public void ReloadRBAC()
        {
            // Passive reload, we mark the data as invalidated and next time a permission is checked it will be reloaded
            Log.outInfo(LogFilter.Rbac, "World.ReloadRBAC()");
            foreach (var session in m_sessions.Values)
                session.InvalidateRBACData();
        }

        public bool HasCharacterInfo(ObjectGuid guid) { return _characterInfoStorage.ContainsKey(guid); }

        public List<WorldSession> GetAllSessions()
        {
            return m_sessions.Values.ToList();
        }

        public int GetActiveAndQueuedSessionCount() { return m_sessions.Count; }
        public int GetActiveSessionCount() { return m_sessions.Count - m_QueuedPlayer.Count; }
        public int GetQueuedSessionCount() { return m_QueuedPlayer.Count; }
        // Get the maximum number of parallel sessions on the server since last reboot
        public uint GetMaxQueuedSessionCount() { return m_maxQueuedSessionCount; }
        public uint GetMaxActiveSessionCount() { return m_maxActiveSessionCount; }

        public uint GetPlayerCount() { return m_PlayerCount; }
        public uint GetMaxPlayerCount() { return m_MaxPlayerCount; }

        public void IncreasePlayerCount()
        {
            m_PlayerCount++;
            m_MaxPlayerCount = Math.Max(m_MaxPlayerCount, m_PlayerCount);
        }
        public void DecreasePlayerCount() { m_PlayerCount--; }

        public AccountTypes GetPlayerSecurityLimit() { return m_allowedSecurityLevel; }

        public void SetPlayerAmountLimit(uint limit) { m_playerLimit = limit; }
        public uint GetPlayerAmountLimit() { return m_playerLimit; }

        /// Get the path where data (dbc, maps) are stored on disk
        public string GetDataPath() { return _dataPath; }

        public void SetDataPath(string path) { _dataPath = path; }

        // When server started?
        long GetStartTime() { return m_startTime; }
        // What time is it?
        public long GetGameTime() { return m_gameTime; }
        // Uptime (in secs)
        public uint GetUptime() { return (uint)(m_gameTime - m_startTime); }
        // Update time
        public uint GetUpdateTime() { return m_updateTime; }

        public long GetNextDailyQuestsResetTime() { return m_NextDailyQuestReset; }
        public long GetNextWeeklyQuestsResetTime() { return m_NextWeeklyQuestReset; }
        long GetNextRandomBGResetTime() { return m_NextRandomBGReset; }

        public uint GetConfigMaxSkillValue()
        {
            int lvl = WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel);
            return (uint)(lvl > 60 ? 300 + ((lvl - 60) * 75) / 10 : lvl * 5);
        }

        public bool IsShuttingDown() { return m_ShutdownTimer > 0; }
        public uint GetShutDownTimeLeft() { return m_ShutdownTimer; }

        public void StopNow(ShutdownExitCode exitcode = ShutdownExitCode.Error) { IsStopped = true; m_ExitCode = exitcode; }

        public bool IsPvPRealm()
        {
            RealmType realmtype = (RealmType)WorldConfig.GetIntValue(WorldCfg.GameType);
            return (realmtype == RealmType.PVP
                || realmtype == RealmType.RPPVP
                || realmtype == RealmType.FFAPVP);
        }
        public bool IsFFAPvPRealm()
        {
            return WorldConfig.GetIntValue(WorldCfg.GameType) == (int)RealmType.FFAPVP;
        }

        public LocaleConstant GetDefaultDbcLocale() { return m_defaultDbcLocale; }

        public bool LoadRealmInfo()
        {
            SQLResult result = DB.Login.Query("SELECT id, name, address, localAddress, localSubnetMask, port, icon, flag, timezone, allowedSecurityLevel, population, gamebuild, Region, Battlegroup FROM realmlist WHERE id = {0}", _realm.Id.Realm);
            if (result.IsEmpty())
                return false;

            _realm.SetName(result.Read<string>(1));
            _realm.ExternalAddress = System.Net.IPAddress.Parse(result.Read<string>(2));
            _realm.LocalAddress = System.Net.IPAddress.Parse(result.Read<string>(3));
            _realm.LocalSubnetMask = System.Net.IPAddress.Parse(result.Read<string>(4));
            _realm.Port = result.Read<ushort>(5);
            _realm.Type = result.Read<byte>(6);
            _realm.Flags = (RealmFlags)result.Read<byte>(7);
            _realm.Timezone = result.Read<byte>(8);
            _realm.AllowedSecurityLevel = (AccountTypes)result.Read<byte>(9);
            _realm.PopulationLevel = result.Read<float>(10);
            _realm.Id.Region = result.Read<byte>(12);
            _realm.Id.Site = result.Read<byte>(13);
            _realm.Build = result.Read<uint>(11);
            return true;
        }

        public Realm GetRealm() { return _realm; }
        public RealmHandle GetRealmId() { return _realm.Id; }

        public void RemoveOldCorpses()
        {
            m_timers[WorldTimers.Corpses].SetCurrent(m_timers[WorldTimers.Corpses].GetInterval());
        }

        public uint GetVirtualRealmAddress()
        {
            return _realm.Id.GetAddress();
        }

        public float GetMaxVisibleDistanceOnContinents() { return m_MaxVisibleDistanceOnContinents; }
        public float GetMaxVisibleDistanceInInstances() { return m_MaxVisibleDistanceInInstances; }
        public float GetMaxVisibleDistanceInBGArenas() { return m_MaxVisibleDistanceInBGArenas; }

        public int GetVisibilityNotifyPeriodOnContinents() { return m_visibility_notify_periodOnContinents; }
        public int GetVisibilityNotifyPeriodInInstances() { return m_visibility_notify_periodInInstances; }
        public int GetVisibilityNotifyPeriodInBGArenas() { return m_visibility_notify_periodInBGArenas; }

        public LocaleConstant GetAvailableDbcLocale(LocaleConstant locale)
        {
            //if (m_availableDbcLocaleMask & (1 << locale))
                //return locale;
            //else
                return m_defaultDbcLocale;
        }

        public CleaningFlags GetCleaningFlags() { return m_CleaningFlags; }
        public void SetCleaningFlags(CleaningFlags flags) { m_CleaningFlags = flags; }

        #region Fields
        uint m_ShutdownTimer;
        ShutdownMask m_ShutdownMask;
        ShutdownExitCode m_ExitCode;
        public bool IsStopped;

        Dictionary<byte, Autobroadcast> m_Autobroadcasts = new Dictionary<byte, Autobroadcast>();

        Dictionary<ObjectGuid, CharacterInfo> _characterInfoStorage = new Dictionary<ObjectGuid, CharacterInfo>();

        CleaningFlags m_CleaningFlags;

        float m_MaxVisibleDistanceOnContinents = SharedConst.DefaultVisibilityDistance;
        float m_MaxVisibleDistanceInInstances = SharedConst.DefaultVisibilityInstance;
        float m_MaxVisibleDistanceInBGArenas = SharedConst.DefaultVisibilityBGAreans;

        int m_visibility_notify_periodOnContinents = SharedConst.DefaultVisibilityNotifyPeriod;
        int m_visibility_notify_periodInInstances = SharedConst.DefaultVisibilityNotifyPeriod;
        int m_visibility_notify_periodInBGArenas = SharedConst.DefaultVisibilityNotifyPeriod;

        bool m_isClosed;

        long m_startTime;
        long m_gameTime;
        Dictionary<WorldTimers, IntervalTimer> m_timers = new Dictionary<WorldTimers, IntervalTimer>();
        long mail_timer;
        long mail_timer_expires;
        long blackmarket_timer;
        uint m_updateTime, m_updateTimeSum;
        uint m_updateTimeCount;
        uint m_currentTime;

        ConcurrentDictionary<uint, WorldSession> m_sessions = new ConcurrentDictionary<uint, WorldSession>();
        Dictionary<uint, long> m_disconnects = new Dictionary<uint, long>();
        uint m_maxActiveSessionCount;
        uint m_maxQueuedSessionCount;
        uint m_PlayerCount;
        uint m_MaxPlayerCount;

        Dictionary<uint, uint> m_worldstates = new Dictionary<uint, uint>();
        uint m_playerLimit;
        AccountTypes m_allowedSecurityLevel;
        LocaleConstant m_defaultDbcLocale;                     // from config for one from loaded DBC locales
        List<string> m_motd = new List<string>();

        // scheduled reset times
        long m_NextDailyQuestReset;
        long m_NextWeeklyQuestReset;
        long m_NextMonthlyQuestReset;
        long m_NextRandomBGReset;
        long m_NextGuildReset;
        long m_NextCurrencyReset;

        List<WorldSession> m_QueuedPlayer = new List<WorldSession>();
        ConcurrentQueue<WorldSession> addSessQueue = new ConcurrentQueue<WorldSession>();

        ConcurrentQueue<Tuple<WorldSocket, ulong>> _linkSocketQueue = new ConcurrentQueue<Tuple<WorldSocket, ulong>>();

        QueryCallbackProcessor _queryProcessor = new QueryCallbackProcessor();

        Realm _realm;

        string _dataPath;
        #endregion
    }

    /// Timers for different object refresh rates
    public enum WorldTimers
    {
        Auctions,
        AuctionsPending,
        UpTime,
        Corpses,
        Events,
        CleanDB,
        AutoBroadcast,
        MailBox,
        DeleteChars,
        AhBot,
        PingDB,
        GuildSave,
        Blackmarket,
        Max
    }

    public enum ServerMessageType
    {
        ShutdownTime = 1,
        RestartTime = 2,
        String = 3,
        ShutdownCancelled = 4,
        RestartCancelled = 5,
        BgShutdownTime = 6,
        BgRestartTime = 7,
        InstanceShutdownTime = 8,
        InstanceRestartTime = 9,
        ContentReady = 10,
        TicketServicedSoon = 11,
        WaitTimeUnavailable = 12,
        TicketWaitTime = 13,
    }

    [Flags]
    public enum ShutdownMask
    {
        Restart = 1,
        Idle = 2,
        Force = 4
    }

    public enum ShutdownExitCode
    {
        Shutdown = 0,
        Error = 1,
        Restart = 2,
    }

    public class CharacterInfo
    {
        public string Name;
        public uint AccountId;
        public Class ClassID;
        public Race RaceID;
        public Gender Sex;
        public byte Level;
        public bool IsDeleted;
    }

    public class WorldWorldTextBuilder : MessageBuilder
    {        
        public WorldWorldTextBuilder(uint textId, params object[] args)
        {
            i_textId = textId;
            i_args = args;
        }

        public override void Invoke(List<ServerPacket> data_list, LocaleConstant loc_idx)
        {
            string text = Global.ObjectMgr.GetCypherString(i_textId, loc_idx);

            if (i_args != null)
                text = string.Format(text, i_args);

            ChatPkt messageChat = new ChatPkt();

            var lines = new StringArray(text, "\n");
            for (var i = 0; i < lines.Length; ++i)
            {
                messageChat.Initialize(ChatMsg.System, Language.Universal, null, null, lines[i]);
                data_list.Add(messageChat);
            }
        }

        uint i_textId;
        object[] i_args;
    }

    struct Autobroadcast
    {
        public Autobroadcast(string message, byte weight)
        {
            Message = message;
            Weight = weight;
        }

        public string Message;
        public byte Weight;
    }
}
