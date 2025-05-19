// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Spells;

namespace Game.AI
{
    public class ConversationAI
    {
        uint _scriptId;
        public Conversation conversation;

        public ConversationAI(Conversation c, uint scriptId = 0)
        {
            _scriptId = scriptId != 0 ? scriptId : c.GetScriptId();
            conversation = c;

            Cypher.Assert(_scriptId != 0, "A ConversationAI was initialized with an invalid scriptId!");
        }

        // Called when the Conversation has just been initialized, just before added to map
        public virtual void OnInitialize() { }

        // Called when Conversation is created but not added to Map yet.
        public virtual void OnCreate(Unit creator) { }

        // Called when Conversation is started
        public virtual void OnStart() { }

        // Called when player sends CMSG_CONVERSATION_LINE_STARTED with valid conversation guid
        public virtual void OnLineStarted(uint lineId, Player sender) { }

        // Called for each update tick
        public virtual void OnUpdate(uint diff) { }

        // Called when the Conversation is removed
        public virtual void OnRemove() { }

        // Pass parameters between AI
        public virtual void DoAction(int param) { }
        public virtual uint GetData(uint id = 0) { return 0; }
        public virtual void SetData(uint id, uint value) { }
        public virtual void SetGUID(ObjectGuid guid, int id = 0) { }
        public virtual ObjectGuid GetGUID(int id = 0) { return ObjectGuid.Empty; }

        // Gets the id of the AI (script id)
        public uint GetId() { return _scriptId; }
    }

    class NullConversationAI : ConversationAI
    {
        public NullConversationAI(Conversation c, uint scriptId) : base(c, scriptId) { }
    }
}
