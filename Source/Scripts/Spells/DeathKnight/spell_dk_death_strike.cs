using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 49998 - Death Strike
internal class spell_dk_death_strike : SpellScript, ISpellAfterCast, IHasSpellEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DeathKnightSpells.DeathStrikeEnabler, DeathKnightSpells.DeathStrikeHeal, DeathKnightSpells.BloodShieldMastery, DeathKnightSpells.BloodShieldAbsorb, DeathKnightSpells.RecentlyUsedDeathStrike, DeathKnightSpells.Frost, DeathKnightSpells.DeathStrikeOffhand) && spellInfo.GetEffects().Count > 2;
	}

	public void AfterCast()
	{
		GetCaster().CastSpell(GetCaster(), DeathKnightSpells.RecentlyUsedDeathStrike, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.Launch));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDummy(uint effIndex)
	{
		Unit caster = GetCaster();

		AuraEffect enabler = caster.GetAuraEffect(DeathKnightSpells.DeathStrikeEnabler, 0, GetCaster().GetGUID());

		if (enabler != null)
		{
			// Heals you for 25% of all Damage taken in the last 5 sec,
			int heal = MathFunctions.CalculatePct(enabler.CalculateAmount(GetCaster()), GetEffectInfo(1).CalcValue(GetCaster()));
			// minimum 7.0% of maximum health.
			int pctOfMaxHealth = MathFunctions.CalculatePct(GetEffectInfo(2).CalcValue(GetCaster()), caster.GetMaxHealth());
			heal = Math.Max(heal, pctOfMaxHealth);

			caster.CastSpell(caster, DeathKnightSpells.DeathStrikeHeal, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, heal));

			AuraEffect aurEff = caster.GetAuraEffect(DeathKnightSpells.BloodShieldMastery, 0);

			if (aurEff != null)
				caster.CastSpell(caster, DeathKnightSpells.BloodShieldAbsorb, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, MathFunctions.CalculatePct(heal, aurEff.GetAmount())));

			if (caster.HasAura(DeathKnightSpells.Frost))
				caster.CastSpell(GetHitUnit(), DeathKnightSpells.DeathStrikeOffhand, true);
		}
	}
}