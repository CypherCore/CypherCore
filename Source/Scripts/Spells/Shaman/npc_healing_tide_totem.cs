// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman
{
	//NPC ID : 59764
	[CreatureScript(59764)]
	public class npc_healing_tide_totem : ScriptedAI
	{
		public npc_healing_tide_totem(Creature creature) : base(creature)
		{
		}

		public override void Reset()
		{
			var time = TimeSpan.FromMilliseconds(1900);

			me.m_Events.AddRepeatEventAtOffset(() =>
			                                   {
				                                   me.CastSpell(me, TotemSpells.SPELL_TOTEM_HEALING_TIDE_EFFECT, true);

				                                   return time;
			                                   },
			                                   time);
		}
	}
}