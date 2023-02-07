using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Mage;

[Script]
public class at_mage_cinderstorm : AreaTriggerAI
{
	public at_mage_cinderstorm(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit unit)
	{
		Unit caster = at.GetCaster();
		if (caster != null)
		{
			if (caster.IsValidAttackTarget(unit))
			{
				caster.CastSpell(unit, MageSpells.SPELL_MAGE_CINDERSTORM_DMG, true);
			}
		}
	}
}