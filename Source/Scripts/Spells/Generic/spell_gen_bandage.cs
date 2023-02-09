using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_bandage : SpellScript, ISpellCheckCast, ISpellAfterHit
{
	public void AfterHit()
	{
		Unit target = GetHitUnit();

		if (target)
			GetCaster().CastSpell(target, GenericSpellIds.RecentlyBandaged, true);
	}

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.RecentlyBandaged);
	}

	public SpellCastResult CheckCast()
	{
		Unit target = GetExplTargetUnit();

		if (target)
			if (target.HasAura(GenericSpellIds.RecentlyBandaged))
				return SpellCastResult.TargetAurastate;

		return SpellCastResult.SpellCastOk;
	}
}