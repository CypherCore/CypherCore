using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 71875, 71877 - Item - Black Bruise: Necrotic Touch Proc
internal class spell_item_necrotic_touch : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.ItemNecroticTouchProc);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetProcTarget() && eventInfo.GetProcTarget().IsAlive();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var damageInfo = eventInfo.GetDamageInfo();

		if (damageInfo == null ||
		    damageInfo.GetDamage() == 0)
			return;

		CastSpellExtraArgs args = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount()));
		GetTarget().CastSpell((Unit)null, ItemSpellIds.ItemNecroticTouchProc, args);
	}
}