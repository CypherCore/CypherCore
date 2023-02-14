// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(362969)]
public class spell_azure_strike : SpellScript, ISpellOnHit
{
	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(362969, Difficulty.None) != null)
			return false;

		return true;
	}

	public void OnHit()
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster != null && target != null)
		{
			var damage = GetHitDamage();
			var bp0    = (int)(damage + (damage * 0.5f)); // Damage + 50% of damage
			caster.CastSpell(target, 362969, bp0);
		}
	}
}