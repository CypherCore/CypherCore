// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 273221 - Aftershock
[SpellScript(273221)]
internal class spell_sha_aftershock : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellEntry)
	{
		return ValidateSpellInfo(ShamanSpells.AftershockEnergize);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var procSpell = eventInfo.GetProcSpell();

		if (procSpell != null)
		{
			var cost = procSpell.GetPowerTypeCostAmount(PowerType.Maelstrom);

			if (cost.HasValue)
				return cost > 0 && RandomHelper.randChance(aurEff.GetAmount());
		}

		return false;
	}

	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var procSpell = eventInfo.GetProcSpell();
		var energize  = procSpell.GetPowerTypeCostAmount(PowerType.Maelstrom);

		eventInfo.GetActor()
		         .CastSpell(eventInfo.GetActor(),
		                    ShamanSpells.AftershockEnergize,
		                    new CastSpellExtraArgs(energize != 0)
			                    .AddSpellMod(SpellValueMod.BasePoint0, energize.Value));
	}
}