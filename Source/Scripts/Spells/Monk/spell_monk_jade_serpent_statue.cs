using System.Collections.Generic;
using System.Linq;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(115313)]
public class spell_monk_jade_serpent_statue : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var player = caster.ToPlayer();

		if (player == null)
			return;

		var serpentStatueList = player.GetCreatureListWithEntryInGrid(MonkSpells.MONK_NPC_JADE_SERPENT_STATUE, 500.0f);

		serpentStatueList.RemoveIf(c => c.GetOwner() == null || c.GetOwner() != player || !c.IsSummon());

		if (serpentStatueList.Count >= 1)
			serpentStatueList.Last().ToTempSummon().UnSummon();
	}
}