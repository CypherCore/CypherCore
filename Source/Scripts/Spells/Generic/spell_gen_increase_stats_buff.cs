using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script("spell_pal_blessing_of_kings")]
[Script("spell_pal_blessing_of_might")]
[Script("spell_dru_mark_of_the_wild")]
[Script("spell_pri_power_word_fortitude")]
[Script("spell_pri_shadow_protection")]
internal class spell_gen_increase_stats_buff : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		if (GetHitUnit().IsInRaidWith(GetCaster()))
			GetCaster().CastSpell(GetCaster(), (uint)GetEffectValue() + 1, true); // raid buff
		else
			GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true); // single-Target buff
	}
}