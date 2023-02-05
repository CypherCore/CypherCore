// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Framework.Constants;
using Framework.Database;
using Game.AI;
using Game.Chat;
using Game.DataStorage;
using Game.Entities;
using Game.Extendability;
using Game.Groups;
using Game.Guilds;
using Game.Movement;
using Game.Scripting.Activators;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAreaTrigger;
using Game.Scripting.Interfaces.IPlayer;
using Game.Scripting.Registers;
using Game.Spells;

namespace Game.Scripting
{
    // Manages registration, loading, and execution of Scripts.
    public class ScriptManager : Singleton<ScriptManager>
    {
        private readonly List<IScriptObject> _blankList = new();

        // creature entry + chain ID
        private readonly MultiMap<Tuple<uint, ushort>, SplineChainLink> _mSplineChainsMap = new(); // spline chains

        private readonly Dictionary<Type, Dictionary<Class, List<IScriptObject>>> _scriptClassByType = new();
        private readonly Dictionary<Type, List<IScriptObject>> _scriptByType = new();
        private readonly Dictionary<Type, ScriptRegistry> _scriptStorage = new();
        private readonly Dictionary<uint, WaypointPath> _waypointStore = new();
        private uint _scriptCount;

        private ScriptManager()
        {
        }

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

        #region Main Script API

        public IEnumerable<T> GetInterfaces<T>() where T : IScriptObject
        {
            if (_scriptByType.TryGetValue(typeof(T), out var ifaceImp))
                return ifaceImp.Cast<T>();

            return _blankList.Cast<T>(); // we dont return null as they might be looping. Empty list is best here.
        }

        public void ForEach<T>(Action<T> a) where T : IScriptObject
        {
            if (_scriptByType.TryGetValue(typeof(T), out var ifaceImp))
                foreach (T s in ifaceImp)
                    a.Invoke(s);
        }

        public void ForEach<T>(Class playerClass, Action<T> a) where T : IScriptObject, IClassRescriction
        {
            if (_scriptClassByType.TryGetValue(typeof(T), out var classKvp))
            {
                if (classKvp.TryGetValue(playerClass, out var ifaceImp))
                    foreach (T s in ifaceImp)
                        a.Invoke(s);

                if (classKvp.TryGetValue(Class.None, out var ifaceImpNone))
                    foreach (T s in ifaceImpNone)
                        a.Invoke(s);
            }
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

            bool hasClass = typeof(T).GetInterfaces().Any(iface => iface.Name == nameof(IClassRescriction));

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

                if (hasClass)
                {
                    if (!_scriptClassByType.TryGetValue(iface, out var classDict))
                    {
                        classDict = new Dictionary<Class, List<IScriptObject>>();
                        _scriptClassByType[iface] = classDict;
                    }

                    classDict.AddToList(((IClassRescriction)script).PlayerClass, script);
                }
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

            return default;
        }

        #endregion

        #region Loading and Unloading

        public void LoadScripts()
        {
            List<Assembly> assemblies = IOHelpers.GetAllAssembliesInDir(Path.Combine(AppContext.BaseDirectory, "Scripts"));

            if (File.Exists(AppContext.BaseDirectory + "Scripts.dll"))
            {
                Assembly scrAss = Assembly.LoadFile(AppContext.BaseDirectory + "Scripts.dll");

                if (scrAss != null)
                    assemblies.Add(scrAss);
            }

            Dictionary<string, IScriptActivator> activators = new();
            Dictionary<Type, IScriptRegister> registers = new();

            foreach (var asm in assemblies)
                foreach (var type in asm.GetTypes()) 
                {
                    RegisterActivators(activators, type);
                    RegisterRegistors(registers, type);
                }

            foreach (var assembly in assemblies)
                foreach (var type in assembly.GetTypes())
                {
                    var attributes = (ScriptAttribute[])type.GetCustomAttributes<ScriptAttribute>(true);

                    if (!attributes.Empty())
                    {
                        var constructors = type.GetConstructors();
                        int numArgsMin = 99;

                        if (constructors.Length == 0)
                        {
                            Log.outError(LogFilter.Scripts, "Script: {0} contains no Public Constructors. Can't load script.", type.Name);

                            continue;
                        }

                        foreach (var attribute in attributes)
                        {
                            string name = type.Name;
                            Type paramType = null;
                            bool validArgs = true;
                            int i = 0;

                            foreach (var constructor in constructors)
                            {
                                var parameters = constructor.GetParameters();

                                if (parameters.Length < numArgsMin)
                                {
                                    numArgsMin = parameters.Length;

                                    if (numArgsMin == 1)
                                        paramType = parameters.FirstOrDefault().ParameterType;
                                }

                                if (parameters.Length != attribute.Args.Length)
                                    continue;

                                foreach (var arg in parameters)
                                    if (arg.ParameterType != attribute.Args[i++].GetType())
                                    {
                                        validArgs = false;

                                        break;
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

                            IScriptObject activatedObj = null;
                            if (!string.IsNullOrEmpty(type?.BaseType?.Name) &&
                                activators.TryGetValue(type.BaseType.Name, out var scriptActivator))
                            {
                                activatedObj = scriptActivator.Activate(type, name, attribute);
                            }

                            if (activatedObj == null)
                                if (attribute.Args.Empty())
                                {
                                    if (numArgsMin == 0)
                                        activatedObj = Activator.CreateInstance(type) as IScriptObject;
                                    else if (numArgsMin == 1 &&
                                             paramType != null &&
                                             paramType == typeof(string))
                                        activatedObj = Activator.CreateInstance(type, name) as IScriptObject;
                                }
                                else
                                {
                                    if (numArgsMin == 1 &&
                                        paramType != null &&
                                        paramType != typeof(string))
                                        activatedObj = Activator.CreateInstance(type, attribute.Args) as IScriptObject;
                                    else
                                        activatedObj = Activator.CreateInstance(type, new object[] { name }.Combine(attribute.Args)) as IScriptObject;
                                }

              
                            if (registers.TryGetValue(attribute.GetType(), out var reg))
                                reg.Register(attribute, activatedObj, name);
                        }
                    }
                }
        }

        private static void RegisterActivators(Dictionary<string, IScriptActivator> activators, Type type)
        {
            if (IOHelpers.DoesTypeSupportInterface(type, typeof(IScriptActivator)))
            {
                var asa = (IScriptActivator)Activator.CreateInstance(type);

                foreach (var t in asa.ScriptBaseTypes)
                    activators[t] = asa;
            }
        }

        private static void RegisterRegistors(Dictionary<Type, IScriptRegister> registers, Type type)
        {
            if (IOHelpers.DoesTypeSupportInterface(type, typeof(IScriptRegister)))
            {
                var newReg = (IScriptRegister)Activator.CreateInstance(type);
                registers[newReg.AttributeType] = newReg;
            }
        }

        public void LoadDatabase()
        {
            LoadScriptWaypoints();
            LoadScriptSplineChains();
        }

        private void LoadScriptWaypoints()
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
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} Script Waypoint nodes in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        private void LoadScriptSplineChains()
        {
            uint oldMSTime = Time.GetMSTime();

            _mSplineChainsMap.Clear();

            //                                             0      1        2         3                 4            5
            SQLResult resultMeta = DB.World.Query("SELECT entry, chainId, splineId, expectedDuration, msUntilNext, velocity FROM script_spline_chain_meta ORDER BY entry asc, chainId asc, splineId asc");
            //                                           0      1        2         3    4  5  6
            SQLResult resultWP = DB.World.Query("SELECT entry, chainId, splineId, wpId, x, y, z FROM script_spline_chain_waypoints ORDER BY entry asc, chainId asc, splineId asc, wpId asc");

            if (resultMeta.IsEmpty() ||
                resultWP.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded spline chain _data for 0 chains, consisting of 0 splines with 0 waypoints. DB tables `script_spline_chain_meta` and `script_spline_chain_waypoints` are empty.");
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

                    if (!_mSplineChainsMap.ContainsKey(key))
                        _mSplineChainsMap[key] = new List<SplineChainLink>();

                    var chain = _mSplineChainsMap[Tuple.Create(entry, chainId)];

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
                    var chain = _mSplineChainsMap.LookupByKey(Tuple.Create(entry, chainId));

                    if (chain == null)
                    {
                        Log.outWarn(LogFilter.ServerLoading, "Creature #{0} has waypoint _data for spline chain {1}. No such chain exists - entry skipped.", entry, chainId);

                        continue;
                    }

                    if (splineId >= chain.Count)
                    {
                        Log.outWarn(LogFilter.ServerLoading, "Creature #{0} has waypoint _data for spline ({1},{2}). The specified chain does not have a spline with this index - entry skipped.", entry, chainId, splineId);

                        continue;
                    }

                    SplineChainLink spline = chain[splineId];

                    if (wpId != spline.Points.Count)
                    {
                        Log.outWarn(LogFilter.ServerLoading, "Creature #{0} has orphaned waypoint _data in spline ({1},{2}) at index {3}. Skipped.", entry, chainId, splineId, wpId);

                        continue;
                    }

                    spline.Points.Add(new Vector3(posX, posY, posZ));
                    ++wpCount;
                } while (resultWP.NextRow());

                Log.outInfo(LogFilter.ServerLoading, "Loaded spline chain _data for {0} chains, consisting of {1} splines with {2} waypoints in {3} ms", chainCount, splineCount, wpCount, Time.GetMSTimeDiffToNow(oldMSTime));
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

        private List<SplineChainLink> GetSplineChain(uint entry, ushort chainId)
        {
            return _mSplineChainsMap.LookupByKey(Tuple.Create(entry, chainId));
        }

        public string ScriptsVersion()
        {
            return "Integrated Cypher Scripts";
        }

        public void IncrementScriptCount()
        {
            ++_scriptCount;
        }

        public uint GetScriptCount()
        {
            return _scriptCount;
        }

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
    }
}