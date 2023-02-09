using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // Warlock T5 2P Bonus
internal class spell_item_pet_healing : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.HealthLink);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		DamageInfo damageInfo = eventInfo.GetDamageInfo();

		if (damageInfo == null ||
		    damageInfo.GetDamage() == 0)
			return;

		CastSpellExtraArgs args = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount()));
		eventInfo.GetActor().CastSpell((Unit)null, ItemSpellIds.HealthLink, args);
	}
}