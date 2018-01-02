/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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
using System.Runtime.InteropServices;

namespace Game.DataStorage
{
    public class M2Storage
    {
        // Convert the geomoetry from a spline value, to an actual WoW XYZ
        static Vector3 translateLocation(Vector4 dbcLocation, Vector3 basePosition, Vector3 splineVector)
        {
            Vector3 work = new Vector3();
            float x = basePosition.X + splineVector.X;
            float y = basePosition.Y + splineVector.Y;
            float z = basePosition.Z + splineVector.Z;
            float distance = (float)Math.Sqrt((x * x) + (y * y));
            float angle = (float)Math.Atan2(x, y) - dbcLocation.W;

            if (angle < 0)
                angle += 2 * MathFunctions.PI;

            work.X = dbcLocation.X + (distance * (float)Math.Sin(angle));
            work.Y = dbcLocation.Y + (distance * (float)Math.Cos(angle));
            work.Z = dbcLocation.Z + z;
            return work;
        }

        // Number of cameras not used. Multiple cameras never used in 7.1.5
        static void readCamera(M2Camera cam, uint buffSize, BinaryReader buffer, CinematicCameraRecord dbcentry)
        {
            List<FlyByCamera> cameras = new List<FlyByCamera>();
            List<FlyByCamera> targetcam = new List<FlyByCamera>();

            Vector4 dbcData = new Vector4(dbcentry.Origin.X, dbcentry.Origin.Y, dbcentry.Origin.Z, dbcentry.OriginFacing);

            // Read target locations, only so that we can calculate orientation
            for (uint k = 0; k < cam.target_positions.timestamps.number; ++k)
            {
                // Extract Target positions
                M2Array targTsArray = new M2Array(buffer, cam.target_positions.timestamps.offset_elements);

                buffer.BaseStream.Position = targTsArray.offset_elements;
                uint[] targTimestamps = new uint[targTsArray.number];
                for (var i = 0; i < targTsArray.number; ++i)
                    targTimestamps[i] = buffer.ReadUInt32();

                M2Array targArray = new M2Array(buffer, cam.target_positions.values.offset_elements);

                buffer.BaseStream.Position = targArray.offset_elements;
                M2SplineKey[] targPositions = new M2SplineKey[targArray.number];
                for (var i = 0; i < targArray.number; ++i)
                    targPositions[i] = new M2SplineKey(buffer);

                // Read the data for this set
                uint currPos = targArray.offset_elements;
                for (uint i = 0; i < targTsArray.number; ++i)
                {
                    // Translate co-ordinates
                    Vector3 newPos = translateLocation(dbcData, cam.target_position_base, targPositions[i].p0);

                    // Add to vector
                    FlyByCamera thisCam = new FlyByCamera();
                    thisCam.timeStamp = targTimestamps[i];
                    thisCam.locations = new Vector4(newPos.X, newPos.Y, newPos.Z, 0.0f);
                    targetcam.Add(thisCam);
                    currPos += (uint)Marshal.SizeOf<M2SplineKey>();
                }
            }

            // Read camera positions and timestamps (translating first position of 3 only, we don't need to translate the whole spline)
            for (uint k = 0; k < cam.positions.timestamps.number; ++k)
            {
                // Extract Camera positions for this set
                M2Array posTsArray = buffer.ReadStruct<M2Array>(cam.positions.timestamps.offset_elements);

                buffer.BaseStream.Position = posTsArray.offset_elements;
                uint[] posTimestamps = new uint[posTsArray.number];
                for (var i = 0; i < posTsArray.number; ++i)
                    posTimestamps[i] = buffer.ReadUInt32();

                M2Array posArray = new M2Array(buffer, cam.positions.values.offset_elements);

                buffer.BaseStream.Position = posArray.offset_elements;
                M2SplineKey[] positions = new M2SplineKey[posTsArray.number];
                for (var i = 0; i < posTsArray.number; ++i)
                    positions[i] = new M2SplineKey(buffer);

                // Read the data for this set
                uint currPos = posArray.offset_elements;
                for (uint i = 0; i < posTsArray.number; ++i)
                {
                    // Translate co-ordinates
                    Vector3 newPos = translateLocation(dbcData, cam.position_base, positions[i].p0);

                    // Add to vector
                    FlyByCamera thisCam = new FlyByCamera();
                    thisCam.timeStamp = posTimestamps[i];
                    thisCam.locations = new Vector4(newPos.X, newPos.Y, newPos.Z, 0);

                    if (targetcam.Count > 0)
                    {
                        // Find the target camera before and after this camera
                        FlyByCamera lastTarget;
                        FlyByCamera nextTarget;

                        // Pre-load first item
                        lastTarget = targetcam[0];
                        nextTarget = targetcam[0];
                        for (int j = 0; j < targetcam.Count; ++j)
                        {
                            nextTarget = targetcam[j];
                            if (targetcam[j].timeStamp > posTimestamps[i])
                                break;

                            lastTarget = targetcam[j];
                        }

                        float x = lastTarget.locations.X;
                        float y = lastTarget.locations.Y;
                        float z = lastTarget.locations.Z;

                        // Now, the timestamps for target cam and position can be different. So, if they differ we interpolate
                        if (lastTarget.timeStamp != posTimestamps[i])
                        {
                            uint timeDiffTarget = nextTarget.timeStamp - lastTarget.timeStamp;
                            uint timeDiffThis = posTimestamps[i] - lastTarget.timeStamp;
                            float xDiff = nextTarget.locations.X - lastTarget.locations.X;
                            float yDiff = nextTarget.locations.Y - lastTarget.locations.Y;
                            float zDiff = nextTarget.locations.Z - lastTarget.locations.Z;
                            x = lastTarget.locations.X + (xDiff * (timeDiffThis / timeDiffTarget));
                            y = lastTarget.locations.Y + (yDiff * (timeDiffThis / timeDiffTarget));
                            z = lastTarget.locations.Z + (zDiff * (timeDiffThis / timeDiffTarget));
                        }
                        float xDiff1 = x - thisCam.locations.X;
                        float yDiff1 = y - thisCam.locations.Y;
                        thisCam.locations.W = (float)Math.Atan2(yDiff1, xDiff1);

                        if (thisCam.locations.W < 0)
                            thisCam.locations.W += 2 * MathFunctions.PI;
                    }

                    cameras.Add(thisCam);
                    currPos += (uint)Marshal.SizeOf<M2SplineKey>();
                }
            }

            FlyByCameraStorage[dbcentry.ID] = cameras;
        }

        public static void LoadM2Cameras(string dataPath)
        {
            FlyByCameraStorage.Clear();
            Log.outInfo(LogFilter.ServerLoading, "Loading Cinematic Camera files");

            uint oldMSTime = Time.GetMSTime();
            foreach (CinematicCameraRecord cameraEntry in CliDB.CinematicCameraStorage.Values)
            {
                string filename = dataPath + "/cameras/" + string.Format("FILE{0:x8}.xxx", cameraEntry.ModelFileDataID);

                try
                {
                    using (BinaryReader m2file = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
                    {
                        // Check file has correct magic (MD21)
                        if (m2file.ReadStringFromChars(4) != "MD21")
                        {
                            Log.outError(LogFilter.ServerLoading, "Camera file {0} is damaged. File identifier not found.", filename);
                            continue;
                        }

                        var unknownSize = m2file.ReadUInt32(); //unknown size

                        // Read header
                        M2Header header = m2file.ReadStruct<M2Header>();
                        var buffer = new BinaryReader(new MemoryStream(m2file.ToByteArray(), 8, (int)(m2file.BaseStream.Length - 8)));

                        // Get camera(s) - Main header, then dump them.
                        M2Camera cam = m2file.ReadStruct<M2Camera>(8 + header.ofsCameras);

                        readCamera(cam, (uint)m2file.BaseStream.Length - 8, buffer, cameraEntry);
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
            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} cinematic waypoint sets in {1} ms", FlyByCameraStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public static List<FlyByCamera> GetFlyByCameras(uint cameraId)
        {
            return FlyByCameraStorage.LookupByKey(cameraId);
        }

        static MultiMap<uint, FlyByCamera> FlyByCameraStorage = new MultiMap<uint, FlyByCamera>();
    }

    public class FlyByCamera
    {
        public uint timeStamp;
        public Vector4 locations;
    }
}
