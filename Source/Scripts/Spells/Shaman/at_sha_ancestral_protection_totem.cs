// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman
{
	//AT ID : 6336
	//Spell ID : 207495
	[Script]
	public class at_sha_ancestral_protection_totem : AreaTriggerAI
	{
		public int timeInterval;

		public at_sha_ancestral_protection_totem(AreaTrigger areatrigger) : base(areatrigger)
		{
		}

		public struct SpellsUsed
		{
			public const uint SPELL_SHAMAN_ANCESTRAL_PROTECTION_TOTEM_AURA = 207498;
		}

		public override void OnCreate()
		{
			var caster = at.GetCaster();

			if (caster == null)
				return;


			foreach (var itr in at.GetInsideUnits())
			{
				var target = ObjectAccessor.Instance.GetUnit(caster, itr);

				if (caster.IsFriendlyTo(target) || target == caster.GetOwner())
					if (!target.IsTotem())
						caster.CastSpell(target, SpellsUsed.SPELL_SHAMAN_ANCESTRAL_PROTECTION_TOTEM_AURA, true);
			}
		}

		public override void OnUnitEnter(Unit unit)
		{
			var caster = at.GetCaster();

			if (caster == null || unit == null)
				return;

			if (caster.IsFriendlyTo(unit) || unit == caster.GetOwner())
			{
				if (unit.IsTotem())
					return;
				else
					caster.CastSpell(unit, SpellsUsed.SPELL_SHAMAN_ANCESTRAL_PROTECTION_TOTEM_AURA, true);
			}
		}

		public override void OnUnitExit(Unit unit)
		{
			var caster = at.GetCaster();

			if (caster == null || unit == null)
				return;

			if (unit.HasAura(SpellsUsed.SPELL_SHAMAN_ANCESTRAL_PROTECTION_TOTEM_AURA) && unit.GetAura(SpellsUsed.SPELL_SHAMAN_ANCESTRAL_PROTECTION_TOTEM_AURA).GetCaster() == caster)
				unit.RemoveAura(SpellsUsed.SPELL_SHAMAN_ANCESTRAL_PROTECTION_TOTEM_AURA);
		}

		public override void OnRemove()
		{
			var caster = at.GetCaster();

			if (caster == null)
				return;

			foreach (var itr in at.GetInsideUnits())
			{
				var target = ObjectAccessor.Instance.GetUnit(caster, itr);

				if (!target.IsTotem())
					if (target.HasAura(SpellsUsed.SPELL_SHAMAN_ANCESTRAL_PROTECTION_TOTEM_AURA))
						target.RemoveAura(SpellsUsed.SPELL_SHAMAN_ANCESTRAL_PROTECTION_TOTEM_AURA);
			}
		}
	}
}