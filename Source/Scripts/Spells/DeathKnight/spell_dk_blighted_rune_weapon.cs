using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(195758)]
public class spell_dk_blighted_rune_weapon : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(DeathKnightSpells.SPELL_DK_FESTERING_WOUND, Difficulty.None) != null)
			return false;

		return true;
	}

	private void HandleHit(uint UnnamedParameter)
	{
		var target = GetHitUnit();

		if (target != null)
			GetCaster().CastSpell(target, DeathKnightSpells.SPELL_DK_FESTERING_WOUND, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}