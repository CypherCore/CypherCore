using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(115151)]
public class spell_monk_renewing_mist : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MonkSpells.SPELL_MONK_RENEWING_MIST_HOT);
	}

	private void HandleDummy(uint effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		Unit target = GetExplTargetUnit();
		if (target != null)
		{
			GetCaster().CastSpell(target, MonkSpells.SPELL_MONK_RENEWING_MIST_HOT, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.Launch));
	}
}