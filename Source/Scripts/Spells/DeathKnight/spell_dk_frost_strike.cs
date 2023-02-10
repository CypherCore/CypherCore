using System;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(49143)]
public class spell_dk_frost_strike : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var caster = GetCaster();
		var target = caster.GetVictim();

		if (caster == null || target == null)
			return;

		if (caster.HasAura(DeathKnightSpells.SPELL_DK_ICECAP))
			if (caster.GetSpellHistory().HasCooldown(DeathKnightSpells.SPELL_DK_PILLAR_OF_FROST))
				caster.GetSpellHistory().ModifyCooldown(DeathKnightSpells.SPELL_DK_PILLAR_OF_FROST, TimeSpan.FromSeconds(-3000));

		if (caster.HasAura(DeathKnightSpells.SPELL_DK_OBLITERATION) && caster.HasAura(DeathKnightSpells.SPELL_DK_PILLAR_OF_FROST))
			caster.CastSpell(null, DeathKnightSpells.SPELL_DK_KILLING_MACHINE, true);
	}
}