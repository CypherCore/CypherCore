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
using Game.Entities;
using System;

namespace Game
{
    public class Formulas
    {
        public static float hk_honor_at_level_f(uint level, float multiplier = 1.0f)
        {
            float honor = multiplier * level * 1.55f;
            Global.ScriptMgr.OnHonorCalculation(honor, level, multiplier);
            return honor;
        }

        public static uint hk_honor_at_level(uint level, float multiplier = 1.0f)
        {
            return (uint)Math.Ceiling(hk_honor_at_level_f(level, multiplier));
        }

        public static uint GetGrayLevel(uint pl_level)
        {
            uint level;

            if (pl_level < 7)
                level = 0;
            else if (pl_level < 35)
            {
                byte count = 0;
                for (int i = 15; i <= pl_level; ++i)
                    if (i % 5 == 0) ++count;

                level = (uint)((pl_level - 7) - (count - 1));
            }
            else
                level = pl_level - 10;

            Global.ScriptMgr.OnGrayLevelCalculation(level, pl_level);
            return level;
        }

        public static XPColorChar GetColorCode(uint pl_level, uint mob_level)
        {
            XPColorChar color;

            if (mob_level >= pl_level + 5)
                color = XPColorChar.Red;
            else if (mob_level >= pl_level + 3)
                color = XPColorChar.Orange;
            else if (mob_level >= pl_level - 2)
                color = XPColorChar.Yellow;
            else if (mob_level > GetGrayLevel(pl_level))
                color = XPColorChar.Green;
            else
                color = XPColorChar.Gray;

            Global.ScriptMgr.OnColorCodeCalculation(color, pl_level, mob_level);
            return color;
        }

        public static uint GetZeroDifference(uint pl_level)
        {
            uint diff;

            if (pl_level < 4)
                diff = 5;
            else if (pl_level < 10)
                diff = 6;
            else if (pl_level < 12)
                diff = 7;
            else if (pl_level < 16)
                diff = 8;
            else if (pl_level < 20)
                diff = 9;
            else if (pl_level < 30)
                diff = 11;
            else if (pl_level < 40)
                diff = 12;
            else if (pl_level < 45)
                diff = 13;
            else if (pl_level < 50)
                diff = 14;
            else if (pl_level < 55)
                diff = 15;
            else if (pl_level < 60)
                diff = 16;
            else
                diff = 17;

            Global.ScriptMgr.OnZeroDifferenceCalculation(diff, pl_level);
            return diff;
        }

        public static uint BaseGain(uint pl_level, uint mob_level)
        {
            uint baseGain;

            GtXpRecord xpPlayer = CliDB.XpGameTable.GetRow(pl_level);
            GtXpRecord xpMob = CliDB.XpGameTable.GetRow(mob_level);

            if (mob_level >= pl_level)
            {
                uint nLevelDiff = mob_level - pl_level;
                if (nLevelDiff > 4)
                    nLevelDiff = 4;

                baseGain = (uint)Math.Round(xpPlayer.PerKill * (1 + 0.05f * nLevelDiff));
            }
            else
            {
                uint gray_level = GetGrayLevel(pl_level);
                if (mob_level > gray_level)
                {
                    uint ZD = GetZeroDifference(pl_level);
                    baseGain = (uint)Math.Round(xpMob.PerKill * ((1 - ((pl_level - mob_level) / ZD)) * (xpMob.Divisor / xpPlayer.Divisor)));
                }
                else
                    baseGain = 0;
            }

            Global.ScriptMgr.OnBaseGainCalculation(baseGain, pl_level, mob_level);
            return baseGain;
        }

        public static uint XPGain(Player player, Unit u, bool isBattleGround = false)
        {
            Creature creature = u.ToCreature();
            uint gain = 0;

            if (!creature || creature.CanGiveExperience())
            {
                float xpMod = 1.0f;

                gain = BaseGain(player.getLevel(), u.GetLevelForTarget(player));

                if (gain != 0 && creature)
                {
                    // Players get only 10% xp for killing creatures of lower expansion levels than himself
                    if ((creature.GetCreatureTemplate().HealthScalingExpansion < (int)GetExpansionForLevel(player.getLevel())))
                        gain = (uint)Math.Round(gain / 10.0f);

                    if (creature.isElite())
                    {
                        // Elites in instances have a 2.75x XP bonus instead of the regular 2x world bonus.
                        if (u.GetMap().IsDungeon())
                            xpMod *= 2.75f;
                        else
                            xpMod *= 2.0f;
                    }

                    xpMod *= creature.GetCreatureTemplate().ModExperience;
                }
                xpMod *= isBattleGround ? WorldConfig.GetFloatValue(WorldCfg.RateXpBgKill) : WorldConfig.GetFloatValue(WorldCfg.RateXpKill);
                if (creature && creature.m_PlayerDamageReq != 0) // if players dealt less than 50% of the damage and were credited anyway (due to CREATURE_FLAG_EXTRA_NO_PLAYER_DAMAGE_REQ), scale XP gained appropriately (linear scaling)
                    xpMod *= 1.0f - 2.0f * creature.m_PlayerDamageReq / creature.GetMaxHealth();

                gain = (uint)(gain * xpMod);
            }

            Global.ScriptMgr.OnGainCalculation(gain, player, u);
            return gain;
        }

        public static float XPInGroupRate(uint count, bool isRaid)
        {
            float rate;

            if (isRaid)
            {
                // FIXME: Must apply decrease modifiers depending on raid size.
                // set to < 1 to, so client will display raid related strings
                rate = 0.99f;
            }
            else
            {
                switch (count)
                {
                    case 0:
                    case 1:
                    case 2:
                        rate = 1.0f;
                        break;
                    case 3:
                        rate = 1.166f;
                        break;
                    case 4:
                        rate = 1.3f;
                        break;
                    case 5:
                    default:
                        rate = 1.4f;
                        break;
                }
            }

            Global.ScriptMgr.OnGroupRateCalculation(rate, count, isRaid);
            return rate;
        }

        static Expansion GetExpansionForLevel(uint level)
        {
            if (level < 60)
                return Expansion.Classic;
            else if (level < 70)
                return Expansion.BurningCrusade;
            else if (level < 80)
                return Expansion.WrathOfTheLichKing;
            else if (level < 85)
                return Expansion.Cataclysm;
            else if (level < 90)
                return Expansion.MistsOfPandaria;
            else if (level < 100)
                return Expansion.WarlordsOfDraenor;
            else
                return Expansion.Legion;
        }

        public static uint ConquestRatingCalculator(uint rate)
        {
            if (rate <= 1500)
                return 1350; // Default conquest points
            else if (rate > 3000)
                rate = 3000;

            // http://www.arenajunkies.com/topic/179536-conquest-point-cap-vs-personal-rating-chart/page__st__60#entry3085246
            return (uint)(1.4326 * ((1511.26 / (1 + 1639.28 / Math.Exp(0.00412 * rate))) + 850.15));
        }

        public static uint BgConquestRatingCalculator(uint rate)
        {
            // WowWiki: Battlegroundratings receive a bonus of 22.2% to the cap they generate
            return (uint)((ConquestRatingCalculator(rate) * 1.222f) + 0.5f);
        }
    }
}
