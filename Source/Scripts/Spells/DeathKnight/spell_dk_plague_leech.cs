// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(123693)]
public class spell_dk_plague_leech : SpellScript, ISpellOnHit, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		var target = GetExplTargetUnit();

		if (target != null)
		{
			var diseases         = target.GetDiseasesByCaster(GetCaster().GetGUID());
			var requiredDiseases = (byte)(GetCaster().HasAura(152281) ? 1 : 2);

			if (diseases < requiredDiseases)
				//SetCustomCastResultMessage(159);
				return SpellCastResult.CustomError;
		}

		return SpellCastResult.SpellCastOk;
	}

	public void OnHit()
	{
		var player = GetCaster().ToPlayer();

		if (player == null)
			return;

		var runes = new List<byte>();

		for (byte i = 0; i < PlayerConst.MaxRunes; ++i)
			if (player.GetRuneCooldown(i) == player.GetRuneBaseCooldown())
				runes.Add(i);

		if (runes.Count > 0)
		{
			var effect = GetEffectInfo(0);

			if (effect != null)
				for (var i = 0; i < effect.BasePoints; ++i)
				{
					var rand = runes.SelectRandom();
					player.SetRuneCooldown(rand, 0);

					runes.Remove(rand);

					if (runes.Empty())
						break;
				}

			GetHitUnit().GetDiseasesByCaster(GetCaster().GetGUID(), true);
		}
	}
}