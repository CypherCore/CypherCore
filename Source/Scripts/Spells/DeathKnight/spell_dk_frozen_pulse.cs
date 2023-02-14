// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.DeathKnight;

[SpellScript(194909)]
public class spell_dk_frozen_pulse : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return false;

		if (caster.GetPower(PowerType.Runes) > GetSpellInfo().GetEffect(1).BasePoints)
			return false;

		return true;
	}
}