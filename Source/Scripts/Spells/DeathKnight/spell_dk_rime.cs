// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 59057 - Rime
internal class spell_dk_rime : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.GetEffects().Count > 1 && ValidateSpellInfo(DeathKnightSpells.FROSTSCYTHE);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.ProcTriggerSpell));
	}

	private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var chance = (double)GetSpellInfo().GetEffect(1).CalcValue(GetTarget());

		if (eventInfo.GetSpellInfo().Id == DeathKnightSpells.FROSTSCYTHE)
			chance /= 2.0f;

		return RandomHelper.randChance(chance);
	}
}