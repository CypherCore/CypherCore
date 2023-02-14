// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(197835)]
public class spell_rog_shuriken_storm_SpellScript : SpellScript, ISpellOnHit, ISpellAfterHit
{
	private bool _stealthed;

	public override bool Load()
	{
		var caster = GetCaster();

		if (caster.HasAuraType(AuraType.ModStealth) || caster.HasAura(RogueSpells.SPELL_ROGUE_SHADOW_DANCE))
			_stealthed = true;

		return true;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(RogueSpells.SPELL_ROGUE_SHURIKEN_STORM);
	}

	public void AfterHit()
	{
		var target = GetHitUnit();

		if (target.HasAura(51690)) //Killing spree debuff #1
			target.RemoveAura(51690);

		if (target.HasAura(61851)) //Killing spree debuff #2
			target.RemoveAura(61851);
	}

	public void OnHit()
	{
		var caster = GetCaster();
		var cp     = caster.GetPower(PowerType.ComboPoints);

		if (_stealthed)
		{
			var dmg = GetHitDamage();
			SetHitDamage(dmg * 2); //Shuriken Storm deals 200% damage from stealth
		}

		if (cp < caster.GetMaxPower(PowerType.ComboPoints))
			caster.SetPower(PowerType.ComboPoints, cp + 1);
	}
}