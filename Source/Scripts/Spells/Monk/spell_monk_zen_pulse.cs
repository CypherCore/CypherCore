using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(124081)]
public class spell_monk_zen_pulse : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(MonkSpells.SPELL_MONK_ZEN_PULSE_DAMAGE, Difficulty.None) != null)
			return false;

		return true;
	}

	private void OnHit(int UnnamedParameter)
	{
		GetCaster().CastSpell(GetCaster(), MonkSpells.SPELL_MONK_ZEN_PULSE_HEAL, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(OnHit, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}