﻿using System;
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
		return ValidateSpellInfo(DemonHunterSpells.SPELL_DH_RAZOR_SPIKES_SLOW);
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

			if (caster.HasAura(DemonHunterSpells.SPELL_DH_DEMON_SPIKES_BUFF))
				caster.m_Events.AddEventAtOffset(() => { caster.CastSpell(target, DemonHunterSpells.SPELL_DH_RAZOR_SPIKES_SLOW, true); }, TimeSpan.FromMilliseconds(750));

			return true;
		}

		return false;
	}
}