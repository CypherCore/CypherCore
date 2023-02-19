// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 51840 Despawn Fruit Tosser
internal class spell_q12634_despawn_fruit_tosser : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellEntry)
	{
		return ValidateSpellInfo(QuestSpellIds.BananasFallToGround, QuestSpellIds.OrangeFallsToGround, QuestSpellIds.PapayaFallsToGround, QuestSpellIds.SummonAdventurousDwarf);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var spellId = QuestSpellIds.BananasFallToGround;

		switch (RandomHelper.URand(0, 3))
		{
			case 1:
				spellId = QuestSpellIds.OrangeFallsToGround;

				break;
			case 2:
				spellId = QuestSpellIds.PapayaFallsToGround;

				break;
		}

		// sometimes, if you're lucky, you get a dwarf
		if (RandomHelper.randChance(5))
			spellId = QuestSpellIds.SummonAdventurousDwarf;

		GetCaster().CastSpell(GetCaster(), spellId, true);
	}
}