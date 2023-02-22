// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 63471 -Spawn Blood Pool
internal class spell_spawn_blood_pool : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCaster));
	}

	private void SetDest(ref SpellDestination dest)
	{
		var        caster       = GetCaster();
		var        summonPos    = caster.GetPosition();
        LiquidData liquidStatus;
        if (caster.GetMap().GetLiquidStatus(caster.GetPhaseShift(), caster.GetPositionX(), caster.GetPositionY(), caster.GetPositionZ(), LiquidHeaderTypeFlags.AllLiquids, out liquidStatus, caster.GetCollisionHeight()) != ZLiquidStatus.NoWater)

            dest.Relocate(summonPos);
	}
}