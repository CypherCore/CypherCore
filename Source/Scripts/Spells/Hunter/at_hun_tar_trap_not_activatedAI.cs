using System;
using Framework.Constants;
using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_tar_trap_not_activatedAI : AreaTriggerAI
{
	public int timeInterval;

	public enum UsedSpells
	{
		SPELL_HUNTER_ACTIVATE_TAR_TRAP = 187700
	}

	public at_hun_tar_trap_not_activatedAI(AreaTrigger areatrigger) : base(areatrigger)
	{
		timeInterval = 200;
	}

	public override void OnCreate()
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		if (!caster.ToPlayer())
			return;

		foreach (var itr in at.GetInsideUnits())
		{
			var target = ObjectAccessor.Instance.GetUnit(caster, itr);

			if (!caster.IsFriendlyTo(target))
			{
				var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, at.GetPosition(), TempSummonType.TimedDespawn, TimeSpan.FromMinutes(1));

				if (tempSumm != null)
				{
					tempSumm.SetFaction(caster.GetFaction());
					tempSumm.SetSummonerGUID(caster.GetGUID());
					PhasingHandler.InheritPhaseShift(tempSumm, caster);
					caster.CastSpell(tempSumm, UsedSpells.SPELL_HUNTER_ACTIVATE_TAR_TRAP, true);
					at.Remove();
				}
			}
		}
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.ToPlayer())
			return;

		if (!caster.IsFriendlyTo(unit))
		{
			var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, at.GetPosition(), TempSummonType.TimedDespawn, TimeSpan.FromMinutes(1));

			if (tempSumm != null)
			{
				tempSumm.SetFaction(caster.GetFaction());
				tempSumm.SetSummonerGUID(caster.GetGUID());
				PhasingHandler.InheritPhaseShift(tempSumm, caster);
				caster.CastSpell(tempSumm, UsedSpells.SPELL_HUNTER_ACTIVATE_TAR_TRAP, true);
				at.Remove();
			}
		}
	}
}