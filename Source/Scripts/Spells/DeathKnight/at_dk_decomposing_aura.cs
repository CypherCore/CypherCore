using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DeathKnight;

[Script]
public class at_dk_decomposing_aura : AreaTriggerAI
{
	public at_dk_decomposing_aura(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitExit(Unit unit)
	{
		unit.RemoveAurasDueToSpell(DeathKnightSpells.SPELL_DK_DECOMPOSING_AURA_DAMAGE, at.GetCasterGuid());
	}
}