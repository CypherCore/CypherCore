using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Shadow Lock - 171140
	[SpellScript(171140)]
	public class spell_warl_shadow_lock : SpellScript, ISpellCheckCast, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleHit(int UnnamedParameter)
		{
			var caster = GetCaster();
			var target = GetHitUnit();
			var pet    = caster.GetGuardianPet();

			if (caster == null || pet == null || target == null)
				return;

			/*if (pet->GetEntry() != PET_ENTRY_DOOMGUARD)
				return;*/

			pet.CastSpell(target, WarlockSpells.DOOMGUARD_SHADOW_LOCK, true);

			caster.ToPlayer().GetSpellHistory().ModifyCooldown(GetSpellInfo().Id, TimeSpan.FromSeconds(24));
		}

		public SpellCastResult CheckCast()
		{
			var caster = GetCaster();
			var pet    = caster.GetGuardianPet();

			if (caster == null || pet == null)
				return SpellCastResult.DontReport;

			if (pet.GetSpellHistory().HasCooldown(WarlockSpells.DOOMGUARD_SHADOW_LOCK))
				return SpellCastResult.CantDoThatRightNow;

			return SpellCastResult.SpellCastOk;
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}