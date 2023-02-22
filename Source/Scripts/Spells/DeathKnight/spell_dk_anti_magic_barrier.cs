// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(205725)]
public class spell_dk_anti_magic_barrier : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(DeathKnightSpells.ANTI_MAGIC_BARRIER, Difficulty.None) != null)
			return false;

		return true;
	}

	private void CalcAmount(AuraEffect aurEff, ref double amount, ref bool UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster != null)
			amount = (int)((caster.GetMaxHealth() * 25.0f) / 100.0f);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 0, AuraType.ModIncreaseHealth2));
	}
}