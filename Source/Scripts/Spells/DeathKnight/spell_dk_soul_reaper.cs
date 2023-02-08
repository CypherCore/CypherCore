using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(343294)]
public class spell_dk_soul_reaper : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void HandlePeriodic(AuraEffect UnnamedParameter)
	{
		if (GetCaster() && GetTarget() && GetTarget().IsDead())
		{
			GetCaster().CastSpell(DeathKnightSpells.SPELL_DK_SOUL_REAPER_MOD_HASTE, true);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDamage));
	}
}