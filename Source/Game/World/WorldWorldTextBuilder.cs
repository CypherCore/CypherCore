// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Collections;
using Framework.Constants;
using Game.Chat;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public class WorldWorldTextBuilder : MessageBuilder
    {
        public class MultiplePacketSender : IDoWork<Player>
        {
            public List<ServerPacket> Packets = new();

            public void Invoke(Player receiver)
            {
                foreach (var packet in Packets)
                    receiver.SendPacket(packet);
            }
        }

        private readonly object[] _args;
        private readonly uint _textId;

        public WorldWorldTextBuilder(uint textId, params object[] args)
        {
            _textId = textId;
            _args = args;
        }

        public override MultiplePacketSender Invoke(Locale locale)
        {
            string text = Global.ObjectMgr.GetCypherString(_textId, locale);

            if (_args != null)
                text = string.Format(text, _args);

            MultiplePacketSender sender = new();

            var lines = new StringArray(text, "\n");

            for (var i = 0; i < lines.Length; ++i)
            {
                ChatPkt messageChat = new();
                messageChat.Initialize(ChatMsg.System, Language.Universal, null, null, lines[i]);
                messageChat.Write();
                sender.Packets.Add(messageChat);
            }

            return sender;
        }
    }
}