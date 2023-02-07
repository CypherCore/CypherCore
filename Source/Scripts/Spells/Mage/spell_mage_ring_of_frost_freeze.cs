using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 82691 - Ring of Frost (freeze efect)
internal class spell_mage_ring_of_frost_freeze : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.RingOfFrostSummon, MageSpells.RingOfFrostFreeze) && !Global.SpellMgr.GetSpellInfo(MageSpells.RingOfFrostSummon, Difficulty.None).GetEffects().Empty();
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		WorldLocation dest      = GetExplTargetDest();
		float         outRadius = Global.SpellMgr.GetSpellInfo(MageSpells.RingOfFrostSummon, GetCastDifficulty()).GetEffect(0).CalcRadius();
		float         inRadius  = 6.5f;

		targets.RemoveAll(target =>
		                  {
			                  Unit unit = target.ToUnit();

			                  if (!unit)
				                  return true;

			                  return unit.HasAura(MageSpells.RingOfFrostDummy) || unit.HasAura(MageSpells.RingOfFrostFreeze) || unit.GetExactDist(dest) > outRadius || unit.GetExactDist(dest) < inRadius;
		                  });
	}
}