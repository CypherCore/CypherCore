using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 221711 - Essence Drain
    // Called by Drain Soul (198590) and Drain Life (234153)
    [SpellScript(221711)]
    public class spell_warlock_essence_drain : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void PeriodicTick(AuraEffect UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetUnitOwner();
            if (caster == null || target == null)
            {
                return;
            }

            if (caster.HasAura(WarlockSpells.ESSENCE_DRAIN))
            {
                caster.CastSpell(target, WarlockSpells.ESSENCE_DRAIN_DEBUFF, true);
            }

            int durationBonus = caster.GetAuraEffectAmount(WarlockSpells.ROT_AND_DECAY, 0);
            if (durationBonus != 0)
            {
                List<uint> dots = new List<uint>() { (uint)WarlockSpells.AGONY, (uint)WarlockSpells.CORRUPTION_TRIGGERED, (uint)WarlockSpells.UNSTABLE_AFFLICTION_DOT1, (uint)WarlockSpells.UNSTABLE_AFFLICTION_DOT2, (uint)WarlockSpells.UNSTABLE_AFFLICTION_DOT3, (uint)WarlockSpells.UNSTABLE_AFFLICTION_DOT4, (uint)WarlockSpells.UNSTABLE_AFFLICTION_DOT5 };

                foreach (uint dot in dots)
                {
                    Aura aur = target.GetAura(dot, caster.GetGUID());
                    if (aur != null)
                    {
                        aur.ModDuration(durationBonus);
                    }
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectPeriodicHandler(PeriodicTick, 0, AuraType.Dummy));
        }
    }
}
