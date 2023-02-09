using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_pet_summoned : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return GetCaster().IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		Player player = GetCaster().ToPlayer();

		if (player.GetLastPetNumber() != 0)
		{
			PetType newPetType = (player.GetClass() == Class.Hunter) ? PetType.Hunter : PetType.Summon;
			Pet     newPet     = new(player, newPetType);

			if (newPet.LoadPetFromDB(player, 0, player.GetLastPetNumber(), true))
			{
				// revive the pet if it is dead
				if (newPet.GetDeathState() != DeathState.Alive &&
				    newPet.GetDeathState() != DeathState.JustRespawned)
					newPet.SetDeathState(DeathState.JustRespawned);

				newPet.SetFullHealth();
				newPet.SetFullPower(newPet.GetPowerType());

				var summonScript = GetSpell().GetSpellScripts<ISpellOnSummon>();

				foreach (ISpellOnSummon summon in summonScript)
					summon.OnSummon(newPet);

				switch (newPet.GetEntry())
				{
					case CreatureIds.Doomguard:
					case CreatureIds.Infernal:
						newPet.SetEntry(CreatureIds.Imp);

						break;
					default:
						break;
				}
			}
		}
	}
}