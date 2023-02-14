// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// 212619 - Call Felhunter
	[SpellScript(212619)]
	public class spell_warlock_call_felhunter : SpellScript, ISpellCheckCast
	{
		public SpellCastResult CheckCast()
		{
			var caster = GetCaster();

			if (caster == null || !caster.ToPlayer())
				return SpellCastResult.BadTargets;

			if (caster.ToPlayer().GetPet() && caster.ToPlayer().GetPet().GetEntry() == 417)
				return SpellCastResult.CantDoThatRightNow;

			return SpellCastResult.SpellCastOk;
		}
	}
}