// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;

namespace Game.Chat
{
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
}