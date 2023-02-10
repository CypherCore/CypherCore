using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(new uint[]
             {
	             120761, 121414
             })]
public class spell_hun_glaive_toss_damage : SpellScript, IHasSpellEffects, ISpellOnHit
{
	public List<ISpellEffect> SpellEffects => new();


	private ObjectGuid mainTargetGUID = new();

	private bool Load()
	{
		mainTargetGUID = ObjectGuid.Empty;

		return true;
	}

	private void CorrectDamageRange(List<WorldObject> targets)
	{
		targets.Clear();

		var targetList = new List<Unit>();
		var radius     = 50.0f;

		GetCaster().GetAnyUnitListInRange(targetList, radius);

		foreach (var itr in targetList)
			if (itr.HasAura(HunterSpells.SPELL_HUNTER_GLAIVE_TOSS_AURA))
			{
				mainTargetGUID = itr.GetGUID();

				break;
			}

		if (mainTargetGUID == default)
			return;

		var target = ObjectAccessor.Instance.GetUnit(GetCaster(), mainTargetGUID);

		if (target == null)
			return;

		targets.Add(target);

		foreach (var itr in targetList)
			if (itr.IsInBetween(GetCaster(), target, 5.0f))
				if (!GetCaster().IsFriendlyTo(itr))
					targets.Add(itr);
	}

	private void CorrectSnareRange(List<WorldObject> targets)
	{
		targets.Clear();

		var targetList = new List<Unit>();
		var radius     = 50.0f;

		GetCaster().GetAnyUnitListInRange(targetList, radius);

		foreach (var itr in targetList)
			if (itr.HasAura(HunterSpells.SPELL_HUNTER_GLAIVE_TOSS_AURA))
			{
				mainTargetGUID = itr.GetGUID();

				break;
			}

		if (mainTargetGUID == default)
			return;

		if (mainTargetGUID == default)
			return;

		var target = ObjectAccessor.Instance.GetUnit(GetCaster(), mainTargetGUID);

		if (target == null)
			return;

		targets.Add(target);

		foreach (var itr in targetList)
			if (itr.IsInBetween(GetCaster(), target, 5.0f))
				if (!GetCaster().IsFriendlyTo(itr))
					targets.Add(itr);
	}

	public void OnHit()
	{
		if (mainTargetGUID == default)
			return;

		var target = ObjectAccessor.Instance.GetUnit(GetCaster(), mainTargetGUID);

		if (target == null)
			return;

		if (GetHitUnit())
			if (GetHitUnit() == target)
				SetHitDamage(GetHitDamage() * 4);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(CorrectDamageRange, 0, Targets.UnitDestAreaEnemy));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(CorrectSnareRange, 1, Targets.UnitDestAreaEnemy));
	}
}