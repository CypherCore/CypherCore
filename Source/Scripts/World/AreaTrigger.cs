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
using Game.Scripting;
using System.Collections.Generic;

namespace Scripts.World
{
    struct AreaTriggerConst
    {
        //Coilfang Waterfall
        public const uint GoCoilfangWaterfall = 184212;

        //Legion Teleporter
        public const uint SpellTeleATo = 37387;
        public const uint QuestGainingAccessA = 10589;

        public const uint SpellTeleHTo = 37389;
        public const uint QuestGainingAccessH = 10604;

        //Stormwright Shelf
        public const uint QuestStrengthOfTheTempest = 12741;
        public const uint SpellCreateTruePowerOfTheTempest = 53067;

        //Scent Larkorwi
        public const uint QuestScentOfLarkorwi = 4291;
        public const uint NpcLarkorwiMate = 9683;

        //Last Rites
        public const uint QuestLastRites = 12019;
        public const uint QuestBreakingThrough = 11898;

        //Sholazar Waygate
        public const uint SpellSholazarToUngoroTeleport = 52056;
        public const uint SpellUngoroToSholazarTeleport = 52057;

        public const uint AtSholazar = 5046;
        public const uint AtUngoro = 5047;

        public const uint QuestTheMakersOverlook = 12613;
        public const uint QuestTheMakersPerch = 12559;
        public const uint QuestMeetingAGreatOne = 13956;

        //Nats Landing
        public const uint QuestNatsBargain = 11209;
        public const uint SpellFishPaste = 42644;
        public const uint NpcLurkingShark = 23928;

        //Brewfest
        public const uint NpcTapperSwindlekeg = 24711;
        public const uint NpcIpfelkoferIronkeg = 24710;

        public const uint AtBrewfestDurotar = 4829;
        public const uint AtBrewfestDunMorogh = 4820;

        public const uint SayWelcome = 4;

        public const uint AreatriggerTalkCooldown = 5; // In Seconds

        //Area 52
        public const uint SpellA52Neuralyzer = 34400;
        public const uint NpcSpotlight = 19913;
        public const uint SummonCooldown = 5;

        public const uint AtArea52South = 4472;
        public const uint AtArea52North = 4466;
        public const uint AtArea52West = 4471;
        public const uint AtArea52East = 4422;

        //Frostgrips Hollow
        public const uint QuestTheLonesomeWatcher = 12877;

        public const uint NpcStormforgedMonitor = 29862;
        public const uint NpcStormforgedEradictor = 29861;

        public const uint TypeWaypoint = 0;
        public const uint DataStart = 0;

        public static Position StormforgedMonitorPosition = new Position(6963.95f, 45.65f, 818.71f, 4.948f);
        public static Position StormforgedEradictorPosition = new Position(6983.18f, 7.15f, 806.33f, 2.228f);
    }

    [Script]
    class AreaTrigger_at_coilfang_waterfall : AreaTriggerScript
    {
        public AreaTrigger_at_coilfang_waterfall() : base("at_coilfang_waterfall") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger, bool entered)
        {
            GameObject go = player.FindNearestGameObject(AreaTriggerConst.GoCoilfangWaterfall, 35.0f);
            if (go)
                if (go.getLootState() == LootState.Ready)
                    go.UseDoorOrButton();

            return false;
        }
    }

    [Script]
    class AreaTrigger_at_legion_teleporter : AreaTriggerScript
    {
        public AreaTrigger_at_legion_teleporter() : base("at_legion_teleporter") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger, bool entered)
        {
            if (player.IsAlive() && !player.IsInCombat())
            {
                if (player.GetTeam() == Team.Alliance && player.GetQuestRewardStatus(AreaTriggerConst.QuestGainingAccessA))
                {
                    player.CastSpell(player, AreaTriggerConst.SpellTeleATo, false);
                    return true;
                }

                if (player.GetTeam() == Team.Horde && player.GetQuestRewardStatus(AreaTriggerConst.QuestGainingAccessH))
                {
                    player.CastSpell(player, AreaTriggerConst.SpellTeleHTo, false);
                    return true;
                }

                return false;
            }
            return false;
        }
    }

    [Script]
    class AreaTrigger_at_stormwright_shelf : AreaTriggerScript
    {
        public AreaTrigger_at_stormwright_shelf() : base("at_stormwright_shelf") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger, bool entered)
        {
            if (!player.IsDead() && player.GetQuestStatus(AreaTriggerConst.QuestStrengthOfTheTempest) == QuestStatus.Incomplete)
                player.CastSpell(player, AreaTriggerConst.SpellCreateTruePowerOfTheTempest, false);

            return true;
        }
    }

    [Script]
    class AreaTrigger_at_scent_larkorwi : AreaTriggerScript
    {
        public AreaTrigger_at_scent_larkorwi() : base("at_scent_larkorwi") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger, bool entered)
        {
            if (!player.IsDead() && player.GetQuestStatus(AreaTriggerConst.QuestScentOfLarkorwi) == QuestStatus.Incomplete)
            {
                if (!player.FindNearestCreature(AreaTriggerConst.NpcLarkorwiMate, 15))
                    player.SummonCreature(AreaTriggerConst.NpcLarkorwiMate, player.GetPositionX() + 5, player.GetPositionY(), player.GetPositionZ(), 3.3f, TempSummonType.TimedDespawnOOC, 100000);
            }

            return false;
        }
    }

    [Script]
    class AreaTrigger_at_last_rites : AreaTriggerScript
    {
        public AreaTrigger_at_last_rites() : base("at_last_rites") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger, bool entered)
        {
            if (!(player.GetQuestStatus(AreaTriggerConst.QuestLastRites) == QuestStatus.Incomplete ||
                player.GetQuestStatus(AreaTriggerConst.QuestLastRites) == QuestStatus.Complete ||
                player.GetQuestStatus(AreaTriggerConst.QuestBreakingThrough) == QuestStatus.Incomplete ||
                player.GetQuestStatus(AreaTriggerConst.QuestBreakingThrough) == QuestStatus.Complete))
                return false;

            WorldLocation pPosition;

            switch (trigger.Id)
            {
                case 5332:
                case 5338:
                    pPosition = new WorldLocation(571, 3733.68f, 3563.25f, 290.812f, 3.665192f);
                    break;
                case 5334:
                    pPosition = new WorldLocation(571, 3802.38f, 3585.95f, 49.5765f, 0.0f);
                    break;
                case 5340:
                    if (player.GetQuestStatus(AreaTriggerConst.QuestLastRites) == QuestStatus.Incomplete ||
                        player.GetQuestStatus(AreaTriggerConst.QuestLastRites) == QuestStatus.Complete)
                        pPosition = new WorldLocation(571, 3687.91f, 3577.28f, 473.342f);
                    else
                        pPosition = new WorldLocation(571, 3739.38f, 3567.09f, 341.58f);
                    break;
                default:
                    return false;
            }

            player.TeleportTo(pPosition);

            return false;
        }
    }

    [Script]
    class AreaTrigger_at_sholazar_waygate : AreaTriggerScript
    {
        public AreaTrigger_at_sholazar_waygate() : base("at_sholazar_waygate") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger, bool entered)
        {
            if (!player.IsDead() && (player.GetQuestStatus(AreaTriggerConst.QuestMeetingAGreatOne) != QuestStatus.None ||
                (player.GetQuestStatus(AreaTriggerConst.QuestTheMakersOverlook) == QuestStatus.Rewarded && player.GetQuestStatus(AreaTriggerConst.QuestTheMakersPerch) == QuestStatus.Rewarded)))
            {
                switch (trigger.Id)
                {
                    case AreaTriggerConst.AtSholazar:
                        player.CastSpell(player, AreaTriggerConst.SpellSholazarToUngoroTeleport, true);
                        break;

                    case AreaTriggerConst.AtUngoro:
                        player.CastSpell(player, AreaTriggerConst.SpellUngoroToSholazarTeleport, true);
                        break;
                }
            }

            return false;
        }
    }

    [Script]
    class AreaTrigger_at_nats_landing : AreaTriggerScript
    {
        public AreaTrigger_at_nats_landing() : base("at_nats_landing") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger, bool entered)
        {
            if (!player.IsAlive() || !player.HasAura(AreaTriggerConst.SpellFishPaste))
                return false;

            if (player.GetQuestStatus(AreaTriggerConst.QuestNatsBargain) == QuestStatus.Incomplete)
            {
                if (!player.FindNearestCreature(AreaTriggerConst.NpcLurkingShark, 20.0f))
                {
                    Creature shark = player.SummonCreature(AreaTriggerConst.NpcLurkingShark, -4246.243f, -3922.356f, -7.488f, 5.0f, TempSummonType.TimedDespawnOOC, 100000);
                    if (shark)
                        shark.GetAI().AttackStart(player);

                    return false;
                }
            }
            return true;
        }
    }

    [Script]
    class AreaTrigger_at_brewfest : AreaTriggerScript
    {
        public AreaTrigger_at_brewfest() : base("at_brewfest")
        {
            // Initialize for cooldown
            _triggerTimes[AreaTriggerConst.AtBrewfestDurotar] = _triggerTimes[AreaTriggerConst.AtBrewfestDunMorogh] = 0;
        }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger, bool entered)
        {
            uint triggerId = trigger.Id;
            // Second trigger happened too early after first, skip for now
            if (Global.WorldMgr.GetGameTime() - _triggerTimes[triggerId] < AreaTriggerConst.AreatriggerTalkCooldown)
                return false;

            switch (triggerId)
            {
                case AreaTriggerConst.AtBrewfestDurotar:
                    Creature tapper = player.FindNearestCreature(AreaTriggerConst.NpcTapperSwindlekeg, 20.0f);
                    if (tapper)
                        tapper.GetAI().Talk(AreaTriggerConst.SayWelcome, player);
                    break;
                case AreaTriggerConst.AtBrewfestDunMorogh:
                    Creature ipfelkofer = player.FindNearestCreature(AreaTriggerConst.NpcIpfelkoferIronkeg, 20.0f);
                    if (ipfelkofer)
                        ipfelkofer.GetAI().Talk(AreaTriggerConst.SayWelcome, player);
                    break;
                default:
                    break;
            }

            _triggerTimes[triggerId] = Global.WorldMgr.GetGameTime();
            return false;
        }

        Dictionary<uint, long> _triggerTimes = new Dictionary<uint, long>();
    }

    [Script]
    class AreaTrigger_at_area_52_entrance : AreaTriggerScript
    {
        public AreaTrigger_at_area_52_entrance() : base("at_area_52_entrance")
        {
            _triggerTimes[AreaTriggerConst.AtArea52South] = _triggerTimes[AreaTriggerConst.AtArea52North] = _triggerTimes[AreaTriggerConst.AtArea52West] = _triggerTimes[AreaTriggerConst.AtArea52East] = 0;
        }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger, bool entered)
        {
            float x = 0.0f, y = 0.0f, z = 0.0f;

            if (!player.IsAlive())
                return false;

            uint triggerId = trigger.Id;
            if (Global.WorldMgr.GetGameTime() - _triggerTimes[trigger.Id] < AreaTriggerConst.SummonCooldown)
                return false;

            switch (triggerId)
            {
                case AreaTriggerConst.AtArea52East:
                    x = 3044.176f;
                    y = 3610.692f;
                    z = 143.61f;
                    break;
                case AreaTriggerConst.AtArea52North:
                    x = 3114.87f;
                    y = 3687.619f;
                    z = 143.62f;
                    break;
                case AreaTriggerConst.AtArea52West:
                    x = 3017.79f;
                    y = 3746.806f;
                    z = 144.27f;
                    break;
                case AreaTriggerConst.AtArea52South:
                    x = 2950.63f;
                    y = 3719.905f;
                    z = 143.33f;
                    break;
            }

            player.SummonCreature(AreaTriggerConst.NpcSpotlight, x, y, z, 0.0f, TempSummonType.TimedDespawn, 5000);
            player.AddAura(AreaTriggerConst.SpellA52Neuralyzer, player);
            _triggerTimes[trigger.Id] = Global.WorldMgr.GetGameTime();
            return false;
        }

        Dictionary<uint, long> _triggerTimes = new Dictionary<uint, long>();
    }

    [Script]
    class AreaTrigger_at_frostgrips_hollow : AreaTriggerScript
    {
        public AreaTrigger_at_frostgrips_hollow() : base("at_frostgrips_hollow") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger, bool entered)
        {
            if (player.GetQuestStatus(AreaTriggerConst.QuestTheLonesomeWatcher) != QuestStatus.Incomplete)
                return false;

            Creature stormforgedMonitor = ObjectAccessor.GetCreature(player, stormforgedMonitorGUID);
            if (stormforgedMonitor)
                return false;

            Creature stormforgedEradictor = ObjectAccessor.GetCreature(player, stormforgedEradictorGUID);
            if (stormforgedEradictor)
                return false;

            stormforgedMonitor = player.SummonCreature(AreaTriggerConst.NpcStormforgedMonitor, AreaTriggerConst.StormforgedMonitorPosition, TempSummonType.TimedDespawnOOC, 60000);
            if (stormforgedMonitor)
            {
                stormforgedMonitorGUID = stormforgedMonitor.GetGUID();
                stormforgedMonitor.SetWalk(false);
                // The npc would search an alternative way to get to the last waypoint without this unit state.
                stormforgedMonitor.AddUnitState(UnitState.IgnorePathfinding);
                stormforgedMonitor.GetMotionMaster().MovePath(AreaTriggerConst.NpcStormforgedMonitor * 100, false);
            }

            stormforgedEradictor = player.SummonCreature(AreaTriggerConst.NpcStormforgedEradictor, AreaTriggerConst.StormforgedEradictorPosition, TempSummonType.TimedDespawnOOC, 60000);
            if (stormforgedEradictor)
            {
                stormforgedEradictorGUID = stormforgedEradictor.GetGUID();
                stormforgedEradictor.GetMotionMaster().MovePath(AreaTriggerConst.NpcStormforgedEradictor * 100, false);
            }

            return true;
        }

        ObjectGuid stormforgedMonitorGUID;
        ObjectGuid stormforgedEradictorGUID;
    }
}
