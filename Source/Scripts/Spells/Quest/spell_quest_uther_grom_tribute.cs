// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 24195 - Grom's Tribute
internal class spell_quest_uther_grom_tribute : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(QuestSpellIds.GromsTrollTribute,
		                         QuestSpellIds.GromsTaurenTribute,
		                         QuestSpellIds.GromsUndeadTribute,
		                         QuestSpellIds.GromsOrcTribute,
		                         QuestSpellIds.GromsBloodelfTribute,
		                         QuestSpellIds.UthersHumanTribute,
		                         QuestSpellIds.UthersGnomeTribute,
		                         QuestSpellIds.UthersDwarfTribute,
		                         QuestSpellIds.UthersNightelfTribute,
		                         QuestSpellIds.UthersDraeneiTribute);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHit));
	}

	private void HandleScript(uint effIndex)
	{
		var caster = GetCaster().ToPlayer();

		if (!caster)
			return;

		uint spell = caster.GetRace() switch
		             {
			             Race.Troll    => QuestSpellIds.GromsTrollTribute,
			             Race.Tauren   => QuestSpellIds.GromsTaurenTribute,
			             Race.Undead   => QuestSpellIds.GromsUndeadTribute,
			             Race.Orc      => QuestSpellIds.GromsOrcTribute,
			             Race.BloodElf => QuestSpellIds.GromsBloodelfTribute,
			             Race.Human    => QuestSpellIds.UthersHumanTribute,
			             Race.Gnome    => QuestSpellIds.UthersGnomeTribute,
			             Race.Dwarf    => QuestSpellIds.UthersDwarfTribute,
			             Race.NightElf => QuestSpellIds.UthersNightelfTribute,
			             Race.Draenei  => QuestSpellIds.UthersDraeneiTribute,
			             _             => 0
		             };

		if (spell != 0)
			caster.CastSpell(caster, spell);
	}
}