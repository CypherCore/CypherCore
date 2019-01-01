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
using Framework.Dynamic;
using Game.Entities;
using Game.Mails;
using System;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    public class MailGetList : ClientPacket
    {
        public MailGetList(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Mailbox = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Mailbox;
    }

    public class MailListResult : ServerPacket
    {
        public MailListResult() : base(ServerOpcodes.MailListResult) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Mails.Count);
            _worldPacket.WriteInt32(TotalNumRecords);

            Mails.ForEach(p => p.Write(_worldPacket));
        }

        public int TotalNumRecords;
        public List<MailListEntry> Mails = new List<MailListEntry>();
    }

    public class MailCreateTextItem : ClientPacket
    {
        public MailCreateTextItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Mailbox = _worldPacket.ReadPackedGuid();
            MailID = _worldPacket.ReadUInt32();
        }

        public ObjectGuid Mailbox;
        public uint MailID;
    }

    public class SendMail : ClientPacket
    {
        public SendMail(WorldPacket packet) : base(packet)
        {
            Info = new StructSendMail();
        }

        public override void Read()
        {
            Info.Mailbox = _worldPacket.ReadPackedGuid();
            Info.StationeryID = _worldPacket.ReadInt32();
            Info.SendMoney = _worldPacket.ReadInt64();
            Info.Cod = _worldPacket.ReadInt64();

            uint targetLength = _worldPacket.ReadBits<uint>(9);
            uint subjectLength = _worldPacket.ReadBits<uint>(9);
            uint bodyLength = _worldPacket.ReadBits<uint>(11);

            uint count = _worldPacket.ReadBits<uint>(5);

            Info.Target = _worldPacket.ReadString(targetLength);
            Info.Subject = _worldPacket.ReadString(subjectLength);
            Info.Body = _worldPacket.ReadString(bodyLength);

            for (var i = 0; i < count; ++i)
            {
                var att = new StructSendMail.MailAttachment()
                {
                    AttachPosition = _worldPacket.ReadUInt8(),
                    ItemGUID = _worldPacket.ReadPackedGuid()
                };

                Info.Attachments.Add(att);
            }
        }

        public StructSendMail Info;

        public class StructSendMail
        {
            public ObjectGuid Mailbox;
            public int StationeryID;
            public long SendMoney;
            public long Cod;
            public string Target;
            public string Subject;
            public string Body;
            public List<MailAttachment> Attachments = new List<MailAttachment>();

            public struct MailAttachment
            {
                public byte AttachPosition;
                public ObjectGuid ItemGUID;
            }
        }
    }

    public class MailCommandResult : ServerPacket
    {
        public MailCommandResult() : base(ServerOpcodes.MailCommandResult) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MailID);
            _worldPacket.WriteUInt32(Command);
            _worldPacket.WriteUInt32(ErrorCode);
            _worldPacket.WriteUInt32(BagResult);
            _worldPacket.WriteUInt32(AttachID);
            _worldPacket.WriteUInt32(QtyInInventory);
        }

        public uint MailID;
        public uint Command;
        public uint ErrorCode;
        public uint BagResult;
        public uint AttachID;
        public uint QtyInInventory;
    }

    public class MailReturnToSender : ClientPacket
    {
        public MailReturnToSender(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MailID = _worldPacket.ReadUInt32();
            SenderGUID = _worldPacket.ReadPackedGuid();
        }

        public uint MailID;
        public ObjectGuid SenderGUID;
    }

    public class MailMarkAsRead : ClientPacket
    {
        public MailMarkAsRead(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Mailbox = _worldPacket.ReadPackedGuid();
            MailID = _worldPacket.ReadUInt32();
            BiReceipt = _worldPacket.HasBit();
        }

        public ObjectGuid Mailbox;
        public uint MailID;
        public bool BiReceipt;
    }

    public class MailDelete : ClientPacket
    {
        public MailDelete(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MailID = _worldPacket.ReadUInt32();
            DeleteReason = _worldPacket.ReadInt32();
        }

        public uint MailID;
        public int DeleteReason;
    }

    public class MailTakeItem : ClientPacket
    {
        public MailTakeItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Mailbox = _worldPacket.ReadPackedGuid();
            MailID = _worldPacket.ReadUInt32();
            AttachID = _worldPacket.ReadUInt32();
        }

        public ObjectGuid Mailbox;
        public uint MailID;
        public uint AttachID;
    }

    public class MailTakeMoney : ClientPacket
    {
        public MailTakeMoney(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Mailbox = _worldPacket.ReadPackedGuid();
            MailID = _worldPacket.ReadUInt32();
            Money = _worldPacket.ReadInt64();
        }

        public ObjectGuid Mailbox;
        public uint MailID;
        public long Money;
    }

    public class MailQueryNextMailTime : ClientPacket
    {
        public MailQueryNextMailTime(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class MailQueryNextTimeResult : ServerPacket
    {
        public MailQueryNextTimeResult() : base(ServerOpcodes.MailQueryNextTimeResult)
        {
            Next = new List<MailNextTimeEntry>();
        }

        public override void Write()
        {
            _worldPacket.WriteFloat(NextMailTime);
            _worldPacket.WriteInt32(Next.Count);

            foreach (var entry in Next)
            {
                _worldPacket.WritePackedGuid(entry.SenderGuid);
                _worldPacket.WriteFloat(entry.TimeLeft);
                _worldPacket.WriteInt32(entry.AltSenderID);
                _worldPacket.WriteInt8(entry.AltSenderType);
                _worldPacket.WriteInt32(entry.StationeryID);
            }
        }

        public float NextMailTime;
        public List<MailNextTimeEntry> Next;

        public class MailNextTimeEntry
        {
            public MailNextTimeEntry(Mail mail)
            {
                switch (mail.messageType)
                {
                    case MailMessageType.Normal:
                        SenderGuid = ObjectGuid.Create(HighGuid.Player, mail.sender);
                        break;
                    case MailMessageType.Auction:
                    case MailMessageType.Creature:
                    case MailMessageType.Gameobject:
                    case MailMessageType.Calendar:
                        AltSenderID = (int)mail.sender;
                        break;
                }

                TimeLeft = mail.deliver_time - Time.UnixTime;
                AltSenderType = (sbyte)mail.messageType;
                StationeryID = (int)mail.stationery;
            }

            public ObjectGuid SenderGuid;
            public float TimeLeft;
            public int AltSenderID;
            public sbyte AltSenderType;
            public int StationeryID;
        }
    }

    public class NotifyRecievedMail : ServerPacket
    {
        public NotifyRecievedMail() : base(ServerOpcodes.NotifyReceivedMail) { }

        public override void Write()
        {
            _worldPacket.WriteFloat(Delay);
        }

        public float Delay = 0.0f;
    }

    class ShowMailbox : ServerPacket
    {
        public ShowMailbox() : base(ServerOpcodes.ShowMailbox) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PostmasterGUID);
        }

        public ObjectGuid PostmasterGUID;
    }

    //Structs
    public class MailAttachedItem
    {
        public MailAttachedItem(Item item, byte pos)
        {
            Position = pos;
            AttachID = (int)item.GetGUID().GetCounter();
            Item = new ItemInstance(item);
            Count = item.GetCount();
            Charges = item.GetSpellCharges();
            MaxDurability = item.GetUInt32Value(ItemFields.MaxDurability);
            Durability = item.GetUInt32Value(ItemFields.Durability);
            Unlocked = !item.IsLocked();

            for (EnchantmentSlot slot = 0; slot < EnchantmentSlot.MaxInspected; slot++)
            {
                if (item.GetEnchantmentId(slot) == 0)
                    continue;

                Enchants.Add(new ItemEnchantData((int)item.GetEnchantmentId(slot), item.GetEnchantmentDuration(slot), (int)item.GetEnchantmentCharges(slot), (byte)slot));
            }

            byte i = 0;
            foreach (ItemDynamicFieldGems gemData in item.GetGems())
            {
                if (gemData.ItemId != 0)
                {
                    ItemGemData gem = new ItemGemData();
                    gem.Slot = i;
                    gem.Item = new ItemInstance(gemData);
                    Gems.Add(gem);
                }
                ++i;
            }
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt8(Position);
            data.WriteUInt32(AttachID);
            data.WriteInt32(Count);
            data.WriteInt32(Charges);
            data.WriteInt32(MaxDurability);
            data.WriteInt32(Durability);
            Item.Write(data);
            data.WriteBits(Enchants.Count, 4);
            data.WriteBits(Gems.Count, 2);
            data.WriteBit(Unlocked);
            data.FlushBits();

            foreach (ItemGemData gem in Gems)
                gem.Write(data);

            foreach (ItemEnchantData en in Enchants)
                en.Write(data);
        }

        public byte Position;
        public int AttachID;
        public ItemInstance Item;
        public uint Count;
        public int Charges;
        public uint MaxDurability;
        public uint Durability;
        public bool Unlocked;
        List<ItemEnchantData> Enchants = new List<ItemEnchantData>();
        List<ItemGemData> Gems= new List<ItemGemData>();
    }

    public class MailListEntry
    {
        public MailListEntry(Mail mail, Player player)
        {
            MailID = (int)mail.messageID;
            SenderType = (byte)mail.messageType;

            switch (mail.messageType)
            {
                case MailMessageType.Normal:
                    SenderCharacter.Set(ObjectGuid.Create(HighGuid.Player, mail.sender));
                    break;
                case MailMessageType.Creature:
                case MailMessageType.Gameobject:
                case MailMessageType.Auction:
                case MailMessageType.Calendar:
                    AltSenderID.Set((uint)mail.sender);
                    break;
            }

            Cod = mail.COD;
            StationeryID = (int)mail.stationery;
            SentMoney = mail.money;
            Flags = (int)mail.checkMask;
            DaysLeft = (float)(mail.expire_time - Time.UnixTime) / Time.Day;
            MailTemplateID = (int)mail.mailTemplateId;
            Subject = mail.subject;
            Body = mail.body;

            for (byte i = 0; i < mail.items.Count; i++)
            {
                Item item = player.GetMItem(mail.items[i].item_guid);
                if (item)
                    Attachments.Add(new MailAttachedItem(item, i));
            }
        }

        public void Write(WorldPacket data)
        {
            data.WriteInt32(MailID);
            data.WriteUInt8(SenderType);
            data.WriteUInt64(Cod);
            data.WriteInt32(StationeryID);
            data.WriteUInt64(SentMoney);
            data.WriteInt32(Flags);
            data.WriteFloat(DaysLeft);
            data.WriteInt32(MailTemplateID);
            data.WriteUInt32(Attachments.Count);

            data.WriteBit(SenderCharacter.HasValue);
            data.WriteBit(AltSenderID.HasValue);
            data.WriteBits(Subject.GetByteCount(), 8);
            data.WriteBits(Body.GetByteCount(), 13);
            data.FlushBits();

            Attachments.ForEach(p => p.Write(data));

            if (SenderCharacter.HasValue)
                data.WritePackedGuid(SenderCharacter.Value);

            if (AltSenderID.HasValue)
                data.WriteInt32(AltSenderID.Value);

            data.WriteString(Subject);
            data.WriteString(Body);
        }

        public int MailID;
        public byte SenderType;
        public Optional<ObjectGuid> SenderCharacter;
        public Optional<uint> AltSenderID;
        public ulong Cod;
        public int StationeryID;
        public ulong SentMoney;
        public int Flags;
        public float DaysLeft;
        public int MailTemplateID;
        public string Subject = "";
        public string Body = "";
        public List<MailAttachedItem> Attachments = new List<MailAttachedItem>();
    }
}
