// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 193358 - Grand Melee
internal class spell_rog_grand_melee : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(RogueSpells.SliceAndDice);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var procSpell = eventInfo.GetProcSpell();

		return procSpell && procSpell.HasPowerTypeCost(PowerType.ComboPoints);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
	{
		var procSpell = procInfo.GetProcSpell();
		var amount    = aurEff.GetAmount() * procSpell.GetPowerTypeCostAmount(PowerType.ComboPoints).Value * 1000;

		var target = GetTarget();

		if (target != null)
		{
			var aura = target.GetAura(RogueSpells.SliceAndDice);

			if (aura != null)
			{
				aura.SetDuration(aura.GetDuration() + amount);
			}
			else
			{
				CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
				args.AddSpellMod(SpellValueMod.Duration, amount);
				target.CastSpell(target, RogueSpells.SliceAndDice, args);
			}
		}
	}
}