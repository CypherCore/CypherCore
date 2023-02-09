using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 77758 - Thrash
	internal class spell_dru_thrash : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.ThrashBearAura);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}

		private void HandleOnHitTarget(int effIndex)
		{
			var hitUnit = GetHitUnit();

			if (hitUnit != null)
			{
				var caster = GetCaster();

				caster.CastSpell(hitUnit, DruidSpellIds.ThrashBearAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
			}
		}
	}
}