// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 29166 - Innervate
	internal class spell_dru_innervate : SpellScript, ISpellCheckCast, ISpellOnHit
	{
		public SpellCastResult CheckCast()
		{
			var target = GetExplTargetUnit()?.ToPlayer();

			if (target == null)
				return SpellCastResult.BadTargets;

			var spec = CliDB.ChrSpecializationStorage.LookupByKey(target.GetPrimarySpecialization());

			if (spec == null ||
			    spec.Role != 1)
				return SpellCastResult.BadTargets;

			return SpellCastResult.SpellCastOk;
		}

		public void OnHit()
		{
			var caster = GetCaster();

			if (caster != GetHitUnit())
			{
				var innervateR2 = caster.GetAuraEffect(DruidSpellIds.InnervateRank2, 0);

				if (innervateR2 != null)
					caster.CastSpell(caster,
					                 DruidSpellIds.Innervate,
					                 new CastSpellExtraArgs(TriggerCastFlags.IgnoreSpellAndCategoryCD | TriggerCastFlags.IgnoreCastInProgress)
						                 .SetTriggeringSpell(GetSpell())
						                 .AddSpellMod(SpellValueMod.BasePoint0, -innervateR2.GetAmount()));
			}
		}
	}
}