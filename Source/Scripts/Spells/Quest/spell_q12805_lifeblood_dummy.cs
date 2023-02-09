using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 54190 - Lifeblood Dummy
internal class spell_q12805_lifeblood_dummy : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return GetCaster().IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		Player caster = GetCaster().ToPlayer();

		Creature target = GetHitCreature();

		if (target)
		{
			caster.KilledMonsterCredit(CreatureIds.ShardKillCredit);
			target.CastSpell(target, (uint)GetEffectValue(), true);
			target.DespawnOrUnsummon(TimeSpan.FromSeconds(2));
		}
	}
}