// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman
{
	// 6826
	[Script]
	public class bfa_at_crashing_storm : AreaTriggerAI
	{
		public bfa_at_crashing_storm(AreaTrigger areatrigger) : base(areatrigger)
		{
		}

		public uint damageTimer;

		public override void OnInitialize()
		{
			damageTimer = 0;
		}

		public override void OnUpdate(uint diff)
		{
			damageTimer += diff;

			if (damageTimer >= 2 * Time.InMilliseconds)
			{
				CheckPlayers();
				damageTimer = 0;
			}
		}

		public void CheckPlayers()
		{
			var caster = at.GetCaster();

			if (caster != null)
			{
				var radius = 2.5f;

				var targetList = caster.GetPlayerListInGrid(radius);

				if (targetList.Count != 0)
					foreach (Player player in targetList)
						if (!player.IsGameMaster())
							caster.CastSpell(player, ShamanSpells.CRASHING_STORM_TALENT_DAMAGE, true);
			}
		}
	}
}