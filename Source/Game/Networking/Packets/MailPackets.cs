// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Mails;

namespace Game.Networking.Packets
{
    public class MailGetList : ClientPacket
    {
        public ObjectGuid Mailbox;

        public MailGetList(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Mailbox = _worldPacket.ReadPackedGuid();
        }
    }

    public class MailListResult : ServerPacket
    {
        public List<MailListEntry> Mails = new();

        public int TotalNumRecords;

        public MailListResult() : base(ServerOpcodes.MailListResult)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(Mails.Count);
            _worldPacket.WriteInt32(TotalNumRecords);

            Mails.ForEach(p => p.Write(_worldPacket));
        }
    }

    public class MailCreateTextItem : ClientPacket
    {
        public ObjectGuid Mailbox;
        public uint MailID;

        public MailCreateTextItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Mailbox = _worldPacket.ReadPackedGuid();
            MailID = _worldPacket.ReadUInt32();
        }
    }

    public class SendMail : ClientPacket
    {
        public StructSendMail Info;

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

        public class StructSendMail
        {
            public List<MailAttachment> Attachments = new();
            public string Body;
            public long Cod;
            public ObjectGuid Mailbox;
            public long SendMoney;
            public int StationeryID;
            public string Subject;
            public string Target;

            public struct MailAttachment
            {
                public byte AttachPosition;
                public ObjectGuid ItemGUID;
            }
        }
    }

    public class MailCommandResult : ServerPacket
    {
        public uint AttachID;
        public uint BagResult;
        public uint Command;
        public uint ErrorCode;

        public uint MailID;
        public uint QtyInInventory;

        public MailCommandResult() : base(ServerOpcodes.MailCommandResult)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MailID);
            _worldPacket.WriteUInt32(Command);
            _worldPacket.WriteUInt32(ErrorCode);
            _worldPacket.WriteUInt32(BagResult);
            _worldPacket.WriteUInt32(AttachID);
            _worldPacket.WriteUInt32(QtyInInventory);
        }
    }

    public class MailReturnToSender : ClientPacket
    {
        public uint MailID;
        public ObjectGuid SenderGUID;

        public MailReturnToSender(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            MailID = _worldPacket.ReadUInt32();
            SenderGUID = _worldPacket.ReadPackedGuid();
        }
    }

    public class MailMarkAsRead : ClientPacket
    {
        public ObjectGuid Mailbox;
        public uint MailID;

        public MailMarkAsRead(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Mailbox = _worldPacket.ReadPackedGuid();
            MailID = _worldPacket.ReadUInt32();
        }
    }

    public class MailDelete : ClientPacket
    {
        public int DeleteReason;

        public uint MailID;

        public MailDelete(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            MailID = _worldPacket.ReadUInt32();
            DeleteReason = _worldPacket.ReadInt32();
        }
    }

    public class MailTakeItem : ClientPacket
    {
        public uint AttachID;

        public ObjectGuid Mailbox;
        public uint MailID;

        public MailTakeItem(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Mailbox = _worldPacket.ReadPackedGuid();
            MailID = _worldPacket.ReadUInt32();
            AttachID = _worldPacket.ReadUInt32();
        }
    }

    public class MailTakeMoney : ClientPacket
    {
        public ObjectGuid Mailbox;
        public uint MailID;
        public long Money;

        public MailTakeMoney(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Mailbox = _worldPacket.ReadPackedGuid();
            MailID = _worldPacket.ReadUInt32();
            Money = _worldPacket.ReadInt64();
        }
    }

    public class MailQueryNextMailTime : ClientPacket
    {
        public MailQueryNextMailTime(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    public class MailQueryNextTimeResult : ServerPacket
    {
        public List<MailNextTimeEntry> Next;

        public float NextMailTime;

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

        public class MailNextTimeEntry
        {
            public int AltSenderID;
            public sbyte AltSenderType;

            public ObjectGuid SenderGuid;
            public int StationeryID;
            public float TimeLeft;

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
        }
    }

    public class NotifyReceivedMail : ServerPacket
    {
        public float Delay = 0.0f;

        public NotifyReceivedMail() : base(ServerOpcodes.NotifyReceivedMail)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteFloat(Delay);
        }
    }

    //Structs
    public class MailAttachedItem
    {
        public int AttachID;
        public int Charges;
        public uint Count;
        public uint Durability;
        private readonly List<ItemEnchantData> Enchants = new();
        private readonly List<ItemGemData> Gems = new();
        public ItemInstance Item;
        public uint MaxDurability;

        public byte Position;
        public bool Unlocked;

        public MailAttachedItem(Item item, byte pos)
        {
            Position = pos;
            AttachID = (int)item.GetGUID().GetCounter();
            Item = new ItemInstance(item);
            Count = item.GetCount();
            Charges = item.GetSpellCharges();
            MaxDurability = item._itemData.MaxDurability;
            Durability = item._itemData.Durability;
            Unlocked = !item.IsLocked();

            for (EnchantmentSlot slot = 0; slot < EnchantmentSlot.MaxInspected; slot++)
            {
                if (item.GetEnchantmentId(slot) == 0)
                    continue;

                Enchants.Add(new ItemEnchantData(item.GetEnchantmentId(slot), item.GetEnchantmentDuration(slot), (int)item.GetEnchantmentCharges(slot), (byte)slot));
            }

            byte i = 0;

            foreach (SocketedGem gemData in item._itemData.Gems)
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
            data.WriteInt32(AttachID);
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
    }

    public class MailListEntry
    {
        public uint? AltSenderID;
        public List<MailAttachedItem> Attachments = new();
        public string Body = "";
        public ulong Cod;
        public float DaysLeft;
        public int Flags;

        public int MailID;
        public int MailTemplateID;
        public ObjectGuid? SenderCharacter;
        public byte SenderType;
        public ulong SentMoney;
        public int StationeryID;
        public string Subject = "";

        public MailListEntry(Mail mail, Player player)
        {
            MailID = (int)mail.messageID;
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
            data.WriteInt32(MailID);
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
    }
}