using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 212623 - Singe Magic
	[SpellScript(212623)]
	public class spell_warlock_singe_magic : SpellScript, ISpellCheckCast, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleHit(uint UnnamedParameter)
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (caster == null || target == null)
				return;

			var pet = caster.ToPlayer().GetPet();

			if (pet != null)
				pet.CastSpell(target, WarlockSpells.SINGE_MAGIC, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)GetEffectInfo(0).BasePoints));
		}

		public SpellCastResult CheckCast()
		{
			var caster = GetCaster();

			if (caster == null || !caster.ToPlayer())
				return SpellCastResult.BadTargets;

			if (caster.ToPlayer().GetPet() && caster.ToPlayer().GetPet().GetEntry() == 416)
				return SpellCastResult.SpellCastOk;

			return SpellCastResult.CantDoThatRightNow;
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}