// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Mage;

[Script]
public class at_mage_flame_patch : AreaTriggerAI
{
	public at_mage_flame_patch(AreaTrigger areatrigger) : base(areatrigger)
	{
	}


	public override void OnCreate()
	{
		timeInterval = 1000;
	}

	public int timeInterval;

	public override void OnUpdate(uint diff)
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		if (caster.GetTypeId() != TypeId.Player)
			return;

		timeInterval += (int)diff;

		if (timeInterval < 1000)
			return;

		caster.CastSpell(at.GetPosition(), MageSpells.FLAME_PATCH_AOE_DMG, true);

		timeInterval -= 1000;
	}
}