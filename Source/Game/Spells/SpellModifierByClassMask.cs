// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Dynamic;

namespace Game.Spells
{
    public class SpellModifierByClassMask : SpellModifier
    {
        public FlagArray128 Mask { get; set; }

        public int Value { get; set; }

        public SpellModifierByClassMask(Aura _ownerAura) : base(_ownerAura)
        {
            Value = 0;
            Mask = new FlagArray128();
        }
    }
}