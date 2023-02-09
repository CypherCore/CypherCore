using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
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
		Unit target = GetTarget();

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
		Unit target = GetTarget();

		// Master of Subtlety
		AuraEffect masterOfSubtletyPassive = GetTarget().GetAuraEffect(RogueSpells.MasterOfSubtletyPassive, 0);

		if (masterOfSubtletyPassive != null)
		{
			Aura masterOfSubtletyAura = GetTarget().GetAura(RogueSpells.MasterOfSubtletyDamagePercent);

			if (masterOfSubtletyAura != null)
			{
				masterOfSubtletyAura.SetMaxDuration(masterOfSubtletyPassive.GetAmount());
				masterOfSubtletyAura.RefreshDuration();
			}
		}

		// Premeditation
		target.RemoveAura(RogueSpells.PremeditationAura);

		target.RemoveAurasDueToSpell(RogueSpells.ShadowFocusEffect);
		target.RemoveAurasDueToSpell(RogueSpells.StealthStealthAura);
		target.RemoveAurasDueToSpell(RogueSpells.StealthShapeshiftAura);
	}
}