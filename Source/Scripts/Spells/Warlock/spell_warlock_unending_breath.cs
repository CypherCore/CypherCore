using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// 5697 - Unending Breath
	[SpellScript(5697)]
	internal class spell_warlock_unending_breath : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleHit(uint effIndex)
		{
			PreventHitDefaultEffect(effIndex);
			var caster = GetCaster();
			var target = GetHitUnit();

			if (target != null)
				if (caster.HasAura(WarlockSpells.SOULBURN))
					caster.CastSpell(target, WarlockSpells.SOULBURN_UNENDING_BREATH, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.ApplyAura, SpellScriptHookType.LaunchTarget));
		}
	}
}