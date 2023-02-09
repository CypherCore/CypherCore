using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_gm_freeze : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.GmFreeze);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModStun, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		// Do what was done before to the Target in HandleFreezeCommand
		Player player = GetTarget().ToPlayer();

		if (player)
		{
			// stop combat + make player unattackable + Duel stop + stop some spells
			player.SetFaction(35);
			player.CombatStop();

			if (player.IsNonMeleeSpellCast(true))
				player.InterruptNonMeleeSpells(true);

			player.SetUnitFlag(UnitFlags.NonAttackable);

			// if player class = hunter || warlock Remove pet if alive
			if ((player.GetClass() == Class.Hunter) ||
			    (player.GetClass() == Class.Warlock))
			{
				Pet pet = player.GetPet();

				if (pet)
				{
					pet.SavePetToDB(PetSaveMode.AsCurrent);

					// not let dismiss dead pet
					if (pet.IsAlive())
						player.RemovePet(pet, PetSaveMode.NotInSlot);
				}
			}
		}
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		// Do what was done before to the Target in HandleUnfreezeCommand
		Player player = GetTarget().ToPlayer();

		if (player)
		{
			// Reset player faction + allow combat + allow duels
			player.SetFactionForRace(player.GetRace());
			player.RemoveUnitFlag(UnitFlags.NonAttackable);
			// save player
			player.SaveToDB();
		}
	}
}