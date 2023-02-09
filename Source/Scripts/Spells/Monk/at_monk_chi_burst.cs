using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Monk;

[Script]
public class at_monk_chi_burst : AreaTriggerAI
{
	public at_monk_chi_burst(AreaTrigger at) : base(at)
	{
	}

	public override void OnUnitEnter(Unit target)
	{
		if (!at.GetCaster())
		{
			return;
		}

		if (at.GetCaster().IsValidAssistTarget(target))
		{
			at.GetCaster().CastSpell(target, MonkSpells.SPELL_MONK_CHI_BURST_HEAL, true);
		}

		if (at.GetCaster().IsValidAttackTarget(target))
		{
			at.GetCaster().CastSpell(target, MonkSpells.SPELL_MONK_CHI_BURST_DAMAGE, true);
		}
	}
}