using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    // 5697 - Unending Breath
    [SpellScript(5697)]
    internal class spell_warlock_unending_breath : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleHit(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target != null)
            {
                if (caster.HasAura(WarlockSpells.SOULBURN))
                {
                    caster.CastSpell(target, WarlockSpells.SOULBURN_UNENDING_BREATH, true);
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.ApplyAura, SpellScriptHookType.LaunchTarget));
        }
    }
}
