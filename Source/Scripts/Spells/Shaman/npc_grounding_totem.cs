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
			me.CastSpell(me, TotemSpells.SPELL_TOTEM_GROUDING_TOTEM_EFFECT, true);
		}
	}
}