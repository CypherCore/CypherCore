using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 50546 - The Focus on the Beach: Ley Line Focus Control Ring Effect
internal class spell_q12066_bunny_kill_credit : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var target = GetHitCreature();

		if (target)
			target.CastSpell(GetCaster(), QuestSpellIds.BunnyCreditBeam, false);
	}
}