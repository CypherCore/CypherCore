// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	// Script to handle the dummy proc of 16196
	[SpellScript(16196)]
	public class spell_sha_resurgence : AuraScript, IAuraOnProc, IAuraCheckProc
	{
		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return ValidateSpellInfo(Resurgence.WATER_SHIELD, Resurgence.RESURGENCE, Resurgence.RESURGENCE_PROC);
		}

		// Spell cannot proc if caster doesn't have aura 52127
		public bool CheckProc(ProcEventInfo procInfo)
		{
			var target = procInfo.GetActor();

			if (target != null)
				return target.HasAura(Resurgence.WATER_SHIELD);

			return false;
		}

		public void OnProc(ProcEventInfo procInfo)
		{
			double healAmount = 0.0f;
			var target     = procInfo.GetActor();

			if (target != null)
			{
				healAmount = target.CalculateSpellDamage(target, procInfo.GetProcSpell().GetSpellInfo().GetEffect(0), 0);

				if (healAmount != 0)
				{
					var damageInfo = procInfo.GetDamageInfo();

					// Change heal amount accoring to the spell that triggered this one */
					if (damageInfo != null)
					{
						switch (damageInfo.GetSpellInfo().Id)
						{
							// 100% on Healing Wave and Greater Healing Wave
							case Resurgence.HEALING_WAVE:
							case Resurgence.GREATER_HEALING_WAVE:
								break;

							// 60% on Riptide, Healing Surge and Unleash Life
							case Resurgence.RIPTIDE:
							case Resurgence.HEALING_SURGE:
							case Resurgence.UNLEASH_LIFE:
								healAmount *= 0.6f;

								break;

							// 33% on Chain Heal
							case Resurgence.CHAIN_HEAL:
								healAmount *= 0.33f;

								break;

							/*
							* If we have something else here, we should assert, because it would not be
							* logic (if spell_proc_event entry in DB is correct). But, since I cannot be
							* sure that proc system is 100% correct, just return for now.
							*/
							default:
								return;
						} //switch damageInfo->GetSpellInfo()->Id

						target.CastSpell(target, Resurgence.RESURGENCE_PROC, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)healAmount));
					} // if procInfo.GetDamageInfo()
				}     // if target->CalculateSpellDamage()
			}         // if procInfo.GetActor()
		}             // void HandleDummyProc
	}
}