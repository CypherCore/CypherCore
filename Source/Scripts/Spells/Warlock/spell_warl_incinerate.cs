using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{

    // Incinerate - 29722
    [SpellScript(29722)]
    public class spell_warl_incinerate : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleOnHitMainTarget(uint UnnamedParameter)
        {
            GetCaster().ModifyPower(PowerType.SoulShards, 20);
        }

        private void HandleOnHitTarget(uint UnnamedParameter)
        {
            Unit target = GetHitUnit();
            if (target != null)
            {
                if (!GetCaster().HasAura(WarlockSpells.FIRE_AND_BRIMSTONE))
                {
                    if (target != GetExplTargetUnit())
                    {
                        PreventHitDamage();
                    }
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHitMainTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }
}
