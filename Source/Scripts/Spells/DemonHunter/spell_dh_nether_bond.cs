// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(207810)]
public class spell_dh_nether_bond : SpellScript, ISpellOnCast
{
	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (!Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_NETHER_BOND_DAMAGE, Difficulty.None) || !Global.SpellMgr.HasSpellInfo(DemonHunterSpells.SPELL_DH_NETHER_BOND_PERIODIC, Difficulty.None))
			return false;

		return true;
	}

	public void OnCast()
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_NETHER_BOND_PERIODIC, true);
	}
}