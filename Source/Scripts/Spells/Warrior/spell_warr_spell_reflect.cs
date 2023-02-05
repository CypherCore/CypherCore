using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
    // 23920 Spell Reflect
    [SpellScript(23920)]
    public class spell_warr_spell_reflect : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null || caster.GetTypeId() != TypeId.Player)
            {
                return;
            }

            Item item = caster.ToPlayer().GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
            if (item != null && item.GetTemplate().GetInventoryType() == InventoryType.Shield)
            {
                caster.CastSpell(caster, 146120, true);
            }
            else if (caster.GetFaction() == 1732) // Alliance
            {
                caster.CastSpell(caster, 147923, true);
            }
            else // Horde
            {
                caster.CastSpell(caster, 146122, true);
            }
        }

        private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null || caster.GetTypeId() != TypeId.Player)
            {
                return;
            }

            // Visuals
            caster.RemoveAura(146120);
            caster.RemoveAura(147923);
            caster.RemoveAura(146122);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnApply, 0, AuraType.ReflectSpells, AuraEffectHandleModes.RealOrReapplyMask));
            AuraEffects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ReflectSpells, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectRemove));
        }
    }
}
