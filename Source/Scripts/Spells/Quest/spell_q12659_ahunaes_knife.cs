using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 52090 Ahunae's Knife
internal class spell_q12659_ahunaes_knife : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return GetCaster().IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		var caster = GetCaster().ToPlayer();

		var target = GetHitCreature();

		if (target)
		{
			target.DespawnOrUnsummon();
			caster.KilledMonsterCredit(CreatureIds.ScalpsKcBunny);
		}
	}
}