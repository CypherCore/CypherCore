// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(196782)]
public class aura_dk_outbreak_periodic : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void HandleDummyTick(AuraEffect UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var friendlyUnits = new List<Unit>();
			GetTarget().GetFriendlyUnitListInRange(friendlyUnits, 10.0f);

			foreach (var unit in friendlyUnits)
				if (!unit.HasUnitFlag(UnitFlags.ImmuneToPc) && unit.IsInCombatWith(caster))
					caster.CastSpell(unit, DeathKnightSpells.SPELL_DK_VIRULENT_PLAGUE, true);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleDummyTick, 0, AuraType.PeriodicDummy));
	}
}