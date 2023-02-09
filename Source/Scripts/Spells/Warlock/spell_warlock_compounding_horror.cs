using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// 231489 - Compounding Horror
	[SpellScript(231489)]
	internal class spell_warlock_compounding_horror : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleHit(int UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var damage = GetHitDamage();
			var stacks = 0;
			var aur    = caster.GetAura(WarlockSpells.COMPOUNDING_HORROR);

			if (aur != null)
				stacks = aur.GetStackAmount();

			SetHitDamage(damage * stacks);

			caster.RemoveAurasDueToSpell(WarlockSpells.COMPOUNDING_HORROR);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}
	}
}