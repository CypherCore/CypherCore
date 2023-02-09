using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(196771)]
public class spell_dk_remorseless_winter_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();


	private void HandleOnHit(int UnnamedParameter)
	{
		var unit = GetHitUnit();

		if (unit != null)
			GetCaster().CastSpell(unit, DeathKnightSpells.SPELL_DK_REMORSELESS_WINTER_SLOW_DOWN, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}