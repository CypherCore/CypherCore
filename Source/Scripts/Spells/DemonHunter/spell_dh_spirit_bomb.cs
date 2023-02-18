// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(218679)]
public class spell_dh_spirit_bomb : SpellScript, ISpellOnHit, ISpellCheckCast
{
	readonly uint[] _ids = new uint[]
	                       {
		                       ShatteredSoulsSpells.LESSER_SOUL_SHARD, ShatteredSoulsSpells.SHATTERED_SOULS, ShatteredSoulsSpells.SHATTERED_SOULS_DEMON
	                       };

	private bool TryCastDamage(Unit caster, Unit target, uint spellId)
	{
		var at = caster.GetAreaTrigger(spellId);

		if (at != null)
		{
			caster.CastSpell(target, DemonHunterSpells.SPIRIT_BOMB_DAMAGE, true);
			at.Remove();

			return true;
		}

		return false;
	}

	public void OnHit()
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster == null || target == null)
			return;

		foreach (var spellId in _ids)

			if (TryCastDamage(caster, target, spellId))
				break;
	}

	public SpellCastResult CheckCast()
	{
		var caster = GetCaster();

		if (caster == null)
			return SpellCastResult.CantDoThatRightNow;

		if (!caster.GetAreaTrigger(ShatteredSoulsSpells.LESSER_SOUL_SHARD) && !caster.GetAreaTrigger(ShatteredSoulsSpells.SHATTERED_SOULS) && !caster.GetAreaTrigger(ShatteredSoulsSpells.SHATTERED_SOULS_DEMON))
			return SpellCastResult.CantDoThatRightNow;

		return SpellCastResult.SpellCastOk;
	}
}