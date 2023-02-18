// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(209400)]
public class spell_dh_razor_spikes : AuraScript, IAuraCheckProc
{
	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(DemonHunterSpells.RAZOR_SPIKES_SLOW);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var damageInfo = eventInfo.GetDamageInfo();

		if (damageInfo == null)
			return false;

		if (damageInfo.GetAttackType() == WeaponAttackType.BaseAttack || damageInfo.GetAttackType() == WeaponAttackType.OffAttack)
		{
			var caster = damageInfo.GetAttacker();
			var target = damageInfo.GetVictim();

			if (caster == null || target == null || !caster.ToPlayer())
				return false;

			if (!caster.IsValidAttackTarget(target))
				return false;

			if (caster.HasAura(DemonHunterSpells.DEMON_SPIKES_BUFF))
				caster.m_Events.AddEventAtOffset(() => { caster.CastSpell(target, DemonHunterSpells.RAZOR_SPIKES_SLOW, true); }, TimeSpan.FromMilliseconds(750));

			return true;
		}

		return false;
	}
}