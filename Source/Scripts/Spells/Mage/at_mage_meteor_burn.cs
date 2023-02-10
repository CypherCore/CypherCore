using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Mage;

[Script]
public class at_mage_meteor_burn : AreaTriggerAI
{
	public at_mage_meteor_burn(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (caster.GetTypeId() != TypeId.Player)
			return;

		if (caster.IsValidAttackTarget(unit))
			caster.CastSpell(unit, MageSpells.SPELL_MAGE_METEOR_BURN, true);
	}

	public override void OnUnitExit(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (caster.GetTypeId() != TypeId.Player)
			return;

		var meteor = unit.GetAura(MageSpells.SPELL_MAGE_METEOR_BURN, caster.GetGUID());

		if (meteor != null)
			meteor.SetDuration(0);
	}
}