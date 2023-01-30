// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Dynamic;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Maps.Checks;
using Game.Maps.Notifiers;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IUnit;

namespace Game.Spells.Auras.EffectHandlers
{
    public partial class AuraEffect
    {
        /********************************/
        /***      HEAL & ENERGIZE     ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModPowerRegen)]
        private void HandleModPowerRegen(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // Update manaregen value
            if (GetMiscValue() == (int)PowerType.Mana)
                target.ToPlayer().UpdateManaRegen();
            else if (GetMiscValue() == (int)PowerType.Runes)
                target.ToPlayer().UpdateAllRunesRegen();
            // other powers are not immediate effects - implemented in Player.Regenerate, Creature.Regenerate
        }

        [AuraEffectHandler(AuraType.ModPowerRegenPercent)]
        private void HandleModPowerRegenPCT(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            HandleModPowerRegen(aurApp, mode, apply);
        }

        [AuraEffectHandler(AuraType.ModManaRegenPct)]
        private void HandleModManaRegenPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsPlayer())
                return;

            target.ToPlayer().UpdateManaRegen();
        }

        [AuraEffectHandler(AuraType.ModIncreaseHealth)]
        [AuraEffectHandler(AuraType.ModIncreaseHealth2)]
        [AuraEffectHandler(AuraType.ModMaxHealth)]
        private void HandleAuraModIncreaseHealth(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            int amt = apply ? GetAmount() : -GetAmount();

            if (amt < 0)
                target.ModifyHealth(Math.Max((int)(1 - target.GetHealth()), amt));

            target.HandleStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Total, GetAmount(), apply);

            if (amt > 0)
                target.ModifyHealth(amt);
        }

        private void HandleAuraModIncreaseMaxHealth(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            float percent = target.GetHealthPct();

            target.HandleStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Total, GetAmount(), apply);

            // refresh percentage
            if (target.GetHealth() > 0)
            {
                uint newHealth = (uint)Math.Max(target.CountPctFromMaxHealth((int)percent), 1);
                target.SetHealth(newHealth);
            }
        }

        [AuraEffectHandler(AuraType.ModIncreaseEnergy)]
        private void HandleAuraModIncreaseEnergy(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();
            PowerType powerType = (PowerType)GetMiscValue();

            UnitMods unitMod = UnitMods.PowerStart + (int)powerType;
            target.HandleStatFlatModifier(unitMod, UnitModifierFlatType.Total, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModIncreaseEnergyPercent)]
        private void HandleAuraModIncreaseEnergyPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();
            PowerType powerType = (PowerType)GetMiscValue();

            UnitMods unitMod = UnitMods.PowerStart + (int)powerType;

            // Save old powers for further calculation
            int oldPower = target.GetPower(powerType);
            int oldMaxPower = target.GetMaxPower(powerType);

            // Handle aura effect for max power
            if (apply)
            {
                target.ApplyStatPctModifier(unitMod, UnitModifierPctType.Total, GetAmount());
            }
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModIncreaseEnergyPercent,
                                                             aurEff =>
                                                             {
                                                                 if (aurEff.GetMiscValue() == (int)powerType)
                                                                     return true;

                                                                 return false;
                                                             });

                amount *= target.GetTotalAuraMultiplier(AuraType.ModMaxPowerPct,
                                                        aurEff =>
                                                        {
                                                            if (aurEff.GetMiscValue() == (int)powerType)
                                                                return true;

                                                            return false;
                                                        });

                target.SetStatPctModifier(unitMod, UnitModifierPctType.Total, amount);
            }

            // Calculate the current power change
            int change = target.GetMaxPower(powerType) - oldMaxPower;
            change = oldPower + change - target.GetPower(powerType);
        }

        [AuraEffectHandler(AuraType.ModIncreaseHealthPercent)]
        private void HandleAuraModIncreaseHealthPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            // Unit will keep hp% after MaxHealth being modified if unit is alive.
            float percent = target.GetHealthPct();

            if (apply)
            {
                target.ApplyStatPctModifier(UnitMods.Health, UnitModifierPctType.Total, GetAmount());
            }
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModIncreaseHealthPercent) * target.GetTotalAuraMultiplier(AuraType.ModIncreaseHealthPercent2);
                target.SetStatPctModifier(UnitMods.Health, UnitModifierPctType.Total, amount);
            }

            if (target.GetHealth() > 0)
            {
                uint newHealth = (uint)Math.Max(MathFunctions.CalculatePct(target.GetMaxHealth(), (int)percent), 1);
                target.SetHealth(newHealth);
            }
        }

        [AuraEffectHandler(AuraType.ModBaseHealthPct)]
        private void HandleAuraIncreaseBaseHealthPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.ApplyStatPctModifier(UnitMods.Health, UnitModifierPctType.Base, GetAmount());
            }
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModBaseHealthPct);
                target.SetStatPctModifier(UnitMods.Health, UnitModifierPctType.Base, amount);
            }
        }

        [AuraEffectHandler(AuraType.ModBaseManaPct)]
        private void HandleAuraModIncreaseBaseManaPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.ApplyStatPctModifier(UnitMods.Mana, UnitModifierPctType.Base, GetAmount());
            }
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModBaseManaPct);
                target.SetStatPctModifier(UnitMods.Mana, UnitModifierPctType.Base, amount);
            }
        }

        [AuraEffectHandler(AuraType.ModManaCostPct)]
        private void HandleModManaCostPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            aurApp.GetTarget().ApplyModManaCostMultiplier(GetAmount() / 100.0f, apply);
        }

        [AuraEffectHandler(AuraType.ModPowerDisplay)]
        private void HandleAuraModPowerDisplay(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.RealOrReapplyMask))
                return;

            if (GetMiscValue() >= (int)PowerType.Max)
                return;

            if (apply)
                aurApp.GetTarget().RemoveAurasByType(GetAuraType(), ObjectGuid.Empty, GetBase());

            aurApp.GetTarget().UpdateDisplayPower();
        }

        [AuraEffectHandler(AuraType.ModOverridePowerDisplay)]
        private void HandleAuraModOverridePowerDisplay(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            PowerDisplayRecord powerDisplay = CliDB.PowerDisplayStorage.LookupByKey(GetMiscValue());

            if (powerDisplay == null)
                return;

            Unit target = aurApp.GetTarget();

            if (target.GetPowerIndex((PowerType)powerDisplay.ActualType) == (int)PowerType.Max)
                return;

            if (apply)
            {
                target.RemoveAurasByType(GetAuraType(), ObjectGuid.Empty, GetBase());
                target.SetOverrideDisplayPowerId(powerDisplay.Id);
            }
            else
            {
                target.SetOverrideDisplayPowerId(0);
            }
        }

        [AuraEffectHandler(AuraType.ModMaxPowerPct)]
        private void HandleAuraModMaxPowerPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsPlayer())
                return;

            PowerType powerType = (PowerType)GetMiscValue();
            UnitMods unitMod = UnitMods.PowerStart + (int)powerType;

            // Save old powers for further calculation
            int oldPower = target.GetPower(powerType);
            int oldMaxPower = target.GetMaxPower(powerType);

            // Handle aura effect for max power
            if (apply)
            {
                target.ApplyStatPctModifier(unitMod, UnitModifierPctType.Total, GetAmount());
            }
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModMaxPowerPct,
                                                             aurEff =>
                                                             {
                                                                 if (aurEff.GetMiscValue() == (int)powerType)
                                                                     return true;

                                                                 return false;
                                                             });

                amount *= target.GetTotalAuraMultiplier(AuraType.ModIncreaseEnergyPercent,
                                                        aurEff =>
                                                        {
                                                            if (aurEff.GetMiscValue() == (int)powerType)
                                                                return true;

                                                            return false;
                                                        });

                target.SetStatPctModifier(unitMod, UnitModifierPctType.Total, amount);
            }

            // Calculate the current power change
            int change = target.GetMaxPower(powerType) - oldMaxPower;
            change = oldPower + change - target.GetPower(powerType);
            target.ModifyPower(powerType, change);
        }

        [AuraEffectHandler(AuraType.TriggerSpellOnHealthPct)]
        private void HandleTriggerSpellOnHealthPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasFlag(AuraEffectHandleModes.Real) ||
                !apply)
                return;

            Unit target = aurApp.GetTarget();
            int thresholdPct = GetAmount();
            uint triggerSpell = GetSpellEffectInfo().TriggerSpell;

            switch ((AuraTriggerOnHealthChangeDirection)GetMiscValue())
            {
                case AuraTriggerOnHealthChangeDirection.Above:
                    if (!target.HealthAbovePct(thresholdPct))
                        return;

                    break;
                case AuraTriggerOnHealthChangeDirection.Below:
                    if (!target.HealthBelowPct(thresholdPct))
                        return;

                    break;
                default:
                    break;
            }

            target.CastSpell(target, triggerSpell, new CastSpellExtraArgs(this));
        }

    }
}