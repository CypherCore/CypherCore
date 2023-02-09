using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 58886 - Food
internal class spell_magic_eater_food : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleTriggerSpell, 1, AuraType.PeriodicTriggerSpell));
	}

	private void HandleTriggerSpell(AuraEffect aurEff)
	{
		PreventDefaultAction();
		var target = GetTarget();

		switch (RandomHelper.URand(0, 5))
		{
			case 0:
				target.CastSpell(target, ItemSpellIds.WildMagic, true);

				break;
			case 1:
				target.CastSpell(target, ItemSpellIds.WellFed1, true);

				break;
			case 2:
				target.CastSpell(target, ItemSpellIds.WellFed2, true);

				break;
			case 3:
				target.CastSpell(target, ItemSpellIds.WellFed3, true);

				break;
			case 4:
				target.CastSpell(target, ItemSpellIds.WellFed4, true);

				break;
			case 5:
				target.CastSpell(target, ItemSpellIds.WellFed5, true);

				break;
		}
	}
}