// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script("spell_item_arcanite_dragonling", ItemSpellIds.ArcaniteDragonling)]
[Script("spell_item_gnomish_battle_chicken", ItemSpellIds.BattleChicken)]
[Script("spell_item_mechanical_dragonling", ItemSpellIds.MechanicalDragonling)]
[Script("spell_item_mithril_mechanical_dragonling", ItemSpellIds.MithrilMechanicalDragonling)]
internal class spell_item_trigger_spell : SpellScript, IHasSpellEffects
{
	private readonly uint _triggeredSpellId;

	public spell_item_trigger_spell(uint triggeredSpellId)
	{
		_triggeredSpellId = triggeredSpellId;
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(_triggeredSpellId);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(uint effIndex)
	{
		var caster = GetCaster();
		var item   = GetCastItem();

		if (item)
			caster.CastSpell(caster, _triggeredSpellId, new CastSpellExtraArgs(item));
	}
}