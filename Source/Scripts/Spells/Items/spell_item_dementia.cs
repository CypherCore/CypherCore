using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 41404 - Dementia
internal class spell_item_dementia : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.DementiaPos, ItemSpellIds.DementiaNeg);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodicDummy, 0, AuraType.PeriodicDummy));
	}

	private void HandlePeriodicDummy(AuraEffect aurEff)
	{
		PreventDefaultAction();
		GetTarget().CastSpell(GetTarget(), RandomHelper.RAND(ItemSpellIds.DementiaPos, ItemSpellIds.DementiaNeg), new CastSpellExtraArgs(aurEff));
	}
}