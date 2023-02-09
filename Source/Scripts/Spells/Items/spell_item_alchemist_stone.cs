using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 17619 - Alchemist Stone
internal class spell_item_alchemist_stone : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.AlchemistStoneExtraHeal, ItemSpellIds.AlchemistStoneExtraMana);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetDamageInfo().GetSpellInfo().SpellFamilyName == SpellFamilyNames.Potion;
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		uint spellId = 0;
		var  amount  = (int)(eventInfo.GetDamageInfo().GetDamage() * 0.4f);

		if (eventInfo.GetDamageInfo().GetSpellInfo().HasEffect(SpellEffectName.Heal))
			spellId = ItemSpellIds.AlchemistStoneExtraHeal;
		else if (eventInfo.GetDamageInfo().GetSpellInfo().HasEffect(SpellEffectName.Energize))
			spellId = ItemSpellIds.AlchemistStoneExtraMana;

		if (spellId == 0)
			return;

		var                caster = eventInfo.GetActionTarget();
		CastSpellExtraArgs args   = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, amount);
		caster.CastSpell((Unit)null, spellId, args);
	}
}