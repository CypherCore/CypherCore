using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_tournament_duel : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.OnTournamentMount, GenericSpellIds.MountedDuel);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScriptEffect(uint effIndex)
	{
		var rider = GetCaster().GetCharmer();

		if (rider)
		{
			var playerTarget = GetHitPlayer();

			if (playerTarget)
			{
				if (playerTarget.HasAura(GenericSpellIds.OnTournamentMount) &&
				    playerTarget.GetVehicleBase())
					rider.CastSpell(playerTarget, GenericSpellIds.MountedDuel, true);

				return;
			}

			var unitTarget = GetHitUnit();

			if (unitTarget)
				if (unitTarget.GetCharmer() &&
				    unitTarget.GetCharmer().IsTypeId(TypeId.Player) &&
				    unitTarget.GetCharmer().HasAura(GenericSpellIds.OnTournamentMount))
					rider.CastSpell(unitTarget.GetCharmer(), GenericSpellIds.MountedDuel, true);
		}
	}
}