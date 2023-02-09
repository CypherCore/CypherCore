using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 131474 - Fishing
internal class spell_gen_fishing : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.FishingNoFishingPole, GenericSpellIds.FishingWithPole);
	}

	public override bool Load()
	{
		return GetCaster().IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		uint spellId;
		Item mainHand = GetCaster().ToPlayer().GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

		if (!mainHand ||
		    mainHand.GetTemplate().GetClass() != ItemClass.Weapon ||
		    (ItemSubClassWeapon)mainHand.GetTemplate().GetSubClass() != ItemSubClassWeapon.FishingPole)
			spellId = GenericSpellIds.FishingNoFishingPole;
		else
			spellId = GenericSpellIds.FishingWithPole;

		GetCaster().CastSpell(GetCaster(), spellId, false);
	}
}