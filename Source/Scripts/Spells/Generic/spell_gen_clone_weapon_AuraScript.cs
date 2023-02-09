using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_clone_weapon_AuraScript : AuraScript, IHasAuraEffects
{
	private uint prevItem;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.WeaponAura, GenericSpellIds.Weapon2Aura, GenericSpellIds.Weapon3Aura, GenericSpellIds.OffhandAura, GenericSpellIds.Offhand2Aura, GenericSpellIds.RangedAura);
	}

	public override bool Load()
	{
		prevItem = 0;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectRemove));
	}

	private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit caster = GetCaster();
		Unit target = GetTarget();

		if (!caster)
			return;

		switch (GetSpellInfo().Id)
		{
			case GenericSpellIds.WeaponAura:
			case GenericSpellIds.Weapon2Aura:
			case GenericSpellIds.Weapon3Aura:
			{
				prevItem = target.GetVirtualItemId(0);

				Player player = caster.ToPlayer();

				if (player)
				{
					Item mainItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

					if (mainItem)
						target.SetVirtualItem(0, mainItem.GetEntry());
				}
				else
				{
					target.SetVirtualItem(0, caster.GetVirtualItemId(0));
				}

				break;
			}
			case GenericSpellIds.OffhandAura:
			case GenericSpellIds.Offhand2Aura:
			{
				prevItem = target.GetVirtualItemId(1);

				Player player = caster.ToPlayer();

				if (player)
				{
					Item offItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

					if (offItem)
						target.SetVirtualItem(1, offItem.GetEntry());
				}
				else
				{
					target.SetVirtualItem(1, caster.GetVirtualItemId(1));
				}

				break;
			}
			case GenericSpellIds.RangedAura:
			{
				prevItem = target.GetVirtualItemId(2);

				Player player = caster.ToPlayer();

				if (player)
				{
					Item rangedItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

					if (rangedItem)
						target.SetVirtualItem(2, rangedItem.GetEntry());
				}
				else
				{
					target.SetVirtualItem(2, caster.GetVirtualItemId(2));
				}

				break;
			}
			default:
				break;
		}
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit target = GetTarget();

		switch (GetSpellInfo().Id)
		{
			case GenericSpellIds.WeaponAura:
			case GenericSpellIds.Weapon2Aura:
			case GenericSpellIds.Weapon3Aura:
				target.SetVirtualItem(0, prevItem);

				break;
			case GenericSpellIds.OffhandAura:
			case GenericSpellIds.Offhand2Aura:
				target.SetVirtualItem(1, prevItem);

				break;
			case GenericSpellIds.RangedAura:
				target.SetVirtualItem(2, prevItem);

				break;
			default:
				break;
		}
	}
}