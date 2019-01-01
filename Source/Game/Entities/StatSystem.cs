/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.DataStorage;
using Game.Network.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public partial class Unit
    {
        public bool HandleStatModifier(UnitMods unitMod, UnitModifierType modifierType, int amount, bool apply)
        {
            return HandleStatModifier(unitMod, modifierType, (float)amount, apply);
        }
        public bool HandleStatModifier(UnitMods unitMod, UnitModifierType modifierType, float amount, bool apply)
        {
            if (unitMod >= UnitMods.End || modifierType >= UnitModifierType.End)
            {
                Log.outError(LogFilter.Unit, "ERROR in HandleStatModifier(): non-existing UnitMods or wrong UnitModifierType!");
                return false;
            }

            switch (modifierType)
            {
                case UnitModifierType.BaseValue:
                case UnitModifierType.BasePCTExcludeCreate:
                case UnitModifierType.TotalValue:
                    m_auraModifiersGroup[(int)unitMod][(int)modifierType] += apply ? amount : -amount;
                    break;
                case UnitModifierType.BasePCT:
                case UnitModifierType.TotalPCT:
                    MathFunctions.ApplyPercentModFloatVar(ref m_auraModifiersGroup[(int)unitMod][(int)modifierType], amount, apply);
                    break;
                default:
                    break;
            }

            if (!CanModifyStats())
                return false;

            switch (unitMod)
            {
                case UnitMods.StatStrength:
                case UnitMods.StatAgility:
                case UnitMods.StatStamina:
                case UnitMods.StatIntellect:
                    UpdateStats(GetStatByAuraGroup(unitMod));
                    break;
                case UnitMods.Armor:
                    UpdateArmor();
                    break;
                case UnitMods.Health:
                    UpdateMaxHealth();
                    break;
                case UnitMods.Mana:
                case UnitMods.Rage:
                case UnitMods.Focus:
                case UnitMods.Energy:
                case UnitMods.ComboPoints:
                case UnitMods.Runes:
                case UnitMods.RunicPower:
                case UnitMods.SoulShards:
                case UnitMods.LunarPower:
                case UnitMods.HolyPower:
                case UnitMods.Alternate:
                case UnitMods.Maelstrom:
                case UnitMods.Chi:
                case UnitMods.Insanity:
                case UnitMods.BurningEmbers:
                case UnitMods.DemonicFury:
                case UnitMods.ArcaneCharges:
                case UnitMods.Fury:
                case UnitMods.Pain:
                    UpdateMaxPower((PowerType)(unitMod - UnitMods.PowerStart));
                    break;
                case UnitMods.ResistanceHoly:
                case UnitMods.ResistanceFire:
                case UnitMods.ResistanceNature:
                case UnitMods.ResistanceFrost:
                case UnitMods.ResistanceShadow:
                case UnitMods.ResistanceArcane:
                    UpdateResistances(GetSpellSchoolByAuraGroup(unitMod));
                    break;
                case UnitMods.AttackPower:
                    UpdateAttackPowerAndDamage();
                    break;
                case UnitMods.AttackPowerRanged:
                    UpdateAttackPowerAndDamage(true);
                    break;
                case UnitMods.DamageMainHand:
                    UpdateDamagePhysical(WeaponAttackType.BaseAttack);
                    break;
                case UnitMods.DamageOffHand:
                    UpdateDamagePhysical(WeaponAttackType.OffAttack);
                    break;
                case UnitMods.DamageRanged:
                    UpdateDamagePhysical(WeaponAttackType.RangedAttack);
                    break;
                default:
                    break;
            }

            return true;
        }

        int GetMinPower(PowerType power) { return power == PowerType.LunarPower ? -100 : 0; }

        // returns negative amount on power reduction
        public int ModifyPower(PowerType power, int dVal)
        {
            int gain = 0;

            if (dVal == 0)
                return 0;

            int curPower = GetPower(power);

            int val = (dVal + curPower);
            if (val <= GetMinPower(power))
            {
                SetPower(power, GetMinPower(power));
                return -curPower;
            }

            int maxPower = GetMaxPower(power);
            if (val < maxPower)
            {
                SetPower(power, val);
                gain = val - curPower;
            }
            else if (curPower != maxPower)
            {
                SetPower(power, maxPower);
                gain = maxPower - curPower;
            }

            return gain;
        }

        Stats GetStatByAuraGroup(UnitMods unitMod)
        {
            Stats stat = Stats.Strength;

            switch (unitMod)
            {
                case UnitMods.StatStrength:
                    stat = Stats.Strength;
                    break;
                case UnitMods.StatAgility:
                    stat = Stats.Agility;
                    break;
                case UnitMods.StatStamina:
                    stat = Stats.Stamina;
                    break;
                case UnitMods.StatIntellect:
                    stat = Stats.Intellect;
                    break;
                default:
                    break;
            }

            return stat;
        }

        public void ApplyStatBuffMod(Stats stat, float val, bool apply)
        {
            ApplyModSignedFloatValue((val > 0 ? UnitFields.PosStat + (int)stat : UnitFields.NegStat + (int)stat), val, apply);
        }
        public void ApplyStatPercentBuffMod(Stats stat, float val, bool apply)
        {
            ApplyPercentModFloatValue(UnitFields.PosStat + (int)stat, val, apply);
            ApplyPercentModFloatValue(UnitFields.NegStat + (int)stat, val, apply);
        }

        public virtual bool UpdateStats(Stats stat) { return false; }
        public virtual bool UpdateAllStats() { return false; }
        public virtual void UpdateResistances(SpellSchools school)
        {
            if (school > SpellSchools.Normal)
            {
                UnitMods unitMod = UnitMods.ResistanceStart + (int)school;
                SetResistance(school, (int)m_auraModifiersGroup[(int)unitMod][(int)UnitModifierType.BaseValue]);
                SetBonusResistanceMod(school, (int)(GetTotalAuraModValue(unitMod) - GetResistance(school)));
            }
            else
                UpdateArmor();
        }
        public virtual void UpdateArmor() { }
        public virtual void UpdateMaxHealth() { }
        public virtual void UpdateMaxPower(PowerType power) { }
        public virtual void UpdateAttackPowerAndDamage(bool ranged = false) { }
        public virtual void UpdateDamagePhysical(WeaponAttackType attType)
        {
            float minDamage = 0.0f;
            float maxDamage = 0.0f;

            CalculateMinMaxDamage(attType, false, true, out minDamage, out maxDamage);

            switch (attType)
            {
                case WeaponAttackType.BaseAttack:
                default:
                    SetStatFloatValue(UnitFields.MinDamage, minDamage);
                    SetStatFloatValue(UnitFields.MaxDamage, maxDamage);
                    break;
                case WeaponAttackType.OffAttack:
                    SetStatFloatValue(UnitFields.MinOffHandDamage, minDamage);
                    SetStatFloatValue(UnitFields.MaxOffHandDamage, maxDamage);
                    break;
                case WeaponAttackType.RangedAttack:
                    SetStatFloatValue(UnitFields.MinRangedDamage, minDamage);
                    SetStatFloatValue(UnitFields.MaxRangedDamage, maxDamage);
                    break;
            }
        }
        public virtual void CalculateMinMaxDamage(WeaponAttackType attType, bool normalized, bool addTotalPct, out float minDamage, out float maxDamage)
        {
            minDamage = 0f;
            maxDamage = 0f;
        }

        public void UpdateAllResistances()
        {
            for (var i = SpellSchools.Normal; i < SpellSchools.Max; ++i)
                UpdateResistances(i);
        }

        //Stats
        public float GetStat(Stats stat)
        {
            return GetUInt32Value(UnitFields.Stat + (int)stat);
        }
        public void SetCreateStat(Stats stat, float val)
        {
            CreateStats[(int)stat] = val;
        }
        public void SetStat(Stats stat, int val)
        {
            SetStatInt32Value(UnitFields.Stat + (int)stat, val);
        }
        public void SetCreateMana(uint val)
        {
            SetUInt32Value(UnitFields.BaseMana, val);
        }
        public uint GetCreateMana()
        {
            return GetUInt32Value(UnitFields.BaseMana);
        }
        public uint GetArmor()
        {
            return (uint)(GetResistance(SpellSchools.Normal) + GetBonusResistanceMod(SpellSchools.Normal));
        }
        public void SetArmor(int val, int bonusVal)
        {
            SetResistance(SpellSchools.Normal, val);
            SetBonusResistanceMod(SpellSchools.Normal, bonusVal);
        }
        public int GetResistance(SpellSchools school)
        {
            return GetInt32Value(UnitFields.Resistances + (int)school);
        }
        public int GetBonusResistanceMod(SpellSchools school)
        {
            return GetInt32Value(UnitFields.BonusResistanceMods + (int)school);
        }
        public int GetResistance(SpellSchoolMask mask)
        {
            int? resist = null;
            for (int i = (int)SpellSchools.Normal; i < (int)SpellSchools.Max; ++i)
            {
                int schoolResistance = GetResistance((SpellSchools)i) + GetBonusResistanceMod((SpellSchools)i);
                if (Convert.ToBoolean((int)mask & (1 << i)) && (!resist.HasValue || resist.Value > schoolResistance))
                    resist = schoolResistance;
            }

            // resist value will never be negative here
            return resist.HasValue ? resist.Value : 0;
        }
        public void SetResistance(SpellSchools school, int val)
        {
            SetStatInt32Value(UnitFields.Resistances + (int)school, val);
        }
        public void SetBonusResistanceMod(SpellSchools school, int val)
        {
            SetStatInt32Value(UnitFields.BonusResistanceMods + (int)school, val);
        }

        public float GetCreateStat(Stats stat)
        {
            return CreateStats[(int)stat];
        }
        public void InitStatBuffMods()
        {
            for (var i = Stats.Strength; i < Stats.Max; ++i)
            {
                SetFloatValue(UnitFields.PosStat + (int)i, 0);
                SetFloatValue(UnitFields.NegStat + (int)i, 0);
            }
        }

        public bool CanModifyStats()
        {
            return canModifyStats;
        }
        public void SetCanModifyStats(bool modifyStats)
        {
            canModifyStats = modifyStats;
        }
        public float GetTotalStatValue(Stats stat)
        {
            UnitMods unitMod = UnitMods.StatStart + (int)stat;

            if (m_auraModifiersGroup[(int)unitMod][(int)UnitModifierType.TotalPCT] <= 0.0f)
                return 0.0f;

            float value = MathFunctions.CalculatePct(m_auraModifiersGroup[(int)unitMod][(int)UnitModifierType.BaseValue], Math.Max(m_auraModifiersGroup[(int)unitMod][(int)UnitModifierType.BasePCTExcludeCreate], -100.0f));
            value += GetCreateStat(stat);
            value *= m_auraModifiersGroup[(int)unitMod][(int)UnitModifierType.BasePCT];
            value += m_auraModifiersGroup[(int)unitMod][(int)UnitModifierType.TotalValue];
            value *= m_auraModifiersGroup[(int)unitMod][(int)UnitModifierType.TotalPCT];

            return value;
        }

        //Health
        public uint GetCreateHealth() { return GetUInt32Value(UnitFields.BaseHealth); }
        public ulong GetHealth() { return GetUInt64Value(UnitFields.Health); }
        public ulong GetMaxHealth() { return GetUInt64Value(UnitFields.MaxHealth); }
        public float GetHealthPct() { return GetMaxHealth() != 0 ? 100.0f * GetHealth() / GetMaxHealth() : 0.0f; }

        public void SetCreateHealth(uint val)
        {
            SetUInt32Value(UnitFields.BaseHealth, val);
        }
        public void SetHealth(ulong val)
        {
            if (getDeathState() == DeathState.JustDied)
                val = 0;
            else if (IsTypeId(TypeId.Player) && getDeathState() == DeathState.Dead)
                val = 1;
            else
            {
                ulong maxHealth = GetMaxHealth();
                if (maxHealth < val)
                    val = maxHealth;
            }

            SetUInt64Value(UnitFields.Health, val);

            // group update
            Player player = ToPlayer();
            if (player)
            {
                if (player.GetGroup())
                    player.SetGroupUpdateFlag(GroupUpdateFlags.CurHp);
            }
            else if (IsPet())
            {
                Pet pet = ToCreature().ToPet();
                if (pet.isControlled())
                    pet.SetGroupUpdateFlag(GroupUpdatePetFlags.CurHp);
            }
        }
        public void SetMaxHealth(ulong val)
        {
            if (val == 0)
                val = 1;

            ulong health = GetHealth();
            SetUInt64Value(UnitFields.MaxHealth, val);

            // group update
            if (IsTypeId(TypeId.Player))
            {
                if (ToPlayer().GetGroup())
                    ToPlayer().SetGroupUpdateFlag(GroupUpdateFlags.MaxHp);
            }
            else if (IsPet())
            {
                Pet pet = ToCreature().ToPet();
                if (pet.isControlled())
                    pet.SetGroupUpdateFlag(GroupUpdatePetFlags.MaxHp);
            }

            if (val < health)
                SetHealth(val);
        }
        public void SetFullHealth() { SetHealth(GetMaxHealth()); }

        public bool IsFullHealth() { return GetHealth() == GetMaxHealth(); }
        public bool HealthBelowPct(int pct) { return GetHealth() < CountPctFromMaxHealth(pct); }
        public bool HealthBelowPctDamaged(int pct, uint damage) { return GetHealth() - damage < CountPctFromMaxHealth(pct); }
        public bool HealthAbovePct(int pct) { return GetHealth() > CountPctFromMaxHealth(pct); }
        bool HealthAbovePctHealed(int pct, uint heal) { return GetHealth() + heal > CountPctFromMaxHealth(pct); }
        public ulong CountPctFromMaxHealth(int pct) { return MathFunctions.CalculatePct(GetMaxHealth(), pct); }
        ulong CountPctFromCurHealth(int pct) { return MathFunctions.CalculatePct(GetHealth(), pct); }

        public virtual float GetHealthMultiplierForTarget(WorldObject target) { return 1.0f; }
        public virtual float GetDamageMultiplierForTarget(WorldObject target) { return 1.0f; }
        public virtual float GetArmorMultiplierForTarget(WorldObject target) { return 1.0f; }

        //Powers
        public PowerType GetPowerType()
        {
            return (PowerType)GetUInt32Value(UnitFields.DisplayPower);
        }
        public void SetPowerType(PowerType powerType)
        {
            if (GetPowerType() == powerType)
                return;

            SetUInt32Value(UnitFields.DisplayPower, (uint)powerType);

            if (IsTypeId(TypeId.Player))
            {
                if (ToPlayer().GetGroup())
                    ToPlayer().SetGroupUpdateFlag(GroupUpdateFlags.PowerType);
            }
            /*else if (IsPet()) TODO 6.x
            {
                Pet pet = ToCreature().ToPet();
                if (pet.isControlled())
                    pet.SetGroupUpdateFlag(GROUP_UPDATE_FLAG_PET_POWER_TYPE);
            }*/

            // Update max power
            UpdateMaxPower(powerType);

            // Update current power
            switch (powerType)
            {
                case PowerType.Mana: // Keep the same (druid form switching...)
                case PowerType.Energy:
                    break;
                case PowerType.Rage: // Reset to zero
                    SetPower(PowerType.Rage, 0);
                    break;
                case PowerType.Focus: // Make it full
                    SetFullPower(powerType);
                    break;
                default:
                    break;
            }
        }
        public void SetMaxPower(PowerType powerType, int val)
        {
            uint powerIndex = GetPowerIndex(powerType);
            if (powerIndex == (int)PowerType.Max || powerIndex >= (int)PowerType.MaxPerClass)
                return;

            int cur_power = GetPower(powerType);
            SetInt32Value(UnitFields.MaxPower + (int)powerIndex, val);

            // group update
            if (IsTypeId(TypeId.Player))
            {
                if (ToPlayer().GetGroup())
                    ToPlayer().SetGroupUpdateFlag(GroupUpdateFlags.MaxPower);
            }
            /*else if (IsPet()) TODO 6.x
            {
                Pet pet = ToCreature().ToPet();
                if (pet.isControlled())
                    pet.SetGroupUpdateFlag(GROUP_UPDATE_FLAG_PET_MAX_POWER);
            }*/

            if (val < cur_power)
                SetPower(powerType, val);
        }
        public void SetPower(PowerType powerType, int val)
        {
            uint powerIndex = GetPowerIndex(powerType);
            if (powerIndex == (int)PowerType.Max || powerIndex >= (int)PowerType.MaxPerClass)
                return;

            int maxPower = GetMaxPower(powerType);
            if (maxPower < val)
                val = maxPower;

            SetInt32Value(UnitFields.Power + (int)powerIndex, val);

            if (IsInWorld)
            {
                PowerUpdate packet = new PowerUpdate();
                packet.Guid = GetGUID();
                packet.Powers.Add(new PowerUpdatePower(val, (byte)powerType));
                SendMessageToSet(packet, IsTypeId(TypeId.Player));
            }

            // group update
            if (IsTypeId(TypeId.Player))
            {
                Player player = ToPlayer();
                if (player.GetGroup())
                    player.SetGroupUpdateFlag(GroupUpdateFlags.CurPower);
            }
            /*else if (IsPet()) TODO 6.x
            {
                Pet pet = ToCreature().ToPet();
                if (pet.isControlled())
                    pet.SetGroupUpdateFlag(GROUP_UPDATE_FLAG_PET_CUR_POWER);
            }*/
        }
        public void SetFullPower(PowerType powerType) { SetPower(powerType, GetMaxPower(powerType)); }
        public int GetPower(PowerType powerType)
        {
            uint powerIndex = GetPowerIndex(powerType);
            if (powerIndex == (int)PowerType.Max || powerIndex >= (int)PowerType.MaxPerClass)
                return 0;

            return GetInt32Value(UnitFields.Power + (int)powerIndex);
        }
        public int GetMaxPower(PowerType powerType)
        {
            uint powerIndex = GetPowerIndex(powerType);
            if (powerIndex == (int)PowerType.Max || powerIndex >= (int)PowerType.MaxPerClass)
                return 0;

            return GetInt32Value(UnitFields.MaxPower + (int)powerIndex);
        }
        public int GetCreatePowers(PowerType powerType)
        {
            if (powerType == PowerType.Mana)
                return (int)GetCreateMana();

            PowerTypeRecord powerTypeEntry = Global.DB2Mgr.GetPowerTypeEntry(powerType);
            if (powerTypeEntry != null)
                return powerTypeEntry.MaxBasePower;

            return 0;
        }
        public virtual uint GetPowerIndex(PowerType powerType) { return 0; }
        public float GetPowerPct(PowerType powerType) { return GetMaxPower(powerType) != 0 ? 100.0f* GetPower(powerType) / GetMaxPower(powerType) : 0.0f; }

        public void ApplyResilience(Unit victim, ref uint damage)
        {
            // player mounted on multi-passenger mount is also classified as vehicle
            if (IsVehicle() || (victim.IsVehicle() && !victim.IsTypeId(TypeId.Player)))
                return;

            // Don't consider resilience if not in PvP - player or pet
            if (!GetCharmerOrOwnerPlayerOrPlayerItself())
                return;

            Unit target = null;
            if (victim.IsTypeId(TypeId.Player))
                target = victim;
            else if (victim.IsTypeId(TypeId.Unit) && victim.GetOwner() && victim.GetOwner().IsTypeId(TypeId.Player))
                target = victim.GetOwner();

            if (!target)
                return;

            damage -= target.GetDamageReduction(damage);
        }
        // player or player's pet resilience (-1%)
        uint GetDamageReduction(uint damage) { return GetCombatRatingDamageReduction(CombatRating.ResiliencePlayerDamage, 1.0f, 100.0f, damage); }

        float GetCombatRatingReduction(CombatRating cr)
        {
            Player player = ToPlayer();
            if (player)
                return player.GetRatingBonusValue(cr);
            // Player's pet get resilience from owner
            else if (IsPet() && GetOwner())
            {
                Player owner = GetOwner().ToPlayer();
                if (owner)
                    return owner.GetRatingBonusValue(cr);
            }

            return 0.0f;
        }

        uint GetCombatRatingDamageReduction(CombatRating cr, float rate, float cap, uint damage)
        {
            float percent = Math.Min(GetCombatRatingReduction(cr) * rate, cap);
            return MathFunctions.CalculatePct(damage, percent);
        }

        //Chances
        float MeleeSpellMissChance(Unit victim, WeaponAttackType attType, uint spellId)
        {
            //calculate miss chance
            float missChance = victim.GetUnitMissChance(attType);

            if (spellId == 0 && haveOffhandWeapon() && !IsInFeralForm())
                missChance += 19;

            // Calculate hit chance
            float hitChance = 100.0f;

            // Spellmod from SPELLMOD_RESIST_MISS_CHANCE
            if (spellId != 0)
            {
                Player modOwner = GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(spellId, SpellModOp.ResistMissChance, ref hitChance);
            }

            missChance += hitChance - 100.0f;

            if (attType == WeaponAttackType.RangedAttack)
                missChance -= m_modRangedHitChance;
            else
                missChance -= m_modMeleeHitChance;

            // Limit miss chance from 0 to 77%
            if (missChance < 0.0f)
                return 0.0f;
            if (missChance > 77.0f)
                return 77.0f;
            return missChance;
        }

        float GetUnitCriticalChance(WeaponAttackType attackType, Unit victim)
        {
            float chance = 0.0f;
            if (IsTypeId(TypeId.Player))
            {
                switch (attackType)
                {
                    case WeaponAttackType.BaseAttack:
                        chance = GetFloatValue(ActivePlayerFields.CritPercentage);
                        break;
                    case WeaponAttackType.OffAttack:
                        chance = GetFloatValue(ActivePlayerFields.OffhandCritPercentage);
                        break;
                    case WeaponAttackType.RangedAttack:
                        chance = GetFloatValue(ActivePlayerFields.RangedCritPercentage);
                        break;
                }
            }
            else
            {
                if (!ToCreature().GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoCrit))
                {
                    chance = 5.0f;
                    chance += GetTotalAuraModifier(AuraType.ModWeaponCritPercent);
                    chance += GetTotalAuraModifier(AuraType.ModCritPct);
                }
            }

            // flat aura mods
            if (attackType == WeaponAttackType.RangedAttack)
                chance += victim.GetTotalAuraModifier(AuraType.ModAttackerRangedCritChance);
            else
                chance += victim.GetTotalAuraModifier(AuraType.ModAttackerMeleeCritChance);

            chance += victim.GetTotalAuraModifier(AuraType.ModCritChanceForCaster, aurEff =>
            {
                if (aurEff.GetCasterGUID() == GetGUID())
                    return true;

                return false;
            });

            chance += victim.GetTotalAuraModifier(AuraType.ModAttackerSpellAndWeaponCritChance);

            return Math.Max(chance, 0.0f);
        }
        float GetUnitDodgeChance(WeaponAttackType attType, Unit victim)
        {
            int levelDiff = (int)(victim.GetLevelForTarget(this) - GetLevelForTarget(victim));

            float chance = 0.0f;
            float levelBonus = 0.0f;
            if (victim.IsTypeId(TypeId.Player))
                chance = victim.GetFloatValue(ActivePlayerFields.DodgePercentage);
            else
            {
                if (!victim.IsTotem())
                {
                    chance = 3.0f;
                    chance += victim.GetTotalAuraModifier(AuraType.ModDodgePercent);

                    if (levelDiff > 0)
                        levelBonus = 1.5f * levelDiff;
                }
            }

            chance += levelBonus;

            // Reduce enemy dodge chance by SPELL_AURA_MOD_COMBAT_RESULT_CHANCE
            chance += GetTotalAuraModifierByMiscValue(AuraType.ModCombatResultChance, (int)VictimState.Dodge);

            // reduce dodge by SPELL_AURA_MOD_ENEMY_DODGE
            chance += GetTotalAuraModifier(AuraType.ModEnemyDodge);

            // Reduce dodge chance by attacker expertise rating
            if (IsTypeId(TypeId.Player))
                chance -= ToPlayer().GetExpertiseDodgeOrParryReduction(attType);
            else
                chance -= GetTotalAuraModifier(AuraType.ModExpertise) / 4.0f;
            return Math.Max(chance, 0.0f);
        }
        float GetUnitParryChance(WeaponAttackType attType, Unit victim)
        {
            int levelDiff = (int)(victim.GetLevelForTarget(this) - GetLevelForTarget(victim));

            float chance = 0.0f;
            float levelBonus = 0.0f;
            Player playerVictim = victim.ToPlayer();
            if (playerVictim)
            {
                if (playerVictim.CanParry())
                {
                    Item tmpitem = playerVictim.GetWeaponForAttack(WeaponAttackType.BaseAttack, true);
                    if (!tmpitem)
                        tmpitem = playerVictim.GetWeaponForAttack(WeaponAttackType.OffAttack, true);

                    if (tmpitem)
                        chance = playerVictim.GetFloatValue(ActivePlayerFields.ParryPercentage);
                }
            }
            else
            {
                if (!victim.IsTotem() && !victim.ToCreature().GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoParry))
                {
                    chance = 6.0f;
                    chance += victim.GetTotalAuraModifier(AuraType.ModParryPercent);

                    if (levelDiff > 0)
                        levelBonus = 1.5f * levelDiff;
                }
            }

            chance += levelBonus;

            // Reduce parry chance by attacker expertise rating
            if (IsTypeId(TypeId.Player))
                chance -= ToPlayer().GetExpertiseDodgeOrParryReduction(attType);
            else
                chance -= GetTotalAuraModifier(AuraType.ModExpertise) / 4.0f;
            return Math.Max(chance, 0.0f);
        }
        float GetUnitMissChance(WeaponAttackType attType)
        {
            float miss_chance = 5.00f;

            if (attType == WeaponAttackType.RangedAttack)
                miss_chance -= GetTotalAuraModifier(AuraType.ModAttackerRangedHitChance);
            else
                miss_chance -= GetTotalAuraModifier(AuraType.ModAttackerMeleeHitChance);

            return miss_chance;
        }
        float GetUnitBlockChance(WeaponAttackType attType, Unit victim)
        {
            int levelDiff = (int)(victim.GetLevelForTarget(this) - GetLevelForTarget(victim));

            float chance = 0.0f;
            float levelBonus = 0.0f;
            Player playerVictim = victim.ToPlayer();
            if (playerVictim)
            {
                if (playerVictim.CanBlock())
                {
                    Item tmpitem = playerVictim.GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
                    if (tmpitem && !tmpitem.IsBroken() && tmpitem.GetTemplate().GetInventoryType() == InventoryType.Shield)
                        chance = playerVictim.GetFloatValue(ActivePlayerFields.BlockPercentage);
                }
            }
            else
            {
                if (!victim.IsTotem() && !(victim.ToCreature().GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoBlock)))
                {
                    chance = 3.0f;
                    chance += victim.GetTotalAuraModifier(AuraType.ModBlockPercent);

                    if (levelDiff > 0)
                        levelBonus = 1.5f * levelDiff;
                }
            }

            chance += levelBonus;
            return Math.Max(chance, 0.0f);
        }

        int GetMechanicResistChance(SpellInfo spellInfo)
        {
            if (spellInfo == null)
                return 0;

            int resistMech = 0;
            foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(GetMap().GetDifficultyID()))
            {
                if (effect == null || !effect.IsEffect())
                    break;

                int effect_mech = (int)spellInfo.GetEffectMechanic(effect.EffectIndex, GetMap().GetDifficultyID());
                if (effect_mech != 0)
                {
                    int temp = GetTotalAuraModifierByMiscValue(AuraType.ModMechanicResistance, effect_mech);
                    if (resistMech < temp)
                        resistMech = temp;
                }
            }
            return Math.Max(resistMech, 0);
        }
    }

    public partial class Player
    {
        public override bool UpdateAllStats()
        {
            for (var i = Stats.Strength; i < Stats.Max; ++i)
            {
                float value = GetTotalStatValue(i);
                SetStat(i, (int)value);
            }

            UpdateArmor();
            // calls UpdateAttackPowerAndDamage() in UpdateArmor for SPELL_AURA_MOD_ATTACK_POWER_OF_ARMOR
            UpdateAttackPowerAndDamage(true);
            UpdateMaxHealth();

            for (var i = PowerType.Mana; i < PowerType.Max; ++i)
                UpdateMaxPower(i);

            UpdateAllRatings();
            UpdateAllCritPercentages();
            UpdateSpellCritChance();
            UpdateBlockPercentage();
            UpdateParryPercentage();
            UpdateDodgePercentage();
            UpdateSpellDamageAndHealingBonus();
            UpdateManaRegen();
            UpdateExpertise(WeaponAttackType.BaseAttack);
            UpdateExpertise(WeaponAttackType.OffAttack);
            RecalculateRating(CombatRating.ArmorPenetration);
            UpdateAllResistances();

            return true;
        }

        public override bool UpdateStats(Stats stat)
        {
            // value = ((base_value * base_pct) + total_value) * total_pct
            float value = GetTotalStatValue(stat);

            SetStat(stat, (int)value);

            if (stat == Stats.Stamina || stat == Stats.Intellect || stat == Stats.Strength)
            {
                Pet pet = GetPet();
                if (pet != null)
                    pet.UpdateStats(stat);
            }

            switch (stat)
            {
                case Stats.Agility:
                    UpdateArmor();
                    UpdateAllCritPercentages();
                    UpdateDodgePercentage();
                    break;
                case Stats.Stamina:
                    UpdateMaxHealth();
                    break;
                case Stats.Intellect:
                    UpdateSpellCritChance();
                    UpdateArmor();                                  //SPELL_AURA_MOD_RESISTANCE_OF_INTELLECT_PERCENT, only armor currently
                    break;
                default:
                    break;
            }

            if (stat == Stats.Strength)
                UpdateAttackPowerAndDamage(false);
            else if (stat == Stats.Agility)
            {
                UpdateAttackPowerAndDamage(false);
                UpdateAttackPowerAndDamage(true);
            }

            UpdateSpellDamageAndHealingBonus();
            UpdateManaRegen();

            // Update ratings in exist SPELL_AURA_MOD_RATING_FROM_STAT and only depends from stat
            uint mask = 0;
            var modRatingFromStat = GetAuraEffectsByType(AuraType.ModRatingFromStat);
            foreach (var eff in modRatingFromStat)
                if ((Stats)eff.GetMiscValueB() == stat)
                    mask |= (uint)eff.GetMiscValue();
            if (mask != 0)
            {
                for (int rating = 0; rating < (int)CombatRating.Max; ++rating)
                    if (Convert.ToBoolean(mask & (1 << rating)))
                        ApplyRatingMod((CombatRating)rating, 0, true);
            }
            return true;
        }

        public override void UpdateResistances(SpellSchools school)
        {
            if (school > SpellSchools.Normal)
            {
                base.UpdateResistances(school);

                Pet pet = GetPet();
                if (pet != null)
                    pet.UpdateResistances(school);
            }
            else
                UpdateArmor();
        }

        void RecalculateRating(CombatRating cr) { ApplyRatingMod(cr, 0, true); }

        public void ApplyRatingMod(CombatRating combatRating, int value, bool apply)
        {
            baseRatingValue[(int)combatRating] += (apply ? value : -value);

            UpdateRating(combatRating);
        }

        public override void CalculateMinMaxDamage(WeaponAttackType attType, bool normalized, bool addTotalPct, out float min_damage, out float max_damage)
        {
            UnitMods unitMod;

            switch (attType)
            {
                case WeaponAttackType.BaseAttack:
                default:
                    unitMod = UnitMods.DamageMainHand;
                    break;
                case WeaponAttackType.OffAttack:
                    unitMod = UnitMods.DamageOffHand;
                    break;
                case WeaponAttackType.RangedAttack:
                    unitMod = UnitMods.DamageRanged;
                    break;
            }

            float attackPowerMod = Math.Max(GetAPMultiplier(attType, normalized), 0.25f);

            float baseValue = GetModifierValue(unitMod, UnitModifierType.BaseValue) + GetTotalAttackPowerValue(attType) / 3.5f * attackPowerMod;
            float basePct = GetModifierValue(unitMod, UnitModifierType.BasePCT);
            float totalValue = GetModifierValue(unitMod, UnitModifierType.TotalValue);
            float totalPct = addTotalPct ? GetModifierValue(unitMod, UnitModifierType.TotalPCT) : 1.0f;

            float weaponMinDamage = GetWeaponDamageRange(attType, WeaponDamageRange.MinDamage);
            float weaponMaxDamage = GetWeaponDamageRange(attType, WeaponDamageRange.MaxDamage);

            float versaDmgMod = 1.0f;

            MathFunctions.AddPct(ref versaDmgMod, GetRatingBonusValue(CombatRating.VersatilityDamageDone) + (float)GetTotalAuraModifier(AuraType.ModVersatility));

            SpellShapeshiftFormRecord shapeshift = CliDB.SpellShapeshiftFormStorage.LookupByKey(GetShapeshiftForm());
            if (shapeshift != null && shapeshift.CombatRoundTime != 0)
            {
                weaponMinDamage = weaponMinDamage * shapeshift.CombatRoundTime / 1000.0f / attackPowerMod;
                weaponMaxDamage = weaponMaxDamage * shapeshift.CombatRoundTime / 1000.0f / attackPowerMod;
            }
            else if (!CanUseAttackType(attType))      //check if player not in form but still can't use (disarm case)
            {
                //cannot use ranged/off attack, set values to 0
                if (attType != WeaponAttackType.BaseAttack)
                {
                    min_damage = 0;
                    max_damage = 0;
                    return;
                }
                weaponMinDamage = SharedConst.BaseMinDamage;
                weaponMaxDamage = SharedConst.BaseMaxDamage;
            }

            min_damage = ((baseValue + weaponMinDamage) * basePct + totalValue) * totalPct * versaDmgMod;
            max_damage = ((baseValue + weaponMaxDamage) * basePct + totalValue) * totalPct * versaDmgMod;
        }

        void UpdateAllCritPercentages()
        {
            float value = 5.0f;

            SetBaseModValue(BaseModGroup.CritPercentage, BaseModType.PCTmod, value);
            SetBaseModValue(BaseModGroup.OffhandCritPercentage, BaseModType.PCTmod, value);
            SetBaseModValue(BaseModGroup.RangedCritPercentage, BaseModType.PCTmod, value);

            UpdateCritPercentage(WeaponAttackType.BaseAttack);
            UpdateCritPercentage(WeaponAttackType.OffAttack);
            UpdateCritPercentage(WeaponAttackType.RangedAttack);
        }

        public void UpdateManaRegen()
        {
            int manaIndex = (int)GetPowerIndex(PowerType.Mana);
            if (manaIndex == (int)PowerType.Max)
                return;

            // Get base of Mana Pool in sBaseMPGameTable
            uint basemana;
            Global.ObjectMgr.GetPlayerClassLevelInfo(GetClass(), getLevel(), out basemana);
            float base_regen = basemana / 100.0f;

            base_regen += GetTotalAuraModifierByMiscValue(AuraType.ModPowerRegen, (int)PowerType.Mana);

            // Apply PCT bonus from SPELL_AURA_MOD_POWER_REGEN_PERCENT
            base_regen *= GetTotalAuraMultiplierByMiscValue(AuraType.ModPowerRegenPercent, (int)PowerType.Mana);

            // Apply PCT bonus from SPELL_AURA_MOD_MANA_REGEN_PCT
            base_regen *= GetTotalAuraMultiplierByMiscValue(AuraType.ModManaRegenPct, (int)PowerType.Mana);

            SetFloatValue(UnitFields.PowerRegenInterruptedFlatModifier + manaIndex, base_regen);
            SetFloatValue(UnitFields.PowerRegenFlatModifier + manaIndex, base_regen);
        }

        public void UpdateSpellDamageAndHealingBonus()
        {
            // Magic damage modifiers implemented in Unit.SpellDamageBonusDone
            // This information for client side use only
            // Get healing bonus for all schools
            SetStatInt32Value(ActivePlayerFields.ModHealingDonePos, (int)SpellBaseHealingBonusDone(SpellSchoolMask.All));
            // Get damage bonus for all schools
            var modDamageAuras = GetAuraEffectsByType(AuraType.ModDamageDone);
            for (var i = SpellSchools.Holy; i < SpellSchools.Max; ++i)
            {
                SetInt32Value(ActivePlayerFields.ModDamageDoneNeg + (int)i, modDamageAuras.Aggregate(0, (negativeMod, aurEff) =>
                {
                    if (aurEff.GetAmount() < 0 && Convert.ToBoolean(aurEff.GetMiscValue() & (1 << (int)i)))
                        negativeMod += aurEff.GetAmount();
                    return negativeMod;
                }));
                SetStatInt32Value(ActivePlayerFields.ModDamageDonePos + (int)i, SpellBaseDamageBonusDone((SpellSchoolMask)(1 << (int)i)) - GetInt32Value(ActivePlayerFields.ModDamageDoneNeg + (int)i));
            }

            if (HasAuraType(AuraType.OverrideAttackPowerBySpPct))
            { 
                UpdateAttackPowerAndDamage();
                UpdateAttackPowerAndDamage(true);
            }
        }
        public uint GetBaseSpellPowerBonus() { return m_baseSpellPower; }

        public override void UpdateAttackPowerAndDamage(bool ranged = false)
        {
            float val2 = 0.0f;
            float level = getLevel();

            var entry = CliDB.ChrClassesStorage.LookupByKey(GetClass());
            UnitMods unitMod = ranged ? UnitMods.AttackPowerRanged : UnitMods.AttackPower;

            UnitFields index = UnitFields.AttackPower;
            UnitFields index_mod = UnitFields.AttackPowerModPos;
            UnitFields index_mult = UnitFields.AttackPowerMultiplier;

            if (ranged)
            {
                index = UnitFields.RangedAttackPower;
                index_mod = UnitFields.RangedAttackPowerModPos;
                index_mult = UnitFields.RangedAttackPowerMultiplier;
            }

            if (!HasAuraType(AuraType.OverrideAttackPowerBySpPct))
            {
                if (!ranged)
                {
                    float strengthValue = Math.Max((GetStat(Stats.Strength)) * entry.AttackPowerPerStrength, 0.0f);
                    float agilityValue = Math.Max((GetStat(Stats.Agility)) * entry.AttackPowerPerAgility, 0.0f);

                    var form = CliDB.SpellShapeshiftFormStorage.LookupByKey((uint)GetShapeshiftForm());
                    // Directly taken from client, SHAPESHIFT_FLAG_AP_FROM_STRENGTH ?
                    if (form != null && Convert.ToBoolean((uint)form.Flags & 0x20))
                        agilityValue += Math.Max(GetStat(Stats.Agility) * entry.AttackPowerPerStrength, 0.0f);

                    val2 = strengthValue + agilityValue;
                }
                else
                    val2 = (level + Math.Max(GetStat(Stats.Agility), 0.0f)) * entry.RangedAttackPowerPerAgility;
            }
            else
            {
                int minSpellPower = GetInt32Value(ActivePlayerFields.ModHealingDonePos);
                for (var i = SpellSchools.Holy; i < SpellSchools.Max; ++i)
                    minSpellPower = Math.Min(minSpellPower, GetInt32Value(ActivePlayerFields.ModDamageDonePos + (int)i));

                val2 = MathFunctions.CalculatePct(minSpellPower, GetFloatValue(ActivePlayerFields.OverrideApBySpellPowerPercent));
            }

            SetModifierValue(unitMod, UnitModifierType.BaseValue, val2);

            float base_attPower = GetModifierValue(unitMod, UnitModifierType.BaseValue) * GetModifierValue(unitMod, UnitModifierType.BasePCT);
            float attPowerMod = GetModifierValue(unitMod, UnitModifierType.TotalValue);
            float attPowerMultiplier = GetModifierValue(unitMod, UnitModifierType.TotalPCT) - 1.0f;

            //add dynamic flat mods
            if (!ranged)
            {
                var mAPbyArmor = GetAuraEffectsByType(AuraType.ModAttackPowerOfArmor);
                foreach (var iter in mAPbyArmor)
                    // always: ((*i).GetModifier().m_miscvalue == 1 == SPELL_SCHOOL_MASK_NORMAL)
                    attPowerMod += (int)(GetArmor() / iter.GetAmount());
            }

            SetUInt32Value(index, (uint)base_attPower);            //UNIT_FIELD_(RANGED)_ATTACK_POWER field
            SetUInt32Value(index_mod, (uint)attPowerMod);          //UNIT_FIELD_(RANGED)_ATTACK_POWER_MOD_POS field
            SetFloatValue(index_mult, attPowerMultiplier);          //UNIT_FIELD_(RANGED)_ATTACK_POWER_MULTIPLIER field

            Pet pet = GetPet();                                //update pet's AP
            Guardian guardian = GetGuardianPet();
            //automatically update weapon damage after attack power modification
            if (ranged)
            {
                UpdateDamagePhysical(WeaponAttackType.RangedAttack);
                if (pet != null && pet.IsHunterPet()) // At ranged attack change for hunter pet
                    pet.UpdateAttackPowerAndDamage();
            }
            else
            {
                UpdateDamagePhysical(WeaponAttackType.BaseAttack);
                Item offhand = GetWeaponForAttack(WeaponAttackType.OffAttack, true);
                if (offhand)
                    if (CanDualWield() || offhand.GetTemplate().GetFlags3().HasAnyFlag(ItemFlags3.AlwaysAllowDualWield))
                        UpdateDamagePhysical(WeaponAttackType.OffAttack);

                if (HasAuraType(AuraType.ModSpellDamageOfAttackPower) ||
                    HasAuraType(AuraType.ModSpellHealingOfAttackPower) ||
                    HasAuraType(AuraType.OverrideSpellPowerByApPct))
                    UpdateSpellDamageAndHealingBonus();

                if (pet != null && pet.IsPetGhoul()) // At melee attack power change for DK pet
                 pet.UpdateAttackPowerAndDamage();

                if (guardian != null && guardian.IsSpiritWolf()) // At melee attack power change for Shaman feral spirit
                    guardian.UpdateAttackPowerAndDamage();
            }
        }

        public override void UpdateArmor()
        {
            UnitMods unitMod = UnitMods.Armor;

            float value = GetModifierValue(unitMod, UnitModifierType.BaseValue);    // base armor (from items)
            float baseValue = value;
            value *= GetModifierValue(unitMod, UnitModifierType.BasePCT);           // armor percent from items
            value += GetModifierValue(unitMod, UnitModifierType.TotalValue);

            //add dynamic flat mods
            var mResbyIntellect = GetAuraEffectsByType(AuraType.ModResistanceOfStatPercent);
            foreach (var i in mResbyIntellect)
            {
                if (Convert.ToBoolean(i.GetMiscValue() & (int)SpellSchoolMask.Normal))
                    value += MathFunctions.CalculatePct(GetStat((Stats)i.GetMiscValueB()), i.GetAmount());
            }

            value *= GetModifierValue(unitMod, UnitModifierType.TotalPCT);

            SetArmor((int)baseValue, (int)(value - baseValue));

            Pet pet = GetPet();
            if (pet)
                pet.UpdateArmor();

            UpdateAttackPowerAndDamage();                           // armor dependent auras update for SPELL_AURA_MOD_ATTACK_POWER_OF_ARMOR
        }

        void _ApplyAllStatBonuses()
        {
            SetCanModifyStats(false);

            _ApplyAllAuraStatMods();
            _ApplyAllItemMods();

            SetCanModifyStats(true);

            UpdateAllStats();
        }
        void _RemoveAllStatBonuses()
        {
            SetCanModifyStats(false);

            _RemoveAllItemMods();
            _RemoveAllAuraStatMods();

            SetCanModifyStats(true);

            UpdateAllStats();
        }

        void UpdateAllRatings()
        {
            for (CombatRating cr = 0; cr < CombatRating.Max; ++cr)
                UpdateRating(cr);
        }
        public void UpdateRating(CombatRating cr)
        {
            int amount = baseRatingValue[(int)cr];
            // Apply bonus from SPELL_AURA_MOD_RATING_FROM_STAT
            // stat used stored in miscValueB for this aura
            var modRatingFromStat = GetAuraEffectsByType(AuraType.ModRatingFromStat);
            foreach (var aurEff in modRatingFromStat)
                if (Convert.ToBoolean(aurEff.GetMiscValue() & (1 << (int)cr)))
                    amount += (int)MathFunctions.CalculatePct(GetStat((Stats)aurEff.GetMiscValueB()), aurEff.GetAmount());

            var modRatingPct = GetAuraEffectsByType(AuraType.ModRatingPct);
            foreach (var aurEff in modRatingPct)
                if (Convert.ToBoolean(aurEff.GetMiscValue() & (1 << (int)cr)))
                    amount += MathFunctions.CalculatePct(amount, aurEff.GetAmount());

            if (amount < 0)
                amount = 0;

            uint oldRating = GetUInt32Value(ActivePlayerFields.CombatRating + (int)cr);
            SetUInt32Value(ActivePlayerFields.CombatRating + (int)cr, (uint)amount);

            bool affectStats = CanModifyStats();

            switch (cr)
            {
                case CombatRating.Amplify:
                case CombatRating.DefenseSkill:
                    break;
                case CombatRating.Dodge:
                    UpdateDodgePercentage();
                    break;
                case CombatRating.Parry:
                    UpdateParryPercentage();
                    break;
                case CombatRating.Block:
                    UpdateBlockPercentage();
                    break;
                case CombatRating.HitMelee:
                    UpdateMeleeHitChances();
                    break;
                case CombatRating.HitRanged:
                    UpdateRangedHitChances();
                    break;
                case CombatRating.HitSpell:
                    UpdateSpellHitChances();
                    break;
                case CombatRating.CritMelee:
                    if (affectStats)
                    {
                        UpdateCritPercentage(WeaponAttackType.BaseAttack);
                        UpdateCritPercentage(WeaponAttackType.OffAttack);
                    }
                    break;
                case CombatRating.CritRanged:
                    if (affectStats)
                        UpdateCritPercentage(WeaponAttackType.RangedAttack);
                    break;
                case CombatRating.CritSpell:
                    if (affectStats)
                        UpdateSpellCritChance();
                    break;
                case CombatRating.HasteMelee:
                case CombatRating.HasteRanged:
                case CombatRating.HasteSpell:
                    {
                        // explicit affected values
                        float multiplier = GetRatingMultiplier(cr);
                        float oldVal = oldRating * multiplier;
                        float newVal = amount * multiplier;
                        switch (cr)
                        {
                            case CombatRating.HasteMelee:
                                ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, oldVal, false);
                                ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, oldVal, false);
                                ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, newVal, true);
                                ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, newVal, true);
                                if (GetClass() == Class.Deathknight)
                                    UpdateAllRunesRegen();
                                break;
                            case CombatRating.HasteRanged:
                                ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, oldVal, false);
                                ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, newVal, true);
                                break;
                            case CombatRating.HasteSpell:
                                ApplyCastTimePercentMod(oldVal, false);
                                ApplyCastTimePercentMod(newVal, true);
                                break;
                            default:
                                break;
                        }
                        break;
                    }
                case CombatRating.Expertise:
                    if (affectStats)
                    {
                        UpdateExpertise(WeaponAttackType.BaseAttack);
                        UpdateExpertise(WeaponAttackType.OffAttack);
                    }
                    break;
                case CombatRating.ArmorPenetration:
                    if (affectStats)
                        UpdateArmorPenetration(amount);
                    break;
                case CombatRating.Mastery:
                    UpdateMastery();
                    break;
                case CombatRating.VersatilityDamageDone:
                    UpdateVersatilityDamageDone();
                    break;
                case CombatRating.VersatilityHealingDone:
                    UpdateHealingDonePercentMod();
                    break;
            }
        }
        public void UpdateMastery()
        {
            if (!CanUseMastery())
            {
                SetFloatValue(ActivePlayerFields.Mastery, 0.0f);
                return;
            }

            float value = GetTotalAuraModifier(AuraType.Mastery);
            value += GetRatingBonusValue(CombatRating.Mastery);
            SetFloatValue(ActivePlayerFields.Mastery, value);

            ChrSpecializationRecord chrSpec = CliDB.ChrSpecializationStorage.LookupByKey(GetUInt32Value(PlayerFields.CurrentSpecId));
            if (chrSpec == null)
                return;

            for (uint i = 0; i < PlayerConst.MaxMasterySpells; ++i)
            {                
                Aura aura = GetAura(chrSpec.MasterySpellID[i]);
                if (aura != null)
                {
                    foreach (SpellEffectInfo effect in aura.GetSpellEffectInfos())
                    {
                        if (effect == null)
                            continue;

                        float mult = effect.BonusCoefficient;
                        if (MathFunctions.fuzzyEq(mult, 0.0f))
                            continue;

                        aura.GetEffect(effect.EffectIndex).ChangeAmount((int)(value * mult));
                    }
                }
            }
        }

        public void UpdateVersatilityDamageDone()
        {
            // No proof that CR_VERSATILITY_DAMAGE_DONE is allways = PLAYER_VERSATILITY
            SetUInt32Value(ActivePlayerFields.Versatility, GetUInt32Value(ActivePlayerFields.CombatRating + (int)CombatRating.VersatilityDamageDone));

            if (GetClass() == Class.Hunter)
                UpdateDamagePhysical(WeaponAttackType.RangedAttack);
            else
                UpdateDamagePhysical(WeaponAttackType.BaseAttack);
        }

        public void UpdateHealingDonePercentMod()
        {
            float value = 1.0f;

            MathFunctions.AddPct(ref value, GetRatingBonusValue(CombatRating.VersatilityHealingDone) + GetTotalAuraModifier(AuraType.ModVersatility));

            foreach (AuraEffect auraEffect in GetAuraEffectsByType(AuraType.ModHealingDonePercent))
                MathFunctions.AddPct(ref value, auraEffect.GetAmount());

            SetStatFloatValue(ActivePlayerFields.ModHealingDonePct, value);
        }

        void UpdateArmorPenetration(int amount)
        {
            // Store Rating Value
            SetInt32Value(ActivePlayerFields.CombatRating + (int)CombatRating.ArmorPenetration, amount);
        }
        public void UpdateParryPercentage()
        {
            float[] parry_cap =
            {
                65.631440f,     // Warrior
                65.631440f,     // Paladin
                145.560408f,    // Hunter
                145.560408f,    // Rogue
                0.0f,           // Priest
                65.631440f,     // DK
                145.560408f,    // Shaman
                0.0f,           // Mage
                0.0f,           // Warlock
                90.6425f,       // Monk
                0.0f,           // Druid
                65.631440f      // Demon Hunter
            };

            // No parry
            float value = 0.0f;
            int pclass = (int)GetClass() - 1;
            if (CanParry() && parry_cap[pclass] > 0.0f)
            {
                float nondiminishing = 5.0f;
                // Parry from rating
                float diminishing = GetRatingBonusValue(CombatRating.Parry);
                // Parry from SPELL_AURA_MOD_PARRY_PERCENT aura
                nondiminishing += GetTotalAuraModifier(AuraType.ModParryPercent);
                // apply diminishing formula to diminishing parry chance
                value = nondiminishing + diminishing * parry_cap[pclass] / (diminishing + parry_cap[pclass] * m_diminishing_k[pclass]);

                if (WorldConfig.GetBoolValue(WorldCfg.StatsLimitsEnable))
                    value = value > WorldConfig.GetFloatValue(WorldCfg.StatsLimitsParry) ? WorldConfig.GetFloatValue(WorldCfg.StatsLimitsParry) : value;

                value = value < 0.0f ? 0.0f : value;
            }
            SetFloatValue(ActivePlayerFields.ParryPercentage, value);
        }

        public void UpdateDodgePercentage()
        {
            float[] dodge_cap =
            {        
                65.631440f,     // Warrior                
                65.631440f,     // Paladin
                145.560408f,    // Hunter
                145.560408f,    // Rogue
                150.375940f,    // Priest
                65.631440f,     // DK
                145.560408f,    // Shaman
                150.375940f,    // Mage
                150.375940f,    // Warlock
                145.560408f,    // Monk
                116.890707f,    // Druid
                145.560408f     // Demon Hunter
            };

            float diminishing = 0.0f, nondiminishing = 0.0f;
            GetDodgeFromAgility(diminishing, nondiminishing);
            // Dodge from SPELL_AURA_MOD_DODGE_PERCENT aura
            nondiminishing += GetTotalAuraModifier(AuraType.ModDodgePercent);
            // Dodge from rating
            diminishing += GetRatingBonusValue(CombatRating.Dodge);
            // apply diminishing formula to diminishing dodge chance
            int pclass = (int)GetClass() - 1;
            float value = nondiminishing + (diminishing * dodge_cap[pclass] / (diminishing + dodge_cap[pclass] * m_diminishing_k[pclass]));

            if (WorldConfig.GetBoolValue(WorldCfg.StatsLimitsEnable))
                value = value > WorldConfig.GetFloatValue(WorldCfg.StatsLimitsDodge) ? WorldConfig.GetFloatValue(WorldCfg.StatsLimitsDodge) : value;

            value = value < 0.0f ? 0.0f : value;
            SetStatFloatValue(ActivePlayerFields.DodgePercentage, value);
        }
        public void UpdateBlockPercentage()
        {
            // No block
            float value = 0.0f;
            if (CanBlock())
            {
                // Base value
                value = 5.0f;
                // Increase from SPELL_AURA_MOD_BLOCK_PERCENT aura
                value += GetTotalAuraModifier(AuraType.ModBlockPercent);
                // Increase from rating
                value += GetRatingBonusValue(CombatRating.Block);

                if (WorldConfig.GetBoolValue(WorldCfg.StatsLimitsEnable))
                    value = value > WorldConfig.GetFloatValue(WorldCfg.StatsLimitsBlock) ? WorldConfig.GetFloatValue(WorldCfg.StatsLimitsBlock) : value;

                value = value < 0.0f ? 0.0f : value;
            }
            SetFloatValue(ActivePlayerFields.BlockPercentage, value);
        }

        public void UpdateCritPercentage(WeaponAttackType attType)
        {
            BaseModGroup modGroup;
            ActivePlayerFields index;
            CombatRating cr;

            switch (attType)
            {
                case WeaponAttackType.OffAttack:
                    modGroup = BaseModGroup.OffhandCritPercentage;
                    index = ActivePlayerFields.OffhandCritPercentage;
                    cr = CombatRating.CritMelee;
                    break;
                case WeaponAttackType.RangedAttack:
                    modGroup = BaseModGroup.RangedCritPercentage;
                    index = ActivePlayerFields.RangedCritPercentage;
                    cr = CombatRating.CritRanged;
                    break;
                case WeaponAttackType.BaseAttack:
                default:
                    modGroup = BaseModGroup.CritPercentage;
                    index = ActivePlayerFields.CritPercentage;
                    cr = CombatRating.CritMelee;
                    break;
            }

            float value = GetTotalPercentageModValue(modGroup) + GetRatingBonusValue(cr);
            // Modify crit from weapon skill and maximized defense skill of same level victim difference
            value += (GetMaxSkillValueForLevel() - GetMaxSkillValueForLevel()) * 0.04f;

            if (WorldConfig.GetBoolValue(WorldCfg.StatsLimitsEnable))
                value = value > WorldConfig.GetFloatValue(WorldCfg.StatsLimitsCrit) ? WorldConfig.GetFloatValue(WorldCfg.StatsLimitsCrit) : value;

            value = value < 0.0f ? 0.0f : value;
            SetFloatValue(index, value);
        }

        public void UpdateExpertise(WeaponAttackType attack)
        {
            if (attack == WeaponAttackType.RangedAttack)
                return;

            int expertise = (int)GetRatingBonusValue(CombatRating.Expertise);

            Item weapon = GetWeaponForAttack(attack, true);

            expertise += GetTotalAuraModifier(AuraType.ModExpertise, aurEff => aurEff.GetSpellInfo().IsItemFitToSpellRequirements(weapon));

            if (expertise < 0)
                expertise = 0;

            switch (attack)
            {
                case WeaponAttackType.BaseAttack:
                    SetInt32Value(ActivePlayerFields.Expertise, expertise);
                    break;
                case WeaponAttackType.OffAttack:
                    SetInt32Value(ActivePlayerFields.OffhandExpertise, expertise);
                    break;
                default: break;
            }
        }

        float GetGameTableColumnForCombatRating(GtCombatRatingsRecord row, CombatRating rating)
        {
            switch (rating)
            {
                case CombatRating.Amplify:
                    return row.Amplify;
                case CombatRating.DefenseSkill:
                    return row.DefenseSkill;
                case CombatRating.Dodge:
                    return row.Dodge;
                case CombatRating.Parry:
                    return row.Parry;
                case CombatRating.Block:
                    return row.Block;
                case CombatRating.HitMelee:
                    return row.HitMelee;
                case CombatRating.HitRanged:
                    return row.HitRanged;
                case CombatRating.HitSpell:
                    return row.HitSpell;
                case CombatRating.CritMelee:
                    return row.CritMelee;
                case CombatRating.CritRanged:
                    return row.CritRanged;
                case CombatRating.CritSpell:
                    return row.CritSpell;
                case CombatRating.Multistrike:
                    return row.MultiStrike;
                case CombatRating.Readiness:
                    return row.Readiness;
                case CombatRating.Speed:
                    return row.Speed;
                case CombatRating.ResilienceCritTaken:
                    return row.ResilienceCritTaken;
                case CombatRating.ResiliencePlayerDamage:
                    return row.ResiliencePlayerDamage;
                case CombatRating.Lifesteal:
                    return row.Lifesteal;
                case CombatRating.HasteMelee:
                    return row.HasteMelee;
                case CombatRating.HasteRanged:
                    return row.HasteRanged;
                case CombatRating.HasteSpell:
                    return row.HasteSpell;
                case CombatRating.Avoidance:
                    return row.Avoidance;
                case CombatRating.Studiness:
                    return row.Sturdiness;
                case CombatRating.Unused7:
                    return row.Unused7;
                case CombatRating.Expertise:
                    return row.Expertise;
                case CombatRating.ArmorPenetration:
                    return row.ArmorPenetration;
                case CombatRating.Mastery:
                    return row.Mastery;
                case CombatRating.PvpPower:
                    return row.PvPPower;
                case CombatRating.Cleave:
                    return row.Cleave;
                case CombatRating.VersatilityDamageDone:
                    return row.VersatilityDamageDone;
                case CombatRating.VersatilityHealingDone:
                    return row.VersatilityHealingDone;
                case CombatRating.VersatilityDamageTaken:
                    return row.VersatilityDamageTaken;
                case CombatRating.Unused12:
                    return row.Unused12;
                default:
                    break;
            }
            return 1.0f;
        }

        public void UpdateSpellCritChance()
        {
            // For others recalculate it from:
            float crit = 5.0f;
            // Increase crit from SPELL_AURA_MOD_SPELL_CRIT_CHANCE
            crit += GetTotalAuraModifier(AuraType.ModSpellCritChance);
            // Increase crit from SPELL_AURA_MOD_CRIT_PCT
            crit += GetTotalAuraModifier(AuraType.ModCritPct);
            // Increase crit from spell crit ratings
            crit += GetRatingBonusValue(CombatRating.CritSpell);

            // Store crit value
            SetFloatValue(ActivePlayerFields.SpellCritPercentage1, crit);
        }

        public void UpdateMeleeHitChances()
        {
            m_modMeleeHitChance = 7.5f + GetTotalAuraModifier(AuraType.ModHitChance);
            m_modMeleeHitChance += GetRatingBonusValue(CombatRating.HitMelee);
        }

        public void UpdateRangedHitChances()
        {
            m_modRangedHitChance = 7.5f + GetTotalAuraModifier(AuraType.ModHitChance);
            m_modRangedHitChance += GetRatingBonusValue(CombatRating.HitRanged);
        }

        public void UpdateSpellHitChances()
        {
            m_modSpellHitChance = 15.0f + GetTotalAuraModifier(AuraType.ModSpellHitChance);
            m_modSpellHitChance += GetRatingBonusValue(CombatRating.HitSpell);
        }
        public override void UpdateMaxHealth()
        {
            UnitMods unitMod = UnitMods.Health;

            float value = GetModifierValue(unitMod, UnitModifierType.BaseValue) + GetCreateHealth();
            value *= GetModifierValue(unitMod, UnitModifierType.BasePCT);
            value += GetModifierValue(unitMod, UnitModifierType.TotalValue) + GetHealthBonusFromStamina();
            value *= GetModifierValue(unitMod, UnitModifierType.TotalPCT);

            SetMaxHealth((uint)value);
        }
        float GetHealthBonusFromStamina()
        {
            // Taken from PaperDollFrame.lua - 6.0.3.19085
            float ratio = 10.0f;
            GtHpPerStaRecord hpBase = CliDB.HpPerStaGameTable.GetRow(getLevel());
            if (hpBase != null)
                ratio = hpBase.Health;

            float stamina = GetStat(Stats.Stamina);

            return stamina * ratio;
        }
        public override uint GetPowerIndex(PowerType powerType)
        {
            return Global.DB2Mgr.GetPowerIndexByClass(powerType, GetClass());
        }
        public override void UpdateMaxPower(PowerType power)
        {
            uint powerIndex = GetPowerIndex(power);
            if (powerIndex == (uint)PowerType.Max || powerIndex >= (uint)PowerType.MaxPerClass)
                return;

            UnitMods unitMod = UnitMods.PowerStart + (int)power;

            float value = GetModifierValue(unitMod, UnitModifierType.BaseValue) + GetCreatePowers(power);
            value *= GetModifierValue(unitMod, UnitModifierType.BasePCT);
            value += GetModifierValue(unitMod, UnitModifierType.TotalValue);
            value *= GetModifierValue(unitMod, UnitModifierType.TotalPCT);

            SetMaxPower(power, (int)Math.Round(value));
        }

        public void ApplySpellPenetrationBonus(int amount, bool apply)
        {
            ApplyModInt32Value(ActivePlayerFields.ModTargetResistance, -amount, apply);
            m_spellPenetrationItemMod += apply ? amount : -amount;
        }

        void ApplyManaRegenBonus(int amount, bool apply)
        {
            _ModifyUInt32(apply, ref m_baseManaRegen, ref amount);
            UpdateManaRegen();
        }

        void ApplyHealthRegenBonus(int amount, bool apply)
        {
            _ModifyUInt32(apply, ref m_baseHealthRegen, ref amount);
        }

        void ApplySpellPowerBonus(int amount, bool apply)
        {
            if (HasAuraType(AuraType.OverrideSpellPowerByApPct))
                return;

            apply = _ModifyUInt32(apply, ref m_baseSpellPower, ref amount);

            // For speed just update for client
            ApplyModUInt32Value(ActivePlayerFields.ModHealingDonePos, amount, apply);
            for (int i = (int)SpellSchools.Holy; i < (int)SpellSchools.Max; ++i)
                ApplyModUInt32Value(ActivePlayerFields.ModDamageDonePos + i, amount, apply);

            if (HasAuraType(AuraType.OverrideAttackPowerBySpPct))
            {
                UpdateAttackPowerAndDamage();
                UpdateAttackPowerAndDamage(true);
            }
        }

        public bool _ModifyUInt32(bool apply, ref uint baseValue, ref int amount)
        {
            // If amount is negative, change sign and value of apply.
            if (amount < 0)
            {
                apply = !apply;
                amount = -amount;
            }
            if (apply)
                baseValue += (uint)amount;
            else
            {
                // Make sure we do not get public uint overflow.
                if (amount > baseValue)
                    amount = (int)baseValue;
                baseValue -= (uint)amount;
            }
            return apply;
        }

        float[] m_diminishing_k =
        {
            0.9560f,  // Warrior
            0.9560f,  // Paladin
            0.9880f,  // Hunter
            0.9880f,  // Rogue
            0.9830f,  // Priest
            0.9560f,  // DK
            0.9880f,  // Shaman
            0.9830f,  // Mage
            0.9830f,  // Warlock
            0.9830f,  // Monk
            0.9720f,  // Druid
            0.9830f   // Demon Hunter
        };

        void SetBaseModValue(BaseModGroup modGroup, BaseModType modType, float value) { m_auraBaseMod[(int)modGroup][(int)modType] = value; }
    }

    public partial class Creature
    {
        public override bool UpdateStats(Stats stat)
        {
            return true;
        }

        public override bool UpdateAllStats()
        {
            UpdateMaxHealth();
            UpdateAttackPowerAndDamage();
            UpdateAttackPowerAndDamage(true);

            for (var i = PowerType.Mana; i < PowerType.Max; ++i)
                UpdateMaxPower(i);

            UpdateAllResistances();

            return true;
        }

        public override void UpdateArmor()
        {
            float baseValue = GetModifierValue(UnitMods.Armor, UnitModifierType.BaseValue);
            float value = GetTotalAuraModValue(UnitMods.Armor);
            SetArmor((int)baseValue, (int)(value - baseValue));
        }

        public override void UpdateMaxHealth()
        {
            float value = GetTotalAuraModValue(UnitMods.Health);
            SetMaxHealth((uint)value);
        }

        public override uint GetPowerIndex(PowerType powerType)
        {
            if (powerType == GetPowerType())
                return 0;
            if (powerType == PowerType.AlternatePower)
                return 1;
            if (powerType == PowerType.ComboPoints)
                return 2;

            return (uint)PowerType.Max;
        }

        public override void UpdateMaxPower(PowerType power)
        {
            if (GetPowerIndex(power) == (uint)PowerType.Max)
                return;

            UnitMods unitMod = UnitMods.PowerStart + (int)power;

            float value = GetModifierValue(unitMod, UnitModifierType.BaseValue) + GetCreatePowers(power);
            value *= GetModifierValue(unitMod, UnitModifierType.BasePCT);
            value += GetModifierValue(unitMod, UnitModifierType.TotalValue);
            value *= GetModifierValue(unitMod, UnitModifierType.TotalPCT);

            SetMaxPower(power, (int)Math.Round(value));
        }

        public override void UpdateAttackPowerAndDamage(bool ranged = false)
        {
            UnitMods unitMod = ranged ? UnitMods.AttackPowerRanged : UnitMods.AttackPower;

            UnitFields index = UnitFields.AttackPower;
            UnitFields index_mult = UnitFields.AttackPowerMultiplier;

            if (ranged)
            {
                index = UnitFields.RangedAttackPower;
                index_mult = UnitFields.RangedAttackPowerMultiplier;
            }

            float base_attPower = GetModifierValue(unitMod, UnitModifierType.BaseValue) * GetModifierValue(unitMod, UnitModifierType.BasePCT);
            float attPowerMultiplier = GetModifierValue(unitMod, UnitModifierType.TotalPCT) - 1.0f;

            SetInt32Value(index, (int)base_attPower);            //UNIT_FIELD_(RANGED)_ATTACK_POWER field
            SetFloatValue(index_mult, attPowerMultiplier);          //UNIT_FIELD_(RANGED)_ATTACK_POWER_MULTIPLIER field

            //automatically update weapon damage after attack power modification
            if (ranged)
                UpdateDamagePhysical(WeaponAttackType.RangedAttack);
            else
            {
                UpdateDamagePhysical(WeaponAttackType.BaseAttack);
                UpdateDamagePhysical(WeaponAttackType.OffAttack);
            }
        }

        public override void CalculateMinMaxDamage(WeaponAttackType attType, bool normalized, bool addTotalPct, out float minDamage, out float maxDamage)
        {
            float variance = 1.0f;
            UnitMods unitMod;
            switch (attType)
            {
                case WeaponAttackType.BaseAttack:
                default:
                    variance = GetCreatureTemplate().BaseVariance;
                    unitMod = UnitMods.DamageMainHand;
                    break;
                case WeaponAttackType.OffAttack:
                    variance = GetCreatureTemplate().BaseVariance;
                    unitMod = UnitMods.DamageOffHand;
                    break;
                case WeaponAttackType.RangedAttack:
                    variance = GetCreatureTemplate().RangeVariance;
                    unitMod = UnitMods.DamageRanged;
                    break;
            }

            if (attType == WeaponAttackType.OffAttack && !haveOffhandWeapon())
            {
                minDamage = 0.0f;
                maxDamage = 0.0f;
                return;
            }

            float weaponMinDamage = GetWeaponDamageRange(attType, WeaponDamageRange.MinDamage);
            float weaponMaxDamage = GetWeaponDamageRange(attType, WeaponDamageRange.MaxDamage);

            if (!CanUseAttackType(attType)) // disarm case
            {
                weaponMinDamage = 0.0f;
                weaponMaxDamage = 0.0f;
            }

            float attackPower = GetTotalAttackPowerValue(attType);
            float attackSpeedMulti = Math.Max(GetAPMultiplier(attType, normalized), 0.25f);

            float baseValue = GetModifierValue(unitMod, UnitModifierType.BaseValue) + (attackPower / 3.5f) * variance;
            float basePct = GetModifierValue(unitMod, UnitModifierType.BasePCT) * attackSpeedMulti;
            float totalValue = GetModifierValue(unitMod, UnitModifierType.TotalValue);
            float totalPct = addTotalPct ? GetModifierValue(unitMod, UnitModifierType.TotalPCT) : 1.0f;
            float dmgMultiplier = GetCreatureTemplate().ModDamage; // = ModDamage * _GetDamageMod(rank);

            minDamage = ((weaponMinDamage + baseValue) * dmgMultiplier * basePct + totalValue) * totalPct;
            maxDamage = ((weaponMaxDamage + baseValue) * dmgMultiplier * basePct + totalValue) * totalPct;
        }
    }
}
