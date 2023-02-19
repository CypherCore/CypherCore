// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

internal class spell_item_blessing_of_faith : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.BlessingOfLowerCityDruid, ItemSpellIds.BlessingOfLowerCityPaladin, ItemSpellIds.BlessingOfLowerCityPriest, ItemSpellIds.BlessingOfLowerCityShaman);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var unitTarget = GetHitUnit();

		if (unitTarget != null)
		{
			uint spellId = 0;

			switch (unitTarget.GetClass())
			{
				case Class.Druid:
					spellId = ItemSpellIds.BlessingOfLowerCityDruid;

					break;
				case Class.Paladin:
					spellId = ItemSpellIds.BlessingOfLowerCityPaladin;

					break;
				case Class.Priest:
					spellId = ItemSpellIds.BlessingOfLowerCityPriest;

					break;
				case Class.Shaman:
					spellId = ItemSpellIds.BlessingOfLowerCityShaman;

					break;
				default:
					return; // ignore for non-healing classes
			}

			var caster = GetCaster();
			caster.CastSpell(caster, spellId, true);
		}
	}
}