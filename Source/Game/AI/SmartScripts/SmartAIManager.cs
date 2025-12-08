// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Movement;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Game.AI
{
    public class SmartAIManager : Singleton<SmartAIManager>
    {
        MultiMap<int, SmartScriptHolder>[] _eventMap = new MultiMap<int, SmartScriptHolder>[(int)SmartScriptType.Max];

        SmartAIManager()
        {
            for (byte i = 0; i < (int)SmartScriptType.Max; i++)
                _eventMap[i] = new MultiMap<int, SmartScriptHolder>();
        }

        public void LoadFromDB()
        {
            uint oldMSTime = Time.GetMSTime();

            for (byte i = 0; i < (int)SmartScriptType.Max; i++)
                _eventMap[i].Clear();  //Drop Existing SmartAI List

            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_SMART_SCRIPTS);
            SQLResult result = DB.World.Query(stmt);
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 SmartAI scripts. DB table `smartai_scripts` is empty.");
                return;
            }

            int count = 0;
            do
            {
                SmartScriptHolder temp = new();

                temp.EntryOrGuid = result.Read<int>(0);
                if (temp.EntryOrGuid == 0)
                {
                    Log.outError(LogFilter.Sql, "SmartAIMgr.LoadFromDB: invalid entryorguid (0), skipped loading.");
                    continue;
                }

                SmartScriptType source_type = (SmartScriptType)result.Read<byte>(1);
                if (source_type >= SmartScriptType.Max)
                {
                    Log.outError(LogFilter.Sql, "SmartAIMgr.LoadFromDB: invalid source_type ({0}), skipped loading.", source_type);
                    continue;
                }
                if (temp.EntryOrGuid >= 0)
                {
                    switch (source_type)
                    {
                        case SmartScriptType.Creature:
                            if (Global.ObjectMgr.GetCreatureTemplate((uint)temp.EntryOrGuid) == null)
                            {
                                Log.outError(LogFilter.Sql, "SmartAIMgr.LoadFromDB: Creature entry ({0}) does not exist, skipped loading.", temp.EntryOrGuid);
                                continue;
                            }
                            break;

                        case SmartScriptType.GameObject:
                        {
                            if (Global.ObjectMgr.GetGameObjectTemplate((uint)temp.EntryOrGuid) == null)
                            {
                                Log.outError(LogFilter.Sql, "SmartAIMgr.LoadFromDB: GameObject entry ({0}) does not exist, skipped loading.", temp.EntryOrGuid);
                                continue;
                            }
                            break;
                        }
                        case SmartScriptType.AreaTrigger:
                        {
                            if (CliDB.AreaTableStorage.LookupByKey((uint)temp.EntryOrGuid) == null)
                            {
                                Log.outError(LogFilter.Sql, "SmartAIMgr.LoadFromDB: AreaTrigger entry ({0}) does not exist, skipped loading.", temp.EntryOrGuid);
                                continue;
                            }
                            break;
                        }
                        case SmartScriptType.Scene:
                        {
                            if (Global.ObjectMgr.GetSceneTemplate((uint)temp.EntryOrGuid) == null)
                            {
                                Log.outError(LogFilter.Sql, "SmartAIMgr.LoadFromDB: Scene id ({0}) does not exist, skipped loading.", temp.EntryOrGuid);
                                continue;
                            }
                            break;
                        }
                        case SmartScriptType.Event:
                        {
                            if (!Global.ObjectMgr.IsValidEvent((uint)temp.EntryOrGuid))
                            {
                                Log.outError(LogFilter.Sql, $"SmartAIMgr::LoadFromDB: Event id ({temp.EntryOrGuid}) does not exist, skipped loading.");
                                continue;
                            }
                            break;
                        }
                        case SmartScriptType.Quest:
                        {
                            if (Global.ObjectMgr.GetQuestTemplate((uint)temp.EntryOrGuid) == null)
                            {
                                Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: Quest id ({temp.EntryOrGuid}) does not exist, skipped loading.");
                                continue;
                            }
                            break;
                        }
                        case SmartScriptType.TimedActionlist:
                            break;//nothing to check, really
                        case SmartScriptType.AreaTriggerEntity:
                        {
                            if (Global.AreaTriggerDataStorage.GetAreaTriggerTemplate(new AreaTriggerId((uint)temp.EntryOrGuid, false)) == null)
                            {
                                Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: AreaTrigger entry ({temp.EntryOrGuid} IsCustom false) does not exist, skipped loading.");
                                continue;
                            }
                            break;
                        }
                        case SmartScriptType.AreaTriggerEntityCustom:
                        {
                            if (Global.AreaTriggerDataStorage.GetAreaTriggerTemplate(new AreaTriggerId((uint)temp.EntryOrGuid, true)) == null)
                            {
                                Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: AreaTrigger entry ({temp.EntryOrGuid} IsCustom true) does not exist, skipped loading.");
                                continue;
                            }
                            break;
                        }
                        default:
                            Log.outError(LogFilter.Sql, "SmartAIMgr.LoadFromDB: not yet implemented source_type {0}", source_type);
                            continue;
                    }
                }
                else
                {
                    switch (source_type)
                    {
                        case SmartScriptType.Creature:
                        {
                            CreatureData creature = Global.ObjectMgr.GetCreatureData((ulong)-temp.EntryOrGuid);
                            if (creature == null)
                            {
                                Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: Creature guid ({-temp.EntryOrGuid}) does not exist, skipped loading.");
                                continue;
                            }

                            CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(creature.Id);
                            if (creatureInfo == null)
                            {
                                Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: Creature entry ({creature.Id}) guid ({-temp.EntryOrGuid}) does not exist, skipped loading.");
                                continue;
                            }

                            if (creatureInfo.AIName != "SmartAI")
                            {
                                Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: Creature entry ({creature.Id}) guid ({-temp.EntryOrGuid}) is not using SmartAI, skipped loading.");
                                continue;
                            }
                            break;
                        }
                        case SmartScriptType.GameObject:
                        {
                            GameObjectData gameObject = Global.ObjectMgr.GetGameObjectData((ulong)-temp.EntryOrGuid);
                            if (gameObject == null)
                            {
                                Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: GameObject guid ({-temp.EntryOrGuid}) does not exist, skipped loading.");
                                continue;
                            }

                            GameObjectTemplate gameObjectInfo = Global.ObjectMgr.GetGameObjectTemplate(gameObject.Id);
                            if (gameObjectInfo == null)
                            {
                                Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: GameObject entry ({gameObject.Id}) guid ({-temp.EntryOrGuid}) does not exist, skipped loading.");
                                continue;
                            }

                            if (gameObjectInfo.AIName != "SmartGameObjectAI")
                            {
                                Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: GameObject entry ({gameObject.Id}) guid ({-temp.EntryOrGuid}) is not using SmartGameObjectAI, skipped loading.");
                                continue;
                            }
                            break;
                        }
                        default:
                            Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: GUID-specific scripting not yet implemented for source_type {source_type}");
                            continue;
                    }
                }

                temp.SourceType = source_type;
                temp.EventId = result.Read<ushort>(2);
                temp.Link = result.Read<ushort>(3);

                bool invalidDifficulties = false;
                foreach (string token in result.Read<string>(4).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!Enum.TryParse<Difficulty>(token, out Difficulty difficultyId))
                    {
                        invalidDifficulties = true;
                        Log.outError(LogFilter.Sql, $"SmartAIMgr::LoadFromDB: Invalid difficulties for entryorguid ({temp.EntryOrGuid}) source_type ({temp.GetScriptType()}) id ({temp.EventId}), skipped loading.");
                        break;
                    }

                    if (difficultyId != 0 && !CliDB.DifficultyStorage.ContainsKey(difficultyId))
                    {
                        invalidDifficulties = true;
                        Log.outError(LogFilter.Sql, $"SmartAIMgr::LoadFromDB: Invalid difficulty id ({difficultyId}) for entryorguid ({temp.EntryOrGuid}) source_type ({temp.GetScriptType()}) id ({temp.EventId}), skipped loading.");
                        break;
                    }

                    temp.Difficulties.Add(difficultyId);
                }

                if (invalidDifficulties)
                    continue;

                temp.Event.type = (SmartEvents)result.Read<byte>(5);
                temp.Event.event_phase_mask = result.Read<ushort>(6);
                temp.Event.event_chance = result.Read<byte>(7);
                temp.Event.event_flags = (SmartEventFlags)result.Read<ushort>(8);

                temp.Event.raw.param1 = result.Read<uint>(9);
                temp.Event.raw.param2 = result.Read<uint>(10);
                temp.Event.raw.param3 = result.Read<uint>(11);
                temp.Event.raw.param4 = result.Read<uint>(12);
                temp.Event.raw.param5 = result.Read<uint>(13);
                temp.Event.param_string = result.Read<string>(14);

                temp.Action.type = (SmartActions)result.Read<byte>(15);
                temp.Action.raw.param1 = result.Read<uint>(16);
                temp.Action.raw.param2 = result.Read<uint>(17);
                temp.Action.raw.param3 = result.Read<uint>(18);
                temp.Action.raw.param4 = result.Read<uint>(19);
                temp.Action.raw.param5 = result.Read<uint>(20);
                temp.Action.raw.param6 = result.Read<uint>(21);
                temp.Action.raw.param7 = result.Read<uint>(22);
                temp.Action.param_string = result.Read<string>(23);

                temp.Target.type = (SmartTargets)result.Read<byte>(24);
                temp.Target.raw.param1 = result.Read<uint>(25);
                temp.Target.raw.param2 = result.Read<uint>(26);
                temp.Target.raw.param3 = result.Read<uint>(27);
                temp.Target.raw.param4 = result.Read<uint>(28);
                temp.Target.param_string = result.Read<string>(29);
                temp.Target.x = result.Read<float>(30);
                temp.Target.y = result.Read<float>(31);
                temp.Target.z = result.Read<float>(32);
                temp.Target.o = result.Read<float>(33);

                //check target
                if (!IsTargetValid(temp))
                    continue;

                // check all event and action params
                if (!IsEventValid(temp))
                    continue;

                // specific check for timed events
                switch (temp.Event.type)
                {
                    case SmartEvents.Update:
                    case SmartEvents.UpdateOoc:
                    case SmartEvents.UpdateIc:
                    case SmartEvents.HealthPct:
                    case SmartEvents.ManaPct:
                    case SmartEvents.Range:
                    case SmartEvents.FriendlyHealthPCT:
                    case SmartEvents.FriendlyMissingBuff:
                    case SmartEvents.HasAura:
                    case SmartEvents.TargetBuffed:
                        if (temp.Event.minMaxRepeat.repeatMin == 0 && temp.Event.minMaxRepeat.repeatMax == 0 && !temp.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable) && temp.SourceType != SmartScriptType.TimedActionlist)
                        {
                            temp.Event.event_flags |= SmartEventFlags.NotRepeatable;
                            Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: Entry {temp.EntryOrGuid} SourceType {temp.GetScriptType()}, Event {temp.EventId}, Missing Repeat flag.");
                        }
                        break;
                    case SmartEvents.VictimCasting:
                        if (temp.Event.minMaxRepeat.min == 0 && temp.Event.minMaxRepeat.max == 0 && !temp.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable) && temp.SourceType != SmartScriptType.TimedActionlist)
                        {
                            temp.Event.event_flags |= SmartEventFlags.NotRepeatable;
                            Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: Entry {temp.EntryOrGuid} SourceType {temp.GetScriptType()}, Event {temp.EventId}, Missing Repeat flag.");
                        }
                        break;
                    case SmartEvents.FriendlyIsCc:
                        if (temp.Event.friendlyCC.repeatMin == 0 && temp.Event.friendlyCC.repeatMax == 0 && !temp.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable) && temp.SourceType != SmartScriptType.TimedActionlist)
                        {
                            temp.Event.event_flags |= SmartEventFlags.NotRepeatable;
                            Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: Entry {temp.EntryOrGuid} SourceType {temp.GetScriptType()}, Event {temp.EventId}, Missing Repeat flag.");
                        }
                        break;
                    default:
                        break;
                }

                // creature entry / guid not found in storage, create empty event list for it and increase counters
                if (!_eventMap[(int)source_type].ContainsKey(temp.EntryOrGuid))
                    ++count;

                // store the new event
                _eventMap[(int)source_type].Add(temp.EntryOrGuid, temp);
            }
            while (result.NextRow());

            // Post Loading Validation
            for (byte i = 0; i < (int)SmartScriptType.Max; ++i)
            {
                if (_eventMap[i] == null)
                    continue;

                foreach (var key in _eventMap[i].Keys)
                {
                    var list = _eventMap[i].LookupByKey(key);
                    foreach (var e in list)
                    {
                        if (e.Link != 0)
                        {
                            if (FindLinkedEvent(list, e.Link) == null)
                            {
                                Log.outError(LogFilter.Sql, "SmartAIMgr.LoadFromDB: Entry {0} SourceType {1}, Event {2}, Link Event {3} not found or invalid.",
                                        e.EntryOrGuid, e.GetScriptType(), e.EventId, e.Link);
                            }
                        }

                        if (e.GetEventType() == SmartEvents.Link)
                        {
                            if (FindLinkedSourceEvent(list, e.EventId) == null)
                            {
                                Log.outError(LogFilter.Sql, "SmartAIMgr.LoadFromDB: Entry {0} SourceType {1}, Event {2}, Link Source Event not found or invalid. Event will never trigger.",
                                        e.EntryOrGuid, e.GetScriptType(), e.EventId);
                            }
                        }
                    }
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} SmartAI scripts in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        static bool EventHasInvoker(SmartEvents smartEvent)
        {
            switch (smartEvent)
            { // white list of events that actually have an invoker passed to them
                case SmartEvents.Aggro:
                case SmartEvents.Death:
                case SmartEvents.Kill:
                case SmartEvents.SummonedUnit:
                case SmartEvents.SummonedUnitDies:
                case SmartEvents.SpellHit:
                case SmartEvents.SpellHitTarget:
                case SmartEvents.Damaged:
                case SmartEvents.ReceiveHeal:
                case SmartEvents.ReceiveEmote:
                case SmartEvents.JustSummoned:
                case SmartEvents.DamagedTarget:
                case SmartEvents.SummonDespawned:
                case SmartEvents.PassengerBoarded:
                case SmartEvents.PassengerRemoved:
                case SmartEvents.GossipHello:
                case SmartEvents.GossipSelect:
                case SmartEvents.AcceptedQuest:
                case SmartEvents.RewardQuest:
                case SmartEvents.FollowCompleted:
                case SmartEvents.OnSpellclick:
                case SmartEvents.GoLootStateChanged:
                case SmartEvents.AreatriggerEnter:
                case SmartEvents.IcLos:
                case SmartEvents.OocLos:
                case SmartEvents.DistanceCreature:
                case SmartEvents.FriendlyHealthPCT:
                case SmartEvents.FriendlyIsCc:
                case SmartEvents.FriendlyMissingBuff:
                case SmartEvents.ActionDone:
                case SmartEvents.Range:
                case SmartEvents.VictimCasting:
                case SmartEvents.TargetBuffed:
                case SmartEvents.InstancePlayerEnter:
                case SmartEvents.TransportAddcreature:
                case SmartEvents.DataSet:
                case SmartEvents.QuestAccepted:
                case SmartEvents.QuestObjCompletion:
                case SmartEvents.QuestCompletion:
                case SmartEvents.QuestFail:
                case SmartEvents.QuestRewarded:
                case SmartEvents.SceneStart:
                case SmartEvents.SceneTrigger:
                case SmartEvents.SceneCancel:
                case SmartEvents.SceneComplete:
                case SmartEvents.SendEventTrigger:
                case SmartEvents.AreatriggerExit:
                    return true;
                default:
                    return false;
            }
        }

        static bool IsTargetValid(SmartScriptHolder e)
        {
            if (Math.Abs(e.Target.o) > 2 * MathFunctions.PI)
                Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} has abs(`target.o` = {e.Target.o}) > 2*PI (orientation is expressed in radians)");

            switch (e.GetTargetType())
            {
                case SmartTargets.CreatureDistance:
                case SmartTargets.CreatureRange:
                {
                    if (e.Target.unitDistance.creature != 0 && Global.ObjectMgr.GetCreatureTemplate(e.Target.unitDistance.creature) == null)
                    {
                        Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Creature entry {e.Target.unitDistance.creature} as target_param1, skipped.");
                        return false;
                    }
                    break;
                }
                case SmartTargets.GameobjectDistance:
                case SmartTargets.GameobjectRange:
                {
                    if (e.Target.goDistance.entry != 0 && Global.ObjectMgr.GetGameObjectTemplate(e.Target.goDistance.entry) == null)
                    {
                        Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent GameObject entry {e.Target.goDistance.entry} as target_param1, skipped.");
                        return false;
                    }
                    break;
                }
                case SmartTargets.CreatureGuid:
                {
                    if (e.Target.unitGUID.entry != 0 && !IsCreatureValid(e, e.Target.unitGUID.entry))
                        return false;

                    ulong guid = e.Target.unitGUID.dbGuid;
                    CreatureData data = Global.ObjectMgr.GetCreatureData(guid);
                    if (data == null)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} using invalid creature guid {guid} as target_param1, skipped.");
                        return false;
                    }
                    else if (e.Target.unitGUID.entry != 0 && e.Target.unitGUID.entry != data.Id)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} using invalid creature entry {e.Target.unitGUID.entry} (expected {data.Id}) for guid {guid} as target_param1, skipped.");
                        return false;
                    }
                    break;
                }
                case SmartTargets.GameobjectGuid:
                {
                    if (e.Target.goGUID.entry != 0 && !IsGameObjectValid(e, e.Target.goGUID.entry))
                        return false;

                    ulong guid = e.Target.goGUID.dbGuid;
                    GameObjectData data = Global.ObjectMgr.GetGameObjectData(guid);
                    if (data == null)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} using invalid gameobject guid {guid} as target_param1, skipped.");
                        return false;
                    }
                    else if (e.Target.goGUID.entry != 0 && e.Target.goGUID.entry != data.Id)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} using invalid gameobject entry {e.Target.goGUID.entry} (expected {data.Id}) for guid {guid} as target_param1, skipped.");
                        return false;
                    }
                    break;
                }
                case SmartTargets.PlayerDistance:
                case SmartTargets.ClosestPlayer:
                {
                    if (e.Target.playerDistance.dist == 0)
                    {
                        Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} has maxDist 0 as target_param1, skipped.");
                        return false;
                    }
                    break;
                }
                case SmartTargets.ActionInvoker:
                case SmartTargets.ActionInvokerVehicle:
                case SmartTargets.InvokerParty:
                    if (e.GetScriptType() != SmartScriptType.TimedActionlist && e.GetEventType() != SmartEvents.Link && !EventHasInvoker(e.Event.type))
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.GetEventType()} Action {e.GetActionType()} has invoker target, but event does not provide any invoker!");
                        return false;
                    }
                    break;
                case SmartTargets.HostileSecondAggro:
                case SmartTargets.HostileLastAggro:
                case SmartTargets.HostileRandom:
                case SmartTargets.HostileRandomNotTop:
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Target.hostilRandom.playerOnly);
                    break;
                case SmartTargets.Farthest:
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Target.farthest.playerOnly);
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Target.farthest.isInLos);
                    break;
                case SmartTargets.ClosestCreature:
                    if (e.Target.unitClosest.findCreatureAliveState < (uint)FindCreatureAliveState.Alive || e.Target.unitClosest.findCreatureAliveState >= (uint)FindCreatureAliveState.Max)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} has invalid alive state {e.Target.unitClosest.findCreatureAliveState}");
                        return false;
                    }
                    break;
                case SmartTargets.ClosestEnemy:
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Target.closestAttackable.playerOnly);
                    break;
                case SmartTargets.ClosestFriendly:
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Target.closestFriendly.playerOnly);
                    break;
                case SmartTargets.OwnerOrSummoner:
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Target.owner.useCharmerOrOwner);
                    break;
                case SmartTargets.ClosestGameobject:
                case SmartTargets.PlayerRange:
                case SmartTargets.Self:
                case SmartTargets.Victim:
                case SmartTargets.Position:
                case SmartTargets.None:
                case SmartTargets.ThreatList:
                case SmartTargets.Stored:
                case SmartTargets.LootRecipients:
                case SmartTargets.VehiclePassenger:
                case SmartTargets.ClosestUnspawnedGameobject:
                    break;
                default:
                    Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Not handled target_type({0}), Entry {1} SourceType {2} Event {3} Action {4}, skipped.", e.GetTargetType(), e.EntryOrGuid, e.GetScriptType(), e.EventId, e.GetActionType());
                    return false;
            }

            if (!CheckUnusedTargetParams(e))
                return false;

            return true;
        }

        static bool IsSpellVisualKitValid(SmartScriptHolder e, uint entry)
        {
            if (!CliDB.SpellVisualKitStorage.ContainsKey(entry))
            {
                Log.outError(LogFilter.Sql, $"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} uses non-existent SpellVisualKit entry {entry}, skipped.");
                return false;
            }
            return true;
        }

        static bool CheckUnusedEventParams(SmartScriptHolder e)
        {
            int paramsStructSize = e.Event.type switch
            {
                SmartEvents.UpdateIc => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
                SmartEvents.UpdateOoc => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
                SmartEvents.HealthPct => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
                SmartEvents.ManaPct => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
                SmartEvents.Aggro => 0,
                SmartEvents.Kill => Marshal.SizeOf(typeof(SmartEvent.Kill)),
                SmartEvents.Death => 0,
                SmartEvents.Evade => 0,
                SmartEvents.SpellHit => Marshal.SizeOf(typeof(SmartEvent.SpellHit)),
                SmartEvents.Range => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
                SmartEvents.OocLos => Marshal.SizeOf(typeof(SmartEvent.Los)),
                SmartEvents.Respawn => Marshal.SizeOf(typeof(SmartEvent.Respawn)),
                SmartEvents.VictimCasting => Marshal.SizeOf(typeof(SmartEvent.TargetCasting)),
                SmartEvents.FriendlyIsCc => Marshal.SizeOf(typeof(SmartEvent.FriendlyCC)),
                SmartEvents.FriendlyMissingBuff => Marshal.SizeOf(typeof(SmartEvent.MissingBuff)),
                SmartEvents.SummonedUnit => Marshal.SizeOf(typeof(SmartEvent.Summoned)),
                SmartEvents.AcceptedQuest => Marshal.SizeOf(typeof(SmartEvent.Quest)),
                SmartEvents.RewardQuest => Marshal.SizeOf(typeof(SmartEvent.Quest)),
                SmartEvents.ReachedHome => 0,
                SmartEvents.ReceiveEmote => Marshal.SizeOf(typeof(SmartEvent.Emote)),
                SmartEvents.HasAura => Marshal.SizeOf(typeof(SmartEvent.Aura)),
                SmartEvents.TargetBuffed => Marshal.SizeOf(typeof(SmartEvent.Aura)),
                SmartEvents.Reset => 0,
                SmartEvents.IcLos => Marshal.SizeOf(typeof(SmartEvent.Los)),
                SmartEvents.PassengerBoarded => Marshal.SizeOf(typeof(SmartEvent.MinMax)),
                SmartEvents.PassengerRemoved => Marshal.SizeOf(typeof(SmartEvent.MinMax)),
                SmartEvents.Charmed => Marshal.SizeOf(typeof(SmartEvent.Charm)),
                SmartEvents.SpellHitTarget => Marshal.SizeOf(typeof(SmartEvent.SpellHit)),
                SmartEvents.Damaged => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
                SmartEvents.DamagedTarget => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
                SmartEvents.Movementinform => Marshal.SizeOf(typeof(SmartEvent.MovementInform)),
                SmartEvents.SummonDespawned => Marshal.SizeOf(typeof(SmartEvent.Summoned)),
                SmartEvents.CorpseRemoved => 0,
                SmartEvents.AiInit => 0,
                SmartEvents.DataSet => Marshal.SizeOf(typeof(SmartEvent.DataSet)),
                SmartEvents.WaypointReached => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
                SmartEvents.TransportAddplayer => 0,
                SmartEvents.TransportAddcreature => Marshal.SizeOf(typeof(SmartEvent.TransportAddCreature)),
                SmartEvents.TransportRemovePlayer => 0,
                SmartEvents.TransportRelocate => Marshal.SizeOf(typeof(SmartEvent.TransportRelocate)),
                SmartEvents.InstancePlayerEnter => Marshal.SizeOf(typeof(SmartEvent.InstancePlayerEnter)),
                SmartEvents.AreatriggerEnter => 0,
                SmartEvents.QuestAccepted => 0,
                SmartEvents.QuestObjCompletion => 0,
                SmartEvents.QuestCompletion => 0,
                SmartEvents.QuestRewarded => 0,
                SmartEvents.QuestFail => 0,
                SmartEvents.TextOver => Marshal.SizeOf(typeof(SmartEvent.TextOver)),
                SmartEvents.ReceiveHeal => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
                SmartEvents.JustSummoned => 0,
                SmartEvents.WaypointPaused => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
                SmartEvents.WaypointResumed => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
                SmartEvents.WaypointStopped => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
                SmartEvents.WaypointEnded => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
                SmartEvents.TimedEventTriggered => Marshal.SizeOf(typeof(SmartEvent.TimedEvent)),
                SmartEvents.Update => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
                SmartEvents.Link => 0,
                SmartEvents.GossipSelect => Marshal.SizeOf(typeof(SmartEvent.Gossip)),
                SmartEvents.JustCreated => 0,
                SmartEvents.GossipHello => Marshal.SizeOf(typeof(SmartEvent.GossipHello)),
                SmartEvents.FollowCompleted => 0,
                SmartEvents.GameEventStart => Marshal.SizeOf(typeof(SmartEvent.GameEvent)),
                SmartEvents.GameEventEnd => Marshal.SizeOf(typeof(SmartEvent.GameEvent)),
                SmartEvents.GoLootStateChanged => Marshal.SizeOf(typeof(SmartEvent.GoLootStateChanged)),
                SmartEvents.GoEventInform => Marshal.SizeOf(typeof(SmartEvent.EventInform)),
                SmartEvents.ActionDone => Marshal.SizeOf(typeof(SmartEvent.DoAction)),
                SmartEvents.OnSpellclick => 0,
                SmartEvents.FriendlyHealthPCT => Marshal.SizeOf(typeof(SmartEvent.FriendlyHealthPct)),
                SmartEvents.DistanceCreature => Marshal.SizeOf(typeof(SmartEvent.Distance)),
                SmartEvents.DistanceGameobject => Marshal.SizeOf(typeof(SmartEvent.Distance)),
                SmartEvents.CounterSet => Marshal.SizeOf(typeof(SmartEvent.Counter)),
                SmartEvents.SceneStart => 0,
                SmartEvents.SceneTrigger => 0,
                SmartEvents.SceneCancel => 0,
                SmartEvents.SceneComplete => 0,
                SmartEvents.SummonedUnitDies => Marshal.SizeOf(typeof(SmartEvent.Summoned)),
                SmartEvents.OnSpellCast => Marshal.SizeOf(typeof(SmartEvent.SpellCast)),
                SmartEvents.OnSpellFailed => Marshal.SizeOf(typeof(SmartEvent.SpellCast)),
                SmartEvents.OnSpellStart => Marshal.SizeOf(typeof(SmartEvent.SpellCast)),
                SmartEvents.OnAuraApplied => Marshal.SizeOf(typeof(SmartEvent.SpellCast)),
                SmartEvents.OnAuraRemoved => Marshal.SizeOf(typeof(SmartEvent.SpellCast)),
                SmartEvents.OnDespawn => 0,
                SmartEvents.SendEventTrigger => 0,
                SmartEvents.AreatriggerExit => 0,
                _ => Marshal.SizeOf(typeof(SmartEvent.Raw)),
            };

            int rawCount = Marshal.SizeOf(typeof(SmartEvent.Raw)) / sizeof(uint);
            int paramsCount = paramsStructSize / sizeof(uint);

            for (int index = paramsCount; index < rawCount; index++)
            {
                uint value = 0;
                switch (index)
                {
                    case 0:
                        value = e.Event.raw.param1;
                        break;
                    case 1:
                        value = e.Event.raw.param2;
                        break;
                    case 2:
                        value = e.Event.raw.param3;
                        break;
                    case 3:
                        value = e.Event.raw.param4;
                        break;
                    case 4:
                        value = e.Event.raw.param5;
                        break;
                }

                if (value != 0)
                    Log.outWarn(LogFilter.Sql, $"SmartAIMgr: {e} has unused event_param{index + 1} with value {value}, it should be 0.");
            }

            bool eventUsesStringParam()
            {
                switch (e.GetEventType())
                {
                    case SmartEvents.SceneTrigger:
                        return true;
                    default:
                        break;
                }

                return false;
            }

            if (!eventUsesStringParam() && !e.Event.param_string.IsEmpty())
                Log.outWarn(LogFilter.Sql, $"SmartAIMgr: {e} has unused event_param_string with value {e.Event.param_string}, it should be NULL.");

            return true;
        }

        static bool CheckUnusedActionParams(SmartScriptHolder e)
        {
            int paramsStructSize = e.Action.type switch
            {
                SmartActions.None => 0,
                SmartActions.Talk => Marshal.SizeOf(typeof(SmartAction.Talk)),
                SmartActions.SetFaction => Marshal.SizeOf(typeof(SmartAction.Faction)),
                SmartActions.MorphToEntryOrModel => Marshal.SizeOf(typeof(SmartAction.MorphOrMount)),
                SmartActions.Sound => Marshal.SizeOf(typeof(SmartAction.Sound)),
                SmartActions.PlayEmote => Marshal.SizeOf(typeof(SmartAction.Emote)),
                SmartActions.FailQuest => Marshal.SizeOf(typeof(SmartAction.Quest)),
                SmartActions.OfferQuest => Marshal.SizeOf(typeof(SmartAction.QuestOffer)),
                SmartActions.SetReactState => Marshal.SizeOf(typeof(SmartAction.React)),
                SmartActions.ActivateGobject => 0,
                SmartActions.RandomEmote => Marshal.SizeOf(typeof(SmartAction.RandomEmote)),
                SmartActions.Cast => Marshal.SizeOf(typeof(SmartAction.Cast)),
                SmartActions.SummonCreature => Marshal.SizeOf(typeof(SmartAction.SummonCreature)),
                SmartActions.ThreatSinglePct => Marshal.SizeOf(typeof(SmartAction.ThreatPCT)),
                SmartActions.ThreatAllPct => Marshal.SizeOf(typeof(SmartAction.ThreatPCT)),
                SmartActions.SetIngamePhaseGroup => Marshal.SizeOf(typeof(SmartAction.IngamePhaseGroup)),
                SmartActions.SetEmoteState => Marshal.SizeOf(typeof(SmartAction.Emote)),
                SmartActions.AutoAttack => Marshal.SizeOf(typeof(SmartAction.AutoAttack)),
                SmartActions.AllowCombatMovement => Marshal.SizeOf(typeof(SmartAction.CombatMove)),
                SmartActions.SetEventPhase => Marshal.SizeOf(typeof(SmartAction.SetEventPhase)),
                SmartActions.IncEventPhase => Marshal.SizeOf(typeof(SmartAction.IncEventPhase)),
                SmartActions.Evade => Marshal.SizeOf(typeof(SmartAction.Evade)),
                SmartActions.FleeForAssist => Marshal.SizeOf(typeof(SmartAction.FleeAssist)),
                SmartActions.CombatStop => 0,
                SmartActions.RemoveAurasFromSpell => Marshal.SizeOf(typeof(SmartAction.RemoveAura)),
                SmartActions.Follow => Marshal.SizeOf(typeof(SmartAction.Follow)),
                SmartActions.RandomPhase => Marshal.SizeOf(typeof(SmartAction.RandomPhase)),
                SmartActions.RandomPhaseRange => Marshal.SizeOf(typeof(SmartAction.RandomPhaseRange)),
                SmartActions.ResetGobject => 0,
                SmartActions.CallKilledmonster => Marshal.SizeOf(typeof(SmartAction.KilledMonster)),
                SmartActions.SetInstData => Marshal.SizeOf(typeof(SmartAction.SetInstanceData)),
                SmartActions.SetInstData64 => Marshal.SizeOf(typeof(SmartAction.SetInstanceData64)),
                SmartActions.UpdateTemplate => Marshal.SizeOf(typeof(SmartAction.UpdateTemplate)),
                SmartActions.Die => 0,
                SmartActions.SetInCombatWithZone => 0,
                SmartActions.CallForHelp => Marshal.SizeOf(typeof(SmartAction.CallHelp)),
                SmartActions.SetSheath => Marshal.SizeOf(typeof(SmartAction.SetSheath)),
                SmartActions.ForceDespawn => Marshal.SizeOf(typeof(SmartAction.ForceDespawn)),
                SmartActions.SetInvincibilityHpLevel => Marshal.SizeOf(typeof(SmartAction.InvincHP)),
                SmartActions.MountToEntryOrModel => Marshal.SizeOf(typeof(SmartAction.MorphOrMount)),
                SmartActions.SetIngamePhaseId => Marshal.SizeOf(typeof(SmartAction.IngamePhaseId)),
                SmartActions.SetData => Marshal.SizeOf(typeof(SmartAction.SetData)),
                SmartActions.AttackStop => 0,
                SmartActions.SetVisibility => Marshal.SizeOf(typeof(SmartAction.Visibility)),
                SmartActions.SetActive => Marshal.SizeOf(typeof(SmartAction.Active)),
                SmartActions.AttackStart => 0,
                SmartActions.SummonGo => Marshal.SizeOf(typeof(SmartAction.SummonGO)),
                SmartActions.KillUnit => 0,
                SmartActions.ActivateTaxi => Marshal.SizeOf(typeof(SmartAction.Taxi)),
                SmartActions.WpStart => Marshal.SizeOf(typeof(SmartAction.WpStart)),
                SmartActions.WpPause => Marshal.SizeOf(typeof(SmartAction.WpPause)),
                SmartActions.WpStop => Marshal.SizeOf(typeof(SmartAction.WpStop)),
                SmartActions.AddItem => Marshal.SizeOf(typeof(SmartAction.Item)),
                SmartActions.RemoveItem => Marshal.SizeOf(typeof(SmartAction.Item)),
                SmartActions.SetRun => Marshal.SizeOf(typeof(SmartAction.SetRun)),
                SmartActions.SetDisableGravity => Marshal.SizeOf(typeof(SmartAction.SetDisableGravity)),
                SmartActions.Teleport => Marshal.SizeOf(typeof(SmartAction.Teleport)),
                SmartActions.SetCounter => Marshal.SizeOf(typeof(SmartAction.SetCounter)),
                SmartActions.StoreTargetList => Marshal.SizeOf(typeof(SmartAction.StoreTargets)),
                SmartActions.WpResume => 0,
                SmartActions.SetOrientation => 0,
                SmartActions.CreateTimedEvent => Marshal.SizeOf(typeof(SmartAction.TimeEvent)),
                SmartActions.Playmovie => Marshal.SizeOf(typeof(SmartAction.Movie)),
                SmartActions.MoveToPos => Marshal.SizeOf(typeof(SmartAction.MoveToPos)),
                SmartActions.EnableTempGobj => Marshal.SizeOf(typeof(SmartAction.EnableTempGO)),
                SmartActions.Equip => Marshal.SizeOf(typeof(SmartAction.Equip)),
                SmartActions.CloseGossip => 0,
                SmartActions.TriggerTimedEvent => Marshal.SizeOf(typeof(SmartAction.TimeEvent)),
                SmartActions.RemoveTimedEvent => Marshal.SizeOf(typeof(SmartAction.TimeEvent)),
                SmartActions.CallScriptReset => 0,
                SmartActions.SetRangedMovement => Marshal.SizeOf(typeof(SmartAction.SetRangedMovement)),
                SmartActions.CallTimedActionlist => Marshal.SizeOf(typeof(SmartAction.TimedActionList)),
                SmartActions.SetNpcFlag => Marshal.SizeOf(typeof(SmartAction.Flag)),
                SmartActions.AddNpcFlag => Marshal.SizeOf(typeof(SmartAction.Flag)),
                SmartActions.RemoveNpcFlag => Marshal.SizeOf(typeof(SmartAction.Flag)),
                SmartActions.SimpleTalk => Marshal.SizeOf(typeof(SmartAction.SimpleTalk)),
                SmartActions.SelfCast => Marshal.SizeOf(typeof(SmartAction.Cast)),
                SmartActions.CrossCast => Marshal.SizeOf(typeof(SmartAction.CrossCast)),
                SmartActions.CallRandomTimedActionlist => Marshal.SizeOf(typeof(SmartAction.RandTimedActionList)),
                SmartActions.CallRandomRangeTimedActionlist => Marshal.SizeOf(typeof(SmartAction.RandRangeTimedActionList)),
                SmartActions.RandomMove => Marshal.SizeOf(typeof(SmartAction.MoveRandom)),
                SmartActions.SetUnitFieldBytes1 => Marshal.SizeOf(typeof(SmartAction.SetunitByte)),
                SmartActions.RemoveUnitFieldBytes1 => Marshal.SizeOf(typeof(SmartAction.DelunitByte)),
                SmartActions.InterruptSpell => Marshal.SizeOf(typeof(SmartAction.InterruptSpellCasting)),
                SmartActions.AddDynamicFlag => Marshal.SizeOf(typeof(SmartAction.Flag)),
                SmartActions.RemoveDynamicFlag => Marshal.SizeOf(typeof(SmartAction.Flag)),
                SmartActions.JumpToPos => Marshal.SizeOf(typeof(SmartAction.Jump)),
                SmartActions.SendGossipMenu => Marshal.SizeOf(typeof(SmartAction.SendGossipMenu)),
                SmartActions.GoSetLootState => Marshal.SizeOf(typeof(SmartAction.SetGoLootState)),
                SmartActions.SendTargetToTarget => Marshal.SizeOf(typeof(SmartAction.SendTargetToTarget)),
                SmartActions.SetHomePos => 0,
                SmartActions.SetHealthRegen => Marshal.SizeOf(typeof(SmartAction.SetHealthRegen)),
                SmartActions.SetRoot => Marshal.SizeOf(typeof(SmartAction.SetRoot)),
                SmartActions.SummonCreatureGroup => Marshal.SizeOf(typeof(SmartAction.CreatureGroup)),
                SmartActions.SetPower => Marshal.SizeOf(typeof(SmartAction.Power)),
                SmartActions.AddPower => Marshal.SizeOf(typeof(SmartAction.Power)),
                SmartActions.RemovePower => Marshal.SizeOf(typeof(SmartAction.Power)),
                SmartActions.GameEventStop => Marshal.SizeOf(typeof(SmartAction.GameEventStop)),
                SmartActions.GameEventStart => Marshal.SizeOf(typeof(SmartAction.GameEventStart)),
                SmartActions.StartClosestWaypoint => Marshal.SizeOf(typeof(SmartAction.ClosestWaypointFromList)),
                SmartActions.MoveOffset => Marshal.SizeOf(typeof(SmartAction.MoveOffset)),
                SmartActions.RandomSound => Marshal.SizeOf(typeof(SmartAction.RandomSound)),
                SmartActions.SetCorpseDelay => Marshal.SizeOf(typeof(SmartAction.CorpseDelay)),
                SmartActions.DisableEvade => Marshal.SizeOf(typeof(SmartAction.DisableEvade)),
                SmartActions.GoSetGoState => Marshal.SizeOf(typeof(SmartAction.GoState)),
                SmartActions.AddThreat => Marshal.SizeOf(typeof(SmartAction.Threat)),
                SmartActions.LoadEquipment => Marshal.SizeOf(typeof(SmartAction.LoadEquipment)),
                SmartActions.TriggerRandomTimedEvent => Marshal.SizeOf(typeof(SmartAction.RandomTimedEvent)),
                SmartActions.PauseMovement => Marshal.SizeOf(typeof(SmartAction.PauseMovement)),
                SmartActions.PlayAnimkit => Marshal.SizeOf(typeof(SmartAction.AnimKit)),
                SmartActions.ScenePlay => Marshal.SizeOf(typeof(SmartAction.Scene)),
                SmartActions.SceneCancel => Marshal.SizeOf(typeof(SmartAction.Scene)),
                SmartActions.SpawnSpawngroup => Marshal.SizeOf(typeof(SmartAction.GroupSpawn)),
                SmartActions.DespawnSpawngroup => Marshal.SizeOf(typeof(SmartAction.GroupSpawn)),
                SmartActions.RespawnBySpawnId => Marshal.SizeOf(typeof(SmartAction.RespawnData)),
                SmartActions.InvokerCast => Marshal.SizeOf(typeof(SmartAction.Cast)),
                SmartActions.PlayCinematic => Marshal.SizeOf(typeof(SmartAction.Cinematic)),
                SmartActions.SetMovementSpeed => Marshal.SizeOf(typeof(SmartAction.MovementSpeed)),
                SmartActions.PlaySpellVisualKit => Marshal.SizeOf(typeof(SmartAction.SpellVisualKit)),
                SmartActions.OverrideLight => Marshal.SizeOf(typeof(SmartAction.OverrideLight)),
                SmartActions.OverrideWeather => Marshal.SizeOf(typeof(SmartAction.OverrideWeather)),
                SmartActions.SetAIAnimKit => 0,
                SmartActions.SetHover => Marshal.SizeOf(typeof(SmartAction.SetHover)),
                SmartActions.SetHealthPct => Marshal.SizeOf(typeof(SmartAction.SetHealthPct)),
                SmartActions.CreateConversation => Marshal.SizeOf(typeof(SmartAction.Conversation)),
                SmartActions.SetImmunePC => Marshal.SizeOf(typeof(SmartAction.SetImmunePC)),
                SmartActions.SetImmuneNPC => Marshal.SizeOf(typeof(SmartAction.SetImmuneNPC)),
                SmartActions.SetUninteractible => Marshal.SizeOf(typeof(SmartAction.SetUninteractible)),
                SmartActions.ActivateGameobject => Marshal.SizeOf(typeof(SmartAction.ActivateGameObject)),
                SmartActions.AddToStoredTargetList => Marshal.SizeOf(typeof(SmartAction.AddToStoredTargets)),
                SmartActions.BecomePersonalCloneForPlayer => Marshal.SizeOf(typeof(SmartAction.BecomePersonalClone)),
                SmartActions.TriggerGameEvent => Marshal.SizeOf(typeof(SmartAction.TriggerGameEvent)),
                SmartActions.DoAction => Marshal.SizeOf(typeof(SmartAction.DoAction)),
                SmartActions.CompleteQuest => Marshal.SizeOf(typeof(SmartAction.Quest)),
                SmartActions.CreditQuestObjectiveTalkTo => 0,
                SmartActions.DestroyConversation => Marshal.SizeOf(typeof(SmartAction.DestroyConversation)),
                SmartActions.EnterVehicle => Marshal.SizeOf(typeof(SmartAction.EnterVehicle)),
                SmartActions.BoardPassenger => Marshal.SizeOf(typeof(SmartAction.EnterVehicle)),
                SmartActions.ExitVehicle => 0,
                _ => Marshal.SizeOf(typeof(SmartAction.Raw)),
            };

            int rawCount = Marshal.SizeOf(typeof(SmartAction.Raw)) / sizeof(uint);
            int paramsCount = paramsStructSize / sizeof(uint);

            for (int index = paramsCount; index < rawCount; index++)
            {
                uint value = 0;
                switch (index)
                {
                    case 0:
                        value = e.Action.raw.param1;
                        break;
                    case 1:
                        value = e.Action.raw.param2;
                        break;
                    case 2:
                        value = e.Action.raw.param3;
                        break;
                    case 3:
                        value = e.Action.raw.param4;
                        break;
                    case 4:
                        value = e.Action.raw.param5;
                        break;
                    case 5:
                        value = e.Action.raw.param6;
                        break;
                }

                if (value != 0)
                    Log.outWarn(LogFilter.Sql, $"SmartAIMgr: {e} has unused action_param{index + 1} with value {value}, it should be 0.");
            }

            bool actionUsesStringParam()
            {
                switch (e.GetActionType())
                {
                    case SmartActions.CrossCast:
                        return true;
                    default:
                        break;
                }

                return false;
            }

            if (!actionUsesStringParam() && !e.Action.param_string.IsEmpty())
                Log.outWarn(LogFilter.Sql, $"SmartAIMgr: {e} has unused action_param_string with value {e.Action.param_string}, it should be NULL.");

            return true;
        }

        static bool CheckUnusedTargetParams(SmartScriptHolder e)
        {
            int paramsStructSize = e.Target.type switch
            {
                SmartTargets.None => 0,
                SmartTargets.Self => 0,
                SmartTargets.Victim => 0,
                SmartTargets.HostileSecondAggro => Marshal.SizeOf(typeof(SmartTarget.HostilRandom)),
                SmartTargets.HostileLastAggro => Marshal.SizeOf(typeof(SmartTarget.HostilRandom)),
                SmartTargets.HostileRandom => Marshal.SizeOf(typeof(SmartTarget.HostilRandom)),
                SmartTargets.HostileRandomNotTop => Marshal.SizeOf(typeof(SmartTarget.HostilRandom)),
                SmartTargets.ActionInvoker => 0,
                SmartTargets.Position => 0, //Uses X,Y,Z,O
                SmartTargets.CreatureRange => Marshal.SizeOf(typeof(SmartTarget.UnitRange)),
                SmartTargets.CreatureGuid => Marshal.SizeOf(typeof(SmartTarget.UnitGUID)),
                SmartTargets.CreatureDistance => Marshal.SizeOf(typeof(SmartTarget.UnitDistance)),
                SmartTargets.Stored => Marshal.SizeOf(typeof(SmartTarget.Stored)),
                SmartTargets.GameobjectRange => Marshal.SizeOf(typeof(SmartTarget.GoRange)),
                SmartTargets.GameobjectGuid => Marshal.SizeOf(typeof(SmartTarget.GoGUID)),
                SmartTargets.GameobjectDistance => Marshal.SizeOf(typeof(SmartTarget.GoDistance)),
                SmartTargets.InvokerParty => 0,
                SmartTargets.PlayerRange => Marshal.SizeOf(typeof(SmartTarget.PlayerRange)),
                SmartTargets.PlayerDistance => Marshal.SizeOf(typeof(SmartTarget.PlayerDistance)),
                SmartTargets.ClosestCreature => Marshal.SizeOf(typeof(SmartTarget.UnitClosest)),
                SmartTargets.ClosestGameobject => Marshal.SizeOf(typeof(SmartTarget.GoClosest)),
                SmartTargets.ClosestPlayer => Marshal.SizeOf(typeof(SmartTarget.PlayerDistance)),
                SmartTargets.ActionInvokerVehicle => 0,
                SmartTargets.OwnerOrSummoner => Marshal.SizeOf(typeof(SmartTarget.Owner)),
                SmartTargets.ThreatList => Marshal.SizeOf(typeof(SmartTarget.ThreatList)),
                SmartTargets.ClosestEnemy => Marshal.SizeOf(typeof(SmartTarget.ClosestAttackable)),
                SmartTargets.ClosestFriendly => Marshal.SizeOf(typeof(SmartTarget.ClosestFriendly)),
                SmartTargets.LootRecipients => 0,
                SmartTargets.Farthest => Marshal.SizeOf(typeof(SmartTarget.Farthest)),
                SmartTargets.VehiclePassenger => Marshal.SizeOf(typeof(SmartTarget.Vehicle)),
                SmartTargets.ClosestUnspawnedGameobject => Marshal.SizeOf(typeof(SmartTarget.GoClosest)),
                _ => Marshal.SizeOf(typeof(SmartTarget.Raw)),
            };

            int rawCount = Marshal.SizeOf(typeof(SmartTarget.Raw)) / sizeof(uint);
            int paramsCount = paramsStructSize / sizeof(uint);

            for (int index = paramsCount; index < rawCount; index++)
            {
                uint value = 0;
                switch (index)
                {
                    case 0:
                        value = e.Target.raw.param1;
                        break;
                    case 1:
                        value = e.Target.raw.param2;
                        break;
                    case 2:
                        value = e.Target.raw.param3;
                        break;
                    case 3:
                        value = e.Target.raw.param4;
                        break;
                }

                if (value != 0)
                    Log.outWarn(LogFilter.Sql, $"SmartAIMgr: {e} has unused target_param{index + 1} with value {value}, it must be 0, skipped.");
            }

            bool targetUsesStringParam()
            {
                switch (e.GetTargetType())
                {
                    case SmartTargets.CreatureRange:
                    case SmartTargets.CreatureDistance:
                    case SmartTargets.GameobjectRange:
                    case SmartTargets.GameobjectDistance:
                    case SmartTargets.ClosestCreature:
                    case SmartTargets.ClosestGameobject:
                        return true;
                    default:
                        break;
                }

                return false;
            }

            if (!targetUsesStringParam() && !e.Target.param_string.IsEmpty())
                Log.outWarn(LogFilter.Sql, $"SmartAIMgr: {e} has unused target_param_string with value {e.Target.param_string}, it should be NULL.");

            return true;
        }

        bool IsEventValid(SmartScriptHolder e)
        {
            if (e.Event.type >= SmartEvents.End)
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid event type ({2}), skipped.", e.EntryOrGuid, e.EventId, e.GetEventType());
                return false;
            }

            // in SMART_SCRIPT_TYPE_TIMED_ACTIONLIST all event types are overriden by core
            if (e.GetScriptType() != SmartScriptType.TimedActionlist && !Convert.ToBoolean(GetEventMask(e.Event.type) & GetTypeMask(e.GetScriptType())))
            {
                Log.outError(LogFilter.Scripts, "SmartAIMgr: EntryOrGuid {0}, event type {1} can not be used for Script type {2}", e.EntryOrGuid, e.GetEventType(), e.GetScriptType());
                return false;
            }
            if (e.Action.type <= 0 || e.Action.type >= SmartActions.End)
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid action type ({2}), skipped.", e.EntryOrGuid, e.EventId, e.GetActionType());
                return false;
            }
            if (e.Event.event_phase_mask > (uint)SmartEventPhaseBits.All)
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid phase mask ({2}), skipped.", e.EntryOrGuid, e.EventId, e.Event.event_phase_mask);
                return false;
            }
            if (e.Event.event_flags > SmartEventFlags.All)
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid event flags ({2}), skipped.", e.EntryOrGuid, e.EventId, e.Event.event_flags);
                return false;
            }
            if (e.Event.event_flags.HasFlag(SmartEventFlags.Deprecated))
            {
                Log.outError(LogFilter.Sql, $"SmartAIMgr: EntryOrGuid {e.EntryOrGuid} using event ({e.EventId}) has deprecated event flags ({e.Event.event_flags}), skipped.");
                return false;
            }
            if (e.Link != 0 && e.Link == e.EventId)
            {
                Log.outError(LogFilter.Sql, "SmartAIMgr: EntryOrGuid {0} SourceType {1}, Event {2}, Event is linking self (infinite loop), skipped.", e.EntryOrGuid, e.GetScriptType(), e.EventId);
                return false;
            }
            if (e.GetScriptType() == SmartScriptType.TimedActionlist)
            {
                e.Event.type = SmartEvents.UpdateOoc;//force default OOC, can change when calling the script!
                if (!IsMinMaxValid(e, e.Event.minMaxRepeat.min, e.Event.minMaxRepeat.max))
                    return false;

                if (!IsMinMaxValid(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax))
                    return false;
            }
            else
            {
                switch (e.Event.type)
                {
                    case SmartEvents.Update:
                    case SmartEvents.UpdateIc:
                    case SmartEvents.UpdateOoc:
                    case SmartEvents.HealthPct:
                    case SmartEvents.ManaPct:
                    case SmartEvents.Range:
                    case SmartEvents.Damaged:
                    case SmartEvents.DamagedTarget:
                    case SmartEvents.ReceiveHeal:
                        if (!IsMinMaxValid(e, e.Event.minMaxRepeat.min, e.Event.minMaxRepeat.max))
                            return false;

                        if (!IsMinMaxValid(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax))
                            return false;
                        break;
                    case SmartEvents.SpellHit:
                    case SmartEvents.SpellHitTarget:
                        if (e.Event.spellHit.spell != 0)
                        {
                            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(e.Event.spellHit.spell, Difficulty.None);
                            if (spellInfo == null)
                            {
                                Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Spell entry {e.Event.spellHit.spell}, skipped.");
                                return false;
                            }
                            if (e.Event.spellHit.school != 0 && ((SpellSchoolMask)e.Event.spellHit.school & spellInfo.SchoolMask) != spellInfo.SchoolMask)
                            {
                                Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses Spell entry {e.Event.spellHit.spell} with invalid school mask, skipped.");
                                return false;
                            }
                        }
                        if (!IsMinMaxValid(e, e.Event.spellHit.cooldownMin, e.Event.spellHit.cooldownMax))
                            return false;
                        break;
                    case SmartEvents.OnSpellCast:
                    case SmartEvents.OnSpellFailed:
                    case SmartEvents.OnSpellStart:
                    case SmartEvents.OnAuraApplied:
                    case SmartEvents.OnAuraRemoved:
                    {
                        if (!IsSpellValid(e, e.Event.spellCast.spell))
                            return false;

                        if (!IsMinMaxValid(e, e.Event.spellCast.cooldownMin, e.Event.spellCast.cooldownMax))
                            return false;
                        break;
                    }
                    case SmartEvents.OocLos:
                    case SmartEvents.IcLos:
                        if (!IsMinMaxValid(e, e.Event.los.cooldownMin, e.Event.los.cooldownMax))
                            return false;
                        if (e.Event.los.hostilityMode >= (uint)LOSHostilityMode.End)
                        {
                            Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses hostilityMode with invalid value {e.Event.los.hostilityMode} (max allowed value {LOSHostilityMode.End - 1}), skipped.");
                            return false;
                        }
                        TC_SAI_IS_BOOLEAN_VALID(e, e.Event.los.playerOnly);
                        break;
                    case SmartEvents.Respawn:
                        if (e.Event.respawn.type == (uint)SmartRespawnCondition.Map && CliDB.MapStorage.LookupByKey(e.Event.respawn.map) == null)
                        {
                            Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Map entry {e.Event.respawn.map}, skipped.");
                            return false;
                        }
                        if (e.Event.respawn.type == (uint)SmartRespawnCondition.Area && !CliDB.AreaTableStorage.ContainsKey(e.Event.respawn.area))
                        {
                            Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Area entry {e.Event.respawn.area}, skipped.");
                            return false;
                        }
                        break;
                    case SmartEvents.FriendlyIsCc:
                        if (!IsMinMaxValid(e, e.Event.friendlyCC.repeatMin, e.Event.friendlyCC.repeatMax))
                            return false;
                        break;
                    case SmartEvents.FriendlyMissingBuff:
                    {
                        if (!IsSpellValid(e, e.Event.missingBuff.spell))
                            return false;

                        if (!NotNULL(e, e.Event.missingBuff.radius))
                            return false;

                        if (!IsMinMaxValid(e, e.Event.missingBuff.repeatMin, e.Event.missingBuff.repeatMax))
                            return false;
                        break;
                    }
                    case SmartEvents.Kill:
                        if (!IsMinMaxValid(e, e.Event.kill.cooldownMin, e.Event.kill.cooldownMax))
                            return false;

                        if (e.Event.kill.creature != 0 && !IsCreatureValid(e, e.Event.kill.creature))
                            return false;

                        TC_SAI_IS_BOOLEAN_VALID(e, e.Event.kill.playerOnly);
                        break;
                    case SmartEvents.VictimCasting:
                        if (e.Event.targetCasting.spellId > 0 && !Global.SpellMgr.HasSpellInfo(e.Event.targetCasting.spellId, Difficulty.None))
                        {
                            Log.outError(LogFilter.Sql, $"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} uses non-existent Spell entry {e.Event.spellHit.spell}, skipped.");
                            return false;
                        }

                        if (!IsMinMaxValid(e, e.Event.minMax.repeatMin, e.Event.minMax.repeatMax))
                            return false;
                        break;
                    case SmartEvents.PassengerBoarded:
                    case SmartEvents.PassengerRemoved:
                        if (!IsMinMaxValid(e, e.Event.minMax.repeatMin, e.Event.minMax.repeatMax))
                            return false;
                        break;
                    case SmartEvents.SummonDespawned:
                    case SmartEvents.SummonedUnit:
                    case SmartEvents.SummonedUnitDies:
                        if (e.Event.summoned.creature != 0 && !IsCreatureValid(e, e.Event.summoned.creature))
                            return false;

                        if (!IsMinMaxValid(e, e.Event.summoned.cooldownMin, e.Event.summoned.cooldownMax))
                            return false;
                        break;
                    case SmartEvents.AcceptedQuest:
                    case SmartEvents.RewardQuest:
                        if (e.Event.quest.questId != 0 && !IsQuestValid(e, e.Event.quest.questId))
                            return false;

                        if (!IsMinMaxValid(e, e.Event.quest.cooldownMin, e.Event.quest.cooldownMax))
                            return false;
                        break;
                    case SmartEvents.ReceiveEmote:
                    {
                        if (e.Event.emote.emoteId != 0 && !IsTextEmoteValid(e, e.Event.emote.emoteId))
                            return false;

                        if (!IsMinMaxValid(e, e.Event.emote.cooldownMin, e.Event.emote.cooldownMax))
                            return false;
                        break;
                    }
                    case SmartEvents.HasAura:
                    case SmartEvents.TargetBuffed:
                    {
                        if (!IsSpellValid(e, e.Event.aura.spell))
                            return false;

                        if (!IsMinMaxValid(e, e.Event.aura.repeatMin, e.Event.aura.repeatMax))
                            return false;
                        break;
                    }
                    case SmartEvents.TransportAddcreature:
                    {
                        if (e.Event.transportAddCreature.creature != 0 && !IsCreatureValid(e, e.Event.transportAddCreature.creature))
                            return false;
                        break;
                    }
                    case SmartEvents.Movementinform:
                    {
                        if (MotionMaster.IsInvalidMovementGeneratorType((MovementGeneratorType)e.Event.movementInform.type))
                        {
                            Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses invalid Motion type {e.Event.movementInform.type}, skipped.");
                            return false;
                        }
                        break;
                    }
                    case SmartEvents.DataSet:
                    {
                        if (!IsMinMaxValid(e, e.Event.dataSet.cooldownMin, e.Event.dataSet.cooldownMax))
                            return false;
                        break;
                    }
                    case SmartEvents.TextOver:
                    {
                        if (!IsTextValid(e, e.Event.textOver.textGroupID))
                            return false;
                        break;
                    }
                    case SmartEvents.GameEventStart:
                    case SmartEvents.GameEventEnd:
                    {
                        var events = Global.GameEventMgr.GetEventMap();
                        if (e.Event.gameEvent.gameEventId >= events.Length || !events[e.Event.gameEvent.gameEventId].IsValid())
                            return false;

                        break;
                    }
                    case SmartEvents.FriendlyHealthPCT:
                        if (!IsMinMaxValid(e, e.Event.friendlyHealthPct.repeatMin, e.Event.friendlyHealthPct.repeatMax))
                            return false;

                        if (e.Event.friendlyHealthPct.maxHpPct > 100 || e.Event.friendlyHealthPct.minHpPct > 100)
                        {
                            Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} has pct value above 100, skipped.");
                            return false;
                        }

                        switch (e.GetTargetType())
                        {
                            case SmartTargets.CreatureRange:
                            case SmartTargets.CreatureGuid:
                            case SmartTargets.CreatureDistance:
                            case SmartTargets.ClosestCreature:
                            case SmartTargets.ClosestPlayer:
                            case SmartTargets.PlayerRange:
                            case SmartTargets.PlayerDistance:
                                break;
                            case SmartTargets.ActionInvoker:
                                if (!NotNULL(e, e.Event.friendlyHealthPct.radius))
                                    return false;
                                break;
                            default:
                                Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses invalid target_type {e.GetTargetType()}, skipped.");
                                return false;
                        }
                        break;
                    case SmartEvents.DistanceCreature:
                        if (e.Event.distance.guid == 0 && e.Event.distance.entry == 0)
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_CREATURE did not provide creature guid or entry, skipped.");
                            return false;
                        }

                        if (e.Event.distance.guid != 0 && e.Event.distance.entry != 0)
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_CREATURE provided both an entry and guid, skipped.");
                            return false;
                        }

                        if (e.Event.distance.guid != 0 && Global.ObjectMgr.GetCreatureData(e.Event.distance.guid) == null)
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_CREATURE using invalid creature guid {0}, skipped.", e.Event.distance.guid);
                            return false;
                        }

                        if (e.Event.distance.entry != 0 && Global.ObjectMgr.GetCreatureTemplate(e.Event.distance.entry) == null)
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_CREATURE using invalid creature entry {0}, skipped.", e.Event.distance.entry);
                            return false;
                        }
                        break;
                    case SmartEvents.DistanceGameobject:
                        if (e.Event.distance.guid == 0 && e.Event.distance.entry == 0)
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_GAMEOBJECT did not provide gameobject guid or entry, skipped.");
                            return false;
                        }

                        if (e.Event.distance.guid != 0 && e.Event.distance.entry != 0)
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_GAMEOBJECT provided both an entry and guid, skipped.");
                            return false;
                        }

                        if (e.Event.distance.guid != 0 && Global.ObjectMgr.GetGameObjectData(e.Event.distance.guid) == null)
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_GAMEOBJECT using invalid gameobject guid {0}, skipped.", e.Event.distance.guid);
                            return false;
                        }

                        if (e.Event.distance.entry != 0 && Global.ObjectMgr.GetGameObjectTemplate(e.Event.distance.entry) == null)
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_GAMEOBJECT using invalid gameobject entry {0}, skipped.", e.Event.distance.entry);
                            return false;
                        }
                        break;
                    case SmartEvents.CounterSet:
                        if (!IsMinMaxValid(e, e.Event.counter.cooldownMin, e.Event.counter.cooldownMax))
                            return false;

                        if (e.Event.counter.id == 0)
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_COUNTER_SET using invalid counter id {0}, skipped.", e.Event.counter.id);
                            return false;
                        }

                        if (e.Event.counter.value == 0)
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_COUNTER_SET using invalid value {0}, skipped.", e.Event.counter.value);
                            return false;
                        }
                        break;
                    case SmartEvents.Reset:
                        if (e.Action.type == SmartActions.CallScriptReset)
                        {
                            // There might be SMART_TARGET_* cases where this should be allowed, they will be handled if needed
                            Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses event SMART_EVENT_RESET and action SMART_ACTION_CALL_SCRIPT_RESET, skipped.");
                            return false;
                        }
                        break;
                    case SmartEvents.Charmed:
                        TC_SAI_IS_BOOLEAN_VALID(e, e.Event.charm.onRemove);
                        break;
                    case SmartEvents.QuestObjCompletion:
                        if (Global.ObjectMgr.GetQuestObjective(e.Event.questObjective.id) == null)
                        {
                            Log.outError(LogFilter.Sql, $"SmartAIMgr: Event SMART_EVENT_QUEST_OBJ_COMPLETION using invalid objective id {e.Event.questObjective.id}, skipped.");
                            return false;
                        }
                        break;
                    case SmartEvents.QuestAccepted:
                    case SmartEvents.QuestCompletion:
                    case SmartEvents.QuestFail:
                    case SmartEvents.QuestRewarded:
                        break;
                    case SmartEvents.Link:
                    case SmartEvents.GoLootStateChanged:
                    case SmartEvents.GoEventInform:
                    case SmartEvents.TimedEventTriggered:
                    case SmartEvents.InstancePlayerEnter:
                    case SmartEvents.TransportRelocate:
                    case SmartEvents.CorpseRemoved:
                    case SmartEvents.AiInit:
                    case SmartEvents.ActionDone:
                    case SmartEvents.TransportAddplayer:
                    case SmartEvents.TransportRemovePlayer:
                    case SmartEvents.Aggro:
                    case SmartEvents.Death:
                    case SmartEvents.Evade:
                    case SmartEvents.ReachedHome:
                    case SmartEvents.JustSummoned:
                    case SmartEvents.WaypointReached:
                    case SmartEvents.WaypointPaused:
                    case SmartEvents.WaypointResumed:
                    case SmartEvents.WaypointStopped:
                    case SmartEvents.WaypointEnded:
                    case SmartEvents.AreatriggerEnter:
                    case SmartEvents.AreatriggerExit:
                    case SmartEvents.GossipSelect:
                    case SmartEvents.GossipHello:
                    case SmartEvents.JustCreated:
                    case SmartEvents.FollowCompleted:
                    case SmartEvents.OnSpellclick:
                    case SmartEvents.OnDespawn:
                    case SmartEvents.SceneStart:
                    case SmartEvents.SceneCancel:
                    case SmartEvents.SceneComplete:
                    case SmartEvents.SceneTrigger:
                    case SmartEvents.SendEventTrigger:
                        break;

                    //Unused
                    case SmartEvents.TargetHealthPct:
                    case SmartEvents.FriendlyHealth:
                    case SmartEvents.TargetManaPct:
                    case SmartEvents.CharmedTarget:
                    case SmartEvents.WaypointStart:
                    case SmartEvents.PhaseChange:
                    case SmartEvents.IsBehindTarget:
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: Unused event_type {e} skipped.");
                        return false;
                    default:
                        Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Not handled event_type({0}), Entry {1} SourceType {2} Event {3} Action {4}, skipped.", e.GetEventType(), e.EntryOrGuid, e.GetScriptType(), e.EventId, e.GetActionType());
                        return false;
                }
                if (e.Event.event_flags.HasAnyFlag(SmartEventFlags.ActionlistWaits))
                {
                    Log.outError(LogFilter.Sql, "SmartAIMgr: {e}, uses SMART_EVENT_FLAG_ACTIONLIST_WAITS but is not part of a timed actionlist.");
                    return false;
                }
            }

            if (!CheckUnusedEventParams(e))
                return false;

            switch (e.GetActionType())
            {
                case SmartActions.Talk:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.talk.useTalkTarget);

                    if (!IsTextValid(e, e.Action.talk.textGroupId))
                        return false;
                    break;
                }
                case SmartActions.SimpleTalk:
                {
                    if (!IsTextValid(e, e.Action.simpleTalk.textGroupId))
                        return false;
                    break;
                }
                case SmartActions.SetFaction:
                    if (e.Action.faction.factionId != 0 && CliDB.FactionTemplateStorage.LookupByKey(e.Action.faction.factionId) == null)
                    {
                        Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Faction {e.Action.faction.factionId}, skipped.");
                        return false;
                    }
                    break;
                case SmartActions.MorphToEntryOrModel:
                case SmartActions.MountToEntryOrModel:
                    if (e.Action.morphOrMount.creature != 0 || e.Action.morphOrMount.model != 0)
                    {
                        if (e.Action.morphOrMount.creature > 0 && Global.ObjectMgr.GetCreatureTemplate(e.Action.morphOrMount.creature) == null)
                        {
                            Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Creature entry {e.Action.morphOrMount.creature}, skipped.");
                            return false;
                        }

                        if (e.Action.morphOrMount.model != 0)
                        {
                            if (e.Action.morphOrMount.creature != 0)
                            {
                                Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} has ModelID set with also set CreatureId, skipped.");
                                return false;
                            }
                            else if (!CliDB.CreatureDisplayInfoStorage.ContainsKey(e.Action.morphOrMount.model))
                            {
                                Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Model id {e.Action.morphOrMount.model}, skipped.");
                                return false;
                            }
                        }
                    }
                    break;
                case SmartActions.Sound:
                    if (!IsSoundValid(e, e.Action.sound.soundId))
                        return false;

                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.sound.onlySelf);
                    break;
                case SmartActions.SetEmoteState:
                case SmartActions.PlayEmote:
                    if (!IsEmoteValid(e, e.Action.emote.emoteId))
                        return false;
                    break;
                case SmartActions.PlayAnimkit:
                    if (e.Action.animKit.animKit != 0 && !IsAnimKitValid(e, e.Action.animKit.animKit))
                        return false;

                    if (e.Action.animKit.type > 3)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses invalid AnimKit type {e.Action.animKit.type}, skipped.");
                        return false;
                    }
                    break;
                case SmartActions.PlaySpellVisualKit:
                    if (e.Action.spellVisualKit.spellVisualKitId != 0 && !IsSpellVisualKitValid(e, e.Action.spellVisualKit.spellVisualKitId))
                        return false;
                    break;
                case SmartActions.OfferQuest:
                    if (!IsQuestValid(e, e.Action.questOffer.questId))
                        return false;

                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.questOffer.directAdd);
                    break;
                case SmartActions.FailQuest:
                    if (!IsQuestValid(e, e.Action.quest.questId))
                        return false;
                    break;
                case SmartActions.ActivateTaxi:
                {
                    if (!CliDB.TaxiPathStorage.ContainsKey(e.Action.taxi.id))
                    {
                        Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses invalid Taxi path ID {e.Action.taxi.id}, skipped.");
                        return false;
                    }
                    break;
                }
                case SmartActions.RandomEmote:
                    if (e.Action.randomEmote.emote1 != 0 && !IsEmoteValid(e, e.Action.randomEmote.emote1))
                        return false;
                    if (e.Action.randomEmote.emote2 != 0 && !IsEmoteValid(e, e.Action.randomEmote.emote2))
                        return false;
                    if (e.Action.randomEmote.emote3 != 0 && !IsEmoteValid(e, e.Action.randomEmote.emote3))
                        return false;
                    if (e.Action.randomEmote.emote4 != 0 && !IsEmoteValid(e, e.Action.randomEmote.emote4))
                        return false;
                    if (e.Action.randomEmote.emote5 != 0 && !IsEmoteValid(e, e.Action.randomEmote.emote5))
                        return false;
                    if (e.Action.randomEmote.emote6 != 0 && !IsEmoteValid(e, e.Action.randomEmote.emote6))
                        return false;
                    break;
                case SmartActions.RandomSound:
                    if (e.Action.randomSound.sound1 != 0 && !IsSoundValid(e, e.Action.randomSound.sound1))
                        return false;
                    if (e.Action.randomSound.sound2 != 0 && !IsSoundValid(e, e.Action.randomSound.sound2))
                        return false;
                    if (e.Action.randomSound.sound3 != 0 && !IsSoundValid(e, e.Action.randomSound.sound3))
                        return false;
                    if (e.Action.randomSound.sound4 != 0 && !IsSoundValid(e, e.Action.randomSound.sound4))
                        return false;

                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.randomSound.onlySelf);
                    break;
                case SmartActions.Cast:
                {
                    if (!IsSpellValid(e, e.Action.cast.spell))
                        return false;

                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(e.Action.cast.spell, Difficulty.None);
                    foreach (var spellEffectInfo in spellInfo.GetEffects())
                    {
                        if (spellEffectInfo.IsEffect(SpellEffectName.KillCredit) || spellEffectInfo.IsEffect(SpellEffectName.KillCredit2))
                        {
                            if (spellEffectInfo.TargetA.GetTarget() == Targets.UnitCaster)
                                Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} Effect: SPELL_EFFECT_KILL_CREDIT: (SpellId: {e.Action.cast.spell} targetA: {spellEffectInfo.TargetA.GetTarget()} - targetB: {spellEffectInfo.TargetB.GetTarget()}) has invalid target for this Action");
                        }
                    }
                    if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.WaitForHit) && !e.Event.event_flags.HasAnyFlag(SmartEventFlags.ActionlistWaits))
                    {
                        Log.outError(LogFilter.Sql, "SmartAIMgr: {e} uses SMARTCAST_WAIT_FOR_HIT but is not part of actionlist event that has SMART_EVENT_FLAG_ACTIONLIST_WAITS");
                        return false;
                    }
                    break;
                }
                case SmartActions.CrossCast:
                {
                    if (!IsSpellValid(e, e.Action.crossCast.spell))
                        return false;

                    SmartTargets targetType = (SmartTargets)e.Action.crossCast.targetType;
                    if (targetType == SmartTargets.CreatureGuid || targetType == SmartTargets.GameobjectGuid)
                    {
                        if (e.Action.crossCast.targetParam2 != 0)
                        {
                            if (targetType == SmartTargets.CreatureGuid && !IsCreatureValid(e, e.Action.crossCast.targetParam2))
                                return false;
                            else if (targetType == SmartTargets.GameobjectGuid && !IsGameObjectValid(e, e.Action.crossCast.targetParam2))
                                return false;
                        }

                        ulong guid = e.Action.crossCast.targetParam1;
                        SpawnObjectType spawnType = targetType == SmartTargets.CreatureGuid ? SpawnObjectType.Creature : SpawnObjectType.GameObject;
                        var data = Global.ObjectMgr.GetSpawnData(spawnType, guid);
                        if (data == null)
                        {
                            Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} specifies invalid CasterTargetType guid ({spawnType},{guid})");
                            return false;
                        }
                        else if (e.Action.crossCast.targetParam2 != 0 && e.Action.crossCast.targetParam2 != data.Id)
                        {
                            Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} specifies invalid entry {e.Action.crossCast.targetParam2} (expected {data.Id}) for CasterTargetType guid ({spawnType},{guid})");
                            return false;
                        }
                    }
                    if (e.Action.crossCast.castFlags.HasAnyFlag((uint)SmartCastFlags.WaitForHit) && !e.Event.event_flags.HasAnyFlag(SmartEventFlags.ActionlistWaits))
                    {
                        Log.outError(LogFilter.Sql, "SmartAIMgr: {e} uses SMARTCAST_WAIT_FOR_HIT but is not part of actionlist event that has SMART_EVENT_FLAG_ACTIONLIST_WAITS");
                        return false;
                    }
                    break;
                }
                case SmartActions.InvokerCast:
                    if (e.GetScriptType() != SmartScriptType.TimedActionlist && e.GetEventType() != SmartEvents.Link && !EventHasInvoker(e.Event.type))
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} has invoker cast action, but event does not provide any invoker!");
                        return false;
                    }
                    if (!IsSpellValid(e, e.Action.cast.spell))
                        return false;
                    break;
                case SmartActions.SelfCast:
                    if (!IsSpellValid(e, e.Action.cast.spell))
                        return false;
                    if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.WaitForHit) && !e.Event.event_flags.HasAnyFlag(SmartEventFlags.ActionlistWaits))
                    {
                        Log.outError(LogFilter.Sql, "SmartAIMgr: {e} uses SMARTCAST_WAIT_FOR_HIT but is not part of actionlist event that has SMART_EVENT_FLAG_ACTIONLIST_WAITS");
                        return false;
                    }
                    break;
                case SmartActions.SetEventPhase:
                    if (e.Action.setEventPhase.phase >= (uint)SmartPhase.Max)
                    {
                        Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} attempts to set phase {e.Action.setEventPhase.phase}. Phase mask cannot be used past phase {SmartPhase.Max - 1}, skipped.");
                        return false;
                    }
                    break;
                case SmartActions.IncEventPhase:
                    if (e.Action.incEventPhase.inc == 0 && e.Action.incEventPhase.dec == 0)
                    {
                        Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} is incrementing phase by 0, skipped.");
                        return false;
                    }
                    else if (e.Action.incEventPhase.inc > (uint)SmartPhase.Max || e.Action.incEventPhase.dec > (uint)SmartPhase.Max)
                    {
                        Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} attempts to increment phase by too large value, skipped.");
                        return false;
                    }
                    break;
                case SmartActions.RemoveAurasFromSpell:
                    if (e.Action.removeAura.spell != 0 && !IsSpellValid(e, e.Action.removeAura.spell))
                        return false;

                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.removeAura.onlyOwnedAuras);
                    break;
                case SmartActions.RandomPhase:
                {
                    if (e.Action.randomPhase.phase1 >= (uint)SmartPhase.Max ||
                        e.Action.randomPhase.phase2 >= (uint)SmartPhase.Max ||
                        e.Action.randomPhase.phase3 >= (uint)SmartPhase.Max ||
                        e.Action.randomPhase.phase4 >= (uint)SmartPhase.Max ||
                        e.Action.randomPhase.phase5 >= (uint)SmartPhase.Max ||
                        e.Action.randomPhase.phase6 >= (uint)SmartPhase.Max)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} attempts to set invalid phase, skipped.");
                        return false;
                    }

                    break;
                }
                case SmartActions.RandomPhaseRange:       //PhaseMin, PhaseMax
                {
                    if (e.Action.randomPhaseRange.phaseMin >= (uint)SmartPhase.Max ||
                        e.Action.randomPhaseRange.phaseMax >= (uint)SmartPhase.Max)
                    {
                        Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} attempts to set invalid phase, skipped.");
                        return false;
                    }
                    if (!IsMinMaxValid(e, e.Action.randomPhaseRange.phaseMin, e.Action.randomPhaseRange.phaseMax))
                        return false;
                    break;
                }
                case SmartActions.SummonCreature:
                    if (!IsCreatureValid(e, e.Action.summonCreature.creature))
                        return false;

                    if (e.Action.summonCreature.type < (uint)TempSummonType.TimedOrDeadDespawn || e.Action.summonCreature.type > (uint)TempSummonType.ManualDespawn)
                    {
                        Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses incorrect TempSummonType {e.Action.summonCreature.type}, skipped.");
                        return false;
                    }

                    if (e.Action.summonCreature.createdBySpell != 0)
                    {
                        if (!IsSpellValid(e, e.Action.summonCreature.createdBySpell))
                            return false;

                        bool propertiesFound = Global.SpellMgr.GetSpellInfo(e.Action.summonCreature.createdBySpell, Difficulty.None).GetEffects().Any(spellEffectInfo =>
                        {
                            return spellEffectInfo.IsEffect(SpellEffectName.Summon) && CliDB.SummonPropertiesStorage.HasRecord((uint)spellEffectInfo.MiscValueB);
                        });

                        if (!propertiesFound)
                        {
                            Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} Spell {e.Action.summonCreature.createdBySpell} is not a summon creature spell.");
                            return false;
                        }
                    }
                    break;
                case SmartActions.CallKilledmonster:
                    if (!IsCreatureValid(e, e.Action.killedMonster.creature))
                        return false;

                    if (e.GetTargetType() == SmartTargets.Position)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses incorrect TargetType {e.GetTargetType()}, skipped.");
                        return false;
                    }
                    break;
                case SmartActions.UpdateTemplate:
                    if (e.Action.updateTemplate.creature != 0 && !IsCreatureValid(e, e.Action.updateTemplate.creature))
                        return false;

                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.updateTemplate.updateLevel);
                    break;
                case SmartActions.SetSheath:
                    if (e.Action.setSheath.sheath != 0 && e.Action.setSheath.sheath >= (uint)SheathState.Max)
                    {
                        Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses incorrect Sheath state {e.Action.setSheath.sheath}, skipped.");
                        return false;
                    }
                    break;
                case SmartActions.SetReactState:
                {
                    if (e.Action.react.state > (uint)ReactStates.Aggressive)
                    {
                        Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Creature {0} Event {1} Action {2} uses invalid React State {3}, skipped.", e.EntryOrGuid, e.EventId, e.GetActionType(), e.Action.react.state);
                        return false;
                    }
                    break;
                }
                case SmartActions.SummonGo:
                    if (!IsGameObjectValid(e, e.Action.summonGO.entry))
                        return false;
                    break;
                case SmartActions.RemoveItem:
                    if (!IsItemValid(e, e.Action.item.entry))
                        return false;

                    if (!NotNULL(e, e.Action.item.count))
                        return false;
                    break;
                case SmartActions.AddItem:
                    if (!IsItemValid(e, e.Action.item.entry))
                        return false;

                    if (!NotNULL(e, e.Action.item.count))
                        return false;
                    break;
                case SmartActions.Teleport:
                    if (!CliDB.MapStorage.ContainsKey(e.Action.teleport.mapID))
                    {
                        Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Map entry {e.Action.teleport.mapID}, skipped.");
                        return false;
                    }
                    break;
                case SmartActions.WpStop:
                    if (e.Action.wpStop.quest != 0 && !IsQuestValid(e, e.Action.wpStop.quest))
                        return false;

                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.wpStop.fail);
                    break;
                case SmartActions.WpStart:
                {
                    WaypointPath path = Global.WaypointMgr.GetPath(e.Action.wpStart.pathID);
                    if (path == null || path.Nodes.Empty())
                    {
                        Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent WaypointPath id {e.Action.wpStart.pathID}, skipped.");
                        return false;
                    }

                    if (e.Action.wpStart.quest != 0 && !IsQuestValid(e, e.Action.wpStart.quest))
                        return false;

                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.wpStart.run);
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.wpStart.repeat);
                    break;
                }
                case SmartActions.CreateTimedEvent:
                {
                    if (!IsMinMaxValid(e, e.Action.timeEvent.min, e.Action.timeEvent.max))
                        return false;

                    if (!IsMinMaxValid(e, e.Action.timeEvent.repeatMin, e.Action.timeEvent.repeatMax))
                        return false;
                    break;
                }
                case SmartActions.CallRandomRangeTimedActionlist:
                {
                    if (!IsMinMaxValid(e, e.Action.randRangeTimedActionList.idMin, e.Action.randRangeTimedActionList.idMax))
                        return false;
                    break;
                }
                case SmartActions.SetPower:
                case SmartActions.AddPower:
                case SmartActions.RemovePower:
                    if (e.Action.power.powerType > (int)PowerType.Max)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses non-existent Power {e.Action.power.powerType}, skipped.");
                        return false;
                    }
                    break;
                case SmartActions.GameEventStop:
                {
                    uint eventId = e.Action.gameEventStop.id;

                    var events = Global.GameEventMgr.GetEventMap();
                    if (eventId < 1 || eventId >= events.Length)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses non-existent event, eventId {e.Action.gameEventStop.id}, skipped.");
                        return false;
                    }

                    GameEventData eventData = events[eventId];
                    if (!eventData.IsValid())
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses non-existent event, eventId {e.Action.gameEventStop.id}, skipped.");
                        return false;
                    }
                    break;
                }
                case SmartActions.GameEventStart:
                {
                    uint eventId = e.Action.gameEventStart.id;

                    var events = Global.GameEventMgr.GetEventMap();
                    if (eventId < 1 || eventId >= events.Length)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses non-existent event, eventId {e.Action.gameEventStart.id}, skipped.");
                        return false;
                    }

                    GameEventData eventData = events[eventId];
                    if (!eventData.IsValid())
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses non-existent event, eventId {e.Action.gameEventStart.id}, skipped.");
                        return false;
                    }
                    break;
                }
                case SmartActions.Equip:
                {
                    if (e.GetScriptType() == SmartScriptType.Creature)
                    {
                        sbyte equipId = (sbyte)e.Action.equip.entry;

                        if (equipId != 0 && Global.ObjectMgr.GetEquipmentInfo((uint)e.EntryOrGuid, equipId) == null)
                        {
                            Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_EQUIP uses non-existent equipment info id {0} for creature {1}, skipped.", equipId, e.EntryOrGuid);
                            return false;
                        }
                    }

                    bool isValidEquipmentSlot(uint itemEntry, byte slot)
                    {
                        ItemRecord dbcItem = CliDB.ItemStorage.LookupByKey(itemEntry);
                        if (dbcItem == null)
                        {
                            Log.outError(LogFilter.Sql, $"SmartScript: SMART_ACTION_EQUIP uses unknown item {itemEntry} (slot {slot}) for creature {e.EntryOrGuid}, skipped.");
                            return false;
                        }

                        if (ItemConst.InventoryTypesEquipable.All(inventoryType => inventoryType != dbcItem.inventoryType))
                        {
                            Log.outError(LogFilter.Sql, $"SmartScript: SMART_ACTION_EQUIP uses item {itemEntry} (slot {slot}) not equipable in a hand for creature {e.EntryOrGuid}, skipped.");
                            return false;
                        }

                        return true;
                    }

                    if (e.Action.equip.slot1 != 0 && !isValidEquipmentSlot(e.Action.equip.slot1, 0))
                        return false;

                    if (e.Action.equip.slot2 != 0 && !isValidEquipmentSlot(e.Action.equip.slot2, 1))
                        return false;

                    if (e.Action.equip.slot3 != 0 && !isValidEquipmentSlot(e.Action.equip.slot3, 2))
                        return false;
                    break;
                }
                case SmartActions.SetInstData:
                {
                    if (e.Action.setInstanceData.type > 1)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses invalid data type {e.Action.setInstanceData.type} (value range 0-1), skipped.");
                        return false;
                    }
                    else if (e.Action.setInstanceData.type == 1)
                    {
                        if (e.Action.setInstanceData.data > (int)EncounterState.ToBeDecided)
                        {
                            Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses invalid boss state {e.Action.setInstanceData.data} (value range 0-5), skipped.");
                            return false;
                        }
                    }
                    break;
                }
                case SmartActions.SetIngamePhaseId:
                {
                    uint phaseId = e.Action.ingamePhaseId.id;
                    uint apply = e.Action.ingamePhaseId.apply;

                    if (apply != 0 && apply != 1)
                    {
                        Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_SET_INGAME_PHASE_ID uses invalid apply value {0} (Should be 0 or 1) for creature {1}, skipped", apply, e.EntryOrGuid);
                        return false;
                    }

                    if (!CliDB.PhaseStorage.ContainsKey(phaseId))
                    {
                        Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_SET_INGAME_PHASE_ID uses invalid phaseid {0} for creature {1}, skipped", phaseId, e.EntryOrGuid);
                        return false;
                    }
                    break;
                }
                case SmartActions.SetIngamePhaseGroup:
                {
                    uint phaseGroup = e.Action.ingamePhaseGroup.groupId;
                    uint apply = e.Action.ingamePhaseGroup.apply;

                    if (apply != 0 && apply != 1)
                    {
                        Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_SET_INGAME_PHASE_GROUP uses invalid apply value {0} (Should be 0 or 1) for creature {1}, skipped", apply, e.EntryOrGuid);
                        return false;
                    }

                    if (Global.DB2Mgr.GetPhasesForGroup(phaseGroup).Empty())
                    {
                        Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_SET_INGAME_PHASE_GROUP uses invalid phase group id {0} for creature {1}, skipped", phaseGroup, e.EntryOrGuid);
                        return false;
                    }
                    break;
                }
                case SmartActions.ScenePlay:
                {
                    if (Global.ObjectMgr.GetSceneTemplate(e.Action.scene.sceneId) == null)
                    {
                        Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_SCENE_PLAY uses sceneId {0} but scene don't exist, skipped", e.Action.scene.sceneId);
                        return false;
                    }

                    break;
                }
                case SmartActions.SceneCancel:
                {
                    if (Global.ObjectMgr.GetSceneTemplate(e.Action.scene.sceneId) == null)
                    {
                        Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_SCENE_CANCEL uses sceneId {0} but scene don't exist, skipped", e.Action.scene.sceneId);
                        return false;
                    }

                    break;
                }
                case SmartActions.RespawnBySpawnId:
                {
                    if (Global.ObjectMgr.GetSpawnData((SpawnObjectType)e.Action.respawnData.spawnType, e.Action.respawnData.spawnId) == null)
                    {
                        Log.outError(LogFilter.Sql, $"Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} specifies invalid spawn data ({e.Action.respawnData.spawnType},{e.Action.respawnData.spawnId})");
                        return false;
                    }
                    break;
                }
                case SmartActions.EnableTempGobj:
                {
                    if (e.Action.enableTempGO.duration == 0)
                    {
                        Log.outError(LogFilter.Sql, $"Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} does not specify duration");
                        return false;
                    }
                    break;
                }
                case SmartActions.PlayCinematic:
                {
                    if (!CliDB.CinematicSequencesStorage.ContainsKey(e.Action.cinematic.entry))
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: SMART_ACTION_PLAY_CINEMATIC {e} uses invalid entry {e.Action.cinematic.entry}, skipped.");
                        return false;
                    }

                    break;
                }
                case SmartActions.PauseMovement:
                {
                    if (e.Action.pauseMovement.pauseTimer == 0)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} does not specify pause duration");
                        return false;
                    }

                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.pauseMovement.force);
                    break;
                }
                case SmartActions.SetMovementSpeed:
                {
                    if (e.Action.movementSpeed.movementType >= (int)MovementGeneratorType.Max)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} uses invalid movementType {e.Action.movementSpeed.movementType}, skipped.");
                        return false;
                    }

                    if (e.Action.movementSpeed.speedInteger == 0 && e.Action.movementSpeed.speedFraction == 0)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} uses speed 0, skipped.");
                        return false;
                    }

                    break;
                }
                case SmartActions.OverrideLight:
                {
                    var areaEntry = CliDB.AreaTableStorage.LookupByKey(e.Action.overrideLight.zoneId);
                    if (areaEntry == null)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses non-existent zoneId {e.Action.overrideLight.zoneId}, skipped.");
                        return false;
                    }

                    if (areaEntry.ParentAreaID != 0 && areaEntry.HasFlag(AreaFlags.IsSubzone))
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses subzone (ID: {e.Action.overrideLight.zoneId}) instead of zone, skipped.");
                        return false;
                    }

                    if (!CliDB.LightStorage.ContainsKey(e.Action.overrideLight.areaLightId))
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses non-existent areaLightId {e.Action.overrideLight.areaLightId}, skipped.");
                        return false;
                    }

                    if (e.Action.overrideLight.overrideLightId != 0 && !CliDB.LightStorage.ContainsKey(e.Action.overrideLight.overrideLightId))
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses non-existent overrideLightId {e.Action.overrideLight.overrideLightId}, skipped.");
                        return false;
                    }

                    break;
                }
                case SmartActions.OverrideWeather:
                {
                    var areaEntry = CliDB.AreaTableStorage.LookupByKey(e.Action.overrideWeather.zoneId);
                    if (areaEntry == null)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses non-existent zoneId {e.Action.overrideWeather.zoneId}, skipped.");
                        return false;
                    }

                    if (areaEntry.ParentAreaID != 0 && areaEntry.HasFlag(AreaFlags.IsSubzone))
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses subzone (ID: {e.Action.overrideWeather.zoneId}) instead of zone, skipped.");
                        return false;
                    }

                    break;
                }
                case SmartActions.SetAIAnimKit:
                {
                    Log.outError(LogFilter.Sql, $"SmartAIMgr: Deprecated Event:({e}) skipped.");
                    break;
                }
                case SmartActions.SetHealthPct:
                {
                    if (e.Action.setHealthPct.percent > 100 || e.Action.setHealthPct.percent == 0)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} is trying to set invalid HP percent {e.Action.setHealthPct.percent}, skipped.");
                        return false;
                    }
                    break;
                }
                case SmartActions.AutoAttack:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.autoAttack.attack);
                    break;
                }
                case SmartActions.AllowCombatMovement:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.combatMove.move);
                    break;
                }
                case SmartActions.CallForHelp:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.callHelp.withEmote);
                    break;
                }
                case SmartActions.SetVisibility:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.visibility.state);
                    break;
                }
                case SmartActions.SetActive:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.active.state);
                    break;
                }
                case SmartActions.SetRun:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setRun.run);
                    break;
                }
                case SmartActions.SetDisableGravity:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setDisableGravity.disable);
                    break;
                }
                case SmartActions.SetCounter:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setCounter.reset);
                    break;
                }
                case SmartActions.CallTimedActionlist:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.timedActionList.allowOverride);
                    break;
                }
                case SmartActions.InterruptSpell:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.interruptSpellCasting.withDelayed);
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.interruptSpellCasting.withInstant);
                    break;
                }
                case SmartActions.FleeForAssist:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.fleeAssist.withEmote);
                    break;
                }
                case SmartActions.MoveToPos:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.moveToPos.transport);
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.moveToPos.disablePathfinding);
                    break;
                }
                case SmartActions.SetRoot:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setRoot.root);
                    break;
                }
                case SmartActions.DisableEvade:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.disableEvade.disable);
                    break;
                }
                case SmartActions.LoadEquipment:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.loadEquipment.force);
                    break;
                }
                case SmartActions.SetHover:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setHover.enable);
                    break;
                }
                case SmartActions.Evade:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.evade.toRespawnPosition);
                    break;
                }
                case SmartActions.SetHealthRegen:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setHealthRegen.regenHealth);
                    break;
                }
                case SmartActions.CreateConversation:
                {
                    if (Global.ConversationDataStorage.GetConversationTemplate(e.Action.conversation.id) == null)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: SMART_ACTION_CREATE_CONVERSATION Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} uses invalid entry {e.Action.conversation.id}, skipped.");
                        return false;
                    }

                    break;
                }
                case SmartActions.SetImmunePC:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setImmunePC.immunePC);
                    break;
                }
                case SmartActions.SetImmuneNPC:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setImmuneNPC.immuneNPC);
                    break;
                }
                case SmartActions.SetUninteractible:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.setUninteractible.uninteractible);
                    break;
                }
                case SmartActions.ActivateGameobject:
                {
                    if (!NotNULL(e, e.Action.activateGameObject.gameObjectAction))
                        return false;

                    if (e.Action.activateGameObject.gameObjectAction >= (uint)GameObjectActions.Max)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} has gameObjectAction parameter out of range (max allowed {(uint)GameObjectActions.Max - 1}, current value {e.Action.activateGameObject.gameObjectAction}), skipped.");
                        return false;
                    }

                    break;
                }
                case SmartActions.StartClosestWaypoint:
                case SmartActions.Follow:
                case SmartActions.SetOrientation:
                case SmartActions.StoreTargetList:
                case SmartActions.CombatStop:
                case SmartActions.Die:
                case SmartActions.SetInCombatWithZone:
                case SmartActions.WpResume:
                case SmartActions.KillUnit:
                case SmartActions.SetInvincibilityHpLevel:
                case SmartActions.ResetGobject:
                case SmartActions.AttackStart:
                case SmartActions.ThreatAllPct:
                case SmartActions.ThreatSinglePct:
                case SmartActions.SetInstData64:
                case SmartActions.SetData:
                case SmartActions.AttackStop:
                case SmartActions.WpPause:
                case SmartActions.ForceDespawn:
                case SmartActions.Playmovie:
                case SmartActions.CloseGossip:
                case SmartActions.TriggerTimedEvent:
                case SmartActions.RemoveTimedEvent:
                case SmartActions.ActivateGobject:
                case SmartActions.CallScriptReset:
                case SmartActions.SetRangedMovement:
                case SmartActions.SetNpcFlag:
                case SmartActions.AddNpcFlag:
                case SmartActions.RemoveNpcFlag:
                case SmartActions.CallRandomTimedActionlist:
                case SmartActions.RandomMove:
                case SmartActions.SetUnitFieldBytes1:
                case SmartActions.RemoveUnitFieldBytes1:
                case SmartActions.JumpToPos:
                case SmartActions.SendGossipMenu:
                case SmartActions.GoSetLootState:
                case SmartActions.GoSetGoState:
                case SmartActions.SendTargetToTarget:
                case SmartActions.SetHomePos:
                case SmartActions.SummonCreatureGroup:
                case SmartActions.MoveOffset:
                case SmartActions.SetCorpseDelay:
                case SmartActions.AddThreat:
                case SmartActions.TriggerRandomTimedEvent:
                case SmartActions.SpawnSpawngroup:
                case SmartActions.AddToStoredTargetList:
                case SmartActions.DoAction:
                case SmartActions.ExitVehicle:
                    break;
                case SmartActions.BecomePersonalCloneForPlayer:
                {
                    if (e.Action.becomePersonalClone.type < (uint)TempSummonType.TimedOrDeadDespawn || e.Action.becomePersonalClone.type > (uint)TempSummonType.ManualDespawn)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses incorrect TempSummonType {e.Action.becomePersonalClone.type}, skipped.");
                        return false;
                    }
                    break;
                }
                case SmartActions.TriggerGameEvent:
                {
                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.triggerGameEvent.useSaiTargetAsGameEventSource);
                    break;
                }
                case SmartActions.CompleteQuest:
                {
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(e.Action.quest.questId);
                    if (quest != null)
                    {
                        if (!quest.HasFlag(QuestFlags.CompletionEvent) && !quest.HasFlag(QuestFlags.CompletionAreaTrigger) && !quest.HasFlag(QuestFlags.TrackingEvent))
                        {
                            Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} Flags for Quest entry {e.Action.quest.questId} does not include QUEST_FLAGS_COMPLETION_EVENT or QUEST_FLAGS_COMPLETION_AREA_TRIGGER or QUEST_FLAGS_TRACKING_EVENT, skipped.");
                            return false;
                        }
                    }
                    else
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses non-existent Quest entry {e.Action.quest.questId}, skipped.");
                        return false;
                    }
                    break;
                }
                case SmartActions.CreditQuestObjectiveTalkTo:
                {
                    if (e.GetScriptType() != SmartScriptType.Creature)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses non-valid SourceType (only valid for SourceType {SmartScriptType.Creature}), skipped.");
                        return false;
                    }
                    break;
                }
                case SmartActions.EnterVehicle:
                case SmartActions.BoardPassenger:
                {
                    if (e.Action.enterVehicle.seatId >= SharedConst.MaxVehicleSeats)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses incorrect seat id (out of range 0 - {SharedConst.MaxVehicleSeats - 1}), skipped.");
                        return false;
                    }
                    break;
                }
                case SmartActions.DestroyConversation:
                {
                    if (Global.ConversationDataStorage.GetConversationTemplate(e.Action.destroyConversation.id) == null)
                    {
                        Log.outError(LogFilter.Sql, $"SmartAIMgr: SMART_ACTION_DESTROY_CONVERSATION {e} uses invalid entry {e.Action.destroyConversation.id}, skipped.");
                        return false;
                    }

                    TC_SAI_IS_BOOLEAN_VALID(e, e.Action.destroyConversation.isPrivate);
                    break;
                }
                // No longer supported
                case SmartActions.CallAreaexploredoreventhappens:
                case SmartActions.CallGroupeventhappens:
                case SmartActions.SetUnitFlag:
                case SmartActions.RemoveUnitFlag:
                case SmartActions.InstallAITemplate:
                case SmartActions.SetSwim:
                case SmartActions.AddAura:
                case SmartActions.OverrideScriptBaseObject:
                case SmartActions.ResetScriptBaseObject:
                case SmartActions.SendGoCustomAnim:
                case SmartActions.SetDynamicFlag:
                case SmartActions.AddDynamicFlag:
                case SmartActions.RemoveDynamicFlag:
                case SmartActions.SetGoFlag:
                case SmartActions.AddGoFlag:
                case SmartActions.RemoveGoFlag:
                case SmartActions.SetCanFly:
                case SmartActions.RemoveAurasByType:
                case SmartActions.SetSightDist:
                case SmartActions.Flee:
                case SmartActions.RemoveAllGameobjects:
                    Log.outError(LogFilter.Sql, $"SmartAIMgr: Unused action_type: {e} Skipped.");
                    return false;
                default:
                    Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Not handled action_type({0}), event_type({1}), Entry {2} SourceType {3} Event {4}, skipped.", e.GetActionType(), e.GetEventType(), e.EntryOrGuid, e.GetScriptType(), e.EventId);
                    return false;
            }

            if (!CheckUnusedActionParams(e))
                return false;

            return true;
        }

        static bool IsAnimKitValid(SmartScriptHolder e, uint entry)
        {
            if (!CliDB.AnimKitStorage.ContainsKey(entry))
            {
                Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses non-existent AnimKit entry {entry}, skipped.");
                return false;
            }
            return true;
        }
        static bool IsTextValid(SmartScriptHolder e, uint id)
        {
            if (e.GetScriptType() != SmartScriptType.Creature)
                return true;

            uint entry;
            if (e.GetEventType() == SmartEvents.TextOver)
            {
                entry = e.Event.textOver.creatureEntry;
            }
            else
            {
                switch (e.GetTargetType())
                {
                    case SmartTargets.CreatureDistance:
                    case SmartTargets.CreatureRange:
                    case SmartTargets.ClosestCreature:
                        return true; // ignore
                    default:
                        if (e.EntryOrGuid < 0)
                        {
                            ulong guid = (ulong)-e.EntryOrGuid;
                            CreatureData data = Global.ObjectMgr.GetCreatureData(guid);
                            if (data == null)
                            {
                                Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} using non-existent Creature guid {guid}, skipped.");
                                return false;
                            }
                            else
                                entry = data.Id;
                        }
                        else
                            entry = (uint)e.EntryOrGuid;
                        break;
                }
            }

            if (entry == 0 || !Global.CreatureTextMgr.TextExist(entry, (byte)id))
            {
                Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} using non-existent Text id {id}, skipped.");
                return false;
            }

            return true;
        }
        static bool IsCreatureValid(SmartScriptHolder e, uint entry)
        {
            if (Global.ObjectMgr.GetCreatureTemplate(entry) == null)
            {
                Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Creature entry {entry}, skipped.");
                return false;
            }
            return true;
        }
        static bool IsGameObjectValid(SmartScriptHolder e, uint entry)
        {
            if (Global.ObjectMgr.GetGameObjectTemplate(entry) == null)
            {
                Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent GameObject entry {entry}, skipped.");
                return false;
            }
            return true;
        }
        static bool IsQuestValid(SmartScriptHolder e, uint entry)
        {
            if (Global.ObjectMgr.GetQuestTemplate(entry) == null)
            {
                Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Quest entry {entry}, skipped.");
                return false;
            }
            return true;
        }
        static bool IsSpellValid(SmartScriptHolder e, uint entry)
        {
            if (!Global.SpellMgr.HasSpellInfo(entry, Difficulty.None))
            {
                Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Spell entry {entry}, skipped.");
                return false;
            }
            return true;
        }
        static bool IsMinMaxValid(SmartScriptHolder e, uint min, uint max)
        {
            if (max < min)
            {
                Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses min/max params wrong ({min}/{max}), skipped.");
                return false;
            }
            return true;
        }
        static bool NotNULL(SmartScriptHolder e, uint data)
        {
            if (data == 0)
            {
                Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} Parameter can not be NULL, skipped.");
                return false;
            }
            return true;
        }
        static bool IsEmoteValid(SmartScriptHolder e, uint entry)
        {
            if (!CliDB.EmotesStorage.ContainsKey(entry))
            {
                Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Emote entry {entry}, skipped.");
                return false;
            }
            return true;
        }
        static bool IsItemValid(SmartScriptHolder e, uint entry)
        {
            if (!CliDB.ItemSparseStorage.ContainsKey(entry))
            {
                Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Item entry {entry}, skipped.");
                return false;
            }
            return true;
        }
        static bool IsTextEmoteValid(SmartScriptHolder e, uint entry)
        {
            if (!CliDB.EmotesTextStorage.ContainsKey(entry))
            {
                Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Text Emote entry {entry}, skipped.");
                return false;
            }
            return true;
        }
        static bool IsSoundValid(SmartScriptHolder e, uint entry)
        {
            if (!CliDB.SoundKitStorage.ContainsKey(entry))
            {
                Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Sound entry {entry}, skipped.");
                return false;
            }
            return true;
        }

        public List<SmartScriptHolder> GetScript(int entry, SmartScriptType type)
        {
            List<SmartScriptHolder> temp = new();
            if (_eventMap[(uint)type].ContainsKey(entry))
            {
                foreach (var holder in _eventMap[(uint)type][entry])
                    temp.Add(new SmartScriptHolder(holder));
            }
            else
            {
                if (entry > 0)//first search is for guid (negative), do not drop error if not found
                    Log.outDebug(LogFilter.ScriptsAi, "SmartAIMgr.GetScript: Could not load Script for Entry {0} ScriptType {1}.", entry, type);
            }

            return temp;
        }

        public static SmartScriptHolder FindLinkedSourceEvent(List<SmartScriptHolder> list, uint eventId)
        {
            var sch = list.Find(p => p.Link == eventId);
            if (sch != null)
                return sch;

            return null;
        }

        public SmartScriptHolder FindLinkedEvent(List<SmartScriptHolder> list, uint link)
        {
            var sch = list.Find(p => p.EventId == link && p.GetEventType() == SmartEvents.Link);
            if (sch != null)
                return sch;

            return null;
        }

        public static uint GetTypeMask(SmartScriptType smartScriptType) =>
            smartScriptType switch
            {
                SmartScriptType.Creature => SmartScriptTypeMaskId.Creature,
                SmartScriptType.GameObject => SmartScriptTypeMaskId.Gameobject,
                SmartScriptType.AreaTrigger => SmartScriptTypeMaskId.Areatrigger,
                SmartScriptType.Event => SmartScriptTypeMaskId.Event,
                SmartScriptType.Gossip => SmartScriptTypeMaskId.Gossip,
                SmartScriptType.Quest => SmartScriptTypeMaskId.Quest,
                SmartScriptType.Spell => SmartScriptTypeMaskId.Spell,
                SmartScriptType.Transport => SmartScriptTypeMaskId.Transport,
                SmartScriptType.Instance => SmartScriptTypeMaskId.Instance,
                SmartScriptType.TimedActionlist => SmartScriptTypeMaskId.TimedActionlist,
                SmartScriptType.Scene => SmartScriptTypeMaskId.Scene,
                SmartScriptType.AreaTriggerEntity => SmartScriptTypeMaskId.AreatrigggerEntity,
                SmartScriptType.AreaTriggerEntityCustom => SmartScriptTypeMaskId.AreatrigggerEntity,
                _ => 0,
            };

        public static uint GetEventMask(SmartEvents smartEvent) =>
            smartEvent switch
            {
                SmartEvents.UpdateIc => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.TimedActionlist,
                SmartEvents.UpdateOoc => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject + SmartScriptTypeMaskId.Instance + SmartScriptTypeMaskId.AreatrigggerEntity,
                SmartEvents.HealthPct => SmartScriptTypeMaskId.Creature,
                SmartEvents.ManaPct => SmartScriptTypeMaskId.Creature,
                SmartEvents.Aggro => SmartScriptTypeMaskId.Creature,
                SmartEvents.Kill => SmartScriptTypeMaskId.Creature,
                SmartEvents.Death => SmartScriptTypeMaskId.Creature,
                SmartEvents.Evade => SmartScriptTypeMaskId.Creature,
                SmartEvents.SpellHit => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.Range => SmartScriptTypeMaskId.Creature,
                SmartEvents.OocLos => SmartScriptTypeMaskId.Creature,
                SmartEvents.Respawn => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.TargetHealthPct => SmartScriptTypeMaskId.Creature,
                SmartEvents.VictimCasting => SmartScriptTypeMaskId.Creature,
                SmartEvents.FriendlyHealth => SmartScriptTypeMaskId.Creature,
                SmartEvents.FriendlyIsCc => SmartScriptTypeMaskId.Creature,
                SmartEvents.FriendlyMissingBuff => SmartScriptTypeMaskId.Creature,
                SmartEvents.SummonedUnit => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.TargetManaPct => SmartScriptTypeMaskId.Creature,
                SmartEvents.AcceptedQuest => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.RewardQuest => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.ReachedHome => SmartScriptTypeMaskId.Creature,
                SmartEvents.ReceiveEmote => SmartScriptTypeMaskId.Creature,
                SmartEvents.HasAura => SmartScriptTypeMaskId.Creature,
                SmartEvents.TargetBuffed => SmartScriptTypeMaskId.Creature,
                SmartEvents.Reset => SmartScriptTypeMaskId.Creature,
                SmartEvents.IcLos => SmartScriptTypeMaskId.Creature,
                SmartEvents.PassengerBoarded => SmartScriptTypeMaskId.Creature,
                SmartEvents.PassengerRemoved => SmartScriptTypeMaskId.Creature,
                SmartEvents.Charmed => SmartScriptTypeMaskId.Creature,
                SmartEvents.CharmedTarget => SmartScriptTypeMaskId.Creature,
                SmartEvents.SpellHitTarget => SmartScriptTypeMaskId.Creature,
                SmartEvents.Damaged => SmartScriptTypeMaskId.Creature,
                SmartEvents.DamagedTarget => SmartScriptTypeMaskId.Creature,
                SmartEvents.Movementinform => SmartScriptTypeMaskId.Creature,
                SmartEvents.SummonDespawned => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.CorpseRemoved => SmartScriptTypeMaskId.Creature,
                SmartEvents.AiInit => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.DataSet => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.WaypointStart => SmartScriptTypeMaskId.Creature,
                SmartEvents.WaypointReached => SmartScriptTypeMaskId.Creature,
                SmartEvents.TransportAddplayer => SmartScriptTypeMaskId.Transport,
                SmartEvents.TransportAddcreature => SmartScriptTypeMaskId.Transport,
                SmartEvents.TransportRemovePlayer => SmartScriptTypeMaskId.Transport,
                SmartEvents.TransportRelocate => SmartScriptTypeMaskId.Transport,
                SmartEvents.InstancePlayerEnter => SmartScriptTypeMaskId.Instance,
                SmartEvents.AreatriggerEnter => SmartScriptTypeMaskId.Areatrigger + SmartScriptTypeMaskId.AreatrigggerEntity,
                SmartEvents.QuestAccepted => SmartScriptTypeMaskId.Quest,
                SmartEvents.QuestObjCompletion => SmartScriptTypeMaskId.Quest,
                SmartEvents.QuestRewarded => SmartScriptTypeMaskId.Quest,
                SmartEvents.QuestCompletion => SmartScriptTypeMaskId.Quest,
                SmartEvents.QuestFail => SmartScriptTypeMaskId.Quest,
                SmartEvents.TextOver => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.ReceiveHeal => SmartScriptTypeMaskId.Creature,
                SmartEvents.JustSummoned => SmartScriptTypeMaskId.Creature,
                SmartEvents.WaypointPaused => SmartScriptTypeMaskId.Creature,
                SmartEvents.WaypointResumed => SmartScriptTypeMaskId.Creature,
                SmartEvents.WaypointStopped => SmartScriptTypeMaskId.Creature,
                SmartEvents.WaypointEnded => SmartScriptTypeMaskId.Creature,
                SmartEvents.TimedEventTriggered => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.Update => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject + SmartScriptTypeMaskId.AreatrigggerEntity,
                SmartEvents.Link => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject + SmartScriptTypeMaskId.Areatrigger + SmartScriptTypeMaskId.Event + SmartScriptTypeMaskId.Gossip + SmartScriptTypeMaskId.Quest + SmartScriptTypeMaskId.Spell + SmartScriptTypeMaskId.Transport + SmartScriptTypeMaskId.Instance + SmartScriptTypeMaskId.AreatrigggerEntity,
                SmartEvents.GossipSelect => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.JustCreated => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.GossipHello => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.FollowCompleted => SmartScriptTypeMaskId.Creature,
                SmartEvents.PhaseChange => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.IsBehindTarget => SmartScriptTypeMaskId.Creature,
                SmartEvents.GameEventStart => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.GameEventEnd => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.GoLootStateChanged => SmartScriptTypeMaskId.Gameobject,
                SmartEvents.GoEventInform => SmartScriptTypeMaskId.Gameobject,
                SmartEvents.ActionDone => SmartScriptTypeMaskId.Creature,
                SmartEvents.OnSpellclick => SmartScriptTypeMaskId.Creature,
                SmartEvents.FriendlyHealthPCT => SmartScriptTypeMaskId.Creature,
                SmartEvents.DistanceCreature => SmartScriptTypeMaskId.Creature,
                SmartEvents.DistanceGameobject => SmartScriptTypeMaskId.Creature,
                SmartEvents.CounterSet => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.SceneStart => SmartScriptTypeMaskId.Scene,
                SmartEvents.SceneTrigger => SmartScriptTypeMaskId.Scene,
                SmartEvents.SceneCancel => SmartScriptTypeMaskId.Scene,
                SmartEvents.SceneComplete => SmartScriptTypeMaskId.Scene,
                SmartEvents.SummonedUnitDies => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
                SmartEvents.OnSpellCast => SmartScriptTypeMaskId.Creature,
                SmartEvents.OnSpellFailed => SmartScriptTypeMaskId.Creature,
                SmartEvents.OnSpellStart => SmartScriptTypeMaskId.Creature,
                SmartEvents.OnDespawn => SmartScriptTypeMaskId.Creature,
                SmartEvents.SendEventTrigger => SmartScriptTypeMaskId.Event,
                SmartEvents.AreatriggerExit => SmartScriptTypeMaskId.Areatrigger + SmartScriptTypeMaskId.AreatrigggerEntity,
                SmartEvents.OnAuraApplied => SmartScriptTypeMaskId.Creature,
                SmartEvents.OnAuraRemoved => SmartScriptTypeMaskId.Creature,
                _ => 0,
            };

        public static void TC_SAI_IS_BOOLEAN_VALID(SmartScriptHolder e, uint value, [CallerArgumentExpression("value")] string valueName = null)
        {
            if (value > 1)
                Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses param {valueName} of type Boolean with value {value}, valid values are 0 or 1, skipped.");
        }
    }

    public class SmartScriptHolder : IComparable<SmartScriptHolder>
    {
        public const uint DefaultPriority = uint.MaxValue;

        public int EntryOrGuid;
        public SmartScriptType SourceType;
        public uint EventId;
        public uint Link;
        public List<Difficulty> Difficulties = new();

        public SmartEvent Event;
        public SmartAction Action;
        public SmartTarget Target;
        public uint Timer;
        public uint Priority;
        public bool Active;
        public bool RunOnce;
        public bool EnableTimed;

        public SmartScriptHolder() { }
        public SmartScriptHolder(SmartScriptHolder other)
        {
            EntryOrGuid = other.EntryOrGuid;
            SourceType = other.SourceType;
            EventId = other.EventId;
            Link = other.Link;
            Event = other.Event;
            Action = other.Action;
            Target = other.Target;
            Timer = other.Timer;
            Active = other.Active;
            RunOnce = other.RunOnce;
            EnableTimed = other.EnableTimed;
        }

        public SmartScriptType GetScriptType() { return SourceType; }
        public SmartEvents GetEventType() { return Event.type; }
        public SmartActions GetActionType() { return Action.type; }
        public SmartTargets GetTargetType() { return Target.type; }

        public override string ToString()
        {
            return $"Entry {EntryOrGuid} SourceType {GetScriptType()} Event {EventId} Action {GetActionType()}";
        }

        public int CompareTo(SmartScriptHolder other)
        {
            int result = Priority.CompareTo(other.Priority);
            if (result == 0)
                result = EntryOrGuid.CompareTo(other.EntryOrGuid);
            if (result == 0)
                result = SourceType.CompareTo(other.SourceType);
            if (result == 0)
                result = EventId.CompareTo(other.EventId);
            if (result == 0)
                result = Link.CompareTo(other.Link);

            return result;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SmartEvent
    {
        [FieldOffset(0)]
        public SmartEvents type;

        [FieldOffset(4)]
        public uint event_phase_mask;

        [FieldOffset(8)]
        public uint event_chance;

        [FieldOffset(12)]
        public SmartEventFlags event_flags;

        [FieldOffset(16)]
        public MinMaxRepeat minMaxRepeat;

        [FieldOffset(16)]
        public Kill kill;

        [FieldOffset(16)]
        public SpellHit spellHit;

        [FieldOffset(16)]
        public Los los;

        [FieldOffset(16)]
        public Respawn respawn;

        [FieldOffset(16)]
        public MinMax minMax;

        [FieldOffset(16)]
        public TargetCasting targetCasting;

        [FieldOffset(16)]
        public FriendlyCC friendlyCC;

        [FieldOffset(16)]
        public MissingBuff missingBuff;

        [FieldOffset(16)]
        public Summoned summoned;

        [FieldOffset(16)]
        public Quest quest;

        [FieldOffset(16)]
        public QuestObjective questObjective;

        [FieldOffset(16)]
        public Emote emote;

        [FieldOffset(16)]
        public Aura aura;

        [FieldOffset(16)]
        public Charm charm;

        [FieldOffset(16)]
        public MovementInform movementInform;

        [FieldOffset(16)]
        public DataSet dataSet;

        [FieldOffset(16)]
        public Waypoint waypoint;

        [FieldOffset(16)]
        public TransportAddCreature transportAddCreature;

        [FieldOffset(16)]
        public TransportRelocate transportRelocate;

        [FieldOffset(16)]
        public InstancePlayerEnter instancePlayerEnter;

        [FieldOffset(16)]
        public TextOver textOver;

        [FieldOffset(16)]
        public TimedEvent timedEvent;

        [FieldOffset(16)]
        public GossipHello gossipHello;

        [FieldOffset(16)]
        public Gossip gossip;

        [FieldOffset(16)]
        public GameEvent gameEvent;

        [FieldOffset(16)]
        public GoLootStateChanged goLootStateChanged;

        [FieldOffset(16)]
        public EventInform eventInform;

        [FieldOffset(16)]
        public DoAction doAction;

        [FieldOffset(16)]
        public FriendlyHealthPct friendlyHealthPct;

        [FieldOffset(16)]
        public Distance distance;

        [FieldOffset(16)]
        public Counter counter;

        [FieldOffset(16)]
        public SpellCast spellCast;

        [FieldOffset(16)]
        public Spell spell;

        [FieldOffset(16)]
        public Raw raw;

        [FieldOffset(40)]
        public string param_string;

        #region Structs
        public struct MinMaxRepeat
        {
            public uint min;
            public uint max;
            public uint repeatMin;
            public uint repeatMax;
        }
        public struct Kill
        {
            public uint cooldownMin;
            public uint cooldownMax;
            public uint playerOnly;
            public uint creature;
        }
        public struct SpellHit
        {
            public uint spell;
            public uint school;
            public uint cooldownMin;
            public uint cooldownMax;
        }
        public struct Los
        {
            public uint hostilityMode;
            public uint maxDist;
            public uint cooldownMin;
            public uint cooldownMax;
            public uint playerOnly;
        }
        public struct Respawn
        {
            public uint type;
            public uint map;
            public uint area;
        }
        public struct MinMax
        {
            public uint repeatMin;
            public uint repeatMax;
        }
        public struct TargetCasting
        {
            public uint repeatMin;
            public uint repeatMax;
            public uint spellId;
        }
        public struct FriendlyCC
        {
            public uint radius;
            public uint repeatMin;
            public uint repeatMax;
        }
        public struct MissingBuff
        {
            public uint spell;
            public uint radius;
            public uint repeatMin;
            public uint repeatMax;
        }
        public struct Summoned
        {
            public uint creature;
            public uint cooldownMin;
            public uint cooldownMax;
        }
        public struct Quest
        {
            public uint questId;
            public uint cooldownMin;
            public uint cooldownMax;
        }
        public struct QuestObjective
        {
            public uint id;
        }
        public struct Emote
        {
            public uint emoteId;
            public uint cooldownMin;
            public uint cooldownMax;
        }
        public struct Aura
        {
            public uint spell;
            public uint count;
            public uint repeatMin;
            public uint repeatMax;
        }
        public struct Charm
        {
            public uint onRemove;
        }
        public struct MovementInform
        {
            public uint type;
            public uint id;
        }
        public struct DataSet
        {
            public uint id;
            public uint value;
            public uint cooldownMin;
            public uint cooldownMax;
        }
        public struct Waypoint
        {
            public uint pointID;
            public uint pathID;
        }
        public struct TransportAddCreature
        {
            public uint creature;
        }
        public struct TransportRelocate
        {
            public uint pointID;
        }
        public struct InstancePlayerEnter
        {
            public uint team;
            public uint cooldownMin;
            public uint cooldownMax;
        }
        public struct TextOver
        {
            public uint textGroupID;
            public uint creatureEntry;
        }
        public struct TimedEvent
        {
            public uint id;
        }
        public struct GossipHello
        {
            public uint filter;
        }
        public struct Gossip
        {
            public uint sender;
            public uint action;
        }
        public struct GameEvent
        {
            public uint gameEventId;
        }
        public struct GoLootStateChanged
        {
            public uint lootState;
        }
        public struct EventInform
        {
            public uint eventId;
        }
        public struct DoAction
        {
            public uint eventId;
        }
        public struct FriendlyHealthPct
        {
            public uint minHpPct;
            public uint maxHpPct;
            public uint repeatMin;
            public uint repeatMax;
            public uint radius;
        }
        public struct Distance
        {
            public uint guid;
            public uint entry;
            public uint dist;
            public uint repeat;
        }
        public struct Counter
        {
            public uint id;
            public uint value;
            public uint cooldownMin;
            public uint cooldownMax;
        }
        public struct SpellCast
        {
            public uint spell;
            public uint cooldownMin;
            public uint cooldownMax;
        }
        public struct Spell
        {
            public uint effIndex;
        }
        public struct Raw
        {
            public uint param1;
            public uint param2;
            public uint param3;
            public uint param4;
            public uint param5;
        }
        #endregion
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SmartAction
    {
        [FieldOffset(0)]
        public SmartActions type;

        [FieldOffset(4)]
        public Talk talk;

        [FieldOffset(4)]
        public SimpleTalk simpleTalk;

        [FieldOffset(4)]
        public Faction faction;

        [FieldOffset(4)]
        public MorphOrMount morphOrMount;

        [FieldOffset(4)]
        public Sound sound;

        [FieldOffset(4)]
        public Emote emote;

        [FieldOffset(4)]
        public Quest quest;

        [FieldOffset(4)]
        public QuestOffer questOffer;

        [FieldOffset(4)]
        public React react;

        [FieldOffset(4)]
        public RandomEmote randomEmote;

        [FieldOffset(4)]
        public Cast cast;

        [FieldOffset(4)]
        public CrossCast crossCast;

        [FieldOffset(4)]
        public SummonCreature summonCreature;

        [FieldOffset(4)]
        public ThreatPCT threatPCT;

        [FieldOffset(4)]
        public Threat threat;

        [FieldOffset(4)]
        public CastCreatureOrGO castCreatureOrGO;

        [FieldOffset(4)]
        public AutoAttack autoAttack;

        [FieldOffset(4)]
        public CombatMove combatMove;

        [FieldOffset(4)]
        public SetEventPhase setEventPhase;

        [FieldOffset(4)]
        public IncEventPhase incEventPhase;

        [FieldOffset(4)]
        public CastedCreatureOrGO castedCreatureOrGO;

        [FieldOffset(4)]
        public RemoveAura removeAura;

        [FieldOffset(4)]
        public Follow follow;

        [FieldOffset(4)]
        public RandomPhase randomPhase;

        [FieldOffset(4)]
        public RandomPhaseRange randomPhaseRange;

        [FieldOffset(4)]
        public KilledMonster killedMonster;

        [FieldOffset(4)]
        public SetInstanceData setInstanceData;

        [FieldOffset(4)]
        public SetInstanceData64 setInstanceData64;

        [FieldOffset(4)]
        public UpdateTemplate updateTemplate;

        [FieldOffset(4)]
        public CallHelp callHelp;

        [FieldOffset(4)]
        public SetSheath setSheath;

        [FieldOffset(4)]
        public ForceDespawn forceDespawn;

        [FieldOffset(4)]
        public InvincHP invincHP;

        [FieldOffset(4)]
        public IngamePhaseId ingamePhaseId;

        [FieldOffset(4)]
        public IngamePhaseGroup ingamePhaseGroup;

        [FieldOffset(4)]
        public SetData setData;

        [FieldOffset(4)]
        public MoveRandom moveRandom;

        [FieldOffset(4)]
        public Visibility visibility;

        [FieldOffset(4)]
        public SummonGO summonGO;

        [FieldOffset(4)]
        public Active active;

        [FieldOffset(4)]
        public Taxi taxi;

        [FieldOffset(4)]
        public WpStart wpStart;

        [FieldOffset(4)]
        public WpPause wpPause;

        [FieldOffset(4)]
        public WpStop wpStop;

        [FieldOffset(4)]
        public Item item;

        [FieldOffset(4)]
        public SetRun setRun;

        [FieldOffset(4)]
        public SetDisableGravity setDisableGravity;

        [FieldOffset(4)]
        public Teleport teleport;

        [FieldOffset(4)]
        public SetCounter setCounter;

        [FieldOffset(4)]
        public StoreTargets storeTargets;

        [FieldOffset(4)]
        public TimeEvent timeEvent;

        [FieldOffset(4)]
        public Movie movie;

        [FieldOffset(4)]
        public Equip equip;

        [FieldOffset(4)]
        public Flag flag;

        [FieldOffset(4)]
        public SetunitByte setunitByte;

        [FieldOffset(4)]
        public DelunitByte delunitByte;

        [FieldOffset(4)]
        public TimedActionList timedActionList;

        [FieldOffset(4)]
        public RandTimedActionList randTimedActionList;

        [FieldOffset(4)]
        public RandRangeTimedActionList randRangeTimedActionList;

        [FieldOffset(4)]
        public InterruptSpellCasting interruptSpellCasting;

        [FieldOffset(4)]
        public Jump jump;

        [FieldOffset(4)]
        public FleeAssist fleeAssist;

        [FieldOffset(4)]
        public EnableTempGO enableTempGO;

        [FieldOffset(4)]
        public MoveToPos moveToPos;

        [FieldOffset(4)]
        public SendGossipMenu sendGossipMenu;

        [FieldOffset(4)]
        public SetGoLootState setGoLootState;

        [FieldOffset(4)]
        public SendTargetToTarget sendTargetToTarget;

        [FieldOffset(4)]
        public SetRangedMovement setRangedMovement;

        [FieldOffset(4)]
        public SetHealthRegen setHealthRegen;

        [FieldOffset(4)]
        public SetRoot setRoot;

        [FieldOffset(4)]
        public GoState goState;

        [FieldOffset(4)]
        public CreatureGroup creatureGroup;

        [FieldOffset(4)]
        public Power power;

        [FieldOffset(4)]
        public GameEventStop gameEventStop;

        [FieldOffset(4)]
        public GameEventStart gameEventStart;

        [FieldOffset(4)]
        public ClosestWaypointFromList closestWaypointFromList;

        [FieldOffset(4)]
        public MoveOffset moveOffset;

        [FieldOffset(4)]
        public RandomSound randomSound;

        [FieldOffset(4)]
        public CorpseDelay corpseDelay;

        [FieldOffset(4)]
        public DisableEvade disableEvade;

        [FieldOffset(4)]
        public GroupSpawn groupSpawn;

        [FieldOffset(4)]
        public AuraType auraType;

        [FieldOffset(4)]
        public LoadEquipment loadEquipment;

        [FieldOffset(4)]
        public RandomTimedEvent randomTimedEvent;

        [FieldOffset(4)]
        public PauseMovement pauseMovement;

        [FieldOffset(4)]
        public RespawnData respawnData;

        [FieldOffset(4)]
        public AnimKit animKit;

        [FieldOffset(4)]
        public Scene scene;

        [FieldOffset(4)]
        public Cinematic cinematic;

        [FieldOffset(4)]
        public MovementSpeed movementSpeed;

        [FieldOffset(4)]
        public SpellVisualKit spellVisualKit;

        [FieldOffset(4)]
        public OverrideLight overrideLight;

        [FieldOffset(4)]
        public OverrideWeather overrideWeather;

        [FieldOffset(4)]
        public SetHover setHover;

        [FieldOffset(4)]
        public Evade evade;

        [FieldOffset(4)]
        public SetHealthPct setHealthPct;

        [FieldOffset(4)]
        public Conversation conversation;

        [FieldOffset(4)]
        public SetImmunePC setImmunePC;

        [FieldOffset(4)]
        public SetImmuneNPC setImmuneNPC;

        [FieldOffset(4)]
        public SetUninteractible setUninteractible;

        [FieldOffset(4)]
        public ActivateGameObject activateGameObject;

        [FieldOffset(4)]
        public AddToStoredTargets addToStoredTargets;

        [FieldOffset(4)]
        public BecomePersonalClone becomePersonalClone;

        [FieldOffset(4)]
        public TriggerGameEvent triggerGameEvent;

        [FieldOffset(4)]
        public DoAction doAction;

        [FieldOffset(4)]
        public DestroyConversation destroyConversation;

        [FieldOffset(4)]
        public EnterVehicle enterVehicle;

        [FieldOffset(4)]
        public Raw raw;

        [FieldOffset(32)]
        public string param_string;

        #region Stucts
        public struct Talk
        {
            public uint textGroupId;
            public uint duration;
            public uint useTalkTarget;
        }
        public struct SimpleTalk
        {
            public uint textGroupId;
            public uint duration;
        }
        public struct Faction
        {
            public uint factionId;
        }
        public struct MorphOrMount
        {
            public uint creature;
            public uint model;
        }
        public struct Sound
        {
            public uint soundId;
            public uint onlySelf;
            public uint distance;
            public uint keyBroadcastTextId;
        }
        public struct Emote
        {
            public uint emoteId;
        }
        public struct Quest
        {
            public uint questId;
        }
        public struct QuestOffer
        {
            public uint questId;
            public uint directAdd;
        }
        public struct React
        {
            public uint state;
        }
        public struct RandomEmote
        {
            public uint emote1;
            public uint emote2;
            public uint emote3;
            public uint emote4;
            public uint emote5;
            public uint emote6;
        }
        public struct Cast
        {
            public uint spell;
            public uint castFlags;
            public uint triggerFlags;
            public uint targetsLimit;
        }
        public struct CrossCast
        {
            public uint spell;
            public uint castFlags;
            public uint targetType;
            public uint targetParam1;
            public uint targetParam2;
            public uint targetParam3;
            public uint targetParam4;
        }
        public struct SummonCreature
        {
            public uint creature;
            public uint type;
            public uint duration;
            public uint storedTargetId;
            public uint flags; // SmartActionSummonCreatureFlags
            public uint count;
            public uint createdBySpell;
        }
        public struct ThreatPCT
        {
            public uint threatINC;
            public uint threatDEC;
        }
        public struct CastCreatureOrGO
        {
            public uint quest;
            public uint spell;
        }
        public struct Threat
        {
            public uint threatINC;
            public uint threatDEC;
        }
        public struct AutoAttack
        {
            public uint attack;
        }
        public struct CombatMove
        {
            public uint move;
        }
        public struct SetEventPhase
        {
            public uint phase;
        }
        public struct IncEventPhase
        {
            public uint inc;
            public uint dec;
        }
        public struct CastedCreatureOrGO
        {
            public uint creature;
            public uint spell;
        }
        public struct RemoveAura
        {
            public uint spell;
            public uint charges;
            public uint onlyOwnedAuras;
        }
        public struct Follow
        {
            public uint dist;
            public uint angle;
            public uint entry;
            public uint credit;
            public uint creditType;
        }
        public struct RandomPhase
        {
            public uint phase1;
            public uint phase2;
            public uint phase3;
            public uint phase4;
            public uint phase5;
            public uint phase6;
        }
        public struct RandomPhaseRange
        {
            public uint phaseMin;
            public uint phaseMax;
        }
        public struct KilledMonster
        {
            public uint creature;
        }
        public struct SetInstanceData
        {
            public uint field;
            public uint data;
            public uint type;
        }
        public struct SetInstanceData64
        {
            public uint field;
        }
        public struct UpdateTemplate
        {
            public uint creature;
            public uint updateLevel;
        }
        public struct CallHelp
        {
            public uint range;
            public uint withEmote;
        }
        public struct SetSheath
        {
            public uint sheath;
        }
        public struct ForceDespawn
        {
            public uint delay;
            public uint forceRespawnTimer;
        }
        public struct InvincHP
        {
            public uint minHP;
            public uint percent;
        }
        public struct IngamePhaseId
        {
            public uint id;
            public uint apply;
        }
        public struct IngamePhaseGroup
        {
            public uint groupId;
            public uint apply;
        }
        public struct SetData
        {
            public uint field;
            public uint data;
        }
        public struct MoveRandom
        {
            public uint distance;
        }
        public struct Visibility
        {
            public uint state;
        }
        public struct SummonGO
        {
            public uint entry;
            public uint despawnTime;
            public uint summonType;
            public uint storedTargetId;
        }
        public struct Active
        {
            public uint state;
        }
        public struct Taxi
        {
            public uint id;
        }
        public struct WpStart
        {
            public uint run;
            public uint pathID;
            public uint repeat;
            public uint quest;
            public uint despawnTime;
            //public uint reactState; DO NOT REUSE
        }
        public struct WpPause
        {
            public uint delay;
        }
        public struct WpStop
        {
            public uint despawnTime;
            public uint quest;
            public uint fail;
        }
        public struct Item
        {
            public uint entry;
            public uint count;
        }
        public struct SetRun
        {
            public uint run;
        }
        public struct SetDisableGravity
        {
            public uint disable;
        }
        public struct Teleport
        {
            public uint mapID;
        }
        public struct SetCounter
        {
            public uint counterId;
            public uint value;
            public uint reset;
        }
        public struct StoreTargets
        {
            public uint id;
        }
        public struct TimeEvent
        {
            public uint id;
            public uint min;
            public uint max;
            public uint repeatMin;
            public uint repeatMax;
            public uint chance;
        }
        public struct Movie
        {
            public uint entry;
        }
        public struct Equip
        {
            public uint entry;
            public uint mask;
            public uint slot1;
            public uint slot2;
            public uint slot3;
        }
        public struct Flag
        {
            public uint flag;
        }
        public struct SetunitByte
        {
            public uint byte1;
            public uint type;
        }
        public struct DelunitByte
        {
            public uint byte1;
            public uint type;
        }
        public struct TimedActionList
        {
            public uint id;
            public uint timerType;
            public uint allowOverride;
        }
        public struct RandTimedActionList
        {
            public uint actionList1;
            public uint actionList2;
            public uint actionList3;
            public uint actionList4;
            public uint actionList5;
            public uint actionList6;
        }
        public struct RandRangeTimedActionList
        {
            public uint idMin;
            public uint idMax;
        }
        public struct InterruptSpellCasting
        {
            public uint withDelayed;
            public uint spell_id;
            public uint withInstant;
        }
        public struct Jump
        {
            public uint SpeedXY;
            public uint SpeedZ;
            public uint Gravity;
            public uint UseDefaultGravity;
            public uint PointId;
            public uint ContactDistance;
        }
        public struct FleeAssist
        {
            public uint withEmote;
        }
        public struct EnableTempGO
        {
            public uint duration;
        }
        public struct MoveToPos
        {
            public uint pointId;
            public uint transport;
            public uint disablePathfinding;
            public uint contactDistance;
        }
        public struct SendGossipMenu
        {
            public uint gossipMenuId;
            public uint gossipNpcTextId;
        }
        public struct SetGoLootState
        {
            public uint state;
        }
        public struct SendTargetToTarget
        {
            public uint id;
        }
        public struct SetRangedMovement
        {
            public uint distance;
            public uint angle;
        }
        public struct SetHealthRegen
        {
            public uint regenHealth;
        }
        public struct SetRoot
        {
            public uint root;
        }
        public struct GoState
        {
            public uint state;
        }
        public struct CreatureGroup
        {
            public uint group;
            public uint attackInvoker;
            public uint storedTargetId;
        }
        public struct Power
        {
            public uint powerType;
            public uint newPower;
        }
        public struct GameEventStop
        {
            public uint id;
        }
        public struct GameEventStart
        {
            public uint id;
        }
        public struct ClosestWaypointFromList
        {
            public uint wp1;
            public uint wp2;
            public uint wp3;
            public uint wp4;
            public uint wp5;
            public uint wp6;
        }
        public struct MoveOffset
        {
            public uint PointId;
        }
        public struct RandomSound
        {
            public uint sound1;
            public uint sound2;
            public uint sound3;
            public uint sound4;
            public uint onlySelf;
            public uint distance;
        }
        public struct CorpseDelay
        {
            public uint timer;
            public uint includeDecayRatio;
        }
        public struct DisableEvade
        {
            public uint disable;
        }
        public struct GroupSpawn
        {
            public uint groupId;
            public uint minDelay;
            public uint maxDelay;
            public uint spawnflags;
        }
        public struct LoadEquipment
        {
            public uint id;
            public uint force;
        }
        public struct RandomTimedEvent
        {
            public uint minId;
            public uint maxId;
        }
        public struct PauseMovement
        {
            public uint movementSlot;
            public uint pauseTimer;
            public uint force;
        }
        public struct RespawnData
        {
            public uint spawnType;
            public uint spawnId;
        }
        public struct AnimKit
        {
            public uint animKit;
            public uint type;
        }
        public struct Scene
        {
            public uint sceneId;
        }
        public struct Cinematic
        {
            public uint entry;
        }
        public struct MovementSpeed
        {
            public uint movementType;
            public uint speedInteger;
            public uint speedFraction;
        }
        public struct SpellVisualKit
        {
            public uint spellVisualKitId;
            public uint kitType;
            public uint duration;
        }
        public struct OverrideLight
        {
            public uint zoneId;
            public uint areaLightId;
            public uint overrideLightId;
            public uint transitionMilliseconds;
        }
        public struct OverrideWeather
        {
            public uint zoneId;
            public uint weatherId;
            public uint intensity;
        }
        public struct SetHover
        {
            public uint enable;
        }
        public struct Evade
        {
            public uint toRespawnPosition;
        }
        public struct SetHealthPct
        {
            public uint percent;
        }
        public struct Conversation
        {
            public uint id;
        }
        public struct SetImmunePC
        {
            public uint immunePC;
        }
        public struct SetImmuneNPC
        {
            public uint immuneNPC;
        }
        public struct SetUninteractible
        {
            public uint uninteractible;
        }
        public struct ActivateGameObject
        {
            public uint gameObjectAction;
            public uint param;
        }
        public struct AddToStoredTargets
        {
            public uint id;
        }
        public struct BecomePersonalClone
        {
            public uint type;
            public uint duration;
        }
        public struct TriggerGameEvent
        {
            public uint eventId;
            public uint useSaiTargetAsGameEventSource;
        }
        public struct DoAction
        {
            public uint actionId;
        }
        public struct DestroyConversation
        {
            public uint id;
            public uint isPrivate;
            public uint range;
        }
        public struct EnterVehicle
        {
            public uint seatId;
        }
        public struct Raw
        {
            public uint param1;
            public uint param2;
            public uint param3;
            public uint param4;
            public uint param5;
            public uint param6;
            public uint param7;
        }
        #endregion
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SmartTarget
    {
        [FieldOffset(0)]
        public SmartTargets type;

        [FieldOffset(4)]
        public float x;

        [FieldOffset(8)]
        public float y;

        [FieldOffset(12)]
        public float z;

        [FieldOffset(16)]
        public float o;

        [FieldOffset(20)]
        public HostilRandom hostilRandom;

        [FieldOffset(20)]
        public Farthest farthest;

        [FieldOffset(20)]
        public UnitRange unitRange;

        [FieldOffset(20)]
        public UnitGUID unitGUID;

        [FieldOffset(20)]
        public UnitDistance unitDistance;

        [FieldOffset(20)]
        public PlayerDistance playerDistance;

        [FieldOffset(20)]
        public PlayerRange playerRange;

        [FieldOffset(20)]
        public Stored stored;

        [FieldOffset(20)]
        public GoRange goRange;

        [FieldOffset(20)]
        public GoGUID goGUID;

        [FieldOffset(20)]
        public GoDistance goDistance;

        [FieldOffset(20)]
        public UnitClosest unitClosest;

        [FieldOffset(20)]
        public GoClosest goClosest;

        [FieldOffset(20)]
        public ClosestAttackable closestAttackable;

        [FieldOffset(20)]
        public ClosestFriendly closestFriendly;

        [FieldOffset(20)]
        public Owner owner;

        [FieldOffset(20)]
        public Vehicle vehicle;

        [FieldOffset(20)]
        public ThreatList threatList;

        [FieldOffset(20)]
        public Raw raw;

        [FieldOffset(40)]
        public string param_string;

        #region Structs
        public struct HostilRandom
        {
            public uint maxDist;
            public uint playerOnly;
            public uint powerType;
        }
        public struct Farthest
        {
            public uint maxDist;
            public uint playerOnly;
            public uint isInLos;
        }
        public struct UnitRange
        {
            public uint creature;
            public uint minDist;
            public uint maxDist;
            public uint maxSize;
        }
        public struct UnitGUID
        {
            public uint dbGuid;
            public uint entry;
        }
        public struct UnitDistance
        {
            public uint creature;
            public uint dist;
            public uint maxSize;
        }
        public struct PlayerDistance
        {
            public uint dist;
        }
        public struct PlayerRange
        {
            public uint minDist;
            public uint maxDist;
        }
        public struct Stored
        {
            public uint id;
        }
        public struct GoRange
        {
            public uint entry;
            public uint minDist;
            public uint maxDist;
            public uint maxSize;
        }
        public struct GoGUID
        {
            public uint dbGuid;
            public uint entry;
        }
        public struct GoDistance
        {
            public uint entry;
            public uint dist;
            public uint maxSize;
        }
        public struct UnitClosest
        {
            public uint entry;
            public uint dist;
            public uint findCreatureAliveState;
        }
        public struct GoClosest
        {
            public uint entry;
            public uint dist;
        }
        public struct ClosestAttackable
        {
            public uint maxDist;
            public uint playerOnly;
        }
        public struct ClosestFriendly
        {
            public uint maxDist;
            public uint playerOnly;
        }
        public struct Owner
        {
            public uint useCharmerOrOwner;
        }
        public struct Vehicle
        {
            public uint seatMask;
        }
        public struct ThreatList
        {
            public uint maxDist;
        }
        public struct Raw
        {
            public uint param1;
            public uint param2;
            public uint param3;
            public uint param4;
        }
        #endregion
    }
}
