using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	// 198103
	[SpellScript(198103)]
	public class spell_sha_earth_elemental : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleSummon(int UnnamedParameter)
		{
			GetCaster().CastSpell(GetHitUnit(), ShamanSpells.SPELL_SHAMAN_EARTH_ELEMENTAL_SUMMON, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleSummon, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}