using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Movement;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 110744 - Divine Star
internal class areatrigger_pri_divine_star : AreaTriggerAI
{
	private readonly List<ObjectGuid> _affectedUnits = new();
	private readonly TaskScheduler _scheduler = new();
	private Position _casterCurrentPosition = new();

	public areatrigger_pri_divine_star(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnInitialize()
	{
		Unit caster = at.GetCaster();

		if (caster != null)
		{
			_casterCurrentPosition = caster.GetPosition();

			// Note: max. distance at which the Divine Star can travel to is 24 yards.
			float divineStarXOffSet = 24.0f;

			Position destPos = _casterCurrentPosition;
			at.MovePositionToFirstCollision(destPos, divineStarXOffSet, 0.0f);

			PathGenerator firstPath = new(at);
			firstPath.CalculatePath(destPos.GetPositionX(), destPos.GetPositionY(), destPos.GetPositionZ(), false);

			Vector3 endPoint = firstPath.GetPath().Last();

			// Note: it takes 1000ms to reach 24 yards, so it takes 41.67ms to run 1 yard.
			at.InitSplines(firstPath.GetPath().ToList(), (uint)(at.GetDistance(endPoint.X, endPoint.Y, endPoint.Z) * 41.67f));
		}
	}

	public override void OnUpdate(uint diff)
	{
		_scheduler.Update(diff);
	}

	public override void OnUnitEnter(Unit unit)
	{
		Unit caster = at.GetCaster();

		if (caster != null)
			if (!_affectedUnits.Contains(unit.GetGUID()))
			{
				if (caster.IsValidAttackTarget(unit))
					caster.CastSpell(unit, PriestSpells.DivineStarDamage, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
				else if (caster.IsValidAssistTarget(unit))
					caster.CastSpell(unit, PriestSpells.DivineStarHeal, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));

				_affectedUnits.Add(unit.GetGUID());
			}
	}

	public override void OnUnitExit(Unit unit)
	{
		// Note: this ensures any unit receives a second hit if they happen to be inside the AT when Divine Star starts its return path.
		Unit caster = at.GetCaster();

		if (caster != null)
			if (!_affectedUnits.Contains(unit.GetGUID()))
			{
				if (caster.IsValidAttackTarget(unit))
					caster.CastSpell(unit, PriestSpells.DivineStarDamage, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
				else if (caster.IsValidAssistTarget(unit))
					caster.CastSpell(unit, PriestSpells.DivineStarHeal, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));

				_affectedUnits.Add(unit.GetGUID());
			}
	}

	public override void OnDestinationReached()
	{
		Unit caster = at.GetCaster();

		if (caster == null)
			return;

		if (at.GetDistance(_casterCurrentPosition) > 0.05f)
		{
			_affectedUnits.Clear();

			ReturnToCaster();
		}
		else
		{
			at.Remove();
		}
	}

	private void ReturnToCaster()
	{
		_scheduler.Schedule(TimeSpan.FromMilliseconds(0),
		                    task =>
		                    {
			                    Unit caster = at.GetCaster();

			                    if (caster != null)
			                    {
				                    _casterCurrentPosition = caster.GetPosition();

				                    List<Vector3> returnSplinePoints = new();

				                    returnSplinePoints.Add(at.GetPosition());
				                    returnSplinePoints.Add(at.GetPosition());
				                    returnSplinePoints.Add(caster.GetPosition());
				                    returnSplinePoints.Add(caster.GetPosition());

				                    at.InitSplines(returnSplinePoints, (uint)at.GetDistance(caster) / 24 * 1000);

				                    task.Repeat(TimeSpan.FromMilliseconds(250));
			                    }
		                    });
	}
}