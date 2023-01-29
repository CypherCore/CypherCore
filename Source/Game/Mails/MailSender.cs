// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.BlackMarket;
using Game.Entities;

namespace Game.Mails
{
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