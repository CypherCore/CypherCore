using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(77575)]
public class spell_dk_outbreak : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleOnHit(uint UnnamedParameter)
	{
		var target = GetHitUnit();

		if (target != null)
			if (!target.HasAura(DeathKnightSpells.SPELL_DK_OUTBREAK_PERIODIC, GetCaster().GetGUID()))
				GetCaster().CastSpell(target, DeathKnightSpells.SPELL_DK_OUTBREAK_PERIODIC, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}