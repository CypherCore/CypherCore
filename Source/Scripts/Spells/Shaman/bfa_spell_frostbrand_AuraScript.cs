﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	// Frostbrand - 196834
	[SpellScript(196834)]
	public class bfa_spell_frostbrand : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();

			var attacker = eventInfo.GetActionTarget();
			var caster   = GetCaster();

			if (caster == null || attacker == null)
				return;

			caster.CastSpell(attacker, ShamanSpells.SPELL_FROSTBRAND_SLOW, true);

			if (caster.HasAura(ShamanSpells.SPELL_HAILSTORM_TALENT))
				caster.CastSpell(attacker, ShamanSpells.SPELL_HAILSTORM_TALENT_PROC, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 1, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
		}
	}
}