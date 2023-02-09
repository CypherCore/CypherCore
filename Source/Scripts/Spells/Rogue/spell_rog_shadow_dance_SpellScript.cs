using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(185313)]
public class spell_rog_shadow_dance_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void HandleHit(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		if (caster.HasAura(RogueSpells.SPELL_ROGUE_MASTER_OF_SHADOWS))
		{
			caster.ModifyPower(PowerType.Energy, +30);
		}

		caster.CastSpell(caster, RogueSpells.SPELL_ROGUE_SHADOW_DANCE_AURA, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHit));
	}
}