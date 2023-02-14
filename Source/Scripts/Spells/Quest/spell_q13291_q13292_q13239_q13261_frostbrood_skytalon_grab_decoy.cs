// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 59318 - Grab Fake Soldier
internal class spell_q13291_q13292_q13239_q13261_frostbrood_skytalon_grab_decoy : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(QuestSpellIds.Ride);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		if (!GetHitCreature())
			return;

		// TO DO: Being triggered is hack, but in checkcast it doesn't pass aurastate requirements.
		// Beside that the decoy won't keep it's freeze animation State when enter.
		GetHitCreature().CastSpell(GetCaster(), QuestSpellIds.Ride, true);
	}
}