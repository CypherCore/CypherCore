// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    [SpellScript(WarlockSpells.SHADOWBURN)]
    public class spell_warl_shadowburn_SpellScript : SpellScript, ISpellCalcCritChance, ISpellOnHit
    {
        public void CalcCritChance(Unit victim, ref float chance)
        {
            if (victim.TryGetAura(WarlockSpells.SHADOWBURN, out var shadowburn) == true && victim.HealthBelowPct(shadowburn.GetEffect(1).GetBaseAmount() + 5))
				chance += shadowburn.GetEffect(2).GetBaseAmount();
        }

        public void OnHit()
        {
            var caster = GetCaster();
            var target = GetHitUnit();

            if (caster == null || target == null)
                return;

            Eradication(caster, target);
            ConflagrationOfChaos(caster, target);
        }

        private void Eradication(Unit caster, Unit target)
        {
            if (caster.HasAura(WarlockSpells.ERADICATION))
                caster.AddAura(WarlockSpells.ERADICATION_DEBUFF, target);
        }

        private void ConflagrationOfChaos(Unit caster, Unit target)
        {
            caster.RemoveAura(WarlockSpells.CONFLAGRATION_OF_CHAOS_SHADOWBURN);

            if (caster.TryGetAura(WarlockSpells.CONFLAGRATION_OF_CHAOS, out var conflagrate))
            {
                if (RandomHelper.randChance(conflagrate.GetEffect(0).GetBaseAmount()))
                    caster.CastSpell(WarlockSpells.CONFLAGRATION_OF_CHAOS_SHADOWBURN, true);
            }
        }
    }
}