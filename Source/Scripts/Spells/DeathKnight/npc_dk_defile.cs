// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DeathKnight;

[Script]
public class npc_dk_defile : ScriptedAI
{
	public npc_dk_defile(Creature creature) : base(creature)
	{
		SetCombatMovement(false);
		me.SetReactState(ReactStates.Passive);
		me.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.NonAttackable);
		me.AddUnitState(UnitState.Root);
	}

	public override void Reset()
	{
		me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(11));
	}
}