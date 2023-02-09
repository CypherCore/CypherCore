using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(197862)]
public class spell_pri_archangel : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(PriestSpells.SPELL_PRIEST_ATONEMENT_AURA);
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		targets.RemoveIf(new UnitAuraCheck<WorldObject>(false, PriestSpells.SPELL_PRIEST_ATONEMENT_AURA, GetCaster().GetGUID()));
	}

	private void HandleScriptEffect(uint UnnamedParameter)
	{
		var aura = GetHitUnit().GetAura(PriestSpells.SPELL_PRIEST_ATONEMENT_AURA, GetCaster().GetGUID());

		if (aura != null)
			aura.RefreshDuration();
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 2, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 2, Targets.UnitCasterAreaParty));
	}
}