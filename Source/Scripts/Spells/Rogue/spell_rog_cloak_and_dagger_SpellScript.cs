// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(new uint[]
             {
	             703, 1833, 8676
             })]
public class spell_rog_cloak_and_dagger_SpellScript : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		var caster = GetCaster();

		if (caster != null)
			if (caster.HasAuraType(AuraType.ModStealth))
				if (caster.HasAura(138106))
				{
					var target = GetExplTargetUnit();

					if (target != null)
						caster.CastSpell(target, 138916, true);
				}

		return SpellCastResult.SpellCastOk;
	}
}