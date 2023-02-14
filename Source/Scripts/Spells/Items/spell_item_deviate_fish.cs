// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 8063 Deviate Fish
internal class spell_item_deviate_fish : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return GetCaster().GetTypeId() == TypeId.Player;
	}

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.Sleepy, ItemSpellIds.Invigorate, ItemSpellIds.Shrink, ItemSpellIds.PartyTime, ItemSpellIds.HealthySpirit, ItemSpellIds.Rejuvenation);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(uint effIndex)
	{
		var caster  = GetCaster();
		var spellId = RandomHelper.RAND(ItemSpellIds.Sleepy, ItemSpellIds.Invigorate, ItemSpellIds.Shrink, ItemSpellIds.PartyTime, ItemSpellIds.HealthySpirit, ItemSpellIds.Rejuvenation);
		caster.CastSpell(caster, spellId, true);
	}
}