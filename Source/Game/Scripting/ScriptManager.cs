// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.AI;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.Chat;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Guilds;
using Game.Maps;
using Game.Movement;
using Game.PvP;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ICreature;
using Game.Scripting.Interfaces.IFormula;
using Game.Scripting.Interfaces.IGameObject;
using Game.Scripting.Interfaces.IItem;
using Game.Scripting.Interfaces.IWorld;
using Game.Scripting.Interfaces.IWorldState;
using Game.Spells;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;

namespace Game.Scripting
{
    // Manages registration, loading, and execution of Scripts.
    public class ScriptManager : Singleton<ScriptManager>
    {
        ScriptManager() { }

        //Initialization
        public void Initialize()
        {
            uint oldMSTime = Time.GetMSTime();

            LoadDatabase();

            Log.outInfo(LogFilter.ServerLoading, "Loading C# scripts");

            FillSpellSummary();

            //Load Scripts.dll
            LoadScripts();

            // MapScripts
            Global.MapMgr.AddSC_BuiltInScripts();

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {GetScriptCount()} C# scripts in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        #region Loading and Unloading
        public void LoadScripts()
        {
            if (!File.Exists(AppContext.BaseDirectory + "Scripts.dll"))
            {
                Log.outError(LogFilter.ServerLoading, "Cant find Scripts.dll, Only Core Scripts are loaded.");
                return;
            }

            Assembly assembly = Assembly.LoadFile(AppContext.BaseDirectory + "Scripts.dll");
            if (assembly == null)
            {
                Log.outError(LogFilter.ServerLoading, "Error Loading Scripts.dll, Only Core Scripts are loaded.");
                return;
            }

            foreach (var type in assembly.GetTypes())
            {
                var attributes = (ScriptAttribute[])type.GetCustomAttributes<ScriptAttribute>();
                if (!attributes.Empty())
                {
                    var constructors = type.GetConstructors();
                    if (constructors.Length == 0)
                    {
                        Log.outError(LogFilter.Scripts, "Script: {0} contains no Public Constructors. Can't load script.", type.Name);
                        continue;
                    }

                    foreach (var attribute in attributes)
                    {
                        var genericType = type;
                        string name = type.Name;

                        bool validArgs = true;
                        int i = 0;

                        foreach (var constructor in constructors)
                        {
                            var parameters = constructor.GetParameters();
                            if (parameters.Length != attribute.Args.Length)
                                continue;

                            foreach (var arg in constructor.GetParameters())
                            {
                                if (arg.ParameterType != attribute.Args[i++].GetType())
                                {
                                    validArgs = false;
                                    break;
                                }
                            }

                            if (validArgs)
                                break;
                        }

                        if (!validArgs)
                        {
                            Log.outError(LogFilter.Scripts, "Script: {0} contains no Public Constructors with the right parameter types. Can't load script.", type.Name);
                            continue;
                        }

                        if (!attribute.Name.IsEmpty())
                            name = attribute.Name;

                        name = name.Replace("_SpellScript", "");
                        name = name.Replace("_AuraScript", "");

                        if (attribute.Args.Empty())
                            Activator.CreateInstance(genericType);
                        else
                            Activator.CreateInstance(genericType, new object[] { name }.Combine(attribute.Args));

                        if (attribute is SpellScriptAttribute spellScript && spellScript.SpellIds != null)
                            foreach(var id in spellScript.SpellIds)
                                Global.ObjectMgr.RegisterSpellScript(id, name);

                    }
                }
            }
        }

        public void LoadDatabase()
        {
            LoadScriptWaypoints();
            LoadScriptSplineChains();
        }

        void LoadScriptWaypoints()
        {
            uint oldMSTime = Time.GetMSTime();

            // Drop Existing Waypoint list
            _waypointStore.Clear();

            ulong entryCount = 0;

            // Load Waypoints
            SQLResult result = DB.World.Query("SELECT COUNT(entry) FROM script_waypoint GROUP BY entry");
            if (!result.IsEmpty())
                entryCount = result.Read<uint>(0);

            Log.outInfo(LogFilter.ServerLoading, $"Loading Script Waypoints for {entryCount} creature(s)...");

            //                                0       1         2           3           4           5
            result = DB.World.Query("SELECT entry, pointid, location_x, location_y, location_z, waittime FROM script_waypoint ORDER BY pointid");

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Script Waypoints. DB table `script_waypoint` is empty.");
                return;
            }

            uint count = 0;

            do
            {
                uint entry = result.Read<uint>(0);
                uint id = result.Read<uint>(1);
                float x = result.Read<float>(2);
                float y = result.Read<float>(3);
                float z = result.Read<float>(4);
                uint waitTime = result.Read<uint>(5);

                CreatureTemplate info = Global.ObjectMgr.GetCreatureTemplate(entry);
                if (info == null)
                {
                    Log.outError(LogFilter.Sql, $"SystemMgr: DB table script_waypoint has waypoint for non-existant creature entry {entry}");
                    continue;
                }

                if (info.ScriptID == 0)
                    Log.outError(LogFilter.Sql, $"SystemMgr: DB table script_waypoint has waypoint for creature entry {entry}, but creature does not have ScriptName defined and then useless.");

                if (!_waypointStore.ContainsKey(entry))
                    _waypointStore[entry] = new WaypointPath();

                WaypointPath path = _waypointStore[entry];
                path.id = entry;
                path.nodes.Add(new WaypointNode(id, x, y, z, null, waitTime));

                ++count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} Script Waypoint nodes in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));

        }

        void LoadScriptSplineChains()
        {
            uint oldMSTime = Time.GetMSTime();

            m_mSplineChainsMap.Clear();

            //                                             0      1        2         3                 4            5
            SQLResult resultMeta = DB.World.Query("SELECT entry, chainId, splineId, expectedDuration, msUntilNext, velocity FROM script_spline_chain_meta ORDER BY entry asc, chainId asc, splineId asc");
            //                                           0      1        2         3    4  5  6
            SQLResult resultWP = DB.World.Query("SELECT entry, chainId, splineId, wpId, x, y, z FROM script_spline_chain_waypoints ORDER BY entry asc, chainId asc, splineId asc, wpId asc");
            if (resultMeta.IsEmpty() || resultWP.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded spline chain data for 0 chains, consisting of 0 splines with 0 waypoints. DB tables `script_spline_chain_meta` and `script_spline_chain_waypoints` are empty.");
            }
            else
            {
                uint chainCount = 0, splineCount = 0, wpCount = 0;
                do
                {
                    uint entry = resultMeta.Read<uint>(0);
                    ushort chainId = resultMeta.Read<ushort>(1);
                    byte splineId = resultMeta.Read<byte>(2);

                    var key = Tuple.Create(entry, chainId);
                    if (!m_mSplineChainsMap.ContainsKey(key))
                        m_mSplineChainsMap[key] = new List<SplineChainLink>();

                    var chain = m_mSplineChainsMap[Tuple.Create(entry, chainId)];
                    if (splineId != chain.Count)
                    {
                        Log.outWarn(LogFilter.ServerLoading, "Creature #{0}: Chain {1} has orphaned spline {2}, skipped.", entry, chainId, splineId);
                        continue;
                    }

                    uint expectedDuration = resultMeta.Read<uint>(3);
                    uint msUntilNext = resultMeta.Read<uint>(4);
                    float velocity = resultMeta.Read<float>(5);
                    chain.Add(new SplineChainLink(expectedDuration, msUntilNext, velocity));

                    if (splineId == 0)
                        ++chainCount;
                    ++splineCount;
                } while (resultMeta.NextRow());

                do
                {
                    uint entry = resultWP.Read<uint>(0);
                    ushort chainId = resultWP.Read<ushort>(1);
                    byte splineId = resultWP.Read<byte>(2);
                    byte wpId = resultWP.Read<byte>(3);
                    float posX = resultWP.Read<float>(4);
                    float posY = resultWP.Read<float>(5);
                    float posZ = resultWP.Read<float>(6);
                    var chain = m_mSplineChainsMap.LookupByKey(Tuple.Create(entry, chainId));
                    if (chain == null)
                    {
                        Log.outWarn(LogFilter.ServerLoading, "Creature #{0} has waypoint data for spline chain {1}. No such chain exists - entry skipped.", entry, chainId);
                        continue;
                    }

                    if (splineId >= chain.Count)
                    {
                        Log.outWarn(LogFilter.ServerLoading, "Creature #{0} has waypoint data for spline ({1},{2}). The specified chain does not have a spline with this index - entry skipped.", entry, chainId, splineId);
                        continue;
                    }
                    SplineChainLink spline = chain[splineId];
                    if (wpId != spline.Points.Count)
                    {
                        Log.outWarn(LogFilter.ServerLoading, "Creature #{0} has orphaned waypoint data in spline ({1},{2}) at index {3}. Skipped.", entry, chainId, splineId, wpId);
                        continue;
                    }
                    spline.Points.Add(new Vector3(posX, posY, posZ));
                    ++wpCount;
                } while (resultWP.NextRow());

                Log.outInfo(LogFilter.ServerLoading, "Loaded spline chain data for {0} chains, consisting of {1} splines with {2} waypoints in {3} ms", chainCount, splineCount, wpCount, Time.GetMSTimeDiffToNow(oldMSTime));
            }
        }

        public void FillSpellSummary()
        {
            UnitAI.FillAISpellInfo();
        }

        public WaypointPath GetPath(uint creatureEntry)
        {
            return _waypointStore.LookupByKey(creatureEntry);
        }
        
        public List<SplineChainLink> GetSplineChain(Creature who, ushort chainId)
        {
            return GetSplineChain(who.GetEntry(), chainId);
        }

        List<SplineChainLink> GetSplineChain(uint entry, ushort chainId)
        {
            return m_mSplineChainsMap.LookupByKey(Tuple.Create(entry, chainId));
        }

        public string ScriptsVersion() { return "Integrated Cypher Scripts"; }

        public void IncrementScriptCount() { ++_ScriptCount; }
        public uint GetScriptCount() { return _ScriptCount; }

        //Reloading
        public void Reload()
        {

        }

        //Unloading
        public void Unload()
        {
            foreach (var entry in _scriptStorage)
            {
                ScriptRegistry scriptRegistry = entry.Value;
                scriptRegistry.Unload();
            }

            _scriptStorage.Clear();
        }
        #endregion

        #region Spells and Auras
        //SpellScriptLoader
        public List<SpellScript> CreateSpellScripts(uint spellId, Spell invoker)
        {
            var scriptList = new List<SpellScript>();
            var bounds = Global.ObjectMgr.GetSpellScriptsBounds(spellId);

            var reg = GetScriptRegistry<SpellScriptLoader>();
            if (reg == null)
                return scriptList;

            foreach (var id in bounds)
            {
                var tmpscript = reg.GetScriptById<SpellScriptLoader>(id);
                if (tmpscript == null)
                    continue;

                SpellScript script = tmpscript.GetSpellScript();
                if (script == null)
                    continue;

                script._Init(tmpscript.GetName(), spellId);
                if (!script._Load(invoker))
                    continue;

                scriptList.Add(script);
            }

            return scriptList;
        }
        public List<AuraScript> CreateAuraScripts(uint spellId, Aura invoker)
        {
            var scriptList = new List<AuraScript>();
            var bounds = Global.ObjectMgr.GetSpellScriptsBounds(spellId);

            var reg = GetScriptRegistry<AuraScriptLoader>();
            if (reg == null)
                return scriptList;

            foreach (var id in bounds)
            {
                var tmpscript = reg.GetScriptById<AuraScriptLoader>(id);
                if (tmpscript == null)
                    continue;

                AuraScript script = tmpscript.GetAuraScript();
                if (script == null)
                    continue;

                script._Init(tmpscript.GetName(), spellId);
                if (!script._Load(invoker))
                    continue;

                scriptList.Add(script);
            }

            return scriptList;
        }
        public Dictionary<SpellScriptLoader, uint> CreateSpellScriptLoaders(uint spellId)
        {
            var scriptDic = new Dictionary<SpellScriptLoader, uint>();
            var bounds = Global.ObjectMgr.GetSpellScriptsBounds(spellId);

            var reg = GetScriptRegistry<SpellScriptLoader>();
            if (reg == null)
                return scriptDic;

            foreach (var id in bounds)
            {
                var tmpscript = reg.GetScriptById<SpellScriptLoader>(id);
                if (tmpscript == null)
                    continue;

                scriptDic[tmpscript] = id;
            }

            return scriptDic;
        }
        public Dictionary<AuraScriptLoader, uint> CreateAuraScriptLoaders(uint spellId)
        {
            var scriptDic = new Dictionary<AuraScriptLoader, uint>();
            var bounds = Global.ObjectMgr.GetSpellScriptsBounds(spellId);

            var reg = GetScriptRegistry<AuraScriptLoader>();
            if (reg == null)
                return scriptDic;

            foreach (var id in bounds)
            {
                var tmpscript = reg.GetScriptById<AuraScriptLoader>(id);
                if (tmpscript == null)
                    continue;

                scriptDic[tmpscript] = id;
            }

            return scriptDic;
        }
        #endregion

        //AreaTriggerScript
        public bool OnAreaTrigger(Player player, AreaTriggerRecord trigger, bool entered)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(trigger != null);

            return RunScriptRet<AreaTriggerScript>(p => entered ? p.OnTrigger(player, trigger) : p.OnExit(player, trigger), Global.ObjectMgr.GetAreaTriggerScriptId(trigger.Id));
        }

        //BattlefieldScript
        public BattleField CreateBattlefield(uint scriptId, Map map)
        {
            return RunScriptRet<BattlefieldScript, BattleField>(p => p.GetBattlefield(map), scriptId, null);
        }

        //BattlegroundScript
        public Battleground CreateBattleground(BattlegroundTypeId typeId)
        {
            // @todo Implement script-side Battlegrounds.
            Cypher.Assert(false);
            return null;
        }

        // OutdoorPvPScript
        public OutdoorPvP CreateOutdoorPvP(uint scriptId, Map map)
        {
            return RunScriptRet<OutdoorPvPScript, OutdoorPvP>(p => p.GetOutdoorPvP(map), scriptId, null);
        }

        // WeatherScript
        public void OnWeatherChange(Weather weather, WeatherState state, float grade)
        {
            Cypher.Assert(weather != null);
            RunScript<WeatherScript>(p => p.OnChange(weather, state, grade), weather.GetScriptId());
        }
        public void OnWeatherUpdate(Weather weather, uint diff)
        {
            Cypher.Assert(weather != null);
            RunScript<WeatherScript>(p => p.OnUpdate(weather, diff), weather.GetScriptId());
        }

        // AuctionHouseScript
        public void OnAuctionAdd(AuctionHouseObject ah, AuctionPosting auction)
        {
            Cypher.Assert(ah != null);
            Cypher.Assert(auction != null);
            ForEach<AuctionHouseScript>(p => p.OnAuctionAdd(ah, auction));
        }
        public void OnAuctionRemove(AuctionHouseObject ah, AuctionPosting auction)
        {
            Cypher.Assert(ah != null);
            Cypher.Assert(auction != null);
            ForEach<AuctionHouseScript>(p => p.OnAuctionRemove(ah, auction));
        }
        public void OnAuctionSuccessful(AuctionHouseObject ah, AuctionPosting auction)
        {
            Cypher.Assert(ah != null);
            Cypher.Assert(auction != null);
            ForEach<AuctionHouseScript>(p => p.OnAuctionSuccessful(ah, auction));
        }
        public void OnAuctionExpire(AuctionHouseObject ah, AuctionPosting auction)
        {
            Cypher.Assert(ah != null);
            Cypher.Assert(auction != null);
            ForEach<AuctionHouseScript>(p => p.OnAuctionExpire(ah, auction));
        }

        // ConditionScript
        public bool OnConditionCheck(Condition condition, ConditionSourceInfo sourceInfo)
        {
            Cypher.Assert(condition != null);

            return RunScriptRet<ConditionScript>(p => p.OnConditionCheck(condition, sourceInfo), condition.ScriptId, true);
        }

        // VehicleScript
        public void OnInstall(Vehicle veh)
        {
            Cypher.Assert(veh != null);
            Cypher.Assert(veh.GetBase().IsTypeId(TypeId.Unit));

            RunScript<VehicleScript>(p => p.OnInstall(veh), veh.GetBase().ToCreature().GetScriptId());
        }
        public void OnUninstall(Vehicle veh)
        {
            Cypher.Assert(veh != null);
            Cypher.Assert(veh.GetBase().IsTypeId(TypeId.Unit));

            RunScript<VehicleScript>(p => p.OnUninstall(veh), veh.GetBase().ToCreature().GetScriptId());
        }
        public void OnReset(Vehicle veh)
        {
            Cypher.Assert(veh != null);
            Cypher.Assert(veh.GetBase().IsTypeId(TypeId.Unit));

            RunScript<VehicleScript>(p => p.OnReset(veh), veh.GetBase().ToCreature().GetScriptId());
        }
        public void OnInstallAccessory(Vehicle veh, Creature accessory)
        {
            Cypher.Assert(veh != null);
            Cypher.Assert(veh.GetBase().IsTypeId(TypeId.Unit));
            Cypher.Assert(accessory != null);

            RunScript<VehicleScript>(p => p.OnInstallAccessory(veh, accessory), veh.GetBase().ToCreature().GetScriptId());
        }
        public void OnAddPassenger(Vehicle veh, Unit passenger, sbyte seatId)
        {
            Cypher.Assert(veh != null);
            Cypher.Assert(veh.GetBase().IsTypeId(TypeId.Unit));
            Cypher.Assert(passenger != null);

            RunScript<VehicleScript>(p => p.OnAddPassenger(veh, passenger, seatId), veh.GetBase().ToCreature().GetScriptId());
        }
        public void OnRemovePassenger(Vehicle veh, Unit passenger)
        {
            Cypher.Assert(veh != null);
            Cypher.Assert(veh.GetBase().IsTypeId(TypeId.Unit));
            Cypher.Assert(passenger != null);

            RunScript<VehicleScript>(p => p.OnRemovePassenger(veh, passenger), veh.GetBase().ToCreature().GetScriptId());
        }

        // DynamicObjectScript
        public void OnDynamicObjectUpdate(DynamicObject dynobj, uint diff)
        {
            Cypher.Assert(dynobj != null);

            ForEach<DynamicObjectScript>(p => p.OnUpdate(dynobj, diff));
        }

        // TransportScript
        public void OnAddPassenger(Transport transport, Player player)
        {
            Cypher.Assert(transport != null);
            Cypher.Assert(player != null);

            RunScript<TransportScript>(p => p.OnAddPassenger(transport, player), transport.GetScriptId());
        }
        public void OnAddCreaturePassenger(Transport transport, Creature creature)
        {
            Cypher.Assert(transport != null);
            Cypher.Assert(creature != null);

            RunScript<TransportScript>(p => p.OnAddCreaturePassenger(transport, creature), transport.GetScriptId());
        }
        public void OnRemovePassenger(Transport transport, Player player)
        {
            Cypher.Assert(transport != null);
            Cypher.Assert(player != null);

            RunScript<TransportScript>(p => p.OnRemovePassenger(transport, player), transport.GetScriptId());
        }
        public void OnTransportUpdate(Transport transport, uint diff)
        {
            Cypher.Assert(transport != null);

            RunScript<TransportScript>(p => p.OnUpdate(transport, diff), transport.GetScriptId());
        }
        public void OnRelocate(Transport transport, uint mapId, float x, float y, float z)
        {
            RunScript<TransportScript>(p => p.OnRelocate(transport, mapId, x, y, z), transport.GetScriptId());
        }

        // Achievement
        public void OnAchievementCompleted(Player player, AchievementRecord achievement)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(achievement != null);

            RunScript<AchievementScript>(p => p.OnCompleted(player, achievement), Global.AchievementMgr.GetAchievementScriptId(achievement.Id));
        }
        
        // AchievementCriteriaScript
        public bool OnCriteriaCheck(uint ScriptId, Player source, Unit target)
        {
            Cypher.Assert(source != null);
            // target can be NULL.

            return RunScriptRet<AchievementCriteriaScript>(p => p.OnCheck(source, target), ScriptId);
        }

        // PlayerScript
        public void OnPVPKill(Player killer, Player killed)
        {
            ForEach<PlayerScript>(p => p.OnPVPKill(killer, killed));
        }
        public void OnCreatureKill(Player killer, Creature killed)
        {
            ForEach<PlayerScript>(p => p.OnCreatureKill(killer, killed));
        }
        public void OnPlayerKilledByCreature(Creature killer, Player killed)
        {
            ForEach<PlayerScript>(p => p.OnPlayerKilledByCreature(killer, killed));
        }
        public void OnPlayerLevelChanged(Player player, byte oldLevel)
        {
            ForEach<PlayerScript>(p => p.OnLevelChanged(player, oldLevel));
        }
        public void OnPlayerFreeTalentPointsChanged(Player player, uint newPoints)
        {
            ForEach<PlayerScript>(p => p.OnFreeTalentPointsChanged(player, newPoints));
        }
        public void OnPlayerTalentsReset(Player player, bool noCost)
        {
            ForEach<PlayerScript>(p => p.OnTalentsReset(player, noCost));
        }
        public void OnPlayerMoneyChanged(Player player, long amount)
        {
            ForEach<PlayerScript>(p => p.OnMoneyChanged(player, amount));
        }
        public void OnGivePlayerXP(Player player, uint amount, Unit victim)
        {
            ForEach<PlayerScript>(p => p.OnGiveXP(player, amount, victim));
        }
        public void OnPlayerReputationChange(Player player, uint factionID, int standing, bool incremental)
        {
            ForEach<PlayerScript>(p => p.OnReputationChange(player, factionID, standing, incremental));
        }
        public void OnPlayerDuelRequest(Player target, Player challenger)
        {
            ForEach<PlayerScript>(p => p.OnDuelRequest(target, challenger));
        }
        public void OnPlayerDuelStart(Player player1, Player player2)
        {
            ForEach<PlayerScript>(p => p.OnDuelStart(player1, player2));
        }
        public void OnPlayerDuelEnd(Player winner, Player loser, DuelCompleteType type)
        {
            ForEach<PlayerScript>(p => p.OnDuelEnd(winner, loser, type));
        }
        public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg)
        {
            ForEach<PlayerScript>(p => p.OnChat(player, type, lang, msg));
        }
        public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg, Player receiver)
        {
            ForEach<PlayerScript>(p => p.OnChat(player, type, lang, msg, receiver));
        }
        public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg, Group group)
        {
            ForEach<PlayerScript>(p => p.OnChat(player, type, lang, msg, group));
        }
        public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg, Guild guild)
        {
            ForEach<PlayerScript>(p => p.OnChat(player, type, lang, msg, guild));
        }
        public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg, Channel channel)
        {
            ForEach<PlayerScript>(p => p.OnChat(player, type, lang, msg, channel));
        }
        public void OnPlayerClearEmote(Player player)
        {
            ForEach<PlayerScript>(p => p.OnClearEmote(player));
        }
        public void OnPlayerTextEmote(Player player, uint textEmote, uint emoteNum, ObjectGuid guid)
        {
            ForEach<PlayerScript>(p => p.OnTextEmote(player, textEmote, emoteNum, guid));
        }
        public void OnPlayerSpellCast(Player player, Spell spell, bool skipCheck)
        {
            ForEach<PlayerScript>(p => p.OnSpellCast(player, spell, skipCheck));
        }
        public void OnPlayerLogin(Player player)
        {
            ForEach<PlayerScript>(p => p.OnLogin(player));
        }
        public void OnPlayerLogout(Player player)
        {
            ForEach<PlayerScript>(p => p.OnLogout(player));
        }
        public void OnPlayerCreate(Player player)
        {
            ForEach<PlayerScript>(p => p.OnCreate(player));
        }
        public void OnPlayerDelete(ObjectGuid guid, uint accountId)
        {
            ForEach<PlayerScript>(p => p.OnDelete(guid, accountId));
        }
        public void OnPlayerFailedDelete(ObjectGuid guid, uint accountId)
        {
            ForEach<PlayerScript>(p => p.OnFailedDelete(guid, accountId));
        }
        public void OnPlayerSave(Player player)
        {
            ForEach<PlayerScript>(p => p.OnSave(player));
        }
        public void OnPlayerBindToInstance(Player player, Difficulty difficulty, uint mapid, bool permanent, byte extendState)
        {
            ForEach<PlayerScript>(p => p.OnBindToInstance(player, difficulty, mapid, permanent, extendState));
        }
        public void OnPlayerUpdateZone(Player player, uint newZone, uint newArea)
        {
            ForEach<PlayerScript>(p => p.OnUpdateZone(player, newZone, newArea));
        }
        public void OnPlayerRepop(Player player)
        {
            ForEach<PlayerScript>(p => p.OnPlayerRepop(player));
        }
        public void OnQuestStatusChange(Player player, uint questId)
        {
            ForEach<PlayerScript>(p => p.OnQuestStatusChange(player, questId));
        }
        public void OnMovieComplete(Player player, uint movieId)
        {
            ForEach<PlayerScript>(p => p.OnMovieComplete(player, movieId));
        }
        public void OnPlayerChoiceResponse(Player player, uint choiceId, uint responseId)
        {
            ForEach<PlayerScript>(p => p.OnPlayerChoiceResponse(player, choiceId, responseId));
        }

        // GuildScript
        public void OnGuildAddMember(Guild guild, Player player, byte plRank)
        {
            ForEach<GuildScript>(p => p.OnAddMember(guild, player, plRank));
        }
        public void OnGuildRemoveMember(Guild guild, Player player, bool isDisbanding, bool isKicked)
        {
            ForEach<GuildScript>(p => p.OnRemoveMember(guild, player, isDisbanding, isKicked));
        }
        public void OnGuildMOTDChanged(Guild guild, string newMotd)
        {
            ForEach<GuildScript>(p => p.OnMOTDChanged(guild, newMotd));
        }
        public void OnGuildInfoChanged(Guild guild, string newInfo)
        {
            ForEach<GuildScript>(p => p.OnInfoChanged(guild, newInfo));
        }
        public void OnGuildCreate(Guild guild, Player leader, string name)
        {
            ForEach<GuildScript>(p => p.OnCreate(guild, leader, name));
        }
        public void OnGuildDisband(Guild guild)
        {
            ForEach<GuildScript>(p => p.OnDisband(guild));
        }
        public void OnGuildMemberWitdrawMoney(Guild guild, Player player, ulong amount, bool isRepair)
        {
            ForEach<GuildScript>(p => p.OnMemberWitdrawMoney(guild, player, amount, isRepair));
        }
        public void OnGuildMemberDepositMoney(Guild guild, Player player, ulong amount)
        {
            ForEach<GuildScript>(p => p.OnMemberDepositMoney(guild, player, amount));
        }
        public void OnGuildItemMove(Guild guild, Player player, Item pItem, bool isSrcBank, byte srcContainer, byte srcSlotId, bool isDestBank, byte destContainer, byte destSlotId)
        {
            ForEach<GuildScript>(p => p.OnItemMove(guild, player, pItem, isSrcBank, srcContainer, srcSlotId, isDestBank, destContainer, destSlotId));
        }
        public void OnGuildEvent(Guild guild, byte eventType, ulong playerGuid1, ulong playerGuid2, byte newRank)
        {
            ForEach<GuildScript>(p => p.OnEvent(guild, eventType, playerGuid1, playerGuid2, newRank));
        }
        public void OnGuildBankEvent(Guild guild, byte eventType, byte tabId, ulong playerGuid, uint itemOrMoney, ushort itemStackCount, byte destTabId)
        {
            ForEach<GuildScript>(p => p.OnBankEvent(guild, eventType, tabId, playerGuid, itemOrMoney, itemStackCount, destTabId));
        }

        // GroupScript
        public void OnGroupAddMember(Group group, ObjectGuid guid)
        {
            Cypher.Assert(group);
            ForEach<GroupScript>(p => p.OnAddMember(group, guid));
        }
        public void OnGroupInviteMember(Group group, ObjectGuid guid)
        {
            Cypher.Assert(group);
            ForEach<GroupScript>(p => p.OnInviteMember(group, guid));
        }
        public void OnGroupRemoveMember(Group group, ObjectGuid guid, RemoveMethod method, ObjectGuid kicker, string reason)
        {
            Cypher.Assert(group);
            ForEach<GroupScript>(p => p.OnRemoveMember(group, guid, method, kicker, reason));
        }
        public void OnGroupChangeLeader(Group group, ObjectGuid newLeaderGuid, ObjectGuid oldLeaderGuid)
        {
            Cypher.Assert(group);
            ForEach<GroupScript>(p => p.OnChangeLeader(group, newLeaderGuid, oldLeaderGuid));
        }
        public void OnGroupDisband(Group group)
        {
            Cypher.Assert(group);
            ForEach<GroupScript>(p => p.OnDisband(group));
        }

        // UnitScript
        public void OnHeal(Unit healer, Unit reciever, ref uint gain)
        {
            uint dmg = gain;
            ForEach<UnitScript>(p => p.OnHeal(healer, reciever, ref dmg));
            gain = dmg;
        }
        public void OnDamage(Unit attacker, Unit victim, ref uint damage)
        {
            uint dmg = damage;
            ForEach<UnitScript>(p => p.OnDamage(attacker, victim, ref dmg));
            damage = dmg;
        }
        public void ModifyPeriodicDamageAurasTick(Unit target, Unit attacker, ref uint damage)
        {
            uint dmg = damage;
            ForEach<UnitScript>(p => p.ModifyPeriodicDamageAurasTick(target, attacker, ref dmg));
            damage = dmg;
        }
        public void ModifyMeleeDamage(Unit target, Unit attacker, ref uint damage)
        {
            uint dmg = damage;
            ForEach<UnitScript>(p => p.ModifyMeleeDamage(target, attacker, ref dmg));
            damage = dmg;
        }
        public void ModifySpellDamageTaken(Unit target, Unit attacker, ref int damage, SpellInfo spellInfo)
        {
            int dmg = damage;
            ForEach<UnitScript>(p => p.ModifySpellDamageTaken(target, attacker, ref dmg, spellInfo));
            damage = dmg;
        }

        // AreaTriggerEntityScript
        public AreaTriggerAI GetAreaTriggerAI(AreaTrigger areaTrigger)
        {
            Cypher.Assert(areaTrigger);

            return RunScriptRet<AreaTriggerEntityScript, AreaTriggerAI>(p => p.GetAI(areaTrigger), areaTrigger.GetScriptId(), null);
        }

        // ConversationScript
        public void OnConversationCreate(Conversation conversation, Unit creator)
        {
            Cypher.Assert(conversation != null);

            RunScript<ConversationScript>(script => script.OnConversationCreate(conversation, creator), conversation.GetScriptId());
        }

        public void OnConversationLineStarted(Conversation conversation, uint lineId, Player sender)
        {
            Cypher.Assert(conversation != null);
            Cypher.Assert(sender != null);

            RunScript<ConversationScript>(script => script.OnConversationLineStarted(conversation, lineId, sender), conversation.GetScriptId());
        }

        //SceneScript
        public void OnSceneStart(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            Cypher.Assert(player);
            Cypher.Assert(sceneTemplate != null);

            RunScript<SceneScript>(script => script.OnSceneStart(player, sceneInstanceID, sceneTemplate), sceneTemplate.ScriptId);
        }
        public void OnSceneTrigger(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate, string triggerName)
        {
            Cypher.Assert(player);
            Cypher.Assert(sceneTemplate != null);

            RunScript<SceneScript>(script => script.OnSceneTriggerEvent(player, sceneInstanceID, sceneTemplate, triggerName), sceneTemplate.ScriptId);
        }
        public void OnSceneCancel(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            Cypher.Assert(player);
            Cypher.Assert(sceneTemplate != null);

            RunScript<SceneScript>(script => script.OnSceneCancel(player, sceneInstanceID, sceneTemplate), sceneTemplate.ScriptId);
        }
        public void OnSceneComplete(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            Cypher.Assert(player);
            Cypher.Assert(sceneTemplate != null);

            RunScript<SceneScript>(script => script.OnSceneComplete(player, sceneInstanceID, sceneTemplate), sceneTemplate.ScriptId);
        }

        //QuestScript
        public void OnQuestStatusChange(Player player, Quest quest, QuestStatus oldStatus, QuestStatus newStatus)
        {
            Cypher.Assert(player);
            Cypher.Assert(quest != null);

            RunScript<QuestScript>(script => script.OnQuestStatusChange(player, quest, oldStatus, newStatus), quest.ScriptId);
        }
        public void OnQuestAcknowledgeAutoAccept(Player player, Quest quest)
        {
            Cypher.Assert(player);
            Cypher.Assert(quest != null);

            RunScript<QuestScript>(script => script.OnAcknowledgeAutoAccept(player, quest), quest.ScriptId);
        }
        public void OnQuestObjectiveChange(Player player, Quest quest, QuestObjective objective, int oldAmount, int newAmount)
        {
            Cypher.Assert(player);
            Cypher.Assert(quest != null);

            RunScript<QuestScript>(script => script.OnQuestObjectiveChange(player, quest, objective, oldAmount, newAmount), quest.ScriptId);
        }
        
        public void ForEach<T>(Action<T> a) where T : IScriptObject
        {
            if (_scriptByType.TryGetValue(typeof(T), out var ifaceImp))
                foreach (T s in ifaceImp)
                    a.Invoke(s);
        }
        public bool RunScriptRet<T>(Func<T, bool> func, uint id, bool ret = false) where T : IScriptObject
        {
            return RunScriptRet<T, bool>(func, id, ret);
        }
        public U RunScriptRet<T, U>(Func<T, U> func, uint id, U ret = default) where T : IScriptObject
        {
            var script = GetScript<T>(id);
            if (script == null)
                return ret;

            return func.Invoke(script);
        }
        public void RunScript<T>(Action<T> a, uint id) where T : IScriptObject
        {
            var script = GetScript<T>(id);
            if (script != null)
                a.Invoke(script);
        }

        public void AddScript<T>(T script) where T : IScriptObject
        {
            Cypher.Assert(script != null);

            if (!_scriptStorage.TryGetValue(typeof(T), out var scriptReg))
            {
                scriptReg = new ScriptRegistry();
                _scriptStorage[typeof(T)] = scriptReg;
            }

            scriptReg.AddScript(script);

            foreach (var iface in typeof(T).GetInterfaces())
            {
                if (iface.Name == nameof(IScriptObject))
                    continue;

                if (!_scriptByType.TryGetValue(iface, out var loadedTypes))
                {
                    loadedTypes = new List<IScriptObject>();
                    _scriptByType[iface] = loadedTypes;
                }

                loadedTypes.Add(script);
            }
        }

        public ScriptRegistry GetScriptRegistry<T>()
        {
            if (_scriptStorage.TryGetValue(typeof(T), out var scriptReg))
                return scriptReg;

            return null;
        }

        public T GetScript<T>(uint id) where T : IScriptObject
        {
            if (_scriptStorage.TryGetValue(typeof(T), out var scriptReg))
                return scriptReg.GetScriptById<T>(id);

            return default(T);
        }

        uint _ScriptCount;
        Dictionary<System.Type, ScriptRegistry> _scriptStorage = new();

        Dictionary<System.Type, List<IScriptObject>> _scriptByType = new();
        Dictionary<uint, WaypointPath> _waypointStore = new();
        
        // creature entry + chain ID
        MultiMap<Tuple<uint, ushort>, SplineChainLink> m_mSplineChainsMap = new(); // spline chains
    }

    public class ScriptRegistry
    {
        public void AddScript(IScriptObject script)
        {
            Cypher.Assert(script != null);

            if (!script.IsDatabaseBound())
            {
                // We're dealing with a code-only script; just add it.
                _scriptMap[Interlocked.Increment(ref _scriptIdCounter)] = script;
                Global.ScriptMgr.IncrementScriptCount();
                return;
            }

            // Get an ID for the script. An ID only exists if it's a script that is assigned in the database
            // through a script name (or similar).
            uint id = Global.ObjectMgr.GetScriptId(script.GetName());
            if (id != 0)
            {
                // Try to find an existing script.
                bool existing = false;

                lock (_scriptMap)
                    foreach (var it in _scriptMap)
                    {
                        if (it.Value.GetName() == script.GetName())
                        {
                            existing = true;
                            break;
                        }
                    }

                // If the script isn't assigned . assign it!
                if (!existing)
                {
                    lock (_scriptMap)
                        _scriptMap[id] = script;
                    Global.ScriptMgr.IncrementScriptCount();
                }
                else
                {
                    // If the script is already assigned . delete it!
                    Log.outError(LogFilter.Scripts, "Script '{0}' already assigned with the same script name, so the script can't work.", script.GetName());

                    Cypher.Assert(false); // Error that should be fixed ASAP.
                }
            }
            else
            {
                // The script uses a script name from database, but isn't assigned to anything.
                Log.outError(LogFilter.Sql, "Script named '{0}' does not have a script name assigned in database.", script.GetName());
                return;
            }
        }

        // Gets a script by its ID (assigned by ObjectMgr).
        public T GetScriptById<T>(uint id) where T : IScriptObject
        {
            lock (_scriptMap)
                return (T)_scriptMap.LookupByKey(id);
        }

        public bool Empty()
        {
            lock(_scriptMap)
                return _scriptMap.Empty();
        }

        public void Unload()
        {
            lock(_scriptMap)
                _scriptMap.Clear();
        }

        // Counter used for code-only scripts.
        uint _scriptIdCounter;
        Dictionary<uint, IScriptObject> _scriptMap = new();
    }


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ScriptAttribute : Attribute
    {
        public ScriptAttribute(string name = "", params object[] args)
        {
            Name = name;
            Args = args;
        }

        public string Name { get; private set; }
        public object[] Args { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SpellScriptAttribute : ScriptAttribute
    {
        public SpellScriptAttribute(string name = "", params object[] args) : base (name, args)
        {

        }

        public SpellScriptAttribute(uint spellId, string name = "", params object[] args) : base(name, args)
        {
            SpellIds = new[] { spellId };
        }

        public SpellScriptAttribute(uint[] spellId, string name = "", params object[] args) : base(name, args)
        {
            SpellIds = spellId;
        }

        public uint[] SpellIds { get; private set; }
    }
}
