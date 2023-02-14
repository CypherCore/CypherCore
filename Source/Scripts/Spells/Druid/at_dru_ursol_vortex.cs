// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Druid;

[Script]
public class at_dru_ursol_vortex : AreaTriggerAI
{
	public at_dru_ursol_vortex(AreaTrigger at) : base(at)
	{
	}

	public override void OnUnitEnter(Unit target)
	{
		var caster = at.GetCaster();

		if (caster != null && caster.IsInCombatWith(target))
			caster.CastSpell(target, DruidSpells.SPELL_DRU_URSOL_VORTEX_DEBUFF, true);
	}

	public override void OnUnitExit(Unit target)
	{
		target.RemoveAurasDueToSpell(DruidSpells.SPELL_DRU_URSOL_VORTEX_DEBUFF);

		if (!_hasPull && target.IsValidAttackTarget(at.GetCaster()))
		{
			_hasPull = true;
			target.CastSpell(at.GetPosition(), DruidSpells.SPELL_DRU_URSOL_VORTEX_PULL, true);
		}
	}

	private bool _hasPull = false;
}