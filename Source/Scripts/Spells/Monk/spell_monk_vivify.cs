using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(116670)]
public class spell_monk_vivify : SpellScript, IHasSpellEffects, ISpellAfterCast, ISpellBeforeCast
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void FilterRenewingMist(List<WorldObject> targets)
	{
		targets.RemoveIf(new UnitAuraCheck<WorldObject>(false, MonkSpells.SPELL_MONK_RENEWING_MIST_HOT, GetCaster().GetGUID()));
	}

	public void BeforeCast()
	{
		if (GetCaster().GetCurrentSpell(CurrentSpellTypes.Channeled) && GetCaster().GetCurrentSpell(CurrentSpellTypes.Channeled).GetSpellInfo().Id == MonkSpells.SPELL_MONK_SOOTHING_MIST)
		{
			GetSpell().m_castFlagsEx = SpellCastFlagsEx.None;
			SpellCastTargets targets = GetCaster().GetCurrentSpell(CurrentSpellTypes.Channeled).m_targets;
			GetSpell().InitExplicitTargets(targets);
		}
	}

	public void AfterCast()
	{
		Player caster = GetCaster().ToPlayer();
		if (caster == null)
		{
			return;
		}

		if (caster.HasAura(MonkSpells.SPELL_LIFECYCLES))
		{
			caster.CastSpell(caster, MonkSpells.SPELL_MONK_LIFECYCLES_ENVELOPING_MIST, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterRenewingMist, 1, Targets.UnitDestAreaAlly));
	}
}