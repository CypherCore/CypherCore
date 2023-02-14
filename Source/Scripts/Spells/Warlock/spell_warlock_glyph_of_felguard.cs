// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// 30146, 112870 - Summon Felguard, Summon Wrathguard
	[SpellScript(new uint[]
	             {
		             30146, 112870
	             })]
	public class spell_warlock_glyph_of_felguard : SpellScript, ISpellAfterHit
	{
		public void AfterHit()
		{
			var caster = GetCaster().ToPlayer();

			if (caster != null)
			{
				if (!caster.HasAura(WarlockSpells.GLYPH_OF_FELGUARD))
					return;

				uint itemEntry = 0;

				for (int i = InventorySlots.ItemStart; i < InventorySlots.ItemEnd; ++i)
				{
					var pItem = caster.GetItemByPos(InventorySlots.Bag0, (byte)i);

					if (pItem != null)
					{
						var itemplate = pItem.GetTemplate();

						if (itemplate != null)
							if (itemplate.GetClass() == ItemClass.Weapon && (itemplate.GetSubClass() == (uint)ItemSubClassWeapon.Sword2 || itemplate.GetSubClass() == (uint)ItemSubClassWeapon.Axe2 || itemplate.GetSubClass() == (uint)ItemSubClassWeapon.Exotic2 || itemplate.GetSubClass() == (uint)ItemSubClassWeapon.Mace2 || itemplate.GetSubClass() == (uint)ItemSubClassWeapon.Polearm))
							{
								itemEntry = itemplate.GetId();

								break;
							}
					}
				}


				var pet = ObjectAccessor.GetPet(caster, caster.GetPetGUID());

				if (pet != null)
				{
					for (byte i = 0; i < 3; ++i)
						pet.SetVirtualItem(i, 0);

					pet.SetVirtualItem(0, itemEntry);
				}
			}
		}
	}
}