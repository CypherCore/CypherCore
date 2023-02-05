using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 108416 - Dark Pact
    [SpellScript(108416)]
    public class spell_warlock_dark_pact : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
        {
            var sacrifiedHealth = GetCaster().CountPctFromCurHealth(GetSpellInfo().GetEffect(1).BasePoints);
            GetCaster().ModifyHealth((long)sacrifiedHealth * -1);
            amount = (int)MathFunctions.CalculatePct(sacrifiedHealth, GetSpellInfo().GetEffect(2).BasePoints);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
        }
    }
}
