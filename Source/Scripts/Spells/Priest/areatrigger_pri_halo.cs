// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 120517 - Halo
internal class areatrigger_pri_halo : AreaTriggerAI
{
	public areatrigger_pri_halo(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster != null)
		{
			if (caster.IsValidAttackTarget(unit))
				caster.CastSpell(unit, PriestSpells.HALO_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
			else if (caster.IsValidAssistTarget(unit))
				caster.CastSpell(unit, PriestSpells.HALO_HEAL, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
		}
	}
}