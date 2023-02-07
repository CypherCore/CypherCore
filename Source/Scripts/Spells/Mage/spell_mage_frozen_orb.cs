using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(84721)]
public class spell_mage_frozen_orb : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void HandleHit(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();
		if (caster == null || target == null)
		{
			return;
		}

		caster.CastSpell(target, MageSpells.SPELL_MAGE_CHILLED, true);

		// Fingers of Frost
		if (caster.HasSpell(MageSpells.SPELL_MAGE_FINGERS_OF_FROST))
		{
			float fingersFrostChance = 10.0f;

			if (caster.HasAura(MageSpells.SPELL_MAGE_FROZEN_TOUCH))
			{
				AuraEffect frozenEff0 = caster.GetAuraEffect(MageSpells.SPELL_MAGE_FROZEN_TOUCH, 0);
				if (frozenEff0 != null)
				{
					int pct = frozenEff0.GetAmount();
					MathFunctions.AddPct(ref fingersFrostChance, pct);
				}
			}

			if (RandomHelper.randChance(fingersFrostChance))
			{
				caster.CastSpell(caster, MageSpells.SPELL_MAGE_FINGERS_OF_FROST_VISUAL_UI, true);
				caster.CastSpell(caster, MageSpells.SPELL_MAGE_FINGERS_OF_FROST_AURA, true);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}