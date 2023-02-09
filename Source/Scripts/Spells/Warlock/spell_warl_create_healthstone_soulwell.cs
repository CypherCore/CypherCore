using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// Create Healthstone (Soulwell) - 34130
	[SpellScript(34130)]
	public class spell_warl_create_healthstone_soulwell : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(WarlockSpells.SOULWELL_CREATE_HEALTHSTONE, Difficulty.None) != null)
				return false;

			return true;
		}

		private void HandleScriptEffect(uint UnnamedParameter)
		{
			GetCaster().CastSpell(GetCaster(), 23517, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
		}
	}
}