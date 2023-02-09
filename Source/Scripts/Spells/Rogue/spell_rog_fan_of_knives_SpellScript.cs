using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(51723)]
public class spell_rog_fan_of_knives_SpellScript : SpellScript, ISpellOnHit, ISpellAfterHit
{
	private bool _hit;

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(RogueSpells.SPELL_ROGUE_FAN_OF_KNIVES);
	}

	public override bool Load()
	{
		return true;
	}

	public void OnHit()
	{
		if (!_hit)
		{
			var cp = GetCaster().GetPower(PowerType.ComboPoints);
			if (cp < GetCaster().GetMaxPower(PowerType.ComboPoints))
			{
				GetCaster().SetPower(PowerType.ComboPoints, cp + 1);
			}
			_hit = true;
		}
	}

	public void AfterHit()
	{
		Unit target = GetHitUnit();
		if (target.HasAura(51690)) //Killing spree debuff #1
		{
			target.RemoveAura(51690);
		}
		if (target.HasAura(61851)) //Killing spree debuff #2
		{
			target.RemoveAura(61851);
		}
	}
}