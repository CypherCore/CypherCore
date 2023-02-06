using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // Burning Rush - 111400
    [SpellScript(111400)]
    public class aura_warl_burning_rush : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnTick(AuraEffect UnnamedParameter)
        {
            if (GetCaster())
            {
                // This way if the current tick takes you below 4%, next tick won't execute
                ulong basepoints = GetCaster().CountPctFromMaxHealth(4);
                if (GetCaster().GetHealth() <= basepoints || GetCaster().GetHealth() - basepoints <= basepoints)
                {
                    GetAura().SetDuration(0);
                }
            }
        }
        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(OnTick, 1, AuraType.PeriodicDamagePercent));
        }
    }

}
