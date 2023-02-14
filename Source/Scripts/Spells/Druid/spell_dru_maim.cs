// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(22570)]
public class spell_dru_maim : SpellScript, ISpellAfterCast, ISpellOnTakePower
{
	private int _usedComboPoints = 0;

	public void TakePower(SpellPowerCost powerCost)
	{
		if (powerCost.Power == PowerType.ComboPoints)
			_usedComboPoints = powerCost.Amount;
	}

	public void AfterCast()
	{
		var target = GetExplTargetUnit();

		if (target == null)
			return;

		GetCaster().CastSpell(target, MaimSpells.SPELL_DRUID_MAIM_STUN, true);

		var maimStun = target.GetAura(MaimSpells.SPELL_DRUID_MAIM_STUN, GetCaster().GetGUID());

		if (maimStun != null)
			maimStun.SetDuration(_usedComboPoints * 1000);
	}
}