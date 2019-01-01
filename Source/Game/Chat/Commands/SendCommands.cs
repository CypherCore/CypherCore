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
            if (!handler.extractPlayerTarget(args, out target, out targetGuid, out targetName))
                return false;

            string tail1 = args.NextString("");
            if (string.IsNullOrEmpty(tail1))
                return false;

            string subject = handler.extractQuotedArg(tail1);
            if (string.IsNullOrEmpty(subject))
                return false;

            string tail2 = args.NextString("");
            if (string.IsNullOrEmpty(tail2))
                return false;

            string text = handler.extractQuotedArg(tail2);
            if (string.IsNullOrEmpty(text))
                return false;

            // from console show not existed sender
            MailSender sender = new MailSender(MailMessageType.Normal, handler.GetSession() ? handler.GetSession().GetPlayer().GetGUID().GetCounter() : 0, MailStationery.Gm);

            // @todo Fix poor design
            SQLTransaction trans = new SQLTransaction();
            new MailDraft(subject, text)
                .SendMailTo(trans, new MailReceiver(target, targetGuid.GetCounter()), sender);

            DB.Characters.CommitTransaction(trans);

            string nameLink = handler.playerLink(targetName);
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
            if (!handler.extractPlayerTarget(args, out receiver, out receiverGuid, out receiverName))
                return false;

            string tail1 = args.NextString("");
            if (string.IsNullOrEmpty(tail1))
                return false;

            string subject = handler.extractQuotedArg(tail1);
            if (string.IsNullOrEmpty(subject))
                return false;

            string tail2 = args.NextString("");
            if (string.IsNullOrEmpty(tail2))
                return false;

            string text = handler.extractQuotedArg(tail2);
            if (string.IsNullOrEmpty(text))
                return false;

            // extract items
            List<KeyValuePair<uint, uint>> items = new List<KeyValuePair<uint, uint>>();

            // get all tail string
            StringArguments tail = new StringArguments(args.NextString(""));

            // get from tail next item str
            StringArguments itemStr;
            while (!(itemStr = new StringArguments(tail.NextString(" "))).Empty())
            {
                // parse item str
                string itemIdStr = itemStr.NextString(":");
                string itemCountStr = itemStr.NextString(" ");

                if (!uint.TryParse(itemIdStr, out uint itemId) || itemId == 0)
                    return false;

                ItemTemplate item_proto = Global.ObjectMgr.GetItemTemplate(itemId);
                if (item_proto == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandItemidinvalid, itemId);
                    return false;
                }

                uint itemCount = 0;
                if (string.IsNullOrEmpty(itemCountStr) || !uint.TryParse(itemCountStr, out itemCount))
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
            MailSender sender = new MailSender(MailMessageType.Normal, handler.GetSession() ? handler.GetSession().GetPlayer().GetGUID().GetCounter() : 0, MailStationery.Gm);

            // fill mail
            MailDraft draft = new MailDraft(subject, text);

            SQLTransaction trans = new SQLTransaction();

            foreach (var pair in items)
            {
                Item item = Item.CreateItem(pair.Key, pair.Value, handler.GetSession() ? handler.GetSession().GetPlayer() : null);
                if (item)
                {
                    item.SaveToDB(trans);                               // save for prevent lost at next mail load, if send fail then item will deleted
                    draft.AddItem(item);
                }
            }

            draft.SendMailTo(trans, new MailReceiver(receiver, receiverGuid.GetCounter()), sender);
            DB.Characters.CommitTransaction(trans);

            string nameLink = handler.playerLink(receiverName);
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
            if (!handler.extractPlayerTarget(args, out receiver, out receiverGuid, out receiverName))
                return false;

            string tail1 = args.NextString("");
            if (string.IsNullOrEmpty(tail1))
                return false;

            string subject = handler.extractQuotedArg(tail1);
            if (string.IsNullOrEmpty(subject))
                return false;

            string tail2 = args.NextString("");
            if (string.IsNullOrEmpty(tail2))
                return false;

            string text = handler.extractQuotedArg(tail2);
            if (string.IsNullOrEmpty(text))
                return false;

            if (!long.TryParse(args.NextString(""), out long money))
                money = 0;

            if (money <= 0)
                return false;

            // from console show not existed sender
            MailSender sender = new MailSender(MailMessageType.Normal, handler.GetSession() ? handler.GetSession().GetPlayer().GetGUID().GetCounter() : 0, MailStationery.Gm);

            SQLTransaction trans = new SQLTransaction();

            new MailDraft(subject, text)
                .AddMoney((uint)money)
                .SendMailTo(trans, new MailReceiver(receiver, receiverGuid.GetCounter()), sender);

            DB.Characters.CommitTransaction(trans);

            string nameLink = handler.playerLink(receiverName);
            handler.SendSysMessage(CypherStrings.MailSent, nameLink);
            return true;
        }

        [Command("message", RBACPermissions.CommandSendMessage, true)]
        static bool HandleSendMessageCommand(StringArguments args, CommandHandler handler)
        {
            // - Find the player
            Player player;
            if (!handler.extractPlayerTarget(args, out player))
                return false;

            string msgStr = args.NextString("");
            if (string.IsNullOrEmpty(msgStr))
                return false;

            // Check that he is not logging out.
            if (player.GetSession().isLogingOut())
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            // - Send the message
            player.GetSession().SendNotification("{0}", msgStr);
            player.GetSession().SendNotification("|cffff0000[Message from administrator]:|r");

            // Confirmation message
            string nameLink = handler.GetNameLink(player);
            handler.SendSysMessage(CypherStrings.Sendmessage, nameLink, msgStr);

            return true;
        }
    }
}
