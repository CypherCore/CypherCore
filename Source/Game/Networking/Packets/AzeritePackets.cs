/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.Entities;
using Framework.Dynamic;

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

    class OpenHeartForge : ServerPacket
    {
        public OpenHeartForge() : base(ServerOpcodes.OpenHeartForge) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ForgeGUID);
        }

        public ObjectGuid ForgeGUID;
    }

    class CloseHeartForge : ServerPacket
    {
        public CloseHeartForge() : base(ServerOpcodes.CloseHeartForge) { }

        public override void Write() { }
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
            Tier = _worldPacket.ReadInt32();
            AzeritePowerID = _worldPacket.ReadInt32();
            ContainerSlot = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
        }

        public int Tier;
        public int AzeritePowerID;
        public byte ContainerSlot;
        public byte Slot;
    }

    class PlayerAzeriteItemEquippedStatusChanged : ServerPacket
    {
        public PlayerAzeriteItemEquippedStatusChanged() : base(ServerOpcodes.PlayerAzeriteItemEquippedStatusChanged) { }

        public override void Write()
        {
            _worldPacket.WriteBit(IsHeartEquipped);
            _worldPacket.FlushBits();
        }

        public bool IsHeartEquipped;
    }

    class AzeriteRespecNPC : ServerPacket
    {
        public AzeriteRespecNPC(ObjectGuid npcGuid) : base(ServerOpcodes.AzeriteRespecNpc)
        {
            NpcGUID = npcGuid;
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(NpcGUID);
        }

        public ObjectGuid NpcGUID;
    }
}
