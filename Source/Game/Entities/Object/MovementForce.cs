// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;

namespace Game.Entities
{
    public class MovementForce
    {
        public Vector3 Direction;
        public ObjectGuid ID;
        public Vector3 Origin;
        public float Magnitude { get; set; }
        public uint TransportID { get; set; }
        public MovementForceType Type { get; set; }
        public int Unused910 { get; set; }

        public void Read(WorldPacket data)
        {
            ID = data.ReadPackedGuid();
            Origin = data.ReadVector3();
            Direction = data.ReadVector3();
            TransportID = data.ReadUInt32();
            Magnitude = data.ReadFloat();
            Unused910 = data.ReadInt32();
            Type = (MovementForceType)data.ReadBits<byte>(2);
        }

        public void Write(WorldPacket data)
        {
            MovementExtensions.WriteMovementForceWithDirection(this, data);
        }
    }
}