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
using Framework.GameMath;
using Game.DataStorage;
using Game.Maps;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public interface ITransport
    {
        // This method transforms supplied transport offsets into global coordinates
        void CalculatePassengerPosition(ref float x, ref float y, ref float z, ref float o);

        // This method transforms supplied global coordinates into local offsets
        void CalculatePassengerOffset(ref float x, ref float y, ref float z, ref float o);
    }

    public class TransportPosHelper
    {
        public static void CalculatePassengerPosition(ref float x, ref float y, ref float z, ref float o, float transX, float transY, float transZ, float transO)
        {
            float inx = x, iny = y, inz = z;
            o = Position.NormalizeOrientation(transO + o);

            x = transX + inx * (float)Math.Cos(transO) - iny * (float)Math.Sin(transO);
            y = transY + iny * (float)Math.Cos(transO) + inx * (float)Math.Sin(transO);
            z = transZ + inz;
        }

        public static void CalculatePassengerOffset(ref float x, ref float y, ref float z, ref float o, float transX, float transY, float transZ, float transO)
        {
            o = Position.NormalizeOrientation(o - transO);

            z -= transZ;
            y -= transY;
            x -= transX;

            float inx = x, iny = y;
            y = (iny - inx * (float)Math.Tan(transO)) / ((float)Math.Cos(transO) + (float)Math.Sin(transO) * (float)Math.Tan(transO));
            x = (inx + iny * (float)Math.Tan(transO)) / ((float)Math.Cos(transO) + (float)Math.Sin(transO) * (float)Math.Tan(transO));
        }
    }

    public class Transport : GameObject, ITransport
    {
        public Transport()
        {
            _isMoving = true;

            m_updateFlag.ServerTime = true;
            m_updateFlag.Stationary = true;
            m_updateFlag.Rotation = true;
        }

        public override void Dispose()
        {
            Cypher.Assert(_passengers.Empty());
            UnloadStaticPassengers();
            base.Dispose();
        }

        public bool Create(ulong guidlow, uint entry, uint mapid, float x, float y, float z, float ang, uint animprogress)
        {
            Relocate(x, y, z, ang);

            if (!IsPositionValid())
            {
                Log.outError(LogFilter.Transport, "Transport (GUID: {0}) not created. Suggested coordinates isn't valid (X: {1} Y: {2})",
                    guidlow, x, y);
                return false;
            }

            _Create(ObjectGuid.Create(HighGuid.Transport, guidlow));

            GameObjectTemplate goinfo = Global.ObjectMgr.GetGameObjectTemplate(entry);

            if (goinfo == null)
            {
                Log.outError(LogFilter.Sql, "Transport not created: entry in `gameobject_template` not found, guidlow: {0} map: {1}  (X: {2} Y: {3} Z: {4}) ang: {5}", guidlow, mapid, x, y, z, ang);
                return false;
            }

            m_goInfo = goinfo;
            m_goTemplateAddon = Global.ObjectMgr.GetGameObjectTemplateAddon(entry);

            TransportTemplate tInfo = Global.TransportMgr.GetTransportTemplate(entry);
            if (tInfo == null)
            {
                Log.outError(LogFilter.Sql, "Transport {0} (name: {1}) will not be created, missing `transport_template` entry.", entry, goinfo.name);
                return false;
            }

            _transportInfo = tInfo;
            _nextFrame = 0;
            _currentFrame = tInfo.keyFrames[_nextFrame++];
            _triggeredArrivalEvent = false;
            _triggeredDepartureEvent = false;

            if (m_goTemplateAddon != null)
            {
                SetFaction(m_goTemplateAddon.faction);
                SetUInt32Value(GameObjectFields.Flags, m_goTemplateAddon.flags);
            }

            m_goValue.Transport.PathProgress = 0;
            SetFloatValue(ObjectFields.ScaleX, goinfo.size);
            SetPeriod(tInfo.pathTime);
            SetEntry(goinfo.entry);
            SetDisplayId(goinfo.displayId);
            SetGoState(goinfo.MoTransport.allowstopping == 0 ? GameObjectState.Ready : GameObjectState.Active);
            SetGoType(GameObjectTypes.MapObjTransport);
            SetGoAnimProgress(animprogress);
            SetName(goinfo.name);
            SetWorldRotation(0.0f, 0.0f, 0.0f, 1.0f);
            SetParentRotation(Quaternion.WAxis);

            m_model = CreateModel();
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
            int positionUpdateDelay = 200;

            if (GetAI() != null)
                GetAI().UpdateAI(diff);
            else if (!AIM_Initialize())
                Log.outError(LogFilter.Transport, "Could not initialize GameObjectAI for Transport");

            if (GetKeyFrames().Count <= 1)
                return;

            if (IsMoving() || !_pendingStop)
                m_goValue.Transport.PathProgress += diff;

            uint timer = m_goValue.Transport.PathProgress % GetTransportPeriod();
            bool justStopped = false;

            // Set current waypoint
            // Desired outcome: _currentFrame.DepartureTime < timer < _nextFrame.ArriveTime
            // ... arrive | ... delay ... | departure
            //      event /         event /
            for (; ; )
            {
                if (timer >= _currentFrame.ArriveTime)
                {
                    if (!_triggeredArrivalEvent)
                    {
                        DoEventIfAny(_currentFrame, false);
                        _triggeredArrivalEvent = true;
                    }

                    if (timer < _currentFrame.DepartureTime)
                    {
                        SetMoving(false);
                        justStopped = true;
                        if (_pendingStop && GetGoState() != GameObjectState.Ready)
                        {
                            SetGoState(GameObjectState.Ready);
                            m_goValue.Transport.PathProgress = (m_goValue.Transport.PathProgress / GetTransportPeriod());
                            m_goValue.Transport.PathProgress *= GetTransportPeriod();
                            m_goValue.Transport.PathProgress += _currentFrame.ArriveTime;
                        }
                        break;  // its a stop frame and we are waiting
                    }
                }

                if (timer >= _currentFrame.DepartureTime && !_triggeredDepartureEvent)
                {
                    DoEventIfAny(_currentFrame, true); // departure event
                    _triggeredDepartureEvent = true;
                }

                // not waiting anymore
                SetMoving(true);

                // Enable movement
                if (GetGoInfo().MoTransport.allowstopping != 0)
                    SetGoState(GameObjectState.Active);

                if (timer >= _currentFrame.DepartureTime && timer < _currentFrame.NextArriveTime)
                    break;  // found current waypoint

                MoveToNextWaypoint();

                Global.ScriptMgr.OnRelocate(this, _currentFrame.Node.NodeIndex, _currentFrame.Node.ContinentID, _currentFrame.Node.Loc.X, _currentFrame.Node.Loc.Y, _currentFrame.Node.Loc.Z);

                Log.outDebug(LogFilter.Transport, "Transport {0} ({1}) moved to node {2} {3} {4} {5} {6}", GetEntry(), GetName(), _currentFrame.Node.NodeIndex, _currentFrame.Node.ContinentID, 
                    _currentFrame.Node.Loc.X, _currentFrame.Node.Loc.Y, _currentFrame.Node.Loc.Z);

                // Departure event
                var nextframe = GetKeyFrames()[_nextFrame];
                if (_currentFrame.IsTeleportFrame())
                    if (TeleportTransport(nextframe.Node.ContinentID, nextframe.Node.Loc.X, nextframe.Node.Loc.Y, nextframe.Node.Loc.Z, nextframe.InitialOrientation))
                        return;
            }

            // Add model to map after we are fully done with moving maps
            if (_delayedAddModel)
            {
                _delayedAddModel = false;
                if (m_model != null)
                    GetMap().InsertGameObjectModel(m_model);
            }

            // Set position
            _positionChangeTimer.Update((int)diff);
            if (_positionChangeTimer.Passed())
            {
                _positionChangeTimer.Reset(positionUpdateDelay);
                if (IsMoving())
                {
                    float t = !justStopped ? CalculateSegmentPos(timer * 0.001f) : 1.0f;
                    Vector3 pos, dir;
                    _currentFrame.Spline.Evaluate_Percent((int)_currentFrame.Index, t, out pos);
                    _currentFrame.Spline.Evaluate_Derivative((int)_currentFrame.Index, t, out dir);
                    UpdatePosition(pos.X, pos.Y, pos.Z, (float)Math.Atan2(dir.Y, dir.X) + MathFunctions.PI);
                }
                else if (justStopped)
                    UpdatePosition(_currentFrame.Node.Loc.X, _currentFrame.Node.Loc.Y, _currentFrame.Node.Loc.Z, _currentFrame.InitialOrientation);
                else
                {
                    /* There are four possible scenarios that trigger loading/unloading passengers:
                     1. transport moves from inactive to active grid
                     2. the grid that transport is currently in becomes active
                     3. transport moves from active to inactive grid
                     4. the grid that transport is currently in unloads
                     */
                    bool gridActive = GetMap().IsGridLoaded(GetPositionX(), GetPositionY());

                    if (_staticPassengers.Empty() && gridActive) // 2.
                        LoadStaticPassengers();
                    else if (!_staticPassengers.Empty() && !gridActive)
                        // 4. - if transports stopped on grid edge, some passengers can remain in active grids
                        //      unload all static passengers otherwise passengers won't load correctly when the grid that transport is currently in becomes active
                        UnloadStaticPassengers();
                }
            }

            Global.ScriptMgr.OnTransportUpdate(this, diff);
        }

        public void DelayedUpdate(uint diff)
        {
            if (GetKeyFrames().Count <= 1)
                return;

            DelayedTeleportTransport();
        }

        public void AddPassenger(WorldObject passenger)
        {
            if (!IsInWorld)
                return;

            if (_passengers.Add(passenger))
            {
                passenger.SetTransport(this);
                passenger.m_movementInfo.transport.guid = GetGUID();

                Player player = passenger.ToPlayer();
                if (player)
                    Global.ScriptMgr.OnAddPassenger(this, player);
            }
        }

        public void RemovePassenger(WorldObject passenger)
        {
            bool erased = _passengers.Remove(passenger);

            if (erased || _staticPassengers.Remove(passenger))
            {
                passenger.SetTransport(null);
                passenger.m_movementInfo.transport.Reset();
                Log.outDebug(LogFilter.Transport, "Object {0} removed from transport {1}.", passenger.GetName(), GetName());

                if (passenger.IsTypeId(TypeId.Player))
                    Global.ScriptMgr.OnRemovePassenger(this, passenger.ToPlayer());
            }
        }

        public Creature CreateNPCPassenger(ulong guid, CreatureData data)
        {
            Map map = GetMap();

            Creature creature = Creature.CreateCreatureFromDB(guid, map, false);
            if (!creature)
                return null;

            float x = data.posX;
            float y = data.posY;
            float z = data.posZ;
            float o = data.orientation;

            creature.SetTransport(this);
            creature.m_movementInfo.transport.guid = GetGUID();
            creature.m_movementInfo.transport.pos.Relocate(x, y, z, o);
            creature.m_movementInfo.transport.seat = -1;
            CalculatePassengerPosition(ref x, ref y, ref z, ref o);
            creature.Relocate(x, y, z, o);
            creature.SetHomePosition(creature.GetPositionX(), creature.GetPositionY(), creature.GetPositionZ(), creature.GetOrientation());
            creature.SetTransportHomePosition(creature.m_movementInfo.transport.pos);

            // @HACK - transport models are not added to map's dynamic LoS calculations
            //         because the current GameObjectModel cannot be moved without recreating
            creature.AddUnitState(UnitState.IgnorePathfinding);

            if (!creature.IsPositionValid())
            {
                Log.outError(LogFilter.Transport, "Creature (guidlow {0}, entry {1}) not created. Suggested coordinates aren't valid (X: {2} Y: {3})", creature.GetGUID().ToString(), creature.GetEntry(), creature.GetPositionX(), creature.GetPositionY());
                return null;
            }

            PhasingHandler.InitDbPhaseShift(creature.GetPhaseShift(), data.phaseUseFlags, data.phaseId, data.phaseGroup);
            PhasingHandler.InitDbVisibleMapId(creature.GetPhaseShift(), data.terrainSwapMap);

            if (!map.AddToMap(creature))
                return null;

            _staticPassengers.Add(creature);
            Global.ScriptMgr.OnAddCreaturePassenger(this, creature);
            return creature;
        }

        GameObject CreateGOPassenger(ulong guid, GameObjectData data)
        {
            Map map = GetMap();

            GameObject go = CreateGameObjectFromDB(guid, map, false);
            if (!go)
                return null;

            float x = data.posX;
            float y = data.posY;
            float z = data.posZ;
            float o = data.orientation;

            go.SetTransport(this);
            go.m_movementInfo.transport.guid = GetGUID();
            go.m_movementInfo.transport.pos.Relocate(x, y, z, o);
            go.m_movementInfo.transport.seat = -1;
            CalculatePassengerPosition(ref x, ref y, ref z, ref o);
            go.Relocate(x, y, z, o);
            go.RelocateStationaryPosition(x, y, z, o);

            if (!go.IsPositionValid())
            {
                Log.outError(LogFilter.Transport, "GameObject (guidlow {0}, entry {1}) not created. Suggested coordinates aren't valid (X: {2} Y: {3})", go.GetGUID().ToString(), go.GetEntry(), go.GetPositionX(), go.GetPositionY());
                return null;
            }

            PhasingHandler.InitDbPhaseShift(go.GetPhaseShift(), data.phaseUseFlags, data.phaseId, data.phaseGroup);
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
            {
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
                                case SummonType.Minion:
                                case SummonType.Guardian:
                                case SummonType.Guardian2:
                                    mask = UnitTypeMask.Guardian;
                                    break;
                                case SummonType.Totem:
                                case SummonType.LightWell:
                                    mask = UnitTypeMask.Totem;
                                    break;
                                case SummonType.Vehicle:
                                case SummonType.Vehicle2:
                                    mask = UnitTypeMask.Summon;
                                    break;
                                case SummonType.Minipet:
                                    mask = UnitTypeMask.Minion;
                                    break;
                                default:
                                    if (properties.Flags.HasAnyFlag(SummonPropFlags.Unk10)) // Mirror Image, Summon Gargoyle
                                        mask = UnitTypeMask.Guardian;
                                    break;
                            }
                            break;
                        }
                    default:
                        return null;
                }
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

            if (!summon.Create(map.GenerateLowGuid(HighGuid.Creature), map, entry, x, y, z, o, null, vehId))
                return null;

            PhasingHandler.InheritPhaseShift(summon, summoner ? (WorldObject)summoner : this);

            summon.SetUInt32Value(UnitFields.CreatedBySpell, spellId);

            summon.SetTransport(this);
            summon.m_movementInfo.transport.guid = GetGUID();
            summon.m_movementInfo.transport.pos.Relocate(pos);
            summon.Relocate(x, y, z, o);
            summon.SetHomePosition(x, y, z, o);
            summon.SetTransportHomePosition(pos);

            // @HACK - transport models are not added to map's dynamic LoS calculations
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

        public void CalculatePassengerPosition(ref float x, ref float y, ref float z, ref float o)
        {
            TransportPosHelper.CalculatePassengerPosition(ref x, ref y, ref z, ref o, GetPositionX(), GetPositionY(), GetPositionZ(), GetOrientation());
        }

        public void CalculatePassengerOffset(ref float x, ref float y, ref float z, ref float o)
        {
            TransportPosHelper.CalculatePassengerOffset(ref x, ref y, ref z, ref o, GetPositionX(), GetPositionY(), GetPositionZ(), GetOrientation());
        }

        public void UpdatePosition(float x, float y, float z, float o)
        {
            bool newActive = GetMap().IsGridLoaded(x, y);
            Cell oldCell = new Cell(GetPositionX(), GetPositionY());

            Relocate(x, y, z, o);
            m_stationaryPosition.SetOrientation(o);
            UpdateModelPosition();

            UpdatePassengerPositions(_passengers);

            /* There are four possible scenarios that trigger loading/unloading passengers:
             1. transport moves from inactive to active grid
             2. the grid that transport is currently in becomes active
             3. transport moves from active to inactive grid
             4. the grid that transport is currently in unloads
             */
            if (_staticPassengers.Empty() && newActive) // 1. and 2.
                LoadStaticPassengers();
            else if (!_staticPassengers.Empty() && !newActive && oldCell.DiffGrid(new Cell(GetPositionX(), GetPositionY()))) // 3.
                UnloadStaticPassengers();
            else
                UpdatePassengerPositions(_staticPassengers);
            // 4. is handed by grid unload
        }

        void LoadStaticPassengers()
        {
            uint mapId = (uint)GetGoInfo().MoTransport.SpawnMap;
            var cells = Global.ObjectMgr.GetMapObjectGuids(mapId, (byte)GetMap().GetDifficultyID());
            if (cells == null)
                return;
            foreach (var cell in cells)
            {
                // Creatures on transport
                foreach (var npc in cell.Value.creatures)
                    CreateNPCPassenger(npc, Global.ObjectMgr.GetCreatureData(npc));

                // GameObjects on transport
                foreach (var go in cell.Value.gameobjects)
                    CreateGOPassenger(go, Global.ObjectMgr.GetGOData(go));
            }
        }

        void UnloadStaticPassengers()
        {
            while (!_staticPassengers.Empty())
            {
                WorldObject obj = _staticPassengers.First();
                obj.AddObjectToRemoveList();   // also removes from _staticPassengers
            }
        }

        public void EnableMovement(bool enabled)
        {
            if (GetGoInfo().MoTransport.allowstopping == 0)
                return;

            _pendingStop = !enabled;
        }

        public void SetDelayedAddModelToMap() { _delayedAddModel = true; }

        void MoveToNextWaypoint()
        {
            // Clear events flagging
            _triggeredArrivalEvent = false;
            _triggeredDepartureEvent = false;

            // Set frames
            _currentFrame = GetKeyFrames()[_nextFrame++];
            if (_nextFrame == GetKeyFrames().Count)
                _nextFrame = 0;
        }

        float CalculateSegmentPos(float now)
        {
            KeyFrame frame = _currentFrame;
            float speed = GetGoInfo().MoTransport.moveSpeed;
            float accel = GetGoInfo().MoTransport.accelRate;
            float timeSinceStop = frame.TimeFrom + (now - (1.0f / Time.InMilliseconds) * frame.DepartureTime);
            float timeUntilStop = frame.TimeTo - (now - (1.0f / Time.InMilliseconds) * frame.DepartureTime);
            float segmentPos, dist;
            float accelTime = _transportInfo.accelTime;
            float accelDist = _transportInfo.accelDist;
            // calculate from nearest stop, less confusing calculation...
            if (timeSinceStop < timeUntilStop)
            {
                if (timeSinceStop < accelTime)
                    dist = 0.5f * accel * timeSinceStop * timeSinceStop;
                else
                    dist = accelDist + (timeSinceStop - accelTime) * speed;
                segmentPos = dist - frame.DistSinceStop;
            }
            else
            {
                if (timeUntilStop < _transportInfo.accelTime)
                    dist = (0.5f * accel) * (timeUntilStop * timeUntilStop);
                else
                    dist = accelDist + (timeUntilStop - accelTime) * speed;
                segmentPos = frame.DistUntilStop - dist;
            }

            return segmentPos / frame.NextDistFromPrev;
        }

        bool TeleportTransport(uint newMapid, float x, float y, float z, float o)
        {
            Map oldMap = GetMap();

            if (oldMap.GetId() != newMapid)
            {
                _delayedTeleport = true;
                UnloadStaticPassengers();
                return true;
            }
            else
            {
                UpdatePosition(x, y, z, o);

                // Teleport players, they need to know it
                foreach (var obj in _passengers)
                {
                    if (obj.IsTypeId(TypeId.Player))
                    {
                        // will be relocated in UpdatePosition of the vehicle
                        Unit veh = obj.ToUnit().GetVehicleBase();
                        if (veh)
                            if (veh.GetTransport() == this)
                                continue;

                        float destX, destY, destZ, destO;
                        obj.m_movementInfo.transport.pos.GetPosition(out destX, out destY, out destZ, out destO);
                        TransportPosHelper.CalculatePassengerPosition(ref destX, ref destY, ref destZ, ref destO, x, y, z, o);

                        obj.ToUnit().NearTeleportTo(destX, destY, destZ, destO);
                    }
                }

                return false;
            }
        }

        void DelayedTeleportTransport()
        {
            if (!_delayedTeleport)
                return;

            var nextFrame = GetKeyFrames()[_nextFrame];

            _delayedTeleport = false;
            Map newMap = Global.MapMgr.CreateBaseMap(nextFrame.Node.ContinentID);
            GetMap().RemoveFromMap(this, false);
            SetMap(newMap);

            float x = nextFrame.Node.Loc.X,
                  y = nextFrame.Node.Loc.Y,
                  z = nextFrame.Node.Loc.Z,
                  o = nextFrame.InitialOrientation;

            foreach(WorldObject obj in _passengers.ToList())
            {
                float destX, destY, destZ, destO;
                obj.m_movementInfo.transport.pos.GetPosition(out destX, out destY, out destZ, out destO);
                TransportPosHelper.CalculatePassengerPosition(ref destX, ref destY, ref destZ, ref destO, x, y, z, o);

                switch (obj.GetTypeId())
                {
                    case TypeId.Player:
                        if (!obj.ToPlayer().TeleportTo(nextFrame.Node.ContinentID, destX, destY, destZ, destO, TeleportToOptions.NotLeaveTransport))
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

            Relocate(x, y, z, o);
            GetMap().AddToMap(this);
        }

        void UpdatePassengerPositions(HashSet<WorldObject> passengers)
        {
            foreach (var passenger in passengers)
            {
                // transport teleported but passenger not yet (can happen for players)
                if (passenger.GetMap() != GetMap())
                    continue;

                // if passenger is on vehicle we have to assume the vehicle is also on transport
                // and its the vehicle that will be updating its passengers
                Unit unit = passenger.ToUnit();
                if (unit)
                    if (unit.GetVehicle() != null)
                        continue;

                // Do not use Unit.UpdatePosition here, we don't want to remove auras
                // as if regular movement occurred
                float x, y, z, o;
                passenger.m_movementInfo.transport.pos.GetPosition(out x, out y, out z, out o);
                CalculatePassengerPosition(ref x, ref y, ref z, ref o);
                switch (passenger.GetTypeId())
                {
                    case TypeId.Unit:
                        {
                            Creature creature = passenger.ToCreature();
                            GetMap().CreatureRelocation(creature, x, y, z, o, false);
                            creature.GetTransportHomePosition(out x, out y, out z, out o);
                            CalculatePassengerPosition(ref x, ref y, ref z, ref o);
                            creature.SetHomePosition(x, y, z, o);
                            break;
                        }
                    case TypeId.Player:
                        if (passenger.IsInWorld)
                            GetMap().PlayerRelocation(passenger.ToPlayer(), x, y, z, o);
                        break;
                    case TypeId.GameObject:
                        GetMap().GameObjectRelocation(passenger.ToGameObject(), x, y, z, o, false);
                        passenger.ToGameObject().RelocateStationaryPosition(x, y, z, o);
                        break;
                    case TypeId.DynamicObject:
                        GetMap().DynamicObjectRelocation(passenger.ToDynamicObject(), x, y, z, o);
                        break;
                    case TypeId.AreaTrigger:
                        GetMap().AreaTriggerRelocation(passenger.ToAreaTrigger(), x, y, z, o);
                        break;
                    default:
                        break;
                }

                if (unit != null)
                {
                    Vehicle vehicle = unit.GetVehicleKit();
                    if (vehicle != null)
                        vehicle.RelocatePassengers();
                }
            }
        }

        void DoEventIfAny(KeyFrame node, bool departure)
        {
            uint eventid = departure ? node.Node.DepartureEventID : node.Node.ArrivalEventID;
            if (eventid != 0)
            {
                Log.outDebug(LogFilter.Scripts, "Taxi {0} event {1} of node {2} of {3} path", departure ? "departure" : "arrival", eventid, node.Node.NodeIndex, GetName());
                GetMap().ScriptsStart(ScriptsType.Event, eventid, this, this);
                EventInform(eventid);
            }
        }

        public override void BuildUpdate(Dictionary<Player, UpdateData> data_map)
        {
            var players = GetMap().GetPlayers();
            if (players.Empty())
                return;

            foreach (var pl in players)
                BuildFieldsUpdate(pl, data_map);

            ClearUpdateMask(true);
        }

        public HashSet<WorldObject> GetPassengers() { return _passengers; }

        public override  uint GetTransportPeriod() { return GetUInt32Value(GameObjectFields.Level); }
        public void SetPeriod(uint period) { SetUInt32Value(GameObjectFields.Level, period); }
        uint GetTimer() { return m_goValue.Transport.PathProgress; }

        public List<KeyFrame> GetKeyFrames() { return _transportInfo.keyFrames; }
        public TransportTemplate GetTransportTemplate() { return _transportInfo; }

        //! Helpers to know if stop frame was reached
        bool IsMoving() { return _isMoving; }
        void SetMoving(bool val) { _isMoving = val; }

        TransportTemplate _transportInfo;

        KeyFrame _currentFrame;
        int _nextFrame;
        TimeTrackerSmall _positionChangeTimer = new TimeTrackerSmall();
        bool _isMoving;
        bool _pendingStop;

        //! These are needed to properly control events triggering only once for each frame
        bool _triggeredArrivalEvent;
        bool _triggeredDepartureEvent;

        HashSet<WorldObject> _passengers = new HashSet<WorldObject>();
        HashSet<WorldObject> _staticPassengers = new HashSet<WorldObject>();

        bool _delayedAddModel;
        bool _delayedTeleport;
    }
}
