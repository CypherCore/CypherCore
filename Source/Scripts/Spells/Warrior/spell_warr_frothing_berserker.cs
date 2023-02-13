﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	//215571 Frothing Berserker
	[SpellScript(215571)]
	public class spell_warr_frothing_berserker : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
		{
			GetCaster().CastSpell(GetCaster(), WarriorSpells.FROTHING_BERSERKER, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.AddFlatModifier, AuraScriptHookType.EffectProc));
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 2, AuraType.AddFlatModifier, AuraScriptHookType.EffectProc));
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 3, AuraType.AddFlatModifier, AuraScriptHookType.EffectProc));
		}
	}
}