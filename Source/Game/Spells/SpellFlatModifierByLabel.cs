// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Spells
{
    public class SpellFlatModifierByLabel : SpellModifier
    {
        public SpellFlatModByLabel Value { get; set; } = new();

        public SpellFlatModifierByLabel(Aura _ownerAura) : base(_ownerAura)
        {
        }
    }
}