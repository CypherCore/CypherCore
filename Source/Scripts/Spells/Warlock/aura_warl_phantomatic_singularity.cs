// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 205179
	[SpellScript(205179)]
	public class aura_warl_phantomatic_singularity : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public void OnTick(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();

			if (GetCaster())
				caster.CastSpell(GetTarget().GetPosition(), WarlockSpells.PHANTOMATIC_SINGULARITY_DAMAGE, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicLeech));
		}
	}
}