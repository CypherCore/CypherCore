using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Monk;

[Script]
public class at_monk_ring_of_peace : AreaTriggerAI
{
	public at_monk_ring_of_peace(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit target)
	{
		if (at.GetCaster())
			if (at.GetCaster().IsValidAttackTarget(target))
				target.CastSpell(target, MonkSpells.SPELL_MONK_RING_OF_PEACE_KNOCKBACK, true);
	}
}