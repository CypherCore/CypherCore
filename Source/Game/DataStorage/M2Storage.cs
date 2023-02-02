// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Game.DataStorage
{
    public class M2Storage
    {
        // Convert the geomoetry from a spline value, to an actual WoW XYZ
        static Vector3 TranslateLocation(Vector4 dbcLocation, Vector3 basePosition, Vector3 splineVector)
        {
            Vector3 work = new();
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
        static void ReadCamera(M2Camera cam, BinaryReader reader, CinematicCameraRecord dbcentry)
        {
            List<FlyByCamera> cameras = new();
            List<FlyByCamera> targetcam = new();

            Vector4 dbcData = new(dbcentry.Origin.X, dbcentry.Origin.Y, dbcentry.Origin.Z, dbcentry.OriginFacing);

            // Read target locations, only so that we can calculate orientation
            for (uint k = 0; k < cam.target_positions.timestamps.number; ++k)
            {
                // Extract Target positions
                reader.BaseStream.Position = cam.target_positions.timestamps.offset_elements;
                M2Array targTsArray = reader.Read<M2Array>();

                reader.BaseStream.Position = targTsArray.offset_elements;
                uint[] targTimestamps = reader.ReadArray<uint>(targTsArray.number);

                reader.BaseStream.Position = cam.target_positions.values.offset_elements;
                M2Array targArray = reader.Read<M2Array>();

                reader.BaseStream.Position = targArray.offset_elements;
                M2SplineKey[] targPositions = new M2SplineKey[targArray.number];
                for (var i = 0; i < targArray.number; ++i)
                    targPositions[i] = new M2SplineKey(reader);

                // Read the data for this set
                for (uint i = 0; i < targTsArray.number; ++i)
                {
                    // Translate co-ordinates
                    Vector3 newPos = TranslateLocation(dbcData, cam.target_position_base, targPositions[i].p0);

                    // Add to vector
                    FlyByCamera thisCam = new();
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
                M2Array posTsArray = reader.Read<M2Array>();

                reader.BaseStream.Position = posTsArray.offset_elements;
                uint[] posTimestamps = reader.ReadArray<uint>(posTsArray.number);

                reader.BaseStream.Position = cam.positions.values.offset_elements;
                M2Array posArray = reader.Read<M2Array>();

                reader.BaseStream.Position = posArray.offset_elements;
                M2SplineKey[] positions = new M2SplineKey[posTsArray.number];
                for (var i = 0; i < posTsArray.number; ++i)
                    positions[i] = new M2SplineKey(reader);

                // Read the data for this set
                for (uint i = 0; i < posTsArray.number; ++i)
                {
                    // Translate co-ordinates
                    Vector3 newPos = TranslateLocation(dbcData, cam.position_base, positions[i].p0);

                    // Add to vector
                    FlyByCamera thisCam = new();
                    thisCam.timeStamp = posTimestamps[i];
                    thisCam.locations = new Vector4(newPos.X, newPos.Y, newPos.Z, 0);

                    if (targetcam.Count > 0)
                    {
                        // Find the target camera before and after this camera
                        // Pre-load first item
                        FlyByCamera lastTarget = targetcam[0];
                        FlyByCamera nextTarget = targetcam[0];
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
                            x = lastTarget.locations.X + (xDiff * ((float)timeDiffThis / timeDiffTarget));
                            y = lastTarget.locations.Y + (yDiff * ((float)timeDiffThis / timeDiffTarget));
                            z = lastTarget.locations.Z + (zDiff * ((float)timeDiffThis / timeDiffTarget));
                        }
                        float xDiff1 = x - thisCam.locations.X;
                        float yDiff1 = y - thisCam.locations.Y;
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

            uint oldMSTime = Time.GetMSTime();
            foreach (CinematicCameraRecord cameraEntry in CliDB.CinematicCameraStorage.Values)
            {
                string filename = dataPath + "/cameras/" + $"FILE{cameraEntry.FileDataID:X8}.xxx";

                try
                {
                    using BinaryReader m2file = new(new FileStream(filename, FileMode.Open, FileAccess.Read));
                    // Check file has correct magic (MD21)
                    if (m2file.ReadUInt32() != 0x3132444D) //"MD21"
                    {
                        Log.outError(LogFilter.ServerLoading, "Camera file {0} is damaged. File identifier not found.", filename);
                        continue;
                    }

                    m2file.ReadUInt32(); //unknown size

                    // Read header
                    M2Header header = m2file.Read<M2Header>();

                    // Get camera(s) - Main header, then dump them.
                    m2file.BaseStream.Position = 8 + header.ofsCameras;
                    M2Camera cam = m2file.Read<M2Camera>();

                    m2file.BaseStream.Position = 8;
                    ReadCamera(cam, new BinaryReader(new MemoryStream(m2file.ReadBytes((int)m2file.BaseStream.Length - 8))), cameraEntry);
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

        static MultiMap<uint, FlyByCamera> FlyByCameraStorage = new();
    }

    public class FlyByCamera
    {
        public uint timeStamp;
        public Vector4 locations;
    }
}
