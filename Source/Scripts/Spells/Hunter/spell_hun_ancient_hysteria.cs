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
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();
	UnitAuraCheck<WorldObject> _ins = new UnitAuraCheck<WorldObject>(true, AncientHysteriaSpells.SPELL_HUNTER_INSANITY);
	UnitAuraCheck<WorldObject> _dis = new UnitAuraCheck<WorldObject>(true, AncientHysteriaSpells.SPELL_MAGE_TEMPORAL_DISPLACEMENT);
	UnitAuraCheck<WorldObject> _ex = new UnitAuraCheck<WorldObject>(true, AncientHysteriaSpells.SPELL_SHAMAN_EXHAUSTION);
	UnitAuraCheck<WorldObject> _sa = new UnitAuraCheck<WorldObject>(true, AncientHysteriaSpells.SPELL_SHAMAN_SATED);

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (!Global.SpellMgr.HasSpellInfo(AncientHysteriaSpells.SPELL_HUNTER_INSANITY, Difficulty.None) ||
		    !Global.SpellMgr.HasSpellInfo(AncientHysteriaSpells.SPELL_MAGE_TEMPORAL_DISPLACEMENT, Difficulty.None) ||
		    !Global.SpellMgr.HasSpellInfo(AncientHysteriaSpells.SPELL_SHAMAN_EXHAUSTION, Difficulty.None) ||
		    !Global.SpellMgr.HasSpellInfo(AncientHysteriaSpells.SPELL_SHAMAN_SATED, Difficulty.None))
		{
			return false;
		}
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
		Unit target = GetHitUnit();
		if (target != null)
		{
			target.CastSpell(target, AncientHysteriaSpells.SPELL_HUNTER_INSANITY, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, (byte)255, Targets.UnitCasterAreaRaid));
	}
}