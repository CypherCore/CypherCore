using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 17 - Power Word: Shield
internal class spell_pri_power_word_shield : SpellScript, ISpellCheckCast, ISpellAfterHit
{
	public void AfterHit()
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();

		if (target != null)
			if (!caster.HasAura(PriestSpells.Rapture))
				caster.CastSpell(target, PriestSpells.WeakenedSoul, true);
	}

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.WeakenedSoul);
	}

	public SpellCastResult CheckCast()
	{
		Unit caster = GetCaster();
		Unit target = GetExplTargetUnit();

		if (target != null)
			if (!caster.HasAura(PriestSpells.Rapture))
				if (target.HasAura(PriestSpells.WeakenedSoul, caster.GetGUID()))
					return SpellCastResult.BadTargets;

		return SpellCastResult.SpellCastOk;
	}
}