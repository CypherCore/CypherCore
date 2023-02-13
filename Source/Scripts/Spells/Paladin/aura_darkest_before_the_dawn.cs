using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    //210378
    [SpellScript(210378)]
    public class aura_darkest_before_the_dawn : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void OnTick(AuraEffect UnnamedParameter)
        {
            var caster = GetCaster();

            if (caster == null)
                return;

            Aura dawnTrigger = caster.GetAura(PaladinSpells.DARKEST_BEFORE_THE_DAWN);

            if (dawnTrigger != null)
                caster.AddAura(PaladinSpells.DARKEST_BEFORE_THE_DAWN_BUFF, caster);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDummy));
        }
    }
}
