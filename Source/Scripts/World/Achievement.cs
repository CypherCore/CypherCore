// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;

namespace Scripts.World.Achievements
{
    [Script("achievement_arena_2v2_kills", ArenaTypes.Team2v2)]
    [Script("achievement_arena_3v3_kills", ArenaTypes.Team3v3)]
    [Script("achievement_arena_5v5_kills", ArenaTypes.Team5v5)]
    class achievement_arena_kills : AchievementCriteriaScript
    {
        ArenaTypes _arenaType;

        public achievement_arena_kills(string name, ArenaTypes arenaType) : base(name)
        {
            _arenaType = arenaType;
        }

        public override bool OnCheck(Player source, Unit target)
        {
            // this checks GetBattleground() for Null already
            if (!source.InArena())
                return false;

            return source.GetBattleground().GetArenaType() == _arenaType;
        }
    }

    [Script]
    class achievement_tilted : AchievementCriteriaScript
    {
        const uint AreaArgentTournamentFields = 4658;
        const uint AreaRingOfAspirants = 4670;
        const uint AreaRingOfArgentValiants = 4671;
        const uint AreaRingOfAllianceValiants = 4672;
        const uint AreaRingOfHordeValiants = 4673;
        const uint AreaRingOfChapions = 4669;

        public achievement_tilted() : base("achievement_tilted") { }

        public override bool OnCheck(Player player, Unit target)
        {
            if (player == null)
                return false;

            bool checkArea = player.GetAreaId() == AreaArgentTournamentFields ||
                                player.GetAreaId() == AreaRingOfAspirants ||
                                player.GetAreaId() == AreaRingOfArgentValiants ||
                                player.GetAreaId() == AreaRingOfAllianceValiants ||
                                player.GetAreaId() == AreaRingOfHordeValiants ||
                                player.GetAreaId() == AreaRingOfChapions;

            return checkArea && player.duel != null && player.duel.IsMounted;
        }
    }

    [Script]
    class achievement_flirt_with_disaster_perf_check : AchievementCriteriaScript
    {
        const uint AuraPerfumeForever = 70235;
        const uint AuraPerfumeEnchantress = 70234;
        const uint AuraPerfumeVictory = 70233;

        public achievement_flirt_with_disaster_perf_check() : base("achievement_flirt_with_disaster_perf_check") { }

        public override bool OnCheck(Player player, Unit target)
        {
            if (player == null)
                return false;

            if (player.HasAura(AuraPerfumeForever) || player.HasAura(AuraPerfumeEnchantress) || player.HasAura(AuraPerfumeVictory))
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
            return target != null && player.IsHonorOrXPTarget(target);
        }
    }

    [Script] // 7433 - Newbie
    class achievement_newbie : AchievementScript
    {
        public achievement_newbie() : base("achievement_newbie") { }

        public override void OnCompleted(Player player, AchievementRecord achievement)
        {
            player.GetSession().GetBattlePetMgr().UnlockSlot(BattlePetSlots.Slot1);
            // Todo: Unlock trap
        }
    }

    [Script] // 6566 - Just a Pup
    class achievement_just_a_pup : AchievementScript
    {
        public achievement_just_a_pup() : base("achievement_just_a_pup") { }

        public override void OnCompleted(Player player, AchievementRecord achievement)
        {
            player.GetSession().GetBattlePetMgr().UnlockSlot(BattlePetSlots.Slot2);
        }
    }
}

