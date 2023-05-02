// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Networking.Packets
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
            _worldPacket.WritePackedGuid(TriggerGUID);

            _worldPacket.WriteBit(AreaTriggerSpline != null);
            _worldPacket.WriteBit(AreaTriggerOrbit != null);
            _worldPacket.WriteBit(AreaTriggerMovementScript.HasValue);
            _worldPacket.FlushBits();

            if (AreaTriggerSpline != null)
                AreaTriggerSpline.Write(_worldPacket);

            if (AreaTriggerMovementScript.HasValue)
                AreaTriggerMovementScript.Value.Write(_worldPacket);

            if (AreaTriggerOrbit != null)
                AreaTriggerOrbit.Write(_worldPacket);
        }

        public AreaTriggerSplineInfo AreaTriggerSpline;
        public AreaTriggerOrbitInfo AreaTriggerOrbit;
        public AreaTriggerMovementScriptInfo? AreaTriggerMovementScript;
        public ObjectGuid TriggerGUID;
    }

    //Structs
    class AreaTriggerSplineInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(TimeToTarget);
            data.WriteUInt32(ElapsedTimeForMovement);

            data.WriteBits(Points.Length, 16);
            data.FlushBits();

            foreach (Vector3 point in Points)
                data.WriteVector3(point);
        }

        public uint TimeToTarget;
        public uint ElapsedTimeForMovement;
        public Vector3[] Points = new Vector3[0];
    }
}
