// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 12601 - Second Chances: Summon Landgren's Soul Moveto Target Bunny
internal class spell_q12847_summon_soul_moveto_bunny : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCaster));
	}

	private void SetDest(ref SpellDestination dest)
	{
		// Adjust effect summon position
		Position offset = new(0.0f, 0.0f, 2.5f, 0.0f);
		dest.RelocateOffset(offset);
	}
}