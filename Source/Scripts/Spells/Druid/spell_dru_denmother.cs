// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(201522)]
public class spell_dru_denmother : SpellScript, ISpellOnHit
{
	private const int DEN_MOTHER = 201522;
	private const int DEN_MOTHER_IRONFUR = 201629;

	public void OnHit()
	{
		var _player = GetCaster().ToPlayer();

		if (_player != null)
			if (_player.HasAura(DEN_MOTHER))
			{
				var validTargets = new List<Unit>();
				var groupList    = new List<Unit>();

				_player.GetPartyMembers(groupList);

				if (groupList.Count == 0)
					return;

				foreach (var itr in groupList)
					if ((itr.GetGUID() != _player.GetGUID()) && (itr.IsInRange(_player, 0, 50, true)))
						validTargets.Add(itr.ToUnit());

				if (validTargets.Count == 0)
					return;

				validTargets.Sort(new HealthPctOrderPred());
				var lowTarget = validTargets.First();

				_player.CastSpell(lowTarget, 201629, true);
			}
	}
}