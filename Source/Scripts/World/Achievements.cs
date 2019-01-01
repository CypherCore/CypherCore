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
using Game.BattleGrounds;
using Game.BattleGrounds.Zones;
using Game.Entities;
using Game.Scripting;

namespace Scripts.World
{
    struct AchievementConst
    {
        //Tilted
        public const uint AreaArgentTournamentFields = 4658;
        public const uint AreaRingOfAspirants = 4670;
        public const uint AreaRingOfArgentValiants = 4671;
        public const uint AreaRingOfAllianceValiants = 4672;
        public const uint AreaRingOfHordeValiants = 4673;
        public const uint AreaRingOfChampions = 4669;

        //Flirt With Disaster
        public const uint AuraPerfumeForever = 70235;
        public const uint AuraPerfumeEnchantress = 70234;
        public const uint AuraPerfumeVictory = 70233;

        //BgSA Artillery
        public const uint AntiPersonnalCannon = 27894;
    }

    [Script]
    class achievement_resilient_victory : AchievementCriteriaScript
    {
        public achievement_resilient_victory() : base("achievement_resilient_victory") { }

        public override bool OnCheck(Player source, Unit target)
        {
            Battleground bg = source.GetBattleground();
            if (bg)
                return bg.CheckAchievementCriteriaMeet((uint)BattlegroundCriteriaId.ResilientVictory, source, target);

            return false;
        }
    }

    [Script]
    class achievement_bg_control_all_nodes : AchievementCriteriaScript
    {
        public achievement_bg_control_all_nodes() : base("achievement_bg_control_all_nodes") { }

        public override bool OnCheck(Player source, Unit target)
        {
            Battleground bg = source.GetBattleground();
            if (bg)
                return bg.IsAllNodesControlledByTeam(source.GetTeam());

            return false;
        }
    }

    [Script]
    class achievement_save_the_day : AchievementCriteriaScript
    {
        public achievement_save_the_day() : base("achievement_save_the_day") { }

        public override bool OnCheck(Player source, Unit target)
        {
            Battleground bg = source.GetBattleground();
            if (bg)
                return bg.CheckAchievementCriteriaMeet((uint)BattlegroundCriteriaId.SaveTheDay, source, target);

            return false;
        }
    }

    [Script]
    class achievement_bg_ic_resource_glut : AchievementCriteriaScript
    {
        public achievement_bg_ic_resource_glut() : base("achievement_bg_ic_resource_glut") { }

        public override bool OnCheck(Player source, Unit target)
        {
            if (source.HasAura(ICSpells.OilRefinery) && source.HasAura(ICSpells.Quarry))
                return true;

            return false;
        }
    }

    [Script]
    class achievement_bg_ic_glaive_grave : AchievementCriteriaScript
    {
        public achievement_bg_ic_glaive_grave() : base("achievement_bg_ic_glaive_grave") { }

        public override bool OnCheck(Player source, Unit target)
        {
            Creature vehicle = source.GetVehicleCreatureBase();
            if (vehicle)
            {
                if (vehicle.GetEntry() == ICCreatures.GlaiveThrowerH || vehicle.GetEntry() == ICCreatures.GlaiveThrowerA)
                    return true;
            }

            return false;
        }
    }

    [Script]
    class achievement_bg_ic_mowed_down : AchievementCriteriaScript
    {
        public achievement_bg_ic_mowed_down() : base("achievement_bg_ic_mowed_down") { }

        public override bool OnCheck(Player source, Unit target)
        {
            Creature vehicle = source.GetVehicleCreatureBase();
            if (vehicle)
            {
                if (vehicle.GetEntry() == ICCreatures.KeepCannon)
                    return true;
            }

            return false;
        }
    }

    [Script]
    class achievement_bg_sa_artillery : AchievementCriteriaScript
    {
        public achievement_bg_sa_artillery() : base("achievement_bg_sa_artillery") { }

        public override bool OnCheck(Player source, Unit target)
        {
            Creature vehicle = source.GetVehicleCreatureBase();
            if (vehicle)
            {
                if (vehicle.GetEntry() == AchievementConst.AntiPersonnalCannon)
                    return true;
            }

            return false;
        }
    }

    [Script("achievement_arena_2v2_kills", ArenaTypes.Team2v2)]
    [Script("achievement_arena_3v3_kills", ArenaTypes.Team3v3)]
    [Script("achievement_arena_5v5_kills", ArenaTypes.Team5v5)]
    class achievement_arena_kills : AchievementCriteriaScript
    {
        public achievement_arena_kills(string name, ArenaTypes arenaType) : base(name)
        {
            _arenaType = arenaType;
        }

        public override bool OnCheck(Player source, Unit target)
        {
            // this checks GetBattleground() for NULL already
            if (!source.InArena())
                return false;

            return source.GetBattleground().GetArenaType() == _arenaType;
        }

        ArenaTypes _arenaType;
    }

    [Script]
    class achievement_sickly_gazelle : AchievementCriteriaScript
    {
        public achievement_sickly_gazelle() : base("achievement_sickly_gazelle") { }

        public override bool OnCheck(Player source, Unit target)
        {
            if (!target)
                return false;

            Player victim = target.ToPlayer();
            if (victim)
                if (victim.IsMounted())
                    return true;

            return false;
        }
    }

    [Script]
    class achievement_everything_counts : AchievementCriteriaScript
    {
        public achievement_everything_counts() : base("achievement_everything_counts") { }

        public override bool OnCheck(Player source, Unit target)
        {
            Battleground bg = source.GetBattleground();
            if (bg)
                return bg.CheckAchievementCriteriaMeet((uint)BattlegroundCriteriaId.EverythingCounts, source, target);

            return false;
        }
    }

    [Script]
    class achievement_bg_av_perfection : AchievementCriteriaScript
    {
        public achievement_bg_av_perfection() : base("achievement_bg_av_perfection") { }

        public override bool OnCheck(Player source, Unit target)
        {
            Battleground bg = source.GetBattleground();
            if (bg)
                return bg.CheckAchievementCriteriaMeet((uint)BattlegroundCriteriaId.AvPerfection, source, target);

            return false;
        }
    }

    [Script]
    class achievement_bg_sa_defense_of_ancients : AchievementCriteriaScript
    {
        public achievement_bg_sa_defense_of_ancients() : base("achievement_bg_sa_defense_of_ancients") { }

        public override bool OnCheck(Player source, Unit target)
        {
            Battleground bg = source.GetBattleground();
            if (bg)
                return bg.CheckAchievementCriteriaMeet((uint)BattlegroundCriteriaId.DefenseOfTheAncients, source, target);

            return false;
        }
    }

    [Script]
    class achievement_tilted : AchievementCriteriaScript
    {
        public achievement_tilted() : base("achievement_tilted") { }

        public override bool OnCheck(Player player, Unit target)
        {
            if (!player)
                return false;

            bool checkArea = player.GetAreaId() == AchievementConst.AreaArgentTournamentFields ||
                                player.GetAreaId() == AchievementConst.AreaRingOfAspirants ||
                                player.GetAreaId() == AchievementConst.AreaRingOfArgentValiants ||
                                player.GetAreaId() == AchievementConst.AreaRingOfAllianceValiants ||
                                player.GetAreaId() == AchievementConst.AreaRingOfHordeValiants ||
                                player.GetAreaId() == AchievementConst.AreaRingOfChampions;

            return checkArea && player.duel != null && player.duel.isMounted;
        }
    }

    [Script]
    class achievement_not_even_a_scratch : AchievementCriteriaScript
    {
        public achievement_not_even_a_scratch() : base("achievement_not_even_a_scratch") { }

        public override bool OnCheck(Player source, Unit target)
        {
            Battleground bg = source.GetBattleground();
            if (bg)
                return bg.CheckAchievementCriteriaMeet((uint)BattlegroundCriteriaId.NotEvenAScratch, source, target);

            return false;
        }
    }

    [Script]
    class achievement_flirt_with_disaster_perf_check : AchievementCriteriaScript
    {
        public achievement_flirt_with_disaster_perf_check() : base("achievement_flirt_with_disaster_perf_check") { }

        public override bool OnCheck(Player player, Unit target)
        {
            if (!player)
                return false;

            if (player.HasAura(AchievementConst.AuraPerfumeForever) || player.HasAura(AchievementConst.AuraPerfumeEnchantress) || player.HasAura(AchievementConst.AuraPerfumeVictory))
                return true;

            return false;
        }
    }

    [Script]
    class achievement_killed_exp_or_honor_target : AchievementCriteriaScript
    {
        public achievement_killed_exp_or_honor_target() : base("achievement_killed_exp_or_honor_target") { }

        public override bool OnCheck(Player player, Unit target)
        {
            return target && player.isHonorOrXPTarget(target);
        }
    }
}