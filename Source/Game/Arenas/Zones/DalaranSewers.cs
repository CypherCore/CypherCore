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
using Framework.Dynamic;
using Game.Entities;
using Game.Network.Packets;

namespace Game.Arenas
{
    class DalaranSewersArena : Arena
    {
        public DalaranSewersArena()
        {
            _events = new EventMap();
        }

        public override void StartingEventCloseDoors()
        {
            for (int i = DalaranSewersObjectTypes.Door1; i <= DalaranSewersObjectTypes.Door2; ++i)
                SpawnBGObject(i, BattlegroundConst.RespawnImmediately);
        }

        public override void StartingEventOpenDoors()
        {
            for (int i = DalaranSewersObjectTypes.Door1; i <= DalaranSewersObjectTypes.Door2; ++i)
                DoorOpen(i);

            for (int i = DalaranSewersObjectTypes.Buff1; i <= DalaranSewersObjectTypes.Buff2; ++i)
                SpawnBGObject(i, 60);

            _events.ScheduleEvent(DalaranSewersEvents.WaterfallWarning, RandomHelper.URand(DalaranSewersData.WaterfallTimerMin, DalaranSewersData.WaterfallTimerMax));
            _events.ScheduleEvent(DalaranSewersEvents.PipeKnockback, DalaranSewersData.PipeKnockbackFirstDelay);

            SpawnBGObject(DalaranSewersObjectTypes.Water2, BattlegroundConst.RespawnImmediately);

            DoorOpen(DalaranSewersObjectTypes.Water1); // Turn off collision
            DoorOpen(DalaranSewersObjectTypes.Water2);

            // Remove effects of Demonic Circle Summon
            foreach (var pair in GetPlayers())
            {
                Player player = _GetPlayer(pair, "BattlegroundDS::StartingEventOpenDoors");
                if (player)
                    player.RemoveAurasDueToSpell(DalaranSewersSpells.DemonicCircle);
            }
        }

        public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            switch (trigger)
            {
                case 5347:
                case 5348:
                    // Remove effects of Demonic Circle Summon
                    player.RemoveAurasDueToSpell(DalaranSewersSpells.DemonicCircle);

                    // Someone has get back into the pipes and the knockback has already been performed,
                    // so we reset the knockback count for kicking the player again into the arena.
                    _events.ScheduleEvent(DalaranSewersEvents.PipeKnockback, DalaranSewersData.PipeKnockbackDelay);
                    break;
                default:
                    base.HandleAreaTrigger(player, trigger, entered);
                    break;
            }
        }

        public override void FillInitialWorldStates(InitWorldStates packet)
        {
            packet.AddState(3610, 1);
            base.FillInitialWorldStates(packet);
        }

        public override bool SetupBattleground()
        {
            bool result = true;
            result &= AddObject(DalaranSewersObjectTypes.Door1, DalaranSewersGameObjects.Door1, 1350.95f, 817.2f, 20.8096f, 3.15f, 0, 0, 0.99627f, 0.0862864f, BattlegroundConst.RespawnImmediately);
            result &= AddObject(DalaranSewersObjectTypes.Door2, DalaranSewersGameObjects.Door2, 1232.65f, 764.913f, 20.0729f, 6.3f, 0, 0, 0.0310211f, -0.999519f, BattlegroundConst.RespawnImmediately);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "DalaranSewersArena: Failed to spawn door object!");
                return false;
            }

            // buffs
            result &= AddObject(DalaranSewersObjectTypes.Buff1, DalaranSewersGameObjects.Buff1, 1291.7f, 813.424f, 7.11472f, 4.64562f, 0, 0, 0.730314f, -0.683111f, 120);
            result &= AddObject(DalaranSewersObjectTypes.Buff2, DalaranSewersGameObjects.Buff2, 1291.7f, 768.911f, 7.11472f, 1.55194f, 0, 0, 0.700409f, 0.713742f, 120);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "DalaranSewersArena: Failed to spawn buff object!");
                return false;
            }

            result &= AddObject(DalaranSewersObjectTypes.Water1, DalaranSewersGameObjects.Water1, 1291.56f, 790.837f, 7.1f, 3.14238f, 0, 0, 0.694215f, -0.719768f, 120);
            result &= AddObject(DalaranSewersObjectTypes.Water2, DalaranSewersGameObjects.Water2, 1291.56f, 790.837f, 7.1f, 3.14238f, 0, 0, 0.694215f, -0.719768f, 120);
            result &= AddCreature(DalaranSewersData.NpcWaterSpout, DalaranSewersCreatureTypes.WaterfallKnockback, 1292.587f, 790.2205f, 7.19796f, 3.054326f, TeamId.Neutral, BattlegroundConst.RespawnImmediately);
            result &= AddCreature(DalaranSewersData.NpcWaterSpout, DalaranSewersCreatureTypes.PipeKnockback1, 1369.977f, 817.2882f, 16.08718f, 3.106686f, TeamId.Neutral, BattlegroundConst.RespawnImmediately);
            result &= AddCreature(DalaranSewersData.NpcWaterSpout, DalaranSewersCreatureTypes.PipeKnockback2, 1212.833f, 765.3871f, 16.09484f, 0.0f, TeamId.Neutral, BattlegroundConst.RespawnImmediately);
            if (!result)
            {
                Log.outError(LogFilter.Sql, "DalaranSewersArena: Failed to spawn collision object!");
                return false;
            }

            return true;
        }

        public override void PostUpdateImpl(uint diff)
        {
            if (GetStatus() != BattlegroundStatus.InProgress)
                return;

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case DalaranSewersEvents.WaterfallWarning:
                        // Add the water
                        DoorClose(DalaranSewersObjectTypes.Water2);
                        _events.ScheduleEvent(DalaranSewersEvents.WaterfallOn, DalaranSewersData.WaterWarningDuration);
                        break;
                    case DalaranSewersEvents.WaterfallOn:
                        // Active collision and start knockback timer
                        DoorClose(DalaranSewersObjectTypes.Water1);
                        _events.ScheduleEvent(DalaranSewersEvents.WaterfallOff, DalaranSewersData.WaterfallDuration);
                        _events.ScheduleEvent(DalaranSewersEvents.WaterfallKnockback, DalaranSewersData.WaterfallKnockbackTimer);
                        break;
                    case DalaranSewersEvents.WaterfallOff:
                        // Remove collision and water
                        DoorOpen(DalaranSewersObjectTypes.Water1);
                        DoorOpen(DalaranSewersObjectTypes.Water2);
                        _events.CancelEvent(DalaranSewersEvents.WaterfallKnockback);
                        _events.ScheduleEvent(DalaranSewersEvents.WaterfallWarning, RandomHelper.URand(DalaranSewersData.WaterfallTimerMin, DalaranSewersData.WaterfallTimerMax));
                        break;
                    case DalaranSewersEvents.WaterfallKnockback:
                    {
                        // Repeat knockback while the waterfall still active
                        Creature waterSpout = GetBGCreature(DalaranSewersCreatureTypes.WaterfallKnockback);
                        if (waterSpout)
                            waterSpout.CastSpell(waterSpout, DalaranSewersSpells.WaterSpout, true);
                        _events.ScheduleEvent(eventId, DalaranSewersData.WaterfallKnockbackTimer);
                    }
                        break;
                    case DalaranSewersEvents.PipeKnockback:
                    {
                        for (int i = DalaranSewersCreatureTypes.PipeKnockback1; i <= DalaranSewersCreatureTypes.PipeKnockback2; ++i)
                        {
                            Creature waterSpout = GetBGCreature(i);
                            if (waterSpout)
                                waterSpout.CastSpell(waterSpout, DalaranSewersSpells.Flush, true);
                        }
                    }
                        break;
                }
            });
        }

        EventMap _events;
    }

    struct DalaranSewersEvents
    {
        public const int WaterfallWarning = 1; // Water starting to fall, but no LoS Blocking nor movement blocking
        public const uint WaterfallOn = 2; // LoS and Movement blocking active
        public const uint WaterfallOff = 3;
        public const uint WaterfallKnockback = 4;

        public const uint PipeKnockback = 5;
    }

    struct DalaranSewersObjectTypes
    {
        public const int Door1 = 0;
        public const int Door2 = 1;
        public const int Water1 = 2; // Collision
        public const int Water2 = 3;
        public const int Buff1 = 4;
        public const int Buff2 = 5;
        public const int Max = 6;
    }

    struct DalaranSewersGameObjects
    {
        public const uint Door1 = 192642;
        public const uint Door2 = 192643;
        public const uint Water1 = 194395; // Collision
        public const uint Water2 = 191877;
        public const uint Buff1 = 184663;
        public const uint Buff2 = 184664;
    }

    struct DalaranSewersCreatureTypes
    {
        public const int WaterfallKnockback = 0;
        public const int PipeKnockback1 = 1;
        public const int PipeKnockback2 = 2;
        public const int Max = 3;
    }

    struct DalaranSewersData
    {
        // These values are NOT blizzlike... need the correct data!
        public const uint WaterfallTimerMin = 30000;
        public const uint WaterfallTimerMax = 60000;
        public const uint WaterWarningDuration = 5000;
        public const uint WaterfallDuration = 30000;
        public const uint WaterfallKnockbackTimer = 1500;

        public const uint PipeKnockbackFirstDelay = 5000;
        public const uint PipeKnockbackDelay = 3000;
        public const uint PipeKnockbackTotalCount = 2;

        public const uint NpcWaterSpout = 28567;
    }

    struct DalaranSewersSpells
    {
        public const uint Flush = 57405; // Visual And Target Selector For The Starting Knockback From The Pipe
        public const uint FlushKnockback = 61698; // Knockback Effect For Previous Spell (Triggered, Not Needed To Be Cast)
        public const uint WaterSpout = 58873; // Knockback Effect Of The Central Waterfall

        public const uint DemonicCircle = 48018;  // Demonic Circle Summon
    }
}
