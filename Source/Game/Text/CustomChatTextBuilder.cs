// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Chat
{

    public class CustomChatTextBuilder : MessageBuilder
    {
        private readonly Language _language;
        private readonly ChatMsg _msgType;

        private readonly WorldObject _source;
        private readonly WorldObject _target;
        private readonly string _text;

        public CustomChatTextBuilder(WorldObject obj, ChatMsg msgType, string text, Language language = Language.Universal, WorldObject target = null)
        {
            _source = obj;
            _msgType = msgType;
            _text = text;
            _language = language;
            _target = target;
        }

        public override ChatPacketSender Invoke(Locale locale)
        {
            return new ChatPacketSender(_msgType, _language, _source, _target, _text, 0, locale);
        }
    }
}