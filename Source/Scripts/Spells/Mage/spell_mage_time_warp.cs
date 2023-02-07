using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 80353 - Time Warp
internal class spell_mage_time_warp : SpellScript, ISpellAfterHit, IHasSpellEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.TemporalDisplacement, MageSpells.HunterInsanity, MageSpells.ShamanExhaustion, MageSpells.ShamanSated, MageSpells.PetNetherwindsFatigued);
	}

	public void AfterHit()
	{
		Unit target = GetHitUnit();

		if (target)
			target.CastSpell(target, MageSpells.TemporalDisplacement, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, SpellConst.EffectAll, Targets.UnitCasterAreaRaid));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void RemoveInvalidTargets(List<WorldObject> targets)
	{
		targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.TemporalDisplacement));
		targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.HunterInsanity));
		targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.ShamanExhaustion));
		targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, MageSpells.ShamanSated));
	}
}