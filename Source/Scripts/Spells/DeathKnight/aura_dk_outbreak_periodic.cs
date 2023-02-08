using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(196782)]
public class aura_dk_outbreak_periodic : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void HandleDummyTick(AuraEffect UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			List<Unit> friendlyUnits = new List<Unit>();
			GetTarget().GetFriendlyUnitListInRange(friendlyUnits, 10.0f);

			foreach (Unit unit in friendlyUnits)
			{
				if (!unit.HasUnitFlag(UnitFlags.ImmuneToPc) && unit.IsInCombatWith(caster))
				{
					caster.CastSpell(unit, DeathKnightSpells.SPELL_DK_VIRULENT_PLAGUE, true);
				}
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleDummyTick, 0, AuraType.PeriodicDummy));
	}
}