// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior
{
	// 203179 - Opportunity Strike
	[SpellScript(203179)]
	public class spell_warr_opportunity_strike : AuraScript, IAuraOnProc
	{
		public void OnProc(ProcEventInfo eventInfo)
		{
			if (!GetCaster())
				return;

			if (eventInfo?.GetDamageInfo()?.GetSpellInfo() != null && eventInfo.GetDamageInfo().GetSpellInfo().Id == WarriorSpells.OPPORTUNITY_STRIKE_DAMAGE)
				return;

			var target = eventInfo.GetActionTarget();

			if (target != null)
			{
				var _player = GetCaster().ToPlayer();

				if (_player != null)
				{
					var aur = GetAura();

					if (aur != null)
					{
						var eff = aur.GetEffect(0);

						if (eff != null)
							if (RandomHelper.randChance(eff.GetAmount()))
								_player.CastSpell(target, WarriorSpells.OPPORTUNITY_STRIKE_DAMAGE, true);
					}
				}
			}
		}
	}
}