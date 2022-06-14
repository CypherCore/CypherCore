/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.Movement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Game.Maps
{
    public class TransportManager : Singleton<TransportManager>
    {
        TransportManager() { }

        void Unload()
        {
            _transportTemplates.Clear();
        }

        public void LoadTransportTemplates()
        {
            uint oldMSTime = Time.GetMSTime();

            SQLResult result = DB.World.Query("SELECT entry FROM gameobject_template WHERE type = 15 ORDER BY entry ASC");

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 transports templates. DB table `gameobject_template` has no transports!");
                return;
            }

            uint count = 0;

            do
            {
                uint entry = result.Read<uint>(0);
                GameObjectTemplate goInfo = Global.ObjectMgr.GetGameObjectTemplate(entry);
                if (goInfo == null)
                {
                    Log.outError(LogFilter.Sql, "Transport {0} has no associated GameObjectTemplate from `gameobject_template` , skipped.", entry);
                    continue;
                }

                if (!CliDB.TaxiPathNodesByPath.ContainsKey(goInfo.MoTransport.taxiPathID))
                {
                    Log.outError(LogFilter.Sql, "Transport {0} (name: {1}) has an invalid path specified in `gameobject_template`.`data0` ({2}) field, skipped.", entry, goInfo.name, goInfo.MoTransport.taxiPathID);
                    continue;
                }

                if (goInfo.MoTransport.taxiPathID == 0)
                    continue;

                // paths are generated per template, saves us from generating it again in case of instanced transports
                TransportTemplate transport = new();

                List<uint> mapsUsed = new();

                GeneratePath(goInfo, transport, mapsUsed);

                _transportTemplates[entry] = transport;

                // transports in instance are only on one map
                if (transport.InInstance)
                    _instanceTransports.Add(mapsUsed.First(), entry);

                ++count;
            } while (result.NextRow());


            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} transports in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadTransportAnimationAndRotation()
        {
            foreach (TransportAnimationRecord anim in CliDB.TransportAnimationStorage.Values)
                AddPathNodeToTransport(anim.TransportID, anim.TimeIndex, anim);

            foreach (TransportRotationRecord rot in CliDB.TransportRotationStorage.Values)
                AddPathRotationToTransport(rot.GameObjectsID, rot.TimeIndex, rot);
        }

        public void LoadTransportSpawns()
        {
            if (_transportTemplates.Empty())
                return;

            uint oldMSTime = Time.GetMSTime();

            SQLResult result = DB.World.Query("SELECT guid, entry, phaseUseFlags, phaseid, phasegroup FROM transports");

            uint count = 0;
            if (!result.IsEmpty())
            {
                do
                {
                    ulong guid = result.Read<ulong>(0);
                    uint entry = result.Read<uint>(1);
                    PhaseUseFlagsValues phaseUseFlags = (PhaseUseFlagsValues)result.Read<byte>(2);
                    uint phaseId = result.Read<uint>(3);
                    uint phaseGroupId = result.Read<uint>(4);

                    if (GetTransportTemplate(entry) == null)
                    {
                        Log.outError(LogFilter.Sql, $"Table `transports` have transport (GUID: {guid} Entry: {entry}) with unknown gameobject `entry` set, skipped.");
                        continue;
                    }

                    if ((phaseUseFlags & ~PhaseUseFlagsValues.All) != 0)
                    {
                        Log.outError(LogFilter.Sql, $"Table `transports` have transport (GUID: {guid} Entry: {entry}) with unknown `phaseUseFlags` set, removed unknown value.");
                        phaseUseFlags &= PhaseUseFlagsValues.All;
                    }

                    if (phaseUseFlags.HasFlag(PhaseUseFlagsValues.AlwaysVisible) && phaseUseFlags.HasFlag(PhaseUseFlagsValues.Inverse))
                    {
                        Log.outError(LogFilter.Sql, $"Table `transports` have transport (GUID: {guid} Entry: {entry}) has both `phaseUseFlags` PHASE_USE_FLAGS_ALWAYS_VISIBLE and PHASE_USE_FLAGS_INVERSE, removing PHASE_USE_FLAGS_INVERSE.");
                        phaseUseFlags &= ~PhaseUseFlagsValues.Inverse;
                    }

                    if (phaseGroupId != 0 && phaseId != 0)
                    {
                        Log.outError(LogFilter.Sql, $"Table `transports` have transport (GUID: {guid} Entry: {entry}) with both `phaseid` and `phasegroup` set, `phasegroup` set to 0");
                        phaseGroupId = 0;
                    }

                    if (phaseId != 0)
                    {
                        if (!CliDB.PhaseStorage.ContainsKey(phaseId))
                        {
                            Log.outError(LogFilter.Sql, $"Table `transports` have transport (GUID: {guid} Entry: {entry}) with `phaseid` {phaseId} does not exist, set to 0");
                            phaseId = 0;
                        }
                    }

                    if (phaseGroupId != 0)
                    {
                        if (Global.DB2Mgr.GetPhasesForGroup(phaseGroupId) == null)
                        {
                            Log.outError(LogFilter.Sql, $"Table `transports` have transport (GUID: {guid} Entry: {entry}) with `phaseGroup` {phaseGroupId} does not exist, set to 0");
                            phaseGroupId = 0;
                        }
                    }

                    TransportSpawn spawn = new();
                    spawn.SpawnId = guid;
                    spawn.TransportGameObjectId = entry;
                    spawn.PhaseUseFlags = phaseUseFlags;
                    spawn.PhaseId = phaseId;
                    spawn.PhaseGroup = phaseGroupId;

                    _transportSpawns[guid] = spawn;

                } while (result.NextRow());
            }

            Log.outInfo(LogFilter.ServerLoading, $"Spawned {count} continent transports in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        static void InitializeLeg(TransportPathLeg leg, List<TransportPathEvent> outEvents, List<TaxiPathNodeRecord> pathPoints, List<TaxiPathNodeRecord> pauses, List<TaxiPathNodeRecord> events, GameObjectTemplate goInfo, ref uint totalTime)
        {
            List<Vector3> splinePath = new(pathPoints.Select(node => new Vector3(node.Loc.X, node.Loc.Y, node.Loc.Z)));
            SplineRawInitializer initer = new(splinePath);
            leg.Spline = new Spline<double>();
            leg.Spline.set_steps_per_segment(20);
            leg.Spline.InitSplineCustom(initer);
            leg.Spline.InitLengths();

            uint legTimeAccelDecel(double dist)
            {
                double speed = (double)goInfo.MoTransport.moveSpeed;
                double accel = (double)goInfo.MoTransport.accelRate;
                double accelDist = 0.5 * speed * speed / accel;
                if (accelDist >= dist * 0.5)
                    return (uint)(Math.Sqrt(dist / accel) * 2000.0);
                else
                    return (uint)((dist - (accelDist + accelDist)) / speed * 1000.0 + speed / accel * 2000.0);
            }

            uint legTimeAccel(double dist)
            {
                double speed = (double)goInfo.MoTransport.moveSpeed;
                double accel = (double)goInfo.MoTransport.accelRate;
                double accelDist = 0.5 * speed * speed / accel;
                if (accelDist >= dist)
                    return (uint)(Math.Sqrt((dist + dist) / accel) * 1000.0);
                else
                    return (uint)(((dist - accelDist) / speed + speed / accel) * 1000.0);
            };

            // Init segments
            int pauseItr = 0;
            int eventItr = 0;
            double splineLengthToPreviousNode = 0.0;
            uint delaySum = 0;
            if (!pauses.Empty())
            {
                for (; pauseItr < pauses.Count; ++pauseItr)
                {
                    var pausePointIndex = pathPoints.IndexOf(pauses[pauseItr]);
                    if (pausePointIndex == -1) // last point is a "fake" spline point, its position can never be reached so transport cannot stop there
                        break;

                    for (; eventItr < events.Count; ++eventItr)
                    {
                        var eventPointIndex = pathPoints.IndexOf(events[eventItr]);
                        if (eventPointIndex > pausePointIndex)
                            break;

                        double eventLength = leg.Spline.Length(eventPointIndex) - splineLengthToPreviousNode;
                        uint eventSplineTime = 0;
                        if (pauseItr != 0)
                            eventSplineTime = legTimeAccelDecel(eventLength);
                        else
                            eventSplineTime = legTimeAccel(eventLength);

                        if (pathPoints[eventPointIndex].ArrivalEventID != 0)
                        {
                            TransportPathEvent Event = new();
                            Event.Timestamp = totalTime + eventSplineTime + leg.Duration;
                            Event.EventId = pathPoints[eventPointIndex].ArrivalEventID;
                            outEvents.Add(Event);
                        }

                        if (pathPoints[eventPointIndex].DepartureEventID != 0)
                        {
                            TransportPathEvent Event = new();
                            Event.Timestamp = totalTime + eventSplineTime + leg.Duration + (pausePointIndex == eventPointIndex ? pathPoints[eventPointIndex].Delay * Time.InMilliseconds : 0);
                            Event.EventId = pathPoints[eventPointIndex].DepartureEventID;
                            outEvents.Add(Event);
                        }
                    }

                    double splineLengthToCurrentNode = leg.Spline.Length(pausePointIndex);
                    double length1 = splineLengthToCurrentNode - splineLengthToPreviousNode;
                    uint movementTime = 0;
                    if (pauseItr != 0)
                        movementTime = legTimeAccelDecel(length1);
                    else
                        movementTime = legTimeAccel(length1);

                    leg.Duration += movementTime;
                    var segment = leg.Segments[pauseItr];
                    segment.SegmentEndArrivalTimestamp = leg.Duration + delaySum;
                    segment.Delay = pathPoints[pausePointIndex].Delay * Time.InMilliseconds;
                    segment.DistanceFromLegStartAtEnd = splineLengthToCurrentNode;
                    delaySum += pathPoints[pausePointIndex].Delay * Time.InMilliseconds;
                    splineLengthToPreviousNode = splineLengthToCurrentNode;
                }
            }

            // Process events happening after last pause
            for (; eventItr < events.Count; ++eventItr)
            {
                var eventPointIndex = pathPoints.IndexOf(events[eventItr]);
                if (eventPointIndex == -1) // last point is a "fake" spline node, events cannot happen there
                    break;

                double eventLength = leg.Spline.Length(eventPointIndex) - splineLengthToPreviousNode;
                uint eventSplineTime = 0;
                if (pauseItr != 0)
                    eventSplineTime = legTimeAccel(eventLength);
                else
                    eventSplineTime = (uint)(eventLength / (double)goInfo.MoTransport.moveSpeed * 1000.0);

                if (pathPoints[eventPointIndex].ArrivalEventID != 0)
                {
                    TransportPathEvent Event = new();
                    Event.Timestamp = totalTime + eventSplineTime + leg.Duration;
                    Event.EventId = pathPoints[eventPointIndex].ArrivalEventID;
                    outEvents.Add(Event);
                }

                if (pathPoints[eventPointIndex].DepartureEventID != 0)
                {
                    TransportPathEvent Event = new();
                    Event.Timestamp = totalTime + eventSplineTime + leg.Duration;
                    Event.EventId = pathPoints[eventPointIndex].DepartureEventID;
                    outEvents.Add(Event);
                }
            }

            // Add segment after last pause
            double length = leg.Spline.Length() - splineLengthToPreviousNode;
            uint splineTime = 0;
            if (pauseItr != 0)
                splineTime = legTimeAccel(length);
            else
                splineTime = (uint)(length / (double)goInfo.MoTransport.moveSpeed * 1000.0);

            leg.StartTimestamp = totalTime;
            leg.Duration += splineTime + delaySum;
            var pauseSegment = leg.Segments[pauseItr];
            pauseSegment.SegmentEndArrivalTimestamp = leg.Duration;
            pauseSegment.Delay = 0;
            pauseSegment.DistanceFromLegStartAtEnd = leg.Spline.Length();
            totalTime += leg.Segments[pauseItr].SegmentEndArrivalTimestamp + leg.Segments[pauseItr].Delay;

            for (var i = 0; i < leg.Segments.Count; ++i)
            {
                var segment = leg.Segments[i];
                segment.SegmentEndArrivalTimestamp += leg.StartTimestamp;
            }
        }

        void GeneratePath(GameObjectTemplate goInfo, TransportTemplate transport, List<uint> mapsUsed)
        {
            uint pathId = goInfo.MoTransport.taxiPathID;
            TaxiPathNodeRecord[] path = CliDB.TaxiPathNodesByPath[pathId];

            transport.Speed = (double)goInfo.MoTransport.moveSpeed;
            transport.AccelerationRate = (double)goInfo.MoTransport.accelRate;
            transport.AccelerationTime = transport.Speed / transport.AccelerationRate;
            transport.AccelerationDistance = 0.5 * transport.Speed * transport.Speed / transport.AccelerationRate;

            List<TaxiPathNodeRecord> pathPoints = new();
            List<TaxiPathNodeRecord> pauses = new();
            List<TaxiPathNodeRecord> events = new();
            TransportPathLeg leg = new();
            leg.MapId = path[0].ContinentID;
            bool prevNodeWasTeleport = false;
            uint totalTime = 0;
            foreach (TaxiPathNodeRecord node in path)
            {
                if (node.ContinentID != leg.MapId || prevNodeWasTeleport)
                {
                    InitializeLeg(leg, transport.Events, pathPoints, pauses, events, goInfo, ref totalTime);

                    leg = new();
                    leg.MapId = node.ContinentID;
                    pathPoints.Clear();
                    pauses.Clear();
                    events.Clear();
                    transport.PathLegs.Add(leg);
                }

                prevNodeWasTeleport = node.Flags.HasFlag(TaxiPathNodeFlags.Teleport);
                pathPoints.Add(node);
                if (node.Flags.HasFlag(TaxiPathNodeFlags.Stop))
                    pauses.Add(node);

                if (node.ArrivalEventID != 0 || node.DepartureEventID != 0)
                    events.Add(node);

                mapsUsed.Add(node.ContinentID);
            }

            if (leg.Spline == null)
                InitializeLeg(leg, transport.Events, pathPoints, pauses, events, goInfo, ref totalTime);

            if (mapsUsed.Count > 1)
            {
                foreach (uint mapId in mapsUsed)
                    Cypher.Assert(!CliDB.MapStorage.LookupByKey(mapId).Instanceable());

                transport.InInstance = false;
            }
            else
                transport.InInstance = CliDB.MapStorage.LookupByKey(mapsUsed.First()).Instanceable();

            transport.TotalPathTime = totalTime;
            transport.PathLegs.Add(leg);
        }
        
        public void AddPathNodeToTransport(uint transportEntry, uint timeSeg, TransportAnimationRecord node)
        {
            TransportAnimation animNode = new();
            if (animNode.TotalTime < timeSeg)
                animNode.TotalTime = timeSeg;

            animNode.Path[timeSeg] = node;

            _transportAnimations[transportEntry] = animNode;
        }

        public void AddPathRotationToTransport(uint transportEntry, uint timeSeg, TransportRotationRecord node)
        {
            if (!_transportAnimations.ContainsKey(transportEntry))
                _transportAnimations[transportEntry] = new();

            TransportAnimation animNode = _transportAnimations[transportEntry];
            animNode.Rotations[timeSeg] = node;

            if (animNode.Path.Empty() && animNode.TotalTime < timeSeg)
                animNode.TotalTime = timeSeg;
        }

        public Transport CreateTransport(uint entry, ulong guid = 0, Map map = null, PhaseUseFlagsValues phaseUseFlags = 0, uint phaseId = 0, uint phaseGroupId = 0)
        {
            // instance case, execute GetGameObjectEntry hook
            if (map != null)
            {
                // SetZoneScript() is called after adding to map, so fetch the script using map
                if (map.IsDungeon())
                {
                    InstanceScript instance = ((InstanceMap)map).GetInstanceScript();
                    if (instance != null)
                        entry = instance.GetGameObjectEntry(0, entry);
                }

                if (entry == 0)
                    return null;
            }

            TransportTemplate tInfo = GetTransportTemplate(entry);
            if (tInfo == null)
            {
                Log.outError(LogFilter.Sql, "Transport {0} will not be loaded, `transport_template` missing", entry);
                return null;
            }

            Position startingPosition = tInfo.ComputePosition(0, out _, out _);
            if (startingPosition == null)
            {
                Log.outError(LogFilter.Sql, $"Transport {entry} will not be loaded, failed to compute starting position");
                return null;
            }

            // create transport...
            Transport trans = new();

            // ...at first waypoint
            uint mapId = tInfo.PathLegs.First().MapId;
            float x = startingPosition.GetPositionX();
            float y = startingPosition.GetPositionY();
            float z = startingPosition.GetPositionZ();
            float o = startingPosition.GetOrientation();

            // initialize the gameobject base
            ulong guidLow = guid != 0 ? guid : map.GenerateLowGuid(HighGuid.Transport);
            if (!trans.Create(guidLow, entry, mapId, x, y, z, o, 255))
                return null;

            PhasingHandler.InitDbPhaseShift(trans.GetPhaseShift(), phaseUseFlags, phaseId, phaseGroupId);

            MapRecord mapEntry = CliDB.MapStorage.LookupByKey(mapId);
            if (mapEntry != null)
            {
                if (mapEntry.Instanceable() != tInfo.InInstance)
                {
                    Log.outError(LogFilter.Transport, "Transport {0} (name: {1}) attempted creation in instance map (id: {2}) but it is not an instanced transport!", entry, trans.GetName(), mapId);
                    //return null;
                }
            }

            // use preset map for instances (need to know which instance)
            trans.SetMap(map != null ? map : Global.MapMgr.CreateMap(mapId, null));
            if (map != null && map.IsDungeon())
                trans.m_zoneScript = map.ToInstanceMap().GetInstanceScript();

            // Passengers will be loaded once a player is near

            Global.ObjAccessor.AddObject(trans);
            trans.GetMap().AddToMap(trans);
            return trans;
        }

        public void SpawnContinentTransports()
        {
            uint oldMSTime = Time.GetMSTime();

            uint count = 0;

            foreach (var pair in _transportSpawns)
                if (!GetTransportTemplate(pair.Value.TransportGameObjectId).InInstance)
                    if (CreateTransport(pair.Value.TransportGameObjectId, pair.Value.SpawnId, null, pair.Value.PhaseUseFlags, pair.Value.PhaseId, pair.Value.PhaseGroup))
                        ++count;

            Log.outInfo(LogFilter.ServerLoading, "Spawned {0} continent transports in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void CreateInstanceTransports(Map map)
        {
            var mapTransports = _instanceTransports.LookupByKey(map.GetId());

            // no transports here
            if (mapTransports.Empty())
                return;

            // create transports
            foreach (var transportGameObjectId in mapTransports)
                CreateTransport(transportGameObjectId, 0, map);
        }

        public TransportTemplate GetTransportTemplate(uint entry)
        {
            return _transportTemplates.LookupByKey(entry);
        }

        public TransportAnimation GetTransportAnimInfo(uint entry)
        {
            return _transportAnimations.LookupByKey(entry);
        }

        public TransportSpawn GetTransportSpawn(ulong spawnId)
        {
            return _transportSpawns.LookupByKey(spawnId);
        }

        Dictionary<uint, TransportTemplate> _transportTemplates = new();
        MultiMap<uint, uint> _instanceTransports = new();
        Dictionary<uint, TransportAnimation> _transportAnimations = new();
        Dictionary<ulong, TransportSpawn> _transportSpawns = new();
    }

    public struct TransportPathSegment
    {
        public uint SegmentEndArrivalTimestamp;
        public uint Delay;
        public double DistanceFromLegStartAtEnd;
    }

    public struct TransportPathEvent
    {
        public uint Timestamp;
        public uint EventId;
    }

    public class TransportPathLeg
    {
        public uint MapId;
        public Spline<double> Spline;
        public uint StartTimestamp;
        public uint Duration;
        public List<TransportPathSegment> Segments = new();
    }

    public class TransportTemplate
    {
        public uint TotalPathTime;
        public double Speed;
        public double AccelerationRate;
        public double AccelerationTime;
        public double AccelerationDistance;
        public List<TransportPathLeg> PathLegs = new();
        public List<TransportPathEvent> Events = new();

        public bool InInstance;

        public Position ComputePosition(uint time, out TransportMovementState moveState, out int legIndex)
        {
            moveState = TransportMovementState.Moving;

            time %= TotalPathTime;

            // find leg
            legIndex = 0;
            while (PathLegs[legIndex].StartTimestamp + PathLegs[legIndex].Duration <= time)
            {
                ++legIndex;

                if (PathLegs.Count >= legIndex)
                    return null;
            }

            var legItr = PathLegs[legIndex];

            // find segment
            uint prevSegmentTime = legItr.StartTimestamp;
            var segmentIndex = 0;
            double distanceMoved = 0.0;
            bool isOnPause = false;
            for (segmentIndex = 0; segmentIndex < legItr.Segments.Count; ++segmentIndex)
            {
                var segment = legItr.Segments[segmentIndex];
                if (time < segment.SegmentEndArrivalTimestamp)
                    break;

                distanceMoved = segment.DistanceFromLegStartAtEnd;
                if (time < segment.SegmentEndArrivalTimestamp + segment.Delay)
                {
                    isOnPause = true;
                    break;
                }

                prevSegmentTime = segment.SegmentEndArrivalTimestamp + segment.Delay;
            }

            var pathSegment = legItr.Segments[segmentIndex];

            if (!isOnPause)
                distanceMoved += CalculateDistanceMoved(
                    (double)(time - prevSegmentTime) * 0.001,
                    (double)(pathSegment.SegmentEndArrivalTimestamp - prevSegmentTime) * 0.001,
                    segmentIndex == 0,
                    segmentIndex == legItr.Segments.Count);

            int splineIndex = 0;
            float splinePointProgress = 0;
            legItr.Spline.ComputeIndex((float)Math.Min(distanceMoved / legItr.Spline.Length(), 1.0), ref splineIndex, ref splinePointProgress);

            Vector3 pos, dir;
            legItr.Spline.Evaluate_Percent(splineIndex, splinePointProgress, out pos);
            legItr.Spline.Evaluate_Derivative(splineIndex, splinePointProgress, out dir);

            moveState = isOnPause ? TransportMovementState.WaitingOnPauseWaypoint : TransportMovementState.Moving;

            return new Position(pos.X, pos.Y, pos.Z, MathF.Atan2(dir.Y, dir.X) + MathF.PI);
        }

        double CalculateDistanceMoved(double timePassedInSegment, double segmentDuration, bool isFirstSegment, bool isLastSegment)
        {
            if (isFirstSegment)
            {
                if (!isLastSegment)
                {
                    double accelerationTime = Math.Min(AccelerationTime, segmentDuration);
                    double segmentTimeAtFullSpeed = segmentDuration - accelerationTime;
                    if (timePassedInSegment <= segmentTimeAtFullSpeed)
                    {
                        return timePassedInSegment * Speed;
                    }
                    else
                    {
                        double segmentAccelerationTime = timePassedInSegment - segmentTimeAtFullSpeed;
                        double segmentAccelerationDistance = AccelerationRate * accelerationTime;
                        double segmentDistanceAtFullSpeed = segmentTimeAtFullSpeed * Speed;
                        return (2.0 * segmentAccelerationDistance - segmentAccelerationTime * AccelerationRate) * 0.5 * segmentAccelerationTime + segmentDistanceAtFullSpeed;
                    }
                }

                return timePassedInSegment * Speed;
            }

            if (isLastSegment)
            {
                if (!isFirstSegment)
                {
                    if (timePassedInSegment <= Math.Min(AccelerationTime, segmentDuration))
                        return AccelerationRate * timePassedInSegment * 0.5 * timePassedInSegment;
                    else
                        return (timePassedInSegment - AccelerationTime) * Speed + AccelerationDistance;
                }

                return timePassedInSegment * Speed;
            }

            double accelerationTime1 = Math.Min(segmentDuration * 0.5, AccelerationTime);
            if (timePassedInSegment <= segmentDuration - accelerationTime1)
            {
                if (timePassedInSegment <= accelerationTime1)
                    return AccelerationRate * timePassedInSegment * 0.5 * timePassedInSegment;
                else
                    return (timePassedInSegment - AccelerationTime) * Speed + AccelerationDistance;
            }
            else
            {
                double segmentTimeSpentAccelerating = timePassedInSegment - (segmentDuration - accelerationTime1);
                return (segmentDuration - 2 * accelerationTime1) * Speed
                    + AccelerationRate * accelerationTime1 * 0.5 * accelerationTime1
                    + (2.0 * AccelerationRate * accelerationTime1 - segmentTimeSpentAccelerating * AccelerationRate) * 0.5 * segmentTimeSpentAccelerating;
            }
        }

        public uint GetNextPauseWaypointTimestamp(uint time)
        {
            var legIndex = 0;
            while (PathLegs[legIndex].StartTimestamp + PathLegs[legIndex].Duration <= time)
            {
                ++legIndex;

                if (legIndex >= PathLegs.Count)
                    return time;
            }

            var leg = PathLegs[legIndex];

            var segmentIndex = 0;
            for (; segmentIndex != leg.Segments.Count - 1; ++segmentIndex)
                if (time < leg.Segments[segmentIndex].SegmentEndArrivalTimestamp + leg.Segments[segmentIndex].Delay)
                    break;

            return leg.Segments[segmentIndex].SegmentEndArrivalTimestamp + leg.Segments[segmentIndex].Delay;
        }
    }

    public class SplineRawInitializer
    {
        public SplineRawInitializer(List<Vector3> points)
        {
            _points = points;
        }

        public void Initialize(ref EvaluationMode mode, ref bool cyclic, ref Vector3[] points, ref int lo, ref int hi)
        {
            mode = EvaluationMode.Catmullrom;
            cyclic = false;
            points = new Vector3[_points.Count];

            for (var i = 0; i < _points.Count; ++i)
                points[i] = _points[i];

            lo = 1;
            hi = points.Length - 2;
        }

        List<Vector3> _points;
    }

    public class TransportAnimation
    {
        public Dictionary<uint, TransportAnimationRecord> Path = new();
        public Dictionary<uint, TransportRotationRecord> Rotations = new();
        public uint TotalTime;

        public TransportAnimationRecord GetPrevAnimNode(uint time)
        {
            if (Path.Empty())
                return null;

            List<uint> lKeys = Path.Keys.ToList();
            int reqIndex = lKeys.IndexOf(time) - 1;

            if (reqIndex != -1)
                return Path[lKeys[reqIndex]];

            return Path.LastOrDefault().Value;
        }

        public TransportRotationRecord GetPrevAnimRotation(uint time)
        {
            if (Rotations.Empty())
                return null;

            List<uint> lKeys = Rotations.Keys.ToList();
            int reqIndex = lKeys.IndexOf(time) - 1;

            if (reqIndex != -1)
                return Rotations[lKeys[reqIndex]];

            return Rotations.LastOrDefault().Value;
        }

        public TransportAnimationRecord GetNextAnimNode(uint time)
        {
            if (Path.Empty())
                return null;

            if (Path.TryGetValue(time, out TransportAnimationRecord record))
                return record;

            return Path.FirstOrDefault().Value;
        }

        public TransportRotationRecord GetNextAnimRotation(uint time)
        {
            if (Rotations.Empty())
                return null;

            if (Rotations.TryGetValue(time, out TransportRotationRecord record))
                return record;

            return Rotations.FirstOrDefault().Value;
        }
    }

    public class TransportSpawn
    {
        public ulong SpawnId;
        public uint TransportGameObjectId; // entry in respective _template table
        public PhaseUseFlagsValues PhaseUseFlags;
        public uint PhaseId;
        public uint PhaseGroup;
    }
}
