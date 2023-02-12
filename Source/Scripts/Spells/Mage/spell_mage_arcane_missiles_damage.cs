using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(7268)]
public class spell_mage_arcane_missiles_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void CheckTarget(ref WorldObject target)
	{
		if (target == GetCaster())
			target = null;
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectTargetSelectHandler(CheckTarget, 0, Targets.UnitChannelTarget));
	}
}