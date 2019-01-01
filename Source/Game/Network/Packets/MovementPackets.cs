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
using Framework.Dynamic;
using Framework.GameMath;
using Game.Entities;
using Game.Movement;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Game.Network.Packets
{
    public static class MovementExtensions
    {
        public static void ReadTransportInfo(WorldPacket data, ref MovementInfo.TransportInfo transportInfo)
        {
            transportInfo.guid = data.ReadPackedGuid();                 // Transport Guid
            transportInfo.pos.posX = data.ReadFloat();
            transportInfo.pos.posY = data.ReadFloat();
            transportInfo.pos.posZ = data.ReadFloat();
            transportInfo.pos.Orientation = data.ReadFloat();
            transportInfo.seat = data.ReadInt8();                 // VehicleSeatIndex
            transportInfo.time = data.ReadUInt32();                 // MoveTime

            bool hasPrevTime = data.HasBit();
            bool hasVehicleId = data.HasBit();

            if (hasPrevTime)
                transportInfo.prevTime = data.ReadUInt32();         // PrevMoveTime

            if (hasVehicleId)
                transportInfo.vehicleId = data.ReadUInt32();        // VehicleRecID
        }

        public static void WriteTransportInfo(WorldPacket data, MovementInfo.TransportInfo transportInfo)
        {
            bool hasPrevTime = transportInfo.prevTime != 0;
            bool hasVehicleId = transportInfo.vehicleId != 0;

            data.WritePackedGuid(transportInfo.guid);                 // Transport Guid
            data.WriteFloat(transportInfo.pos.GetPositionX());
            data.WriteFloat(transportInfo.pos.GetPositionY());
            data.WriteFloat(transportInfo.pos.GetPositionZ());
            data.WriteFloat(transportInfo.pos.GetOrientation());
            data.WriteInt8(transportInfo.seat);                 // VehicleSeatIndex
            data.WriteUInt32(transportInfo.time);                 // MoveTime

            data.WriteBit(hasPrevTime);
            data.WriteBit(hasVehicleId);
            data.FlushBits();

            if (hasPrevTime)
                data.WriteUInt32(transportInfo.prevTime);         // PrevMoveTime

            if (hasVehicleId)
                data.WriteUInt32(transportInfo.vehicleId);        // VehicleRecID
        }

        public static MovementInfo ReadMovementInfo(WorldPacket data)
        {
            var movementInfo = new MovementInfo();
            movementInfo.Guid = data.ReadPackedGuid();
            movementInfo.Time = data.ReadUInt32();
            float x = data.ReadFloat();
            float y = data.ReadFloat();
            float z = data.ReadFloat();
            float o = data.ReadFloat();

            movementInfo.Pos.Relocate(x, y, z, o);
            movementInfo.Pitch = data.ReadFloat();
            movementInfo.SplineElevation = data.ReadFloat();

            uint removeMovementForcesCount = data.ReadUInt32();

            uint moveIndex = data.ReadUInt32();

            for (uint i = 0; i < removeMovementForcesCount; ++i)
            {
                data.ReadPackedGuid();
            }

            // ResetBitReader

            movementInfo.SetMovementFlags((MovementFlag)data.ReadBits<uint>(30));
            movementInfo.SetMovementFlags2((MovementFlag2)data.ReadBits<uint>(18));

            bool hasTransport = data.HasBit();
            bool hasFall = data.HasBit();
            bool hasSpline = data.HasBit(); // todo 6.x read this infos

            data.ReadBit(); // HeightChangeFailed
            data.ReadBit(); // RemoteTimeValid

            if (hasTransport)
                ReadTransportInfo(data, ref movementInfo.transport);

            if (hasFall)
            {
                movementInfo.jump.fallTime = data.ReadUInt32();
                movementInfo.jump.zspeed = data.ReadFloat();

                // ResetBitReader

                bool hasFallDirection = data.HasBit();
                if (hasFallDirection)
                {
                    movementInfo.jump.sinAngle = data.ReadFloat();
                    movementInfo.jump.cosAngle = data.ReadFloat();
                    movementInfo.jump.xyspeed = data.ReadFloat();
                }
            }

            return movementInfo;
        }

        public static void WriteMovementInfo(WorldPacket data, MovementInfo movementInfo)
        {
            bool hasTransportData = !movementInfo.transport.guid.IsEmpty();
            bool hasFallDirection = movementInfo.HasMovementFlag(MovementFlag.Falling | MovementFlag.FallingFar);
            bool hasFallData = hasFallDirection || movementInfo.jump.fallTime != 0;
            bool hasSpline = false; // todo 6.x send this infos

            data.WritePackedGuid(movementInfo.Guid);
            data.WriteUInt32(movementInfo.Time);
            data.WriteFloat(movementInfo.Pos.GetPositionX());
            data.WriteFloat(movementInfo.Pos.GetPositionY());
            data.WriteFloat(movementInfo.Pos.GetPositionZ());
            data.WriteFloat(movementInfo.Pos.GetOrientation());
            data.WriteFloat(movementInfo.Pitch);
            data.WriteFloat(movementInfo.SplineElevation);

            uint removeMovementForcesCount = 0;
            data.WriteUInt32(removeMovementForcesCount);

            uint int168 = 0;
            data.WriteUInt32(int168);

            /*for (public uint i = 0; i < removeMovementForcesCount; ++i)
            {
                _worldPacket << ObjectGuid;
            }*/

            data.WriteBits((uint)movementInfo.GetMovementFlags(), 30);
            data.WriteBits((uint)movementInfo.GetMovementFlags2(), 18);

            data.WriteBit(hasTransportData);
            data.WriteBit(hasFallData);
            data.WriteBit(hasSpline);
            data.WriteBit(0); // HeightChangeFailed
            data.WriteBit(0); // RemoteTimeValid
            data.FlushBits();

            if (hasTransportData)
                WriteTransportInfo(data, movementInfo.transport);

            if (hasFallData)
            {
                data.WriteUInt32(movementInfo.jump.fallTime);
                data.WriteFloat(movementInfo.jump.zspeed);

                data.WriteBit(hasFallDirection);
                data.FlushBits();
                if (hasFallDirection)
                {
                    data.WriteFloat(movementInfo.jump.sinAngle);
                    data.WriteFloat(movementInfo.jump.cosAngle);
                    data.WriteFloat(movementInfo.jump.xyspeed);
                }
            }
        }

        public static void WriteCreateObjectSplineDataBlock(MoveSpline moveSpline, WorldPacket data)
        {
            data.WriteUInt32(moveSpline.GetId());                                         // ID

            if (!moveSpline.isCyclic())                                                 // Destination
                data.WriteVector3(moveSpline.FinalDestination());
            else
                data.WriteVector3(Vector3.Zero);

            bool hasSplineMove = data.WriteBit(!moveSpline.Finalized() && !moveSpline.splineIsFacingOnly);
            data.FlushBits();

            if (hasSplineMove)
            {
                data.WriteUInt32((uint)moveSpline.splineflags.Flags);   // SplineFlags
                data.WriteInt32(moveSpline.timePassed());               // Elapsed
                data.WriteUInt32(moveSpline.Duration());                // Duration
                data.WriteFloat(1.0f);                                  // DurationModifier
                data.WriteFloat(1.0f);                                  // NextDurationModifier
                data.WriteBits((byte)moveSpline.facing.type, 2);        // Face
                bool hasFadeObjectTime = data.WriteBit(moveSpline.splineflags.hasFlag(SplineFlag.FadeObject) && moveSpline.effect_start_time < moveSpline.Duration());
                data.WriteBits(moveSpline.getPath().Length, 16);
                data.WriteBits((byte)moveSpline.spline.m_mode, 2);      // Mode
                data.WriteBit(0);                                       // HasSplineFilter
                data.WriteBit(moveSpline.spell_effect_extra.HasValue);  // HasSpellEffectExtraData
                data.WriteBit(moveSpline.splineflags.hasFlag(SplineFlag.Parabolic));        // HasJumpExtraData
                data.FlushBits();

                //if (HasSplineFilterKey)
                //{
                //    data << uint32(FilterKeysCount);
                //    for (var i = 0; i < FilterKeysCount; ++i)
                //    {
                //        data << float(In);
                //        data << float(Out);
                //    }

                //    data.WriteBits(FilterFlags, 2);
                //    data.FlushBits();
                //}

                switch (moveSpline.facing.type)
                {
                    case MonsterMoveType.FacingSpot:
                        data.WriteVector3(moveSpline.facing.f);         // FaceSpot
                        break;
                    case MonsterMoveType.FacingTarget:
                        data.WritePackedGuid(moveSpline.facing.target); // FaceGUID
                        break;
                    case MonsterMoveType.FacingAngle:
                        data.WriteFloat(moveSpline.facing.angle);       // FaceDirection
                        break;
                }

                if (hasFadeObjectTime)
                    data.WriteUInt32(moveSpline.effect_start_time);     // FadeObjectTime

                foreach (var vec in moveSpline.getPath())
                    data.WriteVector3(vec);

                if (moveSpline.spell_effect_extra.HasValue)
                {
                    data.WritePackedGuid(moveSpline.spell_effect_extra.Value.Target);
                    data.WriteUInt32(moveSpline.spell_effect_extra.Value.SpellVisualId);
                    data.WriteUInt32(moveSpline.spell_effect_extra.Value.ProgressCurveId);
                    data.WriteUInt32(moveSpline.spell_effect_extra.Value.ParabolicCurveId);
                }

                if (moveSpline.splineflags.hasFlag(SplineFlag.Parabolic))
                {
                    data.WriteFloat(moveSpline.vertical_acceleration);
                    data.WriteUInt32(moveSpline.effect_start_time);
                    data.WriteUInt32(0);                                                  // Duration (override)
                }
            }
        }

        public static void WriteCreateObjectAreaTriggerSpline(Spline spline, WorldPacket data)
        {
            data.WriteBits(spline.getPoints().Length, 16);
            foreach (var point in spline.getPoints())
                data.WriteVector3(point);
        }
    }

    public class ClientPlayerMovement : ClientPacket
    {
        public ClientPlayerMovement(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Status = MovementExtensions.ReadMovementInfo(_worldPacket);
        }

        public MovementInfo Status;
    }

    public class MoveUpdate : ServerPacket
    {
        public MoveUpdate() : base(ServerOpcodes.MoveUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, Status);
        }

        public MovementInfo Status;
    }

    public class MonsterMove : ServerPacket
    {
        public MonsterMove() : base(ServerOpcodes.OnMonsterMove, ConnectionType.Instance)
        {
            SplineData = new MovementMonsterSpline();
        }

        public void InitializeSplineData(MoveSpline moveSpline)
        {
            SplineData.ID = moveSpline.GetId();
            MovementSpline movementSpline = SplineData.Move;

            MoveSplineFlag splineFlags = moveSpline.splineflags;
            splineFlags.SetUnsetFlag(SplineFlag.Cyclic, moveSpline.isCyclic());
            movementSpline.Flags = (uint)(splineFlags.Flags & ~SplineFlag.MaskNoMonsterMove);
            movementSpline.Face = moveSpline.facing.type;
            movementSpline.FaceDirection = moveSpline.facing.angle;
            movementSpline.FaceGUID = moveSpline.facing.target;
            movementSpline.FaceSpot = moveSpline.facing.f;

            if (splineFlags.hasFlag(SplineFlag.Animation))
            {
                movementSpline.AnimTier = splineFlags.getAnimationId();
                movementSpline.TierTransStartTime = (uint)moveSpline.effect_start_time;
            }

            movementSpline.MoveTime = (uint)moveSpline.Duration();

            if (splineFlags.hasFlag(SplineFlag.Parabolic))
            {
                movementSpline.JumpExtraData.HasValue = true;
                movementSpline.JumpExtraData.Value.JumpGravity = moveSpline.vertical_acceleration;
                movementSpline.JumpExtraData.Value.StartTime = (uint)moveSpline.effect_start_time;
            }

            if (splineFlags.hasFlag(SplineFlag.FadeObject))
                movementSpline.FadeObjectTime = (uint)moveSpline.effect_start_time;

            if (moveSpline.spell_effect_extra.HasValue)
            {
                movementSpline.SpellEffectExtraData.HasValue = true;
                movementSpline.SpellEffectExtraData.Value.TargetGuid = moveSpline.spell_effect_extra.Value.Target;
                movementSpline.SpellEffectExtraData.Value.SpellVisualID = moveSpline.spell_effect_extra.Value.SpellVisualId;
                movementSpline.SpellEffectExtraData.Value.ProgressCurveID = moveSpline.spell_effect_extra.Value.ProgressCurveId;
                movementSpline.SpellEffectExtraData.Value.ParabolicCurveID = moveSpline.spell_effect_extra.Value.ParabolicCurveId;
                movementSpline.SpellEffectExtraData.Value.JumpGravity = moveSpline.vertical_acceleration;
            }

            Spline spline = moveSpline.spline;
            Vector3[] array = spline.getPoints();

            if (splineFlags.hasFlag(SplineFlag.UncompressedPath))
            {
                if (!splineFlags.hasFlag(SplineFlag.Cyclic))
                {
                    int count = spline.getPointCount() - 3;
                    for (uint i = 0; i < count; ++i)
                        movementSpline.Points.Add(array[i + 2]);
                }
                else
                {
                    int count = spline.getPointCount() - 3;
                    movementSpline.Points.Add(array[1]);
                    for (uint i = 0; i < count; ++i)
                        movementSpline.Points.Add(array[i + 1]);
                }
            }
            else
            {
                int lastIdx = spline.getPointCount() - 3;
                Span<Vector3> realPath = new Span<Vector3>(spline.getPoints()).Slice(1);

                movementSpline.Points.Add(realPath[lastIdx]);

                if (lastIdx > 1)
                {
                    Vector3 middle = (realPath[0] + realPath[lastIdx]) / 2.0f;

                    // first and last points already appended
                    for (int i = 1; i < lastIdx; ++i)
                        movementSpline.PackedDeltas.Add(middle - realPath[i]);
                }
            }
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteVector3(Pos);
            SplineData.Write(_worldPacket);
        }

        public MovementMonsterSpline SplineData;
        public ObjectGuid MoverGUID;
        public Vector3 Pos;
    }

    public class MoveSplineSetSpeed : ServerPacket
    {
        public MoveSplineSetSpeed(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteFloat(Speed);
        }

        public ObjectGuid MoverGUID;
        public float Speed = 1.0f;
    }

    public class MoveSetSpeed : ServerPacket
    {
        public MoveSetSpeed(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteFloat(Speed);
        }

        public ObjectGuid MoverGUID;
        public uint SequenceIndex = 0; // Unit movement packet index, incremented each time
        public float Speed = 1.0f;
    }

    public class MoveUpdateSpeed : ServerPacket
    {
        public MoveUpdateSpeed(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance) { }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, Status);
            _worldPacket.WriteFloat(Speed);
        }

        public MovementInfo Status;
        public float Speed = 1.0f;
    }

    public class MoveSplineSetFlag : ServerPacket
    {
        public MoveSplineSetFlag(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
        }

        public ObjectGuid MoverGUID;
    }

    public class MoveSetFlag : ServerPacket
    {
        public MoveSetFlag(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(SequenceIndex);
        }

        public ObjectGuid MoverGUID;
        public uint SequenceIndex = 0; // Unit movement packet index, incremented each time
    }

    public class TransferPending : ServerPacket
    {
        public TransferPending() : base(ServerOpcodes.TransferPending)
        {
            Ship = new Optional<ShipTransferPending>();
            TransferSpellID = new Optional<int>();
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WriteXYZ(OldMapPosition);
            _worldPacket.WriteBit(Ship.HasValue);
            _worldPacket.WriteBit(TransferSpellID.HasValue);
            if (Ship.HasValue)
            {
                _worldPacket.WriteUInt32(Ship.Value.ID);
                _worldPacket.WriteInt32(Ship.Value.OriginMapID);
            }

            if (TransferSpellID.HasValue)
                _worldPacket.WriteInt32(TransferSpellID.Value);

            _worldPacket.FlushBits();
        }

        public int MapID = -1;
        public Position OldMapPosition;
        public Optional<ShipTransferPending> Ship;
        public Optional<int> TransferSpellID;

        public struct ShipTransferPending
        {
            public uint ID;              // gameobject_template.entry of the transport the player is teleporting on
            public int OriginMapID;     // Map id the player is currently on (before teleport)
        }
    }

    public class TransferAborted : ServerPacket
    {
        public TransferAborted() : base(ServerOpcodes.TransferAborted) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MapID);
            _worldPacket.WriteUInt8(Arg);
            _worldPacket.WriteInt32(MapDifficultyXConditionID);
            _worldPacket.WriteBits(TransfertAbort, 5);
            _worldPacket.FlushBits();
        }

        public uint MapID;
        public byte Arg;
        public int MapDifficultyXConditionID;
        public TransferAbortReason TransfertAbort;
    }

    public class NewWorld : ServerPacket
    {
        public NewWorld() : base(ServerOpcodes.NewWorld) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MapID);
            _worldPacket.WriteXYZO(Pos);
            _worldPacket.WriteUInt32(Reason);
            _worldPacket.WriteXYZ(MovementOffset);
        }

        public uint MapID;
        public uint Reason;
        public Position Pos;
        public Position MovementOffset;    // Adjusts all pending movement events by this offset
    }

    public class WorldPortResponse : ClientPacket
    {
        public WorldPortResponse(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class MoveTeleport : ServerPacket
    {
        public MoveTeleport() : base(ServerOpcodes.MoveTeleport, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteXYZ(Pos);
            _worldPacket.WriteFloat(Facing);
            _worldPacket.WriteUInt8(PreloadWorld);

            _worldPacket.WriteBit(TransportGUID.HasValue);
            _worldPacket.WriteBit(Vehicle.HasValue);
            _worldPacket.FlushBits();

            if (Vehicle.HasValue)
            {
                _worldPacket.WriteUInt8(Vehicle.Value.VehicleSeatIndex);
                _worldPacket.WriteBit(Vehicle.Value.VehicleExitVoluntary);
                _worldPacket.WriteBit(Vehicle.Value.VehicleExitTeleport);
                _worldPacket.FlushBits();
            }

            if (TransportGUID.HasValue)
                _worldPacket.WritePackedGuid(TransportGUID.Value);
        }

        public Position Pos;
        public Optional<VehicleTeleport> Vehicle;
        public uint SequenceIndex;
        public ObjectGuid MoverGUID;
        public Optional<ObjectGuid> TransportGUID;
        public float Facing;
        public byte PreloadWorld;
    }

    public class MoveUpdateTeleport : ServerPacket
    {
        public MoveUpdateTeleport() : base(ServerOpcodes.MoveUpdateTeleport) { }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, Status);

            _worldPacket.WriteInt32(MovementForces.Count);
            _worldPacket.WriteBit(WalkSpeed.HasValue);
            _worldPacket.WriteBit(RunSpeed.HasValue);
            _worldPacket.WriteBit(RunBackSpeed.HasValue);
            _worldPacket.WriteBit(SwimSpeed.HasValue);
            _worldPacket.WriteBit(SwimBackSpeed.HasValue);
            _worldPacket.WriteBit(FlightSpeed.HasValue);
            _worldPacket.WriteBit(FlightBackSpeed.HasValue);
            _worldPacket.WriteBit(TurnRate.HasValue);
            _worldPacket.WriteBit(PitchRate.HasValue);
            _worldPacket.FlushBits();

            foreach (MovementForce force in MovementForces)
                force.Write(_worldPacket);

            if (WalkSpeed.HasValue)
                _worldPacket.WriteFloat(WalkSpeed.Value);

            if (RunSpeed.HasValue)
                _worldPacket.WriteFloat(RunSpeed.Value);

            if (RunBackSpeed.HasValue)
                _worldPacket.WriteFloat(RunBackSpeed.Value);

            if (SwimSpeed.HasValue)
                _worldPacket.WriteFloat(SwimSpeed.Value);

            if (SwimBackSpeed.HasValue)
                _worldPacket.WriteFloat(SwimBackSpeed.Value);

            if (FlightSpeed.HasValue)
                _worldPacket.WriteFloat(FlightSpeed.Value);

            if (FlightBackSpeed.HasValue)
                _worldPacket.WriteFloat(FlightBackSpeed.Value);

            if (TurnRate.HasValue)
                _worldPacket.WriteFloat(TurnRate.Value);

            if (PitchRate.HasValue)
                _worldPacket.WriteFloat(PitchRate.Value);
        }

        public MovementInfo Status;
        public List<MovementForce> MovementForces = new List<MovementForce>();
        public Optional<float> SwimBackSpeed;
        public Optional<float> FlightSpeed;
        public Optional<float> SwimSpeed;
        public Optional<float> WalkSpeed;
        public Optional<float> TurnRate;
        public Optional<float> RunSpeed;
        public Optional<float> FlightBackSpeed;
        public Optional<float> RunBackSpeed;
        public Optional<float> PitchRate;
    }

    class MoveUpdateApplyMovementForce : ServerPacket
    {
        public MoveUpdateApplyMovementForce() : base(ServerOpcodes.MoveUpdateApplyMovementForce) { }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, movementInfo);
            Force.Write(_worldPacket);
        }

        MovementInfo movementInfo = new MovementInfo();
        MovementForce Force = new MovementForce();
    }

    class MoveUpdateRemoveMovementForce : ServerPacket
    {
        public MoveUpdateRemoveMovementForce() : base(ServerOpcodes.MoveUpdateRemoveMovementForce) { }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, movementInfo);
            _worldPacket.WritePackedGuid(TriggerGUID);
        }

        MovementInfo movementInfo = new MovementInfo();
        ObjectGuid TriggerGUID;
    }

    class MoveTeleportAck : ClientPacket
    {
        public MoveTeleportAck(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MoverGUID = _worldPacket.ReadPackedGuid();
            AckIndex = _worldPacket.ReadInt32();
            MoveTime = _worldPacket.ReadInt32();
        }

        public ObjectGuid MoverGUID;
        int AckIndex;
        int MoveTime;
    }

    public class MovementAckMessage : ClientPacket
    {
        public MovementAckMessage(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Ack.Read(_worldPacket);
        }

        public MovementAck Ack;
    }

    public class MovementSpeedAck : ClientPacket
    {
        public MovementSpeedAck(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Ack.Read(_worldPacket);
            Speed = _worldPacket.ReadFloat();
        }

        public MovementAck Ack;
        public float Speed;
    }

    public class SetActiveMover : ClientPacket
    {
        public ObjectGuid ActiveMover;

        public SetActiveMover(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ActiveMover = _worldPacket.ReadPackedGuid();
        }
    }

    public class MoveSetActiveMover : ServerPacket
    {
        public ObjectGuid MoverGUID;

        public MoveSetActiveMover() : base(ServerOpcodes.MoveSetActiveMover) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
        }
    }

    class MoveKnockBack : ServerPacket
    {
        public MoveKnockBack() : base(ServerOpcodes.MoveKnockBack, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteVector2(Direction);
            Speeds.Write(_worldPacket);
        }

        public ObjectGuid MoverGUID;
        public Vector2 Direction;
        public MoveKnockBackSpeeds Speeds;
        public uint SequenceIndex;
    }

    public class MoveUpdateKnockBack : ServerPacket
    {
        public MoveUpdateKnockBack() : base(ServerOpcodes.MoveUpdateKnockBack) { }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, Status);
        }

        public MovementInfo Status;
    }

    class MoveKnockBackAck : ClientPacket
    {
        public MoveKnockBackAck(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Ack.Read(_worldPacket);
            if (_worldPacket.HasBit())
            {
                Speeds.HasValue = true;
                Speeds.Value.Read(_worldPacket);
            }
        }

        public MovementAck Ack;
        public Optional<MoveKnockBackSpeeds> Speeds;
    }

    class MoveSetCollisionHeight : ServerPacket
    {
        public MoveSetCollisionHeight() : base(ServerOpcodes.MoveSetCollisionHeight) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteFloat(Height);
            _worldPacket.WriteFloat(Scale);
            _worldPacket.WriteUInt32(MountDisplayID);
            _worldPacket.WriteInt32(ScaleDuration);
            _worldPacket.WriteBits((uint)Reason, 2);
            _worldPacket.FlushBits();
        }

        public float Scale = 1.0f;
        public ObjectGuid MoverGUID;
        public uint MountDisplayID;
        public UpdateCollisionHeightReason Reason = UpdateCollisionHeightReason.Mount;
        public uint SequenceIndex;
        public int ScaleDuration;
        public float Height = 1.0f;
    }

    public class MoveUpdateCollisionHeight : ServerPacket
    {
        public MoveUpdateCollisionHeight() : base(ServerOpcodes.MoveUpdateCollisionHeight) { }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, Status);
            _worldPacket.WriteFloat(Height);
            _worldPacket.WriteFloat(Scale);
        }

        public MovementInfo Status;
        public float Scale = 1.0f;
        public float Height = 1.0f;
    }

    public class MoveSetCollisionHeightAck : ClientPacket
    {
        public MoveSetCollisionHeightAck(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Data.Read(_worldPacket);
            Height = _worldPacket.ReadFloat();
            MountDisplayID = _worldPacket.ReadUInt32();
            Reason = (UpdateCollisionHeightReason)_worldPacket.ReadBits<uint>(2);
        }

        public MovementAck Data;
        public UpdateCollisionHeightReason Reason = UpdateCollisionHeightReason.Mount;
        public uint MountDisplayID;
        public float Height = 1.0f;
    }

    class MoveTimeSkipped : ClientPacket
    {
        public MoveTimeSkipped(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MoverGUID = _worldPacket.ReadPackedGuid();
            TimeSkipped = _worldPacket.ReadUInt32();
        }

        public ObjectGuid MoverGUID;
        public uint TimeSkipped;
    }

    class SummonResponse : ClientPacket
    {
        public SummonResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SummonerGUID = _worldPacket.ReadPackedGuid();
            Accept = _worldPacket.HasBit();
        }

        public bool Accept;
        public ObjectGuid SummonerGUID;
    }

    public class ControlUpdate : ServerPacket
    {
        public ControlUpdate() : base(ServerOpcodes.ControlUpdate) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteBit(On);
            _worldPacket.FlushBits();
        }

        public bool On;
        public ObjectGuid Guid;
    }

    class MoveSplineDone : ClientPacket
    {
        public MoveSplineDone(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Status = MovementExtensions.ReadMovementInfo(_worldPacket);
            SplineID = _worldPacket.ReadInt32();
        }

        public MovementInfo Status;
        public int SplineID;
    }

    class SummonRequest : ServerPacket
    {
        public SummonRequest() : base(ServerOpcodes.SummonRequest, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(SummonerGUID);
            _worldPacket.WriteUInt32(SummonerVirtualRealmAddress);
            _worldPacket.WriteInt32(AreaID);
            _worldPacket.WriteUInt8(Reason);
            _worldPacket.WriteBit(SkipStartingArea);
            _worldPacket.FlushBits();
        }

        public ObjectGuid SummonerGUID;
        public uint SummonerVirtualRealmAddress;
        public int AreaID;
        public SummonReason Reason;
        public bool SkipStartingArea;

        public enum SummonReason
        {
            Spell = 0,
            Scenario = 1
        }
    }

    class SuspendToken : ServerPacket
    {
        public SuspendToken() : base(ServerOpcodes.SuspendToken, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteBits(Reason, 2);
            _worldPacket.FlushBits();
        }

        public uint SequenceIndex = 1;
        public uint Reason = 1;
    }

    class SuspendTokenResponse : ClientPacket
    {
        public SuspendTokenResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SequenceIndex = _worldPacket.ReadUInt32();
        }

        public uint SequenceIndex;
    }

    class ResumeToken : ServerPacket
    {
        public ResumeToken() : base(ServerOpcodes.ResumeToken, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteBits(Reason, 2);
            _worldPacket.FlushBits();
        }

        public uint SequenceIndex = 1;
        public uint Reason = 1;
    }

    class MoveSetCompoundState : ServerPacket
    {
        public MoveSetCompoundState() : base(ServerOpcodes.MoveSetCompoundState, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(StateChanges.Count);
            foreach (MoveStateChange stateChange in StateChanges)
                stateChange.Write(_worldPacket);
        }

        public ObjectGuid MoverGUID;
        public List<MoveStateChange> StateChanges = new List<MoveStateChange>();

        public struct CollisionHeightInfo
        {
            public float Height;
            public float Scale;
            public UpdateCollisionHeightReason Reason;
        }

        public struct KnockBackInfo
        {
            public float HorzSpeed;
            public Vector2 Direction;
            public float InitVertSpeed;
        }

        public class MoveStateChange
        {
            public MoveStateChange(ServerOpcodes messageId, uint sequenceIndex)
            {
                MessageID = messageId;
                SequenceIndex = sequenceIndex;
            }

            public void Write(WorldPacket data)
            {
                data.WriteUInt16(MessageID);
                data.WriteUInt32(SequenceIndex);
                data.WriteBit(Speed.HasValue);
                data.WriteBit(KnockBack.HasValue);
                data.WriteBit(VehicleRecID.HasValue);
                data.WriteBit(CollisionHeight.HasValue);
                data.WriteBit(MovementForce_.HasValue);
                data.WriteBit(Unknown.HasValue);
                data.FlushBits();

                if (CollisionHeight.HasValue)
                {
                    data.WriteFloat(CollisionHeight.Value.Height);
                    data.WriteFloat(CollisionHeight.Value.Scale);
                    data.WriteBits(CollisionHeight.Value.Reason, 2);
                    data.FlushBits();
                }

                if (Speed.HasValue)
                    data.WriteFloat(Speed.Value);

                if (KnockBack.HasValue)
                {
                    data.WriteFloat(KnockBack.Value.HorzSpeed);
                    data.WriteVector2(KnockBack.Value.Direction);
                    data.WriteFloat(KnockBack.Value.InitVertSpeed);
                }

                if (VehicleRecID.HasValue)
                    data.WriteInt32(VehicleRecID.Value);

                if (Unknown.HasValue)
                    data.WritePackedGuid(Unknown.Value);

                if (MovementForce_.HasValue)
                    MovementForce_.Value.Write(data);
            }

            public ServerOpcodes MessageID;
            public uint SequenceIndex;
            public Optional<float> Speed;
            public Optional<KnockBackInfo> KnockBack;
            public Optional<int> VehicleRecID;
            public Optional<CollisionHeightInfo> CollisionHeight;
            public Optional<MovementForce> MovementForce_;
            public Optional<ObjectGuid> Unknown;
        }
    }

    //Structs
    public struct MonsterSplineFilterKey
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt16(Idx);
            data.WriteUInt16(Speed);
        }

        public short Idx;
        public ushort Speed;
    }

    public class MonsterSplineFilter
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(FilterKeys.Count);
            data.WriteFloat(BaseSpeed);
            data.WriteInt16(StartOffset);
            data.WriteFloat(DistToPrevFilterKey);
            data.WriteInt16(AddedToStart);

            FilterKeys.ForEach(p => p.Write(data));

            data.WriteBits(FilterFlags, 2);
            data.FlushBits();
        }

        public List<MonsterSplineFilterKey> FilterKeys = new List<MonsterSplineFilterKey>();
        public byte FilterFlags;
        public float BaseSpeed;
        public short StartOffset;
        public float DistToPrevFilterKey;
        public short AddedToStart;
    }

    public struct MonsterSplineSpellEffectExtraData
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(TargetGuid);
            data.WriteUInt32(SpellVisualID);
            data.WriteUInt32(ProgressCurveID);
            data.WriteUInt32(ParabolicCurveID);
            data.WriteFloat(JumpGravity);
        }

        public ObjectGuid TargetGuid;
        public uint SpellVisualID;
        public uint ProgressCurveID;
        public uint ParabolicCurveID;
        public float JumpGravity;
    }

    public struct MonsterSplineJumpExtraData
    {
        public float JumpGravity;
        public uint StartTime;
        public uint Duration;

        public void Write(WorldPacket data)
        {
            data.WriteFloat(JumpGravity);
            data.WriteUInt32(StartTime);
            data.WriteUInt32(Duration);
        }
    }

    public class MovementSpline
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Flags);
            data.WriteUInt8(AnimTier);
            data.WriteUInt32(TierTransStartTime);
            data.WriteInt32(Elapsed);
            data.WriteUInt32(MoveTime);
            data.WriteUInt32(FadeObjectTime);
            data.WriteUInt8(Mode);
            data.WriteUInt8(VehicleExitVoluntary);
            data.WritePackedGuid(TransportGUID);
            data.WriteInt8(VehicleSeat);
            data.WriteBits((byte)Face, 2);
            data.WriteBits(Points.Count, 16);
            data.WriteBits(PackedDeltas.Count, 16);
            data.WriteBit(SplineFilter.HasValue);
            data.WriteBit(SpellEffectExtraData.HasValue);
            data.WriteBit(JumpExtraData.HasValue);
            data.FlushBits();

            if (SplineFilter.HasValue)
                SplineFilter.Value.Write(data);

            switch (Face)
            {
                case MonsterMoveType.FacingSpot:
                    data.WriteVector3(FaceSpot);
                    break;
                case MonsterMoveType.FacingTarget:
                    data.WriteFloat(FaceDirection);
                    data.WritePackedGuid(FaceGUID);
                    break;
                case MonsterMoveType.FacingAngle:
                    data.WriteFloat(FaceDirection);
                    break;
            }

            foreach (Vector3 pos in Points)
                data.WriteVector3(pos);

            foreach (Vector3 pos in PackedDeltas)
                data.WritePackXYZ(pos);

            if (SpellEffectExtraData.HasValue)
                SpellEffectExtraData.Value.Write(data);

            if (JumpExtraData.HasValue)
                JumpExtraData.Value.Write(data);
        }

        public uint Flags; // Spline flags
        public MonsterMoveType Face; // Movement direction (see MonsterMoveType enum)
        public byte AnimTier;
        public uint TierTransStartTime;
        public int Elapsed;
        public uint MoveTime;
        public uint FadeObjectTime;
        public List<Vector3> Points = new List<Vector3>(); // Spline path
        public byte Mode; // Spline mode - actually always 0 in this packet - Catmullrom mode appears only in SMSG_UPDATE_OBJECT. In this packet it is determined by flags
        public byte VehicleExitVoluntary;
        public ObjectGuid TransportGUID;
        public sbyte VehicleSeat = -1;
        public List<Vector3> PackedDeltas = new List<Vector3>();
        public Optional<MonsterSplineFilter> SplineFilter;
        public Optional<MonsterSplineSpellEffectExtraData> SpellEffectExtraData;
        public Optional<MonsterSplineJumpExtraData> JumpExtraData;
        public float FaceDirection;
        public ObjectGuid FaceGUID;
        public Vector3 FaceSpot;
    }

    public class MovementMonsterSpline
    {
        public MovementMonsterSpline()
        {
            Move = new MovementSpline();
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(ID);
            data.WriteVector3(Destination);
            data.WriteBit(CrzTeleport);
            data.WriteBits(StopDistanceTolerance, 3);

            Move.Write(data);
        }

        public uint ID;
        public Vector3 Destination;
        public bool CrzTeleport;
        public byte StopDistanceTolerance;    // Determines how far from spline destination the mover is allowed to stop in place 0, 0, 3.0, 2.76, numeric_limits<float>::max, 1.1, float(INT_MAX); default before this field existed was distance 3.0 (index 2)
        public MovementSpline Move;
    }

    public struct VehicleTeleport
    {
        public byte VehicleSeatIndex;
        public bool VehicleExitVoluntary;
        public bool VehicleExitTeleport;
    }

    public struct MovementForce
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(ID);
            data.WriteVector3(Origin);
            data.WriteVector3(Direction);
            data.WriteUInt32(TransportID);
            data.WriteFloat(Magnitude);
            data.WriteBits(Type, 2);
            data.FlushBits();
        }

        public ObjectGuid ID;
        public Vector3 Origin;
        public Vector3 Direction;
        public uint TransportID;
        public float Magnitude;
        public byte Type;
    }

    public struct MovementAck
    {
        public void Read(WorldPacket data)
        {
            Status = MovementExtensions.ReadMovementInfo(data);
            AckIndex = data.ReadInt32();
        }

        public MovementInfo Status;
        public int AckIndex;
    }

    public struct MoveKnockBackSpeeds
    {
        public void Write(WorldPacket data)
        {
            data.WriteFloat(HorzSpeed);
            data.WriteFloat(VertSpeed);
        }

        public void Read(WorldPacket data)
        {
            HorzSpeed = data.ReadFloat();
            VertSpeed = data.ReadFloat();
        }

        public float HorzSpeed;
        public float VertSpeed;
    }
}
