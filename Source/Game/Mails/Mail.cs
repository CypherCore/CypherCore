// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Mails
{
    public class Mail
    {
        public string Body { get; set; }
        public MailCheckMask CheckMask { get; set; }
        public ulong COD { get; set; }
        public long Deliver_time { get; set; }
        public long Expire_time { get; set; }
        public List<MailItemInfo> Items = new();
        public uint MailTemplateId { get; set; }

        public ulong MessageID { get; set; }
        public MailMessageType MessageType { get; set; }
        public ulong Money { get; set; }
        public ulong Receiver { get; set; }
        public List<uint> RemovedItems { get; set; } = new();
        public ulong Sender { get; set; }
        public MailState State { get; set; }
        public MailStationery Stationery { get; set; }
        public string Subject { get; set; }

        public void AddItem(ulong itemGuidLow, uint item_template)
        {
            MailItemInfo mii = new();
            mii.ItemGuid = itemGuidLow;
            mii.Item_Template = item_template;
            Items.Add(mii);
        }

        public bool RemoveItem(ulong itemGuid)
        {
            foreach (var item in Items)
                if (item.ItemGuid == itemGuid)
                {
                    Items.Remove(item);
                    return true;
                }

            return false;
        }

        public bool HasItems()
        {
            return !Items.Empty();
        }
    }
}