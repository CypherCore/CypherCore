using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // 83477 - Eject Passengers 3-8
internal class spell_gen_eject_passengers_3_8 : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScriptEffect(uint effIndex)
	{
		var vehicle = GetHitUnit().GetVehicleKit();

		if (vehicle == null)
			return;

		for (sbyte i = 2; i < 8; i++)
			vehicle.GetPassenger(i)?.ExitVehicle();
	}
}