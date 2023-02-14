// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(new uint[]
             {
	             190984, 194153
             })]
public class spell_dru_blessing_of_elune : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var power = GetHitDamage();

		var aura = caster.GetAura(202737);

		if (aura != null)
		{
			var aurEff = aura.GetEffect(0);

			if (aurEff != null)
				power += MathFunctions.CalculatePct(power, aurEff.GetAmount());
		}

		SetHitDamage(power);
	}
}