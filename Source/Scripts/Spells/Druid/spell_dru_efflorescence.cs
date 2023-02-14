// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(145205)]
public class spell_dru_efflorescence : SpellScript, ISpellOnCast
{
	private struct eCreature
	{
		public static readonly uint NPC_EFFLORESCENCE = 47649;
	}

	public void OnCast()
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var efflorescence = caster.GetSummonedCreatureByEntry(eCreature.NPC_EFFLORESCENCE);

			if (efflorescence != null)
				efflorescence.DespawnOrUnsummon();
		}
	}
}