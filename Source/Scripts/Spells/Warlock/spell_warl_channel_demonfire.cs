// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// Channel Demonfire - 196447
	[SpellScript(196447)]
	public class spell_warl_channel_demonfire : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

		private void HandlePeriodic(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();
			var rangeInfoSpell = Global.SpellMgr.GetSpellInfo(WarlockSpells.CHANNEL_DEMONFIRE_RANGE);

			if (caster == null)
				return;

			var enemies  = new List<Unit>();
			var check    = new AnyUnfriendlyUnitInObjectRangeCheck(caster, caster, rangeInfoSpell.GetMaxRange(), new UnitAuraCheck<Unit>(true, WarlockSpells.IMMOLATE_DOT, caster.GetGUID()).Invoke);
			var searcher = new UnitListSearcher(caster, enemies, check, GridType.All);
			Cell.VisitGrid(caster, searcher, rangeInfoSpell.GetMaxRange());

			if (enemies.Count == 0)
				return;

			var target = enemies.SelectRandom();
			caster.CastSpell(target, WarlockSpells.CHANNEL_DEMONFIRE_DAMAGE, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
		}
	}
}