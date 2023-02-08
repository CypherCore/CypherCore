using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(152280)]
public class aura_dk_defile : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void HandlePeriodic(AuraEffect UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			foreach (AreaTrigger at in caster.GetAreaTriggers(GetId()))
			{
				if (at.GetInsideUnits().Count != 0)
				{
					caster.CastSpell(caster, DeathKnightSpells.SPELL_DK_DEFILE_MASTERY, true);
				}
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 2, AuraType.PeriodicDummy));
	}
}