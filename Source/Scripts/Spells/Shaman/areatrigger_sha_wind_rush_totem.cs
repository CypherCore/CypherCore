using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman;

[Script] //  12676 - AreaTriggerId
internal class areatrigger_sha_wind_rush_totem : AreaTriggerAI
{
	private static readonly int REFRESH_TIME = 4500;

	private int _refreshTimer;

	public areatrigger_sha_wind_rush_totem(AreaTrigger areatrigger) : base(areatrigger)
	{
		_refreshTimer = REFRESH_TIME;
	}

	public override void OnUpdate(uint diff)
	{
		_refreshTimer -= (int)diff;

		if (_refreshTimer <= 0)
		{
			var caster = at.GetCaster();

			if (caster != null)
				foreach (var guid in at.GetInsideUnits())
				{
					var unit = Global.ObjAccessor.GetUnit(caster, guid);

					if (unit != null)
					{
						if (!caster.IsFriendlyTo(unit))
							continue;

						caster.CastSpell(unit, ShamanSpells.WindRush, true);
					}
				}

			_refreshTimer += REFRESH_TIME;
		}
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster != null)
		{
			if (!caster.IsFriendlyTo(unit))
				return;

			caster.CastSpell(unit, ShamanSpells.WindRush, true);
		}
	}
}