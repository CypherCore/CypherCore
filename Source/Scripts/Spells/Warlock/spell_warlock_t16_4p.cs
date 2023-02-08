using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 145091 - Item - Warlock T16 4P Bonus
    [SpellScript(145091)]
    public class spell_warlock_t16_4p : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void PeriodicTick(AuraEffect aurEffConst)
        {
            // "When a Burning Ember fills up, your critical strike chance is increased by 15% for 5 seconds"
            Player caster = GetUnitOwner().ToPlayer();
            if (caster == null || caster.HasAura(WarlockSpells.T16_4P_INTERNAL_CD))
            {
                return;
            }
            // allow only in Destruction
            if (caster.GetPrimarySpecialization() != TalentSpecialization.WarlockDestruction)
            {
                return;
            }


            var currentPower = caster.GetPower(PowerType.BurningEmbers) / 10;
            var oldPower = aurEffConst.GetAmount();
            if (currentPower > oldPower)
            {
                caster.CastSpell(caster, WarlockSpells.T16_4P_TRIGGERED, true);
                caster.CastSpell(caster, WarlockSpells.T16_4P_INTERNAL_CD, true);
            }
            aurEffConst.SetAmount(currentPower);
        }

        private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
        {
            if (eventInfo.GetDamageInfo() != null)
            {
                return;
            }

            Unit caster = GetUnitOwner();
            Unit victim = eventInfo.GetDamageInfo().GetVictim();
            if (caster == null || victim == null)
            {
                return;
            }
            // "Shadow Bolt and Touch of Chaos have a 8% chance to also cast Hand of Gul'dan at the target"
            caster.CastSpell(victim, WarlockSpells.HAND_OF_GULDAN, true);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 1, AuraType.PeriodicDummy));
            AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }
    }
}
