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

using Framework.GameMath;
using System;
using System.Collections.Generic;
using System.IO;

namespace Game.DataStorage
{
    public class M2Storage
    {
        // Convert the geomoetry from a spline value, to an actual WoW XYZ
        private static Vector3 TranslateLocation(Vector4 dbcLocation, Vector3 basePosition, Vector3 splineVector)
        {
            var work = new Vector3();
            var x = basePosition.X + splineVector.X;
            var y = basePosition.Y + splineVector.Y;
            var z = basePosition.Z + splineVector.Z;
            var distance = (float)Math.Sqrt((x * x) + (y * y));
            var angle = (float)Math.Atan2(x, y) - dbcLocation.W;

            if (angle < 0)
                angle += 2 * MathFunctions.PI;

            work.X = dbcLocation.X + (distance * (float)Math.Sin(angle));
            work.Y = dbcLocation.Y + (distance * (float)Math.Cos(angle));
            work.Z = dbcLocation.Z + z;
            return work;
        }

        // Number of cameras not used. Multiple cameras never used in 7.1.5
        private static void ReadCamera(M2Camera cam, BinaryReader reader, CinematicCameraRecord dbcentry)
        {
            var cameras = new List<FlyByCamera>();
            var targetcam = new List<FlyByCamera>();

            var dbcData = new Vector4(dbcentry.Origin.X, dbcentry.Origin.Y, dbcentry.Origin.Z, dbcentry.OriginFacing);

            // Read target locations, only so that we can calculate orientation
            for (uint k = 0; k < cam.target_positions.timestamps.number; ++k)
            {
                // Extract Target positions
                reader.BaseStream.Position = cam.target_positions.timestamps.offset_elements;
                var targTsArray = reader.Read<M2Array>();

                reader.BaseStream.Position = targTsArray.offset_elements;
                var targTimestamps = reader.ReadArray<uint>(targTsArray.number);

                reader.BaseStream.Position = cam.target_positions.values.offset_elements;
                var targArray = reader.Read<M2Array>();

                reader.BaseStream.Position = targArray.offset_elements;
                var targPositions = new M2SplineKey[targArray.number];
                for (var i = 0; i < targArray.number; ++i)
                    targPositions[i] = new M2SplineKey(reader);

                // Read the data for this set
                for (uint i = 0; i < targTsArray.number; ++i)
                {
                    // Translate co-ordinates
                    var newPos = TranslateLocation(dbcData, cam.target_position_base, targPositions[i].p0);

                    // Add to vector
                    var thisCam = new FlyByCamera();
                    thisCam.timeStamp = targTimestamps[i];
                    thisCam.locations = new Vector4(newPos.X, newPos.Y, newPos.Z, 0.0f);
                    targetcam.Add(thisCam);
                }
            }

            // Read camera positions and timestamps (translating first position of 3 only, we don't need to translate the whole spline)
            for (uint k = 0; k < cam.positions.timestamps.number; ++k)
            {
                // Extract Camera positions for this set
                reader.BaseStream.Position = cam.positions.timestamps.offset_elements;
                var posTsArray = reader.Read<M2Array>();

                reader.BaseStream.Position = posTsArray.offset_elements;
                var posTimestamps = reader.ReadArray<uint>(posTsArray.number);

                reader.BaseStream.Position = cam.positions.values.offset_elements;
                var posArray = reader.Read<M2Array>();

                reader.BaseStream.Position = posArray.offset_elements;
                var positions = new M2SplineKey[posTsArray.number];
                for (var i = 0; i < posTsArray.number; ++i)
                    positions[i] = new M2SplineKey(reader);

                // Read the data for this set
                for (uint i = 0; i < posTsArray.number; ++i)
                {
                    // Translate co-ordinates
                    var newPos = TranslateLocation(dbcData, cam.position_base, positions[i].p0);

                    // Add to vector
                    var thisCam = new FlyByCamera();
                    thisCam.timeStamp = posTimestamps[i];
                    thisCam.locations = new Vector4(newPos.X, newPos.Y, newPos.Z, 0);

                    if (targetcam.Count > 0)
                    {
                        // Find the target camera before and after this camera
                        // Pre-load first item
                        var lastTarget = targetcam[0];
                        var nextTarget = targetcam[0];
                        for (var j = 0; j < targetcam.Count; ++j)
                        {
                            nextTarget = targetcam[j];
                            if (targetcam[j].timeStamp > posTimestamps[i])
                                break;

                            lastTarget = targetcam[j];
                        }

                        var x = lastTarget.locations.X;
                        var y = lastTarget.locations.Y;
                        var z = lastTarget.locations.Z;

                        // Now, the timestamps for target cam and position can be different. So, if they differ we interpolate
                        if (lastTarget.timeStamp != posTimestamps[i])
                        {
                            var timeDiffTarget = nextTarget.timeStamp - lastTarget.timeStamp;
                            var timeDiffThis = posTimestamps[i] - lastTarget.timeStamp;
                            var xDiff = nextTarget.locations.X - lastTarget.locations.X;
                            var yDiff = nextTarget.locations.Y - lastTarget.locations.Y;
                            var zDiff = nextTarget.locations.Z - lastTarget.locations.Z;
                            x = lastTarget.locations.X + (xDiff * ((float)timeDiffThis / timeDiffTarget));
                            y = lastTarget.locations.Y + (yDiff * ((float)timeDiffThis / timeDiffTarget));
                            z = lastTarget.locations.Z + (zDiff * ((float)timeDiffThis / timeDiffTarget));
                        }
                        var xDiff1 = x - thisCam.locations.X;
                        var yDiff1 = y - thisCam.locations.Y;
                        thisCam.locations.W = (float)Math.Atan2(yDiff1, xDiff1);

                        if (thisCam.locations.W < 0)
                            thisCam.locations.W += 2 * MathFunctions.PI;
                    }

                    cameras.Add(thisCam);
                }
            }

            FlyByCameraStorage[dbcentry.Id] = cameras;
        }

        public static void LoadM2Cameras(string dataPath)
        {
            FlyByCameraStorage.Clear();
            Log.outInfo(LogFilter.ServerLoading, "Loading Cinematic Camera files");

            var oldMSTime = Time.GetMSTime();
            foreach (var cameraEntry in CliDB.CinematicCameraStorage.Values)
            {
                var filename = dataPath + "/cameras/" + $"FILE{cameraEntry.FileDataID:x8}.xxx";

                try
                {
                    using (var m2file = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
                    {
                        // Check file has correct magic (MD21)
                        if (m2file.ReadUInt32() != 0x3132444D) //"MD21"
                        {
                            Log.outError(LogFilter.ServerLoading, "Camera file {0} is damaged. File identifier not found.", filename);
                            continue;
                        }

                        m2file.ReadUInt32(); //unknown size

                        // Read header
                        var header = m2file.Read<M2Header>();

                        // Get camera(s) - Main header, then dump them.
                        m2file.BaseStream.Position = 8 + header.ofsCameras;
                        var cam = m2file.Read<M2Camera>();

                        m2file.BaseStream.Position = 8;
                        ReadCamera(cam, new BinaryReader(new MemoryStream(m2file.ReadBytes((int)m2file.BaseStream.Length - 8))), cameraEntry);
                    }
                }
                catch (EndOfStreamException)
                {
                    Log.outError(LogFilter.ServerLoading, "Camera file {0} is damaged. Camera references position beyond file end", filename);
                }
                catch (FileNotFoundException)
                {
                    Log.outError(LogFilter.ServerLoading, "File {0} not found!!!!", filename);
                }
            }
            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} cinematic waypoint sets in {1} ms", FlyByCameraStorage.Keys.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public static List<FlyByCamera> GetFlyByCameras(uint cameraId)
        {
            return FlyByCameraStorage.LookupByKey(cameraId);
        }

        private static MultiMap<uint, FlyByCamera> FlyByCameraStorage = new MultiMap<uint, FlyByCamera>();
    }

    public class FlyByCamera
    {
        public uint timeStamp;
        public Vector4 locations;
    }
}
