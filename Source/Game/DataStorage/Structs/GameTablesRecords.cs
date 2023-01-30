// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.DataStorage
{
    public sealed class GtArtifactKnowledgeMultiplierRecord
    {
        public float Multiplier;
    }

    public sealed class GtArtifactLevelXPRecord
    {
        public float XP;
        public float XP2;
    }

    public sealed class GtBarberShopCostBaseRecord
    {
        public float Cost;
    }

    public sealed class GtBaseMPRecord
    {
        public float Adventurer;
        public float DeathKnight;
        public float DemonHunter;
        public float Druid;
        public float Evoker;
        public float Hunter;
        public float Mage;
        public float Monk;
        public float Paladin;
        public float Priest;
        public float Rogue;
        public float Shaman;
        public float Warlock;
        public float Warrior;
    }

    public sealed class GtBattlePetXPRecord
    {
        public float Wins;
        public float Xp;
    }

    public sealed class GtCombatRatingsRecord
    {
        public float Amplify;
        public float ArmorPenetration;
        public float Avoidance;
        public float Block;
        public float Cleave;
        public float Corruption;
        public float CorruptionResistance;
        public float CritMelee;
        public float CritRanged;
        public float CritSpell;
        public float DefenseSkill;
        public float Dodge;
        public float Expertise;
        public float HasteMelee;
        public float HasteRanged;
        public float HasteSpell;
        public float HitMelee;
        public float HitRanged;
        public float HitSpell;
        public float Lifesteal;
        public float Mastery;
        public float Parry;
        public float PvPPower;
        public float ResilienceCritTaken;
        public float ResiliencePlayerDamage;
        public float Speed;
        public float Sturdiness;
        public float Unused12;
        public float Unused7;
        public float VersatilityDamageDone;
        public float VersatilityDamageTaken;
        public float VersatilityHealingDone;
    }

    public sealed class GtGenericMultByILvlRecord
    {
        public float ArmorMultiplier;
        public float JewelryMultiplier;
        public float TrinketMultiplier;
        public float WeaponMultiplier;
    }

    public sealed class GtHpPerStaRecord
    {
        public float Health;
    }

    public sealed class GtItemSocketCostPerLevelRecord
    {
        public float SocketCost;
    }

    public sealed class GtNpcManaCostScalerRecord
    {
        public float Scaler;
    }

    public sealed class GtSpellScalingRecord
    {
        public float Adventurer;
        public float Consumable;
        public float DamageReplaceStat;
        public float DamageSecondary;
        public float DeathKnight;
        public float DemonHunter;
        public float Druid;
        public float Evoker;
        public float Gem1;
        public float Gem2;
        public float Gem3;
        public float Health;
        public float Hunter;
        public float Item;
        public float Mage;
        public float ManaConsumable;
        public float Monk;
        public float Paladin;
        public float Priest;
        public float Rogue;
        public float Shaman;
        public float Warlock;
        public float Warrior;
    }

    public sealed class GtXpRecord
    {
        public float Divisor;
        public float Junk;
        public float PerKill;
        public float Stats;
        public float Total;
    }
}