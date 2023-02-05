using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    //219272 - Demon Skin
    [SpellScript(219272)]
    public class spell_warl_demon_skin : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void PeriodicTick(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            float absorb = (aurEff.GetAmount() / 10.0f) * caster.GetMaxHealth() / 100.0f;

            // Add remaining amount if already applied
            AuraEffect soulLeechShield = caster.GetAuraEffect(WarlockSpells.SOUL_LEECH_SHIELD, 0);
            if (soulLeechShield != null)
            {
                absorb += soulLeechShield.GetAmount();
            }
          
            MathFunctions.AddPct(ref absorb, caster.GetAuraEffectAmount(WarlockSpells.ARENA_DAMPENING, 0));

            float threshold = caster.CountPctFromMaxHealth(GetEffect(1).GetAmount());
            absorb = Math.Min(absorb, threshold);

            if (soulLeechShield != null)
            {
                soulLeechShield.SetAmount((int)absorb);
            }
            else
            {
                caster.CastSpell(caster, WarlockSpells.SOUL_LEECH_SHIELD, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorb));
            }
        }

        private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            Aura aur = caster.GetAura(WarlockSpells.SOUL_LEECH_SHIELD);
            if (aur != null)
            {
                aur.SetMaxDuration(15000);
                aur.RefreshDuration();
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
            AuraEffects.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDummy));
        }
    }
}
