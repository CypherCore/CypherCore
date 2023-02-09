using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 76806 - Mastery: Main Gauche
internal class spell_rog_mastery_main_gauche : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(RogueSpells.MainGauche);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetDamageInfo() != null && eventInfo.GetDamageInfo().GetVictim() != null;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
	{
		var target = GetTarget();

		target?.CastSpell(procInfo.GetDamageInfo().GetVictim(), RogueSpells.MainGauche, new CastSpellExtraArgs(aurEff));
	}
}