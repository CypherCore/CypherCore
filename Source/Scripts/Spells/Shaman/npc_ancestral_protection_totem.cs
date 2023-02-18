// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman
{
	[CreatureScript(78001)]
	//104818 - Ancestral Protection Totem
	public class npc_ancestral_protection_totem : ScriptedAI
	{
		public npc_ancestral_protection_totem(Creature creature) : base(creature)
		{
		}

		public override void Reset()
		{
			me.CastSpell(me, TotemSpells.TOTEM_ANCESTRAL_PROTECTION_AT, true);
		}
	}
}