// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script]
internal class spell_q12279_cast_net : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleActiveObject, 1, SpellEffectName.ActivateObject, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleActiveObject(uint effIndex)
	{
		GetHitGObj().SetLootState(LootState.JustDeactivated);
	}
}