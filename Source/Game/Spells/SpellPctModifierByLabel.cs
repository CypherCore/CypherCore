// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Spells
{
    internal class SpellPctModifierByLabel : SpellModifier
    {
        public SpellPctModByLabel Value { get; set; } = new();

        public SpellPctModifierByLabel(Aura _ownerAura) : base(_ownerAura)
        {
        }
    }
}