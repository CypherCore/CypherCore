// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Rogue;

[Script]
public class at_rog_smoke_bombAI : AreaTriggerAI
{
	public at_rog_smoke_bombAI(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.ToPlayer())
			return;

		if (caster.IsValidAssistTarget(unit))
			caster.CastSpell(unit, RogueSpells.SPELL_ROGUE_SMOKE_BOMB_AURA, true);
	}

	public override void OnUnitExit(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.ToPlayer())
			return;

		if (unit.HasAura(RogueSpells.SPELL_ROGUE_SMOKE_BOMB_AURA))
			unit.RemoveAurasDueToSpell(RogueSpells.SPELL_ROGUE_SMOKE_BOMB_AURA);
	}
}