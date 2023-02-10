using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[Script] // 197125 - Chaos Strike
internal class spell_dh_chaos_strike : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DemonHunterSpells.ChaosStrikeEnergize);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
		args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount());
		args.SetTriggeringAura(aurEff);
		GetTarget().CastSpell(GetTarget(), DemonHunterSpells.ChaosStrikeEnergize, args);
	}
}