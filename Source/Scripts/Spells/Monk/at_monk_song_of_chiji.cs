using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Monk;

[Script]
public class at_monk_song_of_chiji : AreaTriggerAI
{
	public at_monk_song_of_chiji(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		if (!caster.ToPlayer())
			return;

		if (unit != caster && caster.IsValidAttackTarget(unit))
			caster.CastSpell(unit, MonkSpells.SPELL_MONK_SONG_OF_CHIJI, true);
	}
}