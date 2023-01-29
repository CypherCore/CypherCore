// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.DataStorage;
using Game.Maps;

namespace Game.Entities
{

    namespace GameObjectType
    {
        //11 GAMEOBJECT_TYPE_TRANSPORT
        internal class Transport : GameObjectTypeBase, ITransport
        {
            private static readonly TimeSpan PositionUpdateInterval = TimeSpan.FromMilliseconds(50);
            private readonly TransportAnimation _animationInfo;
            private readonly List<WorldObject> _passengers = new();
            private readonly TimeTracker _positionUpdateTimer = new();
            private readonly List<uint> _stopFrames = new();
            private bool _autoCycleBetweenStopFrames;
            private uint _pathProgress;
            private uint _stateChangeProgress;
            private uint _stateChangeTime;

            public Transport(GameObject owner) : base(owner)
            {
                _animationInfo = Global.TransportMgr.GetTransportAnimInfo(owner.GetGoInfo().entry);
                _pathProgress = GameTime.GetGameTimeMS() % GetTransportPeriod();
                _stateChangeTime = GameTime.GetGameTimeMS();
                _stateChangeProgress = _pathProgress;

                GameObjectTemplate goInfo = Owner.GetGoInfo();

                if (goInfo.Transport.Timeto2ndfloor > 0)
                {
                    _stopFrames.Add(goInfo.Transport.Timeto2ndfloor);

                    if (goInfo.Transport.Timeto3rdfloor > 0)
                    {
                        _stopFrames.Add(goInfo.Transport.Timeto3rdfloor);

                        if (goInfo.Transport.Timeto4thfloor > 0)
                        {
                            _stopFrames.Add(goInfo.Transport.Timeto4thfloor);

                            if (goInfo.Transport.Timeto5thfloor > 0)
                            {
                                _stopFrames.Add(goInfo.Transport.Timeto5thfloor);

                                if (goInfo.Transport.Timeto6thfloor > 0)
                                {
                                    _stopFrames.Add(goInfo.Transport.Timeto6thfloor);

                                    if (goInfo.Transport.Timeto7thfloor > 0)
                                    {
                                        _stopFrames.Add(goInfo.Transport.Timeto7thfloor);

                                        if (goInfo.Transport.Timeto8thfloor > 0)
                                        {
                                            _stopFrames.Add(goInfo.Transport.Timeto8thfloor);

                                            if (goInfo.Transport.Timeto9thfloor > 0)
                                            {
                                                _stopFrames.Add(goInfo.Transport.Timeto9thfloor);

                                                if (goInfo.Transport.Timeto10thfloor > 0)
                                                    _stopFrames.Add(goInfo.Transport.Timeto10thfloor);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (!_stopFrames.Empty())
                {
                    _pathProgress = 0;
                    _stateChangeProgress = 0;
                }

                _positionUpdateTimer.Reset(PositionUpdateInterval);
            }

            public ObjectGuid GetTransportGUID()
            {
                return Owner.GetGUID();
            }

            public float GetTransportOrientation()
            {
                return Owner.GetOrientation();
            }

            public void AddPassenger(WorldObject passenger)
            {
                if (!Owner.IsInWorld)
                    return;

                if (!_passengers.Contains(passenger))
                {
                    _passengers.Add(passenger);
                    passenger.SetTransport(this);
                    passenger.MovementInfo.Transport.Guid = GetTransportGUID();
                    Log.outDebug(LogFilter.Transport, $"Object {passenger.GetName()} boarded Transport {Owner.GetName()}.");
                }
            }

            public ITransport RemovePassenger(WorldObject passenger)
            {
                if (_passengers.Remove(passenger))
                {
                    passenger.SetTransport(null);
                    passenger.MovementInfo.Transport.Reset();
                    Log.outDebug(LogFilter.Transport, $"Object {passenger.GetName()} removed from Transport {Owner.GetName()}.");

                    Player plr = passenger.ToPlayer();

                    plr?.SetFallInformation(0, plr.GetPositionZ());
                }

                return this;
            }

            public void CalculatePassengerPosition(ref float x, ref float y, ref float z, ref float o)
            {
                ITransport.CalculatePassengerPosition(ref x, ref y, ref z, ref o, Owner.GetPositionX(), Owner.GetPositionY(), Owner.GetPositionZ(), Owner.GetOrientation());
            }

            public void CalculatePassengerOffset(ref float x, ref float y, ref float z, ref float o)
            {
                ITransport.CalculatePassengerOffset(ref x, ref y, ref z, ref o, Owner.GetPositionX(), Owner.GetPositionY(), Owner.GetPositionZ(), Owner.GetOrientation());
            }

            public int GetMapIdForSpawning()
            {
                return Owner.GetGoInfo().Transport.SpawnMap;
            }

            public override void Update(uint diff)
            {
                if (_animationInfo == null)
                    return;

                _positionUpdateTimer.Update(diff);

                if (!_positionUpdateTimer.Passed())
                    return;

                _positionUpdateTimer.Reset(PositionUpdateInterval);

                uint now = GameTime.GetGameTimeMS();
                uint period = GetTransportPeriod();
                uint newProgress = 0;

                if (_stopFrames.Empty())
                {
                    newProgress = now % period;
                }
                else
                {
                    int stopTargetTime = 0;

                    if (Owner.GetGoState() == GameObjectState.TransportActive)
                        stopTargetTime = 0;
                    else
                        stopTargetTime = (int)(_stopFrames[Owner.GetGoState() - GameObjectState.TransportStopped]);

                    if (now < Owner._gameObjectData.Level)
                    {
                        int timeToStop = (int)(Owner._gameObjectData.Level - _stateChangeTime);
                        float stopSourcePathPct = (float)_stateChangeProgress / (float)period;
                        float stopTargetPathPct = (float)stopTargetTime / (float)period;
                        float timeSinceStopProgressPct = (float)(now - _stateChangeTime) / (float)timeToStop;

                        float progressPct;

                        if (!Owner.HasDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement))
                        {
                            if (Owner.GetGoState() == GameObjectState.TransportActive)
                                stopTargetPathPct = 1.0f;

                            float pathPctBetweenStops = stopTargetPathPct - stopSourcePathPct;

                            if (pathPctBetweenStops < 0.0f)
                                pathPctBetweenStops += 1.0f;

                            progressPct = pathPctBetweenStops * timeSinceStopProgressPct + stopSourcePathPct;

                            if (progressPct > 1.0f)
                                progressPct = progressPct - 1.0f;
                        }
                        else
                        {
                            float pathPctBetweenStops = stopSourcePathPct - stopTargetPathPct;

                            if (pathPctBetweenStops < 0.0f)
                                pathPctBetweenStops += 1.0f;

                            progressPct = stopSourcePathPct - pathPctBetweenStops * timeSinceStopProgressPct;

                            if (progressPct < 0.0f)
                                progressPct += 1.0f;
                        }

                        newProgress = (uint)((float)period * progressPct) % period;
                    }
                    else
                    {
                        newProgress = (uint)stopTargetTime;
                    }

                    if (newProgress == stopTargetTime &&
                        newProgress != _pathProgress)
                    {
                        uint eventId;

                        switch (Owner.GetGoState() - GameObjectState.TransportActive)
                        {
                            case 0:
                                eventId = Owner.GetGoInfo().Transport.Reached1stfloor;

                                break;
                            case 1:
                                eventId = Owner.GetGoInfo().Transport.Reached2ndfloor;

                                break;
                            case 2:
                                eventId = Owner.GetGoInfo().Transport.Reached3rdfloor;

                                break;
                            case 3:
                                eventId = Owner.GetGoInfo().Transport.Reached4thfloor;

                                break;
                            case 4:
                                eventId = Owner.GetGoInfo().Transport.Reached5thfloor;

                                break;
                            case 5:
                                eventId = Owner.GetGoInfo().Transport.Reached6thfloor;

                                break;
                            case 6:
                                eventId = Owner.GetGoInfo().Transport.Reached7thfloor;

                                break;
                            case 7:
                                eventId = Owner.GetGoInfo().Transport.Reached8thfloor;

                                break;
                            case 8:
                                eventId = Owner.GetGoInfo().Transport.Reached9thfloor;

                                break;
                            case 9:
                                eventId = Owner.GetGoInfo().Transport.Reached10thfloor;

                                break;
                            default:
                                eventId = 0u;

                                break;
                        }

                        if (eventId != 0)
                            GameEvents.Trigger(eventId, Owner, null);

                        if (_autoCycleBetweenStopFrames)
                        {
                            GameObjectState currentState = Owner.GetGoState();
                            GameObjectState newState;

                            if (currentState == GameObjectState.TransportActive)
                                newState = GameObjectState.TransportStopped;
                            else if (currentState - GameObjectState.TransportActive == _stopFrames.Count)
                                newState = currentState - 1;
                            else if (Owner.HasDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement))
                                newState = currentState - 1;
                            else
                                newState = currentState + 1;

                            Owner.SetGoState(newState);
                        }
                    }
                }

                if (_pathProgress == newProgress)
                    return;

                _pathProgress = newProgress;

                TransportAnimationRecord oldAnimation = _animationInfo.GetPrevAnimNode(newProgress);
                TransportAnimationRecord newAnimation = _animationInfo.GetNextAnimNode(newProgress);

                if (oldAnimation != null &&
                    newAnimation != null)
                {
                    Matrix4x4 pathRotation = new Quaternion(Owner._gameObjectData.ParentRotation.GetValue().X,
                                                            Owner._gameObjectData.ParentRotation.GetValue().Y,
                                                            Owner._gameObjectData.ParentRotation.GetValue().Z,
                                                            Owner._gameObjectData.ParentRotation.GetValue().W).ToMatrix();

                    Vector3 prev = new(oldAnimation.Pos.X, oldAnimation.Pos.Y, oldAnimation.Pos.Z);
                    Vector3 next = new(newAnimation.Pos.X, newAnimation.Pos.Y, newAnimation.Pos.Z);

                    float animProgress = (float)(newProgress - oldAnimation.TimeIndex) / (float)(newAnimation.TimeIndex - oldAnimation.TimeIndex);

                    Vector3 dst = pathRotation.Multiply(Vector3.Lerp(prev, next, animProgress));

                    dst += Owner.GetStationaryPosition();

                    Owner.GetMap().GameObjectRelocation(Owner, dst.X, dst.Y, dst.Z, Owner.GetOrientation());
                }

                TransportRotationRecord oldRotation = _animationInfo.GetPrevAnimRotation(newProgress);
                TransportRotationRecord newRotation = _animationInfo.GetNextAnimRotation(newProgress);

                if (oldRotation != null &&
                    newRotation != null)
                {
                    Quaternion prev = new(oldRotation.Rot[0], oldRotation.Rot[1], oldRotation.Rot[2], oldRotation.Rot[3]);
                    Quaternion next = new(newRotation.Rot[0], newRotation.Rot[1], newRotation.Rot[2], newRotation.Rot[3]);

                    float animProgress = (float)(newProgress - oldRotation.TimeIndex) / (float)(newRotation.TimeIndex - oldRotation.TimeIndex);

                    Quaternion rotation = Quaternion.Lerp(prev, next, animProgress);

                    Owner.SetLocalRotation(rotation.X, rotation.Y, rotation.Z, rotation.W);
                    Owner.UpdateModelPosition();
                }

                // update progress marker for client
                Owner.SetPathProgressForClient((float)_pathProgress / (float)period);
            }

            public override void OnStateChanged(GameObjectState oldState, GameObjectState newState)
            {
                Cypher.Assert(newState >= GameObjectState.TransportActive);

                // transports without stop frames just keep animating in State 24
                if (_stopFrames.Empty())
                {
                    if (newState != GameObjectState.TransportActive)
                        Owner.SetGoState(GameObjectState.TransportActive);

                    return;
                }

                uint stopPathProgress = 0;

                if (newState != GameObjectState.TransportActive)
                {
                    Cypher.Assert(newState < (GameObjectState)(GameObjectState.TransportStopped + 9));
                    int stopFrame = (int)(newState - GameObjectState.TransportStopped);
                    Cypher.Assert(stopFrame < _stopFrames.Count);
                    stopPathProgress = _stopFrames[stopFrame];
                }

                _stateChangeTime = GameTime.GetGameTimeMS();
                _stateChangeProgress = _pathProgress;
                uint timeToStop = (uint)Math.Abs(_pathProgress - stopPathProgress);
                Owner.SetLevel(GameTime.GetGameTimeMS() + timeToStop);
                Owner.SetPathProgressForClient((float)_pathProgress / (float)GetTransportPeriod());

                if (oldState == GameObjectState.Active ||
                    oldState == newState)
                {
                    // initialization
                    if (_pathProgress > stopPathProgress)
                        Owner.SetDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);
                    else
                        Owner.RemoveDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);

                    return;
                }

                int pauseTimesCount = _stopFrames.Count;
                int newToOldStateDelta = newState - oldState;

                if (newToOldStateDelta < 0)
                    newToOldStateDelta += pauseTimesCount + 1;

                int oldToNewStateDelta = oldState - newState;

                if (oldToNewStateDelta < 0)
                    oldToNewStateDelta += pauseTimesCount + 1;

                // this additional check is neccessary because client doesn't check dynamic Flags on progress update
                // instead it multiplies progress from dynamicflags field by -1 and then compares that against 0
                // when calculating path progress while we simply check the flag if (!_owner.HasDynamicFlag(GO_DYNFLAG_LO_INVERTED_MOVEMENT))
                bool isAtStartOfPath = _stateChangeProgress == 0;

                if (oldToNewStateDelta < newToOldStateDelta &&
                    !isAtStartOfPath)
                    Owner.SetDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);
                else
                    Owner.RemoveDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);
            }

            public override void OnRelocated()
            {
                UpdatePassengerPositions();
            }

            public void UpdatePassengerPositions()
            {
                foreach (WorldObject passenger in _passengers)
                {
                    float x, y, z, o;
                    passenger.MovementInfo.Transport.Pos.GetPosition(out x, out y, out z, out o);
                    CalculatePassengerPosition(ref x, ref y, ref z, ref o);
                    ITransport.UpdatePassengerPosition(this, Owner.GetMap(), passenger, x, y, z, o, true);
                }
            }

            public uint GetTransportPeriod()
            {
                if (_animationInfo != null)
                    return _animationInfo.TotalTime;

                return 1;
            }

            public List<uint> GetPauseTimes()
            {
                return _stopFrames;
            }

            public void SetAutoCycleBetweenStopFrames(bool on)
            {
                _autoCycleBetweenStopFrames = on;
            }
        }
    }
}