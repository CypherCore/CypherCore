// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 55516 - Gymer's Grab
internal class spell_q12919_gymers_grab : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(QuestSpellIds.RideGymer);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		if (!GetHitCreature())
			return;

		CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
		args.AddSpellMod(SpellValueMod.BasePoint0, 2);
		GetHitCreature().CastSpell(GetCaster(), QuestSpellIds.RideGymer, args);
		GetHitCreature().CastSpell(GetHitCreature(), QuestSpellIds.Grabbed, true);
	}
}