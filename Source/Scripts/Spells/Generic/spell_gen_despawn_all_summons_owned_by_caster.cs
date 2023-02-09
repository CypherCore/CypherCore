using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // 160938 - Despawn All Summons (Garrison Intro Only)
internal class spell_gen_despawn_all_summons_owned_by_caster : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScriptEffect(int effIndex)
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var target = GetHitCreature();

			if (target.GetOwner() == caster)
				target.DespawnOrUnsummon();
		}
	}
}