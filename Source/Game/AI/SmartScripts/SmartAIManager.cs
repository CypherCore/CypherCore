// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Movement;
using Game.Spells;
using static Game.AI.SmartAction;

namespace Game.AI
{
    public class SmartAIManager : Singleton<SmartAIManager>
	{
		private MultiMap<int, SmartScriptHolder>[] _eventMap = new MultiMap<int, SmartScriptHolder>[(int)SmartScriptType.Max];
		private Dictionary<uint, WaypointPath> _waypointStore = new();

		private SmartAIManager()
		{
			for (byte i = 0; i < (int)SmartScriptType.Max; i++)
				_eventMap[i] = new MultiMap<int, SmartScriptHolder>();
		}

		public void LoadFromDB()
		{
			uint oldMSTime = Time.GetMSTime();

			for (byte i = 0; i < (int)SmartScriptType.Max; i++)
				_eventMap[i].Clear(); //Drop Existing SmartAI List

			PreparedStatement stmt   = DB.World.GetPreparedStatement(WorldStatements.SEL_SMART_SCRIPTS);
			SQLResult         result = DB.World.Query(stmt);

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
					Log.outError(LogFilter.Sql, "SmartAIMgr.LoadSmartAI: invalid source_type ({0}), skipped loading.", source_type);

					continue;
				}

				if (temp.EntryOrGuid >= 0)
					switch (source_type)
					{
						case SmartScriptType.Creature:
							if (Global.ObjectMgr.GetCreatureTemplate((uint)temp.EntryOrGuid) == null)
							{
								Log.outError(LogFilter.Sql, "SmartAIMgr.LoadSmartAI: Creature entry ({0}) does not exist, skipped loading.", temp.EntryOrGuid);

								continue;
							}

							break;

						case SmartScriptType.GameObject:
						{
							if (Global.ObjectMgr.GetGameObjectTemplate((uint)temp.EntryOrGuid) == null)
							{
								Log.outError(LogFilter.Sql, "SmartAIMgr.LoadSmartAI: GameObject entry ({0}) does not exist, skipped loading.", temp.EntryOrGuid);

								continue;
							}

							break;
						}
						case SmartScriptType.AreaTrigger:
						{
							if (CliDB.AreaTableStorage.LookupByKey((uint)temp.EntryOrGuid) == null)
							{
								Log.outError(LogFilter.Sql, "SmartAIMgr.LoadSmartAI: AreaTrigger entry ({0}) does not exist, skipped loading.", temp.EntryOrGuid);

								continue;
							}

							break;
						}
						case SmartScriptType.Scene:
						{
							if (Global.ObjectMgr.GetSceneTemplate((uint)temp.EntryOrGuid) == null)
							{
								Log.outError(LogFilter.Sql, "SmartAIMgr.LoadFromDB: Scene Id ({0}) does not exist, skipped loading.", temp.EntryOrGuid);

								continue;
							}

							break;
						}
						case SmartScriptType.Quest:
						{
							if (Global.ObjectMgr.GetQuestTemplate((uint)temp.EntryOrGuid) == null)
							{
								Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: Quest Id ({temp.EntryOrGuid}) does not exist, skipped loading.");

								continue;
							}

							break;
						}
						case SmartScriptType.TimedActionlist:
							break; //nothing to check, really
						case SmartScriptType.AreaTriggerEntity:
						{
							if (Global.AreaTriggerDataStorage.GetAreaTriggerTemplate(new AreaTriggerId((uint)temp.EntryOrGuid, false)) == null)
							{
								Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: AreaTrigger entry ({temp.EntryOrGuid} IsServerSide false) does not exist, skipped loading.");

								continue;
							}

							break;
						}
						case SmartScriptType.AreaTriggerEntityServerside:
						{
							if (Global.AreaTriggerDataStorage.GetAreaTriggerTemplate(new AreaTriggerId((uint)temp.EntryOrGuid, true)) == null)
							{
								Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: AreaTrigger entry ({temp.EntryOrGuid} IsServerSide true) does not exist, skipped loading.");

								continue;
							}

							break;
						}
						default:
							Log.outError(LogFilter.Sql, "SmartAIMgr.LoadFromDB: not yet implemented source_type {0}", source_type);

							continue;
					}
				else
					switch (source_type)
					{
						case SmartScriptType.Creature:
						{
							CreatureData creature = Global.ObjectMgr.GetCreatureData((ulong)-temp.EntryOrGuid);

							if (creature == null)
							{
								Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: Creature Guid ({-temp.EntryOrGuid}) does not exist, skipped loading.");

								continue;
							}

							CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(creature.Id);

							if (creatureInfo == null)
							{
								Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: Creature entry ({creature.Id}) Guid ({-temp.EntryOrGuid}) does not exist, skipped loading.");

								continue;
							}

							if (creatureInfo.AIName != "SmartAI")
							{
								Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: Creature entry ({creature.Id}) Guid ({-temp.EntryOrGuid}) is not using SmartAI, skipped loading.");

								continue;
							}

							break;
						}
						case SmartScriptType.GameObject:
						{
							GameObjectData gameObject = Global.ObjectMgr.GetGameObjectData((ulong)-temp.EntryOrGuid);

							if (gameObject == null)
							{
								Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: GameObject Guid ({-temp.EntryOrGuid}) does not exist, skipped loading.");

								continue;
							}

							GameObjectTemplate gameObjectInfo = Global.ObjectMgr.GetGameObjectTemplate(gameObject.Id);

							if (gameObjectInfo == null)
							{
								Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: GameObject entry ({gameObject.Id}) Guid ({-temp.EntryOrGuid}) does not exist, skipped loading.");

								continue;
							}

							if (gameObjectInfo.AIName != "SmartGameObjectAI")
							{
								Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: GameObject entry ({gameObject.Id}) Guid ({-temp.EntryOrGuid}) is not using SmartGameObjectAI, skipped loading.");

								continue;
							}

							break;
						}
						default:
							Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: GUID-specific scripting not yet implemented for source_type {source_type}");

							continue;
					}

				temp.SourceType             = source_type;
				temp.EventId                = result.Read<ushort>(2);
				temp.Link                   = result.Read<ushort>(3);
				temp.Event.type             = (SmartEvents)result.Read<byte>(4);
				temp.Event.event_phase_mask = result.Read<ushort>(5);
				temp.Event.event_chance     = result.Read<byte>(6);
				temp.Event.event_flags      = (SmartEventFlags)result.Read<ushort>(7);

				temp.Event.raw.param1   = result.Read<uint>(8);
				temp.Event.raw.param2   = result.Read<uint>(9);
				temp.Event.raw.param3   = result.Read<uint>(10);
				temp.Event.raw.param4   = result.Read<uint>(11);
				temp.Event.raw.param5   = result.Read<uint>(12);
				temp.Event.param_string = result.Read<string>(13);

				temp.Action.type       = (SmartActions)result.Read<byte>(14);
				temp.Action.raw.param1 = result.Read<uint>(15);
				temp.Action.raw.param2 = result.Read<uint>(16);
				temp.Action.raw.param3 = result.Read<uint>(17);
				temp.Action.raw.param4 = result.Read<uint>(18);
				temp.Action.raw.param5 = result.Read<uint>(19);
				temp.Action.raw.param6 = result.Read<uint>(20);
				temp.Action.raw.param7 = result.Read<uint>(21);

				temp.Target.type       = (SmartTargets)result.Read<byte>(22);
				temp.Target.raw.param1 = result.Read<uint>(23);
				temp.Target.raw.param2 = result.Read<uint>(24);
				temp.Target.raw.param3 = result.Read<uint>(25);
				temp.Target.raw.param4 = result.Read<uint>(26);
				temp.Target.x          = result.Read<float>(27);
				temp.Target.y          = result.Read<float>(28);
				temp.Target.z          = result.Read<float>(29);
				temp.Target.o          = result.Read<float>(30);

				//check Target
				if (!IsTargetValid(temp))
					continue;

				// check all event and Action params
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
						if (temp.Event.minMaxRepeat.repeatMin == 0 &&
						    temp.Event.minMaxRepeat.repeatMax == 0 &&
						    !temp.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable) &&
						    temp.SourceType != SmartScriptType.TimedActionlist)
						{
							temp.Event.event_flags |= SmartEventFlags.NotRepeatable;
							Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: Entry {temp.EntryOrGuid} SourceType {temp.GetScriptType()}, Event {temp.EventId}, Missing Repeat flag.");
						}

						break;
					case SmartEvents.VictimCasting:
					case SmartEvents.IsBehindTarget:
						if (temp.Event.minMaxRepeat.min == 0 &&
						    temp.Event.minMaxRepeat.max == 0 &&
						    !temp.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable) &&
						    temp.SourceType != SmartScriptType.TimedActionlist)
						{
							temp.Event.event_flags |= SmartEventFlags.NotRepeatable;
							Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: Entry {temp.EntryOrGuid} SourceType {temp.GetScriptType()}, Event {temp.EventId}, Missing Repeat flag.");
						}

						break;
					case SmartEvents.FriendlyIsCc:
						if (temp.Event.friendlyCC.repeatMin == 0 &&
						    temp.Event.friendlyCC.repeatMax == 0 &&
						    !temp.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable) &&
						    temp.SourceType != SmartScriptType.TimedActionlist)
						{
							temp.Event.event_flags |= SmartEventFlags.NotRepeatable;
							Log.outError(LogFilter.Sql, $"SmartAIMgr.LoadFromDB: Entry {temp.EntryOrGuid} SourceType {temp.GetScriptType()}, Event {temp.EventId}, Missing Repeat flag.");
						}

						break;
					default:
						break;
				}

				// creature entry / Guid not found in storage, create empty event list for it and increase counters
				if (!_eventMap[(int)source_type].ContainsKey(temp.EntryOrGuid))
					++count;

				// store the new event
				_eventMap[(int)source_type].Add(temp.EntryOrGuid, temp);
			} while (result.NextRow());

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
							if (FindLinkedEvent(list, e.Link) == null)
								Log.outError(LogFilter.Sql,
								             "SmartAIMgr.LoadFromDB: Entry {0} SourceType {1}, Event {2}, Link Event {3} not found or invalid.",
								             e.EntryOrGuid,
								             e.GetScriptType(),
								             e.EventId,
								             e.Link);

						if (e.GetEventType() == SmartEvents.Link)
							if (FindLinkedSourceEvent(list, e.EventId) == null)
								Log.outError(LogFilter.Sql,
								             "SmartAIMgr.LoadFromDB: Entry {0} SourceType {1}, Event {2}, Link Source Event not found or invalid. Event will never trigger.",
								             e.EntryOrGuid,
								             e.GetScriptType(),
								             e.EventId);
					}
				}
			}

			Log.outInfo(LogFilter.ServerLoading, "Loaded {0} SmartAI scripts in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
		}

		public void LoadWaypointFromDB()
		{
			uint oldMSTime = Time.GetMSTime();

			_waypointStore.Clear();

			PreparedStatement stmt   = DB.World.GetPreparedStatement(WorldStatements.SEL_SMARTAI_WP);
			SQLResult         result = DB.World.Query(stmt);

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.ServerLoading, "Loaded 0 SmartAI Waypoint Paths. DB table `waypoints` is empty.");

				return;
			}

			uint count     = 0;
			uint total     = 0;
			uint lastEntry = 0;
			uint lastId    = 1;

			do
			{
				uint   entry = result.Read<uint>(0);
				uint   id    = result.Read<uint>(1);
				float  x     = result.Read<float>(2);
				float  y     = result.Read<float>(3);
				float  z     = result.Read<float>(4);
				float? o     = null;

				if (!result.IsNull(5))
					o = result.Read<float>(5);

				uint delay = result.Read<uint>(6);

				if (lastEntry != entry)
				{
					lastId = 1;
					++count;
				}

				if (lastId != id)
					Log.outError(LogFilter.Sql, $"SmartWaypointMgr.LoadFromDB: Path entry {entry}, unexpected point Id {id}, expected {lastId}.");

				++lastId;

				if (!_waypointStore.ContainsKey(entry))
					_waypointStore[entry] = new WaypointPath();

				WaypointPath path = _waypointStore[entry];
				path.id = entry;
				path.nodes.Add(new WaypointNode(id, x, y, z, o, delay));

				lastEntry = entry;
				++total;
			} while (result.NextRow());

			Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} SmartAI waypoint paths (total {total} waypoints) in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
		}

		private static bool EventHasInvoker(SmartEvents smartEvent)
		{
			switch (smartEvent)
			{
				// white list of events that actually have an invoker passed to them
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
				case SmartEvents.AreatriggerOntrigger:
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
					return true;
				default:
					return false;
			}
		}

		private static bool IsTargetValid(SmartScriptHolder e)
		{
			if (Math.Abs(e.Target.o) > 2 * MathFunctions.PI)
				Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} has abs(`Target.o` = {e.Target.o}) > 2*PI (orientation is expressed in radians)");

			switch (e.GetTargetType())
			{
				case SmartTargets.CreatureDistance:
				case SmartTargets.CreatureRange:
				{
					if (e.Target.unitDistance.creature != 0 &&
					    Global.ObjectMgr.GetCreatureTemplate(e.Target.unitDistance.creature) == null)
					{
						Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Creature entry {e.Target.unitDistance.creature} as target_param1, skipped.");

						return false;
					}

					break;
				}
				case SmartTargets.GameobjectDistance:
				case SmartTargets.GameobjectRange:
				{
					if (e.Target.goDistance.entry != 0 &&
					    Global.ObjectMgr.GetGameObjectTemplate(e.Target.goDistance.entry) == null)
					{
						Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent GameObject entry {e.Target.goDistance.entry} as target_param1, skipped.");

						return false;
					}

					break;
				}
				case SmartTargets.CreatureGuid:
				{
					if (e.Target.unitGUID.entry != 0 &&
					    !IsCreatureValid(e, e.Target.unitGUID.entry))
						return false;

					ulong        guid = e.Target.unitGUID.dbGuid;
					CreatureData data = Global.ObjectMgr.GetCreatureData(guid);

					if (data == null)
					{
						Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} using invalid creature Guid {guid} as target_param1, skipped.");

						return false;
					}
					else if (e.Target.unitGUID.entry != 0 &&
					         e.Target.unitGUID.entry != data.Id)
					{
						Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} using invalid creature entry {e.Target.unitGUID.entry} (expected {data.Id}) for Guid {guid} as target_param1, skipped.");

						return false;
					}

					break;
				}
				case SmartTargets.GameobjectGuid:
				{
					if (e.Target.goGUID.entry != 0 &&
					    !IsGameObjectValid(e, e.Target.goGUID.entry))
						return false;

					ulong          guid = e.Target.goGUID.dbGuid;
					GameObjectData data = Global.ObjectMgr.GetGameObjectData(guid);

					if (data == null)
					{
						Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} using invalid gameobject Guid {guid} as target_param1, skipped.");

						return false;
					}
					else if (e.Target.goGUID.entry != 0 &&
					         e.Target.goGUID.entry != data.Id)
					{
						Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} using invalid gameobject entry {e.Target.goGUID.entry} (expected {data.Id}) for Guid {guid} as target_param1, skipped.");

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
					if (e.GetScriptType() != SmartScriptType.TimedActionlist &&
					    e.GetEventType() != SmartEvents.Link &&
					    !EventHasInvoker(e.Event.type))
					{
						Log.outError(LogFilter.Sql, $"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.GetEventType()} Action {e.GetActionType()} has invoker Target, but event does not provide any invoker!");

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
					TC_SAI_IS_BOOLEAN_VALID(e, e.Target.unitClosest.dead);

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

		private static bool IsSpellVisualKitValid(SmartScriptHolder e, uint entry)
		{
			if (!CliDB.SpellVisualKitStorage.ContainsKey(entry))
			{
				Log.outError(LogFilter.Sql, $"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} uses non-existent SpellVisualKit entry {entry}, skipped.");

				return false;
			}

			return true;
		}

		private static bool CheckUnusedEventParams(SmartScriptHolder e)
		{
			int paramsStructSize = e.Event.type switch
			                       {
				                       SmartEvents.UpdateIc              => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
				                       SmartEvents.UpdateOoc             => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
				                       SmartEvents.HealthPct             => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
				                       SmartEvents.ManaPct               => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
				                       SmartEvents.Aggro                 => 0,
				                       SmartEvents.Kill                  => Marshal.SizeOf(typeof(SmartEvent.Kill)),
				                       SmartEvents.Death                 => 0,
				                       SmartEvents.Evade                 => 0,
				                       SmartEvents.SpellHit              => Marshal.SizeOf(typeof(SmartEvent.SpellHit)),
				                       SmartEvents.Range                 => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
				                       SmartEvents.OocLos                => Marshal.SizeOf(typeof(SmartEvent.Los)),
				                       SmartEvents.Respawn               => Marshal.SizeOf(typeof(SmartEvent.Respawn)),
				                       SmartEvents.VictimCasting         => Marshal.SizeOf(typeof(SmartEvent.TargetCasting)),
				                       SmartEvents.FriendlyIsCc          => Marshal.SizeOf(typeof(SmartEvent.FriendlyCC)),
				                       SmartEvents.FriendlyMissingBuff   => Marshal.SizeOf(typeof(SmartEvent.MissingBuff)),
				                       SmartEvents.SummonedUnit          => Marshal.SizeOf(typeof(SmartEvent.Summoned)),
				                       SmartEvents.AcceptedQuest         => Marshal.SizeOf(typeof(SmartEvent.Quest)),
				                       SmartEvents.RewardQuest           => Marshal.SizeOf(typeof(SmartEvent.Quest)),
				                       SmartEvents.ReachedHome           => 0,
				                       SmartEvents.ReceiveEmote          => Marshal.SizeOf(typeof(SmartEvent.Emote)),
				                       SmartEvents.HasAura               => Marshal.SizeOf(typeof(SmartEvent.Aura)),
				                       SmartEvents.TargetBuffed          => Marshal.SizeOf(typeof(SmartEvent.Aura)),
				                       SmartEvents.Reset                 => 0,
				                       SmartEvents.IcLos                 => Marshal.SizeOf(typeof(SmartEvent.Los)),
				                       SmartEvents.PassengerBoarded      => Marshal.SizeOf(typeof(SmartEvent.MinMax)),
				                       SmartEvents.PassengerRemoved      => Marshal.SizeOf(typeof(SmartEvent.MinMax)),
				                       SmartEvents.Charmed               => Marshal.SizeOf(typeof(SmartEvent.Charm)),
				                       SmartEvents.SpellHitTarget        => Marshal.SizeOf(typeof(SmartEvent.SpellHit)),
				                       SmartEvents.Damaged               => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
				                       SmartEvents.DamagedTarget         => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
				                       SmartEvents.Movementinform        => Marshal.SizeOf(typeof(SmartEvent.MovementInform)),
				                       SmartEvents.SummonDespawned       => Marshal.SizeOf(typeof(SmartEvent.Summoned)),
				                       SmartEvents.CorpseRemoved         => 0,
				                       SmartEvents.AiInit                => 0,
				                       SmartEvents.DataSet               => Marshal.SizeOf(typeof(SmartEvent.DataSet)),
				                       SmartEvents.WaypointReached       => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
				                       SmartEvents.TransportAddplayer    => 0,
				                       SmartEvents.TransportAddcreature  => Marshal.SizeOf(typeof(SmartEvent.TransportAddCreature)),
				                       SmartEvents.TransportRemovePlayer => 0,
				                       SmartEvents.TransportRelocate     => Marshal.SizeOf(typeof(SmartEvent.TransportRelocate)),
				                       SmartEvents.InstancePlayerEnter   => Marshal.SizeOf(typeof(SmartEvent.InstancePlayerEnter)),
				                       SmartEvents.AreatriggerOntrigger  => Marshal.SizeOf(typeof(SmartEvent.Areatrigger)),
				                       SmartEvents.QuestAccepted         => 0,
				                       SmartEvents.QuestObjCompletion    => 0,
				                       SmartEvents.QuestCompletion       => 0,
				                       SmartEvents.QuestRewarded         => 0,
				                       SmartEvents.QuestFail             => 0,
				                       SmartEvents.TextOver              => Marshal.SizeOf(typeof(SmartEvent.TextOver)),
				                       SmartEvents.ReceiveHeal           => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
				                       SmartEvents.JustSummoned          => 0,
				                       SmartEvents.WaypointPaused        => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
				                       SmartEvents.WaypointResumed       => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
				                       SmartEvents.WaypointStopped       => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
				                       SmartEvents.WaypointEnded         => Marshal.SizeOf(typeof(SmartEvent.Waypoint)),
				                       SmartEvents.TimedEventTriggered   => Marshal.SizeOf(typeof(SmartEvent.TimedEvent)),
				                       SmartEvents.Update                => Marshal.SizeOf(typeof(SmartEvent.MinMaxRepeat)),
				                       SmartEvents.Link                  => 0,
				                       SmartEvents.GossipSelect          => Marshal.SizeOf(typeof(SmartEvent.Gossip)),
				                       SmartEvents.JustCreated           => 0,
				                       SmartEvents.GossipHello           => Marshal.SizeOf(typeof(SmartEvent.GossipHello)),
				                       SmartEvents.FollowCompleted       => 0,
				                       SmartEvents.GameEventStart        => Marshal.SizeOf(typeof(SmartEvent.GameEvent)),
				                       SmartEvents.GameEventEnd          => Marshal.SizeOf(typeof(SmartEvent.GameEvent)),
				                       SmartEvents.GoLootStateChanged    => Marshal.SizeOf(typeof(SmartEvent.GoLootStateChanged)),
				                       SmartEvents.GoEventInform         => Marshal.SizeOf(typeof(SmartEvent.EventInform)),
				                       SmartEvents.ActionDone            => Marshal.SizeOf(typeof(SmartEvent.DoAction)),
				                       SmartEvents.OnSpellclick          => 0,
				                       SmartEvents.FriendlyHealthPCT     => Marshal.SizeOf(typeof(SmartEvent.FriendlyHealthPct)),
				                       SmartEvents.DistanceCreature      => Marshal.SizeOf(typeof(SmartEvent.Distance)),
				                       SmartEvents.DistanceGameobject    => Marshal.SizeOf(typeof(SmartEvent.Distance)),
				                       SmartEvents.CounterSet            => Marshal.SizeOf(typeof(SmartEvent.Counter)),
				                       SmartEvents.SceneStart            => 0,
				                       SmartEvents.SceneTrigger          => 0,
				                       SmartEvents.SceneCancel           => 0,
				                       SmartEvents.SceneComplete         => 0,
				                       SmartEvents.SummonedUnitDies      => Marshal.SizeOf(typeof(SmartEvent.Summoned)),
				                       SmartEvents.OnSpellCast           => Marshal.SizeOf(typeof(SmartEvent.SpellCast)),
				                       SmartEvents.OnSpellFailed         => Marshal.SizeOf(typeof(SmartEvent.SpellCast)),
				                       SmartEvents.OnSpellStart          => Marshal.SizeOf(typeof(SmartEvent.SpellCast)),
				                       SmartEvents.OnDespawn             => 0,
				                       _                                 => Marshal.SizeOf(typeof(SmartEvent.Raw))
			                       };

			int rawCount    = Marshal.SizeOf(typeof(SmartEvent.Raw)) / sizeof(uint);
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

			return true;
		}

		private static bool CheckUnusedActionParams(SmartScriptHolder e)
		{
			int paramsStructSize = e.Action.type switch
			                       {
				                       SmartActions.None                           => 0,
				                       SmartActions.Talk                           => Marshal.SizeOf(typeof(Talk)),
				                       SmartActions.SetFaction                     => Marshal.SizeOf(typeof(Faction)),
				                       SmartActions.MorphToEntryOrModel            => Marshal.SizeOf(typeof(MorphOrMount)),
				                       SmartActions.Sound                          => Marshal.SizeOf(typeof(Sound)),
				                       SmartActions.PlayEmote                      => Marshal.SizeOf(typeof(SmartAction.Emote)),
				                       SmartActions.FailQuest                      => Marshal.SizeOf(typeof(SmartAction.Quest)),
				                       SmartActions.OfferQuest                     => Marshal.SizeOf(typeof(QuestOffer)),
				                       SmartActions.SetReactState                  => Marshal.SizeOf(typeof(React)),
				                       SmartActions.ActivateGobject                => 0,
				                       SmartActions.RandomEmote                    => Marshal.SizeOf(typeof(RandomEmote)),
				                       SmartActions.Cast                           => Marshal.SizeOf(typeof(Cast)),
				                       SmartActions.SummonCreature                 => Marshal.SizeOf(typeof(SummonCreature)),
				                       SmartActions.ThreatSinglePct                => Marshal.SizeOf(typeof(ThreatPCT)),
				                       SmartActions.ThreatAllPct                   => Marshal.SizeOf(typeof(ThreatPCT)),
				                       SmartActions.CallAreaexploredoreventhappens => Marshal.SizeOf(typeof(SmartAction.Quest)),
				                       SmartActions.SetIngamePhaseGroup            => Marshal.SizeOf(typeof(IngamePhaseGroup)),
				                       SmartActions.SetEmoteState                  => Marshal.SizeOf(typeof(SmartAction.Emote)),
				                       SmartActions.AutoAttack                     => Marshal.SizeOf(typeof(AutoAttack)),
				                       SmartActions.AllowCombatMovement            => Marshal.SizeOf(typeof(CombatMove)),
				                       SmartActions.SetEventPhase                  => Marshal.SizeOf(typeof(SetEventPhase)),
				                       SmartActions.IncEventPhase                  => Marshal.SizeOf(typeof(IncEventPhase)),
				                       SmartActions.Evade                          => Marshal.SizeOf(typeof(Evade)),
				                       SmartActions.FleeForAssist                  => Marshal.SizeOf(typeof(FleeAssist)),
				                       SmartActions.CallGroupeventhappens          => Marshal.SizeOf(typeof(SmartAction.Quest)),
				                       SmartActions.CombatStop                     => 0,
				                       SmartActions.RemoveAurasFromSpell           => Marshal.SizeOf(typeof(RemoveAura)),
				                       SmartActions.Follow                         => Marshal.SizeOf(typeof(Follow)),
				                       SmartActions.RandomPhase                    => Marshal.SizeOf(typeof(RandomPhase)),
				                       SmartActions.RandomPhaseRange               => Marshal.SizeOf(typeof(RandomPhaseRange)),
				                       SmartActions.ResetGobject                   => 0,
				                       SmartActions.CallKilledmonster              => Marshal.SizeOf(typeof(KilledMonster)),
				                       SmartActions.SetInstData                    => Marshal.SizeOf(typeof(SetInstanceData)),
				                       SmartActions.SetInstData64                  => Marshal.SizeOf(typeof(SetInstanceData64)),
				                       SmartActions.UpdateTemplate                 => Marshal.SizeOf(typeof(UpdateTemplate)),
				                       SmartActions.Die                            => 0,
				                       SmartActions.SetInCombatWithZone            => 0,
				                       SmartActions.CallForHelp                    => Marshal.SizeOf(typeof(CallHelp)),
				                       SmartActions.SetSheath                      => Marshal.SizeOf(typeof(SetSheath)),
				                       SmartActions.ForceDespawn                   => Marshal.SizeOf(typeof(ForceDespawn)),
				                       SmartActions.SetInvincibilityHpLevel        => Marshal.SizeOf(typeof(InvincHP)),
				                       SmartActions.MountToEntryOrModel            => Marshal.SizeOf(typeof(MorphOrMount)),
				                       SmartActions.SetIngamePhaseId               => Marshal.SizeOf(typeof(IngamePhaseId)),
				                       SmartActions.SetData                        => Marshal.SizeOf(typeof(SetData)),
				                       SmartActions.AttackStop                     => 0,
				                       SmartActions.SetVisibility                  => Marshal.SizeOf(typeof(Visibility)),
				                       SmartActions.SetActive                      => Marshal.SizeOf(typeof(Active)),
				                       SmartActions.AttackStart                    => 0,
				                       SmartActions.SummonGo                       => Marshal.SizeOf(typeof(SummonGO)),
				                       SmartActions.KillUnit                       => 0,
				                       SmartActions.ActivateTaxi                   => Marshal.SizeOf(typeof(Taxi)),
				                       SmartActions.WpStart                        => Marshal.SizeOf(typeof(WpStart)),
				                       SmartActions.WpPause                        => Marshal.SizeOf(typeof(WpPause)),
				                       SmartActions.WpStop                         => Marshal.SizeOf(typeof(WpStop)),
				                       SmartActions.AddItem                        => Marshal.SizeOf(typeof(SmartAction.Item)),
				                       SmartActions.RemoveItem                     => Marshal.SizeOf(typeof(SmartAction.Item)),
				                       SmartActions.SetRun                         => Marshal.SizeOf(typeof(SetRun)),
				                       SmartActions.SetDisableGravity              => Marshal.SizeOf(typeof(SetDisableGravity)),
				                       SmartActions.Teleport                       => Marshal.SizeOf(typeof(Teleport)),
				                       SmartActions.SetCounter                     => Marshal.SizeOf(typeof(SetCounter)),
				                       SmartActions.StoreTargetList                => Marshal.SizeOf(typeof(StoreTargets)),
				                       SmartActions.WpResume                       => 0,
				                       SmartActions.SetOrientation                 => 0,
				                       SmartActions.CreateTimedEvent               => Marshal.SizeOf(typeof(TimeEvent)),
				                       SmartActions.Playmovie                      => Marshal.SizeOf(typeof(Movie)),
				                       SmartActions.MoveToPos                      => Marshal.SizeOf(typeof(MoveToPos)),
				                       SmartActions.EnableTempGobj                 => Marshal.SizeOf(typeof(EnableTempGO)),
				                       SmartActions.Equip                          => Marshal.SizeOf(typeof(Equip)),
				                       SmartActions.CloseGossip                    => 0,
				                       SmartActions.TriggerTimedEvent              => Marshal.SizeOf(typeof(TimeEvent)),
				                       SmartActions.RemoveTimedEvent               => Marshal.SizeOf(typeof(TimeEvent)),
				                       SmartActions.CallScriptReset                => 0,
				                       SmartActions.SetRangedMovement              => Marshal.SizeOf(typeof(SetRangedMovement)),
				                       SmartActions.CallTimedActionlist            => Marshal.SizeOf(typeof(TimedActionList)),
				                       SmartActions.SetNpcFlag                     => Marshal.SizeOf(typeof(Flag)),
				                       SmartActions.AddNpcFlag                     => Marshal.SizeOf(typeof(Flag)),
				                       SmartActions.RemoveNpcFlag                  => Marshal.SizeOf(typeof(Flag)),
				                       SmartActions.SimpleTalk                     => Marshal.SizeOf(typeof(SimpleTalk)),
				                       SmartActions.SelfCast                       => Marshal.SizeOf(typeof(Cast)),
				                       SmartActions.CrossCast                      => Marshal.SizeOf(typeof(CrossCast)),
				                       SmartActions.CallRandomTimedActionlist      => Marshal.SizeOf(typeof(RandTimedActionList)),
				                       SmartActions.CallRandomRangeTimedActionlist => Marshal.SizeOf(typeof(RandRangeTimedActionList)),
				                       SmartActions.RandomMove                     => Marshal.SizeOf(typeof(MoveRandom)),
				                       SmartActions.SetUnitFieldBytes1             => Marshal.SizeOf(typeof(SetunitByte)),
				                       SmartActions.RemoveUnitFieldBytes1          => Marshal.SizeOf(typeof(DelunitByte)),
				                       SmartActions.InterruptSpell                 => Marshal.SizeOf(typeof(InterruptSpellCasting)),
				                       SmartActions.AddDynamicFlag                 => Marshal.SizeOf(typeof(Flag)),
				                       SmartActions.RemoveDynamicFlag              => Marshal.SizeOf(typeof(Flag)),
				                       SmartActions.JumpToPos                      => Marshal.SizeOf(typeof(Jump)),
				                       SmartActions.SendGossipMenu                 => Marshal.SizeOf(typeof(SendGossipMenu)),
				                       SmartActions.GoSetLootState                 => Marshal.SizeOf(typeof(SetGoLootState)),
				                       SmartActions.SendTargetToTarget             => Marshal.SizeOf(typeof(SendTargetToTarget)),
				                       SmartActions.SetHomePos                     => 0,
				                       SmartActions.SetHealthRegen                 => Marshal.SizeOf(typeof(SetHealthRegen)),
				                       SmartActions.SetRoot                        => Marshal.SizeOf(typeof(SetRoot)),
				                       SmartActions.SummonCreatureGroup            => Marshal.SizeOf(typeof(SmartAction.CreatureGroup)),
				                       SmartActions.SetPower                       => Marshal.SizeOf(typeof(Power)),
				                       SmartActions.AddPower                       => Marshal.SizeOf(typeof(Power)),
				                       SmartActions.RemovePower                    => Marshal.SizeOf(typeof(Power)),
				                       SmartActions.GameEventStop                  => Marshal.SizeOf(typeof(GameEventStop)),
				                       SmartActions.GameEventStart                 => Marshal.SizeOf(typeof(GameEventStart)),
				                       SmartActions.StartClosestWaypoint           => Marshal.SizeOf(typeof(ClosestWaypointFromList)),
				                       SmartActions.MoveOffset                     => Marshal.SizeOf(typeof(MoveOffset)),
				                       SmartActions.RandomSound                    => Marshal.SizeOf(typeof(RandomSound)),
				                       SmartActions.SetCorpseDelay                 => Marshal.SizeOf(typeof(CorpseDelay)),
				                       SmartActions.DisableEvade                   => Marshal.SizeOf(typeof(DisableEvade)),
				                       SmartActions.GoSetGoState                   => Marshal.SizeOf(typeof(GoState)),
				                       SmartActions.AddThreat                      => Marshal.SizeOf(typeof(Threat)),
				                       SmartActions.LoadEquipment                  => Marshal.SizeOf(typeof(LoadEquipment)),
				                       SmartActions.TriggerRandomTimedEvent        => Marshal.SizeOf(typeof(RandomTimedEvent)),
				                       SmartActions.PauseMovement                  => Marshal.SizeOf(typeof(PauseMovement)),
				                       SmartActions.PlayAnimkit                    => Marshal.SizeOf(typeof(AnimKit)),
				                       SmartActions.ScenePlay                      => Marshal.SizeOf(typeof(Scene)),
				                       SmartActions.SceneCancel                    => Marshal.SizeOf(typeof(Scene)),
				                       SmartActions.SpawnSpawngroup                => Marshal.SizeOf(typeof(GroupSpawn)),
				                       SmartActions.DespawnSpawngroup              => Marshal.SizeOf(typeof(GroupSpawn)),
				                       SmartActions.RespawnBySpawnId               => Marshal.SizeOf(typeof(RespawnData)),
				                       SmartActions.InvokerCast                    => Marshal.SizeOf(typeof(Cast)),
				                       SmartActions.PlayCinematic                  => Marshal.SizeOf(typeof(Cinematic)),
				                       SmartActions.SetMovementSpeed               => Marshal.SizeOf(typeof(MovementSpeed)),
				                       SmartActions.PlaySpellVisualKit             => Marshal.SizeOf(typeof(SpellVisualKit)),
				                       SmartActions.OverrideLight                  => Marshal.SizeOf(typeof(OverrideLight)),
				                       SmartActions.OverrideWeather                => Marshal.SizeOf(typeof(OverrideWeather)),
				                       SmartActions.SetAIAnimKit                   => 0,
				                       SmartActions.SetHover                       => Marshal.SizeOf(typeof(SetHover)),
				                       SmartActions.SetHealthPct                   => Marshal.SizeOf(typeof(SetHealthPct)),
				                       SmartActions.CreateConversation             => Marshal.SizeOf(typeof(SmartAction.Conversation)),
				                       SmartActions.SetImmunePC                    => Marshal.SizeOf(typeof(SetImmunePC)),
				                       SmartActions.SetImmuneNPC                   => Marshal.SizeOf(typeof(SetImmuneNPC)),
				                       SmartActions.SetUninteractible              => Marshal.SizeOf(typeof(SetUninteractible)),
				                       SmartActions.ActivateGameobject             => Marshal.SizeOf(typeof(ActivateGameObject)),
				                       SmartActions.AddToStoredTargetList          => Marshal.SizeOf(typeof(AddToStoredTargets)),
				                       SmartActions.BecomePersonalCloneForPlayer   => Marshal.SizeOf(typeof(BecomePersonalClone)),
				                       SmartActions.TriggerGameEvent               => Marshal.SizeOf(typeof(TriggerGameEvent)),
				                       SmartActions.DoAction                       => Marshal.SizeOf(typeof(DoAction)),
				                       _                                           => Marshal.SizeOf(typeof(Raw))
			                       };

			int rawCount    = Marshal.SizeOf(typeof(Raw)) / sizeof(uint);
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

			return true;
		}

		private static bool CheckUnusedTargetParams(SmartScriptHolder e)
		{
			int paramsStructSize = e.Target.type switch
			                       {
				                       SmartTargets.None                       => 0,
				                       SmartTargets.Self                       => 0,
				                       SmartTargets.Victim                     => 0,
				                       SmartTargets.HostileSecondAggro         => Marshal.SizeOf(typeof(SmartTarget.HostilRandom)),
				                       SmartTargets.HostileLastAggro           => Marshal.SizeOf(typeof(SmartTarget.HostilRandom)),
				                       SmartTargets.HostileRandom              => Marshal.SizeOf(typeof(SmartTarget.HostilRandom)),
				                       SmartTargets.HostileRandomNotTop        => Marshal.SizeOf(typeof(SmartTarget.HostilRandom)),
				                       SmartTargets.ActionInvoker              => 0,
				                       SmartTargets.Position                   => 0, //Uses X,Y,Z,O
				                       SmartTargets.CreatureRange              => Marshal.SizeOf(typeof(SmartTarget.UnitRange)),
				                       SmartTargets.CreatureGuid               => Marshal.SizeOf(typeof(SmartTarget.UnitGUID)),
				                       SmartTargets.CreatureDistance           => Marshal.SizeOf(typeof(SmartTarget.UnitDistance)),
				                       SmartTargets.Stored                     => Marshal.SizeOf(typeof(SmartTarget.Stored)),
				                       SmartTargets.GameobjectRange            => Marshal.SizeOf(typeof(SmartTarget.GoRange)),
				                       SmartTargets.GameobjectGuid             => Marshal.SizeOf(typeof(SmartTarget.GoGUID)),
				                       SmartTargets.GameobjectDistance         => Marshal.SizeOf(typeof(SmartTarget.GoDistance)),
				                       SmartTargets.InvokerParty               => 0,
				                       SmartTargets.PlayerRange                => Marshal.SizeOf(typeof(SmartTarget.PlayerRange)),
				                       SmartTargets.PlayerDistance             => Marshal.SizeOf(typeof(SmartTarget.PlayerDistance)),
				                       SmartTargets.ClosestCreature            => Marshal.SizeOf(typeof(SmartTarget.UnitClosest)),
				                       SmartTargets.ClosestGameobject          => Marshal.SizeOf(typeof(SmartTarget.GoClosest)),
				                       SmartTargets.ClosestPlayer              => Marshal.SizeOf(typeof(SmartTarget.PlayerDistance)),
				                       SmartTargets.ActionInvokerVehicle       => 0,
				                       SmartTargets.OwnerOrSummoner            => Marshal.SizeOf(typeof(SmartTarget.Owner)),
				                       SmartTargets.ThreatList                 => Marshal.SizeOf(typeof(SmartTarget.ThreatList)),
				                       SmartTargets.ClosestEnemy               => Marshal.SizeOf(typeof(SmartTarget.ClosestAttackable)),
				                       SmartTargets.ClosestFriendly            => Marshal.SizeOf(typeof(SmartTarget.ClosestFriendly)),
				                       SmartTargets.LootRecipients             => 0,
				                       SmartTargets.Farthest                   => Marshal.SizeOf(typeof(SmartTarget.Farthest)),
				                       SmartTargets.VehiclePassenger           => Marshal.SizeOf(typeof(SmartTarget.Vehicle)),
				                       SmartTargets.ClosestUnspawnedGameobject => Marshal.SizeOf(typeof(SmartTarget.GoClosest)),
				                       _                                       => Marshal.SizeOf(typeof(SmartTarget.Raw))
			                       };

			int rawCount    = Marshal.SizeOf(typeof(SmartTarget.Raw)) / sizeof(uint);
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

			return true;
		}

		private bool IsEventValid(SmartScriptHolder e)
		{
			if (e.Event.type >= SmartEvents.End)
			{
				Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid event Type ({2}), skipped.", e.EntryOrGuid, e.EventId, e.GetEventType());

				return false;
			}

			// in SMART_SCRIPT_TYPE_TIMED_ACTIONLIST all event types are overriden by core
			if (e.GetScriptType() != SmartScriptType.TimedActionlist &&
			    !Convert.ToBoolean(GetEventMask(e.Event.type) & GetTypeMask(e.GetScriptType())))
			{
				Log.outError(LogFilter.Scripts, "SmartAIMgr: EntryOrGuid {0}, event Type {1} can not be used for Script Type {2}", e.EntryOrGuid, e.GetEventType(), e.GetScriptType());

				return false;
			}

			if (e.Action.type <= 0 ||
			    e.Action.type >= SmartActions.End)
			{
				Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid Action Type ({2}), skipped.", e.EntryOrGuid, e.EventId, e.GetActionType());

				return false;
			}

			if (e.Event.event_phase_mask > (uint)SmartEventPhaseBits.All)
			{
				Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid phase mask ({2}), skipped.", e.EntryOrGuid, e.EventId, e.Event.event_phase_mask);

				return false;
			}

			if (e.Event.event_flags > SmartEventFlags.All)
			{
				Log.outError(LogFilter.ScriptsAi, "SmartAIMgr: EntryOrGuid {0} using event({1}) has invalid event Flags ({2}), skipped.", e.EntryOrGuid, e.EventId, e.Event.event_flags);

				return false;
			}

			if (e.Link != 0 &&
			    e.Link == e.EventId)
			{
				Log.outError(LogFilter.Sql, "SmartAIMgr: EntryOrGuid {0} SourceType {1}, Event {2}, Event is linking self (infinite loop), skipped.", e.EntryOrGuid, e.GetScriptType(), e.EventId);

				return false;
			}

			if (e.GetScriptType() == SmartScriptType.TimedActionlist)
			{
				e.Event.type = SmartEvents.UpdateOoc; //Force default OOC, can change when calling the script!

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

							if (e.Event.spellHit.school != 0 &&
							    ((SpellSchoolMask)e.Event.spellHit.school & spellInfo.SchoolMask) != spellInfo.SchoolMask)
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
						if (e.Event.respawn.type == (uint)SmartRespawnCondition.Map &&
						    CliDB.MapStorage.LookupByKey(e.Event.respawn.map) == null)
						{
							Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Map entry {e.Event.respawn.map}, skipped.");

							return false;
						}

						if (e.Event.respawn.type == (uint)SmartRespawnCondition.Area &&
						    !CliDB.AreaTableStorage.ContainsKey(e.Event.respawn.area))
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

						if (e.Event.kill.creature != 0 &&
						    !IsCreatureValid(e, e.Event.kill.creature))
							return false;

						TC_SAI_IS_BOOLEAN_VALID(e, e.Event.kill.playerOnly);

						break;
					case SmartEvents.VictimCasting:
						if (e.Event.targetCasting.spellId > 0 &&
						    !Global.SpellMgr.HasSpellInfo(e.Event.targetCasting.spellId, Difficulty.None))
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
						if (e.Event.summoned.creature != 0 &&
						    !IsCreatureValid(e, e.Event.summoned.creature))
							return false;

						if (!IsMinMaxValid(e, e.Event.summoned.cooldownMin, e.Event.summoned.cooldownMax))
							return false;

						break;
					case SmartEvents.AcceptedQuest:
					case SmartEvents.RewardQuest:
						if (e.Event.quest.questId != 0 &&
						    !IsQuestValid(e, e.Event.quest.questId))
							return false;

						if (!IsMinMaxValid(e, e.Event.quest.cooldownMin, e.Event.quest.cooldownMax))
							return false;

						break;
					case SmartEvents.ReceiveEmote:
					{
						if (e.Event.emote.emoteId != 0 &&
						    !IsTextEmoteValid(e, e.Event.emote.emoteId))
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
						if (e.Event.transportAddCreature.creature != 0 &&
						    !IsCreatureValid(e, e.Event.transportAddCreature.creature))
							return false;

						break;
					}
					case SmartEvents.Movementinform:
					{
						if (MotionMaster.IsInvalidMovementGeneratorType((MovementGeneratorType)e.Event.movementInform.type))
						{
							Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses invalid Motion Type {e.Event.movementInform.type}, skipped.");

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
						if (e.Event.areatrigger.id != 0 &&
						    (e.GetScriptType() == SmartScriptType.AreaTriggerEntity || e.GetScriptType() == SmartScriptType.AreaTriggerEntityServerside))
						{
							Log.outError(LogFilter.Sql, $"SmartAIMgr: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} areatrigger param not supported for SMART_SCRIPT_TYPE_AREATRIGGER_ENTITY and SMART_SCRIPT_TYPE_AREATRIGGER_ENTITY_SERVERSIDE, skipped.");

							return false;
						}

						if (e.Event.areatrigger.id != 0 &&
						    !IsAreaTriggerValid(e, e.Event.areatrigger.id))
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

						if (e.Event.gameEvent.gameEventId >= events.Length ||
						    !events[e.Event.gameEvent.gameEventId].IsValid())
							return false;

						break;
					}
					case SmartEvents.FriendlyHealthPCT:
						if (!IsMinMaxValid(e, e.Event.friendlyHealthPct.repeatMin, e.Event.friendlyHealthPct.repeatMax))
							return false;

						if (e.Event.friendlyHealthPct.maxHpPct > 100 ||
						    e.Event.friendlyHealthPct.minHpPct > 100)
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
						if (e.Event.distance.guid == 0 &&
						    e.Event.distance.entry == 0)
						{
							Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_CREATURE did not provide creature Guid or entry, skipped.");

							return false;
						}

						if (e.Event.distance.guid != 0 &&
						    e.Event.distance.entry != 0)
						{
							Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_CREATURE provided both an entry and Guid, skipped.");

							return false;
						}

						if (e.Event.distance.guid != 0 &&
						    Global.ObjectMgr.GetCreatureData(e.Event.distance.guid) == null)
						{
							Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_CREATURE using invalid creature Guid {0}, skipped.", e.Event.distance.guid);

							return false;
						}

						if (e.Event.distance.entry != 0 &&
						    Global.ObjectMgr.GetCreatureTemplate(e.Event.distance.entry) == null)
						{
							Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_CREATURE using invalid creature entry {0}, skipped.", e.Event.distance.entry);

							return false;
						}

						break;
					case SmartEvents.DistanceGameobject:
						if (e.Event.distance.guid == 0 &&
						    e.Event.distance.entry == 0)
						{
							Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_GAMEOBJECT did not provide gameobject Guid or entry, skipped.");

							return false;
						}

						if (e.Event.distance.guid != 0 &&
						    e.Event.distance.entry != 0)
						{
							Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_GAMEOBJECT provided both an entry and Guid, skipped.");

							return false;
						}

						if (e.Event.distance.guid != 0 &&
						    Global.ObjectMgr.GetGameObjectData(e.Event.distance.guid) == null)
						{
							Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_DISTANCE_GAMEOBJECT using invalid gameobject Guid {0}, skipped.", e.Event.distance.guid);

							return false;
						}

						if (e.Event.distance.entry != 0 &&
						    Global.ObjectMgr.GetGameObjectTemplate(e.Event.distance.entry) == null)
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
							Log.outError(LogFilter.Sql, "SmartAIMgr: Event SMART_EVENT_COUNTER_SET using invalid counter Id {0}, skipped.", e.Event.counter.id);

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
							Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses event SMART_EVENT_RESET and Action SMART_ACTION_CALL_SCRIPT_RESET, skipped.");

							return false;
						}

						break;
					case SmartEvents.Charmed:
						TC_SAI_IS_BOOLEAN_VALID(e, e.Event.charm.onRemove);

						break;
					case SmartEvents.QuestObjCompletion:
						if (Global.ObjectMgr.GetQuestObjective(e.Event.questObjective.id) == null)
						{
							Log.outError(LogFilter.Sql, $"SmartAIMgr: Event SMART_EVENT_QUEST_OBJ_COMPLETION using invalid objective Id {e.Event.questObjective.id}, skipped.");

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
					if (e.Action.faction.factionId != 0 &&
					    CliDB.FactionTemplateStorage.LookupByKey(e.Action.faction.factionId) == null)
					{
						Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Faction {e.Action.faction.factionId}, skipped.");

						return false;
					}

					break;
				case SmartActions.MorphToEntryOrModel:
				case SmartActions.MountToEntryOrModel:
					if (e.Action.morphOrMount.creature != 0 ||
					    e.Action.morphOrMount.model != 0)
					{
						if (e.Action.morphOrMount.creature > 0 &&
						    Global.ObjectMgr.GetCreatureTemplate(e.Action.morphOrMount.creature) == null)
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
								Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Model Id {e.Action.morphOrMount.model}, skipped.");

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
					if (e.Action.animKit.animKit != 0 &&
					    !IsAnimKitValid(e, e.Action.animKit.animKit))
						return false;

					if (e.Action.animKit.type > 3)
					{
						Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses invalid AnimKit Type {e.Action.animKit.type}, skipped.");

						return false;
					}

					break;
				case SmartActions.PlaySpellVisualKit:
					if (e.Action.spellVisualKit.spellVisualKitId != 0 &&
					    !IsSpellVisualKitValid(e, e.Action.spellVisualKit.spellVisualKitId))
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
					if (e.Action.randomEmote.emote1 != 0 &&
					    !IsEmoteValid(e, e.Action.randomEmote.emote1))
						return false;

					if (e.Action.randomEmote.emote2 != 0 &&
					    !IsEmoteValid(e, e.Action.randomEmote.emote2))
						return false;

					if (e.Action.randomEmote.emote3 != 0 &&
					    !IsEmoteValid(e, e.Action.randomEmote.emote3))
						return false;

					if (e.Action.randomEmote.emote4 != 0 &&
					    !IsEmoteValid(e, e.Action.randomEmote.emote4))
						return false;

					if (e.Action.randomEmote.emote5 != 0 &&
					    !IsEmoteValid(e, e.Action.randomEmote.emote5))
						return false;

					if (e.Action.randomEmote.emote6 != 0 &&
					    !IsEmoteValid(e, e.Action.randomEmote.emote6))
						return false;

					break;
				case SmartActions.RandomSound:
					if (e.Action.randomSound.sound1 != 0 &&
					    !IsSoundValid(e, e.Action.randomSound.sound1))
						return false;

					if (e.Action.randomSound.sound2 != 0 &&
					    !IsSoundValid(e, e.Action.randomSound.sound2))
						return false;

					if (e.Action.randomSound.sound3 != 0 &&
					    !IsSoundValid(e, e.Action.randomSound.sound3))
						return false;

					if (e.Action.randomSound.sound4 != 0 &&
					    !IsSoundValid(e, e.Action.randomSound.sound4))
						return false;

					TC_SAI_IS_BOOLEAN_VALID(e, e.Action.randomSound.onlySelf);

					break;
				case SmartActions.Cast:
				{
					if (!IsSpellValid(e, e.Action.cast.spell))
						return false;

					SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(e.Action.cast.spell, Difficulty.None);

					foreach (var spellEffectInfo in spellInfo.GetEffects())
						if (spellEffectInfo.IsEffect(SpellEffectName.KillCredit) ||
						    spellEffectInfo.IsEffect(SpellEffectName.KillCredit2))
							if (spellEffectInfo.TargetA.GetTarget() == Targets.UnitCaster)
								Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} Effect: SPELL_EFFECT_KILL_CREDIT: (SpellId: {e.Action.cast.spell} targetA: {spellEffectInfo.TargetA.GetTarget()} - targetB: {spellEffectInfo.TargetB.GetTarget()}) has invalid Target for this Action");

					break;
				}
				case SmartActions.CrossCast:
				{
					if (!IsSpellValid(e, e.Action.crossCast.spell))
						return false;

					SmartTargets targetType = (SmartTargets)e.Action.crossCast.targetType;

					if (targetType == SmartTargets.CreatureGuid ||
					    targetType == SmartTargets.GameobjectGuid)
					{
						if (e.Action.crossCast.targetParam2 != 0)
						{
							if (targetType == SmartTargets.CreatureGuid &&
							    !IsCreatureValid(e, e.Action.crossCast.targetParam2))
								return false;
							else if (targetType == SmartTargets.GameobjectGuid &&
							         !IsGameObjectValid(e, e.Action.crossCast.targetParam2))
								return false;
						}

						ulong           guid      = e.Action.crossCast.targetParam1;
						SpawnObjectType spawnType = targetType == SmartTargets.CreatureGuid ? SpawnObjectType.Creature : SpawnObjectType.GameObject;
						var             data      = Global.ObjectMgr.GetSpawnData(spawnType, guid);

						if (data == null)
						{
							Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} specifies invalid CasterTargetType Guid ({spawnType},{guid})");

							return false;
						}
						else if (e.Action.crossCast.targetParam2 != 0 &&
						         e.Action.crossCast.targetParam2 != data.Id)
						{
							Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} specifies invalid entry {e.Action.crossCast.targetParam2} (expected {data.Id}) for CasterTargetType Guid ({spawnType},{guid})");

							return false;
						}
					}

					break;
				}
				case SmartActions.InvokerCast:
					if (e.GetScriptType() != SmartScriptType.TimedActionlist &&
					    e.GetEventType() != SmartEvents.Link &&
					    !EventHasInvoker(e.Event.type))
					{
						Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} has invoker cast Action, but event does not provide any invoker!");

						return false;
					}

					if (!IsSpellValid(e, e.Action.cast.spell))
						return false;

					break;
				case SmartActions.SelfCast:
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
							Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} SpecialFlags for Quest entry {e.Action.quest.questId} does not include FLAGS_EXPLORATION_OR_EVENT(2), skipped.");

							return false;
						}
					}
					else
					{
						Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Quest entry {e.Action.quest.questId}, skipped.");

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
					if (e.Action.incEventPhase.inc == 0 &&
					    e.Action.incEventPhase.dec == 0)
					{
						Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} is incrementing phase by 0, skipped.");

						return false;
					}
					else if (e.Action.incEventPhase.inc > (uint)SmartPhase.Max ||
					         e.Action.incEventPhase.dec > (uint)SmartPhase.Max)
					{
						Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} attempts to increment phase by too large value, skipped.");

						return false;
					}

					break;
				case SmartActions.RemoveAurasFromSpell:
					if (e.Action.removeAura.spell != 0 &&
					    !IsSpellValid(e, e.Action.removeAura.spell))
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
				case SmartActions.RandomPhaseRange: //PhaseMin, PhaseMax
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

					if (e.Action.summonCreature.type < (uint)TempSummonType.TimedOrDeadDespawn ||
					    e.Action.summonCreature.type > (uint)TempSummonType.ManualDespawn)
					{
						Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses incorrect TempSummonType {e.Action.summonCreature.type}, skipped.");

						return false;
					}

					TC_SAI_IS_BOOLEAN_VALID(e, e.Action.summonCreature.attackInvoker);

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
					if (e.Action.updateTemplate.creature != 0 &&
					    !IsCreatureValid(e, e.Action.updateTemplate.creature))
						return false;

					TC_SAI_IS_BOOLEAN_VALID(e, e.Action.updateTemplate.updateLevel);

					break;
				case SmartActions.SetSheath:
					if (e.Action.setSheath.sheath != 0 &&
					    e.Action.setSheath.sheath >= (uint)SheathState.Max)
					{
						Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses incorrect Sheath State {e.Action.setSheath.sheath}, skipped.");

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
					if (e.Action.wpStop.quest != 0 &&
					    !IsQuestValid(e, e.Action.wpStop.quest))
						return false;

					TC_SAI_IS_BOOLEAN_VALID(e, e.Action.wpStop.fail);

					break;
				case SmartActions.WpStart:
				{
					WaypointPath path = GetPath(e.Action.wpStart.pathID);

					if (path == null ||
					    path.nodes.Empty())
					{
						Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent WaypointPath Id {e.Action.wpStart.pathID}, skipped.");

						return false;
					}

					if (e.Action.wpStart.quest != 0 &&
					    !IsQuestValid(e, e.Action.wpStart.quest))
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

					if (eventId < 1 ||
					    eventId >= events.Length)
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

					if (eventId < 1 ||
					    eventId >= events.Length)
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

						if (equipId != 0 &&
						    Global.ObjectMgr.GetEquipmentInfo((uint)e.EntryOrGuid, equipId) == null)
						{
							Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_EQUIP uses non-existent equipment info Id {0} for creature {1}, skipped.", equipId, e.EntryOrGuid);

							return false;
						}
					}

					break;
				}
				case SmartActions.SetInstData:
				{
					if (e.Action.setInstanceData.type > 1)
					{
						Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses invalid _data Type {e.Action.setInstanceData.type} (value range 0-1), skipped.");

						return false;
					}
					else if (e.Action.setInstanceData.type == 1)
					{
						if (e.Action.setInstanceData.data > (int)EncounterState.ToBeDecided)
						{
							Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses invalid boss State {e.Action.setInstanceData.data} (value range 0-5), skipped.");

							return false;
						}
					}

					break;
				}
				case SmartActions.SetIngamePhaseId:
				{
					uint phaseId = e.Action.ingamePhaseId.id;
					uint apply   = e.Action.ingamePhaseId.apply;

					if (apply != 0 &&
					    apply != 1)
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
					uint apply      = e.Action.ingamePhaseGroup.apply;

					if (apply != 0 &&
					    apply != 1)
					{
						Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_SET_INGAME_PHASE_GROUP uses invalid apply value {0} (Should be 0 or 1) for creature {1}, skipped", apply, e.EntryOrGuid);

						return false;
					}

					if (Global.DB2Mgr.GetPhasesForGroup(phaseGroup).Empty())
					{
						Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_SET_INGAME_PHASE_GROUP uses invalid phase group Id {0} for creature {1}, skipped", phaseGroup, e.EntryOrGuid);

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
						Log.outError(LogFilter.Sql, $"Entry {e.EntryOrGuid} SourceType {e.GetScriptType()} Event {e.EventId} Action {e.GetActionType()} specifies invalid spawn _data ({e.Action.respawnData.spawnType},{e.Action.respawnData.spawnId})");

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

					if (e.Action.movementSpeed.speedInteger == 0 &&
					    e.Action.movementSpeed.speedFraction == 0)
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

					if (areaEntry.ParentAreaID != 0)
					{
						Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses subzone (ID: {e.Action.overrideLight.zoneId}) instead of zone, skipped.");

						return false;
					}

					if (!CliDB.LightStorage.ContainsKey(e.Action.overrideLight.areaLightId))
					{
						Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses non-existent areaLightId {e.Action.overrideLight.areaLightId}, skipped.");

						return false;
					}

					if (e.Action.overrideLight.overrideLightId != 0 &&
					    !CliDB.LightStorage.ContainsKey(e.Action.overrideLight.overrideLightId))
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

					if (areaEntry.ParentAreaID != 0)
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
					if (e.Action.setHealthPct.percent > 100 ||
					    e.Action.setHealthPct.percent == 0)
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
					break;
				case SmartActions.BecomePersonalCloneForPlayer:
				{
					if (e.Action.becomePersonalClone.type < (uint)TempSummonType.TimedOrDeadDespawn ||
					    e.Action.becomePersonalClone.type > (uint)TempSummonType.ManualDespawn)
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
				// No longer supported
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

		private static bool IsAnimKitValid(SmartScriptHolder e, uint entry)
		{
			if (!CliDB.AnimKitStorage.ContainsKey(entry))
			{
				Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses non-existent AnimKit entry {entry}, skipped.");

				return false;
			}

			return true;
		}

		private static bool IsTextValid(SmartScriptHolder e, uint id)
		{
			if (e.GetScriptType() != SmartScriptType.Creature)
				return true;

			uint entry;

			if (e.GetEventType() == SmartEvents.TextOver)
				entry = e.Event.textOver.creatureEntry;
			else
				switch (e.GetTargetType())
				{
					case SmartTargets.CreatureDistance:
					case SmartTargets.CreatureRange:
					case SmartTargets.ClosestCreature:
						return true; // ignore
					default:
						if (e.EntryOrGuid < 0)
						{
							ulong        guid = (ulong)-e.EntryOrGuid;
							CreatureData data = Global.ObjectMgr.GetCreatureData(guid);

							if (data == null)
							{
								Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} using non-existent Creature Guid {guid}, skipped.");

								return false;
							}
							else
							{
								entry = data.Id;
							}
						}
						else
						{
							entry = (uint)e.EntryOrGuid;
						}

						break;
				}

			if (entry == 0 ||
			    !Global.CreatureTextMgr.TextExist(entry, (byte)id))
			{
				Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} using non-existent Text Id {id}, skipped.");

				return false;
			}

			return true;
		}

		private static bool IsCreatureValid(SmartScriptHolder e, uint entry)
		{
			if (Global.ObjectMgr.GetCreatureTemplate(entry) == null)
			{
				Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Creature entry {entry}, skipped.");

				return false;
			}

			return true;
		}

		private static bool IsGameObjectValid(SmartScriptHolder e, uint entry)
		{
			if (Global.ObjectMgr.GetGameObjectTemplate(entry) == null)
			{
				Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent GameObject entry {entry}, skipped.");

				return false;
			}

			return true;
		}

		private static bool IsQuestValid(SmartScriptHolder e, uint entry)
		{
			if (Global.ObjectMgr.GetQuestTemplate(entry) == null)
			{
				Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Quest entry {entry}, skipped.");

				return false;
			}

			return true;
		}

		private static bool IsSpellValid(SmartScriptHolder e, uint entry)
		{
			if (!Global.SpellMgr.HasSpellInfo(entry, Difficulty.None))
			{
				Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Spell entry {entry}, skipped.");

				return false;
			}

			return true;
		}

		private static bool IsMinMaxValid(SmartScriptHolder e, uint min, uint max)
		{
			if (max < min)
			{
				Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses min/max params wrong ({min}/{max}), skipped.");

				return false;
			}

			return true;
		}

		private static bool NotNULL(SmartScriptHolder e, uint data)
		{
			if (data == 0)
			{
				Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} Parameter can not be NULL, skipped.");

				return false;
			}

			return true;
		}

		private static bool IsEmoteValid(SmartScriptHolder e, uint entry)
		{
			if (!CliDB.EmotesStorage.ContainsKey(entry))
			{
				Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Emote entry {entry}, skipped.");

				return false;
			}

			return true;
		}

		private static bool IsItemValid(SmartScriptHolder e, uint entry)
		{
			if (!CliDB.ItemSparseStorage.ContainsKey(entry))
			{
				Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Item entry {entry}, skipped.");

				return false;
			}

			return true;
		}

		private static bool IsTextEmoteValid(SmartScriptHolder e, uint entry)
		{
			if (!CliDB.EmotesTextStorage.ContainsKey(entry))
			{
				Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent Text Emote entry {entry}, skipped.");

				return false;
			}

			return true;
		}

		private static bool IsAreaTriggerValid(SmartScriptHolder e, uint entry)
		{
			if (!CliDB.AreaTriggerStorage.ContainsKey(entry))
			{
				Log.outError(LogFilter.ScriptsAi, $"SmartAIMgr: {e} uses non-existent AreaTrigger entry {entry}, skipped.");

				return false;
			}

			return true;
		}

		private static bool IsSoundValid(SmartScriptHolder e, uint entry)
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
				if (entry > 0) //first search is for Guid (negative), do not drop error if not found
					Log.outDebug(LogFilter.ScriptsAi, "SmartAIMgr.GetScript: Could not load Script for Entry {0} ScriptType {1}.", entry, type);
			}

			return temp;
		}

		public WaypointPath GetPath(uint id)
		{
			return _waypointStore.LookupByKey(id);
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

		public static uint GetTypeMask(SmartScriptType smartScriptType)
		{
			return smartScriptType switch
			       {
				       SmartScriptType.Creature                    => SmartScriptTypeMaskId.Creature,
				       SmartScriptType.GameObject                  => SmartScriptTypeMaskId.Gameobject,
				       SmartScriptType.AreaTrigger                 => SmartScriptTypeMaskId.Areatrigger,
				       SmartScriptType.Event                       => SmartScriptTypeMaskId.Event,
				       SmartScriptType.Gossip                      => SmartScriptTypeMaskId.Gossip,
				       SmartScriptType.Quest                       => SmartScriptTypeMaskId.Quest,
				       SmartScriptType.Spell                       => SmartScriptTypeMaskId.Spell,
				       SmartScriptType.Transport                   => SmartScriptTypeMaskId.Transport,
				       SmartScriptType.Instance                    => SmartScriptTypeMaskId.Instance,
				       SmartScriptType.TimedActionlist             => SmartScriptTypeMaskId.TimedActionlist,
				       SmartScriptType.Scene                       => SmartScriptTypeMaskId.Scene,
				       SmartScriptType.AreaTriggerEntity           => SmartScriptTypeMaskId.AreatrigggerEntity,
				       SmartScriptType.AreaTriggerEntityServerside => SmartScriptTypeMaskId.AreatrigggerEntity,
				       _                                           => 0
			       };
		}

		public static uint GetEventMask(SmartEvents smartEvent)
		{
			return smartEvent switch
			       {
				       SmartEvents.UpdateIc              => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.TimedActionlist,
				       SmartEvents.UpdateOoc             => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject + SmartScriptTypeMaskId.Instance + SmartScriptTypeMaskId.AreatrigggerEntity,
				       SmartEvents.HealthPct             => SmartScriptTypeMaskId.Creature,
				       SmartEvents.ManaPct               => SmartScriptTypeMaskId.Creature,
				       SmartEvents.Aggro                 => SmartScriptTypeMaskId.Creature,
				       SmartEvents.Kill                  => SmartScriptTypeMaskId.Creature,
				       SmartEvents.Death                 => SmartScriptTypeMaskId.Creature,
				       SmartEvents.Evade                 => SmartScriptTypeMaskId.Creature,
				       SmartEvents.SpellHit              => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.Range                 => SmartScriptTypeMaskId.Creature,
				       SmartEvents.OocLos                => SmartScriptTypeMaskId.Creature,
				       SmartEvents.Respawn               => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.TargetHealthPct       => SmartScriptTypeMaskId.Creature,
				       SmartEvents.VictimCasting         => SmartScriptTypeMaskId.Creature,
				       SmartEvents.FriendlyHealth        => SmartScriptTypeMaskId.Creature,
				       SmartEvents.FriendlyIsCc          => SmartScriptTypeMaskId.Creature,
				       SmartEvents.FriendlyMissingBuff   => SmartScriptTypeMaskId.Creature,
				       SmartEvents.SummonedUnit          => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.TargetManaPct         => SmartScriptTypeMaskId.Creature,
				       SmartEvents.AcceptedQuest         => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.RewardQuest           => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.ReachedHome           => SmartScriptTypeMaskId.Creature,
				       SmartEvents.ReceiveEmote          => SmartScriptTypeMaskId.Creature,
				       SmartEvents.HasAura               => SmartScriptTypeMaskId.Creature,
				       SmartEvents.TargetBuffed          => SmartScriptTypeMaskId.Creature,
				       SmartEvents.Reset                 => SmartScriptTypeMaskId.Creature,
				       SmartEvents.IcLos                 => SmartScriptTypeMaskId.Creature,
				       SmartEvents.PassengerBoarded      => SmartScriptTypeMaskId.Creature,
				       SmartEvents.PassengerRemoved      => SmartScriptTypeMaskId.Creature,
				       SmartEvents.Charmed               => SmartScriptTypeMaskId.Creature,
				       SmartEvents.CharmedTarget         => SmartScriptTypeMaskId.Creature,
				       SmartEvents.SpellHitTarget        => SmartScriptTypeMaskId.Creature,
				       SmartEvents.Damaged               => SmartScriptTypeMaskId.Creature,
				       SmartEvents.DamagedTarget         => SmartScriptTypeMaskId.Creature,
				       SmartEvents.Movementinform        => SmartScriptTypeMaskId.Creature,
				       SmartEvents.SummonDespawned       => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.CorpseRemoved         => SmartScriptTypeMaskId.Creature,
				       SmartEvents.AiInit                => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.DataSet               => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.WaypointStart         => SmartScriptTypeMaskId.Creature,
				       SmartEvents.WaypointReached       => SmartScriptTypeMaskId.Creature,
				       SmartEvents.TransportAddplayer    => SmartScriptTypeMaskId.Transport,
				       SmartEvents.TransportAddcreature  => SmartScriptTypeMaskId.Transport,
				       SmartEvents.TransportRemovePlayer => SmartScriptTypeMaskId.Transport,
				       SmartEvents.TransportRelocate     => SmartScriptTypeMaskId.Transport,
				       SmartEvents.InstancePlayerEnter   => SmartScriptTypeMaskId.Instance,
				       SmartEvents.AreatriggerOntrigger  => SmartScriptTypeMaskId.Areatrigger + SmartScriptTypeMaskId.AreatrigggerEntity,
				       SmartEvents.QuestAccepted         => SmartScriptTypeMaskId.Quest,
				       SmartEvents.QuestObjCompletion    => SmartScriptTypeMaskId.Quest,
				       SmartEvents.QuestRewarded         => SmartScriptTypeMaskId.Quest,
				       SmartEvents.QuestCompletion       => SmartScriptTypeMaskId.Quest,
				       SmartEvents.QuestFail             => SmartScriptTypeMaskId.Quest,
				       SmartEvents.TextOver              => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.ReceiveHeal           => SmartScriptTypeMaskId.Creature,
				       SmartEvents.JustSummoned          => SmartScriptTypeMaskId.Creature,
				       SmartEvents.WaypointPaused        => SmartScriptTypeMaskId.Creature,
				       SmartEvents.WaypointResumed       => SmartScriptTypeMaskId.Creature,
				       SmartEvents.WaypointStopped       => SmartScriptTypeMaskId.Creature,
				       SmartEvents.WaypointEnded         => SmartScriptTypeMaskId.Creature,
				       SmartEvents.TimedEventTriggered   => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.Update                => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject + SmartScriptTypeMaskId.AreatrigggerEntity,
				       SmartEvents.Link                  => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject + SmartScriptTypeMaskId.Areatrigger + SmartScriptTypeMaskId.Event + SmartScriptTypeMaskId.Gossip + SmartScriptTypeMaskId.Quest + SmartScriptTypeMaskId.Spell + SmartScriptTypeMaskId.Transport + SmartScriptTypeMaskId.Instance + SmartScriptTypeMaskId.AreatrigggerEntity,
				       SmartEvents.GossipSelect          => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.JustCreated           => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.GossipHello           => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.FollowCompleted       => SmartScriptTypeMaskId.Creature,
				       SmartEvents.PhaseChange           => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.IsBehindTarget        => SmartScriptTypeMaskId.Creature,
				       SmartEvents.GameEventStart        => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.GameEventEnd          => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.GoLootStateChanged    => SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.GoEventInform         => SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.ActionDone            => SmartScriptTypeMaskId.Creature,
				       SmartEvents.OnSpellclick          => SmartScriptTypeMaskId.Creature,
				       SmartEvents.FriendlyHealthPCT     => SmartScriptTypeMaskId.Creature,
				       SmartEvents.DistanceCreature      => SmartScriptTypeMaskId.Creature,
				       SmartEvents.DistanceGameobject    => SmartScriptTypeMaskId.Creature,
				       SmartEvents.CounterSet            => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.SceneStart            => SmartScriptTypeMaskId.Scene,
				       SmartEvents.SceneTrigger          => SmartScriptTypeMaskId.Scene,
				       SmartEvents.SceneCancel           => SmartScriptTypeMaskId.Scene,
				       SmartEvents.SceneComplete         => SmartScriptTypeMaskId.Scene,
				       SmartEvents.SummonedUnitDies      => SmartScriptTypeMaskId.Creature + SmartScriptTypeMaskId.Gameobject,
				       SmartEvents.OnSpellCast           => SmartScriptTypeMaskId.Creature,
				       SmartEvents.OnSpellFailed         => SmartScriptTypeMaskId.Creature,
				       SmartEvents.OnSpellStart          => SmartScriptTypeMaskId.Creature,
				       SmartEvents.OnDespawn             => SmartScriptTypeMaskId.Creature,
				       _                                 => 0
			       };
		}

		public static void TC_SAI_IS_BOOLEAN_VALID(SmartScriptHolder e, uint value, [CallerArgumentExpression("value")] string valueName = null)
		{
			if (value > 1)
				Log.outError(LogFilter.Sql, $"SmartAIMgr: {e} uses param {valueName} of Type Boolean with value {value}, valid values are 0 or 1, skipped.");
		}
	}
}