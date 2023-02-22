// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// Soul Leech aura - 228974
	[SpellScript(228974)]
	public class spell_warl_soul_leech_aura : AuraScript, IAuraCheckProc
	{
		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return ValidateSpellInfo(WarlockSpells.DEMONSKIN);
		}

		public bool CheckProc(ProcEventInfo eventInfo)
		{
			var caster = GetCaster();

			if (caster == null)
				return false;

			var basePoints = GetSpellInfo().GetEffect(0).BasePoints;
			var absorb     = ((eventInfo.GetDamageInfo() != null ? eventInfo.GetDamageInfo().GetDamage() : 0) * basePoints) / 100.0f;

			// Add remaining amount if already applied
			var aur = caster.GetAura(WarlockSpells.SOUL_LEECH_ABSORB);

			if (aur != null)
			{
				var aurEff = aur.GetEffect(0);

				if (aurEff != null)
					absorb += aurEff.GetAmount();
			}

			// Cannot go over 15% (or 20% with Demonskin) max health
			var basePointNormal = GetSpellInfo().GetEffect(1).BasePoints;
			var basePointDS     = Global.SpellMgr.GetSpellInfo(WarlockSpells.DEMONSKIN, Difficulty.None).GetEffect(1).BasePoints;
			var totalBP         = caster.HasAura(WarlockSpells.DEMONSKIN) ? basePointDS : basePointNormal;
			var threshold       = ((int)caster.GetMaxHealth() * totalBP) / 100.0f;
			absorb = (int)Math.Min(absorb, threshold);

			caster.CastSpell(caster, WarlockSpells.SOUL_LEECH_ABSORB, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorb));

			return true;
		}
	}
}