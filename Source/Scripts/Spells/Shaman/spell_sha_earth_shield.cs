using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 204288 - Earth Shield
[SpellScript(204288)]
internal class spell_sha_earth_shield : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.EarthShieldHeal);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetDamageInfo() == null ||
		    !HasEffect(1) ||
		    eventInfo.GetDamageInfo().GetDamage() < GetTarget().CountPctFromMaxHealth(GetEffect(1).GetAmount()))
			return false;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		GetTarget().CastSpell(GetTarget(), ShamanSpells.EarthShieldHeal, new CastSpellExtraArgs(aurEff).SetOriginalCaster(GetCasterGUID()));
	}
}