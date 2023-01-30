// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Networking.Packets
{
    public class AutoBankItem : ClientPacket
    {
        public byte Bag;
        public InvUpdate Inv;
        public byte Slot;

        public AutoBankItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            Bag = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
        }
    }

    public class AutoStoreBankItem : ClientPacket
    {
        public byte Bag;
        public InvUpdate Inv;
        public byte Slot;

        public AutoStoreBankItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            Bag = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
        }
    }

    public class BuyBankSlot : ClientPacket
    {
        public ObjectGuid Guid;

        public BuyBankSlot(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }
    }

    internal class AutoBankReagent : ClientPacket
    {
        public InvUpdate Inv;
        public byte PackSlot;
        public byte Slot;

        public AutoBankReagent(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            PackSlot = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
        }
    }

    internal class AutoStoreBankReagent : ClientPacket
    {
        public InvUpdate Inv;
        public byte PackSlot;
        public byte Slot;

        public AutoStoreBankReagent(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            Slot = _worldPacket.ReadUInt8();
            PackSlot = _worldPacket.ReadUInt8();
        }
    }

    // CMSG_BUY_REAGENT_BANK
    // CMSG_REAGENT_BANK_DEPOSIT
    internal class ReagentBank : ClientPacket
    {
        public ObjectGuid Banker;

        public ReagentBank(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
        }
    }
}