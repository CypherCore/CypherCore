// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Shaman
{
	// Stormbringer - 201845
	[SpellScript(201845)]
	public class spell_sha_stormbringer : AuraScript, IAuraCheckProc, IAuraOnProc
	{
		public bool CheckProc(ProcEventInfo eventInfo)
		{
			return eventInfo.GetDamageInfo().GetAttackType() == WeaponAttackType.BaseAttack;
		}

		public void OnProc(ProcEventInfo info)
		{
			var caster = GetCaster();

			if (caster != null)
			{
				caster.CastSpell(caster, ShamanSpells.SPELL_SHAMAN_STORMBRINGER_PROC, true);
				caster.GetSpellHistory().ResetCooldown(ShamanSpells.SPELL_SHAMAN_STORMSTRIKE, true);
			}
		}
	}
}