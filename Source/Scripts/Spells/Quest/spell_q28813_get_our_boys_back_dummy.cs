using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 93072 - Get Our Boys Back Dummy
internal class spell_q28813_get_our_boys_back_dummy : SpellScript, ISpellOnCast
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(QuestSpellIds.RenewedLife);
	}

	public void OnCast()
	{
		Unit     caster                   = GetCaster();
		Creature injuredStormwindInfantry = caster.FindNearestCreature(CreatureIds.InjuredStormwindInfantry, 5.0f, true);

		if (injuredStormwindInfantry)
		{
			injuredStormwindInfantry.SetCreatorGUID(caster.GetGUID());
			injuredStormwindInfantry.CastSpell(injuredStormwindInfantry, QuestSpellIds.RenewedLife, true);
		}
	}
}