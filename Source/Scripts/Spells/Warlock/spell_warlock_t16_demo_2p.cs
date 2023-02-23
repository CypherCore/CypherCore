// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 145072 - Item - Warlock T16 2P Bonus
	[SpellScript(145072)]
	internal class spell_warlock_t16_demo_2p : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

		private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			uint procSpellId = 0;
			var spellInfo = eventInfo.GetDamageInfo().GetSpellInfo();

			if (spellInfo != null)
				procSpellId = spellInfo.Id;

			double chance = 0;
			uint triggeredSpellId = 0;

			switch (procSpellId)
			{
				case WarlockSpells.CONFLAGRATE:
				case WarlockSpells.CONFLAGRATE_FIRE_AND_BRIMSTONE:
					chance = aurEff.GetSpellInfo().GetEffect(3).BasePoints;
					triggeredSpellId = 145075; // Destructive Influence

					break;
				case WarlockSpells.UNSTABLE_AFFLICTION:
					chance = aurEff.GetSpellInfo().GetEffect(1).BasePoints;
					triggeredSpellId = 145082; // Empowered Grasp

					break;
				case WarlockSpells.SOUL_FIRE:
				case WarlockSpells.SOUL_FIRE_METAMORPHOSIS:
					chance = aurEff.GetSpellInfo().GetEffect(3).BasePoints;
					triggeredSpellId = 145085; // Fiery Wrath

					break;
				default:
					return;
			}

			if (!RandomHelper.randChance(chance))
				return;

			var caster = GetUnitOwner();
			caster.CastSpell(caster, triggeredSpellId, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}
}