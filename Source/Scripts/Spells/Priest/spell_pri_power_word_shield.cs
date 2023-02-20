// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 17 - Power Word: Shield
internal class spell_pri_power_word_shield : SpellScript, ISpellCheckCast, ISpellAfterHit
{
	public void AfterHit()
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (target != null)
			if (!caster.HasAura(PriestSpells.RAPTURE))
				caster.CastSpell(target, PriestSpells.WEAKENED_SOUL, true);
	}

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.WEAKENED_SOUL);
	}

	public SpellCastResult CheckCast()
	{
		var caster = GetCaster();
		var target = GetExplTargetUnit();

		if (target != null)
			if (!caster.HasAura(PriestSpells.RAPTURE))
				if (target.HasAura(PriestSpells.WEAKENED_SOUL, caster.GetGUID()))
					return SpellCastResult.BadTargets;

		return SpellCastResult.SpellCastOk;
	}
}