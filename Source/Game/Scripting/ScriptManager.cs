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

using Framework.Constants;
using Framework.Database;
using Framework.GameMath;
using Game.AI;
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
using Game.Spells;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {GetScriptCount()} C# scripts in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

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
                if (attributes.Empty())
                {
                    var baseType = type.BaseType;
                    while (baseType != null)
                    {
                        if (baseType == typeof(ScriptObject))
                        {
                            Log.outWarn(LogFilter.Server, "Script {0} does not have ScriptAttribute", type.Name);
                            continue;
                        }

                        baseType = baseType.BaseType;
                    }
                }
                else
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

                        switch (type.BaseType.Name)
                        {
                            case "SpellScript":
                                genericType = typeof(GenericSpellScriptLoader<>).MakeGenericType(type);
                                name = name.Replace("_SpellScript", "");
                                break;
                            case "AuraScript":
                                genericType = typeof(GenericAuraScriptLoader<>).MakeGenericType(type);
                                name = name.Replace("_AuraScript", "");
                                break;
                            case "SpellScriptLoader":
                            case "AuraScriptLoader":
                            case "WorldScript":
                            case "FormulaScript":
                            case "WorldMapScript":
                            case "InstanceMapScript":
                            case "BattlegroundMapScript":
                            case "ItemScript":
                            case "UnitScript":
                            case "CreatureScript":
                            case "GameObjectScript":
                            case "AreaTriggerScript":
                            case "OutdoorPvPScript":
                            case "WeatherScript":
                            case "AuctionHouseScript":
                            case "ConditionScript":
                            case "VehicleScript":
                            case "DynamicObjectScript":
                            case "TransportScript":
                            case "AchievementCriteriaScript":
                            case "PlayerScript":
                            case "GuildScript":
                            case "GroupScript":
                            case "AreaTriggerEntityScript":
                            case "SceneScript":
                                if (!attribute.Name.IsEmpty())
                                    name = attribute.Name;

                                if (attribute.Args.Empty())
                                    Activator.CreateInstance(genericType);
                                else
                                    Activator.CreateInstance(genericType, new object[] { name }.Combine(attribute.Args));

                                continue;
                            default:
                                genericType = typeof(GenericCreatureScript<>).MakeGenericType(type);
                                break;
                        }

                        if (!attribute.Name.IsEmpty())
                            name = attribute.Name;

                        Activator.CreateInstance(genericType, name, attribute.Args);
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

            m_mPointMoveMap.Clear();

            ulong uiCreatureCount = 0;

            // Load Waypoints
            SQLResult result = DB.World.Query("SELECT COUNT(entry) FROM script_waypoint GROUP BY entry");
            if (!result.IsEmpty())
                uiCreatureCount = result.Read<uint>(0);

            Log.outInfo(LogFilter.ServerLoading, "Loading Script Waypoints for {0} creature(s)...", uiCreatureCount);

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
                ScriptPointMove temp = new ScriptPointMove();

                temp.uiCreatureEntry = result.Read<uint>(0);
                uint uiEntry = temp.uiCreatureEntry;
                temp.uiPointId = result.Read<uint>(1);
                temp.fX = result.Read<float>(2);
                temp.fY = result.Read<float>(3);
                temp.fZ = result.Read<float>(4);
                temp.uiWaitTime = result.Read<uint>(5);

                CreatureTemplate pCInfo = Global.ObjectMgr.GetCreatureTemplate(temp.uiCreatureEntry);

                if (pCInfo == null)
                {
                    Log.outError(LogFilter.Sql, "TSCR: DB table script_waypoint has waypoint for non-existant creature entry {0}", temp.uiCreatureEntry);
                    continue;
                }

                if (pCInfo.ScriptID == 0)
                    Log.outError(LogFilter.Sql, "TSCR: DB table script_waypoint has waypoint for creature entry {0}, but creature does not have ScriptName defined and then useless.", temp.uiCreatureEntry);

                m_mPointMoveMap.Add(uiEntry, temp);
                ++count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} Script Waypoint nodes in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));

        }
        void LoadScriptSplineChains()
        {
            uint oldMSTime = Time.GetMSTime();

            m_mSplineChainsMap.Clear();

            //                                                     0       1        2             3               4
            SQLResult resultMeta = DB.World.Query("SELECT entry, chainId, splineId, expectedDuration, msUntilNext FROM script_spline_chain_meta ORDER BY entry asc, chainId asc, splineId asc");
            //                                                  0       1         2       3   4  5  6
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

                    uint expectedDuration = resultMeta.Read<uint>(3), msUntilNext = resultMeta.Read<uint>(4);
                    chain.Add(new SplineChainLink(expectedDuration, msUntilNext));

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

            SpellInfo pTempSpell;
            var spellStorage = Global.SpellMgr.GetSpellInfoStorage();
            foreach (var i in spellStorage.Keys)
            {
                spellSummaryStorage[i] = new SpellSummary();

                pTempSpell = spellStorage.LookupByKey(i);
                // This spell doesn't exist.
                if (pTempSpell == null)
                    continue;

                foreach (SpellEffectInfo effect in pTempSpell.GetEffectsForDifficulty(Difficulty.None))
                {
                    if (effect == null)
                        continue;

                    // Spell targets self.
                    if (effect.TargetA.GetTarget() == Targets.UnitCaster)
                        spellSummaryStorage[i].Targets |= 1 << ((int)SelectTargetType.Self - 1);

                    // Spell targets a single enemy.
                    if (effect.TargetA.GetTarget() == Targets.UnitEnemy || effect.TargetA.GetTarget() == Targets.DestEnemy)
                        spellSummaryStorage[i].Targets |= 1 << ((int)SelectTargetType.SingleEnemy - 1);

                    // Spell targets AoE at enemy.
                    if (effect.TargetA.GetTarget() == Targets.UnitSrcAreaEnemy || effect.TargetA.GetTarget() == Targets.UnitDestAreaEnemy ||
                        effect.TargetA.GetTarget() == Targets.SrcCaster || effect.TargetA.GetTarget() == Targets.DestDynobjEnemy)
                        spellSummaryStorage[i].Targets |= 1 << ((int)SelectTargetType.AoeEnemy - 1);

                    // Spell targets an enemy.
                    if (effect.TargetA.GetTarget() == Targets.UnitEnemy || effect.TargetA.GetTarget() == Targets.DestEnemy ||
                        effect.TargetA.GetTarget() == Targets.UnitSrcAreaEnemy || effect.TargetA.GetTarget() == Targets.UnitDestAreaEnemy ||
                        effect.TargetA.GetTarget() == Targets.SrcCaster || effect.TargetA.GetTarget() == Targets.DestDynobjEnemy)
                        spellSummaryStorage[i].Targets |= 1 << ((int)SelectTargetType.AnyEnemy - 1);

                    // Spell targets a single friend (or self).
                    if (effect.TargetA.GetTarget() == Targets.UnitCaster || effect.TargetA.GetTarget() == Targets.UnitAlly ||
                        effect.TargetA.GetTarget() == Targets.UnitParty)
                        spellSummaryStorage[i].Targets |= 1 << ((int)SelectTargetType.SingleFriend - 1);

                    // Spell targets AoE friends.
                    if (effect.TargetA.GetTarget() == Targets.UnitCasterAreaParty || effect.TargetA.GetTarget() == Targets.UnitLastareaParty ||
                        effect.TargetA.GetTarget() == Targets.SrcCaster)
                        spellSummaryStorage[i].Targets |= 1 << ((int)SelectTargetType.AoeFriend - 1);

                    // Spell targets any friend (or self).
                    if (effect.TargetA.GetTarget() == Targets.UnitCaster || effect.TargetA.GetTarget() == Targets.UnitAlly ||
                        effect.TargetA.GetTarget() == Targets.UnitParty || effect.TargetA.GetTarget() == Targets.UnitCasterAreaParty ||
                        effect.TargetA.GetTarget() == Targets.UnitLastareaParty || effect.TargetA.GetTarget() == Targets.SrcCaster)
                        spellSummaryStorage[i].Targets |= 1 << ((int)SelectTargetType.AnyFriend - 1);

                    // Make sure that this spell includes a damage effect.
                    if (effect.Effect == SpellEffectName.SchoolDamage || effect.Effect == SpellEffectName.Instakill ||
                        effect.Effect == SpellEffectName.EnvironmentalDamage || effect.Effect == SpellEffectName.HealthLeech)
                        spellSummaryStorage[i].Effects |= 1 << ((int)SelectEffect.Damage - 1);

                    // Make sure that this spell includes a healing effect (or an apply aura with a periodic heal).
                    if (effect.Effect == SpellEffectName.Heal || effect.Effect == SpellEffectName.HealMaxHealth ||
                        effect.Effect == SpellEffectName.HealMechanical || (effect.Effect == SpellEffectName.ApplyAura && effect.ApplyAuraName == AuraType.PeriodicHeal))
                        spellSummaryStorage[i].Effects |= 1 << ((int)SelectEffect.Healing - 1);

                    // Make sure that this spell applies an aura.
                    if (effect.Effect == SpellEffectName.ApplyAura)
                        spellSummaryStorage[i].Effects |= 1 << ((int)SelectEffect.Aura - 1);
                }
            }
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
            foreach (DictionaryEntry entry in ScriptStorage)
            {
                IScriptRegistry scriptRegistry = (IScriptRegistry)entry.Value;
                scriptRegistry.Unload();
            }

            ScriptStorage.Clear();
        }

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
                var tmpscript = reg.GetScriptById(id);
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
                var tmpscript = reg.GetScriptById(id);
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
                var tmpscript = reg.GetScriptById(id);
                if (tmpscript == null)
                    continue;

                scriptDic.Add(tmpscript, id);
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
                var tmpscript = reg.GetScriptById(id);
                if (tmpscript == null)
                    continue;

                scriptDic.Add(tmpscript, id);
            }

            return scriptDic;
        }

        //WorldScript
        public void OnOpenStateChange(bool open)
        {
            ForEach<WorldScript>(p => p.OnOpenStateChange(open));
        }
        public void OnConfigLoad(bool reload)
        {
            ForEach<WorldScript>(p => p.OnConfigLoad(reload));
        }
        public void OnMotdChange(string newMotd)
        {
            ForEach<WorldScript>(p => p.OnMotdChange(newMotd));
        }
        public void OnShutdownInitiate(ShutdownExitCode code, ShutdownMask mask)
        {
            ForEach<WorldScript>(p => p.OnShutdownInitiate(code, mask));
        }
        public void OnShutdownCancel()
        {
            ForEach<WorldScript>(p => p.OnShutdownCancel());
        }
        public void OnWorldUpdate(uint diff)
        {
            ForEach<WorldScript>(p => p.OnUpdate(diff));
        }

        //FormulaScript
        public void OnHonorCalculation(float honor, uint level, float multiplier)
        {
            ForEach<FormulaScript>(p => p.OnHonorCalculation(honor, level, multiplier));
        }
        public void OnGrayLevelCalculation(uint grayLevel, uint playerLevel)
        {
            ForEach<FormulaScript>(p => p.OnGrayLevelCalculation(grayLevel, playerLevel));
        }
        public void OnColorCodeCalculation(XPColorChar color, uint playerLevel, uint mobLevel)
        {
            ForEach<FormulaScript>(p => p.OnColorCodeCalculation(color, playerLevel, mobLevel));
        }
        public void OnZeroDifferenceCalculation(uint diff, uint playerLevel)
        {
            ForEach<FormulaScript>(p => p.OnZeroDifferenceCalculation(diff, playerLevel));
        }
        public void OnBaseGainCalculation(uint gain, uint playerLevel, uint mobLevel)
        {
            ForEach<FormulaScript>(p => p.OnBaseGainCalculation(gain, playerLevel, mobLevel));
        }
        public void OnGainCalculation(uint gain, Player player, Unit unit)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(unit != null);

            ForEach<FormulaScript>(p => p.OnGainCalculation(gain, player, unit));
        }
        public void OnGroupRateCalculation(float rate, uint count, bool isRaid)
        {
            ForEach<FormulaScript>(p => p.OnGroupRateCalculation(rate, count, isRaid));
        }

        //MapScript
        public void OnCreateMap(Map map)
        {
            Cypher.Assert(map != null);
            var record = map.GetEntry();

            if (record != null && record.IsWorldMap())
                ForEach<WorldMapScript>(p => p.OnCreate(map));

            if (record != null && record.IsDungeon())
                ForEach<InstanceMapScript>(p => p.OnCreate(map.ToInstanceMap()));

            if (record != null && record.IsBattleground())
                ForEach<BattlegroundMapScript>(p => p.OnCreate(map.ToBattlegroundMap()));
        }
        public void OnDestroyMap(Map map)
        {
            Cypher.Assert(map != null);
            var record = map.GetEntry();

            if (record != null && record.IsWorldMap())
                ForEach<WorldMapScript>(p => p.OnDestroy(map));

            if (record != null && record.IsDungeon())
                ForEach<InstanceMapScript>(p => p.OnDestroy(map.ToInstanceMap()));

            if (record != null && record.IsBattleground())
                ForEach<BattlegroundMapScript>(p => p.OnDestroy(map.ToBattlegroundMap()));
        }
        public void OnLoadGridMap(Map map, GridMap gmap, uint gx, uint gy)
        {
            Cypher.Assert(map != null);
            Cypher.Assert(gmap != null);
            var record = map.GetEntry();

            if (record != null && record.IsWorldMap())
                ForEach<WorldMapScript>(p => p.OnLoadGridMap(map, gmap, gx, gy));

            if (record != null && record.IsDungeon())
                ForEach<InstanceMapScript>(p => p.OnLoadGridMap(map.ToInstanceMap(), gmap, gx, gy));

            if (record != null && record.IsBattleground())
                ForEach<BattlegroundMapScript>(p => p.OnLoadGridMap(map.ToBattlegroundMap(), gmap, gx, gy));

        }
        public void OnUnloadGridMap(Map map, GridMap gmap, uint gx, uint gy)
        {
            Cypher.Assert(map != null);
            Cypher.Assert(gmap != null);
            var record = map.GetEntry();

            if (record != null && record.IsWorldMap())
                ForEach<WorldMapScript>(p => p.OnUnloadGridMap(map, gmap, gx, gy));

            if (record != null && record.IsDungeon())
                ForEach<InstanceMapScript>(p => p.OnUnloadGridMap(map.ToInstanceMap(), gmap, gx, gy));

            if (record != null && record.IsBattleground())
                ForEach<BattlegroundMapScript>(p => p.OnUnloadGridMap(map.ToBattlegroundMap(), gmap, gx, gy));
        }
        public void OnPlayerEnterMap(Map map, Player player)
        {
            Cypher.Assert(map != null);
            Cypher.Assert(player != null);

            ForEach<PlayerScript>(p => p.OnMapChanged(player));

            var record = map.GetEntry();

            if (record != null && record.IsWorldMap())
                ForEach<WorldMapScript>(p => p.OnPlayerEnter(map, player));

            if (record != null && record.IsDungeon())
                ForEach<InstanceMapScript>(p => p.OnPlayerEnter(map.ToInstanceMap(), player));

            if (record != null && record.IsBattleground())
                ForEach<BattlegroundMapScript>(p => p.OnPlayerEnter(map.ToBattlegroundMap(), player));
        }
        public void OnPlayerLeaveMap(Map map, Player player)
        {
            Cypher.Assert(map != null);
            var record = map.GetEntry();

            if (record != null && record.IsWorldMap())
                ForEach<WorldMapScript>(p => p.OnPlayerLeave(map, player));

            if (record != null && record.IsDungeon())
                ForEach<InstanceMapScript>(p => p.OnPlayerLeave(map.ToInstanceMap(), player));

            if (record != null && record.IsBattleground())
                ForEach<BattlegroundMapScript>(p => p.OnPlayerLeave(map.ToBattlegroundMap(), player));
        }
        public void OnMapUpdate(Map map, uint diff)
        {
            Cypher.Assert(map != null);
            var record = map.GetEntry();

            if (record != null && record.IsWorldMap())
                ForEach<WorldMapScript>(p => p.OnUpdate(map, diff));

            if (record != null && record.IsDungeon())
                ForEach<InstanceMapScript>(p => p.OnUpdate(map.ToInstanceMap(), diff));

            if (record != null && record.IsBattleground())
                ForEach<BattlegroundMapScript>(p => p.OnUpdate(map.ToBattlegroundMap(), diff));
        }

        //InstanceMapScript
        public InstanceScript CreateInstanceData(InstanceMap map)
        {
            Cypher.Assert(map != null);

            return RunScriptRet<InstanceMapScript, InstanceScript>(p => p.GetInstanceScript(map), map.GetScriptId(), null);
        }

        //ItemScript
        public bool OnDummyEffect(Unit caster, uint spellId, uint effIndex, Item target)
        {
            Cypher.Assert(caster != null);
            Cypher.Assert(target != null);

            return RunScriptRet<ItemScript>(p => p.OnDummyEffect(caster, spellId, effIndex, target), target.GetScriptId());
        }
        public bool OnQuestAccept(Player player, Item item, Quest quest)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(item != null);
            Cypher.Assert(quest != null);

            return RunScriptRet<ItemScript>(p => p.OnQuestAccept(player, item, quest), item.GetScriptId());
        }
        public bool OnItemUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            Cypher.Assert(player);
            Cypher.Assert(item);

            return RunScriptRet<ItemScript>(p => p.OnUse(player, item, targets, castId), item.GetScriptId());
        }
        public bool OnItemExpire(Player player, ItemTemplate proto)
        {
            Cypher.Assert(player);
            Cypher.Assert(proto != null);

            return RunScriptRet<ItemScript>(p => p.OnExpire(player, proto), proto.ScriptId);
        }

        //CreatureScript
        public bool OnDummyEffect(Unit caster, uint spellId, uint effIndex, Creature target)
        {
            Cypher.Assert(caster);
            Cypher.Assert(target);

            return RunScriptRet<CreatureScript>(p => p.OnDummyEffect(caster, spellId, effIndex, target), target.GetScriptId());
        }
        public bool OnGossipHello(Player player, Creature creature)
        {
            Cypher.Assert(player);
            Cypher.Assert(creature);

            return RunScriptRet<CreatureScript>(p => p.OnGossipHello(player, creature), creature.GetScriptId());
        }
        public bool OnGossipSelect(Player player, Creature creature, uint sender, uint action)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(creature != null);

            return RunScriptRet<CreatureScript>(p => p.OnGossipSelect(player, creature, sender, action), creature.GetScriptId());
        }
        public bool OnGossipSelectCode(Player player, Creature creature, uint sender, uint action, string code)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(creature != null);
            Cypher.Assert(code != null);

            return RunScriptRet<CreatureScript>(p => p.OnGossipSelectCode(player, creature, sender, action, code), creature.GetScriptId());
        }
        public bool OnQuestAccept(Player player, Creature creature, Quest quest)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(creature != null);
            Cypher.Assert(quest != null);

            return RunScriptRet<CreatureScript>(p => p.OnQuestAccept(player, creature, quest), creature.GetScriptId());
        }
        public bool OnQuestSelect(Player player, Creature creature, Quest quest)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(creature != null);
            Cypher.Assert(quest != null);

            return RunScriptRet<CreatureScript>(p => p.OnQuestSelect(player, creature, quest), creature.GetScriptId());
        }
        public bool OnQuestComplete(Player player, Creature creature, Quest quest)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(creature != null);
            Cypher.Assert(quest != null);

            return RunScriptRet<CreatureScript>(p => p.OnQuestComplete(player, creature, quest), creature.GetScriptId());
        }
        public bool OnQuestReward(Player player, Creature creature, Quest quest, uint opt)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(creature != null);
            Cypher.Assert(quest != null);

            return RunScriptRet<CreatureScript>(p => p.OnQuestReward(player, creature, quest, opt), creature.GetScriptId());
        }
        public uint GetDialogStatus(Player player, Creature creature)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(creature != null);

            player.PlayerTalkClass.ClearMenus();
            return RunScriptRet<CreatureScript, uint>(p => p.GetDialogStatus(player, creature), creature.GetScriptId(), (uint)QuestGiverStatus.ScriptedNoStatus);
        }
        public bool CanSpawn(ulong spawnId, uint entry, CreatureTemplate actTemplate, CreatureData cData, Map map)
        {
            Cypher.Assert(actTemplate != null);

            CreatureTemplate baseTemplate = Global.ObjectMgr.GetCreatureTemplate(entry);
            if (baseTemplate == null)
                baseTemplate = actTemplate;
            return RunScriptRet<CreatureScript, bool>(p => p.CanSpawn(spawnId, entry, baseTemplate, actTemplate, cData, map), cData != null ? cData.ScriptId : baseTemplate.ScriptID, true);
        }
        public CreatureAI GetCreatureAI(Creature creature)
        {
            Cypher.Assert(creature != null);

            return RunScriptRet<CreatureScript, CreatureAI>(p => p.GetAI(creature), creature.GetScriptId());
        }
        public void OnCreatureUpdate(Creature creature, uint diff)
        {
            Cypher.Assert(creature != null);

            RunScript<CreatureScript>(p => p.OnUpdate(creature, diff), creature.GetScriptId());
        }

        //GameObjectScript
        public bool OnDummyEffect(Unit caster, uint spellId, uint effIndex, GameObject target)
        {
            Cypher.Assert(caster != null);
            Cypher.Assert(target != null);

            return RunScriptRet<GameObjectScript>(p => p.OnDummyEffect(caster, spellId, effIndex, target), target.GetScriptId());
        }
        public bool OnGossipHello(Player player, GameObject go)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(go != null);

            player.PlayerTalkClass.ClearMenus();
            return RunScriptRet<GameObjectScript>(p => p.OnGossipHello(player, go), go.GetScriptId());
        }
        public bool OnGossipSelect(Player player, GameObject go, uint sender, uint action)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(go != null);

            return RunScriptRet<GameObjectScript>(p => p.OnGossipSelect(player, go, sender, action), go.GetScriptId());
        }
        public bool OnGossipSelectCode(Player player, GameObject go, uint sender, uint action, string code)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(go != null);
            Cypher.Assert(code != null);

            return RunScriptRet<GameObjectScript>(p => p.OnGossipSelectCode(player, go, sender, action, code), go.GetScriptId());
        }
        public bool OnQuestAccept(Player player, GameObject go, Quest quest)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(go != null);
            Cypher.Assert(quest != null);

            return RunScriptRet<GameObjectScript>(p => p.OnQuestAccept(player, go, quest), go.GetScriptId());
        }
        public bool OnQuestReward(Player player, GameObject go, Quest quest, uint opt)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(go != null);
            Cypher.Assert(quest != null);

            return RunScriptRet<GameObjectScript>(p => p.OnQuestReward(player, go, quest, opt), go.GetScriptId());
        }
        public uint GetDialogStatus(Player player, GameObject go)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(go != null);

            return RunScriptRet<GameObjectScript, uint>(p => p.GetDialogStatus(player, go), go.GetScriptId(), (uint)QuestGiverStatus.ScriptedNoStatus);
        }
        public void OnGameObjectDestroyed(GameObject go, Player player)
        {
            Cypher.Assert(go != null);

            RunScript<GameObjectScript>(p => p.OnDestroyed(go, player), go.GetScriptId());
        }
        public void OnGameObjectDamaged(GameObject go, Player player)
        {
            Cypher.Assert(go != null);

            RunScript<GameObjectScript>(p => p.OnDamaged(go, player), go.GetScriptId());
        }
        public void OnGameObjectLootStateChanged(GameObject go, uint state, Unit unit)
        {
            Cypher.Assert(go != null);

            RunScript<GameObjectScript>(p => p.OnLootStateChanged(go, state, unit), go.GetScriptId());
        }
        public void OnGameObjectStateChanged(GameObject go, GameObjectState state)
        {
            Cypher.Assert(go != null);

            RunScript<GameObjectScript>(p => p.OnGameObjectStateChanged(go, state), go.GetScriptId());
        }
        public void OnGameObjectUpdate(GameObject go, uint diff)
        {
            Cypher.Assert(go != null);

            RunScript<GameObjectScript>(p => p.OnUpdate(go, diff), go.GetScriptId());
        }
        public GameObjectAI GetGameObjectAI(GameObject go)
        {
            Cypher.Assert(go != null);

            return RunScriptRet<GameObjectScript, GameObjectAI>(p => p.GetAI(go), go.GetScriptId());
        }

        //AreaTriggerScript
        public bool OnAreaTrigger(Player player, AreaTriggerRecord trigger, bool entered)
        {
            Cypher.Assert(player != null);
            Cypher.Assert(trigger != null);

            return RunScriptRet<AreaTriggerScript>(p => p.OnTrigger(player, trigger, entered), Global.ObjectMgr.GetAreaTriggerScriptId(trigger.Id));
        }

        //BattlegroundScript
        public Battleground CreateBattleground(BattlegroundTypeId typeId)
        {
            // @todo Implement script-side Battlegrounds.
            Cypher.Assert(false);
            return null;
        }

        // OutdoorPvPScript
        public OutdoorPvP CreateOutdoorPvP(OutdoorPvPData data)
        {
            Cypher.Assert(data != null);
            return RunScriptRet<OutdoorPvPScript, OutdoorPvP>(p => p.GetOutdoorPvP(), data.ScriptId, null);
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
        public void OnAuctionAdd(AuctionHouseObject ah, AuctionEntry entry)
        {
            Cypher.Assert(ah != null);
            Cypher.Assert(entry != null);
            ForEach<AuctionHouseScript>(p => p.OnAuctionAdd(ah, entry));
        }
        public void OnAuctionRemove(AuctionHouseObject ah, AuctionEntry entry)
        {
            Cypher.Assert(ah != null);
            Cypher.Assert(entry != null);
            ForEach<AuctionHouseScript>(p => p.OnAuctionRemove(ah, entry));
        }
        public void OnAuctionSuccessful(AuctionHouseObject ah, AuctionEntry entry)
        {
            Cypher.Assert(ah != null);
            Cypher.Assert(entry != null);
            ForEach<AuctionHouseScript>(p => p.OnAuctionSuccessful(ah, entry));
        }
        public void OnAuctionExpire(AuctionHouseObject ah, AuctionEntry entry)
        {
            Cypher.Assert(ah != null);
            Cypher.Assert(entry != null);
            ForEach<AuctionHouseScript>(p => p.OnAuctionExpire(ah, entry));
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
        public void OnRelocate(Transport transport, uint waypointId, uint mapId, float x, float y, float z)
        {
            RunScript<TransportScript>(p => p.OnRelocate(transport, waypointId, mapId, x, y, z), transport.GetScriptId());
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
        public void OnPlayerDelete(ObjectGuid guid)
        {
            ForEach<PlayerScript>(p => p.OnDelete(guid));
        }
        public void OnPlayerSave(Player player)
        {
            ForEach<PlayerScript>(p => p.OnSave(player));
        }
        public void OnPlayerBindToInstance(Player player, Difficulty difficulty, uint mapid, bool permanent, BindExtensionState extendState)
        {
            ForEach<PlayerScript>(p => p.OnBindToInstance(player, difficulty, mapid, permanent, extendState));
        }
        public void OnPlayerUpdateZone(Player player, uint newZone, uint newArea)
        {
            ForEach<PlayerScript>(p => p.OnUpdateZone(player, newZone, newArea));
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
        public void ModifySpellDamageTaken(Unit target, Unit attacker, ref int damage)
        {
            int dmg = damage;
            ForEach<UnitScript>(p => p.ModifySpellDamageTaken(target, attacker, ref dmg));
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
        public void OnQuestObjectiveChange(Player player, Quest quest, QuestObjective objective, int oldAmount, int newAmount)
        {
            Cypher.Assert(player);
            Cypher.Assert(quest != null);

            RunScript<QuestScript>(script => script.OnQuestObjectiveChange(player, quest, objective, oldAmount, newAmount), quest.ScriptId);
        }

        public void ForEach<T>(Action<T> a) where T : ScriptObject
        {
            var reg = GetScriptRegistry<T>();
            if (reg == null || reg.Empty())
                return;

            foreach (var script in reg.GetStorage())
                a.Invoke(script);
        }
        public bool RunScriptRet<T>(Func<T, bool> func, uint id, bool ret = false) where T : ScriptObject
        {
            return RunScriptRet<T, bool>(func, id, ret);
        }
        public U RunScriptRet<T, U>(Func<T, U> func, uint id, U ret = default(U)) where T : ScriptObject
        {
            var reg = GetScriptRegistry<T>();
            if (reg == null || reg.Empty())
                return ret;

            var script = reg.GetScriptById(id);
            if (script == null)
                return ret;

            return func.Invoke(script);
        }
        public void RunScript<T>(Action<T> a, uint id) where T : ScriptObject
        {
            var reg = GetScriptRegistry<T>();
            if (reg == null || reg.Empty())
                return;

            var script = reg.GetScriptById(id);
            if (script != null)
                a.Invoke(script);
        }
        public void AddScript<T>(T script) where T : ScriptObject
        {
            Cypher.Assert(script != null);

            if (!ScriptStorage.ContainsKey(typeof(T)))
                ScriptStorage[typeof(T)] = new ScriptRegistry<T>();

            GetScriptRegistry<T>().AddScript(script);
        }

        public List<ScriptPointMove> GetPointMoveList(uint creatureEntry)
        {
            return m_mPointMoveMap.LookupByKey(creatureEntry);
        }

        ScriptRegistry<T> GetScriptRegistry<T>() where T : ScriptObject
        {
            if (ScriptStorage.ContainsKey(typeof(T)))
                return (ScriptRegistry<T>)ScriptStorage[typeof(T)];

            return null;
        }

        uint _ScriptCount;
        public Dictionary<uint, SpellSummary> spellSummaryStorage = new Dictionary<uint, SpellSummary>();
        Hashtable ScriptStorage = new Hashtable();

        MultiMap<uint, ScriptPointMove> m_mPointMoveMap = new MultiMap<uint, ScriptPointMove>();
        
        // creature entry + chain ID
        MultiMap<Tuple<uint, ushort>, SplineChainLink> m_mSplineChainsMap = new MultiMap<Tuple<uint, ushort>, SplineChainLink>(); // spline chains
    }

    public interface IScriptRegistry
    {
        void Unload();
    }

    public class ScriptRegistry<TValue> : IScriptRegistry where TValue : ScriptObject
    {
        public void AddScript(TValue script)
        {
            Cypher.Assert(script != null);

            if (!script.IsDatabaseBound())
            {
                // We're dealing with a code-only script; just add it.
                ScriptMap[_scriptIdCounter++] = script;
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
                foreach (var it in ScriptMap)
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
                    ScriptMap[id] = script;
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
        public TValue GetScriptById(uint id)
        {
            return ScriptMap.LookupByKey(id);
        }

        public bool Empty()
        {
            return ScriptMap.Empty();
        }

        public List<TValue> GetStorage()
        {
            return ScriptMap.Values.ToList();
        }

        public void Unload()
        {
            ScriptMap.Clear();
        }

        // Counter used for code-only scripts.
        uint _scriptIdCounter;
        Dictionary<uint, TValue> ScriptMap = new Dictionary<uint, TValue>();
    }

    public class ScriptPointMove
    {
        public uint uiCreatureEntry;
        public uint uiPointId;
        public float fX;
        public float fY;
        public float fZ;
        public uint uiWaitTime;
    }

    public class SpellSummary
    {
        public byte Targets;                                          // set of enum SelectTarget
        public byte Effects;                                          // set of enum SelectEffect
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
}
