﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(201464)]
public class spell_dh_artifact_overwhelming_power : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (RandomHelper.randChance(caster.GetAuraEffectAmount(DemonHunterSpells.SPELL_DH_OVERWHELMING_POWER, 0)))
			caster.CastSpell(caster, ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_MISSILE, SpellValueMod.BasePoint0, (int)ShatteredSoulsSpells.SPELL_DH_LESSER_SOUL_SHARD, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
	}
}