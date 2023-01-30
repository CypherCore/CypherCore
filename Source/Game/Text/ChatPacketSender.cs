// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Chat
{
    public class ChatPacketSender : IDoWork<Player>
    {
        public ChatPkt TranslatedPacket { get; set; }

        // caches
        public ChatPkt UntranslatedPacket;
        private readonly uint _achievementId;
        private readonly Language _language;
        private readonly Locale _locale;
        private readonly WorldObject _receiver;
        private readonly WorldObject _sender;
        private readonly string _text;
        private readonly ChatMsg _type;

        public ChatPacketSender(ChatMsg chatType, Language language, WorldObject sender, WorldObject receiver, string message, uint achievementId = 0, Locale locale = Locale.enUS)
        {
            _type = chatType;
            _language = language;
            _sender = sender;
            _receiver = receiver;
            _text = message;
            _achievementId = achievementId;
            _locale = locale;

            UntranslatedPacket = new ChatPkt();
            UntranslatedPacket.Initialize(_type, _language, _sender, _receiver, _text, _achievementId, "", _locale);
            UntranslatedPacket.Write();
        }

        public void Invoke(Player player)
        {
            if (_language == Language.Universal ||
                _language == Language.Addon ||
                _language == Language.AddonLogged ||
                player.CanUnderstandLanguage(_language))
            {
                player.SendPacket(UntranslatedPacket);

                return;
            }

            if (TranslatedPacket == null)
            {
                TranslatedPacket = new ChatPkt();
                TranslatedPacket.Initialize(_type, _language, _sender, _receiver, Global.LanguageMgr.Translate(_text, (uint)_language, player.GetSession().GetSessionDbcLocale()), _achievementId, "", _locale);
                TranslatedPacket.Write();
            }

            player.SendPacket(TranslatedPacket);
        }
    }
}