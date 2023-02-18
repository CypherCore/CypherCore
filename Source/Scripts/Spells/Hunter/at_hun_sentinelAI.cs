// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_sentinelAI : AreaTriggerAI
{
	public at_hun_sentinelAI(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnCreate()
	{
		timeInterval = 6000;
	}

	public int timeInterval;

	public override void OnUpdate(uint diff)
	{
		timeInterval += (int)diff;

		if (timeInterval < 6000)
			return;

		var caster = at.GetCaster();

		if (caster != null)
		{
			var targetList = new List<Unit>();
			var radius     = Global.SpellMgr.GetSpellInfo(HunterSpells.SENTINEL, Difficulty.None).GetEffect(0).CalcRadius(caster);

			var l_Check    = new AnyUnitInObjectRangeCheck(at, radius);
			var l_Searcher = new UnitListSearcher(at, targetList, l_Check, GridType.All);
			Cell.VisitGrid(at, l_Searcher, radius);

			foreach (var l_Unit in targetList)

			{
				caster.CastSpell(l_Unit, HunterSpells.HUNTERS_MARK_AURA, true);
				caster.CastSpell(caster, HunterSpells.HUNTERS_MARK_AURA_2, true);

				timeInterval -= 6000;
			}
		}
	}

	public override void OnRemove()
	{
		var caster = at.GetCaster();

		if (caster != null)
		{
			var targetList = new List<Unit>();
			var radius     = Global.SpellMgr.GetSpellInfo(HunterSpells.SENTINEL, Difficulty.None).GetEffect(0).CalcRadius(caster);

			var l_Check    = new AnyUnitInObjectRangeCheck(at, radius);
			var l_Searcher = new UnitListSearcher(at, targetList, l_Check, GridType.All);
			Cell.VisitGrid(at, l_Searcher, radius);

			foreach (var l_Unit in targetList)
				if (l_Unit != caster && caster.IsValidAttackTarget(l_Unit))
				{
					caster.CastSpell(l_Unit, HunterSpells.HUNTERS_MARK_AURA, true);
					caster.CastSpell(caster, HunterSpells.HUNTERS_MARK_AURA_2, true);

					timeInterval -= 6000;
				}
		}
	}
}