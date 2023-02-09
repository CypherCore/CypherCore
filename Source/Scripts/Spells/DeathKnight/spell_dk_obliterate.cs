using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(49020)]
public class spell_dk_obliterate : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();


	private void HandleHit(uint UnnamedParameter)
	{
		GetCaster().RemoveAurasDueToSpell(DeathKnightSpells.SPELL_DK_KILLING_MACHINE);

		if (GetCaster().HasAura(DeathKnightSpells.SPELL_DK_ICECAP))
			if (GetCaster().GetSpellHistory().HasCooldown(DeathKnightSpells.SPELL_DK_PILLAR_OF_FROST))
				GetCaster().GetSpellHistory().ModifyCooldown(DeathKnightSpells.SPELL_DK_PILLAR_OF_FROST, TimeSpan.FromSeconds(-3000));

		if (GetCaster().HasAura(DeathKnightSpells.SPELL_DK_INEXORABLE_ASSAULT_STACK))
			GetCaster().CastSpell(GetHitUnit(), DeathKnightSpells.SPELL_DK_INEXORABLE_ASSAULT_DAMAGE, true);

		if (GetCaster().HasAura(DeathKnightSpells.SPELL_DK_RIME) && RandomHelper.randChance(45))
			GetCaster().CastSpell(null, DeathKnightSpells.SPELL_DK_RIME_BUFF, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}
}