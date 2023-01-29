// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.Entities;
using Game.Loots;

namespace Game.Mails
{
    public class MailDraft
    {
        private readonly string _body;

        private readonly Dictionary<ulong, Item> _items = new();

        private readonly uint _mailTemplateId;
        private readonly string _subject;
        private ulong _COD;
        private bool _mailTemplateItemsNeed;

        private ulong _money;

        public MailDraft(uint mailTemplateId, bool need_items = true)
        {
            _mailTemplateId = mailTemplateId;
            _mailTemplateItemsNeed = need_items;
            _money = 0;
            _COD = 0;
        }

        public MailDraft(string subject, string body)
        {
            _mailTemplateId = 0;
            _mailTemplateItemsNeed = false;
            _subject = subject;
            _body = body;
            _money = 0;
            _COD = 0;
        }

        public MailDraft AddItem(Item item)
        {
            _items[item.GetGUID().GetCounter()] = item;

            return this;
        }

        public void SendReturnToSender(uint senderAcc, ulong senderGuid, ulong receiver_guid, SQLTransaction trans)
        {
            ObjectGuid receiverGuid = ObjectGuid.Create(HighGuid.Player, receiver_guid);
            Player receiver = Global.ObjAccessor.FindPlayer(receiverGuid);

            uint rc_account = 0;

            if (receiver == null)
                rc_account = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(receiverGuid);

            if (receiver == null &&
                rc_account == 0) // sender not exist
            {
                DeleteIncludedItems(trans, true);

                return;
            }

            // prepare mail and send in other case
            bool needItemDelay = false;

            if (!_items.Empty())
            {
                // if Item send to character at another account, then apply Item delivery delay
                needItemDelay = senderAcc != rc_account;

                // set owner to new receiver (to prevent delete Item with sender char deleting)
                foreach (var item in _items.Values)
                {
                    item.SaveToDB(trans); // Item not in inventory and can be save standalone
                                          // owner in _data will set at mail receive and Item extracting
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ITEM_OWNER);
                    stmt.AddValue(0, receiver_guid);
                    stmt.AddValue(1, item.GetGUID().GetCounter());
                    trans.Append(stmt);
                }
            }

            // If theres is an Item, there is a one hour delivery delay.
            uint deliver_delay = needItemDelay ? WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay) : 0;

            // will delete Item or place to receiver mail list
            SendMailTo(trans, new MailReceiver(receiver, receiver_guid), new MailSender(MailMessageType.Normal, senderGuid), MailCheckMask.Returned, deliver_delay);
        }

        public void SendMailTo(SQLTransaction trans, Player receiver, MailSender sender, MailCheckMask checkMask = MailCheckMask.None, uint deliver_delay = 0)
        {
            SendMailTo(trans, new MailReceiver(receiver), sender, checkMask, deliver_delay);
        }

        public void SendMailTo(SQLTransaction trans, MailReceiver receiver, MailSender sender, MailCheckMask checkMask = MailCheckMask.None, uint deliver_delay = 0)
        {
            Player pReceiver = receiver.GetPlayer(); // can be NULL
            Player pSender = sender.GetMailMessageType() == MailMessageType.Normal ? Global.ObjAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, sender.GetSenderId())) : null;

            if (pReceiver != null)
                PrepareItems(pReceiver, trans); // generate mail template items

            uint mailId = Global.ObjectMgr.GenerateMailID();

            long deliver_time = GameTime.GetGameTime() + deliver_delay;

            //expire Time if COD 3 days, if no COD 30 days, if auction sale pending 1 hour
            uint expire_delay;

            // auction mail without any items and money
            if (sender.GetMailMessageType() == MailMessageType.Auction &&
                _items.Empty() &&
                _money == 0)
                expire_delay = WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay);
            // default case: expire Time if COD 3 days, if no COD 30 days (or 90 days if sender is a game master)
            else if (_COD != 0)
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
            stmt.AddValue(++index, !_items.Empty());
            stmt.AddValue(++index, expire_time);
            stmt.AddValue(++index, deliver_time);
            stmt.AddValue(++index, _money);
            stmt.AddValue(++index, _COD);
            stmt.AddValue(++index, (byte)checkMask);
            trans.Append(stmt);

            foreach (var item in _items.Values)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_MAIL_ITEM);
                stmt.AddValue(0, mailId);
                stmt.AddValue(1, item.GetGUID().GetCounter());
                stmt.AddValue(2, receiver.GetPlayerGUIDLow());
                trans.Append(stmt);
            }

            // For online receiver update in game mail status and _data
            if (pReceiver != null)
            {
                pReceiver.AddNewMailDeliverTime(deliver_time);


                Mail m = new();
                m.messageID = mailId;
                m.mailTemplateId = GetMailTemplateId();
                m.subject = GetSubject();
                m.body = GetBody();
                m.money = GetMoney();
                m.COD = GetCOD();

                foreach (var item in _items.Values)
                    m.AddItem(item.GetGUID().GetCounter(), item.GetEntry());

                m.messageType = sender.GetMailMessageType();
                m.stationery = sender.GetStationery();
                m.sender = sender.GetSenderId();
                m.receiver = receiver.GetPlayerGUIDLow();
                m.expire_time = expire_time;
                m.deliver_time = deliver_time;
                m.checkMask = checkMask;
                m.state = MailState.Unchanged;

                pReceiver.AddMail(m); // to insert new mail to beginning of maillist

                if (!_items.Empty())
                    foreach (var item in _items.Values)
                        pReceiver.AddMItem(item);
            }
            else if (!_items.Empty())
            {
                DeleteIncludedItems(null);
            }
        }

        public MailDraft AddMoney(ulong money)
        {
            _money = money;

            return this;
        }

        public MailDraft AddCOD(uint COD)
        {
            _COD = COD;

            return this;
        }

        private void PrepareItems(Player receiver, SQLTransaction trans)
        {
            if (_mailTemplateId == 0 ||
                !_mailTemplateItemsNeed)
                return;

            _mailTemplateItemsNeed = false;

            // The mail sent after turning in the quest The Good News and The Bad News contains 100g
            if (_mailTemplateId == 123)
                _money = 1000000;

            Loot mailLoot = new(null, ObjectGuid.Empty, LootType.None, null);

            // can be empty
            mailLoot.FillLoot(_mailTemplateId, LootStorage.Mail, receiver, true, true, LootModes.Default, ItemContext.None);

            for (uint i = 0; _items.Count < SharedConst.MaxMailItems && i < mailLoot.Items.Count; ++i)
            {
                LootItem lootitem = mailLoot.LootItemInSlot(i, receiver);

                if (lootitem != null)
                {
                    Item item = Item.CreateItem(lootitem.Itemid, lootitem.Count, lootitem.Context, receiver);

                    if (item != null)
                    {
                        item.SaveToDB(trans); // save for prevent lost at next mail load, if send fail then Item will deleted
                        AddItem(item);
                    }
                }
            }
        }

        private void DeleteIncludedItems(SQLTransaction trans, bool inDB = false)
        {
            foreach (var item in _items.Values)
                if (inDB)
                    item.DeleteFromDB(trans);

            _items.Clear();
        }

        private uint GetMailTemplateId()
        {
            return _mailTemplateId;
        }

        private string GetSubject()
        {
            return _subject;
        }

        private ulong GetMoney()
        {
            return _money;
        }

        private ulong GetCOD()
        {
            return _COD;
        }

        private string GetBody()
        {
            return _body;
        }
    }
}