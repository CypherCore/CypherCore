// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 50894 - Zul'Drak Rat
internal class spell_q12527_zuldrak_rat : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(QuestSpellIds.SummonGorgedLurkingBasilisk);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScriptEffect(int effIndex)
	{
		if (GetHitAura() != null &&
		    GetHitAura().GetStackAmount() >= GetSpellInfo().StackAmount)
		{
			GetHitUnit().CastSpell((Unit)null, QuestSpellIds.SummonGorgedLurkingBasilisk, true);
			var basilisk = GetHitUnit().ToCreature();

			if (basilisk)
				basilisk.DespawnOrUnsummon();
		}
	}
}