// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[Script("areatrigger_dh_sigil_of_silence", DemonHunterSpells.SigilOfSilenceAoe)]
[Script("areatrigger_dh_sigil_of_misery", DemonHunterSpells.SigilOfMiseryAoe)]
[Script("areatrigger_dh_sigil_of_flame", DemonHunterSpells.SigilOfFlameAoe)]
internal class areatrigger_dh_generic_sigil : AreaTriggerAI
{
	private readonly uint _trigger;

	public areatrigger_dh_generic_sigil(AreaTrigger at, uint trigger) : base(at)
	{
		_trigger = trigger;
	}

	public override void OnRemove()
	{
		var caster = at.GetCaster();

		caster?.CastSpell(at.GetPosition(), _trigger, new CastSpellExtraArgs());
	}
}