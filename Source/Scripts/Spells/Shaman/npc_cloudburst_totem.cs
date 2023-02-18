// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman
{
	//NPC ID : 78001
	[CreatureScript(78001)]
	public class npc_cloudburst_totem : ScriptedAI
	{
		public npc_cloudburst_totem(Creature creature) : base(creature)
		{
		}

		public override void Reset()
		{
			if (me.GetOwner())
				me.CastSpell(me.GetOwner(), TotemSpells.TOTEM_CLOUDBURST_EFFECT, true);
		}
	}
}