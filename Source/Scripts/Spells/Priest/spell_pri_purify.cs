using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(527)]
public class spell_pri_purify : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public SpellCastResult CheckCast()
	{
		var caster = GetCaster();
		var target = GetExplTargetUnit();

		if (caster != target && target.IsFriendlyTo(caster))
			return SpellCastResult.BadTargets;

		return SpellCastResult.SpellCastOk;
	}

	private void AfterEffectHit(uint UnnamedParameter)
	{
		if (GetHitUnit().IsFriendlyTo(GetCaster()))
		{
			GetCaster().CastSpell(GetHitUnit(), PriestSpells.SPELL_PRIEST_DISPEL_MAGIC_HOSTILE, true);
			GetCaster().CastSpell(GetHitUnit(), PriestSpells.SPELL_PRIEST_CURE_DISEASE, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(AfterEffectHit, 0, SpellEffectName.Dispel, SpellScriptHookType.EffectHitTarget));
	}
}