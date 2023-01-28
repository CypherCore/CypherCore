// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Game.DataStorage;
using Game.Maps;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.ITransport;

namespace Game.Entities
{
	public interface ITransport
	{
		ObjectGuid GetTransportGUID();

		// This method transforms supplied Transport offsets into global coordinates
		void CalculatePassengerPosition(ref float x, ref float y, ref float z, ref float o);

		// This method transforms supplied global coordinates into local offsets
		void CalculatePassengerOffset(ref float x, ref float y, ref float z, ref float o);

		float GetTransportOrientation();

		void AddPassenger(WorldObject passenger);

		ITransport RemovePassenger(WorldObject passenger);

		public static void UpdatePassengerPosition(ITransport transport, Map map, WorldObject passenger, float x, float y, float z, float o, bool setHomePosition)
		{
			// Transport teleported but passenger not yet (can happen for players)
			if (passenger.GetMap() != map)
				return;

			// Do not use Unit::UpdatePosition here, we don't want to remove Auras
			// as if regular movement occurred
			switch (passenger.GetTypeId())
			{
				case TypeId.Unit:
				{
					Creature creature = passenger.ToCreature();
					map.CreatureRelocation(creature, x, y, z, o, false);

					if (setHomePosition)
					{
						creature.GetTransportHomePosition(out x, out y, out z, out o);
						transport.CalculatePassengerPosition(ref x, ref y, ref z, ref o);
						creature.SetHomePosition(x, y, z, o);
					}

					break;
				}
				case TypeId.Player:
					//relocate only passengers in world and skip any player that might be still logging in/teleporting
					if (passenger.IsInWorld &&
					    !passenger.ToPlayer().IsBeingTeleported())
					{
						map.PlayerRelocation(passenger.ToPlayer(), x, y, z, o);
						passenger.ToPlayer().SetFallInformation(0, passenger.GetPositionZ());
					}

					break;
				case TypeId.GameObject:
					map.GameObjectRelocation(passenger.ToGameObject(), x, y, z, o, false);
					passenger.ToGameObject().RelocateStationaryPosition(x, y, z, o);

					break;
				case TypeId.DynamicObject:
					map.DynamicObjectRelocation(passenger.ToDynamicObject(), x, y, z, o);

					break;
				case TypeId.AreaTrigger:
					map.AreaTriggerRelocation(passenger.ToAreaTrigger(), x, y, z, o);

					break;
				default:
					break;
			}

			Unit unit = passenger.ToUnit();

			if (unit != null)
			{
				Vehicle vehicle = unit.GetVehicleKit();

				if (vehicle != null)
					vehicle.RelocatePassengers();
			}
		}

		static void CalculatePassengerPosition(ref float x, ref float y, ref float z, ref float o, float transX, float transY, float transZ, float transO)
		{
			float inx = x, iny = y, inz = z;
			o = Position.NormalizeOrientation(transO + o);

			x = transX + inx * MathF.Cos(transO) - iny * MathF.Sin(transO);
			y = transY + iny * MathF.Cos(transO) + inx * MathF.Sin(transO);
			z = transZ + inz;
		}

		static void CalculatePassengerOffset(ref float x, ref float y, ref float z, ref float o, float transX, float transY, float transZ, float transO)
		{
			o = Position.NormalizeOrientation(o - transO);

			z -= transZ;
			y -= transY; // y = searchedY * std::cos(o) + searchedX * std::sin(o)
			x -= transX; // x = searchedX * std::cos(o) + searchedY * std::sin(o + pi)
			float inx = x, iny = y;
			y = (iny - inx * MathF.Tan(transO)) / (MathF.Cos(transO) + MathF.Sin(transO) * MathF.Tan(transO));
			x = (inx + iny * MathF.Tan(transO)) / (MathF.Cos(transO) + MathF.Sin(transO) * MathF.Tan(transO));
		}

		int GetMapIdForSpawning();
	}

	public class Transport : GameObject, ITransport
	{
		private int _currentPathLeg;

		private bool _delayedAddModel;
		private BitArray _eventsToTrigger;

		private TransportMovementState _movementState;

		private HashSet<WorldObject> _passengers = new();
		private uint _pathProgress;
		private TimeTracker _positionChangeTimer = new();
		private uint? _requestStopTimestamp;
		private HashSet<WorldObject> _staticPassengers = new();

		private TransportTemplate _transportInfo;

		public Transport()
		{
			UpdateFlag.ServerTime = true;
			UpdateFlag.Stationary = true;
			UpdateFlag.Rotation   = true;
		}

		public void AddPassenger(WorldObject passenger)
		{
			if (!IsInWorld)
				return;

			if (_passengers.Add(passenger))
			{
				passenger.SetTransport(this);
				passenger.MovementInfo.Transport.Guid = GetGUID();

				Player player = passenger.ToPlayer();

				if (player)
					Global.ScriptMgr.RunScript<ITransportOnAddPassenger>(p => p.OnAddPassenger(this, player), GetScriptId());
			}
		}

		public ITransport RemovePassenger(WorldObject passenger)
		{
			if (_passengers.Remove(passenger) ||
			    _staticPassengers.Remove(passenger)) // static passenger can remove itself in case of grid unload
			{
				passenger.SetTransport(null);
				passenger.MovementInfo.Transport.Reset();
				Log.outDebug(LogFilter.Transport, "Object {0} removed from Transport {1}.", passenger.GetName(), GetName());

				Player plr = passenger.ToPlayer();

				if (plr != null)
				{
					Global.ScriptMgr.RunScript<ITransportOnRemovePassenger>(p => p.OnRemovePassenger(this, plr), GetScriptId());
					plr.SetFallInformation(0, plr.GetPositionZ());
				}
			}

			return this;
		}

		public void CalculatePassengerPosition(ref float x, ref float y, ref float z, ref float o)
		{
			ITransport.CalculatePassengerPosition(ref x, ref y, ref z, ref o, GetPositionX(), GetPositionY(), GetPositionZ(), GetTransportOrientation());
		}

		public void CalculatePassengerOffset(ref float x, ref float y, ref float z, ref float o)
		{
			ITransport.CalculatePassengerOffset(ref x, ref y, ref z, ref o, GetPositionX(), GetPositionY(), GetPositionZ(), GetTransportOrientation());
		}

		public int GetMapIdForSpawning()
		{
			return GetGoInfo().MoTransport.SpawnMap;
		}

		public ObjectGuid GetTransportGUID()
		{
			return GetGUID();
		}

		public float GetTransportOrientation()
		{
			return GetOrientation();
		}

		public override void Dispose()
		{
			Cypher.Assert(_passengers.Empty());
			UnloadStaticPassengers();
			base.Dispose();
		}

		public bool Create(ulong guidlow, uint entry, float x, float y, float z, float ang)
		{
			Relocate(x, y, z, ang);

			if (!IsPositionValid())
			{
				Log.outError(LogFilter.Transport, $"Transport (GUID: {guidlow}) not created. Suggested coordinates isn't valid (X: {x} Y: {y})");

				return false;
			}

			_Create(ObjectGuid.Create(HighGuid.Transport, guidlow));

			GameObjectTemplate goinfo = Global.ObjectMgr.GetGameObjectTemplate(entry);

			if (goinfo == null)
			{
				Log.outError(LogFilter.Sql, $"Transport not created: entry in `gameobject_template` not found, entry: {entry}");

				return false;
			}

			_goInfo          = goinfo;
			_goTemplateAddon = Global.ObjectMgr.GetGameObjectTemplateAddon(entry);

			TransportTemplate tInfo = Global.TransportMgr.GetTransportTemplate(entry);

			if (tInfo == null)
			{
				Log.outError(LogFilter.Sql, "Transport {0} (Name: {1}) will not be created, missing `transport_template` entry.", entry, goinfo.name);

				return false;
			}

			_transportInfo   = tInfo;
			_eventsToTrigger = new BitArray(tInfo.Events.Count, true);

			GameObjectOverride goOverride = GetGameObjectOverride();

			if (goOverride != null)
			{
				SetFaction(goOverride.Faction);
				ReplaceAllFlags(goOverride.Flags);
			}

			_pathProgress = goinfo.MoTransport.allowstopping == 0 ? Time.GetMSTime() /*might be called before world update loop begins, don't use GameTime*/ % tInfo.TotalPathTime : 0;
			SetPathProgressForClient((float)_pathProgress / (float)tInfo.TotalPathTime);
			SetObjectScale(goinfo.size);
			SetPeriod(tInfo.TotalPathTime);
			SetEntry(goinfo.entry);
			SetDisplayId(goinfo.displayId);
			SetGoState(goinfo.MoTransport.allowstopping == 0 ? GameObjectState.Ready : GameObjectState.Active);
			SetGoType(GameObjectTypes.MapObjTransport);
			SetGoAnimProgress(255);
			SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.SpawnTrackingStateAnimID), Global.DB2Mgr.GetEmptyAnimStateID());
			SetName(goinfo.name);
			SetLocalRotation(0.0f, 0.0f, 0.0f, 1.0f);
			SetParentRotation(Quaternion.Identity);

			int legIndex;
			var position = _transportInfo.ComputePosition(_pathProgress, out _, out legIndex);

			if (position != null)
			{
				Relocate(position.GetPositionX(), position.GetPositionY(), position.GetPositionZ(), position.GetOrientation());
				_currentPathLeg = legIndex;
			}

			CreateModel();

			return true;
		}

		public override void CleanupsBeforeDelete(bool finalCleanup)
		{
			UnloadStaticPassengers();

			while (!_passengers.Empty())
			{
				WorldObject obj = _passengers.FirstOrDefault();
				RemovePassenger(obj);
			}

			base.CleanupsBeforeDelete(finalCleanup);
		}

		public override void Update(uint diff)
		{
			TimeSpan positionUpdateDelay = TimeSpan.FromMilliseconds(200);

			if (GetAI() != null)
				GetAI().UpdateAI(diff);
			else if (!AIM_Initialize())
				Log.outError(LogFilter.Transport, "Could not initialize GameObjectAI for Transport");

			Global.ScriptMgr.RunScript<ITransportOnUpdate>(p => p.OnUpdate(this, diff), GetScriptId());

			_positionChangeTimer.Update(diff);

			uint cycleId = _pathProgress / GetTransportPeriod();

			if (GetGoInfo().MoTransport.allowstopping == 0)
				_pathProgress = GameTime.GetGameTimeMS();
			else if (!_requestStopTimestamp.HasValue ||
			         _requestStopTimestamp > _pathProgress + diff)
				_pathProgress += diff;
			else
				_pathProgress = _requestStopTimestamp.Value;

			if (_pathProgress / GetTransportPeriod() != cycleId)
				// reset cycle
				_eventsToTrigger.SetAll(true);

			SetPathProgressForClient((float)_pathProgress / (float)GetTransportPeriod());

			uint timer = _pathProgress % GetTransportPeriod();

			int eventToTriggerIndex = -1;

			for (var i = 0; i < _eventsToTrigger.Count; i++)
				if (_eventsToTrigger.Get(i))
				{
					eventToTriggerIndex = i;

					break;
				}

			if (eventToTriggerIndex != -1)
				while (eventToTriggerIndex < _transportInfo.Events.Count && _transportInfo.Events[eventToTriggerIndex].Timestamp < timer)
				{
					TransportPathLeg leg = _transportInfo.GetLegForTime(_transportInfo.Events[eventToTriggerIndex].Timestamp);

					if (leg != null)
						if (leg.MapId == GetMapId())
							GameEvents.Trigger(_transportInfo.Events[eventToTriggerIndex].EventId, this, this);

					_eventsToTrigger.Set(eventToTriggerIndex, false);
					++eventToTriggerIndex;
				}

			TransportMovementState moveState;
			int                    legIndex;
			Position               newPosition = _transportInfo.ComputePosition(timer, out moveState, out legIndex);

			if (newPosition != null)
			{
				bool justStopped = _movementState == TransportMovementState.Moving && moveState != TransportMovementState.Moving;
				_movementState = moveState;

				if (justStopped)
					if (_requestStopTimestamp != 0 &&
					    GetGoState() != GameObjectState.Ready)
					{
						SetGoState(GameObjectState.Ready);
						SetDynamicFlag(GameObjectDynamicLowFlags.Stopped);
					}

				if (legIndex != _currentPathLeg)
				{
					uint oldMapId = _transportInfo.PathLegs[_currentPathLeg].MapId;
					_currentPathLeg = legIndex;
					TeleportTransport(oldMapId, _transportInfo.PathLegs[legIndex].MapId, newPosition.GetPositionX(), newPosition.GetPositionY(), newPosition.GetPositionZ(), newPosition.GetOrientation());

					return;
				}

				// set position
				if (_positionChangeTimer.Passed() &&
				    GetExpectedMapId() == GetMapId())
				{
					_positionChangeTimer.Reset(positionUpdateDelay);

					if (_movementState == TransportMovementState.Moving || justStopped)
					{
						UpdatePosition(newPosition.GetPositionX(), newPosition.GetPositionY(), newPosition.GetPositionZ(), newPosition.GetOrientation());
					}
					else
					{
						/* There are four possible scenarios that trigger loading/unloading passengers:
						  1. Transport moves from inactive to active grid
						  2. the grid that Transport is currently in becomes active
						  3. Transport moves from active to inactive grid
						  4. the grid that Transport is currently in unloads
						*/
						bool gridActive = GetMap().IsGridLoaded(GetPositionX(), GetPositionY());

						if (_staticPassengers.Empty() && gridActive) // 2.
							LoadStaticPassengers();
						else if (!_staticPassengers.Empty() &&
						         !gridActive)
							// 4. - if transports stopped on grid edge, some passengers can remain in active grids
							//      unload all static passengers otherwise passengers won't load correctly when the grid that Transport is currently in becomes active
							UnloadStaticPassengers();
					}
				}
			}

			// Add model to map after we are fully done with moving maps
			if (_delayedAddModel)
			{
				_delayedAddModel = false;

				if (_model != null)
					GetMap().InsertGameObjectModel(_model);
			}
		}

		public Creature CreateNPCPassenger(ulong guid, CreatureData data)
		{
			Map map = GetMap();

			if (map.GetCreatureRespawnTime(guid) != 0)
				return null;

			Creature creature = Creature.CreateCreatureFromDB(guid, map, false, true);

			if (!creature)
				return null;

			float x, y, z, o;
			data.SpawnPoint.GetPosition(out x, out y, out z, out o);

			creature.SetTransport(this);
			creature.MovementInfo.Transport.Guid = GetGUID();
			creature.MovementInfo.Transport.Pos.Relocate(x, y, z, o);
			creature.MovementInfo.Transport.Seat = -1;
			CalculatePassengerPosition(ref x, ref y, ref z, ref o);
			creature.Relocate(x, y, z, o);
			creature.SetHomePosition(creature.GetPositionX(), creature.GetPositionY(), creature.GetPositionZ(), creature.GetOrientation());
			creature.SetTransportHomePosition(creature.MovementInfo.Transport.Pos);

			// @HACK - Transport models are not added to map's dynamic LoS calculations
			//         because the current GameObjectModel cannot be moved without recreating
			creature.AddUnitState(UnitState.IgnorePathfinding);

			if (!creature.IsPositionValid())
			{
				Log.outError(LogFilter.Transport, "Creature (guidlow {0}, entry {1}) not created. Suggested coordinates aren't valid (X: {2} Y: {3})", creature.GetGUID().ToString(), creature.GetEntry(), creature.GetPositionX(), creature.GetPositionY());

				return null;
			}

			PhasingHandler.InitDbPhaseShift(creature.GetPhaseShift(), data.PhaseUseFlags, data.PhaseId, data.PhaseGroup);
			PhasingHandler.InitDbVisibleMapId(creature.GetPhaseShift(), data.terrainSwapMap);

			if (!map.AddToMap(creature))
				return null;

			_staticPassengers.Add(creature);
			Global.ScriptMgr.RunScript<ITransportOnAddCreaturePassenger>(p => p.OnAddCreaturePassenger(this, creature), GetScriptId());

			return creature;
		}

		private GameObject CreateGOPassenger(ulong guid, GameObjectData data)
		{
			Map map = GetMap();

			if (map.GetGORespawnTime(guid) != 0)
				return null;

			GameObject go = CreateGameObjectFromDB(guid, map, false);

			if (!go)
				return null;

			float x, y, z, o;
			data.SpawnPoint.GetPosition(out x, out y, out z, out o);

			go.SetTransport(this);
			go.MovementInfo.Transport.Guid = GetGUID();
			go.MovementInfo.Transport.Pos.Relocate(x, y, z, o);
			go.MovementInfo.Transport.Seat = -1;
			CalculatePassengerPosition(ref x, ref y, ref z, ref o);
			go.Relocate(x, y, z, o);
			go.RelocateStationaryPosition(x, y, z, o);

			if (!go.IsPositionValid())
			{
				Log.outError(LogFilter.Transport, "GameObject (guidlow {0}, entry {1}) not created. Suggested coordinates aren't valid (X: {2} Y: {3})", go.GetGUID().ToString(), go.GetEntry(), go.GetPositionX(), go.GetPositionY());

				return null;
			}

			PhasingHandler.InitDbPhaseShift(go.GetPhaseShift(), data.PhaseUseFlags, data.PhaseId, data.PhaseGroup);
			PhasingHandler.InitDbVisibleMapId(go.GetPhaseShift(), data.terrainSwapMap);

			if (!map.AddToMap(go))
				return null;

			_staticPassengers.Add(go);

			return go;
		}

		public TempSummon SummonPassenger(uint entry, Position pos, TempSummonType summonType, SummonPropertiesRecord properties = null, uint duration = 0, Unit summoner = null, uint spellId = 0, uint vehId = 0)
		{
			Map map = GetMap();

			if (map == null)
				return null;

			UnitTypeMask mask = UnitTypeMask.Summon;

			if (properties != null)
				switch (properties.Control)
				{
					case SummonCategory.Pet:
						mask = UnitTypeMask.Guardian;

						break;
					case SummonCategory.Puppet:
						mask = UnitTypeMask.Puppet;

						break;
					case SummonCategory.Vehicle:
						mask = UnitTypeMask.Minion;

						break;
					case SummonCategory.Wild:
					case SummonCategory.Ally:
					case SummonCategory.Unk:
					{
						switch (properties.Title)
						{
							case SummonTitle.Minion:
							case SummonTitle.Guardian:
							case SummonTitle.Runeblade:
								mask = UnitTypeMask.Guardian;

								break;
							case SummonTitle.Totem:
							case SummonTitle.LightWell:
								mask = UnitTypeMask.Totem;

								break;
							case SummonTitle.Vehicle:
							case SummonTitle.Mount:
								mask = UnitTypeMask.Summon;

								break;
							case SummonTitle.Companion:
								mask = UnitTypeMask.Minion;

								break;
							default:
								if (properties.GetFlags().HasFlag(SummonPropertiesFlags.JoinSummonerSpawnGroup)) // Mirror Image, Summon Gargoyle
									mask = UnitTypeMask.Guardian;

								break;
						}

						break;
					}
					default:
						return null;
				}

			TempSummon summon = null;

			switch (mask)
			{
				case UnitTypeMask.Summon:
					summon = new TempSummon(properties, summoner, false);

					break;
				case UnitTypeMask.Guardian:
					summon = new Guardian(properties, summoner, false);

					break;
				case UnitTypeMask.Puppet:
					summon = new Puppet(properties, summoner);

					break;
				case UnitTypeMask.Totem:
					summon = new Totem(properties, summoner);

					break;
				case UnitTypeMask.Minion:
					summon = new Minion(properties, summoner, false);

					break;
			}

			float x, y, z, o;
			pos.GetPosition(out x, out y, out z, out o);
			CalculatePassengerPosition(ref x, ref y, ref z, ref o);

			if (!summon.Create(map.GenerateLowGuid(HighGuid.Creature), map, entry, new Position(x, y, z, o), null, vehId))
				return null;

			WorldObject phaseShiftOwner = this;

			if (summoner != null &&
			    !(properties != null && properties.GetFlags().HasFlag(SummonPropertiesFlags.IgnoreSummonerPhase)))
				phaseShiftOwner = summoner;

			if (phaseShiftOwner != null)
				PhasingHandler.InheritPhaseShift(summon, phaseShiftOwner);

			summon.SetCreatedBySpell(spellId);

			summon.SetTransport(this);
			summon.MovementInfo.Transport.Guid = GetGUID();
			summon.MovementInfo.Transport.Pos.Relocate(pos);
			summon.Relocate(x, y, z, o);
			summon.SetHomePosition(x, y, z, o);
			summon.SetTransportHomePosition(pos);

			// @HACK - Transport models are not added to map's dynamic LoS calculations
			//         because the current GameObjectModel cannot be moved without recreating
			summon.AddUnitState(UnitState.IgnorePathfinding);

			summon.InitStats(duration);

			if (!map.AddToMap(summon))
				return null;

			_staticPassengers.Add(summon);

			summon.InitSummon();
			summon.SetTempSummonType(summonType);

			return summon;
		}

		public void UpdatePosition(float x, float y, float z, float o)
		{
			Global.ScriptMgr.RunScript<ITransportOnRelocate>(p => p.OnRelocate(this, GetMapId(), x, y, z), GetScriptId());

			bool newActive = GetMap().IsGridLoaded(x, y);
			Cell oldCell   = new(GetPositionX(), GetPositionY());

			Relocate(x, y, z, o);
			StationaryPosition.SetOrientation(o);
			UpdateModelPosition();

			UpdatePassengerPositions(_passengers);

			/* There are four possible scenarios that trigger loading/unloading passengers:
			 1. Transport moves from inactive to active grid
			 2. the grid that Transport is currently in becomes active
			 3. Transport moves from active to inactive grid
			 4. the grid that Transport is currently in unloads
			 */
			if (_staticPassengers.Empty() && newActive) // 1. and 2.
				LoadStaticPassengers();
			else if (!_staticPassengers.Empty() &&
			         !newActive &&
			         oldCell.DiffGrid(new Cell(GetPositionX(), GetPositionY()))) // 3.
				UnloadStaticPassengers();
			else
				UpdatePassengerPositions(_staticPassengers);
			// 4. is handed by grid unload
		}

		private void LoadStaticPassengers()
		{
			uint mapId = (uint)GetGoInfo().MoTransport.SpawnMap;
			var  cells = Global.ObjectMgr.GetMapObjectGuids(mapId, GetMap().GetDifficultyID());

			if (cells == null)
				return;

			foreach (var cell in cells)
			{
				// Creatures on Transport
				foreach (var npc in cell.Value.creatures)
					CreateNPCPassenger(npc, Global.ObjectMgr.GetCreatureData(npc));

				// GameObjects on Transport
				foreach (var go in cell.Value.gameobjects)
					CreateGOPassenger(go, Global.ObjectMgr.GetGameObjectData(go));
			}
		}

		private void UnloadStaticPassengers()
		{
			while (!_staticPassengers.Empty())
			{
				WorldObject obj = _staticPassengers.First();
				obj.AddObjectToRemoveList(); // also removes from _staticPassengers
			}
		}

		public void EnableMovement(bool enabled)
		{
			if (GetGoInfo().MoTransport.allowstopping == 0)
				return;

			if (!enabled)
			{
				_requestStopTimestamp = (_pathProgress / GetTransportPeriod()) * GetTransportPeriod() + _transportInfo.GetNextPauseWaypointTimestamp(_pathProgress);
			}
			else
			{
				_requestStopTimestamp = null;
				SetGoState(GameObjectState.Active);
				RemoveDynamicFlag(GameObjectDynamicLowFlags.Stopped);
			}
		}

		public void SetDelayedAddModelToMap()
		{
			_delayedAddModel = true;
		}

		private bool TeleportTransport(uint oldMapId, uint newMapId, float x, float y, float z, float o)
		{
			if (oldMapId != newMapId)
			{
				UnloadStaticPassengers();
				TeleportPassengersAndHideTransport(newMapId, x, y, z, o);

				return true;
			}
			else
			{
				UpdatePosition(x, y, z, o);

				// Teleport players, they need to know it
				foreach (var obj in _passengers)
					if (obj.IsTypeId(TypeId.Player))
					{
						// will be relocated in UpdatePosition of the vehicle
						Unit veh = obj.ToUnit().GetVehicleBase();

						if (veh)
							if (veh.GetTransport() == this)
								continue;

						float destX, destY, destZ, destO;
						obj.MovementInfo.Transport.Pos.GetPosition(out destX, out destY, out destZ, out destO);
						ITransport.CalculatePassengerPosition(ref destX, ref destY, ref destZ, ref destO, x, y, z, o);

						obj.ToUnit().NearTeleportTo(destX, destY, destZ, destO);
					}

				return false;
			}
		}

		private void TeleportPassengersAndHideTransport(uint newMapid, float x, float y, float z, float o)
		{
			if (newMapid == GetMapId())
			{
				AddToWorld();

				foreach (var player in GetMap().GetPlayers())
					if (player.GetTransport() != this &&
					    player.InSamePhase(this))
					{
						UpdateData data = new(GetMap().GetId());
						BuildCreateUpdateBlockForPlayer(data, player);
						player.VisibleTransports.Add(GetGUID());
						data.BuildPacket(out UpdateObject packet);
						player.SendPacket(packet);
					}
			}
			else
			{
				UpdateData data = new(GetMap().GetId());
				BuildOutOfRangeUpdateBlock(data);

				data.BuildPacket(out UpdateObject packet);

				foreach (var player in GetMap().GetPlayers())
					if (player.GetTransport() != this &&
					    player.VisibleTransports.Contains(GetGUID()))
					{
						player.SendPacket(packet);
						player.VisibleTransports.Remove(GetGUID());
					}

				RemoveFromWorld();
			}

			List<WorldObject> passengersToTeleport = new(_passengers);

			foreach (WorldObject obj in passengersToTeleport)
			{
				float destX, destY, destZ, destO;
				obj.MovementInfo.Transport.Pos.GetPosition(out destX, out destY, out destZ, out destO);
				ITransport.CalculatePassengerPosition(ref destX, ref destY, ref destZ, ref destO, x, y, z, o);

				switch (obj.GetTypeId())
				{
					case TypeId.Player:
						if (!obj.ToPlayer().TeleportTo(newMapid, destX, destY, destZ, destO, TeleportToOptions.NotLeaveTransport))
							RemovePassenger(obj);

						break;
					case TypeId.DynamicObject:
					case TypeId.AreaTrigger:
						obj.AddObjectToRemoveList();

						break;
					default:
						RemovePassenger(obj);

						break;
				}
			}
		}

		private void UpdatePassengerPositions(HashSet<WorldObject> passengers)
		{
			foreach (WorldObject passenger in passengers)
			{
				float x, y, z, o;
				passenger.MovementInfo.Transport.Pos.GetPosition(out x, out y, out z, out o);
				CalculatePassengerPosition(ref x, ref y, ref z, ref o);
				ITransport.UpdatePassengerPosition(this, GetMap(), passenger, x, y, z, o, true);
			}
		}

		public override void BuildUpdate(Dictionary<Player, UpdateData> data_map)
		{
			var players = GetMap().GetPlayers();

			if (players.Empty())
				return;

			foreach (var playerReference in players)
				if (playerReference.InSamePhase(this))
					BuildFieldsUpdate(playerReference, data_map);

			ClearUpdateMask(true);
		}

		public uint GetExpectedMapId()
		{
			return _transportInfo.PathLegs[_currentPathLeg].MapId;
		}

		public HashSet<WorldObject> GetPassengers()
		{
			return _passengers;
		}

		public uint GetTransportPeriod()
		{
			return _gameObjectData.Level;
		}

		public void SetPeriod(uint period)
		{
			SetLevel(period);
		}

		public uint GetTimer()
		{
			return _pathProgress;
		}
	}
}