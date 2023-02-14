// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// 32216 - Victorious
	// 82368 - Victorious
	[SpellScript(new uint[]
	             {
		             32216, 82368
	             })]
	public class spell_warr_victorious : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void HandleEffectProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
		{
			PreventDefaultAction();
			GetTarget().RemoveAura(GetId());
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.AddPctModifier, AuraScriptHookType.EffectProc));
			AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 1, AuraType.AddFlatModifier, AuraScriptHookType.EffectProc));
		}
	}
}