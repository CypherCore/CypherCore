// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.Battlegrounds.DalaranSewers
{
    enum GameObjectIds
    {
        Door1 = 192642,
        Door2 = 192643,
        Water1 = 194395, // Collision
        Water2 = 191877,
        Buff1 = 184663,
        Buff2 = 184664
    }

    enum EventIds
    {
        WaterfallWarning = 1, // Water Starting To Fall, But No Los Blocking Nor Movement Blocking
        WaterfallOn = 2, // Los And Movement Blocking Active
        WaterfallOff = 3,
        WaterfallKnockback = 4,

        PipeKnockback = 5
    }

    enum CreatureIds
    {
        WaterSpout = 28567
    }

    enum SpellIds
    {
        Flush = 57405, // Visual And Target Selector For The Starting Knockback From The Pipe
        FlushKnockback = 61698, // Knockback Effect For Previous Spell (Triggered, Not Needed To Be Cast)
        WaterSpout = 58873, // Knockback Effect Of The Central Waterfall

        WarlDemonicCircle = 48018  // Demonic Circle Summon
    }

    struct Misc
    {
        // These values are NOT blizzlike... need the correct data!
        public const uint PipeKnockbackFirstDelay = 5000;
        public const uint PipeKnockbackDelay = 3000;

        public static TimeSpan WaterfallTimerMin = TimeSpan.FromSeconds(30);
        public static TimeSpan WaterfallTimerMax = TimeSpan.FromSeconds(60);
        public static TimeSpan WaterfallWarningDuration = TimeSpan.FromSeconds(5);
        public static TimeSpan WaterfallDuration = TimeSpan.FromSeconds(30);
        public static TimeSpan WaterfallKnockbackTimer = TimeSpan.FromSeconds(1.5);
        public const uint DataPipeKnockbackCount = 1;
        public const uint PipeKnockbackTotalCount = 2;
    }

    [Script(nameof(arena_dalaran_sewers), 617)]
    class arena_dalaran_sewers : ArenaScript
    {
        List<ObjectGuid> _doorGUIDs = new();
        ObjectGuid _water1GUID;
        ObjectGuid _water2GUID;
        ObjectGuid _waterfallCreatureGUID;
        List<ObjectGuid> _pipeCreatureGUIDs = new();

        uint _pipeKnockBackTimer;
        uint _pipeKnockBackCount;

        public arena_dalaran_sewers(BattlegroundMap map) : base(map)
        {
            _pipeKnockBackTimer = Misc.PipeKnockbackFirstDelay;
        }

        public override void OnUpdate(uint diff)
        {
            if (battleground.GetStatus() != BattlegroundStatus.InProgress)
                return;

            _events.Update(diff);
            _scheduler.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                switch ((EventIds)eventId)
                {
                    case EventIds.WaterfallWarning:
                    {
                        // Add the water
                        GameObject go = battlegroundMap.GetGameObject(_water2GUID);
                        if (go != null)
                            go.ResetDoorOrButton();
                        _events.ScheduleEvent((uint)EventIds.WaterfallOn, Misc.WaterfallWarningDuration);
                        break;
                    }
                    case EventIds.WaterfallOn:
                    {
                        // Active collision and start knockback timer
                        GameObject go = battlegroundMap.GetGameObject(_water1GUID);
                        if (go != null)
                            go.ResetDoorOrButton();
                        _events.ScheduleEvent((uint)EventIds.WaterfallOff, Misc.WaterfallDuration);
                        _events.ScheduleEvent((uint)EventIds.WaterfallKnockback, Misc.WaterfallKnockbackTimer);
                        break;
                    }
                    case EventIds.WaterfallOff:
                    {
                        // Remove collision and water
                        GameObject go = battlegroundMap.GetGameObject(_water1GUID);
                        if (go != null)
                            go.UseDoorOrButton();

                        go = battlegroundMap.GetGameObject(_water2GUID);
                        if (go != null)
                            go.UseDoorOrButton();
                        _events.CancelEvent((uint)EventIds.WaterfallKnockback);
                        _events.ScheduleEvent((uint)EventIds.WaterfallWarning, Misc.WaterfallTimerMin, Misc.WaterfallTimerMax);
                        break;
                    }
                    case EventIds.WaterfallKnockback:
                    {
                        // Repeat knockback while the waterfall still active
                        Creature waterSpout = battlegroundMap.GetCreature(_waterfallCreatureGUID);
                        if (waterSpout != null)
                            waterSpout.CastSpell(waterSpout, (uint)SpellIds.WaterSpout, true);
                        _events.ScheduleEvent(eventId, Misc.WaterfallKnockbackTimer);
                        break;
                    }
                    case EventIds.PipeKnockback:
                    {
                        foreach (ObjectGuid guid in _pipeCreatureGUIDs)
                        {
                            Creature waterSpout = battlegroundMap.GetCreature(guid);
                            if (waterSpout != null)
                                waterSpout.CastSpell(waterSpout, (uint)SpellIds.Flush, true);
                        }
                        break;
                    }
                    default:
                        break;
                }
            });

            if (_pipeKnockBackCount < Misc.PipeKnockbackTotalCount)
            {
                if (_pipeKnockBackTimer < diff)
                {
                    foreach (ObjectGuid guid in _pipeCreatureGUIDs)
                    {
                        Creature waterSpout = battlegroundMap.GetCreature(guid);
                        if (waterSpout != null)
                            waterSpout.CastSpell(waterSpout, (uint)SpellIds.Flush, true);
                    }

                    ++_pipeKnockBackCount;
                    _pipeKnockBackTimer = Misc.PipeKnockbackDelay;
                }
                else
                    _pipeKnockBackTimer -= diff;
            }

        }

        public override void OnInit()
        {
            AddObject((uint)GameObjectIds.Door1, 1350.95f, 817.2f, 20.8096f, 3.15f, 0, 0, 0.99627f, 0.0862864f, GameObjectState.Ready, _doorGUIDs);
            AddObject((uint)GameObjectIds.Door2, 1232.65f, 764.913f, 20.0729f, 6.3f, 0, 0, 0.0310211f, -0.999519f, GameObjectState.Ready, _doorGUIDs);

            GameObject go = CreateObject((uint)GameObjectIds.Water1, 1291.56f, 790.837f, 7.1f, 3.14238f, 0, 0, 0.694215f, -0.719768f, GameObjectState.Ready);
            if (go != null)
                _water1GUID = go.GetGUID();

            go = CreateObject((uint)GameObjectIds.Water2, 1291.56f, 790.837f, 7.1f, 3.14238f, 0, 0, 0.694215f, -0.719768f, GameObjectState.Ready);
            if (go != null)
                _water2GUID = go.GetGUID();
        }

        public override void OnStart()
        {
            foreach (ObjectGuid guid in _doorGUIDs)
            {
                GameObject door = battlegroundMap.GetGameObject(guid);
                if (door != null)
                {
                    door.UseDoorOrButton();
                    door.DespawnOrUnsummon(TimeSpan.FromSeconds(5));
                }
            }

            _scheduler.Schedule(TimeSpan.FromMinutes(1), _ =>
            {
                CreateObject((uint)GameObjectIds.Buff1, 1291.7f, 813.424f, 7.11472f, 4.64562f, 0, 0, 0.730314f, -0.683111f);
                CreateObject((uint)GameObjectIds.Buff2, 1291.7f, 768.911f, 7.11472f, 1.55194f, 0, 0, 0.700409f, 0.713742f);
            });
            _events.ScheduleEvent((uint)EventIds.WaterfallWarning, Misc.WaterfallTimerMin, Misc.WaterfallTimerMax);
            _pipeKnockBackTimer = Misc.PipeKnockbackFirstDelay;

            // Remove collision and water
            GameObject go = battlegroundMap.GetGameObject(_water1GUID);
            if (go != null)
                go.UseDoorOrButton();
            go = battlegroundMap.GetGameObject(_water2GUID);
            if (go != null)
                go.UseDoorOrButton();

            foreach (var (playerGuid, _) in battleground.GetPlayers())
            {
                Player player = Global.ObjAccessor.FindPlayer(playerGuid);
                if (player != null)
                    player.RemoveAurasDueToSpell((uint)SpellIds.WarlDemonicCircle);
            }
        }

        void AddObject(uint entry, float x, float y, float z, float o, float rotation0, float rotation1, float rotation2, float rotation3, GameObjectState goState, List<ObjectGuid> guidList)
        {
            GameObject go = CreateObject(entry, x, y, z, o, rotation0, rotation1, rotation2, rotation3, goState);
            if (go != null)
                guidList.Add(go.GetGUID());
        }

        public override void SetData(uint dataId, uint value)
        {
            base.SetData(dataId, value);
            if (dataId == Misc.DataPipeKnockbackCount)
                _pipeKnockBackCount = value;
        }

        public override uint GetData(uint dataId)
        {
            if (dataId == Misc.DataPipeKnockbackCount)
                return _pipeKnockBackCount;

            return base.GetData(dataId);
        }
    }

    [Script]
    class at_ds_pipe_knockback : AreaTriggerScript
    {
        public at_ds_pipe_knockback() : base("at_ds_pipe_knockback") { }

        void Trigger(Player player)
        {
            Battleground battleground = player.GetBattleground();
            if (battleground != null)
            {
                if (battleground.GetStatus() != BattlegroundStatus.InProgress)
                    return;

                // Remove effects of Demonic Circle Summon
                player.RemoveAurasDueToSpell((uint)SpellIds.WarlDemonicCircle);

                // Someone has get back into the pipes and the knockback has already been performed,
                // so we reset the knockback count for kicking the player again into the arena.
                if (battleground.GetBgMap().GetBattlegroundScript().GetData(Misc.DataPipeKnockbackCount) >= Misc.PipeKnockbackTotalCount)
                    battleground.GetBgMap().GetBattlegroundScript().SetData(Misc.DataPipeKnockbackCount, 0);
            }
        }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger)
        {
            Trigger(player);
            return true;
        }

        public override bool OnExit(Player player, AreaTriggerRecord trigger)
        {
            Trigger(player);
            return true;
        }
    }
}
