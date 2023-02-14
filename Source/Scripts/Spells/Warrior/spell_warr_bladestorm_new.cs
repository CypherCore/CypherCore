// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// New Bladestorm - 222634
	[SpellScript(222634)]
	public class spell_warr_bladestorm_new : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(WarriorSpells.NEW_BLADESTORM, Difficulty.None) != null)
				return false;

			return true;
		}

		private void HandlePeriodicDummy(AuraEffect UnnamedParameter)
		{
			GetCaster().CastSpell(GetCaster(), 50622, true); // Bladestorm main hand damage
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodicDummy, 0, AuraType.PeriodicDummy));
		}
	}
}