// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
namespace Scripts.Spells.Warrior
{
    [Script] // 107570 - Storm Bolt
    internal class spell_warr_storm_bolt : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(WarriorSpells.STORM_BOLT_STUN);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleOnHit(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), WarriorSpells.STORM_BOLT_STUN, true);
        }
    }
}