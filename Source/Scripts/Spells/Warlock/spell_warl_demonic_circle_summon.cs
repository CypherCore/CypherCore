// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(48018)] // 48018 - Demonic Circle: Summon
	internal class spell_warl_demonic_circle_summon : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectRemove));
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandleDummyTick, 0, AuraType.PeriodicDummy));
		}

		private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			// If effect is Removed by expire Remove the summoned demonic circle too.
			if (!mode.HasAnyFlag(AuraEffectHandleModes.Reapply))
				GetTarget().RemoveGameObject(GetId(), true);

			GetTarget().RemoveAura(WarlockSpells.DEMONIC_CIRCLE_ALLOW_CAST);
		}

		private void HandleDummyTick(AuraEffect aurEff)
		{
			var circle = GetTarget().GetGameObject(GetId());

			if (circle)
			{
				// Here we check if player is in demonic circle teleport range, if so add
				// WARLOCK_DEMONIC_CIRCLE_ALLOW_CAST; allowing him to cast the WARLOCK_DEMONIC_CIRCLE_TELEPORT.
				// If not in range Remove the WARLOCK_DEMONIC_CIRCLE_ALLOW_CAST.

				var spellInfo = Global.SpellMgr.GetSpellInfo(WarlockSpells.DEMONIC_CIRCLE_TELEPORT, GetCastDifficulty());

				if (GetTarget().IsWithinDist(circle, spellInfo.GetMaxRange(true)))
				{
					if (!GetTarget().HasAura(WarlockSpells.DEMONIC_CIRCLE_ALLOW_CAST))
						GetTarget().CastSpell(GetTarget(), WarlockSpells.DEMONIC_CIRCLE_ALLOW_CAST, true);
				}
				else
				{
					GetTarget().RemoveAura(WarlockSpells.DEMONIC_CIRCLE_ALLOW_CAST);
				}
			}
		}
	}
}