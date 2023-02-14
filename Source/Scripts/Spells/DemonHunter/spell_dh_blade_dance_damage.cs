// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[Script] // 210155 - Death Sweep
internal class spell_dh_blade_dance_damage : SpellScript, ISpellOnHit
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DemonHunterSpells.FirstBlood);
	}

	public void OnHit()
	{
		var damage = GetHitDamage();

		var aurEff = GetCaster().GetAuraEffect(DemonHunterSpells.FirstBlood, 0);

		if (aurEff != null)
		{
			var script = aurEff.GetBase().GetScript<spell_dh_first_blood>();

			if (script != null)
				if (GetHitUnit().GetGUID() == script.GetFirstTarget())
					MathFunctions.AddPct(ref damage, aurEff.GetAmount());
		}

		SetHitDamage(damage);
	}
}