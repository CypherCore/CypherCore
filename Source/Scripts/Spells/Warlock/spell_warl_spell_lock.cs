using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Spell Lock - 119910
	[SpellScript(119910)]
	public class spell_warl_spell_lock : SpellScript, ISpellCheckCast, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects => new();

		public SpellCastResult CheckCast()
		{
			var caster = GetCaster();
			var pet    = caster.GetGuardianPet();

			if (caster == null || pet == null)
				return SpellCastResult.DontReport;

			if (pet.GetSpellHistory().HasCooldown(WarlockSpells.FELHUNTER_SPELL_LOCK))
				return SpellCastResult.CantDoThatRightNow;

			return SpellCastResult.SpellCastOk;
		}

		private void HandleHit(int UnnamedParameter)
		{
			var caster = GetCaster();
			var target = GetHitUnit();
			var pet    = caster.GetGuardianPet();

			if (caster == null || pet == null || target == null)
				return;

			/*if (pet->GetEntry() != PET_ENTRY_FELHUNTER)
				return;*/

			pet.CastSpell(target, WarlockSpells.FELHUNTER_SPELL_LOCK, true);
			caster.ToPlayer().GetSpellHistory().ModifyCooldown(GetSpellInfo().Id, TimeSpan.FromSeconds(24));
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}