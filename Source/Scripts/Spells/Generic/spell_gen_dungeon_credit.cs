// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
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
		var caster   = GetCaster();
		var instance = caster.GetInstanceScript();

		instance?.UpdateEncounterStateForSpellCast(GetSpellInfo().Id, caster);
	}
}