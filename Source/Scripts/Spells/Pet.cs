// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using static Global;

namespace Scripts.Spells.Pets
{

    struct SpellIds
    {
        //HunterPetCalculate
        public const uint TamedPetPassive06 = 19591;
        public const uint TamedPetPassive07 = 20784;
        public const uint TamedPetPassive08 = 34666;
        public const uint TamedPetPassive09 = 34667;
        public const uint TamedPetPassive10 = 34675;
        public const uint HunterPetScaling01 = 34902;
        public const uint HunterPetScaling02 = 34903;
        public const uint HunterPetScaling03 = 34904;
        public const uint HunterPetScaling04 = 61017;
        public const uint HunterAnimalHandler = 34453;

        //WarlockPetCalculate    
        public const uint PetPassiveCrit = 35695;
        public const uint PetPassiveDamageTaken = 35697;
        public const uint WarlockPetScaling01 = 34947;
        public const uint WarlockPetScaling02 = 34956;
        public const uint WarlockPetScaling03 = 34957;
        public const uint WarlockPetScaling04 = 34958;
        public const uint WarlockPetScaling05 = 61013;
        public const uint WarlockGlyphOfVoidwalker = 56247;

        //DKPetCalculate    
        public const uint DeathKnightRuneWeapon02 = 51906;
        public const uint DeathKnightPetScaling01 = 54566;
        public const uint DeathKnightPetScaling02 = 51996;
        public const uint DeathKnightPetScaling03 = 61697;
        public const uint NightOfTheDead = 55620;

        //ShamanPetCalculate    
        public const uint FeralSpiritPetUnk01 = 35674;
        public const uint FeralSpiritPetUnk02 = 35675;
        public const uint FeralSpiritPetUnk03 = 35676;
        public const uint FeralSpiritPetScaling04 = 61783;

        //MiscPetCalculate    
        public const uint MagePetPassiveElemental = 44559;
        public const uint PetHealthScaling = 61679;
        public const uint PetUnk01 = 67561;
        public const uint PetUnk02 = 67557;
    }

    struct CreatureIds
    {
        //WarlockPetCalculate   
        public const uint EntryFelguard = 17252;
        public const uint EntryVoidwalker = 1860;
        public const uint EntryFelhunter = 417;
        public const uint EntrySuccubus = 1863;
        public const uint EntryImp = 416;

        //DKPetCalculate 
        public const uint EntryArmyOfTheDeadGhoul = 24207;
    }

    [Script]
    class spell_gen_pet_calculate : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateAmountCritSpell(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float critSpell = 5.0f;
                // Increase crit from AuraType.ModSpellCritChance
                critSpell += owner.GetTotalAuraModifier(AuraType.ModSpellCritChance);
                // Increase crit from AuraType.ModCritPct
                critSpell += owner.GetTotalAuraModifier(AuraType.ModCritPct);
                // Increase crit spell from spell crit ratings
                critSpell += owner.GetRatingBonusValue(CombatRating.CritSpell);

                amount += (int)critSpell;
            }
        }

        void CalculateAmountCritMelee(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float critMelee = 5.0f;
                // Increase crit from AuraType.ModWeaponCritPercent
                critMelee += owner.GetTotalAuraModifier(AuraType.ModWeaponCritPercent);
                // Increase crit from AuraType.ModCritPct
                critMelee += owner.GetTotalAuraModifier(AuraType.ModCritPct);
                // Increase crit melee from melee crit ratings
                critMelee += owner.GetRatingBonusValue(CombatRating.CritMelee);

                amount += (int)critMelee;
            }
        }

        void CalculateAmountMeleeHit(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float hitMelee = 0.0f;
                // Increase hit from AuraType.ModHitChance
                hitMelee += owner.GetTotalAuraModifier(AuraType.ModHitChance);
                // Increase hit melee from meele hit ratings
                hitMelee += owner.GetRatingBonusValue(CombatRating.HitMelee);

                amount += (int)hitMelee;
            }
        }

        void CalculateAmountSpellHit(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float hitSpell = 0.0f;
                // Increase hit from AuraType.ModSpellHitChance
                hitSpell += owner.GetTotalAuraModifier(AuraType.ModSpellHitChance);
                // Increase hit spell from spell hit ratings
                hitSpell += owner.GetRatingBonusValue(CombatRating.HitSpell);

                amount += (int)hitSpell;
            }
        }

        void CalculateAmountExpertise(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float expertise = 0.0f;
                // Increase hit from AuraType.ModExpertise
                expertise += owner.GetTotalAuraModifier(AuraType.ModExpertise);
                // Increase Expertise from Expertise ratings
                expertise += owner.GetRatingBonusValue(CombatRating.Expertise);

                amount += (int)expertise;
            }
        }

        public override void Register()
        {
            switch (m_scriptSpellId)
            {
                case SpellIds.TamedPetPassive06:
                    DoEffectCalcAmount.Add(new(CalculateAmountCritMelee, 0, AuraType.ModWeaponCritPercent));
                    DoEffectCalcAmount.Add(new(CalculateAmountCritSpell, 1, AuraType.ModSpellCritChance));
                    break;
                case SpellIds.PetPassiveCrit:
                    DoEffectCalcAmount.Add(new(CalculateAmountCritSpell, 0, AuraType.ModSpellCritChance));
                    DoEffectCalcAmount.Add(new(CalculateAmountCritMelee, 1, AuraType.ModWeaponCritPercent));
                    break;
                case SpellIds.WarlockPetScaling05:
                case SpellIds.HunterPetScaling04:
                    DoEffectCalcAmount.Add(new(CalculateAmountMeleeHit, 0, AuraType.ModHitChance));
                    DoEffectCalcAmount.Add(new(CalculateAmountSpellHit, 1, AuraType.ModSpellHitChance));
                    DoEffectCalcAmount.Add(new(CalculateAmountExpertise, 2, AuraType.ModExpertise));
                    break;
                case SpellIds.DeathKnightPetScaling03:
                    //                    case SpellShamanPetHit:
                    DoEffectCalcAmount.Add(new(CalculateAmountMeleeHit, 0, AuraType.ModHitChance));
                    DoEffectCalcAmount.Add(new(CalculateAmountSpellHit, 1, AuraType.ModSpellHitChance));
                    break;
                default:
                    break;
            }
        }
    }

    [Script]
    class spell_warl_pet_scaling_01 : AuraScript
    {
        uint _tempBonus;

        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateStaminaAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null && pet.IsPet())
            {
                Unit owner = pet.ToPet().GetOwner();
                if (owner != null)
                {
                    float ownerBonus = MathFunctions.CalculatePct(owner.GetStat(Stats.Stamina), 75);

                    amount += (int)ownerBonus;
                    _tempBonus = (uint)ownerBonus;
                }
            }
        }

        void ApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (_tempBonus != 0)
                {
                    PetLevelInfo pInfo = ObjectMgr.GetPetLevelInfo(pet.GetEntry(), pet.GetLevel());
                    uint healthMod;
                    uint baseHealth = pInfo.health;
                    switch (pet.GetEntry())
                    {
                        case CreatureIds.EntryImp:
                            healthMod = (uint)(_tempBonus * 8.4f);
                            break;
                        case CreatureIds.EntryFelguard:
                        case CreatureIds.EntryVoidwalker:
                            healthMod = _tempBonus * 11;
                            break;
                        case CreatureIds.EntrySuccubus:
                            healthMod = (uint)(_tempBonus * 9.1f);
                            break;
                        case CreatureIds.EntryFelhunter:
                            healthMod = (uint)(_tempBonus * 9.5f);
                            break;
                        default:
                            healthMod = 0;
                            break;
                    }
                    if (healthMod != 0)
                        pet.ToPet().SetCreateHealth(baseHealth + healthMod);
                }
            }
        }

        void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (pet.IsPet())
                {
                    PetLevelInfo pInfo = ObjectMgr.GetPetLevelInfo(pet.GetEntry(), pet.GetLevel());
                    pet.ToPet().SetCreateHealth(pInfo.health);
                }
            }
        }

        void CalculateAttackPowerAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null && pet.IsPet())
            {
                Player owner = pet.ToPet().GetOwner();
                if (owner != null)
                {
                    int fire = owner.m_activePlayerData.ModDamageDonePos[(int)SpellSchools.Fire] - owner.m_activePlayerData.ModDamageDoneNeg[(int)SpellSchools.Fire];
                    int shadow = owner.m_activePlayerData.ModDamageDonePos[(int)SpellSchools.Shadow] - owner.m_activePlayerData.ModDamageDoneNeg[(int)SpellSchools.Shadow];
                    int maximum = (fire > shadow) ? fire : shadow;
                    if (maximum < 0)
                        maximum = 0;
                    float bonusAP = maximum * 0.57f;

                    amount += (int)bonusAP;

                    // Glyph of felguard
                    if (pet.GetEntry() == CreatureIds.EntryFelguard)
                    {
                        AuraEffect aurEffect = owner.GetAuraEffect(56246, 0);
                        if (aurEffect != null)
                        {
                            float base_attPower = pet.GetFlatModifierValue(UnitMods.AttackPower, UnitModifierFlatType.Base) * pet.GetPctModifierValue(UnitMods.AttackPower, UnitModifierPctType.Base);
                            amount += (int)MathFunctions.CalculatePct(amount + base_attPower, aurEffect.GetAmount());
                        }
                    }
                }
            }
        }

        void CalculateDamageDoneAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null && pet.IsPet())
            {
                Player owner = pet.ToPet().GetOwner();
                if (owner != null)
                {
                    //the damage bonus used for pets is either fire or shadow damage, whatever is higher
                    int fire = owner.m_activePlayerData.ModDamageDonePos[(int)SpellSchools.Fire] - owner.m_activePlayerData.ModDamageDoneNeg[(int)SpellSchools.Fire];
                    int shadow = owner.m_activePlayerData.ModDamageDonePos[(int)SpellSchools.Shadow] - owner.m_activePlayerData.ModDamageDoneNeg[(int)SpellSchools.Shadow];
                    int maximum = (fire > shadow) ? fire : shadow;
                    float bonusDamage = 0.0f;

                    if (maximum > 0)
                        bonusDamage = maximum * 0.15f;

                    amount += (int)bonusDamage;
                }
            }
        }

        public override void Register()
        {
            OnEffectRemove.Add(new(RemoveEffect, 0, AuraType.ModStat, AuraEffectHandleModes.ChangeAmountMask));
            AfterEffectApply.Add(new(ApplyEffect, 0, AuraType.ModStat, AuraEffectHandleModes.ChangeAmountMask));
            DoEffectCalcAmount.Add(new(CalculateStaminaAmount, 0, AuraType.ModStat));
            DoEffectCalcAmount.Add(new(CalculateAttackPowerAmount, 1, AuraType.ModAttackPower));
            DoEffectCalcAmount.Add(new(CalculateDamageDoneAmount, 2, AuraType.ModDamageDone));
        }
    }

    [Script]
    class spell_warl_pet_scaling_02 : AuraScript
    {
        uint _tempBonus;

        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateIntellectAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null && pet.IsPet())
            {
                Unit owner = pet.ToPet().GetOwner();
                if (owner != null)
                {
                    int ownerBonus = (int)MathFunctions.CalculatePct(owner.GetStat(Stats.Intellect), 30);

                    amount += ownerBonus;
                    _tempBonus = (uint)ownerBonus;
                }
            }

        }

        void ApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (_tempBonus != 0)
                {
                    PetLevelInfo pInfo = ObjectMgr.GetPetLevelInfo(pet.GetEntry(), pet.GetLevel());
                    uint manaMod;
                    uint baseMana = pInfo.mana;
                    switch (pet.GetEntry())
                    {
                        case CreatureIds.EntryImp:
                            manaMod = (uint)(_tempBonus * 4.9f);
                            break;
                        case CreatureIds.EntryVoidwalker:
                        case CreatureIds.EntrySuccubus:
                        case CreatureIds.EntryFelhunter:
                        case CreatureIds.EntryFelguard:
                            manaMod = (uint)(_tempBonus * 11.5f);
                            break;
                        default:
                            manaMod = 0;
                            break;
                    }
                    if (manaMod != 0)
                        pet.ToPet().SetCreateMana(baseMana + manaMod);
                }
            }
        }

        void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (pet.IsPet())
                {
                    PetLevelInfo pInfo = ObjectMgr.GetPetLevelInfo(pet.GetEntry(), pet.GetLevel());
                    pet.ToPet().SetCreateMana(pInfo.mana);
                }
            }
        }

        void CalculateArmorAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (pet.IsPet())
                {
                    Unit owner = pet.ToPet().GetOwner();
                    if (owner != null)
                    {
                        int ownerBonus = (int)MathFunctions.CalculatePct(owner.GetArmor(), 35);
                        amount += ownerBonus;
                    }
                }
            }
        }

        void CalculateFireResistanceAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null && pet.IsPet())
            {
                Unit owner = pet.ToPet().GetOwner();
                if (owner != null)
                {
                    int ownerBonus = MathFunctions.CalculatePct(owner.GetResistance(SpellSchoolMask.Fire), 40);
                    amount += ownerBonus;
                }
            }
        }

        public override void Register()
        {
            OnEffectRemove.Add(new(RemoveEffect, 0, AuraType.ModStat, AuraEffectHandleModes.ChangeAmountMask));
            AfterEffectApply.Add(new(ApplyEffect, 0, AuraType.ModStat, AuraEffectHandleModes.ChangeAmountMask));
            DoEffectCalcAmount.Add(new(CalculateIntellectAmount, 0, AuraType.ModStat));
            DoEffectCalcAmount.Add(new(CalculateArmorAmount, 1, AuraType.ModResistance));
            DoEffectCalcAmount.Add(new(CalculateFireResistanceAmount, 2, AuraType.ModResistance));
        }
    }

    [Script]
    class spell_warl_pet_scaling_03 : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateFrostResistanceAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null && pet.IsPet())
            {
                Unit owner = pet.ToPet().GetOwner();
                if (owner != null)
                {
                    int ownerBonus = MathFunctions.CalculatePct(owner.GetResistance(SpellSchoolMask.Frost), 40);
                    amount += ownerBonus;
                }
            }
        }

        void CalculateArcaneResistanceAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null && pet.IsPet())
            {
                Unit owner = pet.ToPet().GetOwner();
                if (owner != null)
                {
                    int ownerBonus = MathFunctions.CalculatePct(owner.GetResistance(SpellSchoolMask.Arcane), 40);
                    amount += ownerBonus;
                }
            }
        }

        void CalculateNatureResistanceAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null && pet.IsPet())
            {
                Unit owner = pet.ToPet().GetOwner();
                if (owner != null)
                {
                    int ownerBonus = MathFunctions.CalculatePct(owner.GetResistance(SpellSchoolMask.Nature), 40);
                    amount += ownerBonus;
                }
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateFrostResistanceAmount, 0, AuraType.ModResistance));
            DoEffectCalcAmount.Add(new(CalculateArcaneResistanceAmount, 1, AuraType.ModResistance));
            DoEffectCalcAmount.Add(new(CalculateNatureResistanceAmount, 2, AuraType.ModResistance));
        }
    }

    [Script]
    class spell_warl_pet_scaling_04 : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateShadowResistanceAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null && pet.IsPet())
            {
                Unit owner = pet.ToPet().GetOwner();
                if (owner != null)
                {
                    int ownerBonus = MathFunctions.CalculatePct(owner.GetResistance(SpellSchoolMask.Shadow), 40);
                    amount += ownerBonus;
                }
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateShadowResistanceAmount, 0, AuraType.ModResistance));
        }
    }

    [Script]
    class spell_warl_pet_scaling_05 : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateAmountMeleeHit(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float hitMelee = 0.0f;
                // Increase hit from AuraType.ModSpellHitChance
                hitMelee += owner.GetTotalAuraModifier(AuraType.ModSpellHitChance);
                // Increase hit spell from spell hit ratings
                hitMelee += owner.GetRatingBonusValue(CombatRating.HitSpell);

                amount += (int)hitMelee;
            }
        }

        void CalculateAmountSpellHit(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float hitSpell = 0.0f;
                // Increase hit from AuraType.ModSpellHitChance
                hitSpell += owner.GetTotalAuraModifier(AuraType.ModSpellHitChance);
                // Increase hit spell from spell hit ratings
                hitSpell += owner.GetRatingBonusValue(CombatRating.HitSpell);

                amount += (int)hitSpell;
            }
        }

        void CalculateAmountExpertise(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float expertise = 0.0f;
                // Increase hit from AuraType.ModSpellHitChance
                expertise += owner.GetTotalAuraModifier(AuraType.ModSpellHitChance);
                // Increase hit spell from spell hit ratings
                expertise += owner.GetRatingBonusValue(CombatRating.HitSpell);

                amount += (int)expertise;
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmountMeleeHit, 0, AuraType.ModHitChance));
            DoEffectCalcAmount.Add(new(CalculateAmountSpellHit, 1, AuraType.ModSpellHitChance));
            DoEffectCalcAmount.Add(new(CalculateAmountExpertise, 2, AuraType.ModExpertise));
        }
    }

    [Script]
    class spell_warl_pet_passive : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateAmountCritSpell(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float CritSpell = 5.0f;
                // Increase crit from AuraType.ModSpellCritChance
                CritSpell += owner.GetTotalAuraModifier(AuraType.ModSpellCritChance);
                // Increase crit from AuraType.ModCritPct
                CritSpell += owner.GetTotalAuraModifier(AuraType.ModCritPct);
                // Increase crit spell from spell crit ratings
                CritSpell += owner.GetRatingBonusValue(CombatRating.CritSpell);

                AuraApplication improvedDemonicTacticsApp = owner.GetAuraApplicationOfRankedSpell(54347);
                if (improvedDemonicTacticsApp != null)
                {
                    Aura improvedDemonicTactics = improvedDemonicTacticsApp.GetBase();
                    if (improvedDemonicTactics != null)
                    {
                        AuraEffect improvedDemonicTacticsEffect = improvedDemonicTactics.GetEffect(0);
                        if (improvedDemonicTacticsEffect != null)
                            amount += (int)MathFunctions.CalculatePct(CritSpell, improvedDemonicTacticsEffect.GetAmount());
                    }
                }
            }
        }

        void CalculateAmountCritMelee(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float CritMelee = 5.0f;
                // Increase crit from AuraType.ModWeaponCritPercent
                CritMelee += owner.GetTotalAuraModifier(AuraType.ModWeaponCritPercent);
                // Increase crit from AuraType.ModCritPct
                CritMelee += owner.GetTotalAuraModifier(AuraType.ModCritPct);
                // Increase crit melee from melee crit ratings
                CritMelee += owner.GetRatingBonusValue(CombatRating.CritMelee);

                AuraApplication improvedDemonicTacticsApp = owner.GetAuraApplicationOfRankedSpell(54347);
                if (improvedDemonicTacticsApp != null)
                {
                    Aura improvedDemonicTactics = improvedDemonicTacticsApp.GetBase();
                    if (improvedDemonicTactics != null)
                    {
                        AuraEffect improvedDemonicTacticsEffect = improvedDemonicTactics.GetEffect(0);
                        if (improvedDemonicTacticsEffect != null)
                            amount += (int)MathFunctions.CalculatePct(CritMelee, improvedDemonicTacticsEffect.GetAmount());
                    }
                }
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmountCritSpell, 0, AuraType.ModSpellCritChance));
            DoEffectCalcAmount.Add(new(CalculateAmountCritMelee, 1, AuraType.ModWeaponCritPercent));
        }
    }

    [Script] // this doesnt actually fit in here
    class spell_warl_pet_passive_damage_done : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateAmountDamageDone(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null)
                return;

            if (GetCaster().GetOwner().ToPlayer() != null)
            {
                switch (GetCaster().GetEntry())
                {
                    case CreatureIds.EntryVoidwalker:
                        amount += -16;
                        break;
                    case CreatureIds.EntryFelhunter:
                        amount += -20;
                        break;
                    case CreatureIds.EntrySuccubus:
                    case CreatureIds.EntryFelguard:
                        amount += 5;
                        break;
                }
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmountDamageDone, 0, AuraType.ModDamagePercentDone));
            DoEffectCalcAmount.Add(new(CalculateAmountDamageDone, 1, AuraType.ModDamagePercentDone));
        }
    }

    [Script]
    class spell_warl_pet_passive_voidwalker : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null && pet.IsPet())
            {
                Unit owner = pet.ToPet().GetOwner();
                if (owner != null)
                {
                    AuraEffect aurEffect = owner.GetAuraEffect(SpellIds.WarlockGlyphOfVoidwalker, 0);
                    if (aurEffect != null)
                        amount += aurEffect.GetAmount();
                }
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmount, 0, AuraType.ModTotalStatPercentage));
        }
    }

    [Script]
    class spell_sha_pet_scaling_04 : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateAmountMeleeHit(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float hitMelee = 0.0f;
                // Increase hit from AuraType.ModHitChance
                hitMelee += owner.GetTotalAuraModifier(AuraType.ModHitChance);
                // Increase hit melee from meele hit ratings
                hitMelee += owner.GetRatingBonusValue(CombatRating.HitMelee);

                amount += (int)hitMelee;
            }
        }

        void CalculateAmountSpellHit(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float hitSpell = 0.0f;
                // Increase hit from AuraType.ModSpellHitChance
                hitSpell += owner.GetTotalAuraModifier(AuraType.ModSpellHitChance);
                // Increase hit spell from spell hit ratings
                hitSpell += owner.GetRatingBonusValue(CombatRating.HitSpell);

                amount += (int)hitSpell;
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmountMeleeHit, 0, AuraType.ModHitChance));
            DoEffectCalcAmount.Add(new(CalculateAmountSpellHit, 1, AuraType.ModSpellHitChance));
        }
    }

    [Script]
    class spell_hun_pet_scaling_01 : AuraScript
    {
        uint _tempHealth;

        void CalculateStaminaAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Pet pet = GetUnitOwner()?.ToPet();
            if (pet != null)
            {
                Unit owner = pet.GetOwner();
                if (owner != null)
                {
                    float mod = 0.45f;

                    SpellInfo spellInfo = SpellMgr.GetSpellInfo(62758, GetCastDifficulty()) ?? SpellMgr.GetSpellInfo(62762, GetCastDifficulty());
                    if (spellInfo != null) // If pet has Wild Hunt
                        MathFunctions.AddPct(ref mod, spellInfo.GetEffect(0).CalcValue());

                    amount += (int)(owner.GetStat(Stats.Stamina) * mod);
                }
            }
        }

        void ApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (_tempHealth != 0)
                    pet.SetHealth(_tempHealth);
            }
        }

        void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
                _tempHealth = (uint)pet.GetHealth();
        }

        void CalculateAttackPowerAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (!pet.IsPet())
                    return;

                Unit owner = pet.ToPet().GetOwner();
                if (owner == null)
                    return;

                float mod = 1.0f;                                                 //Hunter contribution modifier

                SpellInfo spellInfo = SpellMgr.GetSpellInfo(62758, GetCastDifficulty()) ?? SpellMgr.GetSpellInfo(62762, GetCastDifficulty());
                if (spellInfo != null) // If pet has Wild Hunt
                    mod += MathFunctions.CalculatePct(1.0f, spellInfo.GetEffect(1).CalcValue());

                amount += (int)(owner.GetTotalAttackPowerValue(WeaponAttackType.RangedAttack) * 0.22f * mod);
            }
        }

        void CalculateDamageDoneAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (!pet.IsPet())
                    return;

                Unit owner = pet.ToPet().GetOwner();
                if (owner == null)
                    return;

                float mod = 1.0f;                                                 //Hunter contribution modifier

                SpellInfo spellInfo = SpellMgr.GetSpellInfo(62758, GetCastDifficulty()) ?? SpellMgr.GetSpellInfo(62762, GetCastDifficulty());
                if (spellInfo != null) // If pet has Wild Hunt
                    mod += MathFunctions.CalculatePct(1.0f, spellInfo.GetEffect(1).CalcValue());

                amount += (int)(owner.GetTotalAttackPowerValue(WeaponAttackType.RangedAttack) * 0.1287f * mod);
            }
        }

        public override void Register()
        {
            OnEffectRemove.Add(new(RemoveEffect, 0, AuraType.ModStat, AuraEffectHandleModes.ChangeAmountMask));
            AfterEffectApply.Add(new(ApplyEffect, 0, AuraType.ModStat, AuraEffectHandleModes.ChangeAmountMask));
            DoEffectCalcAmount.Add(new(CalculateStaminaAmount, 0, AuraType.ModStat));
            DoEffectCalcAmount.Add(new(CalculateAttackPowerAmount, 1, AuraType.ModAttackPower));
            DoEffectCalcAmount.Add(new(CalculateDamageDoneAmount, 2, AuraType.ModDamageDone));
        }
    }

    [Script]
    class spell_hun_pet_scaling_02 : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateFrostResistanceAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (!pet.IsPet())
                    return;

                Unit owner = pet.ToPet().GetOwner();
                if (owner == null)
                    return;

                int ownerBonus = MathFunctions.CalculatePct(owner.GetResistance(SpellSchoolMask.Frost), 40);
                amount += ownerBonus;
            }
        }

        void CalculateFireResistanceAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (!pet.IsPet())
                    return;

                Unit owner = pet.ToPet().GetOwner();
                if (owner == null)
                    return;

                int ownerBonus = MathFunctions.CalculatePct(owner.GetResistance(SpellSchoolMask.Fire), 40);
                amount += ownerBonus;
            }
        }

        void CalculateNatureResistanceAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (!pet.IsPet())
                    return;

                Unit owner = pet.ToPet().GetOwner();
                if (owner == null)
                    return;

                int ownerBonus = MathFunctions.CalculatePct(owner.GetResistance(SpellSchoolMask.Nature), 40);
                amount += ownerBonus;
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateFrostResistanceAmount, 1, AuraType.ModResistance));
            DoEffectCalcAmount.Add(new(CalculateFireResistanceAmount, 0, AuraType.ModResistance));
            DoEffectCalcAmount.Add(new(CalculateNatureResistanceAmount, 2, AuraType.ModResistance));
        }
    }

    [Script]
    class spell_hun_pet_scaling_03 : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateShadowResistanceAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (!pet.IsPet())
                    return;

                Unit owner = pet.ToPet().GetOwner();
                if (owner == null)
                    return;

                int ownerBonus = MathFunctions.CalculatePct(owner.GetResistance(SpellSchoolMask.Shadow), 40);
                amount += ownerBonus;
            }
        }

        void CalculateArcaneResistanceAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (!pet.IsPet())
                    return;

                Unit owner = pet.ToPet().GetOwner();
                if (owner == null)
                    return;

                int ownerBonus = MathFunctions.CalculatePct(owner.GetResistance(SpellSchoolMask.Arcane), 40);
                amount += ownerBonus;
            }
        }

        void CalculateArmorAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (!pet.IsPet())
                    return;

                Unit owner = pet.ToPet().GetOwner();
                if (owner == null)
                    return;

                amount += (int)MathFunctions.CalculatePct(owner.GetArmor(), 35);
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateShadowResistanceAmount, 0, AuraType.ModResistance));
            DoEffectCalcAmount.Add(new(CalculateArcaneResistanceAmount, 1, AuraType.ModResistance));
            DoEffectCalcAmount.Add(new(CalculateArmorAmount, 2, AuraType.ModResistance));
        }
    }

    [Script]
    class spell_hun_pet_scaling_04 : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateAmountMeleeHit(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null)
                return;

            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float hitMelee = 0.0f;
                // Increase hit from AuraType.ModHitChance
                hitMelee += owner.GetTotalAuraModifier(AuraType.ModHitChance);
                // Increase hit melee from meele hit ratings
                hitMelee += owner.GetRatingBonusValue(CombatRating.HitMelee);

                amount += (int)hitMelee;
            }
        }

        void CalculateAmountSpellHit(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null)
                return;

            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float hitSpell = 0.0f;
                // Increase hit from AuraType.ModSpellHitChance
                hitSpell += owner.GetTotalAuraModifier(AuraType.ModSpellHitChance);
                // Increase hit spell from spell hit ratings
                hitSpell += owner.GetRatingBonusValue(CombatRating.HitSpell);

                amount += (int)hitSpell;
            }
        }

        void CalculateAmountExpertise(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null)
                return;

            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float expertise = 0.0f;
                // Increase hit from AuraType.ModExpertise
                expertise += owner.GetTotalAuraModifier(AuraType.ModExpertise);
                // Increase Expertise from Expertise ratings
                expertise += owner.GetRatingBonusValue(CombatRating.Expertise);

                amount += (int)expertise;
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmountMeleeHit, 0, AuraType.ModHitChance));
            DoEffectCalcAmount.Add(new(CalculateAmountSpellHit, 1, AuraType.ModSpellHitChance));
            DoEffectCalcAmount.Add(new(CalculateAmountExpertise, 2, AuraType.ModExpertise));
        }
    }

    [Script]
    class spell_hun_pet_passive_crit : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateAmountCritSpell(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null)
                return;

            if (GetCaster().GetOwner().ToPlayer() != null)
            {
                // For others recalculate it from:
                float CritSpell = 5.0f;
                // Increase crit from AuraType.ModSpellCritChance
                // CritSpell += owner.GetTotalAuraModifier(AuraType.ModSpellCritChance);
                // Increase crit from AuraType.ModCritPct
                // CritSpell += owner.GetTotalAuraModifier(AuraType.ModCritPct);
                // Increase crit spell from spell crit ratings
                // CritSpell += owner.GetRatingBonusValue(CrCritSpell);

                amount += (int)(CritSpell * 0.8f);
            }
        }

        void CalculateAmountCritMelee(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null)
                return;

            if (GetCaster().GetOwner().ToPlayer() != null)
            {
                // For others recalculate it from:
                float CritMelee = 5.0f;
                // Increase crit from AuraType.ModWeaponCritPercent
                // CritMelee += owner.GetTotalAuraModifier(AuraType.ModWeaponCritPercent);
                // Increase crit from AuraType.ModCritPct
                // CritMelee += owner.GetTotalAuraModifier(AuraType.ModCritPct);
                // Increase crit melee from melee crit ratings
                // CritMelee += owner.GetRatingBonusValue(CrCritMelee);

                amount += (int)(CritMelee * 0.8f);
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmountCritSpell, 1, AuraType.ModSpellCritChance));
            DoEffectCalcAmount.Add(new(CalculateAmountCritMelee, 0, AuraType.ModWeaponCritPercent));
        }
    }

    [Script]
    class spell_hun_pet_passive_damage_done : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateAmountDamageDone(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null)
                return;

            if (GetCaster().GetOwner().ToPlayer() != null)
            {
                // Cobra Reflexes
                AuraEffect cobraReflexes = GetCaster().GetAuraEffectOfRankedSpell(61682, 0);
                if (cobraReflexes != null)
                    amount -= cobraReflexes.GetAmount();
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmountDamageDone, 0, AuraType.ModDamagePercentDone));
        }
    }

    [Script]
    class spell_hun_animal_handler : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateAmountDamageDone(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null)
                return;

            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                AuraEffect aurEffect = owner.GetAuraEffectOfRankedSpell(SpellIds.HunterAnimalHandler, 1);
                if (aurEffect != null)
                    amount = aurEffect.GetAmount();
                else
                    amount = 0;
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmountDamageDone, 0, AuraType.ModAttackPowerPct));
        }
    }

    [Script]
    class spell_dk_avoidance_passive : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateAvoidanceAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                Unit owner = pet.GetOwner();
                if (owner != null)
                {
                    // Army of the dead ghoul
                    if (pet.GetEntry() == CreatureIds.EntryArmyOfTheDeadGhoul)
                        amount = -90;
                    // Night of the dead
                    else
                    {
                        Aura aur = owner.GetAuraOfRankedSpell(SpellIds.NightOfTheDead);
                        if (aur != null)
                            amount = aur.GetSpellInfo().GetEffect(2).CalcValue();
                    }
                }
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAvoidanceAmount, 0, AuraType.ModCreatureAoeDamageAvoidance));
        }
    }

    [Script]
    class spell_dk_pet_scaling_01 : AuraScript
    {
        uint _tempHealth;

        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateStaminaAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (pet.IsGuardian())
                {
                    Unit owner = pet.GetOwner();
                    if (owner != null)
                    {
                        float ownerBonus = (float)(owner.GetStat(Stats.Stamina)) * 0.3f;
                        amount += (int)ownerBonus;
                    }
                }
            }
        }

        void ApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit pet = GetUnitOwner();
            if (pet != null && _tempHealth != 0)
                pet.SetHealth(_tempHealth);
        }

        void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
                _tempHealth = (uint)pet.GetHealth();
        }

        void CalculateStrengthAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                if (!pet.IsGuardian())
                    return;

                Unit owner = pet.GetOwner();
                if (owner == null)
                    return;

                float ownerBonus = (float)(owner.GetStat(Stats.Strength)) * 0.7f;
                amount += (int)ownerBonus;
            }
        }

        public override void Register()
        {
            OnEffectRemove.Add(new(RemoveEffect, 0, AuraType.ModStat, AuraEffectHandleModes.ChangeAmountMask));
            AfterEffectApply.Add(new(ApplyEffect, 0, AuraType.ModStat, AuraEffectHandleModes.ChangeAmountMask));
            DoEffectCalcAmount.Add(new(CalculateStaminaAmount, 0, AuraType.ModStat));
            DoEffectCalcAmount.Add(new(CalculateStrengthAmount, 1, AuraType.ModStat));
        }
    }

    [Script]
    class spell_dk_pet_scaling_02 : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateAmountMeleeHaste(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null)
                return;

            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float hasteMelee = 0.0f;
                // Increase hit from AuraType.ModHitChance
                hasteMelee += (1 - owner.m_modAttackSpeedPct[(int)WeaponAttackType.BaseAttack]) * 100;

                amount += (int)hasteMelee;
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmountMeleeHaste, 1, AuraType.MeleeSlow));
        }
    }

    [Script]
    class spell_dk_pet_scaling_03 : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateAmountMeleeHit(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null)
                return;

            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float hitMelee = 0.0f;
                // Increase hit from AuraType.ModHitChance
                hitMelee += owner.GetTotalAuraModifier(AuraType.ModHitChance);
                // Increase hit melee from meele hit ratings
                hitMelee += owner.GetRatingBonusValue(CombatRating.HitMelee);

                amount += (int)hitMelee;
            }
        }

        void CalculateAmountSpellHit(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null)
                return;

            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float hitSpell = 0.0f;
                // Increase hit from AuraType.ModSpellHitChance
                hitSpell += owner.GetTotalAuraModifier(AuraType.ModSpellHitChance);
                // Increase hit spell from spell hit ratings
                hitSpell += owner.GetRatingBonusValue(CombatRating.HitSpell);

                amount += (int)hitSpell;
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateAmountMeleeHit, 0, AuraType.ModHitChance));
            DoEffectCalcAmount.Add(new(CalculateAmountSpellHit, 1, AuraType.ModSpellHitChance));
        }
    }

    [Script]
    class spell_dk_rune_weapon_scaling_02 : AuraScript
    {
        public override bool Load()
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null || !GetCaster().GetOwner().IsPlayer())
                return false;
            return true;
        }

        void CalculateDamageDoneAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            Unit pet = GetUnitOwner();
            if (pet != null)
            {
                Unit owner = pet.GetOwner();
                if (owner == null)
                    return;

                if (pet.IsGuardian())
                    ((Guardian)pet).SetBonusDamage((int)owner.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack));

                amount += (int)owner.CalculateDamage(WeaponAttackType.BaseAttack, true, true);
            }
        }

        void CalculateAmountMeleeHaste(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            if (GetCaster() == null || GetCaster().GetOwner() == null)
                return;

            Player owner = GetCaster().GetOwner().ToPlayer();
            if (owner != null)
            {
                // For others recalculate it from:
                float hasteMelee = 0.0f;
                // Increase hit from AuraType.ModHitChance
                hasteMelee += (1 - owner.m_modAttackSpeedPct[(int)WeaponAttackType.BaseAttack]) * 100;

                amount += (int)hasteMelee;
            }
        }

        public override void Register()
        {
            DoEffectCalcAmount.Add(new(CalculateDamageDoneAmount, 0, AuraType.ModDamageDone));
            DoEffectCalcAmount.Add(new(CalculateAmountMeleeHaste, 1, AuraType.MeleeSlow));
        }
    }
}
