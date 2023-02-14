// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
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
			var caster = GetOriginalCaster();

			if (caster)
			{
				var vehicle = caster.GetVehicleKit();

				if (vehicle)
				{
					var passenger = vehicle.GetPassenger(0);

					if (passenger)
					{
						var player = passenger.ToPlayer();

						if (player)
							player.KilledMonsterCredit(CreatureIds.WyrmrestTempleCredit);
					}
				}
			}
		}
	}
}