using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

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
		var target = GetHitPlayer();

		if (target)
		{
			var aura = GetHitAura();

			if (aura == null ||
			    aura.GetStackAmount() < 10)
				return;

			target.CastSpell(target, GenericSpellIds.LandmineKnockbackAchievement, true);
		}
	}
}