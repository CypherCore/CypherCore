// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Movement;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Networking.Packets
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
            {
                data.ReadPackedGuid();
            }

            bool hasStandingOnGameObjectGUID = data.HasBit();
            bool hasTransport = data.HasBit();
            bool hasFall = data.HasBit();
            bool hasSpline = data.HasBit(); // todo 6.x read this infos

            data.ReadBit(); // HeightChangeFailed
            data.ReadBit(); // RemoteTimeValid
            bool hasInertia = data.HasBit();
            bool hasAdvFlying = data.HasBit();

            if (hasTransport)
                ReadTransportInfo(data, ref movementInfo.transport);

            if (hasStandingOnGameObjectGUID)
                movementInfo.standingOnGameObjectGUID = data.ReadPackedGuid();

            if (hasInertia)
            {
                MovementInfo.Inertia inertia = new();
                inertia.id = data.ReadInt32();
                inertia.force = data.ReadPosition();
                inertia.lifetime = data.ReadUInt32();

                movementInfo.inertia = inertia;
            }

            if (hasAdvFlying)
            {
                MovementInfo.AdvFlying advFlying = new();

                advFlying.forwardVelocity = data.ReadFloat();
                advFlying.upVelocity = data.ReadFloat();
                movementInfo.advFlying = advFlying;
            }

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
            bool hasInertia = movementInfo.inertia.HasValue;
            bool hasAdvFlying = movementInfo.advFlying.HasValue;
            bool hasStandingOnGameObjectGUID = movementInfo.standingOnGameObjectGUID.HasValue;

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

            data.WriteBit(hasStandingOnGameObjectGUID);
            data.WriteBit(hasTransportData);
            data.WriteBit(hasFallData);
            data.WriteBit(hasSpline);
            data.WriteBit(false); // HeightChangeFailed
            data.WriteBit(false); // RemoteTimeValid
            data.WriteBit(hasInertia);
            data.WriteBit(hasAdvFlying);
            data.FlushBits();

            if (hasTransportData)
                WriteTransportInfo(data, movementInfo.transport);

            if (hasStandingOnGameObjectGUID)
                data.WritePackedGuid(movementInfo.standingOnGameObjectGUID.Value);

            if (hasInertia)
            {
                data.WriteInt32(movementInfo.inertia.Value.id);
                data.WriteXYZ(movementInfo.inertia.Value.force);
                data.WriteUInt32(movementInfo.inertia.Value.lifetime);
            }

            if (hasAdvFlying)
            {
                data.WriteFloat(movementInfo.advFlying.Value.forwardVelocity);
                data.WriteFloat(movementInfo.advFlying.Value.upVelocity);
            }

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

            if (!moveSpline.IsCyclic())                                                 // Destination
                data.WriteVector3(moveSpline.FinalDestination());
            else
            {
                var spline = moveSpline.spline;
                if (spline.GetPointCount() <= 1)
                    data.WriteVector3(Vector3.Zero);
                else
                    data.WriteVector3(spline.GetPoint(spline.Last() - 1));
            }

            bool hasSplineMove = data.WriteBit(!moveSpline.Finalized() && !moveSpline.splineIsFacingOnly);
            data.FlushBits();

            if (hasSplineMove)
            {
                data.WriteUInt32((uint)moveSpline.splineflags.Flags);   // SplineFlags
                data.WriteInt32(moveSpline.TimePassed());               // Elapsed
                data.WriteInt32(moveSpline.Duration());                // Duration
                data.WriteFloat(1.0f);                                  // DurationModifier
                data.WriteFloat(1.0f);                                  // NextDurationModifier
                data.WriteBits((byte)moveSpline.facing.type, 2);        // Face
                bool hasFadeObjectTime = data.WriteBit(moveSpline.splineflags.HasFlag(MoveSplineFlagEnum.FadeObject) && moveSpline.effect_start_time < moveSpline.Duration());
                data.WriteBits(moveSpline.GetPath().Length, 16);
                data.WriteBit(false);                                       // HasSplineFilter
                data.WriteBit(moveSpline.spell_effect_extra != null);  // HasSpellEffectExtraData
                bool hasJumpExtraData = data.WriteBit(moveSpline.splineflags.HasFlag(MoveSplineFlagEnum.Parabolic) && (moveSpline.spell_effect_extra == null || moveSpline.effect_start_time != 0));
                data.WriteBit(moveSpline.anim_tier != null);                   // HasAnimTierTransition
                data.WriteBit(false);                                                   // HasUnknown901
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
                    data.WriteInt32(moveSpline.effect_start_time);     // FadeObjectTime

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
                    data.WriteUInt32(0);                                                  // Duration (override)
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
                //        data << int32(unkInner.Unknown_1);
                //        data << int32(unkInner.Unknown_2);
                //        data << int32(unkInner.Unknown_3);
                //        data << uint32(unkInner.Unknown_4);
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
            if (movementForce.Type == MovementForceType.Gravity && objectPosition != null) // gravity
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
                        tmp.posX *= mult;
                        tmp.posY *= mult;
                        tmp.posZ *= mult;
                        float minLengthSquared = (tmp.GetPositionX() * tmp.GetPositionX() * 0.04f) +
                            (tmp.GetPositionY() * tmp.GetPositionY() * 0.04f) +
                            (tmp.GetPositionZ() * tmp.GetPositionZ() * 0.04f);
                        if (lengthSquared > minLengthSquared)
                            direction = new Vector3(tmp.posX, tmp.posY, tmp.posZ);
                    }
                }

                data.WriteVector3(direction);
            }
            else
                data.WriteVector3(movementForce.Direction);

            data.WriteUInt32(movementForce.TransportID);
            data.WriteFloat(movementForce.Magnitude);
            data.WriteInt32(movementForce.Unused910);
            data.WriteBits((byte)movementForce.Type, 2);
            data.FlushBits();
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
            SplineData.Id = moveSpline.GetId();
            MovementSpline movementSpline = SplineData.Move;

            MoveSplineFlag splineFlags = moveSpline.splineflags;
            movementSpline.Flags = (uint)(splineFlags.Flags & ~MoveSplineFlagEnum.MaskNoMonsterMove);
            movementSpline.Face = moveSpline.facing.type;
            movementSpline.FaceDirection = moveSpline.facing.angle;
            movementSpline.FaceGUID = moveSpline.facing.target;
            movementSpline.FaceSpot = moveSpline.facing.f;

            if (moveSpline.anim_tier != null)
            {
                MonsterSplineAnimTierTransition animTierTransition = new();
                animTierTransition.TierTransitionID = (int)moveSpline.anim_tier.TierTransitionId;
                animTierTransition.StartTime = (uint)moveSpline.effect_start_time;
                animTierTransition.AnimTier = moveSpline.anim_tier.AnimTier;
                movementSpline.AnimTierTransition = animTierTransition;
            }

            movementSpline.MoveTime = (uint)moveSpline.Duration();

            if (splineFlags.HasFlag(MoveSplineFlagEnum.Parabolic) && (moveSpline.spell_effect_extra == null || moveSpline.effect_start_time != 0))
            {
                MonsterSplineJumpExtraData jumpExtraData = new();
                jumpExtraData.JumpGravity = moveSpline.vertical_acceleration;
                jumpExtraData.StartTime = (uint)moveSpline.effect_start_time;
                movementSpline.JumpExtraData = jumpExtraData;
            }

            if (splineFlags.HasFlag(MoveSplineFlagEnum.FadeObject))
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

            if (splineFlags.HasFlag(MoveSplineFlagEnum.UncompressedPath))
            {
                int count = spline.GetPointCount() - (splineFlags.HasFlag(MoveSplineFlagEnum.Cyclic) ? 4 : 3);
                for (int i = 0; i < count; ++i)
                    movementSpline.Points.Add(new Vector3(array[i + 2].X, array[i + 2].Y, array[i + 2].Z));
            }
            else
            {
                int lastIdx = spline.GetPointCount() - (splineFlags.HasFlag(MoveSplineFlagEnum.Cyclic) ? 4 : 3);
                Vector3[] realPath = array[1..];

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

    class FlightSplineSync : ServerPacket
    {
        public FlightSplineSync() : base(ServerOpcodes.FlightSplineSync, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteFloat(SplineDist);
        }

        public ObjectGuid Guid;
        public float SplineDist;
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
        public uint SequenceIndex; // Unit movement packet index, incremented each time
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

    class SetAdvFlyingSpeed : ServerPacket
    {
        public ObjectGuid MoverGUID;
        public uint SequenceIndex;
        public float Speed = 1.0f;

        public SetAdvFlyingSpeed(ServerOpcodes opcode) : base(opcode) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteFloat(Speed);
        }
    }

    class SetAdvFlyingSpeedRange : ServerPacket
    {
        public ObjectGuid MoverGUID;
        public uint SequenceIndex;
        public float SpeedMin = 1.0f;
        public float SpeedMax = 1.0f;

        public SetAdvFlyingSpeedRange(ServerOpcodes opcode) : base(opcode) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteFloat(SpeedMin);
            _worldPacket.WriteFloat(SpeedMax);
        }
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
        public uint SequenceIndex; // Unit movement packet index, incremented each time
    }

    public class TransferPending : ServerPacket
    {
        public TransferPending() : base(ServerOpcodes.TransferPending) { }

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

        public int MapID = -1;
        public Position OldMapPosition;
        public ShipTransferPending? Ship;
        public int? TransferSpellID;

        public struct ShipTransferPending
        {
            public uint Id;              // gameobject_template.entry of the transport the player is teleporting on
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
            _worldPacket.WriteUInt32(MapDifficultyXConditionID);
            _worldPacket.WriteBits(TransfertAbort, 6);
            _worldPacket.FlushBits();
        }

        public uint MapID;
        public byte Arg;
        public uint MapDifficultyXConditionID;
        public TransferAbortReason TransfertAbort;
    }

    public class NewWorld : ServerPacket
    {
        public NewWorld() : base(ServerOpcodes.NewWorld) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MapID);
            Loc.Write(_worldPacket);
            _worldPacket.WriteUInt32(Reason);
            _worldPacket.WriteXYZ(MovementOffset);
            _worldPacket.WriteInt32(Counter);
        }

        public uint MapID;
        public uint Reason;
        public TeleportLocation Loc = new();
        public Position MovementOffset;    // Adjusts all pending movement events by this offset
        public int Counter;
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
        public VehicleTeleport? Vehicle;
        public uint SequenceIndex;
        public ObjectGuid MoverGUID;
        public ObjectGuid? TransportGUID;
        public float Facing;
        public byte PreloadWorld;
    }

    public class MoveUpdateTeleport : ServerPacket
    {
        public MoveUpdateTeleport() : base(ServerOpcodes.MoveUpdateTeleport) { }

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

        public MovementInfo Status;
        public List<MovementForce> MovementForces;
        public float? SwimBackSpeed;
        public float? FlightSpeed;
        public float? SwimSpeed;
        public float? WalkSpeed;
        public float? TurnRate;
        public float? RunSpeed;
        public float? FlightBackSpeed;
        public float? RunBackSpeed;
        public float? PitchRate;
    }

    class MoveApplyMovementForce : ServerPacket
    {
        public MoveApplyMovementForce() : base(ServerOpcodes.MoveApplyMovementForce, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteInt32(SequenceIndex);
            Force.Write(_worldPacket);
        }

        public ObjectGuid MoverGUID;
        public int SequenceIndex;
        public MovementForce Force;
    }

    class MoveApplyMovementForceAck : ClientPacket
    {
        public MoveApplyMovementForceAck(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Ack.Read(_worldPacket);
            Force.Read(_worldPacket);
        }

        public MovementAck Ack = new();
        public MovementForce Force = new();
    }

    class MoveRemoveMovementForce : ServerPacket
    {
        public MoveRemoveMovementForce() : base(ServerOpcodes.MoveRemoveMovementForce, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteInt32(SequenceIndex);
            _worldPacket.WritePackedGuid(ID);
        }

        public ObjectGuid MoverGUID;
        public int SequenceIndex;
        public ObjectGuid ID;
    }

    class MoveRemoveMovementForceAck : ClientPacket
    {
        public MoveRemoveMovementForceAck(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Ack.Read(_worldPacket);
            ID = _worldPacket.ReadPackedGuid();
        }

        public MovementAck Ack = new();
        public ObjectGuid ID;
    }

    class MoveUpdateApplyMovementForce : ServerPacket
    {
        public MoveUpdateApplyMovementForce() : base(ServerOpcodes.MoveUpdateApplyMovementForce) { }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, Status);
            Force.Write(_worldPacket);
        }

        public MovementInfo Status = new();
        public MovementForce Force = new();
    }

    class MoveUpdateRemoveMovementForce : ServerPacket
    {
        public MoveUpdateRemoveMovementForce() : base(ServerOpcodes.MoveUpdateRemoveMovementForce) { }

        public override void Write()
        {
            MovementExtensions.WriteMovementInfo(_worldPacket, Status);
            _worldPacket.WritePackedGuid(TriggerGUID);
        }

        public MovementInfo Status = new();
        public ObjectGuid TriggerGUID;
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
        public int AckIndex;
        public int MoveTime;
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

    class MovementSpeedRangeAck : ClientPacket
    {
        public MovementAck Ack;
        public float SpeedMin = 1.0f;
        public float SpeedMax = 1.0f;

        public MovementSpeedRangeAck(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Ack.Read(_worldPacket);
            SpeedMin = _worldPacket.ReadFloat();
            SpeedMax = _worldPacket.ReadFloat();
        }
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
                Speeds = new();
                Speeds.Value.Read(_worldPacket);
            }
        }

        public MovementAck Ack;
        public MoveKnockBackSpeeds? Speeds;
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
            _worldPacket.WriteUInt8((byte)Reason);
            _worldPacket.WriteUInt32(MountDisplayID);
            _worldPacket.WriteInt32(ScaleDuration);
        }

        public float Scale = 1.0f;
        public ObjectGuid MoverGUID;
        public uint MountDisplayID;
        public UpdateCollisionHeightReason Reason;
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
            Reason = (UpdateCollisionHeightReason)_worldPacket.ReadUInt8();
        }

        public MovementAck Data;
        public UpdateCollisionHeightReason Reason;
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

    class MoveSkipTime : ServerPacket
    {
        public MoveSkipTime() : base(ServerOpcodes.MoveSkipTime, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(TimeSkipped);
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
            _worldPacket.WriteUInt8((byte)Reason);
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
            _worldPacket.WriteInt32(StateChanges.Count);
            foreach (MoveStateChange stateChange in StateChanges)
                stateChange.Write(_worldPacket);
        }

        public ObjectGuid MoverGUID;
        public List<MoveStateChange> StateChanges = new();

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
            public float Min;
            public float Max;
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

                if (@MovementForce != null)
                    @MovementForce.Write(data);

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

            public ServerOpcodes MessageID;
            public uint SequenceIndex;
            public float? Speed;
            public SpeedRange SpeedRange;
            public KnockBackInfo? KnockBack;
            public int? VehicleRecID;
            public CollisionHeightInfo? CollisionHeight;
            public MovementForce MovementForce;
            public ObjectGuid? MovementForceGUID;
            public int? MovementInertiaID;
            public uint? MovementInertiaLifetimeMs;
        }
    }

    class MoveInitActiveMoverComplete : ClientPacket
    {
        public uint Ticks;

        public MoveInitActiveMoverComplete(WorldPacket packet) : base(packet) { }

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

        public List<MonsterSplineFilterKey> FilterKeys = new();
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

            if (SplineFilter != null)
                SplineFilter.Write(data);

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

            if (Unknown901 != null)
                Unknown901.Write(data);
        }

        public uint Flags; // Spline flags
        public MonsterMoveType Face; // Movement direction (see MonsterMoveType enum)
        public int Elapsed;
        public uint MoveTime;
        public uint FadeObjectTime;
        public List<Vector3> Points = new(); // Spline path
        public byte Mode; // Spline mode - actually always 0 in this packet - Catmullrom mode appears only in SMSG_UPDATE_OBJECT. In this packet it is determined by flags
        public bool VehicleExitVoluntary;
        public bool Interpolate;
        public ObjectGuid TransportGUID;
        public sbyte VehicleSeat = -1;
        public List<Vector3> PackedDeltas = new();
        public MonsterSplineFilter SplineFilter;
        public MonsterSplineSpellEffectExtraData? SpellEffectExtraData;
        public MonsterSplineJumpExtraData? JumpExtraData;
        public MonsterSplineAnimTierTransition? AnimTierTransition;
        public MonsterSplineUnknown901 Unknown901;
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
            data.WriteUInt32(Id);
            data.WriteBit(CrzTeleport);
            data.WriteBits(StopDistanceTolerance, 3);

            Move.Write(data);
        }

        public uint Id;
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
