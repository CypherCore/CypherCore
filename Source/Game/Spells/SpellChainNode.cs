// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Spells
{
    public class SpellChainNode
    {
        public SpellInfo First { get; set; }
        public SpellInfo Last { get; set; }
        public SpellInfo Next { get; set; }
        public SpellInfo Prev { get; set; }
        public byte Rank { get; set; }
    }
}