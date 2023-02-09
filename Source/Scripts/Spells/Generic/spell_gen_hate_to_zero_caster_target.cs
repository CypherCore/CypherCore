using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // 63984 - Hate to Zero
internal class spell_gen_hate_to_zero_caster_target : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var target = GetHitUnit();

		if (target != null)
			if (target.CanHaveThreatList())
				target.GetThreatManager().ModifyThreatByPercent(GetCaster(), -100);
	}
}