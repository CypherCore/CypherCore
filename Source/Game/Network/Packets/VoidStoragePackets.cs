/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class VoidTransferResult : ServerPacket
    {
        public VoidTransferResult(VoidTransferError result) : base(ServerOpcodes.VoidTransferResult, ConnectionType.Instance)
        {
            Result = result;
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(Result);
        }

        public VoidTransferError Result;
    }

    class UnlockVoidStorage : ClientPacket
    {
        public UnlockVoidStorage(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Npc = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Npc;
    }

    class QueryVoidStorage : ClientPacket
    {
        public QueryVoidStorage(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Npc = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Npc;
    }

    class VoidStorageFailed : ServerPacket
    {
        public VoidStorageFailed() : base(ServerOpcodes.VoidStorageFailed, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8(Reason);
        }

        public byte Reason = 0;
    }

    class VoidStorageContents : ServerPacket
    {
        public VoidStorageContents() : base(ServerOpcodes.VoidStorageContents, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Items.Count, 8);
            _worldPacket.FlushBits();

            foreach (VoidItem voidItem in Items)
                voidItem.Write(_worldPacket);
        }

        public List<VoidItem> Items = new List<VoidItem>();
    }

    class VoidStorageTransfer : ClientPacket
    {
        public VoidStorageTransfer(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Npc = _worldPacket.ReadPackedGuid();
            var DepositCount = _worldPacket.ReadInt32();
            var WithdrawalCount = _worldPacket.ReadInt32();

            for (uint i = 0; i < DepositCount; ++i)
                Deposits[i] = _worldPacket.ReadPackedGuid();

            for (uint i = 0; i < WithdrawalCount; ++i)
                Withdrawals[i] = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid[] Withdrawals = new ObjectGuid[(int)SharedConst.VoidStorageMaxWithdraw];
        public ObjectGuid[] Deposits = new ObjectGuid[(int)SharedConst.VoidStorageMaxDeposit];
        public ObjectGuid Npc;
    }

    class VoidStorageTransferChanges : ServerPacket
    {
        public VoidStorageTransferChanges() : base(ServerOpcodes.VoidStorageTransferChanges, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBits(AddedItems.Count, 4);
            _worldPacket.WriteBits(RemovedItems.Count, 4);
            _worldPacket.FlushBits();

            foreach (VoidItem addedItem in AddedItems)
                addedItem.Write(_worldPacket);

            foreach (ObjectGuid removedItem in RemovedItems)
                _worldPacket.WritePackedGuid(removedItem);
        }

        public List<ObjectGuid> RemovedItems = new List<ObjectGuid>();
        public List<VoidItem> AddedItems = new List<VoidItem>();
    }

    class SwapVoidItem : ClientPacket
    {
        public SwapVoidItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Npc = _worldPacket.ReadPackedGuid();
            VoidItemGuid = _worldPacket.ReadPackedGuid();
            DstSlot = _worldPacket.ReadUInt32();
        }

        public ObjectGuid Npc;
        public ObjectGuid VoidItemGuid;
        public uint DstSlot;
    }

    class VoidItemSwapResponse : ServerPacket
    {
        public VoidItemSwapResponse() : base(ServerOpcodes.VoidItemSwapResponse, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(VoidItemA);
            _worldPacket.WriteUInt32(VoidItemSlotA);
            _worldPacket.WritePackedGuid(VoidItemB);
            _worldPacket.WriteUInt32(VoidItemSlotB);
        }

        public ObjectGuid VoidItemA;
        public ObjectGuid VoidItemB;
        public uint VoidItemSlotA;
        public uint VoidItemSlotB;
    }

    struct VoidItem
    {
        public void Write(WorldPacket data)
        {
            data .WritePackedGuid(Guid);
            data .WritePackedGuid(Creator);
            data .WriteUInt32(Slot);
            Item.Write(data);
        }

        public ObjectGuid Guid;
        public ObjectGuid Creator;
        public uint Slot;
        public ItemInstance Item;
    }
}
