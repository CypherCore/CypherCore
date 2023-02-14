// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(217200)]
public class spell_hun_barbed_shot : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();

		if (caster != null)
		{
			caster.CastSpell(caster, HunterSpells.SPELL_BARBED_SHOT_PLAYERAURA, true);

			if (caster.IsPlayer())
			{
				Unit pet = caster.GetGuardianPet();

				if (pet != null)
				{
					if (!pet)
						return;

					caster.CastSpell(pet, HunterSpells.SPELL_BARBED_SHOT_PETAURA, true);
				}
			}
		}
	}
}