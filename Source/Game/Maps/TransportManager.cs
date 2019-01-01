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
using Game.DataStorage;
using Game.Entities;
using Game.Movement;
using System;
using System.Collections.Generic;
using System.Linq;

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
                TransportTemplate transport = new TransportTemplate();
                transport.entry = entry;
                GeneratePath(goInfo, transport);

                _transportTemplates[entry] = transport;

                // transports in instance are only on one map
                if (transport.inInstance)
                    _instanceTransports.Add(transport.mapsUsed.First(), entry);

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

        void GeneratePath(GameObjectTemplate goInfo, TransportTemplate transport)
        {
            uint pathId = goInfo.MoTransport.taxiPathID;
            var path = CliDB.TaxiPathNodesByPath[pathId];
            List<KeyFrame> keyFrames = transport.keyFrames;
            List<Vector3> splinePath = new List<Vector3>();
            List<Vector3> allPoints = new List<Vector3>();
            bool mapChange = false;

            for (uint i = 0; i < path.Length; ++i)
                allPoints.Add(new Vector3(path[i].Loc.X, path[i].Loc.Y, path[i].Loc.Z));

            // Add extra points to allow derivative calculations for all path nodes
            allPoints.Insert(0, allPoints.First().lerp(allPoints[1], -0.2f));
            allPoints.Add(allPoints.Last().lerp(allPoints[allPoints.Count - 2], -0.2f));
            allPoints.Add(allPoints.Last().lerp(allPoints[allPoints.Count - 2], -1.0f));

            SplineRawInitializer initer = new SplineRawInitializer(allPoints);
            Spline orientationSpline = new Spline();
            orientationSpline.init_spline_custom(initer);
            orientationSpline.initLengths();

            for (uint i = 0; i < path.Length; ++i)
            {
                if (!mapChange)
                {
                    var node_i = path[i];
                    if (i != path.Length - 1 && (node_i.Flags.HasAnyFlag(TaxiPathNodeFlags.Teleport) || node_i.ContinentID != path[i + 1].ContinentID))
                    {
                        keyFrames.Last().Teleport = true;
                        mapChange = true;
                    }
                    else
                    {
                        KeyFrame k = new KeyFrame(node_i);
                        Vector3 h;
                        orientationSpline.Evaluate_Derivative((int)(i + 1), 0.0f, out h);
                        k.InitialOrientation = Position.NormalizeOrientation((float)Math.Atan2(h.Y, h.X) + MathFunctions.PI);

                        keyFrames.Add(k);
                        splinePath.Add(new Vector3(node_i.Loc.X, node_i.Loc.Y, node_i.Loc.Z));
                        if (!transport.mapsUsed.Contains(k.Node.ContinentID))
                            transport.mapsUsed.Add(k.Node.ContinentID);
                    }
                }
                else
                    mapChange = false;
            }

            if (splinePath.Count >= 2)
            {
                // Remove special catmull-rom spline points
                if (!keyFrames.First().IsStopFrame() && keyFrames.First().Node.ArrivalEventID == 0 && keyFrames.First().Node.DepartureEventID == 0)
                {
                    splinePath.RemoveAt(0);
                    keyFrames.RemoveAt(0);
                }
                if (!keyFrames.Last().IsStopFrame() && keyFrames.Last().Node.ArrivalEventID == 0 && keyFrames.Last().Node.DepartureEventID == 0)
                {
                    splinePath.RemoveAt(splinePath.Count - 1);
                    keyFrames.RemoveAt(keyFrames.Count - 1);
                }
            }

            Cypher.Assert(!keyFrames.Empty());

            if (transport.mapsUsed.Count > 1)
            {
                foreach (var mapId in transport.mapsUsed)
                    Cypher.Assert(!CliDB.MapStorage.LookupByKey(mapId).Instanceable());

                transport.inInstance = false;
            }
            else
                transport.inInstance = CliDB.MapStorage.LookupByKey(transport.mapsUsed.First()).Instanceable();

            // last to first is always "teleport", even for closed paths
            keyFrames.Last().Teleport = true;

            float speed = goInfo.MoTransport.moveSpeed;
            float accel = goInfo.MoTransport.accelRate;
            float accel_dist = 0.5f * speed * speed / accel;

            transport.accelTime = speed / accel;
            transport.accelDist = accel_dist;

            int firstStop = -1;
            int lastStop = -1;

            // first cell is arrived at by teleportation :S
            keyFrames[0].DistFromPrev = 0;
            keyFrames[0].Index = 1;
            if (keyFrames[0].IsStopFrame())
            {
                firstStop = 0;
                lastStop = 0;
            }

            // find the rest of the distances between key points
            // Every path segment has its own spline
            int start = 0;
            for (int i = 1; i < keyFrames.Count; ++i)
            {
                if (keyFrames[i - 1].Teleport || i + 1 == keyFrames.Count)
                {
                    int extra = !keyFrames[i - 1].Teleport ? 1 : 0;
                    Spline spline = new Spline();
                    Span<Vector3> span = splinePath.ToArray();
                    spline.Init_Spline(span.Slice(start), i - start + extra, Spline.EvaluationMode.Catmullrom);
                    spline.initLengths();
                    for (int j = start; j < i + extra; ++j)
                    {
                        keyFrames[j].Index = (uint)(j - start + 1);
                        keyFrames[j].DistFromPrev = spline.length(j - start, j + 1 - start);
                        if (j > 0)
                            keyFrames[j - 1].NextDistFromPrev = keyFrames[j].DistFromPrev;
                        keyFrames[j].Spline = spline;
                    }

                    if (keyFrames[i - 1].Teleport)
                    {
                        keyFrames[i].Index = (uint)(i - start + 1);
                        keyFrames[i].DistFromPrev = 0.0f;
                        keyFrames[i - 1].NextDistFromPrev = 0.0f;
                        keyFrames[i].Spline = spline;
                    }

                    start = i;
                }
                if (keyFrames[i].IsStopFrame())
                {
                    // remember first stop frame
                    if (firstStop == -1)
                        firstStop = i;
                    lastStop = i;
                }
            }

            keyFrames.Last().NextDistFromPrev = keyFrames.First().DistFromPrev;

            if (firstStop == -1 || lastStop == -1)
                firstStop = lastStop = 0;

            // at stopping keyframes, we define distSinceStop == 0,
            // and distUntilStop is to the next stopping keyframe.
            // this is required to properly handle cases of two stopping frames in a row (yes they do exist)
            float tmpDist = 0.0f;
            for (int i = 0; i < keyFrames.Count; ++i)
            {
                int j = (i + lastStop) % keyFrames.Count;
                if (keyFrames[j].IsStopFrame() || j == lastStop)
                    tmpDist = 0.0f;
                else
                    tmpDist += keyFrames[j].DistFromPrev;
                keyFrames[j].DistSinceStop = tmpDist;
            }

            tmpDist = 0.0f;
            for (int i = (keyFrames.Count - 1); i >= 0; i--)
            {
                int j = (i + firstStop) % keyFrames.Count;
                tmpDist += keyFrames[(j + 1) % keyFrames.Count].DistFromPrev;
                keyFrames[j].DistUntilStop = tmpDist;
                if (keyFrames[j].IsStopFrame() || j == firstStop)
                    tmpDist = 0.0f;
            }

            for (int i = 0; i < keyFrames.Count; ++i)
            {
                float total_dist = keyFrames[i].DistSinceStop + keyFrames[i].DistUntilStop;
                if (total_dist < 2 * accel_dist) // won't reach full speed
                {
                    if (keyFrames[i].DistSinceStop < keyFrames[i].DistUntilStop) // is still accelerating
                    {
                        // calculate accel+brake time for this short segment
                        float segment_time = 2.0f * (float)Math.Sqrt((keyFrames[i].DistUntilStop + keyFrames[i].DistSinceStop) / accel);
                        // substract acceleration time
                        keyFrames[i].TimeTo = segment_time - (float)Math.Sqrt(2 * keyFrames[i].DistSinceStop / accel);
                    }
                    else // slowing down
                        keyFrames[i].TimeTo = (float)Math.Sqrt(2 * keyFrames[i].DistUntilStop / accel);
                }
                else if (keyFrames[i].DistSinceStop < accel_dist) // still accelerating (but will reach full speed)
                {
                    // calculate accel + cruise + brake time for this long segment
                    float segment_time = (keyFrames[i].DistUntilStop + keyFrames[i].DistSinceStop) / speed + (speed / accel);
                    // substract acceleration time
                    keyFrames[i].TimeTo = segment_time - (float)Math.Sqrt(2 * keyFrames[i].DistSinceStop / accel);
                }
                else if (keyFrames[i].DistUntilStop < accel_dist) // already slowing down (but reached full speed)
                    keyFrames[i].TimeTo = (float)Math.Sqrt(2 * keyFrames[i].DistUntilStop / accel);
                else // at full speed
                    keyFrames[i].TimeTo = (keyFrames[i].DistUntilStop / speed) + (0.5f * speed / accel);
            }

            // calculate tFrom times from tTo times
            float segmentTime = 0.0f;
            for (int i = 0; i < keyFrames.Count; ++i)
            {
                int j = (i + lastStop) % keyFrames.Count;
                if (keyFrames[j].IsStopFrame() || j == lastStop)
                    segmentTime = keyFrames[j].TimeTo;
                keyFrames[j].TimeFrom = segmentTime - keyFrames[j].TimeTo;
            }

            // calculate path times
            keyFrames[0].ArriveTime = 0;
            float curPathTime = 0.0f;
            if (keyFrames[0].IsStopFrame())
            {
                curPathTime = keyFrames[0].Node.Delay;
                keyFrames[0].DepartureTime = (uint)(curPathTime * Time.InMilliseconds);
            }

            for (int i = 1; i < keyFrames.Count; ++i)
            {
                curPathTime += keyFrames[i - 1].TimeTo;
                if (keyFrames[i].IsStopFrame())
                {
                    keyFrames[i].ArriveTime = (uint)(curPathTime * Time.InMilliseconds);
                    keyFrames[i - 1].NextArriveTime = keyFrames[i].ArriveTime;
                    curPathTime += keyFrames[i].Node.Delay;
                    keyFrames[i].DepartureTime = (uint)(curPathTime * Time.InMilliseconds);
                }
                else
                {
                    curPathTime -= keyFrames[i].TimeTo;
                    keyFrames[i].ArriveTime = (uint)(curPathTime * Time.InMilliseconds);
                    keyFrames[i - 1].NextArriveTime = keyFrames[i].ArriveTime;
                    keyFrames[i].DepartureTime = keyFrames[i].ArriveTime;
                }
            }
            keyFrames.Last().NextArriveTime = keyFrames.Last().DepartureTime;

            transport.pathTime = keyFrames.Last().DepartureTime;
            if (transport.pathTime == 0)
            {

            }
        }

        public void AddPathNodeToTransport(uint transportEntry, uint timeSeg, TransportAnimationRecord node)
        {
            TransportAnimation animNode = new TransportAnimation();
            if (animNode.TotalTime < timeSeg)
                animNode.TotalTime = timeSeg;

            animNode.Path[timeSeg] = node;

            _transportAnimations[transportEntry] = animNode;
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

            // create transport...
            Transport trans = new Transport();

            // ...at first waypoint
            TaxiPathNodeRecord startNode = tInfo.keyFrames.First().Node;
            uint mapId = startNode.ContinentID;
            float x = startNode.Loc.X;
            float y = startNode.Loc.Y;
            float z = startNode.Loc.Z;
            float o = tInfo.keyFrames.First().InitialOrientation;

            // initialize the gameobject base
            ulong guidLow = guid != 0 ? guid : map.GenerateLowGuid(HighGuid.Transport);
            if (!trans.Create(guidLow, entry, mapId, x, y, z, o, 255))
                return null;

            PhasingHandler.InitDbPhaseShift(trans.GetPhaseShift(), phaseUseFlags, phaseId, phaseGroupId);

            MapRecord mapEntry = CliDB.MapStorage.LookupByKey(mapId);
            if (mapEntry != null)
            {
                if (mapEntry.Instanceable() != tInfo.inInstance)
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

                    if (Convert.ToBoolean(phaseUseFlags & ~PhaseUseFlagsValues.All))
                    {
                        Log.outError(LogFilter.Sql, $"Table `transports` have transport (GUID: {guid} Entry: {entry}) with unknown `phaseUseFlags` set, removed unknown value.");
                        phaseUseFlags &= PhaseUseFlagsValues.All;
                    }

                    if (phaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.AlwaysVisible) && phaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.Inverse))
                    {
                        Log.outError(LogFilter.Sql, $"Table `transports` have transport (GUID: {guid} Entry: {entry}) has both `phaseUseFlags` PHASE_USE_FLAGS_ALWAYS_VISIBLE and PHASE_USE_FLAGS_INVERSE," +
                            " removing PHASE_USE_FLAGS_INVERSE.");
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
                        if (Global.DB2Mgr.GetPhasesForGroup(phaseGroupId).Empty())
                        {
                            Log.outError(LogFilter.Sql, $"Table `transports` have transport (GUID: {guid} Entry: {entry}) with `phaseGroup` {phaseGroupId} does not exist, set to 0");
                            phaseGroupId = 0;
                        }
                    }

                    TransportTemplate tInfo = GetTransportTemplate(entry);
                    if (tInfo != null)
                        if (!tInfo.inInstance)
                            if (CreateTransport(entry, guid, null, phaseUseFlags, phaseId, phaseGroupId))
                                ++count;

                } while (result.NextRow());
            }

            Log.outInfo(LogFilter.ServerLoading, "Spawned {0} continent transports in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void CreateInstanceTransports(Map map)
        {
            var mapTransports = _instanceTransports.LookupByKey(map.GetId());

            // no transports here
            if (mapTransports.Empty())
                return;

            // create transports
            foreach (var entry in mapTransports)
                CreateTransport(entry, 0, map);
        }

        public TransportTemplate GetTransportTemplate(uint entry)
        {
            return _transportTemplates.LookupByKey(entry);
        }

        public TransportAnimation GetTransportAnimInfo(uint entry)
        {
            return _transportAnimations.LookupByKey(entry);
        }

        public void AddPathRotationToTransport(uint transportEntry, uint timeSeg, TransportRotationRecord node)
        {
            if (!_transportAnimations.ContainsKey(transportEntry))
                _transportAnimations[transportEntry] = new TransportAnimation();

            _transportAnimations[transportEntry].Rotations[timeSeg] = node;
        }

        Dictionary<uint, TransportTemplate> _transportTemplates = new Dictionary<uint, TransportTemplate>();
        MultiMap<uint, uint> _instanceTransports = new MultiMap<uint, uint>();
        Dictionary<uint, TransportAnimation> _transportAnimations = new Dictionary<uint, TransportAnimation>();
    }

    public class SplineRawInitializer
    {
        public SplineRawInitializer(List<Vector3> points)
        {
            _points = points;
        }

        public void Initialize(ref Spline.EvaluationMode mode, ref bool cyclic, ref Vector3[] points, ref int lo, ref int hi)
        {
            mode = Spline.EvaluationMode.Catmullrom;
            cyclic = false;
            points = new Vector3[_points.Count];
            
            for(var i = 0; i < _points.Count; ++i)
                points[i] = _points[i];

            lo = 1;
            hi = points.Length - 2;
        }

        List<Vector3> _points;
    }

    public class KeyFrame
    {
        public KeyFrame(TaxiPathNodeRecord _node)
        {
            Node = _node;
            DistSinceStop = -1.0f;
            DistUntilStop = -1.0f;
            DistFromPrev = -1.0f;
            TimeFrom = 0.0f;
            TimeTo = 0.0f;
            Teleport = false;
            ArriveTime = 0;
            DepartureTime = 0;
            Spline = null;
            NextDistFromPrev = 0.0f;
            NextArriveTime = 0;
        }

        public uint Index;
        public TaxiPathNodeRecord Node;
        public float InitialOrientation;
        public float DistSinceStop;
        public float DistUntilStop;
        public float DistFromPrev;
        public float TimeFrom;
        public float TimeTo;
        public bool Teleport;
        public uint ArriveTime;
        public uint DepartureTime;
        public Spline Spline;

        // Data needed for next frame
        public float NextDistFromPrev;
        public uint NextArriveTime;

        public bool IsTeleportFrame() { return Teleport; }
        public bool IsStopFrame() { return Node.Flags.HasAnyFlag(TaxiPathNodeFlags.Stop); }
    }

    public class TransportTemplate
    {
        public TransportTemplate()
        {
            pathTime = 0;
            accelTime = 0.0f;
            accelDist = 0.0f;
        }

        public List<uint> mapsUsed = new List<uint>();
        public bool inInstance;
        public uint pathTime;
        public List<KeyFrame> keyFrames = new List<KeyFrame>();
        public float accelTime;
        public float accelDist;
        public uint entry;
    }

    public class TransportAnimation
    {
        TransportAnimationRecord GetAnimNode(uint time)
        {
            if (Path.Empty())
                return null;

            foreach (var pair in Path)
                if (time >= pair.Key)
                    return pair.Value;

            return Path.First().Value;
        }

        TransportRotationRecord GetAnimRotation(uint time)
        {
            return Rotations.LookupByKey(time);
        }

        public Dictionary<uint, TransportAnimationRecord> Path = new Dictionary<uint, TransportAnimationRecord>();
        public Dictionary<uint, TransportRotationRecord> Rotations = new Dictionary<uint, TransportRotationRecord>();
        public uint TotalTime;
    }
}
