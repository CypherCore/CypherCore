using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 55421 - Gymer's Throw
internal class spell_q12919_gymers_throw : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		Unit caster = GetCaster();

		if (caster.IsVehicle())
		{
			Unit passenger = caster.GetVehicleKit().GetPassenger(1);

			if (passenger)
			{
				passenger.ExitVehicle();
				caster.CastSpell(passenger, QuestSpellIds.VargulExplosion, true);
			}
		}
	}
}