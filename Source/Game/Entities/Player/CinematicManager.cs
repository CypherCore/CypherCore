// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.DataStorage;

namespace Game.Entities
{
	public class CinematicManager : IDisposable
	{
		public CinematicSequencesRecord _activeCinematic;
		public int _activeCinematicCameraIndex;
		public List<FlyByCamera> _cinematicCamera;

		public uint _cinematicDiff;
		public uint _cinematicLength;
		private TempSummon _CinematicObject;
		public uint _lastCinematicCheck;

		private Position _remoteSightPosition;

		// Remote location information
		private Player player;

		public CinematicManager(Player playerref)
		{
			player                      = playerref;
			_activeCinematicCameraIndex = -1;
			_remoteSightPosition        = new Position(0.0f, 0.0f, 0.0f);
		}

		public virtual void Dispose()
		{
			if (_cinematicCamera != null &&
			    _activeCinematic != null)
				EndCinematic();
		}

		public void BeginCinematic(CinematicSequencesRecord cinematic)
		{
			_activeCinematic            = cinematic;
			_activeCinematicCameraIndex = -1;
		}

		public void NextCinematicCamera()
		{
			// Sanity check for active camera set
			if (_activeCinematic == null ||
			    _activeCinematicCameraIndex >= _activeCinematic.Camera.Length)
				return;

			uint cinematicCameraId = _activeCinematic.Camera[++_activeCinematicCameraIndex];

			if (cinematicCameraId == 0)
				return;

			var flyByCameras = M2Storage.GetFlyByCameras(cinematicCameraId);

			if (!flyByCameras.Empty())
			{
				// Initialize diff, and set camera
				_cinematicDiff   = 0;
				_cinematicCamera = flyByCameras;

				if (!_cinematicCamera.Empty())
				{
					FlyByCamera firstCamera = _cinematicCamera.FirstOrDefault();
					Position    pos         = new(firstCamera.locations.X, firstCamera.locations.Y, firstCamera.locations.Z, firstCamera.locations.W);

					if (!pos.IsPositionValid())
						return;

					player.GetMap().LoadGridForActiveObject(pos.GetPositionX(), pos.GetPositionY(), player);
					_CinematicObject = player.SummonCreature(1, pos.posX, pos.posY, pos.posZ, 0.0f, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(5));

					if (_CinematicObject)
					{
						_CinematicObject.SetActive(true);
						player.SetViewpoint(_CinematicObject, true);
					}

					// Get cinematic length
					_cinematicLength = _cinematicCamera.LastOrDefault().timeStamp;
				}
			}
		}

		public void EndCinematic()
		{
			if (_activeCinematic == null)
				return;

			_cinematicDiff              = 0;
			_cinematicCamera            = null;
			_activeCinematic            = null;
			_activeCinematicCameraIndex = -1;

			if (_CinematicObject)
			{
				WorldObject vpObject = player.GetViewpoint();

				if (vpObject)
					if (vpObject == _CinematicObject)
						player.SetViewpoint(_CinematicObject, false);

				_CinematicObject.AddObjectToRemoveList();
			}
		}

		public void UpdateCinematicLocation(uint diff)
		{
			if (_activeCinematic == null ||
			    _activeCinematicCameraIndex == -1 ||
			    _cinematicCamera == null ||
			    _cinematicCamera.Count == 0)
				return;

			Position lastPosition  = new();
			uint     lastTimestamp = 0;
			Position nextPosition  = new();
			uint     nextTimestamp = 0;

			// Obtain direction of travel
			foreach (FlyByCamera cam in _cinematicCamera)
			{
				if (cam.timeStamp > _cinematicDiff)
				{
					nextPosition  = new Position(cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W);
					nextTimestamp = cam.timeStamp;

					break;
				}

				lastPosition  = new Position(cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W);
				lastTimestamp = cam.timeStamp;
			}

			float angle = lastPosition.GetAbsoluteAngle(nextPosition);
			angle -= lastPosition.GetOrientation();

			if (angle < 0)
				angle += 2 * MathFunctions.PI;

			// Look for position around 2 second ahead of us.
			int workDiff = (int)_cinematicDiff;

			// Modify result based on camera direction (Humans for example, have the camera point behind)
			workDiff += (int)((2 * Time.InMilliseconds) * Math.Cos(angle));

			// Get an iterator to the last entry in the cameras, to make sure we don't go beyond the end
			var endItr = _cinematicCamera.LastOrDefault();

			if (endItr != null &&
			    workDiff > endItr.timeStamp)
				workDiff = (int)endItr.timeStamp;

			// Never try to go back in time before the start of cinematic!
			if (workDiff < 0)
				workDiff = (int)_cinematicDiff;

			// Obtain the previous and next waypoint based on timestamp
			foreach (FlyByCamera cam in _cinematicCamera)
			{
				if (cam.timeStamp >= workDiff)
				{
					nextPosition  = new Position(cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W);
					nextTimestamp = cam.timeStamp;

					break;
				}

				lastPosition  = new Position(cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W);
				lastTimestamp = cam.timeStamp;
			}

			// Never try to go beyond the end of the cinematic
			if (workDiff > nextTimestamp)
				workDiff = (int)nextTimestamp;

			// Interpolate the position for this moment in time (or the adjusted moment in time)
			uint  timeDiff  = nextTimestamp - lastTimestamp;
			uint  interDiff = (uint)(workDiff - lastTimestamp);
			float xDiff     = nextPosition.posX - lastPosition.posX;
			float yDiff     = nextPosition.posY - lastPosition.posY;
			float zDiff     = nextPosition.posZ - lastPosition.posZ;

			Position interPosition = new(lastPosition.posX + (xDiff * ((float)interDiff / timeDiff)),
			                             lastPosition.posY +
			                             (yDiff * ((float)interDiff / timeDiff)),
			                             lastPosition.posZ + (zDiff * ((float)interDiff / timeDiff)));

			// Advance (at speed) to this position. The remote sight object is used
			// to send update information to player in cinematic
			if (_CinematicObject && interPosition.IsPositionValid())
				_CinematicObject.MonsterMoveWithSpeed(interPosition.posX, interPosition.posY, interPosition.posZ, 500.0f, false, true);

			// If we never received an end packet 10 seconds after the final timestamp then force an end
			if (_cinematicDiff > _cinematicLength + 10 * Time.InMilliseconds)
				EndCinematic();
		}

		public bool IsOnCinematic()
		{
			return _cinematicCamera != null;
		}
	}
}