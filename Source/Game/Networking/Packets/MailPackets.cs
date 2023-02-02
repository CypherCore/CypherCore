﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Mails;
using System;
using System.Collections.Generic;

namespace Game.Networking.Packets
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
            _worldPacket.WriteInt32(Mails.Count);
            _worldPacket.WriteInt32(TotalNumRecords);

            Mails.ForEach(p => p.Write(_worldPacket));
        }

        public int TotalNumRecords;
        public List<MailListEntry> Mails = new();
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
        public ulong MailID;
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
            public List<MailAttachment> Attachments = new();

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
            _worldPacket.WriteUInt64(MailID);
            _worldPacket.WriteInt32(Command);
            _worldPacket.WriteInt32(ErrorCode);
            _worldPacket.WriteInt32(BagResult);
            _worldPacket.WriteUInt64(AttachID);
            _worldPacket.WriteInt32(QtyInInventory);
        }

        public ulong MailID;
        public int Command;
        public int ErrorCode;
        public int BagResult;
        public ulong AttachID;
        public int QtyInInventory;
    }

    public class MailReturnToSender : ClientPacket
    {
        public MailReturnToSender(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MailID = _worldPacket.ReadUInt32();
            SenderGUID = _worldPacket.ReadPackedGuid();
        }

        public ulong MailID;
        public ObjectGuid SenderGUID;
    }

    public class MailMarkAsRead : ClientPacket
    {
        public MailMarkAsRead(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Mailbox = _worldPacket.ReadPackedGuid();
            MailID = _worldPacket.ReadUInt32();
        }

        public ObjectGuid Mailbox;
        public ulong MailID;
    }

    public class MailDelete : ClientPacket
    {
        public MailDelete(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MailID = _worldPacket.ReadUInt32();
            DeleteReason = _worldPacket.ReadInt32();
        }

        public ulong MailID;
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
        public ulong MailID;
        public ulong AttachID;
    }

    public class MailTakeMoney : ClientPacket
    {
        public MailTakeMoney(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Mailbox = _worldPacket.ReadPackedGuid();
            MailID = _worldPacket.ReadUInt32();
            Money = _worldPacket.ReadUInt64();
        }

        public ObjectGuid Mailbox;
        public ulong MailID;
        public ulong Money;
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

                TimeLeft = mail.deliver_time - GameTime.GetGameTime();
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

    public class NotifyReceivedMail : ServerPacket
    {
        public NotifyReceivedMail() : base(ServerOpcodes.NotifyReceivedMail) { }

        public override void Write()
        {
            _worldPacket.WriteFloat(Delay);
        }

        public float Delay = 0.0f;
    }

    //Structs
    public class MailAttachedItem
    {
        public MailAttachedItem(Item item, byte pos)
        {
            Position = pos;
            AttachID = item.GetGUID().GetCounter();
            Item = new ItemInstance(item);
            Count = item.GetCount();
            Charges = item.GetSpellCharges();
            MaxDurability = item.m_itemData.MaxDurability;
            Durability = item.m_itemData.Durability;
            Unlocked = !item.IsLocked();

            for (EnchantmentSlot slot = 0; slot < EnchantmentSlot.MaxInspected; slot++)
            {
                if (item.GetEnchantmentId(slot) == 0)
                    continue;

                Enchants.Add(new ItemEnchantData(item.GetEnchantmentId(slot), item.GetEnchantmentDuration(slot), (int)item.GetEnchantmentCharges(slot), (byte)slot));
            }

            byte i = 0;
            foreach (SocketedGem gemData in item.m_itemData.Gems)
            {
                if (gemData.ItemId != 0)
                {
                    ItemGemData gem = new();
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
            data.WriteUInt64(AttachID);
            data.WriteUInt32(Count);
            data.WriteInt32(Charges);
            data.WriteUInt32(MaxDurability);
            data.WriteUInt32(Durability);
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
        public ulong AttachID;
        public ItemInstance Item;
        public uint Count;
        public int Charges;
        public uint MaxDurability;
        public uint Durability;
        public bool Unlocked;
        List<ItemEnchantData> Enchants = new();
        List<ItemGemData> Gems= new();
    }

    public class MailListEntry
    {
        public MailListEntry(Mail mail, Player player)
        {
            MailID = mail.messageID;
            SenderType = (byte)mail.messageType;

            switch (mail.messageType)
            {
                case MailMessageType.Normal:
                    SenderCharacter = ObjectGuid.Create(HighGuid.Player, mail.sender);
                    break;
                case MailMessageType.Creature:
                case MailMessageType.Gameobject:
                case MailMessageType.Auction:
                case MailMessageType.Calendar:
                    AltSenderID = (uint)mail.sender;
                    break;
            }

            Cod = mail.COD;
            StationeryID = (int)mail.stationery;
            SentMoney = mail.money;
            Flags = (int)mail.checkMask;
            DaysLeft = (float)(mail.expire_time - GameTime.GetGameTime()) / Time.Day;
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
            data.WriteUInt64(MailID);
            data.WriteUInt8(SenderType);
            data.WriteUInt64(Cod);
            data.WriteInt32(StationeryID);
            data.WriteUInt64(SentMoney);
            data.WriteInt32(Flags);
            data.WriteFloat(DaysLeft);
            data.WriteInt32(MailTemplateID);
            data.WriteInt32(Attachments.Count);

            data.WriteBit(SenderCharacter.HasValue);
            data.WriteBit(AltSenderID.HasValue);
            data.WriteBits(Subject.GetByteCount(), 8);
            data.WriteBits(Body.GetByteCount(), 13);
            data.FlushBits();

            Attachments.ForEach(p => p.Write(data));

            if (SenderCharacter.HasValue)
                data.WritePackedGuid(SenderCharacter.Value);

            if (AltSenderID.HasValue)
                data.WriteUInt32(AltSenderID.Value);

            data.WriteString(Subject);
            data.WriteString(Body);
        }

        public ulong MailID;
        public byte SenderType;
        public ObjectGuid? SenderCharacter;
        public uint? AltSenderID;
        public ulong Cod;
        public int StationeryID;
        public ulong SentMoney;
        public int Flags;
        public float DaysLeft;
        public int MailTemplateID;
        public string Subject = "";
        public string Body = "";
        public List<MailAttachedItem> Attachments = new();
    }
}
