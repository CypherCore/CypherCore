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
    [SpellScript(23881)] // 23881 - Bloodthirst
    internal class spell_warr_bloodthirst : SpellScript, IHasSpellEffects, IOnCast, IOnHit
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(WarriorSpells.BLOODTHIRST_HEAL);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 3, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
        }

        private void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), WarriorSpells.BLOODTHIRST_HEAL, true);
        }

        public void OnCast()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }

            if (target != ObjectAccessor.Instance.GetUnit(caster, caster.GetTarget()))
            {
                SetHitDamage(GetHitDamage() / 2);
            }

            if (caster.HasAura(WarriorSpells.FRESH_MEAT))
            {
                if (RandomHelper.FRand(0, 15) != 0)
                {
                    caster.CastSpell(null, WarriorSpells.ENRAGE_AURA, true);
                }
            }

            if (caster.HasAura(WarriorSpells.THIRST_FOR_BATTLE))
            {
                caster.AddAura(WarriorSpells.THIRST_FOR_BATTLE_BUFF, caster);
            }
        }

        public void OnHit()
        {
            GetCaster().CastSpell(GetCaster(), WarriorSpells.BLOODTHIRST_HEAL, true);
        }
    }
}