using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(76613)]
public class spell_mastery_icicles_proc : AuraScript, IAuraCheckProc, IHasAuraEffects
{

	public static readonly uint[][] Icicles =
	{
		new uint[] {148012, 148017, 148013},
		new uint[] {148013, 148018, 148014},
		new uint[] {148014, 148019, 148015},
		new uint[] {148015, 148020, 148016},
		new uint[] {148016, 148021, 148012}
	};

	public static readonly uint[] IcicleAuras = { 214124, 214125, 214126, 214127, 214130 };
	public static readonly uint[] IcicleHits = { 148017, 148018, 148019, 148020, 148021 };

	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		bool _spellCanProc = (eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FROSTBOLT || eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_FROSTBOLT_TRIGGER);

		if (_spellCanProc)
		{
			return true;
		}
		return false;
	}

	private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		Unit target = eventInfo.GetDamageInfo().GetVictim();
		Unit caster = eventInfo.GetDamageInfo().GetAttacker();
		if (target == null || caster == null)
		{
			return;
		}

		Player player = caster.ToPlayer();

		if (player == null)
		{
			return;
		}

		// Calculate damage
		var hitDamage = eventInfo.GetDamageInfo().GetDamage() + eventInfo.GetDamageInfo().GetAbsorb();

		// if hitDamage == 0 we have a miss, so we need to except this variant
		if (hitDamage != 0)
		{
			bool icilesAddSecond = false;

			if (caster.HasAura(MageSpells.SPELL_MAGE_ICE_NINE))
			{
				if (RandomHelper.randChance(20))
				{
					icilesAddSecond = true;
				}
			}

			hitDamage *= (uint)((player.m_activePlayerData.Mastery * 2.25f) / 100.0f);

			// Prevent huge hits on player after hitting low level creatures
			if (player.GetLevel() > target.GetLevel())
			{
				hitDamage = (uint)Math.Min((int)hitDamage, (int)target.GetMaxHealth());
			}

			// We need to get the first free icicle slot
			sbyte icicleFreeSlot       = -1; // -1 means no free slot
			sbyte icicleSecondFreeSlot = -1; // -1 means no free slot
			for (sbyte l_I = 0; l_I < 5; ++l_I)
			{
				if (!player.HasAura(IcicleAuras[l_I]))
				{
					icicleFreeSlot = l_I;
					if (icilesAddSecond && icicleFreeSlot != 5)
					{
						icicleSecondFreeSlot = (sbyte)(l_I + 1);
					}
					break;
				}
			}

			if (icicleFreeSlot == -1)
			{
				// We need to find the icicle with the smallest duration.
				sbyte smallestIcicle = 0;
				int   minDuration    = 0xFFFFFF;
				for (sbyte i = 0; i < 5; i++)
				{
					Aura tmpCurrentAura = player.GetAura(IcicleAuras[i]);
					if (tmpCurrentAura != null)
					{
						if (minDuration > tmpCurrentAura.GetDuration())
						{
							minDuration    = tmpCurrentAura.GetDuration();
							smallestIcicle = i;
						}
					}
				}

				// Launch the icicle with the smallest duration
				AuraEffect currentIcicleAuraEffect = player.GetAuraEffect(IcicleAuras[smallestIcicle], 0);
				if (currentIcicleAuraEffect != null)
				{
					int basePoints = currentIcicleAuraEffect.GetAmount();

					if (caster.HasAura(MageSpells.SPELL_MAGE_BLACK_ICE))
					{
						if (RandomHelper.randChance(20))
						{
							basePoints *= 2;
						}
					}

					player.CastSpell(target, IcicleHits[smallestIcicle], true);
					player.CastSpell(target, MageSpells.SPELL_MAGE_ICICLE_DAMAGE, new CastSpellExtraArgs(SpellValueMod.BasePoint0, basePoints));
					player.RemoveAura(IcicleAuras[smallestIcicle]);
				}

				icicleFreeSlot = smallestIcicle;
				// No break because we'll add the icicle in the next case
			}

			switch (icicleFreeSlot)
			{
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
				{

					Aura currentIcicleAura = player.AddAura(IcicleAuras[icicleFreeSlot], player);
					if (currentIcicleAura != null)
					{
						AuraEffect effect = currentIcicleAura.GetEffect(0);
						if (effect != null)
						{
							effect.SetAmount(hitDamage);
						}

						player.AddAura(MageSpells.SPELL_MAGE_ICICLE_AURA, player);

						if (caster.HasSpell(MageSpells.SPELL_MAGE_GLACIAL_SPIKE))
						{
							Aura glacialSpikeProc = player.GetAura(MageSpells.SPELL_MAGE_ICICLE_AURA);
							if (glacialSpikeProc != null)
							{
								if (glacialSpikeProc.GetStackAmount() == 5)
								{
									player.CastSpell(player, MageSpells.SPELL_MAGE_GLACIAL_SPIKE_PROC, true);
								}
							}
						}
					}
					break;
				}
			}

			if (icicleSecondFreeSlot == -1 && icilesAddSecond)
			{
				// We need to find the icicle with the smallest duration.
				sbyte smallestIcicle = 0;
				int   minDuration    = 0xFFFFFF;
				for (sbyte i = 0; i < 5; i++)
				{
					Aura tmpCurrentAura = player.GetAura(IcicleAuras[i]);
					if (tmpCurrentAura != null)
					{
						if (minDuration > tmpCurrentAura.GetDuration())
						{
							minDuration    = tmpCurrentAura.GetDuration();
							smallestIcicle = i;
						}
					}
				}

				// Launch the icicle with the smallest duration
				AuraEffect currentIcicleAuraEffect = player.GetAuraEffect(IcicleAuras[smallestIcicle], 0);
				if (currentIcicleAuraEffect != null)
				{
					var basePoints = currentIcicleAuraEffect.GetAmount();

					if (caster.HasAura(MageSpells.SPELL_MAGE_BLACK_ICE))
					{
						if (RandomHelper.randChance(20))
						{
							basePoints *= 2;
						}
					}

					player.CastSpell(target, IcicleHits[smallestIcicle], true);
					player.CastSpell(target, MageSpells.SPELL_MAGE_ICICLE_DAMAGE, new CastSpellExtraArgs(SpellValueMod.BasePoint0, basePoints));
					player.RemoveAura(IcicleAuras[smallestIcicle]);
				}

				icicleSecondFreeSlot = smallestIcicle;
				// No break because we'll add the icicle in the next case
			}

			switch (icicleSecondFreeSlot)
			{
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
				{
					Aura currentIcicleAura = player.AddAura(IcicleAuras[icicleSecondFreeSlot], player);
					if (currentIcicleAura != null)
					{
						AuraEffect effect = currentIcicleAura.GetEffect(0);
						if (effect != null)
						{
							effect.SetAmount(hitDamage);
						}

						player.AddAura(MageSpells.SPELL_MAGE_ICICLE_AURA, player);

						if (caster.HasSpell(MageSpells.SPELL_MAGE_GLACIAL_SPIKE))
						{
							Aura glacialSpikeProc = player.GetAura(MageSpells.SPELL_MAGE_ICICLE_AURA);
							if (glacialSpikeProc != null)
							{
								if (glacialSpikeProc.GetStackAmount() == 5)
								{
									player.CastSpell(player, MageSpells.SPELL_MAGE_GLACIAL_SPIKE_PROC, true);
								}
							}
						}
					}
					break;
				}
			}
		}
	}

	public override void Register()
	{

		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}