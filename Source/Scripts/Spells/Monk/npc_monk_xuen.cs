using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Monk;

[CreatureScript(63508)]
public class npc_monk_xuen : ScriptedAI
{
	public npc_monk_xuen(Creature creature) : base(creature)
	{
	}

	public override void IsSummonedBy(WorldObject UnnamedParameter)
	{
		me.CastSpell(me, MonkSpells.SPELL_MONK_XUEN_AURA, true);
	}
}