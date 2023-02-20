// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(205604)]
public class spell_dh_reverse_magic : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var player = GetCaster();

		if (player == null || !player.ToPlayer())
			return;

		Unit _player = player.ToPlayer();

		var allies   = new List<Unit>();
		var check    = new AnyFriendlyUnitInObjectRangeCheck(_player, _player, 10.0f, true);
		var searcher = new UnitListSearcher(_player, allies, check, GridType.All);
		Cell.VisitGrid(_player, searcher, 10.0f);

		foreach (var unit in allies)
		{
			var auraListToRemove = new SortedSet<auraData>();
			var AuraList         = unit.GetAppliedAurasQuery();

			foreach (var iter in AuraList.IsPositive(false).GetResults())
			{
				var aura = iter.GetBase();

				if (aura == null)
					continue;

				var caster = aura.GetCaster();

				if (caster == null || caster.GetGUID() == unit.GetGUID())
					continue;

				if (!caster.IsWithinDist(unit, 40.0f))
					continue;

				if (aura.GetSpellInfo().Dispel != DispelType.Magic)
					continue;

				var creature = caster.ToCreature();

				if (creature != null)
				{
					if (creature.GetCreatureTemplate().Rank == CreatureEliteType.WorldBoss)
						continue;

					if (creature.GetCreatureTemplate().Rank == CreatureEliteType.Elite && creature.GetMap().IsDungeon())
						continue;
				}

				var targetAura = unit.AddAura(aura.GetId(), caster);

				if (targetAura != null)
				{
					foreach (var aurEff in targetAura.GetAuraEffects())
					{
						targetAura.SetMaxDuration(aura.GetMaxDuration());
						targetAura.SetDuration(aura.GetDuration());

						if (aura.GetEffect(aurEff.Key) != null)
						{
							var auraEffect = unit.GetAuraEffect(aura.GetId(), aurEff.Key);

							if (auraEffect == null)
								continue;

							var amount = auraEffect.GetAmount();

							if (auraEffect.GetAuraType() == AuraType.PeriodicDamage || auraEffect.GetAuraType() == AuraType.PeriodicDamagePercent)
								amount = (int)caster.SpellDamageBonusDone(unit, aura.GetSpellInfo(), amount, DamageEffectType.DOT, aura.GetSpellInfo().GetEffects()[aurEff.Key], auraEffect.GetBase().GetStackAmount(), GetSpell());

                            //targetAura->GetEffect(i)->VariableStorage.Set("DontRecalculatePerodics", true);
                            aurEff.Value.SetAmount(amount);
                            aurEff.Value.SetPeriodicTimer(auraEffect.GetPeriodicTimer());
						}
					}

					targetAura.SetNeedClientUpdateForTargets();
				}

				auraListToRemove.Add(new auraData(aura.GetId(), caster.GetGUID()));
			}

			foreach (var aura in auraListToRemove)
				unit.RemoveAura(aura.m_id, aura.m_casterGuid);

			auraListToRemove.Clear();
		}
	}
}