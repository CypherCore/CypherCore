// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DeathKnight;

[Script]
public class at_dk_antimagic_zone : AreaTriggerAI
{
	public at_dk_antimagic_zone(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit unit)
	{
		//TODO: Improve unit targets
		if (unit.IsPlayer() && !unit.IsHostileTo(at.GetCaster()))
			if (!unit.HasAura(DeathKnightSpells.ANTIMAGIC_ZONE_DAMAGE_TAKEN))
				unit.AddAura(DeathKnightSpells.ANTIMAGIC_ZONE_DAMAGE_TAKEN, unit);
	}

	public override void OnUnitExit(Unit unit)
	{
		if (unit.HasAura(DeathKnightSpells.ANTIMAGIC_ZONE_DAMAGE_TAKEN))
			unit.RemoveAura(DeathKnightSpells.ANTIMAGIC_ZONE_DAMAGE_TAKEN);
	}
}