// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;
using Game.Movement;

namespace Game.Networking.Packets
{
    public static class MovementExtensions
    {
        public static void ReadTransportInfo(WorldPacket data, ref MovementInfo.TransportInfo transportInfo)
        {
            transportInfo.Guid = data.ReadPackedGuid(); // Transport Guid
            transportInfo.Pos.X = data.ReadFloat();
            transportInfo.Pos.Y = data.ReadFloat();
            transportInfo.Pos.Z = data.ReadFloat();
            transportInfo.Pos.Orientation = data.ReadFloat();
            transportInfo.Seat = data.ReadInt8();   // VehicleSeatIndex
            transportInfo.Time = data.ReadUInt32(); // MoveTime

            bool hasPrevTime = data.HasBit();
            bool hasVehicleId = data.HasBit();

            if (hasPrevTime)
                transportInfo.PrevTime = data.ReadUInt32(); // PrevMoveTime

            if (hasVehicleId)
                transportInfo.VehicleId = data.ReadUInt32(); // VehicleRecID
        }

        public static void WriteTransportInfo(WorldPacket data, MovementInfo.TransportInfo transportInfo)
        {
            bool hasPrevTime = transportInfo.PrevTime != 0;
            bool hasVehicleId = transportInfo.VehicleId != 0;

            data.WritePackedGuid(transportInfo.Guid); // Transport Guid
            data.WriteFloat(transportInfo.Pos.GetPositionX());
            data.WriteFloat(transportInfo.Pos.GetPositionY());
            data.WriteFloat(transportInfo.Pos.GetPositionZ());
            data.WriteFloat(transportInfo.Pos.GetOrientation());
            data.WriteInt8(transportInfo.Seat);   // VehicleSeatIndex
            data.WriteUInt32(transportInfo.Time); // MoveTime

            data.WriteBit(hasPrevTime);
            data.WriteBit(hasVehicleId);
            data.FlushBits();

            if (hasPrevTime)
                data.WriteUInt32(transportInfo.PrevTime); // PrevMoveTime

            if (hasVehicleId)
                data.WriteUInt32(transportInfo.VehicleId); // VehicleRecID
        }

        public static MovementInfo ReadMovementInfo(WorldPacket data)
        {
            var movementInfo = new MovementInfo();
            movementInfo.Guid = data.ReadPackedGuid();
            movementInfo.SetMovementFlags((MovementFlag)data.ReadUInt32());
            movementInfo.SetMovementFlags2((MovementFlag2)data.ReadUInt32());
            movementInfo.SetExtraMovementFlags2((MovementFlags3)data.ReadUInt32());
            movementInfo.Time = data.ReadUInt32();
            float x = data.ReadFloat();
            float y = data.ReadFloat();
            float z = data.ReadFloat();
            float o = data.ReadFloat();

            movementInfo.Pos.Relocate(x, y, z, o);
            movementInfo.Pitch = data.ReadFloat();
            movementInfo.stepUpStartElevation = data.ReadFloat();

            uint removeMovementForcesCount = data.ReadUInt32();

            uint moveIndex = data.ReadUInt32();

            for (uint i = 0; i < removeMovementForcesCount; ++i)
                data.ReadPackedGuid();

            // ResetBitReader

            bool hasTransport = data.HasBit();
            bool hasFall = data.HasBit();
            bool hasSpline = data.HasBit(); // todo 6.x read this infos

            data.ReadBit(); // HeightChangeFailed
            data.ReadBit(); // RemoteTimeValid
            bool hasInertia = data.HasBit();
            bool hasAdvFlying = data.HasBit();

            if (hasTransport)
                ReadTransportInfo(data, ref movementInfo.Transport);

            if (hasInertia)
            {
                MovementInfo.MovementInertia inertia = new();
                inertia.Id = data.ReadInt32();
                inertia.Force = data.ReadPosition();
                inertia.Lifetime = data.ReadUInt32();

                movementInfo.Inertia = inertia;
            }

            if (hasAdvFlying)
            {
                MovementInfo.AdvanFlying advFlying = new();

                advFlying.ForwardVelocity = data.ReadFloat();
                advFlying.UpVelocity = data.ReadFloat();
                movementInfo.AdvFlying = advFlying;
            }

            if (hasFall)
            {
                movementInfo.Jump.FallTime = data.ReadUInt32();
                movementInfo.Jump.Zspeed = data.ReadFloat();

                // ResetBitReader

                bool hasFallDirection = data.HasBit();

                if (hasFallDirection)
                {
                    movementInfo.Jump.SinAngle = data.ReadFloat();
                    movementInfo.Jump.CosAngle = data.ReadFloat();
                    movementInfo.Jump.XYspeed = data.ReadFloat();
                }
            }

            return movementInfo;
        }

        public static void WriteMovementInfo(WorldPacket data, MovementInfo movementInfo)
        {
            bool hasTransportData = !movementInfo.Transport.Guid.IsEmpty();
            bool hasFallDirection = movementInfo.HasMovementFlag(MovementFlag.Falling | MovementFlag.FallingFar);
            bool hasFallData = hasFallDirection || movementInfo.Jump.FallTime != 0;
            bool hasSpline = false; // todo 6.x send this infos
            bool hasInertia = movementInfo.Inertia.HasValue;
            bool hasAdvFlying = movementInfo.AdvFlying.HasValue;

            data.WritePackedGuid(movementInfo.Guid);
            data.WriteUInt32((uint)movementInfo.GetMovementFlags());
            data.WriteUInt32((uint)movementInfo.GetMovementFlags2());
            data.WriteUInt32((uint)movementInfo.GetExtraMovementFlags2());
            data.WriteUInt32(movementInfo.Time);
            data.WriteFloat(movementInfo.Pos.GetPositionX());
            data.WriteFloat(movementInfo.Pos.GetPositionY());
            data.WriteFloat(movementInfo.Pos.GetPositionZ());
            data.WriteFloat(movementInfo.Pos.GetOrientation());
            data.WriteFloat(movementInfo.Pitch);
            data.WriteFloat(movementInfo.stepUpStartElevation);

            uint removeMovementForcesCount = 0;
            data.WriteUInt32(removeMovementForcesCount);

            uint moveIndex = 0;
            data.WriteUInt32(moveIndex);

            /*for (public uint i = 0; i < removeMovementForcesCount; ++i)
			{
			    _worldPacket << ObjectGuid;
			}*/

            data.WriteBit(hasTransportData);
            data.WriteBit(hasFallData);
            data.WriteBit(hasSpline);
            data.WriteBit(false); // HeightChangeFailed
            data.WriteBit(false); // RemoteTimeValid
            data.WriteBit(hasInertia);
            data.WriteBit(hasAdvFlying);
            data.FlushBits();

            if (hasTransportData)
                WriteTransportInfo(data, movementInfo.Transport);

            if (hasInertia)
            {
                data.WriteInt32(movementInfo.Inertia.Value.Id);
                data.WriteXYZ(movementInfo.Inertia.Value.Force);
                data.WriteUInt32(movementInfo.Inertia.Value.Lifetime);
            }

            if (hasAdvFlying)
            {
                data.WriteFloat(movementInfo.AdvFlying.Value.ForwardVelocity);
                data.WriteFloat(movementInfo.AdvFlying.Value.UpVelocity);
            }

            if (hasFallData)
            {
                data.WriteUInt32(movementInfo.Jump.FallTime);
                data.WriteFloat(movementInfo.Jump.Zspeed);

                data.WriteBit(hasFallDirection);
                data.FlushBits();

                if (hasFallDirection)
                {
                    data.WriteFloat(movementInfo.Jump.SinAngle);
                    data.WriteFloat(movementInfo.Jump.CosAngle);
                    data.WriteFloat(movementInfo.Jump.XYspeed);
                }
            }
        }

        public static void WriteCreateObjectSplineDataBlock(MoveSpline moveSpline, WorldPacket data)
        {
            data.WriteUInt32(moveSpline.GetId()); // ID

            if (!moveSpline.IsCyclic()) // Destination
                data.WriteVector3(moveSpline.FinalDestination());
            else
                data.WriteVector3(Vector3.Zero);

            bool hasSplineMove = data.WriteBit(!moveSpline.Finalized() && !moveSpline.splineIsFacingOnly);
            data.FlushBits();

            if (hasSplineMove)
            {
                data.WriteUInt32((uint)moveSpline.splineflags.Flags); // SplineFlags
                data.WriteInt32(moveSpline.TimePassed());             // Elapsed
                data.WriteInt32(moveSpline.Duration());               // Duration
                data.WriteFloat(1.0f);                                // DurationModifier
                data.WriteFloat(1.0f);                                // NextDurationModifier
                data.WriteBits((byte)moveSpline.facing.type, 2);      // Face
                bool hasFadeObjectTime = data.WriteBit(moveSpline.splineflags.HasFlag(SplineFlag.FadeObject) && moveSpline.effect_start_time < moveSpline.Duration());
                data.WriteBits(moveSpline.GetPath().Length, 16);
                data.WriteBit(false);                                 // HasSplineFilter
                data.WriteBit(moveSpline.spell_effect_extra != null); // HasSpellEffectExtraData
                bool hasJumpExtraData = data.WriteBit(moveSpline.splineflags.HasFlag(SplineFlag.Parabolic) && (moveSpline.spell_effect_extra == null || moveSpline.effect_start_time != 0));
                data.WriteBit(moveSpline.anim_tier != null); // HasAnimTierTransition
                data.WriteBit(false);                        // HasUnknown901
                data.FlushBits();

                //if (HasSplineFilterKey)
                //{
                //    _data << uint32(FilterKeysCount);
                //    for (var i = 0; i < FilterKeysCount; ++i)
                //    {
                //        _data << float(In);
                //        _data << float(Out);
                //    }

                //    _data.WriteBits(FilterFlags, 2);
                //    _data.FlushBits();
                //}

                switch (moveSpline.facing.type)
                {
                    case MonsterMoveType.FacingSpot:
                        data.WriteVector3(moveSpline.facing.f); // FaceSpot

                        break;
                    case MonsterMoveType.FacingTarget:
                        data.WritePackedGuid(moveSpline.facing.target); // FaceGUID

                        break;
                    case MonsterMoveType.FacingAngle:
                        data.WriteFloat(moveSpline.facing.angle); // FaceDirection

                        break;
                }

                if (hasFadeObjectTime)
                    data.WriteInt32(moveSpline.effect_start_time); // FadeObjectTime

                foreach (var vec in moveSpline.GetPath())
                    data.WriteVector3(vec);

                if (moveSpline.spell_effect_extra != null)
                {
                    data.WritePackedGuid(moveSpline.spell_effect_extra.Target);
                    data.WriteUInt32(moveSpline.spell_effect_extra.SpellVisualId);
                    data.WriteUInt32(moveSpline.spell_effect_extra.ProgressCurveId);
                    data.WriteUInt32(moveSpline.spell_effect_extra.ParabolicCurveId);
                }

                if (hasJumpExtraData)
                {
                    data.WriteFloat(moveSpline.vertical_acceleration);
                    data.WriteInt32(moveSpline.effect_start_time);
                    data.WriteUInt32(0); // Duration (override)
                }

                if (moveSpline.anim_tier != null)
                {
                    data.WriteUInt32(moveSpline.anim_tier.TierTransitionId);
                    data.WriteInt32(moveSpline.effect_start_time);
                    data.WriteUInt32(0);
                    data.WriteUInt8(moveSpline.anim_tier.AnimTier);
                }

                //if (HasUnknown901)
                //{
                //    for (WorldPackets::Movement::MonsterSplineUnknown901::Inner const& unkInner : unk.Data) size = 16
                //    {
                //        _data << int32(unkInner.Unknown_1);
                //        _data << int32(unkInner.Unknown_2);
                //        _data << int32(unkInner.Unknown_3);
                //        _data << uint32(unkInner.Unknown_4);
                //    }
                //}
            }
        }

        public static void WriteCreateObjectAreaTriggerSpline(Spline<int> spline, WorldPacket data)
        {
            data.WriteBits(spline.GetPoints().Length, 16);

            foreach (var point in spline.GetPoints())
                data.WriteVector3(point);
        }

        public static void WriteMovementForceWithDirection(MovementForce movementForce, WorldPacket data, Position objectPosition = null)
        {
            data.WritePackedGuid(movementForce.ID);
            data.WriteVector3(movementForce.Origin);

            if (movementForce.Type == MovementForceType.Gravity &&
                objectPosition != null) // gravity
            {
                Vector3 direction = Vector3.Zero;

                if (movementForce.Magnitude != 0.0f)
                {
                    Position tmp = new(movementForce.Origin.X - objectPosition.GetPositionX(),
                                       movementForce.Origin.Y - objectPosition.GetPositionY(),
                                       movementForce.Origin.Z - objectPosition.GetPositionZ());

                    float lengthSquared = tmp.GetExactDistSq(0.0f, 0.0f, 0.0f);

                    if (lengthSquared > 0.0f)
                    {
                        float mult = 1.0f / (float)Math.Sqrt(lengthSquared) * movementForce.Magnitude;
                        tmp.X *= mult;
                        tmp.Y *= mult;
                        tmp.Z *= mult;

                        float minLengthSquared = (tmp.GetPositionX() * tmp.GetPositionX() * 0.04f) +
                                                 (tmp.GetPositionY() * tmp.GetPositionY() * 0.04f) +
                                                 (tmp.GetPositionZ() * tmp.GetPositionZ() * 0.04f);

                        if (lengthSquared > minLengthSquared)
                            direction = new Vector3(tmp.X, tmp.Y, tmp.Z);
                    }
                }

                data.WriteVector3(direction);
            }
            else
            {
                data.WriteVector3(movementForce.Direction);
            }

            data.WriteUInt32(movementForce.TransportID);
            data.WriteFloat(movementForce.Magnitude);
            data.WriteInt32(movementForce.Unused910);
            data.WriteBits((byte)movementForce.Type, 2);
            data.FlushBits();
        }
    }

    public class ClientPlayerMovement : ClientPacket
    {
        public MovementInfo Status;

        public ClientPlayerMovement(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Status = MovementExtensions.ReadMovementInfo(_worldPacket);
        }
    }

    public class MoveUpdate : ServerPacket
    {
        public MovementInfo Status;

        public MoveUpdate() : base(ServerOpcodes.MoveUpdate, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, Status);
        }
    }

    public class MonsterMove : ServerPacket
    {
        public ObjectGuid MoverGUID;
        public Vector3 Pos;

        public MovementMonsterSpline SplineData;

        public MonsterMove() : base(ServerOpcodes.OnMonsterMove, ConnectionType.Instance)
        {
            SplineData = new MovementMonsterSpline();
        }

        public void InitializeSplineData(MoveSpline moveSpline)
        {
            SplineData.Id = moveSpline.GetId();
            MovementSpline movementSpline = SplineData.Move;

            MoveSplineFlag splineFlags = moveSpline.splineflags;
            splineFlags.SetUnsetFlag(SplineFlag.Cyclic, moveSpline.IsCyclic());
            movementSpline.Flags = (uint)(splineFlags.Flags & ~SplineFlag.MaskNoMonsterMove);
            movementSpline.Face = moveSpline.facing.type;
            movementSpline.FaceDirection = moveSpline.facing.angle;
            movementSpline.FaceGUID = moveSpline.facing.target;
            movementSpline.FaceSpot = moveSpline.facing.f;

            if (splineFlags.HasFlag(SplineFlag.Animation))
            {
                MonsterSplineAnimTierTransition animTierTransition = new();
                animTierTransition.TierTransitionID = (int)moveSpline.anim_tier.TierTransitionId;
                animTierTransition.StartTime = (uint)moveSpline.effect_start_time;
                animTierTransition.AnimTier = moveSpline.anim_tier.AnimTier;
                movementSpline.AnimTierTransition = animTierTransition;
            }

            movementSpline.MoveTime = (uint)moveSpline.Duration();

            if (splineFlags.HasFlag(SplineFlag.Parabolic) &&
                (moveSpline.spell_effect_extra == null || moveSpline.effect_start_time != 0))
            {
                MonsterSplineJumpExtraData jumpExtraData = new();
                jumpExtraData.JumpGravity = moveSpline.vertical_acceleration;
                jumpExtraData.StartTime = (uint)moveSpline.effect_start_time;
                movementSpline.JumpExtraData = jumpExtraData;
            }

            if (splineFlags.HasFlag(SplineFlag.FadeObject))
                movementSpline.FadeObjectTime = (uint)moveSpline.effect_start_time;

            if (moveSpline.spell_effect_extra != null)
            {
                MonsterSplineSpellEffectExtraData spellEffectExtraData = new();
                spellEffectExtraData.TargetGuid = moveSpline.spell_effect_extra.Target;
                spellEffectExtraData.SpellVisualID = moveSpline.spell_effect_extra.SpellVisualId;
                spellEffectExtraData.ProgressCurveID = moveSpline.spell_effect_extra.ProgressCurveId;
                spellEffectExtraData.ParabolicCurveID = moveSpline.spell_effect_extra.ParabolicCurveId;
                spellEffectExtraData.JumpGravity = moveSpline.vertical_acceleration;
                movementSpline.SpellEffectExtraData = spellEffectExtraData;
            }

            var spline = moveSpline.spline;
            Vector3[] array = spline.GetPoints();

            if (splineFlags.HasFlag(SplineFlag.UncompressedPath))
            {
                if (!splineFlags.HasFlag(SplineFlag.Cyclic))
                {
                    int count = spline.GetPointCount() - 3;

                    for (uint i = 0; i < count; ++i)
                        movementSpline.Points.Add(array[i + 2]);
                }
                else
                {
                    int count = spline.GetPointCount() - 3;
                    movementSpline.Points.Add(array[1]);

                    for (uint i = 0; i < count; ++i)
                        movementSpline.Points.Add(array[i + 1]);
                }
            }
            else
            {
                int lastIdx = spline.GetPointCount() - 3;
                Span<Vector3> realPath = new Span<Vector3>(spline.GetPoints())[1..];

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
    }

    internal class FlightSplineSync : ServerPacket
    {
        public ObjectGuid Guid;
        public float SplineDist;

        public FlightSplineSync() : base(ServerOpcodes.FlightSplineSync, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteFloat(SplineDist);
        }
    }

    public class MoveSplineSetSpeed : ServerPacket
    {
        public ObjectGuid MoverGUID;
        public float Speed = 1.0f;

        public MoveSplineSetSpeed(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteFloat(Speed);
        }
    }

    public class MoveSetSpeed : ServerPacket
    {
        public ObjectGuid MoverGUID;
        public uint SequenceIndex; // Unit movement packet index, incremented each Time
        public float Speed = 1.0f;

        public MoveSetSpeed(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteFloat(Speed);
        }
    }

    public class MoveUpdateSpeed : ServerPacket
    {
        public float Speed = 1.0f;

        public MovementInfo Status;

        public MoveUpdateSpeed(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, Status);
            _worldPacket.WriteFloat(Speed);
        }
    }

    public class MoveSplineSetFlag : ServerPacket
    {
        public ObjectGuid MoverGUID;

        public MoveSplineSetFlag(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
        }
    }

    public class MoveSetFlag : ServerPacket
    {
        public ObjectGuid MoverGUID;
        public uint SequenceIndex; // Unit movement packet index, incremented each Time

        public MoveSetFlag(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(SequenceIndex);
        }
    }

    public class TransferPending : ServerPacket
    {
        public int MapID = -1;
        public Position OldMapPosition;
        public ShipTransferPending? Ship;
        public int? TransferSpellID;

        public TransferPending() : base(ServerOpcodes.TransferPending)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WriteXYZ(OldMapPosition);
            _worldPacket.WriteBit(Ship.HasValue);
            _worldPacket.WriteBit(TransferSpellID.HasValue);

            if (Ship.HasValue)
            {
                _worldPacket.WriteUInt32(Ship.Value.Id);
                _worldPacket.WriteInt32(Ship.Value.OriginMapID);
            }

            if (TransferSpellID.HasValue)
                _worldPacket.WriteInt32(TransferSpellID.Value);

            _worldPacket.FlushBits();
        }

        public struct ShipTransferPending
        {
            public uint Id;         // gameobject_template.entry of the Transport the player is teleporting on
            public int OriginMapID; // Map Id the player is currently on (before teleport)
        }
    }

    public class TransferAborted : ServerPacket
    {
        public byte Arg;
        public uint MapDifficultyXConditionID;

        public uint MapID;
        public TransferAbortReason TransfertAbort;

        public TransferAborted() : base(ServerOpcodes.TransferAborted)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MapID);
            _worldPacket.WriteUInt8(Arg);
            _worldPacket.WriteUInt32(MapDifficultyXConditionID);
            _worldPacket.WriteBits(TransfertAbort, 6);
            _worldPacket.FlushBits();
        }
    }

    public class NewWorld : ServerPacket
    {
        public TeleportLocation Loc = new();

        public uint MapID;
        public Position MovementOffset; // Adjusts all pending movement events by this offset
        public uint Reason;

        public NewWorld() : base(ServerOpcodes.NewWorld)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MapID);
            Loc.Write(_worldPacket);
            _worldPacket.WriteUInt32(Reason);
            _worldPacket.WriteXYZ(MovementOffset);
        }
    }

    public class WorldPortResponse : ClientPacket
    {
        public WorldPortResponse(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    public class MoveTeleport : ServerPacket
    {
        public float Facing;
        public ObjectGuid MoverGUID;

        public Position Pos;
        public byte PreloadWorld;
        public uint SequenceIndex;
        public ObjectGuid? TransportGUID;
        public VehicleTeleport? Vehicle;

        public MoveTeleport() : base(ServerOpcodes.MoveTeleport, ConnectionType.Instance)
        {
        }

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
    }

    public class MoveUpdateTeleport : ServerPacket
    {
        public float? FlightBackSpeed;
        public float? FlightSpeed;
        public List<MovementForce> MovementForces;
        public float? PitchRate;
        public float? RunBackSpeed;
        public float? RunSpeed;

        public MovementInfo Status;
        public float? SwimBackSpeed;
        public float? SwimSpeed;
        public float? TurnRate;
        public float? WalkSpeed;

        public MoveUpdateTeleport() : base(ServerOpcodes.MoveUpdateTeleport)
        {
        }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, Status);

            _worldPacket.WriteInt32(MovementForces != null ? MovementForces.Count : 0);
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

            if (MovementForces != null)
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
    }

    internal class MoveApplyMovementForce : ServerPacket
    {
        public MovementForce Force;

        public ObjectGuid MoverGUID;
        public int SequenceIndex;

        public MoveApplyMovementForce() : base(ServerOpcodes.MoveApplyMovementForce, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteInt32(SequenceIndex);
            Force.Write(_worldPacket);
        }
    }

    internal class MoveApplyMovementForceAck : ClientPacket
    {
        public MovementAck Ack = new();
        public MovementForce Force = new();

        public MoveApplyMovementForceAck(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Ack.Read(_worldPacket);
            Force.Read(_worldPacket);
        }
    }

    internal class MoveRemoveMovementForce : ServerPacket
    {
        public ObjectGuid ID;

        public ObjectGuid MoverGUID;
        public int SequenceIndex;

        public MoveRemoveMovementForce() : base(ServerOpcodes.MoveRemoveMovementForce, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteInt32(SequenceIndex);
            _worldPacket.WritePackedGuid(ID);
        }
    }

    internal class MoveRemoveMovementForceAck : ClientPacket
    {
        public MovementAck Ack = new();
        public ObjectGuid ID;

        public MoveRemoveMovementForceAck(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Ack.Read(_worldPacket);
            ID = _worldPacket.ReadPackedGuid();
        }
    }

    internal class MoveUpdateApplyMovementForce : ServerPacket
    {
        public MovementForce Force = new();

        public MovementInfo Status = new();

        public MoveUpdateApplyMovementForce() : base(ServerOpcodes.MoveUpdateApplyMovementForce)
        {
        }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, Status);
            Force.Write(_worldPacket);
        }
    }

    internal class MoveUpdateRemoveMovementForce : ServerPacket
    {
        public MovementInfo Status = new();
        public ObjectGuid TriggerGUID;

        public MoveUpdateRemoveMovementForce() : base(ServerOpcodes.MoveUpdateRemoveMovementForce)
        {
        }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, Status);
            _worldPacket.WritePackedGuid(TriggerGUID);
        }
    }

    internal class MoveTeleportAck : ClientPacket
    {
        private int AckIndex;

        public ObjectGuid MoverGUID;
        private int MoveTime;

        public MoveTeleportAck(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            MoverGUID = _worldPacket.ReadPackedGuid();
            AckIndex = _worldPacket.ReadInt32();
            MoveTime = _worldPacket.ReadInt32();
        }
    }

    public class MovementAckMessage : ClientPacket
    {
        public MovementAck Ack;

        public MovementAckMessage(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Ack.Read(_worldPacket);
        }
    }

    public class MovementSpeedAck : ClientPacket
    {
        public MovementAck Ack;
        public float Speed;

        public MovementSpeedAck(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Ack.Read(_worldPacket);
            Speed = _worldPacket.ReadFloat();
        }
    }

    public class SetActiveMover : ClientPacket
    {
        public ObjectGuid ActiveMover;

        public SetActiveMover(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ActiveMover = _worldPacket.ReadPackedGuid();
        }
    }

    public class MoveSetActiveMover : ServerPacket
    {
        public ObjectGuid MoverGUID;

        public MoveSetActiveMover() : base(ServerOpcodes.MoveSetActiveMover)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
        }
    }

    internal class MoveKnockBack : ServerPacket
    {
        public Vector2 Direction;

        public ObjectGuid MoverGUID;
        public uint SequenceIndex;
        public MoveKnockBackSpeeds Speeds;

        public MoveKnockBack() : base(ServerOpcodes.MoveKnockBack, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteVector2(Direction);
            Speeds.Write(_worldPacket);
        }
    }

    public class MoveUpdateKnockBack : ServerPacket
    {
        public MovementInfo Status;

        public MoveUpdateKnockBack() : base(ServerOpcodes.MoveUpdateKnockBack)
        {
        }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, Status);
        }
    }

    internal class MoveKnockBackAck : ClientPacket
    {
        public MovementAck Ack;
        public MoveKnockBackSpeeds? Speeds;

        public MoveKnockBackAck(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Ack.Read(_worldPacket);

            if (_worldPacket.HasBit())
            {
                Speeds = new MoveKnockBackSpeeds();
                Speeds.Value.Read(_worldPacket);
            }
        }
    }

    internal class MoveSetCollisionHeight : ServerPacket
    {
        public float Height = 1.0f;
        public uint MountDisplayID;
        public ObjectGuid MoverGUID;
        public UpdateCollisionHeightReason Reason;

        public float Scale = 1.0f;
        public int ScaleDuration;
        public uint SequenceIndex;

        public MoveSetCollisionHeight() : base(ServerOpcodes.MoveSetCollisionHeight)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteFloat(Height);
            _worldPacket.WriteFloat(Scale);
            _worldPacket.WriteUInt8((byte)Reason);
            _worldPacket.WriteUInt32(MountDisplayID);
            _worldPacket.WriteInt32(ScaleDuration);
        }
    }

    public class MoveUpdateCollisionHeight : ServerPacket
    {
        public float Height = 1.0f;
        public float Scale = 1.0f;

        public MovementInfo Status;

        public MoveUpdateCollisionHeight() : base(ServerOpcodes.MoveUpdateCollisionHeight)
        {
        }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, Status);
            _worldPacket.WriteFloat(Height);
            _worldPacket.WriteFloat(Scale);
        }
    }

    public class MoveSetCollisionHeightAck : ClientPacket
    {
        public MovementAck Data;
        public float Height = 1.0f;
        public uint MountDisplayID;
        public UpdateCollisionHeightReason Reason;

        public MoveSetCollisionHeightAck(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Data.Read(_worldPacket);
            Height = _worldPacket.ReadFloat();
            MountDisplayID = _worldPacket.ReadUInt32();
            Reason = (UpdateCollisionHeightReason)_worldPacket.ReadUInt8();
        }
    }

    internal class MoveTimeSkipped : ClientPacket
    {
        public ObjectGuid MoverGUID;
        public uint TimeSkipped;

        public MoveTimeSkipped(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            MoverGUID = _worldPacket.ReadPackedGuid();
            TimeSkipped = _worldPacket.ReadUInt32();
        }
    }

    internal class MoveSkipTime : ServerPacket
    {
        public ObjectGuid MoverGUID;
        public uint TimeSkipped;

        public MoveSkipTime() : base(ServerOpcodes.MoveSkipTime, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(TimeSkipped);
        }
    }

    internal class SummonResponse : ClientPacket
    {
        public bool Accept;
        public ObjectGuid SummonerGUID;

        public SummonResponse(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            SummonerGUID = _worldPacket.ReadPackedGuid();
            Accept = _worldPacket.HasBit();
        }
    }

    public class ControlUpdate : ServerPacket
    {
        public ObjectGuid Guid;

        public bool On;

        public ControlUpdate() : base(ServerOpcodes.ControlUpdate)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteBit(On);
            _worldPacket.FlushBits();
        }
    }

    internal class MoveSplineDone : ClientPacket
    {
        public int SplineID;

        public MovementInfo Status;

        public MoveSplineDone(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Status = MovementExtensions.ReadMovementInfo(_worldPacket);
            SplineID = _worldPacket.ReadInt32();
        }
    }

    internal class SummonRequest : ServerPacket
    {
        public enum SummonReason
        {
            Spell = 0,
            Scenario = 1
        }

        public int AreaID;
        public SummonReason Reason;
        public bool SkipStartingArea;

        public ObjectGuid SummonerGUID;
        public uint SummonerVirtualRealmAddress;

        public SummonRequest() : base(ServerOpcodes.SummonRequest, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(SummonerGUID);
            _worldPacket.WriteUInt32(SummonerVirtualRealmAddress);
            _worldPacket.WriteInt32(AreaID);
            _worldPacket.WriteUInt8((byte)Reason);
            _worldPacket.WriteBit(SkipStartingArea);
            _worldPacket.FlushBits();
        }
    }

    internal class SuspendToken : ServerPacket
    {
        public uint Reason = 1;

        public uint SequenceIndex = 1;

        public SuspendToken() : base(ServerOpcodes.SuspendToken, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteBits(Reason, 2);
            _worldPacket.FlushBits();
        }
    }

    internal class SuspendTokenResponse : ClientPacket
    {
        public uint SequenceIndex;

        public SuspendTokenResponse(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            SequenceIndex = _worldPacket.ReadUInt32();
        }
    }

    internal class ResumeToken : ServerPacket
    {
        public uint Reason = 1;

        public uint SequenceIndex = 1;

        public ResumeToken() : base(ServerOpcodes.ResumeToken, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteBits(Reason, 2);
            _worldPacket.FlushBits();
        }
    }

    internal class MoveSetCompoundState : ServerPacket
    {
        public ObjectGuid MoverGUID;
        public List<MoveStateChange> StateChanges = new();

        public MoveSetCompoundState() : base(ServerOpcodes.MoveSetCompoundState, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteInt32(StateChanges.Count);

            foreach (MoveStateChange stateChange in StateChanges)
                stateChange.Write(_worldPacket);
        }

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

        public class SpeedRange
        {
            public float Max;
            public float Min;
        }

        public class MoveStateChange
        {
            public CollisionHeightInfo? CollisionHeight;
            public KnockBackInfo? KnockBack;

            public ServerOpcodes MessageID;
            public MovementForce MovementForce;
            public ObjectGuid? MovementForceGUID;
            public int? MovementInertiaID;
            public uint? MovementInertiaLifetimeMs;
            public uint SequenceIndex;
            public float? Speed;
            public SpeedRange SpeedRange;
            public int? VehicleRecID;

            public MoveStateChange(ServerOpcodes messageId, uint sequenceIndex)
            {
                MessageID = messageId;
                SequenceIndex = sequenceIndex;
            }

            public void Write(WorldPacket data)
            {
                data.WriteUInt16((ushort)MessageID);
                data.WriteUInt32(SequenceIndex);
                data.WriteBit(Speed.HasValue);
                data.WriteBit(SpeedRange != null);
                data.WriteBit(KnockBack.HasValue);
                data.WriteBit(VehicleRecID.HasValue);
                data.WriteBit(CollisionHeight.HasValue);
                data.WriteBit(@MovementForce != null);
                data.WriteBit(MovementForceGUID.HasValue);
                data.WriteBit(MovementInertiaID.HasValue);
                data.WriteBit(MovementInertiaLifetimeMs.HasValue);
                data.FlushBits();

                @MovementForce?.Write(data);

                if (Speed.HasValue)
                    data.WriteFloat(Speed.Value);

                if (SpeedRange != null)
                {
                    data.WriteFloat(SpeedRange.Min);
                    data.WriteFloat(SpeedRange.Max);
                }

                if (KnockBack.HasValue)
                {
                    data.WriteFloat(KnockBack.Value.HorzSpeed);
                    data.WriteVector2(KnockBack.Value.Direction);
                    data.WriteFloat(KnockBack.Value.InitVertSpeed);
                }

                if (VehicleRecID.HasValue)
                    data.WriteInt32(VehicleRecID.Value);

                if (CollisionHeight.HasValue)
                {
                    data.WriteFloat(CollisionHeight.Value.Height);
                    data.WriteFloat(CollisionHeight.Value.Scale);
                    data.WriteBits(CollisionHeight.Value.Reason, 2);
                    data.FlushBits();
                }

                if (MovementForceGUID.HasValue)
                    data.WritePackedGuid(MovementForceGUID.Value);

                if (MovementInertiaID.HasValue)
                    data.WriteInt32(MovementInertiaID.Value);

                if (MovementInertiaLifetimeMs.HasValue)
                    data.WriteUInt32(MovementInertiaLifetimeMs.Value);
            }
        }
    }

    internal class MoveInitActiveMoverComplete : ClientPacket
    {
        public uint Ticks;

        public MoveInitActiveMoverComplete(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Ticks = _worldPacket.ReadUInt32();
        }
    }

    //Structs
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
        public short AddedToStart;
        public float BaseSpeed;
        public float DistToPrevFilterKey;
        public byte FilterFlags;

        public List<MonsterSplineFilterKey> FilterKeys = new();
        public short StartOffset;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(FilterKeys.Count);
            data.WriteFloat(BaseSpeed);
            data.WriteInt16(StartOffset);
            data.WriteFloat(DistToPrevFilterKey);
            data.WriteInt16(AddedToStart);

            FilterKeys.ForEach(p => p.Write(data));

            data.WriteBits(FilterFlags, 2);
            data.FlushBits();
        }
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

    public struct MonsterSplineAnimTierTransition
    {
        public int TierTransitionID;
        public uint StartTime;
        public uint EndTime;
        public byte AnimTier;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(TierTransitionID);
            data.WriteUInt32(StartTime);
            data.WriteUInt32(EndTime);
            data.WriteUInt8(AnimTier);
        }
    }

    public class MonsterSplineUnknown901
    {
        public Array<Inner> Data = new(16);

        public void Write(WorldPacket data)
        {
            foreach (var unkInner in Data)
            {
                data.WriteInt32(unkInner.Unknown_1);
                unkInner.Visual.Write(data);
                data.WriteUInt32(unkInner.Unknown_4);
            }
        }

        public struct Inner
        {
            public int Unknown_1;
            public SpellCastVisual Visual;
            public uint Unknown_4;
        }
    }

    public class TeleportLocation
    {
        public Position Pos;
        public int Unused901_1 = -1;
        public int Unused901_2 = -1;

        public void Write(WorldPacket data)
        {
            data.WriteXYZO(Pos);
            data.WriteInt32(Unused901_1);
            data.WriteInt32(Unused901_2);
        }
    }

    public class MovementSpline
    {
        public MonsterSplineAnimTierTransition? AnimTierTransition;
        public int Elapsed;
        public MonsterMoveType Face; // Movement direction (see MonsterMoveType enum)
        public float FaceDirection;
        public ObjectGuid FaceGUID;
        public Vector3 FaceSpot;
        public uint FadeObjectTime;

        public uint Flags; // Spline Flags
        public bool Interpolate;
        public MonsterSplineJumpExtraData? JumpExtraData;
        public byte Mode; // Spline mode - actually always 0 in this packet - Catmullrom mode appears only in SMSG_UPDATE_OBJECT. In this packet it is determined by Flags
        public uint MoveTime;
        public List<Vector3> PackedDeltas = new();
        public List<Vector3> Points = new(); // Spline path
        public MonsterSplineSpellEffectExtraData? SpellEffectExtraData;
        public MonsterSplineFilter SplineFilter;
        public ObjectGuid TransportGUID;
        public MonsterSplineUnknown901 Unknown901;
        public bool VehicleExitVoluntary;
        public sbyte VehicleSeat = -1;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Flags);
            data.WriteInt32(Elapsed);
            data.WriteUInt32(MoveTime);
            data.WriteUInt32(FadeObjectTime);
            data.WriteUInt8(Mode);
            data.WritePackedGuid(TransportGUID);
            data.WriteInt8(VehicleSeat);
            data.WriteBits((byte)Face, 2);
            data.WriteBits(Points.Count, 16);
            data.WriteBit(VehicleExitVoluntary);
            data.WriteBit(Interpolate);
            data.WriteBits(PackedDeltas.Count, 16);
            data.WriteBit(SplineFilter != null);
            data.WriteBit(SpellEffectExtraData.HasValue);
            data.WriteBit(JumpExtraData.HasValue);
            data.WriteBit(AnimTierTransition.HasValue);
            data.WriteBit(Unknown901 != null);
            data.FlushBits();

            SplineFilter?.Write(data);

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

            if (AnimTierTransition.HasValue)
                AnimTierTransition.Value.Write(data);

            Unknown901?.Write(data);
        }
    }

    public class MovementMonsterSpline
    {
        public bool CrzTeleport;
        public Vector3 Destination;

        public uint Id;
        public MovementSpline Move;
        public byte StopDistanceTolerance; // Determines how far from spline destination the mover is allowed to stop in place 0, 0, 3.0, 2.76, numeric_limits<float>::max, 1.1, float(INT_MAX); default before this field existed was distance 3.0 (index 2)

        public MovementMonsterSpline()
        {
            Move = new MovementSpline();
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Id);
            data.WriteVector3(Destination);
            data.WriteBit(CrzTeleport);
            data.WriteBits(StopDistanceTolerance, 3);

            Move.Write(data);
        }
    }

    public struct VehicleTeleport
    {
        public byte VehicleSeatIndex;
        public bool VehicleExitVoluntary;
        public bool VehicleExitTeleport;
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