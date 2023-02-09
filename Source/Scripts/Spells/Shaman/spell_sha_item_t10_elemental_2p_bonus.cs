using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 70811 - Item - Shaman T10 Elemental 2P Bonus
internal class spell_sha_item_t10_elemental_2p_bonus : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.ElementalMastery);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		Player target = GetTarget().ToPlayer();

		if (target)
			target.GetSpellHistory().ModifyCooldown(ShamanSpells.ElementalMastery, TimeSpan.FromMilliseconds(-aurEff.GetAmount()));
	}
}