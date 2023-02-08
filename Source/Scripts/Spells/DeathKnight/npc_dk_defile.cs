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