// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	// 61882
	[SpellScript(61882)]
	public class aura_sha_earthquake : AuraScript
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return ValidateSpellInfo(ShamanSpells.EARTHQUAKE);
		}

		private void HandlePeriodic(AuraEffect UnnamedParameter)
		{
			var at = GetTarget().GetAreaTrigger(ShamanSpells.EARTHQUAKE);

			if (at != null)
				GetTarget().CastSpell(at.GetPosition(), ShamanSpells.EARTHQUAKE_TICK, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 1, AuraType.PeriodicDummy));
		}
	}
}