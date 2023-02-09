using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman
{
	//NPC ID : 102392
	[CreatureScript(102392)]
	public class npc_resonance_totem : ScriptedAI
	{
		public npc_resonance_totem(Creature creature) : base(creature)
		{
		}

		public override void Reset()
		{
			var time = TimeSpan.FromSeconds(1);

			me.m_Events.AddRepeatEventAtOffset(() =>
			                                   {
				                                   me.CastSpell(me, TotemSpells.SPELL_TOTEM_RESONANCE_EFFECT, true);

				                                   return time;
			                                   },
			                                   time);
		}
	}
}