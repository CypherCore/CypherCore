using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(199804)]
public class spell_rog_between_the_eyes_SpellScript : SpellScript, ISpellAfterHit, ISpellOnTakePower
{
	private int _cp = 0;

	public void TakePower(SpellPowerCost powerCost)
	{
		if (powerCost.Power == PowerType.ComboPoints)
			_cp = powerCost.Amount;
	}

	public void AfterHit()
	{
		var target = GetHitUnit();

		if (target != null)
		{
			var aura = target.GetAura(TrueBearingIDs.SPELL_ROGUE_BETWEEN_THE_EYES, GetCaster().GetGUID());

			if (aura != null)
				aura.SetDuration(_cp * Time.InMilliseconds);
		}
	}
}