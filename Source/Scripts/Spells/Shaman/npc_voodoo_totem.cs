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
			me.CastSpell(null, TotemSpells.SPELL_TOTEM_VOODOO_AT, true);
		}
	}
}