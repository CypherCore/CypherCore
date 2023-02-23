// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(31230)]
public class spell_rog_cheat_death_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(RogueSpells.CHEAT_DEATH_COOLDOWN);
	}

	public override bool Load()
	{
		return GetUnitOwner().GetTypeId() == TypeId.Player;
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
	{
		// Set absorbtion amount to unlimited
		amount = -1;
	}

	private void Absorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, ref double absorbAmount)
	{
		var target = GetTarget().ToPlayer();

		if (target.HasAura(CheatDeath.CHEAT_DEATH_DMG_REDUC))
		{
			absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), 85);

			return;
		}
		else
		{
			if (dmgInfo.GetDamage() < target.GetHealth() || target.HasAura(RogueSpells.CHEAT_DEATH_COOLDOWN))
				return;

			var health7 = target.CountPctFromMaxHealth(7);
			target.SetHealth(1);
			var healInfo = new HealInfo(target, target, (uint)health7, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
			target.HealBySpell(healInfo);
			target.CastSpell(target, CheatDeath.CHEAT_DEATH_ANIM, true);
			target.CastSpell(target, CheatDeath.CHEAT_DEATH_DMG_REDUC, true);
			target.CastSpell(target, RogueSpells.CHEAT_DEATH_COOLDOWN, true);
			absorbAmount = dmgInfo.GetDamage();
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 1));
	}
}