using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	// 51533 - Feral Spirit
	[SpellScript(51533)]
	public class spell_sha_feral_spirit : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleDummy(uint UnnamedParameter)
		{
			var caster = GetCaster();

			caster.CastSpell(GetHitUnit(), ShamanSpells.SPELL_SHAMAN_FERAL_SPIRIT_SUMMON, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}