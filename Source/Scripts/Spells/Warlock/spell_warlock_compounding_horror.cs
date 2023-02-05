using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 231489 - Compounding Horror
    [SpellScript(231489)]
    internal class spell_warlock_compounding_horror : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            int damage = GetHitDamage();
            int stacks = 0;
            Aura aur = caster.GetAura(WarlockSpells.COMPOUNDING_HORROR);
            if (aur != null)
            {
                stacks = aur.GetStackAmount();
            }

            SetHitDamage(damage * stacks);

            caster.RemoveAurasDueToSpell(WarlockSpells.COMPOUNDING_HORROR);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }
}
