using Framework.Constants;
using Game.Entities;
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
		{
			_usedComboPoints = powerCost.Amount;
		}
	}

	public void AfterCast()
	{
		Unit target = GetExplTargetUnit();
		if (target == null)
		{
			return;
		}

		GetCaster().CastSpell(target, MaimSpells.SPELL_DRUID_MAIM_STUN, true);

		Aura maimStun = target.GetAura(MaimSpells.SPELL_DRUID_MAIM_STUN, GetCaster().GetGUID());
		if (maimStun != null)
		{
			maimStun.SetDuration(_usedComboPoints * 1000);
		}
	}
}