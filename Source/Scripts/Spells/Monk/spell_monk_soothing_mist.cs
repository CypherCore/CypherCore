// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(115175)]
public class spell_monk_soothing_mist : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		if (!GetCaster())
			return;

		var target = GetTarget();

		if (target != null)
			target.CastSpell(target, MonkSpells.SPELL_MONK_SOOTHING_MIST_VISUAL, true);

		var player = GetCaster().ToPlayer();

		if (player != null)
			if (target != null)
			{
				var      playerList = new List<Unit>();
				var      tempList   = new List<Creature>();
				var      statueList = new List<Creature>();
				Creature statue;

				player.GetPartyMembers(playerList);

				if (playerList.Count > 1)
				{
					playerList.Remove(target);
					playerList.Sort(new HealthPctOrderPred());
					playerList.Resize(1);
				}

				tempList   = player.GetCreatureListWithEntryInGrid(60849, 100.0f);
				statueList = player.GetCreatureListWithEntryInGrid(60849, 100.0f);

				for (var i = tempList.GetEnumerator(); i.MoveNext();)
				{
					var owner = i.Current.GetOwner();

					if (owner != null && owner == player && i.Current.IsSummon())
						continue;

					statueList.Remove(i.Current);
				}

				foreach (var itr in playerList)
					if (statueList.Count == 1)
					{
						statue = statueList.First();

						if (statue.GetOwner() != null && statue.GetOwner().GetGUID() == player.GetGUID())
							if (statue.GetOwner() && statue.GetOwner().GetGUID() == player.GetGUID())
								statue.CastSpell(statue.GetOwner().ToPlayer().GetSelectedUnit(), MonkSpells.SPELL_SERPENT_STATUE_SOOTHING_MIST, false);
					}
			}
	}

	private void HandleEffectPeriodic(AuraEffect UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster != null)
			if (GetTarget())
				// 25% to give 1 chi per tick
				if (RandomHelper.randChance(25))
					caster.CastSpell(caster, MonkSpells.SPELL_MONK_SOOTHING_MIST_ENERGIZE, true);
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		if (GetCaster())
		{
			var target = GetTarget();

			if (target != null)
				if (target.HasAura(MonkSpells.SPELL_MONK_SOOTHING_MIST_VISUAL))
					target.RemoveAura(MonkSpells.SPELL_MONK_SOOTHING_MIST_VISUAL);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicHeal));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}