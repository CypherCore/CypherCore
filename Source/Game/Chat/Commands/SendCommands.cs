﻿/*
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

using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.Entities;
using Game.Mails;
using System.Collections.Generic;

namespace Game.Chat.Commands
{
    [CommandGroup("send", RBACPermissions.CommandSend, false)]
    class SendCommands
    {
        [Command("mail", RBACPermissions.CommandSendMail, true)]
        static bool HandleSendMailCommand(StringArguments args, CommandHandler handler)
        {
            // format: name "subject text" "mail text"
            Player target;
            ObjectGuid targetGuid;
            string targetName;
            if (!handler.ExtractPlayerTarget(args, out target, out targetGuid, out targetName))
                return false;

            var tail1 = args.NextString("");
            if (string.IsNullOrEmpty(tail1))
                return false;

            var subject = handler.ExtractQuotedArg(tail1);
            if (string.IsNullOrEmpty(subject))
                return false;

            var tail2 = args.NextString("");
            if (string.IsNullOrEmpty(tail2))
                return false;

            var text = handler.ExtractQuotedArg(tail2);
            if (string.IsNullOrEmpty(text))
                return false;

            // from console show not existed sender
            var sender = new MailSender(MailMessageType.Normal, handler.GetSession() ? handler.GetSession().GetPlayer().GetGUID().GetCounter() : 0, MailStationery.Gm);

            // @todo Fix poor design
            var trans = new SQLTransaction();
            new MailDraft(subject, text)
                .SendMailTo(trans, new MailReceiver(target, targetGuid.GetCounter()), sender);

            DB.Characters.CommitTransaction(trans);

            var nameLink = handler.PlayerLink(targetName);
            handler.SendSysMessage(CypherStrings.MailSent, nameLink);
            return true;
        }

        [Command("items", RBACPermissions.CommandSendItems, true)]
        static bool HandleSendItemsCommand(StringArguments args, CommandHandler handler)
        {
            // format: name "subject text" "mail text" item1[:count1] item2[:count2] ... item12[:count12]
            Player receiver;
            ObjectGuid receiverGuid;
            string receiverName;
            if (!handler.ExtractPlayerTarget(args, out receiver, out receiverGuid, out receiverName))
                return false;

            var tail1 = args.NextString("");
            if (string.IsNullOrEmpty(tail1))
                return false;

            var subject = handler.ExtractQuotedArg(tail1);
            if (string.IsNullOrEmpty(subject))
                return false;

            var tail2 = args.NextString("");
            if (string.IsNullOrEmpty(tail2))
                return false;

            var text = handler.ExtractQuotedArg(tail2);
            if (string.IsNullOrEmpty(text))
                return false;

            // extract items
            var items = new List<KeyValuePair<uint, uint>>();

            // get all tail string
            var tail = new StringArguments(args.NextString(""));

            // get from tail next item str
            StringArguments itemStr;
            while (!(itemStr = new StringArguments(tail.NextString(" "))).Empty())
            {
                // parse item str
                var itemIdStr = itemStr.NextString(":");
                var itemCountStr = itemStr.NextString(" ");

                if (!uint.TryParse(itemIdStr, out var itemId) || itemId == 0)
                    return false;

                var item_proto = Global.ObjectMgr.GetItemTemplate(itemId);
                if (item_proto == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandItemidinvalid, itemId);
                    return false;
                }

                if (string.IsNullOrEmpty(itemCountStr) || !uint.TryParse(itemCountStr, out var itemCount))
                    itemCount = 1;

                if (itemCount < 1 || (item_proto.GetMaxCount() > 0 && itemCount > item_proto.GetMaxCount()))
                {
                    handler.SendSysMessage(CypherStrings.CommandInvalidItemCount, itemCount, itemId);
                    return false;
                }

                while (itemCount > item_proto.GetMaxStackSize())
                {
                    items.Add(new KeyValuePair<uint, uint>(itemId, item_proto.GetMaxStackSize()));
                    itemCount -= item_proto.GetMaxStackSize();
                }

                items.Add(new KeyValuePair<uint, uint>(itemId, itemCount));

                if (items.Count > SharedConst.MaxMailItems)
                {
                    handler.SendSysMessage(CypherStrings.CommandMailItemsLimit, SharedConst.MaxMailItems);
                    return false;
                }
            }

            // from console show not existed sender
            var sender = new MailSender(MailMessageType.Normal, handler.GetSession() ? handler.GetSession().GetPlayer().GetGUID().GetCounter() : 0, MailStationery.Gm);

            // fill mail
            var draft = new MailDraft(subject, text);

            var trans = new SQLTransaction();

            foreach (var pair in items)
            {
                var item = Item.CreateItem(pair.Key, pair.Value, ItemContext.None, handler.GetSession() ? handler.GetSession().GetPlayer() : null);
                if (item)
                {
                    item.SaveToDB(trans);                               // save for prevent lost at next mail load, if send fail then item will deleted
                    draft.AddItem(item);
                }
            }

            draft.SendMailTo(trans, new MailReceiver(receiver, receiverGuid.GetCounter()), sender);
            DB.Characters.CommitTransaction(trans);

            var nameLink = handler.PlayerLink(receiverName);
            handler.SendSysMessage(CypherStrings.MailSent, nameLink);
            return true;
        }

        [Command("money", RBACPermissions.CommandSendMoney, true)]
        static bool HandleSendMoneyCommand(StringArguments args, CommandHandler handler)
        {
            // format: name "subject text" "mail text" money

            Player receiver;
            ObjectGuid receiverGuid;
            string receiverName;
            if (!handler.ExtractPlayerTarget(args, out receiver, out receiverGuid, out receiverName))
                return false;

            var tail1 = args.NextString("");
            if (string.IsNullOrEmpty(tail1))
                return false;

            var subject = handler.ExtractQuotedArg(tail1);
            if (string.IsNullOrEmpty(subject))
                return false;

            var tail2 = args.NextString("");
            if (string.IsNullOrEmpty(tail2))
                return false;

            var text = handler.ExtractQuotedArg(tail2);
            if (string.IsNullOrEmpty(text))
                return false;

            if (!long.TryParse(args.NextString(""), out var money))
                money = 0;

            if (money <= 0)
                return false;

            // from console show not existed sender
            var sender = new MailSender(MailMessageType.Normal, handler.GetSession() ? handler.GetSession().GetPlayer().GetGUID().GetCounter() : 0, MailStationery.Gm);

            var trans = new SQLTransaction();

            new MailDraft(subject, text)
                .AddMoney((uint)money)
                .SendMailTo(trans, new MailReceiver(receiver, receiverGuid.GetCounter()), sender);

            DB.Characters.CommitTransaction(trans);

            var nameLink = handler.PlayerLink(receiverName);
            handler.SendSysMessage(CypherStrings.MailSent, nameLink);
            return true;
        }

        [Command("message", RBACPermissions.CommandSendMessage, true)]
        static bool HandleSendMessageCommand(StringArguments args, CommandHandler handler)
        {
            // - Find the player
            Player player;
            if (!handler.ExtractPlayerTarget(args, out player))
                return false;

            var msgStr = args.NextString("");
            if (string.IsNullOrEmpty(msgStr))
                return false;

            // Check that he is not logging out.
            if (player.GetSession().IsLogingOut())
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            // - Send the message
            player.GetSession().SendNotification("{0}", msgStr);
            player.GetSession().SendNotification("|cffff0000[Message from administrator]:|r");

            // Confirmation message
            var nameLink = handler.GetNameLink(player);
            handler.SendSysMessage(CypherStrings.Sendmessage, nameLink, msgStr);

            return true;
        }
    }
}
