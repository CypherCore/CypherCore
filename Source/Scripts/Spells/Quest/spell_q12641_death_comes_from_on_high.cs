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

[Script] // 51858 - Siphon of Acherus
internal class spell_q12641_death_comes_from_on_high : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(QuestSpellIds.ForgeCredit, QuestSpellIds.TownHallCredit, QuestSpellIds.ScarletHoldCredit, QuestSpellIds.ChapelCredit);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		uint spellId;

		switch (GetHitCreature().GetEntry())
		{
			case CreatureIds.NewAvalonForge:
				spellId = QuestSpellIds.ForgeCredit;

				break;
			case CreatureIds.NewAvalonTownHall:
				spellId = QuestSpellIds.TownHallCredit;

				break;
			case CreatureIds.ScarletHold:
				spellId = QuestSpellIds.ScarletHoldCredit;

				break;
			case CreatureIds.ChapelOfTheCrimsonFlame:
				spellId = QuestSpellIds.ChapelCredit;

				break;
			default:
				return;
		}

		GetCaster().CastSpell((Unit)null, spellId, true);
	}
}