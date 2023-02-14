// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_eject_passenger : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		if (spellInfo.GetEffects().Empty())
			return false;

		if (spellInfo.GetEffect(0).CalcValue() < 1)
			return false;

		return true;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(EjectPassenger, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void EjectPassenger(uint effIndex)
	{
		var vehicle = GetHitUnit().GetVehicleKit();

		if (vehicle != null)
		{
			var passenger = vehicle.GetPassenger((sbyte)(GetEffectValue() - 1));

			if (passenger)
				passenger.ExitVehicle();
		}
	}
}