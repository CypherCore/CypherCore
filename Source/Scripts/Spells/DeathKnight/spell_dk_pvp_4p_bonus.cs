// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 61257 - Runic Power Back on Snare/Root
internal class spell_dk_pvp_4p_bonus : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DeathKnightSpells.RunicReturn);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var spellInfo = eventInfo.GetSpellInfo();

		if (spellInfo == null)
			return false;

		return (spellInfo.GetAllEffectsMechanicMask() & ((1 << (int)Mechanics.Root) | (1 << (int)Mechanics.Snare))) != 0;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		eventInfo.GetActionTarget().CastSpell((Unit)null, DeathKnightSpells.RunicReturn, true);
	}
}