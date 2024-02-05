// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.Magmus
{
    struct SpellIds
    {
        //Magmus
        public const uint Fieryburst = 13900;
        public const uint Warstomp = 24375;

        //IronhandGuardian
        public const uint Goutofflame = 15529;
    }

    enum Phases
    {
        One = 1,
        Two = 2
    }

    [Script]
    class boss_magmus : ScriptedAI
    {
        Phases phase;

        public boss_magmus(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            InstanceScript instance = me.GetInstanceScript();
            if (instance != null)
                instance.SetData(DataTypes.TypeIronHall, (uint)EncounterState.InProgress);

            phase = Phases.One;
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                DoCastVictim(SpellIds.Fieryburst);
                task.Repeat(TimeSpan.FromSeconds(6));
            });
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (me.HealthBelowPctDamaged(50, damage) && phase == Phases.One)
            {
                phase = Phases.Two;
                _scheduler.Schedule(TimeSpan.FromSeconds(0), task =>
                {
                    DoCastVictim(SpellIds.Warstomp);
                    task.Repeat(TimeSpan.FromSeconds(8));
                });
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);
        }

        public override void JustDied(Unit killer)
        {
            InstanceScript instance = me.GetInstanceScript();
            if (instance != null)
            {
                instance.HandleGameObject(instance.GetGuidData(DataTypes.DataThroneDoor), true);
                instance.SetData(DataTypes.TypeIronHall, (uint)EncounterState.Done);
            }
        }
    }

    [Script]
    class npc_ironhand_guardian : ScriptedAI
    {
        InstanceScript _instance;
        bool _active;

        public npc_ironhand_guardian(Creature creature) : base(creature)
        {
            _instance = me.GetInstanceScript();
            _active = false;
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void UpdateAI(uint diff)
        {
            if (!_active)
            {
                if (_instance.GetData(DataTypes.TypeIronHall) == (uint)EncounterState.NotStarted)
                    return;
                // Once the boss is engaged, the guardians will stay activated until the next instance reset
                _scheduler.Schedule(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), task =>
                {
                    DoCastAOE(SpellIds.Goutofflame);
                    task.Repeat(TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(21));
                });
                _active = true;
            }

            _scheduler.Update(diff);
        }
    }
}