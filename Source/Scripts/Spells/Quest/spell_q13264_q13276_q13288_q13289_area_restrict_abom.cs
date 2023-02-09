using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 76245 - Area Restrict Abom
internal class spell_q13264_q13276_q13288_q13289_area_restrict_abom : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		var creature = GetHitCreature();

		if (creature != null)
		{
			var area = creature.GetAreaId();

			if (area != Misc.AreaTheBrokenFront &&
			    area != Misc.AreaMordRetharTheDeathGate)
				creature.DespawnOrUnsummon();
		}
	}
}