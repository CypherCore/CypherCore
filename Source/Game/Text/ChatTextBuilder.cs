// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;

namespace Game.Chat
{
    public class MessageBuilder
    {
        public virtual dynamic Invoke(Locale locale = Locale.enUS) { return default; }
    }

    public class ChatPacketSender
    {
        ChatMsg Type;
        Language Language;
        WorldObject Sender;
        WorldObject Receiver;
        string Text;
        uint AchievementId;
        Locale Locale;
        uint PlayerConditionID;

        // caches
        public ChatPkt UntranslatedPacket;
        public ChatPkt TranslatedPacket;
        public EmoteMessage EmotePacket;
        public ServerPacket SoundPacket;

        public ChatPacketSender(ChatMsg chatType, Language language, WorldObject sender, WorldObject receiver, string message, uint achievementId = 0, Locale locale = Locale.enUS,
            uint broadcastTextId = 0, ushort emoteId = 0, uint soundKitId = 0, SoundKitPlayType soundKitPlayType = SoundKitPlayType.Normal, uint playerConditionId = 0)
        {
            Type = chatType;
            Language = language;
            Sender = sender;
            Receiver = receiver;
            Text = message;
            AchievementId = achievementId;
            Locale = locale;
            PlayerConditionID = playerConditionId;

            UntranslatedPacket = new();
            UntranslatedPacket.Initialize(Type, Language, Sender, Receiver, Text, AchievementId, "", Locale);
            UntranslatedPacket.Write();

            if (sender != null && sender.IsUnit() && emoteId != 0)
            {
                EmotePacket = new();
                EmotePacket.Guid = sender.GetGUID();
                EmotePacket.EmoteID = emoteId;
                EmotePacket.Write();
            }

            SoundPacket = null;
            if (soundKitId != 0)
            {
                if (soundKitPlayType == SoundKitPlayType.Normal)
                {
                    SoundPacket = new PlaySound(sender != null ? sender.GetGUID() : ObjectGuid.Empty, soundKitId, broadcastTextId);
                }
                else if (soundKitPlayType == SoundKitPlayType.ObjectSound)
                {
                    SoundPacket = new PlayObjectSound(receiver != null ? receiver.GetGUID() : ObjectGuid.Empty, sender != null ? sender.GetGUID() : ObjectGuid.Empty, soundKitId, receiver != null ? receiver.GetWorldLocation() : new Position(0, 0, 0), (int)broadcastTextId);
                }
                SoundPacket.Write();
            }
        }

        public static implicit operator IDoWork<Player>(ChatPacketSender obj) => obj.Invoke;

        public void Invoke(Player player)
        {
            if (!player.MeetPlayerCondition(PlayerConditionID))
                return;

            if (SoundPacket != null)
                player.SendPacket(SoundPacket);

            if (EmotePacket != null)
                player.SendPacket(EmotePacket);

            if (Language == Language.Universal || Language == Language.Addon || Language == Language.AddonLogged || player.CanUnderstandLanguage(Language))
            {
                player.SendPacket(UntranslatedPacket);
                return;
            }

            if (TranslatedPacket == null)
            {
                TranslatedPacket = new();
                TranslatedPacket.Initialize(Type, Language, Sender, Receiver, Global.LanguageMgr.Translate(Text, (uint)Language, player.GetSession().GetSessionDbcLocale()), AchievementId, "", Locale);
                TranslatedPacket.Write();
            }

            player.SendPacket(TranslatedPacket);
        }
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

        public override ChatPacketSender Invoke(Locale locale = Locale.enUS)
        {
            BroadcastTextRecord bct = CliDB.BroadcastTextStorage.LookupByKey(_textId);
            Unit unitSender = _source?.ToUnit();
            Gender gender = unitSender != null ? unitSender.GetGender() : Gender.Unknown;
            uint soundKitId = bct != null ? bct.SoundKitID[gender == Gender.Female ? 1 : 0] : 0;

            return new ChatPacketSender(_msgType,
                bct != null ? (Language)bct.LanguageID : Language.Universal,
                _source,
                _target,
                bct != null ? Global.DB2Mgr.GetBroadcastTextValue(bct, locale, _gender) : "",
                _achievementId,
                locale,
                bct != null ? bct.Id : 0,
                bct != null ? bct.EmotesID : (ushort)0,
                soundKitId,
                SoundKitPlayType.Normal,
                bct != null ? (uint)bct.ConditionID : 0
            );
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

        public override ChatPacketSender Invoke(Locale locale)
        {
            return new ChatPacketSender(_msgType, _language, _source, _target, _text, 0, locale);
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

        public override ChatPacketSender Invoke(Locale locale)
        {
            string text = Global.ObjectMgr.GetCypherString(_textId, locale);

            if (_args != null)
                return new ChatPacketSender(_msgType, Language.Universal, _source, _target, string.Format(text, _args), 0, locale);
            else
                return new ChatPacketSender(_msgType, Language.Universal, _source, _target, text, 0, locale);
        }

        WorldObject _source;
        ChatMsg _msgType;
        CypherStrings _textId;
        WorldObject _target;
        object[] _args;
    }
}
