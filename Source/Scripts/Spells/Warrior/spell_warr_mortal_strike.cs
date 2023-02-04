// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
namespace Scripts.Spells.Warrior
{
    [Script] // 12294 - Mortal Strike 7.1.5
    internal class spell_warr_mortal_strike : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(WarriorSpells.MORTAL_WOUNDS);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();

            if (target)
                GetCaster().CastSpell(target, WarriorSpells.MORTAL_WOUNDS, true);
        }
    }
}