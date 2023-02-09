using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

public class spell_dru_tranquility_heal : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();


	private void HandleHeal(uint UnnamedParameter)
	{
		if (!GetCaster())
			return;

		var caster = GetCaster();

		if (caster != null)
		{
			var heal = MathFunctions.CalculatePct(caster.SpellBaseHealingBonusDone(SpellSchoolMask.Nature), 180);
			SetHitHeal((int)heal);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHit));
	}
}