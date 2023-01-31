// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Game.Cache;
using Game.DataStorage;
using Game.Entities;
using Game.Guilds;
using Game.Mails;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        public void SendShowMailBox(ObjectGuid guid)
        {
            NPCInteractionOpenResult npcInteraction = new();
            npcInteraction.Npc = guid;
            npcInteraction.InteractionType = PlayerInteractionType.MailInfo;
            npcInteraction.Success = true;
            SendPacket(npcInteraction);
        }

        private bool CanOpenMailBox(ObjectGuid guid)
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
                if (!GetPlayer().GetNPCIfCanInteractWith(guid, NPCFlags.Mailbox, NPCFlags2.None))
                    return false;
            }
            else
            {
                return false;
            }

            return true;
        }

        [WorldPacketHandler(ClientOpcodes.SendMail)]
        private void HandleSendMail(SendMail sendMail)
        {
            if (sendMail.Info.Attachments.Count > SharedConst.MaxClientMailItems) // client limit
            {
                GetPlayer().SendMailResult(0, MailResponseType.Send, MailResponseResult.TooManyAttachments);

                return;
            }

            if (!CanOpenMailBox(sendMail.Info.Mailbox))
                return;

            if (string.IsNullOrEmpty(sendMail.Info.Target))
                return;

            if (!ValidateHyperlinksAndMaybeKick(sendMail.Info.Subject) ||
                !ValidateHyperlinksAndMaybeKick(sendMail.Info.Body))
                return;

            Player player = GetPlayer();

            if (player.GetLevel() < WorldConfig.GetIntValue(WorldCfg.MailLevelReq))
            {
                SendNotification(CypherStrings.MailSenderReq, WorldConfig.GetIntValue(WorldCfg.MailLevelReq));

                return;
            }

            ObjectGuid receiverGuid = ObjectGuid.Empty;

            if (ObjectManager.NormalizePlayerName(ref sendMail.Info.Target))
                receiverGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(sendMail.Info.Target);

            if (receiverGuid.IsEmpty())
            {
                Log.outInfo(LogFilter.Network,
                            "Player {0} is sending mail to {1} (GUID: not existed!) with subject {2}" +
                            "and body {3} includes {4} items, {5} copper and {6} COD copper with StationeryID = {7}",
                            GetPlayerInfo(),
                            sendMail.Info.Target,
                            sendMail.Info.Subject,
                            sendMail.Info.Body,
                            sendMail.Info.Attachments.Count,
                            sendMail.Info.SendMoney,
                            sendMail.Info.Cod,
                            sendMail.Info.StationeryID);

                player.SendMailResult(0, MailResponseType.Send, MailResponseResult.RecipientNotFound);

                return;
            }

            if (sendMail.Info.SendMoney < 0)
            {
                GetPlayer().SendMailResult(0, MailResponseType.Send, MailResponseResult.InternalError);

                Log.outWarn(LogFilter.Server,
                            "Player {0} attempted to send mail to {1} ({2}) with negative money value (SendMoney: {3})",
                            GetPlayerInfo(),
                            sendMail.Info.Target,
                            receiverGuid.ToString(),
                            sendMail.Info.SendMoney);

                return;
            }

            if (sendMail.Info.Cod < 0)
            {
                GetPlayer().SendMailResult(0, MailResponseType.Send, MailResponseResult.InternalError);

                Log.outWarn(LogFilter.Server,
                            "Player {0} attempted to send mail to {1} ({2}) with negative COD value (Cod: {3})",
                            GetPlayerInfo(),
                            sendMail.Info.Target,
                            receiverGuid.ToString(),
                            sendMail.Info.Cod);

                return;
            }

            Log.outInfo(LogFilter.Network,
                        "Player {0} is sending mail to {1} ({2}) with subject {3} and body {4}" +
                        "includes {5} items, {6} copper and {7} COD copper with StationeryID = {8}",
                        GetPlayerInfo(),
                        sendMail.Info.Target,
                        receiverGuid.ToString(),
                        sendMail.Info.Subject,
                        sendMail.Info.Body,
                        sendMail.Info.Attachments.Count,
                        sendMail.Info.SendMoney,
                        sendMail.Info.Cod,
                        sendMail.Info.StationeryID);

            if (player.GetGUID() == receiverGuid)
            {
                player.SendMailResult(0, MailResponseType.Send, MailResponseResult.CannotSendToSelf);

                return;
            }

            uint cost = (uint)(!sendMail.Info.Attachments.Empty() ? 30 * sendMail.Info.Attachments.Count : 30); // price hardcoded in client

            long reqmoney = cost + sendMail.Info.SendMoney;

            // Check for overflow
            if (reqmoney < sendMail.Info.SendMoney)
            {
                player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotEnoughMoney);

                return;
            }

            if (!player.HasEnoughMoney(reqmoney) &&
                !player.IsGameMaster())
            {
                player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotEnoughMoney);

                return;
            }

            void mailCountCheckContinuation(Team receiverTeam, ulong mailsCount, uint receiverLevel, uint receiverAccountId, uint receiverBnetAccountId)
            {
                if (_player != player)
                    return;

                // do not allow to have more than 100 mails in mailbox.. mails Count is in opcode uint8!!! - so max can be 255..
                if (mailsCount > 100)
                {
                    player.SendMailResult(0, MailResponseType.Send, MailResponseResult.RecipientCapReached);

                    return;
                }

                // test the receiver's Faction... or all items are account bound
                bool accountBound = !sendMail.Info.Attachments.Empty();

                foreach (var att in sendMail.Info.Attachments)
                {
                    Item item = player.GetItemByGuid(att.ItemGUID);

                    if (item != null)
                    {
                        ItemTemplate itemProto = item.GetTemplate();

                        if (itemProto == null ||
                            !itemProto.HasFlag(ItemFlags.IsBoundToAccount))
                        {
                            accountBound = false;

                            break;
                        }
                    }
                }

                if (!accountBound &&
                    player.GetTeam() != receiverTeam &&
                    !HasPermission(RBACPermissions.TwoSideInteractionMail))
                {
                    player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotYourTeam);

                    return;
                }

                if (receiverLevel < WorldConfig.GetIntValue(WorldCfg.MailLevelReq))
                {
                    SendNotification(CypherStrings.MailReceiverReq, WorldConfig.GetIntValue(WorldCfg.MailLevelReq));

                    return;
                }

                List<Item> items = new();

                foreach (var att in sendMail.Info.Attachments)
                {
                    if (att.ItemGUID.IsEmpty())
                    {
                        player.SendMailResult(0, MailResponseType.Send, MailResponseResult.MailAttachmentInvalid);

                        return;
                    }

                    Item item = player.GetItemByGuid(att.ItemGUID);

                    // prevent sending bag with items (cheat: can be placed in bag after adding equipped empty bag to mail)
                    if (item == null)
                    {
                        player.SendMailResult(0, MailResponseType.Send, MailResponseResult.MailAttachmentInvalid);

                        return;
                    }

                    if (!item.CanBeTraded(true))
                    {
                        player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.MailBoundItem);

                        return;
                    }

                    if (item.IsBoundAccountWide() &&
                        item.IsSoulBound() &&
                        player.Session.GetAccountId() != receiverAccountId)
                        if (!item.IsBattlenetAccountBound() ||
                            player.Session.GetBattlenetAccountId() == 0 ||
                            player.Session.GetBattlenetAccountId() != receiverBnetAccountId)
                        {
                            player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.NotSameAccount);

                            return;
                        }

                    if (item.GetTemplate().HasFlag(ItemFlags.Conjured) ||
                        item._itemData.Expiration != 0)
                    {
                        player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.MailBoundItem);

                        return;
                    }

                    if (sendMail.Info.Cod != 0 &&
                        item.IsWrapped())
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
                player.UpdateCriteria(CriteriaType.MoneySpentOnPostage, cost);

                bool needItemDelay = false;

                MailDraft draft = new(sendMail.Info.Subject, sendMail.Info.Body);

                SQLTransaction trans = new();

                if (!sendMail.Info.Attachments.Empty() ||
                    sendMail.Info.SendMoney > 0)
                {
                    bool log = HasPermission(RBACPermissions.LogGmTrade);

                    if (!sendMail.Info.Attachments.Empty())
                    {
                        foreach (var item in items)
                        {
                            if (log)
                                Log.outCommand(GetAccountId(),
                                               $"GM {GetPlayerName()} ({_player.GetGUID()}) (Account: {GetAccountId()}) mail Item: {item.GetTemplate().GetName()} " +
                                               $"(Entry: {item.GetEntry()} Count: {item.GetCount()}) to: {sendMail.Info.Target} ({receiverGuid}) (Account: {receiverAccountId})");

                            item.SetNotRefundable(GetPlayer()); // makes the Item no longer refundable
                            player.MoveItemFromInventory(item.GetBagSlot(), item.GetSlot(), true);

                            item.DeleteFromInventoryDB(trans); // deletes Item from character's inventory
                            item.SetOwnerGUID(receiverGuid);
                            item.SetState(ItemUpdateState.Changed);
                            item.SaveToDB(trans); // recursive and not have transaction guard into self, Item not in inventory and can be save standalone

                            draft.AddItem(item);
                        }

                        // if Item send to character at another account, then apply Item delivery delay
                        needItemDelay = player.Session.GetAccountId() != receiverAccountId;
                    }

                    if (log && sendMail.Info.SendMoney > 0)
                        Log.outCommand(GetAccountId(), "GM {GetPlayerName()} ({_player.GetGUID()}) (Account: {GetAccountId()}) mail money: {mailInfo.SendMoney} to: {mailInfo.Target} ({receiverGuid}) (Account: {receiverAccountId})");
                }

                // If theres is an Item, there is a one hour delivery delay if sent to another account's character.
                uint deliver_delay = needItemDelay ? WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay) : 0;

                // Mail sent between guild members arrives instantly
                Guild guild = Global.GuildMgr.GetGuildById(player.GetGuildId());

                if (guild != null)
                    if (guild.IsMember(receiverGuid))
                        deliver_delay = 0;

                // don't ask for COD if there are no items
                if (sendMail.Info.Attachments.Empty())
                    sendMail.Info.Cod = 0;

                // will delete Item or place to receiver mail list
                draft.AddMoney((ulong)sendMail.Info.SendMoney)
                     .AddCOD((uint)sendMail.Info.Cod)
                     .SendMailTo(trans, new MailReceiver(Global.ObjAccessor.FindConnectedPlayer(receiverGuid), receiverGuid.GetCounter()), new MailSender(player), sendMail.Info.Body.IsEmpty() ? MailCheckMask.Copied : MailCheckMask.HasBody, deliver_delay);

                player.SaveInventoryAndGoldToDB(trans);
                DB.Characters.CommitTransaction(trans);
            }

            Player receiver = Global.ObjAccessor.FindPlayer(receiverGuid);

            if (receiver != null)
            {
                mailCountCheckContinuation(receiver.GetTeam(), receiver.GetMailSize(), receiver.GetLevel(), receiver.Session.GetAccountId(), receiver.Session.GetBattlenetAccountId());
            }
            else
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_COUNT);
                stmt.AddValue(0, receiverGuid.GetCounter());

                GetQueryProcessor()
                    .AddCallback(DB.Characters.AsyncQuery(stmt)
                                   .WithChainingCallback((queryCallback, mailCountResult) =>
                                                         {
                                                             CharacterCacheEntry characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(receiverGuid);

                                                             if (characterInfo != null)
                                                                 queryCallback.WithCallback(bnetAccountResult =>
                                                                                            {
                                                                                                mailCountCheckContinuation(Player.TeamForRace(characterInfo.RaceId),
                                                                                                                           !mailCountResult.IsEmpty() ? mailCountResult.Read<ulong>(0) : 0,
                                                                                                                           characterInfo.Level,
                                                                                                                           characterInfo.AccountId,
                                                                                                                           !bnetAccountResult.IsEmpty() ? bnetAccountResult.Read<uint>(0) : 0);
                                                                                            })
                                                                              .SetNextQuery(Global.BNetAccountMgr.GetIdByGameAccountAsync(characterInfo.AccountId));
                                                         }));
            }
        }

        //called when mail is read
        [WorldPacketHandler(ClientOpcodes.MailMarkAsRead)]
        private void HandleMailMarkAsRead(MailMarkAsRead markAsRead)
        {
            if (!CanOpenMailBox(markAsRead.Mailbox))
                return;

            Player player = GetPlayer();
            Mail m = player.GetMail(markAsRead.MailID);

            if (m != null &&
                m.State != MailState.Deleted)
            {
                if (player.UnReadMails != 0)
                    --player.UnReadMails;

                m.CheckMask = m.CheckMask | MailCheckMask.Read;
                player.MailUpdated = true;
                m.State = MailState.Changed;
            }
        }

        //called when client deletes mail
        [WorldPacketHandler(ClientOpcodes.MailDelete)]
        private void HandleMailDelete(MailDelete mailDelete)
        {
            Mail m = GetPlayer().GetMail(mailDelete.MailID);
            Player player = GetPlayer();
            player.MailUpdated = true;

            if (m != null)
            {
                // delete shouldn't show up for COD mails
                if (m.COD != 0)
                {
                    player.SendMailResult(mailDelete.MailID, MailResponseType.Deleted, MailResponseResult.InternalError);

                    return;
                }

                m.State = MailState.Deleted;
            }

            player.SendMailResult(mailDelete.MailID, MailResponseType.Deleted, MailResponseResult.Ok);
        }

        [WorldPacketHandler(ClientOpcodes.MailReturnToSender)]
        private void HandleMailReturnToSender(MailReturnToSender returnToSender)
        {
            if (!CanOpenMailBox(_player.PlayerTalkClass.GetInteractionData().SourceGuid))
                return;

            Player player = GetPlayer();
            Mail m = player.GetMail(returnToSender.MailID);

            if (m == null ||
                m.State == MailState.Deleted ||
                m.Deliver_time > GameTime.GetGameTime() ||
                m.Sender != returnToSender.SenderGUID.GetCounter())
            {
                player.SendMailResult(returnToSender.MailID, MailResponseType.ReturnedToSender, MailResponseResult.InternalError);

                return;
            }

            //we can return mail now, so firstly delete the old one
            SQLTransaction trans = new();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
            stmt.AddValue(0, returnToSender.MailID);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
            stmt.AddValue(0, returnToSender.MailID);
            trans.Append(stmt);

            player.RemoveMail(returnToSender.MailID);

            // only return mail if the player exists (and delete if not existing)
            if (m.MessageType == MailMessageType.Normal &&
                m.Sender != 0)
            {
                MailDraft draft = new(m.Subject, m.Body);

                if (m.MailTemplateId != 0)
                    draft = new MailDraft(m.MailTemplateId, false); // items already included

                if (m.HasItems())
                    foreach (var itemInfo in m.Items)
                    {
                        Item item = player.GetMItem(itemInfo.ItemGuid);

                        if (item)
                            draft.AddItem(item);

                        player.RemoveMItem(itemInfo.ItemGuid);
                    }

                draft.AddMoney(m.Money).SendReturnToSender(GetAccountId(), m.Receiver, m.Sender, trans);
            }

            DB.Characters.CommitTransaction(trans);

            player.SendMailResult(returnToSender.MailID, MailResponseType.ReturnedToSender, MailResponseResult.Ok);
        }

        //called when player takes Item attached in mail
        [WorldPacketHandler(ClientOpcodes.MailTakeItem)]
        private void HandleMailTakeItem(MailTakeItem takeItem)
        {
            uint AttachID = takeItem.AttachID;

            if (!CanOpenMailBox(takeItem.Mailbox))
                return;

            Player player = GetPlayer();

            Mail m = player.GetMail(takeItem.MailID);

            if (m == null ||
                m.State == MailState.Deleted ||
                m.Deliver_time > GameTime.GetGameTime())
            {
                player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.InternalError);

                return;
            }

            // verify that the mail has the Item to avoid cheaters taking COD items without paying
            if (!m.Items.Any(p => p.ItemGuid == AttachID))
            {
                player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.InternalError);

                return;
            }

            // prevent cheating with skip client money check
            if (!player.HasEnoughMoney(m.COD))
            {
                player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.NotEnoughMoney);

                return;
            }

            Item it = player.GetMItem(takeItem.AttachID);

            List<ItemPosCount> dest = new();
            InventoryResult msg = GetPlayer().CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, it, false);

            if (msg == InventoryResult.Ok)
            {
                SQLTransaction trans = new();
                m.RemoveItem(takeItem.AttachID);
                m.RemovedItems.Add(takeItem.AttachID);

                if (m.COD > 0) //if there is COD, take COD money from player and send them to sender by mail
                {
                    ObjectGuid sender_guid = ObjectGuid.Create(HighGuid.Player, m.Sender);
                    Player receiver = Global.ObjAccessor.FindPlayer(sender_guid);

                    uint sender_accId = 0;

                    if (HasPermission(RBACPermissions.LogGmTrade))
                    {
                        string sender_name;

                        if (receiver)
                        {
                            sender_accId = receiver.Session.GetAccountId();
                            sender_name = receiver.GetName();
                        }
                        else
                        {
                            // can be calculated early
                            sender_accId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(sender_guid);

                            if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(sender_guid, out sender_name))
                                sender_name = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);
                        }

                        Log.outCommand(GetAccountId(),
                                       "GM {0} (Account: {1}) receiver mail Item: {2} (Entry: {3} Count: {4}) and send COD money: {5} to player: {6} (Account: {7})",
                                       GetPlayerName(),
                                       GetAccountId(),
                                       it.GetTemplate().GetName(),
                                       it.GetEntry(),
                                       it.GetCount(),
                                       m.COD,
                                       sender_name,
                                       sender_accId);
                    }
                    else if (!receiver)
                    {
                        sender_accId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(sender_guid);
                    }

                    // check player existence
                    if (receiver || sender_accId != 0)
                        new MailDraft(m.Subject, "")
                            .AddMoney(m.COD)
                            .SendMailTo(trans, new MailReceiver(receiver, m.Sender), new MailSender(MailMessageType.Normal, m.Receiver), MailCheckMask.CodPayment);

                    player.ModifyMoney(-(long)m.COD);
                }

                m.COD = 0;
                m.State = MailState.Changed;
                player.MailUpdated = true;
                player.RemoveMItem(it.GetGUID().GetCounter());

                uint count = it.GetCount();             // save counts before store and possible merge with deleting
                it.SetState(ItemUpdateState.Unchanged); // need to set this State, otherwise Item cannot be removed later, if neccessary
                player.MoveItemToInventory(dest, it, true);

                player.SaveInventoryAndGoldToDB(trans);
                player._SaveMail(trans);
                DB.Characters.CommitTransaction(trans);

                player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.Ok, 0, takeItem.AttachID, count);
            }
            else
            {
                player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.EquipError, msg);
            }
        }

        [WorldPacketHandler(ClientOpcodes.MailTakeMoney)]
        private void HandleMailTakeMoney(MailTakeMoney takeMoney)
        {
            if (!CanOpenMailBox(takeMoney.Mailbox))
                return;

            Player player = GetPlayer();

            Mail m = player.GetMail(takeMoney.MailID);

            if ((m == null || m.State == MailState.Deleted || m.Deliver_time > GameTime.GetGameTime()) ||
                (takeMoney.Money > 0 && m.Money != (ulong)takeMoney.Money))
            {
                player.SendMailResult(takeMoney.MailID, MailResponseType.MoneyTaken, MailResponseResult.InternalError);

                return;
            }

            if (!player.ModifyMoney((long)m.Money, false))
            {
                player.SendMailResult(takeMoney.MailID, MailResponseType.MoneyTaken, MailResponseResult.EquipError, InventoryResult.TooMuchGold);

                return;
            }

            m.Money = 0;
            m.State = MailState.Changed;
            player.MailUpdated = true;

            player.SendMailResult(takeMoney.MailID, MailResponseType.MoneyTaken, MailResponseResult.Ok);

            // save money and mail to prevent cheating
            SQLTransaction trans = new();
            player.SaveGoldToDB(trans);
            player._SaveMail(trans);
            DB.Characters.CommitTransaction(trans);
        }

        //called when player lists his received mails
        [WorldPacketHandler(ClientOpcodes.MailGetList)]
        private void HandleGetMailList(MailGetList getList)
        {
            if (!CanOpenMailBox(getList.Mailbox))
                return;

            Player player = GetPlayer();

            var mails = player.GetMails();

            MailListResult response = new();
            long curTime = GameTime.GetGameTime();

            foreach (Mail m in mails)
            {
                // skip deleted or not delivered (deliver delay not expired) mails
                if (m.State == MailState.Deleted ||
                    curTime < m.Deliver_time)
                    continue;

                // max. 100 mails can be sent
                if (response.Mails.Count < 100)
                    response.Mails.Add(new MailListEntry(m, player));
            }

            player.PlayerTalkClass.GetInteractionData().Reset();
            player.PlayerTalkClass.GetInteractionData().SourceGuid = getList.Mailbox;
            SendPacket(response);

            // recalculate _nextMailDelivereTime and UnReadMails
            GetPlayer().UpdateNextMailTimeAndUnreads();
        }

        //used when player copies mail body to his inventory
        [WorldPacketHandler(ClientOpcodes.MailCreateTextItem)]
        private void HandleMailCreateTextItem(MailCreateTextItem createTextItem)
        {
            if (!CanOpenMailBox(createTextItem.Mailbox))
                return;

            Player player = GetPlayer();

            Mail m = player.GetMail(createTextItem.MailID);

            if (m == null ||
                (string.IsNullOrEmpty(m.Body) && m.MailTemplateId == 0) ||
                m.State == MailState.Deleted ||
                m.Deliver_time > GameTime.GetGameTime())
            {
                player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.InternalError);

                return;
            }

            Item bodyItem = new(); // This is not bag and then can be used new Item.

            if (!bodyItem.Create(Global.ObjectMgr.GetGenerator(HighGuid.Item).Generate(), 8383, ItemContext.None, player))
                return;

            // in mail template case we need create new Item text
            if (m.MailTemplateId != 0)
            {
                MailTemplateRecord mailTemplateEntry = CliDB.MailTemplateStorage.LookupByKey(m.MailTemplateId);

                if (mailTemplateEntry == null)
                {
                    player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.InternalError);

                    return;
                }

                bodyItem.SetText(mailTemplateEntry.Body[GetSessionDbcLocale()]);
            }
            else
            {
                bodyItem.SetText(m.Body);
            }

            if (m.MessageType == MailMessageType.Normal)
                bodyItem.SetCreator(ObjectGuid.Create(HighGuid.Player, m.Sender));

            bodyItem.SetItemFlag(ItemFieldFlags.Readable);

            Log.outInfo(LogFilter.Network, "HandleMailCreateTextItem mailid={0}", createTextItem.MailID);

            List<ItemPosCount> dest = new();
            InventoryResult msg = GetPlayer().CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, bodyItem, false);

            if (msg == InventoryResult.Ok)
            {
                m.CheckMask = m.CheckMask | MailCheckMask.Copied;
                m.State = MailState.Changed;
                player.MailUpdated = true;

                player.StoreItem(dest, bodyItem, true);
                player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.Ok);
            }
            else
            {
                player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.EquipError, msg);
            }
        }

        [WorldPacketHandler(ClientOpcodes.QueryNextMailTime)]
        private void HandleQueryNextMailTime(MailQueryNextMailTime queryNextMailTime)
        {
            MailQueryNextTimeResult result = new();

            if (GetPlayer().UnReadMails > 0)
            {
                result.NextMailTime = 0.0f;

                long now = GameTime.GetGameTime();
                List<ulong> sentSenders = new();

                foreach (Mail mail in GetPlayer().GetMails())
                {
                    if (mail.CheckMask.HasAnyFlag(MailCheckMask.Read))
                        continue;

                    // and already delivered
                    if (now < mail.Deliver_time)
                        continue;

                    // only send each mail sender once
                    if (sentSenders.Any(p => p == mail.Sender))
                        continue;

                    result.Next.Add(new MailQueryNextTimeResult.MailNextTimeEntry(mail));

                    sentSenders.Add(mail.Sender);

                    // do not send more than 2 mails
                    if (sentSenders.Count > 2)
                        break;
                }
            }
            else
            {
                result.NextMailTime = -Time.Day;
            }

            SendPacket(result);
        }
    }
}