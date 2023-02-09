using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
	// 260798  - Executes damages
	[SpellScript(260798)]
	public class spell_warr_execute_damages : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleDamage(int UnnamedParameter)
		{
			var damageMultiplier = GetCaster().VariableStorage.GetValue<float>("spell_warr_execute_damages::multiplier", 1.0f);
			SetHitDamage((int)(GetHitDamage() * damageMultiplier));
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}
	}
}