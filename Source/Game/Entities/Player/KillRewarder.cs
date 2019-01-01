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
using Game.Groups;
using Game.Maps;
using Game.Scenarios;
using System.Collections.Generic;

namespace Game.Entities
{
    public class KillRewarder
    {
        public KillRewarder(Player killer, Unit victim, bool isBattleground)
        {
            _killer = killer;
            _victim = victim;
            _group = killer.GetGroup();
            _groupRate = 1.0f;
            _maxNotGrayMember = null;
            _count = 0;
            _sumLevel = 0;
            _xp = 0;
            _isFullXP = false;
            _maxLevel = 0;
            _isBattleground = isBattleground;
            _isPvP = false;

            // mark the credit as pvp if victim is player
            if (victim.IsTypeId(TypeId.Player))
                _isPvP = true;
            // or if its owned by player and its not a vehicle
            else if (victim.GetCharmerOrOwnerGUID().IsPlayer())
                _isPvP = !victim.IsVehicle();

            _InitGroupData();
        }

        public void Reward()
        {
            // 3. Reward killer (and group, if necessary).
            if (_group)
                // 3.1. If killer is in group, reward group.
                _RewardGroup();
            else
            {
                // 3.2. Reward single killer (not group case).
                // 3.2.1. Initialize initial XP amount based on killer's level.
                _InitXP(_killer);
                // To avoid unnecessary calculations and calls,
                // proceed only if XP is not ZERO or player is not on Battleground
                // (Battlegroundrewards only XP, that's why).
                if (!_isBattleground || _xp != 0)
                    // 3.2.2. Reward killer.
                    _RewardPlayer(_killer, false);
            }

            // 5. Credit instance encounter.
            // 6. Update guild achievements.
            // 7. Credit scenario criterias
            Creature victim = _victim.ToCreature();
            if (victim != null)
            {
                if (victim.IsDungeonBoss())
                {
                    InstanceScript instance = _victim.GetInstanceScript();
                    if (instance != null)
                        instance.UpdateEncounterStateForKilledCreature(_victim.GetEntry(), _victim);
                }

                uint guildId = victim.GetMap().GetOwnerGuildId();
                var guild = Global.GuildMgr.GetGuildById(guildId);
                if (guild != null)
                    guild.UpdateCriteria(CriteriaTypes.KillCreature, victim.GetEntry(), 1, 0, victim, _killer);

                Scenario scenario = victim.GetScenario();
                if (scenario != null)
                    scenario.UpdateCriteria(CriteriaTypes.KillCreature, victim.GetEntry(), 1, 0, victim, _killer);
            }
        }

        void _InitGroupData()
        {
            if (_group)
            {
                // 2. In case when player is in group, initialize variables necessary for group calculations:
                for (GroupReference refe = _group.GetFirstMember(); refe != null; refe = refe.next())
                {
                    Player member = refe.GetSource();
                    if (member)
                    {
                        if (member.IsAlive() && member.IsAtGroupRewardDistance(_victim))
                        {
                            uint lvl = member.getLevel();
                            // 2.1. _count - number of alive group members within reward distance;
                            ++_count;
                            // 2.2. _sumLevel - sum of levels of alive group members within reward distance;
                            _sumLevel += lvl;
                            // 2.3. _maxLevel - maximum level of alive group member within reward distance;
                            if (_maxLevel < lvl)
                                _maxLevel = (byte)lvl;
                            // 2.4. _maxNotGrayMember - maximum level of alive group member within reward distance,
                            //      for whom victim is not gray;
                            uint grayLevel = Formulas.GetGrayLevel(lvl);
                            if (_victim.GetLevelForTarget(member) > grayLevel && (!_maxNotGrayMember || _maxNotGrayMember.getLevel() < lvl))
                                _maxNotGrayMember = member;
                        }
                    }
                }
                // 2.5. _isFullXP - flag identifying that for all group members victim is not gray,
                //      so 100% XP will be rewarded (50% otherwise).
                _isFullXP = _maxNotGrayMember && (_maxLevel == _maxNotGrayMember.getLevel());
            }
            else
                _count = 1;
        }

        void _InitXP(Player player)
        {
            // Get initial value of XP for kill.
            // XP is given:
            // * on Battlegrounds;
            // * otherwise, not in PvP;
            // * not if killer is on vehicle.
            if (_isBattleground || (!_isPvP && _killer.GetVehicle() == null))
                _xp = Formulas.XPGain(player, _victim, _isBattleground);
        }

        void _RewardHonor(Player player)
        {
            // Rewarded player must be alive.
            if (player.IsAlive())
                player.RewardHonor(_victim, _count, -1, true);
        }

        void _RewardXP(Player player, float rate)
        {
            uint xp = _xp;
            if (_group)
            {
                // 4.2.1. If player is in group, adjust XP:
                //        * set to 0 if player's level is more than maximum level of not gray member;
                //        * cut XP in half if _isFullXP is false.
                if (_maxNotGrayMember != null && player.IsAlive() &&
                    _maxNotGrayMember.getLevel() >= player.getLevel())
                    xp = _isFullXP ?
                        (uint)(xp * rate) :             // Reward FULL XP if all group members are not gray.
                        (uint)(xp * rate / 2) + 1;      // Reward only HALF of XP if some of group members are gray.
                else
                    xp = 0;
            }
            if (xp != 0)
            {
                // 4.2.2. Apply auras modifying rewarded XP (SPELL_AURA_MOD_XP_PCT and SPELL_AURA_MOD_XP_FROM_CREATURE_TYPE).
                xp = (uint)(xp * player.GetTotalAuraMultiplier(AuraType.ModXpPct));
                xp = (uint)(xp * player.GetTotalAuraMultiplierByMiscValue(AuraType.ModXpFromCreatureType, (int)_victim.GetCreatureType()));

                // 4.2.3. Give XP to player.
                player.GiveXP(xp, _victim, _groupRate);
                Pet pet = player.GetPet();
                if (pet)
                    // 4.2.4. If player has pet, reward pet with XP (100% for single player, 50% for group case).
                    pet.GivePetXP(_group ? xp / 2 : xp);
            }
        }

        void _RewardReputation(Player player, float rate)
        {
            // 4.3. Give reputation (player must not be on BG).
            // Even dead players and corpses are rewarded.
            player.RewardReputation(_victim, rate);
        }

        void _RewardKillCredit(Player player)
        {
            // 4.4. Give kill credit (player must not be in group, or he must be alive or without corpse).
            if (!_group || player.IsAlive() || player.GetCorpse() == null)
            {
                Creature target = _victim.ToCreature();
                if (target != null)
                {
                    player.KilledMonster(target.GetCreatureTemplate(), target.GetGUID());
                    player.UpdateCriteria(CriteriaTypes.KillCreatureType, (ulong)target.GetCreatureType(), 1, 0, target);
                }
            }
        }

        void _RewardPlayer(Player player, bool isDungeon)
        {
            // 4. Reward player.
            if (!_isBattleground)
            {
                // 4.1. Give honor (player must be alive and not on BG).
                _RewardHonor(player);
                // 4.1.1 Send player killcredit for quests with PlayerSlain
                if (_victim.IsTypeId(TypeId.Player))
                    player.KilledPlayerCredit();
            }
            // Give XP only in PvE or in Battlegrounds.
            // Give reputation and kill credit only in PvE.
            if (!_isPvP || _isBattleground)
            {
                float rate = _group ? _groupRate * player.getLevel() / _sumLevel : 1.0f;
                if (_xp != 0)
                    // 4.2. Give XP.
                    _RewardXP(player, rate);
                if (!_isBattleground)
                {
                    // If killer is in dungeon then all members receive full reputation at kill.
                    _RewardReputation(player, isDungeon ? 1.0f : rate);
                    _RewardKillCredit(player);
                }
            }
        }

        void _RewardGroup()
        {
            if (_maxLevel != 0)
            {
                if (_maxNotGrayMember != null)
                    // 3.1.1. Initialize initial XP amount based on maximum level of group member,
                    //        for whom victim is not gray.
                    _InitXP(_maxNotGrayMember);
                // To avoid unnecessary calculations and calls,
                // proceed only if XP is not ZERO or player is not on Battleground
                // (Battlegroundrewards only XP, that's why).
                if (!_isBattleground || _xp != 0)
                {
                    bool isDungeon = !_isPvP && CliDB.MapStorage.LookupByKey(_killer.GetMapId()).IsDungeon();
                    if (!_isBattleground)
                    {
                        // 3.1.2. Alter group rate if group is in raid (not for Battlegrounds).
                        bool isRaid = !_isPvP && CliDB.MapStorage.LookupByKey(_killer.GetMapId()).IsRaid() && _group.isRaidGroup();
                        _groupRate = Formulas.XPInGroupRate(_count, isRaid);
                    }
                    // 3.1.3. Reward each group member (even dead or corpse) within reward distance.
                    for (GroupReference refe = _group.GetFirstMember(); refe != null; refe = refe.next())
                    {
                        Player member = refe.GetSource();
                        if (member)
                        {
                            if (member.IsAtGroupRewardDistance(_victim))
                            {
                                _RewardPlayer(member, isDungeon);
                                member.UpdateCriteria(CriteriaTypes.SpecialPvpKill, 1, 0, 0, _victim);
                            }
                        }
                    }
                }
            }
        }

        Player _killer;
        Unit _victim;
        Group _group;
        float _groupRate;
        Player _maxNotGrayMember;
        uint _count;
        uint _sumLevel;
        uint _xp;
        bool _isFullXP;
        byte _maxLevel;
        bool _isBattleground;
        bool _isPvP;
    }
}
