// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	//80240 - Havoc
	[SpellScript(WarlockSpells.HAVOC)]
	internal class spell_warl_havoc : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var victim = procInfo.GetActionTarget();

			if (victim != null)
			{
				var target = procInfo.GetProcTarget();

				if (target != null)
					if (victim != target)
					{
						var spellInfo = aurEff.GetSpellInfo();

						if (spellInfo != null)
						{
							var dmg   = procInfo.GetDamageInfo().GetDamage();
							var spell = new SpellNonMeleeDamage(caster, target, spellInfo, new SpellCastVisual(spellInfo.GetSpellVisual(caster), 0), SpellSchoolMask.Shadow);
							spell.damage      = dmg;
							spell.cleanDamage = spell.damage;
							caster.DealSpellDamage(spell, false);
							caster.SendSpellNonMeleeDamageLog(spell);
						}
					}
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}
}