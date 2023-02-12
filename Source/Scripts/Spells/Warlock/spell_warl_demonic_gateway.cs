using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Demonic Gateway - 111771
	[SpellScript(111771)]
	public class spell_warl_demonic_gateway : SpellScript, ISpellCheckCast, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleLaunch(uint UnnamedParameter)
		{
			var caster = GetCaster();

			// despawn all other gateways
			var targets1 = new List<Creature>();
			var targets2 = new List<Creature>();
			targets1 = caster.GetCreatureListWithEntryInGrid(WarlockSpells.NPC_WARLOCK_DEMONIC_GATEWAY_GREEN, 200.0f);
			targets2 = caster.GetCreatureListWithEntryInGrid(WarlockSpells.NPC_WARLOCK_DEMONIC_GATEWAY_PURPLE, 200.0f);

			targets1.AddRange(targets2);

			foreach (var target in targets1)
			{
				if (target.GetOwnerGUID() != caster.GetGUID())
					continue;

				target.DespawnOrUnsummon(TimeSpan.FromMilliseconds(100)); // despawn at next tick
			}

			var dest = GetExplTargetDest();

			if (dest != null)
			{
				caster.CastSpell(caster, WarlockSpells.DEMONIC_GATEWAY_SUMMON_PURPLE, true);
				caster.CastSpell(dest, WarlockSpells.DEMONIC_GATEWAY_SUMMON_GREEN, true);
			}
		}

		public SpellCastResult CheckCast()
		{
			// don't allow during Arena Preparation
			if (GetCaster().HasAura(BattlegroundConst.SpellArenaPreparation))
				return SpellCastResult.CantDoThatRightNow;

			// check if player can reach the location
			var spell = GetSpell();

			if (spell.m_targets.HasDst())
			{
				var pos = new Position();
				pos = spell.m_targets.GetDst().Position.GetPosition();
				var caster = GetCaster();

				if (caster.GetPositionZ() + 6.0f < pos.GetPositionZ() || caster.GetPositionZ() - 6.0f > pos.GetPositionZ())
					return SpellCastResult.NoPath;
			}

			return SpellCastResult.SpellCastOk;
		}

		private void HandleVisual(uint UnnamedParameter)
		{
			var caster = GetCaster();
			var dest   = GetExplTargetDest();

			if (caster == null || dest == null)
				return;

			var pos = dest.GetPosition();

			caster.SendPlaySpellVisual(pos, 20.0f, 63644, 0, 0, 2.0f);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleVisual, 0, SpellEffectName.Summon, SpellScriptHookType.Launch));
			SpellEffects.Add(new EffectHandler(HandleLaunch, 1, SpellEffectName.Dummy, SpellScriptHookType.Launch));
		}
	}
}