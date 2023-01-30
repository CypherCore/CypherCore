// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Spells
{
    public class SpellLearnSpellNode
    {
        public bool Active { get; set; }      // show in spellbook or not
        public bool AutoLearned { get; set; } // This marks the spell as automatically learned from another source that - will only be used for unlearning
        public uint OverridesSpell { get; set; }
        public uint Spell { get; set; }
    }
}