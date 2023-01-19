// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.Broodlord
{
    struct SpellIds
    {
        public const uint Cleave = 26350;
        public const uint Blastwave = 23331;
        public const uint Mortalstrike = 24573;
        public const uint Knockback = 25778;
        public const uint SuppressionAura = 22247; // Suppression Device Spell
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SayLeash = 1;
    }

    struct EventIds
    {
        // Suppression Device Events
        public const uint SuppressionCast = 1;
        public const uint SuppressionReset = 2;
    }

    struct ActionIds
    {
        public const int Deactivate = 0;
    }

    [Script]
    class boss_broodlord : BossAI
    {
        public boss_broodlord(Creature creature) : base(creature, DataTypes.BroodlordLashlayer) { }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            Talk(TextIds.SayAggro);

            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                DoCastVictim(SpellIds.Cleave);
                task.Repeat(TimeSpan.FromSeconds(7));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.Blastwave);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.Mortalstrike);
                task.Repeat(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(30), task =>
            {
                DoCastVictim(SpellIds.Knockback);
                if (GetThreat(me.GetVictim()) != 0)
                    ModifyThreatByPercent(me.GetVictim(), -50);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                if (me.GetDistance(me.GetHomePosition()) > 150.0f)
                {
                    Talk(TextIds.SayLeash);
                    EnterEvadeMode(EvadeReason.Boundary);
                }
                task.Repeat(TimeSpan.FromSeconds(1));
            });
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();

            List<GameObject> _goList = me.GetGameObjectListWithEntryInGrid(BWLGameObjectIds.SuppressionDevice, 200.0f);
            foreach (var go in _goList)
                go.GetAI().DoAction(ActionIds.Deactivate);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }

    [Script]
    class go_suppression_device : GameObjectAI
    {
        InstanceScript _instance;
        bool _active;

        public go_suppression_device(GameObject go) : base(go)
        {
            _instance = go.GetInstanceScript();
            _active = true;
        }

        public override void InitializeAI()
        {
            if (_instance.GetBossState(DataTypes.BroodlordLashlayer) == EncounterState.Done)
            {
                Deactivate();
                return;
            }

            _events.ScheduleEvent(EventIds.SuppressionCast, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5));
        }

        public override void UpdateAI(uint diff)
        {
            _events.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventIds.SuppressionCast:
                        if (me.GetGoState() == GameObjectState.Ready)
                        {
                            me.CastSpell(null, SpellIds.SuppressionAura, true);
                            me.SendCustomAnim(0);
                        }
                        _events.ScheduleEvent(EventIds.SuppressionCast, TimeSpan.FromSeconds(5));
                        break;
                    case EventIds.SuppressionReset:
                        Activate();
                        break;
                }
            });
        }

        public override void OnLootStateChanged(uint state, Unit unit)
        {
            switch ((LootState)state)
            {
                case LootState.Activated:
                    Deactivate();
                    _events.CancelEvent(EventIds.SuppressionCast);
                    _events.ScheduleEvent(EventIds.SuppressionReset, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(120));
                    break;
                case LootState.JustDeactivated: // This case prevents the Gameobject despawn by Disarm Trap
                    me.SetLootState(LootState.Ready);
                    break;
            }
        }

        public override void DoAction(int action)
        {
            if (action == ActionIds.Deactivate)
            {
                Deactivate();
                _events.CancelEvent(EventIds.SuppressionReset);
            }
        }

        void Activate()
        {
            if (_active)
                return;
            _active = true;
            if (me.GetGoState() == GameObjectState.Active)
                me.SetGoState(GameObjectState.Ready);
            me.SetLootState(LootState.Ready);
            me.RemoveFlag(GameObjectFlags.NotSelectable);
            _events.ScheduleEvent(EventIds.SuppressionCast, TimeSpan.FromSeconds(0));
        }

        void Deactivate()
        {
            if (!_active)
                return;
            _active = false;
            me.SetGoState(GameObjectState.Active);
            me.SetFlag(GameObjectFlags.NotSelectable);
            _events.CancelEvent(EventIds.SuppressionCast);
        }
    }
}

