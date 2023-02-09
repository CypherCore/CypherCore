using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 59906 - Swift Hand of Justice Dummy
internal class spell_item_swift_hand_justice_dummy : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.SwiftHandOfJusticeHeal);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var                caster = eventInfo.GetActor();
		CastSpellExtraArgs args   = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)caster.CountPctFromMaxHealth(aurEff.GetAmount()));
		caster.CastSpell((Unit)null, ItemSpellIds.SwiftHandOfJusticeHeal, args);
	}
}