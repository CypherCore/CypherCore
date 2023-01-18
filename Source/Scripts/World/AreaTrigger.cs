// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using System.Collections.Generic;
using Game.AI;
using System;

namespace Scripts.World.Areatriggers
{
    struct TextIds
    {
        //Brewfest
        public const uint SayWelcome = 4;
    }

    struct SpellIds
    {
        //Legion Teleporter
        public const uint TeleATo = 37387;
        public const uint TeleHTo = 37389;

        //Sholazar Waygate
        public const uint SholazarToUngoroTeleport = 52056;
        public const uint UngoroToSholazarTeleport = 52057;

        //Nats Landing
        public const uint FishPaste = 42644;

        //Area 52
        public const uint A52Neuralyzer = 34400;

        //Stormwind teleport
        public const uint DustInTheStormwind = 312593;
    }

    struct QuestIds
    {
        //Legion Teleporter
        public const uint GainingAccessA = 10589;
        public const uint GainingAccessH = 10604;

        //Scent Larkorwi
        public const uint ScentOfLarkorwi = 4291;

        //Last Rites
        public const uint LastRites = 12019;
        public const uint BreakingThrough = 11898;

        //Sholazar Waygate
        public const uint TheMakersOverlook = 12613;
        public const uint TheMakersPerch = 12559;
        public const uint MeetingAGreatOne = 13956;

        //Nats Landing
        public const uint NatsBargain = 11209;

        //Frostgrips Hollow
        public const uint TheLonesomeWatcher = 12877;
    }

    struct CreatureIds
    {
        //Scent Larkorwi
        public const uint LarkorwiMate = 9683;

        //Nats Landing
        public const uint LurkingShark = 23928;

        //Brewfest
        public const uint TapperSwindlekeg = 24711;
        public const uint IpfelkoferIronkeg = 24710;

        //Area 52
        public const uint Spotlight = 19913;

        //Frostgrips Hollow
        public const uint StormforgedMonitor = 29862;
        public const uint StormforgedEradictor = 29861;

        //Stormwind Teleport
        public const uint KillCreditTeleportStormwind = 160561;
    }

    struct GameObjectIds
    {
        //Coilfang Waterfall
        public const uint CoilfangWaterfall = 184212;
    }

    struct AreaTriggerIds
    {
        //Sholazar Waygate
        public const uint Sholazar = 5046;
        public const uint Ungoro = 5047;

        //Brewfest
        public const uint BrewfestDurotar = 4829;
        public const uint BrewfestDunMorogh = 4820;

        //Area 52
        public const uint Area52South = 4472;
        public const uint Area52North = 4466;
        public const uint Area52West = 4471;
        public const uint Area52East = 4422;
    }

    struct Misc
    { 
        //Brewfest
        public const uint AreatriggerTalkCooldown = 5; // In Seconds

        //Area 52
        public const uint SummonCooldown = 5;

        //Frostgrips Hollow
        public const uint TypeWaypoint = 0;
        public const uint DataStart = 0;

        public static Position StormforgedMonitorPosition = new(6963.95f, 45.65f, 818.71f, 4.948f);
        public static Position StormforgedEradictorPosition = new(6983.18f, 7.15f, 806.33f, 2.228f);
    }

    [Script]
    class AreaTrigger_at_coilfang_waterfall : AreaTriggerScript
    {
        public AreaTrigger_at_coilfang_waterfall() : base("at_coilfang_waterfall") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
        {
            GameObject go = player.FindNearestGameObject(GameObjectIds.CoilfangWaterfall, 35.0f);
            if (go)
                if (go.GetLootState() == LootState.Ready)
                    go.UseDoorOrButton();

            return false;
        }
    }

    [Script]
    class AreaTrigger_at_legion_teleporter : AreaTriggerScript
    {
        public AreaTrigger_at_legion_teleporter() : base("at_legion_teleporter") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
        {
            if (player.IsAlive() && !player.IsInCombat())
            {
                if (player.GetTeam() == Team.Alliance && player.GetQuestRewardStatus(QuestIds.GainingAccessA))
                {
                    player.CastSpell(player, SpellIds.TeleATo, false);
                    return true;
                }

                if (player.GetTeam() == Team.Horde && player.GetQuestRewardStatus(QuestIds.GainingAccessH))
                {
                    player.CastSpell(player, SpellIds.TeleHTo, false);
                    return true;
                }

                return false;
            }
            return false;
        }
    }

    [Script]
    class AreaTrigger_at_scent_larkorwi : AreaTriggerScript
    {
        public AreaTrigger_at_scent_larkorwi() : base("at_scent_larkorwi") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
        {
            if (!player.IsDead() && player.GetQuestStatus(QuestIds.ScentOfLarkorwi) == QuestStatus.Incomplete)
            {
                if (!player.FindNearestCreature(CreatureIds.LarkorwiMate, 15))
                    player.SummonCreature(CreatureIds.LarkorwiMate, player.GetPositionX() + 5, player.GetPositionY(), player.GetPositionZ(), 3.3f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(100));
            }

            return false;
        }
    }

    [Script]
    class AreaTrigger_at_sholazar_waygate : AreaTriggerScript
    {
        public AreaTrigger_at_sholazar_waygate() : base("at_sholazar_waygate") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
        {
            if (!player.IsDead() && (player.GetQuestStatus(QuestIds.MeetingAGreatOne) != QuestStatus.None ||
                (player.GetQuestStatus(QuestIds.TheMakersOverlook) == QuestStatus.Rewarded && player.GetQuestStatus(QuestIds.TheMakersPerch) == QuestStatus.Rewarded)))
            {
                switch (areaTrigger.Id)
                {
                    case AreaTriggerIds.Sholazar:
                        player.CastSpell(player, SpellIds.SholazarToUngoroTeleport, true);
                        break;

                    case AreaTriggerIds.Ungoro:
                        player.CastSpell(player, SpellIds.UngoroToSholazarTeleport, true);
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

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
        {
            if (!player.IsAlive() || !player.HasAura(SpellIds.FishPaste))
                return false;

            if (player.GetQuestStatus(QuestIds.NatsBargain) == QuestStatus.Incomplete)
            {
                if (!player.FindNearestCreature(CreatureIds.LurkingShark, 20.0f))
                {
                    Creature shark = player.SummonCreature(CreatureIds.LurkingShark, -4246.243f, -3922.356f, -7.488f, 5.0f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(100));
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
        Dictionary<uint, long> _triggerTimes;

        public AreaTrigger_at_brewfest() : base("at_brewfest")
        {
            // Initialize for cooldown
            _triggerTimes = new Dictionary<uint, long>()
            {
                { AreaTriggerIds.BrewfestDurotar, 0 },
                { AreaTriggerIds.BrewfestDunMorogh,0 },
            };
        }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
        {
            uint triggerId = areaTrigger.Id;
            // Second trigger happened too early after first, skip for now
            if (GameTime.GetGameTime() - _triggerTimes[triggerId] < Misc.AreatriggerTalkCooldown)
                return false;

            switch (triggerId)
            {
                case AreaTriggerIds.BrewfestDurotar:
                    Creature tapper = player.FindNearestCreature(CreatureIds.TapperSwindlekeg, 20.0f);
                    if (tapper)
                        tapper.GetAI().Talk(TextIds.SayWelcome, player);
                    break;
                case AreaTriggerIds.BrewfestDunMorogh:
                    Creature ipfelkofer = player.FindNearestCreature(CreatureIds.IpfelkoferIronkeg, 20.0f);
                    if (ipfelkofer)
                        ipfelkofer.GetAI().Talk(TextIds.SayWelcome, player);
                    break;
                default:
                    break;
            }

            _triggerTimes[triggerId] = GameTime.GetGameTime();
            return false;
        }
    }

    [Script]
    class AreaTrigger_at_area_52_entrance : AreaTriggerScript
    {
        Dictionary<uint, long> _triggerTimes;

        public AreaTrigger_at_area_52_entrance() : base("at_area_52_entrance")
        {
            _triggerTimes = new Dictionary<uint, long>()
            {
                { AreaTriggerIds.Area52South, 0 },
                { AreaTriggerIds.Area52North,0 },
                { AreaTriggerIds.Area52West,0},
                { AreaTriggerIds.Area52East,0},
            };
        }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
        {
            float x = 0.0f, y = 0.0f, z = 0.0f;

            if (!player.IsAlive())
                return false;

            if (GameTime.GetGameTime() - _triggerTimes[areaTrigger.Id] < Misc.SummonCooldown)
                return false;

            switch (areaTrigger.Id)
            {
                case AreaTriggerIds.Area52East:
                    x = 3044.176f;
                    y = 3610.692f;
                    z = 143.61f;
                    break;
                case AreaTriggerIds.Area52North:
                    x = 3114.87f;
                    y = 3687.619f;
                    z = 143.62f;
                    break;
                case AreaTriggerIds.Area52West:
                    x = 3017.79f;
                    y = 3746.806f;
                    z = 144.27f;
                    break;
                case AreaTriggerIds.Area52South:
                    x = 2950.63f;
                    y = 3719.905f;
                    z = 143.33f;
                    break;
            }

            player.SummonCreature(CreatureIds.Spotlight, x, y, z, 0.0f, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(5));
            player.AddAura(SpellIds.A52Neuralyzer, player);
            _triggerTimes[areaTrigger.Id] = GameTime.GetGameTime();
            return false;
        }
    }

    [Script]
    class AreaTrigger_at_frostgrips_hollow : AreaTriggerScript
    {
        ObjectGuid stormforgedMonitorGUID;
        ObjectGuid stormforgedEradictorGUID;

        public AreaTrigger_at_frostgrips_hollow() : base("at_frostgrips_hollow")
        {
            stormforgedMonitorGUID.Clear();
            stormforgedEradictorGUID.Clear();
        }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
        {
            if (player.GetQuestStatus(QuestIds.TheLonesomeWatcher) != QuestStatus.Incomplete)
                return false;

            Creature stormforgedMonitor = ObjectAccessor.GetCreature(player, stormforgedMonitorGUID);
            if (stormforgedMonitor)
                return false;

            Creature stormforgedEradictor = ObjectAccessor.GetCreature(player, stormforgedEradictorGUID);
            if (stormforgedEradictor)
                return false;

            stormforgedMonitor = player.SummonCreature(CreatureIds.StormforgedMonitor, Misc.StormforgedMonitorPosition, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(60));
            if (stormforgedMonitor)
            {
                stormforgedMonitorGUID = stormforgedMonitor.GetGUID();
                stormforgedMonitor.SetWalk(false);
                /// The npc would search an alternative way to get to the last waypoint without this unit state.
                stormforgedMonitor.AddUnitState(UnitState.IgnorePathfinding);
                stormforgedMonitor.GetMotionMaster().MovePath(CreatureIds.StormforgedMonitor * 100, false);
            }

            stormforgedEradictor = player.SummonCreature(CreatureIds.StormforgedEradictor, Misc.StormforgedEradictorPosition, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(60));
            if (stormforgedEradictor)
            {
                stormforgedEradictorGUID = stormforgedEradictor.GetGUID();
                stormforgedEradictor.GetMotionMaster().MovePath(CreatureIds.StormforgedEradictor * 100, false);
            }

            return true;
        }
    }

    [Script]
    class areatrigger_stormwind_teleport_unit : AreaTriggerAI
    {
        public areatrigger_stormwind_teleport_unit(AreaTrigger areatrigger) : base(areatrigger) { }

        public override void OnUnitEnter(Unit unit)
        {
            Player player = unit.ToPlayer();
            if (player == null)
                return;

            player.CastSpell(unit, SpellIds.DustInTheStormwind);
            player.KilledMonsterCredit(CreatureIds.KillCreditTeleportStormwind);
        }
    }
}