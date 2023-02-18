// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Druid;

[Script]
public class at_dru_starfall : AreaTriggerAI
{
	public int timeInterval;

	public at_dru_starfall(AreaTrigger areatrigger) : base(areatrigger)
	{
		// How often should the action be executed
		areatrigger.SetPeriodicProcTimer(850);
	}

	public override void OnPeriodicProc()
	{
		var caster = at.GetCaster();

		if (caster != null)
			foreach (var objguid in at.GetInsideUnits())
			{
				var unit = ObjectAccessor.Instance.GetUnit(caster, objguid);

				if (unit != null)
					if (caster.IsValidAttackTarget(unit))
						if (unit.IsInCombat())
						{
							caster.CastSpell(unit, StarfallSpells.STARFALL_DAMAGE, true);
							caster.CastSpell(unit, StarfallSpells.STELLAR_EMPOWERMENT, true);
						}
			}
	}
}