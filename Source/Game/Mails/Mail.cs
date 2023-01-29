// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.BlackMarket;
using Game.Entities;

namespace Game.Mails
{
    public class Mail
    {
        public string body;
        public MailCheckMask checkMask;
        public ulong COD;
        public long deliver_time;
        public long expire_time;
        public List<MailItemInfo> items = new();
        public uint mailTemplateId;

        public uint messageID;
        public MailMessageType messageType;
        public ulong money;
        public ulong receiver;
        public List<uint> removedItems = new();
        public ulong sender;
        public MailState state;
        public MailStationery stationery;
        public string subject;

        public void AddItem(ulong itemGuidLow, uint item_template)
        {
            MailItemInfo mii = new();
            mii.item_guid = itemGuidLow;
            mii.item_template = item_template;
            items.Add(mii);
        }

        public bool RemoveItem(uint item_guid)
        {
            foreach (var item in items)
                if (item.item_guid == item_guid)
                {
                    items.Remove(item);

                    return true;
                }

            return false;
        }

        public bool HasItems()
        {
            return !items.Empty();
        }
    }

    public class MailItemInfo
    {
        public ulong item_guid;
        public uint item_template;
    }

    public class MailReceiver
    {
        private readonly Player _receiver;
        private readonly ulong _receiver_lowguid;

        public MailReceiver(ulong receiver_lowguid)
        {
            _receiver = null;
            _receiver_lowguid = receiver_lowguid;
        }

        public MailReceiver(Player receiver)
        {
            _receiver = receiver;
            _receiver_lowguid = receiver.GetGUID().GetCounter();
        }

        public MailReceiver(Player receiver, ulong receiver_lowguid)
        {
            _receiver = receiver;
            _receiver_lowguid = receiver_lowguid;

            Cypher.Assert(!receiver || receiver.GetGUID().GetCounter() == receiver_lowguid);
        }

        public MailReceiver(Player receiver, ObjectGuid receiverGuid)
        {
            _receiver = receiver;
            _receiver_lowguid = receiverGuid.GetCounter();

            Cypher.Assert(!receiver || receiver.GetGUID() == receiverGuid);
        }

        public Player GetPlayer()
        {
            return _receiver;
        }

        public ulong GetPlayerGUIDLow()
        {
            return _receiver_lowguid;
        }
    }

    public class MailSender
    {
        private readonly MailMessageType _messageType;
        private readonly ulong _senderId; // player low Guid or other object entry
        private readonly MailStationery _stationery;

        public MailSender(MailMessageType messageType, ulong sender_guidlow_or_entry, MailStationery stationery = MailStationery.Default)
        {
            _messageType = messageType;
            _senderId = sender_guidlow_or_entry;
            _stationery = stationery;
        }

        public MailSender(WorldObject sender, MailStationery stationery = MailStationery.Default)
        {
            _stationery = stationery;

            switch (sender.GetTypeId())
            {
                case TypeId.Unit:
                    _messageType = MailMessageType.Creature;
                    _senderId = sender.GetEntry();

                    break;
                case TypeId.GameObject:
                    _messageType = MailMessageType.Gameobject;
                    _senderId = sender.GetEntry();

                    break;
                case TypeId.Player:
                    _messageType = MailMessageType.Normal;
                    _senderId = sender.GetGUID().GetCounter();

                    break;
                default:
                    _messageType = MailMessageType.Normal;
                    _senderId = 0; // will show mail from not existed player
                    Log.outError(LogFilter.Server, "MailSender:MailSender - Mail have unexpected sender typeid ({0})", sender.GetTypeId());

                    break;
            }
        }

        public MailSender(CalendarEvent sender)
        {
            _messageType = MailMessageType.Calendar;
            _senderId = (uint)sender.EventId;
            _stationery = MailStationery.Default;
        }

        public MailSender(AuctionHouseObject sender)
        {
            _messageType = MailMessageType.Auction;
            _senderId = sender.GetAuctionHouseId();
            _stationery = MailStationery.Auction;
        }

        public MailSender(BlackMarketEntry sender)
        {
            _messageType = MailMessageType.Blackmarket;
            _senderId = sender.GetTemplate().SellerNPC;
            _stationery = MailStationery.Auction;
        }

        public MailSender(Player sender)
        {
            _messageType = MailMessageType.Normal;
            _stationery = sender.IsGameMaster() ? MailStationery.Gm : MailStationery.Default;
            _senderId = sender.GetGUID().GetCounter();
        }

        public MailSender(uint senderEntry)
        {
            _messageType = MailMessageType.Creature;
            _senderId = senderEntry;
            _stationery = MailStationery.Default;
        }

        public MailMessageType GetMailMessageType()
        {
            return _messageType;
        }

        public ulong GetSenderId()
        {
            return _senderId;
        }

        public MailStationery GetStationery()
        {
            return _stationery;
        }
    }
}