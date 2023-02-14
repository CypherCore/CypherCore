// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(90355)]
public class spell_hun_ancient_hysteria : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();
	readonly UnitAuraCheck<WorldObject> _ins = new(true, AncientHysteriaSpells.SPELL_HUNTER_INSANITY);
	readonly UnitAuraCheck<WorldObject> _dis = new(true, AncientHysteriaSpells.SPELL_MAGE_TEMPORAL_DISPLACEMENT);
	readonly UnitAuraCheck<WorldObject> _ex = new(true, AncientHysteriaSpells.SPELL_SHAMAN_EXHAUSTION);
	readonly UnitAuraCheck<WorldObject> _sa = new(true, AncientHysteriaSpells.SPELL_SHAMAN_SATED);

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (!Global.SpellMgr.HasSpellInfo(AncientHysteriaSpells.SPELL_HUNTER_INSANITY, Difficulty.None) ||
		    !Global.SpellMgr.HasSpellInfo(AncientHysteriaSpells.SPELL_MAGE_TEMPORAL_DISPLACEMENT, Difficulty.None) ||
		    !Global.SpellMgr.HasSpellInfo(AncientHysteriaSpells.SPELL_SHAMAN_EXHAUSTION, Difficulty.None) ||
		    !Global.SpellMgr.HasSpellInfo(AncientHysteriaSpells.SPELL_SHAMAN_SATED, Difficulty.None))
			return false;

		return true;
	}

	private void RemoveInvalidTargets(List<WorldObject> targets)
	{
		targets.RemoveIf(_ins);
		targets.RemoveIf(_dis);
		targets.RemoveIf(_ex);
		targets.RemoveIf(_sa);
	}

	private void ApplyDebuff()
	{
		var target = GetHitUnit();

		if (target != null)
			target.CastSpell(target, AncientHysteriaSpells.SPELL_HUNTER_INSANITY, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, (byte)255, Targets.UnitCasterAreaRaid));
	}
}