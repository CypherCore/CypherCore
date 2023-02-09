using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DeathKnight;

[Script]
public class at_dk_antimagic_zone : AreaTriggerAI
{
	public at_dk_antimagic_zone(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit unit)
	{
		//TODO: Improve unit targets
		if (unit.IsPlayer() && !unit.IsHostileTo(at.GetCaster()))
			if (!unit.HasAura(DeathKnightSpells.SPELL_DK_ANTIMAGIC_ZONE_DAMAGE_TAKEN))
				unit.AddAura(DeathKnightSpells.SPELL_DK_ANTIMAGIC_ZONE_DAMAGE_TAKEN, unit);
	}

	public override void OnUnitExit(Unit unit)
	{
		if (unit.HasAura(DeathKnightSpells.SPELL_DK_ANTIMAGIC_ZONE_DAMAGE_TAKEN))
			unit.RemoveAura(DeathKnightSpells.SPELL_DK_ANTIMAGIC_ZONE_DAMAGE_TAKEN);
	}
}