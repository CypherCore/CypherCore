using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(115994)]
public class spell_dk_unholy_blight : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


	private void HandlePeriodic(AuraEffect UnnamedParameter)
	{
		return;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDamage));
	}
}