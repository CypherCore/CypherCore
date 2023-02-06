using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    // 63106 - Siphon Life @ Glyph of Siphon Life
    [SpellScript(63106)]
    public class spell_warlock_siphon_life : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleHit(uint effIndex)
        {
            Unit caster = GetCaster();
            uint heal = caster.SpellHealingBonusDone(caster, GetSpellInfo(), (uint)caster.CountPctFromMaxHealth(GetSpellInfo().GetEffect(effIndex).BasePoints), DamageEffectType.Heal, GetEffectInfo());
            heal /= 100; // 0.5%
            heal = caster.SpellHealingBonusTaken(caster, GetSpellInfo(), heal, DamageEffectType.Heal);
            SetHitHeal((int)heal);
            PreventHitDefaultEffect(effIndex);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
        }
    }
}
