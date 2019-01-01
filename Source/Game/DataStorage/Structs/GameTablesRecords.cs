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

namespace Game.DataStorage
{
    public sealed class GtArmorMitigationByLvlRecord
    {
        public float Mitigation;
    }

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
        public float MultiStrike;
        public float Readiness;
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

    public sealed class GtCombatRatingsMultByILvlRecord
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

    public sealed class GtItemSocketCostPerLevelRecord
    {
        public float SocketCost;
    }

    public sealed class GtNpcDamageByClassRecord
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
    }

    public sealed class GtNpcManaCostScalerRecord
    {
        public float Scaler;
    }

    public sealed class GtNpcTotalHpRecord
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
        public float Item;
        public float Consumable;
        public float Gem1;
        public float Gem2;
        public float Gem3;
        public float Health;
        public float DamageReplaceStat;
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
