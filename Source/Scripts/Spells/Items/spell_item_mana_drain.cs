using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 27522, 40336 - Mana Drain
internal class spell_item_mana_drain : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.ManaDrainEnergize, ItemSpellIds.ManaDrainLeech);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var caster = eventInfo.GetActor();
		var target = eventInfo.GetActionTarget();

		if (caster.IsAlive())
			caster.CastSpell(caster, ItemSpellIds.ManaDrainEnergize, new CastSpellExtraArgs(aurEff));

		if (target && target.IsAlive())
			caster.CastSpell(target, ItemSpellIds.ManaDrainLeech, new CastSpellExtraArgs(aurEff));
	}
}