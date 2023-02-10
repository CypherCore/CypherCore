using System;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Felstorm - 119914
	[SpellScript(119914)]
	public class spell_warl_felstorm : SpellScript, ISpellAfterHit, ISpellCheckCast
	{
		public void AfterHit()
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.ToPlayer().GetSpellHistory().ModifyCooldown(GetSpellInfo().Id, TimeSpan.FromSeconds(45));
		}

		public SpellCastResult CheckCast()
		{
			var caster = GetCaster();
			var pet    = caster.GetGuardianPet();

			if (caster == null || pet == null)
				return SpellCastResult.DontReport;

			if (pet.GetSpellHistory().HasCooldown(WarlockSpells.FELGUARD_FELSTORM))
				return SpellCastResult.CantDoThatRightNow;

			return SpellCastResult.SpellCastOk;
		}
	}
}