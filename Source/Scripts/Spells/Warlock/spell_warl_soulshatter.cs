using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(29858)] // 29858 - Soulshatter
	internal class spell_warl_soulshatter : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarlockSpells.SOULSHATTER);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}

		private void HandleDummy(uint effIndex)
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (target)
				if (target.CanHaveThreatList() &&
				    target.GetThreatManager().GetThreat(caster) > 0.0f)
					caster.CastSpell(target, WarlockSpells.SOULSHATTER, true);
		}
	}
}