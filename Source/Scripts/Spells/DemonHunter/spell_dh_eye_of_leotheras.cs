// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(206649)]
public class spell_dh_eye_of_leotheras : AuraScript, IAuraCheckProc
{
	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_EYE_OF_LEOTHERAS_DAMAGE, Difficulty.None) != null)
			return false;

		return true;
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var caster = GetCaster();
		var target = GetAura().GetOwner();

		if (caster == null || target == null || eventInfo.GetSpellInfo() != null || !caster.ToPlayer())
			return false;

		var unitTarget = target.ToUnit();

		if (unitTarget == null || eventInfo.GetSpellInfo().IsPositive())
			return false;

		var aurEff = GetAura().GetEffect(0);

		if (aurEff != null)
		{
			var bp = aurEff.GetAmount();
			GetAura().RefreshDuration();


			caster.m_Events.AddEventAtOffset(() => { caster.CastSpell(unitTarget, DemonHunterSpells.SPELL_DH_EYE_OF_LEOTHERAS_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)bp)); }, TimeSpan.FromMilliseconds(100));

			return true;
		}

		return false;
	}
}