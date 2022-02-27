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

using Game.Entities;

namespace Game.Networking.Packets
{
    public class AutoBankItem : ClientPacket
    {
        public InvUpdate Inv;
        public byte Bag;
        public byte Slot;

        public AutoBankItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
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
}
