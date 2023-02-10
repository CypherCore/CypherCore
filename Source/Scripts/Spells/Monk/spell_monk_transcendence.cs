using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(101643)]
public class spell_monk_transcendence : SpellScript, ISpellOnSummon
{
	public const string MONK_TRANSCENDENCE_GUID = "MONK_TRANSCENDENCE_GUID";

	public void OnSummon(Creature creature)
	{
		DespawnSpirit(GetCaster());
		GetCaster().CastSpell(creature, MonkSpells.SPELL_MONK_TRANSCENDENCE_CLONE_TARGET, true);
		creature.CastSpell(creature, MonkSpells.SPELL_MONK_TRANSCENDENCE_VISUAL, true);
		creature.SetAIAnimKitId(2223); // Sniff Data
		creature.SetDisableGravity(true);
		creature.SetControlled(true, UnitState.Root);
		GetCaster().VariableStorage.Set(MONK_TRANSCENDENCE_GUID, creature.GetGUID());
	}

	public static Creature GetSpirit(Unit caster)
	{
		var spiritGuid = caster.VariableStorage.GetValue<ObjectGuid>(MONK_TRANSCENDENCE_GUID, default);

		if (spiritGuid.IsEmpty())
			return null;

		return ObjectAccessor.GetCreature(caster, spiritGuid);
	}

	public static void DespawnSpirit(Unit caster)
	{
		// Remove previous one if any
		var spirit = GetSpirit(caster);

		if (spirit != null)
			spirit.DespawnOrUnsummon();

		caster.VariableStorage.Remove(MONK_TRANSCENDENCE_GUID);
	}
}