using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(88625)]
public class spell_pri_holy_word_chastise : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void HandleOnHit(int UnnamedParameter)
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster == null || target == null)
			return;

		if (caster.HasAura(PriestSpells.SPELL_PRIEST_CENSURE))
			caster.CastSpell(target, PriestSpells.SPELL_PRIEST_HOLY_WORD_CHASTISE_STUN, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}