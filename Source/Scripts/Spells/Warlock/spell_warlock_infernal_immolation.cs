// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 19483 - Immolation
	public class spell_warlock_infernal_immolation : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();


		private void PeriodicTick(AuraEffect UnnamedParameter)
		{
			PreventDefaultAction();
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.CastSpell(caster, WarlockSpells.IMMOLATION_TRIGGERED, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(caster.GetOwnerGUID()));
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
		}
	}
}