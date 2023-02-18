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

namespace Scripts.Spells.Priest;

[Script] // 155793 - prayer of mending (Jump) - PRAYER_OF_MENDING_JUMP
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
		var foundPlayer = false;

		foreach (var worldObject in targets)
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
			var selected = targets.SelectRandom();
			targets.Clear();
			targets.Add(selected);
		}
	}

	private void HandleJump(uint effIndex)
	{
		var origCaster = GetOriginalCaster(); // the one that started the prayer of mending chain
		var target     = GetHitUnit();        // the Target we decided the aura should Jump to

		if (origCaster)
		{
			var                basePoints = origCaster.SpellHealingBonusDone(target, _spellInfoHeal, (uint)_healEffectDummy.CalcValue(origCaster), DamageEffectType.Heal, _healEffectDummy, 1, GetSpell());
			CastSpellExtraArgs args       = new(TriggerCastFlags.FullMask);
			args.AddSpellMod(SpellValueMod.AuraStack, GetEffectValue());
			args.AddSpellMod(SpellValueMod.BasePoint0, (int)basePoints);
			origCaster.CastSpell(target, PriestSpells.PrayerOfMendingAura, args);
		}
	}
}