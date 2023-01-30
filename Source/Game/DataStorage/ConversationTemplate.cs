// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.DataStorage
{
    public class ConversationTemplate
    {
        public List<ConversationActorTemplate> Actors { get; set; } = new();
        public uint FirstLineId { get; set; } // Link to ConversationLine.db2
        public uint Id { get; set; }
        public List<ConversationLineTemplate> Lines { get; set; } = new();
        public uint ScriptId { get; set; }
        public uint TextureKitId { get; set; } // Background texture
    }
}