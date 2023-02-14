// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Mage;

[Script]
public class at_mage_rune_of_power : AreaTriggerAI
{
	public at_mage_rune_of_power(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public struct UsingSpells
	{
		public const uint SPELL_MAGE_RUNE_OF_POWER_AURA = 116014;
	}

	public override void OnCreate()
	{
		//at->SetSpellXSpellVisualId(25943);
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster != null)
			if (unit.GetGUID() == caster.GetGUID())
				caster.CastSpell(unit, UsingSpells.SPELL_MAGE_RUNE_OF_POWER_AURA, true);
	}

	public override void OnUnitExit(Unit unit)
	{
		if (unit.HasAura(UsingSpells.SPELL_MAGE_RUNE_OF_POWER_AURA))
			unit.RemoveAurasDueToSpell(UsingSpells.SPELL_MAGE_RUNE_OF_POWER_AURA);
	}
}