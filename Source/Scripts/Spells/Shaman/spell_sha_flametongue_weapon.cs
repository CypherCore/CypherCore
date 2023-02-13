using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 318038 - Flametongue Weapon
[SpellScript(318038)]
internal class spell_sha_flametongue_weapon : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.FlametongueWeaponEnchant);
	}

	public override bool Load()
	{
		return GetCaster().IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffect, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleEffect(uint index)
	{
		var player = GetCaster().ToPlayer();
		var slot   = EquipmentSlot.MainHand;

		if (player.GetPrimarySpecialization() == TalentSpecialization.ShamanEnhancement)
			slot = EquipmentSlot.OffHand;

		var targetItem = player.GetItemByPos(InventorySlots.Bag0, slot);

		if (targetItem == null ||
		    !targetItem.GetTemplate().IsWeapon())
			return;

		GetCaster().CastSpell(targetItem, ShamanSpells.FlametongueWeaponEnchant, true);
	}
}