using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(115098)]
public class spell_monk_chi_wave : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDummy(uint UnnamedParameter)
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (target == null)
			return;

		if (caster.IsFriendlyTo(target))
			caster.CastSpell(target, 132464, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint1, GetEffectValue()));
		else if (caster.IsValidAttackTarget(target))
			caster.CastSpell(target, 132467, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint1, GetEffectValue()));
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}