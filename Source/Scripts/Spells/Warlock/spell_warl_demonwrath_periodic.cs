// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// Demonwrath periodic - 193440
	[SpellScript(193440)]
	public class spell_warl_demonwrath_periodic : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void HandlePeriodic(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var rollChance = GetSpellInfo().GetEffect(2).BasePoints;

			if (RandomHelper.randChance(rollChance))
				caster.CastSpell(caster, WarlockSpells.DEMONWRATH_SOULSHARD, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 1, AuraType.PeriodicTriggerSpell));
		}
	}
}