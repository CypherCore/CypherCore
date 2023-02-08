using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(new uint[] { 47750, 47666 })]
public class spell_pri_penance_heal_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(PriestSpells.SPELL_PRIEST_POWER_OF_THE_DARK_SIDE_MARKER, PriestSpells.SPELL_PRIEST_PENANCE_HEAL);
	}

	private void HandleDummy(uint UnnamedParameter)
	{
		if (GetCaster().GetAuraEffect(PriestSpells.SPELL_PRIEST_CONTRITION, 0) != null)
		{
			foreach (AuraApplication auApp in GetCaster().GetAppliedAuras().LookupByKey(PriestSpells.SPELL_PRIEST_ATONEMENT_AURA))
			{
				GetCaster().CastSpell(auApp.GetTarget(), PriestSpells.SPELL_PRIEST_CONTRITION_HEAL, true);
			}
		}

		AuraEffect powerOfTheDarkSide = GetCaster().GetAuraEffect(PriestSpells.SPELL_PRIEST_POWER_OF_THE_DARK_SIDE_MARKER, 0);
		if (powerOfTheDarkSide != null)
		{
			if (GetSpellInfo().Id == PriestSpells.SPELL_PRIEST_PENANCE_HEAL)
			{
				int heal = GetHitHeal();
				MathFunctions.AddPct(ref heal, powerOfTheDarkSide.GetAmount());
				SetHitHeal(heal);
			}
			else
			{
				int damage = GetHitDamage();
				MathFunctions.AddPct(ref damage, powerOfTheDarkSide.GetAmount());
				SetHitDamage(damage);
			}
		}
	}

	public override void Register()
	{
		if (ScriptSpellId == PriestSpells.SPELL_PRIEST_PENANCE_HEAL)
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
		}
		if (ScriptSpellId == PriestSpells.SPELL_PRIEST_PENANCE_DAMAGE)
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}
	}
}