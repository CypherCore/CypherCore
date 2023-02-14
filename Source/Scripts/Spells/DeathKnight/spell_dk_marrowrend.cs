// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(195182)]
public class spell_dk_marrowrend : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();

		if (caster != null)
		{
			caster.CastSpell(null, DeathKnightSpells.SPELL_DK_BONE_SHIELD, true);
			var boneShield = caster.GetAura(DeathKnightSpells.SPELL_DK_BONE_SHIELD);

			if (boneShield != null)
				boneShield.SetStackAmount(3);
		}
	}
}