using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_dungeon_credit : SpellScript, ISpellAfterHit
{
	private bool _handled;

	public override bool Load()
	{
		_handled = false;

		return GetCaster().IsTypeId(TypeId.Unit);
	}

	public void AfterHit()
	{
		// This hook is executed for every Target, make sure we only credit instance once
		if (_handled)
			return;

		_handled = true;
		Unit           caster   = GetCaster();
		InstanceScript instance = caster.GetInstanceScript();

		instance?.UpdateEncounterStateForSpellCast(GetSpellInfo().Id, caster);
	}
}