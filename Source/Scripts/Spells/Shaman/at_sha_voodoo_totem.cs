// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman
{
	// Spell 196935 - Voodoo Totem
	// AT - 11577
	[Script]
	public class at_sha_voodoo_totem : AreaTriggerAI
	{
		public at_sha_voodoo_totem(AreaTrigger areaTrigger) : base(areaTrigger)
		{
		}

		public override void OnUnitEnter(Unit unit)
		{
			var caster = at.GetCaster();

			if (caster == null || unit == null)
				return;

			if (caster.IsValidAttackTarget(unit))
			{
				caster.CastSpell(unit, TotemSpells.SPELL_TOTEM_VOODOO_EFFECT, true);
				caster.CastSpell(unit, TotemSpells.SPELL_TOTEM_VOODOO_COOLDOWN, true);
			}
		}

		public override void OnUnitExit(Unit unit)
		{
			unit.RemoveAurasDueToSpell(TotemSpells.SPELL_TOTEM_VOODOO_EFFECT, at.GetCasterGuid());
		}
	}
}