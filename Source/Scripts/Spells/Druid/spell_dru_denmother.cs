using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(201522)]
public class spell_dru_denmother : SpellScript, ISpellOnHit
{
	private const int SPELL_DRUID_DEN_MOTHER = 201522;
	private const int SPELL_DRUID_DEN_MOTHER_IRONFUR = 201629;

	public void OnHit()
	{
		Player _player = GetCaster().ToPlayer();
		if (_player != null)
		{
			if (_player.HasAura(SPELL_DRUID_DEN_MOTHER))
			{
				List<Unit> validTargets = new List<Unit>();
				List<Unit> groupList    = new List<Unit>();

				_player.GetPartyMembers(groupList);

				if (groupList.Count == 0)
				{
					return;
				}

				foreach (var itr in groupList)
				{
					if ((itr.GetGUID() != _player.GetGUID()) && (itr.IsInRange(_player, 0, 50, true)))
					{
						validTargets.Add(itr.ToUnit());
					}
				}

				if (validTargets.Count == 0)
				{
					return;
				}

				validTargets.Sort(new HealthPctOrderPred());
				var lowTarget = validTargets.First();

				_player.CastSpell(lowTarget, 201629, true);
			}
		}
	}
}