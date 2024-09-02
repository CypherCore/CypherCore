// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
    class PlayerAzeriteItemGains : ServerPacket
    {
        public PlayerAzeriteItemGains() : base(ServerOpcodes.PlayerAzeriteItemGains) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGUID);
            _worldPacket.WriteUInt64(XP);
        }

        public ObjectGuid ItemGUID;
        public ulong XP;
    }

    class AzeriteEssenceUnlockMilestone : ClientPacket
    {
        public AzeriteEssenceUnlockMilestone(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            AzeriteItemMilestonePowerID = _worldPacket.ReadInt32();
        }

        public int AzeriteItemMilestonePowerID;
    }

    class AzeriteEssenceActivateEssence : ClientPacket
    {
        public AzeriteEssenceActivateEssence(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            AzeriteEssenceID = _worldPacket.ReadUInt32();
            Slot = _worldPacket.ReadUInt8();
        }

        public uint AzeriteEssenceID;
        public byte Slot;
    }

    class ActivateEssenceFailed : ServerPacket
    {
        public ActivateEssenceFailed() : base(ServerOpcodes.ActivateEssenceFailed) { }

        public override void Write()
        {
            _worldPacket.WriteBits((int)Reason, 4);
            _worldPacket.WriteBit(Slot.HasValue);
            _worldPacket.WriteUInt32(Arg);
            _worldPacket.WriteUInt32(AzeriteEssenceID);
            if (Slot.HasValue)
                _worldPacket.WriteUInt8(Slot.Value);
        }

        public AzeriteEssenceActivateResult Reason;
        public uint Arg;
        public uint AzeriteEssenceID;
        public byte? Slot;
    }

    class AzeriteEmpoweredItemViewed : ClientPacket
    {
        public AzeriteEmpoweredItemViewed(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ItemGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ItemGUID;
    }

    class AzeriteEmpoweredItemSelectPower : ClientPacket
    {
        public AzeriteEmpoweredItemSelectPower(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ContainerSlot = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
            Tier = _worldPacket.ReadUInt8();
            AzeritePowerID = _worldPacket.ReadInt32();
        }

        public byte Tier;
        public int AzeritePowerID;
        public byte ContainerSlot;
        public byte Slot;
    }

    public class PlayerAzeriteItemEquippedStatusChanged : ServerPacket
    {
        public PlayerAzeriteItemEquippedStatusChanged() : base(ServerOpcodes.PlayerAzeriteItemEquippedStatusChanged) { }

        public override void Write()
        {
            _worldPacket.WriteBit(IsHeartEquipped);
            _worldPacket.FlushBits();
        }

        public bool IsHeartEquipped;
    }
}
