using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
    // 95738 - Bladestorm Offhand
    [SpellScript(95738)]
    public class spell_warr_bladestorm_offhand : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleOnHit(uint effIndex)
        {
            Player caster = GetCaster().ToPlayer();
            if (caster == null)
            {
                return;
            }

            var _spec = caster.GetPrimarySpecialization();
            if (_spec != TalentSpecialization.WarriorFury) //only fury warriors should deal damage with offhand
            {
                PreventHitDamage();
                PreventHitDefaultEffect(effIndex);
                PreventHitEffect(effIndex);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.WeaponPercentDamage, SpellScriptHookType.EffectHitTarget));
        }
    }
}
