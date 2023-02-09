using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] //  8921 - Moonfire
	internal class spell_dru_moonfire : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.MoonfireDamage);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}

		private void HandleOnHit(int effIndex)
		{
			GetCaster().CastSpell(GetHitUnit(), DruidSpellIds.MoonfireDamage, true);
		}
	}
}