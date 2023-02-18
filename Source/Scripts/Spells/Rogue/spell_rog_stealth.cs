// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 1784 - Stealth
internal class spell_rog_stealth : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(RogueSpells.MasterOfSubtletyPassive, RogueSpells.MasterOfSubtletyDamagePercent, RogueSpells.Sanctuary, RogueSpells.ShadowFocus, RogueSpells.ShadowFocusEffect, RogueSpells.StealthStealthAura, RogueSpells.StealthShapeshiftAura);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = GetTarget();

		// Master of Subtlety
		if (target.HasAura(RogueSpells.MasterOfSubtletyPassive))
			target.CastSpell(target, RogueSpells.MasterOfSubtletyDamagePercent, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

		// Shadow Focus
		if (target.HasAura(RogueSpells.ShadowFocus))
			target.CastSpell(target, RogueSpells.ShadowFocusEffect, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

		// Premeditation
		if (target.HasAura(RogueSpells.PremeditationPassive))
			target.CastSpell(target, RogueSpells.PremeditationAura, true);

		target.CastSpell(target, RogueSpells.Sanctuary, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
		target.CastSpell(target, RogueSpells.StealthStealthAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
		target.CastSpell(target, RogueSpells.StealthShapeshiftAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
	}

	private void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = GetTarget();

		// Master of Subtlety
		var masterOfSubtletyPassive = GetTarget().GetAuraEffect(RogueSpells.MasterOfSubtletyPassive, 0);

		if (masterOfSubtletyPassive != null)
		{
			var masterOfSubtletyAura = GetTarget().GetAura(RogueSpells.MasterOfSubtletyDamagePercent);

			if (masterOfSubtletyAura != null)
			{
				masterOfSubtletyAura.SetMaxDuration(masterOfSubtletyPassive.GetAmount());
				masterOfSubtletyAura.RefreshDuration();
			}
		}

		// Premeditation
		target.RemoveAura(RogueSpells.PremeditationAura);

		target.RemoveAura(RogueSpells.ShadowFocusEffect);
		target.RemoveAura(RogueSpells.StealthStealthAura);
		target.RemoveAura(RogueSpells.StealthShapeshiftAura);
	}
}