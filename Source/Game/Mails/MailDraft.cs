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
using Framework.Database;
using Game.Entities;
using Game.Loots;
using System.Collections.Generic;

namespace Game.Mails
{
    public class MailDraft
    {
        public MailDraft(uint mailTemplateId, bool need_items = true)
        {
            m_mailTemplateId = mailTemplateId;
            m_mailTemplateItemsNeed = need_items;
            m_money = 0;
            m_COD = 0;
        }

        public MailDraft(string subject, string body)
        {
            m_mailTemplateId = 0;
            m_mailTemplateItemsNeed = false;
            m_subject = subject;
            m_body = body;
            m_money = 0;
            m_COD = 0;
        }

        public MailDraft AddItem(Item item)
        {
            m_items[item.GetGUID().GetCounter()] = item; 
            return this;
        }

        void prepareItems(Player receiver, SQLTransaction trans)
        {
            if (m_mailTemplateId == 0 || !m_mailTemplateItemsNeed)
                return;

            m_mailTemplateItemsNeed = false;

            Loot mailLoot = new Loot();

            // can be empty
            mailLoot.FillLoot(m_mailTemplateId, LootStorage.Mail, receiver, true, true);

            uint max_slot = mailLoot.GetMaxSlotInLootFor(receiver);
            for (uint i = 0; m_items.Count < SharedConst.MaxMailItems && i < max_slot; ++i)
            {
                LootItem lootitem = mailLoot.LootItemInSlot(i, receiver);
                if (lootitem != null)
                {
                    Item item = Item.CreateItem(lootitem.itemid, lootitem.count, receiver);
                    if (item != null)
                    {
                        item.SaveToDB(trans);                           // save for prevent lost at next mail load, if send fail then item will deleted
                        AddItem(item);
                    }
                }
            }
        }

        void deleteIncludedItems(SQLTransaction trans, bool inDB = false)
        {
            foreach (var item in m_items.Values)
            {
                if (inDB)
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE);
                    stmt.AddValue(0, item.GetGUID().GetCounter());
                    trans.Append(stmt);
                }
            }

            m_items.Clear();
        }

        public void SendReturnToSender(uint senderAcc, ulong senderGuid, ulong receiver_guid, SQLTransaction trans)
        {
            ObjectGuid receiverGuid = ObjectGuid.Create(HighGuid.Player, receiver_guid);
            Player receiver = Global.ObjAccessor.FindPlayer(receiverGuid);

            uint rc_account = 0;
            if (receiver == null)
                rc_account = ObjectManager.GetPlayerAccountIdByGUID(receiverGuid);

            if (receiver == null && rc_account == 0)                            // sender not exist
            {
                deleteIncludedItems(trans, true);
                return;
            }

            // prepare mail and send in other case
            bool needItemDelay = false;

            if (!m_items.Empty())
            {
                // if item send to character at another account, then apply item delivery delay
                needItemDelay = senderAcc != rc_account;

                // set owner to new receiver (to prevent delete item with sender char deleting)
                foreach (var item in m_items.Values)
                {
                    item.SaveToDB(trans);                      // item not in inventory and can be save standalone
                    // owner in data will set at mail receive and item extracting
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ITEM_OWNER);
                    stmt.AddValue(0, receiver_guid);
                    stmt.AddValue(1, item.GetGUID().GetCounter());
                    trans.Append(stmt);
                }
            }

            // If theres is an item, there is a one hour delivery delay.
            uint deliver_delay = needItemDelay ? WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay) : 0;

            // will delete item or place to receiver mail list
            SendMailTo(trans, new MailReceiver(receiver, receiver_guid), new MailSender(MailMessageType.Normal, senderGuid), MailCheckMask.Returned, deliver_delay);
        }

        public void SendMailTo(SQLTransaction trans, Player receiver, MailSender sender, MailCheckMask checkMask = MailCheckMask.None, uint deliver_delay = 0)
        {
            SendMailTo(trans, new MailReceiver(receiver), sender, checkMask, deliver_delay);
        }
        public void SendMailTo(SQLTransaction trans, MailReceiver receiver, MailSender sender, MailCheckMask checkMask = MailCheckMask.None, uint deliver_delay = 0)
        {
            Player pReceiver = receiver.GetPlayer();               // can be NULL
            Player pSender = Global.ObjAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, sender.GetSenderId()));

            if (pReceiver != null)
                prepareItems(pReceiver, trans);                            // generate mail template items

            uint mailId = Global.ObjectMgr.GenerateMailID();

            long deliver_time = Time.UnixTime + deliver_delay;

            //expire time if COD 3 days, if no COD 30 days, if auction sale pending 1 hour
            uint expire_delay;

            // auction mail without any items and money
            if (sender.GetMailMessageType() == MailMessageType.Auction && m_items.Empty() && m_money == 0)
                expire_delay = WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay);
            // default case: expire time if COD 3 days, if no COD 30 days (or 90 days if sender is a game master)
            else
                if (m_COD != 0)
                    expire_delay = 3 * Time.Day;
                else
                    expire_delay = (uint)(pSender != null && pSender.IsGameMaster() ? 90 * Time.Day : 30 * Time.Day);

            long expire_time = deliver_time + expire_delay;

            // Add to DB
            byte index = 0;
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_MAIL);
            stmt.AddValue(index, mailId);
            stmt.AddValue(++index, (byte)sender.GetMailMessageType());
            stmt.AddValue(++index, (sbyte)sender.GetStationery());
            stmt.AddValue(++index, GetMailTemplateId());
            stmt.AddValue(++index, sender.GetSenderId());
            stmt.AddValue(++index, receiver.GetPlayerGUIDLow());
            stmt.AddValue(++index, GetSubject());
            stmt.AddValue(++index, GetBody());
            stmt.AddValue(++index, !m_items.Empty());
            stmt.AddValue(++index, expire_time);
            stmt.AddValue(++index, deliver_time);
            stmt.AddValue(++index, m_money);
            stmt.AddValue(++index, m_COD);
            stmt.AddValue(++index, (byte)checkMask);
            trans.Append(stmt);

            foreach (var item in m_items.Values)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_MAIL_ITEM);
                stmt.AddValue(0, mailId);
                stmt.AddValue(1, item.GetGUID().GetCounter());
                stmt.AddValue(2, receiver.GetPlayerGUIDLow());
                trans.Append(stmt);
            }

            // For online receiver update in game mail status and data
            if (pReceiver != null)
            {
                pReceiver.AddNewMailDeliverTime(deliver_time);

                if (pReceiver.IsMailsLoaded())
                {
                    Mail m = new Mail();
                    m.messageID = mailId;
                    m.mailTemplateId = GetMailTemplateId();
                    m.subject = GetSubject();
                    m.body = GetBody();
                    m.money = GetMoney();
                    m.COD = GetCOD();

                    foreach (var item in m_items.Values)
                        m.AddItem(item.GetGUID().GetCounter(), item.GetEntry());

                    m.messageType = sender.GetMailMessageType();
                    m.stationery = sender.GetStationery();
                    m.sender = sender.GetSenderId();
                    m.receiver = receiver.GetPlayerGUIDLow();
                    m.expire_time = expire_time;
                    m.deliver_time = deliver_time;
                    m.checkMask = checkMask;
                    m.state = MailState.Unchanged;

                    pReceiver.AddMail(m);                           // to insert new mail to beginning of maillist

                    if (!m_items.Empty())
                    {
                        foreach (var item in m_items.Values)
                            pReceiver.AddMItem(item);
                    }
                }
                else if (!m_items.Empty())
                    deleteIncludedItems(null);
            }
            else if (!m_items.Empty())
                deleteIncludedItems(null);
        }

        uint GetMailTemplateId() { return m_mailTemplateId; }
        string GetSubject() { return m_subject; }
        ulong GetMoney() { return m_money; }
        ulong GetCOD() { return m_COD; }
        string GetBody() { return m_body; }

        public MailDraft AddMoney(ulong money)
        {
            m_money = money;
            return this;
        }
        public MailDraft AddCOD(uint COD)
        {
            m_COD = COD;
            return this;
        }

        uint m_mailTemplateId;
        bool m_mailTemplateItemsNeed;
        string m_subject;
        string m_body;

        Dictionary<ulong, Item> m_items = new Dictionary<ulong, Item>();

        ulong m_money;
        ulong m_COD;
    }
}
