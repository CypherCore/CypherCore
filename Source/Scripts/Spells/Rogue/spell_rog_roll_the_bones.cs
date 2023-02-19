// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 315508 - Roll the Bones
internal class spell_rog_roll_the_bones : SpellScript, IHasSpellEffects
{
	private static readonly uint[] Spells =
	{
		RogueSpells.SkullAndCrossbones, RogueSpells.GrandMelee, RogueSpells.RuthlessPrecision, RogueSpells.TrueBearing, RogueSpells.BuriedTreasure, RogueSpells.Broadside
	};

	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(Spells);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var currentDuration = 0;

		foreach (var spellId in Spells)
		{
			var aura = GetCaster().GetAura(spellId);

			if (aura != null)
			{
				currentDuration = aura.GetDuration();
				GetCaster().RemoveAura(aura);
			}
		}

		var possibleBuffs = Spells.Shuffle().ToArray();

		// https://www.icy-veins.com/wow/outlaw-rogue-pve-dps-rotation-cooldowns-abilities
		// 1 Roll the Bones buff  : 100.0 % chance;
		// 2 Roll the Bones buffs : 19 % chance;
		// 5 Roll the Bones buffs : 1 % chance.
		var chance   = RandomHelper.IRand(1, 100);
		var numBuffs = 1;

		if (chance <= 1)
			numBuffs = 5;
		else if (chance <= 20)
			numBuffs = 2;

		for (var i = 0; i < numBuffs; ++i)
		{
			var                spellId = possibleBuffs[i];
			CastSpellExtraArgs args    = new(TriggerCastFlags.FullMask);
			args.AddSpellMod(SpellValueMod.Duration, GetSpellInfo().GetDuration() + currentDuration);
			GetCaster().CastSpell(GetCaster(), spellId, args);
		}
	}
}