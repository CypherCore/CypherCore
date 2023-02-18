// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman
{
	//NPC ID : 100099
	//NPC NAME : Voodoo Totem
	[CreatureScript(100099)]
	public class npc_voodoo_totem : ScriptedAI
	{
		public npc_voodoo_totem(Creature creature) : base(creature)
		{
		}

		public override void Reset()
		{
			me.CastSpell(null, TotemSpells.TOTEM_VOODOO_AT, true);
		}
	}
}