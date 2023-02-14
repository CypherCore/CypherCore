// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(new uint[]
             {
	             48263, 48265, 48266
             })]
public class spell_dk_presence_SpellScript : SpellScript
{
	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(DeathKnightSpells.SPELL_DK_SCOURGE_STRIKE_TRIGGERED, Difficulty.None) != null)
			return false;

		return true;
	}

	public void AfterHit()
	{
		var caster = GetCaster();

		if (GetHitUnit())
		{
			var runicPower = caster.GetPower(PowerType.RunicPower);
			var aurEff     = caster.GetAuraEffect(58647, 0);

			if (aurEff != null)
				runicPower = MathFunctions.CalculatePct(runicPower, aurEff.GetAmount());
			else
				runicPower = 0;

			caster.SetPower(PowerType.RunicPower, runicPower);
		}
	}
}