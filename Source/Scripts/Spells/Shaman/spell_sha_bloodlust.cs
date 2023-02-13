using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 2825 - Bloodlust
[SpellScript(2825)]
internal class spell_sha_bloodlust : SpellScript, ISpellAfterHit, IHasSpellEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.Sated, ShamanSpells.HunterInsanity, ShamanSpells.MageTemporalDisplacement, ShamanSpells.PetNetherwindsFatigued);
	}

	public void AfterHit()
	{
		var target = GetHitUnit();

		if (target)
			target.CastSpell(target, ShamanSpells.Sated, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 0, Targets.UnitCasterAreaRaid));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 1, Targets.UnitCasterAreaRaid));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void RemoveInvalidTargets(List<WorldObject> targets)
	{
		targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, ShamanSpells.Sated));
		targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, ShamanSpells.HunterInsanity));
		targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, ShamanSpells.MageTemporalDisplacement));
	}
}