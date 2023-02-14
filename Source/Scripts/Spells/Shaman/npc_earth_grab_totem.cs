// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman
{
	//60561
	[CreatureScript(60561)]
	public class npc_earth_grab_totem : ScriptedAI
	{
		public npc_earth_grab_totem(Creature creature) : base(creature)
		{
		}

		public List<ObjectGuid> alreadyRooted = new();

		public override void Reset()
		{
			var time = TimeSpan.FromSeconds(2);

			me.m_Events.AddRepeatEventAtOffset(() =>
			                                   {
				                                   var unitList = new List<Unit>();
				                                   me.GetAttackableUnitListInRange(unitList, 10.0f);

				                                   foreach (var target in unitList)
				                                   {
					                                   if (target.HasAura(TotemSpells.SPELL_TOTEM_EARTH_GRAB_ROOT_EFFECT))
						                                   continue;

					                                   if (!alreadyRooted.Contains(target.GetGUID()))
					                                   {
						                                   alreadyRooted.Add(target.GetGUID());
						                                   me.CastSpell(target, TotemSpells.SPELL_TOTEM_EARTH_GRAB_ROOT_EFFECT, true);
					                                   }
					                                   else
					                                   {
						                                   me.CastSpell(target, TotemSpells.SPELL_TOTEM_EARTH_GRAB_SLOW_EFFECT, true);
					                                   }
				                                   }

				                                   return time;
			                                   },
			                                   time);
		}
	}
}