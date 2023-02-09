using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(585)]
public class spell_pri_smite : SpellScript, IHasSpellEffects, ISpellAfterCast
{
	public List<ISpellEffect> SpellEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(PriestSpells.SPELL_PRIEST_SMITE_ABSORB);
	}

	private void HandleHit(uint UnnamedParameter)
	{
		var caster = GetCaster().ToPlayer();
		var target = GetHitUnit();

		if (caster == null || target == null)
			return;

		if (!caster.ToPlayer())
			return;

		var dmg = GetHitDamage();

		if (caster.HasAura(PriestSpells.SPELL_PRIEST_HOLY_WORDS) || caster.GetPrimarySpecialization() == TalentSpecialization.PriestHoly)
			if (caster.GetSpellHistory().HasCooldown(PriestSpells.SPELL_PRIEST_HOLY_WORD_CHASTISE))
				caster.GetSpellHistory().ModifyCooldown(PriestSpells.SPELL_PRIEST_HOLY_WORD_CHASTISE, TimeSpan.FromSeconds(-4 * Time.InMilliseconds));
	}

	public void AfterCast()
	{
		var caster = GetCaster().ToPlayer();

		if (caster == null)
			return;

		if (caster.GetPrimarySpecialization() == TalentSpecialization.PriestHoly)
			if (caster.GetSpellHistory().HasCooldown(PriestSpells.SPELL_PRIEST_HOLY_WORD_CHASTISE))
				caster.GetSpellHistory().ModifyCooldown(PriestSpells.SPELL_PRIEST_HOLY_WORD_CHASTISE, TimeSpan.FromSeconds(-6 * Time.InMilliseconds));
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}