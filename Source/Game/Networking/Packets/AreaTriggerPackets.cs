// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
    internal class AreaTriggerPkt : ClientPacket
    {
        public uint AreaTriggerID;
        public bool Entered;
        public bool FromClient;

        public AreaTriggerPkt(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            AreaTriggerID = _worldPacket.ReadUInt32();
            Entered = _worldPacket.HasBit();
            FromClient = _worldPacket.HasBit();
        }
    }

    internal class AreaTriggerDenied : ServerPacket
    {
        public int AreaTriggerID;
        public bool Entered;

        public AreaTriggerDenied() : base(ServerOpcodes.AreaTriggerDenied)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(AreaTriggerID);
            _worldPacket.WriteBit(Entered);
            _worldPacket.FlushBits();
        }
    }

    internal class AreaTriggerNoCorpse : ServerPacket
    {
        public AreaTriggerNoCorpse() : base(ServerOpcodes.AreaTriggerNoCorpse)
        {
        }

        public override void Write()
        {
        }
    }

    internal class AreaTriggerRePath : ServerPacket
    {
        public AreaTriggerMovementScriptInfo? AreaTriggerMovementScript;
        public AreaTriggerOrbitInfo AreaTriggerOrbit;

        public AreaTriggerSplineInfo AreaTriggerSpline;
        public ObjectGuid TriggerGUID;

        public AreaTriggerRePath() : base(ServerOpcodes.AreaTriggerRePath)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(TriggerGUID);

            _worldPacket.WriteBit(AreaTriggerSpline != null);
            _worldPacket.WriteBit(AreaTriggerOrbit != null);
            _worldPacket.WriteBit(AreaTriggerMovementScript.HasValue);
            _worldPacket.FlushBits();

            AreaTriggerSpline?.Write(_worldPacket);

            if (AreaTriggerMovementScript.HasValue)
                AreaTriggerMovementScript.Value.Write(_worldPacket);

            AreaTriggerOrbit?.Write(_worldPacket);
        }
    }

    //Structs
    internal class AreaTriggerSplineInfo
    {
        public uint ElapsedTimeForMovement;
        public List<Vector3> Points = new();

        public uint TimeToTarget;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(TimeToTarget);
            data.WriteUInt32(ElapsedTimeForMovement);

            data.WriteBits(Points.Count, 16);
            data.FlushBits();

            foreach (Vector3 point in Points)
                data.WriteVector3(point);
        }
    }
}