// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 260878 - Spirit Wolf
[SpellScript(260878)]
internal class spell_sha_spirit_wolf : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.GhostWolf, ShamanSpells.SpiritWolfTalent, ShamanSpells.SpiritWolfPeriodic, ShamanSpells.SpiritWolfAura);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Any, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Any, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = GetTarget();

		if (target.HasAura(ShamanSpells.SpiritWolfTalent) &&
		    target.HasAura(ShamanSpells.GhostWolf))
			target.CastSpell(target, ShamanSpells.SpiritWolfPeriodic, new CastSpellExtraArgs(aurEff));
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		GetTarget().RemoveAurasDueToSpell(ShamanSpells.SpiritWolfPeriodic);
		GetTarget().RemoveAurasDueToSpell(ShamanSpells.SpiritWolfAura);
	}
}