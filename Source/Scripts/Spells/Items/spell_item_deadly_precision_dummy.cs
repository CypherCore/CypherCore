// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 71563 - Deadly Precision Dummy
internal class spell_item_deadly_precision_dummy : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.DeadlyPrecision);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(uint effIndex)
	{
		var                spellInfo = Global.SpellMgr.GetSpellInfo(ItemSpellIds.DeadlyPrecision, GetCastDifficulty());
		CastSpellExtraArgs args      = new(TriggerCastFlags.FullMask);
		args.AddSpellMod(SpellValueMod.AuraStack, (int)spellInfo.StackAmount);
		GetCaster().CastSpell(GetCaster(), spellInfo.Id, args);
	}
}