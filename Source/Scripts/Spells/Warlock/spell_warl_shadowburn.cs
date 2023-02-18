// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(17877)]
	public class spell_warl_shadowburn : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

		private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			if (GetCaster())
			{
				var removeMode = GetTargetApplication().GetRemoveMode();

				if (removeMode == AuraRemoveMode.Death)
					GetCaster().SetPower(PowerType.SoulShards, GetCaster().GetPower(PowerType.SoulShards) + 50);
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
		}
	}
}