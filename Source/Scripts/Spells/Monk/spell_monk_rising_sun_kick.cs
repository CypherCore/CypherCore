// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
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

		if (caster.HasAura(MonkSpells.RISING_THUNDER))
			caster.ToPlayer().GetSpellHistory().ResetCooldown(MonkSpells.THUNDER_FOCUS_TEA, true);

		if (caster.GetPrimarySpecialization() == TalentSpecialization.MonkBattledancer)
			caster.CastSpell(target, MonkSpells.MORTAL_WOUNDS, true);

		if (caster.GetPrimarySpecialization() == TalentSpecialization.MonkMistweaver && caster.HasAura(MonkSpells.RISING_MIST))
		{
			caster.CastSpell(MonkSpells.RISING_MIST_HEAL, true);

			var reneWingMist = caster.GetAura(MonkSpells.RENEWING_MIST_HOT);

			if (reneWingMist != null)
				reneWingMist.RefreshDuration(true);

			var envelopingMist = caster.GetAura(MonkSpells.ENVELOPING_MIST);

			if (envelopingMist != null)
				envelopingMist.RefreshDuration(true);

			var essenceFont = caster.GetAura(MonkSpells.ESSENCE_FONT_PERIODIC_HEAL);

			if (essenceFont != null)
				essenceFont.RefreshDuration(true);
		}

		var u_li = new List<Unit>();
		caster.GetFriendlyUnitListInRange(u_li, 100.0f);

		foreach (var targets in u_li)
		{
			var relatedAuras = targets.GetAura(MonkSpells.RENEWING_MIST_HOT);

			if (relatedAuras == null)
				relatedAuras = targets.GetAura(MonkSpells.ENVELOPING_MIST);

			if (relatedAuras == null)
				relatedAuras = targets.GetAura(MonkSpells.ESSENCE_FONT_PERIODIC_HEAL);

			if (relatedAuras != null)
				relatedAuras.RefreshDuration(true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.TriggerSpell, SpellScriptHookType.EffectHitTarget));
	}
}