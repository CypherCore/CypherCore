// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Monk;

[CreatureScript(63508)]
public class npc_monk_xuen : ScriptedAI
{
	public npc_monk_xuen(Creature creature) : base(creature)
	{
	}

	public override void IsSummonedBy(WorldObject UnnamedParameter)
	{
		me.CastSpell(me, MonkSpells.XUEN_AURA, true);
	}
}