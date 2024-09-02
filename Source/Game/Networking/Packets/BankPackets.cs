// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Framework.Constants;

namespace Game.Networking.Packets
{
    public class AutoBankItem : ClientPacket
    {
        public InvUpdate Inv;
        public BankType BankType;
        public byte Bag;
        public byte Slot;

        public AutoBankItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            BankType = (BankType)_worldPacket.ReadInt8();
            Bag = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
        }
    }

    public class AutoStoreBankItem : ClientPacket
    {
        public InvUpdate Inv;
        public byte Bag;
        public byte Slot;

        public AutoStoreBankItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            Bag = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
        }
    }

    public class BuyBankSlot : ClientPacket
    {
        public BuyBankSlot(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Guid;
    }

    class AutoBankReagent : ClientPacket
    {
        public AutoBankReagent(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new(_worldPacket);
            PackSlot = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
        }

        public InvUpdate Inv;
        public byte Slot;
        public byte PackSlot;
    }

    class AutoStoreBankReagent : ClientPacket
    {
        public AutoStoreBankReagent(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new(_worldPacket);
            Slot = _worldPacket.ReadUInt8();
            PackSlot = _worldPacket.ReadUInt8();
        }

        public InvUpdate Inv;
        public byte Slot;
        public byte PackSlot;
    }

    // CMSG_BUY_REAGENT_BANK
    // CMSG_REAGENT_BANK_DEPOSIT
    class ReagentBank : ClientPacket
    {
        public ReagentBank(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Banker;
    }

    class BankerActivate : ClientPacket
    {
        public ObjectGuid Banker;
        public PlayerInteractionType InteractionType;

        public BankerActivate(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            InteractionType = (PlayerInteractionType)_worldPacket.ReadInt32();
        }
    }
}
