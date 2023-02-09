using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(WarlockSpells.SPELL_INQUISITORS_GAZE)]
	public class spell_warlock_inquisitors_gaze : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleOnHit(uint effectIndex)
		{
			var target = GetHitUnit();

			if (target != null)
			{
				var damage = (GetCaster().SpellBaseDamageBonusDone(GetSpellInfo().GetSchoolMask()) * 15 * 16) / 100;
				GetCaster().CastSpell(target, WarlockSpells.SPELL_INQUISITORS_GAZE_EFFECT, new CastSpellExtraArgs(SpellValueMod.BasePoint0, damage));
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}