// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Entities
{
    public class SpellEnchantProcEntry
    {
        public EnchantProcAttributes AttributesMask { get; set; } // bitmask, see EnchantProcAttributes
        public float Chance { get; set; }                         // if nonzero - overwrite SpellItemEnchantment value
        public uint HitMask { get; set; }                         // if nonzero - bitmask for matching proc condition based on hit result, see enum ProcFlagsHit
        public float ProcsPerMinute { get; set; }                 // if nonzero - chance to proc is equal to value * aura caster's weapon speed / 60
    }
}