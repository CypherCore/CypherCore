﻿using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 196277 - Implosion
	[SpellScript(WarlockSpells.IMPLOSION)]
	public class spell_warl_implosion : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public void HandleHit(uint UnnamedParameter)
		{
			var caster = GetCaster();
			var target = GetHitUnit();

			if (caster == null ||
			    target == null)
				return;

			var imps = caster.GetCreatureListWithEntryInGrid(55659); // Wild Imps

			foreach (var imp in imps)
				if (imp.ToTempSummon().GetSummoner() == caster)
				{
					imp.InterruptNonMeleeSpells(false);
					imp.VariableStorage.Set("controlled", true);
					imp.VariableStorage.Set("ForceUpdateTimers", true);
					imp.CastSpell(target, WarlockSpells.IMPLOSION_JUMP, true);
					imp.GetMotionMaster().MoveJump(target, 300.0f, 1.0f, EventId.Jump);
					var casterGuid = caster.GetGUID();

					imp.GetAI()
					   .Scheduler.Schedule(TimeSpan.FromMilliseconds(500),
					                       task =>
					                       {
						                       imp.CastSpell(imp, WarlockSpells.IMPLOSION_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(casterGuid));
						                       imp.DisappearAndDie();
					                       });
				}
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}