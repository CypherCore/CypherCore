using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script("spell_gen_eject_passenger_1", 0)]
[Script("spell_gen_eject_passenger_3", 2)]
internal class spell_gen_eject_passenger_with_seatId : SpellScript, IHasSpellEffects
{
	private readonly sbyte _seatId;

	public spell_gen_eject_passenger_with_seatId(sbyte seatId)
	{
		_seatId = seatId;
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(EjectPassenger, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void EjectPassenger(uint effIndex)
	{
		var vehicle = GetHitUnit().GetVehicleKit();

		if (vehicle != null)
		{
			var passenger = vehicle.GetPassenger(_seatId);

			passenger?.ExitVehicle();
		}
	}
}