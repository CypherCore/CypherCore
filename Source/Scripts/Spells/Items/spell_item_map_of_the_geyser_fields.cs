using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_map_of_the_geyser_fields : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		var caster = GetCaster();

		if (caster.FindNearestCreature(CreatureIds.SouthSinkhole, 30.0f, true) ||
		    caster.FindNearestCreature(CreatureIds.NortheastSinkhole, 30.0f, true) ||
		    caster.FindNearestCreature(CreatureIds.NorthwestSinkhole, 30.0f, true))
			return SpellCastResult.SpellCastOk;

		SetCustomCastResultMessage(SpellCustomErrors.MustBeCloseToSinkhole);

		return SpellCastResult.CustomError;
	}
}