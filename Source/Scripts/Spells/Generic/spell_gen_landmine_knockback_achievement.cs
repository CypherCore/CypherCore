using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_landmine_knockback_achievement : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		Player target = GetHitPlayer();

		if (target)
		{
			Aura aura = GetHitAura();

			if (aura == null ||
			    aura.GetStackAmount() < 10)
				return;

			target.CastSpell(target, GenericSpellIds.LandmineKnockbackAchievement, true);
		}
	}
}