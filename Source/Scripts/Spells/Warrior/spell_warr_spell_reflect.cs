// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// 23920 Spell Reflect
	[SpellScript(23920)]
	public class spell_warr_spell_reflect : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster == null || caster.GetTypeId() != TypeId.Player)
				return;

			var item = caster.ToPlayer().GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

			if (item != null && item.GetTemplate().GetInventoryType() == InventoryType.Shield)
				caster.CastSpell(caster, 146120, true);
			else if (caster.GetFaction() == 1732) // Alliance
				caster.CastSpell(caster, 147923, true);
			else // Horde
				caster.CastSpell(caster, 146122, true);
		}

		private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster == null || caster.GetTypeId() != TypeId.Player)
				return;

			// Visuals
			caster.RemoveAura(146120);
			caster.RemoveAura(147923);
			caster.RemoveAura(146122);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ReflectSpells, AuraEffectHandleModes.RealOrReapplyMask));
			AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ReflectSpells, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectRemove));
		}
	}
}