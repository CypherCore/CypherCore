using Framework.Constants;
using Game.Entities;

namespace Game.AI;

public class VehicleAI : CreatureAI
{
	private const int VEHICLE_CONDITION_CHECK_TIME = 1000;
	private const int VEHICLE_DISMISS_TIME = 5000;
	private uint _conditionsTimer;
	private uint _dismissTimer;
	private bool _doDismiss;

	private bool _hasConditions;

	public VehicleAI(Creature creature) : base(creature)
	{
		_conditionsTimer = VEHICLE_CONDITION_CHECK_TIME;
		LoadConditions();
		_doDismiss    = false;
		_dismissTimer = VEHICLE_DISMISS_TIME;
	}

	public override void UpdateAI(uint diff)
	{
		CheckConditions(diff);

		if (_doDismiss)
		{
			if (_dismissTimer < diff)
			{
				_doDismiss = false;
				me.DespawnOrUnsummon();
			}
			else
			{
				_dismissTimer -= diff;
			}
		}
	}

	public override void MoveInLineOfSight(Unit who)
	{
	}

	public override void AttackStart(Unit victim)
	{
	}

	public override void OnCharmed(bool isNew)
	{
		bool charmed = me.IsCharmed();

		if (!me.GetVehicleKit().IsVehicleInUse() &&
		    !charmed &&
		    _hasConditions)    //was used and has conditions
			_doDismiss = true; //needs reset
		else if (charmed)
			_doDismiss = false; //in use again

		_dismissTimer = VEHICLE_DISMISS_TIME; //reset timer
	}

	private void LoadConditions()
	{
		_hasConditions = Global.ConditionMgr.HasConditionsForNotGroupedEntry(ConditionSourceType.CreatureTemplateVehicle, me.GetEntry());
	}

	private void CheckConditions(uint diff)
	{
		if (!_hasConditions)
			return;

		if (_conditionsTimer <= diff)
		{
			Vehicle vehicleKit = me.GetVehicleKit();

			if (vehicleKit)
				foreach (var pair in vehicleKit.Seats)
				{
					Unit passenger = Global.ObjAccessor.GetUnit(me, pair.Value.Passenger.Guid);

					if (passenger)
					{
						Player player = passenger.ToPlayer();

						if (player)
							if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.CreatureTemplateVehicle, me.GetEntry(), player, me))
							{
								player.ExitVehicle();

								return; //check other pessanger in next tick
							}
					}
				}

			_conditionsTimer = VEHICLE_CONDITION_CHECK_TIME;
		}
		else
		{
			_conditionsTimer -= diff;
		}
	}
}