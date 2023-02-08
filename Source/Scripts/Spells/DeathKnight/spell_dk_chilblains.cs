using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(45524)]
public class spell_dk_chilblains : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		Player player = GetCaster().ToPlayer();
		if (player != null)
		{
			Unit target = GetHitUnit();
			if (target != null)
			{
				if (player.HasAura(DeathKnightSpells.SPELL_DK_CHILBLAINS))
				{
					player.CastSpell(target, DeathKnightSpells.SPELL_DK_CHAINS_OF_ICE_ROOT, true);
				}
			}
		}

		if (GetCaster().HasAura(DeathKnightSpells.SPELL_DK_COLD_HEART_CHARGE))
		{
			Aura coldHeartCharge = GetCaster().GetAura(DeathKnightSpells.SPELL_DK_COLD_HEART_CHARGE);
			if (coldHeartCharge != null)
			{
				byte stacks = coldHeartCharge.GetStackAmount();
				SetHitDamage(GetHitDamage() * stacks);
				GetCaster().CastSpell(GetHitUnit(), DeathKnightSpells.SPELL_DK_COLD_HEART_DAMAGE, true);
				coldHeartCharge.ModStackAmount(-stacks);
			}
		}
	}
}