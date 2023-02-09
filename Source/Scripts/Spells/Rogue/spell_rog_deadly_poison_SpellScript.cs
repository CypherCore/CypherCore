using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 2818 - Deadly Poison
internal class spell_rog_deadly_poison_SpellScript : SpellScript, ISpellBeforeHit, ISpellAfterHit
{
	private byte _stackAmount = 0;

	public void AfterHit()
	{
		if (_stackAmount < 5)
			return;

		Player player = GetCaster().ToPlayer();
		Unit   target = GetHitUnit();

		if (target != null)
		{
			Item item = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

			if (item == GetCastItem())
				item = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

			if (!item)
				return;

			// Item combat enchantments
			for (byte slot = 0; slot < (int)EnchantmentSlot.Max; ++slot)
			{
				var enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetEnchantmentId((EnchantmentSlot)slot));

				if (enchant == null)
					continue;

				for (byte s = 0; s < 3; ++s)
				{
					if (enchant.Effect[s] != ItemEnchantmentType.CombatSpell)
						continue;

					SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(enchant.EffectArg[s], Difficulty.None);

					if (spellInfo == null)
					{
						Log.outError(LogFilter.Spells, $"Player::CastItemCombatSpell Enchant {enchant.Id}, player (Name: {player.GetName()}, {player.GetGUID()}) cast unknown spell {enchant.EffectArg[s]}");

						continue;
					}

					// Proc only rogue poisons
					if (spellInfo.SpellFamilyName != SpellFamilyNames.Rogue ||
					    spellInfo.Dispel != DispelType.Poison)
						continue;

					// Do not reproc deadly
					if (spellInfo.SpellFamilyFlags & new FlagArray128(0x10000))
						continue;

					if (spellInfo.IsPositive())
						player.CastSpell(player, enchant.EffectArg[s], item);
					else
						player.CastSpell(target, enchant.EffectArg[s], item);
				}
			}
		}
	}

	public override bool Load()
	{
		// at this point CastItem must already be initialized
		return GetCaster().IsPlayer() && GetCastItem();
	}

	public void BeforeHit(SpellMissInfo missInfo)
	{
		if (missInfo != SpellMissInfo.None)
			return;

		Unit target = GetHitUnit();

		if (target != null)
		{
			// Deadly Poison
			AuraEffect aurEff = target.GetAuraEffect(AuraType.PeriodicDummy, SpellFamilyNames.Rogue, new FlagArray128(0x10000, 0x80000, 0), GetCaster().GetGUID());

			if (aurEff != null)
				_stackAmount = aurEff.GetBase().GetStackAmount();
		}
	}
}