// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Spells
{
    // Spell modifier (used for modify other spells)
    public class SpellModifier
    {
        public SpellModifier(Aura _ownerAura)
        {
            Op = SpellModOp.HealingAndDamage;
            Type = SpellModType.Flat;
            SpellId = 0;
            OwnerAura = _ownerAura;
        }

        public SpellModOp Op { get; set; }
        public SpellModType Type { get; set; }
        public uint SpellId { get; set; }
        public Aura OwnerAura { get; set; }
    }
}