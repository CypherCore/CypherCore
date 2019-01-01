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
using Game.DataStorage;
using Game.Entities;
using Game.Guilds;
using Game.Mails;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public partial class WorldSession
    {
        bool CanOpenMailBox(ObjectGuid guid)
        {
            if (guid == GetPlayer().GetGUID())
            {
                if (!HasPermission(RBACPermissions.CommandMailbox))
                {
                    Log.outWarn(LogFilter.ChatSystem, "{0} attempt open mailbox in cheating way.", GetPlayer().GetName());
                    return false;
                }
            }
            else if (guid.IsGameObject())
            {
                if (!GetPlayer().GetGameObjectIfCanInteractWith(guid, GameObjectTypes.Mailbox))
                    return false;
            }
            else if (guid.IsAnyTypeCreature())
            {
                if (!GetPlayer().GetNPCIfCanInteractWith(guid, NPCFlags.Mailbox))
                    return false;
            }
            else
                return false;

            return true;
        }

        [WorldPacketHandler(ClientOpcodes.SendMail)]
        void HandleSendMail(SendMail packet)
        {
            if (packet.Info.Attachments.Count > SharedConst.MaxMailItems)                      // client limit
            {
                GetPlayer().SendMailResult(0, MailResponseType.Send, MailResponseResult.TooManyAttachments);
                return;
            }

            if (!CanOpenMailBox(packet.Info.Mailbox))
                return;

            if (string.IsNullOrEmpty(packet.Info.Target))
                return;

            Player player = GetPlayer();
            if (player.getLevel() < WorldConfig.GetIntValue(WorldCfg.MailLevelReq))
            {
                SendNotification(CypherStrings.MailSenderReq, WorldConfig.GetIntValue(WorldCfg.MailLevelReq));
                return;
            }

            ObjectGuid receiverGuid = ObjectGuid.Empty;
            if (ObjectManager.NormalizePlayerName(ref packet.Info.Target))
                receiverGuid = ObjectManager.GetPlayerGUIDByName(packet.Info.Target);

            if (receiverGuid.IsEmpty())
            {
                Log.outInfo(LogFilter.Network, "Player {0} is sending mail to {1} (GUID: not existed!) with subject {2}" +
                    "and body {3} includes {4} items, {5} copper and {6} COD copper with StationeryID = {7}",
                    GetPlayerInfo(), packet.Info.Target, packet.Info.Subject, packet.Info.Body,
                    packet.Info.Attachments.Count, packet.Info.SendMoney, packet.Info.Cod, packet.Info.StationeryID);
                player.SendMailResult(0, MailResponseType.Send, MailResponseResult.RecipientNotFound);
                return;
            }

            if (packet.Info.SendMoney < 0)
            {
                GetPlayer().SendMailResult(0, MailResponseType.Send, MailResponseResult.InternalError);
                Log.outWarn(LogFilter.Server, "Player {0} attempted to send mail to {1} ({2}) with negative money value (SendMoney: {3})",
                    GetPlayerInfo(), packet.Info.Target, receiverGuid.ToString(), packet.Info.SendMoney);
                return;
            }

            if (packet.Info.Cod < 0)
            {
                GetPlayer().SendMailResult(0, MailResponseType.Send, MailResponseResult.InternalError);
                Log.outWarn(LogFilter.Server, "Player {0} attempted to send mail to {1} ({2}) with negative COD value (Cod: {3})",
                    GetPlayerInfo(), packet.Info.Target, receiverGuid.ToString(), packet.Info.Cod);
                return;
            }

            Log.outInfo(LogFilter.Network, "Player {0} is sending mail to {1} ({2}) with subject {3} and body {4}" +
                "includes {5} items, {6} copper and {7} COD copper with StationeryID = {8}",
                GetPlayerInfo(), packet.Info.Target, receiverGuid.ToString(), packet.Info.Subject,
                packet.Info.Body, packet.Info.Attachments.Count, packet.Info.SendMoney, packet.Info.Cod, packet.Info.StationeryID);

            if (player.GetGUID() == receiverGuid)
            {
                player.SendMailResult(0, MailResponseType.Send, MailResponseResult.CannotSendToSelf);
                return;
            }

            uint cost = (uint)(!packet.Info.Attachments.Empty() ? 30 * packet.Info.Attachments.Count : 30);  // price hardcoded in client

            long reqmoney = cost + packet.Info.SendMoney;

            // Check for overflow
            if (reqmoney < packet.Info.SendMoney)
            {
                player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotEnoughMoney);
                return;
            }

            if (!player.HasEnoughMoney(reqmoney) && !player.IsGameMaster())
            {
                player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotEnoughMoney);
                return;
            }

            Player receiver = Global.ObjAccessor.FindPlayer(receiverGuid);

            Team receiverTeam = 0;
            byte mailsCount = 0;                                  //do not allow to send to one player more than 100 mails
            byte receiverLevel = 0;
            uint receiverAccountId = 0;

            if (receiver)
            {
                receiverTeam = receiver.GetTeam();
                mailsCount = (byte)receiver.GetMails().Count;
                receiverLevel = (byte)receiver.getLevel();
                receiverAccountId = receiver.GetSession().GetAccountId();
            }
            else
            {
                receiverTeam = ObjectManager.GetPlayerTeamByGUID(receiverGuid);

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_COUNT);
                stmt.AddValue(0, receiverGuid.GetCounter());

                SQLResult result = DB.Characters.Query(stmt);
                if (!result.IsEmpty())
                    mailsCount = (byte)result.Read<ulong>(0);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_LEVEL);
                stmt.AddValue(0, receiverGuid.GetCounter());

                result = DB.Characters.Query(stmt);
                if (!result.IsEmpty())
                    receiverLevel = result.Read<byte>(0);

                receiverAccountId = ObjectManager.GetPlayerAccountIdByGUID(receiverGuid);
            }

            // do not allow to have more than 100 mails in mailbox.. mails count is in opcode byte!!! - so max can be 255..
            if (mailsCount > 100)
            {
                player.SendMailResult(0, MailResponseType.Send, MailResponseResult.RecipientCapReached);
                return;
            }

            // test the receiver's Faction... or all items are account bound
            bool accountBound = !packet.Info.Attachments.Empty();
            foreach (var att in packet.Info.Attachments)
            {
                Item item = player.GetItemByGuid(att.ItemGUID);
                if (item)
                {
                    ItemTemplate itemProto = item.GetTemplate();
                    if (itemProto == null || !itemProto.GetFlags().HasAnyFlag(ItemFlags.IsBoundToAccount))
                    {
                        accountBound = false;
                        break;
                    }
                }
            }

            if (!accountBound && player.GetTeam() != receiverTeam && !HasPermission(RBACPermissions.TwoSideInteractionMail))
            {
                player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotYourTeam);
                return;
            }

            if (receiverLevel < WorldConfig.GetIntValue(WorldCfg.MailLevelReq))
            {
                SendNotification(CypherStrings.MailReceiverReq, WorldConfig.GetIntValue(WorldCfg.MailLevelReq));
                return;
            }

            List<Item> items = new List<Item>();
            foreach (var att in packet.Info.Attachments)
            {
                if (att.ItemGUID.IsEmpty())
                {
                    player.SendMailResult(0, MailResponseType.Send, MailResponseResult.MailAttachmentInvalid);
                    return;
                }

                Item item = player.GetItemByGuid(att.ItemGUID);

                // prevent sending bag with items (cheat: can be placed in bag after adding equipped empty bag to mail)
                if (!item)
                {
                    player.SendMailResult(0, MailResponseType.Send, MailResponseResult.MailAttachmentInvalid);
                    return;
                }

                if (!item.CanBeTraded(true))
                {
                    player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.MailBoundItem);
                    return;
                }

                if (item.IsBoundAccountWide() && item.IsSoulBound() && player.GetSession().GetAccountId() != receiverAccountId)
                {
                    player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.NotSameAccount);
                    return;
                }

                if (item.GetTemplate().GetFlags().HasAnyFlag(ItemFlags.Conjured) || item.GetUInt32Value(ItemFields.Duration) != 0)
                {
                    player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.MailBoundItem);
                    return;
                }

                if (packet.Info.Cod != 0 && item.HasFlag(ItemFields.Flags, ItemFieldFlags.Wrapped))
                {
                    player.SendMailResult(0, MailResponseType.Send, MailResponseResult.CantSendWrappedCod);
                    return;
                }

                if (item.IsNotEmptyBag())
                {
                    player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.DestroyNonemptyBag);
                    return;
                }

                items.Add(item);
            }

            player.SendMailResult(0, MailResponseType.Send, MailResponseResult.Ok);

            player.ModifyMoney(-reqmoney);
            player.UpdateCriteria(CriteriaTypes.GoldSpentForMail, cost);

            bool needItemDelay = false;

            MailDraft draft = new MailDraft(packet.Info.Subject, packet.Info.Body);

            SQLTransaction trans = new SQLTransaction();

            if (!packet.Info.Attachments.Empty() || packet.Info.SendMoney > 0)
            {
                bool log = HasPermission(RBACPermissions.LogGmTrade);
                if (!packet.Info.Attachments.Empty())
                {
                    foreach (var item in items)
                    {
                        if (log)
                        {
                            Log.outCommand(GetAccountId(), "GM {0} ({1}) (Account: {2}) mail item: {3} (Entry: {4} Count: {5}) to player: {6} ({7}) (Account: {8})", 
                                GetPlayerName(), GetPlayer().GetGUID().ToString(), GetAccountId(), item.GetTemplate().GetName(), item.GetEntry(), item.GetCount(),
                                packet.Info.Target, receiverGuid.ToString(), receiverAccountId);
                        }

                        item.SetNotRefundable(GetPlayer()); // makes the item no longer refundable
                        player.MoveItemFromInventory(item.GetBagSlot(), item.GetSlot(), true);

                        item.DeleteFromInventoryDB(trans);     // deletes item from character's inventory
                        item.SetOwnerGUID(receiverGuid);
                        item.SetState(ItemUpdateState.Changed);
                        item.SaveToDB(trans);                  // recursive and not have transaction guard into self, item not in inventory and can be save standalone

                        draft.AddItem(item);
                    }

                    // if item send to character at another account, then apply item delivery delay
                    needItemDelay = player.GetSession().GetAccountId() != receiverAccountId;
                }

                if (log && packet.Info.SendMoney > 0)
                {
                    Log.outCommand(GetAccountId(), "GM {0} ({1}) (Account: {{2}) mail money: {3} to player: {4} ({5}) (Account: {6})",
                        GetPlayerName(), GetPlayer().GetGUID().ToString(), GetAccountId(), packet.Info.SendMoney, packet.Info.Target, receiverGuid.ToString(), receiverAccountId);
                }
            }

            // If theres is an item, there is a one hour delivery delay if sent to another account's character.
            uint deliver_delay = needItemDelay ? WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay) : 0;

            // Mail sent between guild members arrives instantly
            Guild guild = Global.GuildMgr.GetGuildById(player.GetGuildId());
            if (guild)
                if (guild.IsMember(receiverGuid))
                    deliver_delay = 0;

            // don't ask for COD if there are no items
            if (packet.Info.Attachments.Empty())
                packet.Info.Cod = 0;

            // will delete item or place to receiver mail list
            draft.AddMoney((ulong)packet.Info.SendMoney).AddCOD((uint)packet.Info.Cod).SendMailTo(trans, new MailReceiver(receiver, receiverGuid.GetCounter()), new MailSender(player), string.IsNullOrEmpty(packet.Info.Body) ? MailCheckMask.Copied : MailCheckMask.HasBody, deliver_delay);

            player.SaveInventoryAndGoldToDB(trans);
            DB.Characters.CommitTransaction(trans);
        }

        //called when mail is read
        [WorldPacketHandler(ClientOpcodes.MailMarkAsRead)]
        void HandleMailMarkAsRead(MailMarkAsRead packet)
        {
            if (!CanOpenMailBox(packet.Mailbox))
                return;

            Player player = GetPlayer();
            Mail m = player.GetMail(packet.MailID);
            if (m != null && m.state != MailState.Deleted)
            {
                if (player.unReadMails != 0)
                    --player.unReadMails;
                m.checkMask = m.checkMask | MailCheckMask.Read;
                player.m_mailsUpdated = true;
                m.state = MailState.Changed;
            }
        }

        //called when client deletes mail
        [WorldPacketHandler(ClientOpcodes.MailDelete)]
        void HandleMailDelete(MailDelete packet)
        {
            Mail m = GetPlayer().GetMail(packet.MailID);
            Player player = GetPlayer();
            player.m_mailsUpdated = true;
            if (m != null)
            {
                // delete shouldn't show up for COD mails
                if (m.COD != 0)
                {
                    player.SendMailResult(packet.MailID, MailResponseType.Deleted, MailResponseResult.InternalError);
                    return;
                }

                m.state = MailState.Deleted;
            }
            player.SendMailResult(packet.MailID, MailResponseType.Deleted, MailResponseResult.Ok);
        }

        [WorldPacketHandler(ClientOpcodes.MailReturnToSender)]
        void HandleMailReturnToSender(MailReturnToSender packet)
        {
            if (!CanOpenMailBox(_player.PlayerTalkClass.GetInteractionData().SourceGuid))
                return;

            Player player = GetPlayer();
            Mail m = player.GetMail(packet.MailID);
            if (m == null || m.state == MailState.Deleted || m.deliver_time > Time.UnixTime || m.sender != packet.SenderGUID.GetCounter())
            {
                player.SendMailResult(packet.MailID, MailResponseType.ReturnedToSender, MailResponseResult.InternalError);
                return;
            }
            //we can return mail now, so firstly delete the old one
            SQLTransaction trans = new SQLTransaction();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
            stmt.AddValue(0, packet.MailID);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
            stmt.AddValue(0, packet.MailID);
            trans.Append(stmt);

            player.RemoveMail(packet.MailID);

            // only return mail if the player exists (and delete if not existing)
            if (m.messageType == MailMessageType.Normal && m.sender != 0)
            {
                MailDraft draft = new MailDraft(m.subject, m.body);
                if (m.mailTemplateId != 0)
                    draft = new MailDraft(m.mailTemplateId, false);     // items already included

                if (m.HasItems())
                {
                    foreach (var itemInfo in m.items)
                    {
                        Item item = player.GetMItem(itemInfo.item_guid);
                        if (item)
                            draft.AddItem(item);
                        player.RemoveMItem(itemInfo.item_guid);
                    }
                }
                draft.AddMoney(m.money).SendReturnToSender(GetAccountId(), m.receiver, m.sender, trans);
            }

            DB.Characters.CommitTransaction(trans);

            player.SendMailResult(packet.MailID, MailResponseType.ReturnedToSender, MailResponseResult.Ok);
        }

        //called when player takes item attached in mail
        [WorldPacketHandler(ClientOpcodes.MailTakeItem)]
        void HandleMailTakeItem(MailTakeItem packet)
        {
            uint AttachID = packet.AttachID;

            if (!CanOpenMailBox(packet.Mailbox))
                return;

            Player player = GetPlayer();

            Mail m = player.GetMail(packet.MailID);
            if (m == null || m.state == MailState.Deleted || m.deliver_time > Time.UnixTime)
            {
                player.SendMailResult(packet.MailID, MailResponseType.ItemTaken, MailResponseResult.InternalError);
                return;
            }

            // verify that the mail has the item to avoid cheaters taking COD items without paying
            if (!m.items.Any(p => p.item_guid == AttachID))
            {
                player.SendMailResult(packet.MailID, MailResponseType.ItemTaken, MailResponseResult.InternalError);
                return;
            }

            // prevent cheating with skip client money check
            if (!player.HasEnoughMoney(m.COD))
            {
                player.SendMailResult(packet.MailID, MailResponseType.ItemTaken, MailResponseResult.NotEnoughMoney);
                return;
            }

            Item it = player.GetMItem(packet.AttachID);

            List<ItemPosCount> dest = new List<ItemPosCount>();
            InventoryResult msg = GetPlayer().CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, it, false);
            if (msg == InventoryResult.Ok)
            {
                SQLTransaction trans = new SQLTransaction();
                m.RemoveItem(packet.AttachID);
                m.removedItems.Add(packet.AttachID);

                if (m.COD > 0)                                     //if there is COD, take COD money from player and send them to sender by mail
                {
                    ObjectGuid sender_guid = ObjectGuid.Create(HighGuid.Player, m.sender);
                    Player receiver = Global.ObjAccessor.FindPlayer(sender_guid);

                    uint sender_accId = 0;

                    if (HasPermission(RBACPermissions.LogGmTrade))
                    {
                        string sender_name;
                        if (receiver)
                        {
                            sender_accId = receiver.GetSession().GetAccountId();
                            sender_name = receiver.GetName();
                        }
                        else
                        {
                            // can be calculated early
                            sender_accId = ObjectManager.GetPlayerAccountIdByGUID(sender_guid);

                            if (!ObjectManager.GetPlayerNameByGUID(sender_guid, out sender_name))
                                sender_name = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);
                        }
                        Log.outCommand(GetAccountId(), "GM {0} (Account: {1}) receiver mail item: {2} (Entry: {3} Count: {4}) and send COD money: {5} to player: {6} (Account: {7})",
                            GetPlayerName(), GetAccountId(), it.GetTemplate().GetName(), it.GetEntry(), it.GetCount(), m.COD, sender_name, sender_accId);
                    }
                    else if (!receiver)
                        sender_accId = ObjectManager.GetPlayerAccountIdByGUID(sender_guid);

                    // check player existence
                    if (receiver || sender_accId != 0)
                    {
                        new MailDraft(m.subject, "")
                            .AddMoney(m.COD)
                            .SendMailTo(trans, new MailReceiver(receiver, m.sender), new MailSender( MailMessageType.Normal, m.receiver), MailCheckMask.CodPayment);
                    }

                    player.ModifyMoney(-(long)m.COD);
                }
                m.COD = 0;
                m.state = MailState.Changed;
                player.m_mailsUpdated = true;
                player.RemoveMItem(it.GetGUID().GetCounter());

                uint count = it.GetCount();                      // save counts before store and possible merge with deleting
                it.SetState(ItemUpdateState.Unchanged);                       // need to set this state, otherwise item cannot be removed later, if neccessary
                player.MoveItemToInventory(dest, it, true);

                player.SaveInventoryAndGoldToDB(trans);
                player._SaveMail(trans);
                DB.Characters.CommitTransaction(trans);

                player.SendMailResult(packet.MailID, MailResponseType.ItemTaken, MailResponseResult.Ok, 0, packet.AttachID, count);
            }
            else
                player.SendMailResult(packet.MailID, MailResponseType.ItemTaken, MailResponseResult.EquipError, msg);
        }

        [WorldPacketHandler(ClientOpcodes.MailTakeMoney)]
        void HandleMailTakeMoney(MailTakeMoney packet)
        {
            if (!CanOpenMailBox(packet.Mailbox))
                return;

            Player player = GetPlayer();

            Mail m = player.GetMail(packet.MailID);
            if ((m == null || m.state == MailState.Deleted || m.deliver_time > Time.UnixTime) ||
                (packet.Money > 0 && m.money != (ulong)packet.Money))
            {
                player.SendMailResult(packet.MailID, MailResponseType.MoneyTaken, MailResponseResult.InternalError);
                return;
            }

            if (!player.ModifyMoney((long)m.money, false))
            {
                player.SendMailResult(packet.MailID, MailResponseType.MoneyTaken, MailResponseResult.EquipError, InventoryResult.TooMuchGold);
                return;
            }

            m.money = 0;
            m.state = MailState.Changed;
            player.m_mailsUpdated = true;

            player.SendMailResult(packet.MailID, MailResponseType.MoneyTaken, MailResponseResult.Ok);

            // save money and mail to prevent cheating
            SQLTransaction trans = new SQLTransaction();
            player.SaveGoldToDB(trans);
            player._SaveMail(trans);
            DB.Characters.CommitTransaction(trans);
        }

        //called when player lists his received mails
        [WorldPacketHandler(ClientOpcodes.MailGetList)]
        void HandleGetMailList(MailGetList packet)
        {
            if (!CanOpenMailBox(packet.Mailbox))
                return;

            Player player = GetPlayer();

            //load players mails, and mailed items
            if (!player.m_mailsLoaded)
                player._LoadMail();

            var mails = player.GetMails();

            MailListResult response = new MailListResult();
            long curTime  = Time.UnixTime;

            foreach (Mail m in mails)
            {
                // skip deleted or not delivered (deliver delay not expired) mails
                if (m.state == MailState.Deleted || curTime < m.deliver_time)
                    continue;

                // max. 50 mails can be sent
                if (response.Mails.Count < 50)
                    response.Mails.Add(new MailListEntry(m, player));
            }

            player.PlayerTalkClass.GetInteractionData().Reset();
            player.PlayerTalkClass.GetInteractionData().SourceGuid = packet.Mailbox;
            SendPacket(response);

            // recalculate m_nextMailDelivereTime and unReadMails
            GetPlayer().UpdateNextMailTimeAndUnreads();
        }

        //used when player copies mail body to his inventory
        [WorldPacketHandler(ClientOpcodes.MailCreateTextItem)]
        void HandleMailCreateTextItem(MailCreateTextItem packet)
        {
            if (!CanOpenMailBox(packet.Mailbox))
                return;

            Player player = GetPlayer();

            Mail m = player.GetMail(packet.MailID);
            if (m == null || (string.IsNullOrEmpty(m.body) && m.mailTemplateId == 0) || m.state == MailState.Deleted || m.deliver_time > Time.UnixTime)
            {
                player.SendMailResult(packet.MailID, MailResponseType.MadePermanent, MailResponseResult.InternalError);
                return;
            }

            Item bodyItem = new Item();                              // This is not bag and then can be used new Item.
            if (!bodyItem.Create(Global.ObjectMgr.GetGenerator(HighGuid.Item).Generate(), 8383, player))
                return;

            // in mail template case we need create new item text
            if (m.mailTemplateId != 0)
            {
                MailTemplateRecord mailTemplateEntry = CliDB.MailTemplateStorage.LookupByKey(m.mailTemplateId);
                if (mailTemplateEntry == null)
                {
                    player.SendMailResult(packet.MailID, MailResponseType.MadePermanent, MailResponseResult.InternalError);
                    return;
                }

                bodyItem.SetText(mailTemplateEntry.Body[GetSessionDbcLocale()]);
            }
            else
                bodyItem.SetText(m.body);

            if (m.messageType == MailMessageType.Normal)
                bodyItem.SetGuidValue(ItemFields.Creator, ObjectGuid.Create(HighGuid.Player, m.sender));

            bodyItem.SetFlag(ItemFields.Flags, ItemFieldFlags.Readable);

            Log.outInfo(LogFilter.Network, "HandleMailCreateTextItem mailid={0}", packet.MailID);

            List<ItemPosCount> dest = new List<ItemPosCount>();
            InventoryResult msg = GetPlayer().CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, bodyItem, false);
            if (msg == InventoryResult.Ok)
            {
                m.checkMask = m.checkMask | MailCheckMask.Copied;
                m.state = MailState.Changed;
                player.m_mailsUpdated = true;

                player.StoreItem(dest, bodyItem, true);
                player.SendMailResult(packet.MailID, MailResponseType.MadePermanent, MailResponseResult.Ok);
            }
            else
                player.SendMailResult(packet.MailID, MailResponseType.MadePermanent, MailResponseResult.EquipError, msg);
        }

        [WorldPacketHandler(ClientOpcodes.QueryNextMailTime)]
        void HandleQueryNextMailTime(MailQueryNextMailTime packet)
        {
            MailQueryNextTimeResult result = new MailQueryNextTimeResult();

            if (!GetPlayer().m_mailsLoaded)
                GetPlayer()._LoadMail();

            if (GetPlayer().unReadMails > 0)
            {
                result.NextMailTime = 0.0f;

                long now = Time.UnixTime;
                List<ulong> sentSenders = new List<ulong>();

                foreach (Mail mail in GetPlayer().GetMails())
                {
                    if (mail.checkMask.HasAnyFlag(MailCheckMask.Read))
                        continue;

                    // and already delivered
                    if (now < mail.deliver_time)
                        continue;

                    // only send each mail sender once
                    if (sentSenders.Any(p => p == mail.sender))
                        continue;

                    result.Next.Add(new MailQueryNextTimeResult.MailNextTimeEntry(mail));

                    sentSenders.Add(mail.sender);

                    // do not send more than 2 mails
                    if (sentSenders.Count > 2)
                        break;
                }
            }
            else
                result.NextMailTime = -Time.Day;

            SendPacket(result);
        }

        public void SendShowMailBox(ObjectGuid guid)
        {
            ShowMailbox packet = new ShowMailbox();
            packet.PostmasterGUID = guid;
            SendPacket(packet);
        }
    }
}
