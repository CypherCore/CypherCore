// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Chat
{
    public class MessageBuilder
    {
        public virtual dynamic Invoke(Locale locale = Locale.enUS)
        {
            return default;
        }
    }

    public class ChatPacketSender : IDoWork<Player>
    {
        private readonly uint AchievementId;
        private readonly Language Language;
        private readonly Locale Locale;
        private readonly WorldObject Receiver;
        private readonly WorldObject Sender;
        private readonly string Text;
        public ChatPkt TranslatedPacket;
        private readonly ChatMsg Type;

        // caches
        public ChatPkt UntranslatedPacket;

        public ChatPacketSender(ChatMsg chatType, Language language, WorldObject sender, WorldObject receiver, string message, uint achievementId = 0, Locale locale = Locale.enUS)
        {
            Type = chatType;
            Language = language;
            Sender = sender;
            Receiver = receiver;
            Text = message;
            AchievementId = achievementId;
            Locale = locale;

            UntranslatedPacket = new ChatPkt();
            UntranslatedPacket.Initialize(Type, Language, Sender, Receiver, Text, AchievementId, "", Locale);
            UntranslatedPacket.Write();
        }

        public void Invoke(Player player)
        {
            if (Language == Language.Universal ||
                Language == Language.Addon ||
                Language == Language.AddonLogged ||
                player.CanUnderstandLanguage(Language))
            {
                player.SendPacket(UntranslatedPacket);

                return;
            }

            if (TranslatedPacket == null)
            {
                TranslatedPacket = new ChatPkt();
                TranslatedPacket.Initialize(Type, Language, Sender, Receiver, Global.LanguageMgr.Translate(Text, (uint)Language, player.GetSession().GetSessionDbcLocale()), AchievementId, "", Locale);
                TranslatedPacket.Write();
            }

            player.SendPacket(TranslatedPacket);
        }
    }

    public class BroadcastTextBuilder : MessageBuilder
    {
        private readonly uint _achievementId;
        private readonly Gender _gender;
        private readonly ChatMsg _msgType;

        private readonly WorldObject _source;
        private readonly WorldObject _target;
        private readonly uint _textId;

        public BroadcastTextBuilder(WorldObject obj, ChatMsg msgtype, uint textId, Gender gender, WorldObject target = null, uint achievementId = 0)
        {
            _source = obj;
            _msgType = msgtype;
            _textId = textId;
            _gender = gender;
            _target = target;
            _achievementId = achievementId;
        }

        public override ChatPacketSender Invoke(Locale locale = Locale.enUS)
        {
            BroadcastTextRecord bct = CliDB.BroadcastTextStorage.LookupByKey(_textId);

            return new ChatPacketSender(_msgType, bct != null ? (Language)bct.LanguageID : Language.Universal, _source, _target, bct != null ? Global.DB2Mgr.GetBroadcastTextValue(bct, locale, _gender) : "", _achievementId, locale);
        }
    }

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