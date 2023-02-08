using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(47468)]
public class spell_dk_ghoul_claw : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		Unit caster = GetCaster();
		Unit target = GetExplTargetUnit();

		if (caster == null || target == null)
		{
			return;
		}

		Unit owner = caster.GetOwner().ToPlayer();
		if (owner != null)
		{
			if (owner.HasAura(DeathKnightSpells.SPELL_DK_INFECTED_CLAWS))
			{
				if (RandomHelper.randChance(30))
				{
					caster.CastSpell(target, DeathKnightSpells.SPELL_DK_FESTERING_WOUND_DAMAGE, true);
				}
			}
		}
	}
}