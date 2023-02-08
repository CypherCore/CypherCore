using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 120517 - Halo
internal class areatrigger_pri_halo : AreaTriggerAI
{
	public areatrigger_pri_halo(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit unit)
	{
		Unit caster = at.GetCaster();

		if (caster != null)
		{
			if (caster.IsValidAttackTarget(unit))
				caster.CastSpell(unit, PriestSpells.HaloDamage, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
			else if (caster.IsValidAssistTarget(unit))
				caster.CastSpell(unit, PriestSpells.HaloHeal, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
		}
	}
}