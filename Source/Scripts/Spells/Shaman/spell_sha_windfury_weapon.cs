// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 33757 - Windfury Weapon
[SpellScript(33757)]
internal class spell_sha_windfury_weapon : SpellScript, ISpellOnCast, ISpellCheckCast
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.WindfuryEnchantment);
	}

	public override bool Load()
	{
		return GetCaster().IsPlayer();
    }

    public SpellCastResult CheckCast()
    {
        _item = GetCaster().ToPlayer().GetWeaponForAttack(WeaponAttackType.BaseAttack, false);
        return _item == null || !_item.GetTemplate().IsWeapon() ? SpellCastResult.TargetNoWeapons : SpellCastResult.SpellCastOk;
    }

    public void OnCast()
	{
		GetCaster().CastSpell(_item, ShamanSpells.WindfuryEnchantment, GetSpell());
    }

    Item _item;
}