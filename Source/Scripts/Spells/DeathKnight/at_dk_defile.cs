// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DeathKnight;

[Script]
public class at_dk_defile : AreaTriggerAI
{
	public at_dk_defile(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnCreate()
	{
		at.GetCaster().CastSpell(at.GetPosition(), DeathKnightSpells.SPELL_DK_SUMMON_DEFILE, true);
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster != null)
			caster.CastSpell(unit, DeathKnightSpells.SPELL_DK_DEFILE_DUMMY, true);
	}

	public override void OnUnitExit(Unit unit)
	{
		unit.RemoveAurasDueToSpell(DeathKnightSpells.SPELL_DK_DEFILE_DUMMY);
	}
}