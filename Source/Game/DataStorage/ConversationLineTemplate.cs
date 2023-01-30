// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.DataStorage
{
    public class ConversationLineTemplate
    {
        public byte ActorIdx { get; set; } // Index from conversation_actors
        public byte Flags { get; set; }
        public uint Id { get; set; }         // Link to ConversationLine.db2
        public uint UiCameraID { get; set; } // Link to UiCamera.db2
    }
}