// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
    public class MoveSetVehicleRecID : ServerPacket
    {
        public MoveSetVehicleRecID() : base(ServerOpcodes.MoveSetVehicleRecId) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(MoverGUID);
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteUInt32(VehicleRecID);
        }

        public ObjectGuid MoverGUID;
        public uint SequenceIndex;
        public uint VehicleRecID;
    }

    public class MoveSetVehicleRecIdAck : ClientPacket
    {
        public MoveSetVehicleRecIdAck(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Data.Read(_worldPacket);
            VehicleRecID = _worldPacket.ReadInt32();
        }

        public MovementAck Data;
        public int VehicleRecID;
    }

    public class SetVehicleRecID : ServerPacket
    {
        public SetVehicleRecID() : base(ServerOpcodes.SetVehicleRecId, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(VehicleGUID);
            _worldPacket.WriteUInt32(VehicleRecID);
        }

        public ObjectGuid VehicleGUID;
        public uint VehicleRecID;
    }

    public class OnCancelExpectedRideVehicleAura : ServerPacket
    {
        public OnCancelExpectedRideVehicleAura() : base(ServerOpcodes.OnCancelExpectedRideVehicleAura, ConnectionType.Instance) { }

        public override void Write() { }
    }

    public class MoveDismissVehicle : ClientPacket
    {
        public MoveDismissVehicle(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Status = MovementExtensions.ReadMovementInfo(_worldPacket);
        }

        public MovementInfo Status;
    }

    public class RequestVehiclePrevSeat : ClientPacket
    {
        public RequestVehiclePrevSeat(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class RequestVehicleNextSeat : ClientPacket
    {
        public RequestVehicleNextSeat(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class MoveChangeVehicleSeats : ClientPacket
    {
        public MoveChangeVehicleSeats(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Status = MovementExtensions.ReadMovementInfo(_worldPacket);
            DstVehicle = _worldPacket.ReadPackedGuid();
            DstSeatIndex = _worldPacket.ReadUInt8();
        }

        public ObjectGuid DstVehicle;
        public MovementInfo Status;
        public byte DstSeatIndex = 255;
    }

    public class RequestVehicleSwitchSeat : ClientPacket
    {
        public RequestVehicleSwitchSeat(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Vehicle = _worldPacket.ReadPackedGuid();
            SeatIndex = _worldPacket.ReadUInt8();
        }

        public ObjectGuid Vehicle;
        public byte SeatIndex = 255;
    }

    public class RideVehicleInteract : ClientPacket
    {
        public RideVehicleInteract(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Vehicle = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Vehicle;
    }

    public class EjectPassenger : ClientPacket
    {
        public EjectPassenger(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Passenger = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Passenger;
    }

    public class RequestVehicleExit : ClientPacket
    {
        public RequestVehicleExit(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }
}
