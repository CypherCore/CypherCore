/*
 * This file is part of the TrinityCore Project. See Authors file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the Gnu General Public License as published by the
 * Free Software Foundation; either version 2 of the License, or (at your
 * option) any later version.
 *
 * This program is distributed in the hope that it will be useful, but Without
 * Any Warranty; without even the implied warranty of Merchantability or
 * Fitness For A Particular Purpose. See the Gnu General Public License for
 * more details.
 *
 * You should have received a copy of the Gnu General Public License along
 * with this program. If not, see <http://www.gnu.org/licenses/>.
 */

/*
 * Scripts for spells with SpellfamilyDeathknight and SpellfamilyGeneric spells used by deathknight players.
 * Ordered alphabetically using scriptname.
 * Scriptnames of files in this file should be prefixed with "spell_dk_".
 */


using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Pets;

struct SpellIds
{
    // HunterPetCalculate
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

    // WarlockPetCalculate
    public const uint PetPassiveCrit = 35695;
    public const uint PetPassiveDamageTaken = 35697;
    public const uint WarlockPetScaling01 = 34947;
    public const uint WarlockPetScaling02 = 34956;
    public const uint WarlockPetScaling03 = 34957;
    public const uint WarlockPetScaling04 = 34958;
    public const uint WarlockPetScaling05 = 61013;
    public const uint WarlockGlyphOfVoidwalker = 56247;

    // DKPetCalculate
    public const uint DeathKnightRuneWeapon02 = 51906;
    public const uint DeathKnightPetScaling01 = 54566;
    public const uint DeathKnightPetScaling02 = 51996;
    public const uint DeathKnightPetScaling03 = 61697;
    public const uint NightOfTheDead = 55620;

    // ShamanPetCalculate
    public const uint FeralSpiritPetUnk01 = 35674;
    public const uint FeralSpiritPetUnk02 = 35675;
    public const uint FeralSpiritPetUnk03 = 35676;
    public const uint FeralSpiritPetScaling04 = 61783;

    // MiscPetCalculate
    public const uint MagePetPassiveElemental = 44559;
    public const uint PetHealthScaling = 61679;
    public const uint PetUnk01 = 67561;
    public const uint PetUnk02 = 67557;
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
            float CritSpell = 5.0f;
            // Increase crit from SpellAuraModSpellCritChance
            CritSpell += owner.GetTotalAuraModifier(AuraType.ModSpellCritChance);
            // Increase crit from SpellAuraModCritPct
            CritSpell += owner.GetTotalAuraModifier(AuraType.ModCritPct);
            // Increase crit spell from spell crit ratings
            CritSpell += owner.GetRatingBonusValue(CombatRating.CritSpell);

            amount += (int)CritSpell;
        }
    }

    void CalculateAmountCritMelee(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        Player owner = GetCaster().GetOwner().ToPlayer();
        if (owner != null)
        {
            // For others recalculate it from:
            float CritMelee = 5.0f;
            // Increase crit from SpellAuraModWeaponCritPercent
            CritMelee += owner.GetTotalAuraModifier(AuraType.ModWeaponCritPercent);
            // Increase crit from SpellAuraModCritPct
            CritMelee += owner.GetTotalAuraModifier(AuraType.ModCritPct);
            // Increase crit melee from melee crit ratings
            CritMelee += owner.GetRatingBonusValue(CombatRating.CritMelee);

            amount += (int)CritMelee;
        }
    }

    void CalculateAmountMeleeHit(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        Player owner = GetCaster().GetOwner().ToPlayer();
        if (owner != null)
        {
            // For others recalculate it from:
            float HitMelee = 0.0f;
            // Increase hit from SpellAuraModHitChance
            HitMelee += owner.GetTotalAuraModifier(AuraType.ModHitChance);
            // Increase hit melee from meele hit ratings
            HitMelee += owner.GetRatingBonusValue(CombatRating.HitMelee);

            amount += (int)HitMelee;
        }
    }

    void CalculateAmountSpellHit(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        Player owner = GetCaster().GetOwner().ToPlayer();
        if (owner != null)
        {
            // For others recalculate it from:
            float HitSpell = 0.0f;
            // Increase hit from SpellAuraModSpellHitChance
            HitSpell += owner.GetTotalAuraModifier(AuraType.ModSpellHitChance);
            // Increase hit spell from spell hit ratings
            HitSpell += owner.GetRatingBonusValue(CombatRating.HitSpell);

            amount += (int)HitSpell;
        }
    }

    void CalculateAmountExpertise(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
    {
        Player owner = GetCaster().GetOwner().ToPlayer();
        if (owner != null)
        {
            // For others recalculate it from:
            float Expertise = 0.0f;
            // Increase hit from SpellAuraModExpertise
            Expertise += owner.GetTotalAuraModifier(AuraType.ModExpertise);
            // Increase Expertise from Expertise ratings
            Expertise += owner.GetRatingBonusValue(CombatRating.Expertise);

            amount += (int)Expertise;
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