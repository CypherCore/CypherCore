// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(119996)]
public class spell_monk_transcendence_transfer : SpellScript, ISpellOnCast, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		var caster = GetCaster();

		if (caster == null)
			return SpellCastResult.Error;

		Unit spirit = spell_monk_transcendence.GetSpirit(caster);

		if (spirit == null)
		{
			SetCustomCastResultMessage(SpellCustomErrors.YouHaveNoSpiritActive);

			return SpellCastResult.CustomError;
		}

		if (!spirit.IsWithinDist(caster, GetSpellInfo().GetMaxRange(true, caster, GetSpell())))
			return SpellCastResult.OutOfRange;

		return SpellCastResult.SpellCastOk;
	}

	public void OnCast()
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		Unit spirit = spell_monk_transcendence.GetSpirit(caster);

		if (spirit == null)
			return;

		caster.NearTeleportTo(spirit, true);
		spirit.NearTeleportTo(caster, true);
	}
}