using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(MonkSpells.SPELL_MONK_FISTS_OF_FURY)]
public class spell_monk_fists_of_fury : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void HandlePeriodic(AuraEffect aurEff)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		if (aurEff.GetTickNumber() % 6 == 0)
		{
			caster.CastSpell(GetTarget(), MonkSpells.SPELL_MONK_FISTS_OF_FURY_DAMAGE, true);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 2, AuraType.PeriodicDummy));
	}
}