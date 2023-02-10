using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid
{
	[Script] //  93402 - Sunfire
	internal class spell_dru_sunfire : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}

		private void HandleOnHit(uint effIndex)
		{
			GetCaster().CastSpell(GetHitUnit(), DruidSpellIds.SunfireDamage, true);
		}
	}
}