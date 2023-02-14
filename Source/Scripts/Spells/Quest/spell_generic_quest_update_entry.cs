// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script("spell_q55_sacred_cleansing", SpellEffectName.Dummy, 1u, CreatureIds.Morbent, CreatureIds.WeakenedMorbent, true, 0)]
[Script("spell_q10255_administer_antidote", SpellEffectName.Dummy, 0u, CreatureIds.Helboar, CreatureIds.Dreadtusk, true, 0)]
[Script("spell_q11515_fel_siphon_dummy", SpellEffectName.Dummy, 0u, CreatureIds.FelbloodInitiate, CreatureIds.EmaciatedFelblood, true, 0)]
internal class spell_generic_quest_update_entry : SpellScript, IHasSpellEffects
{
	private readonly uint _despawnTime;
	private readonly byte _effIndex;
	private readonly uint _newEntry;
	private readonly uint _originalEntry;
	private readonly bool _shouldAttack;

	private readonly SpellEffectName _spellEffect;

	public spell_generic_quest_update_entry(SpellEffectName spellEffect, uint effIndex, uint originalEntry, uint newEntry, bool shouldAttack, uint despawnTime)
	{
		_spellEffect   = spellEffect;
		_effIndex      = (byte)effIndex;
		_originalEntry = originalEntry;
		_newEntry      = newEntry;
		_shouldAttack  = shouldAttack;
		_despawnTime   = despawnTime;
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, _effIndex, _spellEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		var creatureTarget = GetHitCreature();

		if (creatureTarget)
			if (!creatureTarget.IsPet() &&
			    creatureTarget.GetEntry() == _originalEntry)
			{
				creatureTarget.UpdateEntry(_newEntry);

				if (_shouldAttack)
					creatureTarget.EngageWithTarget(GetCaster());

				if (_despawnTime != 0)
					creatureTarget.DespawnOrUnsummon(TimeSpan.FromMilliseconds(_despawnTime));
			}
	}
}