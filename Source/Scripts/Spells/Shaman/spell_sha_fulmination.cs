// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	// 88766 - Fulmination
	[SpellScript(88766)]
	public class spell_sha_fulmination : AuraScript, IHasAuraEffects, IAuraCheckProc
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(ShamanSpells.FULMINATION, Difficulty.None) != null)
				return false;

			if (Global.SpellMgr.GetSpellInfo(ShamanSpells.FULMINATION_INFO, Difficulty.None) != null)
				return false;

			if (Global.SpellMgr.GetSpellInfo(ShamanSpells.IMPROVED_LIGHTNING_SHIELD, Difficulty.None) != null)
				return false;

			var lightningShield = Global.SpellMgr.GetSpellInfo(ShamanSpells.LIGHTNING_SHIELD, Difficulty.None);

			if (lightningShield == null || !lightningShield.GetEffect(0).IsEffect() || Global.SpellMgr.GetSpellInfo(lightningShield.GetEffect(0).TriggerSpell, Difficulty.None) != null)
				return false;

			if (Global.SpellMgr.GetSpellInfo(ShamanSpells.ITEM_T18_ELEMENTAL_2P_BONUS, Difficulty.None) != null)
				return false;

			if (Global.SpellMgr.GetSpellInfo(ShamanSpells.ITEM_T18_ELEMENTAL_4P_BONUS, Difficulty.None) != null)
				return false;

			if (Global.SpellMgr.GetSpellInfo(ShamanSpells.ITEM_T18_LIGHTNING_VORTEX, Difficulty.None) != null)
				return false;

			return true;
		}

		public bool CheckProc(ProcEventInfo eventInfo)
		{
			// Lava Burst cannot add lightning shield stacks without Improved Lightning Shield
			if ((eventInfo.GetSpellInfo().SpellFamilyFlags[1] & 0x00001000) != 0 && !eventInfo.GetActor().HasAura(ShamanSpells.IMPROVED_LIGHTNING_SHIELD))
				return false;

			return eventInfo.GetActor().HasAura(ShamanSpells.LIGHTNING_SHIELD);
		}

		private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			var caster = eventInfo.GetActor();
			var target = eventInfo.GetActionTarget();
			var aura   = caster.GetAura(ShamanSpells.LIGHTNING_SHIELD);

			if (aura != null)
			{
				// Earth Shock releases the charges
				if ((eventInfo.GetSpellInfo().SpellFamilyFlags[0] & 0x00100000) != 0)
				{
					uint stacks = aura.GetCharges();

					if (stacks > 1)
					{
						var triggerSpell  = Global.SpellMgr.AssertSpellInfo(aura.GetSpellInfo().GetEffect(0).TriggerSpell, Difficulty.None);
						var triggerEffect = triggerSpell.GetEffect(0);

						double damage;
						damage = caster.SpellDamageBonusDone(target, triggerSpell, triggerEffect.CalcValue(caster), DamageEffectType.SpellDirect, triggerEffect, stacks - 1);
						damage = target.SpellDamageBonusTaken(caster, triggerSpell, damage, DamageEffectType.SpellDirect);

						caster.CastSpell(target, ShamanSpells.FULMINATION, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)(int)damage));
						caster.RemoveAura(ShamanSpells.FULMINATION_INFO);

						var t18_4p = caster.GetAuraEffect(ShamanSpells.ITEM_T18_ELEMENTAL_4P_BONUS, 0);

						if (t18_4p != null)
						{
							var gatheringVortex = caster.GetAura(ShamanSpells.ITEM_T18_GATHERING_VORTEX);

							if (gatheringVortex != null)
							{
								if (gatheringVortex.GetStackAmount() + stacks >= (uint)t18_4p.GetAmount())
									caster.CastSpell(caster, ShamanSpells.ITEM_T18_LIGHTNING_VORTEX, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

								var newStacks = (byte)((gatheringVortex.GetStackAmount() + stacks) % t18_4p.GetAmount());

								if (newStacks != 0)
									gatheringVortex.SetStackAmount(newStacks);
								else
									gatheringVortex.Remove();
							}
							else
							{
								caster.CastSpell(caster, ShamanSpells.ITEM_T18_GATHERING_VORTEX, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, (int)stacks));
							}
						}

						var t18_2p = caster.GetAuraEffect(ShamanSpells.ITEM_T18_ELEMENTAL_2P_BONUS, 0);

						if (t18_2p != null)
							if (RandomHelper.randChance(t18_2p.GetAmount()))
							{
								caster.GetSpellHistory().ResetCooldown(ShamanSpells.EARTH_SHOCK, true);

								return;
							}

						aura.SetCharges(1);
						aura.SetUsingCharges(false);
					}
				}
				else
				{
					aura.SetCharges(Math.Min(aura.GetCharges() + 1, (byte)aurEff.GetAmount()));
					aura.SetUsingCharges(false);
					aura.RefreshDuration();

					if (aura.GetCharges() == aurEff.GetAmount())
						caster.CastSpell(caster, ShamanSpells.FULMINATION_INFO, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
				}
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}
}