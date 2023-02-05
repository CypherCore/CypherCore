using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 234153 - Drain Life
    [SpellScript(234153)]
    public class spell_warlock_drain_life : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void PeriodicTick(AuraEffect UnnamedParameter)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicLeech));
        }
    }
}
