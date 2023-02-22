// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_absorb0_hitlimit1 : AuraScript, IHasAuraEffects
{
	private double limit;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Load()
	{
		// Max Absorb stored in 1 dummy effect
		limit = GetSpellInfo().GetEffect(1).CalcValue();

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0, false, AuraScriptHookType.EffectAbsorb));
	}

	private void Absorb(AuraEffect aurEff, DamageInfo dmgInfo, ref double absorbAmount)
	{
		absorbAmount = Math.Min(limit, absorbAmount);
	}
}