// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.Pets.Shaman
{
    [Script]
    class npc_pet_shaman_earth_elemental : ScriptedAI
    {
        const uint SpellShamanAngeredearth = 36213;

        public npc_pet_shaman_earth_elemental(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(0), task =>
            {
                DoCastVictim(SpellShamanAngeredearth);
                task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_pet_shaman_fire_elemental : ScriptedAI
    {
        const uint SpellShamanFireblast = 57984;
        const uint SpellShamanFirenova = 12470;
        const uint SpellShamanFireshield = 13376;

        public npc_pet_shaman_fire_elemental(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellShamanFirenova);
                task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellShamanFireblast);
                task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(0), task =>
            {
                DoCastVictim(SpellShamanFireshield);
                task.Repeat(TimeSpan.FromSeconds(2));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (me.HasUnitState(UnitState.Casting))
                return;

            _scheduler.Update(diff, DoMeleeAttackIfReady);
        }
    }
}