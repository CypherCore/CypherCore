// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Movement;
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

    class AreaTriggerPlaySpellVisual : ServerPacket
    {
        public ObjectGuid AreaTriggerGUID;
        public uint SpellVisualID;

        public AreaTriggerPlaySpellVisual() : base(ServerOpcodes.AreaTriggerPlaySpellVisual) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(AreaTriggerGUID);
            _worldPacket.WriteUInt32(SpellVisualID);
        }
    }

    class UpdateAreaTriggerVisual : ClientPacket
    {
        public int SpellID;
        public SpellCastVisual Visual;
        public ObjectGuid TargetGUID;

        public UpdateAreaTriggerVisual(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SpellID = _worldPacket.ReadInt32();
            Visual.Read(_worldPacket);
            TargetGUID = _worldPacket.ReadPackedGuid();
        }
    }
}
