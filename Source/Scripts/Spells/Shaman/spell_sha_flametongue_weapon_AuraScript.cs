using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 319778 - Flametongue
internal class spell_sha_flametongue_weapon_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.FlametongueAttack);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var                attacker = eventInfo.GetActor();
		CastSpellExtraArgs args     = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, Math.Max(1, (int)(attacker.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.0264f)));
		attacker.CastSpell(eventInfo.GetActionTarget(), ShamanSpells.FlametongueAttack, args);
	}
}