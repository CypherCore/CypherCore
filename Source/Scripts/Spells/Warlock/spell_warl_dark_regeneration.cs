using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(108359)]
    public class spell_warl_dark_regeneration : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            if (GetTarget())
            {
                Guardian pet = GetTarget().GetGuardianPet();
                if (pet != null)
                {
                    pet.CastSpell(pet, WarlockSpells.DARK_REGENERATION, true);
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(HandleApply, 0, AuraType.ObsModHealth, AuraEffectHandleModes.Real));
        }
    }
}
