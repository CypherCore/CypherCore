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

using Framework.Constants;
using Framework.Dynamic;
using Framework.GameMath;
using Game.Entities;
using System.Collections.Generic;


namespace Game.Network.Packets
{
    class AreaTriggerPkt : ClientPacket
    {
        public AreaTriggerPkt(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            AreaTriggerID = _worldPacket.ReadUInt32();
            Entered = _worldPacket.HasBit();
            FromClient = _worldPacket.HasBit();
        }

        public uint AreaTriggerID;
        public bool Entered;
        public bool FromClient;
    }

    class AreaTriggerDenied : ServerPacket
    {
        public AreaTriggerDenied() : base(ServerOpcodes.AreaTriggerDenied) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(AreaTriggerID);
            _worldPacket.WriteBit(Entered);
            _worldPacket.FlushBits();
        }

        public int AreaTriggerID;
        public bool Entered;
    }

    class AreaTriggerNoCorpse : ServerPacket
    {
        public AreaTriggerNoCorpse() : base(ServerOpcodes.AreaTriggerNoCorpse) { }

        public override void Write() { }
    }

    class AreaTriggerRePath : ServerPacket
    {
        public AreaTriggerRePath() : base(ServerOpcodes.AreaTriggerRePath) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid( TriggerGUID);
            AreaTriggerSpline.Write(_worldPacket);
        }

        public AreaTriggerSplineInfo AreaTriggerSpline = new AreaTriggerSplineInfo();
        public ObjectGuid TriggerGUID;
    }

    class AreaTriggerReShape : ServerPacket
    {
        public AreaTriggerReShape() : base(ServerOpcodes.AreaTriggerReShape) { }

        public override void Write()
        {
            _worldPacket .WritePackedGuid( TriggerGUID);

            _worldPacket.WriteBit(AreaTriggerSpline.HasValue);
            _worldPacket.WriteBit(AreaTriggerUnkType.HasValue);
            _worldPacket.FlushBits();

            if (AreaTriggerSpline.HasValue)
                AreaTriggerSpline.Value.Write(_worldPacket);

            if (AreaTriggerUnkType.HasValue)
                AreaTriggerUnkType.Value.Write(_worldPacket);
        }

        public Optional<AreaTriggerSplineInfo> AreaTriggerSpline;
        public Optional<AreaTriggerUnkTypeInfo> AreaTriggerUnkType;
        public ObjectGuid TriggerGUID;
    }

    //Structs
    class AreaTriggerSplineInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(TimeToTarget);
            data.WriteUInt32(ElapsedTimeForMovement);

            data.WriteBits(Points.Count, 16);
            data.FlushBits();

            foreach (Vector3 point in Points)
                data.WriteVector3(point);
        }

        public uint TimeToTarget;
        public uint ElapsedTimeForMovement;
        public List<Vector3> Points = new List<Vector3>();
    }

    struct AreaTriggerUnkTypeInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteBit(AreaTriggerUnkGUID.HasValue);
            data.WriteBit(Center.HasValue);
            data.WriteBit(UnkBit1);
            data.WriteBit(UnkBit2);

            data.WriteUInt32(UnkUInt1);
            data.WriteInt32(UnkInt1);
            data.WriteUInt32(UnkUInt2);
            data.WriteFloat(Radius);
            data.WriteFloat(BlendFromRadius);
            data.WriteFloat(InitialAngel);
            data.WriteFloat(ZOffset);

            if (AreaTriggerUnkGUID.HasValue)
                data.WritePackedGuid(AreaTriggerUnkGUID.Value);

            if (Center.HasValue)
                data.WriteVector3(Center.Value);
        }

        public Optional<ObjectGuid> AreaTriggerUnkGUID;
        public Optional<Vector3> Center;
        public bool UnkBit1;
        public bool UnkBit2;
        public uint UnkUInt1;
        public int UnkInt1;
        public uint UnkUInt2;
        public float Radius;
        public float BlendFromRadius;
        public float InitialAngel;
        public float ZOffset;
    }
}
