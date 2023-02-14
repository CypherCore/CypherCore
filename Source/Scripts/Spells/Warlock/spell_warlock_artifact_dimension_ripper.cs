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
	// 219415 - Dimension Ripper
	[SpellScript(219415)]
	public class spell_warlock_artifact_dimension_ripper : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
		{
			PreventDefaultAction();
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(WarlockSpells.DIMENSIONAL_RIFT, Difficulty.None).ChargeCategoryId);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}
}