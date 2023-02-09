using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(49821)]
public class spell_pri_mind_sear : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(PriestSpells.SPELL_PRIEST_MIND_SEAR_INSANITY, Difficulty.None) != null)
			return false;

		return true;
	}

	private void HandleInsanity(int UnnamedParameter)
	{
		GetCaster().CastSpell(GetCaster(), PriestSpells.SPELL_PRIEST_MIND_SEAR_INSANITY, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleInsanity, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}