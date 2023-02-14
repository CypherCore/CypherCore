// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Whiplash - 119909
	[SpellScript(119909)]
	public class spell_warl_whiplash : SpellScript, ISpellAfterHit, ISpellCheckCast
	{
		public void AfterHit()
		{
			var caster = GetCaster();
			var dest   = GetExplTargetDest();
			var pet    = caster.GetGuardianPet();

			if (caster == null || pet == null || dest == null)
				return;

			/*if (pet->GetEntry() != PET_ENTRY_SUCCUBUS)
				return;*/

			pet.CastSpell(new Position(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ()), WarlockSpells.SUCCUBUS_WHIPLASH, true);
			caster.ToPlayer().GetSpellHistory().ModifyCooldown(GetSpellInfo().Id, TimeSpan.FromSeconds(25));
		}

		public SpellCastResult CheckCast()
		{
			var caster = GetCaster();
			var pet    = caster.GetGuardianPet();

			if (caster == null || pet == null)
				return SpellCastResult.DontReport;

			if (pet.GetSpellHistory().HasCooldown(WarlockSpells.SUCCUBUS_WHIPLASH))
				return SpellCastResult.CantDoThatRightNow;

			return SpellCastResult.SpellCastOk;
		}
	}
}