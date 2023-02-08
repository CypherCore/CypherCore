using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(185358)]
public class spell_hun_arcane_shot : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();
		if (caster == null || target == null)
		{
			return;
		}

		if (caster.HasAura(HunterSpells.SPELL_HUNTER_MARKING_TARGETS))
		{
			caster.CastSpell(target, HunterSpells.SPELL_HUNTER_HUNTERS_MARK_AURA, true);
			caster.CastSpell(caster, HunterSpells.SPELL_HUNTER_HUNTERS_MARK_AURA_2, true);
			caster.RemoveAurasDueToSpell(HunterSpells.SPELL_HUNTER_MARKING_TARGETS);
		}

		if (caster.HasAura(HunterSpells.SPELL_HUNTER_LETHAL_SHOTS) && RandomHelper.randChance(20))
		{
			if (caster.GetSpellHistory().HasCooldown(HunterSpells.SPELL_HUNTER_RAPID_FIRE))
			{
				caster.GetSpellHistory().ModifyCooldown(HunterSpells.SPELL_HUNTER_RAPID_FIRE, TimeSpan.FromSeconds(-5000));
			}
		}

		if (caster.HasAura(HunterSpells.SPELL_HUNTER_CALLING_THE_SHOTS))
		{
			if (caster.GetSpellHistory().HasCooldown(HunterSpells.SPELL_HUNTER_TRUESHOT))
			{
				caster.GetSpellHistory().ModifyCooldown(HunterSpells.SPELL_HUNTER_TRUESHOT, TimeSpan.FromSeconds(-2500));
			}
		}
	}
}