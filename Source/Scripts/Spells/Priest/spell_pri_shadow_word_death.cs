using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(32379)]
public class spell_pri_shadow_word_death : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void HandleDamage(int UnnamedParameter)
	{
		var target = GetHitUnit();

		if (target != null)
		{
			if (target.GetHealth() < (ulong)GetHitDamage())
				GetCaster().CastSpell(GetCaster(), PriestSpells.SPELL_PRIEST_SHADOW_WORD_DEATH_ENERGIZE_KILL, true);
			else
				GetCaster().ModifyPower(PowerType.Insanity, GetSpellInfo().GetEffect(2).BasePoints);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}