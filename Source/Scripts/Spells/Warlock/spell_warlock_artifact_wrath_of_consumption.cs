// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 199472 - Wrath of Consumption
	[SpellScript(199472)]
	public class spell_warlock_artifact_wrath_of_consumption : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster != null)
				caster.CastSpell(caster, WarlockSpells.WRATH_OF_CONSUMPTION_PROC, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}
}