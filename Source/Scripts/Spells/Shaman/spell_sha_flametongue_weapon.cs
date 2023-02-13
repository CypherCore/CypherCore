using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 318038 - Flametongue Weapon
[SpellScript(318038)]
internal class spell_sha_flametongue_weapon : SpellScript, ISpellOnCast, ISpellCheckCast
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.FlametongueWeaponEnchant);
	}

	public override bool Load()
	{
		return GetCaster().IsTypeId(TypeId.Player);
    }

    public SpellCastResult CheckCast()
    {
        var player = GetCaster().ToPlayer();
        var slot = EquipmentSlot.MainHand;

        if (player.GetPrimarySpecialization() == TalentSpecialization.ShamanEnhancement)
            slot = EquipmentSlot.OffHand;

        _item = player.GetItemByPos(InventorySlots.Bag0, slot);

		return _item == null || !_item.GetTemplate().IsWeapon() ? SpellCastResult.TargetNoWeapons : SpellCastResult.SpellCastOk;
    }

    public void OnCast()
	{
		GetCaster().CastSpell(_item, ShamanSpells.FlametongueWeaponEnchant, true);
	}

	Item _item;
}