using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(MonkSpells.SPELL_MONK_FISTS_OF_FURY)]
public class spell_monk_fists_of_fury_visual_filter : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void RemoveInvalidTargets(List<WorldObject> targets)
	{
		targets.RemoveIf(new UnitAuraCheck<WorldObject>(true, 123154, GetCaster().GetGUID()));
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 1, Targets.UnitConeEnemy24));
	}
}