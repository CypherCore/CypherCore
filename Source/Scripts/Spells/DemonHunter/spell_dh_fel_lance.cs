using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(206966)]
public class spell_dh_fel_lance : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void HandleHit(uint UnnamedParameter)
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster == null || target == null)
			return;

		var pct = GetSpellInfo().GetEffect(0).BasePoints;
		SetHitDamage(GetHitDamage() + (int)target.CountPctFromMaxHealth(pct));
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}