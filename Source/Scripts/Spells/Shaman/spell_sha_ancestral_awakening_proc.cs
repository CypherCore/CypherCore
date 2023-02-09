using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	// 52759 - Ancestral Awakening
	/// Updated 4.3.4
	[SpellScript(52759)]
	public class spell_sha_ancestral_awakening_proc : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(ShamanSpells.SPELL_SHAMAN_ANCESTRAL_AWAKENING_PROC, Difficulty.None) != null)
				return false;

			return true;
		}

		private void FilterTargets(List<WorldObject> targets)
		{
			if (targets.Count < 2)
				return;

			targets.Sort(new HealthPctOrderPred());

			var target = targets.First();
			targets.Clear();
			targets.Add(target);
		}

		private void HandleDummy(int UnnamedParameter)
		{
			GetCaster().CastSpell(GetHitUnit(), ShamanSpells.SPELL_SHAMAN_ANCESTRAL_AWAKENING_PROC, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)GetEffectValue()));
		}

		public override void Register()
		{
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitCasterAreaRaid));
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}