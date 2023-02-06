using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    // 3110 - Firebolt
    [SpellScript(3110)]
    public class spell_warlock_imp_firebolt : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || !caster.GetOwner() || target == null)
            {
                return;
            }

            Unit owner = caster.GetOwner();
            int damage = GetHitDamage();
            if (target.HasAura(WarlockSpells.IMMOLATE_DOT, owner.GetGUID()))
            {
                MathFunctions.AddPct(ref damage, owner.GetAuraEffectAmount(WarlockSpells.FIREBOLT_BONUS, 0));
            }

            SetHitDamage(damage);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }
}
