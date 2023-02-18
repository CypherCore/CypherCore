// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman
{
	//5925
	[CreatureScript(5925)]
	public class npc_grounding_totem : ScriptedAI
	{
		public npc_grounding_totem(Creature creature) : base(creature)
		{
		}

		public override void Reset()
		{
			me.CastSpell(me, TotemSpells.TOTEM_GROUDING_TOTEM_EFFECT, true);
		}
	}
}