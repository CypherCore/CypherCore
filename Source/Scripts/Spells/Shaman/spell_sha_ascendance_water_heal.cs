using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	// Ascendance (Water)(heal) - 114083
	[SpellScript(114083)]
	public class spell_sha_ascendance_water_heal : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private uint m_TargetSize = 0;

		private void OnEffectHeal(uint UnnamedParameter)
		{
			SetHitHeal((int)(GetHitHeal() / m_TargetSize));
		}

		private void FilterTargets(List<WorldObject> p_Targets)
		{
			m_TargetSize = (uint)p_Targets.Count;
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(OnEffectHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaAlly));
		}
	}
}