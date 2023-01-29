using Game.Entities;
using Game.Spells;

namespace Game.AI;

public class TriggerAI : NullCreatureAI
{
	public TriggerAI(Creature c) : base(c)
	{
	}

	public override void IsSummonedBy(WorldObject summoner)
	{
		if (me.Spells[0] != 0)
		{
			CastSpellExtraArgs extra = new();
			extra.OriginalCaster = summoner.GetGUID();
			me.CastSpell(me, me.Spells[0], extra);
		}
	}
}