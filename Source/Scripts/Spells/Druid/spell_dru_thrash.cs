// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 77758 - Thrash
    internal class spell_dru_thrash : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(DruidSpellIds.ThrashBearAura);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleOnHitTarget(uint effIndex)
        {
            Unit hitUnit = GetHitUnit();

            if (hitUnit != null)
            {
                Unit caster = GetCaster();

                caster.CastSpell(hitUnit, DruidSpellIds.ThrashBearAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
            }
        }
    }
}