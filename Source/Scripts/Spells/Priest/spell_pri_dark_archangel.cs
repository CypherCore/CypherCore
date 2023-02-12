using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(197871)]
public class spell_pri_dark_archangel : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(PriestSpells.SPELL_PRIEST_DARK_ARCHANGEL_BUFF);
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		targets.Remove(GetCaster());
		targets.RemoveIf(new UnitAuraCheck<WorldObject>(false, PriestSpells.SPELL_PRIEST_ATONEMENT_AURA, GetCaster().GetGUID()));
	}

	private void HandleScriptEffect(uint UnnamedParameter)
	{
		GetCaster().CastSpell(GetHitUnit(), PriestSpells.SPELL_PRIEST_DARK_ARCHANGEL_BUFF, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaAlly));
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}
}