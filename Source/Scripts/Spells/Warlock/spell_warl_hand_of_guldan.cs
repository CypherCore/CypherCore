// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Hand of Gul'Dan - 105174
	[SpellScript(105174)]
	public class spell_warl_hand_of_guldan : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var caster = GetCaster();

			if (caster != null)
			{
				var target = GetHitUnit();

				if (target != null)
				{
					var nrofsummons = 1;
					nrofsummons += caster.GetPower(PowerType.SoulShards);

					if (nrofsummons > 4)
						nrofsummons = 4;

					sbyte[] offsetX =
					{
						0, 0, 1, 1
					};

					sbyte[] offsetY =
					{
						0, 1, 0, 1
					};

					for (var i = 0; i < nrofsummons; i++)
						caster.CastSpell(new Position(target.GetPositionX() + offsetX[i], target.GetPositionY() + offsetY[i], target.GetPositionZ()), 104317, true);

					caster.CastSpell(target, WarlockSpells.HAND_OF_GULDAN_DAMAGE, true);
				}
			}
		}
	}
}