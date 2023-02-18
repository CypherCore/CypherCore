// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DeathKnight;

[Script]
public class at_dk_decomposing_aura : AreaTriggerAI
{
	public at_dk_decomposing_aura(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitExit(Unit unit)
	{
		unit.RemoveAurasDueToSpell(DeathKnightSpells.DECOMPOSING_AURA_DAMAGE, at.GetCasterGuid());
	}
}