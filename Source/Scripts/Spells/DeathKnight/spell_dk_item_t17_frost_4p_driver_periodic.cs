using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

/// Item - Death Knight T17 Frost 4P Driver (Periodic) - 170205
[SpellScript(170205)]
public class spell_dk_item_t17_frost_4p_driver_periodic : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private struct eSpells
	{
		public const uint FrozenRunebladeMainHand = 165569;
		public const uint FrozenRunebladeOffHand = 178808;
		public const uint FrozenRunebladeStacks = 170202;
	}

	private void OnTick(AuraEffect UnnamedParameter)
	{
		Unit l_Caster = GetCaster();
		if (l_Caster == null)
		{
			return;
		}

		Unit l_Target = l_Caster.GetVictim();
		if (l_Target == null)
		{
			return;
		}

		Player l_Player = l_Caster.ToPlayer();
		if (l_Player != null)
		{
			Aura l_Aura = l_Player.GetAura(eSpells.FrozenRunebladeStacks);
			if (l_Aura != null)
			{
				Item l_MainHand = l_Player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
				if (l_MainHand != null)
				{
					l_Player.CastSpell(l_Target, eSpells.FrozenRunebladeMainHand, true);
				}

				Item l_OffHand = l_Player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
				if (l_OffHand != null)
				{
					l_Player.CastSpell(l_Target, eSpells.FrozenRunebladeOffHand, true);
				}

				l_Aura.DropCharge();
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicTriggerSpell));
	}
}