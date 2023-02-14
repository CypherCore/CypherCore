// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 48681 - Summon Silverbrook Worgen
internal class spell_q12308_escape_from_silverbrook_summon_worgen : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new DestinationTargetSelectHandler(ModDest, 0, Targets.DestCasterSummon));
	}

	private void ModDest(ref SpellDestination dest)
	{
		var dist  = GetEffectInfo(0).CalcRadius(GetCaster());
		var angle = RandomHelper.FRand(0.75f, 1.25f) * MathFunctions.PI;

		var pos = GetCaster().GetNearPosition(dist, angle);
		dest.Relocate(pos);
	}
}