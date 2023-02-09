using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 49370 - Wyrmrest Defender: Destabilize Azure Dragonshrine Effect
internal class spell_q12372_destabilize_azure_dragonshrine_dummy : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		if (GetHitCreature())
		{
			Unit caster = GetOriginalCaster();

			if (caster)
			{
				Vehicle vehicle = caster.GetVehicleKit();

				if (vehicle)
				{
					Unit passenger = vehicle.GetPassenger(0);

					if (passenger)
					{
						Player player = passenger.ToPlayer();

						if (player)
							player.KilledMonsterCredit(CreatureIds.WyrmrestTempleCredit);
					}
				}
			}
		}
	}
}