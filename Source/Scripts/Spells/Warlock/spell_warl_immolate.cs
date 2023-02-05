// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(348)] // 348 - Immolate
    internal class spell_warl_immolate : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(WarlockSpells.IMMOLATE_DOT);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnEffectHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleOnEffectHit(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), WarlockSpells.IMMOLATE_DOT, GetSpell());
        }
    }
}