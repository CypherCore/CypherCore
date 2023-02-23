// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(115072)]
public class spell_monk_expel_harm : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		if (!GetCaster())
			return;

		var _player = GetCaster().ToPlayer();

		if (_player != null)
		{
			var targetList = new List<Unit>();
			_player.GetAttackableUnitListInRange(targetList, 10.0f);

			foreach (var itr in targetList)
				if (_player.IsValidAttackTarget(itr))
				{
					double bp   = MathFunctions.CalculatePct((-GetHitDamage()), 50);
					var   args = new CastSpellExtraArgs();
					args.AddSpellMod(SpellValueMod.BasePoint0, (int)bp);
					args.SetTriggerFlags(TriggerCastFlags.FullMask);
					_player.CastSpell(itr, 115129, args);
				}
		}
	}
}