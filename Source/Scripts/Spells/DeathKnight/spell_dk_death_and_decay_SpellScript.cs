// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 43265 - Death and Decay
internal class spell_dk_death_and_decay_SpellScript : SpellScript, ISpellOnCast
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DeathKnightSpells.TighteningGrasp, DeathKnightSpells.TighteningGraspSlow);
	}

	public void OnCast()
	{
		if (GetCaster().HasAura(DeathKnightSpells.TighteningGrasp))
		{
			var pos = GetExplTargetDest();

			if (pos != null)
				GetCaster().CastSpell(pos, DeathKnightSpells.TighteningGraspSlow, new CastSpellExtraArgs(true));
		}
	}
}