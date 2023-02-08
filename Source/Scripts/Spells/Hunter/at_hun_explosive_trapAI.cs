using System;
using Framework.Constants;
using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_explosive_trapAI : AreaTriggerAI
{
	public int timeInterval;

	public enum UsedSpells
	{
		SPELL_HUNTER_EXPLOSIVE_TRAP_DAMAGE = 13812
	}

	public at_hun_explosive_trapAI(AreaTrigger areatrigger) : base(areatrigger)
	{
		timeInterval = 200;
	}

	public override void OnCreate()
	{
		Unit caster = at.GetCaster();

		if (caster == null)
		{
			return;
		}

		if (!caster.ToPlayer())
		{
			return;
		}

		foreach (var itr in at.GetInsideUnits())
		{
			Unit target = ObjectAccessor.Instance.GetUnit(caster, itr);
			if (!caster.IsFriendlyTo(target))
			{
				TempSummon tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, at.GetPosition(), TempSummonType.TimedDespawn, TimeSpan.FromSeconds(200));

				if (tempSumm != null)
				{
					tempSumm.SetFaction(caster.GetFaction());
					tempSumm.SetSummonerGUID(caster.GetGUID());
					PhasingHandler.InheritPhaseShift(tempSumm, caster);
					caster.CastSpell(tempSumm, UsedSpells.SPELL_HUNTER_EXPLOSIVE_TRAP_DAMAGE, true);
					at.Remove();
				}
			}
		}
	}

	public override void OnUnitEnter(Unit unit)
	{
		Unit caster = at.GetCaster();

		if (caster == null || unit == null)
		{
			return;
		}

		if (!caster.ToPlayer())
		{
			return;
		}

		if (!caster.IsFriendlyTo(unit))
		{
			TempSummon tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, at.GetPosition(), TempSummonType.TimedDespawn, TimeSpan.FromSeconds(200));
			if (tempSumm != null) 
			{
				tempSumm.SetFaction(caster.GetFaction());
				tempSumm.SetSummonerGUID(caster.GetGUID());
				PhasingHandler.InheritPhaseShift(tempSumm, caster);
				caster.CastSpell(tempSumm, UsedSpells.SPELL_HUNTER_EXPLOSIVE_TRAP_DAMAGE, true);
				at.Remove();
			}
		}
	}
}