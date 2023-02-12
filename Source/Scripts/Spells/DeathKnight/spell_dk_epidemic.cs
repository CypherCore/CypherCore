using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(207317)]
public class spell_dk_epidemic : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	private void HandleHit(uint UnnamedParameter)
	{
		var target = GetHitUnit();

		if (target != null)
		{
			var aura = target.GetAura(DeathKnightSpells.SPELL_DK_VIRULENT_PLAGUE, GetCaster().GetGUID());

			if (aura != null)
			{
				target.RemoveAura(aura);
				GetCaster().CastSpell(target, DeathKnightSpells.SPELL_DK_EPIDEMIC_DAMAGE_SINGLE, true);
				GetCaster().CastSpell(target, DeathKnightSpells.SPELL_DK_EPIDEMIC_DAMAGE_AOE, true);
			}
		}

		PreventHitDamage();
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}