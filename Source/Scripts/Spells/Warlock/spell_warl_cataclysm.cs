using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    // Cataclysm - 152108
    [SpellScript(152108)]
    internal class spell_warl_cataclysm : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }
            if (!caster.ToPlayer())
            {
                return;
            }

            if (GetCaster().ToPlayer().GetPrimarySpecialization() == TalentSpecialization.WarlockDestruction)
            {
                caster.CastSpell(target, WarlockSpells.IMMOLATE_DOT, true);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }
}
