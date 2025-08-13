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
        public float Rogue;
        public float Druid;
        public float Hunter;
        public float Mage;
        public float Paladin;
        public float Priest;
        public float Shaman;
        public float Warlock;
        public float Warrior;
        public float DeathKnight;
        public float Monk;
        public float DemonHunter;
        public float Evoker;
        public float Adventurer;
    }

    public sealed class GtBattlePetXPRecord
    {
        public float Wins;
        public float Xp;
    }

    public sealed class GtCombatRatingsRecord
    {
        public float Amplify;
        public float DefenseSkill;
        public float Dodge;
        public float Parry;
        public float Block;
        public float HitMelee;
        public float HitRanged;
        public float HitSpell;
        public float CritMelee;
        public float CritRanged;
        public float CritSpell;
        public float Corruption;
        public float CorruptionResistance;
        public float Speed;
        public float ResilienceCritTaken;
        public float ResiliencePlayerDamage;
        public float Lifesteal;
        public float HasteMelee;
        public float HasteRanged;
        public float HasteSpell;
        public float Avoidance;
        public float Sturdiness;
        public float Unused7;
        public float Expertise;
        public float ArmorPenetration;
        public float Mastery;
        public float PvPPower;
        public float Cleave;
        public float VersatilityDamageDone;
        public float VersatilityHealingDone;
        public float VersatilityDamageTaken;
        public float Unused12;
    }

    public sealed class GtGenericMultByILvlRecord
    {
        public float ArmorMultiplier;
        public float WeaponMultiplier;
        public float TrinketMultiplier;
        public float JewelryMultiplier;
    }

    public sealed class GtHpPerStaRecord
    {
        public float Health;
    }

    public sealed class GtItemLevelByLevelRecord
    {
        public float ItemLevel;
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
        public float Rogue;
        public float Druid;
        public float Hunter;
        public float Mage;
        public float Paladin;
        public float Priest;
        public float Shaman;
        public float Warlock;
        public float Warrior;
        public float DeathKnight;
        public float Monk;
        public float DemonHunter;
        public float Evoker;
        public float Adventurer;
        public float Item;
        public float Consumable;
        public float Gem1;
        public float Gem2;
        public float Gem3;
        public float Health;
        public float DamageReplaceStat;
        public float DamageSecondary;
        public float ManaConsumable;
    }

    public sealed class GtXpRecord
    {
        public float Total;
        public float PerKill;
        public float Junk;
        public float Stats;
        public float Divisor;
    }
}
