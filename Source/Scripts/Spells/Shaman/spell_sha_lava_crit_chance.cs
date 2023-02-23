// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 285466 - Lava Burst Overload Damage
[SpellScript(285466)]
internal class spell_sha_lava_crit_chance : SpellScript, ISpellCalcCritChance
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.LavaBurstRank2, ShamanSpells.FlameShock);
	}

	public void CalcCritChance(Unit victim, ref double critChance)
	{
		var caster = GetCaster();

		if (caster == null ||
		    victim == null)
			return;

		if (caster.HasAura(ShamanSpells.LavaBurstRank2) &&
		    victim.HasAura(ShamanSpells.FlameShock, caster.GetGUID()))
			if (victim.GetTotalAuraModifier(AuraType.ModAttackerSpellAndWeaponCritChance) > -100)
				critChance = 100.0f;
	}
}