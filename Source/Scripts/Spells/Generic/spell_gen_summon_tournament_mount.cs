using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_summon_tournament_mount : SpellScript, ISpellCheckCast
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.LanceEquipped);
	}

	public SpellCastResult CheckCast()
	{
		if (GetCaster().IsInDisallowedMountForm())
			GetCaster().RemoveAurasByType(AuraType.ModShapeshift);

		if (!GetCaster().HasAura(GenericSpellIds.LanceEquipped))
		{
			SetCustomCastResultMessage(SpellCustomErrors.MustHaveLanceEquipped);

			return SpellCastResult.CustomError;
		}

		return SpellCastResult.SpellCastOk;
	}
}