using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Mage;

[Script]
public class at_mage_arcane_orb : AreaTriggerAI
{
	public at_mage_arcane_orb(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster != null)
			if (caster.IsValidAttackTarget(unit))
				caster.CastSpell(unit, MageSpells.SPELL_MAGE_ARCANE_ORB_DAMAGE, true);
	}
}