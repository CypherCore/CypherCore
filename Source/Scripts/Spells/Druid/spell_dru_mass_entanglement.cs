using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(102359)]
public class spell_dru_mass_entanglement : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var targetList = new List<Unit>();
		GetCaster().GetAttackableUnitListInRange(targetList, 15.0f);

		if (targetList.Count != 0)
			foreach (var targets in targetList)
				GetCaster().AddAura(DruidSpells.SPELL_DRU_MASS_ENTANGLEMENT, targets);
	}
}