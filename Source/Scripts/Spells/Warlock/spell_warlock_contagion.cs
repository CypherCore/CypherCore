using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 233494 - Contagion
    [SpellScript(233494)]
    public class spell_warlock_contagion : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void PeriodicTick(AuraEffect UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetTarget();
            if (caster == null || target == null)
            {
                return;
            }

            List<uint> uaspells = new List<uint>() { WarlockSpells.UNSTABLE_AFFLICTION_DOT5, WarlockSpells.UNSTABLE_AFFLICTION_DOT4, WarlockSpells.UNSTABLE_AFFLICTION_DOT3, WarlockSpells.UNSTABLE_AFFLICTION_DOT2, WarlockSpells.UNSTABLE_AFFLICTION_DOT1 };

            bool hasUa = false;
            foreach (uint ua in uaspells)
            {
                if (target.HasAura(ua, caster.GetGUID()))
                {
                    hasUa = true;
                }
            }

            if (!hasUa)
            {
                Remove();
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.ModSchoolMaskDamageFromCaster));
        }
    }
}
