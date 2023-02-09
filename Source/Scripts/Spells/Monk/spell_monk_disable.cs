using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(116095)]
public class spell_monk_disable : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MonkSpells.SPELL_MONK_DISABLE, MonkSpells.SPELL_MONK_DISABLE_ROOT);
	}

	private void OnHitTarget(int UnnamedParameter)
	{
		var target = GetExplTargetUnit();

		if (target != null)
			if (target.HasAuraType(AuraType.ModDecreaseSpeed))
				GetCaster().CastSpell(target, MonkSpells.SPELL_MONK_DISABLE_ROOT, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(OnHitTarget, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
	}
}