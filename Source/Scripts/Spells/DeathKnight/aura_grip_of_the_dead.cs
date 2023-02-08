using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(273980)]
public class aura_grip_of_the_dead : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


	private void OnTick(AuraEffect UnnamedParameter)
	{
		Unit target = GetTarget();
		if (target != null)
		{
			Unit caster = GetCaster();
			if (caster != null)
			{
				caster.CastSpell(target, DeathKnightSpells.SPELL_DK_GRIP_OF_THE_DEAD_SLOW, true);
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDummy));
	}
}