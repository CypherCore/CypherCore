using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// 64380, 65941 - Shattering Throw
	[SpellScript(new uint[]
	             {
		             64380, 65941
	             })]
	public class spell_warr_shattering_throw : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleScript(uint effIndex)
		{
			PreventHitDefaultEffect(effIndex);

			// remove shields, will still display immune to damage part
			var target = GetHitUnit();

			if (target != null)
				target.RemoveAurasWithMechanic((ulong)Mechanics.ImmuneShield, AuraRemoveMode.EnemySpell);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
		}
	}
}