using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DemonHunter;

[Script]
public class at_demon_hunter_demonic_trample : AreaTriggerAI
{
	public at_demon_hunter_demonic_trample(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (caster.IsValidAttackTarget(unit))
		{
			caster.CastSpell(unit, DemonHunterSpells.SPELL_DH_DEMONIC_TRAMPLE_STUN, true);
			caster.CastSpell(unit, DemonHunterSpells.SPELL_DH_DEMONIC_TRAMPLE_DAMAGE, true);
		}
	}
}