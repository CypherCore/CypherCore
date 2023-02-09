using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(116841)]
public class spell_monk_tiger_lust : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return Global.SpellMgr.GetSpellInfo(TigerLust.SPELL_MONK_TIGER_LUST, Difficulty.None) != null;
	}

	private void HandleDummy(int UnnamedParameter)
	{
		var target = GetHitUnit();

		if (target != null)
			target.RemoveMovementImpairingAuras(false);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
	}
}