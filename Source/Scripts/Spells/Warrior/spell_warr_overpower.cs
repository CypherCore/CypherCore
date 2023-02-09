using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
	// 7384 - Overpower
	[SpellScript(7384)]
	public class spell_warr_overpower : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleEffect(int UnnamedParameter)
		{
			if (!GetCaster())
				return;

			uint spellId = 0;

			if (GetCaster().HasAura(WarriorSpells.UNRELENTING_ASSAULT_RANK_1))
				spellId = WarriorSpells.UNRELENTING_ASSAULT_TRIGGER_1;
			else if (GetCaster().HasAura(WarriorSpells.UNRELENTING_ASSAULT_RANK_2))
				spellId = WarriorSpells.UNRELENTING_ASSAULT_TRIGGER_2;

			if (spellId == 0)
				return;

			var target = GetHitPlayer();

			if (target != null)
				if (target.IsNonMeleeSpellCast(false, false, true)) // UNIT_STATE_CASTING should not be used here, it's present during a tick for instant casts
					target.CastSpell(target, spellId, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleEffect, 0, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
		}
	}
}