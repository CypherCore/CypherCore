using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(408)]
public class spell_rog_kidney_shot_SpellScript : SpellScript, ISpellAfterHit, ISpellOnTakePower
{
	private int _cp = 0;

	public void TakePower(SpellPowerCost powerCost)
	{
		if (powerCost.Power == PowerType.ComboPoints)
		{
			_cp = powerCost.Amount + 1;
		}
	}

	public void AfterHit()
	{
		Unit target = GetHitUnit();
		if (target != null)
		{
			Aura aura = target.GetAura(RogueSpells.SPELL_ROGUE_KIDNEY_SHOT, GetCaster().GetGUID());
			if (aura != null)
			{
				aura.SetDuration(_cp * Time.InMilliseconds);
			}
		}
	}
}