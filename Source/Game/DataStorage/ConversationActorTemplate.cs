// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.DataStorage
{
    public struct ConversationActorTemplate
    {
        public int Id;
        public uint Index;
        public ConversationActorWorldObjectTemplate WorldObjectTemplate;
        public ConversationActorNoObjectTemplate NoObjectTemplate;
        public ConversationActorActivePlayerTemplate ActivePlayerTemplate;
        public ConversationActorTalkingHeadTemplate TalkingHeadTemplate;
    }
}