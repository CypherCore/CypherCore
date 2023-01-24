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
using Game.Scripting.Interfaces.IAchievement;
using Game.Scripting.Interfaces.IAreaTrigger;
using Game.Scripting.Interfaces.ICreature;
using Game.Scripting.Interfaces.IFormula;
using Game.Scripting.Interfaces.IGameObject;
using Game.Scripting.Interfaces.IItem;
using Game.Scripting.Interfaces.IPlayer;
using Game.Scripting.Interfaces.ITransport;
using Game.Scripting.Interfaces.IVehicle;
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

namespace Game.Scripting
{
    // Manages registration, loading, and execution of Scripts.
    public class ScriptManager : Singleton<ScriptManager>
    {
        uint _ScriptCount;
        Dictionary<System.Type, ScriptRegistry> _scriptStorage = new();

        Dictionary<System.Type, List<IScriptObject>> _scriptByType = new();
        Dictionary<uint, WaypointPath> _waypointStore = new();

        // creature entry + chain ID
        MultiMap<Tuple<uint, ushort>, SplineChainLink> m_mSplineChainsMap = new(); // spline chains

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

        #region Main Script API


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


        #endregion

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
            Unload();
            LoadScripts();
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
            _scriptByType.Clear();

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

            if (entered)
                return RunScriptRet<IAreaTriggerOnTrigger>(a => a.OnTrigger(player, trigger), Global.ObjectMgr.GetAreaTriggerScriptId(trigger.Id));
            else
                return RunScriptRet<IAreaTriggerOnExit>(p => p.OnExit(player, trigger), Global.ObjectMgr.GetAreaTriggerScriptId(trigger.Id));
        }

        #region Player Chat

        public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg)
        {
            ForEach<IPlayerOnChat>(p => p.OnChat(player, type, lang, msg));
        }
        public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg, Player receiver)
        {
            ForEach<IPlayerOnChatWhisper>(p => p.OnChat(player, type, lang, msg, receiver));
        }
        public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg, Group group)
        {
            ForEach<IPlayerOnChatGroup>(p => p.OnChat(player, type, lang, msg, group));
        }
        public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg, Guild guild)
        {
            ForEach<IPlayerOnChatGuild>(p => p.OnChat(player, type, lang, msg, guild));
        }
        public void OnPlayerChat(Player player, ChatMsg type, Language lang, string msg, Channel channel)
        {
            ForEach<IPlayerOnChatChannel>(p => p.OnChat(player, type, lang, msg, channel));
        }

        #endregion

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
        

    }
}
