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
using Game.DataStorage;
using Game.Entities;
using Game.Network;
using Game.Network.Packets;
using System.Collections.Generic;

namespace Game.Chat
{
    public class MessageBuilder
    {
        public virtual ServerPacket Invoke(LocaleConstant locale = LocaleConstant.enUS) { return null; }
        public virtual void Invoke(List<ServerPacket> data, LocaleConstant locale = LocaleConstant.enUS) { }
    }

    public class BroadcastTextBuilder : MessageBuilder
    {
        public BroadcastTextBuilder(WorldObject obj, ChatMsg msgtype, uint textId, Gender gender, WorldObject target = null, uint achievementId = 0)
        {
            _source = obj;
            _msgType = msgtype;
            _textId = textId;
            _gender = gender;
            _target = target;
            _achievementId = achievementId;
        }

        public override ServerPacket Invoke(LocaleConstant locale)
        {
            BroadcastTextRecord bct = CliDB.BroadcastTextStorage.LookupByKey(_textId);
            var packet = new ChatPkt();
            packet.Initialize(_msgType, bct != null ? (Language)bct.LanguageID : Language.Universal, _source, _target, bct != null ? Global.DB2Mgr.GetBroadcastTextValue(bct, locale, _gender) : "", _achievementId, "", locale);
            return packet;
        }

        WorldObject _source;
        ChatMsg _msgType;
        uint _textId;
        Gender _gender;
        WorldObject _target;
        uint _achievementId;
    }

    public class CustomChatTextBuilder : MessageBuilder
    {
        public CustomChatTextBuilder(WorldObject obj, ChatMsg msgType, string text, Language language = Language.Universal, WorldObject target = null)
        {
            _source = obj;
            _msgType = msgType;
            _text = text;
            _language = language;
            _target = target;
        }

        public override ServerPacket Invoke(LocaleConstant locale)
        {
            var packet = new ChatPkt();
            packet.Initialize(_msgType, _language, _source, _target, _text, 0, "", locale);
            return packet;
        }

        WorldObject _source;
        ChatMsg _msgType;
        string _text;
        Language _language;
        WorldObject _target;
    }

    class CypherStringChatBuilder : MessageBuilder
    {
        public CypherStringChatBuilder(WorldObject obj, ChatMsg msgType, CypherStrings textId, WorldObject target = null, object[] args = null)
        {
            _source = obj;
            _msgType = msgType;
            _textId = textId;
            _target = target;
            _args = args;
        }

        public override ServerPacket Invoke(LocaleConstant locale)
        {
            ChatPkt packet = new ChatPkt();

            string text = Global.ObjectMgr.GetCypherString(_textId, locale);

            if (_args != null)
                packet.Initialize(_msgType, Language.Universal, _source, _target, string.Format(text, _args), 0, "", locale);
            else
                packet.Initialize(_msgType, Language.Universal, _source, _target, text, 0, "", locale);

            return packet;
        }

        WorldObject _source;
        ChatMsg _msgType;
        CypherStrings _textId;
        WorldObject _target;
        object[] _args;
    }
}
