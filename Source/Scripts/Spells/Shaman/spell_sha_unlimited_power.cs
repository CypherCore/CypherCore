using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 260895 - Unlimited Power
internal class spell_sha_unlimited_power : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.UnlimitedPowerBuff);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
	{
		var caster = procInfo.GetActor();
		var aura   = caster.GetAura(ShamanSpells.UnlimitedPowerBuff);

		if (aura != null)
			aura.SetStackAmount((byte)(aura.GetStackAmount() + 1));
		else
			caster.CastSpell(caster, ShamanSpells.UnlimitedPowerBuff, procInfo.GetProcSpell());
	}
}