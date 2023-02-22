// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

/// Honor Talents

// Solitude - 211509
[SpellScript(211509)]
public class spell_dh_solitude : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();


	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SOLITUDE_BUFF, Difficulty.None) != null)
			return false;

		return true;
	}

	private void HandlePeriodic(AuraEffect UnnamedParameter)
	{
		PreventDefaultAction();

		var caster = GetCaster();

		if (caster == null || !GetSpellInfo().GetEffect(1).IsEffect())
			return;

		var range    = (float)GetSpellInfo().GetEffect(1).BasePoints;
		var allies   = new List<Unit>();
		var check    = new AnyFriendlyUnitInObjectRangeCheck(caster, caster, range, true);
		var searcher = new UnitListSearcher(caster, allies, check, GridType.All);
		Cell.VisitGrid(caster, searcher, range);
		allies.Remove(caster);

		if (allies.Count == 0 && !caster.HasAura(DemonHunterSpells.SOLITUDE_BUFF))
			caster.CastSpell(caster, DemonHunterSpells.SOLITUDE_BUFF, true);
		else if (allies.Count > 0)
			caster.RemoveAura(DemonHunterSpells.SOLITUDE_BUFF);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
	}
}