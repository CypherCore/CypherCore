// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_binding_shotAI : AreaTriggerAI
{
	public enum UsedSpells
	{
		BINDING_SHOT_AURA = 117405,
		BINDING_SHOT_STUN = 117526,
		BINDING_SHOT_IMMUNE = 117553,
		BINDING_SHOT_VISUAL_1 = 118306,
		HUNDER_BINDING_SHOT_VISUAL_2 = 117614
	}

	public at_hun_binding_shotAI(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		if (unit == null)
			return;

		if (!caster.IsFriendlyTo(unit))
			unit.CastSpell(unit, UsedSpells.BINDING_SHOT_AURA, true);
	}

	public override void OnUnitExit(Unit unit)
	{
		if (unit == null || !at.GetCaster())
			return;

		var pos = at.GetPosition();

		// Need to check range also, since when the trigger is removed, this get called as well.
		if (unit.HasAura(UsedSpells.BINDING_SHOT_AURA) && unit.GetExactDist(pos) >= 5.0f)
		{
			unit.RemoveAura(UsedSpells.BINDING_SHOT_AURA);
			at.GetCaster().CastSpell(unit, UsedSpells.BINDING_SHOT_STUN, true);
		}
	}
}