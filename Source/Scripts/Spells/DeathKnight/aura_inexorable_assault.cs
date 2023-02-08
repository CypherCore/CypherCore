using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(253593)]
public class aura_inexorable_assault : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


	private void OnPeriodic(AuraEffect UnnamedParameter)
	{
		if (GetCaster())
		{
			GetCaster().CastSpell(null, DeathKnightSpells.SPELL_DK_INEXORABLE_ASSAULT_STACK, true);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
	}
}