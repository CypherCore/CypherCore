using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 155793 - prayer of mending (Jump) - SPELL_PRIEST_PRAYER_OF_MENDING_JUMP
internal class spell_pri_prayer_of_mending_jump : SpellScript, IHasSpellEffects
{
	private SpellEffectInfo _healEffectDummy;
	private SpellInfo _spellInfoHeal;
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.PrayerOfMendingHeal, PriestSpells.PrayerOfMendingAura) && Global.SpellMgr.GetSpellInfo(PriestSpells.PrayerOfMendingHeal, Difficulty.None).GetEffect(0) != null;
	}

	public override bool Load()
	{
		_spellInfoHeal   = Global.SpellMgr.GetSpellInfo(PriestSpells.PrayerOfMendingHeal, Difficulty.None);
		_healEffectDummy = _spellInfoHeal.GetEffect(0);

		return true;
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(OnTargetSelect, 0, Targets.UnitSrcAreaAlly));
		SpellEffects.Add(new EffectHandler(HandleJump, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void OnTargetSelect(List<WorldObject> targets)
	{
		// Find the best Target - prefer players over pets
		bool foundPlayer = false;

		foreach (WorldObject worldObject in targets)
			if (worldObject.IsPlayer())
			{
				foundPlayer = true;

				break;
			}

		if (foundPlayer)
			targets.RemoveAll(new ObjectTypeIdCheck(TypeId.Player, false));

		// choose one random Target from targets
		if (targets.Count > 1)
		{
			WorldObject selected = targets.SelectRandom();
			targets.Clear();
			targets.Add(selected);
		}
	}

	private void HandleJump(uint effIndex)
	{
		Unit origCaster = GetOriginalCaster(); // the one that started the prayer of mending chain
		Unit target     = GetHitUnit();        // the Target we decided the aura should Jump to

		if (origCaster)
		{
			uint               basePoints = origCaster.SpellHealingBonusDone(target, _spellInfoHeal, (uint)_healEffectDummy.CalcValue(origCaster), DamageEffectType.Heal, _healEffectDummy);
			CastSpellExtraArgs args       = new(TriggerCastFlags.FullMask);
			args.AddSpellMod(SpellValueMod.AuraStack, GetEffectValue());
			args.AddSpellMod(SpellValueMod.BasePoint0, (int)basePoints);
			origCaster.CastSpell(target, PriestSpells.PrayerOfMendingAura, args);
		}
	}
}