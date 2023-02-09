using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 49285 - Hand Over Reins
internal class spell_q12414_hand_over_reins : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		Creature caster = GetCaster().ToCreature();
		GetHitUnit().ExitVehicle();

		if (caster)
			caster.DespawnOrUnsummon();
	}
}