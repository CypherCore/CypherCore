// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Burning Rush - 111400
	[SpellScript(111400)]
	public class spell_warl_burning_rush : SpellScript, ISpellCheckCast, ISpellBeforeCast, ISpellAfterHit
	{
		private bool _isRemove = false;

		public SpellCastResult CheckCast()
		{
			var caster = GetCaster();

			if (caster == null)
				return SpellCastResult.CantDoThatRightNow;

			if (caster.HealthBelowPct(5))
				return SpellCastResult.CantDoThatRightNow;

			return SpellCastResult.SpellCastOk;
		}

		public void BeforeCast()
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			if (caster.HasAura(WarlockSpells.BURNING_RUSH))
				_isRemove = true;
		}

		public void AfterHit()
		{
			if (_isRemove)
				GetCaster().RemoveAurasDueToSpell(WarlockSpells.BURNING_RUSH);
		}
	}
}