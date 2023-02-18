// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;

namespace Scripts.Spells.Druid;

[SpellScript(50464)]
public class spell_dru_nourish : SpellScript
{
	private const int NOURISH_PASSIVE = 203374;
	private const int REJUVENATION = 774;

	public void OnHit()
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var target = GetHitUnit();

			if (target != null)
				if (caster.HasAura(NOURISH_PASSIVE))
					caster.CastSpell(target, REJUVENATION, true);
		}
	}
}