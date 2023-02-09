using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 26467 - Persistent Shield
internal class spell_item_persistent_shield : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.PersistentShieldTriggered);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetHealInfo() != null && eventInfo.GetHealInfo().GetHeal() != 0;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		Unit caster = eventInfo.GetActor();
		Unit target = eventInfo.GetProcTarget();
		int  bp0    = (int)MathFunctions.CalculatePct(eventInfo.GetHealInfo().GetHeal(), 15);

		// Scarab Brooch does not replace stronger shields
		AuraEffect shield = target.GetAuraEffect(ItemSpellIds.PersistentShieldTriggered, 0, caster.GetGUID());

		if (shield != null)
			if (shield.GetAmount() > bp0)
				return;

		CastSpellExtraArgs args = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
		caster.CastSpell(target, ItemSpellIds.PersistentShieldTriggered, args);
	}
}