using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Druid;

[Script]
public class at_dru_lunar_beam : AreaTriggerAI
{
	public at_dru_lunar_beam(AreaTrigger at) : base(at)
	{
	}

	public override void OnCreate()
	{
		at.SetPeriodicProcTimer(1000);
	}

	public override void OnPeriodicProc()
	{
		if (at.GetCaster())
		{
			at.GetCaster().CastSpell(at.GetPosition(), DruidSpells.SPELL_DRU_LUNAR_BEAM_DAMAGE_HEAL, true);
		}
	}
}