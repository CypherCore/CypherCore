using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(107428)]
public class spell_monk_rising_sun_kick : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void HandleOnHit(uint UnnamedParameter)
	{
		Player caster = GetCaster().ToPlayer();
		Unit   target = GetHitUnit();
		if (target == null || caster == null)
		{
			return;
		}

		if (caster.HasAura(MonkSpells.SPELL_MONK_RISING_THUNDER))
		{
			caster.ToPlayer().GetSpellHistory().ResetCooldown(MonkSpells.SPELL_MONK_THUNDER_FOCUS_TEA, true);
		}

		if (caster.GetPrimarySpecialization() == TalentSpecialization.MonkBattledancer)
		{
			caster.CastSpell(target, MonkSpells.SPELL_MONK_MORTAL_WOUNDS, true);
		}

		if (caster.GetPrimarySpecialization() == TalentSpecialization.MonkMistweaver && caster.HasAura(MonkSpells.SPELL_RISING_MIST))
		{
			caster.CastSpell(MonkSpells.SPELL_RISING_MIST_HEAL, true);

			Aura reneWingMist = caster.GetAura(MonkSpells.SPELL_MONK_RENEWING_MIST_HOT);
			if (reneWingMist != null)
			{
				reneWingMist.RefreshDuration(true);
			}

			Aura envelopingMist = caster.GetAura(MonkSpells.SPELL_MONK_ENVELOPING_MIST);
			if (envelopingMist != null)
			{
				envelopingMist.RefreshDuration(true);
			}

			Aura essenceFont = caster.GetAura(MonkSpells.SPELL_MONK_ESSENCE_FONT_PERIODIC_HEAL);
			if (essenceFont != null)
			{
				essenceFont.RefreshDuration(true);
			}
		}
		List<Unit> u_li = new List<Unit>();
		caster.GetFriendlyUnitListInRange(u_li, 100.0f);
		foreach (var targets in u_li)
		{
			Aura relatedAuras = targets.GetAura(MonkSpells.SPELL_MONK_RENEWING_MIST_HOT);

			if (relatedAuras == null)
				relatedAuras = targets.GetAura(MonkSpells.SPELL_MONK_ENVELOPING_MIST);

			if (relatedAuras == null)
				relatedAuras = targets.GetAura(MonkSpells.SPELL_MONK_ESSENCE_FONT_PERIODIC_HEAL);

			if (relatedAuras != null)
			{
				relatedAuras.RefreshDuration(true);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.TriggerSpell, SpellScriptHookType.EffectHitTarget));
	}
}