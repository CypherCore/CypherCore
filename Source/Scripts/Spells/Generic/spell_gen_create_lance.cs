using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 63845 - Create Lance
internal class spell_gen_create_lance : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.CreateLanceAlliance, GenericSpellIds.CreateLanceHorde);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		PreventHitDefaultEffect(effIndex);

		Player target = GetHitPlayer();

		if (target)
		{
			if (target.GetTeam() == Team.Alliance)
				GetCaster().CastSpell(target, GenericSpellIds.CreateLanceAlliance, true);
			else
				GetCaster().CastSpell(target, GenericSpellIds.CreateLanceHorde, true);
		}
	}
}