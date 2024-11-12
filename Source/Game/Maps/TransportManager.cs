// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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

                GeneratePath(goInfo, transport);
                _transportTemplates[entry] = transport;

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

                    TransportTemplate transportTemplate = GetTransportTemplate(entry);
                    if (transportTemplate == null)
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

                    foreach (uint mapId in transportTemplate.MapIds)
                        _transportsByMap.Add(mapId, spawn);

                    _transportSpawns[guid] = spawn;

                    count++;
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
                    if (pausePointIndex == pathPoints.Count - 1) // last point is a "fake" spline point, its position can never be reached so transport cannot stop there
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
                            Event.Timestamp = totalTime + eventSplineTime + leg.Duration + delaySum;
                            Event.EventId = pathPoints[eventPointIndex].ArrivalEventID;
                            outEvents.Add(Event);
                        }

                        if (pathPoints[eventPointIndex].DepartureEventID != 0)
                        {
                            TransportPathEvent Event = new();
                            Event.Timestamp = totalTime + eventSplineTime + leg.Duration + delaySum + (pausePointIndex == eventPointIndex ? pathPoints[eventPointIndex].Delay * Time.InMilliseconds : 0);
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
                    TransportPathSegment segment = new();
                    segment.SegmentEndArrivalTimestamp = leg.Duration + delaySum;
                    segment.Delay = pathPoints[pausePointIndex].Delay * Time.InMilliseconds;
                    segment.DistanceFromLegStartAtEnd = splineLengthToCurrentNode;
                    leg.Segments.Add(segment);
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
                    Event.Timestamp = totalTime + eventSplineTime + leg.Duration + delaySum;
                    Event.EventId = pathPoints[eventPointIndex].ArrivalEventID;
                    outEvents.Add(Event);
                }

                if (pathPoints[eventPointIndex].DepartureEventID != 0)
                {
                    TransportPathEvent Event = new();
                    Event.Timestamp = totalTime + eventSplineTime + leg.Duration + delaySum;
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
            TransportPathSegment pauseSegment = new();
            pauseSegment.SegmentEndArrivalTimestamp = leg.Duration;
            pauseSegment.Delay = 0;
            pauseSegment.DistanceFromLegStartAtEnd = leg.Spline.Length();
            leg.Segments.Add(pauseSegment);
            totalTime += leg.Segments[pauseItr].SegmentEndArrivalTimestamp + leg.Segments[pauseItr].Delay;

            for (var i = 0; i < leg.Segments.Count; ++i)
                leg.Segments[i].SegmentEndArrivalTimestamp += leg.StartTimestamp;
        }

        void GeneratePath(GameObjectTemplate goInfo, TransportTemplate transport)
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

            transport.PathLegs.Add(new TransportPathLeg());

            TransportPathLeg leg = transport.PathLegs[0];
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

                prevNodeWasTeleport = node.HasFlag(TaxiPathNodeFlags.Teleport);
                pathPoints.Add(node);
                if (node.HasFlag(TaxiPathNodeFlags.Stop))
                    pauses.Add(node);

                if (node.ArrivalEventID != 0 || node.DepartureEventID != 0)
                    events.Add(node);

                transport.MapIds.Add(node.ContinentID);
            }

            if (leg.Spline == null)
                InitializeLeg(leg, transport.Events, pathPoints, pauses, events, goInfo, ref totalTime);

            if (transport.MapIds.Count > 1)
            {
                foreach (uint mapId in transport.MapIds)
                    Cypher.Assert(!CliDB.MapStorage.LookupByKey(mapId).Instanceable());
            }

            transport.TotalPathTime = totalTime;
        }

        public void AddPathNodeToTransport(uint transportEntry, uint timeSeg, TransportAnimationRecord node)
        {
            if (!_transportAnimations.ContainsKey(transportEntry))
                _transportAnimations[transportEntry] = new();

            TransportAnimation animNode = _transportAnimations[transportEntry];
            if (animNode.TotalTime < timeSeg)
                animNode.TotalTime = timeSeg;

            animNode.Path[timeSeg] = node;
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

        public Transport CreateTransport(uint entry, Map map, ulong guid = 0, PhaseUseFlagsValues phaseUseFlags = 0, uint phaseId = 0, uint phaseGroupId = 0)
        {
            // SetZoneScript() is called after adding to map, so fetch the script using map
            InstanceMap instanceMap = map.ToInstanceMap();
            if (instanceMap != null)
            {
                InstanceScript instance = instanceMap.GetInstanceScript();
                if (instance != null)
                    entry = instance.GetGameObjectEntry(0, entry);
            }

            if (entry == 0)
                return null;

            TransportTemplate tInfo = GetTransportTemplate(entry);
            if (tInfo == null)
            {
                Log.outError(LogFilter.Sql, "Transport {0} will not be loaded, `transport_template` missing", entry);
                return null;
            }

            if (!tInfo.MapIds.Contains(map.GetId()))
            {
                Log.outError(LogFilter.Transport, $"Transport {entry} attempted creation on map it has no path for {map.GetId()}!");
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
            float x = startingPosition.GetPositionX();
            float y = startingPosition.GetPositionY();
            float z = startingPosition.GetPositionZ();
            float o = startingPosition.GetOrientation();

            // initialize the gameobject base
            ulong guidLow = guid != 0 ? guid : map.GenerateLowGuid(HighGuid.Transport);
            if (!trans.Create(guidLow, entry, x, y, z, o))
                return null;

            PhasingHandler.InitDbPhaseShift(trans.GetPhaseShift(), phaseUseFlags, phaseId, phaseGroupId);

            // use preset map for instances (need to know which instance)
            trans.SetMap(map);
            if (instanceMap != null)
                trans.m_zoneScript = instanceMap.GetInstanceScript();

            // Passengers will be loaded once a player is near

            map.AddToMap(trans);
            return trans;
        }

        public void CreateTransportsForMap(Map map)
        {
            var mapTransports = _transportsByMap.LookupByKey(map.GetId());

            // no transports here
            if (mapTransports.Empty())
                return;

            // create transports
            foreach (var transport in mapTransports)
                CreateTransport(transport.TransportGameObjectId, map, transport.SpawnId, transport.PhaseUseFlags, transport.PhaseId, transport.PhaseGroup);
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
        MultiMap<uint, TransportSpawn> _transportsByMap = new();
        Dictionary<uint, TransportAnimation> _transportAnimations = new();
        Dictionary<ulong, TransportSpawn> _transportSpawns = new();
    }

    public class TransportPathSegment
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

        public HashSet<uint> MapIds = new();

        public Position ComputePosition(uint time, out TransportMovementState moveState, out int legIndex)
        {
            moveState = TransportMovementState.Moving;
            legIndex = 0;

            time %= TotalPathTime;

            // find leg
            TransportPathLeg leg = GetLegForTime(time);
            if (leg == null)
                return null;

            // find segment
            uint prevSegmentTime = leg.StartTimestamp;
            var segmentIndex = 0;
            double distanceMoved = 0.0;
            bool isOnPause = false;
            for (segmentIndex = 0; segmentIndex < leg.Segments.Count - 1; ++segmentIndex)
            {
                var segment = leg.Segments[segmentIndex];
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

            var pathSegment = leg.Segments[segmentIndex];

            if (!isOnPause)
                distanceMoved += CalculateDistanceMoved(
                    (double)(time - prevSegmentTime) * 0.001,
                    (double)(pathSegment.SegmentEndArrivalTimestamp - prevSegmentTime) * 0.001,
                    segmentIndex == 0,
                    segmentIndex == leg.Segments.Count - 1);

            int splineIndex = 0;
            float splinePointProgress = 0;
            leg.Spline.ComputeIndex((float)Math.Min(distanceMoved / leg.Spline.Length(), 1.0), ref splineIndex, ref splinePointProgress);

            Vector3 pos, dir;
            leg.Spline.Evaluate_Percent(splineIndex, splinePointProgress, out pos);
            leg.Spline.Evaluate_Derivative(splineIndex, splinePointProgress, out dir);

            moveState = isOnPause ? TransportMovementState.WaitingOnPauseWaypoint : TransportMovementState.Moving;
            legIndex = PathLegs.IndexOf(leg);

            return new Position(pos.X, pos.Y, pos.Z, MathF.Atan2(dir.Y, dir.X) + MathF.PI);
        }

        public TransportPathLeg GetLegForTime(uint time)
        {
            int legIndex = 0;
            while (PathLegs[legIndex].StartTimestamp + PathLegs[legIndex].Duration <= time)
            {
                ++legIndex;

                if (legIndex >= PathLegs.Count)
                    return null;
            }

            return PathLegs[legIndex];
        }

        public uint GetNextPauseWaypointTimestamp(uint time)
        {
            TransportPathLeg leg = GetLegForTime(time);
            if (leg == null)
                return time;

            int segmentIndex = 0;
            for (; segmentIndex != leg.Segments.Count - 1; ++segmentIndex)
                if (time < leg.Segments[segmentIndex].SegmentEndArrivalTimestamp + leg.Segments[segmentIndex].Delay)
                    break;

            return leg.Segments[segmentIndex].SegmentEndArrivalTimestamp + leg.Segments[segmentIndex].Delay;
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
        public SortedList<uint, TransportAnimationRecord> Path = new();
        public SortedList<uint, TransportRotationRecord> Rotations = new();
        public uint TotalTime;

        public TransportAnimationRecord GetPrevAnimNode(uint time)
        {
            if (Path.Empty())
                return null;

            int reqIndex = Path.IndexOfKey(time);
            if (reqIndex != -1)
                return Path.GetValueAtIndex(reqIndex - 1);

            return Path.LastOrDefault().Value;
        }

        public TransportRotationRecord GetPrevAnimRotation(uint time)
        {
            if (Rotations.Empty())
                return null;

            int reqIndex = Rotations.IndexOfKey(time);
            if (reqIndex != -1)
                return Rotations.GetValueAtIndex(reqIndex - 1);

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
