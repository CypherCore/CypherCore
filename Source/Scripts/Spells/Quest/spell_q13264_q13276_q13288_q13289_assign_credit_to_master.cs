using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 60041 - Assign Geist Kill Credit to Master
internal class spell_q13264_q13276_q13288_q13289_assign_credit_to_master : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		Unit target = GetHitUnit();

		if (target != null)
		{
			Unit owner = target.GetOwner();

			owner?.CastSpell(owner, (uint)GetEffectValue(), true);
		}
	}
}