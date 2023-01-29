// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Mails
{
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
}