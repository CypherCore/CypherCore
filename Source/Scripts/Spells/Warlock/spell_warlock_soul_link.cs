using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 108446 - Soul Link
    [SpellScript(108446)]
    public class spell_warlock_soul_link : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void HandleSplit(AuraEffect UnnamedParameter, DamageInfo UnnamedParameter2, ref uint splitAmount)
        {
            Unit pet = GetUnitOwner();
            if (pet == null)
            {
                return;
            }

            Unit owner = pet.GetOwner();
            if (owner == null)
            {
                return;
            }

            if (owner.HasAura(WarlockSpells.SOUL_SKIN) && owner.HealthBelowPct(35))
            {
                splitAmount *= 2;
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectSplitHandler(HandleSplit, 0));
        }
    }
}
