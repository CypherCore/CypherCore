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
        /*********************************************************/
        /***                  MODIFY STATS                     ***/
        /*********************************************************/

        /********************************/
        /***        RESISTANCE        ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModResistance)]
        private void HandleAuraModResistance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            for (byte x = (byte)SpellSchools.Normal; x < (byte)SpellSchools.Max; x++)
                if (Convert.ToBoolean(GetMiscValue() & 1 << x))
                    target.HandleStatFlatModifier(UnitMods.ResistanceStart + x, UnitModifierFlatType.Total, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModBaseResistancePct)]
        private void HandleAuraModBaseResistancePCT(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            // only players have base Stats
            if (!target.IsTypeId(TypeId.Player))
            {
                //pets only have base armor
                if (target.IsPet() &&
                    Convert.ToBoolean(GetMiscValue() & (int)SpellSchoolMask.Normal))
                {
                    if (apply)
                    {
                        target.ApplyStatPctModifier(UnitMods.Armor, UnitModifierPctType.Base, GetAmount());
                    }
                    else
                    {
                        float amount = target.GetTotalAuraMultiplierByMiscMask(AuraType.ModBaseResistancePct, (uint)SpellSchoolMask.Normal);
                        target.SetStatPctModifier(UnitMods.Armor, UnitModifierPctType.Base, amount);
                    }
                }
            }
            else
            {
                for (byte x = (byte)SpellSchools.Normal; x < (byte)SpellSchools.Max; x++)
                    if (Convert.ToBoolean(GetMiscValue() & 1 << x))
                    {
                        if (apply)
                        {
                            target.ApplyStatPctModifier(UnitMods.ResistanceStart + x, UnitModifierPctType.Base, GetAmount());
                        }
                        else
                        {
                            float amount = target.GetTotalAuraMultiplierByMiscMask(AuraType.ModBaseResistancePct, 1u << x);
                            target.SetStatPctModifier(UnitMods.ResistanceStart + x, UnitModifierPctType.Base, amount);
                        }
                    }
            }
        }

        [AuraEffectHandler(AuraType.ModResistancePct)]
        private void HandleModResistancePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            for (byte i = (byte)SpellSchools.Normal; i < (byte)SpellSchools.Max; i++)
                if (Convert.ToBoolean(GetMiscValue() & 1 << i))
                {
                    float amount = target.GetTotalAuraMultiplierByMiscMask(AuraType.ModResistancePct, 1u << i);

                    if (target.GetPctModifierValue(UnitMods.ResistanceStart + i, UnitModifierPctType.Total) == amount)
                        continue;

                    target.SetStatPctModifier(UnitMods.ResistanceStart + i, UnitModifierPctType.Total, amount);
                }
        }

        [AuraEffectHandler(AuraType.ModBaseResistance)]
        private void HandleModBaseResistance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            // only players have base Stats
            if (!target.IsTypeId(TypeId.Player))
            {
                //only pets have base Stats
                if (target.IsPet() &&
                    Convert.ToBoolean(GetMiscValue() & (int)SpellSchoolMask.Normal))
                    target.HandleStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Total, GetAmount(), apply);
            }
            else
            {
                for (byte i = (byte)SpellSchools.Normal; i < (byte)SpellSchools.Max; i++)
                    if (Convert.ToBoolean(GetMiscValue() & 1 << i))
                        target.HandleStatFlatModifier(UnitMods.ResistanceStart + i, UnitModifierFlatType.Total, GetAmount(), apply);
            }
        }

        [AuraEffectHandler(AuraType.ModTargetResistance)]
        private void HandleModTargetResistance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            // applied to Damage as HandleNoImmediateEffect in Unit.CalcAbsorbResist and Unit.CalcArmorReducedDamage

            // show armor penetration
            if (target.IsTypeId(TypeId.Player) &&
                Convert.ToBoolean(GetMiscValue() & (int)SpellSchoolMask.Normal))
                target.ApplyModTargetPhysicalResistance(GetAmount(), apply);

            // show as spell penetration only full spell penetration bonuses (all resistances except armor and holy
            if (target.IsTypeId(TypeId.Player) &&
                ((SpellSchoolMask)GetMiscValue() & SpellSchoolMask.Spell) == SpellSchoolMask.Spell)
                target.ApplyModTargetResistance(GetAmount(), apply);
        }

        /********************************/
        /***           STAT           ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModStat)]
        private void HandleAuraModStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            if (GetMiscValue() < -2 ||
                GetMiscValue() > 4)
            {
                Log.outError(LogFilter.Spells, "WARNING: Spell {0} effect {1} has an unsupported misc value ({2}) for SPELL_AURA_MOD_STAT ", GetId(), GetEffIndex(), GetMiscValue());

                return;
            }

            Unit target = aurApp.GetTarget();
            int spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.ModStat, true, GetMiscValue());

            if (Math.Abs(spellGroupVal) >= Math.Abs(GetAmount()))
                return;

            for (var i = Stats.Strength; i < Stats.Max; i++)
                // -1 or -2 is all Stats (misc < -2 checked in function beginning)
                if (GetMiscValue() < 0 ||
                    GetMiscValue() == (int)i)
                {
                    if (spellGroupVal != 0)
                    {
                        target.HandleStatFlatModifier(UnitMods.StatStart + (int)i, UnitModifierFlatType.Total, spellGroupVal, !apply);

                        if (target.IsTypeId(TypeId.Player) ||
                            target.IsPet())
                            target.UpdateStatBuffMod(i);
                    }

                    target.HandleStatFlatModifier(UnitMods.StatStart + (int)i, UnitModifierFlatType.Total, GetAmount(), apply);

                    if (target.IsTypeId(TypeId.Player) ||
                        target.IsPet())
                        target.UpdateStatBuffMod(i);
                }
        }

        [AuraEffectHandler(AuraType.ModPercentStat)]
        private void HandleModPercentStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (GetMiscValue() < -1 ||
                GetMiscValue() > 4)
            {
                Log.outError(LogFilter.Spells, "WARNING: Misc Value for SPELL_AURA_MOD_PERCENT_STAT not valid");

                return;
            }

            // only players have base Stats
            if (!target.IsTypeId(TypeId.Player))
                return;

            for (int i = (int)Stats.Strength; i < (int)Stats.Max; ++i)
                if (GetMiscValue() == i ||
                    GetMiscValue() == -1)
                {
                    if (apply)
                    {
                        target.ApplyStatPctModifier(UnitMods.StatStart + i, UnitModifierPctType.Base, GetAmount());
                    }
                    else
                    {
                        float amount = target.GetTotalAuraMultiplier(AuraType.ModPercentStat,
                                                                     aurEff =>
                                                                     {
                                                                         if (aurEff.GetMiscValue() == i ||
                                                                             aurEff.GetMiscValue() == -1)
                                                                             return true;

                                                                         return false;
                                                                     });

                        target.SetStatPctModifier(UnitMods.StatStart + i, UnitModifierPctType.Base, amount);
                    }
                }
        }

        [AuraEffectHandler(AuraType.ModSpellDamageOfStatPercent)]
        private void HandleModSpellDamagePercentFromStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // Magic Damage modifiers implemented in Unit.SpellDamageBonus
            // This information for client side use only
            // Recalculate bonus
            target.ToPlayer().UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.ModSpellHealingOfStatPercent)]
        private void HandleModSpellHealingPercentFromStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // Recalculate bonus
            target.ToPlayer().UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.ModHealingDone)]
        private void HandleModHealingDone(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // implemented in Unit.SpellHealingBonus
            // this information is for client side only
            target.ToPlayer().UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.ModHealingDonePercent)]
        private void HandleModHealingDonePct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player player = aurApp.GetTarget().ToPlayer();

            if (player)
                player.UpdateHealingDonePercentMod();
        }

        [AuraEffectHandler(AuraType.ModTotalStatPercentage)]
        private void HandleModTotalPercentStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            // save current health State
            float healthPct = target.GetHealthPct();
            bool zeroHealth = !target.IsAlive();

            // players in corpse State may mean two different states:
            /// 1. player just died but did not release (in this case health == 0)
            /// 2. player is corpse running (ie ghost) (in this case health == 1)
            if (target.GetDeathState() == DeathState.Corpse)
                zeroHealth = target.GetHealth() == 0;

            for (int i = (int)Stats.Strength; i < (int)Stats.Max; i++)
                if (Convert.ToBoolean(GetMiscValueB() & 1 << i) ||
                    GetMiscValueB() == 0) // 0 is also used for all Stats
                {
                    float amount = target.GetTotalAuraMultiplier(AuraType.ModTotalStatPercentage,
                                                                 aurEff =>
                                                                 {
                                                                     if ((aurEff.GetMiscValueB() & 1 << i) != 0 ||
                                                                         aurEff.GetMiscValueB() == 0)
                                                                         return true;

                                                                     return false;
                                                                 });

                    if (target.GetPctModifierValue(UnitMods.StatStart + i, UnitModifierPctType.Total) == amount)
                        continue;

                    target.SetStatPctModifier(UnitMods.StatStart + i, UnitModifierPctType.Total, amount);

                    if (target.IsTypeId(TypeId.Player) ||
                        target.IsPet())
                        target.UpdateStatBuffMod((Stats)i);
                }

            // recalculate current HP/MP after applying aura modifications (only for spells with SPELL_ATTR0_ABILITY 0x00000010 flag)
            // this check is total bullshit i think
            if ((Convert.ToBoolean(GetMiscValueB() & 1 << (int)Stats.Stamina) || GetMiscValueB() == 0) &&
                _spellInfo.HasAttribute(SpellAttr0.IsAbility))
                target.SetHealth(Math.Max(MathFunctions.CalculatePct(target.GetMaxHealth(), healthPct), zeroHealth ? 0 : 1ul));
        }

        [AuraEffectHandler(AuraType.ModExpertise)]
        private void HandleAuraModExpertise(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().UpdateExpertise(WeaponAttackType.BaseAttack);
            target.ToPlayer().UpdateExpertise(WeaponAttackType.OffAttack);
        }

        // Increase armor by <AuraEffect.BasePoints> % of your <primary stat>
        [AuraEffectHandler(AuraType.ModArmorPctFromStat)]
        private void HandleModArmorPctFromStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            // only players have primary Stats
            Player player = aurApp.GetTarget().ToPlayer();

            if (!player)
                return;

            player.UpdateArmor();
        }

        [AuraEffectHandler(AuraType.ModBonusArmor)]
        private void HandleModBonusArmor(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            aurApp.GetTarget().HandleStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Base, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModBonusArmorPct)]
        private void HandleModBonusArmorPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            aurApp.GetTarget().UpdateArmor();
        }

        [AuraEffectHandler(AuraType.ModStatBonusPct)]
        private void HandleModStatBonusPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (GetMiscValue() < -1 ||
                GetMiscValue() > 4)
            {
                Log.outError(LogFilter.Spells, "WARNING: Misc Value for SPELL_AURA_MOD_STAT_BONUS_PCT not valid");

                return;
            }

            // only players have base Stats
            if (!target.IsTypeId(TypeId.Player))
                return;

            for (Stats stat = Stats.Strength; stat < Stats.Max; ++stat)
                if (GetMiscValue() == (int)stat ||
                    GetMiscValue() == -1)
                {
                    target.HandleStatFlatModifier(UnitMods.StatStart + (int)stat, UnitModifierFlatType.BasePCTExcludeCreate, GetAmount(), apply);
                    target.UpdateStatBuffMod(stat);
                }
        }

        [AuraEffectHandler(AuraType.OverrideSpellPowerByApPct)]
        private void HandleOverrideSpellPowerByAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (!target)
                return;

            target.ApplyModOverrideSpellPowerByAPPercent(GetAmount(), apply);
            target.UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.OverrideAttackPowerBySpPct)]
        private void HandleOverrideAttackPowerBySpellPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (!target)
                return;

            target.ApplyModOverrideAPBySpellPowerPercent(GetAmount(), apply);
            target.UpdateAttackPowerAndDamage();
            target.UpdateAttackPowerAndDamage(true);
        }

        [AuraEffectHandler(AuraType.ModVersatility)]
        private void HandleModVersatilityByPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target)
            {
                target.SetVersatilityBonus(target.GetTotalAuraModifier(AuraType.ModVersatility));
                target.UpdateHealingDonePercentMod();
                target.UpdateVersatilityDamageDone();
            }
        }

        [AuraEffectHandler(AuraType.ModMaxPower)]
        private void HandleAuraModMaxPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            PowerType power = (PowerType)GetMiscValue();
            UnitMods unitMod = UnitMods.PowerStart + (int)power;

            target.HandleStatFlatModifier(unitMod, UnitModifierFlatType.Total, GetAmount(), apply);
        }

    }
}