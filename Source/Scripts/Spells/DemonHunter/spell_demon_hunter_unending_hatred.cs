// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.DemonHunter;

[SpellScript(213480)]
public class spell_demon_hunter_unending_hatred : AuraScript, IAuraCheckProc, IAuraOnProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetDamageInfo() != null && (eventInfo.GetDamageInfo().GetSchoolMask() & SpellSchoolMask.Shadow) != 0;
	}

	public void OnProc(ProcEventInfo eventInfo)
	{
		var caster = GetPlayerCaster();

		if (caster == null)
			return;

		var pointsGained = GetPointsGained(caster, eventInfo.GetDamageInfo().GetDamage());

		if (caster.GetPrimarySpecialization() == TalentSpecialization.DemonHunterHavoc)
			caster.EnergizeBySpell(caster, GetSpellInfo(), pointsGained, PowerType.Fury);
		else if (caster.GetPrimarySpecialization() == TalentSpecialization.DemonHunterVengeance)
			caster.EnergizeBySpell(caster, GetSpellInfo(), pointsGained, PowerType.Pain);
	}

	public Player GetPlayerCaster()
	{
		var caster = GetCaster();

		if (caster == null)
			return null;

		return caster.ToPlayer();
	}

	public double GetPointsGained(Player caster, double damage)
	{
		var damagePct = damage / caster.GetMaxHealth() * 100.0f / 2;
		var max       = GetSpellInfo().GetEffect(0).BasePoints;

		if (damagePct > max)
			return max;

		if (damagePct < 1F)
			return 1;

		return 0;
	}
}