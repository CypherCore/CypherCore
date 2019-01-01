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
using Game.DataStorage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public class CinematicManager : IDisposable
    {
        public CinematicManager(Player playerref)
        {
            player = playerref;
            m_cinematicDiff = 0;
            m_lastCinematicCheck = 0;
            m_activeCinematicCameraId = 0;
            m_cinematicLength = 0;
            m_cinematicCamera = null;
            m_remoteSightPosition = new Position(0.0f, 0.0f, 0.0f);
            m_CinematicObject = null;
        }

        public virtual void Dispose()
        {
            if (m_cinematicCamera != null && m_activeCinematicCameraId != 0)
                EndCinematic();
        }

        public void BeginCinematic()
        {
            // Sanity check for active camera set
            if (m_activeCinematicCameraId == 0)
                return;

            var list = M2Storage.GetFlyByCameras(m_activeCinematicCameraId);
            if (!list.Empty())
            {
                // Initialize diff, and set camera
                m_cinematicDiff = 0;
                m_cinematicCamera = list;

                if (!m_cinematicCamera.Empty())
                {
                    FlyByCamera firstCamera = m_cinematicCamera.FirstOrDefault();
                    Position pos = new Position(firstCamera.locations.X, firstCamera.locations.Y, firstCamera.locations.Z, firstCamera.locations.W);
                    if (!pos.IsPositionValid())
                        return;

                    player.GetMap().LoadGrid(firstCamera.locations.X, firstCamera.locations.Y);
                    m_CinematicObject = player.SummonCreature(1, pos.posX, pos.posY, pos.posZ, 0.0f, TempSummonType.TimedDespawn, 5 * Time.Minute * Time.InMilliseconds);
                    if (m_CinematicObject)
                    {
                        m_CinematicObject.setActive(true);
                        player.SetViewpoint(m_CinematicObject, true);
                    }

                    // Get cinematic length
                    m_cinematicLength = m_cinematicCamera.LastOrDefault().timeStamp;
                }
            }
        }

        public void EndCinematic()
        {
            if (m_activeCinematicCameraId == 0)
                return;

            m_cinematicDiff = 0;
            m_cinematicCamera = null;
            m_activeCinematicCameraId = 0;
            if (m_CinematicObject)
            {
                WorldObject vpObject = player.GetViewpoint();
                if (vpObject)
                    if (vpObject == m_CinematicObject)
                        player.SetViewpoint(m_CinematicObject, false);

                m_CinematicObject.AddObjectToRemoveList();
            }
        }

        public void UpdateCinematicLocation(uint diff)
        {
            if (m_activeCinematicCameraId == 0 || m_cinematicCamera == null || m_cinematicCamera.Count == 0)
                return;

            Position lastPosition = new Position();
            uint lastTimestamp = 0;
            Position nextPosition = new Position();
            uint nextTimestamp = 0;

            // Obtain direction of travel
            foreach (FlyByCamera cam in m_cinematicCamera)
            {
                if (cam.timeStamp > m_cinematicDiff)
                {
                    nextPosition = new Position(cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W);
                    nextTimestamp = cam.timeStamp;
                    break;
                }
                lastPosition = new Position(cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W);
                lastTimestamp = cam.timeStamp;
            }
            float angle = lastPosition.GetAngle(nextPosition);
            angle -= lastPosition.GetOrientation();
            if (angle < 0)
                angle += 2 * MathFunctions.PI;

            // Look for position around 2 second ahead of us.
            int workDiff = (int)m_cinematicDiff;

            // Modify result based on camera direction (Humans for example, have the camera point behind)
            workDiff += (int)((2 * Time.InMilliseconds) * Math.Cos(angle));

            // Get an iterator to the last entry in the cameras, to make sure we don't go beyond the end
            var endItr = m_cinematicCamera.LastOrDefault();
            if (endItr != null && workDiff > endItr.timeStamp)
                workDiff = (int)endItr.timeStamp;

            // Never try to go back in time before the start of cinematic!
            if (workDiff < 0)
                workDiff = (int)m_cinematicDiff;

            // Obtain the previous and next waypoint based on timestamp
            foreach (FlyByCamera cam in m_cinematicCamera)
            {
                if (cam.timeStamp >= workDiff)
                {
                    nextPosition = new Position(cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W);
                    nextTimestamp = cam.timeStamp;
                    break;
                }
                lastPosition = new Position(cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W);
                lastTimestamp = cam.timeStamp;
            }

            // Never try to go beyond the end of the cinematic
            if (workDiff > nextTimestamp)
                workDiff = (int)nextTimestamp;

            // Interpolate the position for this moment in time (or the adjusted moment in time)
            uint timeDiff = nextTimestamp - lastTimestamp;
            uint interDiff = (uint)(workDiff - lastTimestamp);
            float xDiff = nextPosition.posX - lastPosition.posX;
            float yDiff = nextPosition.posY - lastPosition.posY;
            float zDiff = nextPosition.posZ - lastPosition.posZ;
            Position interPosition = new Position(lastPosition.posX + (xDiff * ((float)interDiff / timeDiff)), lastPosition.posY +
                (yDiff * ((float)interDiff / timeDiff)), lastPosition.posZ + (zDiff * ((float)interDiff / timeDiff)));

            // Advance (at speed) to this position. The remote sight object is used
            // to send update information to player in cinematic
            if (m_CinematicObject && interPosition.IsPositionValid())
                m_CinematicObject.MonsterMoveWithSpeed(interPosition.posX, interPosition.posY, interPosition.posZ, 500.0f, false, true);

            // If we never received an end packet 10 seconds after the final timestamp then force an end
            if (m_cinematicDiff > m_cinematicLength + 10 * Time.InMilliseconds)
                EndCinematic();
        }

        uint GetActiveCinematicCamera() { return m_activeCinematicCameraId; }
        public void SetActiveCinematicCamera(uint cinematicCameraId = 0) { m_activeCinematicCameraId = cinematicCameraId; }
        public bool IsOnCinematic() { return (m_cinematicCamera != null); }

        // Remote location information
        Player player;

        public uint m_cinematicDiff;
        public uint m_lastCinematicCheck;
        public uint m_activeCinematicCameraId;
        public uint m_cinematicLength;
        List<FlyByCamera> m_cinematicCamera;
        Position m_remoteSightPosition;
        TempSummon m_CinematicObject;
    }
}
