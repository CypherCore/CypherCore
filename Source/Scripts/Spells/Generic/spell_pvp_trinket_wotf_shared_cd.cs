// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script("spell_pvp_trinket_shared_cd", GenericSpellIds.WillOfTheForsakenCooldownTrigger)]
[Script("spell_wotf_shared_cd", GenericSpellIds.WillOfTheForsakenCooldownTriggerWotf)]
internal class spell_pvp_trinket_wotf_shared_cd : SpellScript, ISpellAfterCast
{
	private readonly uint _triggered;

	public spell_pvp_trinket_wotf_shared_cd(uint triggered)
	{
		_triggered = triggered;
	}

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(_triggered);
	}

	public void AfterCast()
	{
		/*
			 * @workaround: PendingCast flag normally means 'triggered' spell, however
			 * if the spell is cast triggered, the core won't send SMSG_GO packet
			 * so client never registers the cooldown (see Spell::IsNeedSendToClient)
			 *
			 * ServerToClient: SMSG_GO (0x0132) Length: 42 ConnIdx: 0 Time: 07/19/2010 02:32:35.000 Number: 362675
			 * Caster GUID: Full: Player
			 * Caster Unit GUID: Full: Player
			 * Cast Count: 0
			 * Spell ID: 72752 (72752)
			 * Cast Flags: PendingCast, Unknown3, Unknown7 (265)
			 * Time: 3901468825
			 * Hit Count: 1
			 * [0] Hit GUID: Player
			 * Miss Count: 0
			 * Target Flags: Unit (2)
			 * Target GUID: 0x0
			*/

		// Spell flags need further research, until then just cast not triggered
		GetCaster().CastSpell((Unit)null, _triggered, false);
	}
}