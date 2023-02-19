// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman
{
	//192077 - Wind Rush Totem
	//97285 - NPC ID
	[CreatureScript(97285)]
	public class npc_wind_rush_totem : ScriptedAI
	{
		public npc_wind_rush_totem(Creature creature) : base(creature)
		{
		}

		public override void Reset()
		{
			var time = TimeSpan.FromSeconds(1);

			me.m_Events.AddRepeatEventAtOffset(() =>
			                                   {
				                                   me.CastSpell(me, TotemSpells.TOTEM_WIND_RUSH_EFFECT, true);

				                                   return time;
			                                   },
			                                   time);
		}
	}
}