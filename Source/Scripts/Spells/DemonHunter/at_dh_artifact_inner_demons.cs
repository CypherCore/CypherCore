// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DemonHunter;

[Script]
public class at_dh_artifact_inner_demons : AreaTriggerAI
{
	public at_dh_artifact_inner_demons(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnInitialize()
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		var guid   = caster.VariableStorage.GetValue<ObjectGuid>("Spells.InnerDemonsTarget", ObjectGuid.Empty);
		var target = ObjectAccessor.Instance.GetUnit(caster, guid);

		if (target != null)
		{
			List<Vector3> splinePoints = new();
			var           orientation  = caster.GetOrientation();
			var           posX         = caster.GetPositionX() - 7 * (float)Math.Cos(orientation);
			var           posY         = caster.GetPositionY() - 7 * (float)Math.Sin(orientation); // Start from behind the caster
			splinePoints.Add(new Vector3(posX, posY, caster.GetPositionZ()));
			splinePoints.Add(new Vector3(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ()));

			at.InitSplines(splinePoints, 1000);
		}
		else
		{
			caster.VariableStorage.Remove("Spells.InnerDemonsTarget");
		}
	}

	public override void OnRemove()
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		caster.CastSpell(at, DemonHunterSpells.INNER_DEMONS_DAMAGE, true);
	}
}