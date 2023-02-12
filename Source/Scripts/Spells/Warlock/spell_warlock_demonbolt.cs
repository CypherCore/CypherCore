using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// 264178 - Demonbolt
	[SpellScript(264178)]
	public class spell_warlock_demonbolt : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleHit(uint UnnamedParameter)
		{
			if (GetCaster())
			{
				GetCaster().CastSpell(GetCaster(), WarlockSpells.DEMONBOLT_ENERGIZE, true);
				GetCaster().CastSpell(GetCaster(), WarlockSpells.DEMONBOLT_ENERGIZE, true);
			}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHit));
		}
	}
}