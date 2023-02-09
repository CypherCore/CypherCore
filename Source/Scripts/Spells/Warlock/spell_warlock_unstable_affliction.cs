using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// 30108 - Unstable Affliction
	[SpellScript(30108)]
	public class spell_warlock_unstable_affliction : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleHit(int effIndex)
		{
			PreventHitDefaultEffect(effIndex);
			var caster = GetCaster();
			var target = GetHitUnit();

			if (caster == null || target == null)
				return;

			var uaspells = new List<int>()
			               {
				               (int)WarlockSpells.UNSTABLE_AFFLICTION_DOT5,
				               (int)WarlockSpells.UNSTABLE_AFFLICTION_DOT4,
				               (int)WarlockSpells.UNSTABLE_AFFLICTION_DOT3,
				               (int)WarlockSpells.UNSTABLE_AFFLICTION_DOT2,
				               (int)WarlockSpells.UNSTABLE_AFFLICTION_DOT1
			               };

			uint spellToCast         = 0;
			var  minDuration         = 10000;
			uint lowestDurationSpell = 0;

			foreach (uint spellId in uaspells)
			{
				var ua = target.GetAura(spellId, caster.GetGUID());

				if (ua != null)
				{
					if (ua.GetDuration() < minDuration)
					{
						minDuration         = ua.GetDuration();
						lowestDurationSpell = ua.GetSpellInfo().Id;
					}
				}
				else
				{
					spellToCast = spellId;
				}
			}

			if (spellToCast == 0)
				caster.CastSpell(target, lowestDurationSpell, true);
			else
				caster.CastSpell(target, spellToCast, true);

			if (caster.HasAura(WarlockSpells.CONTAGION))
				caster.CastSpell(target, WarlockSpells.CONTAGION_DEBUFF, true);

			if (caster.HasAura(WarlockSpells.COMPOUNDING_HORROR))
				caster.CastSpell(target, WarlockSpells.COMPOUNDING_HORROR_DAMAGE, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}