using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	//5782 - Fear
	[SpellScript(5782)]
	public class spell_warl_fear : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(WarlockSpells.FEAR, Difficulty.None) != null)
				return false;

			if (Global.SpellMgr.GetSpellInfo(WarlockSpells.FEAR_BUFF, Difficulty.None) != null)
				return false;

			return true;
		}

		private void HandleDummy(uint UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var target = GetExplTargetUnit();

			if (target == null)
				return;

			caster.CastSpell(target, WarlockSpells.FEAR_BUFF, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}