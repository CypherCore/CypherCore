// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
    public class GameObjUse : ClientPacket
    {
        public GameObjUse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Guid;
    }

    public class GameObjReportUse : ClientPacket
    {
        public GameObjReportUse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Guid;
    }

    class GameObjectDespawn : ServerPacket
    {
        public GameObjectDespawn() : base(ServerOpcodes.GameObjectDespawn) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ObjectGUID);
        }

        public ObjectGuid ObjectGUID;
    }

    class PageTextPkt : ServerPacket
    {
        public PageTextPkt() : base(ServerOpcodes.PageText) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(GameObjectGUID);
        }

        public ObjectGuid GameObjectGUID;
    }

    class GameObjectActivateAnimKit : ServerPacket
    {
        public GameObjectActivateAnimKit() : base(ServerOpcodes.GameObjectActivateAnimKit, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ObjectGUID);
            _worldPacket.WriteInt32(AnimKitID);
            _worldPacket.WriteBit(Maintain);
            _worldPacket.FlushBits();
        }

        public ObjectGuid ObjectGUID;
        public int AnimKitID;
        public bool Maintain;
    }

    class DestructibleBuildingDamage : ServerPacket
    {
        public DestructibleBuildingDamage() : base(ServerOpcodes.DestructibleBuildingDamage, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Target);
            _worldPacket.WritePackedGuid(Owner);
            _worldPacket.WritePackedGuid(Caster);
            _worldPacket.WriteInt32(Damage);
            _worldPacket.WriteUInt32(SpellID);
        }

        public ObjectGuid Target;
        public ObjectGuid Caster;
        public ObjectGuid Owner;
        public int Damage;
        public uint SpellID;
    }

    class FishNotHooked : ServerPacket
    {
        public FishNotHooked() : base(ServerOpcodes.FishNotHooked) { }

        public override void Write() { }
    }

    class FishEscaped : ServerPacket
    {
        public FishEscaped() : base(ServerOpcodes.FishEscaped) { }

        public override void Write() { }
    }

    class GameObjectCustomAnim : ServerPacket
    {
        public GameObjectCustomAnim() : base(ServerOpcodes.GameObjectCustomAnim, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ObjectGUID);
            _worldPacket.WriteUInt32(CustomAnim);
            _worldPacket.WriteBit(PlayAsDespawn);
            _worldPacket.FlushBits();
        }

        public ObjectGuid ObjectGUID;
        public uint CustomAnim;
        public bool PlayAsDespawn;
    }

    class GameObjectPlaySpellVisual : ServerPacket
    {
        public GameObjectPlaySpellVisual() : base(ServerOpcodes.GameObjectPlaySpellVisual) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ObjectGUID);
            _worldPacket.WritePackedGuid(ActivatorGUID);
            _worldPacket.WriteUInt32(SpellVisualID);
        }

        public ObjectGuid ObjectGUID;
        public ObjectGuid ActivatorGUID;
        public uint SpellVisualID;
    }

    class GameObjectSetStateLocal : ServerPacket
    {
        public GameObjectSetStateLocal() : base(ServerOpcodes.GameObjectSetStateLocal, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ObjectGUID);
            _worldPacket.WriteUInt8(State);
        }

        public ObjectGuid ObjectGUID;
        public byte State;
    }

    class GameObjectInteraction : ServerPacket
    {
        public ObjectGuid ObjectGUID;
        public PlayerInteractionType InteractionType;

        public GameObjectInteraction() : base(ServerOpcodes.GameObjectInteraction) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ObjectGUID);
            _worldPacket.WriteInt32((int)InteractionType);
        }
    }
    class GameObjectCloseInteraction : ServerPacket
    {
        public PlayerInteractionType InteractionType;

        public GameObjectCloseInteraction() : base(ServerOpcodes.GameObjectCloseInteraction) { }

        public override void Write()
        {
            _worldPacket.WriteInt32((int)InteractionType);
        }
    }
}
