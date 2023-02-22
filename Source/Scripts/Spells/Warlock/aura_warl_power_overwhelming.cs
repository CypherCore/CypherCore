// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(WarlockSpells.POWER_OVERWHELMING_AURA)]
	public class aura_warl_power_overwhelming : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

        void Apply(AuraEffect aura, AuraEffectHandleModes auraMode)
		{
			if (Global.SpellMgr.TryGetSpellInfo(WarlockSpells.POWER_OVERWHELMING, out var power))
				aura.ChangeAmount(power.GetEffect(1).BasePoints, true, true);
		}


        public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(Apply, 0, AuraType.Mastery, AuraEffectHandleModes.Real));
		}
	}
}