/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

namespace Scripts.World.Areatriggers
{
    internal struct TextIds
    {
        //Brewfest
        public const uint SayWelcome = 4;
    }

    internal struct SpellIds
    {
        //Legion Teleporter
        public const uint TeleATo = 37387;
        public const uint TeleHTo = 37389;

        //Stormwright Shelf
        public const uint CreateTruePowerOfTheTempest = 53067;

        //Sholazar Waygate
        public const uint SholazarToUngoroTeleport = 52056;
        public const uint UngoroToSholazarTeleport = 52057;

        //Nats Landing
        public const uint FishPaste = 42644;

        //Area 52
        public const uint A52Neuralyzer = 34400;
    }

    internal struct QuestIds
    {
        //Legion Teleporter
        public const uint GainingAccessA = 10589;
        public const uint GainingAccessH = 10604;

        //Stormwright Shelf
        public const uint StrengthOfTheTempest = 12741;

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

    internal struct CreatureIds
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
    }

    internal struct GameObjectIds
    {
        //Coilfang Waterfall
        public const uint CoilfangWaterfall = 184212;
    }

    internal struct AreaTriggerIds
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

    internal struct Misc
    { 
        //Brewfest
        public const uint AreatriggerTalkCooldown = 5; // In Seconds

        //Area 52
        public const uint SummonCooldown = 5;

        //Frostgrips Hollow
        public const uint TypeWaypoint = 0;
        public const uint DataStart = 0;

        public static Position StormforgedMonitorPosition = new Position(6963.95f, 45.65f, 818.71f, 4.948f);
        public static Position StormforgedEradictorPosition = new Position(6983.18f, 7.15f, 806.33f, 2.228f);
    }

    [Script]
    internal class AreaTrigger_at_coilfang_waterfall : AreaTriggerScript
    {
        public AreaTrigger_at_coilfang_waterfall() : base("at_coilfang_waterfall") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            var go = player.FindNearestGameObject(GameObjectIds.CoilfangWaterfall, 35.0f);
            if (go)
                if (go.GetLootState() == LootState.Ready)
                    go.UseDoorOrButton();

            return false;
        }
    }

    [Script]
    internal class AreaTrigger_at_legion_teleporter : AreaTriggerScript
    {
        public AreaTrigger_at_legion_teleporter() : base("at_legion_teleporter") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
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
    internal class AreaTrigger_at_stormwright_shelf : AreaTriggerScript
    {
        public AreaTrigger_at_stormwright_shelf() : base("at_stormwright_shelf") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            if (!player.IsDead() && player.GetQuestStatus(QuestIds.StrengthOfTheTempest) == QuestStatus.Incomplete)
                player.CastSpell(player, SpellIds.CreateTruePowerOfTheTempest, false);

            return true;
        }
    }

    [Script]
    internal class AreaTrigger_at_scent_larkorwi : AreaTriggerScript
    {
        public AreaTrigger_at_scent_larkorwi() : base("at_scent_larkorwi") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            if (!player.IsDead() && player.GetQuestStatus(QuestIds.ScentOfLarkorwi) == QuestStatus.Incomplete)
            {
                if (!player.FindNearestCreature(CreatureIds.LarkorwiMate, 15))
                    player.SummonCreature(CreatureIds.LarkorwiMate, player.GetPositionX() + 5, player.GetPositionY(), player.GetPositionZ(), 3.3f, TempSummonType.TimedDespawnOutOfCombat, 100000);
            }

            return false;
        }
    }

    [Script]
    internal class AreaTrigger_at_last_rites : AreaTriggerScript
    {
        public AreaTrigger_at_last_rites() : base("at_last_rites") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            if (!(player.GetQuestStatus(QuestIds.LastRites) == QuestStatus.Incomplete ||
                player.GetQuestStatus(QuestIds.LastRites) == QuestStatus.Complete ||
                player.GetQuestStatus(QuestIds.BreakingThrough) == QuestStatus.Incomplete ||
                player.GetQuestStatus(QuestIds.BreakingThrough) == QuestStatus.Complete))
                return false;

            WorldLocation pPosition;

            switch (areaTrigger.Id)
            {
                case 5332:
                case 5338:
                    pPosition = new WorldLocation(571, 3733.68f, 3563.25f, 290.812f, 3.665192f);
                    break;
                case 5334:
                    pPosition = new WorldLocation(571, 3802.38f, 3585.95f, 49.5765f, 0.0f);
                    break;
                case 5340:
                    if (player.GetQuestStatus(QuestIds.LastRites) == QuestStatus.Incomplete ||
                        player.GetQuestStatus(QuestIds.LastRites) == QuestStatus.Complete)
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
    internal class AreaTrigger_at_sholazar_waygate : AreaTriggerScript
    {
        public AreaTrigger_at_sholazar_waygate() : base("at_sholazar_waygate") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
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
    internal class AreaTrigger_at_nats_landing : AreaTriggerScript
    {
        public AreaTrigger_at_nats_landing() : base("at_nats_landing") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            if (!player.IsAlive() || !player.HasAura(SpellIds.FishPaste))
                return false;

            if (player.GetQuestStatus(QuestIds.NatsBargain) == QuestStatus.Incomplete)
            {
                if (!player.FindNearestCreature(CreatureIds.LurkingShark, 20.0f))
                {
                    Creature shark = player.SummonCreature(CreatureIds.LurkingShark, -4246.243f, -3922.356f, -7.488f, 5.0f, TempSummonType.TimedDespawnOutOfCombat, 100000);
                    if (shark)
                        shark.GetAI().AttackStart(player);

                    return false;
                }
            }
            return true;
        }
    }

    [Script]
    internal class AreaTrigger_at_brewfest : AreaTriggerScript
    {
        private Dictionary<uint, long> _triggerTimes;

        public AreaTrigger_at_brewfest() : base("at_brewfest")
        {
            // Initialize for cooldown
            _triggerTimes = new Dictionary<uint, long>()
            {
                { AreaTriggerIds.BrewfestDurotar, 0 },
                { AreaTriggerIds.BrewfestDunMorogh,0 },
            };
        }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            var triggerId = areaTrigger.Id;
            // Second trigger happened too early after first, skip for now
            if (GameTime.GetGameTime() - _triggerTimes[triggerId] < Misc.AreatriggerTalkCooldown)
                return false;

            switch (triggerId)
            {
                case AreaTriggerIds.BrewfestDurotar:
                    var tapper = player.FindNearestCreature(CreatureIds.TapperSwindlekeg, 20.0f);
                    if (tapper)
                        tapper.GetAI().Talk(TextIds.SayWelcome, player);
                    break;
                case AreaTriggerIds.BrewfestDunMorogh:
                    var ipfelkofer = player.FindNearestCreature(CreatureIds.IpfelkoferIronkeg, 20.0f);
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
    internal class AreaTrigger_at_area_52_entrance : AreaTriggerScript
    {
        private Dictionary<uint, long> _triggerTimes;

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

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
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

            player.SummonCreature(CreatureIds.Spotlight, x, y, z, 0.0f, TempSummonType.TimedDespawn, 5000);
            player.AddAura(SpellIds.A52Neuralyzer, player);
            _triggerTimes[areaTrigger.Id] = GameTime.GetGameTime();
            return false;
        }
    }

    [Script]
    internal class AreaTrigger_at_frostgrips_hollow : AreaTriggerScript
    {
        private ObjectGuid stormforgedMonitorGUID;
        private ObjectGuid stormforgedEradictorGUID;

        public AreaTrigger_at_frostgrips_hollow() : base("at_frostgrips_hollow")
        {
            stormforgedMonitorGUID.Clear();
            stormforgedEradictorGUID.Clear();
        }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            if (player.GetQuestStatus(QuestIds.TheLonesomeWatcher) != QuestStatus.Incomplete)
                return false;

            var stormforgedMonitor = ObjectAccessor.GetCreature(player, stormforgedMonitorGUID);
            if (stormforgedMonitor)
                return false;

            var stormforgedEradictor = ObjectAccessor.GetCreature(player, stormforgedEradictorGUID);
            if (stormforgedEradictor)
                return false;

            stormforgedMonitor = player.SummonCreature(CreatureIds.StormforgedMonitor, Misc.StormforgedMonitorPosition, TempSummonType.TimedDespawnOutOfCombat, 60000);
            if (stormforgedMonitor)
            {
                stormforgedMonitorGUID = stormforgedMonitor.GetGUID();
                stormforgedMonitor.SetWalk(false);
                /// The npc would search an alternative way to get to the last waypoint without this unit state.
                stormforgedMonitor.AddUnitState(UnitState.IgnorePathfinding);
                stormforgedMonitor.GetMotionMaster().MovePath(CreatureIds.StormforgedMonitor * 100, false);
            }

            stormforgedEradictor = player.SummonCreature(CreatureIds.StormforgedEradictor, Misc.StormforgedEradictorPosition, TempSummonType.TimedDespawnOutOfCombat, 60000);
            if (stormforgedEradictor)
            {
                stormforgedEradictorGUID = stormforgedEradictor.GetGUID();
                stormforgedEradictor.GetMotionMaster().MovePath(CreatureIds.StormforgedEradictor * 100, false);
            }

            return true;
        }
    }
}