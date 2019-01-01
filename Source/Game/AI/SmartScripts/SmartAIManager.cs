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
using Game.DataStorage;
using Game.Entities;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Game.AI
{
    public class SmartAIManager : Singleton<SmartAIManager>
    {
        SmartAIManager()
        {
            for (byte i = 0; i < (int)SmartScriptType.Max; i++)
                mEventMap[i] = new MultiMap<int, SmartScriptHolder>();
        }

        public void LoadFromDB()
        {
            uint oldMSTime = Time.GetMSTime();

            for (byte i = 0; i < (int)SmartScriptType.Max; i++)
                mEventMap[i].Clear();  //Drop Existing SmartAI List

            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_SMART_SCRIPTS);
            SQLResult result = DB.World.Query(stmt);
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 SmartAI scripts. DB table `smartai_scripts` is empty.");
                return;
            }

            int count = 0;
            do
            {
                SmartScriptHolder temp = new SmartScriptHolder();

                temp.entryOrGuid = result.Read<int>(0);
                SmartScriptType source_type = (SmartScriptType)result.Read<byte>(1);
                if (source_type >= SmartScriptType.Max)
                {
                    Log.outError(LogFilter.Sql, "SmartAIMgr.LoadSmartAI: invalid source_type ({0}), skipped loading.", source_type);
                    continue;
                }
                if (temp.entryOrGuid >= 0)
                {
                    switch (source_type)
                    {
                        case SmartScriptType.Creature:
                            if (Global.ObjectMgr.GetCreatureTemplate((uint)temp.entryOrGuid) == null)
                            {
                                Log.outError(LogFilter.Sql, "SmartAIMgr.LoadSmartAI: Creature entry ({0}) does not exist, skipped loading.", temp.entryOrGuid);
                                continue;
                            }
                            break;

                        case SmartScriptType.GameObject:
                            {
                                if (Global.ObjectMgr.GetGameObjectTemplate((uint)temp.entryOrGuid) == null)
                                {
                                    Log.outError(LogFilter.Sql, "SmartAIMgr.LoadSmartAI: GameObject entry ({0}) does not exist, skipped loading.", temp.entryOrGuid);
                                    continue;
                                }
                                break;
                            }
                        case SmartScriptType.AreaTrigger:
                            {
                                if (CliDB.AreaTableStorage.LookupByKey((uint)temp.entryOrGuid) == null)
                                {
                                    Log.outError(LogFilter.Sql, "SmartAIMgr.LoadSmartAI: AreaTrigger entry ({0}) does not exist, skipped loading.", temp.entryOrGuid);
                                    continue;
                                }
                                break;
                            }
                        case SmartScriptType.Scene:
                            {
                                if (Global.ObjectMgr.GetSceneTemplate((uint)temp.entryOrGuid) == null)
                                {
                                    Log.outError(LogFilter.Sql, "SmartAIMgr.LoadSmartAIFromDB: Scene id ({0}) does not exist, skipped loading.", temp.entryOrGuid);
                                    continue;
                                }
                                break;
                            }
                        case SmartScriptType.Spell:
                            {
                                if (!Global.SpellMgr.HasSpellInfo((uint)temp.entryOrGuid))
                                {
                                    Log.outError(LogFilter.Sql, "SmartAIMgr.LoadSmartAIFromDB: Scene id ({0}) does not exist, skipped loading.", temp.entryOrGuid);
                                    continue;
                                }
                                break;
                            }
                        case SmartScriptType.TimedActionlist:
                            break;//nothing to check, really
                        default:
                            Log.outError(LogFilter.Sql, "SmartAIMgr.LoadSmartAIFromDB: not yet implemented source_type {0}", source_type);
                            continue;
                    }
                }
                else
                {
                    if (Global.ObjectMgr.GetCreatureData((uint)Math.Abs(temp.entryOrGuid)) == null)
                    {
                        Log.outError(LogFilter.Sql, "SmartAIMgr.LoadSmartAI: Creature guid ({0}) does not exist, skipped loading.", Math.Abs(temp.entryOrGuid));
                        continue;
                    }
                }

                temp.source_type = source_type;
                temp.event_id = result.Read<ushort>(2);
                temp.link = result.Read<ushort>(3);
                temp.Event.type = (SmartEvents)result.Read<byte>(4);
                temp.Event.event_phase_mask = result.Read<byte>(5);
                temp.Event.event_chance = result.Read<byte>(6);
                temp.Event.event_flags = (SmartEventFlags)result.Read<ushort>(7);

                temp.Event.raw.param1 = result.Read<uint>(8);
                temp.Event.raw.param2 = result.Read<uint>(9);
                temp.Event.raw.param3 = result.Read<uint>(10);
                temp.Event.raw.param4 = result.Read<uint>(11);
                temp.Event.param_string = result.Read<string>(12);

                temp.Action.type = (SmartActions)result.Read<byte>(13);
                temp.Action.raw.param1 = result.Read<uint>(14);
                temp.Action.raw.param2 = result.Read<uint>(15);
                temp.Action.raw.param3 = result.Read<uint>(16);
                temp.Action.raw.param4 = result.Read<uint>(17);
                temp.Action.raw.param5 = result.Read<uint>(18);
                temp.Action.raw.param6 = result.Read<uint>(19);

                temp.Target.type = (SmartTargets)result.Read<byte>(20);
                temp.Target.raw.param1 = result.Read<uint>(21);
                temp.Target.raw.param2 = result.Read<uint>(22);
                temp.Target.raw.param3 = result.Read<uint>(23);
                temp.Target.x = result.Read<float>(24);
                temp.Target.y = result.Read<float>(25);
                temp.Target.z = result.Read<float>(26);
                temp.Target.o = result.Read<float>(27);

                //check target
                if (!IsTargetValid(temp))
                    continue;

                // check all event and action params
                if (!IsEventValid(temp))
                    continue;

                // creature entry / guid not found in storage, create empty event list for it and increase counters
                if (!mEventMap[(int)source_type].ContainsKey(temp.entryOrGuid))
                    ++count;

                // store the new event
                mEventMap[(int)source_type].Add(temp.entryOrGuid, temp);
            }
            while (result.NextRow());

            // Post Loading Validation
            for (byte i = 0; i < (int)SmartScriptType.Max; ++i)
            {
                if (mEventMap[i] == null)
                    continue;

                foreach (var key in mEventMap[i].Keys)
                {
                    var list = mEventMap[i].LookupByKey(key);
                    foreach (var e in list)
                    {
                        if (e.link != 0)
                        {
                            if (FindLinkedEvent(list, e.link) == null)
                            {
                                Log.outError(LogFilter.Sql, "SmartAIMgr.LoadFromDB: Entry {0} SourceType {1}, Event {2}, Link Event {3} not found or invalid.",
                                        e.entryOrGuid, e.GetScriptType(), e.event_id, e.link);
                            }
                        }

                        if (e.GetEventType() == SmartEvents.Link)
                        {
                            if (FindLinkedSourceEvent(list, e.event_id) == null)
                            {
                                Log.outError(LogFilter.Sql, "SmartAIMgr.LoadFromDB: Entry {0} SourceType {1}, Event {2}, Link Source Event not found or invalid. Event will never trigger.",
                                        e.entryOrGuid, e.GetScriptType(), e.event_id);
                            }
                        }
                    }
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} SmartAI scripts in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }
        public void LoadWaypointFromDB()
        {
            uint oldMSTime = Time.GetMSTime();

            waypoint_map.Clear();

            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_SMARTAI_WP);
            SQLResult result = DB.World.Query(stmt);

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 SmartAI Waypoint Paths. DB table `waypoints` is empty.");

                return;
            }

            uint count = 0;
            uint total = 0;
            uint last_entry = 0;
            uint last_id = 1;

            do
            {
                uint entry = result.Read<uint>(0);
                uint id = result.Read<uint>(1);
                float x = result.Read<float>(2);
                float y = result.Read<float>(3);
                float z = result.Read<float>(4);

                if (last_entry != entry)
                {
                    last_id = 1;
                    count++;
                }

                if (last_id != id)
                    Log.outError(LogFilter.Sql, "SmartWaypointMgr.LoadFromDB: Path entry {0}, unexpected point id {1}, expected {2}.", entry, id, last_id);

                last_id++;

                WayPoint point = new WayPoint();
                point.id = id;
                point.x = x;
                point.y = y;
                point.z = z;

                waypoint_map.Add(entry, point);

                last_entry = entry;
                total++;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} SmartAI waypoint paths (total {1} waypoints) in {2} ms", count, total, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        bool IsTargetValid(SmartScriptHolder e)
        {
            if (Math.Abs(e.Target.o) > 2 * MathFunctions.PI)
                Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} has abs(`target.o` = {4}) > 2*PI (orientation is expressed in radians)",
                    e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Target.o);

            if (e.GetActionType() == SmartActions.InstallAiTemplate)
                return true; // AI template has special handling

            switch (e.GetTargetType())
            {
                case SmartTargets.CreatureDistance:
                case SmartTargets.CreatureRange:
                    {
                        if (e.Target.unitDistance.creature != 0 && Global.ObjectMgr.GetCreatureTemplate(e.Target.unitDistance.creature) == null)
                        {
                            Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Creature entry {4} as target_param1, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Target.unitDistance.creature);
                            return false;
                        }
                        break;
                    }
                case SmartTargets.GameobjectDistance:
                case SmartTargets.GameobjectRange:
                    {
                        if (e.Target.goDistance.entry != 0 && Global.ObjectMgr.GetGameObjectTemplate(e.Target.goDistance.entry) == null)
                        {
                            Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent GameObject entry {4} as target_param1, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Target.goDistance.entry);
                            return false;
                        }
                        break;
                    }
                case SmartTargets.CreatureGuid:
                    {
                        if (e.Target.unitGUID.entry != 0 && !IsCreatureValid(e, e.Target.unitGUID.entry))
                            return false;
                        break;
                    }
                case SmartTargets.GameobjectGuid:
                    {
                        if (e.Target.goGUID.entry != 0 && !IsGameObjectValid(e, e.Target.goGUID.entry))
                            return false;
                        break;
                    }
                case SmartTargets.PlayerDistance:
                case SmartTargets.ClosestPlayer:
                    {
                        if (e.Target.playerDistance.dist == 0)
                        {
                            Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} has maxDist 0 as target_param1, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType());
                            return false;
                        }
                        break;
                    }
                case SmartTargets.PlayerRange:
                case SmartTargets.Self:
                case SmartTargets.Victim:
                case SmartTargets.HostileSecondAggro:
                case SmartTargets.HostileLastAggro:
                case SmartTargets.HostileRandom:
                case SmartTargets.HostileRandomNotTop:
                case SmartTargets.ActionInvoker:
                case SmartTargets.InvokerParty:
                case SmartTargets.Position:
                case SmartTargets.None:
                case SmartTargets.ActionInvokerVehicle:
                case SmartTargets.OwnerOrSummoner:
                case SmartTargets.ThreatList:
                case SmartTargets.ClosestGameobject:
                case SmartTargets.ClosestCreature:
                case SmartTargets.ClosestEnemy:
                case SmartTargets.ClosestFriendly:
                case SmartTargets.Stored:
                case SmartTargets.LootRecipients:
                case SmartTargets.VehicleAccessory:
                case SmartTargets.SpellTarget:
                    break;
                default:
                    Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Not handled target_type({0}), Entry {1} SourceType {2} Event {3} Action {4}, skipped.", e.GetTargetType(), e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType());
                    return false;
            }
            return true;
        }

        bool IsEventValid(SmartScriptHolder e)
        {
            if (e.Event.type >= SmartEvents.End)
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid event type ({2}), skipped.", e.entryOrGuid, e.event_id, e.GetEventType());
                return false;
            }

            // in SMART_SCRIPT_TYPE_TIMED_ACTIONLIST all event types are overriden by core
            if (e.GetScriptType() != SmartScriptType.TimedActionlist && !Convert.ToBoolean(SmartAIEventMask[e.Event.type] & SmartAITypeMask[e.GetScriptType()]))
            {
                Log.outError(LogFilter.Scripts, "SmartAIMgr: EntryOrGuid {0}, event type {1} can not be used for Script type {2}", e.entryOrGuid, e.GetEventType(), e.GetScriptType());
                return false;
            }
            if (e.Action.type <= 0 || e.Action.type >= SmartActions.End)
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid action type ({2}), skipped.", e.entryOrGuid, e.event_id, e.GetActionType());
                return false;
            }
            if (e.Event.event_phase_mask > (uint)SmartPhase.All)
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid phase mask ({2}), skipped.", e.entryOrGuid, e.event_id, e.Event.event_phase_mask);
                return false;
            }
            if (e.Event.event_flags > SmartEventFlags.All)
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid event flags ({2}), skipped.", e.entryOrGuid, e.event_id, e.Event.event_flags);
                return false;
            }
            if (e.link != 0 && e.link == e.event_id)
            {
                Log.outError(LogFilter.Sql, "SmartAIMgr: EntryOrGuid {0} SourceType {1}, Event {2}, Event is linking self (infinite loop), skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id);
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
                    case SmartEvents.HealtPct:
                    case SmartEvents.ManaPct:
                    case SmartEvents.TargetHealthPct:
                    case SmartEvents.TargetManaPct:
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
                    case SmartEvents.SpellhitTarget:
                        if (e.Event.spellHit.spell != 0)
                        {
                            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(e.Event.spellHit.spell);
                            if (spellInfo == null)
                            {
                                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Spell entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Event.spellHit.spell);
                                return false;
                            }
                            if (e.Event.spellHit.school != 0 && ((SpellSchoolMask)e.Event.spellHit.school & spellInfo.SchoolMask) != spellInfo.SchoolMask)
                            {
                                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses Spell entry {4} with invalid school mask, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Event.spellHit.spell);
                                return false;
                            }
                        }
                        if (!IsMinMaxValid(e, e.Event.spellHit.cooldownMin, e.Event.spellHit.cooldownMax))
                            return false;
                        break;
                    case SmartEvents.OocLos:
                    case SmartEvents.IcLos:
                        if (!IsMinMaxValid(e, e.Event.los.cooldownMin, e.Event.los.cooldownMax))
                            return false;
                        break;
                    case SmartEvents.Respawn:
                        if (e.Event.respawn.type == (uint)SmartRespawnCondition.Map && CliDB.MapStorage.LookupByKey(e.Event.respawn.map) == null)
                        {
                            Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Map entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Event.respawn.map);
                            return false;
                        }
                        if (e.Event.respawn.type == (uint)SmartRespawnCondition.Area && !CliDB.AreaTableStorage.ContainsKey(e.Event.respawn.area))
                        {
                            Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Area entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Event.respawn.area);
                            return false;
                        }
                        break;
                    case SmartEvents.FriendlyHealth:
                        if (!NotNULL(e, e.Event.friendlyHealth.radius))
                            return false;

                        if (!IsMinMaxValid(e, e.Event.friendlyHealth.repeatMin, e.Event.friendlyHealth.repeatMax))
                            return false;
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
                        break;
                    case SmartEvents.VictimCasting:
                    case SmartEvents.PassengerBoarded:
                    case SmartEvents.PassengerRemoved:
                        if (!IsMinMaxValid(e, e.Event.minMax.repeatMin, e.Event.minMax.repeatMax))
                            return false;
                        break;
                    case SmartEvents.SummonDespawned:
                    case SmartEvents.SummonedUnit:
                        if (e.Event.summoned.creature != 0 && !IsCreatureValid(e, e.Event.summoned.creature))
                            return false;

                        if (!IsMinMaxValid(e, e.Event.summoned.cooldownMin, e.Event.summoned.cooldownMax))
                            return false;
                        break;
                    case SmartEvents.AcceptedQuest:
                    case SmartEvents.RewardQuest:
                        if (e.Event.quest.questId != 0 && !IsQuestValid(e, e.Event.quest.questId))
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
                            if (e.Event.movementInform.type >= (uint)MovementGeneratorType.Max)
                            {
                                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses invalid Motion type {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Event.movementInform.type);
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
                    case SmartEvents.AreatriggerOntrigger:
                        {
                            if (e.Event.areatrigger.id != 0 && !IsAreaTriggerValid(e, e.Event.areatrigger.id))
                                return false;
                            break;
                        }
                    case SmartEvents.TextOver:
                        {
                            if (!IsTextValid(e, e.Event.textOver.textGroupID))
                                return false;
                            break;
                        }
                    case SmartEvents.DummyEffect:
                        {
                            if (!IsSpellValid(e, e.Event.dummy.spell))
                                return false;

                            if (e.Event.dummy.effIndex > 2)
                                return false;
                            break;
                        }
                    case SmartEvents.IsBehindTarget:
                        {
                            if (!IsMinMaxValid(e, e.Event.behindTarget.cooldownMin, e.Event.behindTarget.cooldownMax))
                                return false;
                            break;
                        }
                    case SmartEvents.GameEventStart:
                    case SmartEvents.GameEventEnd:
                        {
                            var events = Global.GameEventMgr.GetEventMap();
                            if (e.Event.gameEvent.gameEventId >= events.Length || !events[e.Event.gameEvent.gameEventId].isValid())
                                return false;

                            break;
                        }
                    case SmartEvents.ActionDone:
                        {
                            if (e.Event.doAction.eventId > EventId.Charge)
                            {
                                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses invalid event id {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Event.doAction.eventId);
                                return false;
                            }
                            break;
                        }
                    case SmartEvents.FriendlyHealthPCT:
                        if (!IsMinMaxValid(e, e.Event.friendlyHealthPct.repeatMin, e.Event.friendlyHealthPct.repeatMax))
                            return false;

                        if (e.Event.friendlyHealthPct.maxHpPct > 100 || e.Event.friendlyHealthPct.minHpPct > 100)
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} has pct value above 100, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType());
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
                            default:
                                Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses invalid target_type {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.GetTargetType());
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

                        if (e.Event.distance.guid != 0 && Global.ObjectMgr.GetGOData(e.Event.distance.guid) == null)
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
                    case SmartEvents.Link:
                    case SmartEvents.GoStateChanged:
                    case SmartEvents.GoEventInform:
                    case SmartEvents.TimedEventTriggered:
                    case SmartEvents.InstancePlayerEnter:
                    case SmartEvents.TransportRelocate:
                    case SmartEvents.Charmed:
                    case SmartEvents.CharmedTarget:
                    case SmartEvents.CorpseRemoved:
                    case SmartEvents.AiInit:
                    case SmartEvents.TransportAddplayer:
                    case SmartEvents.TransportRemovePlayer:
                    case SmartEvents.Aggro:
                    case SmartEvents.Death:
                    case SmartEvents.Evade:
                    case SmartEvents.ReachedHome:
                    case SmartEvents.Reset:
                    case SmartEvents.QuestAccepted:
                    case SmartEvents.QuestObjCompletion:
                    case SmartEvents.QuestCompletion:
                    case SmartEvents.QuestRewarded:
                    case SmartEvents.QuestFail:
                    case SmartEvents.JustSummoned:
                    case SmartEvents.WaypointStart:
                    case SmartEvents.WaypointReached:
                    case SmartEvents.WaypointPaused:
                    case SmartEvents.WaypointResumed:
                    case SmartEvents.WaypointStopped:
                    case SmartEvents.WaypointEnded:
                    case SmartEvents.GossipSelect:
                    case SmartEvents.GossipHello:
                    case SmartEvents.JustCreated:
                    case SmartEvents.FollowCompleted:
                    case SmartEvents.OnSpellclick:
                    case SmartEvents.SceneStart:
                    case SmartEvents.SceneCancel:
                    case SmartEvents.SceneComplete:
                    case SmartEvents.SceneTrigger:
                    case SmartEvents.SpellEffectHit:
                        break;
                    default:
                        Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Not handled event_type({0}), Entry {1} SourceType {2} Event {3} Action {4}, skipped.", e.GetEventType(), e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType());
                        return false;
                }
            }

            switch (e.GetActionType())
            {
                case SmartActions.Talk:
                case SmartActions.SimpleTalk:
                    {
                        if (!IsTextValid(e, e.Action.talk.textGroupId))
                            return false;
                        break;
                    }
                case SmartActions.SetFaction:
                    if (e.Action.faction.factionID != 0 && CliDB.FactionTemplateStorage.LookupByKey(e.Action.faction.factionID) == null)
                    {
                        Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Faction {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.faction.factionID);
                        return false;
                    }
                    break;
                case SmartActions.MorphToEntryOrModel:
                case SmartActions.MountToEntryOrModel:
                    if (e.Action.morphOrMount.creature != 0 || e.Action.morphOrMount.model != 0)
                    {
                        if (e.Action.morphOrMount.creature > 0 && Global.ObjectMgr.GetCreatureTemplate(e.Action.morphOrMount.creature) == null)
                        {
                            Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Creature entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.morphOrMount.creature);
                            return false;
                        }

                        if (e.Action.morphOrMount.model != 0)
                        {
                            if (e.Action.morphOrMount.creature != 0)
                            {
                                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} has ModelID set with also set CreatureId, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType());
                                return false;
                            }
                            else if (!CliDB.CreatureDisplayInfoStorage.ContainsKey(e.Action.morphOrMount.model))
                            {
                                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Model id {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.morphOrMount.model);
                                return false;
                            }
                        }
                    }
                    break;
                case SmartActions.Sound:
                    if (!IsSoundValid(e, e.Action.sound.soundId))
                        return false;
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
                        Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses invalid AnimKit type {4}, skipped.", 
                            e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.animKit.type);
                        return false;
                    }
                    break;
                case SmartActions.FailQuest:
                case SmartActions.OfferQuest:
                    if (e.Action.quest.questId == 0 || !IsQuestValid(e, e.Action.quest.questId))
                        return false;
                    break;
                case SmartActions.ActivateTaxi:
                    {
                        if (!CliDB.TaxiPathStorage.ContainsKey(e.Action.taxi.id))
                        {
                            Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses invalid Taxi path ID {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.taxi.id);
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
                    if (e.Action.randomSound.sound5 != 0 && !IsSoundValid(e, e.Action.randomSound.sound5))
                        return false;
                    break;
                case SmartActions.Cast:
                    {
                        if (!IsSpellValid(e, e.Action.cast.spell))
                            return false;

                        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(e.Action.cast.spell);
                        foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(Difficulty.None))
                        {
                            if (effect != null && (effect.IsEffect(SpellEffectName.KillCredit) || effect.IsEffect(SpellEffectName.KillCredit2)))
                            {
                                if (effect.TargetA.GetTarget() == Targets.UnitCaster)
                                    Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} Effect: SPELL_EFFECT_KILL_CREDIT: (SpellId: {4} targetA: {5} - targetB: {6}) has invalid target for this Action",
                                    e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.cast.spell, effect.TargetA.GetTarget(), effect.TargetB.GetTarget());
                            }
                        }
                        break;
                    }
                case SmartActions.AddAura:
                case SmartActions.InvokerCast:
                    if (!IsSpellValid(e, e.Action.cast.spell))
                        return false;
                    break;
                case SmartActions.CallAreaexploredoreventhappens:
                case SmartActions.CallGroupeventhappens:
                    Quest qid = Global.ObjectMgr.GetQuestTemplate(e.Action.quest.questId);
                    if (qid != null)
                    {
                        if (!qid.HasSpecialFlag(QuestSpecialFlags.ExplorationOrEvent))
                        {
                            Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} SpecialFlags for Quest entry {4} does not include FLAGS_EXPLORATION_OR_EVENT(2), skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.quest.questId);
                            return false;
                        }
                    }
                    else
                    {
                        Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Quest entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.quest.questId);
                        return false;
                    }
                    break;
                case SmartActions.SetEventPhase:
                    if (e.Action.setEventPhase.phase >= (uint)SmartPhase.Max)
                    {
                        Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} attempts to set phase {4}. Phase mask cannot be used past phase {5}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.setEventPhase.phase, SmartPhase.Max - 1);
                        return false;
                    }
                    break;
                case SmartActions.IncEventPhase:
                    if (e.Action.incEventPhase.inc == 0 && e.Action.incEventPhase.dec == 0)
                    {
                        Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} is incrementing phase by 0, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType());
                        return false;
                    }
                    else if (e.Action.incEventPhase.inc > (uint)SmartPhase.Max || e.Action.incEventPhase.dec > (uint)SmartPhase.Max)
                    {
                        Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} attempts to increment phase by too large value, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType());
                        return false;
                    }
                    break;
                case SmartActions.Removeaurasfromspell:
                    if (e.Action.removeAura.spell != 0 && !IsSpellValid(e, e.Action.removeAura.spell))
                        return false;
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
                            Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} attempts to set invalid phase, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType());
                            return false;
                        }
                    }
                    break;
                case SmartActions.RandomPhaseRange:       //PhaseMin, PhaseMax
                    {
                        if (e.Action.randomPhaseRange.phaseMin >= (uint)SmartPhase.Max ||
                            e.Action.randomPhaseRange.phaseMax >= (uint)SmartPhase.Max)
                        {
                            Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} attempts to set invalid phase, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType());
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
                        Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses incorrect TempSummonType {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.summonCreature.type);
                        return false;
                    }
                    break;
                case SmartActions.CallKilledmonster:
                    if (!IsCreatureValid(e, e.Action.killedMonster.creature))
                        return false;

                    if (e.GetTargetType() == SmartTargets.Position)
                    {
                        Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses incorrect TargetType {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.GetTargetType());
                        return false;
                    }
                    break;
                case SmartActions.UpdateTemplate:
                    if (e.Action.updateTemplate.creature != 0 && !IsCreatureValid(e, e.Action.updateTemplate.creature))
                        return false;
                    break;
                case SmartActions.SetSheath:
                    if (e.Action.setSheath.sheath != 0 && e.Action.setSheath.sheath >= (uint)SheathState.Max)
                    {
                        Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses incorrect Sheath state {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.setSheath.sheath);
                        return false;
                    }
                    break;
                case SmartActions.SetReactState:
                    {
                        if (e.Action.react.state > (uint)ReactStates.Aggressive)
                        {
                            Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Creature {0} Event {1} Action {2} uses invalid React State {3}, skipped.", e.entryOrGuid, e.event_id, e.GetActionType(), e.Action.react.state);
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
                        Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Map entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.teleport.mapID);
                        return false;
                    }
                    break;
                case SmartActions.SetCounter:
                    if (e.Action.setCounter.counterId == 0)
                    {
                        Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses wrong counterId {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.setCounter.counterId);
                        return false;
                    }

                    if (e.Action.setCounter.value == 0)
                    {
                        Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses wrong value {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.setCounter.value);
                        return false;
                    }

                    break;
                case SmartActions.InstallAiTemplate:
                    if (e.Action.installTtemplate.id >= (uint)SmartAITemplate.End)
                    {
                        Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Creature {0} Event {1} Action {2} uses non-existent AI template id {3}, skipped.", e.entryOrGuid, e.event_id, e.GetActionType(), e.Action.installTtemplate.id);
                        return false;
                    }
                    break;
                case SmartActions.WpStop:
                    if (e.Action.wpStop.quest != 0 && !IsQuestValid(e, e.Action.wpStop.quest))
                        return false;
                    break;
                case SmartActions.WpStart:
                    {
                        List<WayPoint> path = Global.SmartAIMgr.GetPath(e.Action.wpStart.pathID);
                        if (path.Empty())
                        {
                            Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Creature {0} Event {1} Action {2} uses non-existent WaypointPath id {3}, skipped.", e.entryOrGuid, e.event_id, e.GetActionType(), e.Action.wpStart.pathID);
                            return false;
                        }
                        if (e.Action.wpStart.quest != 0 && !IsQuestValid(e, e.Action.wpStart.quest))
                            return false;
                        if (e.Action.wpStart.reactState > (uint)ReactStates.Aggressive)
                        {
                            Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Creature {0} Event {1} Action {2} uses invalid React State {3}, skipped.", e.entryOrGuid, e.event_id, e.GetActionType(), e.Action.wpStart.reactState);
                            return false;
                        }
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
                        if (!IsMinMaxValid(e, e.Action.randTimedActionList.entry1, e.Action.randTimedActionList.entry2))
                            return false;
                        break;
                    }
                case SmartActions.SetPower:
                case SmartActions.AddPower:
                case SmartActions.RemovePower:
                    if (e.Action.power.powerType > (int)PowerType.Max)
                    {
                        Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Power {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.power.powerType);
                        return false;
                    }
                    break;
                case SmartActions.GameEventStop:
                    {
                        uint eventId = e.Action.gameEventStop.id;

                        var events = Global.GameEventMgr.GetEventMap();
                        if (eventId < 1 || eventId >= events.Length)
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent event, eventId {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.gameEventStop.id);
                            return false;
                        }

                        GameEventData eventData = events[eventId];
                        if (!eventData.isValid())
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent event, eventId {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.gameEventStop.id);
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
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent event, eventId {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.gameEventStart.id);
                            return false;
                        }

                        GameEventData eventData = events[eventId];
                        if (!eventData.isValid())
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent event, eventId {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.gameEventStart.id);
                            return false;
                        }
                        break;
                    }
                case SmartActions.Equip:
                    {
                        if (e.GetScriptType() == SmartScriptType.Creature)
                        {
                            sbyte equipId = (sbyte)e.Action.equip.entry;

                            if (equipId != 0 && Global.ObjectMgr.GetEquipmentInfo((uint)e.entryOrGuid, equipId) == null)
                            {
                                Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_EQUIP uses non-existent equipment info id {0} for creature {1}, skipped.", equipId, e.entryOrGuid);
                                return false;
                            }
                        }
                        break;
                    }
                case SmartActions.SetInstData:
                    {
                        if (e.Action.setInstanceData.type > 1)
                        {
                            Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses invalid data type {4} (value range 0-1), skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.setInstanceData.type);
                            return false;
                        }
                        else if (e.Action.setInstanceData.type == 1)
                        {
                            if (e.Action.setInstanceData.data > (int)EncounterState.ToBeDecided)
                            {
                                Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses invalid boss state {4} (value range 0-5), skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), e.Action.setInstanceData.data);
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
                            Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_SET_INGAME_PHASE_ID uses invalid apply value {0} (Should be 0 or 1) for creature {1}, skipped", apply, e.entryOrGuid);
                            return false;
                        }

                        if (!CliDB.PhaseStorage.ContainsKey(phaseId))
                        {
                            Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_SET_INGAME_PHASE_ID uses invalid phaseid {0} for creature {1}, skipped", phaseId, e.entryOrGuid);
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
                            Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_SET_INGAME_PHASE_GROUP uses invalid apply value {0} (Should be 0 or 1) for creature {1}, skipped", apply, e.entryOrGuid);
                            return false;
                        }

                        if (Global.DB2Mgr.GetPhasesForGroup(phaseGroup).Empty())
                        {
                            Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_SET_INGAME_PHASE_GROUP uses invalid phase group id {0} for creature {1}, skipped", phaseGroup, e.entryOrGuid);
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
                case SmartActions.Follow:
                case SmartActions.SetOrientation:
                case SmartActions.StoreTargetList:
                case SmartActions.Evade:
                case SmartActions.FleeForAssist:
                case SmartActions.CombatStop:
                case SmartActions.Die:
                case SmartActions.SetInCombatWithZone:
                case SmartActions.SetActive:
                case SmartActions.WpResume:
                case SmartActions.KillUnit:
                case SmartActions.SetInvincibilityHpLevel:
                case SmartActions.ResetGobject:
                case SmartActions.AttackStart:
                case SmartActions.ThreatAllPct:
                case SmartActions.ThreatSinglePct:
                case SmartActions.SetInstData64:
                case SmartActions.AutoAttack:
                case SmartActions.AllowCombatMovement:
                case SmartActions.CallForHelp:
                case SmartActions.SetData:
                case SmartActions.SetVisibility:
                case SmartActions.WpPause:
                case SmartActions.SetFly:
                case SmartActions.SetRun:
                case SmartActions.SetSwim:
                case SmartActions.ForceDespawn:
                case SmartActions.SetUnitFlag:
                case SmartActions.RemoveUnitFlag:
                case SmartActions.Playmovie:
                case SmartActions.MoveToPos:
                case SmartActions.RespawnTarget:
                case SmartActions.CloseGossip:
                case SmartActions.TriggerTimedEvent:
                case SmartActions.RemoveTimedEvent:
                case SmartActions.OverrideScriptBaseObject:
                case SmartActions.ResetScriptBaseObject:
                case SmartActions.ActivateGobject:
                case SmartActions.CallScriptReset:
                case SmartActions.SetRangedMovement:
                case SmartActions.CallTimedActionlist:
                case SmartActions.SetNpcFlag:
                case SmartActions.AddNpcFlag:
                case SmartActions.RemoveNpcFlag:
                case SmartActions.CrossCast:
                case SmartActions.CallRandomTimedActionlist:
                case SmartActions.RandomMove:
                case SmartActions.SetUnitFieldBytes1:
                case SmartActions.RemoveUnitFieldBytes1:
                case SmartActions.InterruptSpell:
                case SmartActions.SendGoCustomAnim:
                case SmartActions.SetDynamicFlag:
                case SmartActions.AddDynamicFlag:
                case SmartActions.RemoveDynamicFlag:
                case SmartActions.JumpToPos:
                case SmartActions.SendGossipMenu:
                case SmartActions.GoSetLootState:
                case SmartActions.GoSetGoState:
                case SmartActions.SendTargetToTarget:
                case SmartActions.SetHomePos:
                case SmartActions.SetHealthRegen:
                case SmartActions.SetRoot:
                case SmartActions.SetGoFlag:
                case SmartActions.AddGoFlag:
                case SmartActions.RemoveGoFlag:
                case SmartActions.SummonCreatureGroup:
                case SmartActions.MoveOffset:
                case SmartActions.SetCorpseDelay:
                case SmartActions.DisableEvade:
                    break;
                default:
                    Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Not handled action_type({0}), event_type({1}), Entry {2} SourceType {3} Event {4}, skipped.", e.GetActionType(), e.GetEventType(), e.entryOrGuid, e.GetScriptType(), e.event_id);
                    return false;
            }

            return true;
        }
        bool IsAnimKitValid(SmartScriptHolder e, uint entry)
        {
            if (!CliDB.AnimKitStorage.ContainsKey(entry))
            {
                Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent AnimKit entry {4}, skipped.",
                    e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), entry);
                return false;
            }
            return true;
        }
        bool IsTextValid(SmartScriptHolder e, uint id)
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
                        if (e.entryOrGuid < 0)
                        {
                            ulong guid = (ulong)-e.entryOrGuid;
                            CreatureData data = Global.ObjectMgr.GetCreatureData(guid);
                            if (data == null)
                            {
                                Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} using non-existent Creature guid {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), guid);
                                return false;
                            }
                            else
                                entry = data.id;
                        }
                        else
                            entry = (uint)e.entryOrGuid;
                        break;
                }
            }

            if (entry == 0 || !Global.CreatureTextMgr.TextExist(entry, (byte)id))
            {
                Log.outError(LogFilter.Sql, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} using non-existent Text id {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), id);
                return false;
            }

            return true;
        }
        bool IsCreatureValid(SmartScriptHolder e, uint entry)
        {
            if (Global.ObjectMgr.GetCreatureTemplate(entry) == null)
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Creature entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), entry);
                return false;
            }
            return true;
        }
        bool IsGameObjectValid(SmartScriptHolder e, uint entry)
        {
            if (Global.ObjectMgr.GetGameObjectTemplate(entry) == null)
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent GameObject entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), entry);
                return false;
            }
            return true;
        }
        bool IsQuestValid(SmartScriptHolder e, uint entry)
        {
            if (Global.ObjectMgr.GetQuestTemplate(entry) == null)
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Quest entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), entry);
                return false;
            }
            return true;
        }
        bool IsSpellValid(SmartScriptHolder e, uint entry)
        {
            if (!Global.SpellMgr.HasSpellInfo(entry))
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Spell entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), entry);
                return false;
            }
            return true;
        }
        bool IsMinMaxValid(SmartScriptHolder e, uint min, uint max)
        {
            if (max < min)
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses min/max params wrong ({4}/{5}), skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), min, max);
                return false;
            }
            return true;
        }
        bool NotNULL(SmartScriptHolder e, uint data)
        {
            if (data == 0)
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} Parameter can not be NULL, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType());
                return false;
            }
            return true;
        }
        bool IsEmoteValid(SmartScriptHolder e, uint entry)
        {
            if (!CliDB.EmotesStorage.ContainsKey(entry))
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Emote entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), entry);
                return false;
            }
            return true;
        }
        bool IsItemValid(SmartScriptHolder e, uint entry)
        {
            if (!CliDB.ItemSparseStorage.ContainsKey(entry))
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Item entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), entry);
                return false;
            }
            return true;
        }
        bool IsTextEmoteValid(SmartScriptHolder e, uint entry)
        {
            if (!CliDB.EmotesTextStorage.ContainsKey(entry))
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Text Emote entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), entry);
                return false;
            }
            return true;
        }
        bool IsAreaTriggerValid(SmartScriptHolder e, uint entry)
        {
            if (!CliDB.AreaTriggerStorage.ContainsKey(entry))
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent AreaTrigger entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), entry);
                return false;
            }
            return true;
        }
        bool IsSoundValid(SmartScriptHolder e, uint entry)
        {
            if (!CliDB.SoundKitStorage.ContainsKey(entry))
            {
                Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: Entry {0} SourceType {1} Event {2} Action {3} uses non-existent Sound entry {4}, skipped.", e.entryOrGuid, e.GetScriptType(), e.event_id, e.GetActionType(), entry);
                return false;
            }
            return true;
        }

        public List<SmartScriptHolder> GetScript(int entry, SmartScriptType type)
        {
            List<SmartScriptHolder> temp = new List<SmartScriptHolder>();
            if (mEventMap[(uint)type].ContainsKey(entry))
            {
                foreach (var holder in mEventMap[(uint)type][entry])
                    temp.Add(new SmartScriptHolder(holder));
            }
            else
            {
                if (entry > 0)//first search is for guid (negative), do not drop error if not found
                    Log.outDebug(LogFilter.ScriptsAi, "SmartAIMgr.GetScript: Could not load Script for Entry {0} ScriptType {1}.", entry, type);
            }

            return temp;
        }

        public List<WayPoint> GetPath(uint id)
        {
            return waypoint_map.LookupByKey(id);
        }

        public SmartScriptHolder FindLinkedSourceEvent(List<SmartScriptHolder> list, uint eventId)
        {
            var sch = list.Find(p => p.link == eventId);
            if (sch != null)
                return sch;

            return null;
        }

        public SmartScriptHolder FindLinkedEvent(List<SmartScriptHolder> list, uint link)
        {
            var sch = list.Find(p => p.event_id == link && p.GetEventType() == SmartEvents.Link);
            if (sch != null)
                return sch;

            return null;
        }

        MultiMap<int, SmartScriptHolder>[] mEventMap = new MultiMap<int, SmartScriptHolder>[(int)SmartScriptType.Max];
        MultiMap<uint, WayPoint> waypoint_map = new MultiMap<uint, WayPoint>();

        Dictionary<SmartScriptType, uint> SmartAITypeMask = new Dictionary<SmartScriptType, uint>
        {
            { SmartScriptType.Creature,         SmartScriptTypeMaskId.Creature },
            { SmartScriptType.GameObject,       SmartScriptTypeMaskId.Gameobject },
            { SmartScriptType.AreaTrigger,      SmartScriptTypeMaskId.Areatrigger },
            { SmartScriptType.Event,            SmartScriptTypeMaskId.Event },
            { SmartScriptType.Gossip,           SmartScriptTypeMaskId.Gossip },
            { SmartScriptType.Quest,            SmartScriptTypeMaskId.Quest },
            { SmartScriptType.Spell,            SmartScriptTypeMaskId.Spell },
            { SmartScriptType.Transport,        SmartScriptTypeMaskId.Transport },
            { SmartScriptType.Instance,         SmartScriptTypeMaskId.Instance },
            { SmartScriptType.TimedActionlist,  SmartScriptTypeMaskId.TimedActionlist },
            { SmartScriptType.Scene,            SmartScriptTypeMaskId.Scene }
        };

        Dictionary<SmartEvents, uint> SmartAIEventMask = new Dictionary<SmartEvents, uint>
        {
            { SmartEvents.UpdateIc,                 SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.TimedActionlist },
            { SmartEvents.UpdateOoc,                SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject + SmartScriptTypeMaskId.Instance },
            { SmartEvents.HealtPct,                 SmartScriptTypeMaskId.Creature },
            { SmartEvents.ManaPct,                  SmartScriptTypeMaskId.Creature },
            { SmartEvents.Aggro,                    SmartScriptTypeMaskId.Creature },
            { SmartEvents.Kill,                     SmartScriptTypeMaskId.Creature },
            { SmartEvents.Death,                    SmartScriptTypeMaskId.Creature },
            { SmartEvents.Evade,                    SmartScriptTypeMaskId.Creature },
            { SmartEvents.SpellHit,                 SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.Range,                    SmartScriptTypeMaskId.Creature },
            { SmartEvents.OocLos,                   SmartScriptTypeMaskId.Creature },
            { SmartEvents.Respawn,                  SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.TargetHealthPct,          SmartScriptTypeMaskId.Creature },
            { SmartEvents.VictimCasting,            SmartScriptTypeMaskId.Creature },
            { SmartEvents.FriendlyHealth,           SmartScriptTypeMaskId.Creature },
            { SmartEvents.FriendlyIsCc,             SmartScriptTypeMaskId.Creature },
            { SmartEvents.FriendlyMissingBuff,      SmartScriptTypeMaskId.Creature },
            { SmartEvents.SummonedUnit,             SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.TargetManaPct,            SmartScriptTypeMaskId.Creature },
            { SmartEvents.AcceptedQuest,            SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.RewardQuest,              SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.ReachedHome,              SmartScriptTypeMaskId.Creature },
            { SmartEvents.ReceiveEmote,             SmartScriptTypeMaskId.Creature },
            { SmartEvents.HasAura,                  SmartScriptTypeMaskId.Creature },
            { SmartEvents.TargetBuffed,             SmartScriptTypeMaskId.Creature },
            { SmartEvents.Reset,                    SmartScriptTypeMaskId.Creature },
            { SmartEvents.IcLos,                    SmartScriptTypeMaskId.Creature },
            { SmartEvents.PassengerBoarded,         SmartScriptTypeMaskId.Creature },
            { SmartEvents.PassengerRemoved,         SmartScriptTypeMaskId.Creature },
            { SmartEvents.Charmed,                  SmartScriptTypeMaskId.Creature },
            { SmartEvents.CharmedTarget,            SmartScriptTypeMaskId.Creature },
            { SmartEvents.SpellhitTarget,           SmartScriptTypeMaskId.Creature },
            { SmartEvents.Damaged,                  SmartScriptTypeMaskId.Creature },
            { SmartEvents.DamagedTarget,            SmartScriptTypeMaskId.Creature },
            { SmartEvents.Movementinform,           SmartScriptTypeMaskId.Creature },
            { SmartEvents.SummonDespawned,          SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.CorpseRemoved,            SmartScriptTypeMaskId.Creature },
            { SmartEvents.AiInit,                   SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.DataSet,                  SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.WaypointStart,            SmartScriptTypeMaskId.Creature },
            { SmartEvents.WaypointReached,          SmartScriptTypeMaskId.Creature },
            { SmartEvents.TransportAddplayer,       SmartScriptTypeMaskId.Transport },
            { SmartEvents.TransportAddcreature,     SmartScriptTypeMaskId.Transport },
            { SmartEvents.TransportRemovePlayer,    SmartScriptTypeMaskId.Transport },
            { SmartEvents.TransportRelocate,        SmartScriptTypeMaskId.Transport },
            { SmartEvents.InstancePlayerEnter,      SmartScriptTypeMaskId.Instance },
            { SmartEvents.AreatriggerOntrigger,     SmartScriptTypeMaskId.Areatrigger },
            { SmartEvents.QuestAccepted,            SmartScriptTypeMaskId.Quest },
            { SmartEvents.QuestObjCompletion,       SmartScriptTypeMaskId.Quest },
            { SmartEvents.QuestRewarded,            SmartScriptTypeMaskId.Quest },
            { SmartEvents.QuestCompletion,          SmartScriptTypeMaskId.Quest },
            { SmartEvents.QuestFail,                SmartScriptTypeMaskId.Quest },
            { SmartEvents.TextOver,                 SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.ReceiveHeal,              SmartScriptTypeMaskId.Creature },
            { SmartEvents.JustSummoned,             SmartScriptTypeMaskId.Creature },
            { SmartEvents.WaypointPaused,           SmartScriptTypeMaskId.Creature },
            { SmartEvents.WaypointResumed,          SmartScriptTypeMaskId.Creature },
            { SmartEvents.WaypointStopped,          SmartScriptTypeMaskId.Creature },
            { SmartEvents.WaypointEnded,            SmartScriptTypeMaskId.Creature },
            { SmartEvents.TimedEventTriggered,      SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.Update,                   SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.Link,                     SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject + SmartScriptTypeMaskId.Areatrigger + SmartScriptTypeMaskId.Event + SmartScriptTypeMaskId.Gossip + SmartScriptTypeMaskId.Quest + SmartScriptTypeMaskId.Spell + SmartScriptTypeMaskId.Transport + SmartScriptTypeMaskId.Instance },
            { SmartEvents.GossipSelect,             SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.JustCreated,              SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.GossipHello,              SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.FollowCompleted,          SmartScriptTypeMaskId.Creature },
            { SmartEvents.DummyEffect,              SmartScriptTypeMaskId.Spell    },
            { SmartEvents.IsBehindTarget,           SmartScriptTypeMaskId.Creature },
            { SmartEvents.GameEventStart,           SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.GameEventEnd,             SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.GoStateChanged,           SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.GoEventInform,            SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.ActionDone,               SmartScriptTypeMaskId.Creature },
            { SmartEvents.OnSpellclick,             SmartScriptTypeMaskId.Creature },
            { SmartEvents.FriendlyHealthPCT,        SmartScriptTypeMaskId.Creature },
            { SmartEvents.DistanceCreature,         SmartScriptTypeMaskId.Creature },
            { SmartEvents.DistanceGameobject,       SmartScriptTypeMaskId.Creature },
            { SmartEvents.CounterSet,               SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject },
            { SmartEvents.SceneStart,               SmartScriptTypeMaskId.Scene },
            { SmartEvents.SceneTrigger,             SmartScriptTypeMaskId.Scene },
            { SmartEvents.SceneCancel,              SmartScriptTypeMaskId.Scene },
            { SmartEvents.SceneComplete,            SmartScriptTypeMaskId.Scene },
            { SmartEvents.SpellEffectHit,           SmartScriptTypeMaskId.Spell },
            { SmartEvents.SpellEffectHitTarget,     SmartScriptTypeMaskId.Spell }
        };
    }

    public class WayPoint
    {
        public uint id;
        public float x;
        public float y;
        public float z;
    }

    public class SmartScriptHolder
    {
        public SmartScriptHolder() { }
        public SmartScriptHolder(SmartScriptHolder other)
        {
            entryOrGuid = other.entryOrGuid;
            source_type = other.source_type;
            event_id = other.event_id;
            link = other.link;
            Event = other.Event;
            Action = other.Action;
            Target = other.Target;
            timer = other.timer;
            active = other.active;
            runOnce = other.runOnce;
            enableTimed = other.enableTimed;
        }

        public SmartScriptType GetScriptType() { return source_type; }
        public SmartEvents GetEventType() { return Event.type; }
        public SmartActions GetActionType() { return Action.type; }
        public SmartTargets GetTargetType() { return Target.type; }

        public int entryOrGuid;
        public SmartScriptType source_type;
        public uint event_id;
        public uint link;
        public SmartEvent Event;
        public SmartAction Action;
        public SmartTarget Target;
        public uint timer;
        public bool active;
        public bool runOnce;
        public bool enableTimed;
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
        public FriendlyHealt friendlyHealth;

        [FieldOffset(16)]
        public FriendlyCC friendlyCC;

        [FieldOffset(16)]
        public MissingBuff missingBuff;

        [FieldOffset(16)]
        public Summoned summoned;

        [FieldOffset(16)]
        public Quest quest;

        [FieldOffset(16)]
        public Emote emote;

        [FieldOffset(16)]
        public Aura aura;

        [FieldOffset(16)]
        public Charm charm;

        [FieldOffset(16)]
        public TargetAura targetAura;

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
        public Areatrigger areatrigger;

        [FieldOffset(16)]
        public TextOver textOver;

        [FieldOffset(16)]
        public TimedEvent timedEvent;

        [FieldOffset(16)]
        public Gossip gossip;

        [FieldOffset(16)]
        public Dummy dummy;

        [FieldOffset(16)]
        public BehindTarget behindTarget;

        [FieldOffset(16)]
        public GameEvent gameEvent;

        [FieldOffset(16)]
        public GoStateChanged goStateChanged;

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
        public Scene scene;

        [FieldOffset(16)]
        public Spell spell;

        [FieldOffset(16)]
        public Raw raw;

        [FieldOffset(32)]
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
            public uint noHostile;
            public uint maxDist;
            public uint cooldownMin;
            public uint cooldownMax;
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
        public struct FriendlyHealt
        {
            public uint hpDeficit;
            public uint radius;
            public uint repeatMin;
            public uint repeatMax;
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
        public struct TargetAura
        {
            public uint spell;
            public uint count;
            public uint repeatMin;
            public uint repeatMax;
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
        public struct Areatrigger
        {
            public uint id;
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
        public struct Gossip
        {
            public uint sender;
            public uint action;
        }
        public struct Dummy
        {
            public uint spell;
            public uint effIndex;
        }
        public struct BehindTarget
        {
            public uint cooldownMin;
            public uint cooldownMax;
        }
        public struct GameEvent
        {
            public uint gameEventId;
        }
        public struct GoStateChanged
        {
            public uint state;
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
        public struct Scene
        {
            public uint sceneId;
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
        public CastCreatureOrGO castCreatureOrGO;

        [FieldOffset(4)]
        public AddUnitFlag addUnitFlag;

        [FieldOffset(4)]
        public RemoveUnitFlag removeUnitFlag;

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
        public InstallTtemplate installTtemplate;

        [FieldOffset(4)]
        public SetRun setRun;

        [FieldOffset(4)]
        public SetFly setFly;

        [FieldOffset(4)]
        public SetSwim setSwim;

        [FieldOffset(4)]
        public Teleport teleport;

        [FieldOffset(4)]
        public SetCounter setCounter;

        [FieldOffset(4)]
        public StoreVar storeVar;

        [FieldOffset(4)]
        public StoreTargets storeTargets;

        [FieldOffset(4)]
        public TimeEvent timeEvent;

        [FieldOffset(4)]
        public Movie movie;

        [FieldOffset(4)]
        public Equip equip;

        [FieldOffset(4)]
        public UnitFlag unitFlag;

        [FieldOffset(4)]
        public SetunitByte setunitByte;

        [FieldOffset(4)]
        public DelunitByte delunitByte;

        [FieldOffset(4)]
        public EnterVehicle enterVehicle;

        [FieldOffset(4)]
        public TimedActionList timedActionList;

        [FieldOffset(4)]
        public RandTimedActionList randTimedActionList;

        [FieldOffset(4)]
        public InterruptSpellCasting interruptSpellCasting;

        [FieldOffset(4)]
        public SendGoCustomAnim sendGoCustomAnim;

        [FieldOffset(4)]
        public Jump jump;

        [FieldOffset(4)]
        public Flee flee;

        [FieldOffset(4)]
        public RespawnTarget respawnTarget;

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
        public GoFlag goFlag;

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
        public RandomSound randomSound;

        [FieldOffset(4)]
        public CorpseDelay corpseDelay;

        [FieldOffset(4)]
        public DisableEvade disableEvade;

        [FieldOffset(4)]
        public AnimKit animKit;

        [FieldOffset(4)]
        public Scene scene;

        [FieldOffset(4)]
        public Raw raw;

        #region Stucts
        public struct Talk
        {
            public uint textGroupId;
            public uint duration;
            public uint useTalkTarget;
        }
        public struct Faction
        {
            public uint factionID;
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
        }
        public struct CrossCast
        {
            public uint spell;
            public uint castFlags;
            public uint targetType;
            public uint targetParam1;
            public uint targetParam2;
            public uint targetParam3;
        }
        public struct SummonCreature
        {
            public uint creature;
            public uint type;
            public uint duration;
            public uint storageID;
            public uint attackInvoker;
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
        public struct AddUnitFlag
        {
            public uint flag1;
            public uint flag2;
            public uint flag3;
            public uint flag4;
            public uint flag5;
            public uint flag6;
        }
        public struct RemoveUnitFlag
        {
            public uint flag1;
            public uint flag2;
            public uint flag3;
            public uint flag4;
            public uint flag5;
            public uint flag6;
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
            public uint respawn;
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
            public uint reactState;
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
        public struct InstallTtemplate
        {
            public uint id;
            public uint param1;
            public uint param2;
            public uint param3;
            public uint param4;
            public uint param5;
        }
        public struct SetRun
        {
            public uint run;
        }
        public struct SetFly
        {
            public uint fly;
        }
        public struct SetSwim
        {
            public uint swim;
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
        public struct StoreVar
        {
            public uint id;
            public uint number;
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
        public struct UnitFlag
        {
            public uint flag;
            public uint type;
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
        public struct EnterVehicle
        {
            public uint seat;
        }
        public struct TimedActionList
        {
            public uint id;
            public uint timerType;
        }
        public struct RandTimedActionList
        {
            public uint entry1;
            public uint entry2;
            public uint entry3;
            public uint entry4;
            public uint entry5;
            public uint entry6;
        }
        public struct InterruptSpellCasting
        {
            public uint withDelayed;
            public uint spell_id;
            public uint withInstant;
        }
        public struct SendGoCustomAnim
        {
            public uint anim;
        }
        public struct Jump
        {
            public uint speedxy;
            public uint speedz;
        }
        public struct Flee
        {
            public uint withEmote;
        }
        public struct RespawnTarget
        {
            public uint goRespawnTime;
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
        public struct GoFlag
        {
            public uint flag;
        }
        public struct GoState
        {
            public uint state;
        }
        public struct CreatureGroup
        {
            public uint group;
            public uint attackInvoker;
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
        public struct RandomSound
        {
            public uint sound1;
            public uint sound2;
            public uint sound3;
            public uint sound4;
            public uint sound5;
            public uint onlySelf;
        }
        public struct CorpseDelay
        {
            public uint timer;
        }
        public struct DisableEvade
        {
            public uint disable;
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
        public struct Raw
        {
            public uint param1;
            public uint param2;
            public uint param3;
            public uint param4;
            public uint param5;
            public uint param6;
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
        public Position postion;

        [FieldOffset(20)]
        public Closest closest;

        [FieldOffset(20)]
        public ClosestAttackable closestAttackable;

        [FieldOffset(20)]
        public ClosestFriendly closestFriendly;

        [FieldOffset(20)]
        public Vehicle vehicle;

        [FieldOffset(20)]
        public Raw raw;

        #region Structs

        public struct UnitRange
        {
            public uint creature;
            public uint minDist;
            public uint maxDist;
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
        }
        public struct Position
        {
            public uint map;
        }
        public struct Closest
        {
            public uint entry;
            public uint dist;
            public uint dead;
        }
        public struct ClosestAttackable
        {
            public uint maxDist;
        }
        public struct ClosestFriendly
        {
            public uint maxDist;
        }
        public struct Vehicle
        {
            public uint seat;
        }
        public struct Raw
        {
            public uint param1;
            public uint param2;
            public uint param3;
        }
        #endregion
    }
}
