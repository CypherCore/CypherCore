using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script]
internal class spell_q11896_weakness_to_lightning_46444 : SpellScript, IHasSpellEffects
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

			if (owner != null)
				target.CastSpell(owner, (uint)GetEffectValue(), true);
		}
	}
}