// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
    internal class VoidTransferResult : ServerPacket
    {
        public VoidTransferError Result;

        public VoidTransferResult(VoidTransferError result) : base(ServerOpcodes.VoidTransferResult, ConnectionType.Instance)
        {
            Result = result;
        }

        public override void Write()
        {
            _worldPacket.WriteInt32((int)Result);
        }
    }

    internal class UnlockVoidStorage : ClientPacket
    {
        public ObjectGuid Npc;

        public UnlockVoidStorage(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Npc = _worldPacket.ReadPackedGuid();
        }
    }

    internal class QueryVoidStorage : ClientPacket
    {
        public ObjectGuid Npc;

        public QueryVoidStorage(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Npc = _worldPacket.ReadPackedGuid();
        }
    }

    internal class VoidStorageFailed : ServerPacket
    {
        public byte Reason = 0;

        public VoidStorageFailed() : base(ServerOpcodes.VoidStorageFailed, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8(Reason);
        }
    }

    internal class VoidStorageContents : ServerPacket
    {
        public List<VoidItem> Items = new();

        public VoidStorageContents() : base(ServerOpcodes.VoidStorageContents, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteBits(Items.Count, 8);
            _worldPacket.FlushBits();

            foreach (VoidItem voidItem in Items)
                voidItem.Write(_worldPacket);
        }
    }

    internal class VoidStorageTransfer : ClientPacket
    {
        public ObjectGuid[] Deposits = new ObjectGuid[(int)SharedConst.VoidStorageMaxDeposit];
        public ObjectGuid Npc;

        public ObjectGuid[] Withdrawals = new ObjectGuid[(int)SharedConst.VoidStorageMaxWithdraw];

        public VoidStorageTransfer(WorldPacket packet) : base(packet)
        {
        }

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
    }

    internal class VoidStorageTransferChanges : ServerPacket
    {
        public List<VoidItem> AddedItems = new();

        public List<ObjectGuid> RemovedItems = new();

        public VoidStorageTransferChanges() : base(ServerOpcodes.VoidStorageTransferChanges, ConnectionType.Instance)
        {
        }

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
    }

    internal class SwapVoidItem : ClientPacket
    {
        public uint DstSlot;

        public ObjectGuid Npc;
        public ObjectGuid VoidItemGuid;

        public SwapVoidItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Npc = _worldPacket.ReadPackedGuid();
            VoidItemGuid = _worldPacket.ReadPackedGuid();
            DstSlot = _worldPacket.ReadUInt32();
        }
    }

    internal class VoidItemSwapResponse : ServerPacket
    {
        public ObjectGuid VoidItemA;
        public ObjectGuid VoidItemB;
        public uint VoidItemSlotA;
        public uint VoidItemSlotB;

        public VoidItemSwapResponse() : base(ServerOpcodes.VoidItemSwapResponse, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(VoidItemA);
            _worldPacket.WriteUInt32(VoidItemSlotA);
            _worldPacket.WritePackedGuid(VoidItemB);
            _worldPacket.WriteUInt32(VoidItemSlotB);
        }
    }

    internal struct VoidItem
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(Guid);
            data.WritePackedGuid(Creator);
            data.WriteUInt32(Slot);
            Item.Write(data);
        }

        public ObjectGuid Guid;
        public ObjectGuid Creator;
        public uint Slot;
        public ItemInstance Item;
    }
}