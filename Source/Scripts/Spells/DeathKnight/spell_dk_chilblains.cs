// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(45524)]
public class spell_dk_chilblains : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var player = GetCaster().ToPlayer();

		if (player != null)
		{
			var target = GetHitUnit();

			if (target != null)
				if (player.HasAura(DeathKnightSpells.SPELL_DK_CHILBLAINS))
					player.CastSpell(target, DeathKnightSpells.SPELL_DK_CHAINS_OF_ICE_ROOT, true);
		}

		if (GetCaster().HasAura(DeathKnightSpells.SPELL_DK_COLD_HEART_CHARGE))
		{
			var coldHeartCharge = GetCaster().GetAura(DeathKnightSpells.SPELL_DK_COLD_HEART_CHARGE);

			if (coldHeartCharge != null)
			{
				var stacks = coldHeartCharge.GetStackAmount();
				SetHitDamage(GetHitDamage() * stacks);
				GetCaster().CastSpell(GetHitUnit(), DeathKnightSpells.SPELL_DK_COLD_HEART_DAMAGE, true);
				coldHeartCharge.ModStackAmount(-stacks);
			}
		}
	}
}