// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Chat
{
    internal class CypherStringChatBuilder : MessageBuilder
    {
        private readonly object[] _args;
        private readonly ChatMsg _msgType;

        private readonly WorldObject _source;
        private readonly WorldObject _target;
        private readonly CypherStrings _textId;

        public CypherStringChatBuilder(WorldObject obj, ChatMsg msgType, CypherStrings textId, WorldObject target = null, object[] args = null)
        {
            _source = obj;
            _msgType = msgType;
            _textId = textId;
            _target = target;
            _args = args;
        }

        public override ChatPacketSender Invoke(Locale locale)
        {
            string text = Global.ObjectMgr.GetCypherString(_textId, locale);

            if (_args != null)
                return new ChatPacketSender(_msgType, Language.Universal, _source, _target, string.Format(text, _args), 0, locale);
            else
                return new ChatPacketSender(_msgType, Language.Universal, _source, _target, text, 0, locale);
        }
    }
}