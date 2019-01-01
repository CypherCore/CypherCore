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
using Framework.Dynamic;
using Game.BattleGrounds;
using Game.Network.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public partial class Unit
    {
        public virtual bool HasSpell(uint spellId) { return false; }

        // function uses real base points (typically value - 1)
        public int CalculateSpellDamage(Unit target, SpellInfo spellProto, uint effect_index, int? basePoints = null, int itemLevel = -1)
        {
            SpellEffectInfo effect = spellProto.GetEffect(GetMap().GetDifficultyID(), effect_index);
            return effect != null ? effect.CalcValue(this, basePoints, target) : 0;
        }
        public int CalculateSpellDamage(Unit target, SpellInfo spellProto, uint effect_index, out float variance, int? basePoints = null, int itemLevel = -1)
        {
            SpellEffectInfo effect = spellProto.GetEffect(GetMap().GetDifficultyID(), effect_index);
            variance = 0.0f;
            return effect != null ? effect.CalcValue(out variance, this, basePoints, target, itemLevel) : 0;
        }

        public int SpellBaseDamageBonusDone(SpellSchoolMask schoolMask)
        {
            if (IsTypeId(TypeId.Player))
            {
                float overrideSP = GetFloatValue(ActivePlayerFields.OverrideSpellPowerByApPct);
                if (overrideSP > 0.0f)
                    return (int)(MathFunctions.CalculatePct(GetTotalAttackPowerValue(WeaponAttackType.BaseAttack), overrideSP) + 0.5f);
            }

            int DoneAdvertisedBenefit = GetTotalAuraModifierByMiscMask(AuraType.ModDamageDone, (int)schoolMask);

            if (IsTypeId(TypeId.Player))
            {
                // Base value
                DoneAdvertisedBenefit += (int)ToPlayer().GetBaseSpellPowerBonus();

                // Check if we are ever using mana - PaperDollFrame.lua
                if (GetPowerIndex(PowerType.Mana) != (uint)PowerType.Max)
                    DoneAdvertisedBenefit += Math.Max(0, (int)GetStat(Stats.Intellect));  // spellpower from intellect

                // Damage bonus from stats
                var mDamageDoneOfStatPercent = GetAuraEffectsByType(AuraType.ModSpellDamageOfStatPercent);
                foreach (var eff in mDamageDoneOfStatPercent)
                {
                    if (Convert.ToBoolean(eff.GetMiscValue() & (int)schoolMask))
                    {
                        // stat used stored in miscValueB for this aura
                        Stats usedStat = (Stats)eff.GetMiscValueB();
                        DoneAdvertisedBenefit += (int)MathFunctions.CalculatePct(GetStat(usedStat), eff.GetAmount());
                    }
                }
                // ... and attack power
                DoneAdvertisedBenefit += (int)MathFunctions.CalculatePct(GetTotalAttackPowerValue(WeaponAttackType.BaseAttack), GetTotalAuraModifierByMiscMask(AuraType.ModSpellDamageOfAttackPower, (int)schoolMask));

            }
            return DoneAdvertisedBenefit;
        }

        public int SpellBaseDamageBonusTaken(SpellSchoolMask schoolMask)
        {
            return GetTotalAuraModifierByMiscMask(AuraType.ModDamageTaken, (int)schoolMask);
        }

        public uint SpellDamageBonusDone(Unit victim, SpellInfo spellProto, uint pdamage, DamageEffectType damagetype, SpellEffectInfo effect, uint stack = 1)
        {
            if (spellProto == null || victim == null || damagetype == DamageEffectType.Direct)
                return pdamage;

            // Some spells don't benefit from done mods
            if (spellProto.HasAttribute(SpellAttr3.NoDoneBonus))
                return pdamage;

            // For totems get damage bonus from owner
            if (IsTypeId(TypeId.Unit) && IsTotem())
            {
                Unit owner = GetOwner();
                if (owner != null)
                    return owner.SpellDamageBonusDone(victim, spellProto, pdamage, damagetype, effect, stack);
            }

            int DoneTotal = 0;

            // Done fixed damage bonus auras
            int DoneAdvertisedBenefit = SpellBaseDamageBonusDone(spellProto.GetSchoolMask());
            // Pets just add their bonus damage to their spell damage
            // note that their spell damage is just gain of their own auras
            if (HasUnitTypeMask(UnitTypeMask.Guardian))
                DoneAdvertisedBenefit += ((Guardian)this).GetBonusDamage();

            // Check for table values
            if (effect.BonusCoefficientFromAP > 0.0f)
            {
                float ApCoeffMod = effect.BonusCoefficientFromAP;
                Player modOwner = GetSpellModOwner();
                if (modOwner)
                {
                    ApCoeffMod *= 100.0f;
                    modOwner.ApplySpellMod(spellProto.Id, SpellModOp.BonusMultiplier, ref ApCoeffMod);
                    ApCoeffMod /= 100.0f;
                }

                WeaponAttackType attType = (spellProto.IsRangedWeaponSpell() && spellProto.DmgClass != SpellDmgClass.Melee) ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack;
                float APbonus = victim.GetTotalAuraModifier(attType == WeaponAttackType.BaseAttack ? AuraType.MeleeAttackPowerAttackerBonus : AuraType.RangedAttackPowerAttackerBonus);
                APbonus += GetTotalAttackPowerValue(attType);
                DoneTotal += (int)(stack * ApCoeffMod * APbonus);
            }

            // Default calculation
            float coeff = effect.BonusCoefficient;
            if (DoneAdvertisedBenefit != 0)
            {
                Player modOwner = GetSpellModOwner();
                if (modOwner)
                {
                    coeff *= 100.0f;
                    modOwner.ApplySpellMod(spellProto.Id, SpellModOp.BonusMultiplier, ref coeff);
                    coeff /= 100.0f;
                }
                DoneTotal += (int)(DoneAdvertisedBenefit * coeff * stack);
            }

            // Done Percentage for DOT is already calculated, no need to do it again. The percentage mod is applied in Aura.HandleAuraSpecificMods.
            float tmpDamage = ((int)pdamage + DoneTotal) * (damagetype == DamageEffectType.DOT ? 1.0f : SpellDamagePctDone(victim, spellProto, damagetype));
            // apply spellmod to Done damage (flat and pct)
            Player _modOwner = GetSpellModOwner();
            if (_modOwner)
            {
                if (damagetype == DamageEffectType.DOT)
                    _modOwner.ApplySpellMod(spellProto.Id, SpellModOp.Dot, ref tmpDamage);
                else
                    _modOwner.ApplySpellMod(spellProto.Id, SpellModOp.Damage, ref tmpDamage);
            }

            return (uint)Math.Max(tmpDamage, 0.0f);
        }

        public float SpellDamagePctDone(Unit victim, SpellInfo spellProto, DamageEffectType damagetype)
        {
            if (spellProto == null || !victim || damagetype == DamageEffectType.Direct)
                return 1.0f;

            // Some spells don't benefit from pct done mods
            if (spellProto.HasAttribute(SpellAttr6.NoDonePctDamageMods))
                return 1.0f;

            // For totems pct done mods are calculated when its calculation is run on the player in SpellDamageBonusDone.
            if (IsTypeId(TypeId.Unit) && IsTotem())
                return 1.0f;

            // Done total percent damage auras
            float DoneTotalMod = 1.0f;

            // Pet damage?
            if (IsTypeId(TypeId.Unit) && !IsPet())
                DoneTotalMod *= ToCreature().GetSpellDamageMod(ToCreature().GetCreatureTemplate().Rank);

            // Versatility
            Player modOwner = GetSpellModOwner();
            if (modOwner)
                MathFunctions.AddPct(ref DoneTotalMod, modOwner.GetRatingBonusValue(CombatRating.VersatilityDamageDone) + modOwner.GetTotalAuraModifier(AuraType.ModVersatility));

            float maxModDamagePercentSchool = 0.0f;
            if (IsTypeId(TypeId.Player))
            {
                for (int i = 0; i < (int)SpellSchools.Max; ++i)
                {
                    if (Convert.ToBoolean((int)spellProto.GetSchoolMask() & (1 << i)))
                        maxModDamagePercentSchool = Math.Max(maxModDamagePercentSchool, GetFloatValue(ActivePlayerFields.ModDamageDonePct + i));
                }
            }
            else
                maxModDamagePercentSchool = GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentDone, (uint)spellProto.GetSchoolMask());

            DoneTotalMod *= maxModDamagePercentSchool;

            uint creatureTypeMask = victim.GetCreatureTypeMask();

            DoneTotalMod *= GetTotalAuraMultiplierByMiscMask(AuraType.ModDamageDoneVersus, creatureTypeMask);

            // bonus against aurastate
            DoneTotalMod *= GetTotalAuraMultiplier(AuraType.ModDamageDoneVersusAurastate, aurEff =>
            {
                if (victim.HasAuraState((AuraStateType)aurEff.GetMiscValue()))
                    return true;
                return false;
            });

            // Add SPELL_AURA_MOD_DAMAGE_DONE_FOR_MECHANIC percent bonus
            MathFunctions.AddPct(ref DoneTotalMod, GetTotalAuraModifierByMiscValue(AuraType.ModDamageDoneForMechanic, (int)spellProto.Mechanic));

            // Custom scripted damage
            switch (spellProto.SpellFamilyName)
            {
                case SpellFamilyNames.Mage:
                    // Ice Lance (no unique family flag)
                    if (spellProto.Id == 228598)
                        if (victim.HasAuraState(AuraStateType.Frozen, spellProto, this))
                            DoneTotalMod *= 3.0f;

                    break;
                case SpellFamilyNames.Warlock:
                    // Shadow Bite (30% increase from each dot)
                    if (spellProto.SpellFamilyFlags[1].HasAnyFlag<uint>(0x00400000) && IsPet())
                    {
                        uint count = victim.GetDoTsByCaster(GetOwnerGUID());
                        if (count != 0)
                            MathFunctions.AddPct(ref DoneTotalMod, 30 * count);
                    }

                    // Drain Soul - increased damage for targets under 20% HP
                    if (spellProto.Id == 198590)
                        if (HasAuraState(AuraStateType.HealthLess20Percent))
                            DoneTotalMod *= 2;
                    break;
            }

            return DoneTotalMod;
        }

        public uint SpellDamageBonusTaken(Unit caster, SpellInfo spellProto, uint pdamage, DamageEffectType damagetype, SpellEffectInfo effect, uint stack = 1)
        {
            if (spellProto == null || damagetype == DamageEffectType.Direct)
                return pdamage;

            int TakenTotal = 0;
            float TakenTotalMod = 1.0f;
            float TakenTotalCasterMod = 0.0f;

            // Mod damage from spell mechanic
            uint mechanicMask = spellProto.GetAllEffectsMechanicMask();
            if (mechanicMask != 0)
            {
                TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModMechanicDamageTakenPercent, aurEff =>
                {
                    if ((mechanicMask & (1 << aurEff.GetMiscValue())) != 0)
                        return true;
                    return false;
                });
            }

            AuraEffect cheatDeath = GetAuraEffect(45182, 0);
            if (cheatDeath != null)
                if (cheatDeath.GetMiscValue().HasAnyFlag((int)SpellSchoolMask.Normal))
                    MathFunctions.AddPct(ref TakenTotalMod, cheatDeath.GetAmount());

            // Spells with SPELL_ATTR4_FIXED_DAMAGE should only benefit from mechanic damage mod auras.
            if (!spellProto.HasAttribute(SpellAttr4.FixedDamage))
            {
                // get all auras from caster that allow the spell to ignore resistance (sanctified wrath)
                TakenTotalCasterMod += GetTotalAuraModifierByMiscMask(AuraType.ModIgnoreTargetResist, (int)spellProto.GetSchoolMask());

                // Versatility
                Player modOwner = GetSpellModOwner();
                if (modOwner)
                {
                    // only 50% of SPELL_AURA_MOD_VERSATILITY for damage reduction
                    float versaBonus = modOwner.GetTotalAuraModifier(AuraType.ModVersatility) / 2.0f;
                    MathFunctions.AddPct(ref TakenTotalMod, -(modOwner.GetRatingBonusValue(CombatRating.VersatilityDamageTaken) + versaBonus));
                }

                // from positive and negative SPELL_AURA_MOD_DAMAGE_PERCENT_TAKEN
                // multiplicative bonus, for example Dispersion + Shadowform (0.10*0.85=0.085)
                TakenTotalMod *= GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentTaken, (uint)spellProto.GetSchoolMask());

                // From caster spells
                TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModSpellDamageFromCaster, aurEff =>
                {
                    if (aurEff.GetCasterGUID() == caster.GetGUID() && aurEff.IsAffectingSpell(spellProto))
                        return true;
                    return false;
                });

                int TakenAdvertisedBenefit = SpellBaseDamageBonusTaken(spellProto.GetSchoolMask());

                // Check for table values
                float coeff = effect.BonusCoefficient;

                // Default calculation
                if (TakenAdvertisedBenefit != 0)
                {
                    // level penalty still applied on Taken bonus - is it blizzlike?
                    if (modOwner)
                    {
                        coeff *= 100.0f;
                        modOwner.ApplySpellMod(spellProto.Id, SpellModOp.BonusMultiplier, ref coeff);
                        coeff /= 100.0f;
                    }
                    TakenTotal += (int)(TakenAdvertisedBenefit * coeff * stack);
                }
            }

            float tmpDamage = 0.0f;

            if (TakenTotalCasterMod != 0)
            {
                if (TakenTotal < 0)
                {
                    if (TakenTotalMod < 1)
                        tmpDamage = (((MathFunctions.CalculatePct(pdamage, TakenTotalCasterMod) + TakenTotal) * TakenTotalMod) + MathFunctions.CalculatePct(pdamage, TakenTotalCasterMod));
                    else
                        tmpDamage = (((float)(MathFunctions.CalculatePct(pdamage, TakenTotalCasterMod) + TakenTotal) + MathFunctions.CalculatePct(pdamage, TakenTotalCasterMod)) * TakenTotalMod);
                }
                else if (TakenTotalMod < 1)
                    tmpDamage = ((MathFunctions.CalculatePct(pdamage + TakenTotal, TakenTotalCasterMod) * TakenTotalMod) + MathFunctions.CalculatePct(pdamage + TakenTotal, TakenTotalCasterMod));
            }
            if (tmpDamage == 0)
                tmpDamage = (pdamage + TakenTotal) * TakenTotalMod;

            return (uint)Math.Max(tmpDamage, 0.0f);
        }

        public uint SpellBaseHealingBonusDone(SpellSchoolMask schoolMask)
        {
            if (IsTypeId(TypeId.Player))
            {
                float overrideSP = GetFloatValue(ActivePlayerFields.OverrideSpellPowerByApPct);
                if (overrideSP > 0.0f)
                    return (uint)(MathFunctions.CalculatePct(GetTotalAttackPowerValue(WeaponAttackType.BaseAttack), overrideSP) + 0.5f);
            }

            uint advertisedBenefit = (uint)GetTotalAuraModifier(AuraType.ModHealingDone, aurEff =>
            {
                if (aurEff.GetMiscValue() == 0 || (aurEff.GetMiscValue() & (int)schoolMask) != 0)
                    return true;
                return false;
            });

            // Healing bonus of spirit, intellect and strength
            if (IsTypeId(TypeId.Player))
            {
                // Base value
                advertisedBenefit += ToPlayer().GetBaseSpellPowerBonus();

                // Check if we are ever using mana - PaperDollFrame.lua
                if (GetPowerIndex(PowerType.Mana) != (uint)PowerType.Max)
                    advertisedBenefit += Math.Max(0, (uint)GetStat(Stats.Intellect));  // spellpower from intellect

                // Healing bonus from stats
                var mHealingDoneOfStatPercent = GetAuraEffectsByType(AuraType.ModSpellHealingOfStatPercent);
                foreach (var i in mHealingDoneOfStatPercent)
                {
                    // stat used dependent from misc value (stat index)
                    Stats usedStat = (Stats)(i.GetSpellEffectInfo().MiscValue);
                    advertisedBenefit += (uint)MathFunctions.CalculatePct(GetStat(usedStat), i.GetAmount());
                }

                // ... and attack power
                var mHealingDonebyAP = GetAuraEffectsByType(AuraType.ModSpellHealingOfAttackPower);
                foreach (var i in mHealingDonebyAP)
                    if (Convert.ToBoolean(i.GetMiscValue() & (int)schoolMask))
                        advertisedBenefit += (uint)MathFunctions.CalculatePct(GetTotalAttackPowerValue(WeaponAttackType.BaseAttack), i.GetAmount());
            }
            return advertisedBenefit;
        }

        int SpellBaseHealingBonusTaken(SpellSchoolMask schoolMask)
        {
            return GetTotalAuraModifierByMiscMask(AuraType.ModHealing, (int)schoolMask);
        }

        public int SpellCriticalHealingBonus(SpellInfo spellProto, int damage, Unit victim)
        {
            // Calculate critical bonus
            int crit_bonus = damage;

            damage += crit_bonus;

            damage = (int)(damage * GetTotalAuraMultiplier(AuraType.ModCriticalHealingAmount));

            return damage;
        }

        public uint SpellHealingBonusDone(Unit victim, SpellInfo spellProto, uint healamount, DamageEffectType damagetype, SpellEffectInfo effect, uint stack = 1)
        {
            // For totems get healing bonus from owner (statue isn't totem in fact)
            if (IsTypeId(TypeId.Unit) && IsTotem())
            {
                Unit owner = GetOwner();
                if (owner)
                    return owner.SpellHealingBonusDone(victim, spellProto, healamount, damagetype, effect, stack);
            }

            // No bonus healing for potion spells
            if (spellProto.SpellFamilyName == SpellFamilyNames.Potion)
                return healamount;

            int DoneTotal = 0;

            // done scripted mod (take it from owner)
            Unit owner1 = GetOwner() ?? this;
            var mOverrideClassScript = owner1.GetAuraEffectsByType(AuraType.OverrideClassScripts);
            foreach (var eff in mOverrideClassScript)
            {
                if (!eff.IsAffectingSpell(spellProto))
                    continue;

                switch (eff.GetMiscValue())
                {
                    case 3736: // Hateful Totem of the Third Wind / Increased Lesser Healing Wave / LK Arena (4/5/6) Totem of the Third Wind / Savage Totem of the Third Wind
                        DoneTotal += eff.GetAmount();
                        break;
                    default:
                        break;
                }
            }

            // Done fixed damage bonus auras
            uint DoneAdvertisedBenefit = SpellBaseHealingBonusDone(spellProto.GetSchoolMask());

            // Check for table values
            float coeff = effect.BonusCoefficient;
            if (effect.BonusCoefficientFromAP > 0.0f)
            {
                DoneTotal += (int)(effect.BonusCoefficientFromAP * stack * GetTotalAttackPowerValue(
                    (spellProto.IsRangedWeaponSpell() && spellProto.DmgClass != SpellDmgClass.Melee) ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack));
            }
            else if (coeff <= 0.0f)
            {
                // No bonus healing for SPELL_DAMAGE_CLASS_NONE class spells by default
                if (spellProto.DmgClass == SpellDmgClass.None)
                    return healamount;
            }

            // Default calculation
            if (DoneAdvertisedBenefit != 0)
            {
                Player modOwner = GetSpellModOwner();
                if (modOwner)
                {
                    coeff *= 100.0f;
                    modOwner.ApplySpellMod(spellProto.Id, SpellModOp.BonusMultiplier, ref coeff);
                    coeff /= 100.0f;
                }

                DoneTotal += (int)(DoneAdvertisedBenefit * coeff * stack);
            }

            foreach (SpellEffectInfo eff in spellProto.GetEffectsForDifficulty(GetMap().GetDifficultyID()))
            {
                if (eff == null)
                    continue;

                switch (eff.ApplyAuraName)
                {
                    // Bonus healing does not apply to these spells
                    case AuraType.PeriodicLeech:
                    case AuraType.PeriodicHealthFunnel:
                        DoneTotal = 0;
                        break;
                }
                if (eff.Effect == SpellEffectName.HealthLeech)
                    DoneTotal = 0;
            }

            // use float as more appropriate for negative values and percent applying
            float heal = (healamount + DoneTotal) * (damagetype == DamageEffectType.DOT ? 1.0f : SpellHealingPctDone(victim, spellProto));
            // apply spellmod to Done amount
            Player _modOwner = GetSpellModOwner();
            if (_modOwner)
            {
                if (damagetype == DamageEffectType.DOT)
                    _modOwner.ApplySpellMod(spellProto.Id, SpellModOp.Dot, ref heal);
                else
                    _modOwner.ApplySpellMod(spellProto.Id, SpellModOp.Damage, ref heal);
            }

            return (uint)Math.Max(heal, 0.0f);
        }

        public float SpellHealingPctDone(Unit victim, SpellInfo spellProto)
        {
            // For totems pct done mods are calculated when its calculation is run on the player in SpellHealingBonusDone.
            if (IsTypeId(TypeId.Unit) && IsTotem())
                return 1.0f;

            // No bonus healing for potion spells
            if (spellProto.SpellFamilyName == SpellFamilyNames.Potion)
                return 1.0f;

            if (IsPlayer())
                return GetFloatValue(ActivePlayerFields.ModHealingDonePct);

            float DoneTotalMod = 1.0f;

            // Healing done percent
            DoneTotalMod *= GetTotalAuraMultiplier(AuraType.ModHealingDonePercent);

            return DoneTotalMod;
        }

        public uint SpellHealingBonusTaken(Unit caster, SpellInfo spellProto, uint healamount, DamageEffectType damagetype, SpellEffectInfo effect, uint stack = 1)
        {
            float TakenTotalMod = 1.0f;

            // Healing taken percent
            float minval = GetMaxNegativeAuraModifier(AuraType.ModHealingPct);
            if (minval != 0)
                MathFunctions.AddPct(ref TakenTotalMod, minval);

            float maxval = GetMaxPositiveAuraModifier(AuraType.ModHealingPct);
            if (maxval != 0)
                MathFunctions.AddPct(ref TakenTotalMod, maxval);

            // Tenacity increase healing % taken
            AuraEffect Tenacity = GetAuraEffect(58549, 0);
            if (Tenacity != null)
                MathFunctions.AddPct(ref TakenTotalMod, Tenacity.GetAmount());

            // Healing Done
            int TakenTotal = 0;

            // Taken fixed damage bonus auras
            int TakenAdvertisedBenefit = SpellBaseHealingBonusTaken(spellProto.GetSchoolMask());

            // Nourish cast
            if (spellProto.SpellFamilyName == SpellFamilyNames.Druid && spellProto.SpellFamilyFlags[1].HasAnyFlag(0x2000000u))
            {
                // Rejuvenation, Regrowth, Lifebloom, or Wild Growth
                if (GetAuraEffect(AuraType.PeriodicHeal, SpellFamilyNames.Druid, new FlagArray128(0x50, 0x4000010, 0)) != null)
                    // increase healing by 20%
                    TakenTotalMod *= 1.2f;
            }

            // Check for table values
            float coeff = effect.BonusCoefficient;
            if (coeff <= 0.0f)
            {
                // No bonus healing for SPELL_DAMAGE_CLASS_NONE class spells by default
                if (spellProto.DmgClass == SpellDmgClass.None)
                {
                    healamount = (uint)Math.Max((healamount * TakenTotalMod), 0.0f);
                    return healamount;
                }
            }

            // Default calculation
            if (TakenAdvertisedBenefit != 0)
            {
                Player modOwner = GetSpellModOwner();
                if (modOwner)
                {
                    coeff *= 100.0f;
                    modOwner.ApplySpellMod(spellProto.Id, SpellModOp.BonusMultiplier, ref coeff);
                    coeff /= 100.0f;
                }

                TakenTotal += (int)(TakenAdvertisedBenefit * coeff * stack);
            }

            TakenTotalMod *= GetTotalAuraMultiplier(AuraType.ModHealingReceived, aurEff =>
            {
                if (caster.GetGUID() == aurEff.GetCasterGUID() && aurEff.IsAffectingSpell(spellProto))
                    return true;
                return false;
            });

            foreach (SpellEffectInfo eff in spellProto.GetEffectsForDifficulty(GetMap().GetDifficultyID()))
            {
                if (eff == null)
                    continue;

                switch (eff.ApplyAuraName)
                {
                    // Bonus healing does not apply to these spells
                    case AuraType.PeriodicLeech:
                    case AuraType.PeriodicHealthFunnel:
                        TakenTotal = 0;
                        break;
                }
                if (eff.Effect == SpellEffectName.HealthLeech)
                    TakenTotal = 0;
            }

            float heal = (healamount + TakenTotal) * TakenTotalMod;

            return (uint)Math.Max(heal, 0.0f);
        }

        public bool IsSpellCrit(Unit victim, SpellInfo spellProto, SpellSchoolMask schoolMask, WeaponAttackType attackType = WeaponAttackType.BaseAttack)
        {
            return RandomHelper.randChance(GetUnitSpellCriticalChance(victim, spellProto, schoolMask, attackType));
        }

        public float GetUnitSpellCriticalChance(Unit victim, SpellInfo spellProto, SpellSchoolMask schoolMask, WeaponAttackType attackType = WeaponAttackType.BaseAttack)
        {
            //! Mobs can't crit with spells. Player Totems can
            //! Fire Elemental (from totem) can too - but this part is a hack and needs more research
            if (GetGUID().IsCreatureOrVehicle() && !(IsTotem() && GetOwnerGUID().IsPlayer()) && GetEntry() != 15438)
                return 0.0f;

            // not critting spell
            if (spellProto.HasAttribute(SpellAttr2.CantCrit))
                return 0.0f;

            float crit_chance = 0.0f;
            switch (spellProto.DmgClass)
            {
                case SpellDmgClass.None:
                    // We need more spells to find a general way (if there is any)
                    switch (spellProto.Id)
                    {
                        case 379:   // Earth Shield
                        case 33778: // Lifebloom Final Bloom
                        case 64844: // Divine Hymn
                        case 71607: // Item - Bauble of True Blood 10m
                        case 71646: // Item - Bauble of True Blood 25m
                            break;
                        default:
                            return 0.0f;
                    }
                    goto case SpellDmgClass.Magic;
                case SpellDmgClass.Magic:
                    {
                        if (schoolMask.HasAnyFlag(SpellSchoolMask.Normal))
                            crit_chance = 0.0f;
                        // For other schools
                        else if (IsTypeId(TypeId.Player))
                            crit_chance = GetFloatValue(ActivePlayerFields.CritPercentage);
                        else
                            crit_chance = m_baseSpellCritChance;

                        // taken
                        if (victim)
                        {
                            if (!spellProto.IsPositive())
                            {
                                // Modify critical chance by victim SPELL_AURA_MOD_ATTACKER_SPELL_AND_WEAPON_CRIT_CHANCE
                                crit_chance += victim.GetTotalAuraModifier(AuraType.ModAttackerSpellAndWeaponCritChance);
                            }
                            // scripted (increase crit chance ... against ... target by x%
                            var mOverrideClassScript = GetAuraEffectsByType(AuraType.OverrideClassScripts);
                            foreach (var eff in mOverrideClassScript)
                            {
                                if (!eff.IsAffectingSpell(spellProto))
                                    continue;

                                switch (eff.GetMiscValue())
                                {
                                    case 911: // Shatter
                                        if (victim.HasAuraState(AuraStateType.Frozen, spellProto, this))
                                        {
                                            crit_chance *= 1.5f;
                                            AuraEffect _eff = eff.GetBase().GetEffect(1);
                                            if (_eff != null)
                                                crit_chance += _eff.GetAmount();
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            // Custom crit by class
                            switch (spellProto.SpellFamilyName)
                            {
                                case SpellFamilyNames.Rogue:
                                    // Shiv-applied poisons can't crit
                                    if (FindCurrentSpellBySpellId(5938) != null)
                                        crit_chance = 0.0f;
                                    break;
                                case SpellFamilyNames.Shaman:
                                    // Lava Burst
                                    if (spellProto.SpellFamilyFlags[1].HasAnyFlag(0x00001000u))
                                    {
                                        if (victim.GetAuraEffect(AuraType.PeriodicDamage, SpellFamilyNames.Shaman, new FlagArray128(0x10000000, 0, 0), GetGUID()) != null)
                                            if (victim.GetTotalAuraModifier(AuraType.ModAttackerSpellAndWeaponCritChance) > -100)
                                                return 100.0f;
                                        break;
                                    }
                                    break;
                            }

                            // Spell crit suppression
                            if (victim.GetTypeId() == TypeId.Unit)
                            {
                                int levelDiff = (int)(victim.GetLevelForTarget(this) - getLevel());
                                crit_chance -= levelDiff * 1.0f;
                            }
                        }
                        break;
                    }
                case SpellDmgClass.Melee:
                case SpellDmgClass.Ranged:
                    {
                        if (victim)
                            crit_chance += GetUnitCriticalChance(attackType, victim);
                        break;
                    }
                default:
                    return 0.0f;
            }
            // percent done
            // only players use intelligence for critical chance computations
            Player modOwner = GetSpellModOwner();
            if (modOwner != null)
                modOwner.ApplySpellMod(spellProto.Id, SpellModOp.CriticalChance, ref crit_chance);

            // for this types the bonus was already added in GetUnitCriticalChance, do not add twice
            if (spellProto.DmgClass != SpellDmgClass.Melee && spellProto.DmgClass != SpellDmgClass.Ranged)
            {
                crit_chance += victim.GetTotalAuraModifier(AuraType.ModCritChanceForCaster, aurEff =>
                {
                    if (aurEff.GetCasterGUID() == GetGUID() && aurEff.IsAffectingSpell(spellProto))
                        return true;
                    return false;
                });
            }

            return Math.Max(crit_chance, 0.0f);
        }

        // Calculate spell hit result can be:
        // Every spell can: Evade/Immune/Reflect/Sucesful hit
        // For melee based spells:
        //   Miss
        //   Dodge
        //   Parry
        // For spells
        //   Resist
        public SpellMissInfo SpellHitResult(Unit victim, SpellInfo spellInfo, bool canReflect = false)
        {
            if (spellInfo.HasAttribute(SpellAttr3.IgnoreHitResult))
                return SpellMissInfo.None;

            // Check for immune
            if (victim.IsImmunedToSpell(spellInfo, this))
                return SpellMissInfo.Immune;

            // Damage immunity is only checked if the spell has damage effects, this immunity must not prevent aura apply
            // returns SPELL_MISS_IMMUNE in that case, for other spells, the SMSG_SPELL_GO must show hit
            if (spellInfo.HasOnlyDamageEffects() && victim.IsImmunedToDamage(spellInfo))
                return SpellMissInfo.Immune;

            // All positive spells can`t miss
            // @todo client not show miss log for this spells - so need find info for this in dbc and use it!
            if (spellInfo.IsPositive()
                && (!IsHostileTo(victim)))  // prevent from affecting enemy by "positive" spell
                return SpellMissInfo.None;

            if (this == victim)
                return SpellMissInfo.None;

            // Return evade for units in evade mode
            if (victim.IsTypeId(TypeId.Unit) && victim.ToCreature().IsEvadingAttacks())
                return SpellMissInfo.Evade;

            // Try victim reflect spell
            if (canReflect)
            {
                int reflectchance = victim.GetTotalAuraModifier(AuraType.ReflectSpells);
                reflectchance += victim.GetTotalAuraModifierByMiscMask(AuraType.ReflectSpellsSchool, (int)spellInfo.GetSchoolMask());

                if (reflectchance > 0 && RandomHelper.randChance(reflectchance))
                    return SpellMissInfo.Reflect;
            }

            switch (spellInfo.DmgClass)
            {
                case SpellDmgClass.Ranged:
                case SpellDmgClass.Melee:
                    return MeleeSpellHitResult(victim, spellInfo);
                case SpellDmgClass.None:
                    return SpellMissInfo.None;
                case SpellDmgClass.Magic:
                    return MagicSpellHitResult(victim, spellInfo);
            }
            return SpellMissInfo.None;
        }

        // Melee based spells hit result calculations
        SpellMissInfo MeleeSpellHitResult(Unit victim, SpellInfo spellInfo)
        {
            WeaponAttackType attType = WeaponAttackType.BaseAttack;

            // Check damage class instead of attack type to correctly handle judgements
            // - they are meele, but can't be dodged/parried/deflected because of ranged dmg class
            if (spellInfo.DmgClass == SpellDmgClass.Ranged)
                attType = WeaponAttackType.RangedAttack;

            int roll = RandomHelper.IRand(0, 9999);

            int missChance = (int)(MeleeSpellMissChance(victim, attType, spellInfo.Id) * 100.0f);
            // Roll miss
            int tmp = missChance;
            if (roll < tmp)
                return SpellMissInfo.Miss;

            // Chance resist mechanic
            int resist_chance = victim.GetMechanicResistChance(spellInfo) * 100;
            tmp += resist_chance;
            if (roll < tmp)
                return SpellMissInfo.Resist;

            // Same spells cannot be parried/dodged
            if (spellInfo.HasAttribute(SpellAttr0.ImpossibleDodgeParryBlock))
                return SpellMissInfo.None;

            bool canDodge = true;
            bool canParry = true;
            bool canBlock = spellInfo.HasAttribute(SpellAttr3.BlockableSpell);

            // if victim is casting or cc'd it can't avoid attacks
            if (victim.IsNonMeleeSpellCast(false) || victim.HasUnitState(UnitState.Controlled))
            {
                canDodge = false;
                canParry = false;
                canBlock = false;
            }

            // Ranged attacks can only miss, resist and deflect
            if (attType == WeaponAttackType.RangedAttack)
            {
                canParry = false;
                canDodge = false;

                // only if in front
                if (!victim.HasUnitState(UnitState.Controlled) && (victim.HasInArc(MathFunctions.PI, this) || victim.HasAuraType(AuraType.IgnoreHitDirection)))
                {
                    int deflect_chance = victim.GetTotalAuraModifier(AuraType.DeflectSpells) * 100;
                    tmp += deflect_chance;
                    if (roll < tmp)
                        return SpellMissInfo.Deflect;
                }
                return SpellMissInfo.None;
            }

            // Check for attack from behind
            if (!victim.HasInArc(MathFunctions.PI, this))
            {
                if (!victim.HasAuraType(AuraType.IgnoreHitDirection))
                {
                    // Can`t dodge from behind in PvP (but its possible in PvE)
                    if (victim.IsTypeId(TypeId.Player))
                        canDodge = false;
                    // Can`t parry or block
                    canParry = false;
                    canBlock = false;
                }
                else // Only deterrence as of 3.3.5
                {
                    if (spellInfo.HasAttribute(SpellCustomAttributes.ReqCasterBehindTarget))
                        canParry = false;
                }
            }

            // Ignore combat result aura
            var ignore = GetAuraEffectsByType(AuraType.IgnoreCombatResult);
            foreach (var aurEff in ignore)
            {
                if (!aurEff.IsAffectingSpell(spellInfo))
                    continue;

                switch ((MeleeHitOutcome)aurEff.GetMiscValue())
                {
                    case MeleeHitOutcome.Dodge:
                        canDodge = false;
                        break;
                    case MeleeHitOutcome.Block:
                        canBlock = false;
                        break;
                    case MeleeHitOutcome.Parry:
                        canParry = false;
                        break;
                    default:
                        Log.outDebug(LogFilter.Unit, "Spell {0} SPELL_AURA_IGNORE_COMBAT_RESULT has unhandled state {1}", aurEff.GetId(), aurEff.GetMiscValue());
                        break;
                }
            }

            if (canDodge)
            {
                // Roll dodge
                int dodgeChance = (int)(GetUnitDodgeChance(attType, victim) * 100.0f);
                if (dodgeChance < 0)
                    dodgeChance = 0;

                if (roll < (tmp += dodgeChance))
                    return SpellMissInfo.Dodge;
            }

            if (canParry)
            {
                // Roll parry
                int parryChance = (int)(GetUnitParryChance(attType, victim) * 100.0f);
                if (parryChance < 0)
                    parryChance = 0;

                tmp += parryChance;
                if (roll < tmp)
                    return SpellMissInfo.Parry;
            }

            if (canBlock)
            {
                int blockChance = (int)(GetUnitBlockChance(attType, victim) * 100.0f);
                if (blockChance < 0)
                    blockChance = 0;
                tmp += blockChance;

                if (roll < tmp)
                    return SpellMissInfo.Block;
            }

            return SpellMissInfo.None;
        }

        // @todo need use unit spell resistances in calculations
        SpellMissInfo MagicSpellHitResult(Unit victim, SpellInfo spell)
        {
            // Can`t miss on dead target (on skinning for example)
            if (!victim.IsAlive() && !victim.IsTypeId(TypeId.Player))
                return SpellMissInfo.None;

            SpellSchoolMask schoolMask = spell.GetSchoolMask();
            // PvP - PvE spell misschances per leveldif > 2
            int lchance = victim.IsTypeId(TypeId.Player) ? 7 : 11;
            int thisLevel = (int)GetLevelForTarget(victim);
            if (IsTypeId(TypeId.Unit) && ToCreature().IsTrigger())
                thisLevel = (int)Math.Max(thisLevel, spell.SpellLevel);
            int leveldif = (int)(victim.GetLevelForTarget(this)) - thisLevel;
            int levelBasedHitDiff = leveldif;

            // Base hit chance from attacker and victim levels
            int modHitChance = 100;
            if (levelBasedHitDiff >= 0)
            {
                if (!victim.IsTypeId(TypeId.Player))
                {
                    modHitChance = 94 - 3 * Math.Min(levelBasedHitDiff, 3);
                    levelBasedHitDiff -= 3;
                }
                else
                {
                    modHitChance = 96 - Math.Min(levelBasedHitDiff, 2);
                    levelBasedHitDiff -= 2;
                }
                if (levelBasedHitDiff > 0)
                    modHitChance -= lchance * Math.Min(levelBasedHitDiff, 7);
            }
            else
                modHitChance = 97 - levelBasedHitDiff;

            // Spellmod from SPELLMOD_RESIST_MISS_CHANCE
            Player modOwner = GetSpellModOwner();
            if (modOwner != null)
                modOwner.ApplySpellMod(spell.Id, SpellModOp.ResistMissChance, ref modHitChance);

            // Spells with SPELL_ATTR3_IGNORE_HIT_RESULT will ignore target's avoidance effects
            if (!spell.HasAttribute(SpellAttr3.IgnoreHitResult))
            {
                // Chance hit from victim SPELL_AURA_MOD_ATTACKER_SPELL_HIT_CHANCE auras
                modHitChance += victim.GetTotalAuraModifierByMiscMask(AuraType.ModAttackerSpellHitChance, (int)schoolMask);
            }

            int HitChance = modHitChance * 100;
            // Increase hit chance from attacker SPELL_AURA_MOD_SPELL_HIT_CHANCE and attacker ratings
            HitChance += (int)(modHitChance * 100.0f);

            if (HitChance < 100)
                HitChance = 100;
            else if (HitChance > 10000)
                HitChance = 10000;

            int tmp = 10000 - HitChance;

            int rand = RandomHelper.IRand(0, 9999);
            if (rand < tmp)
                return SpellMissInfo.Miss;

            // Spells with SPELL_ATTR3_IGNORE_HIT_RESULT will additionally fully ignore
            // resist and deflect chances
            if (spell.HasAttribute(SpellAttr3.IgnoreHitResult))
                return SpellMissInfo.None;

            // Chance resist mechanic (select max value from every mechanic spell effect)
            int resist_chance = victim.GetMechanicResistChance(spell) * 100;
            tmp += resist_chance;

            // Roll chance
            if (rand < tmp)
                return SpellMissInfo.Resist;

            // cast by caster in front of victim
            if (!victim.HasUnitState(UnitState.Controlled) && (victim.HasInArc(MathFunctions.PI, this) || victim.HasAuraType(AuraType.IgnoreHitDirection)))
            {
                int deflect_chance = victim.GetTotalAuraModifier(AuraType.DeflectSpells) * 100;
                tmp += deflect_chance;
                if (rand < tmp)
                    return SpellMissInfo.Deflect;
            }

            return SpellMissInfo.None;
        }

        public void CastSpell(SpellCastTargets targets, SpellInfo spellInfo, Dictionary<SpellValueMod, int> values, TriggerCastFlags triggerFlags = TriggerCastFlags.None, Item castItem = null, AuraEffect triggeredByAura = null, ObjectGuid originalCaster = default(ObjectGuid))
        {
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Spells, "CastSpell: unknown spell by caster: {0}", GetGUID().ToString());
                return;
            }

            Spell spell = new Spell(this, spellInfo, triggerFlags, originalCaster);

            if (values != null)
                foreach (var pair in values)
                    spell.SetSpellValue(pair.Key, pair.Value);

            spell.m_CastItem = castItem;
            spell.prepare(targets, triggeredByAura);
        }
        public void CastSpell(Unit victim, uint spellId, bool triggered, Item castItem = null, AuraEffect triggeredByAura = null, ObjectGuid originalCaster = default(ObjectGuid))
        {
            CastSpell(victim, spellId, triggered ? TriggerCastFlags.FullMask : TriggerCastFlags.None, castItem, triggeredByAura, originalCaster);
        }
        public void CastSpell(Unit victim, uint spellId, TriggerCastFlags triggerFlags = TriggerCastFlags.None, Item castItem = null, AuraEffect triggeredByAura = null, ObjectGuid originalCaster = default(ObjectGuid))
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Spells, "CastSpell: unknown spell id {0} by caster: {1}", spellId, GetGUID().ToString());
                return;
            }

            CastSpell(victim, spellInfo, triggerFlags, castItem, triggeredByAura, originalCaster);
        }
        public void CastSpell(Unit victim, SpellInfo spellInfo, bool triggered, Item castItem = null, AuraEffect triggeredByAura = null, ObjectGuid originalCaster = default(ObjectGuid))
        {
            CastSpell(victim, spellInfo, triggered ? TriggerCastFlags.FullMask : TriggerCastFlags.None, castItem, triggeredByAura, originalCaster);
        }
        public void CastSpell(Unit victim, SpellInfo spellInfo, TriggerCastFlags triggerFlags = TriggerCastFlags.None, Item castItem = null, AuraEffect triggeredByAura = null, ObjectGuid originalCaster = default(ObjectGuid))
        {
            SpellCastTargets targets = new SpellCastTargets();
            targets.SetUnitTarget(victim);
            CastSpell(targets, spellInfo, null, triggerFlags, castItem, triggeredByAura, originalCaster);
        }
        public void CastSpell(float x, float y, float z, uint spellId, bool triggered, Item castItem = null, AuraEffect triggeredByAura = null, ObjectGuid originalCaster = default(ObjectGuid))
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Unit, "CastSpell: unknown spell id {0} by caster: {1}", spellId, GetGUID().ToString());
                return;
            }
            SpellCastTargets targets = new SpellCastTargets();
            targets.SetDst(x, y, z, GetOrientation());

            CastSpell(targets, spellInfo, null, triggered ? TriggerCastFlags.FullMask : TriggerCastFlags.None, castItem, triggeredByAura, originalCaster);
        }
        public void CastSpell(GameObject go, uint spellId, bool triggered, Item castItem = null, AuraEffect triggeredByAura = null, ObjectGuid originalCaster = default(ObjectGuid))
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Unit, "CastSpell: unknown spell id {0} by caster: {1}", spellId, GetGUID().ToString());
                return;
            }
            SpellCastTargets targets = new SpellCastTargets();
            targets.SetGOTarget(go);

            CastSpell(targets, spellInfo, null, triggered ? TriggerCastFlags.FullMask : TriggerCastFlags.None, castItem, triggeredByAura, originalCaster);
        }

        public void CastCustomSpell(Unit target, uint spellId, int bp0, int bp1, int bp2, bool triggered, Item castItem = null, AuraEffect triggeredByAura = null, ObjectGuid originalCaster = default(ObjectGuid))
        {
            Dictionary<SpellValueMod, int> values = new Dictionary<SpellValueMod, int>();
            if (bp0 != 0)
                values.Add(SpellValueMod.BasePoint0, bp0);
            if (bp1 != 0)
                values.Add(SpellValueMod.BasePoint1, bp1);
            if (bp2 != 0)
                values.Add(SpellValueMod.BasePoint2, bp2);
            CastCustomSpell(spellId, values, target, triggered ? TriggerCastFlags.FullMask : TriggerCastFlags.None, castItem, triggeredByAura, originalCaster);
        }
        public void CastCustomSpell(uint spellId, SpellValueMod mod, int value, Unit target, bool triggered, Item castItem = null, AuraEffect triggeredByAura = null, ObjectGuid originalCaster = default(ObjectGuid))
        {
            Dictionary<SpellValueMod, int> values = new Dictionary<SpellValueMod, int>();
            values.Add(mod, value);
            CastCustomSpell(spellId, values, target, triggered ? TriggerCastFlags.FullMask : TriggerCastFlags.None, castItem, triggeredByAura, originalCaster);
        }
        public void CastCustomSpell(uint spellId, SpellValueMod mod, int value, Unit target = null, TriggerCastFlags triggerFlags = TriggerCastFlags.None, Item castItem = null, AuraEffect triggeredByAura = null, ObjectGuid originalCaster = default(ObjectGuid))
        {
            Dictionary<SpellValueMod, int> values = new Dictionary<SpellValueMod, int>();
            values.Add(mod, value);
            CastCustomSpell(spellId, values, target, triggerFlags, castItem, triggeredByAura, originalCaster);
        }
        public void CastCustomSpell(uint spellId, Dictionary<SpellValueMod, int> values, Unit victim = null, TriggerCastFlags triggerFlags = TriggerCastFlags.None, Item castItem = null, AuraEffect triggeredByAura = null, ObjectGuid originalCaster = default(ObjectGuid))
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Unit, "CastSpell: unknown spell id {0} by caster: {1}", spellId, GetGUID().ToString());
                return;
            }
            SpellCastTargets targets = new SpellCastTargets();
            targets.SetUnitTarget(victim);

            CastSpell(targets, spellInfo, values, triggerFlags, castItem, triggeredByAura, originalCaster);
        }

        public void FinishSpell(CurrentSpellTypes spellType, bool ok = true)
        {
            Spell spell = GetCurrentSpell(spellType);
            if (spell == null)
                return;

            if (spellType == CurrentSpellTypes.Channeled)
                spell.SendChannelUpdate(0);

            spell.finish(ok);
        }

        uint GetCastingTimeForBonus(SpellInfo spellProto, DamageEffectType damagetype, uint CastingTime)
        {
            // Not apply this to creature casted spells with casttime == 0
            if (CastingTime == 0 && IsTypeId(TypeId.Unit) && !IsPet())
                return 3500;

            if (CastingTime > 7000) CastingTime = 7000;
            if (CastingTime < 1500) CastingTime = 1500;

            if (damagetype == DamageEffectType.DOT && !spellProto.IsChanneled())
                CastingTime = 3500;

            int overTime = 0;
            byte effects = 0;
            bool DirectDamage = false;
            bool AreaEffect = false;

            foreach (SpellEffectInfo effect in spellProto.GetEffectsForDifficulty(GetMap().GetDifficultyID()))
            {
                if (effect == null)
                    continue;

                switch (effect.Effect)
                {
                    case SpellEffectName.SchoolDamage:
                    case SpellEffectName.PowerDrain:
                    case SpellEffectName.HealthLeech:
                    case SpellEffectName.EnvironmentalDamage:
                    case SpellEffectName.PowerBurn:
                    case SpellEffectName.Heal:
                        DirectDamage = true;
                        break;
                    case SpellEffectName.ApplyAura:
                        switch (effect.ApplyAuraName)
                        {
                            case AuraType.PeriodicDamage:
                            case AuraType.PeriodicHeal:
                            case AuraType.PeriodicLeech:
                                if (spellProto.GetDuration() != 0)
                                    overTime = spellProto.GetDuration();
                                break;
                            default:
                                // -5% per additional effect
                                ++effects;
                                break;
                        }
                        break;
                    default:
                        break;
                }

                if (effect.IsTargetingArea())
                    AreaEffect = true;
            }

            // Combined Spells with Both Over Time and Direct Damage
            if (overTime > 0 && CastingTime > 0 && DirectDamage)
            {
                // mainly for DoTs which are 3500 here otherwise
                int OriginalCastTime = spellProto.CalcCastTime();
                if (OriginalCastTime > 7000) OriginalCastTime = 7000;
                if (OriginalCastTime < 1500) OriginalCastTime = 1500;
                // Portion to Over Time
                float PtOT = (overTime / 15000.0f) / ((overTime / 15000.0f) + (OriginalCastTime / 3500.0f));

                if (damagetype == DamageEffectType.DOT)
                    CastingTime = (uint)(CastingTime * PtOT);
                else if (PtOT < 1.0f)
                    CastingTime = (uint)(CastingTime * (1 - PtOT));
                else
                    CastingTime = 0;
            }

            // Area Effect Spells receive only half of bonus
            if (AreaEffect)
                CastingTime /= 2;

            // 50% for damage and healing spells for leech spells from damage bonus and 0% from healing
            foreach (SpellEffectInfo effect in spellProto.GetEffectsForDifficulty(GetMap().GetDifficultyID()))
            {
                if (effect != null && (effect.Effect == SpellEffectName.HealthLeech ||
                    (effect.Effect == SpellEffectName.ApplyAura && effect.ApplyAuraName == AuraType.PeriodicLeech)))
                {
                    CastingTime /= 2;
                    break;
                }
            }

            // -5% of total per any additional effect
            for (byte i = 0; i < effects; ++i)
                CastingTime *= (uint)0.95f;

            return CastingTime;
        }

        public virtual SpellInfo GetCastSpellInfo(SpellInfo spellInfo)
        {
            var swaps = GetAuraEffectsByType(AuraType.OverrideActionbarSpells);
            var swaps2 = GetAuraEffectsByType(AuraType.OverrideActionbarSpellsTriggered);
            if (!swaps2.Empty())
                swaps.AddRange(swaps2);

            foreach (AuraEffect auraEffect in swaps)
            {
                if (auraEffect.GetMiscValue() == spellInfo.Id || auraEffect.IsAffectingSpell(spellInfo))
                {
                    SpellInfo newInfo = Global.SpellMgr.GetSpellInfo((uint)auraEffect.GetAmount());
                    if (newInfo != null)
                        return newInfo;
                }
            }

            return spellInfo;
        }

        public uint GetCastSpellXSpellVisualId(SpellInfo spellInfo)
        {
            var visualOverrides = GetAuraEffectsByType(AuraType.OverrideSpellVisual);
            foreach (AuraEffect effect in visualOverrides)
            {
                if (effect.GetMiscValue() == spellInfo.Id)
                {
                    SpellInfo visualSpell = Global.SpellMgr.GetSpellInfo((uint)effect.GetMiscValueB());
                    if (visualSpell != null)
                    {
                        spellInfo = visualSpell;
                        break;
                    }
                }
            }

            return spellInfo.GetSpellXSpellVisualId(this);
        }

        public SpellHistory GetSpellHistory() { return _spellHistory; }

        public static ProcFlagsHit createProcHitMask(SpellNonMeleeDamage damageInfo, SpellMissInfo missCondition)
        {
            ProcFlagsHit hitMask = ProcFlagsHit.None;
            // Check victim state
            if (missCondition != SpellMissInfo.None)
            {
                switch (missCondition)
                {
                    case SpellMissInfo.Miss:
                        hitMask |= ProcFlagsHit.Miss;
                        break;
                    case SpellMissInfo.Dodge:
                        hitMask |= ProcFlagsHit.Dodge;
                        break;
                    case SpellMissInfo.Parry:
                        hitMask |= ProcFlagsHit.Parry;
                        break;
                    case SpellMissInfo.Block:
                        // spells can't be partially blocked (it's damage can though)
                        hitMask |= ProcFlagsHit.Block | ProcFlagsHit.FullBlock;
                        break;
                    case SpellMissInfo.Evade:
                        hitMask |= ProcFlagsHit.Evade;
                        break;
                    case SpellMissInfo.Immune:
                    case SpellMissInfo.Immune2:
                        hitMask |= ProcFlagsHit.Immune;
                        break;
                    case SpellMissInfo.Deflect:
                        hitMask |= ProcFlagsHit.Deflect;
                        break;
                    case SpellMissInfo.Absorb:
                        hitMask |= ProcFlagsHit.Absorb;
                        break;
                    case SpellMissInfo.Reflect:
                        hitMask |= ProcFlagsHit.Reflect;
                        break;
                    case SpellMissInfo.Resist:
                        hitMask |= ProcFlagsHit.FullResist;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                // On block
                if (damageInfo.blocked != 0)
                {
                    hitMask |= ProcFlagsHit.Block;
                    if (damageInfo.fullBlock)
                        hitMask |= ProcFlagsHit.FullBlock;
                }
                // On absorb
                if (damageInfo.absorb != 0)
                    hitMask |= ProcFlagsHit.Absorb;

                // Don't set hit/crit hitMask if damage is nullified
                bool damageNullified = damageInfo.HitInfo.HasAnyFlag(HitInfo.FullAbsorb | HitInfo.FullResist) || hitMask.HasAnyFlag(ProcFlagsHit.FullBlock);
                if (!damageNullified)
                {
                    // On crit
                    if (damageInfo.HitInfo.HasAnyFlag(HitInfo.CriticalHit))
                        hitMask |= ProcFlagsHit.Critical;
                    else
                        hitMask |= ProcFlagsHit.Normal;
                }
                else if (damageInfo.HitInfo.HasAnyFlag(HitInfo.FullResist))
                    hitMask |= ProcFlagsHit.FullResist;
            }

            return hitMask;
        }

        public void SetAuraStack(uint spellId, Unit target, uint stack)
        {
            Aura aura = target.GetAura(spellId, GetGUID());
            if (aura == null)
                aura = AddAura(spellId, target);
            if (aura != null && stack != 0)
                aura.SetStackAmount((byte)stack);
        }

        public Spell FindCurrentSpellBySpellId(uint spell_id)
        {
            foreach (var spell in m_currentSpells.Values)
            {
                if (spell == null)
                    continue;
                if (spell.m_spellInfo.Id == spell_id)
                    return spell;
            }
            return null;
        }

        public int GetCurrentSpellCastTime(uint spell_id)
        {
            Spell spell = FindCurrentSpellBySpellId(spell_id);
            if (spell != null)
                return spell.GetCastTime();
            return 0;
        }

        public bool IsMovementPreventedByCasting()
        {
            // can always move when not casting
            if (!HasUnitState(UnitState.Casting))
                return false;

            // channeled spells during channel stage (after the initial cast timer) allow movement with a specific spell attribute
            Spell spell = m_currentSpells.LookupByKey(CurrentSpellTypes.Channeled);
            if (spell)
                if (spell.getState() != SpellState.Finished && spell.IsChannelActive())
                    if (spell.GetSpellInfo().IsMoveAllowedChannel())
                        return false;

            // prohibit movement for all other spell casts
            return true;
        }

        bool HasBreakableByDamageAuraType(AuraType type, uint excludeAura)
        {
            var auras = GetAuraEffectsByType(type);
            foreach (var eff in auras)
                if ((excludeAura == 0 || excludeAura != eff.GetSpellInfo().Id) && //Avoid self interrupt of channeled Crowd Control spells like Seduction
                    eff.GetSpellInfo().HasAuraInterruptFlag(SpellAuraInterruptFlags.TakeDamage))
                    return true;
            return false;
        }

        public bool HasBreakableByDamageCrowdControlAura(Unit excludeCasterChannel = null)
        {
            uint excludeAura = 0;
            Spell currentChanneledSpell = excludeCasterChannel?.GetCurrentSpell(CurrentSpellTypes.Channeled);
            if (currentChanneledSpell != null)
                excludeAura = currentChanneledSpell.GetSpellInfo().Id; //Avoid self interrupt of channeled Crowd Control spells like Seduction

            return (HasBreakableByDamageAuraType(AuraType.ModConfuse, excludeAura)
                    || HasBreakableByDamageAuraType(AuraType.ModFear, excludeAura)
                    || HasBreakableByDamageAuraType(AuraType.ModStun, excludeAura)
                    || HasBreakableByDamageAuraType(AuraType.ModRoot, excludeAura)
                    || HasBreakableByDamageAuraType(AuraType.ModRoot2, excludeAura)
                    || HasBreakableByDamageAuraType(AuraType.Transform, excludeAura));
        }

        public uint GetDiseasesByCaster(ObjectGuid casterGUID, bool remove = false)
        {
            AuraType[] diseaseAuraTypes =
            {
                AuraType.PeriodicDamage, // Frost Fever and Blood Plague
                AuraType.Linked,          // Crypt Fever and Ebon Plague
                AuraType.None
            };

            uint diseases = 0;
            foreach (var aura in diseaseAuraTypes)
            {
                if (aura == AuraType.None)
                    break;

                for (var i = 0; i < m_modAuras[aura].Count;)
                {
                    var eff = m_modAuras[aura][i];
                    // Get auras with disease dispel type by caster
                    if (eff.GetSpellInfo().Dispel == DispelType.Disease
                        && eff.GetCasterGUID() == casterGUID)
                    {
                        ++diseases;

                        if (remove)
                        {
                            RemoveAura(eff.GetId(), eff.GetCasterGUID());
                            i = 0;
                            continue;
                        }
                    }
                    i++;
                }
            }
            return diseases;
        }

        uint GetDoTsByCaster(ObjectGuid casterGUID)
        {
            AuraType[] diseaseAuraTypes =
            {
                AuraType.PeriodicDamage,
                AuraType.PeriodicDamagePercent,
                AuraType.None
            };

            uint dots = 0;
            foreach (var aura in diseaseAuraTypes)
            {
                if (aura == AuraType.None)
                    break;

                var auras = GetAuraEffectsByType(aura);
                foreach (var eff in auras)
                {
                    // Get auras by caster
                    if (eff.GetCasterGUID() == casterGUID)
                        ++dots;
                }
            }
            return dots;
        }

        public void SendEnergizeSpellLog(Unit victim, uint spellId, int amount, int overEnergize, PowerType powerType)
        {
            SpellEnergizeLog data = new SpellEnergizeLog();
            data.CasterGUID = GetGUID();
            data.TargetGUID = victim.GetGUID();
            data.SpellID = spellId;
            data.Type = powerType;
            data.Amount = amount;
            data.OverEnergize = overEnergize;
            data.LogData.Initialize(victim);

            SendCombatLogMessage(data);
        }

        public void EnergizeBySpell(Unit victim, uint spellId, int damage, PowerType powerType)
        {
            int gain = victim.ModifyPower(powerType, damage);
            int overEnergize = damage - gain;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
            victim.getHostileRefManager().threatAssist(this, damage * 0.5f, spellInfo);

            SendEnergizeSpellLog(victim, spellId, damage, overEnergize, powerType);
        }

        public void ApplySpellImmune(uint spellId, SpellImmunity op, SpellSchoolMask type, bool apply)
        {
            ApplySpellImmune(spellId, op, (uint)type, apply);
        }

        public void ApplySpellImmune(uint spellId, SpellImmunity op, AuraType type, bool apply)
        {
            ApplySpellImmune(spellId, op, (uint)type, apply);
        }

        public void ApplySpellImmune(uint spellId, SpellImmunity op, SpellEffectName type, bool apply)
        {
            ApplySpellImmune(spellId, op, (uint)type, apply);
        }

        public void ApplySpellImmune(uint spellId, SpellImmunity op, uint type, bool apply)
        {
            if (apply)
            {
                m_spellImmune[(int)op].Add(type, spellId);
            }
            else
            {
                var bounds = m_spellImmune[(int)op].LookupByKey(type);
                foreach (var spell in bounds)
                {
                    if (spell == spellId)
                    {
                        m_spellImmune[(int)op].Remove(type, spell);
                        break;
                    }
                }
            }
        }
        public virtual bool IsImmunedToSpell(SpellInfo spellInfo, Unit caster)
        {
            if (spellInfo == null)
                return false;

            // Single spell immunity.
            var idList = m_spellImmune[(int)SpellImmunity.Id];
            if (idList.ContainsKey(spellInfo.Id))
                return true;

            if (spellInfo.HasAttribute(SpellAttr0.UnaffectedByInvulnerability))
                return false;

            uint dispel = (uint)spellInfo.Dispel;
            if (dispel != 0)
            {
                var dispelList = m_spellImmune[(int)SpellImmunity.Dispel];
                if (dispelList.ContainsKey(dispel))
                    return true;
            }

            // Spells that don't have effectMechanics.
            uint mechanic = (uint)spellInfo.Mechanic;
            if (mechanic != 0)
            {
                var mechanicList = m_spellImmune[(int)SpellImmunity.Mechanic];
                    if (mechanicList.ContainsKey(mechanic))
                        return true;
            }

            bool immuneToAllEffects = true;
            foreach (SpellEffectInfo effect in spellInfo.GetEffectsForDifficulty(GetMap().GetDifficultyID()))
            {
                // State/effect immunities applied by aura expect full spell immunity
                // Ignore effects with mechanic, they are supposed to be checked separately
                if (effect == null || !effect.IsEffect())
                    continue;

                if (!IsImmunedToSpellEffect(spellInfo, effect.EffectIndex, caster))
                {
                    immuneToAllEffects = false;
                    break;
                }
            }

            if (immuneToAllEffects) //Return immune only if the target is immune to all spell effects.
                return true;

            var schoolList = m_spellImmune[(int)SpellImmunity.School];
            foreach (var pair in schoolList)
            {
                if ((pair.Key & (uint)spellInfo.GetSchoolMask()) == 0)
                    continue;

                SpellInfo immuneSpellInfo = Global.SpellMgr.GetSpellInfo(pair.Value);
                if (!(immuneSpellInfo != null && immuneSpellInfo.IsPositive() && spellInfo.IsPositive() && caster && IsFriendlyTo(caster)))
                    if (!spellInfo.CanPierceImmuneAura(immuneSpellInfo))
                        return true;
            }

            return false;
        }
        public uint GetSchoolImmunityMask()
        {
            uint mask = 0;
            var mechanicList = m_spellImmune[(int)SpellImmunity.School];
            foreach (var pair in mechanicList)
                mask |= pair.Key;

            return mask;
        }
        public uint GetMechanicImmunityMask()
        {
            uint mask = 0;
            var mechanicList = m_spellImmune[(int)SpellImmunity.Mechanic];
            foreach (var pair in mechanicList)
                mask |= (1u << (int)pair.Value);

            return mask;
        }
        public virtual bool IsImmunedToSpellEffect(SpellInfo spellInfo, uint index, Unit caster)
        {
            if (spellInfo == null)
                return false;

            SpellEffectInfo effect = spellInfo.GetEffect(GetMap().GetDifficultyID(), index);
            if (effect == null || !effect.IsEffect())
                return false;

            // If m_immuneToEffect type contain this effect type, IMMUNE effect.
            uint eff = (uint)effect.Effect;
            var effectList = m_spellImmune[(int)SpellImmunity.Effect];
            if (effectList.ContainsKey(eff))
                return true;

            uint mechanic = (uint)effect.Mechanic;
            if (mechanic != 0)
            {
                var mechanicList = m_spellImmune[(int)SpellImmunity.Mechanic];
                if (mechanicList.ContainsKey(mechanic))
                    return true;
            }

            if (!spellInfo.HasAttribute(SpellAttr3.IgnoreHitResult))
            {
                uint aura = (uint)effect.ApplyAuraName;
                if (aura != 0)
                {
                    var list = m_spellImmune[(int)SpellImmunity.State];
                    if (list.ContainsKey(aura))
                        return true;

                    if (!spellInfo.HasAttribute(SpellAttr2.UnaffectedByAuraSchoolImmune))
                    {
                        // Check for immune to application of harmful magical effects
                        var immuneAuraApply = GetAuraEffectsByType(AuraType.ModImmuneAuraApplySchool);
                        foreach (var auraEffect in immuneAuraApply)
                            if (Convert.ToBoolean(auraEffect.GetMiscValue() & (int)spellInfo.GetSchoolMask()) &&  // Check school
                                ((caster && !IsFriendlyTo(caster)) || !spellInfo.IsPositiveEffect(index)))                       // Harmful
                                return true;
                    }
                }
            }

            return false;
        }
        public bool IsImmunedToDamage(SpellSchoolMask schoolMask)
        {
            // If m_immuneToSchool type contain this school type, IMMUNE damage.
            var schoolList = m_spellImmune[(int)SpellImmunity.School];
            foreach (var immune in schoolList)
                if (Convert.ToBoolean(immune.Key & (uint)schoolMask))
                    return true;

            // If m_immuneToDamage type contain magic, IMMUNE damage.
            var damageList = m_spellImmune[(int)SpellImmunity.Damage];
            foreach (var immune in damageList)
                if (Convert.ToBoolean(immune.Key & (uint)schoolMask))
                    return true;

            return false;
        }
        public bool IsImmunedToDamage(SpellInfo spellInfo)
        {
            if (spellInfo == null)
                return false;

            // for example 40175
            if (spellInfo.HasAttribute(SpellAttr0.UnaffectedByInvulnerability) && spellInfo.HasAttribute(SpellAttr3.IgnoreHitResult))
                return false;

            if (spellInfo.HasAttribute(SpellAttr1.UnaffectedBySchoolImmune) || spellInfo.HasAttribute(SpellAttr2.UnaffectedByAuraSchoolImmune))
                return false;

            uint schoolMask = (uint)spellInfo.GetSchoolMask();
            // If m_immuneToSchool type contain this school type, IMMUNE damage.
            var schoolList = m_spellImmune[(int)SpellImmunity.School];
            foreach (var pair in schoolList)
                if (Convert.ToBoolean(pair.Key & schoolMask) && !spellInfo.CanPierceImmuneAura(Global.SpellMgr.GetSpellInfo(pair.Value)))
                    return true;

            // If m_immuneToDamage type contain magic, IMMUNE damage.
            var damageList = m_spellImmune[(int)SpellImmunity.Damage];
            foreach (var immune in damageList)
                if (Convert.ToBoolean(immune.Key & schoolMask))
                    return true;

            return false;
        }

        public void ProcSkillsAndAuras(Unit actionTarget, ProcFlags typeMaskActor, ProcFlags typeMaskActionTarget, ProcFlagsSpellType spellTypeMask, ProcFlagsSpellPhase spellPhaseMask, ProcFlagsHit hitMask, Spell spell, DamageInfo damageInfo, HealInfo healInfo)
        {
            WeaponAttackType attType = damageInfo != null ? damageInfo.GetAttackType() : WeaponAttackType.BaseAttack;
            if (typeMaskActor != 0)
                ProcSkillsAndReactives(false, actionTarget, typeMaskActor, hitMask, attType);

            if (typeMaskActionTarget != 0 && actionTarget)
                actionTarget.ProcSkillsAndReactives(true, this, typeMaskActionTarget, hitMask, attType);

            TriggerAurasProcOnEvent(null, null, actionTarget, typeMaskActor, typeMaskActionTarget, spellTypeMask, spellPhaseMask, hitMask, spell, damageInfo, healInfo);
        }

        void ProcSkillsAndReactives(bool isVictim, Unit procTarget, ProcFlags typeMask, ProcFlagsHit hitMask, WeaponAttackType attType)
        {
            // Player is loaded now - do not allow passive spell casts to proc
            if (IsPlayer() && ToPlayer().GetSession().PlayerLoading())
                return;

            // For melee/ranged based attack need update skills and set some Aura states if victim present
            if (typeMask.HasAnyFlag(ProcFlags.MeleeBasedTriggerMask) && procTarget)
            {
                // If exist crit/parry/dodge/block need update aura state (for victim and attacker)
                if (hitMask.HasAnyFlag(ProcFlagsHit.Critical | ProcFlagsHit.Parry | ProcFlagsHit.Dodge | ProcFlagsHit.Block))
                {
                    // for victim
                    if (isVictim)
                    {
                        // if victim and dodge attack
                        if (hitMask.HasAnyFlag(ProcFlagsHit.Dodge))
                        {
                            // Update AURA_STATE on dodge
                            if (GetClass() != Class.Rogue) // skip Rogue Riposte
                            {
                                ModifyAuraState(AuraStateType.Defense, true);
                                StartReactiveTimer(ReactiveType.Defense);
                            }
                        }
                        // if victim and parry attack
                        if (hitMask.HasAnyFlag(ProcFlagsHit.Parry))
                        {
                            // For Hunters only Counterattack (skip Mongoose bite)
                            if (GetClass() == Class.Hunter)
                            {
                                ModifyAuraState(AuraStateType.HunterParry, true);
                                StartReactiveTimer(ReactiveType.HunterParry);
                            }
                            else
                            {
                                ModifyAuraState(AuraStateType.Defense, true);
                                StartReactiveTimer(ReactiveType.Defense);
                            }
                        }
                        // if and victim block attack
                        if (hitMask.HasAnyFlag(ProcFlagsHit.Block))
                        {
                            ModifyAuraState(AuraStateType.Defense, true);
                            StartReactiveTimer(ReactiveType.Defense);
                        }
                    }
                    else // For attacker
                    {
                        // Overpower on victim dodge
                        if (hitMask.HasAnyFlag(ProcFlagsHit.Dodge) && IsPlayer() && GetClass() == Class.Warrior)
                        {
                            ToPlayer().AddComboPoints(1);
                            StartReactiveTimer(ReactiveType.OverPower);
                        }
                    }
                }
            }
        }

        void GetProcAurasTriggeredOnEvent(List<Tuple<uint, AuraApplication>> aurasTriggeringProc, List<AuraApplication> procAuras, ProcEventInfo eventInfo)
        {
            DateTime now = DateTime.Now;

            // use provided list of auras which can proc
            if (procAuras != null)
            {
                foreach (AuraApplication aurApp in procAuras)
                {
                    Cypher.Assert(aurApp.GetTarget() == this);
                    uint procEffectMask = aurApp.GetBase().IsProcTriggeredOnEvent(aurApp, eventInfo, now);
                    if (procEffectMask != 0)
                    {
                        aurApp.GetBase().PrepareProcToTrigger(aurApp, eventInfo, now);
                        aurasTriggeringProc.Add(Tuple.Create(procEffectMask, aurApp));
                    }
                }
            }
            // or generate one on our own
            else
            {
                foreach (var pair in GetAppliedAuras())
                {
                    uint procEffectMask = pair.Value.GetBase().IsProcTriggeredOnEvent(pair.Value, eventInfo, now);
                    if (procEffectMask != 0)
                    {
                        pair.Value.GetBase().PrepareProcToTrigger(pair.Value, eventInfo, now);
                        aurasTriggeringProc.Add(Tuple.Create(procEffectMask, pair.Value));
                    }
                }
            }
        }

        void TriggerAurasProcOnEvent(CalcDamageInfo damageInfo)
        {
            DamageInfo dmgInfo = new DamageInfo(damageInfo);
            TriggerAurasProcOnEvent(null, null, damageInfo.target, damageInfo.procAttacker, damageInfo.procVictim, ProcFlagsSpellType.None, ProcFlagsSpellPhase.None, dmgInfo.GetHitMask(), null, dmgInfo, null);
        }

        void TriggerAurasProcOnEvent(List<AuraApplication> myProcAuras, List<AuraApplication> targetProcAuras, Unit actionTarget, ProcFlags typeMaskActor, ProcFlags typeMaskActionTarget, ProcFlagsSpellType spellTypeMask, ProcFlagsSpellPhase spellPhaseMask, ProcFlagsHit hitMask, Spell spell, DamageInfo damageInfo, HealInfo healInfo)
        {
            // prepare data for self trigger
            ProcEventInfo myProcEventInfo = new ProcEventInfo(this, actionTarget, actionTarget, typeMaskActor, spellTypeMask, spellPhaseMask, hitMask, spell, damageInfo, healInfo);
            List<Tuple<uint, AuraApplication>> myAurasTriggeringProc = new List<Tuple<uint, AuraApplication>>();
            if (typeMaskActor != 0)
            {
                GetProcAurasTriggeredOnEvent(myAurasTriggeringProc, myProcAuras, myProcEventInfo);

                // needed for example for Cobra Strikes, pet does the attack, but aura is on owner
                Player modOwner = GetSpellModOwner();
                if (modOwner)
                {
                    if (modOwner != this && spell)
                    {
                        List<AuraApplication> modAuras = new List<AuraApplication>();
                        foreach (var itr in modOwner.GetAppliedAuras())
                        {
                            if (spell.m_appliedMods.Contains(itr.Value.GetBase()))
                                modAuras.Add(itr.Value);
                        }
                        modOwner.GetProcAurasTriggeredOnEvent(myAurasTriggeringProc, modAuras, myProcEventInfo);
                    }
                }
            }

            // prepare data for target trigger
            ProcEventInfo targetProcEventInfo = new ProcEventInfo(this, actionTarget, this, typeMaskActionTarget, spellTypeMask, spellPhaseMask, hitMask, spell, damageInfo, healInfo);
            List<Tuple<uint, AuraApplication>> targetAurasTriggeringProc = new List<Tuple<uint, AuraApplication>>();
            if (typeMaskActionTarget != 0 && actionTarget)
                actionTarget.GetProcAurasTriggeredOnEvent(targetAurasTriggeringProc, targetProcAuras, targetProcEventInfo);

            TriggerAurasProcOnEvent(myProcEventInfo, myAurasTriggeringProc);

            if (typeMaskActionTarget != 0 && actionTarget)
                actionTarget.TriggerAurasProcOnEvent(targetProcEventInfo, targetAurasTriggeringProc);
        }

        void TriggerAurasProcOnEvent(ProcEventInfo eventInfo, List<Tuple<uint, AuraApplication>> aurasTriggeringProc)
        {
            Spell triggeringSpell = eventInfo.GetProcSpell();
            bool disableProcs = triggeringSpell && triggeringSpell.IsProcDisabled();
            if (disableProcs)
                SetCantProc(true);

            foreach (var aurAppProc in aurasTriggeringProc)
            {
                AuraApplication aurApp = aurAppProc.Item2;
                uint procEffectMask = aurAppProc.Item1;

                if (aurApp.GetRemoveMode() != 0)
                    continue;

                SpellInfo spellInfo = aurApp.GetBase().GetSpellInfo();
                if (spellInfo.HasAttribute(SpellAttr3.DisableProc))
                    SetCantProc(true);

                aurApp.GetBase().TriggerProcOnEvent(procEffectMask, aurApp, eventInfo);

                if (spellInfo.HasAttribute(SpellAttr3.DisableProc))
                    SetCantProc(false);
            }

            if (disableProcs)
                SetCantProc(false);
        }

        void SetCantProc(bool apply)
        {
            if (apply)
                ++m_procDeep;
            else
            {
                Cypher.Assert(m_procDeep != 0);
                --m_procDeep;
            }
        }

        public void CastStop(uint except_spellid = 0)
        {
            for (var i = CurrentSpellTypes.Generic; i < CurrentSpellTypes.Max; i++)
                if (GetCurrentSpell(i) != null && GetCurrentSpell(i).m_spellInfo.Id != except_spellid)
                    InterruptSpell(i, false);
        }
        public void ModSpellCastTime(SpellInfo spellInfo, ref int castTime, Spell spell = null)
        {
            if (spellInfo == null || castTime < 0)
                return;

            // called from caster
            Player modOwner = GetSpellModOwner();
            if (modOwner != null)
                modOwner.ApplySpellMod(spellInfo.Id, SpellModOp.CastingTime, ref castTime, spell);

            if (!(spellInfo.HasAttribute(SpellAttr0.Ability | SpellAttr0.Tradespell) || spellInfo.HasAttribute(SpellAttr3.NoDoneBonus))
                && (IsTypeId(TypeId.Player) && spellInfo.SpellFamilyName != 0) || IsTypeId(TypeId.Unit))
                castTime = (int)(castTime * GetFloatValue(UnitFields.ModCastSpeed));
            else if (spellInfo.HasAttribute(SpellAttr0.ReqAmmo) && !spellInfo.HasAttribute(SpellAttr2.AutorepeatFlag))
                castTime = (int)(castTime * m_modAttackSpeedPct[(int)WeaponAttackType.RangedAttack]);
            else if (Global.SpellMgr.IsPartOfSkillLine(SkillType.Cooking, spellInfo.Id) && HasAura(67556)) // cooking with Chef Hat.
                castTime = 500;
        }
        public void ModSpellDurationTime(SpellInfo spellInfo, ref int duration, Spell spell = null)
        {
            if (spellInfo == null || duration < 0)
                return;

            if (spellInfo.IsChanneled() && !spellInfo.HasAttribute(SpellAttr5.HasteAffectDuration))
                return;

            // called from caster
            Player modOwner = GetSpellModOwner();
            if (modOwner)
                modOwner.ApplySpellMod(spellInfo.Id, SpellModOp.CastingTime, ref duration, spell);

            if (!(spellInfo.HasAttribute(SpellAttr0.Ability) || spellInfo.HasAttribute(SpellAttr0.Tradespell) || spellInfo.HasAttribute(SpellAttr3.NoDoneBonus)) &&
                (IsTypeId(TypeId.Player) && spellInfo.SpellFamilyName != 0) || IsTypeId(TypeId.Unit))
                duration = (int)(duration * GetFloatValue(UnitFields.ModCastSpeed));
            else if (spellInfo.HasAttribute(SpellAttr0.ReqAmmo) && !spellInfo.HasAttribute(SpellAttr2.AutorepeatFlag))
                duration = (int)(duration * m_modAttackSpeedPct[(int)WeaponAttackType.RangedAttack]);
        }
        public float ApplyEffectModifiers(SpellInfo spellProto, uint effect_index, float value)
        {
            Player modOwner = GetSpellModOwner();
            if (modOwner != null)
            {
                modOwner.ApplySpellMod(spellProto.Id, SpellModOp.AllEffects, ref value);
                switch (effect_index)
                {
                    case 0:
                        modOwner.ApplySpellMod(spellProto.Id, SpellModOp.Effect1, ref value);
                        break;
                    case 1:
                        modOwner.ApplySpellMod(spellProto.Id, SpellModOp.Effect2, ref value);
                        break;
                    case 2:
                        modOwner.ApplySpellMod(spellProto.Id, SpellModOp.Effect3, ref value);
                        break;
                    case 3:
                        modOwner.ApplySpellMod(spellProto.Id, SpellModOp.Effect4, ref value);
                        break;
                    case 4:
                        modOwner.ApplySpellMod(spellProto.Id, SpellModOp.Effect5, ref value);
                        break;
                }
            }
            return value;
        }

        public ushort GetMaxSkillValueForLevel(Unit target = null)
        {
            return (ushort)(target != null ? GetLevelForTarget(target) : getLevel() * 5);
        }
        public Player GetSpellModOwner()
        {
            if (IsTypeId(TypeId.Player))
                return ToPlayer();
            if (IsPet() || IsTotem())
            {
                Unit owner = GetOwner();
                if (owner != null && owner.IsTypeId(TypeId.Player))
                    return owner.ToPlayer();
            }
            return null;
        }

        public Spell GetCurrentSpell(CurrentSpellTypes spellType)
        {
            return m_currentSpells.LookupByKey(spellType);
        }
        public void SetCurrentCastSpell(Spell pSpell)
        {
            Cypher.Assert(pSpell != null);                                         // NULL may be never passed here, use InterruptSpell or InterruptNonMeleeSpells

            CurrentSpellTypes CSpellType = pSpell.GetCurrentContainer();

            if (pSpell == GetCurrentSpell(CSpellType))             // avoid breaking self
                return;

            // break same type spell if it is not delayed
            InterruptSpell(CSpellType, false);

            // special breakage effects:
            switch (CSpellType)
            {
                case CurrentSpellTypes.Generic:
                    {
                        // generic spells always break channeled not delayed spells
                        InterruptSpell(CurrentSpellTypes.Channeled, false);

                        // autorepeat breaking
                        if (GetCurrentSpell(CurrentSpellTypes.AutoRepeat) != null)
                        {
                            // break autorepeat if not Auto Shot
                            if (m_currentSpells[CurrentSpellTypes.AutoRepeat].m_spellInfo.Id != 75)
                                InterruptSpell(CurrentSpellTypes.AutoRepeat);
                            m_AutoRepeatFirstCast = true;
                        }
                        if (pSpell.m_spellInfo.CalcCastTime(getLevel()) > 0)
                            AddUnitState(UnitState.Casting);

                        break;
                    }
                case CurrentSpellTypes.Channeled:
                    {
                        // channel spells always break generic non-delayed and any channeled spells
                        InterruptSpell(CurrentSpellTypes.Generic, false);
                        InterruptSpell(CurrentSpellTypes.Channeled);

                        // it also does break autorepeat if not Auto Shot
                        if (GetCurrentSpell(CurrentSpellTypes.AutoRepeat) != null &&
                            m_currentSpells[CurrentSpellTypes.AutoRepeat].m_spellInfo.Id != 75)
                            InterruptSpell(CurrentSpellTypes.AutoRepeat);
                        AddUnitState(UnitState.Casting);

                        break;
                    }
                case CurrentSpellTypes.AutoRepeat:
                    {
                        // only Auto Shoot does not break anything
                        if (pSpell.m_spellInfo.Id != 75)
                        {
                            // generic autorepeats break generic non-delayed and channeled non-delayed spells
                            InterruptSpell(CurrentSpellTypes.Generic, false);
                            InterruptSpell(CurrentSpellTypes.Channeled, false);
                        }
                        // special action: set first cast flag
                        m_AutoRepeatFirstCast = true;

                        break;
                    }
                default:
                    break; // other spell types don't break anything now
            }

            // current spell (if it is still here) may be safely deleted now
            if (GetCurrentSpell(CSpellType) != null)
                m_currentSpells[CSpellType].SetReferencedFromCurrent(false);

            // set new current spell
            m_currentSpells[CSpellType] = pSpell;
            pSpell.SetReferencedFromCurrent(true);

            pSpell.m_selfContainer = m_currentSpells[pSpell.GetCurrentContainer()];
        }

        public bool IsNonMeleeSpellCast(bool withDelayed, bool skipChanneled = false, bool skipAutorepeat = false, bool isAutoshoot = false, bool skipInstant = true)
        {
            // We don't do loop here to explicitly show that melee spell is excluded.
            // Maybe later some special spells will be excluded too.

            // generic spells are cast when they are not finished and not delayed
            var currentSpell = GetCurrentSpell(CurrentSpellTypes.Generic);
            if (currentSpell &&
                    (currentSpell.getState() != SpellState.Finished) &&
                    (withDelayed || currentSpell.getState() != SpellState.Delayed))
            {
                if (!skipInstant || currentSpell.GetCastTime() != 0)
                {
                    if (!isAutoshoot || !currentSpell.m_spellInfo.HasAttribute(SpellAttr2.NotResetAutoActions))
                        return true;
                }
            }
            currentSpell = GetCurrentSpell(CurrentSpellTypes.Channeled);
            // channeled spells may be delayed, but they are still considered cast
            if (!skipChanneled && currentSpell &&
                (currentSpell.getState() != SpellState.Finished))
            {
                if (!isAutoshoot || !currentSpell.m_spellInfo.HasAttribute(SpellAttr2.NotResetAutoActions))
                    return true;
            }
            currentSpell = GetCurrentSpell(CurrentSpellTypes.AutoRepeat);
            // autorepeat spells may be finished or delayed, but they are still considered cast
            if (!skipAutorepeat && currentSpell)
                return true;

            return false;
        }

        public uint SpellCriticalDamageBonus(SpellInfo spellProto, uint damage, Unit victim = null)
        {
            // Calculate critical bonus
            int crit_bonus = (int)damage;
            float crit_mod = 0.0f;

            switch (spellProto.DmgClass)
            {
                case SpellDmgClass.Melee:                      // for melee based spells is 100%
                case SpellDmgClass.Ranged:
                    // @todo write here full calculation for melee/ranged spells
                    crit_bonus += (int)damage;
                    break;
                default:
                    crit_bonus += (int)damage / 2;                       // for spells is 50%
                    break;
            }

            crit_mod += (GetTotalAuraMultiplierByMiscMask(AuraType.ModCritDamageBonus, (uint)spellProto.GetSchoolMask()) - 1.0f) * 100;

            if (crit_bonus != 0)
                MathFunctions.AddPct(ref crit_bonus, (int)crit_mod);

            crit_bonus -= (int)damage;

            if (damage > crit_bonus)
            {
                // adds additional damage to critBonus (from talents)
                Player modOwner = GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(spellProto.Id, SpellModOp.CritDamageBonus, ref crit_bonus);
            }

            crit_bonus += (int)damage;

            return (uint)crit_bonus;
        }

        bool isSpellBlocked(Unit victim, SpellInfo spellProto, WeaponAttackType attackType = WeaponAttackType.BaseAttack)
        {
            // These spells can't be blocked
            if (spellProto != null && (spellProto.HasAttribute(SpellAttr0.ImpossibleDodgeParryBlock) || spellProto.HasAttribute(SpellAttr3.IgnoreHitResult)))
                return false;

            // Can't block when casting/controlled
            if (victim.IsNonMeleeSpellCast(false) || victim.HasUnitState(UnitState.Controlled))
                return false;

            if (victim.HasAuraType(AuraType.IgnoreHitDirection) || victim.HasInArc(MathFunctions.PI, this))
            {
                float blockChance = GetUnitBlockChance(attackType, victim);
                if (blockChance != 0 && RandomHelper.randChance(blockChance))
                    return true;
            }
            return false;
        }

        public void _DeleteRemovedAuras()
        {
            while (!m_removedAuras.Empty())
            {
                m_removedAuras.Remove(m_removedAuras.First());
            }
        }

        public uint getTransForm() { return m_transform; }

        public bool HasStealthAura() { return HasAuraType(AuraType.ModStealth); }
        public bool HasInvisibilityAura() { return HasAuraType(AuraType.ModInvisibility); }
        public bool isFeared() { return HasAuraType(AuraType.ModFear); }
        public bool isFrozen() { return HasAuraState(AuraStateType.Frozen); }
        public bool IsPolymorphed()
        {
            uint transformId = getTransForm();
            if (transformId == 0)
                return false;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(transformId);
            if (spellInfo == null)
                return false;

            return spellInfo.GetSpellSpecific() == SpellSpecificType.MagePolymorph;
        }

        public void DealHeal(HealInfo healInfo)
        {
            int gain = 0;
            Unit victim = healInfo.GetTarget();
            uint addhealth = healInfo.GetHeal();

            if (victim.IsAIEnabled)
                victim.GetAI().HealReceived(this, addhealth);

            if (IsAIEnabled)
                GetAI().HealDone(victim, addhealth);

            if (addhealth != 0)
                gain = (int)victim.ModifyHealth(addhealth);

            // Hook for OnHeal Event
            uint tempGain = (uint)gain;
            Global.ScriptMgr.OnHeal(this, victim, ref tempGain);
            gain = (int)tempGain;

            Unit unit = this;

            if (IsTypeId(TypeId.Unit) && IsTotem())
                unit = GetOwner();

            Player player = unit.ToPlayer();
            if (player != null)
            {
                Battleground bg = player.GetBattleground();
                if (bg)
                    bg.UpdatePlayerScore(player, ScoreType.HealingDone, (uint)gain);

                // use the actual gain, as the overheal shall not be counted, skip gain 0 (it ignored anyway in to criteria)
                if (gain != 0)
                    player.UpdateCriteria(CriteriaTypes.HealingDone, (uint)gain, 0, 0, victim);

                player.UpdateCriteria(CriteriaTypes.HighestHealCasted, addhealth);
            }

            if ((player = victim.ToPlayer()) != null)
            {
                player.UpdateCriteria(CriteriaTypes.TotalHealingReceived, (uint)gain);
                player.UpdateCriteria(CriteriaTypes.HighestHealingReceived, addhealth);
            }

            if (gain != 0)
                healInfo.SetEffectiveHeal(gain > 0 ? (uint)gain : 0u);
        }

        void SendHealSpellLog(HealInfo healInfo, bool critical = false)
        {
            SpellHealLog spellHealLog = new SpellHealLog();

            spellHealLog.TargetGUID = healInfo.GetTarget().GetGUID();
            spellHealLog.CasterGUID = healInfo.GetHealer().GetGUID();
            spellHealLog.SpellID = healInfo.GetSpellInfo().Id;
            spellHealLog.Health = healInfo.GetHeal();
            spellHealLog.OriginalHeal = (int)healInfo.GetOriginalHeal();
            spellHealLog.OverHeal = healInfo.GetHeal() - healInfo.GetEffectiveHeal();
            spellHealLog.Absorbed = healInfo.GetAbsorb();
            spellHealLog.Crit = critical;

            spellHealLog.LogData.Initialize(healInfo.GetTarget());
            SendCombatLogMessage(spellHealLog);
        }

        public uint HealBySpell(HealInfo healInfo, bool critical = false)
        {
            // calculate heal absorb and reduce healing
            CalcHealAbsorb(healInfo);

            DealHeal(healInfo);
            SendHealSpellLog(healInfo, critical);
            return healInfo.GetEffectiveHeal();
        }

        public void ApplyCastTimePercentMod(float val, bool apply)
        {
            if (val > 0)
            {
                ApplyPercentModFloatValue(UnitFields.ModCastSpeed, val, !apply);
                ApplyPercentModFloatValue(UnitFields.ModCastHaste, val, !apply);
                ApplyPercentModFloatValue(UnitFields.ModHasteRegen, val, !apply);
            }
            else
            {
                ApplyPercentModFloatValue(UnitFields.ModCastSpeed, -val, apply);
                ApplyPercentModFloatValue(UnitFields.ModCastHaste, -val, apply);
                ApplyPercentModFloatValue(UnitFields.ModHasteRegen, -val, apply);
            }
        }

        public void RemoveAllGroupBuffsFromCaster(ObjectGuid casterGUID)
        {
            foreach (var pair in GetOwnedAuras())
            {
                Aura aura = pair.Value;
                if (aura.GetCasterGUID() == casterGUID && aura.GetSpellInfo().IsGroupBuff())
                    RemoveOwnedAura(pair);
            }
        }

        public void DelayOwnedAuras(uint spellId, ObjectGuid caster, int delaytime)
        {
            var range = m_ownedAuras.LookupByKey(spellId);
            foreach (var aura in range)
            {
                if (caster.IsEmpty() || aura.GetCasterGUID() == caster)
                {
                    if (aura.GetDuration() < delaytime)
                        aura.SetDuration(0);
                    else
                        aura.SetDuration(aura.GetDuration() - delaytime);

                    // update for out of range group members (on 1 slot use)
                    aura.SetNeedClientUpdateForTargets();
                    Log.outDebug(LogFilter.Spells, "Aura {0} partially interrupted on {1}, new duration: {2} ms", aura.GetId(), GetGUID().ToString(), aura.GetDuration());
                }
            }
        }

        public void CalculateSpellDamageTaken(SpellNonMeleeDamage damageInfo, int damage, SpellInfo spellInfo, WeaponAttackType attackType = WeaponAttackType.BaseAttack, bool crit = false)
        {
            if (damage < 0)
                return;

            Unit victim = damageInfo.target;
            if (victim == null || !victim.IsAlive())
                return;

            SpellSchoolMask damageSchoolMask = damageInfo.schoolMask;

            if (IsDamageReducedByArmor(damageSchoolMask, spellInfo))
                damage = (int)CalcArmorReducedDamage(damageInfo.attacker, victim, (uint)damage, spellInfo, attackType);

            bool blocked = false;
            // Per-school calc
            switch (spellInfo.DmgClass)
            {
                // Melee and Ranged Spells
                case SpellDmgClass.Ranged:
                case SpellDmgClass.Melee:
                    {
                        // Physical Damage
                        if (damageSchoolMask.HasAnyFlag(SpellSchoolMask.Normal))
                        {
                            // Get blocked status
                            blocked = isSpellBlocked(victim, spellInfo, attackType);
                        }

                        if (crit)
                        {
                            damageInfo.HitInfo |= HitInfo.CriticalHit;

                            // Calculate crit bonus
                            uint crit_bonus = (uint)damage;
                            // Apply crit_damage bonus for melee spells
                            Player modOwner = GetSpellModOwner();
                            if (modOwner != null)
                                modOwner.ApplySpellMod(spellInfo.Id, SpellModOp.CritDamageBonus, ref crit_bonus);
                            damage += (int)crit_bonus;

                            // Increase crit damage from SPELL_AURA_MOD_CRIT_DAMAGE_BONUS
                            float critPctDamageMod = (GetTotalAuraMultiplierByMiscMask(AuraType.ModCritDamageBonus, (uint)spellInfo.GetSchoolMask()) - 1.0f) * 100;

                            if (critPctDamageMod != 0)
                                MathFunctions.AddPct(ref damage, (int)critPctDamageMod);
                        }

                        // Spell weapon based damage CAN BE crit & blocked at same time
                        if (blocked)
                        {
                            // double blocked amount if block is critical
                            uint value = victim.GetBlockPercent();
                            if (victim.isBlockCritical())
                                value *= 2; // double blocked percent
                            damageInfo.blocked = (uint)MathFunctions.CalculatePct(damage, value);
                            if (damage <= damageInfo.blocked)
                            {
                                damageInfo.blocked = (uint)damage;
                                damageInfo.fullBlock = true;
                            }
                            damage -= (int)damageInfo.blocked;
                        }
                        uint dmg = (uint)damage;
                        ApplyResilience(victim, ref dmg);
                        damage = (int)dmg;
                        break;
                    }
                // Magical Attacks
                case SpellDmgClass.None:
                case SpellDmgClass.Magic:
                    {
                        // If crit add critical bonus
                        if (crit)
                        {
                            damageInfo.HitInfo |= HitInfo.CriticalHit;
                            damage = (int)SpellCriticalDamageBonus(spellInfo, (uint)damage, victim);
                        }
                        uint dmg = (uint)damage;
                        ApplyResilience(victim, ref dmg);
                        damage = (int)dmg;
                        break;
                    }
                default:
                    break;
            }

            // Script Hook For CalculateSpellDamageTaken -- Allow scripts to change the Damage post class mitigation calculations
            Global.ScriptMgr.ModifySpellDamageTaken(damageInfo.target, damageInfo.attacker, ref damage);

            // Calculate absorb resist
            if (damage < 0)
                damage = 0;

            damageInfo.damage = (uint)damage;
            damageInfo.originalDamage = (uint)damage;
            DamageInfo dmgInfo = new DamageInfo(damageInfo, DamageEffectType.SpellDirect, WeaponAttackType.BaseAttack, ProcFlagsHit.None);
            CalcAbsorbResist(dmgInfo);
            damageInfo.absorb = dmgInfo.GetAbsorb();
            damageInfo.resist = dmgInfo.GetResist();

            if (damageInfo.absorb != 0)
                damageInfo.HitInfo |= (damageInfo.damage - damageInfo.absorb == 0 ? HitInfo.FullAbsorb : HitInfo.PartialAbsorb);

            if (damageInfo.resist != 0)
                damageInfo.HitInfo |= (damageInfo.damage - damageInfo.resist == 0 ? HitInfo.FullResist : HitInfo.PartialResist);

            damageInfo.damage = dmgInfo.GetDamage();
        }
        public void DealSpellDamage(SpellNonMeleeDamage damageInfo, bool durabilityLoss)
        {
            if (damageInfo == null)
                return;

            Unit victim = damageInfo.target;
            if (victim == null)
                return;

            if (!victim.IsAlive() || victim.HasUnitState(UnitState.InFlight) || (victim.IsTypeId(TypeId.Unit) && victim.ToCreature().IsEvadingAttacks()))
                return;

            SpellInfo spellProto = Global.SpellMgr.GetSpellInfo(damageInfo.SpellId);
            if (spellProto == null)
            {
                Log.outDebug(LogFilter.Unit, "Unit.DealSpellDamage has wrong damageInfo.SpellID: {0}", damageInfo.SpellId);
                return;
            }

            // Call default DealDamage
            CleanDamage cleanDamage = new CleanDamage(damageInfo.cleanDamage, damageInfo.absorb, WeaponAttackType.BaseAttack, MeleeHitOutcome.Normal);
            DealDamage(victim, damageInfo.damage, cleanDamage, DamageEffectType.SpellDirect, damageInfo.schoolMask, spellProto, durabilityLoss);
        }

        public void SendSpellNonMeleeDamageLog(SpellNonMeleeDamage log)
        {
            SpellNonMeleeDamageLog packet = new SpellNonMeleeDamageLog();
            packet.Me = log.target.GetGUID();
            packet.CasterGUID = log.attacker.GetGUID();
            packet.CastID = log.castId;
            packet.SpellID = (int)log.SpellId;
            packet.Damage = (int)log.damage;
            packet.OriginalDamage = (int)log.originalDamage;
            if (log.damage > log.preHitHealth)
                packet.Overkill = (int)(log.damage - log.preHitHealth);
            else
                packet.Overkill = -1;

            packet.SchoolMask = (byte)log.schoolMask;
            packet.Absorbed = (int)log.absorb;
            packet.Resisted = (int)log.resist;
            packet.ShieldBlock = (int)log.blocked;
            packet.Periodic = log.periodicLog;
            packet.Flags = (int)log.HitInfo;

            ContentTuningParams contentTuningParams = new ContentTuningParams();
            if (contentTuningParams.GenerateDataForUnits(log.attacker, log.target))
                packet.ContentTuning.Set(contentTuningParams);

            SendCombatLogMessage(packet);
        }

        public void SendPeriodicAuraLog(SpellPeriodicAuraLogInfo info)
        {
            AuraEffect aura = info.auraEff;

            SpellPeriodicAuraLog data = new SpellPeriodicAuraLog();
            data.TargetGUID = GetGUID();
            data.CasterGUID = aura.GetCasterGUID();
            data.SpellID = aura.GetId();
            data.LogData.Initialize(this);

            SpellPeriodicAuraLog.SpellLogEffect spellLogEffect = new SpellPeriodicAuraLog.SpellLogEffect();
            spellLogEffect.Effect = (uint)aura.GetAuraType();
            spellLogEffect.Amount = info.damage;
            spellLogEffect.OriginalDamage = (int)info.originalDamage;
            spellLogEffect.OverHealOrKill = (uint)info.overDamage;
            spellLogEffect.SchoolMaskOrPower = (uint)aura.GetSpellInfo().GetSchoolMask();
            spellLogEffect.AbsorbedOrAmplitude = info.absorb;
            spellLogEffect.Resisted = info.resist;
            spellLogEffect.Crit = info.critical;
            // @todo: implement debug info

            ContentTuningParams contentTuningParams = new ContentTuningParams();
            Unit caster = Global.ObjAccessor.GetUnit(this, aura.GetCasterGUID());
            if (caster && contentTuningParams.GenerateDataForUnits(caster, this))
                spellLogEffect.ContentTuning.Set(contentTuningParams);

            data.Effects.Add(spellLogEffect);

            SendCombatLogMessage(data);
        }
        public void SendSpellMiss(Unit target, uint spellID, SpellMissInfo missInfo)
        {
            SpellMissLog spellMissLog = new SpellMissLog();
            spellMissLog.SpellID = spellID;
            spellMissLog.Caster = GetGUID();
            spellMissLog.Entries.Add(new SpellLogMissEntry(target.GetGUID(), (byte)missInfo));
            SendMessageToSet(spellMissLog, true);
        }

        void SendSpellDamageResist(Unit target, uint spellId)
        {
            ProcResist procResist = new ProcResist();
            procResist.Caster = GetGUID();
            procResist.SpellID = spellId;
            procResist.Target = target.GetGUID();
            SendMessageToSet(procResist, true);
        }

        public void SendSpellDamageImmune(Unit target, uint spellId, bool isPeriodic)
        {
            SpellOrDamageImmune spellOrDamageImmune = new SpellOrDamageImmune();
            spellOrDamageImmune.CasterGUID = GetGUID();
            spellOrDamageImmune.VictimGUID = target.GetGUID();
            spellOrDamageImmune.SpellID = spellId;
            spellOrDamageImmune.IsPeriodic = isPeriodic;
            SendMessageToSet(spellOrDamageImmune, true);
        }

        public void SendSpellInstakillLog(uint spellId, Unit caster, Unit target = null)
        {
            SpellInstakillLog spellInstakillLog = new SpellInstakillLog();
            spellInstakillLog.Caster = caster.GetGUID();
            spellInstakillLog.Target = target ? target.GetGUID() : caster.GetGUID();
            spellInstakillLog.SpellID = spellId;
            SendMessageToSet(spellInstakillLog, false);
        }

        public void RemoveAurasOnEvade()
        {
            if (IsCharmedOwnedByPlayerOrPlayer()) // if it is a player owned creature it should not remove the aura
                return;

            // don't remove vehicle auras, passengers aren't supposed to drop off the vehicle
            // don't remove clone caster on evade (to be verified)
            RemoveAllAurasExceptType(AuraType.ControlVehicle, AuraType.CloneCaster);
        }

        public void RemoveAllAurasOnDeath()
        {
            // used just after dieing to remove all visible auras
            // and disable the mods for the passive ones
            foreach (var app in GetAppliedAuras())
            {
                if (app.Value == null)
                    continue;

                Aura aura = app.Value.GetBase();
                if (!aura.IsPassive() && !aura.IsDeathPersistent())
                    _UnapplyAura(app, AuraRemoveMode.Death);
            }

            foreach (var pair in GetOwnedAuras())
            {
                Aura aura = pair.Value;
                if (pair.Value == null)
                    continue;

                if (!aura.IsPassive() && !aura.IsDeathPersistent())
                    RemoveOwnedAura(pair, AuraRemoveMode.Death);
            }
        }
        public void RemoveMovementImpairingAuras()
        {
            RemoveAurasWithMechanic((1 << (int)Mechanics.Snare) | (1 << (int)Mechanics.Root));
        }
        public void RemoveAllAurasRequiringDeadTarget()
        {
            foreach (var app in GetAppliedAuras())
            {
                Aura aura = app.Value.GetBase();
                if (!aura.IsPassive() && aura.GetSpellInfo().IsRequiringDeadTarget())
                    _UnapplyAura(app, AuraRemoveMode.Default);
            }

            foreach (var aura in GetOwnedAuras())
            {
                if (!aura.Value.IsPassive() && aura.Value.GetSpellInfo().IsRequiringDeadTarget())
                    RemoveOwnedAura(aura, AuraRemoveMode.Default);
            }
        }

        public AuraEffect IsScriptOverriden(SpellInfo spell, int script)
        {
            var auras = GetAuraEffectsByType(AuraType.OverrideClassScripts);
            foreach (var eff in auras)
            {
                if (eff.GetMiscValue() == script)
                    if (eff.IsAffectingSpell(spell))
                        return eff;
            }
            return null;
        }

        public DiminishingLevels GetDiminishing(DiminishingGroup group)
        {
            DiminishingReturn diminish = m_Diminishing[(int)group];
            if (diminish.HitCount == 0)
                return DiminishingLevels.Level1;

            // If last spell was cast more than 18 seconds ago - reset the count.
            if (diminish.Stack == 0 && Time.GetMSTimeDiffToNow(diminish.HitTime) > 18 * Time.InMilliseconds)
            {
                diminish.HitCount = DiminishingLevels.Level1;
                return DiminishingLevels.Level1;
            }

            return diminish.HitCount;
        }

        public void IncrDiminishing(SpellInfo auraSpellInfo)
        {
            DiminishingGroup group = auraSpellInfo.GetDiminishingReturnsGroupForSpell();
            DiminishingLevels maxLevel = auraSpellInfo.GetDiminishingReturnsMaxLevel();

            // Checking for existing in the table
            DiminishingReturn diminish = m_Diminishing[(int)group];
            if (diminish.HitCount < maxLevel)
                ++diminish.HitCount;
        }

        public float ApplyDiminishingToDuration(SpellInfo auraSpellInfo, ref int duration, Unit caster, DiminishingLevels previousLevel)
        {
            DiminishingGroup group = auraSpellInfo.GetDiminishingReturnsGroupForSpell();
            if (duration == -1 || group == DiminishingGroup.None)
                return 1.0f;

            int limitDuration = auraSpellInfo.GetDiminishingReturnsLimitDuration();

            // test pet/charm masters instead pets/charmeds
            Unit targetOwner = GetCharmerOrOwner();
            Unit casterOwner = caster.GetCharmerOrOwner();

            if (limitDuration > 0 && duration > limitDuration)
            {
                Unit target = targetOwner ?? this;
                Unit source = casterOwner ?? caster;

                if ((target.IsTypeId(TypeId.Player) || target.ToCreature().GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.AllDiminish))
                    && source.IsTypeId(TypeId.Player))
                    duration = limitDuration;
            }

            float mod = 1.0f;

            switch (group)
            {
                case DiminishingGroup.Taunt:
                    if (IsTypeId(TypeId.Unit) && ToCreature().GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.TauntDiminish))
                    {
                        DiminishingLevels diminish = previousLevel;
                        switch (diminish)
                        {
                            case DiminishingLevels.Level1:
                                break;
                            case DiminishingLevels.Level2:
                                mod = 0.65f;
                                break;
                            case DiminishingLevels.Level3:
                                mod = 0.4225f;
                                break;
                            case DiminishingLevels.Level4:
                                mod = 0.274625f;
                                break;
                            case DiminishingLevels.TauntImmune:
                                mod = 0.0f;
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case DiminishingGroup.AOEKnockback:
                    if ((auraSpellInfo.GetDiminishingReturnsGroupType() == DiminishingReturnsType.Player && (((targetOwner ? targetOwner : this).ToPlayer())
                        || IsCreature() && ToCreature().GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.AllDiminish)))
                        || auraSpellInfo.GetDiminishingReturnsGroupType() == DiminishingReturnsType.All)
                    {
                        DiminishingLevels diminish = previousLevel;
                        switch (diminish)
                        {
                            case DiminishingLevels.Level1:
                                break;
                            case DiminishingLevels.Level2:
                                mod = 0.5f;
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                default:
                    if ((auraSpellInfo.GetDiminishingReturnsGroupType() == DiminishingReturnsType.Player && (((targetOwner ? targetOwner : this).ToPlayer())
                        || IsCreature() && ToCreature().GetCreatureTemplate().FlagsExtra.HasAnyFlag(CreatureFlagsExtra.AllDiminish)))
                        || auraSpellInfo.GetDiminishingReturnsGroupType() == DiminishingReturnsType.All)
                    {
                        DiminishingLevels diminish = previousLevel;
                        switch (diminish)
                        {
                            case DiminishingLevels.Level1:
                                break;
                            case DiminishingLevels.Level2:
                                mod = 0.5f;
                                break;
                            case DiminishingLevels.Level3:
                                mod = 0.25f;
                                break;
                            case DiminishingLevels.Immune:
                                mod = 0.0f;
                                break;
                            default: break;
                        }
                    }
                    break;
            }

            duration = (int)(duration * mod);
            return mod;
        }

        public void ApplyDiminishingAura(DiminishingGroup group, bool apply)
        {
            // Checking for existing in the table
            DiminishingReturn diminish = m_Diminishing[(int)group];

            if (apply)
                ++diminish.Stack;
            else if (diminish.Stack != 0)
            {
                --diminish.Stack;

                // Remember time after last aura from group removed
                if (diminish.Stack == 0)
                    diminish.HitTime = Time.GetMSTime();
            }
        }

        void ClearDiminishings()
        {
            for (int i = 0; i < (int)DiminishingGroup.Max; ++i)
                m_Diminishing[i].Clear();
        }

        public uint GetRemainingPeriodicAmount(ObjectGuid caster, uint spellId, AuraType auraType, int effectIndex = 0)
        {
            uint amount = 0;
            var periodicAuras = GetAuraEffectsByType(auraType);
            foreach (var eff in periodicAuras)
            {
                if (eff.GetCasterGUID() != caster || eff.GetId() != spellId || eff.GetEffIndex() != effectIndex || eff.GetTotalTicks() == 0)
                    continue;
                amount += (uint)((eff.GetAmount() * Math.Max(eff.GetTotalTicks() - eff.GetTickNumber(), 0)) / eff.GetTotalTicks());
                break;
            }

            return amount;
        }

        // Interrupts
        public void InterruptNonMeleeSpells(bool withDelayed, uint spell_id = 0, bool withInstant = true)
        {
            // generic spells are interrupted if they are not finished or delayed
            if (GetCurrentSpell(CurrentSpellTypes.Generic) != null && (spell_id == 0 || m_currentSpells[CurrentSpellTypes.Generic].m_spellInfo.Id == spell_id))
                InterruptSpell(CurrentSpellTypes.Generic, withDelayed, withInstant);

            // autorepeat spells are interrupted if they are not finished or delayed
            if (GetCurrentSpell(CurrentSpellTypes.AutoRepeat) != null && (spell_id == 0 || m_currentSpells[CurrentSpellTypes.AutoRepeat].m_spellInfo.Id == spell_id))
                InterruptSpell(CurrentSpellTypes.AutoRepeat, withDelayed, withInstant);

            // channeled spells are interrupted if they are not finished, even if they are delayed
            if (GetCurrentSpell(CurrentSpellTypes.Channeled) != null && (spell_id == 0 || m_currentSpells[CurrentSpellTypes.Channeled].m_spellInfo.Id == spell_id))
                InterruptSpell(CurrentSpellTypes.Channeled, true, true);
        }
        public void InterruptSpell(CurrentSpellTypes spellType, bool withDelayed = true, bool withInstant = true)
        {
            Cypher.Assert(spellType < CurrentSpellTypes.Max);

            Log.outDebug(LogFilter.Unit, "Interrupt spell for unit {0}", GetEntry());
            Spell spell = m_currentSpells.LookupByKey(spellType);
            if (spell != null
                && (withDelayed || spell.getState() != SpellState.Delayed)
                && (withInstant || spell.GetCastTime() > 0))
            {
                // for example, do not let self-stun aura interrupt itself
                if (!spell.IsInterruptable())
                    return;

                // send autorepeat cancel message for autorepeat spells
                if (spellType == CurrentSpellTypes.AutoRepeat)
                    if (IsTypeId(TypeId.Player))
                        ToPlayer().SendAutoRepeatCancel(this);

                if (spell.getState() != SpellState.Finished)
                    spell.cancel();

                if (IsCreature() && IsAIEnabled)
                    ToCreature().GetAI().OnSpellCastInterrupt(spell.GetSpellInfo());

                m_currentSpells[spellType] = null;
                spell.SetReferencedFromCurrent(false);
            }
        }
        public void UpdateInterruptMask()
        {
            m_interruptMask.Clear();
            foreach (AuraApplication aurApp in m_interruptableAuras)
            {
                for (var i = 0; i < m_interruptMask.Length; ++i)
                    m_interruptMask[i] |= aurApp.GetBase().GetSpellInfo().AuraInterruptFlags[i];
            }

            Spell spell = GetCurrentSpell(CurrentSpellTypes.Channeled);
            if (spell != null)
            {
                if (spell.getState() == SpellState.Casting)
                {
                    for (var i = 0; i < m_interruptMask.Length; ++i)
                        m_interruptMask[i] |= spell.m_spellInfo.ChannelInterruptFlags[i];
                }
            }
        }

        // Auras
        public List<Aura> GetSingleCastAuras() { return m_scAuras; }
        public List<KeyValuePair<uint, Aura>> GetOwnedAuras()
        {
            return m_ownedAuras.KeyValueList;
        }
        public List<KeyValuePair<uint, AuraApplication>> GetAppliedAuras()
        {
            return m_appliedAuras.KeyValueList;
        }

        public Aura AddAura(uint spellId, Unit target)
        {
            if (target == null)
                return null;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
            if (spellInfo == null)
                return null;

            if (!target.IsAlive() && !spellInfo.IsPassive() && !spellInfo.HasAttribute(SpellAttr2.CanTargetDead))
                return null;

            return AddAura(spellInfo, SpellConst.MaxEffectMask, target);
        }

        public Aura AddAura(SpellInfo spellInfo, uint effMask, Unit target)
        {
            if (spellInfo == null)
                return null;

            if (target.IsImmunedToSpell(spellInfo, this))
                return null;

            for (byte i = 0; i < SpellConst.MaxEffects; ++i)
            {
                if (!Convert.ToBoolean(effMask & (1 << i)))
                    continue;
                if (target.IsImmunedToSpellEffect(spellInfo, i, this))
                    effMask &= ~(uint)(1 << i);
            }

            ObjectGuid castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, GetMapId(), spellInfo.Id, GetMap().GenerateLowGuid(HighGuid.Cast));
            Aura aura = Aura.TryRefreshStackOrCreate(spellInfo, castId, effMask, target, this);
            if (aura != null)
            {
                aura.ApplyForTargets();
                return aura;
            }
            return null;
        }

        public bool HandleSpellClick(Unit clicker, sbyte seatId = -1)
        {
            bool result = false;

            uint spellClickEntry = GetVehicleKit() != null ? GetVehicleKit().GetCreatureEntry() : GetEntry();
            var clickPair = Global.ObjectMgr.GetSpellClickInfoMapBounds(spellClickEntry);
            foreach (var clickInfo in clickPair)
            {
                //! First check simple relations from clicker to clickee
                if (!clickInfo.IsFitToRequirements(clicker, this))
                    continue;

                //! Check database conditions
                if (!Global.ConditionMgr.IsObjectMeetingSpellClickConditions(spellClickEntry, clickInfo.spellId, clicker, this))
                    continue;

                Unit caster = Convert.ToBoolean(clickInfo.castFlags & (byte)SpellClickCastFlags.CasterClicker) ? clicker : this;
                Unit target = Convert.ToBoolean(clickInfo.castFlags & (byte)SpellClickCastFlags.TargetClicker) ? clicker : this;
                ObjectGuid origCasterGUID = Convert.ToBoolean(clickInfo.castFlags & (byte)SpellClickCastFlags.OrigCasterOwner) ? GetOwnerGUID() : clicker.GetGUID();

                SpellInfo spellEntry = Global.SpellMgr.GetSpellInfo(clickInfo.spellId);
                // if (!spellEntry) should be checked at npc_spellclick load

                if (seatId > -1)
                {
                    byte i = 0;
                    bool valid = false;
                    foreach (SpellEffectInfo effect in spellEntry.GetEffectsForDifficulty(GetMap().GetDifficultyID()))
                    {
                        if (effect == null)
                            continue;

                        if (effect.ApplyAuraName == AuraType.ControlVehicle)
                        {
                            valid = true;
                            break;
                        }
                        ++i;
                    }

                    if (!valid)
                    {
                        Log.outError(LogFilter.Sql, "Spell {0} specified in npc_spellclick_spells is not a valid vehicle enter aura!", clickInfo.spellId);
                        continue;
                    }

                    if (IsInMap(caster))
                        caster.CastCustomSpell(clickInfo.spellId, SpellValueMod.BasePoint0 + i, seatId + 1, target, GetVehicleKit() != null ? TriggerCastFlags.IgnoreCasterMountedOrOnVehicle : TriggerCastFlags.None, null, null, origCasterGUID);
                    else    // This can happen during Player._LoadAuras
                    {
                        int[] bp0 = new int[SpellConst.MaxEffects];
                        foreach (SpellEffectInfo effect in spellEntry.GetEffectsForDifficulty(GetMap().GetDifficultyID()))
                        {
                            if (effect != null)
                                bp0[effect.EffectIndex] = effect.BasePoints;
                        }

                        bp0[i] = seatId;
                        Aura.TryRefreshStackOrCreate(spellEntry, ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, GetMapId(), spellEntry.Id, GetMap().GenerateLowGuid(HighGuid.Cast)), SpellConst.MaxEffectMask, this, clicker, bp0, null, origCasterGUID);
                    }
                }
                else
                {
                    if (IsInMap(caster))
                        caster.CastSpell(target, spellEntry, GetVehicleKit() != null ? TriggerCastFlags.IgnoreCasterMountedOrOnVehicle : TriggerCastFlags.None, null, null, origCasterGUID);
                    else
                        Aura.TryRefreshStackOrCreate(spellEntry, ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, GetMapId(), spellEntry.Id, GetMap().GenerateLowGuid(HighGuid.Cast)), SpellConst.MaxEffectMask, this, clicker, null, null, origCasterGUID);
                }

                result = true;
            }

            Creature creature = ToCreature();
            if (creature && creature.IsAIEnabled)
                creature.GetAI().OnSpellClick(clicker, ref result);

            return result;
        }

        public bool HasAura(uint spellId, ObjectGuid casterGUID = default(ObjectGuid), ObjectGuid itemCasterGUID = default(ObjectGuid), uint reqEffMask = 0)
        {
            if (GetAuraApplication(spellId, casterGUID, itemCasterGUID, reqEffMask) != null)
                return true;
            return false;
        }
        public bool HasAuraEffect(uint spellId, uint effIndex, ObjectGuid casterGUID = default(ObjectGuid))
        {
            var range = m_appliedAuras.LookupByKey(spellId);
            if (!range.Empty())
            {
                foreach (var aura in range)
                    if (aura.HasEffect(effIndex) && (casterGUID.IsEmpty() || aura.GetBase().GetCasterGUID() == casterGUID))
                        return true;
            }

            return false;
        }
        public bool HasAuraWithMechanic(uint mechanicMask)
        {
            foreach (var pair in GetAppliedAuras())
            {
                SpellInfo spellInfo = pair.Value.GetBase().GetSpellInfo();
                if (spellInfo.Mechanic != 0 && Convert.ToBoolean(mechanicMask & (1 << (int)spellInfo.Mechanic)))
                    return true;

                foreach (SpellEffectInfo effect in pair.Value.GetBase().GetSpellEffectInfos())
                    if (effect != null && effect.Effect != 0 && effect.Mechanic != 0)
                        if (Convert.ToBoolean(mechanicMask & (1 << (int)effect.Mechanic)))
                            return true;
            }

            return false;
        }

        // target dependent range checks
        public float GetSpellMaxRangeForTarget(Unit target, SpellInfo spellInfo)
        {
            if (spellInfo.RangeEntry == null)
                return 0;
            if (spellInfo.RangeEntry.RangeMax[0] == spellInfo.RangeEntry.RangeMax[1])
                return spellInfo.GetMaxRange();
            if (!target)
                return spellInfo.GetMaxRange(true);
            return spellInfo.GetMaxRange(!IsHostileTo(target));
        }

        public float GetSpellMinRangeForTarget(Unit target, SpellInfo spellInfo)
        {
            if (spellInfo.RangeEntry == null)
                return 0;
            if (spellInfo.RangeEntry.RangeMin[0] == spellInfo.RangeEntry.RangeMin[1])
                return spellInfo.GetMinRange();
            if (!target)
                return spellInfo.GetMinRange(true);
            return spellInfo.GetMinRange(!IsHostileTo(target));
        }

        public bool HasAuraType(AuraType auraType)
        {
            return !m_modAuras.LookupByKey(auraType).Empty();
        }
        public bool HasAuraTypeWithCaster(AuraType auratype, ObjectGuid caster)
        {
            var mTotalAuraList = GetAuraEffectsByType(auratype);
            foreach (var aura in mTotalAuraList)
                if (caster == aura.GetCasterGUID())
                    return true;
            return false;
        }
        public bool HasAuraTypeWithMiscvalue(AuraType auratype, int miscvalue)
        {
            var mTotalAuraList = GetAuraEffectsByType(auratype);
            foreach (var aura in mTotalAuraList)
                if (miscvalue == aura.GetMiscValue())
                    return true;
            return false;
        }
        public bool HasAuraTypeWithAffectMask(AuraType auratype, SpellInfo affectedSpell)
        {
            var mTotalAuraList = GetAuraEffectsByType(auratype);
            foreach (var aura in mTotalAuraList)
                if (aura.IsAffectingSpell(affectedSpell))
                    return true;
            return false;
        }
        public bool HasAuraTypeWithValue(AuraType auratype, int value)
        {
            var mTotalAuraList = GetAuraEffectsByType(auratype);
            foreach (var aura in mTotalAuraList)
                if (value == aura.GetAmount())
                    return true;
            return false;
        }

        public bool HasNegativeAuraWithInterruptFlag(SpellAuraInterruptFlags flag, ObjectGuid guid = default(ObjectGuid))
        {
            return HasNegativeAuraWithInterruptFlag((uint)flag, 0, guid);
        }

        public bool HasNegativeAuraWithInterruptFlag(SpellAuraInterruptFlags2 flag, ObjectGuid guid = default(ObjectGuid))
        {
            return HasNegativeAuraWithInterruptFlag((uint)flag, 1, guid);
        }

        public bool HasNegativeAuraWithInterruptFlag(uint flag, int index, ObjectGuid guid = default(ObjectGuid))
        {
            if (!Convert.ToBoolean(m_interruptMask[index] & flag))
                return false;

            foreach (var aura in m_interruptableAuras)
            {
                if (!aura.IsPositive() && Convert.ToBoolean(aura.GetBase().GetSpellInfo().AuraInterruptFlags[index] & flag)
                    && (guid.IsEmpty() || aura.GetBase().GetCasterGUID() == guid))
                    return true;
            }
            return false;
        }
        bool HasNegativeAuraWithAttribute(SpellAttr0 flag, ObjectGuid guid = default(ObjectGuid))
        {
            foreach (var list in GetAppliedAuras())
            {
                Aura aura = list.Value.GetBase();
                if (!list.Value.IsPositive() && aura.GetSpellInfo().HasAttribute(flag) && (guid.IsEmpty() || aura.GetCasterGUID() == guid))
                    return true;
            }
            return false;
        }

        public uint GetAuraCount(uint spellId)
        {
            uint count = 0;
            var range = m_appliedAuras.LookupByKey(spellId);
            foreach (var aura in range)
            {
                if (aura.GetBase().GetStackAmount() == 0)
                    ++count;
                else
                    count += aura.GetBase().GetStackAmount();
            }

            return count;
        }
        public Aura GetAuraOfRankedSpell(uint spellId, ObjectGuid casterGUID = default(ObjectGuid), ObjectGuid itemCasterGUID = default(ObjectGuid), uint reqEffMask = 0)
        {
            var aurApp = GetAuraApplicationOfRankedSpell(spellId, casterGUID, itemCasterGUID, reqEffMask);
            return aurApp?.GetBase();
        }
        AuraApplication GetAuraApplicationOfRankedSpell(uint spellId, ObjectGuid casterGUID = default(ObjectGuid), ObjectGuid itemCasterGUID = default(ObjectGuid), uint reqEffMask = 0, AuraApplication except = null)
        {
            uint rankSpell = Global.SpellMgr.GetFirstSpellInChain(spellId);
            while (rankSpell != 0)
            {
                AuraApplication aurApp = GetAuraApplication(rankSpell, casterGUID, itemCasterGUID, reqEffMask, except);
                if (aurApp != null)
                    return aurApp;
                rankSpell = Global.SpellMgr.GetNextSpellInChain(rankSpell);
            }
            return null;
        }

        public List<DispelableAura> GetDispellableAuraList(Unit caster, uint dispelMask, bool isReflect = false)
        {
            List<DispelableAura> dispelList = new List<DispelableAura>();

            var auras = GetOwnedAuras();
            foreach (var pair in auras)
            {
                Aura aura = pair.Value;
                AuraApplication aurApp = aura.GetApplicationOfTarget(GetGUID());
                if (aurApp == null)
                    continue;

                // don't try to remove passive auras
                if (aura.IsPassive())
                    continue;

                if (Convert.ToBoolean(aura.GetSpellInfo().GetDispelMask() & dispelMask))
                {
                    // do not remove positive auras if friendly target
                    //               negative auras if non-friendly
                    // unless we're reflecting (dispeller eliminates one of it's benefitial buffs)
                    if (isReflect != (aurApp.IsPositive() == IsFriendlyTo(caster)))
                        continue;

                    // 2.4.3 Patch Notes: "Dispel effects will no longer attempt to remove effects that have 100% dispel resistance."
                    int chance = aura.CalcDispelChance(this, !IsFriendlyTo(caster));
                    if (chance == 0)
                        continue;

                    // The charges / stack amounts don't count towards the total number of auras that can be dispelled.
                    // Ie: A dispel on a target with 5 stacks of Winters Chill and a Polymorph has 1 / (1 + 1) -> 50% chance to dispell
                    // Polymorph instead of 1 / (5 + 1) -> 16%.
                    bool dispelCharges = aura.GetSpellInfo().HasAttribute(SpellAttr7.DispelCharges);
                    byte charges = dispelCharges ? aura.GetCharges() : aura.GetStackAmount();
                    if (charges > 0)
                        dispelList.Add(new DispelableAura(aura, chance, charges));
                }
            }

            return dispelList;
        }

        public void RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags flag, uint except = 0)
        {
            RemoveAurasWithInterruptFlags((uint)flag, 0, except);
        }

        public void RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2 flag, uint except = 0)
        {
            RemoveAurasWithInterruptFlags((uint)flag, 1, except);
        }

        void RemoveAurasWithInterruptFlags(uint flag, int index, uint except = 0)
        {
            if (!Convert.ToBoolean(m_interruptMask[index] & flag))
                return;

            // interrupt auras
            for (var i = 0; i < m_interruptableAuras.Count; i++)
            {
                Aura aura = m_interruptableAuras[i].GetBase();

                if (Convert.ToBoolean(aura.GetSpellInfo().AuraInterruptFlags[index] & flag) && (except == 0 || aura.GetId() != except)
                    && !(Convert.ToBoolean(flag & (uint)SpellAuraInterruptFlags.Move) && HasAuraTypeWithAffectMask(AuraType.CastWhileWalking, aura.GetSpellInfo())))
                {
                    uint removedAuras = m_removedAurasCount;
                    RemoveAura(aura, AuraRemoveMode.Interrupt);
                    if (m_removedAurasCount > removedAuras + 1)
                        i = 0;
                }
            }

            // interrupt channeled spell
            Spell spell = GetCurrentSpell(CurrentSpellTypes.Channeled);
            if (spell != null)
            {
                if (spell.getState() == SpellState.Casting
                    && Convert.ToBoolean(spell.GetSpellInfo().ChannelInterruptFlags[index] & flag)
                    && spell.GetSpellInfo().Id != except
                    && !(Convert.ToBoolean(flag & (uint)SpellAuraInterruptFlags.Move) && HasAuraTypeWithAffectMask(AuraType.CastWhileWalking, spell.GetSpellInfo())))
                    InterruptNonMeleeSpells(false);
            }

            UpdateInterruptMask();
        }

        public void RemoveAurasWithMechanic(uint mechanic_mask, AuraRemoveMode removemode = AuraRemoveMode.Default, uint except = 0)
        {
            foreach (var app in GetAppliedAuras())
            {
                if (app.Value == null)
                    continue;
                Aura aura = app.Value.GetBase();
                if (except == 0 || aura.GetId() != except)
                {
                    if (Convert.ToBoolean(aura.GetSpellInfo().GetAllEffectsMechanicMask() & mechanic_mask))
                    {
                        RemoveAura(app, removemode);
                        continue;
                    }
                }
            }
        }
        public void RemoveAurasDueToSpellBySteal(uint spellId, ObjectGuid casterGUID, Unit stealer)
        {
            var range = m_ownedAuras.LookupByKey(spellId);
            foreach (var aura in range)
            {
                if (aura.GetCasterGUID() == casterGUID)
                {
                    int[] damage = new int[SpellConst.MaxEffects];
                    int[] baseDamage = new int[SpellConst.MaxEffects];
                    uint effMask = 0;
                    uint recalculateMask = 0;
                    Unit caster = aura.GetCaster();
                    for (byte i = 0; i < SpellConst.MaxEffects; ++i)
                    {
                        if (aura.GetEffect(i) != null)
                        {
                            baseDamage[i] = aura.GetEffect(i).GetBaseAmount();
                            damage[i] = aura.GetEffect(i).GetAmount();
                            effMask |= 1u << i;
                            if (aura.GetEffect(i).CanBeRecalculated())
                                recalculateMask |= 1u << i;
                        }
                        else
                        {
                            baseDamage[i] = 0;
                            damage[i] = 0;
                        }
                    }

                    bool stealCharge = aura.GetSpellInfo().HasAttribute(SpellAttr7.DispelCharges);
                    // Cast duration to unsigned to prevent permanent aura's such as Righteous Fury being permanently added to caster
                    uint dur = (uint)Math.Min(2u * Time.Minute * Time.InMilliseconds, aura.GetDuration());

                    Aura oldAura = stealer.GetAura(aura.GetId(), aura.GetCasterGUID());
                    if (oldAura != null)
                    {
                        if (stealCharge)
                            oldAura.ModCharges(1);
                        else
                            oldAura.ModStackAmount(1);
                        oldAura.SetDuration((int)dur);
                    }
                    else
                    {
                        // single target state must be removed before aura creation to preserve existing single target aura
                        if (aura.IsSingleTarget())
                            aura.UnregisterSingleTarget();

                        Aura newAura = Aura.TryRefreshStackOrCreate(aura.GetSpellInfo(), aura.GetCastGUID(), effMask, stealer, null, baseDamage, null, aura.GetCasterGUID());
                        if (newAura != null)
                        {
                            // created aura must not be single target aura,, so stealer won't loose it on recast
                            if (newAura.IsSingleTarget())
                            {
                                newAura.UnregisterSingleTarget();
                                // bring back single target aura status to the old aura
                                aura.SetIsSingleTarget(true);
                                caster.GetSingleCastAuras().Add(aura);
                            }
                            // FIXME: using aura.GetMaxDuration() maybe not blizzlike but it fixes stealing of spells like Innervate
                            newAura.SetLoadedState(aura.GetMaxDuration(), (int)dur, stealCharge ? 1 : aura.GetCharges(), 1, recalculateMask, damage);
                            newAura.ApplyForTargets();
                        }
                    }

                    if (stealCharge)
                        aura.ModCharges(-1, AuraRemoveMode.EnemySpell);
                    else
                        aura.ModStackAmount(-1, AuraRemoveMode.EnemySpell);

                    return;
                }
            }
        }
        public void RemoveAurasDueToItemSpell(uint spellId, ObjectGuid castItemGuid)
        {
            var appliedAuras = m_appliedAuras.LookupByKey(spellId);
            for (var i = 0; i < appliedAuras.Count; ++i)
            {
                AuraApplication app = appliedAuras[i];
                if (app.GetBase().GetCastItemGUID() == castItemGuid)
                {
                    RemoveAura(app);
                }
            }
        }
        public void RemoveAurasByType(AuraType auraType, ObjectGuid casterGUID = default(ObjectGuid), Aura except = null, bool negative = true, bool positive = true)
        {
            var list = m_modAuras[auraType];
            for (var i = 0; i < list.Count; i++)
            {
                Aura aura = list[i].GetBase();
                AuraApplication aurApp = aura.GetApplicationOfTarget(GetGUID());

                if (aura != except && (casterGUID.IsEmpty() || aura.GetCasterGUID() == casterGUID)
                    && ((negative && !aurApp.IsPositive()) || (positive && aurApp.IsPositive())))
                {
                    uint removedAuras = m_removedAurasCount;
                    RemoveAura(aurApp);
                    if (m_removedAurasCount > removedAuras + 1)
                        i = 0;
                }
            }
        }
        public void RemoveNotOwnSingleTargetAuras(bool onPhaseChange = false)
        {
            // Iterate m_ownedAuras - aura is marked as single target in Unit::AddAura (and pushed to m_ownedAuras).
            // m_appliedAuras will NOT contain the aura before first Unit::Update after adding it to m_ownedAuras.
            // Quickly removing such an aura will lead to it not being unregistered from caster's single cast auras container
            // leading to assertion failures if the aura was cast on a player that can
            // (and is changing map at the point where this function is called).
            // Such situation occurs when player is logging in inside an instance and fails the entry check for any reason.
            // The aura that was loaded from db (indirectly, via linked casts) gets removed before it has a chance
            // to register in m_appliedAuras
            foreach (var pair in GetOwnedAuras())
            {
                Aura aura = pair.Value;

                if (aura.GetCasterGUID() != GetGUID() && aura.IsSingleTarget())
                {
                    if (onPhaseChange)
                        RemoveOwnedAura(pair);
                    else
                    {
                        Unit caster = aura.GetCaster();
                        if (!caster || !caster.IsInPhase(this))
                            RemoveOwnedAura(pair);
                    }
                }
            }

            // single target auras at other targets
            for (var i = 0; i < m_scAuras.Count; i++)
            {
                var aura = m_scAuras[i];
                if (aura.GetUnitOwner() != this && (!onPhaseChange || !aura.GetUnitOwner().IsInPhase(this)))
                    aura.Remove();
            }
        }
        // All aura base removes should go threw this function!
        public void RemoveOwnedAura(KeyValuePair<uint, Aura> keyValuePair, AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            Aura aura = keyValuePair.Value;

            Cypher.Assert(!aura.IsRemoved());

            m_ownedAuras.Remove(keyValuePair);
            m_removedAuras.Add(aura);

            // Unregister single target aura
            if (aura.IsSingleTarget())
                aura.UnregisterSingleTarget();

            aura._Remove(removeMode);
        }
        public void RemoveOwnedAura(uint spellId, ObjectGuid casterGUID = default(ObjectGuid), uint reqEffMask = 0, AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            foreach (var pair in GetOwnedAuras())
            {
                if (pair.Key != spellId)
                    continue;

                if (((pair.Value.GetEffectMask() & reqEffMask) == reqEffMask) && (casterGUID.IsEmpty() || pair.Value.GetCasterGUID() == casterGUID))
                    RemoveOwnedAura(pair, removeMode);
            }
        }
        public void RemoveOwnedAura(Aura auraToRemove, AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            if (auraToRemove.IsRemoved())
                return;

            Cypher.Assert(auraToRemove.GetOwner() == this);

            if (removeMode == AuraRemoveMode.None)
            {
                Log.outError(LogFilter.Spells, "Unit.RemoveOwnedAura() called with unallowed removeMode AURA_REMOVE_NONE, spellId {0}", auraToRemove.GetId());
                return;
            }

            uint spellId = auraToRemove.GetId();
            foreach (var pair in GetOwnedAuras())
            {
                if (pair.Key != spellId)
                    continue;

                if (pair.Value == auraToRemove)
                {
                    RemoveOwnedAura(pair, removeMode);
                    return;
                }
            }

            Cypher.Assert(false);
        }

        public void RemoveAurasDueToSpell(uint spellId, ObjectGuid casterGUID = default(ObjectGuid), uint reqEffMask = 0, AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            foreach (var pair in GetAppliedAuras())
            {
                if (pair.Key != spellId)
                    continue;

                Aura aura = pair.Value.GetBase();
                if (((aura.GetEffectMask() & reqEffMask) == reqEffMask) && (casterGUID.IsEmpty() || aura.GetCasterGUID() == casterGUID))
                {
                    RemoveAura(pair, removeMode);
                }
            }
        }
        public void RemoveAurasDueToSpellByDispel(uint spellId, uint dispellerSpellId, ObjectGuid casterGUID, Unit dispeller, byte chargesRemoved = 1)
        {
            foreach (var pair in GetOwnedAuras())
            {
                if (pair.Key != spellId)
                    continue;

                Aura aura = pair.Value;
                if (aura.GetCasterGUID() == casterGUID)
                {
                    DispelInfo dispelInfo = new DispelInfo(dispeller, dispellerSpellId, chargesRemoved);

                    // Call OnDispel hook on AuraScript
                    aura.CallScriptDispel(dispelInfo);

                    if (aura.GetSpellInfo().HasAttribute(SpellAttr7.DispelCharges))
                        aura.ModCharges(-dispelInfo.GetRemovedCharges(), AuraRemoveMode.EnemySpell);
                    else
                        aura.ModStackAmount(-dispelInfo.GetRemovedCharges(), AuraRemoveMode.EnemySpell);

                    // Call AfterDispel hook on AuraScript
                    aura.CallScriptAfterDispel(dispelInfo);
                    return;
                }
            }
        }
        public void RemoveAuraFromStack(uint spellId, ObjectGuid casterGUID = default(ObjectGuid), AuraRemoveMode removeMode = AuraRemoveMode.Default, ushort num = 1)
        {
            var range = m_ownedAuras.LookupByKey(spellId);
            foreach (var aura in range)
            {
                if ((aura.GetAuraType() == AuraObjectType.Unit) && (casterGUID.IsEmpty() || aura.GetCasterGUID() == casterGUID))
                {
                    aura.ModStackAmount(-num, removeMode);
                    return;
                }
            }
        }
        public void RemoveAura(KeyValuePair<uint, AuraApplication> appMap, AuraRemoveMode mode = AuraRemoveMode.Default)
        {
            var aurApp = appMap.Value;
            // Do not remove aura which is already being removed
            if (aurApp.HasRemoveMode())
                return;
            Aura aura = aurApp.GetBase();
            _UnapplyAura(appMap, mode);
            // Remove aura - for Area and Target auras
            if (aura.GetOwner() == this)
                aura.Remove(mode);
        }
        public void RemoveAura(uint spellId, ObjectGuid caster = default(ObjectGuid), uint reqEffMask = 0, AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            var range = m_appliedAuras.LookupByKey(spellId);
            foreach (var iter in range)
            {
                Aura aura = iter.GetBase();
                if (((aura.GetEffectMask() & reqEffMask) == reqEffMask) && (caster.IsEmpty() || aura.GetCasterGUID() == caster))
                {
                    RemoveAura(iter, removeMode);
                    return;
                }
            }
        }
        public void RemoveAura(AuraApplication aurApp, AuraRemoveMode mode = AuraRemoveMode.Default)
        {
            // we've special situation here, RemoveAura called while during aura removal
            // this kind of call is needed only when aura effect removal handler
            // or event triggered by it expects to remove
            // not yet removed effects of an aura
            if (aurApp.HasRemoveMode())
            {
                // remove remaining effects of an aura
                for (byte effectIndex = 0; effectIndex < SpellConst.MaxEffects; ++effectIndex)
                {
                    if (aurApp.HasEffect(effectIndex))
                        aurApp._HandleEffect(effectIndex, false);
                }
                return;
            }
            // no need to remove
            if (aurApp.GetBase().GetApplicationOfTarget(GetGUID()) != aurApp || aurApp.GetBase().IsRemoved())
                return;

            uint spellId = aurApp.GetBase().GetId();

            var range = m_appliedAuras.Where(p => p.Key == spellId);
            foreach (var pair in range)
            {
                if (aurApp == pair.Value)
                {
                    RemoveAura(pair, mode);
                    return;
                }
            }
        }
        public void RemoveAura(Aura aura, AuraRemoveMode mode = AuraRemoveMode.Default)
        {
            if (aura.IsRemoved())
                return;
            AuraApplication aurApp = aura.GetApplicationOfTarget(GetGUID());
            if (aurApp != null)
                RemoveAura(aurApp, mode);
        }
        public void RemoveAurasWithAttribute(SpellAttr0 flags)
        {
            foreach (var app in GetAppliedAuras())
            {
                SpellInfo spell = app.Value.GetBase().GetSpellInfo();
                if (spell.HasAttribute(flags))
                    RemoveAura(app);
            }
        }
        public void RemoveAurasWithFamily(SpellFamilyNames family, FlagArray128 familyFlag, ObjectGuid casterGUID)
        {
            foreach (var pair in GetAppliedAuras())
            {
                Aura aura = pair.Value.GetBase();
                if (casterGUID.IsEmpty() || aura.GetCasterGUID() == casterGUID)
                {
                    SpellInfo spell = aura.GetSpellInfo();
                    if (spell.SpellFamilyName == family && spell.SpellFamilyFlags & familyFlag)
                    {
                        RemoveAura(pair);
                        continue;
                    }
                }
            }
        }

        public void RemoveAppliedAuras(Func<AuraApplication, bool> check)
        {
            foreach (var pair in GetAppliedAuras())
            {
                if (check(pair.Value))
                    RemoveAura(pair);
            }
        }

        public void RemoveOwnedAuras(Func<Aura, bool> check)
        {
            foreach (var pair in GetOwnedAuras())
            {
                if (check(pair.Value))
                    RemoveOwnedAura(pair);
            }
        }

        void RemoveAppliedAuras(uint spellId, Func<AuraApplication, bool> check)
        {
            foreach (var app in m_appliedAuras.LookupByKey(spellId))
            {
                if (check(app))
                    RemoveAura(app);
            }
        }

        void RemoveOwnedAuras(uint spellId, Func<Aura, bool> check)
        {
            foreach (var aura in m_ownedAuras.LookupByKey(spellId))
            {
                if (check(aura))
                    RemoveOwnedAura(aura);
            }
        }

        public void RemoveAurasByType(AuraType auraType, Func<AuraApplication, bool> check)
        {
            var list = m_modAuras[auraType];
            for (var i = 0; i < list.Count; ++i)
            {
                Aura aura = m_modAuras[auraType][i].GetBase();
                AuraApplication aurApp = aura.GetApplicationOfTarget(GetGUID());
                Cypher.Assert(aurApp != null);

                if (check(aurApp))
                {
                    uint removedAuras = m_removedAurasCount;
                    RemoveAura(aurApp);
                    if (m_removedAurasCount > removedAuras + 1)
                        i = 0;
                }
            }
        }

        void RemoveAreaAurasDueToLeaveWorld()
        {
            // make sure that all area auras not applied on self are removed
            foreach (var pair in GetOwnedAuras())
            {
                var appMap = pair.Value.GetApplicationMap();
                foreach (var aurApp in appMap.Values)
                {
                    Unit target = aurApp.GetTarget();
                    if (target == this)
                        continue;
                    target.RemoveAura(aurApp);
                    // things linked on aura remove may apply new area aura - so start from the beginning
                }
            }

            // remove area auras owned by others
            foreach (var pair in GetAppliedAuras())
            {
                if (pair.Value.GetBase().GetOwner() != this)
                    RemoveAura(pair);
            }
        }
        public void RemoveAllAuras()
        {
            // this may be a dead loop if some events on aura remove will continiously apply aura on remove
            // we want to have all auras removed, so use your brain when linking events
            while (!m_appliedAuras.Empty())
                _UnapplyAura(m_appliedAuras.FirstOrDefault(), AuraRemoveMode.Default);

            while (!m_ownedAuras.Empty())
                RemoveOwnedAura(m_ownedAuras.FirstOrDefault());
        }
        public void RemoveArenaAuras()
        {
            // in join, remove positive buffs, on end, remove negative
            // used to remove positive visible auras in arenas
            RemoveAppliedAuras(aurApp =>
            {
                Aura aura = aurApp.GetBase();
                return !aura.GetSpellInfo().HasAttribute(SpellAttr4.Unk21) // don't remove stances, shadowform, pally/hunter auras
                    && !aura.IsPassive()                               // don't remove passive auras
                    && (aurApp.IsPositive() || !aura.GetSpellInfo().HasAttribute(SpellAttr3.DeathPersistent)); // not negative death persistent auras
            });
        }
        public void RemoveAllAurasExceptType(AuraType type)
        {
            foreach (var pair in GetAppliedAuras())
            {
                if (pair.Value == null)
                    continue;
                Aura aura = pair.Value.GetBase();
                if (!aura.GetSpellInfo().HasAura(GetMap().GetDifficultyID(), type))
                    _UnapplyAura(pair, AuraRemoveMode.Default);
            }

            foreach (var pair in GetOwnedAuras())
            {
                if (pair.Value == null)
                    continue;

                Aura aura = pair.Value;
                if (!aura.GetSpellInfo().HasAura(GetMap().GetDifficultyID(), type))
                    RemoveOwnedAura(pair, AuraRemoveMode.Default);
            }
        }
        public void RemoveAllAurasExceptType(AuraType type1, AuraType type2)
        {
            foreach (var pair in GetAppliedAuras())
            {
                Aura aura = pair.Value.GetBase();
                if (!aura.GetSpellInfo().HasAura(GetMap().GetDifficultyID(), type1) || !aura.GetSpellInfo().HasAura(GetMap().GetDifficultyID(), type2))
                    _UnapplyAura(pair, AuraRemoveMode.Default);
            }

            foreach (var pair in GetOwnedAuras())
            {
                Aura aura = pair.Value;
                if (!aura.GetSpellInfo().HasAura(GetMap().GetDifficultyID(), type1) || !aura.GetSpellInfo().HasAura(GetMap().GetDifficultyID(), type2))
                    RemoveOwnedAura(pair, AuraRemoveMode.Default);
            }
        }

        public void ModifyAuraState(AuraStateType flag, bool apply)
        {
            if (apply)
            {
                if (!HasFlag(UnitFields.AuraState, (1u << ((int)flag - 1))))
                {
                    SetFlag(UnitFields.AuraState, (1u << (int)flag - 1));
                    if (IsTypeId(TypeId.Player))
                    {
                        var sp_list = ToPlayer().GetSpellMap();
                        foreach (var spell in sp_list)
                        {
                            if (spell.Value.State == PlayerSpellState.Removed || spell.Value.Disabled)
                                continue;

                            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell.Key);
                            if (spellInfo == null || !spellInfo.IsPassive())
                                continue;

                            if (spellInfo.CasterAuraState == flag)
                                CastSpell(this, spell.Key, true, null);
                        }
                    }
                    else if (IsPet())
                    {
                        Pet pet = ToPet();
                        foreach (var spell in pet.m_spells)
                        {
                            if (spell.Value.state == PetSpellState.Removed)
                                continue;
                            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell.Key);
                            if (spellInfo == null || !spellInfo.IsPassive())
                                continue;
                            if (spellInfo.CasterAuraState == flag)
                                CastSpell(this, spell.Key, true, null);
                        }
                    }
                }
            }
            else
            {
                if (HasFlag(UnitFields.AuraState, (1u << (int)flag - 1)))
                {
                    RemoveFlag(UnitFields.AuraState, (1u << (int)flag - 1));

                    foreach (var app in GetAppliedAuras())
                    {
                        if (app.Value == null)
                            continue;

                        SpellInfo spellProto = app.Value.GetBase().GetSpellInfo();
                        if (app.Value.GetBase().GetCasterGUID() == GetGUID() && spellProto.CasterAuraState == flag && (spellProto.IsPassive() || flag != AuraStateType.Enrage))
                            RemoveAura(app);
                    }
                }
            }
        }
        public bool HasAuraState(AuraStateType flag, SpellInfo spellProto = null, Unit caster = null)
        {
            if (caster != null)
            {
                if (spellProto != null)
                {
                    if (caster.HasAuraTypeWithAffectMask(AuraType.AbilityIgnoreAurastate, spellProto))
                        return true;
                }

                // Check per caster aura state
                // If aura with aurastate by caster not found return false
                if (Convert.ToBoolean((1 << (int)flag) & (int)AuraStateType.PerCasterAuraStateMask))
                {
                    var range = m_auraStateAuras.LookupByKey(flag);
                    foreach (var auraApp in range)
                        if (auraApp.GetBase().GetCasterGUID() == caster.GetGUID())
                            return true;
                    return false;
                }
            }

            return HasFlag(UnitFields.AuraState, (uint)(1 << (int)flag - 1));
        }

        SpellSchools GetSpellSchoolByAuraGroup(UnitMods unitMod)
        {
            SpellSchools school = SpellSchools.Normal;

            switch (unitMod)
            {
                case UnitMods.ResistanceHoly:
                    school = SpellSchools.Holy;
                    break;
                case UnitMods.ResistanceFire:
                    school = SpellSchools.Fire;
                    break;
                case UnitMods.ResistanceNature:
                    school = SpellSchools.Nature;
                    break;
                case UnitMods.ResistanceFrost:
                    school = SpellSchools.Frost;
                    break;
                case UnitMods.ResistanceShadow:
                    school = SpellSchools.Shadow;
                    break;
                case UnitMods.ResistanceArcane:
                    school = SpellSchools.Arcane;
                    break;
            }

            return school;
        }

        public void _ApplyAllAuraStatMods()
        {
            foreach (var i in GetAppliedAuras())
                i.Value.GetBase().HandleAllEffects(i.Value, AuraEffectHandleModes.Stat, true);
        }
        public void _RemoveAllAuraStatMods()
        {
            foreach (var i in GetAppliedAuras())
                i.Value.GetBase().HandleAllEffects(i.Value, AuraEffectHandleModes.Stat, false);
        }

        // removes aura application from lists and unapplies effects
        public void _UnapplyAura(KeyValuePair<uint, AuraApplication> pair, AuraRemoveMode removeMode)
        {
            AuraApplication aurApp = pair.Value;
            Cypher.Assert(aurApp != null);
            Cypher.Assert(!aurApp.HasRemoveMode());
            Cypher.Assert(aurApp.GetTarget() == this);

            aurApp.SetRemoveMode(removeMode);
            Aura aura = aurApp.GetBase();
            Log.outDebug(LogFilter.Spells, "Aura {0} now is remove mode {1}", aura.GetId(), removeMode);

            // dead loop is killing the server probably
            Cypher.Assert(m_removedAurasCount < 0xFFFFFFFF);

            ++m_removedAurasCount;

            Unit caster = aura.GetCaster();

            m_appliedAuras.Remove(pair);

            if (aura.GetSpellInfo().HasAnyAuraInterruptFlag())
            {
                m_interruptableAuras.Remove(aurApp);
                UpdateInterruptMask();
            }

            bool auraStateFound = false;
            AuraStateType auraState = aura.GetSpellInfo().GetAuraState();
            if (auraState != 0)
            {
                bool canBreak = false;
                // Get mask of all aurastates from remaining auras
                var list = m_auraStateAuras.LookupByKey(auraState);
                for (var i = 0; i < list.Count && !(auraStateFound && canBreak);)
                {
                    if (list[i] == aurApp)
                    {
                        m_auraStateAuras.Remove(auraState, list[i]);
                        list = m_auraStateAuras.LookupByKey(auraState);
                        i = 0;
                        canBreak = true;
                        continue;
                    }
                    auraStateFound = true;
                    ++i;
                }
            }

            aurApp._Remove();
            aura._UnapplyForTarget(this, caster, aurApp);

            // remove effects of the spell - needs to be done after removing aura from lists
            for (byte c = 0; c < SpellConst.MaxEffects; ++c)
            {
                if (aurApp.HasEffect(c))
                    aurApp._HandleEffect(c, false);
            }

            // all effect mustn't be applied
            Cypher.Assert(aurApp.GetEffectMask() == 0);

            // Remove totem at next update if totem loses its aura
            if (aurApp.GetRemoveMode() == AuraRemoveMode.Expire && IsTypeId(TypeId.Unit) && IsTotem())
            {
                if (ToTotem().GetSpell() == aura.GetId() && ToTotem().GetTotemType() == TotemType.Passive)
                    ToTotem().setDeathState(DeathState.JustDied);
            }

            // Remove aurastates only if were not found
            if (!auraStateFound)
                ModifyAuraState(auraState, false);

            aura.HandleAuraSpecificMods(aurApp, caster, false, false);
        }

        public void _UnapplyAura(AuraApplication aurApp, AuraRemoveMode removeMode)
        {
            // aura can be removed from unit only if it's applied on it, shouldn't happen
            Cypher.Assert(aurApp.GetBase().GetApplicationOfTarget(GetGUID()) == aurApp);

            uint spellId = aurApp.GetBase().GetId();
            var range = m_appliedAuras.LookupByKey(spellId);

            foreach (var app in range)
            {
                if (app == aurApp)
                {
                    _UnapplyAura(new KeyValuePair<uint, AuraApplication>(spellId, app), removeMode);
                    return;
                }
            }
            Cypher.Assert(false);
        }

        public AuraEffect GetAuraEffect(uint spellId, uint effIndex, ObjectGuid casterGUID = default(ObjectGuid))
        {
            var range = m_appliedAuras.LookupByKey(spellId);
            if (!range.Empty())
            {
                foreach (var aura in range)
                {
                    if (aura.HasEffect(effIndex)
                            && (casterGUID.IsEmpty() || aura.GetBase().GetCasterGUID() == casterGUID))
                    {
                        return aura.GetBase().GetEffect(effIndex);
                    }
                }
            }
            return null;
        }
        public AuraEffect GetAuraEffectOfRankedSpell(uint spellId, uint effIndex, ObjectGuid casterGUID = default(ObjectGuid))
        {
            uint rankSpell = Global.SpellMgr.GetFirstSpellInChain(spellId);
            while (rankSpell != 0)
            {
                AuraEffect aurEff = GetAuraEffect(rankSpell, effIndex, casterGUID);
                if (aurEff != null)
                    return aurEff;
                rankSpell = Global.SpellMgr.GetNextSpellInChain(rankSpell);
            }
            return null;
        }
        
        // spell mustn't have familyflags
        public AuraEffect GetAuraEffect(AuraType type, SpellFamilyNames family, FlagArray128 familyFlag, ObjectGuid casterGUID = default(ObjectGuid))
        {
            var auras = GetAuraEffectsByType(type);
            foreach (var aura in auras)
            {
                SpellInfo spell = aura.GetSpellInfo();
                if (spell.SpellFamilyName == family && spell.SpellFamilyFlags & familyFlag)
                {
                    if (!casterGUID.IsEmpty() && aura.GetCasterGUID() != casterGUID)
                        continue;
                    return aura;
                }
            }
            return null;

        }

        public AuraApplication GetAuraApplication(uint spellId, ObjectGuid casterGUID = default(ObjectGuid), ObjectGuid itemCasterGUID = default(ObjectGuid), uint reqEffMask = 0, AuraApplication except = null)
        {
            var range = m_appliedAuras.LookupByKey(spellId);
            if (!range.Empty())
            {
                foreach (var app in range)
                {
                    Aura aura = app.GetBase();
                    if (((aura.GetEffectMask() & reqEffMask) == reqEffMask) && (casterGUID.IsEmpty() || aura.GetCasterGUID() == casterGUID)
                        && (itemCasterGUID.IsEmpty() || aura.GetCastItemGUID() == itemCasterGUID) && (except == null || except != app))
                    {
                        return app;
                    }
                }
            }
            return null;
        }
        public Aura GetAura(uint spellId, ObjectGuid casterGUID = default(ObjectGuid), ObjectGuid itemCasterGUID = default(ObjectGuid), uint reqEffMask = 0)
        {
            AuraApplication aurApp = GetAuraApplication(spellId, casterGUID, itemCasterGUID, reqEffMask);
            return aurApp?.GetBase();
        }

        uint BuildAuraStateUpdateForTarget(Unit target)
        {
            uint auraStates = GetUInt32Value(UnitFields.AuraState) & ~(uint)AuraStateType.PerCasterAuraStateMask;
            foreach (var state in m_auraStateAuras)
                if (Convert.ToBoolean((1 << (int)state.Key - 1) & (uint)AuraStateType.PerCasterAuraStateMask))
                    if (state.Value.GetBase().GetCasterGUID() == target.GetGUID())
                        auraStates |= (uint)(1 << (int)state.Key - 1);

            return auraStates;
        }

        public bool CanProc() { return m_procDeep == 0; }

        public void _ApplyAuraEffect(Aura aura, uint effIndex)
        {
            Cypher.Assert(aura != null);
            Cypher.Assert(aura.HasEffect(effIndex));
            AuraApplication aurApp = aura.GetApplicationOfTarget(GetGUID());
            Cypher.Assert(aurApp != null);
            if (aurApp.GetEffectMask() == 0)
                _ApplyAura(aurApp, (uint)(1 << (int)effIndex));
            else
                aurApp._HandleEffect(effIndex, true);
        }
        // handles effects of aura application
        // should be done after registering aura in lists
        public void _ApplyAura(AuraApplication aurApp, uint effMask)
        {
            Aura aura = aurApp.GetBase();

            _RemoveNoStackAurasDueToAura(aura);

            if (aurApp.HasRemoveMode())
                return;

            // Update target aura state flag
            AuraStateType aState = aura.GetSpellInfo().GetAuraState();
            if (aState != 0)
                ModifyAuraState(aState, true);

            if (aurApp.HasRemoveMode())
                return;

            // Sitdown on apply aura req seated
            if (aura.GetSpellInfo().HasAuraInterruptFlag(SpellAuraInterruptFlags.NotSeated) && !IsSitState())
                SetStandState(UnitStandStateType.Sit);

            Unit caster = aura.GetCaster();

            if (aurApp.HasRemoveMode())
                return;

            aura.HandleAuraSpecificPeriodics(aurApp, caster);
            aura.HandleAuraSpecificMods(aurApp, caster, true, false);

            // apply effects of the aura
            for (byte i = 0; i < SpellConst.MaxEffects; i++)
            {
                if (Convert.ToBoolean(effMask & 1 << i) && !(aurApp.HasRemoveMode()))
                    aurApp._HandleEffect(i, true);
            }
        }
        public void _AddAura(UnitAura aura, Unit caster)
        {
            Cypher.Assert(!m_cleanupDone);
            m_ownedAuras.Add(aura.GetId(), aura);

            _RemoveNoStackAurasDueToAura(aura);

            if (aura.IsRemoved())
                return;

            aura.SetIsSingleTarget(caster != null && aura.GetSpellInfo().IsSingleTarget() || aura.HasEffectType(AuraType.ControlVehicle));
            if (aura.IsSingleTarget())
            {

                // @HACK: Player is not in world during loading auras.
                //Single target auras are not saved or loaded from database
                //but may be created as a result of aura links (player mounts with passengers)
                Cypher.Assert((IsInWorld && !IsDuringRemoveFromWorld()) || (aura.GetCasterGUID() == GetGUID()) || (IsLoading() && aura.HasEffectType(AuraType.ControlVehicle)));

                // register single target aura
                caster.m_scAuras.Add(aura);
                // remove other single target auras
                var scAuras = caster.GetSingleCastAuras();
                for (var i = 0; i < scAuras.Count; ++i)
                {
                    var aur = scAuras[i];
                    if (aur != aura && aur.IsSingleTargetWith(aura))
                        aur.Remove();

                }
            }
        }
        public Aura _TryStackingOrRefreshingExistingAura(SpellInfo newAura, uint effMask, Unit caster, int[] baseAmount = null, Item castItem = null, ObjectGuid casterGUID = default(ObjectGuid), bool resetPeriodicTimer = true, ObjectGuid castItemGuid = default(ObjectGuid), int castItemLevel = -1)
        {
            Cypher.Assert(!casterGUID.IsEmpty() || caster);

            // Check if these can stack anyway
            if (casterGUID.IsEmpty() && !newAura.IsStackableOnOneSlotWithDifferentCasters())
                casterGUID = caster.GetGUID();

            // passive and Incanter's Absorption and auras with different type can stack with themselves any number of times
            if (!newAura.IsMultiSlotAura())
            {
                // check if cast item changed
                if (castItem != null)
                {
                    castItemGuid = castItem.GetGUID();
                    Player owner = castItem.GetOwner();
                    if (owner)
                        castItemLevel = (int)castItem.GetItemLevel(owner);
                    else if (castItem.GetOwnerGUID() == caster.GetGUID())
                        castItemLevel = (int)castItem.GetItemLevel(caster.ToPlayer());
                }

                // find current aura from spell and change it's stackamount, or refresh it's duration
                Aura foundAura = GetOwnedAura(newAura.Id, casterGUID, (newAura.HasAttribute(SpellCustomAttributes.EnchantProc) ? castItemGuid : ObjectGuid.Empty), 0);
                if (foundAura != null)
                {
                    // effect masks do not match
                    // extremely rare case
                    // let's just recreate aura
                    if (effMask != foundAura.GetEffectMask())
                        return null;

                    // update basepoints with new values - effect amount will be recalculated in ModStackAmount
                    foreach (SpellEffectInfo effect in foundAura.GetSpellEffectInfos())
                    {
                        if (effect == null)
                            continue;

                        AuraEffect eff = foundAura.GetEffect(effect.EffectIndex);
                        if (eff == null)
                            continue;

                        int bp;
                        if (baseAmount != null)
                            bp = baseAmount[effect.EffectIndex];
                        else
                            bp = effect.BasePoints;

                        eff.m_baseAmount = bp;
                    }

                    // correct cast item guid if needed
                    if (castItemGuid != foundAura.GetCastItemGUID())
                    {
                        foundAura.SetCastItemGUID(castItemGuid);
                        foundAura.SetCastItemLevel(castItemLevel);
                    }

                    // try to increase stack amount
                    foundAura.ModStackAmount(1, AuraRemoveMode.Default, resetPeriodicTimer);
                    return foundAura;
                }
            }

            return null;
        }

        void _RemoveNoStackAurasDueToAura(Aura aura)
        {
            SpellInfo spellProto = aura.GetSpellInfo();

            // passive spell special case (only non stackable with ranks)
            if (spellProto.IsPassiveStackableWithRanks())
                return;

            if (!IsHighestExclusiveAura(aura))
            {
                if (!aura.GetSpellInfo().IsAffectingArea(GetMap().GetDifficultyID()))
                {
                    Unit caster = aura.GetCaster();
                    if (caster && caster.IsTypeId(TypeId.Player))
                        Spell.SendCastResult(caster.ToPlayer(), aura.GetSpellInfo(), aura.GetSpellXSpellVisualId(), aura.GetCastGUID(), SpellCastResult.AuraBounced);
                }

                aura.Remove();
                return;
            }

            bool remove = false;
            for (var i = 0; i < m_appliedAuras.KeyValueList.Count; i++)
            {
                var app = m_appliedAuras.KeyValueList[i];
                if (remove)
                {
                    remove = false;
                    i = 0;
                }

                if (aura.CanStackWith(app.Value.GetBase()))
                    continue;

                RemoveAura(app, AuraRemoveMode.Default);
                if (i == m_appliedAuras.KeyValueList.Count - 1)
                    break;

                remove = true;
            }
        }
        public int GetHighestExclusiveSameEffectSpellGroupValue(AuraEffect aurEff, AuraType auraType, bool checkMiscValue = false, int miscValue = 0)
        {
            int val = 0;
            var spellGroupList = Global.SpellMgr.GetSpellSpellGroupMapBounds(aurEff.GetSpellInfo().GetFirstRankSpell().Id);
            foreach (var spellGroup in spellGroupList)
            {
                if (Global.SpellMgr.GetSpellGroupStackRule(spellGroup) == SpellGroupStackRule.ExclusiveSameEffect)
                {
                    var auraEffList = GetAuraEffectsByType(auraType);
                    foreach (var auraEffect in auraEffList)
                    {
                        if (aurEff != auraEffect && (!checkMiscValue || auraEffect.GetMiscValue() == miscValue) &&
                            Global.SpellMgr.IsSpellMemberOfSpellGroup(auraEffect.GetSpellInfo().Id, spellGroup))
                        {
                            // absolute value only
                            if (Math.Abs(val) < Math.Abs(auraEffect.GetAmount()))
                                val = auraEffect.GetAmount();
                        }
                    }
                }
            }
            return val;
        }

        void UpdateLastDamagedTime(SpellInfo spellProto)
        {
            if (!IsTypeId(TypeId.Unit) || IsPet())
                return;

            if (spellProto != null && spellProto.HasAura(Difficulty.None, AuraType.DamageShield))
                return;

            SetLastDamagedTime(Time.UnixTime);
        }

        public bool IsHighestExclusiveAura(Aura aura, bool removeOtherAuraApplications = false)
        {
            foreach (AuraEffect aurEff in aura.GetAuraEffects())
            {
                if (aurEff == null)
                    continue;

                AuraType auraType = aurEff.GetSpellEffectInfo().ApplyAuraName;
                var auras = GetAuraEffectsByType(auraType);
                for (var i = 0; i < auras.Count;)
                {
                    AuraEffect existingAurEff = auras[i];
                    ++i;

                    if (Global.SpellMgr.CheckSpellGroupStackRules(aura.GetSpellInfo(), existingAurEff.GetSpellInfo()) == SpellGroupStackRule.ExclusiveHighest)
                    {
                        int diff = Math.Abs(aurEff.GetAmount()) - Math.Abs(existingAurEff.GetAmount());
                        if (diff == 0)
                            diff = (int)(aura.GetEffectMask() - existingAurEff.GetBase().GetEffectMask());

                        if (diff > 0)
                        {
                            Aura auraBase = existingAurEff.GetBase();
                            // no removing of area auras from the original owner, as that completely cancels them
                            if (removeOtherAuraApplications && (!auraBase.IsArea() || auraBase.GetOwner() != this))
                            {
                                AuraApplication aurApp = existingAurEff.GetBase().GetApplicationOfTarget(GetGUID());
                                if (aurApp != null)
                                {
                                    bool hasMoreThanOneEffect = auraBase.HasMoreThanOneEffectForType(auraType);
                                    uint removedAuras = m_removedAurasCount;
                                    RemoveAura(aurApp);
                                    if (hasMoreThanOneEffect || m_removedAurasCount > removedAuras + 1)
                                        i = 0;
                                }
                            }
                        }
                        else if (diff < 0)
                            return false;
                    }
                }

            }

            return true;
        }

        public Aura GetOwnedAura(uint spellId, ObjectGuid casterGUID = default(ObjectGuid), ObjectGuid itemCasterGUID = default(ObjectGuid), uint reqEffMask = 0, Aura except = null)
        {
            var range = m_ownedAuras.LookupByKey(spellId);
            foreach (var aura in range)
            {
                if (((aura.GetEffectMask() & reqEffMask) == reqEffMask) && (casterGUID.IsEmpty() || aura.GetCasterGUID() == casterGUID)
                    && (itemCasterGUID.IsEmpty() || aura.GetCastItemGUID() == itemCasterGUID) && (except == null || except != aura))
                {
                    return aura;
                }
            }
            return null;
        }

        public List<AuraEffect> GetAuraEffectsByType(AuraType type)
        {
            return m_modAuras.LookupByKey(type);
        }

        public int GetTotalAuraModifier(AuraType auratype)
        {
            return GetTotalAuraModifier(auratype, aurEff => true);
        }

        public int GetTotalAuraModifier(AuraType auratype, Func<AuraEffect, bool> predicate)
        {
            Dictionary<SpellGroup, int> sameEffectSpellGroup = new Dictionary<SpellGroup, int>();
            int modifier = 0;

            var mTotalAuraList = GetAuraEffectsByType(auratype);
            foreach (AuraEffect aurEff in mTotalAuraList)
            {
                if (predicate(aurEff))
                {
                    // Check if the Aura Effect has a the Same Effect Stack Rule and if so, use the highest amount of that SpellGroup
                    // If the Aura Effect does not have this Stack Rule, it returns false so we can add to the multiplier as usual
                    if (!Global.SpellMgr.AddSameEffectStackRuleSpellGroups(aurEff.GetSpellInfo(), aurEff.GetAmount(), sameEffectSpellGroup))
                        modifier += aurEff.GetAmount();
                }
            }

            // Add the highest of the Same Effect Stack Rule SpellGroups to the accumulator
            foreach (var pair in sameEffectSpellGroup)
                modifier += pair.Value;

            return modifier;
        }

        public float GetTotalAuraMultiplier(AuraType auratype)
        {
            return GetTotalAuraMultiplier(auratype, aurEff => true);
        }

        public float GetTotalAuraMultiplier(AuraType auratype, Func<AuraEffect, bool> predicate)
        {
            var mTotalAuraList = GetAuraEffectsByType(auratype);
            if (mTotalAuraList.Empty())
                return 1.0f;

            Dictionary<SpellGroup, int> sameEffectSpellGroup = new Dictionary<SpellGroup, int>();
            float multiplier = 1.0f;

            foreach (var aurEff in mTotalAuraList)
            {
                if (predicate(aurEff))
                {
                    // Check if the Aura Effect has a the Same Effect Stack Rule and if so, use the highest amount of that SpellGroup
                    // If the Aura Effect does not have this Stack Rule, it returns false so we can add to the multiplier as usual
                    if (!Global.SpellMgr.AddSameEffectStackRuleSpellGroups(aurEff.GetSpellInfo(), aurEff.GetAmount(), sameEffectSpellGroup))
                        MathFunctions.AddPct(ref multiplier, aurEff.GetAmount());
                }
            }

            // Add the highest of the Same Effect Stack Rule SpellGroups to the multiplier
            foreach (var pair in sameEffectSpellGroup)
                MathFunctions.AddPct(ref multiplier, pair.Value);

            return multiplier;
        }

        public int GetMaxPositiveAuraModifier(AuraType auratype)
        {
            return GetMaxPositiveAuraModifier(auratype, aurEff => true);
        }

        public int GetMaxPositiveAuraModifier(AuraType auratype, Func<AuraEffect, bool> predicate)
        {
            var mTotalAuraList = GetAuraEffectsByType(auratype);
            if (mTotalAuraList.Empty())
                return 0;

            int modifier = 0;
            foreach (var aurEff in mTotalAuraList)
            {
                if (predicate(aurEff))
                    modifier = Math.Max(modifier, aurEff.GetAmount());
            }

            return modifier;
        }

        public int GetMaxNegativeAuraModifier(AuraType auratype)
        {
            return GetMaxNegativeAuraModifier(auratype, aurEff => true);
        }

        public int GetMaxNegativeAuraModifier(AuraType auratype, Func<AuraEffect, bool> predicate)
        {
            var mTotalAuraList = GetAuraEffectsByType(auratype);
            if (mTotalAuraList.Empty())
                return 0;

            int modifier = 0;
            foreach (var aurEff in mTotalAuraList)
                if (predicate(aurEff))
                    modifier = Math.Min(modifier, aurEff.GetAmount());

            return modifier;
        }

        public int GetTotalAuraModifierByMiscMask(AuraType auratype, int miscMask)
        {
            return GetTotalAuraModifier(auratype, aurEff =>
            {
                if ((aurEff.GetMiscValue() & miscMask) != 0)
                    return true;
                return false;
            });
        }

        public float GetTotalAuraMultiplierByMiscMask(AuraType auratype, uint miscMask)
        {
            return GetTotalAuraMultiplier(auratype, aurEff =>
            {
                if ((aurEff.GetMiscValue() & miscMask) != 0)
                    return true;
                return false;
            });
        }

        public int GetMaxPositiveAuraModifierByMiscMask(AuraType auratype, uint miscMask, AuraEffect except = null)
        {
            return GetMaxPositiveAuraModifier(auratype, aurEff =>
            {
                if (except != aurEff && (aurEff.GetMiscValue() & miscMask) != 0)
                    return true;
                return false;
            });
        }

        public int GetMaxNegativeAuraModifierByMiscMask(AuraType auratype, uint miscMask)
        {
            return GetMaxNegativeAuraModifier(auratype, aurEff =>
            {
                if ((aurEff.GetMiscValue() & miscMask) != 0)
                    return true;
                return false;
            });
        }

        public int GetTotalAuraModifierByMiscValue(AuraType auratype, int miscValue)
        {
            return GetTotalAuraModifier(auratype, aurEff =>
            {
                if (aurEff.GetMiscValue() == miscValue)
                    return true;
                return false;
            });
        }

        public float GetTotalAuraMultiplierByMiscValue(AuraType auratype, int miscValue)
        {
            return GetTotalAuraMultiplier(auratype, aurEff =>
            {
                if (aurEff.GetMiscValue() == miscValue)
                    return true;
                return false;
            });
        }

        int GetMaxPositiveAuraModifierByMiscValue(AuraType auratype, int miscValue)
        {
            return GetMaxPositiveAuraModifier(auratype, aurEff =>
            {
                if (aurEff.GetMiscValue() == miscValue)
                    return true;
                return false;
            });
        }

        int GetMaxNegativeAuraModifierByMiscValue(AuraType auratype, int miscValue)
        {
            return GetMaxNegativeAuraModifier(auratype, aurEff =>
            {
                if (aurEff.GetMiscValue() == miscValue)
                    return true;
                return false;
            });
        }


        public void _RegisterAuraEffect(AuraEffect aurEff, bool apply)
        {
            if (apply)
                m_modAuras.Add(aurEff.GetAuraType(), aurEff);
            else
                m_modAuras.Remove(aurEff.GetAuraType(), aurEff);
        }
        public float GetTotalAuraModValue(UnitMods unitMod)
        {
            if (unitMod >= UnitMods.End)
            {
                Log.outError(LogFilter.Unit, "attempt to access non-existing UnitMods in GetTotalAuraModValue()!");
                return 0.0f;
            }

            if (m_auraModifiersGroup[(int)unitMod][(int)UnitModifierType.TotalPCT] <= 0.0f)
                return 0.0f;

            float value = MathFunctions.CalculatePct(m_auraModifiersGroup[(int)unitMod][(int)UnitModifierType.BaseValue], Math.Max(m_auraModifiersGroup[(int)unitMod][(int)UnitModifierType.BasePCTExcludeCreate], -100.0f));
            value *= m_auraModifiersGroup[(int)unitMod][(int)UnitModifierType.BasePCT];
            value += m_auraModifiersGroup[(int)unitMod][(int)UnitModifierType.TotalValue];
            value *= m_auraModifiersGroup[(int)unitMod][(int)UnitModifierType.TotalPCT];

            return value;
        }

        public void SetVisibleAura(AuraApplication aurApp)
        {
            m_visibleAuras.Add(aurApp);
            m_visibleAurasToUpdate.Add(aurApp);
            UpdateAuraForGroup();
        }

        public void RemoveVisibleAura(AuraApplication aurApp)
        {
            m_visibleAuras.Remove(aurApp);
            m_visibleAurasToUpdate.Remove(aurApp);
            UpdateAuraForGroup();
        }

        void UpdateAuraForGroup()
        {
            Player player = ToPlayer();
            if (player != null)
            {
                if (player.GetGroup() != null)
                    player.SetGroupUpdateFlag(GroupUpdateFlags.Auras);
            }
            else if (IsPet())
            {
                Pet pet = ToPet();
                if (pet.isControlled())
                    pet.SetGroupUpdateFlag(GroupUpdatePetFlags.Auras);
            }
        }

        public SortedSet<AuraApplication> GetVisibleAuras() { return m_visibleAuras; }
        public bool HasVisibleAura(AuraApplication aurApp) { return m_visibleAuras.Contains(aurApp); }
        public void SetVisibleAuraUpdate(AuraApplication aurApp) { m_visibleAurasToUpdate.Add(aurApp); }
    }
}
