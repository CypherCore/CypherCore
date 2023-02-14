// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] //  8382 - AreaTriggerId
internal class areatrigger_sha_earthquake : AreaTriggerAI
{
	private readonly HashSet<ObjectGuid> _stunnedUnits = new();
	private TimeSpan _period;
	private TimeSpan _refreshTimer;

	public areatrigger_sha_earthquake(AreaTrigger areatrigger) : base(areatrigger)
	{
		_refreshTimer = TimeSpan.Zero;
		_period       = TimeSpan.FromSeconds(1);
	}

	public override void OnCreate()
	{
		var caster = at.GetCaster();

		if (caster != null)
		{
			var earthquake = caster.GetAuraEffect(ShamanSpells.Earthquake, 1);

			if (earthquake != null)
				_period = TimeSpan.FromMilliseconds(earthquake.GetPeriod());
		}
	}

	public override void OnUpdate(uint diff)
	{
		_refreshTimer -= TimeSpan.FromMilliseconds(diff);

		while (_refreshTimer <= TimeSpan.Zero)
		{
			var caster = at.GetCaster();

			caster?.CastSpell(at.GetPosition(),
			                  ShamanSpells.EarthquakeTick,
			                  new CastSpellExtraArgs(TriggerCastFlags.FullMask)
				                  .SetOriginalCaster(at.GetGUID()));

			_refreshTimer += _period;
		}
	}

	// Each Target can only be stunned once by each earthquake - keep track of who we already stunned
	public bool AddStunnedTarget(ObjectGuid guid)
	{
		return _stunnedUnits.Add(guid);
	}
}