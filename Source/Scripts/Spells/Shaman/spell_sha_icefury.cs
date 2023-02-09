using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 210714 - Icefury
internal class spell_sha_icefury : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.FrostShockEnergize);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 1, AuraType.AddPctModifier, AuraScriptHookType.EffectProc));
	}

	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var caster = GetCaster();

		caster?.CastSpell(caster, ShamanSpells.FrostShockEnergize, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
	}
}