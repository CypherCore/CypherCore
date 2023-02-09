using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(new uint[]
             {
	             205320, 205414, 222029
             })]
public class spell_monk_strike_of_the_windlord : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void HandleDummy(int UnnamedParameter)
	{
		var target = GetHitUnit();

		if (target != null)
		{
			var damage = GetEffectValue();
			MathFunctions.AddPct(ref damage, target.GetTotalAuraModifier(AuraType.ModDamagePercentTaken));
			SetHitDamage(damage);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}