using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(204215)]
public class spell_pri_purge_the_wicked_selector : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(PriestSpells.SPELL_PRIEST_PURGE_THE_WICKED_DOT, PriestSpells.SPELL_PRIEST_PURGE_THE_WICKED);
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		targets.RemoveIf(new UnitAuraCheck<WorldObject>(true, PriestSpells.SPELL_PRIEST_PURGE_THE_WICKED_DOT, GetCaster().GetGUID()));
		targets.Sort(new ObjectDistanceOrderPred(GetExplTargetUnit()));
		if (targets.Count > 1)
		{
			targets.Resize(1);
		}
	}

	private void HandleDummy(uint UnnamedParameter)
	{
		GetCaster().AddAura(PriestSpells.SPELL_PRIEST_PURGE_THE_WICKED_DOT, GetHitUnit());
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
		SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}