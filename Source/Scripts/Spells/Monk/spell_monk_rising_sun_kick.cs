﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(107428)]
public class spell_monk_rising_sun_kick : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleOnHit(uint UnnamedParameter)
	{
		var caster = GetCaster().ToPlayer();
		var target = GetHitUnit();

		if (target == null || caster == null)
			return;

		if (caster.HasAura(MonkSpells.SPELL_MONK_RISING_THUNDER))
			caster.ToPlayer().GetSpellHistory().ResetCooldown(MonkSpells.SPELL_MONK_THUNDER_FOCUS_TEA, true);

		if (caster.GetPrimarySpecialization() == TalentSpecialization.MonkBattledancer)
			caster.CastSpell(target, MonkSpells.SPELL_MONK_MORTAL_WOUNDS, true);

		if (caster.GetPrimarySpecialization() == TalentSpecialization.MonkMistweaver && caster.HasAura(MonkSpells.SPELL_RISING_MIST))
		{
			caster.CastSpell(MonkSpells.SPELL_RISING_MIST_HEAL, true);

			var reneWingMist = caster.GetAura(MonkSpells.SPELL_MONK_RENEWING_MIST_HOT);

			if (reneWingMist != null)
				reneWingMist.RefreshDuration(true);

			var envelopingMist = caster.GetAura(MonkSpells.SPELL_MONK_ENVELOPING_MIST);

			if (envelopingMist != null)
				envelopingMist.RefreshDuration(true);

			var essenceFont = caster.GetAura(MonkSpells.SPELL_MONK_ESSENCE_FONT_PERIODIC_HEAL);

			if (essenceFont != null)
				essenceFont.RefreshDuration(true);
		}

		var u_li = new List<Unit>();
		caster.GetFriendlyUnitListInRange(u_li, 100.0f);

		foreach (var targets in u_li)
		{
			var relatedAuras = targets.GetAura(MonkSpells.SPELL_MONK_RENEWING_MIST_HOT);

			if (relatedAuras == null)
				relatedAuras = targets.GetAura(MonkSpells.SPELL_MONK_ENVELOPING_MIST);

			if (relatedAuras == null)
				relatedAuras = targets.GetAura(MonkSpells.SPELL_MONK_ESSENCE_FONT_PERIODIC_HEAL);

			if (relatedAuras != null)
				relatedAuras.RefreshDuration(true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.TriggerSpell, SpellScriptHookType.EffectHitTarget));
	}
}