using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 47540 - Penance
internal class spell_pri_penance : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.PenanceChannelDamage, PriestSpells.PenanceChannelHealing);
	}

	public SpellCastResult CheckCast()
	{
		Unit caster = GetCaster();
		Unit target = GetExplTargetUnit();

		if (target)
			if (!caster.IsFriendlyTo(target))
			{
				if (!caster.IsValidAttackTarget(target))
					return SpellCastResult.BadTargets;

				if (!caster.IsInFront(target))
					return SpellCastResult.NotInfront;
			}

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDummy(uint effIndex)
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();

		if (target)
		{
			if (caster.IsFriendlyTo(target))
				caster.CastSpell(target, PriestSpells.PenanceChannelHealing, new CastSpellExtraArgs().SetTriggeringSpell(GetSpell()));
			else
				caster.CastSpell(target, PriestSpells.PenanceChannelDamage, new CastSpellExtraArgs().SetTriggeringSpell(GetSpell()));
		}
	}
}